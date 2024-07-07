// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Components.ImageListButton;

namespace JetBrains.Omea
{
	/**
     * Balloon notification form.
     */

    internal class BalloonForm : Form
	{
        private double _safeOpacity = 0.0;
        private const int DefaultVisibilityInterval = 4;

        private Panel               _contentPane;
        private Label               _lblFrom, _lblSubject;
        private JetLinkLabel        _lblBody;
        private ImageListPictureBox _resourceIconBox;
        private ImageListButton     _btnDelete, _btnClose;
        private readonly ImageList  _delImageList;
        private readonly ImageList  _closeImageList;
        private ToolTip             _toolTipReason;

        private Timer           _fadeInTimer, _fadeOutTimer;
        private Timer           _visibleTimer;
        private Timer           _balloonLeaveTimer;
        private EventHandler    _clickHandler;

        private System.ComponentModel.IContainer components;

        private IResource       _lastResource;
        private IResourceList   _lastResourceList;
        private int             _visibilityInterval;

		public BalloonForm()
		{
            _delImageList = new ImageList();
            _closeImageList = new ImageList();
            _delImageList.ColorDepth = Core.ResourceIconManager.IconColorDepth;

            _delImageList.Images.Add( Utils.TryGetEmbeddedResourceIconFromAssembly( "OmniaMea", "OmniaMea.Icons.delete.ico" ) );
            _delImageList.Images.Add( Utils.TryGetEmbeddedResourceIconFromAssembly( "OmniaMea", "OmniaMea.Icons.delete_hover.ico" ) );
            _delImageList.Images.Add( Utils.TryGetEmbeddedResourceIconFromAssembly( "OmniaMea", "OmniaMea.Icons.delete_pressed.ico" ) );

            _closeImageList.Images.Add( Utils.TryGetEmbeddedResourceIconFromAssembly( "OmniaMea", "OmniaMea.Icons.close1.ico" ) );
            _closeImageList.Images.Add( Utils.TryGetEmbeddedResourceIconFromAssembly( "OmniaMea", "OmniaMea.Icons.close2.ico" ) );
            _closeImageList.Images.Add( Utils.TryGetEmbeddedResourceIconFromAssembly( "OmniaMea", "OmniaMea.Icons.close3.ico" ) );

            ReadVisibilityTimer();
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
            this.components = new System.ComponentModel.Container();
            this._contentPane = new System.Windows.Forms.Panel();
            this._lblBody = new JetBrains.Omea.GUIControls.JetLinkLabel();
            this._resourceIconBox = new JetBrains.Omea.GUIControls.ImageListPictureBox();
            this._lblSubject = new System.Windows.Forms.Label();
            this._lblFrom = new System.Windows.Forms.Label();
            _btnDelete = new ImageListButton();
            _btnClose =  new ImageListButton();
            this._fadeInTimer = new System.Windows.Forms.Timer(this.components);
            this._visibleTimer = new System.Windows.Forms.Timer(this.components);
            this._fadeOutTimer = new System.Windows.Forms.Timer(this.components);
            this._balloonLeaveTimer = new System.Windows.Forms.Timer(this.components);

			_toolTipReason = new ToolTip( components );
			_toolTipReason.ShowAlways = true;

            this._contentPane.SuspendLayout();
            this.SuspendLayout();
            //
            // _contentPane
            //
            this._contentPane.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._contentPane.BackColor = System.Drawing.Color.FromArgb(((System.Byte)(192)), ((System.Byte)(192)), ((System.Byte)(255)));
            this._contentPane.Controls.Add(_btnClose);
            this._contentPane.Controls.Add(this._lblBody);
            this._contentPane.Controls.Add(this._resourceIconBox);
            this._contentPane.Controls.Add(this._btnDelete);
            this._contentPane.Controls.Add(this._lblSubject);
            this._contentPane.Controls.Add(this._lblFrom);
            this._contentPane.Location = new System.Drawing.Point(2, 2);
            this._contentPane.Name = "_contentPane";
            this._contentPane.Size = new System.Drawing.Size(296, 68);
            this._contentPane.TabIndex = 0;
            //
            // _lblBody
            //
            this._lblBody.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblBody.AutoSize = false;
            this._lblBody.Cursor = System.Windows.Forms.Cursors.Hand;
            this._lblBody.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._lblBody.ForeColor = System.Drawing.Color.Blue;
            this._lblBody.Location = new System.Drawing.Point(28, 36);
            this._lblBody.Name = "_lblBody";
            this._lblBody.Size = new System.Drawing.Size(264, 28);
            this._lblBody.TabIndex = 3;
            this._lblBody.WordWrap = true;
            this._lblBody.Click += new System.EventHandler(this._lblBody_Click);
            this._lblBody.MouseEnter += new System.EventHandler(this.OnBalloonMouseEnter);
            this._lblBody.MouseLeave += new System.EventHandler(this.OnBalloonMouseLeave);
            //
            // _resourceIconBox
            //
            this._resourceIconBox.ImageIndex = 0;
            this._resourceIconBox.ImageLeftTopPoint = new System.Drawing.Point(0, 0);
            this._resourceIconBox.Location = new System.Drawing.Point(8, 2);
            this._resourceIconBox.Name = "_resourceIconBox";
            this._resourceIconBox.Size = new System.Drawing.Size(16, 16);
            this._resourceIconBox.TabIndex = 2;
            this._resourceIconBox.TabStop = false;
            this._resourceIconBox.MouseEnter += new System.EventHandler(this.OnBalloonMouseEnter);
            this._resourceIconBox.MouseLeave += new System.EventHandler(this.OnBalloonMouseLeave);
            //
            // _btnDelete
            //
            this._btnDelete.Anchor = (AnchorStyles.Top | AnchorStyles.Right);
            this._btnDelete.Name = "_btnDelete";
            this._btnDelete.Size = new System.Drawing.Size(16, 16);
            this._btnDelete.Location = new System.Drawing.Point(8, 20);
            this._btnDelete.TabIndex = 3;
            this._btnDelete.Click += new EventHandler(_btnDelete_Click);
            this._btnDelete.NormalImageIndex = 0;
            this._btnDelete.HotImageIndex = 1;
            this._btnDelete.PressedImageIndex = 2;
            this._btnDelete.ImageList = _delImageList;
            this._btnDelete.MouseEnter += new System.EventHandler(this.OnBalloonMouseEnter);
            this._btnDelete.MouseLeave += new System.EventHandler(this.OnBalloonMouseLeave);
            //
            // _btnClose
            //
            this._btnClose.Name = "_btnClose";
            this._btnClose.Size = new System.Drawing.Size(16, 16);
            this._btnClose.Location = new System.Drawing.Point(280, 2);
            this._btnClose.TabIndex = 3;
            this._btnClose.NormalImageIndex = 0;
            this._btnClose.HotImageIndex = 1;
            this._btnClose.PressedImageIndex = 2;
            this._btnClose.ImageList = _closeImageList;
            this._btnClose.MouseEnter += new System.EventHandler(this.OnBalloonMouseEnter);
            this._btnClose.MouseLeave += new System.EventHandler(this.OnBalloonMouseLeave);
            this._btnClose.Click += new EventHandler(_btnClose_Click);
            //
            // _lblSubject
            //
            this._lblSubject.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblSubject.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblSubject.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._lblSubject.Location = new System.Drawing.Point(28, 20);
            this._lblSubject.Name = "_lblSubject";
            this._lblSubject.Size = new System.Drawing.Size(264, 14);
            this._lblSubject.TabIndex = 1;
            this._lblSubject.Text = "<no subject>";
            this._lblSubject.UseMnemonic = false;
            this._lblSubject.MouseEnter += new System.EventHandler(this.OnBalloonMouseEnter);
            this._lblSubject.MouseLeave += new System.EventHandler(this.OnBalloonMouseLeave);
            //
            // _lblFrom
            //
            this._lblFrom.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblFrom.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblFrom.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._lblFrom.Location = new System.Drawing.Point(28, 4);
            this._lblFrom.Name = "_lblFrom";
            this._lblFrom.Size = new System.Drawing.Size(264, 16);
            this._lblFrom.TabIndex = 0;
            this._lblFrom.Text = "From:";
            this._lblFrom.UseMnemonic = false;
            this._lblFrom.MouseEnter += new System.EventHandler(this.OnBalloonMouseEnter);
            this._lblFrom.MouseLeave += new System.EventHandler(this.OnBalloonMouseLeave);
            //
            // _fadeInTimer
            //
            this._fadeInTimer.Interval = 100;
            this._fadeInTimer.Tick += new System.EventHandler(this._fadeInTimer_Tick);
            //
            // _visibleTimer
            //
            this._visibleTimer.Interval = _visibilityInterval;
            this._visibleTimer.Tick += new System.EventHandler(this._visibleTimer_Tick);
            //
            // _fadeOutTimer
            //
            this._fadeOutTimer.Interval = 100;
            this._fadeOutTimer.Tick += new System.EventHandler(this._fadeOutTimer_Tick);
            //
            // _balloonLeaveTimer
            //
            this._balloonLeaveTimer.Interval = 150;
            this._balloonLeaveTimer.Tick += new System.EventHandler(this._balloonLeaveTimer_Tick);
            //
            // BalloonForm
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.ClientSize = new System.Drawing.Size(300, 72);
            this.Controls.Add(this._contentPane);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "BalloonForm";
            this.Opacity = 0;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "BalloonForm";
            this.TopMost = true;
            this._contentPane.ResumeLayout(false);
            this.ResumeLayout(false);
        }
        #endregion

