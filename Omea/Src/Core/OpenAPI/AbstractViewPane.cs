/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Windows.Forms;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Base class for view panes displayed in the sidebar.
    /// </summary>
    /// <remarks>This can't be abstract, otherwise it won't be possible to edit derived classes 
    /// in the forms designer.</remarks>
    public class AbstractViewPane: UserControl
    {
        /// <summary>
        /// Fills the pane with the data which needs to be displayed. Called when the pane is
        /// displayed for the first time.
        /// </summary>
        public virtual void Populate()
        {
        }

        /// <summary>
        /// The resource which is currently selected in the pane.
        /// </summary>
        public virtual IResource SelectedResource
        {
            get { return null; }
        }

        /// <summary>
        /// Whether the pane needs to show the current selection if it is not focused.
        /// </summary>
        public virtual bool ShowSelection
        {
            get { return true; }
            set { }
        }

        /// <summary>
        /// Adjusts the size of the pane so that it is as large as it's needed to
        /// fit the content, but no larger than the specified size.
        /// </summary>
        /// <param name="maxHeight">Maximum height that the pane can take.</param>
        public virtual void AutoSize( int maxHeight )
        {
        }

        /// <summary>
        /// Selects the specified resource in the pane.
        /// </summary>
        /// <param name="resource">The resource to select.</param>
        /// <param name="highlightOnly">If true, the selection should be set to the node,
        /// but the resource list should not be updated.</param>
        /// <returns>true if the resource was selected successfully or false if it was not 
        /// found in the pane</returns>
        public virtual bool SelectResource( IResource resource, bool highlightOnly )
        {
            return false;
        }

        /// <summary>
        /// Forces the pane to repeat the action which is executed when a node is selected
        /// in the pane.
        /// </summary>
        public virtual void UpdateSelection()
        {
        }

        /// <summary>
        /// Forces the pane to repeat asynchronously the action which is executed when a node is selected
        /// in the pane.
        /// </summary>
        public virtual void AsyncUpdateSelection()
        {
        }

        /// <summary>
        /// Sets the currently active workspace. The pane can choose to filter its
        /// contents depending on the workspace.
        /// </summary>
        /// <param name="workspace">Active workspace, or null if the main workspace is active.</param>
        public virtual void SetActiveWorkspace( IResource workspace )
        {
        }

        /// <summary>
        /// Selects the previous view containing the specified resource, preceeding the specified view.
        /// </summary>
        /// <param name="view">The base view for searching the previous view.</param>
        /// <returns>true if the previous view was selected successfully, false otherwise.</returns>
        public virtual bool GotoPrevView( IResource view )
        {
            return false;
        }

        /// <summary>
        /// Selects the previous view containing the unread resource, preceeding the specified view.
        /// </summary>
        /// <param name="view">The base view for searching the previous view with unread item(s).</param>
        /// <returns>true if the previous view was selected successfully, false otherwise.</returns>
        public virtual bool GotoPrevUnreadView( IResource view )
        {
            return false;
        }

        /// <summary>
        /// Selects the next view containing the specified resource, following the specified view.
        /// </summary>
        /// <param name="view">The base view for searching the next view.</param>
        /// <returns>true if the next view was selected successfully, false otherwise.</returns>
        public virtual bool GotoNextView( IResource view )
        {
            return false;
        }

        /// <summary>
        /// Selects the next view containing the unread resource, following the specified view.
        /// </summary>
        /// <param name="view">The base view for searching the next view with unread item(s).</param>
        /// <returns>true if the next view was selected successfully, false otherwise.</returns>
        public virtual bool GotoNextUnreadView( IResource view )
        {
            return false;
        }
    }
}
