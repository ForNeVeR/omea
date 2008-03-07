/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Favorites
{
    internal class RemoteAnnotateForm : DialogBase
    {
        private System.ComponentModel.Container components = null;

        private Label               lblName;
        private JetTextBox          _nameBox;
        private Label               lblURL;
        private JetLinkLabel        _urlLink;
        private GroupBox            _boxAnnotation;
        private TextBox             _edtAnnotation;
        private CategoriesSelector  _panelCategories;
        private Button              _cancelButton;
        private Button              _okButton;

        private IResource           _weblink;
        private static IntHashTable _forms = new IntHashTable();

        private RemoteAnnotateForm( IResource weblink )
        {
            InitializeComponent();
            RestoreSettings();
            Icon = FavoritesPlugin.LoadIconFromAssembly( "categorize_annotate.ico" );
            _weblink = weblink;
            InitializeContent();
            IBookmarkService service =
                (IBookmarkService) Core.PluginLoader.GetPluginService( typeof( IBookmarkService ) );
            IBookmarkProfile profile = service.GetOwnerProfile( _weblink );
            string error;
            if( profile == null || profile.CanCreate( _weblink, out error ) )
            {
                _nameBox.ReadOnly = false;
                _nameBox.BorderStyle = BorderStyle.Fixed3D;
                _nameBox.Font = new Font( _nameBox.Font, FontStyle.Regular );
            }
        }

        private void  InitializeContent()
        {
            _nameBox.Text = _weblink.GetPropText( Core.Props.Name );
            _urlLink.Text = _weblink.GetPropText( FavoritesPlugin._propURL );
            _edtAnnotation.Text = _weblink.GetPropText( "Annotation" );
            _panelCategories.Resource = _weblink;
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

        public static void EditAnnotation( IResource weblink )
        {
            int id = weblink.Id;
            Trace.WriteLine( "EditAnnotation( " + id + " )" );
            RemoteAnnotateForm form = (RemoteAnnotateForm) _forms[ id ];
            if( form == null )
            {
                form = new RemoteAnnotateForm( weblink );
                _forms[ id ] = form;
            }
            form.Show();
            form.Activate();
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(RemoteAnnotateForm));
            this.lblName = new System.Windows.Forms.Label();
            this.lblURL = new System.Windows.Forms.Label();
            this._nameBox = new JetTextBox();
            this._urlLink = new JetBrains.Omea.GUIControls.JetLinkLabel();
            _boxAnnotation = new GroupBox();
            _edtAnnotation = new System.Windows.Forms.TextBox();
            _panelCategories = new CategoriesSelector();
            this._cancelButton = new System.Windows.Forms.Button();
            this._okButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblName
            // 
            this.lblName.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.lblName.Location = new System.Drawing.Point(8, 10);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(48, 23);
            this.lblName.Text = "Name:";
            // 
            // _nameBox
            // 
            this._nameBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._nameBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._nameBox.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._nameBox.Location = new System.Drawing.Point(60, 8);
            this._nameBox.Name = "_nameBox";
            this._nameBox.ReadOnly = true;
            this._nameBox.Size = new System.Drawing.Size(428, 14);
            this._nameBox.TabIndex = 0;
            this._nameBox.Text = "Bookmark name";
            // 
            // lblURL
            // 
            this.lblURL.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.lblURL.Location = new System.Drawing.Point(8, 35);
            this.lblURL.Name = "lblURL";
            this.lblURL.Size = new System.Drawing.Size(48, 23);
            this.lblURL.Text = "URL:";
            // 
            // _urlLink
            // 
            this._urlLink.Cursor = System.Windows.Forms.Cursors.Hand;
            this._urlLink.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(70)), ((System.Byte)(70)), ((System.Byte)(211)));
            this._urlLink.Location = new System.Drawing.Point(60, 35);
            this._urlLink.Name = "_urlLink";
            this._urlLink.Size = new System.Drawing.Size(0, 0);
            this._urlLink.TabIndex = 1;
            this._urlLink.Click += new System.EventHandler(this._urlLink_Click);
            //
            // _boxAnnotation
            //
            this._boxAnnotation.Controls.Add(_edtAnnotation);
            this._boxAnnotation.Anchor = (AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            this._boxAnnotation.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._boxAnnotation.Location = new System.Drawing.Point(8, 60);
            this._boxAnnotation.Name = "boxAnnotation";
            this._boxAnnotation.Size = new System.Drawing.Size(480, 124);
            this._boxAnnotation.TabIndex = 2;
            this._boxAnnotation.Text = "&Annotation";
            // 
            // _edtAnnotation
            // 
            this._edtAnnotation.AcceptsReturn = true;
            this._edtAnnotation.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this._edtAnnotation.Location = new System.Drawing.Point(8, 16);
            this._edtAnnotation.Multiline = true;
            this._edtAnnotation.Name = "_edtAnnotation";
            this._edtAnnotation.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._edtAnnotation.Size = new System.Drawing.Size(464, 98 );
            this._edtAnnotation.TabIndex = 1;
            //
            // _panelCategories
            //
            this._panelCategories.Anchor = (AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom);
            this._panelCategories.Location = new System.Drawing.Point(8, 192);
            this._panelCategories.Name = "_subjectDescriptionPanel";
            this._panelCategories.Size = new System.Drawing.Size(480, 40);
            this._panelCategories.TabIndex = 3;
            // 
            // _okButton
            // 
            this._okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._okButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okButton.Location = new System.Drawing.Point(328, 236);
            this._okButton.Name = "_okButton";
            this._okButton.TabIndex = 5;
            this._okButton.Text = "OK";
            this._okButton.Click += new System.EventHandler(this._okButton_Click);
            // 
            // _cancelButton
            // 
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelButton.Location = new System.Drawing.Point(412, 236);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.TabIndex = 6;
            this._cancelButton.Text = "Cancel";
            this._cancelButton.Click += new System.EventHandler(this._cancelButton_Click);
            // 
            // RemoteAnnotateForm
            // 
            this.AcceptButton = this._okButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(496, 266);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this._nameBox);
            this.Controls.Add(this.lblURL);
            this.Controls.Add(this._urlLink);
            this.Controls.Add(_boxAnnotation);
            this.Controls.Add(_panelCategories);
            this.Controls.Add(this._okButton);
            this.Controls.Add(this._cancelButton);
            this.MinimumSize = new System.Drawing.Size(400, 200);
            this.Name = "RemoteAnnotateForm";
            this.ShowInTaskbar = true;
            this.Text = "Annotate and Categorize Bookmark";
            this.Closing += new System.ComponentModel.CancelEventHandler(this.RemoteAnnotateForm_Closing);
            this.ResumeLayout(false);
        }
        #endregion

        private void _urlLink_Click( object sender, System.EventArgs e )
        {
            OpenFavoriteAction.OpenUrl( _urlLink.Text );
        }

        private void _cancelButton_Click( object sender, System.EventArgs e )
        {
            Close();
        }

        private void _okButton_Click( object sender, System.EventArgs e )
        {
            Core.ResourceAP.RunUniqueJob( new MethodInvoker( SubmitChanges ) );
            Close();
        }

        private void RemoteAnnotateForm_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            _forms.Remove( _weblink.Id );
        }

        private void SubmitChanges()
        {
            if( !_weblink.IsTransient )
            {
                _weblink.BeginUpdate();
            }
            try
            {
                string annotation = _edtAnnotation.Text;
                if( annotation.Length == 0 )
                {
                    _weblink.DeleteProp( "Annotation" );
                }
                else
                {
                    _weblink.SetProp( "Annotation", annotation );
                }
                string name = _nameBox.Text;
                if( name != _weblink.GetPropText( Core.Props.Name ) )
                {
                    _weblink.SetProp( Core.Props.Name, name );
                    IBookmarkService service =
                        (IBookmarkService) Core.PluginLoader.GetPluginService( typeof( IBookmarkService ) );
                    IBookmarkProfile profile = service.GetOwnerProfile( _weblink );
                    string error;
                    if( profile != null && profile.CanCreate( _weblink, out error ) )
                    {
                        profile.Create( _weblink );
                    }
                }
            }
            finally
            {
                _weblink.EndUpdate();
            }
        }
    }
}