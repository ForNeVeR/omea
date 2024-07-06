// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
	/// Notification message shown by a rule action.
	/// </summary>
	public class NotificationMessageDlg : DialogBase
	{
        private JetBrains.Omea.GUIControls.JetLinkLabel _lblMessage;
        private System.Windows.Forms.Label label1;
        private ResourceLinkLabel _lblResource;
        private System.Windows.Forms.Button _btnOK;
        private System.Windows.Forms.Button _btnReadNext;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        private static NotificationMessageDlg _theMessage = null;

        private static ArrayList _nextMessages = new ArrayList();
        private static ArrayList _nextResources = new ArrayList();

		public NotificationMessageDlg()
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
            this._lblMessage = new JetBrains.Omea.GUIControls.JetLinkLabel();
            this.label1 = new System.Windows.Forms.Label();
            this._lblResource = new JetBrains.Omea.GUIControls.ResourceLinkLabel();
            this._btnOK = new System.Windows.Forms.Button();
            this._btnReadNext = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // _lblMessage
            //
            this._lblMessage.ClickableLink = false;
            this._lblMessage.Cursor = System.Windows.Forms.Cursors.Default;
            this._lblMessage.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._lblMessage.ForeColor = System.Drawing.SystemColors.ControlText;
            this._lblMessage.Location = new System.Drawing.Point(8, 8);
            this._lblMessage.Name = "_lblMessage";
            this._lblMessage.Size = new System.Drawing.Size(0, 0);
            this._lblMessage.TabIndex = 0;
            //
            // label1
            //
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 32);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 16);
            this.label1.TabIndex = 1;
            this.label1.Text = "Received resource:";
            //
            // _lblResource
            //
            this._lblResource.Cursor = System.Windows.Forms.Cursors.Default;
            this._lblResource.ForeColor = System.Drawing.SystemColors.ControlText;
            this._lblResource.LinkOwnerResource = null;
            this._lblResource.LinkType = 0;
            this._lblResource.Location = new System.Drawing.Point(8, 52);
            this._lblResource.Name = "_lblResource";
            this._lblResource.PostfixText = "";
            this._lblResource.ResourceLinkClicked += new CancelEventHandler(_lblResource_OnResourceLinkClicked);
            this._lblResource.Size = new System.Drawing.Size(23, 20);
            this._lblResource.TabIndex = 2;
            //
            // _btnOK
            //
            this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(192, 76);
            this._btnOK.Name = "_btnOK";
            this._btnOK.Size = new System.Drawing.Size(92, 23);
            this._btnOK.TabIndex = 3;
            this._btnOK.Text = "Close";
            //
            // _btnReadNext
            //
            this._btnReadNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._btnReadNext.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnReadNext.Location = new System.Drawing.Point(80, 76);
            this._btnReadNext.Name = "_btnReadNext";
            this._btnReadNext.Size = new System.Drawing.Size(92, 23);
            this._btnReadNext.TabIndex = 4;
            this._btnReadNext.Text = "Read Next";
            this._btnReadNext.Visible = false;
            this._btnReadNext.Click += new System.EventHandler(this._btnReadNext_Click);
            //
            // NotificationMessageDlg
            //
            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnOK;
            this.ClientSize = new System.Drawing.Size(292, 106);
            this.Controls.Add(this._btnReadNext);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this._lblResource);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._lblMessage);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "NotificationMessageDlg";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Notification Message";
            this.Closed += new System.EventHandler(this.NotificationMessageDlg_Closed);
            this.ResumeLayout(false);

        }

	    #endregion

        public static void QueueNotificationMessage( IResource res, string message )
        {
            lock( _nextMessages )
            {
                _nextMessages.Add( message );
                _nextResources.Add( res );
            }
            Core.UIManager.QueueUIJob( new MethodInvoker( UpdateNotificationMessage ) );
        }

	    private static void UpdateNotificationMessage()
	    {
            if ( Core.State == CoreState.ShuttingDown )
            {
                return;
            }
            lock( _nextMessages )
            {
                SkipDeletedResources();
                if ( _nextMessages.Count == 0 )
                {
                    return;
                }
            }
            if ( _theMessage == null )
            {
                _theMessage = new NotificationMessageDlg();
                _theMessage.ShowCurrentNotificationMessage();
                _theMessage.ShowDialog( Core.MainWindow );
            }
            else
            {
                _theMessage.UpdateReadNextButton();
            }
        }

	    private static void SkipDeletedResources()
	    {
	        while( _nextMessages.Count > 0 &&
                (_nextResources [0] == null || ((IResource) _nextResources [0]).IsDeleted ) )
	        {
	            _nextMessages.RemoveAt( 0 );
	            _nextResources.RemoveAt( 0 );
	        }
	    }

	    public void ShowCurrentNotificationMessage()
        {
            IResource res;
            string text;
            lock( _nextMessages )
            {
                SkipDeletedResources();
                if ( _nextMessages.Count == 0 )
                {
                    UpdateReadNextButton();
                    return;
                }
                res = (IResource) _nextResources [0];
                text = (string) _nextMessages [0];
            }

            _lblMessage.Text = text;
            _lblResource.Resource = res;
            _lblResource.Width = _lblResource.PreferredWidth;

            int maxWidth = (int) (Screen.PrimaryScreen.Bounds.Width * 0.75);
            int msgWidth = Math.Max( _lblMessage.PreferredWidth, _lblResource.Width );
            msgWidth = Math.Max( msgWidth, _btnOK.Width + _btnReadNext.Width + 32 );   // make sure the buttons fit
            int newWidth = Math.Min( msgWidth + 20, maxWidth );
            if ( Width < newWidth )
            {
                Width = newWidth;
                CenterToScreen();
            }

            UpdateReadNextButton();
        }

	    private void UpdateReadNextButton()
	    {
	        lock( _nextMessages )
	        {
	            if ( _nextMessages.Count <= 1 )
	            {
	                _btnReadNext.Visible = false;
                    _btnOK.Left = (Width - _btnOK.Width) / 2;
	            }
	            else
	            {
	                _btnReadNext.Visible = true;
	                _btnReadNext.Text = "Read Next (" + (_nextMessages.Count-1) + ")";
                    _btnReadNext.Left = Width / 2 - 8 - _btnReadNext.Width;
                    _btnOK.Left = Width / 2 + 8;
                }
	        }
	    }

        private void _btnReadNext_Click( object sender, System.EventArgs e )
        {
            lock( _nextMessages )
            {
                if ( _nextMessages.Count > 0 )
                {
                    _nextMessages.RemoveAt( 0 );
                    _nextResources.RemoveAt( 0 );
                }
            }
            ShowCurrentNotificationMessage();
        }

	    private void NotificationMessageDlg_Closed( object sender, System.EventArgs e )
        {
            lock( _nextMessages )
            {
                _nextMessages.Clear();
                _nextResources.Clear();
            }
            _theMessage = null;
            Dispose();
        }

        private void _lblResource_OnResourceLinkClicked( object sender, CancelEventArgs e )
        {
            (Core.MainWindow as Form).Activate();
        }
    }

    public class MessageBoxNotificationAction : IRuleAction
    {
        private delegate void ShowMessageDelegate( IResource res, string message );
        public void   Exec( IResource res, IActionParameterStore actionStore )
        {
            string  message = actionStore.ParameterAsString();
            DelayedExec( res, message );
        }

        private static void DelayedExec( IResource res, string message )
        {
            if( Core.State == CoreState.Running )
            {
                NotificationMessageDlg.QueueNotificationMessage( res, message );
            }
            else if( Core.State != CoreState.ShuttingDown )
            {
                Core.ResourceAP.QueueJobAt( DateTime.Now.AddSeconds( 10 ),
                    new ShowMessageDelegate( DelayedExec ), res, message );
            }
        }
    }
}
