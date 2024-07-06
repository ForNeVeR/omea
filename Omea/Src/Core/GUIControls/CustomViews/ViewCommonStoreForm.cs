// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.Omea.FiltersManagement;
using JetBrains.UI.Components.ImageListButton;

namespace JetBrains.Omea.GUIControls.CustomViews
{
    public class LabelInfo
    {
        public  enum  ParamType   { StrType, Resource, Int, Date, None }

        public  IResource   AssociatedResource;
        public  object      Parameters;
        public  string      Representation;
        public  Panel       ParentPanel;
        public  ImageListButton DelButton;
        public  int         GroupIndex;

        public LabelInfo()
        {
            GroupIndex = -1;
        }
    }

    public class ViewCommonDialogBase : DialogBase
    {
        #region Attributes
        protected const int     _cInitialVIndent = 2;
        protected const int     _cBaseWidth = 260;
        protected const int     _cBaseHeight = 22;
        protected const int     _cAddLabelXPosDiff = 20;
        protected const int     _cInterControlSpace = 7;
        protected const int     _cTopInterval = 6;
        protected const int     _cCollapsedPanelHeight = 30;
        protected const int     _cMinimalPanelHeight = 100;
        protected const int     _cPanelsInterval = 10;
        protected const string  _cQueryHelpTopic = "/reference/query_syntax.html";
        protected const string  _cOpenConditionsKey = "ConditionsOpen";
        protected const string  _cOpenExceptionsKey = "ExceptionsOpen";

        protected delegate bool ConditionChecker( out string errorText, out Control errorHighlighter );
        protected ConditionChecker    _externalChecker = null;

        protected bool                HideShowProcessing;

        protected bool                MustHaveHeading;
        protected bool                CanAllRTWithNoConditions;
        protected bool                IsQueryConditionsAllowed;
        protected bool                ShowOrButton = false;

        protected string              FormTitleString = "name of a rule"; // most common use
        protected string              _referenceTopic;
        protected string              CurrentResTypeDeep;
        protected string              InitialName;
        protected IResourceList       ValidResourceTypes = null;
        protected IResource           BaseResource = null;
        protected int                 LinkedPinnedSignProp;
        private readonly Hashtable    _labelsOfBoxes = new Hashtable();
        private readonly Hashtable    _labelsOfPanels = new Hashtable();

        protected IFilterRegistry      FMgr;
        protected IResourceStore      RStore;

        protected Label         _lblHeading;
        protected TextBox       _editHeading;
        protected Label         forResourcesLabel;
        protected JetLinkLabel  resourceTypesLink;

        protected GroupBox      _boxConditions;
        protected Label         _lblConditionsTitle;
        protected ImageListButton  buttonHideShowConditions;
        protected JetLinkLabel  labelHideShowConditionsText;
        protected Panel         panelConditions;
        protected JetLinkLabel  labelAddConditionByAnd;
        protected JetLinkLabel  labelAddConditionByOr;
        protected Label         labelAdd;
        protected JetLinkLabel  labelAddCondition;

        protected GroupBox      _boxExceptions;
        protected Label         _lblExceptionsTitle;
        protected ImageListButton buttonHideShowExceptions;
        protected JetLinkLabel  labelHideShowExceptionsText;
        protected Panel         panelExceptions;
        protected JetLinkLabel  labelAddException;

        protected Button        okButton;
        protected Button        cancelButton;
        protected Button        helpButton;
        protected GroupBox      delimiterLine;
        protected ToolTip       resTypeToolTip;
        protected Label         _lblErrorText;
        protected ErrorProvider _errorProvider;

        protected Font          _labelFont = new Font( "Tahoma", 8 );
        protected Font          _labelGreyedFont = new Font( "Tahoma", 8 );
        protected Font          _labelBoldFont = new Font( "Tahoma", 8, FontStyle.Bold );

        private   ImageList     _pinIconImages, _delIconImages;
        protected ImageList     _showHideImageList, _addImageList;
        #endregion Attributes

        #region Ctor/Initialization
        public ViewCommonDialogBase( string linkedPinnedPropName, bool mustHaveHeading,
                                  bool isAllResTypeAllowed, bool isQueryConditionsAllowed )
        {
            FMgr = Core.FilterRegistry;
            RStore = Core.ResourceStore;
            MustHaveHeading = mustHaveHeading;
            CanAllRTWithNoConditions = isAllResTypeAllowed;
            IsQueryConditionsAllowed = isQueryConditionsAllowed && Core.TextIndexManager.IsIndexPresent();
            LinkedPinnedSignProp = Core.ResourceStore.GetPropId( linkedPinnedPropName );

            _pinIconImages = new ImageList();
            _delIconImages = new ImageList();
            _showHideImageList = new ImageList();
            _addImageList = new ImageList();
            Assembly thisOne = Assembly.GetExecutingAssembly();

            Stream stream = thisOne.GetManifestResourceStream( "GUIControls.Icons.FlagNoProp.ico" );
            _pinIconImages.Images.Add( new Icon( stream ) );
            stream = thisOne.GetManifestResourceStream( "GUIControls.Icons.pinYes.ico" );
            _pinIconImages.Images.Add( new Icon( stream ) );
            stream = thisOne.GetManifestResourceStream( "GUIControls.Icons.pinNo.ico" );
            _pinIconImages.Images.Add( new Icon( stream ) );

            stream = thisOne.GetManifestResourceStream( "GUIControls.Icons.deleteRaised.ico" );
            _delIconImages.Images.Add( new Icon( stream ) );
            stream = thisOne.GetManifestResourceStream( "GUIControls.Icons.deleteDisabled.ico" );
            _delIconImages.Images.Add( new Icon( stream ) );
            stream = thisOne.GetManifestResourceStream( "GUIControls.Icons.deleteGreyed.ico" );
            _delIconImages.Images.Add( new Icon( stream ) );

            stream = thisOne.GetManifestResourceStream( "GUIControls.Icons.ExpandPanel.ico" );
            _showHideImageList.Images.Add( new Icon( stream ) );
            stream = thisOne.GetManifestResourceStream( "GUIControls.Icons.CollapsePanel.ico" );
            _showHideImageList.Images.Add( new Icon( stream ) );
            stream = thisOne.GetManifestResourceStream( "GUIControls.Icons.ExpandPanelHover.ico" );
            _showHideImageList.Images.Add( new Icon( stream ) );
            stream = thisOne.GetManifestResourceStream( "GUIControls.Icons.CollapsePanelHover.ico" );
            _showHideImageList.Images.Add( new Icon( stream ) );

            stream = thisOne.GetManifestResourceStream( "GUIControls.Icons.AddConditionNormal.ico" );
            _addImageList.Images.Add( new Icon( stream ) );
            stream = thisOne.GetManifestResourceStream( "GUIControls.Icons.AddConditionHover.ico" );
            _addImageList.Images.Add( new Icon( stream ) );
        }
        protected void  InitializeBasePanels( string[] resTypes, IResource[][] conds, IResource[] excpts )
        {
            RecreateResTypes( resTypes );

            ArrayList parameters = new ArrayList(), negParameters = new ArrayList();
            ArrayList conditions = CollectResourcesAndTemplates( conds, parameters );
            ArrayList exceptions = CollectResourcesAndTemplates( excpts, negParameters );
            InitializePanelsAndButtons( conditions, parameters, exceptions, negParameters );
        }
        protected void  InitializeBasePanels( IResource view )
        {
            RecreateResTypes( view );

            ArrayList parameters = new ArrayList(), negParameters = new ArrayList();
            ArrayList conditions = CollectResourcesAndTemplates( view, parameters, Core.FilterRegistry.Props.LinkedConditions );
            ArrayList exceptions = CollectResourcesAndTemplates( view, negParameters, Core.FilterRegistry.Props.LinkedExceptions );
            InitializePanelsAndButtons( conditions, parameters, exceptions, negParameters );
        }

        protected void  InitializePanelsAndButtons( ArrayList conditions, ArrayList parameters,
                                                    ArrayList exceptions, ArrayList negParameters )
        {
            ArrayList pinList = new ArrayList(), pinParams = new ArrayList();
            IResourceList pinned = Core.ResourceStore.FindResourcesWithProp( null, LinkedPinnedSignProp );
            foreach( IResource res in pinned )
            {
                if( conditions.IndexOf( res ) == -1 )
                {
                    pinList.Add( res );
                    pinParams.Add( new LabelInfo() );
                }
            }

            AddConditions( panelConditions, pinList, pinParams );
            AddConditions( panelConditions, conditions, parameters  );
            AddConditions( panelExceptions, exceptions, negParameters );
        }
        #endregion Ctor/Initialization

