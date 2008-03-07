/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
    /**
     * Pane for configuring options for the mail formatter (which is
     * used both for mail and for news messages).
     */
    
    public class MailFormatPane: AbstractOptionsPane
    {
        private System.Windows.Forms.CheckBox _chkPrefixInitials;
        private System.Windows.Forms.TextBox _boxPrefix;
        private System.Windows.Forms.CheckBox _chkGreetingInReplies;
        private System.Windows.Forms.TextBox _signatureBox;
        private System.Windows.Forms.CheckBox _chkUseSignature;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox groupBox2;
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        private System.Windows.Forms.RadioButton _radReplySignatureNone;
        private System.Windows.Forms.RadioButton _radSignatureBeforeQuoting;
        private System.Windows.Forms.RadioButton _radSignatureAfterQuoting;
        private System.Windows.Forms.GroupBox _grpSignatureInReplies;

        private ISettingStore _ini;

        public MailFormatPane()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            // TODO: Add any initialization after the InitializeComponent call

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
            this._chkPrefixInitials = new System.Windows.Forms.CheckBox();
            this._boxPrefix = new System.Windows.Forms.TextBox();
            this._chkGreetingInReplies = new System.Windows.Forms.CheckBox();
            this._signatureBox = new System.Windows.Forms.TextBox();
            this._chkUseSignature = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this._grpSignatureInReplies = new System.Windows.Forms.GroupBox();
            this._radSignatureAfterQuoting = new System.Windows.Forms.RadioButton();
            this._radSignatureBeforeQuoting = new System.Windows.Forms.RadioButton();
            this._radReplySignatureNone = new System.Windows.Forms.RadioButton();
            this._grpSignatureInReplies.SuspendLayout();
            this.SuspendLayout();
            // 
            // _chkGreetingInReplies
            // 
            this._chkGreetingInReplies.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkGreetingInReplies.Location = new System.Drawing.Point(0, 20);
            this._chkGreetingInReplies.Name = "_chkGreetingInReplies";
            this._chkGreetingInReplies.Size = new System.Drawing.Size(155, 16);
            this._chkGreetingInReplies.TabIndex = 1;
            this._chkGreetingInReplies.Text = "Include greeting in replies";
            this._chkGreetingInReplies.CheckedChanged +=new System.EventHandler(_chkGreetingInReplies_CheckedChanged);
            // 
            // _boxPrefix
            // 
            this._boxPrefix.AcceptsReturn = false;
            this._boxPrefix.Location = new System.Drawing.Point(168, 18);
            this._boxPrefix.Multiline = false;
            this._boxPrefix.Name = "_boxPrefix";
            this._boxPrefix.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._boxPrefix.Size = new System.Drawing.Size(100, 19);
            this._boxPrefix.TabIndex = 2;
            this._boxPrefix.Text = "";
            this._boxPrefix.TextChanged += new System.EventHandler(_boxPrefix_TextChanged);
            // 
            // _chkPrefixInitials
            // 
            this._chkPrefixInitials.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkPrefixInitials.Location = new System.Drawing.Point(0, 44);
            this._chkPrefixInitials.Name = "_chkPrefixInitials";
            this._chkPrefixInitials.Size = new System.Drawing.Size(232, 16);
            this._chkPrefixInitials.TabIndex = 3;
            this._chkPrefixInitials.Text = "Prefix replies with sender\'s initials";
            // 
            // _signatureBox
            // 
            this._signatureBox.AcceptsReturn = true;
            this._signatureBox.Location = new System.Drawing.Point(12, 124);
            this._signatureBox.Multiline = true;
            this._signatureBox.Name = "_signatureBox";
            this._signatureBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._signatureBox.Size = new System.Drawing.Size(240, 80);
            this._signatureBox.TabIndex = 14;
            this._signatureBox.Text = "";
            // 
            // _useSignature
            // 
            this._chkUseSignature.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkUseSignature.Location = new System.Drawing.Point(0, 100);
            this._chkUseSignature.Name = "_chkUseSignature";
            this._chkUseSignature.Size = new System.Drawing.Size(280, 20);
            this._chkUseSignature.TabIndex = 13;
            this._chkUseSignature.Text = "Include signature in outgoing messages";
            this._chkUseSignature.CheckedChanged += new System.EventHandler(this._useSignature_CheckedChanged);
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(56, 16);
            this.label1.TabIndex = 16;
            this.label1.Text = "Replies";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Location = new System.Drawing.Point(56, 2);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(336, 8);
            this.groupBox1.TabIndex = 17;
            this.groupBox1.TabStop = false;
            // 
            // label2
            // 
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(0, 80);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 16);
            this.label2.TabIndex = 18;
            this.label2.Text = "Signature";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Location = new System.Drawing.Point(68, 82);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(320, 8);
            this.groupBox2.TabIndex = 19;
            this.groupBox2.TabStop = false;
            // 
            // _grpSignatureInReplies
            // 
            this._grpSignatureInReplies.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._grpSignatureInReplies.Controls.Add(this._radSignatureAfterQuoting);
            this._grpSignatureInReplies.Controls.Add(this._radSignatureBeforeQuoting);
            this._grpSignatureInReplies.Controls.Add(this._radReplySignatureNone);
            this._grpSignatureInReplies.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._grpSignatureInReplies.Location = new System.Drawing.Point(12, 212);
            this._grpSignatureInReplies.Name = "_grpSignatureInReplies";
            this._grpSignatureInReplies.Size = new System.Drawing.Size(380, 84);
            this._grpSignatureInReplies.TabIndex = 20;
            this._grpSignatureInReplies.TabStop = false;
            this._grpSignatureInReplies.Text = "Signature in Replies";
            // 
            // _radSignatureAfterQuoting
            // 
            this._radSignatureAfterQuoting.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._radSignatureAfterQuoting.Location = new System.Drawing.Point(8, 60);
            this._radSignatureAfterQuoting.Name = "_radSignatureAfterQuoting";
            this._radSignatureAfterQuoting.Size = new System.Drawing.Size(236, 20);
            this._radSignatureAfterQuoting.TabIndex = 2;
            this._radSignatureAfterQuoting.Text = "After quoted text";
            // 
            // _radSignatureBeforeQuoting
            // 
            this._radSignatureBeforeQuoting.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._radSignatureBeforeQuoting.Location = new System.Drawing.Point(8, 40);
            this._radSignatureBeforeQuoting.Name = "_radSignatureBeforeQuoting";
            this._radSignatureBeforeQuoting.Size = new System.Drawing.Size(240, 20);
            this._radSignatureBeforeQuoting.TabIndex = 1;
            this._radSignatureBeforeQuoting.Text = "Before quoted text";
            // 
            // _radReplySignatureNone
            // 
            this._radReplySignatureNone.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._radReplySignatureNone.Location = new System.Drawing.Point(8, 20);
            this._radReplySignatureNone.Name = "_radReplySignatureNone";
            this._radReplySignatureNone.Size = new System.Drawing.Size(104, 20);
            this._radReplySignatureNone.TabIndex = 0;
            this._radReplySignatureNone.Text = "None";
            // 
            // MailFormatPane
            // 
            this.Controls.Add(this._grpSignatureInReplies);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._signatureBox);
            this.Controls.Add(this._chkUseSignature);
            this.Controls.Add(this._chkGreetingInReplies);
            this.Controls.Add(this._boxPrefix);
            this.Controls.Add(this._chkPrefixInitials);
            this.Name = "MailFormatPane";
            this.Size = new System.Drawing.Size(396, 324);
            this._grpSignatureInReplies.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        public static AbstractOptionsPane MailFormatPaneCreator()
        {
            return new MailFormatPane();
        }

        public override void ShowPane()
        {
            _ini = ICore.Instance.SettingStore;
            _chkGreetingInReplies.Checked = _ini.ReadBool( "MailFormat", "GreetingInReplies", true );
            _chkPrefixInitials.Checked = _ini.ReadBool( "MailFormat", "PrefixInitials", false );
            _boxPrefix.Text = _ini.ReadString( "MailFormat", "GreetingString", "Hello" );

            /** 
             * signatures 
             */
            _chkUseSignature.Checked = _ini.ReadBool( "MailFormat", "UseSignature", false );
            _signatureBox.Enabled = _chkUseSignature.Checked;
            if ( _signatureBox.Enabled )
            {
                _signatureBox.Text = _ini.ReadString( "MailFormat", "Signature" );
            }

            switch( _ini.ReadInt( "MailFormat", "SignatureInReplies", 1 ) )
            {
                case 0: _radReplySignatureNone.Checked = true; break;
                case 1: _radSignatureBeforeQuoting.Checked = true; break;
                case 2: _radSignatureAfterQuoting.Checked = true; break;
            }
        }

        public override void OK()
        {
            _ini.WriteBool  ( "MailFormat", "GreetingInReplies", _chkGreetingInReplies.Checked );
            _ini.WriteBool  ( "MailFormat", "PrefixInitials", _chkPrefixInitials.Checked );
            _ini.WriteString( "MailFormat", "GreetingString", _boxPrefix.Text );

            _ini.WriteBool( "MailFormat", "UseSignature", _chkUseSignature.Checked );
            if( _chkUseSignature.Checked )
                _ini.WriteString( "MailFormat", "Signature", _signatureBox.Text );

            int signatureInReplies = 1;
            if ( _radReplySignatureNone.Checked )
            {
                signatureInReplies = 0;
            }
            else if ( _radSignatureAfterQuoting.Checked )
            {
                signatureInReplies = 2;
            }
            _ini.WriteInt( "MailFormat", "SignatureInReplies", signatureInReplies );
        }

        private void _chkGreetingInReplies_CheckedChanged(object sender, System.EventArgs e)
        {
            _boxPrefix.Enabled = _chkGreetingInReplies.Checked;
        }

        private void _boxPrefix_TextChanged(object sender, System.EventArgs e)
        {
        }

        private void _useSignature_CheckedChanged(object sender, System.EventArgs e)
        {
            if( !_chkUseSignature.Checked )
            {
                _signatureBox.Text = string.Empty;
                _signatureBox.Enabled = false;
                _grpSignatureInReplies.Enabled = false;
            }
            else
            {
                _signatureBox.Enabled = true;
                string signature = ICore.Instance.SettingStore.ReadString( "MailFormat", "Signature" );
                if( signature.Length > 0 )
                {
                    _signatureBox.Text = signature;
                }
                else
                {
                    IContact myself = Core.ContactManager.MySelf;
                    _signatureBox.Text = "WBR,\r\n" + myself.Resource.DisplayName;

                    if ( myself.DefaultEmailAddress != null )
                    {
                        _signatureBox.Text += "\r\nmailto:" + myself.DefaultEmailAddress;
                    }
                }
                _grpSignatureInReplies.Enabled = true;
            }
        }

        public override string GetHelpKeyword()
        {
            return "/reference/mail_format.html";
        }
    }
}