        internal double SafeOpacity
        {
            get { return _safeOpacity; }
            set
            {
                _safeOpacity = value;
                try
                {
                    Opacity = value;
                }
                catch( Exception ex )
                {
                    Trace.WriteLine( "Error setting BalloonForm opacity:" + ex );
                }
            }
        }

        internal void ShowResource( IResource res )
        {
            SetLastResource( res );
            ReadBackColor();
            DisplayResourceAlertData( res );
            StartVisibilityTimer();
        }

        internal void ShowAlert( ImageList imageList, int imageIndex, string from, string subject,
                                 string body, EventHandler clickHandler )
        {
            SetLastResource( null );
            ReadBackColor();
            DisplayAlertData( imageList, imageIndex, from, subject, body, clickHandler );
            StartVisibilityTimer();
        }

        private void DisplayResourceAlertData( IResource res )
        {
            string subject;
            string from = res.GetPropText( Core.ContactManager.Props.LinkFrom );
            if ( res.HasProp( Core.Props.Subject ) )
            {
                subject = res.GetPropText( Core.Props.Subject );
            }
            else
            {
                if ( from.Length == 0 )
                {
                    from = res.DisplayName;
                }
                subject = "<no subject>";
            }

            DisplayAlertData( Core.ResourceIconManager.ImageList,
                              Core.ResourceIconManager.GetIconIndex( res ), from, subject,
                              Core.MessageFormatter.GetPreviewText( res, 2 ), HandleResourceClick );
        }