        #region InitializeComponent
        protected void InitializeComponent()
        {
            _lblHeading = new Label();
            _editHeading = new TextBox();
            forResourcesLabel = new Label();
            resourceTypesLink = new JetLinkLabel();

            _boxConditions = new GroupBox();
            _lblConditionsTitle = new Label();
            buttonHideShowConditions = new ImageListButton();
            labelHideShowConditionsText = new JetLinkLabel();
            panelConditions = new Panel();
            labelAdd = new Label();
            labelAddConditionByAnd = new JetLinkLabel();
            labelAddConditionByOr = new JetLinkLabel();
            labelAddCondition = new JetLinkLabel();

            _boxExceptions = new GroupBox();
            _lblExceptionsTitle = new Label();
            buttonHideShowExceptions = new ImageListButton();
            labelHideShowExceptionsText = new JetLinkLabel();
            panelExceptions = new Panel();
            labelAddException = new JetLinkLabel();

            okButton = new Button();
            cancelButton = new Button();
            helpButton = new Button();
            delimiterLine = new GroupBox();
            _lblErrorText = new Label();
            _errorProvider = new ErrorProvider( this );
            //
            // _lblHeading
            //
            _lblHeading.FlatStyle = FlatStyle.System;
            _lblHeading.Location = new Point(8, 8);
            _lblHeading.Name = "viewNameLabel";
            _lblHeading.Size = new Size(64, 16);
            _lblHeading.TabIndex = 0;
            _lblHeading.Text = "&View name:";
            //
            // _editHeading
            //
            _editHeading.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            _editHeading.Location = new Point(72, 4);
            _editHeading.Name = "viewNameText";
            _editHeading.Size = new Size(300, 21);
            _editHeading.TabIndex = 1;
            _editHeading.Text = "";
            _editHeading.TextChanged += this.viewNameText_TextChanged;
            //
            // forResourcesLabel
            //
            forResourcesLabel.FlatStyle = FlatStyle.System;
            forResourcesLabel.Location = new Point(9, 32);
            forResourcesLabel.Name = "forResourcesLabel";
            forResourcesLabel.Size = new Size(56, 16);
            forResourcesLabel.TabIndex = 2;
            forResourcesLabel.Text = "Active &for:";
            //
            // resourceTypesLink
            //
            resourceTypesLink.Anchor = (AnchorStyles.Top | AnchorStyles.Left );
            resourceTypesLink.CausesValidation = false;
            resourceTypesLink.Location = new Point(72, 32);
            resourceTypesLink.Name = "resourceTypesLink";
            resourceTypesLink.Size = new Size(300, 16);
            resourceTypesLink.TabIndex = 3;
            resourceTypesLink.TabStop = true;
            resourceTypesLink.Text = "All resource types";
            resourceTypesLink.Click += resourceTypesLink_LinkClicked;

            #region Conditions
            //
            // _boxConditions
            //
            _boxConditions.Location = new Point(7, 52);
            _boxConditions.Anchor = (AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right);
            _boxConditions.Name = "_boxConditions";
            _boxConditions.Size = new Size(384, 215);
            _boxConditions.FlatStyle = FlatStyle.System;
            _boxConditions.TabStop = false;
            //
            // _lblConditionsTitle
            //
            _lblConditionsTitle.FlatStyle = FlatStyle.System;
            _lblConditionsTitle.Location = new Point(10, 10);
            _lblConditionsTitle.Name = "_lblConditionsTitle";
            _lblConditionsTitle.Size = new Size(90, 16);
            _lblConditionsTitle.TabStop = false;
            _lblConditionsTitle.TextAlign = ContentAlignment.MiddleLeft;
            _lblConditionsTitle.Text = "Conditions";
            //
            // labelHideShowConditionsText
            //
            labelHideShowConditionsText.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            labelHideShowConditionsText.Location = new Point(330, 10);
            labelHideShowConditionsText.Name = "labelHideShowConditionsText";
            labelHideShowConditionsText.Size = new Size(28, 16);
            labelHideShowConditionsText.TabStop = false;
            labelHideShowConditionsText.TextAlign = ContentAlignment.MiddleLeft;
            labelHideShowConditionsText.Text = "Hide";
            labelHideShowConditionsText.Tag = buttonHideShowConditions;
            labelHideShowConditionsText.Click += labelHideShowPanel_Click;
            //
            // buttonHideShowConditions
            //
            buttonHideShowConditions.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            buttonHideShowConditions.Name = "buttonHideShowConditions";
            buttonHideShowConditions.Size = new Size(16, 16);
            buttonHideShowConditions.Location = new Point(360, 9);
            buttonHideShowConditions.TabIndex = 9;
            buttonHideShowConditions.Click += HideShowPanel_Click;
            buttonHideShowConditions.Tag = panelConditions;
            buttonHideShowConditions.NormalImageIndex = 1;
            buttonHideShowConditions.PressedImageIndex = 0;
            buttonHideShowConditions.HotImageIndex = 3;
            buttonHideShowConditions.ImageList = _showHideImageList;
            buttonHideShowConditions.Cursor = Cursors.Hand;
            //
            // panelConditions
            //
            panelConditions.AutoScroll = true;
            panelConditions.BackColor = SystemColors.Window;
            panelConditions.BorderStyle = BorderStyle.Fixed3D;
            panelConditions.Location = new Point(8, 28);
            panelConditions.Name = "panelConditions";
            panelConditions.Size = new Size(370, 158);
            panelConditions.TabIndex = 8;
            panelConditions.Resize += panel_Resize;
            panelConditions.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            //
            // labelAddConditionByOr
            //
            labelAddConditionByOr.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            labelAddConditionByOr.Name = "labelAddConditionByOr";
            labelAddConditionByOr.Size = new Size(28, 16);
            labelAddConditionByOr.TabStop = true;
            labelAddConditionByOr.TextAlign = ContentAlignment.MiddleLeft;
            labelAddConditionByOr.Text = "OR...";
            labelAddConditionByOr.Tag = panelConditions;
            labelAddConditionByOr.Click += AddConditionsByOr_Click;
            int position = _boxConditions.Width - _cAddLabelXPosDiff - (int)(labelAddConditionByOr.Size.Width * Core.ScaleFactor.Width);
            labelAddConditionByOr.Location = new Point(position, 194);
            //
            // labelAddConditionByAnd
            //
            labelAddConditionByAnd.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            labelAddConditionByAnd.Name = "labelAddConditionByAnd";
            labelAddConditionByAnd.Size = new Size(28, 16);
            labelAddConditionByAnd.TabStop = true;
            labelAddConditionByAnd.TextAlign = ContentAlignment.MiddleLeft;
            labelAddConditionByAnd.Text = "And...";
            labelAddConditionByAnd.Tag = panelConditions;
            labelAddConditionByAnd.Click += AddConditionsByAnd_Click;
            position = position - (int)((labelAddConditionByAnd.Size.Width + 10) * Core.ScaleFactor.Width);
            labelAddConditionByAnd.Location = new Point(position, 194);
            //
            // labelAdd
            //
            labelAdd.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            labelAdd.Name = "labelAdd";
            labelAdd.Size = new Size(90, 16);
            labelAdd.TabStop = true;
            labelAdd.TextAlign = ContentAlignment.MiddleLeft;
            labelAdd.Text = "Add condition by:";
            position = position - (int)((labelAdd.Size.Width + 15) * Core.ScaleFactor.Width);
            labelAdd.Location = new Point( position, 194);
            //
            // labelAddCondition
            //
            labelAddCondition.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            labelAddCondition.Name = "labelAddConditionByOr";
            labelAddCondition.Size = new Size(90, 16);
            labelAddCondition.TabStop = true;
            labelAddCondition.TextAlign = ContentAlignment.MiddleLeft;
            labelAddCondition.Text = "Add Condition...";
            labelAddCondition.Tag = panelConditions;
            labelAddCondition.Click += AddConditionsByAnd_Click;
            position = _boxConditions.Width - _cAddLabelXPosDiff - (int)(labelAddCondition.Size.Width * Core.ScaleFactor.Width);
            labelAddCondition.Location = new Point(position, 194);
            #endregion Conditions

            #region Exceptions
            //
            // _boxExceptions
            //
            _boxExceptions.Location = new Point(7, 270);
            _boxExceptions.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            _boxExceptions.Name = "_boxExceptions";
            _boxExceptions.Size = new Size(384, 215);
            _boxExceptions.FlatStyle = FlatStyle.System;
            _boxExceptions.TabStop = false;
            //
            // _lblExceptionsTitle
            //
            _lblExceptionsTitle.FlatStyle = FlatStyle.System;
            _lblExceptionsTitle.Location = new Point(10, 10);
            _lblExceptionsTitle.Name = "_lblExceptionsTitle";
            _lblExceptionsTitle.Size = new Size(90, 16);
            _lblExceptionsTitle.TabStop = false;
            _lblExceptionsTitle.TextAlign = ContentAlignment.MiddleLeft;
            _lblExceptionsTitle.Text = "Exceptions";
            //
            // labelHideShowExceptionsText
            //
            labelHideShowExceptionsText.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            labelHideShowExceptionsText.Location = new Point(330, 10);
            labelHideShowExceptionsText.Name = "labelHideShowExceptionsText";
            labelHideShowExceptionsText.Size = new Size(28, 16);
            labelHideShowExceptionsText.TabStop = false;
            labelHideShowExceptionsText.TextAlign = ContentAlignment.MiddleLeft;
            labelHideShowExceptionsText.Text = "Hide";
            labelHideShowExceptionsText.Tag = buttonHideShowExceptions;
            labelHideShowExceptionsText.Click += labelHideShowPanel_Click;
            //
            // buttonHideShowExceptions
            //
            buttonHideShowExceptions.Location = new Point(360, 9);
            buttonHideShowExceptions.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            buttonHideShowExceptions.Name = "buttonHideShowExceptions";
            buttonHideShowExceptions.Size = new Size(16, 16);
            buttonHideShowExceptions.TabIndex = 9;
            buttonHideShowExceptions.Click += HideShowPanel_Click;
            buttonHideShowExceptions.Tag = panelExceptions;
            buttonHideShowExceptions.NormalImageIndex = 1;
            buttonHideShowExceptions.PressedImageIndex = 0;
            buttonHideShowExceptions.HotImageIndex = 3;
            buttonHideShowExceptions.ImageList = _showHideImageList;
            buttonHideShowExceptions.Cursor = Cursors.Hand;
            //
            //
            // panelExceptions
            //
            panelExceptions.AutoScroll = true;
            panelExceptions.BackColor = SystemColors.Window;
            panelExceptions.BorderStyle = BorderStyle.Fixed3D;
            panelExceptions.Location = new Point(8, 28);
            panelExceptions.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panelExceptions.Name = "panelExceptions";
            panelExceptions.Size = new Size(370, 158);
            panelExceptions.TabIndex = 12;
            panelExceptions.Resize += panel_Resize;
            //
            // labelAddExceptionByAnd
            //
            labelAddException.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            labelAddException.Name = "labelAddConditionByAnd";
            labelAddException.Size = new Size(90, 16);
            labelAddException.TabStop = true;
            labelAddException.TextAlign = ContentAlignment.MiddleLeft;
            labelAddException.Text = "Add Exception...";
            labelAddException.Tag = panelExceptions;
            labelAddException.Click += AddConditionsByAnd_Click;
            position = _boxExceptions.Width - _cAddLabelXPosDiff - (int)(labelAddException.Size.Width * Core.ScaleFactor.Width);
            labelAddException.Location = new Point( position, 194);
            #endregion Exceptions

            //
            // okButton
            //
            okButton.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            okButton.FlatStyle = FlatStyle.System;
            okButton.Location = new Point(158, 270);
            okButton.Name = "okButton";
            okButton.Size = new Size(72, 24);
            okButton.TabIndex = 20;
            okButton.Text = "OK";
            //
            // cancelButton
            //
            cancelButton.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            cancelButton.FlatStyle = FlatStyle.System;
            cancelButton.Location = new Point(238, 270);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(72, 24);
            cancelButton.TabIndex = 22;
            cancelButton.Text = "Cancel";
            //
            // helpButton
            //
            helpButton.Anchor = (AnchorStyles.Bottom | AnchorStyles.Right);
            helpButton.FlatStyle = FlatStyle.System;
            helpButton.Location = new Point(318, 270);
            helpButton.Name = "helpButton";
            helpButton.Size = new Size(72, 24);
            helpButton.TabIndex = 24;
            helpButton.Text = "Help";
            helpButton.Click += helpButton_Click;
            //
            // delimiterLine
            //
            delimiterLine.Location = new Point(4, 300);
            delimiterLine.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            delimiterLine.Name = "delimiterLine";
            delimiterLine.Size = new Size(400, 4);
            delimiterLine.FlatStyle = FlatStyle.System;
            delimiterLine.TabStop = false;
            delimiterLine.ForeColor = Color.AliceBlue;
            //
            // _lblErrorText
            //
            _lblErrorText.FlatStyle = FlatStyle.System;
            _lblErrorText.Anchor = (AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right);
            _lblErrorText.Location = new Point(8, 313);
            _lblErrorText.Name = "_lblErrorText";
            _lblErrorText.Size = new Size(370, 18);
            _lblErrorText.ForeColor = Color.Red;
            _lblErrorText.Text = "";
            _lblErrorText.TabStop = false;
            _lblErrorText.Visible = false;
            //
            // this Form
            //
            AcceptButton = okButton;
            CancelButton = cancelButton;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            KeyPreview = true;
            Font = new Font("Tahoma", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((System.Byte)(204)));
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterScreen;
            VisibleChanged += OnFormVisibleChanged;
            SizeChanged += CommonDialogStore_SizeChanged;

            Controls.Add(_lblHeading);
            Controls.Add(_editHeading);
            Controls.Add(resourceTypesLink);
            Controls.Add(forResourcesLabel);

            Controls.Add(_boxConditions);
            _boxConditions.Controls.Add(_lblConditionsTitle);
            _boxConditions.Controls.Add(buttonHideShowConditions);
            _boxConditions.Controls.Add(labelHideShowConditionsText);
            _boxConditions.Controls.Add(panelConditions);
            _boxConditions.Controls.Add(labelAdd);
            _boxConditions.Controls.Add(labelAddConditionByOr);
            _boxConditions.Controls.Add(labelAddConditionByAnd);
            _boxConditions.Controls.Add(labelAddCondition);

            Controls.Add(_boxExceptions);
            _boxExceptions.Controls.Add(_lblExceptionsTitle);
            _boxExceptions.Controls.Add(buttonHideShowExceptions);
            _boxExceptions.Controls.Add(labelHideShowExceptionsText);
            _boxExceptions.Controls.Add(panelExceptions);
            _boxExceptions.Controls.Add(labelAddException);

            Controls.Add(okButton);
            Controls.Add(cancelButton);
            Controls.Add(helpButton);
            Controls.Add(delimiterLine);
            Controls.Add(_lblErrorText);

            buttonHideShowConditions.BringToFront();
            buttonHideShowExceptions.BringToFront();

            _labelsOfBoxes[ _boxConditions ] = labelHideShowConditionsText;
            _labelsOfBoxes[ _boxExceptions ] = labelHideShowExceptionsText;
            _labelsOfPanels[ _boxConditions ] = new Control[] { labelAddConditionByAnd, labelAddConditionByOr, labelAdd, labelAddCondition };
            _labelsOfPanels[ _boxExceptions ] = new Control[] { labelAddException };
        }

