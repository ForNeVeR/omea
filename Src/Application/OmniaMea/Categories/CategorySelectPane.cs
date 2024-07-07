// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.JetListViewLibrary;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Categories
{
	/// <summary>
	/// Pane for selecting categories, allowing to choose categories for any resource type.
	/// </summary>
	public class CategorySelectPane: ResourceTreeSelectPane
	{
		public CategorySelectPane() : base()
		{
            _resourceTree.AddNodeFilter( new CategoryNodeFilter() );
            _resourceTree.OpenProperty = (Core.CategoryManager as CategoryManager).PropCategoryExpandedInSelector;
		}

        public override IResource GetSelectorRoot( string resType )
        {
            return Core.ResourceTreeManager.ResourceTreeRoot;
        }

	    public override bool ShowNewButton
	    {
	        get { return true; }
	    }

	    /// <summary>
	    /// Called when the "New..." button is clicked in the selector dialog.
	    /// </summary>
	    public override void HandleNewButtonClicked()
	    {
	        IResource res = Core.UIManager.ShowNewCategoryDialog( FindForm(), null, null, null );
            if ( res != null )
            {
                if ( _resourceTree.SelectResourceNode( res ) )
                {
                    _resourceTree.SetNodeCheckState( res, CheckBoxState.Checked );
                }
            }
	    }
	}
}
