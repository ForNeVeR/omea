// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using DBIndex;

namespace JetBrains.Omea.MemoryWatchPlugin
{
	public class MemoryWatch : System.Windows.Forms.Form
	{
        private System.Windows.Forms.Label _lblTotalMemory;
        private System.Windows.Forms.Timer _tmrUpdate;
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.Label _lblManagedMemory;
        private System.Windows.Forms.Label _lblHeapFreeSize;
        private System.Windows.Forms.Label _lblManagedHeapOverhead;

        private PerformanceCounter _ctrPrivateBytes;
        private PerformanceCounter _ctrBytesInAllHeaps;
        private System.Windows.Forms.Label _lblWin32HeapSize;
        private System.Windows.Forms.Label _lblOtherMemorySize;
        private System.Windows.Forms.Button _btnDumpHeaps;
        private System.Windows.Forms.SaveFileDialog _dlgSaveDump;
        private System.Windows.Forms.Label _dbindexHeapInfo;
        private System.Windows.Forms.Label _mapiHeapInfo;
        private PerformanceCounter _ctrBytesCommitted;

		public MemoryWatch()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

            string exeName = Path.GetFileNameWithoutExtension( Application.ExecutablePath );
            _ctrPrivateBytes = new PerformanceCounter( "Process", "Private Bytes", exeName );
            _ctrBytesInAllHeaps = new PerformanceCounter( ".NET CLR Memory", "# Bytes in all Heaps", exeName );
            _ctrBytesCommitted = new PerformanceCounter( ".NET CLR Memory", "# Total committed Bytes", exeName );
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
            this._lblTotalMemory = new System.Windows.Forms.Label();
            this._tmrUpdate = new System.Windows.Forms.Timer(this.components);
            this._lblManagedMemory = new System.Windows.Forms.Label();
            this._lblHeapFreeSize = new System.Windows.Forms.Label();
            this._lblManagedHeapOverhead = new System.Windows.Forms.Label();
            this._lblWin32HeapSize = new System.Windows.Forms.Label();
            this._lblOtherMemorySize = new System.Windows.Forms.Label();
            this._btnDumpHeaps = new System.Windows.Forms.Button();
            this._dlgSaveDump = new System.Windows.Forms.SaveFileDialog();
            this._dbindexHeapInfo = new System.Windows.Forms.Label();
            this._mapiHeapInfo = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // _lblTotalMemory
            //
            this._lblTotalMemory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblTotalMemory.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblTotalMemory.Location = new System.Drawing.Point(4, 8);
            this._lblTotalMemory.Name = "_lblTotalMemory";
            this._lblTotalMemory.Size = new System.Drawing.Size(324, 16);
            this._lblTotalMemory.TabIndex = 0;
            //
            // _tmrUpdate
            //
            this._tmrUpdate.Enabled = true;
            this._tmrUpdate.Interval = 1000;
            this._tmrUpdate.Tick += new System.EventHandler(this._tmrUpdate_Tick);
            //
            // _lblManagedMemory
            //
            this._lblManagedMemory.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblManagedMemory.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblManagedMemory.Location = new System.Drawing.Point(4, 28);
            this._lblManagedMemory.Name = "_lblManagedMemory";
            this._lblManagedMemory.Size = new System.Drawing.Size(324, 20);
            this._lblManagedMemory.TabIndex = 1;
            //
            // _lblHeapFreeSize
            //
            this._lblHeapFreeSize.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblHeapFreeSize.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblHeapFreeSize.Location = new System.Drawing.Point(4, 52);
            this._lblHeapFreeSize.Name = "_lblHeapFreeSize";
            this._lblHeapFreeSize.Size = new System.Drawing.Size(324, 20);
            this._lblHeapFreeSize.TabIndex = 2;
            //
            // _lblManagedHeapOverhead
            //
            this._lblManagedHeapOverhead.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblManagedHeapOverhead.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblManagedHeapOverhead.Location = new System.Drawing.Point(4, 76);
            this._lblManagedHeapOverhead.Name = "_lblManagedHeapOverhead";
            this._lblManagedHeapOverhead.Size = new System.Drawing.Size(324, 16);
            this._lblManagedHeapOverhead.TabIndex = 3;
            //
            // _lblWin32HeapSize
            //
            this._lblWin32HeapSize.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblWin32HeapSize.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblWin32HeapSize.Location = new System.Drawing.Point(4, 96);
            this._lblWin32HeapSize.Name = "_lblWin32HeapSize";
            this._lblWin32HeapSize.Size = new System.Drawing.Size(324, 16);
            this._lblWin32HeapSize.TabIndex = 4;
            //
            // _lblOtherMemorySize
            //
            this._lblOtherMemorySize.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblOtherMemorySize.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblOtherMemorySize.Location = new System.Drawing.Point(4, 116);
            this._lblOtherMemorySize.Name = "_lblOtherMemorySize";
            this._lblOtherMemorySize.Size = new System.Drawing.Size(324, 16);
            this._lblOtherMemorySize.TabIndex = 4;
            //
            // _btnDumpHeaps
            //
            this._btnDumpHeaps.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnDumpHeaps.Location = new System.Drawing.Point(4, 180);
            this._btnDumpHeaps.Name = "_btnDumpHeaps";
            this._btnDumpHeaps.Size = new System.Drawing.Size(140, 23);
            this._btnDumpHeaps.TabIndex = 5;
            this._btnDumpHeaps.Text = "Dump Win32 Heaps";
            this._btnDumpHeaps.Click += new System.EventHandler(this._btnDumpHeaps_Click);
            //
            // _dbindexHeapInfo
            //
            this._dbindexHeapInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._dbindexHeapInfo.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._dbindexHeapInfo.Location = new System.Drawing.Point(4, 136);
            this._dbindexHeapInfo.Name = "_dbindexHeapInfo";
            this._dbindexHeapInfo.Size = new System.Drawing.Size(324, 16);
            this._dbindexHeapInfo.TabIndex = 6;
            //
            // _mapiHeapInfo
            //
            this._mapiHeapInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._mapiHeapInfo.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._mapiHeapInfo.Location = new System.Drawing.Point(4, 156);
            this._mapiHeapInfo.Name = "_mapiHeapInfo";
            this._mapiHeapInfo.Size = new System.Drawing.Size(324, 16);
            this._mapiHeapInfo.TabIndex = 7;
            //
            // MemoryWatch
            //
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(332, 208);
            this.Controls.Add(this._mapiHeapInfo);
            this.Controls.Add(this._dbindexHeapInfo);
            this.Controls.Add(this._btnDumpHeaps);
            this.Controls.Add(this._lblWin32HeapSize);
            this.Controls.Add(this._lblManagedHeapOverhead);
            this.Controls.Add(this._lblHeapFreeSize);
            this.Controls.Add(this._lblManagedMemory);
            this.Controls.Add(this._lblTotalMemory);
            this.Controls.Add(this._lblOtherMemorySize);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "MemoryWatch";
            this.Text = "MemoryWatch";
            this.TopMost = true;
            this.ResumeLayout(false);

        }
		#endregion