        private void DisplayAlertData( ImageList imageList, int imageIndex, string from, string subject,
                                       string body, EventHandler clickHandler )
        {
            _resourceIconBox.ImageList = imageList;
            _resourceIconBox.ImageIndex = imageIndex;
            _lblFrom.Text = "From: " + from;
            _lblSubject.Text = subject;
            _lblBody.Text = !String.IsNullOrEmpty( body ) ? body : "<no body>";
            _clickHandler = clickHandler;
            _lblBody.ClickableLink = (clickHandler != null);

            //  Show the tooltip only if the actual text does not fit
            //  into the control.
            int       filled, linesFilled;
            Graphics  grph = Graphics.FromHwnd( _lblSubject.Handle );
            grph.MeasureString( subject, _lblSubject.Font, new SizeF( _lblSubject.Width, 16.0f ),
                                new StringFormat(), out filled, out linesFilled );
            _toolTipReason.SetToolTip( _lblSubject, (filled < subject.Length) ? subject : null );
        }

        private void SetLastResource( IResource res )
        {
            if ( _lastResourceList != null )
            {
                _lastResourceList.ResourceDeleting -= HandleResourceDeleted;
                _lastResourceList.ResourceChanged -= HandleResourceChanged;
                _lastResourceList.Dispose();
                _lastResourceList = null;
            }
            _lastResource = res;
            if ( _lastResource != null )
            {
                _lastResourceList = _lastResource.ToResourceListLive();
                _lastResourceList.ResourceDeleting += HandleResourceDeleted;
                _lastResourceList.ResourceChanged += HandleResourceChanged;
            }
        }

        private void HandleResourceDeleted( object sender, ResourceIndexEventArgs e )
        {
            Core.UIManager.QueueUIJob( new MethodInvoker( HideBalloon ) );
        }

        private void HandleResourceChanged( object sender, ResourcePropIndexEventArgs e )
        {
            Core.UIManager.QueueUIJob( new MethodInvoker( UpdateLastResource ) );
        }

        private void UpdateLastResource()
        {
            if ( _lastResource != null )
            {
                DisplayResourceAlertData( _lastResource );
            }
        }

        internal void SetDefaultLocation()
        {
        	Rectangle rect = Screen.PrimaryScreen.Bounds;
            rect = Screen.GetWorkingArea( rect );
            Left = rect.Right - Width;
            Top = rect.Bottom - Height;
        }

