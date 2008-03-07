/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

namespace JetBrains.ExceptionReport
{
	/// <summary>
	/// Summary description for ExceptionProxySetup.
	/// </summary>
	public class ExceptionProxySetup : System.Windows.Forms.Form
	{
    private System.Windows.Forms.Button buttonOK;
    private System.Windows.Forms.Button buttonCancel;
    private System.Windows.Forms.CheckBox checkCustom;
    private System.Windows.Forms.CheckBox checkAuthentication;
    private System.Windows.Forms.GroupBox groupBox2;
    private System.Windows.Forms.TextBox textPassword;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.TextBox textLogin;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.TextBox textHost;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.TextBox textPort;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.Label label6;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

	  public ProxySettings ProxySettings
	  {
	    get
	    {
        ProxySettings settings = new ProxySettings();
        settings.CustomProxy = checkCustom.Checked;
        settings.Host = textHost.Text;
        settings.Port = Convert.ToInt32(textPort.Text);
        settings.Authentication = checkAuthentication.Checked;
        settings.Login = textLogin.Text;
        settings.Password = textPassword.Text;
        return settings;
	    }
	  }

	  public ExceptionProxySetup(ProxySettings settings)
		{
      InitializeComponent();

      checkCustom.Checked = settings.CustomProxy;
      textHost.Text = settings.Host;
      textPort.Text = settings.Port.ToString();
      checkAuthentication.Checked = settings.Authentication;
      textLogin.Text = settings.Login;
      textPassword.Text = settings.Password;

      UpdateControls();
		}

