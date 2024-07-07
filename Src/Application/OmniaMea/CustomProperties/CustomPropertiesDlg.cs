// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.DataStructures;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.CustomProperties
{
	/**
     * Dialog for editing the custom properties of a resource.
     */

    public class CustomPropertiesDlg : DialogBase
	{
        private System.Windows.Forms.Button _btnOK;
        private System.Windows.Forms.Button _btnCancel;
        private System.Windows.Forms.Panel _contentPane;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        private IResourceList _resources;
        private IntHashTable _propControls = new IntHashTable();   // prop ID -> ICustomPropertyEditor

		public CustomPropertiesDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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
			this._btnOK = new System.Windows.Forms.Button();
			this._btnCancel = new System.Windows.Forms.Button();
			this._contentPane = new System.Windows.Forms.Panel();
			this.SuspendLayout();
			//
			// _btnOK
			//
			this._btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this._btnOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnOK.Location = new System.Drawing.Point(124, 240);
			this._btnOK.Name = "_btnOK";
			this._btnOK.TabIndex = 1;
			this._btnOK.Text = "OK";
			//
			// _btnCancel
			//
			this._btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this._btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this._btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this._btnCancel.Location = new System.Drawing.Point(208, 240);
			this._btnCancel.Name = "_btnCancel";
			this._btnCancel.TabIndex = 2;
			this._btnCancel.Text = "Cancel";
			//
			// _contentPane
			//
			this._contentPane.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
				| System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this._contentPane.AutoScroll = true;
			this._contentPane.Location = new System.Drawing.Point(0, 0);
			this._contentPane.Name = "_contentPane";
			this._contentPane.Size = new System.Drawing.Size(292, 232);
			this._contentPane.TabIndex = 0;
			//
			// CustomPropertiesDlg
			//
			this.AcceptButton = this._btnOK;
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
			this.CancelButton = this._btnCancel;
			this.ClientSize = new System.Drawing.Size(292, 271);
			this.Controls.Add(this._contentPane);
			this.Controls.Add(this._btnCancel);
			this.Controls.Add(this._btnOK);
			this.MinimumSize = new System.Drawing.Size(300, 305);
			this.Name = "CustomPropertiesDlg";
			this.Text = "CustomPropertiesDlg";
			this.ResumeLayout(false);

		}
		#endregion

        public void EditCustomProperties( IResourceList resList )
        {
            _resources = resList;

            if ( resList.Count == 1 )
            {
            	Text = "Custom Properties: " + resList [0].DisplayName;
            }
            else
            {
            	Text = "Custom Properties: " + resList.Count + " Resources";
            }

            int curY = 4;

            int maxLength = 0;
            string longestName = "";
            foreach( IPropType propType in ResourceTypeHelper.GetCustomPropTypes() )
            {
                string curName;
                if ( propType.Name.StartsWith( "Custom." ) )
                {
                    curName = propType.Name.Substring( 7 );
                }
                else
                {
                    curName = propType.Name;
                }
                if ( curName.Length > maxLength )
                {
                    longestName = curName;
                    maxLength = curName.Length;
                }
            }

            int valueX;
            using( Graphics g = CreateGraphics() )
            {
                valueX = (int) g.MeasureString( longestName, Font ).Width + 16;
            }
            if ( valueX < 80 )
                valueX = 80;

            Width = valueX + 136;

            foreach( IPropType propType in ResourceTypeHelper.GetCustomPropTypes() )
            {
                bool valuesDiffer;
                object propValue = GetCustomPropValue( resList, propType.Id,  out valuesDiffer );
                CreateCustomPropControl( propType, propValue, valuesDiffer, valueX, ref curY );
            }

            curY = Math.Min( curY, Screen.PrimaryScreen.Bounds.Height / 2 );
            Height = curY + (Height - _contentPane.Height);

        	if ( ShowDialog() == DialogResult.OK )
        	{
        	    Core.ResourceAP.RunJob( new MethodInvoker( SaveCustomProperties ) );
        	}
        }

        private object GetCustomPropValue( IResourceList resList, int propId, out bool valuesDiffer )
        {
            object propValue = resList [0].GetProp( propId );
            for( int i=1; i<resList.Count; i++ )
            {
                object resValue = resList [i].GetProp( propId );
                if ( !Object.Equals( propValue, resValue ) )
                {
                    valuesDiffer = true;
                    return null;
                }
            }
            valuesDiffer = false;
            return propValue;
        }

		private void CreateCustomPropControl( IPropType type, object propValue,
            bool valuesDiffer, int valueX, ref int curY )
		{
            ICustomPropertyEditor propEditor = null;
            switch( type.DataType )
            {
                case PropDataType.String:
                    propEditor = new StringPropertyEditor();
                    break;

                case PropDataType.Int:
                    propEditor = new IntPropertyEditor();
                    break;

                case PropDataType.Date:
                    propEditor = new DatePropertyEditor();
                    break;

                case PropDataType.Bool:
                    propEditor = new BoolPropertyEditor();
                    break;

                default:
                    throw new Exception( "Unsupported custom property type " + type.DataType );
            }

            int editorX = 4;

            if ( propEditor.NeedLabel() )
            {
                Label lbl = new Label();

                if ( type.Name.StartsWith( "Custom." ) )
                {
                    lbl.Text = type.Name.Substring( 7 );
                }
                else
                {
                    lbl.Text = type.Name;
                }

                lbl.Location = new Point( 4, curY );
                lbl.AutoSize = true;
                lbl.FlatStyle = FlatStyle.System;
                _contentPane.Controls.Add( lbl );

                editorX = valueX;
            }

            Rectangle ctlRect = new Rectangle( editorX, curY, 120, 24 );
            Control editCtl = propEditor.CreateControl( ctlRect, type, propValue, valuesDiffer );
            _contentPane.Controls.Add( editCtl );
            _propControls [type.Id] = propEditor;

            curY += 28;
		}

        private void SaveCustomProperties()
        {
            foreach( IResource res in _resources )
            {
            	res.BeginUpdate();
            }
            foreach( IntHashTable.Entry e in _propControls )
            {
                ICustomPropertyEditor propEditor = (ICustomPropertyEditor) e.Value;
                foreach( IResource res in _resources )
                {
                    propEditor.SaveValue( res, e.Key );
                }
            }
            foreach( IResource res in _resources )
            {
                res.EndUpdate();
            }
        }
    }

    internal interface ICustomPropertyEditor
    {
    	bool NeedLabel();
        Control CreateControl( Rectangle rect, IPropType type, object curValue, bool valuesDiffer );
        void SaveValue( IResource res, int propID );
    }

    /**
     * Property editor for string controls, based on a regular edit box.
     */

    internal class StringPropertyEditor: ICustomPropertyEditor
    {
        private TextBox _editBox;
        private bool _valueChanged = false;

        public Control CreateControl( Rectangle rect, IPropType type, object curValue, bool valuesDiffer )
        {
            _editBox = new TextBox();
            _editBox.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            _editBox.Bounds = rect;
            if ( curValue != null )
            {
                _editBox.Text = curValue.ToString();
            }
            if ( valuesDiffer )
            {
                _editBox.BackColor = SystemColors.ControlLight;
            }
            _editBox.TextChanged += new EventHandler( OnTextChanged );
            return _editBox;
        }

        private void OnTextChanged( object sender, EventArgs e )
        {
            _editBox.BackColor = SystemColors.Window;
            _valueChanged = true;
        }

        public void SaveValue( IResource res, int propID )
        {
            _editBox.TextChanged -= new EventHandler( OnTextChanged );
            if ( _valueChanged )
            {
                if ( _editBox.Text == "" )
                {
                    res.DeleteProp( propID );
                }
                else
                {
                    res.SetProp( propID, _editBox.Text );
                }
            }
        }

        public bool NeedLabel()
        {
            return true;
        }
    }

    /**
     * Property editor for Int properties, based on an up/down control.
     */

    internal class IntPropertyEditor: ICustomPropertyEditor
    {
        private NumericUpDownSettingEditor _upDown;
        private bool _valueChanged;
        private bool _valueParsed = false;
        private bool _valueError = false;
        private int _value;

        public Control CreateControl( Rectangle rect, IPropType type, object curValue, bool valuesDiffer )
        {
            _upDown = new NumericUpDownSettingEditor();
            _upDown.Bounds = rect;
            if ( curValue != null )
            {
                _upDown.Text = curValue.ToString();
            }
            else
            {
                _upDown.Text = "";
                if ( valuesDiffer )
                {
                    _upDown.BackColor = SystemColors.ControlLight;
                }
            }
            _upDown.TextChanged += new EventHandler( OnTextChanged );
            return _upDown;
        }

        private void OnTextChanged( object sender, EventArgs e )
        {
            _upDown.BackColor = SystemColors.Window;
            _valueChanged = true;
        }

        public void SaveValue( IResource res, int propID )
        {
            _upDown.TextChanged -= new EventHandler( OnTextChanged );

            if ( _valueChanged )
            {
                if ( _upDown.Text == "" )
                {
                    res.DeleteProp( propID );
                }
                else
                {
                    if ( !_valueParsed )
                    {
                        _valueParsed = true;
                        try
                        {
                            _value = Int32.Parse( _upDown.Text );
                        }
                        catch( Exception )
                        {
                            MessageBox.Show( Core.MainWindow,
                                "The value '" + _upDown.Text + "' is not a valid number value",
                                "Edit Custom Property" );
                            _valueError = true;
                        }
                    }
                    if ( !_valueError )
                    {
                        res.SetProp( propID, _value );
                    }
                }
            }
        }

        public bool NeedLabel()
        {
            return true;
        }
    }

    /**
     * Property editor for Date properties, based on a DatePickerCtrl.
     */

    internal class DatePropertyEditor: ICustomPropertyEditor
    {
        private DatePickerCtrl _datePicker;
        private bool _valueChanged;

        public Control CreateControl( Rectangle rect, IPropType type, object curValue, bool valuesDiffer )
        {
            _datePicker = new DatePickerCtrl();
            _datePicker.Bounds = rect;
            _datePicker.ShowClearButton = true;
            if ( curValue != null )
            {
                _datePicker.CurrentDate = (DateTime) curValue;
            }
            _datePicker.ValueChanged += new EventHandler( OnValueChanged );
            return _datePicker;
        }

        private void OnValueChanged( object sender, EventArgs e )
        {
            _valueChanged = true;
        }

        public void SaveValue( IResource res, int propID )
        {
            _datePicker.ValueChanged -= new EventHandler( OnValueChanged );
            if ( _valueChanged )
            {
                if ( _datePicker.CurrentDate != DateTime.MinValue )
                {
                    res.SetProp( propID, _datePicker.CurrentDate );
                }
                else
                {
                    res.DeleteProp( propID );
                }
            }
        }

        public bool NeedLabel()
        {
            return true;
        }
    }

    /**
     * Property editor for Bool properties, based on a checkbox.
     */

    internal class BoolPropertyEditor: ICustomPropertyEditor
    {
        private CheckBox _checkBox;
        private bool _valueChanged;

        public Control CreateControl( Rectangle rect, IPropType type, object curValue, bool valuesDiffer )
        {
            _checkBox = new CheckBox();
            _checkBox.Bounds = rect;
            _checkBox.Text = type.DisplayName;
            _checkBox.FlatStyle = FlatStyle.System;
            if ( valuesDiffer )
            {
                _checkBox.CheckState = CheckState.Indeterminate;
            }
            else if ( curValue != null )
            {
                bool isChecked = (bool) curValue;
                _checkBox.Checked = isChecked;
            }
            _checkBox.CheckedChanged += new EventHandler( _checkBox_OnCheckedChanged );
            return _checkBox;
        }

        private void _checkBox_OnCheckedChanged( object sender, EventArgs e )
        {
            _valueChanged = true;
        }

        public void SaveValue( IResource res, int propID )
        {
            _checkBox.CheckedChanged -= new EventHandler( _checkBox_OnCheckedChanged );
            if ( _valueChanged )
            {
                res.SetProp( propID, _checkBox.Checked );
            }
        }

        public bool NeedLabel()
        {
            return false;
        }
    }
}