        private void _tmrUpdate_Tick(object sender, System.EventArgs e)
        {
            long managedMemory = GC.GetTotalMemory( true );
            long privateBytes = _ctrPrivateBytes.NextSample().RawValue;
            long bytesInAllHeaps = _ctrBytesInAllHeaps.NextSample().RawValue;
            long bytesCommitted = _ctrBytesCommitted.NextSample().RawValue;
            uint win32Heaps = Win32Heaps.TotalHeapSize();
            int dbIndexHeapSize = OmniaMeaBTree.GetUsedMemory();
            int dbIndexObjectsCount = OmniaMeaBTree.GetObjectsCount();
            int mapiHeapSize = EMAPILib.EMAPISession.HeapSize();
            int mapiObjectsCount = EMAPILib.EMAPISession.ObjectsCount();
            _lblTotalMemory.Text = "Total Memory:" + FormatMemorySize( privateBytes );
            _lblManagedMemory.Text = "Managed Memory: " + FormatMemorySize( managedMemory );
            _lblHeapFreeSize.Text = "Managed Heap Free Size: " + FormatMemorySize( bytesInAllHeaps - managedMemory );
            _lblManagedHeapOverhead.Text = "Managed Heap Overhead: " + FormatMemorySize( bytesCommitted - bytesInAllHeaps );
            _lblWin32HeapSize.Text = "Win32 Heap Size: " + FormatMemorySize( win32Heaps );
            _lblOtherMemorySize.Text = "Other Memory: " + FormatMemorySize( privateBytes - bytesCommitted - win32Heaps );
            _dbindexHeapInfo.Text = "DBIndex Heap Size: " + FormatMemorySize( dbIndexHeapSize ) + ", DBIndex Objects Count: " + dbIndexObjectsCount;
            _mapiHeapInfo.Text = "MAPI Heap Size: "  + FormatMemorySize( mapiHeapSize ) + ", MAPI Objects Count: " + mapiObjectsCount;
        }

        private string FormatMemorySize( long memSize )
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalDigits = 1;
            if( memSize > 100 * 1024 )
            {
                double megaBytes = (double) memSize / (1024*1024);
                return megaBytes.ToString( "N", nfi ) + "M";
            }
            else
            {
                double kiloBytes = (double) memSize / 1024;
                return kiloBytes.ToString( "N", nfi ) + "K";
            }
        }

        private void _btnDumpHeaps_Click(object sender, System.EventArgs e)
        {
            if ( _dlgSaveDump.ShowDialog( this ) == DialogResult.OK )
            {
                Win32Heaps.Dump( _dlgSaveDump.FileName );
            }
        }
	}
}
