/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;

namespace GUIControls.CustomViews
{
	/// <summary>
	/// Summary description for FormattingForm.
	/// </summary>
	public class FormattingForm : Form
	{
        #region Attributes
        private CheckBox   checkBold;
        private CheckBox   checkItalic;
        private CheckBox   checkUnderline;
        private CheckBox   checkStrikeout;
        private Label      labelBackground;
        private Panel      panelBackground;
        private Button     buttonChangeBackground;
        private Label      labelForeground;
        private Panel      panelForeground;
        private Button     buttonChangeForeground;
        private Panel      panelSample;
        private Label      labelSample;
        private Button     _btnClear;

        private GroupBox   _grpScope;
        private GroupBox   _grpFormatting;
        private RadioButton _radioSubthread;
        private RadioButton _radioThread;
        private RadioButton _radioCurrentSelection;

        private CheckBox   checkRememberLast;

        private Button      btnOK;
        private Button      btnCancel;
        private Button      btnHelp;
		private System.ComponentModel.Container components = null;

        private readonly IResource  _clickedRes;
        private IResource       _headFormattingRes;
        private bool            _clearFormatting = false;
        #endregion Attributes

		public FormattingForm( IResource head )
		{
            _clickedRes = head;
			InitializeComponent();

            checkRememberLast.Checked = Core.SettingStore.ReadBool( "Rules", "RestoreLastFormatting", false );
            RestoreCurrentSelection( head );
            ConfigureApplytoGroup( head );
		}

        private void  RestoreCurrentSelection( IResource head )
        {
            IResource rule = null;

            while( head != null )
            {
                string  ruleName = ConstructRuleName( head );
                rule = Core.FormattingRuleManager.FindRule( ruleName );
                if( rule != null )
                {
                    _headFormattingRes = head;
                    break;
                }
                head = head.GetLinkProp( Core.Props.Reply );
            }

            if( rule != null )
            {
                checkBold.Checked = rule.HasProp( "IsBold" );
                checkItalic.Checked = rule.HasProp( "IsItalic" );
                checkUnderline.Checked = rule.HasProp( "IsUnderline" );
                checkStrikeout.Checked = rule.HasProp( "IsStrikeout" );

                string foreColor = rule.GetStringProp( "ForeColor" );
                string backColor = rule.GetStringProp( "BackColor" );
                if( !String.IsNullOrEmpty( foreColor ) )
                    panelForeground.BackColor = labelSample.ForeColor = Utils.ColorFromString( foreColor );
                else
                    panelForeground.BackColor = labelSample.ForeColor = Color.Black;

                if( !String.IsNullOrEmpty( backColor ) )
                    panelBackground.BackColor = panelSample.BackColor = Utils.ColorFromString( backColor );
                else
                    panelBackground.BackColor = panelSample.BackColor = Color.White;
            }
            else
            if( checkRememberLast.Checked )
            {
                checkBold.Checked = Core.SettingStore.ReadBool( "Rules", "IsBold", false );
                checkItalic.Checked = Core.SettingStore.ReadBool( "Rules", "IsItalic", false );
                checkUnderline.Checked = Core.SettingStore.ReadBool( "Rules", "IsUnderline", false );
                checkStrikeout.Checked = Core.SettingStore.ReadBool( "Rules", "IsStrikeout", false );
                string foreColor = Core.SettingStore.ReadString( "Rules", "ForeColor", Color.Black.ToString() );
                string backColor = Core.SettingStore.ReadString( "Rules", "BackColor", Color.White.ToString() );

                if( !String.IsNullOrEmpty( foreColor ) )
                    panelForeground.BackColor = labelSample.ForeColor = Utils.ColorFromString( foreColor );
                else
                    panelForeground.BackColor = labelSample.ForeColor = Color.Black;

                if( !String.IsNullOrEmpty( backColor ) )
                    panelBackground.BackColor = panelSample.BackColor = Utils.ColorFromString( backColor );
                else
                    panelBackground.BackColor = panelSample.BackColor = Color.White;
            }
        }

