/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

using JetBrains.Interop.WinApi;
using JetBrains.Interop.WinApi.Wrappers;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

using Timer=System.Windows.Forms.Timer;

namespace JetBrains.Omea
{
	/// <summary>
	/// Summary description for SplashScreen.
	/// </summary>
	public class SplashScreen : Form, IProgressWindow
	{
		/// <summary>
		/// A handle to the unmanaged pre-splash screen window, if one was shown by the unmanaged Launcher stage. Should be discarded by the managed splash.
		/// </summary>
		private IntPtr _hwndUnmanagedPreSplash;

		private const int _BaseHeight = 300;

        private PictureBox _pictureBox1;
        private ProgressBar _progressBar;
        private JetLinkLabel _timeLabel;
        private JetLinkLabel _messageLabel;
        private Panel _loadingErrors;
        private PictureBox _exclPicture;
        private JetLinkLabel _lblClose;
        private int _startTickCount;
        private string _lastTimeMessage;
        private int _errorsCount = 0;

        /// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private Timer _timerRemoveUnmanagedPreSplash;

		internal event EventHandler OnFirstShow;

		/// <summary>
		/// Creates the splash-and-progress window.
		/// </summary>
		/// <param name="hwndUnmanagedPreSplash">
		/// A handle to the unmanaged pre-splash screen window, if one was shown by the unmanaged Launcher stage. Should be discarded by the managed splash.
		/// </param>
		public SplashScreen(IntPtr hwndUnmanagedPreSplash)
		{
			_hwndUnmanagedPreSplash = hwndUnmanagedPreSplash;

			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			// Positioning
			SetStartWindowPos();

            Text = "Starting " + Core.ProductFullName;

#if READER
            _pictureBox1.Image = Image.FromStream( 
                Assembly.GetExecutingAssembly().GetManifestResourceStream( "OmniaMea.Icons.SplashScreenReader.png" ) );
#else
            _pictureBox1.Image = Image.FromStream( 
                Assembly.GetExecutingAssembly().GetManifestResourceStream( "OmniaMea.Icons.splash_pro.png" ) );
#endif

			CreateHandle();
			Refresh();
		}

