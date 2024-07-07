// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea
{
    /// <summary>
    /// Summary description for CreditsDlg.
    /// </summary>
    public class CreditsDlg : DialogBase
    {
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Button _btnOK;
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        public CreditsDlg()
        {
            //
            // Required for Windows Form Designer support
            //
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
            this.label1 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label14 = new System.Windows.Forms.Label();
            this._btnOK = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.label1.Location = new System.Drawing.Point(8, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 16);
            this.label1.TabIndex = 0;
            this.label1.Text = "Developers:";
            //
            // label2
            //
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(144, 8);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 50);
            this.label2.TabIndex = 1;
            this.label2.Text = "Michael Gerasimov\r\nVyacheslav Lukianov\r\nSerge Baltic";
            //
            // label4
            //
            this.label4.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label4.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.label4.Location = new System.Drawing.Point(8, 70);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(100, 38);
            this.label4.TabIndex = 2;
            this.label4.Text = "Thanks for\r\ncontribution:";
            //
            // label3
            //
            this.label3.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label3.Location = new System.Drawing.Point(144, 70);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(116, 38);
            this.label3.TabIndex = 3;
            this.label3.Text = "Julia Repina (graphics)\r\nKatya Musienko (styles)";
            //
            // label6
            //
            this.label6.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label6.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.label6.Location = new System.Drawing.Point(8, 120);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(100, 38);
            this.label6.TabIndex = 4;
            this.label6.Text = "Special thanks\r\nto former crew:";
            //
            // label5
            //
            this.label5.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label5.Location = new System.Drawing.Point(144, 120);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(116, 140);
            this.label5.TabIndex = 5;
            this.label5.Text = "Sergey Dmitriev (PM)\r\nDmitry Jemerov\r\nSergey Zhulin\r\nLev Serebryakov\r\nTatiana Speranskaya\r\nElizaveta Revyakina\r\nRobert Palomo\r\nAlexandra Maximova";
            //
            // label7
            //
            this.label7.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label7.Location = new System.Drawing.Point(144, 132);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(116, 16);
            this.label7.TabIndex = 7;
            this.label7.Text = "Tatiana Speranskaya";
            //
            // label8
            //
            this.label8.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label8.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.label8.Location = new System.Drawing.Point(8, 132);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(116, 16);
            this.label8.TabIndex = 6;
            this.label8.Text = "Quality Assurance";
            //
            // label9
            //
            this.label9.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label9.Location = new System.Drawing.Point(144, 156);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(116, 16);
            this.label9.TabIndex = 9;
            this.label9.Text = "Julia Repina";
            //
            // label10
            //
            this.label10.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label10.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.label10.Location = new System.Drawing.Point(8, 156);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(116, 16);
            this.label10.TabIndex = 8;
            this.label10.Text = "Visual Design";
            //
            // label11
            //
            this.label11.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label11.Location = new System.Drawing.Point(144, 180);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(116, 16);
            this.label11.TabIndex = 11;
            this.label11.Text = "Alexandra Maximova";
            //
            // label12
            //
            this.label12.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label12.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.label12.Location = new System.Drawing.Point(8, 180);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(116, 16);
            this.label12.TabIndex = 10;
            this.label12.Text = "Interface Design";
            //
            // label13
            //
            this.label13.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label13.Location = new System.Drawing.Point(144, 204);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(116, 28);
            this.label13.TabIndex = 13;
            this.label13.Text = "Robert Palomo\r\nElizaveta Revyakina";
            //
            // label14
            //
            this.label14.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label14.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.label14.Location = new System.Drawing.Point(8, 204);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(116, 16);
            this.label14.TabIndex = 12;
            this.label14.Text = "Technical Writers";
            //
            // _btnOK
            //
            this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._btnOK.Location = new System.Drawing.Point(180, 240);
            this._btnOK.Name = "_btnOK";
            this._btnOK.TabIndex = 16;
            this._btnOK.Text = "OK";
            //
            // CreditsDlg
            //
            this.AcceptButton = this._btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._btnOK;
            this.ClientSize = new System.Drawing.Size(264, 272);
            this.Controls.Add(this._btnOK);
/*
            this.Controls.Add(this.label13);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label8);
*/
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "CreditsDlg";
            this.Text = "The Omea Team";
            this.ResumeLayout(false);

        }
        #endregion
    }
}
