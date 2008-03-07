/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceStore;
using JetBrains.Omea.Containers;
using System.Text;
using JetBrains.Omea.GUIControls;
using JetBrains.DataStructures;

namespace JetBrains.Omea.DebugPlugin
{
	/**
     * The form for viewing some internal metrics of OmniaMea
     */
    
    public class CacheWatch : DialogBase
	{
        private System.Windows.Forms.Button _btnGC;
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label _lblWeakCacheCount;
        private System.Windows.Forms.Timer _tmrUpdate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label _lblLiveResourceListCount;
        private System.Windows.Forms.Label _lblResourceWeakCacheDetails;
        private System.Windows.Forms.Button _btnWeakCacheDetails;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label _lblUpdatingResourceCount;
        private System.Windows.Forms.Button _btnTraceLiveLists;

		public CacheWatch()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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
            this._btnGC = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this._lblWeakCacheCount = new System.Windows.Forms.Label();
            this._tmrUpdate = new System.Windows.Forms.Timer(this.components);
            this.label2 = new System.Windows.Forms.Label();
            this._lblLiveResourceListCount = new System.Windows.Forms.Label();
            this._lblResourceWeakCacheDetails = new System.Windows.Forms.Label();
            this._btnWeakCacheDetails = new System.Windows.Forms.Button();
            this._btnTraceLiveLists = new System.Windows.Forms.Button();
            this._lblUpdatingResourceCount = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // _btnGC
            // 
            this._btnGC.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnGC.Location = new System.Drawing.Point(8, 92);
            this._btnGC.Name = "_btnGC";
            this._btnGC.Size = new System.Drawing.Size(132, 24);
            this._btnGC.TabIndex = 0;
            this._btnGC.Text = "Collect Garbage";
            this._btnGC.Click += new System.EventHandler(this._btnGC_Click);
            // 
            // label1
            // 
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(8, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(156, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "ResourceWeakCache Count:";
            // 
            // _lblWeakCacheCount
            // 
            this._lblWeakCacheCount.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblWeakCacheCount.Location = new System.Drawing.Point(188, 9);
            this._lblWeakCacheCount.Name = "_lblWeakCacheCount";
            this._lblWeakCacheCount.Size = new System.Drawing.Size(48, 17);
            this._lblWeakCacheCount.TabIndex = 2;
            this._lblWeakCacheCount.Text = "label2";
            // 
            // _tmrUpdate
            // 
            this._tmrUpdate.Enabled = true;
            this._tmrUpdate.Interval = 500;
            this._tmrUpdate.Tick += new System.EventHandler(this._tmrUpdate_Tick);
            // 
            // label2
            // 
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(8, 30);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(140, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "LiveResourceList Count:";
            // 
            // _lblLiveResourceListCount
            // 
            this._lblLiveResourceListCount.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblLiveResourceListCount.Location = new System.Drawing.Point(188, 30);
            this._lblLiveResourceListCount.Name = "_lblLiveResourceListCount";
            this._lblLiveResourceListCount.Size = new System.Drawing.Size(48, 17);
            this._lblLiveResourceListCount.TabIndex = 4;
            this._lblLiveResourceListCount.Text = "label2";
            // 
            // _lblResourceWeakCacheDetails
            // 
            this._lblResourceWeakCacheDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._lblResourceWeakCacheDetails.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblResourceWeakCacheDetails.Location = new System.Drawing.Point(256, 9);
            this._lblResourceWeakCacheDetails.Name = "_lblResourceWeakCacheDetails";
            this._lblResourceWeakCacheDetails.Size = new System.Drawing.Size(180, 174);
            this._lblResourceWeakCacheDetails.TabIndex = 5;
            this._lblResourceWeakCacheDetails.Text = "label3";
            // 
            // _btnWeakCacheDetails
            // 
            this._btnWeakCacheDetails.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnWeakCacheDetails.Location = new System.Drawing.Point(8, 120);
            this._btnWeakCacheDetails.Name = "_btnWeakCacheDetails";
            this._btnWeakCacheDetails.Size = new System.Drawing.Size(132, 25);
            this._btnWeakCacheDetails.TabIndex = 6;
            this._btnWeakCacheDetails.Text = "Cache Details";
            this._btnWeakCacheDetails.Click += new System.EventHandler(this._btnWeakCacheDetails_Click);
            // 
            // _btnTraceLiveLists
            // 
            this._btnTraceLiveLists.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnTraceLiveLists.Location = new System.Drawing.Point(8, 152);
            this._btnTraceLiveLists.Name = "_btnTraceLiveLists";
            this._btnTraceLiveLists.Size = new System.Drawing.Size(132, 23);
            this._btnTraceLiveLists.TabIndex = 7;
            this._btnTraceLiveLists.Text = "Trace live lists";
            this._btnTraceLiveLists.Click += new System.EventHandler(this._btnTraceLiveLists_Click);
            // 
            // _lblUpdatingResourceCount
            // 
            this._lblUpdatingResourceCount.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._lblUpdatingResourceCount.Location = new System.Drawing.Point(188, 52);
            this._lblUpdatingResourceCount.Name = "_lblUpdatingResourceCount";
            this._lblUpdatingResourceCount.Size = new System.Drawing.Size(48, 17);
            this._lblUpdatingResourceCount.TabIndex = 9;
            this._lblUpdatingResourceCount.Text = "label2";
            // 
            // label4
            // 
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label4.Location = new System.Drawing.Point(8, 52);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(140, 17);
            this.label4.TabIndex = 8;
            this.label4.Text = "Updating resource count:";
            // 
            // CacheWatch
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(440, 186);
            this.Controls.Add(this._lblUpdatingResourceCount);
            this.Controls.Add(this.label4);
            this.Controls.Add(this._btnTraceLiveLists);
            this.Controls.Add(this._btnWeakCacheDetails);
            this.Controls.Add(this._lblResourceWeakCacheDetails);
            this.Controls.Add(this._lblLiveResourceListCount);
            this.Controls.Add(this.label2);
            this.Controls.Add(this._lblWeakCacheCount);
            this.Controls.Add(this.label1);
            this.Controls.Add(this._btnGC);
            this.Name = "CacheWatch";
            this.Text = "CacheWatch";
            this.TopMost = true;
            this.ResumeLayout(false);

        }
		#endregion

        private void _btnGC_Click( object sender, System.EventArgs e )
        {
            GC.Collect();
        }

        private void _tmrUpdate_Tick(object sender, System.EventArgs e)
        {
            if ( MyPalStorage.Storage != null )
            {
                _lblWeakCacheCount.Text = MyPalStorage.Storage.GetResourceWeakCacheCount().ToString();
                _lblLiveResourceListCount.Text = MyPalStorage.Storage.ResourceSavedHandlerCount.ToString();
                _lblUpdatingResourceCount.Text = MyPalStorage.Storage.GetUpdatingResourceCount().ToString();
            }
        }

        private void _btnWeakCacheDetails_Click(object sender, System.EventArgs e)
        {
            HashSet hashSet = new HashSet();
            int totalMemorySize = 0;
            
            CountedSet cacheEntries = new CountedSet();
            foreach( IntHashTable.Entry entry in MyPalStorage.Storage.ResourceWeakCache )
            {
                WeakReference weakRef = (WeakReference) entry.Value;
                Resource res = (Resource) weakRef.Target;
                hashSet.Add( res.Id );
                cacheEntries.Add( res.Type );
                totalMemorySize += res.EstimateMemorySize();
            }
            foreach( Resource res in MyPalStorage.Storage.ResourceCache )
            {
                if ( res != null && !hashSet.Contains( res.Id ) )
                {
                    cacheEntries.Add( res.Type );
                    totalMemorySize += res.EstimateMemorySize();
                }
            }

            StringBuilder builder = new StringBuilder();
            cacheEntries.SortByCount();
            foreach( CountedSet.Entry de in cacheEntries )
            {
                builder.Append( de.Count + " - " + de.Value + "\n");
            }
            builder.Append( "Estimated memory size " + totalMemorySize / 1024 + "K" );
            _lblResourceWeakCacheDetails.Text = builder.ToString();
        }

        private void _btnTraceLiveLists_Click(object sender, System.EventArgs e)
        {
            MyPalStorage.Storage.TraceLiveResourceLists();
        }
    }
}