		/// <summary>
		/// By default, the window is positioned in the “center-screen” fashion.
		/// If the unmanaged pre-splash was available, use its location as the initial one.
		/// </summary>
		private unsafe void SetStartWindowPos()
		{
			try
			{
				if(_hwndUnmanagedPreSplash == IntPtr.Zero)
					return;
				if(User32Dll.IsWindow((void*)_hwndUnmanagedPreSplash) == 0)
					return; // Dead

				Location = User32Dll.Helpers.GetWindowRect(_hwndUnmanagedPreSplash).Location;
				StartPosition = FormStartPosition.Manual;
			}
			catch(Exception ex)
			{
				MessageBox.Show(new Win32Window(_hwndUnmanagedPreSplash), string.Format("Could not get the window coordinates. {0}", ex.Message), "JetBrains Omea", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			}
		}

		protected override unsafe void WndProc(ref Message m)
		{
			switch((WindowsMessages)m.Msg)
			{
			case WindowsMessages.WM_ERASEBKGND:
				var hdc = (void*)m.WParam;
				RECT rcClient = ClientRectangle;
				User32Dll.FillRect(hdc, &rcClient, Gdi32Dll.GetStockObject((int)StockLogicalObjects.WHITE_BRUSH));
				m.Result = (IntPtr)1; // Did it
			Thread.Sleep(5000);
				return;
			}
			base.WndProc(ref m);
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
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(SplashScreen));
            this._pictureBox1 = new System.Windows.Forms.PictureBox();
            this._messageLabel = new JetBrains.Omea.GUIControls.JetLinkLabel();
            this._progressBar = new System.Windows.Forms.ProgressBar();
            this._timeLabel = new JetBrains.Omea.GUIControls.JetLinkLabel();
			this._timerRemoveUnmanagedPreSplash = new Timer();
			components = new Container();
			components.Add(_timerRemoveUnmanagedPreSplash);
            this.SuspendLayout();
            // 
            // _pictureBox1
            // 
            this._pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this._pictureBox1.Location = new System.Drawing.Point(0, 0);
            this._pictureBox1.Name = "_pictureBox1";
            this._pictureBox1.Size = new System.Drawing.Size(400, 300);
            this._pictureBox1.TabIndex = 0;
            this._pictureBox1.TabStop = false;
            this._pictureBox1.Paint += new PaintEventHandler(SplashScreen_Paint);
            // 
            // _progressBar
            // 
            this._progressBar.Location = new System.Drawing.Point(24, 240);
            this._progressBar.Name = "_progressBar";
            this._progressBar.Size = new System.Drawing.Size(348, 16);
            this._progressBar.TabIndex = 2;
            // 
            // _messageLabel
            // 
            this._messageLabel.AutoSize = false;
            this._messageLabel.BackColor = System.Drawing.SystemColors.Window;
            this._messageLabel.ClickableLink = false;
            this._messageLabel.Cursor = System.Windows.Forms.Cursors.Default;
            this._messageLabel.EndEllipsis = true;
            this._messageLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._messageLabel.ForeColor = System.Drawing.Color.Black;
            this._messageLabel.Location = new System.Drawing.Point(26, 222);
            this._messageLabel.Name = "_messageLabel";
            this._messageLabel.Size = new System.Drawing.Size(344, 20);
            this._messageLabel.TabIndex = 1;
            // 
            // _timeLabel
            // 
            this._timeLabel.AutoSize = false;
            this._timeLabel.BackColor = System.Drawing.SystemColors.Window;
            this._timeLabel.ClickableLink = false;
            this._timeLabel.Cursor = System.Windows.Forms.Cursors.Default;
            this._timeLabel.EndEllipsis = true;
            this._timeLabel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._timeLabel.ForeColor = System.Drawing.Color.Black;
            this._timeLabel.Location = new System.Drawing.Point(26, 260);
            this._timeLabel.Name = "_timeLabel";
            this._timeLabel.Size = new System.Drawing.Size(344, 20);
            this._timeLabel.TabIndex = 3;
			// Timer
			_timerRemoveUnmanagedPreSplash.Interval = (int)TimeSpan.FromSeconds(.3).TotalMilliseconds;
			_timerRemoveUnmanagedPreSplash.Enabled = true;
			_timerRemoveUnmanagedPreSplash.Tick += OnTimerRemoveUnmanagedPreSplash;
            // 
            // SplashScreen
            // 
            this.AutoScaleMode = AutoScaleMode.None;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(400, 300);
            this.Name = "SplashScreen";
            this.BackColor = Color.White;
            this.Controls.Add(this._timeLabel);
            this.Controls.Add(this._progressBar);
            this.Controls.Add(this._messageLabel);
            this.Controls.Add(this._pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Omea Splash Screen";
            this.VisibleChanged += new System.EventHandler(this.ProgressWindow_VisibleChanged);
            this.Icon = LoadIcon();
            this.ResumeLayout(false);
        }

		/// <summary>
		/// Time to remove the unmanaged splash.
		/// Note: we won't do it on <see cref="Control.VisibleChanged"/>, as the window is not responding for some time after that (loading netfx, jitting, so on), so let's wait for messages to pump and then remove the pre-splash.
		/// </summary>
		private void OnTimerRemoveUnmanagedPreSplash(object sender, EventArgs e)
		{
			if((!IsHandleCreated) || (!Visible))
				return; // Not ready yet
			_timerRemoveUnmanagedPreSplash.Stop(); // Needed no more; reentrancy OK

			// Cause the managed splash to appear onscreen
			Refresh();
			User32Dll.Helpers.SetLayeredWindowAttributes(this, Color.Empty, 0.9, SetLayeredWindowAttributesFlags.LWA_ALPHA);

			// Remove the unmanaged pre-splash
			if(_hwndUnmanagedPreSplash != IntPtr.Zero)
			{
				User32Dll.Helpers.DestroyWindow(_hwndUnmanagedPreSplash);
				_hwndUnmanagedPreSplash = IntPtr.Zero;
			}
		}

		private static Icon LoadIcon()
        {
#if !READER
            return MainFrame.LoadIconFromAssembly("App.ico");
#else
		    return MainFrame.LoadIconFromAssembly( "AppReader.ico" );
#endif
        }
		#endregion

        private void SplashScreen_Paint(object sender, PaintEventArgs e)
        {
            base.OnPaint( e );
            if (_loadingErrors != null)
            {
                Graphics g = e.Graphics;
                Point[] track = new Point[4];

                track[0].X = 0; track[0].Y = _BaseHeight;
                track[1].X = 0; track[1].Y = _pictureBox1.Height - 1;
                track[2].X = _pictureBox1.Width - 1; track[2].Y = _pictureBox1.Height - 1;
                track[3].X = _pictureBox1.Width - 1; track[3].Y = _BaseHeight;

                g.DrawLines(new Pen(Color.Gray), track);
                g.FillRectangle(new SolidBrush(Color.White), 1, _BaseHeight, _pictureBox1.Width - 2, _pictureBox1.Height - _BaseHeight - 2);
            }
        }

        /**
         * When the form is first shown, invokes a delegate to start the indexing
         * process after the form becomes visible.
         */
		
        private void ProgressWindow_VisibleChanged(object sender, EventArgs e)
        {
            VisibleChanged -= ProgressWindow_VisibleChanged;
            BeginInvoke( new MethodInvoker( FirstShow ) );
        }

        /**
         * After the form is shown, starts the index building process.
         */
		
        private void FirstShow()
        {
            ResetElapsedTime();
            if ( OnFirstShow != null )
            {
                OnFirstShow( this, EventArgs.Empty );
            }

            //  Normally return only if there were no erros during the startup.
            //  Otherwise, display little link label which allows to continue the
            //  application.
            if (_loadingErrors == null)
            {
                DialogResult = DialogResult.OK;
            }
        }

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				cp.ClassStyle |= (int)WindowClassStyles.CS_DROPSHADOW;
				cp.ExStyle |= (int)WindowExStyles.WS_EX_LAYERED;
				return cp;
			}
		}

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			// Make the window translucent
			// That's the easiest way of making the system repaint our window for us
			// We don't use the Opacity property, as it'd throw from time to time
			// Initially, the translucent window appears black onscreen, that's why we make it 100% transparent
			// When the painting is ready, we'd make it more opaque, see ontimer
			User32Dll.Helpers.SetLayeredWindowAttributes(this, Color.Empty, 0, SetLayeredWindowAttributesFlags.LWA_ALPHA);
		}

		#region UpdateProgress
        private delegate void UpdateProgressDelegate( int percentage, string message, string timeMessage );

        public void ResetElapsedTime()
        {
            _startTickCount = Environment.TickCount;
        }

        public void UpdateProgress(int percentage, string message, string timeMessage)
        {
            #region Preconditions
            if ( percentage < 0 )
                throw new ArgumentException( "Percent must be non-negative" );
            if ( percentage > 100 )
                throw new ArgumentException( "Percent must be > 100" );
            if ( message == null )
                throw new ArgumentNullException( "message", "Progress message must be valid string (not null)" );
            #endregion Preconditions

            if ( !Core.UserInterfaceAP.IsOwnerThread )
            {
                Core.UserInterfaceAP.QueueJob( JobPriority.Immediate,
                    new UpdateProgressDelegate( UpdateProgress ), percentage, message, timeMessage );
                return;
            }

            _messageLabel.Text = message;
            _progressBar.Value = percentage;
            _lastTimeMessage = timeMessage;
            UpdateElapsedTime();
            Application.DoEvents();
        }

        private void UpdateElapsedTime()
        {
            int secs = (Environment.TickCount - _startTickCount) / 1000;
            string elapsedTime = "Elapsed time: " + String.Format("{0}:{1:d2}", secs / 60, secs % 60);
            if( !string.IsNullOrEmpty( _lastTimeMessage ) )
            {
                elapsedTime += ", " + _lastTimeMessage;
            }
            _timeLabel.Text = elapsedTime;
        }
        #endregion UpdateProgress

        #region Add Error Record
        public void AddErrorRecord( string pluginName, string message )
        {
            Label label = CreateErrorLabel(pluginName, message);
            if (_loadingErrors == null)
            {
                Height += Math.Max(70, label.Height + 20);

                CreatePanel();
            }
            else
            if( _errorsCount < 3 )
            {
                int fitSpace = _loadingErrors.Height - label.Top;
                if (fitSpace < label.Height)
                {
                    int shift = label.Height - fitSpace + 4;

                    Height += shift;
                }

                _errorsCount++;
            }
            _loadingErrors.Controls.Add( label );
            _pictureBox1.Invalidate();
        }

        private void CreatePanel()
        {
            _loadingErrors = new Panel();
            _loadingErrors.AutoScroll = true;
            _loadingErrors.Location = new Point(9, _BaseHeight + 4);
            _loadingErrors.Size = new Size(382, 40);
            _loadingErrors.Name = "_loadingErrors";
            _loadingErrors.Anchor = AnchorStyles.Top | AnchorStyles.Bottom;
            _loadingErrors.BorderStyle = BorderStyle.Fixed3D;

            _exclPicture = new PictureBox();
            _exclPicture.Anchor = AnchorStyles.Top;
            _exclPicture.Location = new Point(4, _BaseHeight);
            _exclPicture.Name = "_exclPicture";
            _exclPicture.Size = new Size(17, 17);
            _exclPicture.TabStop = false;
            _exclPicture.Image = Image.FromStream( Assembly.GetExecutingAssembly().GetManifestResourceStream("OmniaMea.Icons.BackgroundException.ico"));

            _lblClose = new JetLinkLabel();
            _lblClose.Anchor = AnchorStyles.Bottom;
            _lblClose.AutoSize = false;
            _lblClose.BackColor = SystemColors.Window;
            _lblClose.ForeColor = SystemColors.HotTrack;
            _lblClose.ClickableLink = true;
            _lblClose.Cursor = Cursors.Default;
            _lblClose.EndEllipsis = false;
            _lblClose.Font = new Font("Tahoma", 8.25F, FontStyle.Regular, GraphicsUnit.Point, ((System.Byte)(204)));
            _lblClose.Location = new Point(330, 350);
            _lblClose.Name = "_lblClose";
            _lblClose.Size = new Size(60, 18);
            _lblClose.TabStop = false;
            _lblClose.Text = "Continue...";
            _lblClose.Click += _lblClose_Click;

            Controls.Add(_lblClose);
            Controls.Add(_exclPicture);
            Controls.Add(_loadingErrors);

            _loadingErrors.BringToFront();
            _exclPicture.BringToFront();
            _lblClose.BringToFront();
        }

        void _lblClose_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private Label CreateErrorLabel(string pluginName, string message)
        {
            int baseYCoordinate = CalcSummaryLabelHeights();

            Label lblMessage = new Label();
            lblMessage.Text = "Error occured in " + pluginName + ": " + message;
            lblMessage.Location = new Point(8, baseYCoordinate );
            lblMessage.FlatStyle = FlatStyle.System;
            lblMessage.ForeColor = Color.Red;

            Font font = lblMessage.Font;
            float fontHeight = font.GetHeight( Graphics.FromHwnd(Handle) );
            int currWidth = (int)Graphics.FromHwnd(Handle).MeasureString(lblMessage.Text, font).Width;

            //  NB: the string width computation (line above) is often sucks giving
            //      us lesser width than it is really. Thus we add some penalty.
            currWidth += 20;

            int lines = currWidth / 340 + 1;
            int height = (lines == 1) ? 18 : (int)(lines * ( fontHeight + 2 ) );
            lblMessage.Size = new Size( 360, height );

            return lblMessage;
        }

        private int CalcSummaryLabelHeights()
        {
            int baseYCoordinate = 4;
            if (_loadingErrors != null)
            {
                foreach (Control ctrl in _loadingErrors.Controls)
                {
                    baseYCoordinate += ctrl.Size.Height + 4;
                }
                baseYCoordinate += _loadingErrors.AutoScrollPosition.Y;
            }
            return baseYCoordinate;
        }
        #endregion Add Error Record
    }
}
