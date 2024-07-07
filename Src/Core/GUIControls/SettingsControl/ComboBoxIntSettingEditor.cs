// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Summary description for ComboBoxIntSettingEditor.
	/// </summary>
	public class ComboBoxSettingEditor : ComboBox, ISettingControl
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
        private Setting _setting = null;
        private bool _changed = false;
        private object _oldValue = null;

		public ComboBoxSettingEditor()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
            DropDownStyle = ComboBoxStyle.DropDownList;
            this.SelectedIndexChanged+=new EventHandler(ComboBoxSettingEditor_SelectedIndexChanged);
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

        public virtual void SetSetting(Setting setting)
        {
            Guard.NullArgument( setting, "setting" );
            _setting = setting;
            Reset();
        }

        public void SetData( object[] values, object[] toStrings )
        {
            this.Items.Clear();
            Guard.NullArgument( values, "values" );
            Guard.NullArgument( toStrings, "toStrings" );
            if ( values.Length != toStrings.Length )
            {
                throw new ArgumentException( "Lenght for 'values' and 'toStrings' must be equal" );
            }
            for ( int i = 0; i < values.Length; ++i )
            {
                this.Items.Add( new RadioOrComboSettingItem( values[i], toStrings[i] ) );
            }
        }

        public void Reset()
        {
            Guard.NullMember( _setting, "_setting" );
            _oldValue = null;
            _setting.Load();
            this.SelectedItem = -1;
            if ( !_setting.Different )
            {
                SetValue( _setting.Value );
                _oldValue = _setting.Value;
            }
            Changed = false;
            this.Update();
        }
        public void SetValue( object value )
        {
            foreach ( RadioOrComboSettingItem item in this.Items )
            {
                if ( item.Value.Equals( value ) )
                {
                    bool oldChanged = _changed;
                    this.SelectedItem = item;
                    _changed = oldChanged;
                    break;
                }
            }
        }

        public void SaveSetting()
        {
            Guard.NullMember( _setting, "_setting" );
            if ( Changed && SelectedIndex != -1 && !((RadioOrComboSettingItem)SelectedItem).Value.Equals( _oldValue ) )
            {
                _setting.Save( ((RadioOrComboSettingItem)SelectedItem).Value );
            }
            Reset();
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

        private void ComboBoxSettingEditor_SelectedIndexChanged(object sender, EventArgs e)
        {
            Changed = true;
        }
    }
    internal class RadioOrComboSettingItem
    {
        private object _object = null;
        private object _value = null;
        public RadioOrComboSettingItem( object value, object tag )
        {
            Guard.NullArgument( value, "value" );
            Guard.NullArgument( tag, "tag" );
            _value = value;
            _object = tag;
        }
        public object Object{ get { return _object; } }
        public object Value{ get { return _value; } }
        public override string ToString()
        {
            return _object.ToString();
        }

    }

}
