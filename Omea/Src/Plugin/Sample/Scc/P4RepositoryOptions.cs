/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.SccPlugin
{
	/// <summary>
	/// Form for editing the configuration of a Perforce repository watched by the plugin.
	/// </summary>
	public class P4RepositoryOptions : System.Windows.Forms.Form
	{
        private System.Windows.Forms.Button _btnOK;
        private System.Windows.Forms.Button _btnCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox _edtRepositoryName;
        private System.Windows.Forms.TextBox _edtIgnoreClients;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox _edtPathsToWatch;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox _edtP4WebPath;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label lblServerPort;
        private System.Windows.Forms.Label edtClient;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox _edtServerPort;
        private System.Windows.Forms.TextBox _edtClient;
        private System.Windows.Forms.TextBox _edtUserName;
        private System.Windows.Forms.TextBox _edtPassword;
	    private IResource _repository;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public P4RepositoryOptions()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this._btnOK = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this._edtRepositoryName = new System.Windows.Forms.TextBox();
            this._edtIgnoreClients = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this._edtPathsToWatch = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this._edtP4WebPath = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this._edtServerPort = new System.Windows.Forms.TextBox();
            this.lblServerPort = new System.Windows.Forms.Label();
            this._edtClient = new System.Windows.Forms.TextBox();
            this.edtClient = new System.Windows.Forms.Label();
            this._edtUserName = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this._edtPassword = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // _btnOK
            // 
            this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(236, 336);
            this._btnOK.Name = "_btnOK";
            this._btnOK.TabIndex = 8;
            this._btnOK.Text = "OK";
            this._btnOK.Click += new System.EventHandler(this._btnOK_Click);
            // 
            // _btnCancel
            // 
            this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(320, 336);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.TabIndex = 9;
            this._btnCancel.Text = "Cancel";
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 16);
            this.label1.TabIndex = 2;
            this.label1.Text = "Name:";
            // 
            // _edtRepositoryName
            // 
            this._edtRepositoryName.Location = new System.Drawing.Point(116, 4);
            this._edtRepositoryName.Name = "_edtRepositoryName";
            this._edtRepositoryName.Size = new System.Drawing.Size(168, 21);
            this._edtRepositoryName.TabIndex = 0;
            this._edtRepositoryName.Text = "";
            // 
            // _edtIgnoreClients
            // 
            this._edtIgnoreClients.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtIgnoreClients.Location = new System.Drawing.Point(20, 168);
            this._edtIgnoreClients.Name = "_edtIgnoreClients";
            this._edtIgnoreClients.Size = new System.Drawing.Size(376, 21);
            this._edtIgnoreClients.TabIndex = 5;
            this._edtIgnoreClients.Text = "";
            // 
            // label2
            // 
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(8, 148);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(280, 16);
            this.label2.TabIndex = 4;
            this.label2.Text = "Ignore changes from clients (separate with semicolons):";
            // 
            // _edtPathsToWatch
            // 
            this._edtPathsToWatch.AcceptsReturn = true;
            this._edtPathsToWatch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtPathsToWatch.Location = new System.Drawing.Point(20, 220);
            this._edtPathsToWatch.Multiline = true;
            this._edtPathsToWatch.Name = "_edtPathsToWatch";
            this._edtPathsToWatch.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._edtPathsToWatch.Size = new System.Drawing.Size(376, 60);
            this._edtPathsToWatch.TabIndex = 6;
            this._edtPathsToWatch.Text = "";
            // 
            // label5
            // 
            this.label5.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label5.Location = new System.Drawing.Point(8, 200);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(408, 16);
            this.label5.TabIndex = 9;
            this.label5.Text = "Depot paths to watch (one per line; if empty, entire depot is watched):";
            // 
            // _edtP4WebPath
            // 
            this._edtP4WebPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtP4WebPath.Location = new System.Drawing.Point(20, 304);
            this._edtP4WebPath.Name = "_edtP4WebPath";
            this._edtP4WebPath.Size = new System.Drawing.Size(376, 21);
            this._edtP4WebPath.TabIndex = 7;
            this._edtP4WebPath.Text = "";
            // 
            // label3
            // 
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label3.Location = new System.Drawing.Point(8, 284);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(280, 16);
            this.label3.TabIndex = 12;
            this.label3.Text = "P4Web URL:";
            // 
            // _edtServerPort
            // 
            this._edtServerPort.Location = new System.Drawing.Point(116, 32);
            this._edtServerPort.Name = "_edtServerPort";
            this._edtServerPort.Size = new System.Drawing.Size(168, 21);
            this._edtServerPort.TabIndex = 1;
            this._edtServerPort.Text = "";
            // 
            // lblServerPort
            // 
            this.lblServerPort.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.lblServerPort.Location = new System.Drawing.Point(8, 36);
            this.lblServerPort.Name = "lblServerPort";
            this.lblServerPort.Size = new System.Drawing.Size(100, 16);
            this.lblServerPort.TabIndex = 13;
            this.lblServerPort.Text = "Server/port:";
            // 
            // _edtClient
            // 
            this._edtClient.Location = new System.Drawing.Point(116, 60);
            this._edtClient.Name = "_edtClient";
            this._edtClient.Size = new System.Drawing.Size(168, 21);
            this._edtClient.TabIndex = 2;
            this._edtClient.Text = "";
            // 
            // edtClient
            // 
            this.edtClient.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.edtClient.Location = new System.Drawing.Point(8, 64);
            this.edtClient.Name = "edtClient";
            this.edtClient.Size = new System.Drawing.Size(100, 16);
            this.edtClient.TabIndex = 15;
            this.edtClient.Text = "Client:";
            // 
            // _edtUserName
            // 
            this._edtUserName.Location = new System.Drawing.Point(116, 88);
            this._edtUserName.Name = "_edtUserName";
            this._edtUserName.Size = new System.Drawing.Size(168, 21);
            this._edtUserName.TabIndex = 3;
            this._edtUserName.Text = "";
            // 
            // label6
            // 
            this.label6.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label6.Location = new System.Drawing.Point(8, 92);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(100, 16);
            this.label6.TabIndex = 17;
            this.label6.Text = "User name:";
            // 
            // _edtPassword
            // 
            this._edtPassword.Location = new System.Drawing.Point(116, 116);
            this._edtPassword.Name = "_edtPassword";
            this._edtPassword.PasswordChar = '*';
            this._edtPassword.Size = new System.Drawing.Size(168, 21);
            this._edtPassword.TabIndex = 4;
            this._edtPassword.Text = "";
            // 
            // label7
            // 
            this.label7.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label7.Location = new System.Drawing.Point(8, 120);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(100, 16);
            this.label7.TabIndex = 19;
            this.label7.Text = "Password:";
            // 
            // P4RepositoryOptions
            // 
            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(404, 367);
            this.Controls.Add(this._edtPassword);
            this.Controls.Add(this.label7);
            this.Controls.Add(this._edtUserName);
            this.Controls.Add(this._edtClient);
            this.Controls.Add(this._edtServerPort);
            this.Controls.Add(this._edtP4WebPath);
            this.Controls.Add(this._edtPathsToWatch);
            this.Controls.Add(this._edtIgnoreClients);
            this.Controls.Add(this._edtRepositoryName);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.edtClient);
            this.Controls.Add(this.lblServerPort);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "P4RepositoryOptions";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Perforce Repository Options";
            this.ResumeLayout(false);

        }
		#endregion

	    public DialogResult EditRepository( IWin32Window ownerWindow, IResource repository )
	    {
	        _repository = repository;
	        _edtRepositoryName.Text = repository.GetStringProp( Core.Props.Name );
	        _edtServerPort.Text = repository.GetStringProp( Props.P4ServerPort );
	        _edtClient.Text = repository.GetStringProp( Props.P4Client );
	        _edtUserName.Text = repository.GetStringProp( Props.UserName );
	        _edtPassword.Text = repository.GetStringProp( Props.Password );
	        _edtIgnoreClients.Text = repository.GetStringProp( Props.P4IgnoreChanges );
	        _edtPathsToWatch.Lines = repository.GetPropText( Props.PathsToWatch ).Split( ';' );
	        _edtP4WebPath.Text = repository.GetPropText( Props.P4WebUrl );
	        DialogResult dr = ShowDialog( ownerWindow );
	        if ( dr == DialogResult.OK )
	        {
	            ResourceProxy proxy = new ResourceProxy( repository );
	            proxy.BeginUpdate();
	            proxy.SetProp( Core.Props.Name, _edtRepositoryName.Text );
                proxy.SetProp( Props.P4ServerPort, _edtServerPort.Text );
                proxy.SetProp( Props.P4Client, _edtClient.Text );
                proxy.SetProp( Props.UserName, _edtUserName.Text );
                proxy.SetProp( Props.Password, _edtPassword.Text );
	            proxy.SetProp( Props.P4IgnoreChanges, _edtIgnoreClients.Text );
	            proxy.SetProp( Props.PathsToWatch, String.Join( ";", _edtPathsToWatch.Lines ) );
	            proxy.SetProp( Props.P4WebUrl, _edtP4WebPath.Text );
	            proxy.EndUpdate();
	        }
	        return dr;
	    }

        private void _btnOK_Click( object sender, System.EventArgs e )
        {
            if ( !CheckDeleteIgnoredClients() )
            {
                DialogResult = DialogResult.None;
                return;
            }
        }

	    private bool CheckDeleteIgnoredClients()
	    {
	        string oldClientsText = _repository.GetStringProp( Props.P4IgnoreChanges );
	        if ( oldClientsText != null && _edtIgnoreClients.Text != oldClientsText && 
	             _edtIgnoreClients.Text.Trim().Length > 0 )
	        {
	            ArrayList oldClients = new ArrayList( oldClientsText.Split( ';' ) );
	            string[] newClients = _edtIgnoreClients.Text.Split( ';' );
	            bool addedClients = false;
	            foreach( string newClient in newClients )
	            {
	                if ( oldClients.IndexOf( newClient ) < 0 )
	                {
	                    addedClients = true;
	                    break;
	                }
	            }
                
	            if ( addedClients )
	            {
	                DialogResult dr = MessageBox.Show( this,
	                                                   "Would you like to delete the changesets from clients which are now ignored?",
	                                                   "Repository Properties", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question );
	                if ( dr == DialogResult.Cancel )
	                {
	                    return false;
	                }
	                if ( dr == DialogResult.Yes )
	                {
	                    Core.ResourceAP.RunJob( new ResourceStringsDelegate( DeleteIgnoredClientChangesets ),
	                                            _repository, newClients );
	                }
	            }
	        }
	        return true;
	    }

	    private void DeleteIgnoredClientChangesets( IResource repository, string[] clients )
	    {
	        foreach( string client in clients )
	        {
	            IResourceList changeSets = Core.ResourceStore.FindResources( Props.ChangeSetResource,
	                Props.P4Client, client );
	            changeSets = changeSets.Intersect( repository.GetLinksOfType( Props.ChangeSetResource, 
	                Props.ChangeSetRepository ) );
	            foreach( IResource changeSet in changeSets )
	            {
	                DeleteRepositoryAction.DeleteChangeSet( changeSet );
	            }
	        }
	    }
	    
	    private delegate void ResourceStringsDelegate( IResource res, string[] strings );
	}
}
