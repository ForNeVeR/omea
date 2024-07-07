// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Summary description for EditViewDateCondition.
	/// </summary>
	public class EditTimeSpanConditionForm : System.Windows.Forms.Form
	{
        public  string             TimeSpanDescription = "";
        private static string[]    SpanUnits = new string[] { "hours", "days", "weeks", "months", "years" };
        private static string[]    FixedAnchors = new string[] { "Today", "Tomorrow", "Yesterday", "This week", "Last week", "Next week", "This month", "Last month" };
        private System.Windows.Forms.CheckBox checkForAnchor;
        private System.Windows.Forms.CheckBox checkForLast;
        private System.Windows.Forms.CheckBox checkBefore;
        private System.Windows.Forms.CheckBox checkAfter;
        private System.Windows.Forms.ComboBox comboAnchors;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.ComboBox comboUnits;
        private System.Windows.Forms.DateTimePicker pickerBefore;
        private System.Windows.Forms.DateTimePicker pickerAfter;
        private System.Windows.Forms.NumericUpDown counterValues;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public EditTimeSpanConditionForm() : this( "" ) {}
		public EditTimeSpanConditionForm( string currentTimeSpan )
		{
			InitializeComponent();

            comboAnchors.SelectedIndex = comboUnits.SelectedIndex = 0;
            pickerBefore.Value = pickerAfter.Value = DateTime.Now;
            pickerBefore.Format = pickerAfter.Format = DateTimePickerFormat.Short;
            if( currentTimeSpan != "" )
                InitializeControls( currentTimeSpan );
            buttonOK.Enabled = AnythingChecked();
        }

		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
					components.Dispose();
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
            this.checkForAnchor = new System.Windows.Forms.CheckBox();
            this.checkForLast = new System.Windows.Forms.CheckBox();
            this.checkBefore = new System.Windows.Forms.CheckBox();
            this.checkAfter = new System.Windows.Forms.CheckBox();
            this.comboAnchors = new System.Windows.Forms.ComboBox();
            this.comboUnits = new System.Windows.Forms.ComboBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.pickerBefore = new System.Windows.Forms.DateTimePicker();
            this.pickerAfter = new System.Windows.Forms.DateTimePicker();
            this.counterValues = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.counterValues)).BeginInit();
            this.SuspendLayout();
            //
            // checkForAnchor
            //
            this.checkForAnchor.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkForAnchor.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.checkForAnchor.Location = new System.Drawing.Point(4, 8);
            this.checkForAnchor.Name = "checkForAnchor";
            this.checkForAnchor.Size = new System.Drawing.Size(50, 16);
            this.checkForAnchor.TabIndex = 0;
            this.checkForAnchor.Text = "&For";
            this.checkForAnchor.CheckedChanged += new System.EventHandler(this.checkForAnchor_CheckedChanged);
            //
            // checkForLast
            //
            this.checkForLast.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkForLast.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.checkForLast.Location = new System.Drawing.Point(4, 36);
            this.checkForLast.Name = "checkForLast";
            this.checkForLast.Size = new System.Drawing.Size(64, 16);
            this.checkForLast.TabIndex = 2;
            this.checkForLast.Text = "For &last";
            this.checkForLast.CheckedChanged += new System.EventHandler(this.checkForLast_CheckedChanged);
            //
            // checkBefore
            //
            this.checkBefore.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkBefore.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.checkBefore.Location = new System.Drawing.Point(4, 92);
            this.checkBefore.Name = "checkBefore";
            this.checkBefore.Size = new System.Drawing.Size(64, 16);
            this.checkBefore.TabIndex = 5;
            this.checkBefore.Text = "&Before";
            this.checkBefore.CheckedChanged += new System.EventHandler(this.checkBefore_CheckedChanged);
            //
            // checkAfter
            //
            this.checkAfter.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkAfter.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.checkAfter.Location = new System.Drawing.Point(4, 64);
            this.checkAfter.Name = "checkAfter";
            this.checkAfter.Size = new System.Drawing.Size(64, 12);
            this.checkAfter.TabIndex = 7;
            this.checkAfter.Text = "&After";
            this.checkAfter.CheckedChanged += new System.EventHandler(this.checkAfter_CheckedChanged);
            //
            // comboAnchors
            //
            this.comboAnchors.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboAnchors.Enabled = false;
            this.comboAnchors.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.comboAnchors.Items.AddRange(new object[] {
                                                              "Today",
                                                              "Tomorrow",
                                                              "Yesterday",
                                                              "This week",
                                                              "Last week",
                                                              "Next week",
                                                              "This month",
                                                              "Last month"});
            this.comboAnchors.Location = new System.Drawing.Point(72, 4);
            this.comboAnchors.Name = "comboAnchors";
            this.comboAnchors.Size = new System.Drawing.Size(128, 21);
            this.comboAnchors.TabIndex = 1;
            //
            // comboUnits
            //
            this.comboUnits.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboUnits.Enabled = false;
            this.comboUnits.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.comboUnits.Items.AddRange(new object[] {
                                                            "hours",
                                                            "days",
                                                            "weeks",
                                                            "months",
                                                            "years"});
            this.comboUnits.Location = new System.Drawing.Point(136, 33);
            this.comboUnits.Name = "comboUnits";
            this.comboUnits.Size = new System.Drawing.Size(64, 21);
            this.comboUnits.TabIndex = 4;
            //
            // buttonOK
            //
            this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonOK.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.buttonOK.Location = new System.Drawing.Point(40, 116);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.TabIndex = 3;
            this.buttonOK.Text = "OK";
            this.buttonOK.Click += new System.EventHandler(this.buttonOkClick);
            //
            // buttonCancel
            //
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonCancel.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.buttonCancel.Location = new System.Drawing.Point(124, 116);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.TabIndex = 3;
            this.buttonCancel.Text = "Cancel";
            //
            // pickerBefore
            //
            this.pickerBefore.Enabled = false;
            this.pickerBefore.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.pickerBefore.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.pickerBefore.Location = new System.Drawing.Point(72, 87);
            this.pickerBefore.Name = "pickerBefore";
            this.pickerBefore.Size = new System.Drawing.Size(128, 21);
            this.pickerBefore.TabIndex = 6;
            this.pickerBefore.ValueChanged += new System.EventHandler(this.pickerBefore_ValueChanged);
            //
            // pickerAfter
            //
            this.pickerAfter.Enabled = false;
            this.pickerAfter.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.pickerAfter.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.pickerAfter.Location = new System.Drawing.Point(72, 60);
            this.pickerAfter.Name = "pickerAfter";
            this.pickerAfter.Size = new System.Drawing.Size(128, 21);
            this.pickerAfter.TabIndex = 8;
            this.pickerAfter.ValueChanged += new System.EventHandler(this.pickerAfter_ValueChanged);
            //
            // counterValues
            //
            this.counterValues.Enabled = false;
            this.counterValues.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.counterValues.Location = new System.Drawing.Point(72, 34);
            this.counterValues.Maximum = new System.Decimal(new int[] {
                                                                          300,
                                                                          0,
                                                                          0,
                                                                          0});
            this.counterValues.Minimum = new System.Decimal(new int[] {
                                                                          1,
                                                                          0,
                                                                          0,
                                                                          0});
            this.counterValues.Name = "counterValues";
            this.counterValues.ReadOnly = true;
            this.counterValues.Size = new System.Drawing.Size(60, 21);
            this.counterValues.TabIndex = 3;
            this.counterValues.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.counterValues.Value = new System.Decimal(new int[] {
                                                                        1,
                                                                        0,
                                                                        0,
                                                                        0});
            //
            // EditTimeSpanConditionForm
            //
            this.AcceptButton = this.buttonOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(206, 147);
            this.Controls.Add(this.counterValues);
            this.Controls.Add(this.pickerAfter);
            this.Controls.Add(this.pickerBefore);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.comboAnchors);
            this.Controls.Add(this.checkForAnchor);
            this.Controls.Add(this.checkForLast);
            this.Controls.Add(this.checkBefore);
            this.Controls.Add(this.checkAfter);
            this.Controls.Add(this.comboUnits);
            this.Controls.Add(this.buttonCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "EditTimeSpanConditionForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Edit Time Span";
            ((System.ComponentModel.ISupportInitialize)(this.counterValues)).EndInit();
            this.ResumeLayout(false);

        }
		#endregion

        private bool AnythingChecked()
        {
            return checkForAnchor.Checked || checkForLast.Checked || checkBefore.Checked || checkAfter.Checked;
        }

        #region ResultConstruction
        private void buttonOkClick(object sender, System.EventArgs e)
        {
            if( checkForAnchor.Checked )
                TimeSpanDescription = comboAnchors.SelectedItem.ToString();
            else
            if( checkForLast.Checked )
                TimeSpanDescription = "last " + counterValues.Text + " " + comboUnits.SelectedItem.ToString();
            else
            {
                if( checkBefore.Checked )
                {
                    TimeSpanDescription = "before " + pickerBefore.Value.ToShortDateString();
                }
                if( checkAfter.Checked )
                {
                    if( TimeSpanDescription.Length > 0 )
                        TimeSpanDescription += " and ";
                    TimeSpanDescription += "after " + pickerAfter.Value.ToShortDateString();
                }
            }

            DialogResult = DialogResult.OK;
        }
        #endregion ResultConstruction

        #region CheckboxSwitching
        private void checkForAnchor_CheckedChanged(object sender, System.EventArgs e)
        {
            if( checkForAnchor.Checked )
            {
                checkForLast.Checked = comboUnits.Enabled = counterValues.Enabled = false;
                checkBefore.Checked = pickerBefore.Enabled = false;
                checkAfter.Checked = pickerAfter.Enabled = false;
            }
            comboAnchors.Enabled = checkForAnchor.Checked;
            buttonOK.Enabled = AnythingChecked();
        }

        private void checkForLast_CheckedChanged(object sender, System.EventArgs e)
        {
            if( checkForLast.Checked )
            {
                checkForAnchor.Checked = comboAnchors.Enabled = false;
                checkBefore.Checked = pickerBefore.Enabled = false;
                checkAfter.Checked = pickerAfter.Enabled = false;
            }
            comboUnits.Enabled = counterValues.Enabled = checkForLast.Checked;
            buttonOK.Enabled = AnythingChecked();
        }

        private void checkBefore_CheckedChanged(object sender, System.EventArgs e)
        {
            if( checkBefore.Checked )
            {
                checkForAnchor.Checked = comboAnchors.Enabled = false;
                checkForLast.Checked = comboUnits.Enabled = counterValues.Enabled = false;
                if( !checkAfter.Checked )
                    pickerAfter.MaxDate = pickerBefore.Value;
            }
            else
            {
                pickerAfter.MaxDate = DateTimePicker.MaxDateTime;
                if( checkAfter.Checked )
                    pickerBefore.MinDate = pickerAfter.Value;
            }

            pickerBefore.Enabled = checkBefore.Checked;
            buttonOK.Enabled = AnythingChecked();
        }

        private void checkAfter_CheckedChanged(object sender, System.EventArgs e)
        {
            if( checkAfter.Checked )
            {
                checkForAnchor.Checked = comboAnchors.Enabled = false;
                checkForLast.Checked = comboUnits.Enabled = counterValues.Enabled = false;
                if( !checkBefore.Checked )
                    pickerBefore.MinDate = pickerAfter.Value;
            }
            else
            {
                pickerBefore.MinDate = DateTimePicker.MinDateTime;
                if( checkBefore.Checked )
                    pickerAfter.MaxDate = pickerBefore.Value;
            }

            pickerAfter.Enabled = checkAfter.Checked;
            buttonOK.Enabled = AnythingChecked();
        }
        #endregion CheckboxSwitching

        #region Convertions
        private void InitializeControls( string currentTimeSpan )
        {
            int anchorIndex = Array.IndexOf( FixedAnchors, currentTimeSpan );
            if( anchorIndex != -1 )
            {
                Trace.WriteLine( "EditTimeSpan -- It is first alternative with index " + anchorIndex );
                comboAnchors.SelectedIndex = anchorIndex;
                checkForAnchor.Enabled = comboAnchors.Enabled = true;
                checkForAnchor.Checked = true;
            }
            else
            if( currentTimeSpan.StartsWith( "last " ))
            {
                string[]    fields = currentTimeSpan.Split( ' ' );

                counterValues.Value = Int32.Parse( fields[ 1 ] );
                comboUnits.SelectedIndex = Array.IndexOf( SpanUnits, fields[ 2 ] );
                checkForLast.Enabled = comboUnits.Enabled = counterValues.Enabled = true;
                checkForLast.Checked = true;
            }
            else
            {
                Trace.WriteLine( "EditTimeSpan -- It is last alternative" );
                int      partsDelimiter = currentTimeSpan.IndexOf( " and " );
                if( currentTimeSpan.StartsWith( "before " ))
                {
                    Trace.WriteLine( "EditTimeSpan -- There is a [before] part" );
                    string beforePart = currentTimeSpan.Substring( 7 );
                    if( partsDelimiter != -1 )
                    {
                        Trace.WriteLine( "EditTimeSpan -- and we even managed to strip [and]" );
                        beforePart = beforePart.Substring( 0, partsDelimiter - 7 );
                        currentTimeSpan = currentTimeSpan.Substring( partsDelimiter + 5 );
                    }
                    Trace.WriteLine( "EditTimeSpan -- Finally we process [" + beforePart + "]" );
                    pickerBefore.Value = DateTime.Parse( beforePart );
                    pickerBefore.MinDate = DateTimePicker.MinDateTime;
                    pickerBefore.MaxDate = DateTimePicker.MaxDateTime;
                    pickerAfter.MaxDate = pickerBefore.Value;
                    checkBefore.Enabled = pickerBefore.Enabled = true;
                    checkBefore.Checked = true;
                }

                if( currentTimeSpan.StartsWith( "after " ))
                {
                    pickerAfter.Value = DateTime.Parse( currentTimeSpan.Substring( 6 ) );
                    pickerAfter.MinDate = DateTimePicker.MinDateTime;
                    pickerBefore.MinDate = pickerAfter.Value;
                    checkAfter.Enabled = pickerAfter.Enabled = true;
                    checkAfter.Checked = true;
                }
            }
        }

        public static string Condition2Text( IResource condition )
        {
            Debug.Assert( ResourceTypeHelper.IsDateProperty( condition.GetStringProp( "ApplicableToProp" )),
                          "Can not apply IResource->string transformation to inappropriate condition" );

            string      description;
            ConditionOp op = (ConditionOp)condition.GetIntProp( "ConditionOp" );
            if( op == ConditionOp.Lt )
                description = "before " + condition.GetStringProp( "ConditionVal" );
            else
            if( op == ConditionOp.Gt )
                description = "after " + condition.GetStringProp( "ConditionVal" );
            else
            if( op == ConditionOp.Eq )
                description = condition.GetStringProp( "ConditionVal" );
            else
            if( op == ConditionOp.InRange )
            {
                string srcLower = condition.GetStringProp( "ConditionValLower" ),
                       srcUpper = condition.GetStringProp( "ConditionValUpper" );
                string lower = srcLower.ToLower(), upper = srcUpper.ToLower();

                if(( lower == "tomorrow" ) && ( upper == "1" || upper == "+1" ))
                    description = "Tomorrow";
                else
                if(( lower == "yesterday" ) && ( upper == "today" || upper == "1" || upper == "+1" ))
                    description = "Yesterday";
                else
                if(( lower == "weekstart" ) && ( upper == "7" || upper == "+7" ))
                    description = "This week";
                else
                if(( lower == "weekstart" ) && ( upper == "-7" ))
                    description = "Last week";
                else
                if(( lower == "nextweekstart" ) && ( upper == "+7" ))
                    description = "Next week";
                else
                if(( lower == "monthstart" ) &&
                   ( upper == "30" || upper == "31" || upper == "+30" || upper == "+31" ))
                    description = "This month";
                else
                if(( lower == "monthstart" ) && ( upper == "-30" ))
                    description = "Last month";
                else
                if( lower == "today" && ( upper == "1" || upper == "+1" ))
                    description = "Today";
                else
                if( lower == "tomorrow" )
                {
                    if( upper == "1" || upper == "+1" )
                        description = "Today";
                    else
                    {
                        string  unit = "days";
                        char    charUnit = upper[ upper.Length - 1 ];
                        if( Char.IsLetter( charUnit ))
                        {
                            if( charUnit == 'h' )
                                unit = "hours";
                            else
                            if( charUnit == 'd' )
                                unit = "days";
                            else
                            if( charUnit == 'w' )
                                unit = "weeks";
                            else
                            if( charUnit == 'm' )
                                unit = "months";
                            else
                            if( charUnit == 'y' )
                                unit = "years";
                            upper = upper.Remove( upper.Length - 1, 1 );
                        }
                        if( upper[ 0 ] == '-' )
                            upper = upper.Remove( 0, 1 );
                        description = "last " + upper + " " + unit;
                    }
                }
                else
                    description = "before " + srcLower + " and after " + srcUpper;
            }
            else
                throw new Exception( "Unexpecter type of operation in back parsing of condition into string" );

            return description;
        }
        #endregion Convertions

        private void pickerBefore_ValueChanged(object sender, EventArgs e)
        {
            if( checkAfter.Checked == false )
            {
                pickerAfter.MaxDate = pickerBefore.Value;
                if( pickerAfter.Value > pickerBefore.Value )
                    pickerAfter.Value = pickerBefore.Value;
            }
        }

        private void pickerAfter_ValueChanged(object sender, EventArgs e)
        {
            if( checkBefore.Checked == false )
            {
                pickerBefore.MinDate = pickerAfter.Value;
                if( pickerBefore.Value < pickerAfter.Value )
                    pickerBefore.Value = pickerAfter.Value;
            }
        }
    }
}
