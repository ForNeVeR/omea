/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace JetBrains.ExceptionReport
{
    public class ExceptionReportForm : Form
    {
        private static Hashtable _excludedExceptions = new Hashtable();

        private IExceptionSubmitter _submitter;
        private string _projectKey;

        private Label label1;
        private TextBox _errorText;
        private GroupBox _grpITNLogin;
        private Label label2;
        private TextBox _edtUserName;
        private Label label3;
        private TextBox _edtPassword;
        private Button _btnSubmit;
        private Label _lblProgress;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private Container components = null;

        private Exception _exception;
        private Label label4;
        private TextBox _edtDescription;
        private Label label5;
        private CheckBox _chkDontShowAgain;
        private int _buildNumber;
        private string _buildDesc;
        private Button _btnIgnore;
        private LinkLabel _linkITNRegister;
        private bool _displaySubmissionResult;

        private ProxySettings _proxySettings = new ProxySettings();
        private CheckBox _chkAttachLog;
        private Button _btnProxy;
        private string _defaultUserName;
        private string _defaultPassword;

        public ExceptionReportForm()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            _buildNumber = Assembly.GetExecutingAssembly().GetName().Version.Build;
            _submitter = null;
        }

        public IExceptionSubmitter Submitter
        {
            set { _submitter = value; }
        }

        public string ProjectKey
        {
            get { return _projectKey; }
            set { _projectKey = value; }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if (disposing)
            {
                if (components != null)
                    components.Dispose();
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
            this.label1 = new System.Windows.Forms.Label();
            this._errorText = new System.Windows.Forms.TextBox();
            this._grpITNLogin = new System.Windows.Forms.GroupBox();
            this._linkITNRegister = new System.Windows.Forms.LinkLabel();
            this._edtPassword = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this._edtUserName = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this._btnProxy = new System.Windows.Forms.Button();
            this._btnSubmit = new System.Windows.Forms.Button();
            this._lblProgress = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this._edtDescription = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this._chkDontShowAgain = new System.Windows.Forms.CheckBox();
            this._btnIgnore = new System.Windows.Forms.Button();
            this._chkAttachLog = new System.Windows.Forms.CheckBox();
            this._grpITNLogin.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(424, 32);
            this.label1.TabIndex = 0;
            this.label1.Text = "An internal error has occured. To help us fix the problem, please submit the erro" +
                "r information to the ITN tracker.";
            // 
            // _errorText
            // 
            this._errorText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._errorText.Location = new System.Drawing.Point(8, 172);
            this._errorText.Multiline = true;
            this._errorText.Name = "_errorText";
            this._errorText.ReadOnly = true;
            this._errorText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._errorText.Size = new System.Drawing.Size(424, 92);
            this._errorText.TabIndex = 4;
            this._errorText.Text = "";
            // 
            // _grpITNLogin
            // 
            this._grpITNLogin.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._grpITNLogin.Controls.Add(this._linkITNRegister);
            this._grpITNLogin.Controls.Add(this._edtPassword);
            this._grpITNLogin.Controls.Add(this.label3);
            this._grpITNLogin.Controls.Add(this._edtUserName);
            this._grpITNLogin.Controls.Add(this.label2);
            this._grpITNLogin.Controls.Add(this._btnProxy);
            this._grpITNLogin.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._grpITNLogin.Location = new System.Drawing.Point(8, 268);
            this._grpITNLogin.Name = "_grpITNLogin";
            this._grpITNLogin.Size = new System.Drawing.Size(424, 96);
            this._grpITNLogin.TabIndex = 5;
            this._grpITNLogin.TabStop = false;
            this._grpITNLogin.Text = "JIRA Login (optional)";
            // 
            // _linkITNRegister
            // 
            this._linkITNRegister.Location = new System.Drawing.Point(84, 68);
            this._linkITNRegister.Name = "_linkITNRegister";
            this._linkITNRegister.Size = new System.Drawing.Size(100, 16);
            this._linkITNRegister.TabIndex = 4;
            this._linkITNRegister.TabStop = true;
            this._linkITNRegister.Text = "&Register at JIRA";
            this._linkITNRegister.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this._linkITNRegister_LinkClicked);
            // 
            // _edtPassword
            // 
            this._edtPassword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtPassword.Location = new System.Drawing.Point(84, 44);
            this._edtPassword.Name = "_edtPassword";
            this._edtPassword.PasswordChar = '*';
            this._edtPassword.Size = new System.Drawing.Size(332, 21);
            this._edtPassword.TabIndex = 3;
            this._edtPassword.Text = "";
            // 
            // label3
            // 
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label3.Location = new System.Drawing.Point(8, 48);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(64, 16);
            this.label3.TabIndex = 2;
            this.label3.Text = "&Password:";
            // 
            // _edtUserName
            // 
            this._edtUserName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtUserName.Location = new System.Drawing.Point(84, 19);
            this._edtUserName.Name = "_edtUserName";
            this._edtUserName.Size = new System.Drawing.Size(332, 21);
            this._edtUserName.TabIndex = 1;
            this._edtUserName.Text = "";
            // 
            // label2
            // 
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(8, 24);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(64, 20);
            this.label2.TabIndex = 0;
            this.label2.Text = "&User name:";
            // 
            // _btnProxy
            // 
            this._btnProxy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnProxy.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnProxy.Location = new System.Drawing.Point(304, 68);
            this._btnProxy.Name = "_btnProxy";
            this._btnProxy.Size = new System.Drawing.Size(112, 23);
            this._btnProxy.TabIndex = 5;
            this._btnProxy.Text = "Pro&xy Settings";
            this._btnProxy.Click += new System.EventHandler(this._btnProxy_Click);
            // 
            // _btnSubmit
            // 
            this._btnSubmit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnSubmit.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnSubmit.Location = new System.Drawing.Point(272, 408);
            this._btnSubmit.Name = "_btnSubmit";
            this._btnSubmit.TabIndex = 9;
            this._btnSubmit.Text = "&Submit";
            this._btnSubmit.Click += new System.EventHandler(this._btnSubmit_Click);
            // 
            // _lblProgress
            // 
            this._lblProgress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblProgress.Location = new System.Drawing.Point(8, 388);
            this._lblProgress.Name = "_lblProgress";
            this._lblProgress.Size = new System.Drawing.Size(424, 16);
            this._lblProgress.TabIndex = 8;
            this._lblProgress.TextAlign = System.Drawing.ContentAlignment.TopCenter;
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label4.Location = new System.Drawing.Point(8, 40);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(424, 16);
            this.label4.TabIndex = 1;
            this.label4.Text = "Please &tell us what you were doing when you got the problem:";
            // 
            // _edtDescription
            // 
            this._edtDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._edtDescription.Location = new System.Drawing.Point(8, 56);
            this._edtDescription.Multiline = true;
            this._edtDescription.Name = "_edtDescription";
            this._edtDescription.Size = new System.Drawing.Size(424, 92);
            this._edtDescription.TabIndex = 2;
            this._edtDescription.Text = "";
            // 
            // label5
            // 
            this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.label5.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label5.Location = new System.Drawing.Point(8, 156);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(424, 16);
            this.label5.TabIndex = 3;
            this.label5.Text = "Technical &details:";
            // 
            // _chkDontShowAgain
            // 
            this._chkDontShowAgain.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._chkDontShowAgain.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkDontShowAgain.Location = new System.Drawing.Point(8, 368);
            this._chkDontShowAgain.Name = "_chkDontShowAgain";
            this._chkDontShowAgain.Size = new System.Drawing.Size(196, 16);
            this._chkDontShowAgain.TabIndex = 6;
            this._chkDontShowAgain.Text = "Do &not show this exception again";
            // 
            // _btnIgnore
            // 
            this._btnIgnore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._btnIgnore.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnIgnore.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnIgnore.Location = new System.Drawing.Point(356, 408);
            this._btnIgnore.Name = "_btnIgnore";
            this._btnIgnore.TabIndex = 10;
            this._btnIgnore.Text = "&Ignore";
            // 
            // _chkAttachLog
            // 
            this._chkAttachLog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._chkAttachLog.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkAttachLog.Location = new System.Drawing.Point(240, 368);
            this._chkAttachLog.Name = "_chkAttachLog";
            this._chkAttachLog.Size = new System.Drawing.Size(192, 16);
            this._chkAttachLog.TabIndex = 7;
            this._chkAttachLog.Text = "Attach &log file";
            this._chkAttachLog.Visible = false;
            // 
            // ExceptionReportForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(440, 465);
            this.ControlBox = false;
            this.Controls.Add(this._chkAttachLog);
            this.Controls.Add(this._btnIgnore);
            this.Controls.Add(this._chkDontShowAgain);
            this.Controls.Add(this.label5);
            this.Controls.Add(this._edtDescription);
            this.Controls.Add(this._errorText);
            this.Controls.Add(this.label4);
            this.Controls.Add(this._lblProgress);
            this.Controls.Add(this._btnSubmit);
            this.Controls.Add(this._grpITNLogin);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.MinimumSize = new System.Drawing.Size(448, 473);
            this.Name = "ExceptionReportForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Unhandled Exception";
            this.VisibleChanged += new System.EventHandler(this.ExceptionReportForm_VisibleChanged);
            this._grpITNLogin.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        public bool DisplaySubmissionResult
        {
            set { _displaySubmissionResult = value; }
        }

        public void SetITNLogin( string userName, string password )
        {
            _edtUserName.Text = userName;
            _edtPassword.Text = password;
        }

        public void SetDefaultLogin( string userName, string password )
        {
            _defaultUserName = userName;
            _defaultPassword = password;
        }

        public void SetProxy( ProxySettings settings )
        {
            _proxySettings = settings;
        }

        public void SetBuildNumber( int buildNumber )
        {
            _buildNumber = buildNumber;
        }

        public string ITNUserName
        {
            get { return _edtUserName.Text; }
        }

        public string ITNPassword
        {
            get { return _edtPassword.Text; }
        }

        public ProxySettings ProxySettings
        {
            get { return _proxySettings; }
        }

        public DialogResult ReportException( IWin32Window ownerWindow, Exception e, string buildDesc )
        {
            string excString = ITNExceptionSubmitter.FilterExceptionString( e.ToString() );
            if (_excludedExceptions.ContainsKey( excString ))
            {
                AttachLog = false;
                return DialogResult.None;
            }

            _errorText.Text = e.ToString();
            _errorText.Select( 0, 0 );
            _exception = e;
            _buildDesc = buildDesc;
            DialogResult dlgResult = ownerWindow == null ? ShowDialog() : ShowDialog( ownerWindow );

            if (dlgResult == DialogResult.OK && _chkDontShowAgain.Checked)
            {
                string filterStr = ITNExceptionSubmitter.FilterExceptionString( _exception.ToString() );
                if (!_excludedExceptions.ContainsKey( filterStr ))
                    _excludedExceptions.Add( filterStr, true );
            }
            return dlgResult;
        }


        private void _btnSubmit_Click( object sender, EventArgs e )
        {
            string userName = _edtUserName.Text;
            string password = _edtPassword.Text;
            if ( userName == "" || password == "")
            {
                if ( _defaultUserName == "" || _defaultPassword == "" )
                {
                    MessageBox.Show( this, "Please enter your user name and password.", "Report Unhandled Exception", MessageBoxButtons.OK, MessageBoxIcon.Exclamation );
                    return;
                }
                userName = _defaultUserName;
                password = _defaultPassword;
            }

            try
            {
                _btnSubmit.Enabled = false;

                string description;
                if (_edtDescription.Text.Length > 0)
                    description = _edtDescription.Text + "\n" + _buildDesc;
                else
                    description = _buildDesc;

                _submitter.SubmitProgress += new SubmitProgressEventHandler( submitter_SubmitProgress );
                SubmissionResult submissionResult = _submitter.SubmitException( _exception, description, userName, password, _buildNumber, ProxySettings.Proxy );

                if (_displaySubmissionResult && submissionResult != null)
                {
                    ShowSubmissionResult( this, submissionResult, this._projectKey );
                }
                else
                {
                    MessageBox.Show( this, "Thank you for your bug report!", "Report Unhandled Exception", MessageBoxButtons.OK, MessageBoxIcon.Information );
                }
                
                DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show( this, "Error submitting exception to the tracker.\n" + ex.ToString(), "Report Unhandled Exception", MessageBoxButtons.OK, MessageBoxIcon.Error );
            }
            _submitter = null;
        }

        public static void ShowSubmissionResult( IWin32Window owner, SubmissionResult submissionResult, string projectKey )
        {
            string msgBoxText = "Thank you for your bug report!";
            if ( IsRequestFixed( submissionResult ) )
            {
                string resolution = GetRequestResolution( submissionResult );
                if ( resolution == null )
                {
                    msgBoxText += "\nThis request (" + BuildRequestNumber(  submissionResult.ThreadId, projectKey ) + ") has already been FIXED";
                }
                else
                {
                    msgBoxText += "\nThis request (" + BuildRequestNumber(  submissionResult.ThreadId, projectKey ) + ") has been resolved as " + resolution;
                }
                msgBoxText += GetFixVersionText( submissionResult );
            }
            else
            {
                string verb = submissionResult.IsUpdated ? "updated" : "created";
                msgBoxText += "\n";
                msgBoxText += "Request " + BuildRequestNumber(  submissionResult.ThreadId, projectKey ) + " has been " + verb;
            }
            MessageBox.Show( owner, msgBoxText, "Report Unhandled Exception", MessageBoxButtons.OK, MessageBoxIcon.Information );
        }

        private static bool IsRequestFixed( SubmissionResult submissionResult )
        {
            XmlDocument requestDescripion = submissionResult.RequestDescription;
            if (requestDescripion == null)
                return false;
            string state = requestDescripion.DocumentElement.GetAttribute( "state" );
            return state == "Fixed" || state == "Resolved";
        }

        private static string GetRequestResolution( SubmissionResult submissionResult )
        {
            XmlDocument requestDescripion = submissionResult.RequestDescription;
            if (requestDescripion == null)
                return null;
            return requestDescripion.DocumentElement.GetAttribute( "resolution" );
        }

        private static string GetFixVersionText( SubmissionResult submissionResult )
        {
            XmlDocument requestDescripion = submissionResult.RequestDescription;
            if (requestDescripion == null)
                return "";

            XmlNodeList versionNodes = requestDescripion.SelectNodes( "//fixVersions/version" );
            if ( versionNodes.Count == 0 )
                return "";

            if ( versionNodes.Count == 1 )
            {
                return " in version " + versionNodes [0].Attributes ["name"].Value;
            }

            StringBuilder result = new StringBuilder( " in versions " );
            bool first = true;
            foreach( XmlNode node in versionNodes )
            {
                if ( !first )
                {
                    result.Append( ", " );
                }
                else
                {
                    first = false;
                }
                result.Append( node.Attributes ["name"].Value );
            }
            return result.ToString();
        }

        private void ExceptionReportForm_VisibleChanged( object sender, EventArgs e )
        {
            BringToFront();
        }

        private void _linkITNRegister_LinkClicked( object sender, LinkLabelLinkClickedEventArgs e )
        {
            Process.Start( "http://www.jetbrains.net/jira/Signup!default.jspa" );
        }

        private void submitter_SubmitProgress( object sender, SubmitProgressEventArgs e )
        {
            _lblProgress.Text = e.Message;
            Application.DoEvents();
        }

        private void _btnProxy_Click( object sender, EventArgs e )
        {
            using (ExceptionProxySetup dlg = new ExceptionProxySetup( _proxySettings ))
            {
                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                try
                {
                    _proxySettings = dlg.ProxySettings;
                }
                catch( FormatException )
                {
                    MessageBox.Show( this, "Invalid port number specified in proxy settings.", "Exception Reporter",
                        MessageBoxButtons.OK, MessageBoxIcon.Error );
                }
            }
        }

        public bool AttachLog
        {
            get { return _chkAttachLog.Checked; }
            set
            {
                if (value)
                    _chkAttachLog.Visible = true;
                _chkAttachLog.Checked = value;
            }
        }

        private static string BuildRequestNumber( int id, string projectKey )
        {
            if ( projectKey != null )
            {
                return projectKey + "-" + id;
            }
            return "#" + id;
        }
    }
}