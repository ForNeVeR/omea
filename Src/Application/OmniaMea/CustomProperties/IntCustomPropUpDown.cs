﻿// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Globalization;
using System.Windows.Forms;

namespace JetBrains.Omea.CustomProperties
{
	/// <summary>
	/// A customized implementation of an up-down property for editing custom
	/// properties (which allows empty text to be set).
	/// </summary>
	public class IntCustomPropUpDown: UpDownBase
	{
        public override void UpButton()
        {
            if ( Text == "" )
            {
                Text = "1";
            }
            else
            {
                Text = (Int32.Parse( Text ) + 1).ToString();
            }
            UpdateEditText();
        }

        public override void DownButton()
	    {
            if ( Text == "" )
            {
                Text = "-1";
            }
            else
            {
                Text = (Int32.Parse( Text ) - 1).ToString();
            }
            UpdateEditText();
	    }

        protected override void UpdateEditText()
        {
        }

        protected override void OnTextBoxKeyPress( object source, KeyPressEventArgs e )
        {
            base.OnTextBoxKeyPress( source, e );
            if ( Char.IsDigit( e.KeyChar ) || e.KeyChar.ToString() == CultureInfo.CurrentCulture.NumberFormat.NegativeSign
                || e.KeyChar == '\b' )
            {
                return;
            }

            e.Handled = true;
        }
	}
}