        protected void  PlaceBottomControls( int baseFormHeight )
        {
            okButton.Location = new Point(okButton.Left, baseFormHeight - 58);
            cancelButton.Location = new Point(cancelButton.Left, baseFormHeight - 58);
            helpButton.Location = new Point(helpButton.Left, baseFormHeight - 58);
            delimiterLine.Location = new Point(delimiterLine.Left, baseFormHeight - 28);
            _lblErrorText.Location = new Point(_lblErrorText.Left, baseFormHeight - 20);
        }
        #endregion InitializeComponent

        public string    HeadingText     {   get{  return( _editHeading.Text );  }  }
        public IResource ResultResource  {   get{  return( BaseResource );  }  }

        #region Resource Type Processing
        protected void resourceTypesLink_LinkClicked(object sender, EventArgs e)
        {
            ChooseResTypeDialog resTypesSelector = new ChooseResTypeDialog( CurrentResTypeDeep, ValidResourceTypes );
            if( resTypesSelector.ShowDialog( this ) == DialogResult.OK )
            {
                resourceTypesLink.Tag = resTypesSelector.ChosenResourcesFullText;
                CurrentResTypeDeep = resTypesSelector.ChosenResourcesDeepText;

                AssignResTypesText( resTypesSelector.ChosenResourcesFullText );
                CheckFormConsistency();
            }
            resTypesSelector.Dispose();
        }

        protected void  RecreateResTypes( IResource view )
        {
            string[] types = FilterRegistry.CompoundType( view );
            RecreateResTypes( types );
        }

        protected void  RecreateResTypes( string[] types )
        {
            CurrentResTypeDeep = (types == null) ? null : string.Join( "|", types );
            if( CurrentResTypeDeep != null )
            {
                string   shortTypes, fullTypes;
                ChooseResTypeDialog.Deep2Display( CurrentResTypeDeep, out shortTypes, out fullTypes );
                resourceTypesLink.Tag = fullTypes;
                AssignResTypesText( shortTypes );
            }
            else
                resourceTypesLink.Tag = resourceTypesLink.Text = ChooseResTypeDialog.AllResTypesRepresentation;
        }

        protected void  AssignResTypesText( string resTypes )
        {
            int       charsFilled, linesFilled;
            Graphics  helper = Graphics.FromHwnd( resourceTypesLink.Handle );
            helper.MeasureString( resTypes, _labelFont, new SizeF( Width - resourceTypesLink.Location.X - 15, 16.0f ),
                                  new StringFormat(), out charsFilled, out linesFilled );
            if( charsFilled < resTypes.Length )
                resourceTypesLink.Text = resTypes.Substring( 0, charsFilled - 3 ) + "...";
            else
                resourceTypesLink.Text = resTypes;

            if( resourceTypesLink.Text == (string)resourceTypesLink.Tag )
                resTypeToolTip.SetToolTip( resourceTypesLink, "" );
            else
                resTypeToolTip.SetToolTip( resourceTypesLink, (string)resourceTypesLink.Tag );
        }
        #endregion

        #region Template to Condition
        protected IResource[][] Controls2Resources( Control.ControlCollection controls )
        {
            ArrayList   conditionGroups = new ArrayList();
            ArrayList   group = new ArrayList();
            foreach( Control ctrl in controls )
            {
                if( ctrl is Label || ctrl is LinkLabel )
                {
                    if( ctrl.Text == "OR" )
                    {
                        conditionGroups.Add( group.ToArray( typeof(IResource)) );
                        group = new ArrayList();
                    }
                    else
                        ConvertControlParams2Condition( ctrl, group );
                }
            }

            if( group.Count > 0 )
                conditionGroups.Add( group.ToArray( typeof(IResource)) );

            return (conditionGroups.Count > 0) ?
                    ((IResource[][]) conditionGroups.ToArray( typeof(IResource[])) ) : null;
        }

        protected IResource[] ConvertTemplates2Conditions( Control.ControlCollection controls )
        {
            ArrayList   conditions = new ArrayList();
            foreach( Control ctrl in controls )
            {
                if( ctrl is Label || ctrl is LinkLabel )
                {
                    ConvertControlParams2Condition( ctrl, conditions );
                }
            }
            return( (IResource[]) conditions.ToArray( typeof( IResource ) ) );
        }

