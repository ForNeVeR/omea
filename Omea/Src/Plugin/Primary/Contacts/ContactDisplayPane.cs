// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ContactsPlugin
{
    internal class ContactDisplayPane : MessageDisplayPane, IContextProvider, IContactBlockContainer
    {
        private const string _cDefaultPicture = "ContactsPlugin.Icons.contact32.png";
        private const string _StylePath = "ContactsPlugin.Styles.ContactView.css";
        private const string _ExpandIconEmbeddedResourcePath = "ContactsPlugin.Icons.ExpandPanel.gif";
        private const string _ExpandIconHoverEmbeddedResourcePath = "ContactsPlugin.Icons.ExpandPanelHover.gif";
        private const string _CollapseIconEmbeddedResourcePath = "ContactsPlugin.Icons.CollapsePanel.gif";
        private const string _CollapseIconHoverEmbeddedResourcePath = "ContactsPlugin.Icons.CollapsePanelHover.gif";
        private const string _Script = "<script type=\"text/javascript\">\n" +
                                        "function doIt(link, el) {" +
                                        "  if (el)" +
                                        "    if (el.className == \"displayNone\") {" +
                                        "      el.className = \"displayBlock\";" +
                                        "      link.className = \"block\";" +
                                        "    } else { el.className = \"displayNone\";link.className = \"\"; }" +
                                        "}\n</script>";

        private System.ComponentModel.Container components = null;

        private IResource       _contact;
        private IResourceList   _contactResourceList;
        private readonly ArrayList _leftBlocks, _rightBlocks;

        private static string   _style = null;
        private static string _expandIconPath, _expandIconHoverPath;
        private static string _collapseIconPath, _collapseIconHoverPath;

        /// <summary>
        /// The Web Security Context that displays the Contact preview by default,
        /// in the restricted environment.
        /// </summary>
        private readonly WebSecurityContext _ctxRestricted;

        public ContactDisplayPane()
        {
            InitializeComponent();

            // Initialize the security context
            _ctxRestricted = WebSecurityContext.Trusted;
            _ctxRestricted.WorkOffline = false;	// Enable downloading of the referenced content
            _ctxRestricted.ShowPictures = true;

            _leftBlocks = new ArrayList();
            _rightBlocks = new ArrayList();

            ContactService.GetInstance().CreateContactBlocks( this );

            _headerPane.Visible = false;
        }

        private void  InitializeContactChangeListener()
        {
            _contactResourceList = _contact.ToResourceListLive();
            _contactResourceList.ResourceChanged += OnContactChanged;
        }

        private void DisposeContactResourceList()
        {
            if ( _contactResourceList != null )
            {
                _contactResourceList.ResourceChanged -= OnContactChanged;
                _contactResourceList.Dispose();
                _contactResourceList = null;
            }
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
                    _style = _style.Replace("%1%", _expandIconPath);
                    _style = _style.Replace("%2%", _collapseIconPath);
                    _style = _style.Replace("%3%", _expandIconHoverPath);
                    _style = _style.Replace("%4%", _collapseIconHoverPath);
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

        public override void DisposePane() { }

        #region Component Designer generated code
        private void InitializeComponent()
        {
            this.SuspendLayout();
            //
            // ArticlePreviewPane
            //
            this.Name = "ContactViewPane";
            this.Size = new System.Drawing.Size(608, 280);
            this.ResumeLayout(false);

        }
        #endregion

        public override void DisplayResource( IResource contact, WordPtr[] wordsToHighlight )
        {
            _ieBrowser.Visible = true;
            _ieBrowser.ContextProvider = this;

            _contact = contact;
            InitializePictogramPaths();
            DisposeContactResourceList();
            InitializeContactChangeListener();

            ShowResourceContent();
        }

        public override void EndDisplayResource( IResource res )
        {
            _ieBrowser.Visible = true;
        }

        #region Impl
        private void OnContactChanged( object sender, ResourcePropIndexEventArgs e )
        {
            if ( IsDisposed )
                return;

            //  Some desynchronization is possible during visual context
            //  switching, thus check that handler is actually called for
            //  the appropriate resource.
            if( _contact.Id == e.Resource.Id )
            {
                if ( InvokeRequired )
                {
                    Core.UIManager.QueueUIJob( new ResourcePropIndexEventHandler( OnContactChanged ), new object[] { sender, e } );
                }
                else
                {
                    ShowResourceContent();
                }
            }
        }
        #endregion Impl

        #region Show Html Content
        private void ShowResourceContent()
        {
            try
            {
                StringBuilder htmlCtor = StringBuilderPool.Alloc();

                string head = "<head><title>Contacts</title><style type=\"text/css\">" + "\n" + Style + "</style>";
                htmlCtor.Append("<html>").Append(head).Append("\n").Append(_Script).Append("\n</head>\n").Append("<body>");

                AppendHeader( htmlCtor );

                htmlCtor.Append("<table id=\"main\" border=\"0\" cellpadding=\"0\" cellspacing=\"4\">\n");
                htmlCtor.Append("<tr class=\"top\">\n");
                ContsructColumn( _leftBlocks, htmlCtor );
                ContsructColumn( _rightBlocks, htmlCtor );
                htmlCtor.Append("</tr>\n");

                ConstructCorrespondenceBlock( htmlCtor );

                int mergeCandidates = ContactManager.GetContactsForMerging( _contact ).Count;
                if( mergeCandidates > 0 )
                {
                    string verb = (mergeCandidates == 1) ? " is " : " are ";
                    string ending = (mergeCandidates == 1) ? "" : "s";
                    string text = "<p style=\"color:#aca899\">&nbsp;There" + verb + mergeCandidates + " contact" + ending + " suggested for merging</p>";
                    htmlCtor.Append( "<tr><td class=\"text\" colspan=\"2\">" ).Append( text ).Append( "</td></tr>" );
                }
                htmlCtor.Append("</table>").Append("</body></html>");

                ShowHtml( htmlCtor.ToString(), _ctxRestricted, null );

                StringBuilderPool.Dispose( htmlCtor );
            }
            catch (Exception ex)
            {
                Core.ReportException(ex, false);
                return;
            }
        }

        private void AppendHeader( StringBuilder builder )
        {
            Image pic;

            builder.Append( "<h1>" );
            if( _contact.HasProp( Core.ContactManager.Props.Picture ) )
            {
                Stream stream = _contact.GetBlobProp( Core.ContactManager.Props.Picture );
                pic = Image.FromStream( stream );
            }
            else
            {
                pic = Utils.TryGetEmbeddedResourceImageFromAssembly( Assembly.GetExecutingAssembly(), _cDefaultPicture );
            }

            string picName = pic.GetHashCode() + ".png";
            string path = Path.Combine(Path.GetTempPath(), picName);
            if (!File.Exists(path))
            {
                Bitmap bmp = GraphicalUtils.ConvertIco2Bmp( pic, new SolidBrush( Color.FromArgb( 0x5A, 0x52, 0xB5 ) ));
                using (FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    bmp.Save( fs, ImageFormat.Png );
                }
            }
            builder.Append( "<img src=\"" + path + "\" style=\"float:left;margin-right:.5em\" >&nbsp;" );
            builder.Append( _contact.DisplayName + "</h1>\n" );
        }

        private void ContsructColumn( IList blocks, StringBuilder htmlCtor )
        {
            htmlCtor.Append( "<td class=\"content\">\n" );
            htmlCtor.Append( "<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\">" );

            for (int i = 0; i < blocks.Count; i++)
            {
                AbstractContactViewBlock block = (AbstractContactViewBlock)blocks[ i ];
                htmlCtor.Append( block.HtmlContent( _contact )).Append( "\n" );

                if( i < blocks.Count - 1 )
                    htmlCtor.Append( "\t<tr><td colspan=\"2\"><hr/></td></tr>\n" );
            }
            htmlCtor.Append("</table></td>\n");
        }

        private void ConstructCorrespondenceBlock( StringBuilder htmlCtor )
        {
            IResourceList deleted = Core.ResourceStore.FindResourcesWithProp(null, Core.Props.IsDeleted);
            IResourceList attaches = Core.ResourceStore.FindResourcesWithProp(null, "Attachment");

            IResourceList from = _contact.GetLinksOfType(null, Core.ContactManager.Props.LinkFrom).Minus(deleted).Minus(attaches);
            IResourceList to = _contact.GetLinksOfType(null, Core.ContactManager.Props.LinkTo).Minus(deleted).Minus(attaches);
            IResourceList cced = _contact.GetLinksOfType(null, Core.ContactManager.Props.LinkCC).Minus(deleted).Minus(attaches);
            int items = from.Count + to.Count + cced.Count;

            htmlCtor.Append("\n<tr><td colspan=\"2\" class=\"content bottom\">");
            htmlCtor.Append( "<div class=\"butt\">" );

            if (items > 0)
            {
                htmlCtor.Append( "<a href=\"#\" onclick=\"doIt(this,collaps);return false;\">.</a>" );
                htmlCtor.Append("Correspondence (" + items + " items)");
            }
            else
                htmlCtor.Append( "No correspondence with the contact" );
            htmlCtor.Append("</div>\n");

            if (items > 0)
            {
                htmlCtor.Append("<table id=\"collaps\" class=\"displayNone\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\">\n");
                if (from.Count > 0)
                {
                    ProcessList( "From", from, htmlCtor );
                }
                if (to.Count > 0)
                {
                    ProcessList( "To", to, htmlCtor );
                }
                if (cced.Count > 0)
                {
                    ProcessList( "CC", cced, htmlCtor );
                }
                htmlCtor.Append("</table>");
            }
            htmlCtor.Append("</td></tr>");
        }

        private static void ProcessList( string name, IResourceList list, StringBuilder result )
        {
            result.Append("<tr><td class=\"title\">" + name + ":</td>\n" );
            result.Append("    <td class=\"correspondence\">");

            int count = Math.Min(list.Count, 5);
            for( int i = 0; i < count; i++ )
            {
                string output = "<a href=\"omea://" + list[ i ].Id + "/\">" + list[ i ].GetPropText( Core.Props.Subject ) + "</a><br/>";
                Icon resIcon = Core.ResourceIconManager.GetResourceIconProvider( list[ i ].Type ).GetResourceIcon( list[ i ] );
                if( resIcon != null )
                {
                    Image img = GraphicalUtils.ConvertIco2Bmp( resIcon, new SolidBrush( Color.FromArgb( 0xF6, 0xF4, 0xEC )) );
                    string path = Utils.IconPath( img );
                    result.Append( "<img src=\"" + path + "\" >&nbsp;" );
                }
                result.Append( output );
            }
            if( list.Count > count )
            {
                result.Append( "and " + ( list.Count - count ) + " more" );
            }
            result.Append("</td></tr>\n");
        }

        public static void InitializePictogramPaths()
        {
            InitializePictogramPath(_ExpandIconEmbeddedResourcePath, ref _expandIconPath, "ExpandPanel.gif");
            InitializePictogramPath(_ExpandIconHoverEmbeddedResourcePath, ref _expandIconHoverPath, "ExpandPanelHover.gif");
            InitializePictogramPath(_CollapseIconEmbeddedResourcePath, ref _collapseIconPath, "CollapsePanel.gif");
            InitializePictogramPath(_CollapseIconHoverEmbeddedResourcePath, ref _collapseIconHoverPath, "CollapsePanelHover.gif");
        }

        public static void InitializePictogramPath( string resource, ref string path, string name )
        {
            Assembly theAssm = Assembly.GetExecutingAssembly();
            Stream strm = theAssm.GetManifestResourceStream(resource);
            byte[] content = new BinaryReader(strm).ReadBytes((int)strm.Length);

            string tempPath = Path.Combine(Path.GetTempPath(), name);
            FileStream outstream = null;
            try
            {
                outstream = new FileStream(tempPath, FileMode.Create);
            }
            catch (DirectoryNotFoundException)
            {
                try
                {
                    tempPath = Path.Combine(Application.UserAppDataPath, name);
                    outstream = new FileStream(tempPath, FileMode.Create);
                }
                catch (DirectoryNotFoundException)
                {
                    //  do nothing. May be try out another path later.
                }
            }
            if (outstream != null)
            {
                BinaryWriter writer = new BinaryWriter(outstream);
                writer.Write(content);
                writer.Close();
                path = tempPath;
            }
        }
        #endregion Show Html Content

        #region IContextProvider Members
        public IActionContext GetContext( ActionContextKind kind )
        {
            return new ActionContext( kind, null, (_contact == null) ? null : _contact.ToResourceList() );
        }
        #endregion IContextProvider Members

        #region IContactBlockContainer Members
        public void AddContactBlock(int col, string caption, AbstractContactViewBlock block)
        {
            if( col == 0 )
                _leftBlocks.Add( block );
            else
                _rightBlocks.Add( block );
        }
        #endregion
    }
}
