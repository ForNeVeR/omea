/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.DebugPlugin
{
	/// <summary>
	/// Summary description for SettingOptionsForDebugDlg.
	/// </summary>
	public class SettingOptionsForDebugDlg : System.Windows.Forms.Form
	{
        private CheckBoxSettingEditor _boolSetting;
        private NumericUpDownSettingEditor _intSetting;
        private StringSettingEditor _stringSetting;
        private StringSettingEditor _stringSettingDef;

        private static int IntDebugSetting;
        private static int BoolDebugSetting;
        private static int StringDebugSetting;
        private static int StringDebugSettingDef;
        private static int IntComboDebugSetting;
        private static int IntRadioDebugSetting;
        private const string DebugOption = "DebugOption";

        private System.Windows.Forms.Button _okBtn;
        private System.Windows.Forms.Button _closeBtn;
        private System.Windows.Forms.Button _reCreateOptionsBtn;
        private ComboBoxSettingEditor _cmbIntSetting;
        private RadioButtonSettingEditor _radioSetting;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public SettingOptionsForDebugDlg()
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
            this._boolSetting = new JetBrains.Omea.GUIControls.CheckBoxSettingEditor();
            this._okBtn = new System.Windows.Forms.Button();
            this._closeBtn = new System.Windows.Forms.Button();
            this._reCreateOptionsBtn = new System.Windows.Forms.Button();
            this._intSetting = new JetBrains.Omea.GUIControls.NumericUpDownSettingEditor();
            this._stringSetting = new JetBrains.Omea.GUIControls.StringSettingEditor();
            this._stringSettingDef = new JetBrains.Omea.GUIControls.StringSettingEditor();
            this._cmbIntSetting = new JetBrains.Omea.GUIControls.ComboBoxSettingEditor();
            this._radioSetting = new RadioButtonSettingEditor();
            this.SuspendLayout();
            // 
            // _boolSetting
            // 
            this._boolSetting.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._boolSetting.Location = new System.Drawing.Point(12, 8);
            this._boolSetting.Name = "_boolSetting";
            this._boolSetting.TabIndex = 0;
            this._boolSetting.Text = "bool setting";
            // 
            // _okBtn
            // 
            this._okBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._okBtn.Location = new System.Drawing.Point(272, 196);
            this._okBtn.Name = "_okBtn";
            this._okBtn.TabIndex = 1;
            this._okBtn.Text = "Apply";
            this._okBtn.Click += new System.EventHandler(this.OnOK);
            // 
            // _closeBtn
            // 
            this._closeBtn.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._closeBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._closeBtn.Location = new System.Drawing.Point(360, 196);
            this._closeBtn.Name = "_closeBtn";
            this._closeBtn.TabIndex = 2;
            this._closeBtn.Text = "Close";
            // 
            // _reCreateOptionsBtn
            // 
            this._reCreateOptionsBtn.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._reCreateOptionsBtn.Location = new System.Drawing.Point(16, 196);
            this._reCreateOptionsBtn.Name = "_reCreateOptionsBtn";
            this._reCreateOptionsBtn.Size = new System.Drawing.Size(132, 23);
            this._reCreateOptionsBtn.TabIndex = 3;
            this._reCreateOptionsBtn.Text = "Recreate Options";
            this._reCreateOptionsBtn.Click += new System.EventHandler(this._reCreateOptionsBtn_Click);
            // 
            // _intSetting
            // 
            this._intSetting.Location = new System.Drawing.Point(12, 40);
            this._intSetting.Maximum = 100;
            this._intSetting.Minimum = 0;
            this._intSetting.Name = "_intSetting";
            this._intSetting.TabIndex = 4;
            this._intSetting.Text = "0";
            this._intSetting.Value = 0;
            // 
            // _stringSetting
            // 
            this._stringSetting.Location = new System.Drawing.Point(16, 76);
            this._stringSetting.Name = "_stringSetting";
            this._stringSetting.TabIndex = 5;
            this._stringSetting.Text = "textBox1";
            // 
            // _stringSettingDef
            // 
            this._stringSettingDef.Location = new System.Drawing.Point(20, 112);
            this._stringSettingDef.Name = "_stringSettingDef";
            this._stringSettingDef.TabIndex = 0;
            this._stringSettingDef.Text = "";
            // 
            // _cmbIntSetting
            // 
            this._cmbIntSetting.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cmbIntSetting.Location = new System.Drawing.Point(196, 8);
            this._cmbIntSetting.Name = "_cmbIntSetting";
            this._cmbIntSetting.Size = new System.Drawing.Size(121, 21);
            this._cmbIntSetting.TabIndex = 6;
            // 
            // _radioSetting
            // 
            this._radioSetting.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._radioSetting.Location = new System.Drawing.Point(200, 48);
            this._radioSetting.Name = "_radioSetting";
            this._radioSetting.Size = new System.Drawing.Size(228, 132);
            this._radioSetting.TabIndex = 7;
            this._radioSetting.TabStop = false;
            // 
            // SettingOptionsForDebugDlg
            // 
            this.AcceptButton = this._okBtn;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this._closeBtn;
            this.ClientSize = new System.Drawing.Size(460, 246);
            this.Controls.Add(this._radioSetting);
            this.Controls.Add(this._cmbIntSetting);
            this.Controls.Add(this._stringSettingDef);
            this.Controls.Add(this._stringSetting);
            this.Controls.Add(this._intSetting);
            this.Controls.Add(this._reCreateOptionsBtn);
            this.Controls.Add(this._closeBtn);
            this.Controls.Add(this._okBtn);
            this.Controls.Add(this._boolSetting);
            this.Name = "SettingOptionsForDebugDlg";
            this.Text = "SettingOptionsForDebugDlg";
            this.Load += new System.EventHandler(this.OnLoad);
            this.ResumeLayout(false);

        }
		#endregion

        private void OnOK(object sender, System.EventArgs e)
        {
            SettingSaver.Save( Controls );
            RefreshSetting();
        }

        public static void RegisterResources()
        {
            Core.ResourceStore.ResourceTypes.Register( "DebugOption", string.Empty, ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal );
            IntDebugSetting = ResourceTypeHelper.UpdatePropTypeRegistration( "IntDbgSetting", PropDataType.Int, PropTypeFlags.Internal );
            BoolDebugSetting = ResourceTypeHelper.UpdatePropTypeRegistration( "BoolDbgSetting", PropDataType.Bool, PropTypeFlags.Internal );
            StringDebugSetting = ResourceTypeHelper.UpdatePropTypeRegistration( "StringDbgSetting", PropDataType.String, PropTypeFlags.Internal );
            StringDebugSettingDef = ResourceTypeHelper.UpdatePropTypeRegistration( "StringDbgSettingDef", PropDataType.String, PropTypeFlags.Internal );
            IntComboDebugSetting = ResourceTypeHelper.UpdatePropTypeRegistration( "IntComboDbgSetting", PropDataType.Int, PropTypeFlags.Internal );
            IntRadioDebugSetting = ResourceTypeHelper.UpdatePropTypeRegistration( "IntRadioDbgSetting", PropDataType.Int, PropTypeFlags.Internal );
            RecreateOptionsImpl();
        }

        private static void RecreateOptionsImpl()
        {
            IResourceList list = Core.ResourceStore.GetAllResources( "DebugOption" );
            list.DeleteAll();
            IResource option = Core.ResourceStore.NewResource( "DebugOption" );
            option.SetProp( BoolDebugSetting, true );
            option.SetProp( IntDebugSetting, 10 );
            option.SetProp( StringDebugSetting, "1" );
            option.SetProp( IntComboDebugSetting, -1 );
            option.SetProp( IntRadioDebugSetting, 7 );
            option = Core.ResourceStore.NewResource( "DebugOption" );
            option.SetProp( BoolDebugSetting, false );
            option.SetProp( IntDebugSetting, 20 );
            option.SetProp( StringDebugSetting, "2" );
            option.SetProp( IntComboDebugSetting, 1 );
            option.SetProp( IntRadioDebugSetting, 8 );
        }

        private static void RecreateOptions()
        {
            Core.ResourceAP.RunJob( new MethodInvoker( RecreateOptionsImpl ) );
        }

        private void RefreshSetting()
        {
            IResourceList options = Core.ResourceStore.GetAllResources( DebugOption);
            _boolSetting.SetSetting( SettingArray.FromResourceList( options, BoolDebugSetting, true ) );
            _intSetting.SetSetting( SettingArray.FromResourceList( options, IntDebugSetting, 100 ) );
            _stringSetting.SetSetting( SettingArray.FromResourceList( options, StringDebugSetting, "qwerty", false ) );
            _stringSettingDef.SetSetting( SettingArray.FromResourceList( options, StringDebugSettingDef, "Default String", false ) );
            _cmbIntSetting.SetData( new object[]{ 0, 1, 2 }, new object[]{ "00", "11", "22" } );
            _cmbIntSetting.SetSetting( SettingArray.FromResourceList( options, IntComboDebugSetting, 0 ) );
            _radioSetting.Text = "Radio Button Setting";
            _radioSetting.SetData( new object[]{ 7, 8, 9 }, new object[]{ "this is 77", "this is 88", "very long description for radio button" } );
            _radioSetting.SetSetting( SettingArray.FromResourceList( options, IntRadioDebugSetting, 99 ) );
        }

        private void OnLoad(object sender, System.EventArgs e)
        {
            RefreshSetting();
        }

        private void _reCreateOptionsBtn_Click(object sender, System.EventArgs e)
        {
            RecreateOptions();
            RefreshSetting();
        }
	}
}