        private void  ConvertControlParams2Condition( Control ctrl, ArrayList conditions )
        {
            LabelInfo   labelTag = (LabelInfo)ctrl.Tag;
            IResource res = labelTag.AssociatedResource;

            #region Preconditions
            Debug.Assert( !isTemplate( res ) || labelTag.Parameters != null || res.HasProp( LinkedPinnedSignProp ),
                          "Can not construct condition out from the template without parameters." );
            #endregion Preconditions

            if( !isTemplate( res ))
                conditions.Add( res );
            else
            if( labelTag.Parameters != null )
            {
                string[] formTypes = ReformatTypes( CurrentResTypeDeep );
                res = FilterConvertors.InstantiateTemplate( res, labelTag.Parameters, labelTag.Representation, formTypes );

                conditions.Add( res );
            }
        }
        #endregion Template to Condition

        #region Condition to Template
        protected ArrayList CollectResourcesAndTemplates( IResource source, ArrayList paramList, int propId )
        {
            #region Preconditions
            if( paramList.Count != 0 )
                throw new ArgumentException( "CollectResourcesAndTemplates -- Input vector of parameters must be empty on entry." );
            #endregion Preconditions

            IResourceList allActions = source.GetLinksOfType( null, propId );

            //  Method distinguishes both old-style and new-style structuring:
            //  - conditions are linked via "Conjunction"-groupping resource, while
            //  - exceptions and actions are linked direclty.

            if( allActions.Count > 0 && allActions[ 0 ].Type == FilterManagerProps.ConjunctionGroup )
            {
                ArrayList metaList = new ArrayList();
                for( int i = 0; i < allActions.Count; i++ )
                {
                    IResourceList conds = allActions[ i ].GetLinksOfType( null, propId );
                    conds = conds.Minus( allActions[ i ].GetLinksOfType( source.Type, propId ) );
                    metaList.AddRange( CollectResourcesAndTemplates( conds, paramList, i ) );
                }
                return metaList;
            }
            else
                return CollectResourcesAndTemplates( allActions, paramList, 0 );
        }

        protected ArrayList CollectResourcesAndTemplates( IResource[][] allGroups, ArrayList paramList )
        {
            #region Preconditions
            if( paramList.Count != 0 )
                throw new ArgumentException( "CollectResourcesAndTemplates -- Input vector of parameters must be empty on entry." );
            #endregion Preconditions

            ArrayList metaList = new ArrayList();
            if( allGroups != null )
            {
                for( int i = 0; i < allGroups.Length; i++ )
                {
                    IResource[] conds = allGroups[ i ];
                    metaList.AddRange( CollectResourcesAndTemplates( Vector2Reslist( conds ), paramList, i ) );
                }
            }
            return metaList;
        }

        protected ArrayList CollectResourcesAndTemplates( IResource[] allActions, ArrayList paramList )
        {
            #region Preconditions
            if( paramList.Count != 0 )
                throw new ArgumentException( "CollectResourcesAndTemplates -- Input vector of parameters must be empty on entry." );
            #endregion Preconditions

            return CollectResourcesAndTemplates( Vector2Reslist( allActions ), paramList, 0 );
        }

        protected virtual ArrayList CollectResourcesAndTemplates( IResourceList allActions, ArrayList paramList, int group )
        {
            ArrayList result = new ArrayList();

            foreach( IResource res in allActions )
            {
                IResource template = res.GetLinkProp( "TemplateLink" );
                if( template != null )
                {
                    result.Add( template );
                    paramList.Add( ConditionParams2ExplicitList( template, res ) );
                    ((LabelInfo)paramList[ paramList.Count - 1 ]).GroupIndex = group;
                }
                else
                if( !res.HasProp( "Invisible" ) )
                {
                    result.Add( res );
                    paramList.Add( new LabelInfo() );
                    ((LabelInfo)paramList[ paramList.Count - 1 ]).GroupIndex = group;
                }
            }
            return( result );
        }

        protected static LabelInfo ConditionParams2ExplicitList( IResource template, IResource res )
        {
            LabelInfo   info = new LabelInfo();
            ConditionOp op = (ConditionOp)template.GetIntProp( "ConditionOp" );
            string      baseProp = template.GetStringProp( "ApplicableToProp" );

            info.Representation = res.GetStringProp( "SurfaceConditionVal" );

            if( op == ConditionOp.Eq && ResourceTypeHelper.IsDateProperty( baseProp ) )
                info.Parameters = EditTimeSpanConditionForm.Condition2Text( res );
            else
            if( op == ConditionOp.In )
            {
                if( template.GetStringProp( "ChooseFromResourceType" ) == FilterRegistry.ExternalFileTag ||
                    template.GetStringProp( "ChooseFromResourceType" ) == FilterRegistry.ExternalDirTag )
                {
                    info.Parameters = res.GetStringProp( "ConditionVal" );
                }
                else
                {
                    IResourceList linkedParams = res.GetLinksOfType( null, "LinkedSetValue" );
                    //  check that linked previously resources are removed
                    //  (accidentally). In such case we have to restore default
                    //  values.
                    if( linkedParams.Count == 0 )
                    {
                        MessageBox.Show( "Parameter of \"" + template.DisplayName +
                                         "\" refers to the resource which no longer exists in Omea. The value of this parameter is changed to the default.",
                                         "Invalid argument", MessageBoxButtons.OK, MessageBoxIcon.Warning );
                        linkedParams = null;
                    }
                    info.Parameters = linkedParams;
                }
            }
            else
            if( op == ConditionOp.QueryMatch )
                info.Parameters = res.GetStringProp( "ApplicableToProp" );
            else
            if( op == ConditionOp.Eq || op == ConditionOp.Has )
                info.Parameters = res.GetStringProp( "ConditionVal" );
            else
            if( op == ConditionOp.InRange || op == ConditionOp.Lt || op == ConditionOp.Gt )
                info.Parameters = IntIntervalForm.Condition2Text( res );
            else
                throw new InvalidOperationException( "Not supported operation in referenced template" );

            return info;
        }
        #endregion

        #region Add Conditions
        //---------------------------------------------------------------------
        //  - Form a complete list of conditions available in the system
        //  - Remove those which were already chosen
        //  - Add new condition
        //---------------------------------------------------------------------
        protected void AddConditionsByAnd_Click(object sender, EventArgs e)
        {
            Panel basePanel = (Panel)((JetLinkLabel)sender).Tag;

            labelAddConditionByAnd.Enabled = labelAddException.Enabled = false;
            IResourceList   selected = CollectConditionsFromUser( basePanel.Controls );
            if(( selected != null ) && ( selected.Count > 0 ))
            {
                ArrayList  emptyParams = CreateEmptyList( selected.Count, -1 );
                AddConditions( basePanel, selected, emptyParams );
            }

            labelAddConditionByAnd.Enabled = labelAddException.Enabled = true;
            CheckFormConsistency();
        }

        //---------------------------------------------------------------------
        //  - Form a complete list of conditions available in the system
        //  - Remove those which were already chosen
        //  - Add new condition
        //---------------------------------------------------------------------
        protected void AddConditionsByOr_Click(object sender, EventArgs e)
        {
            Panel basePanel = (Panel)((JetLinkLabel)sender).Tag;

            labelAddConditionByAnd.Enabled = labelAddException.Enabled = false;
            IResourceList   selected = CollectConditionsFromUser( basePanel.Controls );
            if(( selected != null ) && ( selected.Count > 0 ))
            {
                ArrayList  emptyParams = CreateEmptyList( selected.Count, Int32.MaxValue );
                AddConditions( basePanel, selected, emptyParams );
            }

            labelAddConditionByAnd.Enabled = labelAddException.Enabled = true;
            CheckFormConsistency();
        }

        protected void  AddConditions( Panel panel, IResourceList conditions, ArrayList parameters )
        {
            AddConditions( panel, ResList2ArrayList( conditions ), parameters );
        }
        protected void  AddConditions( Panel panel, ArrayList conditions, ArrayList storedParams )
        {
            Control  firstInAdded = null;

            //  Calc start vertical position in the panel for the new group of controls
            int  lastGroupIndex = 0;
            int  baseYCoordinate = _cTopInterval;
            foreach( Control ctrl in panel.Controls )
            {
                if( ctrl is Label || ctrl is LinkLabel )
                {
                    baseYCoordinate += ctrl.Size.Height + _cInterControlSpace;
                    lastGroupIndex = Math.Max( lastGroupIndex, ((LabelInfo) ctrl.Tag).GroupIndex );
                }
            }
            baseYCoordinate += panel.AutoScrollPosition.Y;

            for( int i = 0; i < conditions.Count; i++ )
            {
                IResource   condition = (IResource)conditions[ i ];
                string      name = condition.GetStringProp( Core.Props.Name );

                LabelInfo   info = (LabelInfo) storedParams[ i ];
                info.ParentPanel = panel;
                info.AssociatedResource = condition;

                //-------------------------------------------------------------
                //  Add "OR"-label if the new condition starts new group -
                //  - either its index is greater than the index of the previous
                //    condition, or
                //  - its index is MaxInt which unconditionally creates new group
                //    (e.g. as the result of "And by OR" action).
                //-------------------------------------------------------------
                if( info.GroupIndex != -1 &&
                   ( info.GroupIndex != lastGroupIndex || info.GroupIndex == Int32.MaxValue ))
                {
                    Label   orLabel = CreateORLabel( panel, baseYCoordinate );
                    panel.Controls.Add( orLabel );
                    baseYCoordinate += orLabel.Size.Height + _cInterControlSpace;
                }

                //-------------------------------------------------------------
                //  New group number depends on the conditions' previous groupping.
                //-------------------------------------------------------------
                if( info.GroupIndex == -1 )
                {
                    info.GroupIndex = lastGroupIndex;
                }
                else
                if( info.GroupIndex == Int32.MaxValue )
                {
                    if( i == 0 )
                        lastGroupIndex = info.GroupIndex = lastGroupIndex + 1;
                    else
                        info.GroupIndex = lastGroupIndex;
                }
                lastGroupIndex = info.GroupIndex;

                //-------------------------------------------------------------
                Label   newControl = CreateNewLabel( name, info, panel, baseYCoordinate );

                if( panel == panelConditions )
                {
                    ImageListButton button = CreatePinButton( panel, baseYCoordinate, info );
                    panel.Controls.Add( button );
                }

                //-------------------------------------------------------------
                //  Add label control AFTER the button so that if some overlap
                //  happens, button is always on the top of the label and thus
                //  operable nevertheless.
                //-------------------------------------------------------------
                panel.Controls.Add( newControl );

                //-------------------------------------------------------------
                ImageListButton delButton = CreateDeleteButton( panel, baseYCoordinate, info );
                panel.Controls.Add( delButton );

                baseYCoordinate += _cBaseHeight + _cInterControlSpace;
                if( firstInAdded == null )
                    firstInAdded = newControl;
            }
            panel.ScrollControlIntoView( firstInAdded );
        }

