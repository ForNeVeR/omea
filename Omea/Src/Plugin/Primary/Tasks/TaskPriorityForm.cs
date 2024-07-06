// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;

namespace Tasks
{
	/// <summary>
	/// Summary description for TaskPriorityForm.
	/// </summary>
	public class TaskPriorityForm : DialogBase
	{
        private Button      buttonOK;
        private Button      buttonCancel;
        private GroupBox    groupPriorities;
        private RadioButton radioHigh;
        private RadioButton radioNormal;
        private RadioButton radioLow;

        private string      ValueString, RepresentationString;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public TaskPriorityForm( string currentValue )
		{
			InitializeComponent();

            if( String.IsNullOrEmpty( currentValue ) || currentValue == "0" )
                radioNormal.Checked = true;
            else
            if( currentValue == "1" )
                radioHigh.Checked = true;
            else
                radioLow.Checked = true;
		}

        public string  Value          {  get{  return ValueString;  }  }
        public string  Representation {  get{  return RepresentationString;  }  }

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
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.groupPriorities = new System.Windows.Forms.GroupBox();
            this.radioLow = new System.Windows.Forms.RadioButton();
            this.radioNormal = new System.Windows.Forms.RadioButton();
            this.radioHigh = new System.Windows.Forms.RadioButton();
            this.groupPriorities.SuspendLayout();
            this.SuspendLayout();
            //
            // buttonOK
            //
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonOK.Location = new System.Drawing.Point(104, 16);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.TabIndex = 0;
            this.buttonOK.Text = "OK";
            //
            // buttonCancel
            //
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonCancel.Location = new System.Drawing.Point(104, 48);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.TabIndex = 0;
            this.buttonCancel.Text = "Cancel";
            //
            // groupPriorities
            //
            this.groupPriorities.Controls.Add(this.radioLow);
            this.groupPriorities.Controls.Add(this.radioNormal);
            this.groupPriorities.Controls.Add(this.radioHigh);
            this.groupPriorities.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.groupPriorities.Location = new System.Drawing.Point(4, 8);
            this.groupPriorities.Name = "groupPriorities";
            this.groupPriorities.Size = new System.Drawing.Size(92, 100);
            this.groupPriorities.TabIndex = 1;
            this.groupPriorities.TabStop = false;
            this.groupPriorities.Text = "Task Priorities";
            //
            // radioLow
            //
            this.radioLow.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioLow.Location = new System.Drawing.Point(8, 72);
            this.radioLow.Name = "radioLow";
            this.radioLow.Size = new System.Drawing.Size(64, 22);
            this.radioLow.TabIndex = 2;
            this.radioLow.Text = "Low";
            this.radioLow.Tag = "2";
            this.radioLow.CheckedChanged += new System.EventHandler(radioLow_CheckedChanged);
            //
            // radioNormal
            //
            this.radioNormal.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioNormal.Location = new System.Drawing.Point(8, 44);
            this.radioNormal.Name = "radioNormal";
            this.radioNormal.Size = new System.Drawing.Size(64, 22);
            this.radioNormal.TabIndex = 1;
            this.radioNormal.Text = "Normal";
            this.radioNormal.Tag = "0";
            this.radioNormal.CheckedChanged += new System.EventHandler(radioLow_CheckedChanged);
            //
            // radioHigh
            //
            this.radioHigh.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.radioHigh.Location = new System.Drawing.Point(8, 16);
            this.radioHigh.Name = "radioHigh";
            this.radioHigh.Size = new System.Drawing.Size(64, 22);
            this.radioHigh.TabIndex = 0;
            this.radioHigh.Text = "High";
            this.radioHigh.Tag = "1";
            this.radioHigh.CheckedChanged += new System.EventHandler(radioLow_CheckedChanged);
            //
            // TaskPriorityForm
            //
            this.AcceptButton = this.buttonOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(186, 115);
            this.Controls.Add(this.groupPriorities);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "TaskPriorityForm";
            this.Text = "Select Task Priority";
            this.groupPriorities.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

        private void radioLow_CheckedChanged(object sender, EventArgs e)
        {
            if( ((RadioButton)sender).Checked )
            {
                ValueString = (string) ((RadioButton)sender).Tag;
                RepresentationString = ((RadioButton)sender).Text;
            }
        }
    }
}
