// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
	public class WizardForm : DialogBase
	{
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button _backButton;
        private System.Windows.Forms.Button _nextButton;
        private System.Windows.Forms.Button _cancelButton;
        private System.Windows.Forms.Button _btnHelp;
        public System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.PictureBox _pictureBox;
        private System.Windows.Forms.Panel _optionsPanel;
        private System.Windows.Forms.Panel _panePanel;
        private JetBrains.Omea.GUIControls.CustomStylePanel _paneContentPanel;
        private JetBrains.Omea.GUIControls.CustomStylePanel _paneControlPanel;
        public System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label _headerLabel;
        private System.Windows.Forms.Splitter splitter1;
        private JetBrains.Omea.GUIControls.CustomStylePanel _listPanel;
        private System.Windows.Forms.Label _explanatoryLabel;
        private System.Windows.Forms.Label _welcomeLabel;
		private System.ComponentModel.Container components = null;

        public WizardForm( string caption ) : this( caption, "", "" ) {}

		public WizardForm( string caption, string welcomeText, string explanatoryText )
		{
			InitializeComponent();
            RestoreSettings();
            Text = caption;
            _welcomeLabel.Text = welcomeText;
            _explanatoryLabel.Text = explanatoryText;
            _minPaneIndex = ( welcomeText.Length + explanatoryText.Length == 0 ) ? 0 : -1;
            _currentIndex = _maxPaneIndex = _minPaneIndex;
            _panePanel.Dock = DockStyle.Fill;
            _headerLabel.FlatStyle = FlatStyle.Standard;
            _cancelButton.DialogResult = DialogResult.None;
            _panesQueue = new PriorityQueue();
            _panesList = new ArrayList();
            _cachedPanes = new HashMap();
            _controlPool = new ControlPool( _listPanel, new ControlPoolCreateDelegate( CreateJetLinkLabel ) );
        }

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

        public void RegisterPane( int priority, AbstractWizardPane pane )
        {
            _panesQueue.Push( -priority, pane );
            OnRegisterPane( pane );
        }

        public void DeregisterPane( AbstractWizardPane pane )
        {
            foreach( PriorityQueue.QueueEntry e in _panesQueue )
            {
                if( e.Value.Equals( pane ) )
                {
                    _panesQueue.Remove( e );
                    OnDeregisterPane( pane );
                    break;
                }
            }
        }

        public AbstractWizardPane CurrentPane
        {
            get
            {
                if( _currentIndex >= 0 && _currentIndex < _panesList.Count )
                {
                    return (AbstractWizardPane)(((Pair) _panesList[ _currentIndex ]).Second);
                }
                return null;
            }
        }

        public virtual bool ConfirmCancel()
        {
            return true;
        }

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(WizardForm));
            this.panel1 = new System.Windows.Forms.Panel();
            this._backButton = new System.Windows.Forms.Button();
            this._nextButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._btnHelp = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this._pictureBox = new System.Windows.Forms.PictureBox();
            this._optionsPanel = new System.Windows.Forms.Panel();
            this._panePanel = new System.Windows.Forms.Panel();
            this._paneContentPanel = new JetBrains.Omea.GUIControls.CustomStylePanel();
            this._paneControlPanel = new JetBrains.Omea.GUIControls.CustomStylePanel();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this._headerLabel = new System.Windows.Forms.Label();
            this.splitter1 = new System.Windows.Forms.Splitter();
            this._listPanel = new JetBrains.Omea.GUIControls.CustomStylePanel();
            this._explanatoryLabel = new System.Windows.Forms.Label();
            this._welcomeLabel = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this._optionsPanel.SuspendLayout();
            this._panePanel.SuspendLayout();
            this._paneContentPanel.SuspendLayout();
            this.SuspendLayout();
            //
            // panel1
            //
            this.panel1.Controls.Add(this._backButton);
            this.panel1.Controls.Add(this._nextButton);
            this.panel1.Controls.Add(this._cancelButton);
            this.panel1.Controls.Add(this._btnHelp);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.panel1.Location = new System.Drawing.Point(0, 398);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(552, 48);
            this.panel1.TabIndex = 1;
            //
            // _backButton
            //
            this._backButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._backButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._backButton.Location = new System.Drawing.Point(216, 12);
            this._backButton.Name = "_backButton";
            this._backButton.TabIndex = 0;
            this._backButton.Text = "< &Back";
            this._backButton.Click += new System.EventHandler(this._backButton_Click);
            //
            // _nextButton
            //
            this._nextButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._nextButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._nextButton.Location = new System.Drawing.Point(296, 12);
            this._nextButton.Name = "_nextButton";
            this._nextButton.TabIndex = 1;
            this._nextButton.Text = "&Next >";
            this._nextButton.Click += new System.EventHandler(this._nextButton_Click);
            //
            // _cancelButton
            //
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelButton.Location = new System.Drawing.Point(384, 12);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.TabIndex = 2;
            this._cancelButton.Text = "Cancel";
            this._cancelButton.Click += new System.EventHandler(this._cancelButton_Click);
            //
            // _btnHelp
            //
            this._btnHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this._btnHelp.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnHelp.Location = new System.Drawing.Point(468, 12);
            this._btnHelp.Name = "_btnHelp";
            this._btnHelp.TabIndex = 2;
            this._btnHelp.Text = "Help";
            this._btnHelp.Click += new System.EventHandler(this._btnHelp_Click);
            //
            // groupBox1
            //
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.groupBox1.Location = new System.Drawing.Point(0, 394);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(552, 4);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            //
            // _pictureBox
            //
            this._pictureBox.BackColor = System.Drawing.SystemColors.Window;
            this._pictureBox.Dock = System.Windows.Forms.DockStyle.Left;
            this._pictureBox.Image = Image.FromStream( Assembly.GetExecutingAssembly().GetManifestResourceStream( "GUIControls.Icons.WizardReader.png" ) );
            this._pictureBox.Location = new System.Drawing.Point(0, 0);
            this._pictureBox.Name = "_pictureBox";
            this._pictureBox.Size = new System.Drawing.Size(168, 394);
            this._pictureBox.TabIndex = 3;
            this._pictureBox.TabStop = false;
            //
            // _optionsPanel
            //
            this._optionsPanel.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this._optionsPanel.Controls.Add(this._panePanel);
            this._optionsPanel.Controls.Add(this._explanatoryLabel);
            this._optionsPanel.Controls.Add(this._welcomeLabel);
            this._optionsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._optionsPanel.Location = new System.Drawing.Point(168, 0);
            this._optionsPanel.Name = "_optionsPanel";
            this._optionsPanel.Size = new System.Drawing.Size(384, 394);
            this._optionsPanel.TabIndex = 4;
            //
            // _panePanel
            //
            this._panePanel.BackColor = System.Drawing.SystemColors.Control;
            this._panePanel.Controls.Add(this._paneContentPanel);
            this._panePanel.Controls.Add(this.splitter1);
            this._panePanel.Controls.Add(this._listPanel);
            this._panePanel.Location = new System.Drawing.Point(16, 240);
            this._panePanel.Name = "_panePanel";
            this._panePanel.Size = new System.Drawing.Size(328, 136);
            this._panePanel.TabIndex = 2;
            //
            // _paneContentPanel
            //
            this._paneContentPanel.Controls.Add(this._paneControlPanel);
            this._paneContentPanel.Controls.Add(this.groupBox2);
            this._paneContentPanel.Controls.Add(this._headerLabel);
            this._paneContentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this._paneContentPanel.Location = new System.Drawing.Point(176, 0);
            this._paneContentPanel.Name = "_paneContentPanel";
            this._paneContentPanel.Size = new System.Drawing.Size(152, 136);
            this._paneContentPanel.TabIndex = 2;
            this._paneContentPanel.Paint += new System.Windows.Forms.PaintEventHandler(this._paneContentPanel_Paint);
            //
            // _paneControlPanel
            //
            this._paneControlPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._paneControlPanel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this._paneControlPanel.Location = new System.Drawing.Point(8, 60);
            this._paneControlPanel.Name = "_paneControlPanel";
            this._paneControlPanel.Size = new System.Drawing.Size(136, 68);
            this._paneControlPanel.TabIndex = 6;
            //
            // groupBox2
            //
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Location = new System.Drawing.Point(2, 52);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(166, 4);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            //
            // _headerLabel
            //
            this._headerLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._headerLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._headerLabel.Location = new System.Drawing.Point(2, 0);
            this._headerLabel.Name = "_headerLabel";
            this._headerLabel.Size = new System.Drawing.Size(166, 48);
            this._headerLabel.TabIndex = 4;
            this._headerLabel.Text = "Header";
            this._headerLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // splitter1
            //
            this.splitter1.BackColor = System.Drawing.SystemColors.ControlLight;
            this.splitter1.Location = new System.Drawing.Point(172, 0);
            this.splitter1.Name = "splitter1";
            this.splitter1.Size = new System.Drawing.Size(4, 136);
            this.splitter1.TabIndex = 1;
            this.splitter1.TabStop = false;
            //
            // _listPanel
            //
            this._listPanel.Dock = System.Windows.Forms.DockStyle.Left;
            this._listPanel.Location = new System.Drawing.Point(0, 0);
            this._listPanel.Name = "_listPanel";
            this._listPanel.Size = new System.Drawing.Size(172, 136);
            this._listPanel.TabIndex = 0;
            this._listPanel.Resize += new System.EventHandler(this._listPanel_Resize);
            this._listPanel.Paint += new System.Windows.Forms.PaintEventHandler(this._listPanel_Paint);
            //
            // _explanatoryLabel
            //
            this._explanatoryLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._explanatoryLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._explanatoryLabel.Location = new System.Drawing.Point(20, 152);
            this._explanatoryLabel.Name = "_explanatoryLabel";
            this._explanatoryLabel.Size = new System.Drawing.Size(320, 72);
            this._explanatoryLabel.TabIndex = 1;
            this._explanatoryLabel.Text = "This wizard helps you to configure what you want";
            //
            // _welcomeLabel
            //
            this._welcomeLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._welcomeLabel.ForeColor = System.Drawing.Color.FromArgb(((System.Byte)(87)), ((System.Byte)(77)), ((System.Byte)(162)));
            this._welcomeLabel.Location = new System.Drawing.Point(20, 80);
            this._welcomeLabel.Name = "_welcomeLabel";
            this._welcomeLabel.Size = new System.Drawing.Size(320, 48);
            this._welcomeLabel.TabIndex = 0;
            this._welcomeLabel.Text = "Welcome to Omea Wizard";
            //
            // WizardForm
            //
            this.AcceptButton = this._nextButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(552, 446);
            this.ControlBox = false;
            this.Controls.Add(this._optionsPanel);
            this.Controls.Add(this._pictureBox);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.panel1);
            this.MinimumSize = new System.Drawing.Size(560, 500);
            this.Name = "WizardForm";
            this.ShowInTaskbar = true;
            this.Text = "WizardFrom";
            this.HelpRequested += new System.Windows.Forms.HelpEventHandler(this.WizardFrom_HelpRequested);
            this.panel1.ResumeLayout(false);
            this._optionsPanel.ResumeLayout(false);
            this._panePanel.ResumeLayout(false);
            this._paneContentPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }
		#endregion

        #region implementation details

        private int                 _currentIndex;
        private int                 _maxPaneIndex;
        private int                 _minPaneIndex;
        private PriorityQueue       _panesQueue;
        private ArrayList           _panesList;
        private HashMap             _cachedPanes;
        private ControlPool         _controlPool;

        internal void OnRegisterPane( AbstractWizardPane pane )
        {
            InvalidateView();
        }

        internal void OnDeregisterPane( AbstractWizardPane pane )
        {
            AbstractWizardPane currentPane = CurrentPane;
            if( currentPane != null && currentPane.Equals( pane ) )
            {
                _currentIndex = _minPaneIndex;
            }
            _cachedPanes.Remove( pane );
            InvalidateView();
        }

        internal void OnPaneGotValid( AbstractWizardPane pane )
        {
            AbstractWizardPane currentPane = CurrentPane;
            if( currentPane != null && currentPane.Equals( pane ) )
            {
                _nextButton.Enabled = true;
            }
        }

        internal void OnPaneGotInvalid( AbstractWizardPane pane )
        {
            AbstractWizardPane currentPane = CurrentPane;
            if( currentPane != null && currentPane.Equals( pane ) )
            {
                _nextButton.Enabled = false;
            }
        }

        private Control GetControl( AbstractWizardPane pane )
        {
            Control result = (Control) _cachedPanes[ pane ];
            if( result == null )
            {
                result = pane.Pane;
                DialogBase.AdjustContolProperties( result );
                pane.ShowPane();
                _cachedPanes[ pane ] = result;
            }
            return result;
        }

        private void InvalidateView()
        {
            LeaveCurrentPane( PaneChangeReason.Invalidate );
            foreach( Pair pair in _panesList )
            {
                AbstractWizardPane pane = (AbstractWizardPane) pair.Second;
                pane.PaneGotValid -= OnPaneGotValid;
                pane.PaneGotInvalid -= OnPaneGotInvalid;
                pane.PaneRegistered -= OnRegisterPane;
                pane.PaneDeregistered -= OnDeregisterPane;
            }
            _panesList.Clear();
            InvalidateView( _panesQueue, 0 );
            EnterCurrentPane( PaneChangeReason.Invalidate );
        }

        private void InvalidateView( PriorityQueue panes, int indent )
        {
            foreach( PriorityQueue.QueueEntry e in panes )
            {
                AbstractWizardPane pane = (AbstractWizardPane) e.Value;
                pane.PaneGotValid += OnPaneGotValid;
                pane.PaneGotInvalid += OnPaneGotInvalid;
                pane.PaneRegistered += OnRegisterPane;
                pane.PaneDeregistered += OnDeregisterPane;
                _panesList.Add( new Pair( indent, pane ) );
                InvalidateView( pane._panesQueue, indent + 1 );
            }
        }

        private void ShowActivePaneHelp()
        {
            string topic = null;
            AbstractWizardPane currentPane = CurrentPane;
            if( currentPane != null )
            {
                topic = currentPane.HelpKeyword;
            }
            if ( topic == null )
            {
                topic = "/getstarted/indexing_your_computers_resources.html";
            }
            Help.ShowHelp( this, Core.UIManager.HelpFileName, topic );
        }

        private void EnterCurrentPane( PaneChangeReason reason )
        {
            if( _maxPaneIndex < _currentIndex )
            {
                _maxPaneIndex = _currentIndex;
            }
            AbstractWizardPane currentPane = CurrentPane;
            if( currentPane != null )
            {
                Control control = GetControl( currentPane );
                _paneControlPanel.Controls.Add( control );
                control.Dock = DockStyle.Fill;
                control.Visible = true;
                _headerLabel.Text = currentPane.Header;
                Core.UserInterfaceAP.QueueJob( new MethodInvoker( DrawListOfHeaders ) );
                currentPane.EnterPane( reason );
            }
            _backButton.Enabled = _currentIndex > _minPaneIndex;
            _panePanel.Visible = _currentIndex >= 0;
            _pictureBox.Visible = _currentIndex < 0;
            _nextButton.Text = ( _currentIndex < _panesList.Count - 1 ) ? "&Next >" : "Fi&nish";
        }

        private void LeaveCurrentPane( PaneChangeReason reason )
        {
            AbstractWizardPane currentPane = CurrentPane;
            if( currentPane != null )
            {
                Control control = GetControl( currentPane );
                control.Visible = false;
                _paneControlPanel.Controls.Remove( control );
                currentPane.LeavePane( reason );
            }
        }

        private void DrawListOfHeaders()
        {
            int top = 8;
            int paneIndex = 0;
            string header = _headerLabel.Text;

            _controlPool.MoveControlsToPool();
            foreach( Pair pair in _panesList )
            {
                AbstractWizardPane pane = (AbstractWizardPane) pair.Second;
                JetLinkLabel label = (JetLinkLabel) _controlPool.GetControl();
                label.Text = pane.Header;
                label.ClickableLink = ( paneIndex <= _maxPaneIndex + 1 ) && label.Text != header;
                if( label.ClickableLink )
                {
                    if( label.Tag == null )
                    {
                        label.Click += new EventHandler( selectlabel_Click );
                    }
                    label.Tag = paneIndex;
                }
                ++paneIndex;
                label.Top = top;
                label.Left = 8 + 8 * (int) pair.First;
                label.Width = _listPanel.Width - 8 - label.Left;
                label.Height = 17;
                label.Font = new Font( _btnHelp.Font, FontStyle.Regular );
                if( label.Text == header )
                {
                    label.Font = new Font( label.Font, FontStyle.Bold );
                }
                top += 18;
            }
            _controlPool.RemovePooledControls();
        }

        private static Control CreateJetLinkLabel()
        {
            return new JetLinkLabel();
        }

        private void selectlabel_Click( object sender, EventArgs e )
        {
            LeaveCurrentPane( PaneChangeReason.DirectSelect );
            JetLinkLabel label = (JetLinkLabel) sender;
            _currentIndex = (int) label.Tag;
            EnterCurrentPane( PaneChangeReason.DirectSelect );
        }

        #endregion

        private void _backButton_Click(object sender, System.EventArgs e)
        {
            LeaveCurrentPane( PaneChangeReason.BackPressed );
            --_currentIndex;
            EnterCurrentPane( PaneChangeReason.BackPressed );
            _backButton.Focus();
        }

	    private void _nextButton_Click(object sender, System.EventArgs e)
        {
            LeaveCurrentPane( PaneChangeReason.NextPressed );
            if( ++_currentIndex < _panesList.Count )
            {
                EnterCurrentPane( PaneChangeReason.NextPressed );
                _nextButton.Focus();
            }
            else
            {
                foreach( Pair pair in _panesList )
                {
                    ( (AbstractWizardPane) pair.Second ).OK();
                }
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private void _cancelButton_Click(object sender, System.EventArgs e)
        {
            if( !ConfirmCancel() )
            {
                return;
            }
            foreach( Pair pair in _panesList )
            {
                ( (AbstractWizardPane) pair.Second ).Cancel();
            }
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void _btnHelp_Click(object sender, System.EventArgs e)
        {
            ShowActivePaneHelp();
        }

        private void WizardFrom_HelpRequested(object sender, System.Windows.Forms.HelpEventArgs hlpevent)
        {
            ShowActivePaneHelp();
        }

        private void _listPanel_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            Pen apen = new Pen( SystemColors.ActiveCaption, 1 );
            using( apen )
            {
                e.Graphics.DrawRectangle( apen, -1, -1, _listPanel.Width, _listPanel.Height + 1 );
            }
        }

        private void _listPanel_Resize(object sender, System.EventArgs e)
        {
            if( _listPanel.Width < 172 )
            {
                _listPanel.Width = 172;
            }
        }

        private void _paneContentPanel_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            Pen apen = new Pen( SystemColors.ActiveCaption, 1 );
            using( apen )
            {
                e.Graphics.DrawRectangle( apen, 0, -1, _paneContentPanel.Width, _paneContentPanel.Height + 1 );
            }
        }
	}

    public enum PaneChangeReason
    {
        Invalidate,
        DirectSelect,
        BackPressed,
        NextPressed
    }

    public delegate void AbstractWizardPaneDelegate( AbstractWizardPane pane );

    public abstract class AbstractWizardPane
    {
        public abstract string  Header { get; }
        public abstract Control Pane { get; }
        public abstract string  HelpKeyword{ get; }
        public abstract void    ShowPane();
        public abstract void    EnterPane( PaneChangeReason reason );
        public abstract void    LeavePane( PaneChangeReason reason );
        public abstract void    OK();
        public abstract void    Cancel();

        public event AbstractWizardPaneDelegate PaneGotValid;
        public event AbstractWizardPaneDelegate PaneGotInvalid;
        public event AbstractWizardPaneDelegate PaneRegistered;
        public event AbstractWizardPaneDelegate PaneDeregistered;

        public AbstractWizardPane Parent
        {
            get
            {
                return _parent;
            }
        }

        public void RegisterPane( int priority, AbstractWizardPane pane )
        {
            _panesQueue.Push( -priority, pane );
            pane._parent = this;
            if( PaneRegistered != null )
            {
                PaneRegistered( pane );
            }
        }

        public void DeregisterPane( AbstractWizardPane pane )
        {
            foreach( PriorityQueue.QueueEntry e in _panesQueue )
            {
                if( e.Value.Equals( pane ) )
                {
                    _panesQueue.Remove( e );
                    if( PaneDeregistered != null )
                    {
                        PaneDeregistered( pane );
                    }
                    break;
                }
            }
        }

        protected AbstractWizardPane        _parent;
        protected internal PriorityQueue    _panesQueue = new PriorityQueue();
    }

    public class OptionsPaneWizardAdapter : AbstractWizardPane
    {
        public OptionsPaneWizardAdapter( string header, OptionsPaneCreator creator )
        {
            _header = header;
            _creator = creator;
        }

        public bool IsStartupPane
        {
            get { return _isStartupPane; }
            set
            {
                _isStartupPane = value;
                if( _pane != null )
                {
                    _pane.IsStartupPane = value;
                }
            }
        }

        public override string Header
        {
            get
            {
                return _header;
            }
        }

        public override Control Pane
        {
            get
            {
                CheckPane();
                return _pane;
            }
        }

        public override void ShowPane()
        {
            CheckPane();
            _pane.ShowPane();
        }

        public override void EnterPane( PaneChangeReason reason )
        {
            CheckPane();
            _pane.EnterPane();
        }

        public override void LeavePane( PaneChangeReason reason )
        {
            CheckPane();
            _pane.LeavePane();
        }

        public override void OK()
        {
            CheckPane();
            _pane.OK();
        }

        public override void Cancel()
        {
            CheckPane();
            _pane.Cancel();
        }

        public override string HelpKeyword
        {
            get
            {
                CheckPane();
                return _pane.GetHelpKeyword();
            }
        }

        private void CheckPane()
        {
            if( _pane == null )
            {
                _pane = _creator();
                _pane.IsStartupPane = _isStartupPane;
            }
        }

        private string              _header;
        private OptionsPaneCreator  _creator;
        private AbstractOptionsPane _pane;
        private bool                _isStartupPane;
    }

}