        private Label CreateNewLabel( string name, LabelInfo info, Panel panel, int baseYCoordinate )
        {
            Label       control;
            string  labelText = name;
            if( labelText.IndexOf( "%" ) == -1 )
                control = new Label();
            else
            {
                control = new LinkLabel();

                int  LeftSide = name.IndexOf( "%" ), RightSide = name.IndexOf( "%", LeftSide + 1 );
                if( info.Parameters == null )
                {
                    labelText = labelText.Replace( "%", "" );
                    ((LinkLabel)control).LinkArea = new LinkArea( LeftSide, RightSide - LeftSide - 1 );
                    control.ForeColor = Color.LightGray;
                    control.Font = _labelGreyedFont;
                }
                else
                {
                    if( info.Parameters is string )
                    {
                        if( !String.IsNullOrEmpty( info.Representation ) )
                            labelText = info.Representation;
                        else
                            labelText = (string)info.Parameters;
                    }
                    else
                    if( info.Parameters is IResourceList )
                        labelText = Resource2NamesList( (IResourceList)info.Parameters );
                    else
                        throw( new Exception( "Illegal resource type in internal buffer" ));

                    labelText = name.Substring( 0, LeftSide ) + labelText + name.Substring( RightSide + 1 );
                    ((LinkLabel)control).LinkArea = new LinkArea( LeftSide, labelText.Length - LeftSide - (name.Length - RightSide - 1));
                    control.Font = _labelFont;
                }

                ((LinkLabel)control).LinkClicked += LinkLabelClicked;
                ((LinkLabel)control).DoubleClick += LinkLabelDoubleClick;
            }
            control.Size = new Size( panel.Size.Width - 70, _cBaseHeight );
            control.Location = new Point( 5, baseYCoordinate );
            control.Text = labelText;
            control.FlatStyle = FlatStyle.System;
            control.Click += OnClickedInsideConditionControl;
            control.TextAlign = ContentAlignment.MiddleLeft;
            control.Tag = info;

            return( control );
        }

        private static Label  CreateORLabel( Panel panel, int baseYCoordinate )
        {
            Label   orLabel = new Label();
            orLabel.Text = "OR";
            orLabel.FlatStyle = FlatStyle.System;
            orLabel.Click += OnClickedInsideConditionControl;
            orLabel.TextAlign = ContentAlignment.MiddleCenter;
            orLabel.Size = new Size( panel.Size.Width - 70, _cBaseHeight );
            orLabel.Location = new Point( 5, baseYCoordinate );

            //  "OR"-labels have their own LabelInfo, which must not be
            //  matched with those of conditions with no criteria.
            orLabel.Tag = new LabelInfo();
            ((LabelInfo)orLabel.Tag).ParentPanel = panel;

            return orLabel;
        }

        private ImageListButton CreatePinButton( Panel panel, int baseYCoordinate, LabelInfo info )
        {
            ImageListButton button = new ImageListButton();
            button.ImageList = _pinIconImages;
            button.NormalImageIndex = 0;
            button.PressedImageIndex = 1;

            button.Location = new Point( panel.Size.Width - 54, baseYCoordinate );
            button.Size = new Size( 16, 16 );
            button.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            button.Tag = info;
            button.Click += pinButton_CheckedChanged;
            if( info.AssociatedResource.HasProp( LinkedPinnedSignProp ) )
            {
                button.NormalImageIndex = 1;
                button.PressedImageIndex = 0;
            }
            resTypeToolTip.SetToolTip( button, "Pin/Unpin Condition" );

            return button;
        }

        private ImageListButton CreateDeleteButton( Panel panel, int baseYCoordinate, LabelInfo info )
        {
            ImageListButton delButton = new ImageListButton();
            delButton.ImageList = _delIconImages;
            delButton.Location = new Point( panel.Size.Width - 32, baseYCoordinate );
            delButton.Size = new Size( 16, 16 );
            delButton.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            delButton.Tag = info;
            delButton.Click += delButton_Click;

            info.DelButton = delButton;
            resTypeToolTip.SetToolTip( delButton, "Clear/Delete Condition" );
            SetDeleteButtonIcon( info.AssociatedResource, info.Parameters, delButton );

            return delButton;
        }

        protected IResourceList CollectConditionsFromUser( Control.ControlCollection controls )
        {
            ArrayList     usedResources = CollectResourcesInControls( controls );
            IResourceList selectedConditions = RStore.EmptyResourceList;
            ChooseConditionForm form = new ChooseConditionForm( usedResources, CurrentResTypeDeep,
                                                                !MustHaveHeading || !CanAllRTWithNoConditions,
                                                                IsQueryConditionsAllowed );
            if( form.ShowDialog( this ) == DialogResult.OK )
                selectedConditions = form.SelectedConditions;

            return selectedConditions;
        }
        #endregion Add Conditions

        #region Remove Conditions
        private void delButton_Click(object sender, EventArgs e)
        {
            ImageListButton delButton = (ImageListButton) sender;
            Panel  panel = (Panel)delButton.Parent;
            LabelInfo commonInfo = (LabelInfo) delButton.Tag;
            IResource baseRes = commonInfo.AssociatedResource;

            //  For a condition/template which is not pinned, deletion is
            //  strightforward - remove it from the list.
            //  For a pinned condition/template we "disable" the condition
            //  if it is a template with the parameters set.
            if( delButton.NormalImageIndex == 0 )
            {
                if( commonInfo.Parameters == null )
                {
                    DeleteCondition( delButton );
                }
                else
                {
                    foreach( Control ctrl in panel.Controls )
                    {
                        if( ctrl.Tag == commonInfo && ctrl is LinkLabel )
                        {
                            ctrl.Font = _labelGreyedFont;
                            ctrl.ForeColor = Color.LightGray;
                            string pattern = baseRes.GetStringProp( Core.Props.Name );
                            ChangeLinkLabelName( pattern, "", (LinkLabel)ctrl );
                            commonInfo.Parameters = null;
                        }
                    }

                    SetDeleteButtonIcon( baseRes, commonInfo.Parameters, delButton );
                }
            }
            CheckFormConsistency();
        }

        private static void  DeleteCondition( ImageListButton delButton )
        {
            int    baseY = delButton.Top;
            int    maxHeigth = delButton.Height;
            Panel  panel = (Panel)delButton.Parent;
            LabelInfo commonInfo = (LabelInfo) delButton.Tag;

            int        inGroupCount = 0;
            ArrayList  controlsToRemove = new ArrayList();
            foreach( Control ctrl in panel.Controls )
            {
                LabelInfo info = (LabelInfo)ctrl.Tag;
                if( ctrl is Label || ctrl is LinkLabel )
                {
                    if( info.GroupIndex == commonInfo.GroupIndex )
                        inGroupCount++;
                }

                if( info == commonInfo )
                {
                    if( commonInfo.Parameters is IResourceList )
                        ((IResourceList)commonInfo.Parameters).Dispose();

                    maxHeigth = Math.Max( maxHeigth, ctrl.Size.Height );
                    controlsToRemove.Add( ctrl );
                }
            }

            foreach( Control ctrl in controlsToRemove )
                panel.Controls.Remove( ctrl );

            //  Move controls that are below the current one
            foreach( Control c in panel.Controls )
            {
                if( c.Location.Y > baseY )
                    c.Location = new Point( c.Location.X, c.Location.Y - maxHeigth - _cInterControlSpace );
            }

            //  Now check whether we need to delete an "OR"-label around this
            //  condition if it is last in the group:
            //  - previous "OR"-label if this group is not first,
            //  - subsequent "OR"-label if this group is first.
            if( inGroupCount == 1 )
            {
                Label  orLabel = null;
                int    pos;

                FindOrLabelToDelete( panel, baseY, (commonInfo.GroupIndex==0), out orLabel, out pos );
                if( orLabel != null )
                {
                    panel.Controls.Remove( orLabel );
                    foreach( Control ctrl in panel.Controls )
                    {
                        if( ctrl.Location.Y > pos )
                        {
                            ctrl.Location = new Point( ctrl.Location.X, ctrl.Location.Y - orLabel.Height - _cInterControlSpace );
                            LabelInfo info = (LabelInfo)ctrl.Tag;
                            if( info != null && info.GroupIndex > commonInfo.GroupIndex )
                                info.GroupIndex--;
                        }
                    }
                }
            }
        }

