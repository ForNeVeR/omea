// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.UI.Interop;
using JetBrains.Omea.OpenAPI;
using System.Diagnostics;

namespace JetBrains.Omea
{
    /// <summary>
    /// Summary description for ProgressWindow.
    /// </summary>
    public class ProgressWindow : Form, IProgressWindow
    {
        private System.ComponentModel.IContainer components;

        private int _startTickCount;
        private Label _statusLabel;
        private ProgressBar _progressBar;
        private Label _timeLabel;
        internal event EventHandler OnFirstShow;
    	private Timer _tmrUpdateElapsedTime;
        private string _lastTimeMessage;
        private string _lastProgressText;

        public ProgressWindow( bool canMinimize )
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            Icon = Core.UIManager.ApplicationIcon;

            if ( canMinimize )
            {
                EnableMinimize();
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
            this.components = new System.ComponentModel.Container();
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(ProgressWindow));
            this._statusLabel = new System.Windows.Forms.Label();
            this._progressBar = new System.Windows.Forms.ProgressBar();
            this._timeLabel = new System.Windows.Forms.Label();
            this._tmrUpdateElapsedTime = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            //
            // _statusLabel
            //
            this._statusLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._statusLabel.Location = new System.Drawing.Point(8, 8);
            this._statusLabel.Name = "_statusLabel";
            this._statusLabel.Size = new System.Drawing.Size(392, 14);
            this._statusLabel.TabIndex = 0;
            this._statusLabel.Text = "Indexing...";
            this._statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._statusLabel.UseMnemonic = false;
            //
            // _progressBar
            //
            this._progressBar.Location = new System.Drawing.Point(8, 48);
            this._progressBar.Name = "_progressBar";
            this._progressBar.Size = new System.Drawing.Size(384, 16);
            this._progressBar.TabIndex = 1;
            //
            // _timeLabel
            //
            this._timeLabel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._timeLabel.Location = new System.Drawing.Point(8, 24);
            this._timeLabel.Name = "_timeLabel";
            this._timeLabel.Size = new System.Drawing.Size(392, 14);
            this._timeLabel.TabIndex = 2;
            this._timeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._timeLabel.UseMnemonic = false;
            //
            // _tmrUpdateElapsedTime
            //
            this._tmrUpdateElapsedTime.Enabled = true;
            this._tmrUpdateElapsedTime.Interval = 1000;
            this._tmrUpdateElapsedTime.Tick += new System.EventHandler(this._tmrUpdateElapsedTime_Tick);
            //
            // ProgressWindow
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(402, 69);
            this.ControlBox = false;
            this.Controls.Add(this._timeLabel);
            this.Controls.Add(this._progressBar);
            this.Controls.Add(this._statusLabel);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProgressWindow";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Building Indexes...";
            this.VisibleChanged += new System.EventHandler(this.ProgressWindow_VisibleChanged);
            this.ResumeLayout(false);

        }
        #endregion


        /**
         * When the form is first shown, invokes a delegate to start the indexing
         * process after the form becomes visible.
         */

    	private void ProgressWindow_VisibleChanged(object sender, EventArgs e)
    	{
    		try
    		{
    			VisibleChanged -= ProgressWindow_VisibleChanged;
    			BeginInvoke(new MethodInvoker(FirstShow));
    		}
    		catch(Exception ex)
    		{
    			if(ICore.Instance != null)
    				Core.ReportException(ex, false);
    		}
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
            DialogResult = DialogResult.OK;
        }

        /**
         * Resets the elapsed indexing time.
         */

        public void ResetElapsedTime()
        {
            _startTickCount = Environment.TickCount;
        }

    	private delegate void UpdateProgressDelegate( int percentage, string message, string timeMessage );

        /**
         * Called after a message has been scanned by SourceAccessors. Updates the
         * progress bar and the status label.
         */

        public void UpdateProgress( int percentage, string message, string timeMessage )
        {
            if ( percentage < 0 )
                throw new ArgumentException( "Percent must be non-negative" );
            if ( percentage > 100 )
                throw new ArgumentException( "Percent must be > 100" );
            if ( message == null )
                throw new ArgumentNullException( "Progress message must be valid string (not null)" );

            if ( !Core.UserInterfaceAP.IsOwnerThread )
            {
                Core.UserInterfaceAP.QueueJob( JobPriority.Immediate,
                    new UpdateProgressDelegate( UpdateProgress ), percentage, message, timeMessage );
                return;
            }

            if ( _lastProgressText != message )
            {
                Trace.WriteLine( "Progress message: " + message );
            }

            _progressBar.Value = percentage;
            _statusLabel.Text = message;
            _lastProgressText = message;
            _lastTimeMessage = timeMessage;
            UpdateElapsedTime();

            Application.DoEvents();
        }

        private void UpdateElapsedTime()
        {
            string elapsedTime = "Elapsed time: "+ TicksToString( Environment.TickCount - _startTickCount );
            if ( !string.IsNullOrEmpty(_lastTimeMessage) )
            {
                elapsedTime += ", " + _lastTimeMessage;
            }
            _timeLabel.Text = elapsedTime;
        }

        private static string TicksToString( int ticks )
        {
            int secs = ticks / 1000;
            return String.Format( "{0}:{1:d2}", secs / 60, secs % 60);
        }

        public void EnableMinimize()
        {
            ControlBox = true;
            MinimizeBox = true;
            ShowInTaskbar = true;

            IntPtr hMenu = Win32Declarations.GetSystemMenu( Handle, false );
            int cItems = Win32Declarations.GetMenuItemCount( hMenu );
            Win32Declarations.RemoveMenu( hMenu, (uint) (cItems-1), Win32Declarations.MF_BYPOSITION );
            Win32Declarations.RemoveMenu( hMenu, (uint) (cItems-2), Win32Declarations.MF_BYPOSITION );
            Win32Declarations.DrawMenuBar( Handle );
        }

        private void _tmrUpdateElapsedTime_Tick( object sender, EventArgs e )
        {
            UpdateElapsedTime();
        }
    }

    internal class MockProgressWindow: IProgressWindow
    {
        private readonly bool _logToConsole;

        internal MockProgressWindow()
        {
            _logToConsole = false;
        }

        internal MockProgressWindow( bool logToConsole )
        {
            _logToConsole = logToConsole;
        }

        public void UpdateProgress( int percentage, string message, string timeMessage )
        {
            if ( _logToConsole )
            {
                Console.WriteLine( message );
            }
        }
    }
}
