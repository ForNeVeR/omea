// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Components.ImageListButton;

namespace JetBrains.Omea
{
	/// <summary>
	/// Edit pane for Fragment resources.
	/// </summary>
    public class FragmentEditPane : AbstractEditPane, IContextProvider, ICommandProcessor
	{
		private Panel               _panelTop;
		private Label               _lblName;
		private JetTextBox          _edtName;

		private Panel               _panelContent;
		private AbstractWebBrowser  _browser;
		private JetRichTextBox      _richTextBox;

		private Panel               _panelAnnotation;
        private GroupBox            _boxAnnotation;
        private Label               _lblAnnotation;
        private ImageListButton     _btnHideShowAnnotation;
        private JetLinkLabel        _lblHideShowAnnotationText;
        private JetTextBox          _edtAnnotation;

        private CategoriesSelector  _selector;
        private Panel               _openOptionsPanel;
        private CheckBox            _chkOpenAfterSave;

        private const int     CollapsedPanelHeight = 30;
        private const int     MinimalPanelHeight = 100;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		private IResource   _fragment;
        private IResource   _linkedFlag;

		/// <summary>
		/// Whether we can display the resource already or not.
		/// </summary>
		protected bool	_isLoaded = false;

		/// <summary>
		/// Security context in which the clippings should be displayed.
		/// Derives from the Restricted context, but prohibits navigating in-place.
		/// </summary>
		protected WebSecurityContext	_ctxDisplayClipping;

