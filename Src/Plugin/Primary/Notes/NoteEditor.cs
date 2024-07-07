// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.GUIControls.MshtmlBrowser;
using JetBrains.Omea.HTML;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Notes
{
    public class NoteEditor : AbstractEditPane, IContextProvider
    {
        public const string _DefaultTitle = "New Note";

		private MshtmlEdit  _htmled;
        private Label       _subject;
        private JetTextBox _txtTitle;
        private RichEditToolbar _toolbar;
        private Panel       _panelSubject;
        private Panel       _panelBody;
        private Panel       _panelCategories;
        private CategoriesSelector  _selector;

        private IResource   _item;
        private IResource   _linkedFlag;

		private Container components = null;

        /// <summary>
        /// <c>True</c> if we have edited the title, <c>False</c> otherwise.
        /// Also used to determine whether we use the editor contents or the
        /// source LongBody property when sending the item.
        /// </summary>
        protected bool _dirty = false;

        /// <summary>
		/// Initializes the object.
		/// </summary>
        public NoteEditor()
        {
            InitializeComponentSelf();
            _toolbar.ContextProvider = this;
        }

        /// <param name="item">An item that provides the base text for editing the note.</param>
        public override void EditResource( IResource item )
        {
            _item = item;
            _linkedFlag = _item.GetLinkProp( "Flag" );

            // Populate the editing fields with initial values
            string  title = _item.GetPropText( Core.Props.Subject );
            string  body = _item.GetPropText( Core.Props.LongBody );

            _txtTitle.Text = title;
            _selector.Resource = _item;

		    WebSecurityContext	_ctx;
            _ctx = WebSecurityContext.Restricted;
            _ctx.WorkOffline = true;
            _ctx.ShowPictures = true;
            _htmled.SetHtml( "<html>\n<body>\n" + body + "\n</body>\n</html>\n", _ctx );

            //  Do not mark as dirty the existing note so that immediate "Esc"
            //  does not cause extra confirmation.
            string defTitle = "New " + Core.ResourceStore.ResourceTypes[ "Note" ].DisplayName;

            _dirty = (title != defTitle || body.Length != 0);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if( components != null )
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Visual Init
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponentSelf()
		{
            _subject = new Label();
			_txtTitle = new JetTextBox();
//            _toolbar = new RichEditToolbarFull();
            _toolbar = new RichEditToolbar();
            _htmled = new MshtmlEdit();
            _panelSubject = new Panel();
            _panelBody = new Panel();
            _panelCategories = new Panel();
            _selector = new CategoriesSelector();
			SuspendLayout();

            //
            // _panelSubject
            //
            _panelSubject.Location = new Point( 0, 0 );
            _panelSubject.Size = new Size( 800, 30 );
            _panelSubject.Controls.Add( _subject );
            _panelSubject.Controls.Add( _txtTitle );
            _panelSubject.Dock = DockStyle.Top;
            //
            // _subject
            //
            _subject.Location = new Point( 4, 7 );
            _subject.Name = "_subject";
            _subject.TabStop = false;
            _subject.Text = "&Subject:";
            _subject.Size = new Size(65, 25);
            //
			// _txtTitle
			//
            _txtTitle.Location = new Point( 80, 4 );
            _txtTitle.Size = new Size( 700, 25 );
            _txtTitle.Name = "_txtTitle";
			_txtTitle.TabStop = false;
			_txtTitle.Text = "";
			_txtTitle.TextChanged += OnTitleChanged;
			_txtTitle.KeyDown += OnEditorKeyDown;
			//
			// _htmled
			//
			_htmled.Name = "_htmled";
			_htmled.TabIndex = 1;
			_htmled.Dock = DockStyle.Fill;
			_htmled.add_KeyDown( new KeyEventHandler( OnEditorKeyDown ) );
            _htmled.add_PropertyChanged( new EventHandler( OnPropChanged ) );
            _htmled.add_PasteHandler( new EventHandler( OnPropChanged ) );
			//
			// _toolbar
			//
//			_toolbar.DropDownArrows = true;
			_toolbar.Location = new Point( 36, 0 );
			_toolbar.Name = "_toolbar";
			_toolbar.TabStop = false;
            _toolbar.Dock = DockStyle.Top;
            //
			// _panelBody
			//
			_panelBody.BorderStyle = BorderStyle.Fixed3D;
            _panelBody.Controls.Add( _htmled );
            _panelBody.Controls.Add( _toolbar );
            _panelBody.Dock = DockStyle.Fill;
			_panelBody.Name = "_panelBody";
			_panelBody.TabIndex = 1;
            //
            // _panelCategories
            //
            _panelCategories.Controls.Add( _selector );
            _panelCategories.BorderStyle = BorderStyle.Fixed3D;
            _panelCategories.Size = new Size( 800, 40 );
            _panelCategories.Dock = DockStyle.Bottom;
            _panelCategories.Name = "_panelCategories";
            _panelCategories.TabIndex = 2;
            //
            // _selector
            //
            _selector.Dock = DockStyle.Fill;
            _selector.Location = new Point(0, 0);
            _selector.Name = "_selector";
            _selector.Size = new Size(320, 40);
            _selector.TabIndex = 3;
            //
			// NoteComposer
			//
			ClientSize = new Size( 800, 600 );
            Controls.Add( _panelBody );
            Controls.Add( _panelCategories );
            Controls.Add( _panelSubject );
            ParentChanged += NoteEditor_ParentChanged;
			Name = "NoteComposer";
			Text = "Edit a Note";
			ResumeLayout( false );
        }
		#endregion

		/// <summary>
		/// Submits the blog posting to blog server using the extension that is controlled by this instance of composer.
		/// </summary>
        public override void Save()
        {
            _htmled.Focus();
		    string oldSubject = _item.GetPropText( Core.Props.Subject );
		    string oldBody = _item.GetPropText( Core.Props.LongBody );

            ResourceProxy proxy = new ResourceProxy( _item );
            try
            {
                proxy.BeginUpdate();
                if( !String.IsNullOrEmpty( _txtTitle.Text ) )
                    proxy.SetProp( Core.Props.Subject, _txtTitle.Text );
                proxy.SetProp( Core.Props.Date, DateTime.Now );
                proxy.SetProp( Core.Props.LongBodyIsHTML, true );
                IResource wsp = Core.WorkspaceManager.ActiveWorkspace;
                if( wsp != null )
                {
                    proxy.SetProp( "WorkspaceVisible", wsp );
                }
                _htmled.Focus();
                proxy.SetProp( Core.Props.LongBody, _htmled.ManagedHtmlDocument.Body.InnerHtml );
            }
            finally
            {
                proxy.EndUpdate();
            }
            if ( oldSubject != _item.GetPropText( Core.Props.Subject ) ||
                 oldBody != _item.GetPropText( Core.Props.LongBody ) )
            {
                Core.TextIndexManager.QueryIndexing( _item.Id );
            }
		}

        public override void Cancel()
        {
            if( _linkedFlag == null )
                new ResourceProxy( _item ).DeleteLinks( "Flag" );
            else
                new ResourceProxy( _item ).SetProp( "Flag", _linkedFlag );
        }

		private void OnTitleChanged( object sender, EventArgs e )
		{
			_dirty = true;
		}

		/// <summary>
		/// A key has been pressed in the MSHTML editor control.
		/// </summary>
		protected void OnEditorKeyDown( object sender, KeyEventArgs e )
		{
			// Ctrl+Enter / Alt+S means Submit
			if(( e.KeyData == (Keys.Enter | Keys.Control) ) || ( e.KeyData == (Keys.S | Keys.Alt) ))
			{
                Form form = FindForm();
                Save();
                Core.UserInterfaceAP.QueueJob( new MethodInvoker( form.Close ) );
                Core.ResourceBrowser.RedisplaySelectedResource();
                e.Handled = true;
            }
			else if( e.KeyData == Keys.Escape )
			{
                Form form = FindForm();
                Core.UserInterfaceAP.QueueJob( new MethodInvoker( form.CancelButton.PerformClick ) );
                e.Handled = true;
            }
        }

		protected void OnPropChanged( object sender, EventArgs e )
		{
            if( !_dirty )
            {
                String inner = _htmled.Html;
                if( !String.IsNullOrEmpty( inner ))
                {
                    MessageBox.Show( "OPS" );
                }
            }
        }

		protected void OnPaste( object sender, EventArgs e )
		{
            if( !_dirty )
            {
                string body = _htmled.Html;
                string title = "";
                StringReader reader = new StringReader( body );
                using (HTMLParser parser = new HTMLParser( reader, true ) )
                {
                    while( !parser.Finished )
                    {
                        string fragment = parser.ReadNextFragment();
                        if (fragment.Length > 0)
                        {
                            if( parser.InHeading )
                                title += fragment;
                            else
                            if( title.Length > 0 )
                                break;
                        }
                    }
                }

                if( title.Length > 0 )
                    _subject.Text = title;
            }
		}

        private void NoteEditor_ParentChanged(object sender, EventArgs e)
        {
            Control parent = Parent;
            if( parent != null )
            {
                while( !( parent is Form ))
                {
                    parent = parent.Parent;
                }
                Assembly theAsm = Assembly.GetExecutingAssembly();
                Form form = FindForm();
                form.Icon = new Icon( theAsm.GetManifestResourceStream( "NotesPlugin.Icons.Note.ico" ) );
            }
        }

        #region IContextProvider Members

        public IActionContext GetContext( ActionContextKind kind )
        {
            ActionContext context = new ActionContext( ActionContextKind.Toolbar, this, Core.ResourceBrowser.SelectedResources );
            context.SetSelectedText( _htmled.SelectedHtml, _htmled.SelectedText, TextFormat.Html );
            context.SetCommandProcessor( _htmled );
            context.SetOwnerForm( FindForm() );
            return context;
        }
        #endregion
    }
}