        private static void  FindOrLabelToDelete( Panel panel, int baseY, bool isFirst,
                                                  out Label orLabel, out int pos )
        {
            orLabel = null;
            if( isFirst )
            {
                pos = Int32.MaxValue;
                foreach( Control ctrl in panel.Controls )
                {
                    if( ctrl is Label && ctrl.Text == "OR" )
                    {
                        if( ctrl.Top < pos )
                        {
                            orLabel = (Label)ctrl;
                            pos = ctrl.Top;
                        }
                    }
                }
            }
            else
            {
                pos = Int32.MinValue;
                foreach( Control ctrl in panel.Controls )
                {
                    if( ctrl is Label && ctrl.Text == "OR" )
                    {
                        if( ctrl.Top > pos && ctrl.Top < baseY )
                        {
                            orLabel = (Label)ctrl;
                            pos = ctrl.Top;
                        }
                    }
                }
            }
        }
        #endregion Remove Conditions

        #region Pin/Unpin
        private void  pinButton_CheckedChanged(object sender, EventArgs e)
        {
            ImageListButton pinButton = (ImageListButton) sender;
            LabelInfo info = (LabelInfo)pinButton.Tag;
            ResourceProxy proxy = new ResourceProxy( info.AssociatedResource );

            if( !isPinnedCondition( proxy.Resource ) )
                proxy.SetProp( LinkedPinnedSignProp, true );
            else
                proxy.DeleteProp( LinkedPinnedSignProp );

            pinButton.NormalImageIndex = 1 - pinButton.NormalImageIndex;
            pinButton.PressedImageIndex = 1 - pinButton.PressedImageIndex;

            SetDeleteButtonIcon( proxy.Resource, info.Parameters, info.DelButton );

            CheckFormConsistency();
        }

        private void  SetDeleteButtonIcon( IResource template, object param, ImageListButton delButton )
        {
            if( isTemplate( template ) && isPinnedCondition( template ) && param == null )
            {
                delButton.NormalImageIndex = delButton.HotImageIndex = 2;
            }
            else
            {
                delButton.NormalImageIndex = 0;
                delButton.HotImageIndex = 1;
            }
        }
        #endregion Pin/Unpin

        #region Hide/Show
        private void  labelHideShowPanel_Click(object sender, EventArgs e)
        {
            ImageListButton button = (ImageListButton) ((JetLinkLabel) sender).Tag;
            HideShowPanel_Click( button, e );
        }
        private void  HideShowPanel_Click(object sender, EventArgs e)
        {
            ImageListButton button = (ImageListButton) sender;
            Panel           panel = (Panel) button.Tag;
            GroupBox        box = (GroupBox) button.Parent;
            JetLinkLabel    text = (JetLinkLabel) _labelsOfBoxes[ box ];
            Control[] controls = (Control[]) _labelsOfPanels[ box ];

            HideShowProcessing = true;
            if( panel.Visible )
            {
                panel.Visible = false;
                foreach( Control ctrl in controls )
                    ctrl.Visible = false;

                box.Tag = box.Height;
                box.Height = _cCollapsedPanelHeight;
                text.Text = "Show";

                //  temporarily let us shrink however we want, then update this info.
                MinimumSize = new Size( MinimumSize.Width, 100 );

                int delta = (int)box.Tag - _cCollapsedPanelHeight;
                Size = new Size( Width, Height - delta );

                if( (box.Anchor & AnchorStyles.Bottom) > 0 )
                    box.Top += delta;
            }
            else
            {
                //  If at least one panel is expanded, no reason to control
                //  the maximal size.
                MaximumSize = new Size( 1000, 1000 );

                int delta = (int)box.Tag - _cCollapsedPanelHeight;
                Size = new Size( Width, Height + delta);
                box.Height = (int)box.Tag;
                text.Text = "Hide";

                panel.Visible = true;
                foreach( Control ctrl in controls )
                    ctrl.Visible = true;

                if( (box.Anchor & AnchorStyles.Bottom) > 0 )
                    box.Top -= delta;

                CheckFormConsistency();
            }
            HideShowProcessing = false;

            //  Update the state in our INI file.
            string section = GetFormSettingsSection();
            if( panel == panelExceptions )
                Core.SettingStore.WriteBool( section, _cOpenExceptionsKey, panel.Visible );
            else
                Core.SettingStore.WriteBool( section, _cOpenConditionsKey, panel.Visible );

            //  Change the state of our graphical buttons
            button.NormalImageIndex = 1 - button.NormalImageIndex;
            button.PressedImageIndex = 1 - button.NormalImageIndex;
            button.HotImageIndex = button.NormalImageIndex + 2;

            AdjustMinimalSize();
        }
        #endregion Hide/Show

        #region LinkLabel Editing
        //---------------------------------------------------------------------
        //  Process editing of the changeable part of the LinkLabel
        //---------------------------------------------------------------------
        private void LinkLabelDoubleClick(object sender, EventArgs e)
        {
            LinkLabelClicked( sender, null );
        }
        private void LinkLabelClicked( object sender, LinkLabelLinkClickedEventArgs e )
        {
            EditLinkLabelParameter( (LinkLabel)sender );
            CheckFormConsistency();
        }

        protected void  EditLinkLabelParameter( LinkLabel control )
        {
            LabelInfo   info = (LabelInfo)control.Tag;
            IResource   template = info.AssociatedResource;
            string      valueObjName = template.GetStringProp( "ChooseFromResourceType" );
            string      baseProp = template.GetStringProp( "ApplicableToProp" );
            string      labelText = control.Text;
            string      pattern = template.GetStringProp( "Name" );
            ConditionOp     templateOp = (ConditionOp)template.GetProp( "ConditionOp" );
            IResourceList   currentSelection = RStore.EmptyResourceList;
            ITemplateParamUIHandler uiHandler = FilterRegistry.GetUIHandler( template.GetStringProp("Name") );

            string  currVal = ExtractParameterString( labelText, pattern );

            if( templateOp == ConditionOp.Eq && ResourceTypeHelper.IsDateProperty( baseProp ) )
            {
                if( currVal == ExtractDefaultParameter( pattern ) )
                    currVal = "";

                EditTimeSpanConditionForm form = new EditTimeSpanConditionForm( currVal );
                if( form.ShowDialog( this ) == DialogResult.OK )
                {
                    info.Parameters = form.TimeSpanDescription;
                    ChangeLinkLabelName( pattern, form.TimeSpanDescription, control );
                }
                form.Dispose();
            }
            else
            if( templateOp == ConditionOp.In )
            {
                if( valueObjName != FilterRegistry.ExternalFileTag &&
                    valueObjName != FilterRegistry.ExternalDirTag )
                {
                    if( currVal != ExtractDefaultParameter( pattern ) )
                    {
                        Debug.Assert( info.Parameters != null, "After any template editing, res list can not be null" );
                        currentSelection = (IResourceList)info.Parameters;
                    }

                    IResourceList objects = null;
                    IResource     selection;

                    string caption = "Select " + Core.ResourceStore.ResourceTypes [valueObjName].DisplayName;
                    if( template.HasProp( "IsSingleSelection" ))
                    {
                        selection = Core.UIManager.SelectResource( this, valueObjName, caption,
                                                                 (currentSelection.Count > 0)? currentSelection[ 0 ] : null );
                        if( selection != null )
                            objects = selection.ToResourceList();
                    }
                    else
                        objects = Core.UIManager.SelectResources( this, valueObjName, caption, currentSelection );

                    //  objects.Count == 0 means that user deselected all the
                    //  previosly checked items.
                    if( objects != null )
                    {
                        string  resultValues = Resource2NamesList( objects );
                        info.Parameters = (objects.Count > 0) ? objects : null;
                        ChangeLinkLabelName( pattern, resultValues, control );
                        currentSelection.Dispose();
                    }
                }
                else
                {
                    if( currVal != ExtractDefaultParameter( pattern ) )
                    {
                        Debug.Assert( info.Parameters != null, "After any template editing, res list can not be null" );
                    }

                    if( valueObjName == FilterRegistry.ExternalFileTag )
                    {
                        string  filterMask = template.GetStringProp( "ApplicableToProp" );

                        OpenFileDialog dlg = new OpenFileDialog();
                        dlg.Filter = filterMask;
                        if( dlg.ShowDialog() == DialogResult.OK )
                        {
                            info.Parameters = dlg.FileName;
                            ChangeLinkLabelName( pattern, dlg.FileName, control );
                        }
                    }
                    else
                    {
                        FolderBrowserDialog dlg = new FolderBrowserDialog();
                        dlg.ShowNewFolderButton = true;
                        if( info.Parameters != null )
                        {
                            dlg.SelectedPath = (string)info.Parameters;
                        }
                        if( dlg.ShowDialog() == DialogResult.OK )
                        {
                            info.Parameters = dlg.SelectedPath;
                            ChangeLinkLabelName( pattern, dlg.SelectedPath, control );
                        }
                    }
                }
            }
            else
            if( templateOp == ConditionOp.InRange || templateOp == ConditionOp.Gt ||
                templateOp == ConditionOp.Lt )
            {
                if( currVal == ExtractDefaultParameter( pattern ) )
                    currVal = "";

                int  minValue = Int32.MinValue, maxValue = Int32.MaxValue;
                try
                {
                    if( template.HasProp( "ConditionValLower" ))
                        minValue = Int32.Parse( template.GetStringProp( "ConditionValLower" ));
                    if( template.HasProp( "ConditionValUpper" ))
                        maxValue = Int32.Parse( template.GetStringProp( "ConditionValUpper" ));
                }
                catch( Exception )
                {}

                IntIntervalForm form = new IntIntervalForm( currVal, minValue, maxValue );
                if( form.ShowDialog( this ) == DialogResult.OK )
                {
                    info.Parameters = form.IntervalDescription;
                    ChangeLinkLabelName( pattern, form.IntervalDescription, control );
                }
                form.Dispose();
            }
            else
            if( templateOp == ConditionOp.QueryMatch || templateOp == ConditionOp.Eq || templateOp == ConditionOp.Has )
            {
                string result = null, showStr = null;
                currVal = (string) info.Parameters;

                if( uiHandler != null )
                {
                    IStringTemplateParamUIHandler stringUIHandler = uiHandler as IStringTemplateParamUIHandler;
                    stringUIHandler.Template = template;
                    stringUIHandler.CurrentValue = currVal;
                    if( uiHandler.ShowUI( this ) == DialogResult.OK )
                    {
                        result = stringUIHandler.Result;
                        showStr = stringUIHandler.DisplayString;
                    }
                }
                else
                if( templateOp == ConditionOp.QueryMatch )
                    result = Core.UIManager.InputString( "Search Query", "Enter the words or phrase to search for:",
                                                          currVal, null, this, 0, _cQueryHelpTopic );
                else
                if( templateOp == ConditionOp.Has )
                    result = Core.UIManager.InputString( "Enter Value", "Enter string to search:", currVal, null, this );
                else
                    result = Core.UIManager.InputString( "Enter Value", "Enter the value:", currVal, null, this );

                if( !String.IsNullOrEmpty( result ) )
                {
                    info.Parameters = result;
                    info.Representation = showStr;

                    ChangeLinkLabelName( pattern, showStr ?? result, control );
                }
            }

            SetDeleteButtonIcon( template, info.Parameters, info.DelButton );
        }

