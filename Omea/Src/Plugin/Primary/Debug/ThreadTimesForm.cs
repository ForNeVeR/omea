// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Threading;
using System.Windows.Forms;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Diagnostics;

namespace JetBrains.Omea.DebugPlugin
{
	/// <summary>
	/// Summary description for ThreadTimesForm.
	/// </summary>
	public class ThreadTimesForm : System.Windows.Forms.Form
	{
        private System.Windows.Forms.ListView _listView;
        private System.Windows.Forms.Button _refreshButton;
        private System.Windows.Forms.Button _closeButton;
        private System.Windows.Forms.ColumnHeader _nameHeader;
        private System.Windows.Forms.ColumnHeader _kernelTimeHeader;
        private System.Windows.Forms.ColumnHeader _userTimeHeader;
        private System.Windows.Forms.ContextMenu contextMenu1;
        private System.Windows.Forms.MenuItem _contextMenu;
        private System.Windows.Forms.MenuItem menuItem1;
        private System.Windows.Forms.MenuItem menuItem2;
        private System.Windows.Forms.MenuItem menuItem3;
        private System.Windows.Forms.MenuItem menuItem4;
        private System.Windows.Forms.MenuItem menuItem5;
        private System.Windows.Forms.MenuItem menuItem6;
        private System.Windows.Forms.MenuItem menuItem7;
        private System.Windows.Forms.MenuItem menuItem8;
        private System.Windows.Forms.ColumnHeader _priorityHeader;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ThreadTimesForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		    RefreshItems( false );
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
            this._listView = new System.Windows.Forms.ListView();
            this._nameHeader = new System.Windows.Forms.ColumnHeader();
            this._kernelTimeHeader = new System.Windows.Forms.ColumnHeader();
            this._userTimeHeader = new System.Windows.Forms.ColumnHeader();
            this.contextMenu1 = new System.Windows.Forms.ContextMenu();
            this._contextMenu = new System.Windows.Forms.MenuItem();
            this.menuItem1 = new System.Windows.Forms.MenuItem();
            this.menuItem2 = new System.Windows.Forms.MenuItem();
            this._refreshButton = new System.Windows.Forms.Button();
            this._closeButton = new System.Windows.Forms.Button();
            this.menuItem3 = new System.Windows.Forms.MenuItem();
            this.menuItem4 = new System.Windows.Forms.MenuItem();
            this.menuItem5 = new System.Windows.Forms.MenuItem();
            this.menuItem6 = new System.Windows.Forms.MenuItem();
            this.menuItem7 = new System.Windows.Forms.MenuItem();
            this.menuItem8 = new System.Windows.Forms.MenuItem();
            this._priorityHeader = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();
            //
            // _listView
            //
            this._listView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
                                                                                        this._nameHeader,
                                                                                        this._priorityHeader,
                                                                                        this._kernelTimeHeader,
                                                                                        this._userTimeHeader});
            this._listView.ContextMenu = this.contextMenu1;
            this._listView.FullRowSelect = true;
            this._listView.Location = new System.Drawing.Point(8, 12);
            this._listView.Name = "_listView";
            this._listView.Size = new System.Drawing.Size(492, 168);
            this._listView.TabIndex = 0;
            this._listView.View = System.Windows.Forms.View.Details;
            //
            // _nameHeader
            //
            this._nameHeader.Text = "Name";
            this._nameHeader.Width = 120;
            //
            // _kernelTimeHeader
            //
            this._kernelTimeHeader.Text = "Kernel Time";
            this._kernelTimeHeader.Width = 108;
            //
            // _userTimeHeader
            //
            this._userTimeHeader.Text = "User Time";
            this._userTimeHeader.Width = 108;
            //
            // contextMenu1
            //
            this.contextMenu1.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                                         this._contextMenu,
                                                                                         this.menuItem1,
                                                                                         this.menuItem2,
                                                                                         this.menuItem3});
            //
            // _contextMenu
            //
            this._contextMenu.Index = 0;
            this._contextMenu.Text = "Sleep thread on 10 seconds";
            this._contextMenu.Click += new System.EventHandler(this.sleepOn10Sec);
            //
            // menuItem1
            //
            this.menuItem1.Index = 1;
            this.menuItem1.Text = "Throw System.Exception";
            this.menuItem1.Click += new System.EventHandler(this.menuItem1_Click);
            //
            // menuItem2
            //
            this.menuItem2.Index = 2;
            this.menuItem2.Text = "Abort";
            this.menuItem2.Click += new System.EventHandler(this.menuItem2_Click);
            //
            // _refreshButton
            //
            this._refreshButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._refreshButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._refreshButton.Location = new System.Drawing.Point(340, 192);
            this._refreshButton.Name = "_refreshButton";
            this._refreshButton.TabIndex = 1;
            this._refreshButton.Text = "Refresh";
            this._refreshButton.Click += new System.EventHandler(this._refreshButton_Click);
            //
            // _closeButton
            //
            this._closeButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._closeButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._closeButton.Location = new System.Drawing.Point(424, 192);
            this._closeButton.Name = "_closeButton";
            this._closeButton.TabIndex = 2;
            this._closeButton.Text = "Close";
            this._closeButton.Click += new System.EventHandler(this._closeButton_Click);
            //
            // menuItem3
            //
            this.menuItem3.Index = 3;
            this.menuItem3.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
                                                                                      this.menuItem4,
                                                                                      this.menuItem5,
                                                                                      this.menuItem6,
                                                                                      this.menuItem7,
                                                                                      this.menuItem8});
            this.menuItem3.Text = "Priority";
            this.menuItem3.Popup += new System.EventHandler(this.menuItem3_Popup);
            //
            // menuItem4
            //
            this.menuItem4.Index = 0;
            this.menuItem4.Text = "Lowest";
            this.menuItem4.Click += new System.EventHandler(this.menuItem4_Click);
            //
            // menuItem5
            //
            this.menuItem5.Index = 1;
            this.menuItem5.Text = "Below Normal";
            this.menuItem5.Click += new System.EventHandler(this.menuItem5_Click);
            //
            // menuItem6
            //
            this.menuItem6.Index = 2;
            this.menuItem6.Text = "Normal";
            this.menuItem6.Click += new System.EventHandler(this.menuItem6_Click);
            //
            // menuItem7
            //
            this.menuItem7.Index = 3;
            this.menuItem7.Text = "Above Normal";
            this.menuItem7.Click += new System.EventHandler(this.menuItem7_Click);
            //
            // menuItem8
            //
            this.menuItem8.Index = 4;
            this.menuItem8.Text = "Highest ";
            this.menuItem8.Click += new System.EventHandler(this.menuItem8_Click);
            //
            // _priorityHeader
            //
            this._priorityHeader.Text = "Priority";
            this._priorityHeader.Width = 100;
            //
            // ThreadTimesForm
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(512, 226);
            this.Controls.Add(this._closeButton);
            this.Controls.Add(this._refreshButton);
            this.Controls.Add(this._listView);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ThreadTimesForm";
            this.Text = "Thread Times";
            this.ResumeLayout(false);

        }
		#endregion

        private void _refreshButton_Click(object sender, System.EventArgs e)
        {
            RefreshItems( true );
        }

	    private void RefreshItems( bool displayThreadTimes )
	    {
	        AsyncProcessor[] procs = AsyncProcessor.GetAllPooledProcessors();
	        _listView.BeginUpdate();
	        try
	        {
	            _listView.Items.Clear();
	            foreach( AsyncProcessor proc in procs )
	            {
	                ListViewItem item = new ListViewItem();
                    item.Tag = proc;
	                item.Text = proc.ThreadName;
                    item.SubItems.Add( proc.ThreadPriority.ToString() );
                    if( displayThreadTimes )
                    {
                        item.SubItems.Add( proc.GetKernelTime().ToString() );
                        item.SubItems.Add( proc.GetUserTime().ToString() );
                    }
	                _listView.Items.Add( item );
	            }
	        }
	        finally
	        {
	            _listView.EndUpdate();
	        }
	    }

	    private void _closeButton_Click(object sender, System.EventArgs e)
        {
            Close();
        }

        private void sleepOn10Sec(object sender, System.EventArgs e)
        {
            foreach ( ListViewItem item in _listView.SelectedItems )
            {
                AsyncProcessor proc =  item.Tag as AsyncProcessor;
                if ( proc != null )
                {
                    proc.QueueJob( "10 seconds sleeping", new MethodInvoker( Sleep10Seconds ) );
                }
            }
        }

        private void menuItem1_Click(object sender, System.EventArgs e)
        {
            foreach ( ListViewItem item in _listView.SelectedItems )
            {
                AsyncProcessor proc =  item.Tag as AsyncProcessor;
                if ( proc != null )
                {
                    proc.QueueJob( "Throw Exception", new MethodInvoker( ExceptionJob ) );
                }
            }
        }
        private void menuItem2_Click(object sender, System.EventArgs e)
        {
            foreach ( ListViewItem item in _listView.SelectedItems )
            {
                AsyncProcessor proc =  item.Tag as AsyncProcessor;
                if ( proc != null )
                {
                    proc.Thread.Abort();
                }
            }
        }
        private void menuItem4_Click(object sender, System.EventArgs e)
        {
            SetThreadPriority( ThreadPriority.Lowest );
        }

	    private void menuItem5_Click(object sender, System.EventArgs e)
        {
            SetThreadPriority( ThreadPriority.BelowNormal );
        }

        private void menuItem6_Click(object sender, System.EventArgs e)
        {
            SetThreadPriority( ThreadPriority.Normal );
        }

        private void menuItem7_Click(object sender, System.EventArgs e)
        {
            SetThreadPriority( ThreadPriority.AboveNormal );
        }

        private void menuItem8_Click(object sender, System.EventArgs e)
        {
            SetThreadPriority( ThreadPriority.Highest );
        }

        private void menuItem3_Popup(object sender, System.EventArgs e)
        {
            ThreadPriority priority = ThreadPriority.Normal;
            foreach ( ListViewItem item in _listView.SelectedItems )
            {
                AsyncProcessor proc =  item.Tag as AsyncProcessor;
                if ( proc != null )
                {
                    priority = proc.ThreadPriority;
                }
            }
            menuItem4.Checked = menuItem5.Checked = menuItem6.Checked = menuItem7.Checked = menuItem8.Checked = false;
            switch( priority )
            {
                    case ThreadPriority.Lowest: menuItem4.Checked = true; break;
                    case ThreadPriority.BelowNormal: menuItem5.Checked = true; break;
                    case ThreadPriority.Normal: menuItem6.Checked = true; break;
                    case ThreadPriority.AboveNormal: menuItem7.Checked = true; break;
                    case ThreadPriority.Highest: menuItem8.Checked = true; break;
            }
        }

        private void SetThreadPriority( ThreadPriority priority )
        {
            foreach ( ListViewItem item in _listView.SelectedItems )
            {
                AsyncProcessor proc =  item.Tag as AsyncProcessor;
                if ( proc != null )
                {
                    proc.ThreadPriority = priority;
                }
            }
        }

        private void Sleep10Seconds()
        {
            Tracer._Trace( "Thread Sleeper: Prepare to sleep" );
            Thread.Sleep( 10000 );
            Tracer._Trace( "Thread Sleeper: OK. Wake up" );
        }

        private void ExceptionJob()
        {
            throw new Exception( "ExceptionJob" );
        }
	}
}
