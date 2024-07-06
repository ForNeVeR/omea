// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

namespace JetBrains.Omea.SamplePlugins.LiveJournalPlugin
{
    /// <summary>
    /// Summary description for FriendsImportForm.
    /// </summary>
    internal class FriendsImportForm : System.Windows.Forms.Form
    {
        private string _login;
        private string _password;
        private int _updateFreq;
        private int _updatePeriod;

        private System.Windows.Forms.Label labelLJUsername;
        private System.Windows.Forms.TextBox textLJUsername;
        private System.Windows.Forms.Label labelPassword;
        private System.Windows.Forms.TextBox textPassword;
        private System.Windows.Forms.CheckBox cbUpdate;
        private System.Windows.Forms.NumericUpDown numericudUpdate;
        private System.Windows.Forms.ComboBox comboPeriod;
        private System.Windows.Forms.Button buttonImport;
        private System.Windows.Forms.Button buttonCancel;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        internal string Login    { get { return _login;    } }
        internal string Password { get { return _password; } }
        internal int    UpdateFreq   { get { return _updateFreq;   } }
        internal int    UpdatePeriod { get { return _updatePeriod; } }

        public FriendsImportForm(string login, string passwd, int updateFreq, int updatePeriod)
        {
            _login = login;
            _password = passwd;
            _updateFreq = updateFreq;
            _updatePeriod = updatePeriod;
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            //
            // TODO: Add any constructor code after InitializeComponent call
            //
            textLJUsername.Text = _login;
            textPassword.Text = _password;

            numericudUpdate.Value = _updateFreq;
            comboPeriod.SelectedIndex = updatePeriod;
            cbUpdate.Checked = updateFreq > 0;
            numericudUpdate.Enabled = cbUpdate.Checked;
            comboPeriod.Enabled = cbUpdate.Checked;
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
            this.buttonImport = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.labelLJUsername = new System.Windows.Forms.Label();
            this.textLJUsername = new System.Windows.Forms.TextBox();
            this.textPassword = new System.Windows.Forms.TextBox();
            this.labelPassword = new System.Windows.Forms.Label();
            this.numericudUpdate = new System.Windows.Forms.NumericUpDown();
            this.cbUpdate = new System.Windows.Forms.CheckBox();
            this.comboPeriod = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.numericudUpdate)).BeginInit();
            this.SuspendLayout();
            //
            // buttonImport
            //
            this.buttonImport.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonImport.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonImport.Location = new System.Drawing.Point(80, 104);
            this.buttonImport.Name = "buttonImport";
            this.buttonImport.TabIndex = 7;
            this.buttonImport.Text = "&Import";
            this.buttonImport.Click += new System.EventHandler(this.buttonImport_Click);
            //
            // buttonCancel
            //
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonCancel.Location = new System.Drawing.Point(176, 104);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.TabIndex = 8;
            this.buttonCancel.Text = "&Cancel";
            //
            // labelLJUsername
            //
            this.labelLJUsername.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelLJUsername.Location = new System.Drawing.Point(8, 8);
            this.labelLJUsername.Name = "labelLJUsername";
            this.labelLJUsername.Size = new System.Drawing.Size(80, 16);
            this.labelLJUsername.TabIndex = 0;
            this.labelLJUsername.Text = "&LJ username:";
            this.labelLJUsername.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // textLJUsername
            //
            this.textLJUsername.Location = new System.Drawing.Point(96, 8);
            this.textLJUsername.Name = "textLJUsername";
            this.textLJUsername.Size = new System.Drawing.Size(224, 20);
            this.textLJUsername.TabIndex = 1;
            this.textLJUsername.Text = "";
            //
            // textPassword
            //
            this.textPassword.Location = new System.Drawing.Point(96, 40);
            this.textPassword.Name = "textPassword";
            this.textPassword.PasswordChar = '*';
            this.textPassword.Size = new System.Drawing.Size(224, 20);
            this.textPassword.TabIndex = 3;
            this.textPassword.Text = "";
            //
            // labelPassword
            //
            this.labelPassword.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelPassword.Location = new System.Drawing.Point(8, 40);
            this.labelPassword.Name = "labelPassword";
            this.labelPassword.Size = new System.Drawing.Size(80, 16);
            this.labelPassword.TabIndex = 2;
            this.labelPassword.Text = "&Password:";
            this.labelPassword.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // numericudUpdate
            //
            this.numericudUpdate.Location = new System.Drawing.Point(96, 72);
            this.numericudUpdate.Minimum = new System.Decimal(new int[] {
                                                                            1,
                                                                            0,
                                                                            0,
                                                                            0});
            this.numericudUpdate.Name = "numericudUpdate";
            this.numericudUpdate.Size = new System.Drawing.Size(40, 20);
            this.numericudUpdate.TabIndex = 5;
            this.numericudUpdate.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericudUpdate.Value = new System.Decimal(new int[] {
                                                                          1,
                                                                          0,
                                                                          0,
                                                                          0});
            //
            // cbUpdate
            //
            this.cbUpdate.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cbUpdate.Location = new System.Drawing.Point(8, 72);
            this.cbUpdate.Name = "cbUpdate";
            this.cbUpdate.Size = new System.Drawing.Size(88, 16);
            this.cbUpdate.TabIndex = 4;
            this.cbUpdate.Text = "&Update every";
            this.cbUpdate.CheckedChanged += new System.EventHandler(this.cbUpdate_CheckedChanged);
            //
            // comboPeriod
            //
            this.comboPeriod.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboPeriod.Items.AddRange(new object[] {
                                                             "minutes",
                                                             "hours",
                                                             "days",
                                                             "weeks"});
            this.comboPeriod.Location = new System.Drawing.Point(144, 72);
            this.comboPeriod.Name = "comboPeriod";
            this.comboPeriod.Size = new System.Drawing.Size(121, 21);
            this.comboPeriod.TabIndex = 6;
            //
            // FriendsImportForm
            //
            this.AcceptButton = this.buttonImport;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(330, 135);
            this.Controls.Add(this.comboPeriod);
            this.Controls.Add(this.cbUpdate);
            this.Controls.Add(this.numericudUpdate);
            this.Controls.Add(this.textPassword);
            this.Controls.Add(this.textLJUsername);
            this.Controls.Add(this.labelPassword);
            this.Controls.Add(this.labelLJUsername);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonImport);
            this.Font = new System.Drawing.Font("Tahoma", 8F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FriendsImportForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Import LiveJournal Friends";
            ((System.ComponentModel.ISupportInitialize)(this.numericudUpdate)).EndInit();
            this.ResumeLayout(false);

        }
        #endregion

        private void buttonImport_Click(object sender, System.EventArgs e)
        {
            _login = textLJUsername.Text;
            _password = textPassword.Text;
            _updateFreq = cbUpdate.Checked ? (int) numericudUpdate.Value : -1;
            _updatePeriod = comboPeriod.SelectedIndex;
        }

        private void cbUpdate_CheckedChanged(object sender, System.EventArgs e)
        {
            numericudUpdate.Enabled = cbUpdate.Checked;
            comboPeriod.Enabled = cbUpdate.Checked;
        }
    }
}
