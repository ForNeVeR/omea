/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
	/// A universal dialog for entering a string.
	/// </summary>
	public class InputStringDlg : DialogBase
	{
        private System.Windows.Forms.Label _lblPrompt;
        private System.Windows.Forms.TextBox _edtString;
        private System.Windows.Forms.Button _btnOK;
        private System.Windows.Forms.Button _btnCancel;
        private System.Windows.Forms.Label _lblValidateError;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
	    private ValidateStringDelegate _validateStringDelegate;
        private bool _allowEmpty = false;
        private System.Windows.Forms.Button _btnHelp;
        private string _helpTopic;

	    public InputStringDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
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
            this._lblPrompt = new System.Windows.Forms.Label();
            this._edtString = new System.Windows.Forms.TextBox();
            this._btnOK = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this._lblValidateError = new System.Windows.Forms.Label();
            this._btnHelp = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _lblPrompt
            // 
            this._lblPrompt.AutoSize = true;
            this._lblPrompt.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblPrompt.Location = new System.Drawing.Point(4, 4);
            this._lblPrompt.Name = "_lblPrompt";
            this._lblPrompt.Size = new System.Drawing.Size(111, 17);
            this._lblPrompt.TabIndex = 0;
            this._lblPrompt.Text = "Please enter a string:";
            // 
            // _edtString
            // 
            this._edtString.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtString.Location = new System.Drawing.Point(4, 24);
            this._edtString.Name = "_edtString";
            this._edtString.Size = new System.Drawing.Size(376, 21);
            this._edtString.TabIndex = 1;
            this._edtString.Text = "";
            this._edtString.TextChanged += new System.EventHandler(this._edtString_TextChanged);
            // 
            // _btnOK
            // 
            this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(224, 75);
            this._btnOK.Name = "_btnOK";
            this._btnOK.TabIndex = 2;
            this._btnOK.Text = "OK";
            // 
            // _btnCancel
            // 
            this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnCancel.Location = new System.Drawing.Point(304, 75);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.TabIndex = 3;
            this._btnCancel.Text = "Cancel";
            // 
            // _lblValidateError
            // 
            this._lblValidateError.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblValidateError.ForeColor = System.Drawing.Color.Red;
            this._lblValidateError.Location = new System.Drawing.Point(4, 52);
            this._lblValidateError.Name = "_lblValidateError";
            this._lblValidateError.Size = new System.Drawing.Size(372, 16);
            this._lblValidateError.TabIndex = 4;
            this._lblValidateError.Text = "The string is not valid";
            this._lblValidateError.Visible = false;
            // 
            // _btnHelp
            // 
            this._btnHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnHelp.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnHelp.Location = new System.Drawing.Point(304, 75);
            this._btnHelp.Name = "_btnHelp";
            this._btnHelp.TabIndex = 5;
            this._btnHelp.Text = "Help";
            this._btnHelp.Visible = false;
            this._btnHelp.Click += new System.EventHandler(this._btnHelp_Click);
            // 
            // InputStringDlg
            // 
            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnCancel;
            this.ClientSize = new System.Drawing.Size(384, 106);
            this.Controls.Add(this._btnHelp);
            this.Controls.Add(this._lblValidateError);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnOK);
            this.Controls.Add(this._edtString);
            this.Controls.Add(this._lblPrompt);
            this.Name = "InputStringDlg";
            this.Text = "Input String";
            this.VisibleChanged += new System.EventHandler(this.InputStringDlg_VisibleChanged);
            this.ResumeLayout(false);

        }

	    #endregion

        private void _edtString_TextChanged( object sender, System.EventArgs e )
        {
            DoValidate();
        }

	    private void DoValidate()
	    {
	        if ( _edtString.Text.Trim().Length == 0 && !_allowEmpty )
	        {
                _lblValidateError.Visible = false;
                _btnOK.Enabled = false;
	        }
            else 
            {
                if ( _validateStringDelegate != null )
	            {
	                string validateError = null;
	                _validateStringDelegate( _edtString.Text, ref validateError );
	                if ( validateError == null )
	                {
	                    _lblValidateError.Visible = false;
	                    _btnOK.Enabled = true;
	                }
	                else
	                {
	                    _lblValidateError.Text = validateError;
	                    _lblValidateError.Visible = true;
	                    _btnOK.Enabled = false;
	                }
	            }
                else
                {
                    _lblValidateError.Visible = false;
                    _btnOK.Enabled = true;
                }
            }
	    }

	    public string PromptText
        {
            get { return _lblPrompt.Text; }
            set
            {
                _lblPrompt.Text = value;
                if ( _lblPrompt.Width > ClientSize.Width - 8 )
                {
                    _lblPrompt.AutoSize = false;
                    _lblPrompt.Width = ClientSize.Width - 8;
                    _lblPrompt.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
                    Height += 16;
                    _edtString.Top += 16;
                    _lblPrompt.Height += 16;
                }
            }
        }

        public string StringText
        {
            get { return _edtString.Text; }
            set { _edtString.Text = value; }
        }
	    
        public ValidateStringDelegate ValidateStringDelegate
	    {
	        set
	        {
	            _validateStringDelegate = value;
                DoValidate();
	        }
        }

        private void InputStringDlg_VisibleChanged( object sender, EventArgs e )
        {
            MinimumSize = new Size( 0, Height );
            MaximumSize = new Size( Screen.GetWorkingArea( this ).Width, Height );
        }

	    public bool AllowEmptyString
	    {
            get { return _allowEmpty; }
            set { _allowEmpty = value; }
	    }

	    public string HelpTopic
	    {
	        get { return _helpTopic; }
	        set
	        {
                _helpTopic = value;
                if ( _helpTopic != null )
                {
                    if ( !_btnHelp.Visible )
                    {
                        _btnHelp.Visible = true;
                        _btnCancel.Left -= 80;
                        _btnOK.Left -= 80;
                    }
                }
	        }
	    }

        private void _btnHelp_Click( object sender, System.EventArgs e )
        {
            Help.ShowHelp( this, Core.UIManager.HelpFileName, _helpTopic );
        }
    }
}
