// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Interface for panes used in the resource selector dialog.
    /// </summary>
    public interface IResourceSelectPane
    {
        /// <summary>
        /// Prepares the pane for selecting a single resource from the specified list.
        /// </summary>
        /// <param name="resTypes">Obsolete; please don't use.</param>
        /// <param name="baseList">The list from which the resources are selected.</param>
        /// <param name="selection">The resource which is initially selected in the list.</param>
        void SelectResource( string[] resTypes, IResourceList baseList, IResource selection );

        /// <summary>
        /// Prepares the pane for selecting multiple resources from the specified list.
        /// </summary>
        /// <param name="resTypes">Obsolete; please don't use.</param>
        /// <param name="baseList">The list from which the resources are selected.</param>
        /// <param name="selection">The resources which are initially selected in the list.</param>
        void SelectResources( string[] resTypes, IResourceList baseList, IResourceList selection );

        /// <summary>
        /// Returns the list of resources currently selected by the user.
        /// </summary>
        /// <returns>The list of selected resources.</returns>
        IResourceList GetSelection();

        /// <summary>
        /// Occurs when the user accepts the selection, for example, by double-clicking the selection
        /// list.
        /// </summary>
        event EventHandler Accept;
    }

    /// <summary>
    /// Extended interface for IResourceSelectPane allowing to show the "New..." button
    /// in the selector dialog.
    /// </summary>
    /// <since>2.0</since>
    public interface IResourceSelectPane2: IResourceSelectPane
    {
        /// <summary>
        /// Returns true if the "New..." button should be shown or false otherwise.
        /// </summary>
        bool ShowNewButton { get; }

        /// <summary>
        /// Called when the "New..." button is clicked in the selector dialog.
        /// </summary>
        void HandleNewButtonClicked();
    }

    /// <summary>
    /// Base class for panes used in the resource selector dialog.
    /// </summary>
    public class AbstractResourceSelectPane: UserControl, IResourceSelectPane2
	{
        /// <summary>
        /// Prepares the pane for selecting a single resource from the specified list.
        /// </summary>
        /// <param name="resTypes">Obsolete; please don't use.</param>
        /// <param name="baseList">The list from which the resources are selected.</param>
        /// <param name="selection">The resource which is initially selected in the list.</param>
        public virtual void SelectResource( string[] resTypes, IResourceList baseList, IResource selection )
        {
        }

        /// <summary>
        /// Prepares the pane for selecting multiple resources from the specified list.
        /// </summary>
        /// <param name="resTypes">Obsolete; please don't use.</param>
        /// <param name="baseList">The list from which the resources are selected.</param>
        /// <param name="selection">The resources which are initially selected in the list.</param>
        public virtual void SelectResources( string[] resTypes, IResourceList baseList, IResourceList selection )
        {
        }

        /// <summary>
        /// Returns the list of resources currently selected by the user.
        /// </summary>
        /// <returns>The list of selected resources.</returns>
        public virtual IResourceList GetSelection()
        {
            return null;
        }

        /// <summary>
        /// Occurs when the user accepts the selection, for example, by double-clicking the selection
        /// list.
        /// </summary>
        public event EventHandler Accept;

        /// <summary>
        /// Fires the <see cref="Accept"/> event.
        /// </summary>
        protected void OnAccept()
        {
            if ( Accept != null )
            {
                Accept( this, EventArgs.Empty );
            }
        }

        /// <summary>
        /// Returns true if the "New..." button should be shown or false otherwise.
        /// </summary>
        /// <since>2.0</since>
        public virtual bool ShowNewButton
        {
            get { return false; }
        }

        /// <summary>
        /// Called when the "New..." button is clicked in the selector dialog.
        /// </summary>
        /// <since>2.0</since>
        public virtual void HandleNewButtonClicked()
        {
        }
	}
}
