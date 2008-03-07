/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Expands nodes based on the value of a resource property, and saves the expanded state
	/// of nodes in the resource property.
	/// </summary>
	public class ExpandedPropManager: IDisposable
	{
        private JetListView _jetListView;
        private int _propId;

		public ExpandedPropManager( JetListView listView, int propId )
		{
            _jetListView = listView;
            _propId = propId;

            _jetListView.NodeCollection.NodeAdded += new JetListViewNodeEventHandler( HandleNodeAdded );
            _jetListView.NodeCollection.NodeExpandChanged += new JetListViewNodeEventHandler( HandleNodeExpandChanged );
		}

	    public void Dispose()
	    {
            _jetListView.NodeCollection.NodeAdded -= new JetListViewNodeEventHandler( HandleNodeAdded );
            _jetListView.NodeCollection.NodeExpandChanged -= new JetListViewNodeEventHandler( HandleNodeExpandChanged );
        }

	    public int PropId
	    {
	        get { return _propId; }
	    }

	    private void HandleNodeAdded( object sender, JetListViewNodeEventArgs e )
	    {
            IResource res = (IResource) e.Node.Data;
            if ( res.GetIntProp( _propId ) == 1 )
            {
                e.Node.Expanded = true;
            }
	    }

        private void HandleNodeExpandChanged( object sender, JetListViewNodeEventArgs e )
        {
            IResource res = (IResource) e.Node.Data;
            new ResourceProxy( res ).SetPropAsync( _propId, e.Node.Expanded ? 1 : 0 );
        }
    }
}
