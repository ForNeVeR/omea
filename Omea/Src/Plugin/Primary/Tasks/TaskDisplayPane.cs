/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.Tasks
{
    internal class TaskDisplayPane : MessageDisplayPane, IContextProvider
    {
        private System.ComponentModel.Container components = null;

        private IResource _task;
        private IResourceList _taskListener;

        private const string _StylePath = "Tasks.Styles.TaskView.css";
        private const string _Script = "<script type=\"text/javascript\">\n" + 
                                        "function doIt(link, el) {" + 
                                        "  if (el)" + 
                                        "    if (el.className == \"displayNone\") {" + 
                                        "      el.className = \"displayBlock\";" + 
                                        "      link.className = \"block\";" + 
                                        "    } else { el.className = \"displayNone\";link.className = \"\"; }" + 
                                        "}\n</script>";
        private const string _cSeparatorLine = "<tr><td colspan=\"2\"><hr/></td></tr>";
        private static string _style;

        /// <summary>
        /// The Web Security Context that displays the task preview by default,
        /// in the restricted environment.
        /// </summary>
        private readonly WebSecurityContext _ctxRestricted;

        public TaskDisplayPane()
        {
            InitializeComponent();            

            // Initialize the security context
            _ctxRestricted = WebSecurityContext.Trusted;
            _ctxRestricted.WorkOffline = false;	// Enable downloading of the referenced content
            _ctxRestricted.ShowPictures = true;

            _headerPane.Visible = false;
        }

        private void  InitializeContactChangeListener()
        {
            _taskListener = _task.ToResourceListLive();
            _taskListener.ResourceChanged += OnContactChanged;
        }

        private void DisposeContactResourceList()
        {
            if ( _taskListener != null )
            {
                _taskListener.ResourceChanged -= OnContactChanged;
                _taskListener.Dispose();
                _taskListener = null;
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            Name = "TaskDisplayPane";
            Size = new Size(608, 280);
            ResumeLayout(false);
        }

        private static string  Style
        {
            get
            {
                if( _style == null )
                {
                    Assembly theAssm = Assembly.GetExecutingAssembly();
                    Stream strm = theAssm.GetManifestResourceStream( _StylePath );
                    _style = Utils.StreamToString( strm );
                }
                return _style;
            }
        }

        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if(components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

        public override void DisplayResource( IResource task, WordPtr[] wordsToHighlight )
        {
            _ieBrowser.Visible = true;
            _ieBrowser.ContextProvider = this;
            _ieBrowser.ShowImages = true;

            _task = task;
            DisposeContactResourceList();
            InitializeContactChangeListener();

            ShowResourceContent();
        }

        public override void EndDisplayResource( IResource res )
        {
            _ieBrowser.Visible = true;
        }

        public override void DisposePane()
        { }

        private void OnContactChanged( object sender, ResourcePropIndexEventArgs e )
        {
            if ( IsDisposed )
                return;

            //  Some desynchronization is possible during visual context
            //  switching, thus check that handler is actually called for
            //  the appropriate resource.
            if( _task.Id == e.Resource.Id )
            {
                if ( InvokeRequired )
                {
                    Core.UIManager.QueueUIJob( new ResourcePropIndexEventHandler( OnContactChanged ), new[] { sender, e } );
                }
                else
                {
                    ShowResourceContent();
                }
            }
        }

        #region Show resource content in html
        private void  ShowResourceContent()
        {
            try
            {
                StringBuilder htmlCtor = StringBuilderPool.Alloc();

                string head = "<head>\n<title>Contacts</title>\n<style type=\"text/css\">\n\n" + Style + "</style>\n" + _Script + "</head>\n\n";

                htmlCtor.Append( "<html>" ).Append( head );
                htmlCtor.Append( "<body>" ).Append( "<h1>" + _task.DisplayName + "</h1>\n" );
                htmlCtor.Append( "<table id=\"main\" border=\"0\" cellpadding=\"0\" cellspacing=\"4\">\n" );
                htmlCtor.Append( "<tr class=\"top\">\n" );
                ContsructLeftColumn( htmlCtor );
                ContsructRightColumn( htmlCtor );
                htmlCtor.Append( "</tr>\n" );
                htmlCtor.Append( "</table>\n" );
                htmlCtor.Append( "</body></html>" );
                ShowHtml( htmlCtor.ToString(), _ctxRestricted, null );

                StringBuilderPool.Dispose( htmlCtor );
            }
            catch( Exception e )
            {
                Utils.DisplayException( e, "Error" );
                return;
            }
        }

        private void ContsructLeftColumn( StringBuilder strBuilder )
        {
            strBuilder.Append( "<td class=\"content\">\n" );
            strBuilder.Append( "<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\">\n" );
            strBuilder.Append( ObligatoryTag( _task, "Subject", Core.Props.Subject ) );
            strBuilder.Append( ObligatoryTag( _task, "Description", TasksPlugin._propDescription ) );
            strBuilder.Append( _cSeparatorLine );
            PrintLinkedResources( strBuilder );
            strBuilder.Append( _cSeparatorLine );
            strBuilder.Append( ObligatoryTag( _task, "On workspace", TasksPlugin._propRemindWorkspace ) );
            strBuilder.Append( "</table>\n</td>\n" );
        }

        private void ContsructRightColumn( StringBuilder strBuilder )
        {
            int status = _task.GetIntProp( TasksPlugin._propStatus );
            string strStatus = ( status >= 0 && status < TasksPlugin._statuses.Length ) ? 
                                TasksPlugin._statuses[ status ] : string.Empty;
            int priority = _task.GetIntProp( TasksPlugin._propPriority );
            string strPri = ( priority >= 0 && priority < TasksPlugin._priorities.Length ) ? 
                            TasksPlugin._priorities[ priority ] : string.Empty;

            strBuilder.Append( "<td class=\"content\">\n" );
            strBuilder.Append( "<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\">\n" );
            strBuilder.Append( ObligatoryTag( "Status", strStatus ) );
            strBuilder.Append( ObligatoryTag( "Priority", strPri ) );
            strBuilder.Append( _cSeparatorLine );
            strBuilder.Append( ObligatoryTag( _task, "Start date and time", TasksPlugin._propStartDate ) );
            strBuilder.Append( ObligatoryTag( _task, "Due date and time", Core.Props.Date ) );
            strBuilder.Append( ObligatoryTag( _task, "Completed at", TasksPlugin._propCompletedDate ) );
            strBuilder.Append( ObligatoryTag( _task, "Reminder at", TasksPlugin._propRemindDate ) );
            strBuilder.Append( "</table>\n</td>\n" );
        }

        private static string ObligatoryTag( IResource res, string head, int prop )
        {
            string result = "<tr><td>" + head + "</td>";
            string text = res.GetPropText( prop );
            result += (text.Length > 0)? "<td class=\"name\">" + text + "</td>" : ContactViewStandardTags.NotSpecifiedHtmlText;
            result += "</tr>\n";
            return result;
        }

        private static string ObligatoryTag( string head, string val )
        {
            string result = "<tr><td>" + head + "</td>";
            result += !String.IsNullOrEmpty( val ) ? "<td class=\"name\">" + val + "</td>" : ContactViewStandardTags.NotSpecifiedHtmlText;
            result += "</tr>\n";
            return result;
        }

        private void PrintLinkedResources( StringBuilder strBuilder )
        {
            IResourceList list = _task.GetLinksTo( null, TasksPlugin._linkTarget );

            strBuilder.Append( "<tr><td>Linked Resources</td>" );
            if( list.Count == 0 )
                strBuilder.Append( ContactViewStandardTags.NotSpecifiedHtmlText );
            else
            {
                strBuilder.Append( "<td>" );
                foreach( IResource res in list )
                {
                    string visibleRef = "<a href=\"omea://" + res.Id + "/\">" + res.GetPropText( Core.Props.Subject ) + "</a><br/>";
                    Icon resIcon = Core.ResourceIconManager.GetResourceIconProvider( res.Type ).GetResourceIcon( res );
                    if( resIcon != null )
                    {
                        Image img = GraphicalUtils.ConvertIco2Bmp( resIcon, new SolidBrush( Color.FromArgb( 0xF6, 0xF4, 0xEC )) );
                        string path = Utils.IconPath( img );
                        strBuilder.Append( "<img src=\"" + path + "\" >&nbsp;" );
                    }
                    strBuilder.Append( visibleRef );
                }
                strBuilder.Append( "</td>" );
            }
            strBuilder.Append( "</tr>\n" );
        }

        #endregion Show resource content in html

        #region IContextProvider Members
        public IActionContext GetContext( ActionContextKind kind )
        {
            return new ActionContext( kind, null, (_task == null) ? null : _task.ToResourceList() );
        }
        #endregion IContextProvider Members
    }
}