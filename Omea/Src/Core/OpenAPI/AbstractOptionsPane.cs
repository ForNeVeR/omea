// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Windows.Forms;

using JetBrains.Annotations;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary><seealso cref="IUIManager.RegisterOptionsPane"/><seealso cref="IUIManager.AddOptionsChangesListener"/>
    /// Base class for options panes displayed in the options dialog.
    /// </summary>
    /// <remarks>
    /// <para>Inherit a user control from this abstract base class if you wish to add a page to Omea options dialog.</para>
    /// <para>Settings should be loaded in <see cref="AbstractOptionsPane.ShowPane"/> and saved in <see cref="AbstractOptionsPane.OK"/> via the <see cref="ISettingStore">Setting Store</see>.</para>
    /// <para>To register a pane for the Options dialog, use <see cref="IUIManager.RegisterOptionsPane"/>. The panes are created through calling the <see cref="OptionsPaneCreator"/> delegate you supply rather than directly instantiating the class object.</para>
    /// </remarks>
    public class AbstractOptionsPane : UserControl
    {
        /// <summary><seealso cref="ISettingStore"/>
        /// Called when the pane is initially shown in the dialog.
        /// </summary>
        /// <remarks>
        /// <para>Typically, this method would fill the form with settings data.</para>
        /// </remarks>
        public virtual void ShowPane()
        {
        }

        /// <summary>
        /// Called always when the pane is entered in the dialog.
        /// </summary>
        public virtual void EnterPane()
        {
        }

        /// <summary>
        /// Called always when the pane is left in the dialog.
        /// </summary>
        public virtual void LeavePane()
        {
        }

        /// <summary><seealso cref="ISettingStore"/>
        /// Called when the Options dialog or the Startup Wizard is closed with the OK button.
        /// </summary>
        /// <remarks>Typically, this method would save the settings data.</remarks>
        public virtual void OK()
        {
        }

        /// <summary>
        /// Called when the dialog is closed by pressing the "Cancel" button.
        /// </summary>
        /// <remarks>Settings should not be saved in this case.</remarks>
        public virtual void Cancel()
        {
        }

        /// <summary>
        /// Called before calling OK() when the dialog is being closed by pressing either "OK" or button,
        /// or the "Apply" button is pressed to check if the control values are valid.
        /// </summary>
        /// <param name="errorMessage">Set error string if validation failed.</param>
        /// <param name="controlToSelect">Set focus to specified control.</param>
        /// <returns>Returns true if options pane is valid.</returns>
        /// <remarks>If returns <c>False</c>, error message should be set. Control to select can be unspecified.</remarks>
        /// <since>1.0.3</since>
        public virtual bool IsValid( ref string errorMessage, [CanBeNull] ref Control controlToSelect )
        {
            return true;
        }

    	/// <summary>
    	/// Gets or sets the flag signifying that the pane is currently being shown
    	/// in the Startup Wizard and not in the Options dialog.
    	/// </summary>
    	public bool IsStartupPane { get; set; }

    	/// <summary>
    	/// Gets or sets the flag whether restart of the application is needed. Should be used if an
    	/// AbstractOptionsPane implementor wishes to restart the application after settings are submitted.
    	/// </summary>
    	/// <since>2.2</since>
    	public bool NeedRestart { get; set; }

    	/// <summary>
        /// Retrieves the keyword in the help file that should be activated when help is
        /// requested for this options pane.
        /// </summary>
        /// <returns>Help index keyword.</returns>
    	[CanBeNull]
    	public virtual string GetHelpKeyword()
        {
            return null;
        }
    }

    /// <summary>
    /// Represents the method which creates an instance of the options pane.
    /// </summary>
    public delegate AbstractOptionsPane OptionsPaneCreator();
}