        private void StartVisibilityTimer()
        {
            ReadVisibilityTimer();
            if ( SafeOpacity < 0.1 )
            {
                _fadeInTimer.Start();
            }
            else if ( _visibleTimer.Enabled )
            {
                _visibleTimer.Stop();
                _visibleTimer.Start();
            }
        }

        private void _fadeInTimer_Tick( object sender, EventArgs e )
        {
            SafeOpacity = Opacity + 0.2;
            if ( SafeOpacity >= 0.99 )
            {
            	_fadeInTimer.Stop();
                _visibleTimer.Start();
            }
        }

        private void _visibleTimer_Tick( object sender, EventArgs e )
        {
            _visibleTimer.Stop();
            _fadeOutTimer.Start();
        }

        private void _fadeOutTimer_Tick( object sender, EventArgs e )
        {
            if ( SafeOpacity < 0.1 )
            {
                HideBalloon();
            }
            else
            {
                SafeOpacity = SafeOpacity - 0.2;
            }
        }

        private void HideBalloon()
        {
            Visible = false;
            SafeOpacity = 0;
            SetLastResource( null );
            StopAllTimers();
        }

        private void _lblBody_Click( object sender, EventArgs e )
        {
            if ( _clickHandler != null )
            {
                _clickHandler( sender, e );
            }
        }

        private void HandleResourceClick( object sender, EventArgs e )
        {
            if ( _lastResource != null )
            {
                Core.UIManager.RestoreMainWindow();
                Core.UIManager.DisplayResourceInContext( _lastResource, true );
            }
        }

        private void OnBalloonMouseEnter( object sender, EventArgs e )
        {
            StopAllTimers();
            SafeOpacity = 1.0;
        }

        private void StopAllTimers()
        {
            _visibleTimer.Stop();
            _fadeInTimer.Stop();
            _fadeOutTimer.Stop();
            _balloonLeaveTimer.Stop();
        }

        private void OnBalloonMouseLeave( object sender, EventArgs e )
        {
            _balloonLeaveTimer.Start();
        }

        private void _balloonLeaveTimer_Tick( object sender, EventArgs e )
        {
            _balloonLeaveTimer.Stop();
            _fadeOutTimer.Start();
        }

        private void  ReadVisibilityTimer()
        {
            //  parameter is set in seconds.
            _visibilityInterval = Core.SettingStore.ReadInt( "General", "BalloonTimeout", DefaultVisibilityInterval );
            _visibilityInterval *= 1000;
        }

        private void  ReadBackColor()
        {
            int  r, g, b;
            r = Core.SettingStore.ReadInt( "General", "BalloonBackgroundR", 192 );
            g = Core.SettingStore.ReadInt( "General", "BalloonBackgroundG", 192 );
            b = Core.SettingStore.ReadInt( "General", "BalloonBackgroundB", 255 );
            try
            {
                _contentPane.BackColor = Color.FromArgb(r, g, b);
            }
            catch( Exception )
            {
                _contentPane.BackColor = Color.FromArgb(192, 192, 255);
            }
        }

        private void  _btnDelete_Click(object sender, EventArgs e)
        {
            //  OM-12575, releasing mouse button may be with huge (enough)
            //  lag, during which the resource might have been deleted.
            if( _lastResource != null )
            {
                IResourceDeleter deleter = Core.PluginLoader.GetResourceDeleter( _lastResource.Type );
                if( deleter != null )
                {
                    Core.ResourceAP.QueueJob( new ResourceDelegate( deleter.DeleteResource ), _lastResource );
                    Core.UIManager.QueueUIJob( new MethodInvoker( HideBalloon ) );
                }
            }
        }

        private void  _btnClose_Click(object sender, EventArgs e)
        {
            HideBalloon();
        }
	}

    public class BalloonNotificationAction: IRuleAction
    {
        private DateTime _lastNotificationTime = DateTime.MinValue;

        public void Exec( IResource res, IActionParameterStore actionStore )
    	{
            if ( Core.State != CoreState.ShuttingDown )
            {
                TimeSpan ts = DateTime.Now - _lastNotificationTime;
                if ( ts.TotalMilliseconds > 500 )
                {
                    Core.UIManager.QueueUIJob( new ResourceDelegate( Core.UIManager.ShowDesktopAlert ),
                        res );
                    _lastNotificationTime = DateTime.Now;
                }
            }
    	}
    }
}
