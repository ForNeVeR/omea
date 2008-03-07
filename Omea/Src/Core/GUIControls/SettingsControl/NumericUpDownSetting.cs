/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// Summary description for NumericUpDownSetting.
    /// </summary>
    public class NumericUpDownSettingEditor : UpDownBase, ISettingControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        private Setting _setting = null;
        private bool _checkMinMax = false;
        private int _maximum = 100;
        private int _minimum = 0;
        private bool _changed = false;
        private object _oldValue = null;

        public NumericUpDownSettingEditor()
        {
            // This call is required by the Windows.Forms Form Designer.
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

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            this.TextChanged += new EventHandler(NumericUpDownSettingEditor_TextChanged);
        }
        #endregion

        public void SetSetting( Setting setting )
        {
            Guard.NullArgument( setting, "setting" );
            _setting = setting;
            Reset();
            Changed = false;
        }

        public void Reset()
        {
            Guard.NullMember( _setting, "_setting" );
            _setting.Load();
            _oldValue = null;
            if ( _setting.Different )
            {
                Text = "";
            }
            else
            {
                Value = (int)_setting.Value;
                _oldValue = _setting.Value;
            }
            Changed = false;
        }
        public void SetValue( object value )
        {
            Value = (int)value;
        }

        public void SaveSetting()
        {
            Guard.NullMember( _setting, "_setting" );
            if( !String.IsNullOrEmpty( Text ) )
            {
                int uncheckedValue = Value;
                CheckMinMax();
                if ( uncheckedValue != Value )
                {
                    Changed = true;
                }
                if ( Changed && !Value.Equals( _oldValue ) )
                {
                    _setting.Save( Value );
                }
                Reset();
            }
        }
        public int  Maximum         { get { return _maximum; } set { _checkMinMax = true; _maximum = value; } }
        public int  Minimum         { get { return _minimum; } set { _checkMinMax = true; _minimum = value; } }
        public bool Determinated    { get {  return ValidInt();  } }
        public Setting Setting      { get { return _setting; }   }

        public bool Changed
        {
            get { return _changed;  }
            set { _changed = value; }
        }

        private void CheckMinMax()
        {
            if ( !_checkMinMax ) return;

            if ( String.IsNullOrEmpty( Text ) ) return;

            if ( Value > Maximum )
            {
                Value = Maximum;
            }
            if ( Value < Minimum )
            {
                Value = Minimum;
            }
        }

        public override void UpButton()
        {
            if ( !Determinated )
            {
                if ( _setting != null )
                {
                    Value = (int)_setting.Default;
                }
                else
                {
                    Value = 1;
                }
            }
            else
            {
                Value++;
            }
            CheckMinMax();
            UpdateEditText();
            Changed = true;
        }

        public int Value
        {
            get
            {
                try
                {
                    return Int32.Parse( Text );
                }
                catch ( Exception ) //  both OverflowException and FormatException
                {
                    Changed = true;
                    Value = (int)_setting.Value;
                    return (int)_setting.Value;
                }
            }
            set
            {
                Text = value.ToString();
            }
        }
        
        public override void DownButton()
        {
            if ( !Determinated )
            {
                if ( _setting != null )
                {
                    Value = (int)_setting.Default;
                }
                else
                {
                    Value = -1;
                }
            }
            else
            {
                Value--;
            }
            CheckMinMax();
            UpdateEditText();
            Changed = true;
        }

        protected override void ValidateEditText()
        {
            CheckMinMax();
            this.ForeColor = ValidInt() ? Color.Black : Color.Red;
        }

        private bool ValidInt()
        {
            try
            {
                Int32.Parse( Text );
                return true;
            }
            catch( Exception ) //  Cover OverflowException and FormatException
            {
                return false;
            }
        }

        private void NumericUpDownSettingEditor_TextChanged(object sender, EventArgs e)
        {
            ValidateEditText();
        }

        protected override void UpdateEditText()
        {}

        protected override void OnTextBoxKeyPress( object source, KeyPressEventArgs e )
        {
            base.OnTextBoxKeyPress( source, e );
            if ( Char.IsDigit( e.KeyChar ) || e.KeyChar.ToString() == CultureInfo.CurrentCulture.NumberFormat.NegativeSign 
                || e.KeyChar == '\b' )
            {
                Changed = true;
                return;
            }

            e.Handled = true;
        }
    }
}