        private void  ConfigureApplytoGroup( IResource head )
        {
            int  linksCount = head.GetLinksFrom( null, Core.Props.Reply ).Count;
            if( linksCount == 0 )
            {
                _grpScope.Visible = false;
                Height -= _grpScope.Height + 5;
                checkRememberLast.Top -= _grpScope.Height + 5;
            }
            else
            if( _headFormattingRes == null || _headFormattingRes.Id == _clickedRes.Id )
            {
                _radioCurrentSelection.Visible = false;
                _grpScope.Height -= _radioCurrentSelection.Height;
                this.Height -= _radioCurrentSelection.Height;
                checkRememberLast.Top -= _radioCurrentSelection.Height;
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
            this.checkBold = new System.Windows.Forms.CheckBox();
            this.checkItalic = new System.Windows.Forms.CheckBox();
            this.checkUnderline = new System.Windows.Forms.CheckBox();
            this.checkStrikeout = new System.Windows.Forms.CheckBox();
            this.labelBackground = new System.Windows.Forms.Label();
            this.labelForeground = new System.Windows.Forms.Label();
            this.panelBackground = new System.Windows.Forms.Panel();
            this.panelForeground = new System.Windows.Forms.Panel();
            this.buttonChangeBackground = new System.Windows.Forms.Button();
            this.buttonChangeForeground = new System.Windows.Forms.Button();
            this.panelSample = new System.Windows.Forms.Panel();
            this.labelSample = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnHelp = new System.Windows.Forms.Button();
            this._grpScope = new System.Windows.Forms.GroupBox();
            this._radioThread = new System.Windows.Forms.RadioButton();
            this._radioSubthread = new System.Windows.Forms.RadioButton();
            this._grpFormatting = new System.Windows.Forms.GroupBox();
            this._btnClear = new System.Windows.Forms.Button();
            this._radioCurrentSelection = new System.Windows.Forms.RadioButton();
            this.checkRememberLast = new System.Windows.Forms.CheckBox();
            this.panelSample.SuspendLayout();
            this._grpScope.SuspendLayout();
            this._grpFormatting.SuspendLayout();
            this.SuspendLayout();
            // 
            // checkBold
            // 
            this.checkBold.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkBold.Location = new System.Drawing.Point(12, 20);
            this.checkBold.Name = "checkBold";
            this.checkBold.Size = new System.Drawing.Size(50, 20);
            this.checkBold.TabIndex = 0;
            this.checkBold.Text = "Bold";
            this.checkBold.CheckStateChanged += new System.EventHandler(this.CheckStateChanged);
            // 
            // checkItalic
            // 
            this.checkItalic.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkItalic.Location = new System.Drawing.Point(64, 20);
            this.checkItalic.Name = "checkItalic";
            this.checkItalic.Size = new System.Drawing.Size(50, 20);
            this.checkItalic.TabIndex = 1;
            this.checkItalic.Text = "Italic";
            this.checkItalic.CheckStateChanged += new System.EventHandler(this.CheckStateChanged);
            // 
            // checkUnderline
            // 
            this.checkUnderline.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkUnderline.Location = new System.Drawing.Point(124, 20);
            this.checkUnderline.Name = "checkUnderline";
            this.checkUnderline.Size = new System.Drawing.Size(75, 20);
            this.checkUnderline.TabIndex = 2;
            this.checkUnderline.Text = "Underline";
            this.checkUnderline.CheckedChanged += new System.EventHandler(this.CheckStateChanged);
            // 
            // checkStrikeout
            // 
            this.checkStrikeout.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkStrikeout.Location = new System.Drawing.Point(204, 20);
            this.checkStrikeout.Name = "checkStrikeout";
            this.checkStrikeout.Size = new System.Drawing.Size(75, 20);
            this.checkStrikeout.TabIndex = 3;
            this.checkStrikeout.Text = "Strikeout";
            this.checkStrikeout.CheckStateChanged += new System.EventHandler(this.CheckStateChanged);
            // 
            // labelBackground
            // 
            this.labelBackground.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelBackground.Location = new System.Drawing.Point(12, 44);
            this.labelBackground.Name = "labelBackground";
            this.labelBackground.Size = new System.Drawing.Size(90, 16);
            this.labelBackground.TabIndex = 4;
            this.labelBackground.Text = "Background Color:";
            // 
            // labelForeground
            // 
            this.labelForeground.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelForeground.Location = new System.Drawing.Point(12, 72);
            this.labelForeground.Name = "labelForeground";
            this.labelForeground.Size = new System.Drawing.Size(90, 16);
            this.labelForeground.TabIndex = 7;
            this.labelForeground.Text = "Foreground Color:";
            // 
            // panelBackground
            // 
            this.panelBackground.BackColor = System.Drawing.SystemColors.Window;
            this.panelBackground.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelBackground.Location = new System.Drawing.Point(108, 44);
            this.panelBackground.Name = "panelBackground";
            this.panelBackground.Size = new System.Drawing.Size(70, 20);
            this.panelBackground.TabIndex = 4;
            // 
            // panelForeground
            // 
            this.panelForeground.BackColor = System.Drawing.Color.Black;
            this.panelForeground.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelForeground.Location = new System.Drawing.Point(108, 72);
            this.panelForeground.Name = "panelForeground";
            this.panelForeground.Size = new System.Drawing.Size(70, 20);
            this.panelForeground.TabIndex = 7;
            // 
            // buttonChangeBackground
            // 
            this.buttonChangeBackground.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonChangeBackground.Location = new System.Drawing.Point(184, 44);
            this.buttonChangeBackground.Name = "buttonChangeBackground";
            this.buttonChangeBackground.Size = new System.Drawing.Size(22, 20);
            this.buttonChangeBackground.TabIndex = 5;
            this.buttonChangeBackground.Text = "...";
            this.buttonChangeBackground.Click += new System.EventHandler(this.buttonChangeBackground_Click);
            // 
            // buttonChangeForeground
            // 
            this.buttonChangeForeground.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonChangeForeground.Location = new System.Drawing.Point(184, 68);
            this.buttonChangeForeground.Name = "buttonChangeForeground";
            this.buttonChangeForeground.Size = new System.Drawing.Size(22, 20);
            this.buttonChangeForeground.TabIndex = 8;
            this.buttonChangeForeground.Text = "...";
            this.buttonChangeForeground.Click += new System.EventHandler(this.buttonChangeForeground_Click);
            // 
            // panelSample
            // 
            this.panelSample.BackColor = System.Drawing.Color.White;
            this.panelSample.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelSample.Controls.Add(this.labelSample);
            this.panelSample.Location = new System.Drawing.Point(216, 44);
            this.panelSample.Name = "panelSample";
            this.panelSample.Size = new System.Drawing.Size(80, 45);
            this.panelSample.TabIndex = 4;
            // 
            // labelSample
            // 
            this.labelSample.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelSample.Location = new System.Drawing.Point(9, 15);
            this.labelSample.Name = "labelSample";
            this.labelSample.Size = new System.Drawing.Size(70, 16);
            this.labelSample.TabIndex = 0;
            this.labelSample.Text = "AaBbCcDdEe";
            // 
            // _btnClear
            // 
            this._btnClear.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnClear.Location = new System.Drawing.Point(220, 100);
            this._btnClear.Name = "_btnClear";
            this._btnClear.TabIndex = 9;
            this._btnClear.Text = "Default";
            this._btnClear.Click += new System.EventHandler(this._btnClear_Click);
            // 
            // _grpFormatting
            // 
            this._grpFormatting.Controls.Add(this._btnClear);
            this._grpFormatting.Controls.Add(this.labelBackground);
            this._grpFormatting.Controls.Add(this.panelForeground);
            this._grpFormatting.Controls.Add(this.checkBold);
            this._grpFormatting.Controls.Add(this.labelForeground);
            this._grpFormatting.Controls.Add(this.panelBackground);
            this._grpFormatting.Controls.Add(this.checkItalic);
            this._grpFormatting.Controls.Add(this.checkUnderline);
            this._grpFormatting.Controls.Add(this.buttonChangeBackground);
            this._grpFormatting.Controls.Add(this.checkStrikeout);
            this._grpFormatting.Controls.Add(this.buttonChangeForeground);
            this._grpFormatting.Controls.Add(this.panelSample);
            this._grpFormatting.Location = new System.Drawing.Point(8, 8);
            this._grpFormatting.Name = "_grpFormatting";
            this._grpFormatting.Size = new System.Drawing.Size(308, 136);
            this._grpFormatting.TabIndex = 26;
            this._grpFormatting.TabStop = false;
            this._grpFormatting.Text = "Formatting";
            // 
            // _grpScope
            // 
            this._grpScope.Controls.Add(this._radioCurrentSelection);
            this._grpScope.Controls.Add(this._radioThread);
            this._grpScope.Controls.Add(this._radioSubthread);
            this._grpScope.Location = new System.Drawing.Point(8, 148);
            this._grpScope.Name = "_grpScope";
            this._grpScope.Size = new System.Drawing.Size(308, 80);
            this._grpScope.TabIndex = 25;
            this._grpScope.TabStop = false;
            this._grpScope.Text = "Apply to";
            // 
            // _radioSubthread
            // 
            this._radioSubthread.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._radioSubthread.Location = new System.Drawing.Point(8, 16);
            this._radioSubthread.Name = "_radioSubthread";
            this._radioSubthread.Size = new System.Drawing.Size(136, 20);
            this._radioSubthread.TabIndex = 10;
            this._radioSubthread.TabStop = true;
            this._radioSubthread.Text = "Selected subthread";
            this._radioSubthread.Checked = true;
            // 
            // _radioCurrentSelection
            // 
            this._radioCurrentSelection.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._radioCurrentSelection.Location = new System.Drawing.Point(8, 36);
            this._radioCurrentSelection.Name = "_radioCurrentSelection";
            this._radioCurrentSelection.Size = new System.Drawing.Size(144, 20);
            this._radioCurrentSelection.TabIndex = 13;
            this._radioCurrentSelection.Text = "Current selection";
            // 
            // _radioThread
            // 
            this._radioThread.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._radioThread.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._radioThread.Location = new System.Drawing.Point(8, 56);
            this._radioThread.Name = "_radioThread";
            this._radioThread.Size = new System.Drawing.Size(112, 20);
            this._radioThread.TabIndex = 12;
            this._radioThread.TabStop = true;
            this._radioThread.Text = "Whole thread";
            // 
            // checkRememberLast
            // 
            this.checkRememberLast.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkRememberLast.Location = new System.Drawing.Point(8, 235);
            this.checkRememberLast.Name = "checkRememberLast";
            this.checkRememberLast.Size = new System.Drawing.Size(150, 20);
            this.checkRememberLast.TabIndex = 18;
            this.checkRememberLast.Text = "&Remember last setting";
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOK.Location = new System.Drawing.Point(82, 260);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(72, 24);
            this.btnOK.TabIndex = 20;
            this.btnOK.Text = "OK";
            this.btnOK.Click += new System.EventHandler(this.okButton_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(162, 260);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(72, 24);
            this.btnCancel.TabIndex = 22;
            this.btnCancel.Text = "Cancel";
            // 
            // btnHelp
            // 
            this.btnHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnHelp.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnHelp.Location = new System.Drawing.Point(242, 260);
            this.btnHelp.Name = "btnHelp";
            this.btnHelp.Size = new System.Drawing.Size(72, 24);
            this.btnHelp.TabIndex = 24;
            this.btnHelp.Text = "Help";
            this.btnHelp.Click += new System.EventHandler(this.helpButton_Click);
            // 
            // FormattingForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(324, 296);
            this.Controls.Add(this._grpFormatting);
            this.Controls.Add(this._grpScope);
            this.Controls.Add(this.checkRememberLast);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnHelp);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.KeyPreview = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormattingForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Edit Font and Color Attributes";
            this.panelSample.ResumeLayout(false);
            this._grpScope.ResumeLayout(false);
            this._grpFormatting.ResumeLayout(false);
            this.ResumeLayout(false);
        }
		#endregion

        #region Common Event Handlers
        private void _btnClear_Click(object sender, EventArgs e)
        {
            checkBold.Checked = checkItalic.Checked =
            checkUnderline.Checked = checkStrikeout.Checked = false;
            panelSample.BackColor = panelBackground.BackColor = SystemColors.Window;
            labelSample.ForeColor = panelForeground.BackColor = SystemColors.WindowText;
            _clearFormatting = true;
        }

        private void  buttonChangeBackground_Click(object sender, EventArgs e)
        {
            ColorDialog dlg = new ColorDialog();
            if( dlg.ShowDialog( this ) == DialogResult.OK )
            {
                panelSample.BackColor = panelBackground.BackColor = dlg.Color;
                _clearFormatting = false;
            }
        }

        private void buttonChangeForeground_Click(object sender, EventArgs e)
        {
            ColorDialog dlg = new ColorDialog();
            if( dlg.ShowDialog( this ) == DialogResult.OK )
            {
                labelSample.ForeColor = panelForeground.BackColor = dlg.Color;
                _clearFormatting = false;
            }
        }

        private void CheckStateChanged(object sender, EventArgs e)
        {
            UpdateFont();
             _clearFormatting = false;
        }

        private void UpdateFont()
        {
            FontStyle fs = FontStyle.Regular;
            if( checkBold.Checked )
                fs |= FontStyle.Bold;
            if( checkItalic.Checked )
                fs |= FontStyle.Italic;
            if( checkUnderline.Checked )
                fs |= FontStyle.Underline;
            if( checkStrikeout.Checked )
                fs |= FontStyle.Strikeout;

            labelSample.Font = new Font( "Tahoma", 8.25f, fs );
        }

        private void helpButton_Click(object sender, EventArgs e)
        {
            Help.ShowHelp( this, Core.UIManager.HelpFileName, "reference\\new_font_and_color_rule.html" );
        }
        #endregion Common Event Handlers

        #region OK
        private void okButton_Click(object sender, EventArgs e)
        {
            btnOK.Enabled = false;

            IResource root = _clickedRes;
            if( _radioThread.Checked )
                root = GetRoot( _clickedRes );
            else
            if( _radioCurrentSelection.Checked )
                root = _headFormattingRes;

            string  ruleName = ConstructRuleName( root );
            if( _clearFormatting )
            {
                if( Core.FormattingRuleManager.IsRuleRegistered( ruleName ) )
                    Core.FormattingRuleManager.UnregisterRule( ruleName );
            }
            else
            {
                IResource[] conditions = new IResource[ 1 ];
                conditions[ 0 ] = FilterConvertors.InstantiateTemplate( Core.FilterManager.Std.MessageIsInThreadOfX, root.ToResourceList(), null );

                Core.FormattingRuleManager.RegisterRule( ruleName, null, conditions, null,
                                                        checkBold.Checked, checkItalic.Checked,
                                                        checkUnderline.Checked, checkStrikeout.Checked,
                                                        labelSample.ForeColor.Name, panelSample.BackColor.Name );
            }
            DeleteRulesUnderTheRoot( root );

            Core.SettingStore.WriteBool( "Rules", "RestoreLastFormatting", checkRememberLast.Checked );
            Core.SettingStore.WriteBool( "Rules", "IsBold", checkBold.Checked );
            Core.SettingStore.WriteBool( "Rules", "IsItalic", checkItalic.Checked );
            Core.SettingStore.WriteBool( "Rules", "IsUnderline", checkUnderline.Checked );
            Core.SettingStore.WriteBool( "Rules", "IsStrikeout", checkStrikeout.Checked );
            Core.SettingStore.WriteString( "Rules", "ForeColor", labelSample.ForeColor.Name );
            Core.SettingStore.WriteString( "Rules", "BackColor", panelSample.BackColor.Name );

            DialogResult = DialogResult.OK;
        }
        #endregion OK

        private static IResource  GetRoot( IResource current )
        {
            IResource root;
            do
            {
                root = current;
                current = current.GetLinkProp( Core.Props.Reply );
            }
            while( current != null );

            return root;
        }

        private static void  DeleteRulesUnderTheRoot( IResource root )
        {
            IntHashSet    hashDone = new IntHashSet();
            DeleteRulesUnderTheRoot( root, hashDone );
        }
        private static void  DeleteRulesUnderTheRoot( IResource root, IntHashSet hashDone )
        {
            if( !hashDone.Contains( root.Id ))
            {
                hashDone.Add( root.Id );

                IResourceList children = root.GetLinksTo( null, Core.Props.Reply );
                foreach( IResource res in children )
                {
                    string ruleName = ConstructRuleName( res );
                    if( Core.FormattingRuleManager.IsRuleRegistered( ruleName ) )
                        Core.FormattingRuleManager.UnregisterRule( ruleName );
                }

                foreach( IResource res in children )
                {
                    DeleteRulesUnderTheRoot( res, hashDone );
                }
            }
        }

        private static string ConstructRuleName( IResource res )
        {
            return "Thread formatting-" + res.Id;
        }
	}
}
