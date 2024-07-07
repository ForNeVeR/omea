// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// The base class for the panes which support editing resources in separate
    /// windows.
    /// </summary>
    public class AbstractEditPane: UserControl
	{
        /// <summary>
        /// Called when the edit window for the specified resource is opened.
        /// </summary>
        /// <param name="res">The resource to edit in the pane.</param>
        public virtual void EditResource( IResource res ) { }

        /// <summary>
        /// Called when the Save button is pressed in the edit window.
        /// </summary>
        public virtual void Save() { }

        /// <summary>
        /// Called when the Cancel button is pressed in the edit window.
        /// </summary>
        public virtual void Cancel() { }

        /// <summary>
        /// Returns the minimum size to which the edit pane can be shrunk.
        /// </summary>
        /// <returns>The minimum size value.</returns>
        public virtual Size GetMinimumSize() { return new Size( 0, 0 ); }

        /// <summary>
        /// Can be fired by the pane when the valid state of the form controls changes.
        /// </summary>
        public event ValidStateEventHandler ValidStateChanged;

        /// <summary>
        /// Fires the <see cref="ValidStateChanged"/> event.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected void OnValidStateChanged( ValidStateEventArgs e )
        {
            if ( ValidStateChanged != null )
            {
                ValidStateChanged( this, e );
            }
        }
	}

    /// <summary>
    /// Provides data for the <see cref="AbstractEditPane.ValidStateChanged">ValidStateChanged</see>
    /// event.
    /// </summary>
    public class ValidStateEventArgs: EventArgs
    {
        private bool _isValid;
        private bool _isWarning;
        private string _message;

        /// <summary>
        /// Initializes the instance of the class with the specified validity flag and an empty message.
        /// </summary>
        /// <param name="isValid">The validity flag.</param>
        public ValidStateEventArgs( bool isValid ) : this( isValid, false, "" ) {}

        /// <summary>
        /// Initializes the instance of the class with the specified validity flag and message.
        /// </summary>
        /// <param name="isValid">The validity flag.</param>
        /// <param name="message">The message to be shown to the user if the form state is not valid.</param>
        public ValidStateEventArgs( bool isValid, string message ) : this( isValid, false, message ) {}

        public ValidStateEventArgs( bool isValid, bool isWarning, string message )
        {
            _isValid = isValid;
            _isWarning = isWarning;
            _message = message;
        }

        /// <summary>
        /// Gets the flag signifying whether the current form state is valid.
        /// </summary>
        public bool IsValid { get { return _isValid; } }

        /// <summary>
        /// Gets the flag signifying whether the message correspond to fatal case.
        /// </summary>
        public bool IsWarning { get { return _isWarning; } }

        /// <summary>
        /// Gets the message that is shown to the user to inform him that the form
        /// state is not valid.
        /// </summary>
        public string Message { get { return _message; } }
    }

    /// <summary>
    /// Represents the method that will handle the <see cref="AbstractEditPane.ValidStateChanged">
    /// ValidStateChanged</see> event of an <see cref="AbstractEditPane"/>.
    /// </summary>
    public delegate void ValidStateEventHandler( object sender, ValidStateEventArgs e );
}