	    public FragmentEditPane()
		{
			_ctxDisplayClipping = WebSecurityContext.Restricted;
			_ctxDisplayClipping.WorkOffline = false;
			_ctxDisplayClipping.AllowInPlaceNavigation = false;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			_richTextBox.ContextProvider = this;
			_edtAnnotation.ContextProvider = this;

			// Some custom init (to be not affected by the forms designer)
			InitializeBrowserComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
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

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this._edtAnnotation = new JetTextBox();
            this._edtName = new JetTextBox();
            this._lblName = new System.Windows.Forms.Label();
            this._richTextBox = new JetRichTextBox();
            this._panelContent = new System.Windows.Forms.Panel();
            this._panelAnnotation = new System.Windows.Forms.Panel();
            this._panelTop = new System.Windows.Forms.Panel();
            this._selector = new CategoriesSelector();
            this._openOptionsPanel = new Panel();
            this._chkOpenAfterSave = new CheckBox();

		    _boxAnnotation = new System.Windows.Forms.GroupBox();
		    _lblAnnotation = new System.Windows.Forms.Label();
		    _btnHideShowAnnotation = new ImageListButton();
		    _lblHideShowAnnotationText = new JetLinkLabel();

            this._panelTop.SuspendLayout();
            this._panelContent.SuspendLayout();
            this._panelAnnotation.SuspendLayout();
            this.SuspendLayout();
            //
            // _panelTop
            //
            this._panelTop.Controls.Add(this._lblName);
            this._panelTop.Controls.Add(this._edtName);
            this._panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this._panelTop.Location = new System.Drawing.Point(0, 0);
            this._panelTop.Name = "_panelTop";
            this._panelTop.Size = new System.Drawing.Size(700, 28);
            this._panelTop.TabIndex = 0;
            //
            // lblName
            //
            this._lblName.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblName.Location = new System.Drawing.Point(4, 8);
            this._lblName.Name = "_lblName";
            this._lblName.Size = new System.Drawing.Size(36, 16);
            this._lblName.TabIndex = 0;
            this._lblName.Text = "&Name:";
            //
            // _edtName
            //
            this._edtName.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            this._edtName.Location = new System.Drawing.Point(44, 4);
            this._edtName.Name = "_edtName";
            this._edtName.Size = new System.Drawing.Size(648, 21);
            this._edtName.TabIndex = 1;
            this._edtName.Text = "";

            //
            // _panelContent
            //
            this._panelContent.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this._panelContent.Controls.Add(this._richTextBox);
            this._panelContent.Dock = System.Windows.Forms.DockStyle.Fill;
            this._panelContent.Location = new System.Drawing.Point(0, 28);
            this._panelContent.Name = "_panelContent";
            this._panelContent.Size = new System.Drawing.Size(700, 70);
            this._panelContent.TabIndex = 1;
            //
            // _richTextBox
            //
            this._richTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._richTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._richTextBox.Location = new System.Drawing.Point(4, 0);
            this._richTextBox.Name = "_richTextBox";
            this._richTextBox.ReadOnly = true;
            this._richTextBox.Size = new System.Drawing.Size(692, 60);
            this._richTextBox.TabIndex = 0;
            this._richTextBox.Text = "";
            //
            // _panelAnnotation
            //
            this._panelAnnotation.Dock = System.Windows.Forms.DockStyle.Bottom;
            this._panelAnnotation.Size = new System.Drawing.Size(340, 176);
            this._panelAnnotation.Location = new System.Drawing.Point(0, 168);
            this._panelAnnotation.Name = "_panelAnnotation";
            this._panelAnnotation.TabIndex = 3;

            this._panelAnnotation.Controls.Add(_boxAnnotation);
            this._panelAnnotation.Controls.Add(_selector);
            //
            // _boxAnnotation
            //
		    _boxAnnotation.Location = new System.Drawing.Point(8, 4);
		    _boxAnnotation.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
		    _boxAnnotation.Name = "_boxAnnotation";
		    _boxAnnotation.Size = new System.Drawing.Size(324, 132);
		    _boxAnnotation.FlatStyle = FlatStyle.System;
		    _boxAnnotation.TabStop = false;

		    _boxAnnotation.Controls.Add( _lblAnnotation );
		    _boxAnnotation.Controls.Add( _btnHideShowAnnotation );
		    _boxAnnotation.Controls.Add( _lblHideShowAnnotationText );
		    _boxAnnotation.Controls.Add( _edtAnnotation );
            //
            // _edtAnnotation
            //
            this._edtAnnotation.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this._edtAnnotation.Location = new System.Drawing.Point(8, 28);
            this._edtAnnotation.Multiline = true;
            this._edtAnnotation.Name = "_edtAnnotation";
            this._edtAnnotation.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._edtAnnotation.Size = new System.Drawing.Size(308, 95);
            this._edtAnnotation.TabIndex = 1;
            this._edtAnnotation.Text = "";
            //
            // labelAnnotation
            //
		    _lblAnnotation.FlatStyle = System.Windows.Forms.FlatStyle.System;
		    _lblAnnotation.Location = new System.Drawing.Point(10, 10);
		    _lblAnnotation.Name = "labelExceptions";
		    _lblAnnotation.Size = new System.Drawing.Size(64, 16);
		    _lblAnnotation.TabStop = false;
		    _lblAnnotation.TextAlign = ContentAlignment.MiddleLeft;
		    _lblAnnotation.Text = "Annotation";
            //
            // labelHideShowAnnotationText
            //
		    _lblHideShowAnnotationText.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
		    _lblHideShowAnnotationText.Location = new System.Drawing.Point(270, 10);
		    _lblHideShowAnnotationText.Name = "labelHideShowExceptionsText";
		    _lblHideShowAnnotationText.Size = new System.Drawing.Size(28, 16);
		    _lblHideShowAnnotationText.TabStop = false;
		    _lblHideShowAnnotationText.TextAlign = ContentAlignment.MiddleLeft;
		    _lblHideShowAnnotationText.Text = "Hide";
		    _lblHideShowAnnotationText.Tag = _btnHideShowAnnotation;
		    _lblHideShowAnnotationText.Click += new EventHandler(labelHideShowPanel_Click);
            //
            // buttonHideShowAnnotation
            //
		    _btnHideShowAnnotation.Location = new System.Drawing.Point(300, 9);
		    _btnHideShowAnnotation.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
		    _btnHideShowAnnotation.Name = "_btnHideShowAnnotation";
		    _btnHideShowAnnotation.Size = new System.Drawing.Size(16, 16);
		    _btnHideShowAnnotation.TabIndex = 9;
		    _btnHideShowAnnotation.Click += new EventHandler(HideShowPanel_Click);
		    _btnHideShowAnnotation.Cursor = System.Windows.Forms.Cursors.Hand;

		    _btnHideShowAnnotation.AddIcon( Utils.TryGetEmbeddedResourceIconFromAssembly( "GUIControls", "GUIControls.Icons.CollapsePanel.ico" ), ImageListButton.ButtonState.Normal );
		    _btnHideShowAnnotation.AddIcon( Utils.TryGetEmbeddedResourceIconFromAssembly( "GUIControls", "GUIControls.Icons.ExpandPanel.ico" ), ImageListButton.ButtonState.Normal );
		    _btnHideShowAnnotation.AddIcon( Utils.TryGetEmbeddedResourceIconFromAssembly( "GUIControls", "GUIControls.Icons.CollapsePanelHover.ico" ), ImageListButton.ButtonState.Hot );
		    _btnHideShowAnnotation.AddIcon( Utils.TryGetEmbeddedResourceIconFromAssembly( "GUIControls", "GUIControls.Icons.ExpandPanelHover.ico" ), ImageListButton.ButtonState.Hot );
            //
            // _selector
            //
            this._selector.Anchor = (AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
            this._selector.Location = new System.Drawing.Point(8, 140);
            this._selector.Name = "_selector";
            this._selector.Size = new System.Drawing.Size(324, 40);
            this._selector.TabIndex = 3;
            //
            // _openOptionsPanel
            //
            this._openOptionsPanel.Dock = DockStyle.Bottom;
            this._openOptionsPanel.Name = "_openOptionsPanel";
            this._openOptionsPanel.Size = new Size( 200, 24 );
            this._openOptionsPanel.Controls.Add( _chkOpenAfterSave );
            //
            // _chkOpenAfterSave
            //
            this._chkOpenAfterSave.FlatStyle = FlatStyle.System;
            this._chkOpenAfterSave.Location = new Point( 10, 4 ) ;
            this._chkOpenAfterSave.Size = new Size( 150, 20 );
            this._chkOpenAfterSave.Text = "Open clipping after save";
            //
            // FragmentEditPane
            //
            this.Controls.Add(this._panelContent);
            this.Controls.Add(this._panelAnnotation);
            this.Controls.Add(this._panelTop);
            this.Controls.Add( this._openOptionsPanel );
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "FragmentEditPane";
            this.Size = new System.Drawing.Size(700, 428);
            this.Load += new System.EventHandler(this.OnLoad);
            this.VisibleChanged += new EventHandler( OnFormVisibleChanged );
            this._panelContent.ResumeLayout(false);
            this._panelAnnotation.ResumeLayout(false);
            this._panelTop.ResumeLayout(false);
            this.ResumeLayout(false);
        }

		private void InitializeBrowserComponent()
		{
			this._browser = Core.WebBrowser.NewInstance();
			this.SuspendLayout();
			_panelContent.SuspendLayout();
			//
			// _browser
			//
			this._browser.Name = "_browser";
			this._browser.Dock = DockStyle.Fill;
			this._browser.TabIndex = 12;

			_panelContent.Controls.Add( this._browser );
			_panelContent.ResumeLayout( false );
			this.ResumeLayout( false );

			_browser.ContextProvider = this;
        }

        private void OnFormVisibleChanged( object sender, EventArgs e )
        {
            if( Visible )
            {
                string section = GetType().FullName;
                if( !Core.SettingStore.ReadBool( section, "FragmentDialogOpen", true ) )
                    HideShowPanel_Click( _btnHideShowAnnotation, null );
            }
        }
		#endregion

        public override void EditResource( IResource fragment )
        {
			_fragment = fragment;
            _linkedFlag = _fragment.GetLinkProp( "Flag" );

			if( !_isLoaded )  // The Web browser has not been created yet, wait for the OnLoad event
				return;

            _edtName.Text = fragment.GetStringProp( Core.Props.Subject );
            _edtAnnotation.Text = fragment.GetPropText( Core.Props.Annotation );
            if ( fragment.HasProp( Core.Props.LongBodyIsRTF ) )
            {
                _richTextBox.Rtf = fragment.GetPropText( Core.Props.LongBody );
            	_browser.Visible = false;
            }
            else
            {
            	_browser.ShowHtml( fragment.GetPropText( Core.Props.LongBody ), _ctxDisplayClipping, null );
                _richTextBox.Visible = false;
            }

            _chkOpenAfterSave.Checked = Core.SettingStore.ReadBool( "Clippings", "OpenAfterSave", true );
            if ( !_fragment.IsTransient )
            {
                _openOptionsPanel.Visible = false;
            }

            _selector.Resource = fragment;
        }

        public override void Save()
        {
            ResourceProxy proxy = new ResourceProxy( _fragment );
            proxy.BeginUpdate();
            proxy.SetProp( Core.Props.Subject, _edtName.Text );
            if ( _edtAnnotation.Text.Trim().Length > 0 )
            {
                proxy.SetProp( Core.Props.Annotation, _edtAnnotation.Text );
            }
            else
            {
                proxy.DeleteProp( Core.Props.Annotation );
            }
            proxy.EndUpdateAsync();

			// Save size
			Core.SettingStore.WriteInt( "Clippings", "AnnotationEditWindowHeight", _panelAnnotation.Height );
            Core.SettingStore.WriteBool( "Clippings", "OpenAfterSave", _chkOpenAfterSave.Checked );
        }

        public override void Cancel()
        {
            if( _linkedFlag == null )
                new ResourceProxy( _fragment ).DeleteLinks( "Flag" );
            else
                new ResourceProxy( _fragment ).SetProp( "Flag", _linkedFlag );
        }

        private void OnLoad(object sender, System.EventArgs e)
		{
			_isLoaded = true;
			if( _fragment != null ) // Resource was delayed
				EditResource( _fragment );	// Do it now
		}

		/// <summary>
		/// Returns the action context for the current state of the control.
		/// </summary>
		/// <param name="kind">The kind of action which is invoked (keyboard, menu and so on).</param>
		/// <returns>The action context for the specified kind and the current state.</returns>
		public IActionContext GetContext(ActionContextKind kind)
		{
			ActionContext context = new ActionContext(kind, this, _fragment.ToResourceList());
			context.SetCommandProcessor(this);
			context.SetOwnerForm(this.ParentForm);
			return context;
		}

		/// <summary>
		/// Checks if the command with the specified ID can be executed in the current state
		/// of the control.
		/// </summary>
		/// <param name="command">The ID of the command.</param>
		/// <returns>true if the ID of the command is known to the control and can be
		/// executed; false otherwise.</returns>
		public bool CanExecuteCommand(string command)
		{
			// Delegate to the proper control
			if(_edtName.Focused)
				return _edtName.CanExecuteCommand(command);
			else if(_edtAnnotation.Focused)
				return _edtAnnotation.CanExecuteCommand(command);
			else if(_richTextBox.Focused)
				return _richTextBox.CanExecuteCommand(command);
			else
				return _browser.CanExecuteCommand(command);
		}

		/// <summary>
		/// Executes the command with the specified ID.
		/// </summary>
		/// <param name="command">ID of the command to execute.</param>
		public void ExecuteCommand(string command)
		{
			// Delegate to the proper control
			if(_edtName.Focused)
				_edtName.ExecuteCommand(command);
			else if(_edtAnnotation.Focused)
				_edtAnnotation.ExecuteCommand(command);
			else if(_richTextBox.Focused)
				_richTextBox.ExecuteCommand(command);
			else
				_browser.ExecuteCommand(command);
		}

		public bool OpenAfterSave
        {
            get { return _chkOpenAfterSave.Checked; }
        }

        #region Hide/Show
        private void  labelHideShowPanel_Click(object sender, EventArgs e)
        {
            HideShowPanel_Click( _btnHideShowAnnotation, e );
        }

        private void  HideShowPanel_Click(object sender, EventArgs e)
        {
            ImageListButton button = (ImageListButton) sender;

            if( _edtAnnotation.Visible )
            {
                _edtAnnotation.Visible = false;

                _boxAnnotation.Tag = _boxAnnotation.Height;
                _boxAnnotation.Height = CollapsedPanelHeight;
                _lblHideShowAnnotationText.Text = "Show";

                //  temporarily let us shrink however we want, then update this info.
//                this.MinimumSize = new Size( this.MinimumSize.Width, 100 );

                int delta = (int)_boxAnnotation.Tag - CollapsedPanelHeight;
                _panelAnnotation.Height = _panelAnnotation.Height - delta;
//                Form parent = FindForm();
//                parent.Size = new Size( Width, Height - delta );

                _boxAnnotation.Top += delta;
            }
            else
            {
                //  If at least one panel is expanded, no reason to control
                //  the maximal size.
//                MaximumSize = new Size( 1000, 1000 );

//                Form parent = FindForm();
                int delta = (int)_boxAnnotation.Tag - CollapsedPanelHeight;
                _boxAnnotation.Height = (int)_boxAnnotation.Tag;
//                parent.Size = new Size( Width, Height + delta );

                _lblHideShowAnnotationText.Text = "Hide";

                _edtAnnotation.Visible = true;

                _panelAnnotation.Height = _panelAnnotation.Height + delta;
                _boxAnnotation.Top -= delta;
            }

            //  Update the state in our INI file.
            string section = GetType().FullName;
            Core.SettingStore.WriteBool( section, "FragmentDialogOpen", _edtAnnotation.Visible );

            //  Change the state of our graphical buttons
            button.NormalImageIndex = 1 - button.NormalImageIndex;
            button.PressedImageIndex = 1 - button.NormalImageIndex;
            button.HotImageIndex = button.NormalImageIndex + 2;

//            AdjustMinimalSize();
        }

        protected virtual void AdjustMinimalSize()
        {
            int formMinimalHeight = this.Height;
            if( _boxAnnotation.Height == CollapsedPanelHeight )
            {
                //  Forbid also to maximize the dialog.
//                MaximumSize = new Size( 1000, this.Height );
            }
            else
            {
                formMinimalHeight -= ( _boxAnnotation.Height - MinimalPanelHeight );
            }
//            this.MinimumSize = new Size( 315, formMinimalHeight );
        }
        #endregion Hide/Show
	}
}
