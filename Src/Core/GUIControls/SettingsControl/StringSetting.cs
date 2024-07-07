// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// Summary description for StringSetting.
    /// </summary>
    public class StringSettingEditor : TextBox, ISettingControl
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;
        private Setting _setting = null;
        private bool _changed = false;
        private object _oldValue = null;

        public StringSettingEditor()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();
            this.TextChanged+=new System.EventHandler(StringSettingEditor_TextChanged);
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
                Text = (string)_setting.Value;
                _oldValue = _setting.Value;
            }
            Changed = false;
        }
        public void SetValue( object value )
        {
            if ( ( Text == null && value != null ) ||  !Text.Equals( value ) )
            {
                bool oldChanged = _changed;
                Text = (string)value;
                _changed = oldChanged;
            }
        }

        public void SaveSetting()
        {
            Guard.NullMember( _setting, "_setting" );
            if ( _changed )
            {
                if ( Text != null )
                {
                    Text = Text.Trim();
                }
                if ( _oldValue == null || !_oldValue.Equals( Text ) )
                {
                    _setting.Save( Text );
                }
            }
            Reset();
        }
        public bool Determinated
        {
            get
            {
                if ( Changed )
                {
                    return true;
                }
                return !_setting.Different;
            }
        }

        public Setting Setting
        {
            get { return _setting; }
        }

        public bool Changed
        {
            get { return _changed; }
            set { _changed = value; }
        }

        private void StringSettingEditor_TextChanged(object sender, System.EventArgs e)
        {
            Changed = true;
        }
    }
}
