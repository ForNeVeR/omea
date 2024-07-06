// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Drawing;
using System.Reflection;
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
        private int _startTickCount;
        private string _lastTimeMessage;

        /// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

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
                var track = new Point[4];

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
                Core.UserInterfaceAP.QueueJob( JobPriority.Immediate, "Update Progress",
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
    }
}
