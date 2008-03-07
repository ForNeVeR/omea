/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
    /**
     * The base class for OmniaMea dialog windows. Handles some default form settings,
     * size/position persistence etc.
     */

    public class DialogBase : System.Windows.Forms.Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components = null;

        private ISettingStore _ini;
        private SizeF _scaleFactor = new SizeF( 1.0f, 1.0f );

        public DialogBase()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            SizeGripStyle = SizeGripStyle.Show;
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
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(DialogBase));
            // 
            // DialogBase
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.ClientSize = new System.Drawing.Size(292, 271);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.Icon = Core.UIManager.ApplicationIcon;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DialogBase";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "DialogBase";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.DialogBase_KeyDown);

        }
        #endregion

        protected override void OnClosing( CancelEventArgs e )
        {
            base.OnClosing( e );
            if ( _ini != null )
            {
                SaveSettings( _ini );
            }
        }

        protected override void ScaleCore( float x, float y )
        {
            base.ScaleCore( x, y );
            if( Environment.Version.Major < 2 )
            {
                _scaleFactor = new SizeF( x, y );
            }
        }

        /**
         * Saves the setting store used by the form and restores its settings 
         * from the INI file.
         */

        public void RestoreSettings()
        {
            AdjustContolProperties( Controls );
            KeyPreview = true;

            _ini = Core.SettingStore;
            string section = GetFormSettingsSection();
            bool maximized = _ini.ReadBool( section, "Maximized", false );
            if ( maximized )
                WindowState = FormWindowState.Maximized;
            else
            {
                int x = _ini.ReadInt( section, "X", -1 );
                int y = _ini.ReadInt( section, "Y", -1 );
                int width  = _ini.ReadInt( section, "Width", -1 );
                int height = _ini.ReadInt( section, "Height", -1 );

                if( x != -1 && y != -1 )
                {
                    Screen scr = Screen.FromPoint( new Point( x, y ));

                    //  First correct horizontal location (since it is that
                    //  what changes most of the time when screens configuration
                    //  is changed). If new point is suitable, do not change vertical
                    //  location.
                    //  NB: pay attention to cases when Screen.WorkingArea is (0, 0, 0, 0)!!!
                    if( !scr.Bounds.Contains( x, y ))
                    {
                        x = scr.WorkingArea.X;
                        if( scr.WorkingArea.Width != 0 )
                            x += Math.Abs( x ) % scr.WorkingArea.Width;
                    }

                    if( !scr.Bounds.Contains( x, y ))
                    {
                        y = scr.WorkingArea.Y;
                        if( scr.WorkingArea.Height != 0 )
                            y += Math.Abs( y ) % scr.WorkingArea.Height;
                    }
                    StartPosition = FormStartPosition.Manual;
                    Location = new Point( x, y );
                }
                if ( width > 0 && height > 0 )
                {
                    ClientSize = new Size( width, height );
                }
            }
        }

        /**
         * Saves the settings in the INI file.
         */

        private void SaveSettings( ISettingStore settingStore )
        {
            string section = GetFormSettingsSection();
            bool maximized = (WindowState == FormWindowState.Maximized);
            settingStore.WriteBool( section, "Maximized", maximized );
            if ( !maximized ) 
            {
                settingStore.WriteInt( section, "X", Location.X );
                settingStore.WriteInt( section, "Y", Location.Y );
                settingStore.WriteInt( section, "Width", (int) ((float) ClientSize.Width / _scaleFactor.Width ) );
                settingStore.WriteInt( section, "Height", (int) ((float) ClientSize.Height / _scaleFactor.Height ) );
            }
        }

        /**
         * Returns the default name of the section where the settings should be saved.
         */

        protected string GetFormSettingsSection()
        {
            return GetType().FullName;
        }

        public static void AdjustContolProperties( Control control )
        {
            AdjustContolProperties( control.Controls );
        }

        private static void AdjustContolProperties( Control.ControlCollection collection )
        {
            foreach( Control control in collection )
            {
                Button btn = control as Button;
                if( btn != null )
                {
                    btn.Height = 23;
                    btn.FlatStyle = FlatStyle.System;
                    continue;
                }
                Label label = control as Label;
                if( label != null )
                {
                    label.FlatStyle = FlatStyle.System;
                    continue;
                }

                TextBox textBox = control as TextBox;
                if( textBox != null )
                {
                    textBox.AcceptsReturn = textBox.Multiline;
                    continue;
                }
                CheckBox checkBox = control as CheckBox;
                if( checkBox != null )
                {
                    checkBox.FlatStyle = FlatStyle.System;
                    continue;
                }
                RadioButton radio = control as RadioButton;
                if( radio != null )
                {
                    radio.FlatStyle = FlatStyle.System;
                    continue;
                }
                GroupBox groupBox = control as GroupBox;
                if ( groupBox != null && groupBox.Height > 8 )
                {
                    groupBox.FlatStyle = FlatStyle.System;
                }
                AdjustContolProperties( control.Controls );
            }
        }

        private void DialogBase_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if( !e.Handled )
            {
                switch( e.KeyCode )
                {
                    case Keys.Enter:
                    {
                        if( e.Control && AcceptButton != null )
                        {
                            e.Handled = true;
                            Core.UIManager.QueueUIJob( new MethodInvoker( AcceptButton.PerformClick ) );
                        }
                        break;
                    }
                    case Keys.Escape:
                    {
                        if( !e.Control && !e.Shift && !e.Alt && CancelButton != null )
                        {
                            e.Handled = true;
                            Core.UIManager.QueueUIJob( new MethodInvoker( CancelButton.PerformClick ) );
                        }
                        break;
                    }
                }
            }
        }
    }
}
