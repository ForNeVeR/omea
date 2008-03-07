/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls.CustomViews
{
	/// <summary>
	/// Summary description for ViewConstructorForm.
	/// </summary>
	public class EditViewForm : ViewCommonDialogBase
	{
        private const int _ciFormHeight = 590;
        private const int _ciShortFormHeight = 555;

        private CheckBox        checkInWsps;
        private Button          buttonChooseWsps;
        private TextBox         textWsps;
        private IResourceList   linkedWsps;

        private System.ComponentModel.IContainer components;

        #region Ctor
		public EditViewForm() : this( (string)null ) {}

		public EditViewForm( IResource view ) : base( "IsViewLinked", true, false, true )
		{
		    BaseResource = view;
            Initialize( view.GetStringProp( Core.Props.Name ) );
            InitializeBasePanels( BaseResource );
            Text = "Edit View";
        }

		public EditViewForm( string name, string[] resTypes, IResource[][] conditions, IResource[] exceptions )
               : base( "IsViewLinked", true, false, true )
		{
            Initialize( name );
            InitializeBasePanels( resTypes, conditions, exceptions );
        }

		public EditViewForm( string initialType ) : base( "IsViewLinked", true, false, true )
		{
            Initialize( null );
            InitializeBasePanels( (initialType == null) ? null : new string[] { initialType }, new IResource[][] {}, new IResource[] {} );
		}

        protected void  Initialize( string viewName )
        {
            ShowOrButton = true;
			InitializeComponent();
            InitialName = string.Empty;
            resourceTypesLink.Tag = resourceTypesLink.Text = ChooseResTypeDialog.AllResTypesRepresentation;
            _editHeading.Text = InitialName = viewName ?? string.Empty;
            FormTitleString = "name of a view";
            _referenceTopic = "organizing\\organizing_using_views.html#newmanual";

            linkedWsps = Core.ResourceStore.EmptyResourceList;
            if( Core.WorkspaceManager.GetAllWorkspaces().Count > 1 )
            {
                if( BaseResource != null )
                    linkedWsps = BaseResource.GetLinksOfType( null, "InWorkspace" );

                checkInWsps.Checked = buttonChooseWsps.Enabled = textWsps.Enabled = (linkedWsps.Count > 0);
                if( checkInWsps.Checked )
                {
                    PrintSelectedWsps( linkedWsps );
                }
            }
        }

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
					components.Dispose();
			}
			base.Dispose( disposing );
		}
        #endregion Ctor

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private new void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();

            base.InitializeComponent();

            this.resTypeToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.resTypeToolTip.Active = true;
            this.resTypeToolTip.ShowAlways = true;

            this.SuspendLayout();

            //-----------------------------------------------------------------
            //  Create and place controls only in the case when user managed to
            //  create at least one workspace of his own.
            //-----------------------------------------------------------------
            int wspsCpunt = Core.WorkspaceManager.GetAllWorkspaces().Count;
            if( wspsCpunt > 1 )
            {
                this.checkInWsps = new System.Windows.Forms.CheckBox();
                this.buttonChooseWsps = new System.Windows.Forms.Button();
                this.textWsps = new System.Windows.Forms.TextBox();
                // 
                // checkInWsps
                // 
                this.checkInWsps.FlatStyle = System.Windows.Forms.FlatStyle.System;
                this.checkInWsps.Location = new Point( 8, _ciFormHeight - 89 );
                this.checkInWsps.Size = new Size( 100, 20 );
                this.checkInWsps.Text = "In Workspaces:";
                this.checkInWsps.CheckStateChanged += new EventHandler(checkInWsps_CheckStateChanged);
                this.checkInWsps.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left);
                // 
                // buttonChooseWsps
                // 
                this.buttonChooseWsps.Anchor = (AnchorStyles)(AnchorStyles.Bottom | AnchorStyles.Left);
                this.buttonChooseWsps.FlatStyle = System.Windows.Forms.FlatStyle.System;
                this.buttonChooseWsps.Location = new System.Drawing.Point(110, _ciFormHeight - 90);
                this.buttonChooseWsps.Size = new System.Drawing.Size(60, 20);
                this.buttonChooseWsps.Name = "buttonChooseWsps";
                this.buttonChooseWsps.TabIndex = 6;
                this.buttonChooseWsps.Text = "Choose...";
                this.buttonChooseWsps.Click += new EventHandler(buttonChooseWsps_Click);
                // 
                // textWsps
                // 
                this.textWsps.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
                this.textWsps.Location = new System.Drawing.Point(180, _ciFormHeight - 90);
                this.textWsps.Name = "textWsps";
                this.textWsps.Size = new System.Drawing.Size(200, 21);
                this.textWsps.TabIndex = 1;
                this.textWsps.Text = "";
                this.textWsps.ReadOnly = true;

                this.Controls.Add(checkInWsps);
                this.Controls.Add(buttonChooseWsps);
                this.Controls.Add(textWsps);

                this.ClientSize = new System.Drawing.Size(398, _ciFormHeight);
                PlaceBottomControls( _ciFormHeight );
            }
            else
            {
                this.ClientSize = new System.Drawing.Size(398, _ciShortFormHeight);
                PlaceBottomControls( _ciShortFormHeight );
            }

            // 
            // ViewConstructorForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.Name = "ViewConstructorForm";
            this.Text = "New View";
            base.okButton.Click += new System.EventHandler(this.okButton_Click);

            ResumeLayout( false );
        }
		#endregion

        #region OK
        private void okButton_Click(object sender, EventArgs e)
        {
            #region Preconditions
            if( !okButton.Enabled )
                throw new ApplicationException( "ViewConstructor -- Can not construct view given the current conditions/resource types configuration" );
            #endregion Preconditions

            okButton.Enabled = false;
            if( isResourceNewAndNameExist( FilterManagerProps.ViewResName ) )
            {
                DialogResult result = MessageBox.Show( this, "View with such name already exists. Do you want to overwrite it?", 
                                                       "Names collision", MessageBoxButtons.YesNo );
                if( result == DialogResult.No )
                    return;

                #region Debug Check
                //  The following check is crazy, but bug #6066 is caused by the
                //  null string passed to the DeleteView method.
                //  Here I want to be sure that I'm not completely mad about
                //  that guards.
                if( _editHeading.Text == null )
                    throw new ArgumentException( "ViewConstructor -- Null value of TextEdit control" );
                #endregion Debug Check

                FMgr.DeleteView( _editHeading.Text );
            }

            //-------------------------------------------------------------
            IResource[][] conditionGroups = Controls2Resources( panelConditions.Controls );
            IResource[] exceptions = ConvertTemplates2Conditions( panelExceptions.Controls );
            string[] formTypes = ReformatTypes( CurrentResTypeDeep );

            //-------------------------------------------------------------
            //  - Create new view as usual;
            //  - For an existing view - do not create it anew, but save all
            //    its existing characteristics like base view folder and
            //    position in the tree. Additionally, editing existing view
            //    will not cause Views Pane to flash.
            //-------------------------------------------------------------

            if( BaseResource == null ) // new view?
                BaseResource = FMgr.RegisterView( _editHeading.Text, formTypes, conditionGroups, exceptions );
            else
                FMgr.ReregisterView( BaseResource, _editHeading.Text, formTypes, conditionGroups, exceptions );
            LinkWithWorkspaces();

            FreeConditionLists( panelConditions.Controls );
            FreeConditionLists( panelExceptions.Controls );
            DialogResult = DialogResult.OK;
        }

        private void  LinkWithWorkspaces()
        {
            ResourceProxy proxy = new ResourceProxy( BaseResource );
            proxy.BeginUpdate();
            proxy.DeleteLinks( "InWorkspace" );
            foreach( IResource wsp in linkedWsps )
            {
                proxy.SetProp( "InWorkspace", wsp );
            }
            proxy.EndUpdate();
        }
        #endregion OK

        #region Select Workspaces
        private void buttonChooseWsps_Click(object sender, EventArgs e)
        {
            IResourceList selection = Core.UIManager.SelectResources( this, "Workspace", "Select Workspaces", linkedWsps );
            if( selection != null )
            {
                PrintSelectedWsps( selection );
                linkedWsps = selection;

                if( selection.Count == 0 )
                    checkInWsps.Checked = false;
            }
        }
        private void  PrintSelectedWsps( IResourceList list )
        {
            string text = string.Empty;
            foreach( IResource res in list )
                text += res.DisplayName + ", ";

            if( text.Length > 0 )
                text = text.Substring( 0, text.Length - 2 );

            textWsps.Text = text;
        }

        private void checkInWsps_CheckStateChanged(object sender, EventArgs e)
        {
            buttonChooseWsps.Enabled = textWsps.Enabled = checkInWsps.Checked;
            if( !checkInWsps.Checked )
            {
                textWsps.Text = string.Empty;
                linkedWsps = Core.ResourceStore.EmptyResourceList;
            }
        }
        #endregion Select Workspaces
    }
}
