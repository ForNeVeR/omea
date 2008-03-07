/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls.CustomViews
{
	/// <summary>
	/// Summary description for EditFormattingRuleForm.
	/// </summary>
	public class EditFormattingRuleForm : ViewCommonDialogBase
	{
        private const int  ciFormHeight = 645;

        private CheckBox    checkBold;
        private CheckBox    checkItalic;
        private CheckBox    checkUnderline;
        private CheckBox    checkStrikeout;
        private Label       labelBackground;
        private Panel       panelBackground;
        private Button      buttonChangeBackground;
        private Label       labelForeground;
        private Panel       panelForeground;
        private Button      buttonChangeForeground;
        private Panel       panelSample;
        private Label       labelSample;
        private GroupBox    boxFormatting;
        private System.ComponentModel.IContainer components;

        #region Ctor
        public EditFormattingRuleForm( string ruleName )
               : base( "IsFormRuleLinked", true, false, false )
		{
            if( !String.IsNullOrEmpty( ruleName ) )
                throw new ArgumentNullException( "ruleName", "EditRuleForm -- Input rule name is NULL" );

            Initialize( ruleName );
            BaseResource = Core.FormattingRuleManager.FindRule( ruleName );
            InitializeBasePanels( BaseResource );
            RecreateFormatting( BaseResource );
        }

		public EditFormattingRuleForm( string name, string[] resTypes, IResource[][] conditions, IResource[] exceptions,
                                       bool isBold, bool isItalic, bool isUnderline, bool isStrikeout, string foreColor, string backColor )
               : base( "IsFormRuleLinked", true, false, false )
		{
            Initialize( name );
            InitializeBasePanels( resTypes, conditions, exceptions );
            RecreateFormatting( isBold, isItalic, isUnderline, isStrikeout, foreColor, backColor );
		}

		public  EditFormattingRuleForm()
                : base( "IsFormRuleLinked", true, false, false )
		{
            Initialize( null );
            InitializeBasePanels( null, new IResource[][] {}, new IResource[] {} );
            Text = "New Font and Color Rule";
		}

        private void  Initialize( string viewName )
        {
			InitializeComponent();
            if( !String.IsNullOrEmpty( viewName ))
                _editHeading.Text = InitialName = viewName;
            _referenceTopic = "reference\\new_font_and_color_rule.html";
        }
        #endregion Ctor

        #region RecreateFormatting
        private void RecreateFormatting( IResource rule )
        {
            RecreateFormatting( rule.HasProp( "IsBold" ), rule.HasProp( "IsItalic" ), 
                                rule.HasProp( "IsUnderline" ), rule.HasProp( "IsStrikeout" ),
                                rule.GetStringProp( "ForeColor" ), rule.GetStringProp( "BackColor" ) );
        }
        private void RecreateFormatting( bool isBold, bool isItalic, bool isUnderline,
                                         bool isStrikeout, string foreColor, string backColor )
        {
            checkBold.Checked = isBold;
            checkItalic.Checked = isItalic;
            checkUnderline.Checked = isUnderline;
            checkStrikeout.Checked = isStrikeout;

            //-----------------------------------------------------------------
            if( !String.IsNullOrEmpty( foreColor ) )
                panelForeground.BackColor = labelSample.ForeColor = Utils.ColorFromString( foreColor );
            else
                panelForeground.BackColor = labelSample.ForeColor = Color.Black;

            if( !String.IsNullOrEmpty( backColor ) )
                panelBackground.BackColor = panelSample.BackColor = Utils.ColorFromString( backColor );
            else
                panelBackground.BackColor = panelSample.BackColor = Color.White;

            //-----------------------------------------------------------------
            UpdateFont();
        }
        #endregion RecreateFormatting

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
					components.Dispose();
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private new void InitializeComponent()
		{
            base.InitializeComponent();

            this.components = new System.ComponentModel.Container();
            this.resTypeToolTip = new System.Windows.Forms.ToolTip(this.components);
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
            this.boxFormatting = new GroupBox();
            this.SuspendLayout();
            // 
            // boxFormatting 
            // 
            this.boxFormatting.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.boxFormatting.Location = new Point( 7, 467 );
            this.boxFormatting.Size = new Size( 385, 90 );
            this.boxFormatting.Text = "Formatting";
            this.boxFormatting.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            // 
            // checkBold
            // 
            this.checkBold.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkBold.Location = new Point( 8, 15 );
            this.checkBold.Size = new Size( 50, 20 );
            this.checkBold.Text = "Bold";
            this.checkBold.CheckStateChanged += new EventHandler(CheckStateChanged);
            this.checkBold.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left);
            // 
            // checkItalic
            // 
            this.checkItalic.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkItalic.Location = new Point( 60, 15 );
            this.checkItalic.Size = new Size( 50, 20 );
            this.checkItalic.Text = "Italic";
            this.checkItalic.CheckStateChanged += new EventHandler(CheckStateChanged);
            this.checkItalic.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left);
            // 
            // checkUnderline
            // 
            this.checkUnderline.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkUnderline.Location = new Point( 120, 15 );
            this.checkUnderline.Size = new Size( 75, 20 );
            this.checkUnderline.Text = "Underline";
            this.checkUnderline.CheckedChanged += new EventHandler(CheckStateChanged);
            this.checkUnderline.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left);
            // 
            // checkStrikeout
            // 
            this.checkStrikeout.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkStrikeout.Location = new Point( 200, 15 );
            this.checkStrikeout.Size = new Size( 75, 20 );
            this.checkStrikeout.Text = "Strikeout";
            this.checkStrikeout.CheckStateChanged += new EventHandler(CheckStateChanged);
            this.checkStrikeout.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left);
            // 
            // labelBackground
            // 
            this.labelBackground.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelBackground.Location = new Point( 8, 40 );
            this.labelBackground.Size = new Size( 90, 16 );
            this.labelBackground.Text = "Background Color:";
            this.labelBackground.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left);
            // 
            // panelBackground
            // 
            this.panelBackground.AutoScroll = false;
            this.panelBackground.BackColor = System.Drawing.SystemColors.Window;
            this.panelBackground.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelBackground.Location = new System.Drawing.Point(102, 38);
            this.panelBackground.Name = "panelBackground";
            this.panelBackground.Size = new System.Drawing.Size(70, 20);
            this.panelBackground.TabIndex = 4;
            this.panelBackground.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left);
            // 
            // buttonChangeBackground
            // 
            this.buttonChangeBackground.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonChangeBackground.Location = new System.Drawing.Point(180, 38);
            this.buttonChangeBackground.Size = new System.Drawing.Size(22, 20);
            this.buttonChangeBackground.Name = "buttonChangeBackground";
            this.buttonChangeBackground.TabIndex = 6;
            this.buttonChangeBackground.Text = "...";
            this.buttonChangeBackground.Click += new EventHandler(buttonChangeBackground_Click);
            this.buttonChangeBackground.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left);
            // 
            // labelForeground
            // 
            this.labelForeground.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelForeground.Location = new Point( 8, 65 );
            this.labelForeground.Size = new Size( 90, 16 );
            this.labelForeground.Text = "Foreground Color:";
            this.labelForeground.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left);
            // 
            // panelForeground
            // 
            this.panelForeground.AutoScroll = false;
            this.panelForeground.BackColor = System.Drawing.Color.Black;
            this.panelForeground.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelForeground.Location = new System.Drawing.Point(102, 63);
            this.panelForeground.Name = "panelBackground";
            this.panelForeground.Size = new System.Drawing.Size(70, 20);
            this.panelForeground.TabIndex = 4;
            this.panelForeground.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left);
            // 
            // buttonChangeForeground
            // 
            this.buttonChangeForeground.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonChangeForeground.Location = new System.Drawing.Point(180, 63);
            this.buttonChangeForeground.Size = new System.Drawing.Size(22, 20);
            this.buttonChangeForeground.Name = "buttonChangeForeground";
            this.buttonChangeForeground.TabIndex = 6;
            this.buttonChangeForeground.Text = "...";
            this.buttonChangeForeground.Click += new EventHandler(buttonChangeForeground_Click);
            this.buttonChangeForeground.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left);
            // 
            // panelSample
            // 
            this.panelSample.AutoScroll = false;
            this.panelSample.BackColor = System.Drawing.Color.White;
            this.panelSample.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelSample.Location = new System.Drawing.Point(210, 40);
            this.panelSample.Name = "panelSample";
            this.panelSample.Size = new System.Drawing.Size(83, 45);
            this.panelSample.TabIndex = 4;
            this.panelSample.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left);
            // 
            // labelSample
            // 
            this.labelSample.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelSample.Location = new Point( 9, 15 );
            this.labelSample.Size = new Size( 70, 16 );
            this.labelSample.Text = "AaBbCcDdEe";
            // 
            // EditFormattingRuleForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(398, ciFormHeight);
            this.MinimumSize = new Size( 320, 350 );
            this.Name = "EditFormattingRuleForm";
            this.Text = "Edit Font and Color Rule";

            panelSample.Controls.Add( labelSample );

            boxFormatting.Controls.Add( checkBold );
            boxFormatting.Controls.Add( checkItalic );
            boxFormatting.Controls.Add( checkUnderline );
            boxFormatting.Controls.Add( checkStrikeout );
            boxFormatting.Controls.Add( labelBackground );
            boxFormatting.Controls.Add( panelBackground );
            boxFormatting.Controls.Add( buttonChangeBackground );
            boxFormatting.Controls.Add( labelForeground );
            boxFormatting.Controls.Add( panelForeground );
            boxFormatting.Controls.Add( buttonChangeForeground );
            boxFormatting.Controls.Add( panelSample );

            this.Controls.Add(this.boxFormatting);

            base._lblHeading.Text = "Rule &name:";
            base.okButton.Click += new System.EventHandler(this.okButton_Click);

            boxFormatting.Location = new Point( boxFormatting.Left, ciFormHeight - 160 );
            PlaceBottomControls( ciFormHeight );

            ResumeLayout(false);
        }
		#endregion

        #region OK
        private void okButton_Click(object sender, EventArgs e)
        {
            Debug.Assert( okButton.Enabled );

            okButton.Enabled = false;
            if( areNamesDiffer( _editHeading.Text, InitialName ) &&
               ( Core.FormattingRuleManager.FindRule( _editHeading.Text ) != null ))
            {
                DialogResult result = MessageBox.Show( this, "A Font and Color rule with such name already exists. Do you want to overwrite it?", 
                                                       "Names collision", MessageBoxButtons.YesNo );
                if( result == DialogResult.No )
                    return;
                Core.FormattingRuleManager.UnregisterRule( _editHeading.Text );
            }

            //-------------------------------------------------------------
            IResource[] conditions = ConvertTemplates2Conditions( panelConditions.Controls );
            IResource[] exceptions = ConvertTemplates2Conditions( panelExceptions.Controls );

            string[] formTypes = ReformatTypes( CurrentResTypeDeep );
            if( BaseResource == null )
            {
                BaseResource = Core.FormattingRuleManager.RegisterRule( _editHeading.Text, formTypes, conditions, exceptions,
                                            checkBold.Checked, checkItalic.Checked, checkUnderline.Checked, checkStrikeout.Checked,
                                            labelSample.ForeColor.Name, panelSample.BackColor.Name );
            }
            else
            {
                Core.FormattingRuleManager.ReregisterRule( BaseResource, _editHeading.Text, formTypes, conditions, exceptions,
                                            checkBold.Checked, checkItalic.Checked, checkUnderline.Checked, checkStrikeout.Checked,
                                            labelSample.ForeColor.Name, panelSample.BackColor.Name );
            }
            FreeConditionLists( panelConditions.Controls );
            FreeConditionLists( panelExceptions.Controls );
            DialogResult = DialogResult.OK;
        }
        #endregion OK

        #region Common Event Handlers
        private void  buttonChangeBackground_Click(object sender, EventArgs e)
        {
            ColorDialog dlg = new ColorDialog();
            if( dlg.ShowDialog( this ) == DialogResult.OK )
            {
                panelSample.BackColor = panelBackground.BackColor = dlg.Color;
            }
        }

        private void buttonChangeForeground_Click(object sender, EventArgs e)
        {
            ColorDialog dlg = new ColorDialog();
            if( dlg.ShowDialog( this ) == DialogResult.OK )
            {
                labelSample.ForeColor = panelForeground.BackColor = dlg.Color;
            }
        }

        private void CheckStateChanged(object sender, EventArgs e)
        {
            UpdateFont();
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
        #endregion Common Event Handlers
    }
}
