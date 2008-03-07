/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
    /// <summary>
    /// Allows the user to choose whether to index the complete data or the data for some
    /// recent period.
    /// </summary>
    public class IndexPeriodPane: AbstractOptionsPane
    {
        private System.Windows.Forms.GroupBox groupBox1;
        internal System.Windows.Forms.RadioButton _radFullIndex;
        internal System.Windows.Forms.RadioButton _radTwoWeeks;
        private System.Windows.Forms.Label label1;

        private bool    WasCheckedBefore = false;
        private System.Windows.Forms.CheckBox _chkIdleIndexing;
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        public IndexPeriodPane()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            // TODO: Add any initialization after the InitializeComponent call

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

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this._radFullIndex = new System.Windows.Forms.RadioButton();
            this._radTwoWeeks = new System.Windows.Forms.RadioButton();
            this.label1 = new System.Windows.Forms.Label();
            this._chkIdleIndexing = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this._chkIdleIndexing);
            this.groupBox1.Controls.Add(this._radFullIndex);
            this.groupBox1.Controls.Add(this._radTwoWeeks);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.Location = new System.Drawing.Point(0, 72);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(388, 80);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            // 
            // _radFullIndex
            // 
            this._radFullIndex.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._radFullIndex.Location = new System.Drawing.Point(8, 56);
            this._radFullIndex.Name = "_radFullIndex";
            this._radFullIndex.Size = new System.Drawing.Size(380, 16);
            this._radFullIndex.TabIndex = 1;
            this._radFullIndex.Text = "Index my &entire message archive (30-60 minutes or more)";
            this._radFullIndex.Click += new System.EventHandler(this.FullIndexing_Click);
            // 
            // _radTwoWeeks
            // 
            this._radTwoWeeks.Checked = true;
            this._radTwoWeeks.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._radTwoWeeks.Location = new System.Drawing.Point(8, 12);
            this._radTwoWeeks.Name = "_radTwoWeeks";
            this._radTwoWeeks.Size = new System.Drawing.Size(380, 20);
            this._radTwoWeeks.TabIndex = 0;
            this._radTwoWeeks.TabStop = true;
            this._radTwoWeeks.Text = "Index the messages for the &last two weeks (1-3 minutes)";
            this._radTwoWeeks.Click += new System.EventHandler(this.TwoWeeks_Click);
            // 
            // label1
            // 
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(388, 72);
            this.label1.TabIndex = 4;
            this.label1.Text = "Before you can use OmniaMea, it needs to build an index on your messages. Since build" +
                "ing an index for your entire e-mail and instant messages archive can take a long time, you can index " +
                "your messages for the last two weeks only. Would you like to do so?";
            // 
            // _chkIdleIndexing
            // 
            this._chkIdleIndexing.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            this._chkIdleIndexing.Checked = true;
            this._chkIdleIndexing.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkIdleIndexing.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._chkIdleIndexing.Location = new System.Drawing.Point(32, 32);
            this._chkIdleIndexing.Name = "_chkIdleIndexing";
            this._chkIdleIndexing.Size = new System.Drawing.Size(344, 20);
            this._chkIdleIndexing.TabIndex = 6;
            this._chkIdleIndexing.Text = "Index &remaining messages when my computer is idle";
            // 
            // IndexPeriodPane
            // 
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Name = "IndexPeriodPane";
            this.Size = new System.Drawing.Size(388, 196);
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        public override void ShowPane()
        {
            base.ShowPane();
            label1.Text = "Before you can use " + ICore.Instance.ProductName + 
                ", it needs to build an index on your messages.\r\n\r\nSince building an index for your entire e-mail " + 
                "and instant messages archive can take a long time, you can index " +
                "your messages for the last two weeks only.";
        }


        public override void OK()
        {
            DateTime indexStartDate = ( _radTwoWeeks.Checked ) 
                ? DateTime.Now.Date.AddDays( -14.0 )
                : DateTime.MinValue;
            
            ISettingStore ini = ICore.Instance.SettingStore;
            ini.WriteDate( "Startup", "IndexStartDate", indexStartDate );
            ini.WriteBool( "Startup", "IdleIndexing", _chkIdleIndexing.Checked );
        }

        private void FullIndexing_Click(object sender, EventArgs e)
        {
            WasCheckedBefore = _chkIdleIndexing.Checked;
            _chkIdleIndexing.Enabled = _chkIdleIndexing.Checked = false;
        }

        private void TwoWeeks_Click(object sender, EventArgs e)
        {
            _chkIdleIndexing.Enabled = true;
            _chkIdleIndexing.Checked = WasCheckedBefore;
        }
    }
}
