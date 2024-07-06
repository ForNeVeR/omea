// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.GUIControls
{
    public interface ISettingControl
    {
        void SetSetting( Setting setting );
        void Reset( );
        void SaveSetting();
        Setting Setting{ get; }
        bool Changed{ get; set; }
        void SetValue( object value );
    }

    public class SettingSaver
    {
        public static void Save( Control.ControlCollection controls )
        {

            Guard.NullArgument( controls, "controls" );
            foreach ( Control control in controls )
            {
                ISettingControl settingControl = control as ISettingControl;
                if ( settingControl != null )
                {
                    settingControl.SaveSetting();
                }
                Save( control.Controls );
            }
        }
    }
}
