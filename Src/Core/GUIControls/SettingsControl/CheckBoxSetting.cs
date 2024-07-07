// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Summary description for CheckBoxSetting.
	/// </summary>
	public class CheckBoxSettingEditor : System.Windows.Forms.CheckBox, ISettingControl
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private Setting _setting = null;
        private bool _changed = false;
        private bool _inverted = false;
        private object _oldValue = null;

		public CheckBoxSettingEditor()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
            this.CheckedChanged+=new EventHandler(CheckBoxSettingEditor_CheckedChanged);
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

        public void SetSetting( Setting setting )
        {
            Guard.NullArgument( setting, "setting" );
            _setting = setting;
            Reset( );
        }
        public bool InvertValue
        {
            get { return _inverted; }
            set { _inverted = value; }
        }
        public void Reset( )
        {
            Guard.NullMember( _setting, "_setting" );
            _setting.Load();
            if ( _setting.Different )
            {
                _oldValue = null;
                CheckState = CheckState.Indeterminate;
            }
            else
            {
                _oldValue = _setting.Value;
                if ( !InvertValue )
                {
                    Checked = (bool)_setting.Value;
                }
                else
                {
                    Checked = !(bool)_setting.Value;
                }
            }
            Changed = false;
        }

        public void SaveSetting()
        {
            Guard.NullMember( _setting, "_setting" );
            bool newValue = Checked;
            if ( InvertValue )
            {
                newValue = !newValue;
            }
            if ( Changed && CheckState != CheckState.Indeterminate && !newValue.Equals(_oldValue) )
            {
                _setting.Save( newValue );
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

	    public void SetValue( object value )
        {
            bool oldChanged = _changed;
            Checked = (bool)value;
            _changed = oldChanged;
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

        private void CheckBoxSettingEditor_CheckedChanged(object sender, EventArgs e)
        {
            Changed = true;
        }
    }
}
