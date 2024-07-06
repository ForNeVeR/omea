// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Summary description for IntIntervalForm.
	/// </summary>
	public class IntIntervalForm : System.Windows.Forms.Form
	{
        public  string              IntervalDescription = "";
        private int                 MaxValue, MinValue;

        private System.Windows.Forms.CheckBox checkLarger;
        private System.Windows.Forms.TextBox textMinSize;
        private System.Windows.Forms.TextBox textMaxSize;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.CheckBox checkSmaller;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public IntIntervalForm( string interval )
               : this( interval, Int32.MinValue, Int32.MaxValue )
		{
        }
		public IntIntervalForm( string interval, int minValue, int maxValue )
		{
			InitializeComponent();
            if( interval != "" )
                InitializeControls( interval );
            MaxValue = maxValue;
            MinValue = minValue;
            buttonOK.Enabled = CanComplete();
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
            this.checkLarger = new System.Windows.Forms.CheckBox();
            this.checkSmaller = new System.Windows.Forms.CheckBox();
            this.textMinSize = new System.Windows.Forms.TextBox();
            this.textMaxSize = new System.Windows.Forms.TextBox();
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // checkLarger
            //
            this.checkLarger.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkLarger.Location = new System.Drawing.Point(4, 8);
            this.checkLarger.Name = "checkLarger";
            this.checkLarger.Size = new System.Drawing.Size(92, 16);
            this.checkLarger.TabIndex = 0;
            this.checkLarger.Text = "Larger than:";
            this.checkLarger.CheckedChanged += new System.EventHandler(this.checkLarger_CheckedChanged);
            //
            // checkSmaller
            //
            this.checkSmaller.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.checkSmaller.Location = new System.Drawing.Point(4, 32);
            this.checkSmaller.Name = "checkSmaller";
            this.checkSmaller.Size = new System.Drawing.Size(92, 16);
            this.checkSmaller.TabIndex = 0;
            this.checkSmaller.Text = "Smaller than:";
            this.checkSmaller.CheckedChanged += new System.EventHandler(this.checkSmaller_CheckedChanged);
            //
            // textMinSize
            //
            this.textMinSize.Enabled = false;
            this.textMinSize.Location = new System.Drawing.Point(96, 4);
            this.textMinSize.MaxLength = 11;
            this.textMinSize.Name = "textMinSize";
            this.textMinSize.TabIndex = 1;
            this.textMinSize.Text = "";
            this.textMinSize.TextChanged += new System.EventHandler(this.textMinSize_TextChanged);
            //
            // textMaxSize
            //
            this.textMaxSize.Enabled = false;
            this.textMaxSize.Location = new System.Drawing.Point(96, 28);
            this.textMaxSize.MaxLength = 11;
            this.textMaxSize.Name = "textMaxSize";
            this.textMaxSize.TabIndex = 1;
            this.textMaxSize.Text = "";
            this.textMaxSize.TextChanged += new System.EventHandler(this.textMaxSize_TextChanged);
            //
            // buttonOK
            //
            this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonOK.Location = new System.Drawing.Point(38, 60);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.TabIndex = 2;
            this.buttonOK.Text = "OK";
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            //
            // buttonCancel
            //
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonCancel.Location = new System.Drawing.Point(120, 60);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.TabIndex = 2;
            this.buttonCancel.Text = "Cancel";
            //
            // IntIntervalForm
            //
            this.AcceptButton = this.buttonOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(202, 91);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.textMinSize);
            this.Controls.Add(this.textMaxSize);
            this.Controls.Add(this.checkLarger);
            this.Controls.Add(this.checkSmaller);
            this.Controls.Add(this.buttonCancel);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "IntIntervalForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Select size range";
            this.ResumeLayout(false);

        }
		#endregion

        private void    InitializeControls( string interval )
        {
            int  partsDelimiter = interval.IndexOf( " and " );

            if( interval.StartsWith( "larger than " ))
            {
                string largerPart = interval.Substring( 12 );
                if( partsDelimiter != -1 )
                {
                    largerPart = largerPart.Substring( 0, partsDelimiter - 12 );
                    interval = interval.Substring( partsDelimiter + 5 );
                }
                textMinSize.Enabled = checkLarger.Checked = true;
                textMinSize.Text = largerPart;
            }

            if( interval.StartsWith( "smaller than " ))
            {
                string smallerPart = interval.Substring( 13 );
                textMaxSize.Enabled = checkSmaller.Checked = true;
                textMaxSize.Text = smallerPart;
            }
        }

        public static string Condition2Text( IResource condition )
        {
            string      result;
            ConditionOp op = (ConditionOp)condition.GetIntProp( "ConditionOp" );
            if( op == ConditionOp.InRange )
                result = "larger than " + condition.GetStringProp( "ConditionValLower" ) +
                         " and smaller than " + condition.GetStringProp( "ConditionValUpper" );
            else
            if( op == ConditionOp.Gt )
                result = "larger than " + condition.GetStringProp( "ConditionVal" );
            else
            if( op == ConditionOp.Lt )
                result = "smaller than " + condition.GetStringProp( "ConditionVal" );
            else
                throw new InvalidOperationException( "Can not parse operation for an interval condition" );
            return result;
        }

        private bool    CanComplete()
        {
            bool    minOK = checkLarger.Checked && IsNumberParseable( textMinSize.Text ),
                    maxOK = checkSmaller.Checked && IsNumberParseable( textMaxSize.Text );
            if( minOK && maxOK )
                return( Int32.Parse( textMinSize.Text ) < Int32.Parse( textMaxSize.Text ));
            else
                return( checkLarger.Checked && !checkSmaller.Checked && minOK ||
                        !checkLarger.Checked && checkSmaller.Checked && maxOK );
        }
        private bool    IsNumberParseable( string str )
        {
            try
            {
                int x = Int32.Parse( str );
                return( x >= MinValue && x <= MaxValue );
            }
            catch( Exception )
            {
                return false;
            }
        }

        //---------------------------------------------------------------------
        private void checkLarger_CheckedChanged(object sender, System.EventArgs e)
        {
            textMinSize.Enabled = checkLarger.Checked;
            buttonOK.Enabled = CanComplete();
        }

        private void checkSmaller_CheckedChanged(object sender, System.EventArgs e)
        {
            textMaxSize.Enabled = checkSmaller.Checked;
            buttonOK.Enabled = CanComplete();
        }

        private void textMinSize_TextChanged(object sender, System.EventArgs e)
        {
            buttonOK.Enabled = CanComplete();
        }

        private void textMaxSize_TextChanged(object sender, System.EventArgs e)
        {
            buttonOK.Enabled = CanComplete();
        }

        private void buttonOK_Click(object sender, System.EventArgs e)
        {
            IntervalDescription = "";
            if( checkLarger.Checked )
                IntervalDescription = "larger than " + textMinSize.Text;
            if( checkLarger.Checked && checkSmaller.Checked )
                IntervalDescription += " and ";
            if( checkSmaller.Checked )
                IntervalDescription += "smaller than " + textMaxSize.Text;
            DialogResult = DialogResult.OK;
        }
	}
}
