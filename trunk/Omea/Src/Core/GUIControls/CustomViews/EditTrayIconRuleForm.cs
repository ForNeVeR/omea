/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using GUIControls.CustomViews;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls.CustomViews
{
	/// <summary>
	/// Summary description for EditFormattingRuleForm.
	/// </summary>
	public class EditTrayIconRuleForm : ViewCommonDialogBase
	{
        private const int  ciFormHeight = 620;

        private GroupBox    boxIcon;
        private Panel       panelIcon;
        private Label       labelLoadFrom;
        private Button      buttonBrowseFile, buttonBrowseInternals;
        private Icon        iconPicture = null;

        private System.ComponentModel.IContainer components;

        #region Ctor
		public EditTrayIconRuleForm( string ruleName )
               : base( "IsTrayRuleLinked", true, false, false )
        {
            #region Preconditions
            if ( String.IsNullOrEmpty( ruleName ) )
                throw new ArgumentNullException( "ruleName", "EditRuleForm -- Input rule name is NULL" );
            #endregion Preconditions

            BaseResource = Core.TrayIconManager.FindRule( ruleName );

            Initialize( ruleName, BaseResource );
            InitializeBasePanels( BaseResource );
        }

		public EditTrayIconRuleForm( string name, string[] types,
                                     IResource[][] conds, IResource[] expts, string iconFileName )
               : base( "IsTrayRuleLinked", true, false, false )
		{
            Initialize( name, null );
            InitializeBasePanels( types, conds, expts );
		}

		public EditTrayIconRuleForm() : base( "IsTrayRuleLinked", true, false, false )
		{
            Initialize( null, null );
            InitializeBasePanels( null, new IResource[][] {}, new IResource[] {} );
            Text = "New Tray Icon Rule";
		}

        private void  Initialize( string name, IResource rule )
        {
			InitializeComponent();

		    if( !String.IsNullOrEmpty( name ) )
                _editHeading.Text = InitialName = name;
            _referenceTopic = "reference\\tray_icon_rule.htm";
            _externalChecker = CheckValidActions;

            if( rule != null )
            {
                Stream strm = rule.GetBlobProp( "IconBlob" );
                if( strm != null )
                {
                    Trace.WriteLine( "EditTrayIconRuleForm -- length of the stream is " + strm.Length );
                    iconPicture = new Icon( strm );
                    DrawIcon( iconPicture );
                }
            }

            Text = "Edit Tray Icon Rule";
        }
        #endregion Ctor

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
            this.resTypeToolTip = new ToolTip(this.components);
            boxIcon = new GroupBox();
            panelIcon = new Panel();
            labelLoadFrom = new Label();
		    buttonBrowseFile = new Button();
		    buttonBrowseInternals = new Button();
            this.SuspendLayout();
            // 
            // boxIcon
            // 
            this.boxIcon.Location = new System.Drawing.Point(7, 495);
            this.boxIcon.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            this.boxIcon.Name = "boxIcon";
            this.boxIcon.Size = new System.Drawing.Size(384, 56);
            this.boxIcon.FlatStyle = FlatStyle.System;
            this.boxIcon.TabStop = false;
            this.boxIcon.Text = "Icon";
            // 
            // panelIcon
            // 
            this.panelIcon.Location = new Point( 9, 15 );
            this.panelIcon.Size = new Size( 36, 36 );
            this.panelIcon.Anchor = (AnchorStyles)(AnchorStyles.Top | AnchorStyles.Left);
            this.panelIcon.AutoScroll = false;
            this.panelIcon.BackColor = System.Drawing.SystemColors.Window;
            this.panelIcon.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelIcon.TabStop = false;
            this.panelIcon.Paint += new PaintEventHandler(panelIcon_Paint);
            // 
            // labelLoadFrom
            // 
            this.labelLoadFrom.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.labelLoadFrom.Location = new System.Drawing.Point(60, 26);
            this.labelLoadFrom.Size = new System.Drawing.Size(58, 16);
            this.labelLoadFrom.Name = "labelLoadFrom";
            this.labelLoadFrom.TabStop = false;
            this.labelLoadFrom.Text = "Load from:";
            // 
            // buttonBrowseInternals
            // 
            this.buttonBrowseInternals.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonBrowseInternals.Location = new System.Drawing.Point(125, 21);
            this.buttonBrowseInternals.Size = new System.Drawing.Size(72, 24);
            this.buttonBrowseInternals.Name = "buttonBrowseInternals";
            this.buttonBrowseInternals.TabIndex = 40;
            this.buttonBrowseInternals.Text = "Omea...";
            this.buttonBrowseInternals.Click += new EventHandler(buttonBrowseInternals_Click);
            this.buttonBrowseInternals.Anchor = (AnchorStyles)(AnchorStyles.Top | AnchorStyles.Left);
            // 
            // buttonBrowseFile
            // 
            this.buttonBrowseFile.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonBrowseFile.Location = new System.Drawing.Point(205, 21);
            this.buttonBrowseFile.Size = new System.Drawing.Size(72, 24);
            this.buttonBrowseFile.Name = "buttonBrowseFile";
            this.buttonBrowseFile.TabIndex = 40;
            this.buttonBrowseFile.Text = "File...";
            this.buttonBrowseFile.Click += new EventHandler(buttonBrowseIcon_Click);
            this.buttonBrowseFile.Anchor = (AnchorStyles)(AnchorStyles.Top | AnchorStyles.Left);
            // 
            // EditTrayIconRuleForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(398, ciFormHeight);
            this.MinimumSize = new Size( 315, 300 );
            this.Name = "EditTrayIconRuleForm";
            this.Text = "New Tray Icon Rule";

            this.Controls.Add(this.boxIcon);
            boxIcon.Controls.Add(this.panelIcon);
            boxIcon.Controls.Add(this.labelLoadFrom);
            boxIcon.Controls.Add(this.buttonBrowseInternals);
            boxIcon.Controls.Add(this.buttonBrowseFile);

            base._lblHeading.Text = "Rule &name:";
            base.okButton.Click += new System.EventHandler(this.okButton_Click);

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
                Core.TrayIconManager.IsTrayIconRuleRegistered( _editHeading.Text ))
            {
                DialogResult result = MessageBox.Show( this, "A tray icon rule with such name already exists. Do you want to overwrite it?", 
                                                       "Names Collision", MessageBoxButtons.YesNo );
                if( result == DialogResult.No )
                    return;
                else
                    Core.TrayIconManager.UnregisterTrayIconRule( _editHeading.Text );
            }

            //-------------------------------------------------------------
            IResource[] conditions = ConvertTemplates2Conditions( panelConditions.Controls );
            IResource[] exceptions = ConvertTemplates2Conditions( panelExceptions.Controls );

            string[] formTypes = ReformatTypes( CurrentResTypeDeep );
            if( BaseResource == null )
                BaseResource = Core.TrayIconManager.RegisterTrayIconRule( _editHeading.Text, formTypes, conditions, exceptions, iconPicture );
            else
                Core.TrayIconManager.ReregisterTrayIconRule( BaseResource, _editHeading.Text, formTypes, conditions, exceptions, iconPicture );
            FreeConditionLists( panelConditions.Controls );
            FreeConditionLists( panelExceptions.Controls );
            DialogResult = DialogResult.OK;
        }
        #endregion OK

        #region Event Handlers
        private void buttonBrowseInternals_Click(object sender, EventArgs e)
        {
            LoadFromAssembly();
            CheckFormConsistency();
        }

        private void buttonBrowseIcon_Click(object sender, EventArgs e)
        {
            LoadFromFile();
            CheckFormConsistency();
        }

        private void panelIcon_Paint(object sender, PaintEventArgs e)
        {
            if( iconPicture != null )
                DrawIcon( iconPicture );
        }
        #endregion Event Handlers

        #region Impl
        private void  DrawIcon( Icon icon )
        {
            int shift = (panelIcon.Width - icon.Width) / 2 - 1;
            Graphics.FromHwnd( panelIcon.Handle ).DrawIcon( icon, shift, shift );
        }

        private void  LoadFromFile()
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.CheckFileExists = true;
            dlg.DefaultExt = "ico";
            dlg.Multiselect = false;
            dlg.Filter = "Icon files (*.ico)|*.ico|All files|*.*";
            if( dlg.ShowDialog( this ) == DialogResult.OK )
            {
                try
                {
                    iconPicture = new Icon( dlg.FileName );
                    DrawIcon( iconPicture );
                }
                catch( Exception )
                {
                    MessageBox.Show( this, "File does not contain a valid Icon resource", "Error Loading Icon", MessageBoxButtons.OK, MessageBoxIcon.Error );
                }
            }
        }

        private void  LoadFromAssembly()
        {
            Hashtable icons = Core.ResourceIconManager.CollectAssemblyIcons();
            ShowIconsForm form = new ShowIconsForm( icons );
            if( form.ShowDialog( this ) == DialogResult.OK )
            {
                iconPicture = (Icon)icons[ form.IconName ];
                DrawIcon( iconPicture );
            }
        }

        private bool CheckValidActions(out string errorMsg, out Control errCtrl)
        {
            errorMsg = "No image is specified"; //  some default values...
            errCtrl = panelIcon;
            return (iconPicture != null);
        }
        #endregion Impl
    }
}