        private void  ChangeLinkLabelName( string pattern, string subst, LinkLabel control )
        {
            string  result;
            int     leftMargin = pattern.IndexOf( "%" ),
                    rightMargin = pattern.IndexOf( "%", leftMargin + 1 );

            if( subst.Length > 0 )
            {
                control.Text = result = pattern.Substring( 0, leftMargin ) + subst + pattern.Substring( rightMargin + 1 );
                control.LinkArea = new LinkArea( leftMargin, result.Length - leftMargin - (pattern.Length - rightMargin - 1));
                control.ForeColor = Color.Black;
                control.Font = _labelFont;
            }
            else
            {
                control.Text = pattern.Replace( "%", "" );
                control.LinkArea = new LinkArea( leftMargin, rightMargin - leftMargin - 1 );
                control.ForeColor = Color.LightGray;
                control.Font = _labelGreyedFont;
            }
            ResizeControl( control );
        }

        private void  ResizeControl( Control control )
        {
            if( control is Label || control is LinkLabel )
            {
                Panel   parentPanel = ((LabelInfo)control.Tag).ParentPanel;
                float   fontHeight = _labelFont.GetHeight( Graphics.FromHwnd( control.Handle ) );
                int     newWidth = parentPanel.ClientRectangle.Width - 70;
                int     currWidth = (int)Graphics.FromHwnd( control.Handle ).MeasureString( control.Text, _labelFont ).Width;

                //  NB: the string width computation (line above) is often
                //      sucks giving us lesser width than it is really. Thus
                //      we add some penalty.
                currWidth += 20;

                int     lines = currWidth / newWidth + 1;
                int     newHeight = ( lines == 1 ) ? _cBaseHeight : (int)(lines * ( fontHeight + 2 ));
                int     delta = newHeight - control.Size.Height;

                control.Size = new Size( newWidth, newHeight );
                foreach( Control ctrl in parentPanel.Controls )
                {
                    if( ctrl.Top > control.Location.Y )
                        ctrl.Top = ctrl.Top + delta;
                }
            }
        }
        #endregion LinkLabel Editing

        #region Label Highlighting
        //---------------------------------------------------------------------
        //  Methods which deal with active condition highlighting
        //---------------------------------------------------------------------
        private static void  OnClickedInsideConditionControl(object sender, EventArgs e)
        {
/*
            ((Control)sender).Focus();
            HighlightControl( (Label)sender );
*/
        }

        private static int GetHighlightedControlIndex( Control.ControlCollection controls )
        {
            for( int i = 0; i < controls.Count; i++ )
            {
                if( controls[ i ].BackColor == SystemColors.Highlight )
                    return( i );
            }
            return( -1 );
        }

        protected static bool isThereActiveControl( Control.ControlCollection controls )
        {
            int  highlighted = GetHighlightedControlIndex( controls );
            return( highlighted != -1 );
        }
        #endregion Label Highlighting

        #region CanConstructView
        protected void  CheckFormConsistency()
        {
            okButton.Enabled = CanConstructView();

            //  Show different set of available link labels depending on
            //  can we add a condition by OR in current config.

            int  readyConditionsCount = CountReadyConditions( panelConditions );
            bool what2show = (readyConditionsCount == 0);
            labelAddCondition.Visible = what2show && panelConditions.Visible;
            labelAdd.Visible = labelAddConditionByAnd.Visible = labelAddConditionByOr.Visible = !what2show && panelConditions.Visible;

            SetProperFontToLabels();
        }
        private bool  CanConstructView()
        {
            SetErrorText( null, null );

            int  readyConditionsCount = CountReadyConditions( panelConditions ),
                 readyExceptionsCount = CountReadyConditions( panelExceptions );

            //  Some forms require that name of a view/rule/etc must be present.
            if( MustHaveHeading && _editHeading.Text.Trim().Length == 0 )
            {
                SetErrorText( "Please specify " + FormTitleString, _editHeading );
                return false;
            }

            if( !MustHaveHeading && _editHeading.Text.Trim().Length == 0 &&
                (readyConditionsCount + readyExceptionsCount) == 0 )
            {
                SetErrorText( "No condition is chosen and no " + FormTitleString + " is specified", _editHeading );
                return false;
            }

            foreach( Control ctrl in panelConditions.Controls )
            {
                LabelInfo info = (LabelInfo)ctrl.Tag;
                if( isTemplate( info.AssociatedResource ) && info.Parameters == null &&
                    !info.AssociatedResource.HasProp( LinkedPinnedSignProp ))
                {
                    string text = info.AssociatedResource.GetPropText( Core.Props.Name );
                    SetErrorText( "Condition [" + text.Replace( "%", "" ) + "] is not instantiated", _lblConditionsTitle );
                    return( false );
                }
            }
            foreach( Control ctrl in panelExceptions.Controls )
            {
                LabelInfo info = (LabelInfo)ctrl.Tag;
                if( isTemplate( info.AssociatedResource ) && info.Parameters == null &&
                    !info.AssociatedResource.HasProp( LinkedPinnedSignProp ))
                {
                    string text = info.AssociatedResource.GetPropText( Core.Props.Name );
                    SetErrorText( "Exception [" + text.Replace( "%", "" ) + "] is not instantiated", _lblExceptionsTitle );
                    return( false );
                }
            }

            if( !CanAllRTWithNoConditions && CurrentResTypeDeep == null && readyConditionsCount == 0 )
            {
                SetErrorText( "View with no conditions can not be applied to All Resource Types", _lblConditionsTitle );
                return false;
            }

            string  errorText = null;
            Control errorControl = null;
            bool externalOk = (_externalChecker != null) ? _externalChecker( out errorText, out errorControl ) : true;
            if( !externalOk )
            {
                SetErrorText( errorText, errorControl );
            }

            return( externalOk );
        }

        private void SetProperFontToLabels()
        {
            int  readyConditionsCount = CountReadyConditions( panelConditions ),
                 readyExceptionsCount = CountReadyConditions( panelExceptions );

            Font set = (readyConditionsCount > 0) ? _labelBoldFont : _labelFont;
            if( _lblConditionsTitle.Font != set )
                _lblConditionsTitle.Font = set;

            string text = (readyConditionsCount == 0) ? "Conditions" : "Conditions (" + readyConditionsCount + ")";
            if (_lblConditionsTitle.Text != text)
                _lblConditionsTitle.Text = text;

            set = (readyExceptionsCount > 0) ? _labelBoldFont : _labelFont;
            if( _lblExceptionsTitle.Font != set )
                _lblExceptionsTitle.Font = set;

            text = (readyExceptionsCount == 0) ? "Exceptions" : "Exceptions (" + readyExceptionsCount + ")";
            if( _lblExceptionsTitle.Text != text )
                _lblExceptionsTitle.Text = text;
        }
        #endregion CanConstructView

        #region Misc
        //---------------------------------------------------------------------
        //  Miscellaneous routines
        //---------------------------------------------------------------------
        protected static bool areNamesDiffer( string newName, string oldName )
        {
            return( oldName == null || newName.ToLower() != oldName.ToLower() );
        }

        protected bool isResourceNewAndNameExist( string resType )
        {
            return( areNamesDiffer( _editHeading.Text, InitialName ) &&
                   ( RStore.FindResources( resType, Core.Props.Name, _editHeading.Text ).Count > 0 ));
        }

