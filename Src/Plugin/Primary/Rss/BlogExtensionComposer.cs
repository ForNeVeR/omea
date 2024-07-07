// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.GUIControls.MshtmlBrowser;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.RSSPlugin;
using Syndication.Extensibility;

namespace RSSPlugin
{
	/// <summary>
	/// Summary description for BlogExtensionComposer.
	/// </summary>
	public class BlogExtensionComposer : Form, IContextProvider
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		protected Container components = null;

		protected MshtmlEdit _htmled;

		protected RichEditToolbar _toolbar;

		protected JetTextBox _txtTitle;

		protected Panel _panelBody;

		/// <summary>
		/// Initializes the object.
		/// </summary>
		/// <param name="extension">IBlogExtension-implementing object to which the blog item will be submitted for publishing.</param>
		/// <param name="item">An item that provides the base text for composing the blog post.</param>
		/// <param name="feed">A feed from which the <paramref name="item"/> originates.</param>
		protected BlogExtensionComposer( IBlogExtension extension, IResource item, IResource feed )
		{
			// Store the parameters
			_extension = extension;
			_item = item;
			_feed = feed;

			// Prepare for loading the icons
			Assembly assembly = Assembly.GetExecutingAssembly();

			//
			// Required for Windows Form Designer support
			//
			InitializeComponentSelf();

			// Assign a context provider to the toolbar
			_toolbar.ContextProvider = this;

			// Add the Submit icon
			string sGroup = "File";
			_toolbar.ActionManager.RegisterActionGroup( sGroup, ListAnchor.First );
			_toolbar.ActionManager.RegisterAction(
				new MethodInvokerAction( new ActionExecuteDelegate( OnSubmitAction ),
				                         new ActionUpdateDelegate( OnUpdateSubmitAction ) ),
				sGroup,
				ListAnchor.Last,
				JetBrains.Omea.RSSPlugin.RSSPlugin.LoadIconFromAssembly( "BlogExtensionComposer.Submit.ico" ),
				"&Submit",
				"Submit (Ctrl+Enter)",
				null,
				null );

			// Load the window icon.
			Icon = JetBrains.Omea.RSSPlugin.RSSPlugin.LoadIconFromAssembly( "BlogExtensionComposer.Window.ico" );

			// Populate the editing fields with initial values
			_txtTitle.Text = _item.GetPropText( Core.Props.Subject );
			_htmled.Html = "<html>\n<body>\n" + _item.GetPropText( Core.Props.LongBody ) + "\n</body>\n</html>\n";
		}

		/// <summary>
		/// IBlogExtension-implementing object to which the blog item will be submitted for publishing.
		/// </summary>
		protected IBlogExtension _extension;

		/// <summary>
		/// An item that provides the base text for composing the blog post.
		/// </summary>
		protected IResource _item;

		/// <summary>
		/// A feed from which the <see cref="_item"/> originates.
		/// </summary>
		protected IResource _feed;

		/// <summary>
		/// <c>True</c> if we have edited the description, <c>False</c> otherwise.
		/// Also used to determine whether we use the editor contents or the source LongBody property when sending the item.
		/// </summary>
		protected bool _dirty = false;

		/// <summary>
		/// Composes the blog posting and submits via the given extension.
		/// If the extension does not support editing UI, own UI is shown.
		/// </summary>
		/// <param name="extension">IBlogExtension-implementing object to which the blog item will be submitted for publishing.</param>
		/// <param name="item">An item that provides the base text for composing the blog post.</param>
		/// <param name="feed">A feed from which the <paramref name="item"/> originates.</param>
		public static void Compose( IBlogExtension extension, IResource item, IResource feed )
		{
			BlogExtensionComposer composer = new BlogExtensionComposer( extension, item, feed );

			// Display editing UI if the extension does not provide its own
			if( !extension.HasEditingGUI )
				composer.Show(); // This will handle submit, when done
			else // Extension has its own UI, submit immediately
				composer.Submit( false ); // False — do not take values from UI
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
			_txtTitle = new JetTextBox();
			_toolbar = new RichEditToolbar();
			_htmled = new MshtmlEdit();
			Closing += new CancelEventHandler( OnBeforeCloseForm );
			SuspendLayout();
			//
			// _txtTitle
			//
			_txtTitle.Location = new Point( 36, 100 );
			_txtTitle.Name = "_txtTitle";
			_txtTitle.TabIndex = 1;
			_txtTitle.Text = "";
			_txtTitle.Dock = DockStyle.Top;
			_txtTitle.TextChanged += new EventHandler( OnTitleChanged );
			_txtTitle.KeyDown += new KeyEventHandler( OnEditorKeyDown );
			//
			// _htmled
			//
			_htmled.Name = "_htmled";
			_htmled.TabIndex = 2;
			_htmled.Dock = DockStyle.Fill;
			_htmled.add_KeyDown( new KeyEventHandler( OnEditorKeyDown ) );
			//
			// _toolbar
			//
//			_toolbar.DropDownArrows = true;
			_toolbar.Location = new Point( 0, 0 );
			_toolbar.Name = "_toolbar";
			_toolbar.TabStop = true;
			_toolbar.TabIndex = 3;
			//
			// _panelBody
			//
			_panelBody = new Panel();
			_panelBody.BorderStyle = BorderStyle.Fixed3D;
			_panelBody.Controls.Add( _htmled );
			_panelBody.Dock = DockStyle.Fill;
			_panelBody.Name = "_panelBody";
			_panelBody.TabIndex = 2;
			//
			// BlogExtensionComposer
			//
			AutoScaleBaseSize = new Size( 5, 13 );
			ClientSize = new Size( 800, 600 );
			Controls.Add( _panelBody );
			Controls.Add( _txtTitle );
			Controls.Add( _toolbar );
			Name = "BlogExtensionComposer";
			Text = "Compose a Blog Entry";
			ResumeLayout( false );
		}

