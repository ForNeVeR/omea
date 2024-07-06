// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// Summary description for RadioButtonSettingEditor.
    /// </summary>
    public class RadioButtonSettingEditor : GroupBox, ISettingControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        private Setting _setting = null;
        private ArrayList _items = new ArrayList();
        private bool _changed = false;
        private object _oldValue = null;

        public delegate void CheckedChangedHandler( object sender, EventArgs e );
        public event CheckedChangedHandler CheckedChanged;

        public RadioButtonSettingEditor()
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
        }
        #endregion

        public void SetSetting(Setting setting)
        {
            Guard.NullArgument( setting, "setting" );
            _setting = setting;
            Reset();
        }

        private void ClearItems()
        {
            for ( int i = Controls.Count - 1; i >= 0; --i )
            {
                if ( Controls[i] is RadioButton )
                {
                    Controls.RemoveAt( i );
                }
            }
            foreach ( RadioOrComboSettingItem item in _items )
            {
                ((RadioButton)item.Object).Dispose();
            }
            _items.Clear();
        }

        public void SetData( object[] values, object[] toStrings )
        {
            ClearItems();
            Guard.NullArgument( values, "values" );
            Guard.NullArgument( toStrings, "toStrings" );
            if ( values.Length == 0 || values.Length != toStrings.Length )
            {
                throw new ArgumentException( "Lenght for 'values' and 'toStrings' must be equal and more than zero." );
            }
            int width = Width - 16;
            for ( int i = 0; i < values.Length; ++i )
            {
                RadioButton radio = new RadioButton();
                radio.Text = toStrings[i].ToString();
                radio.FlatStyle = FlatStyle.System;
                int delta = ( Height - 18 - radio.Height * values.Length ) / ( values.Length + 1 );
                int posY = delta + i * ( delta + radio.Height );
                radio.Location = new Point( 8, posY + 14 );
                radio.Size = new Size( width, radio.Height );
				radio.TabIndex = i;
                _items.Add( new RadioOrComboSettingItem( values[i], radio ) );
                radio.CheckedChanged+=new EventHandler(radio_CheckedChanged);
                Controls.Add( radio );
            }
        }

        private void ClearChecked()
        {
            bool oldChanged = _changed;
            foreach ( RadioOrComboSettingItem item in _items )
            {
                ((RadioButton)item.Object).Checked = false;
            }
            _changed = oldChanged;
        }
        public void SetValue( object value )
        {
            SetValue( value, false );
        }
        private void SetValue( object value, bool defValue )
        {
            foreach ( RadioOrComboSettingItem item in _items )
            {
                if ( item.Value.Equals( value ) )
                {
                    bool oldChanged = _changed;
                    ((RadioButton)item.Object).Checked = true;
                    _changed = oldChanged;
                    return;
                }
            }
            if ( !defValue )
            {
                SetValue( _setting.Default, true );
            }
        }
        public object GetValue( )
        {
            foreach ( RadioOrComboSettingItem item in _items )
            {
                if ( ((RadioButton)item.Object).Checked )
                {
                    return item.Value;
                }
            }
            return null;
        }

        public void Reset()
        {
            Guard.NullMember( _setting, "_setting" );
            _setting.Load();
            ClearChecked();
            _oldValue = null;
            if ( !_setting.Different )
            {
                SetValue( _setting.Value );
                _oldValue = _setting.Value;
            }
            Changed = false;
            this.Update();
        }

        public void SaveSetting()
        {
            Guard.NullMember( _setting, "_setting" );
            if ( Changed )
            {
                object value = GetValue();
                if ( value == null || !value.Equals( _oldValue ) )
                {
                    foreach ( RadioOrComboSettingItem item in _items )
                    {
                        if ( ((RadioButton)item.Object).Checked == true )
                        {
                            _setting.Save( item.Value );
                            break;
                        }
                    }
                }
            }
            Reset();
        }

        public Setting Setting
        {
            get { return _setting; }
        }
        public bool Changed
        {
            get
            {
                return _changed;
            }
            set
            {
                _changed = value;
            }
        }

        private void radio_CheckedChanged(object sender, EventArgs e)
        {
            _changed = true;
            if ( CheckedChanged != null )
            {
                CheckedChanged( sender, e );
            }
        }
    }
}