        protected static bool isTemplate( IResource res )
        {
            //  NB: "OR"-labels are not associated with any resource.
            return( res != null && (res.Type == FilterManagerProps.ConditionTemplateResName ||
                                    res.Type == FilterManagerProps.RuleActionTemplateResName ));
        }

        protected bool  isPinnedCondition( IResource condition )
        {
            return condition.HasProp( LinkedPinnedSignProp );
        }

        protected static string  Resource2NamesList( IResourceList objects )
        {
            string  result = "";
            foreach( IResource res in objects )
                result += res.DisplayName + ", ";
            if( result.Length >= 2 )
                result = result.Remove( result.Length - 2, 2 );
            return( result );
        }

        protected static IResourceList  Vector2Reslist( IResource[] vector )
        {
            IResourceList list = Core.ResourceStore.EmptyResourceList;
            if( vector != null )
            {
                foreach( IResource res in vector )
                    list = list.Union( res.ToResourceList() );
            }
            return list;
        }

        protected static ArrayList ResList2ArrayList( IResourceList list )
        {
            ArrayList result = new ArrayList();
            foreach( IResource res in list )
                result.Add( res );
            return result;
        }

        protected static string  ExtractParameterString( string labelText, string pattern )
        {
            int     leftMargin = pattern.IndexOf( "%" ),
                    rightMargin = pattern.IndexOf( "%", leftMargin + 1 );
            return( labelText.Substring( leftMargin, labelText.Length - leftMargin - (pattern.Length - rightMargin - 1)) );
        }

        protected static string  ExtractDefaultParameter( string pattern )
        {
            int     leftMargin = pattern.IndexOf( "%" ),
                    rightMargin = pattern.IndexOf( "%", leftMargin + 1 );
            return( pattern.Substring( leftMargin + 1, rightMargin - leftMargin - 1 ));
        }

        protected static void FreeConditionLists( Panel.ControlCollection controls )
        {
            foreach( Control ctrl in controls )
            {
                if( ((LabelInfo)ctrl.Tag).Parameters is IResourceList )
                    ((IResourceList)((LabelInfo)ctrl.Tag).Parameters).Dispose();
            }
        }

        protected static ArrayList CreateEmptyList( int count, int groupInitializer )
        {
            ArrayList result = new ArrayList();
            for( int i = 0; i < count; i++ )
            {
                LabelInfo info = new LabelInfo();
                info.GroupIndex = groupInitializer;
                result.Add( info );
            }
            return result;
        }

        protected static ArrayList CollectResourcesInControls( Panel.ControlCollection controls )
        {
            ArrayList result = new ArrayList();
            foreach( Control ctrl in controls )
            {
                if( !( ctrl is Button ) )
                {
                    IResource res = ((LabelInfo)ctrl.Tag).AssociatedResource;
                    if( res != null )
                        result.Add( res );
                }
            }
            return result;
        }

        protected static IResource[] JoinLists( IResource[] list1, IResource[] list2 )
        {
            IResource[] joined = new IResource[ list1.Length + list2.Length ];
            Array.Copy( list1, joined, list1.Length );
            Array.Copy( list2, 0, joined, list1.Length, list2.Length );
            return joined;
        }

        public static string[] ReformatTypes( string resTypeDeep )
        {
            if( resTypeDeep != null )
                return resTypeDeep.Split( '|', '#' );
            return null;
        }

        protected static bool isTypeConforms( string resTypes, IResource cond )
        {
            ArrayList condTypes = new ArrayList();
            string types = cond.GetStringProp( Core.Props.ContentType );
            if( types != null )
            {
                string[] arr = types.Split( '|' );
                condTypes.AddRange( arr );
            }
            types = cond.GetStringProp( "ContentLinks" );
            if( types != null )
            {
                string[] arr = types.Split( '|' );
                condTypes.AddRange( arr );
            }

            foreach( string type in condTypes )
            {
                if( resTypes.IndexOf( type ) == -1 )
                    return false;
            }
            return true;
        }

        private static int  CountReadyConditions( Panel panel )
        {
            int  count = 0;
            foreach( Control ctrl in panel.Controls )
            {
                if( ctrl is LinkLabel )
                {
                    LinkLabel label = ctrl as LinkLabel;
                    LabelInfo info = (LabelInfo) label.Tag;
                    if( info.Parameters != null )
                        count++;
                }
                else
                if( ctrl is Label )
                    count++;
            }
            return count;
        }

        protected static void ShiftControlsV( int shift, params Control[] ctrls )
        {
            foreach( Control ctrl in ctrls )
                ctrl.Top = ctrl.Top + shift;
        }

        protected void SetErrorText( string text, Control ctrl )
        {
            SetErrorText( text );

            if ( ctrl != null )
                _errorProvider.SetError( ctrl, text );
            else
                _errorProvider.Clear();
        }

        protected void  SetErrorText( string text )
        {
            _lblErrorText.Visible = !String.IsNullOrEmpty( text );
            if( _lblErrorText.Visible )
            {
                int charsFilled, linesFilled;
                Graphics helper = Graphics.FromHwnd( _lblErrorText.Handle );

                helper.MeasureString( text, _labelFont, new SizeF( _lblErrorText.Width, 16.0f), new StringFormat(), out charsFilled, out linesFilled );
                if (charsFilled < text.Length)
                    _lblErrorText.Text = text.Substring(0, charsFilled - 3) + "...";
                else
                    if (_lblErrorText.Text != text)
                        _lblErrorText.Text = text;
                _lblErrorText.Tag = text;
            }
        }

        protected virtual void AdjustMinimalSize()
        {
            int formMinimalHeight = Height;
            if( _boxConditions.Height == _cCollapsedPanelHeight && _boxExceptions.Height == _cCollapsedPanelHeight )
            {
                //  Forbid also to maximize the dialog.
                MaximumSize = new Size( 1000, Height );
            }
            else
            {
                if( _boxConditions.Height != _cCollapsedPanelHeight )
                    formMinimalHeight -= ( _boxConditions.Height - _cMinimalPanelHeight );

                if( _boxExceptions.Height != _cCollapsedPanelHeight )
                    formMinimalHeight -= ( _boxExceptions.Height - _cMinimalPanelHeight );
            }
            MinimumSize = new Size( 315, formMinimalHeight );
        }
        #endregion Misc

        #region Common events handlers
        protected void viewNameText_TextChanged(object sender, EventArgs e)
        {
            CheckFormConsistency();
        }

        protected void panel_Resize(object sender, EventArgs e)
        {
            Panel panel = (Panel)sender;
            for( int i = 0; i < panel.Controls.Count; i++ )
                ResizeControl( panel.Controls[ i ] );

            if( _lblErrorText.Visible )
                SetErrorText( (string) _lblErrorText.Tag );
        }

        /**
         * To avoid problems with auto-scaling, set up anchoring of the controls
         * on the first tab page after the form has been initially loaded, and
         * explicitly set size of all controls.
         */

        private void OnFormVisibleChanged( object sender, EventArgs e )
        {
            if( Visible )
            {
                CheckFormConsistency();

                string section = GetFormSettingsSection();
                if( !Core.SettingStore.ReadBool( section, _cOpenExceptionsKey, true ) )
                    HideShowPanel_Click( buttonHideShowExceptions, null );

                if( !Core.SettingStore.ReadBool( section, _cOpenConditionsKey, true ) )
                    HideShowPanel_Click( buttonHideShowConditions, null );

                AdjustMinimalSize();
            }
            RestoreSettings();
        }

        private void CommonDialogStore_SizeChanged(object sender, EventArgs e)
        {
            if( !HideShowProcessing )
            {
                //  Delta value is computed using the fact that one pane is
                //  Top-anchored (and thus can not be moved) while the second
                //  one is bottom-anchored, thus is moved along the lower border.
                int delta = _boxExceptions.Top - _boxConditions.Bottom - _cPanelsInterval;
                int halfExcp, halfConds;

                //  In the case of form enlargement, both panes are resized
                //  symmetrically, otherwise both panes are resized in asymmetric
                //  manner - the larger pane is resized with greater velocity;
                if( delta > 0 )
                {
                    halfConds = halfExcp = (delta + 1) / 2;
                }
                else
                {
                    int     sumSize = _boxConditions.Height + _boxExceptions.Height;
                    halfExcp = (int) (((double) (delta * _boxExceptions.Height)) / (double) sumSize );
                    if( halfExcp == 0 && _boxExceptions.Height > _boxConditions.Height )
                    {
                        halfConds = halfExcp;
                        halfExcp = delta - halfConds;
                    }
                    else
                        halfConds = delta - halfExcp;
                }

                if( _boxConditions.Height != _cCollapsedPanelHeight && _boxExceptions.Height != _cCollapsedPanelHeight )
                {
                    _boxConditions.Height += halfConds;
                    _boxExceptions.Height += halfExcp;
                    _boxExceptions.Top -= halfExcp;
                }
                else
                if( _boxConditions.Height != _cCollapsedPanelHeight )
                {
                    _boxConditions.Height += delta;
                }
                else
                if( _boxExceptions.Height != _cCollapsedPanelHeight )
                {
                    _boxExceptions.Top -= delta;
                    _boxExceptions.Height += delta;
                }
            }
        }

        private void helpButton_Click(object sender, EventArgs e)
        {
            if( _referenceTopic != null )
            {
                Help.ShowHelp( this, Core.UIManager.HelpFileName, _referenceTopic );
            }
        }
        #endregion Common events handlers
    }
}