		#endregion

		/// <summary>
		/// Submits the blog posting to blog server using the extension that is controlled by this instance of composer.
		/// </summary>
		/// <param name="bUIInvolved">
		/// <para>If <c>True</c>, then the content-editing UI was shown, and the item may have been edited. If edited, its content should be taken out from the editing form. if not edited, the content may be taken directly from the resource.</para>
		/// <para>If <c>False</c>, no UI has been shown at all and visual elements should not be consulted.</para>
		/// </param>
		protected void Submit( bool bUIInvolved )
		{
			//////////
			// Obtain content

			string sSubject;
			string sBody;

			// Retrieve either from the source or from UI
			if( (bUIInvolved) && (_dirty) ) // UI is allowed and there were some changes made to the content it
			{
				sSubject = _txtTitle.Text;
				sBody = GetEditedContent();
			}
			else // UI not allowed, or no changes and no need to take from UI
			{
				sSubject = _item.GetPropText( Core.Props.Subject );
				sBody = _item.GetPropText( Core.Props.LongBody );
			}

			//////////
			// create and Submit

			XmlDocument doc = new XmlDocument();
			doc.AppendChild( doc.CreateElement( "rss" ) );
			XmlElement channel = doc.CreateElement( "channel" );
			doc[ "rss" ].AppendChild( channel );

			if( _feed != null )
			{
				AddChildNode( channel, "title", _feed.GetPropText( Core.Props.Name ) );
				AddChildNode( channel, "link", _feed.GetPropText( Props.HomePage ) );
			}

			XmlElement item = doc.CreateElement( "item" );
			channel.AppendChild( item );

			AddChildNode( item, "title", sSubject );
			AddChildNode( item, "description", sBody );
			AddChildNode( item, "link", _item.GetPropText( Props.Link ) );

			try
			{
				_extension.BlogItem( item, false );

				if( Visible )
					Close();
			}
			catch( Exception ex )
			{
				MessageBox.Show( (Visible ? this : Core.MainWindow), // Use this form as parent, if available
				                 "Extension has failed to post the blog item.\n\n" + ex.Message,
				                 "Blog Extension – " + Core.ProductFullName, MessageBoxButtons.OK, MessageBoxIcon.Error );
			}
		}

		/// <summary>
		/// Adds a child element with a specific text value to the XML element.
		/// </summary>
		protected static void AddChildNode( XmlNode parentNode, string tagName, string value )
		{
			XmlNode childNode = parentNode.OwnerDocument.CreateElement( tagName );
			childNode.InnerText = value;
			parentNode.AppendChild( childNode );
		}

		/// <summary>
		/// Retrieves the body from the HTML Editor.
		/// </summary>
		/// <returns></returns>
		protected string GetEditedContent()
		{
			// Retrieve the editing results
			return _htmled.ManagedHtmlDocument.Body.InnerHtml;
		}

		private void OnTitleChanged( object sender, EventArgs e )
		{
			_dirty = true;
		}

		#region IContextProvider Members

		public IActionContext GetContext( ActionContextKind kind )
		{
			ActionContext context = new ActionContext( ActionContextKind.Toolbar, this, null );

			// TODO: take the proper member
			/*if(_txtTitle.Focused)
				context.SetSelectedText(_txtTitle.Text, _txtTitle.Text, TextFormat.PlainText);
			else if(_htmled.Focused)
			{*/
			context.SetSelectedText( _htmled.SelectedHtml, _htmled.SelectedText, TextFormat.Html );
			context.SetCommandProcessor( _htmled );
			//}

			context.SetOwnerForm( this );
			return context;
		}

		#endregion

		/// <summary>
		/// Action execution handler for the Submit toolbar button.
		/// </summary>
		private void OnSubmitAction( IActionContext context )
		{
			Submit( true );
		}

		/// <summary>
		/// Action UI handler for the Submit toolbar button.
		/// </summary>
		private void OnUpdateSubmitAction( IActionContext context, ref ActionPresentation presentation )
		{
			presentation.Enabled = true;
		}

		/// <summary>
		/// A key has been pressed in the MSHTML editor control.
		/// </summary>
		protected void OnEditorKeyDown( object sender, KeyEventArgs e )
		{
			// Ctrl+Enter / Alt+S means Submit
			if( (e.KeyData == (Keys.Enter | Keys.Control)) || (e.KeyData == (Keys.S | Keys.Alt)) )
			{
				e.Handled = true;
				Submit( true );
			}
				// ESC means close without saving
			else if( e.KeyData == Keys.Escape )
			{
				e.Handled = true;
				Close(); // User will be prompted, if needed
			}

			_dirty = true; // Some key has been pressed, probably the text has gotten dirty
		}

		/// <summary>
		/// Raises when the form is about to be closed, prompts user if there were changes to the content.
		/// </summary>
		protected void OnBeforeCloseForm( object sender, CancelEventArgs e )
		{
			if( CanClose )
				e.Cancel = true;
		}

		/// <summary>
		/// If the document is dirty, prompts the user whether the form can be closed.
		/// </summary>
		/// <returns>Whether the form is allowed to close.</returns>
		protected bool CanClose
		{
			get
			{
				// Prompt user if dirty
				if( _dirty )
				{
					if( MessageBox.Show( "Changes you have made will be discarded.", Core.ProductFullName, MessageBoxButtons.OKCancel, MessageBoxIcon.Exclamation ) == DialogResult.Cancel )
						return true;
				}
				return false;
			}
		}
	}
}