	  private void UpdateControls()
	  {
      if (checkCustom.Checked)
      {
        textHost.Enabled = true;
        textPort.Enabled = true;
        checkAuthentication.Enabled = true;
        textLogin.Enabled = checkAuthentication.Checked;
        textPassword.Enabled = checkAuthentication.Checked;
      }
      else
      {
        textHost.Enabled = false;
        textPort.Enabled = false;
        checkAuthentication.Enabled = false;
        textLogin.Enabled = false;
        textPassword.Enabled = false;
      }
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
      this.checkCustom = new System.Windows.Forms.CheckBox();
      this.buttonOK = new System.Windows.Forms.Button();
      this.buttonCancel = new System.Windows.Forms.Button();
      this.checkAuthentication = new System.Windows.Forms.CheckBox();
      this.groupBox2 = new System.Windows.Forms.GroupBox();
      this.textLogin = new System.Windows.Forms.TextBox();
      this.label4 = new System.Windows.Forms.Label();
      this.textPassword = new System.Windows.Forms.TextBox();
      this.label3 = new System.Windows.Forms.Label();
      this.label1 = new System.Windows.Forms.Label();
      this.textHost = new System.Windows.Forms.TextBox();
      this.label2 = new System.Windows.Forms.Label();
      this.textPort = new System.Windows.Forms.TextBox();
      this.label5 = new System.Windows.Forms.Label();
      this.label6 = new System.Windows.Forms.Label();
      this.groupBox2.SuspendLayout();
      this.SuspendLayout();
      // 
      // checkCustom
      // 
      this.checkCustom.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.checkCustom.Location = new System.Drawing.Point(8, 8);
      this.checkCustom.Name = "checkCustom";
      this.checkCustom.Size = new System.Drawing.Size(120, 16);
      this.checkCustom.TabIndex = 0;
      this.checkCustom.Text = "Use proxy server";
      this.checkCustom.CheckedChanged += new System.EventHandler(this.checkDefault_CheckedChanged);
      // 
      // buttonOK
      // 
      this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.buttonOK.Location = new System.Drawing.Point(104, 208);
      this.buttonOK.Name = "buttonOK";
      this.buttonOK.TabIndex = 5;
      this.buttonOK.Text = "OK";
      // 
      // buttonCancel
      // 
      this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.buttonCancel.Location = new System.Drawing.Point(184, 208);
      this.buttonCancel.Name = "buttonCancel";
      this.buttonCancel.TabIndex = 6;
      this.buttonCancel.Text = "Cancel";
      // 
      // checkAuthentication
      // 
      this.checkAuthentication.BackColor = System.Drawing.Color.Transparent;
      this.checkAuthentication.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
      this.checkAuthentication.Location = new System.Drawing.Point(16, 24);
      this.checkAuthentication.Name = "checkAuthentication";
      this.checkAuthentication.Size = new System.Drawing.Size(208, 16);
      this.checkAuthentication.TabIndex = 5;
      this.checkAuthentication.Text = "Proxy server requires authentication";
      this.checkAuthentication.CheckedChanged += new System.EventHandler(this.checkAuthentication_CheckedChanged);
      // 
      // groupBox2
      // 
      this.groupBox2.Controls.Add(this.textLogin);
      this.groupBox2.Controls.Add(this.label4);
      this.groupBox2.Controls.Add(this.textPassword);
      this.groupBox2.Controls.Add(this.checkAuthentication);
      this.groupBox2.Controls.Add(this.label3);
      this.groupBox2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
      this.groupBox2.Location = new System.Drawing.Point(24, 88);
      this.groupBox2.Name = "groupBox2";
      this.groupBox2.Size = new System.Drawing.Size(240, 104);
      this.groupBox2.TabIndex = 6;
      this.groupBox2.TabStop = false;
      this.groupBox2.Text = "Authentication";
      // 
      // textLogin
      // 
      this.textLogin.Location = new System.Drawing.Point(104, 48);
      this.textLogin.Name = "textLogin";
      this.textLogin.Size = new System.Drawing.Size(120, 21);
      this.textLogin.TabIndex = 1;
      this.textLogin.Text = "textBox1";
      // 
      // label4
      // 
      this.label4.Location = new System.Drawing.Point(32, 72);
      this.label4.Name = "label4";
      this.label4.Size = new System.Drawing.Size(64, 23);
      this.label4.TabIndex = 2;
      this.label4.Text = "Password:";
      // 
      // textPassword
      // 
      this.textPassword.Location = new System.Drawing.Point(104, 72);
      this.textPassword.Name = "textPassword";
      this.textPassword.PasswordChar = '*';
      this.textPassword.Size = new System.Drawing.Size(120, 21);
      this.textPassword.TabIndex = 3;
      this.textPassword.Text = "textBox1";
      // 
      // label3
      // 
      this.label3.Location = new System.Drawing.Point(32, 48);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(48, 23);
      this.label3.TabIndex = 0;
      this.label3.Text = "Login:";
      // 
      // label1
      // 
      this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
      this.label1.Location = new System.Drawing.Point(24, 32);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(48, 16);
      this.label1.TabIndex = 1;
      this.label1.Text = "Host:";
      this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
      // 
      // textHost
      // 
      this.textHost.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
      this.textHost.Location = new System.Drawing.Point(80, 32);
      this.textHost.Name = "textHost";
      this.textHost.Size = new System.Drawing.Size(184, 21);
      this.textHost.TabIndex = 2;
      this.textHost.Text = "textBox1";
      // 
      // label2
      // 
      this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.label2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
      this.label2.Location = new System.Drawing.Point(24, 56);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(48, 16);
      this.label2.TabIndex = 3;
      this.label2.Text = "Port:";
      // 
      // textPort
      // 
      this.textPort.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
      this.textPort.Location = new System.Drawing.Point(80, 56);
      this.textPort.Name = "textPort";
      this.textPort.Size = new System.Drawing.Size(80, 21);
      this.textPort.TabIndex = 4;
      this.textPort.Text = "textBox1";
      // 
      // label5
      // 
      this.label5.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.label5.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
      this.label5.Location = new System.Drawing.Point(24, 32);
      this.label5.Name = "label5";
      this.label5.Size = new System.Drawing.Size(48, 16);
      this.label5.TabIndex = 1;
      this.label5.Text = "Host:";
      this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
      // 
      // label6
      // 
      this.label6.FlatStyle = System.Windows.Forms.FlatStyle.System;
      this.label6.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
      this.label6.Location = new System.Drawing.Point(24, 56);
      this.label6.Name = "label6";
      this.label6.Size = new System.Drawing.Size(48, 16);
      this.label6.TabIndex = 3;
      this.label6.Text = "Port:";
      // 
      // ExceptionProxySetup
      // 
      this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
      this.ClientSize = new System.Drawing.Size(274, 245);
      this.ControlBox = false;
      this.Controls.Add(this.checkCustom);
      this.Controls.Add(this.buttonCancel);
      this.Controls.Add(this.buttonOK);
      this.Controls.Add(this.label1);
      this.Controls.Add(this.label2);
      this.Controls.Add(this.textHost);
      this.Controls.Add(this.textPort);
      this.Controls.Add(this.groupBox2);
      this.Controls.Add(this.label5);
      this.Controls.Add(this.label6);
      this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.Name = "ExceptionProxySetup";
      this.ShowInTaskbar = false;
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
      this.Text = "Proxy Settings";
      this.groupBox2.ResumeLayout(false);
      this.ResumeLayout(false);

    }
		#endregion

    private void checkDefault_CheckedChanged(object sender, System.EventArgs e)
    {
      UpdateControls();
    }

    private void checkAuthentication_CheckedChanged(object sender, System.EventArgs e)
    {
      UpdateControls();
    }
	}
}
