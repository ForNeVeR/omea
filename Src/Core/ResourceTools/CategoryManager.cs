// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Windows.Forms;

using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.Categories
{
    /**
     * A set of static methods for working with resource categories.
     */

	public class CategoryManager: ICategoryManager
	{
        private static int _propCategory;
        private static int _propCategoryExpanded;
        private static int _propCategoryExpandedInSelector;
        private static int _propShowContentsRecursively;
        private IResource _rootCategory;
        private IResourceTreeManager _resourceTreeManager;
        private IResourceStore _store;

        /**
         * Registers the resource and property types related to categories.
         */

        public CategoryManager( IResourceStore store, IResourceTreeManager resourceTreeManager )
        {
            _store = store;
            _resourceTreeManager = resourceTreeManager;
            _store.ResourceTypes.Register( "Category", "Category", "Name",
                ResourceTypeFlags.ResourceContainer | ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            _store.ResourceTypes.Register( "ResourceTreeRoot", "ResourceTreeRoot", "", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );

            _propCategory = ResourceTypeHelper.UpdatePropTypeRegistration( "Category", PropDataType.Link, PropTypeFlags.CountUnread );

            _propCategoryExpanded = _store.PropTypes.Register( "CategoryExpanded", PropDataType.Int, PropTypeFlags.Internal );
            _propCategoryExpandedInSelector = _store.PropTypes.Register( "CategoryExpandedInSelector", PropDataType.Int, PropTypeFlags.Internal );
            _propShowContentsRecursively = _store.PropTypes.Register( "ShowContentsRecursively", PropDataType.Bool, PropTypeFlags.Internal );

            UpdateCategoryRoot();

            if ( _store.ResourceTypes.Exist( "CategoryIntersection" ) )
            {
                _store.GetAllResources( "CategoryIntersection" ).DeleteAll();
                _store.ResourceTypes.Delete( "CategoryIntersection" );
            }
        }

        public int PropCategory         { get { return _propCategory; } }
        public int PropCategoryExpanded { get { return _propCategoryExpanded; } }
        public int PropCategoryExpandedInSelector { get { return _propCategoryExpandedInSelector; } }
        public int PropShowContentsRecursively { get { return _propShowContentsRecursively; } }

        public IResource RootCategory
        {
            get { return _rootCategory; }
        }

        /**
         * Creates the new category root resource (which is the standard root of
         * resource type Category) and, if necessary, deletes the old root (which
         * was a category marked with the IsRoot property).
         */

        private void UpdateCategoryRoot()
        {
            _rootCategory = _resourceTreeManager.GetRootForType( "Category" );
            _rootCategory.DisplayName = "Categories";
            _rootCategory.SetProp( Core.Props.Open, 1 );
            _resourceTreeManager.SetResourceNodeSort( _rootCategory, "Name" );
            _resourceTreeManager.LinkToResourceRoot( _rootCategory, 20 );

            IResourceList typedCategories = _store.FindResourcesWithProp("Category", Core.Props.ContentType );
            foreach( IResource res in typedCategories )
            {
                if ( res.GetLinkProp( "Parent" ) == _rootCategory )
                {
                    IResource rootForType = GetRootForTypedCategory(res.GetStringProp( Core.Props.ContentType ));
                    res.SetProp( "Parent", rootForType );
                }
            }
        }

        /**
         * Returns the root of categories with the specified content type.
         */

        public IResource GetRootForTypedCategory( string resType )
        {
            IResource root = _resourceTreeManager.GetRootForType( "Category:" + resType );
            string displayName = _store.ResourceTypes [resType].DisplayName + " Categories";
            if ( root.DisplayName != displayName )
            {
                ResourceProxy proxy = new ResourceProxy( root );
                proxy.BeginUpdate();
                proxy.SetDisplayName( displayName );
                proxy.SetProp( Core.Props.ContentType, resType );
                proxy.SetProp( Core.Props.Open, 1 );
                proxy.EndUpdate();
            }
            _resourceTreeManager.SetResourceNodeSort( root, "Name" );
            _resourceTreeManager.LinkToResourceRoot( root, 21 );
            return root;
        }

	    public IResource FindRootForTypedCategory( string resType )
	    {
	        return Core.ResourceStore.FindUniqueResource( "ResourceTreeRoot", "RootResourceType",
                "Category:" + resType );
	    }

	    /**
         * Returns a categories list to which the
         * resource belongs.
         */

        public IResourceList GetResourceCategories( IResource resource )
        {
            return resource.GetLinksOfType( "Category", _propCategory );
        }

        /**
         * Adds a category link between a resource and a category and updates
         * the category intersections with all other categories to which the
         * resource belongs.
         */

        public void AddResourceCategory( IResource res, IResource category )
        {
            if ( res.HasLink( _propCategory, category ) )
                return;

            new ResourceProxy( res ).AddLink( _propCategory, category );
        }

        /**
         * Sets a category link between a resource and a category. The category
         * becomes the only one for the specified resource.
         */

        public void SetResourceCategory( IResource res, IResource category )
        {
            new ResourceProxy( res ).SetProp( _propCategory, category );
        }

        /**
         * Removes a category link between a resource and a category and updates the category
         * intersections.
         */

        public void RemoveResourceCategory( IResource res, IResource category )
        {
            if ( !res.HasLink( _propCategory, category ) )
                return;

            new ResourceProxy( res ).DeleteLink( _propCategory, category );
        }

        /**
         * Checks if a category with the specified name already exists.
         */

        public bool CategoryExists( IResource parent, string name )
        {
            #region Preconditions
            if ( name == null )
                throw new ArgumentNullException( "name" );
            #endregion Preconditions

            IResourceList nameList = Core.ResourceStore.FindResources( "Category", Core.Props.Name, name );
            if ( parent == null )
            {
                parent = RootCategory;
            }
            IResourceList parentList = parent.GetLinksTo( null, Core.Props.Parent );
            return nameList.Intersect( parentList, true ).Count > 0;
        }

        /**
         * Returns the list of resources that belong to all of the specified categories.
         */

        public static IResourceList GetResourcesInCategories( ArrayList categories )
        {
            IResourceList result = null;
            foreach( IResource category in categories )
            {
                result = category.GetLinksOfTypeLive( null, _propCategory ).Intersect( result, true );
            }
            return result;
        }

        /**
         * If there are any resources or subcategories connected to the category,
         * shows a confirmation dialog.
         */

        public static bool ConfirmDeleteCategories( IWin32Window ownerWindow, IResourceList categoryList )
        {
            int linkCount = 0, subCategoryCount = 0;
            foreach( IResource category in categoryList )
            {
                IResourceList linked = category.GetLinksOfType( null, _propCategory );
                linked = linked.Minus( Core.ResourceStore.FindResourcesWithProp( null, Core.Props.IsDeleted ));
                linkCount += linked.Count;

                subCategoryCount += category.GetLinksTo( null, Core.Props.Parent ).Count;
            }

            if ( linkCount > 0 || subCategoryCount > 0 )
            {
                string msg = "There are ";
                if ( linkCount > 0 )
                {
                    msg += linkCount + " resources ";
                    if ( subCategoryCount > 0 )
                    {
                        msg += "and ";
                    }
                }
                if ( subCategoryCount > 0 )
                {
                    msg += subCategoryCount + " subcategories ";
                }

                if ( categoryList.Count == 1 )
                {
                    msg += "in the category " + categoryList [0].DisplayName +
                        ". Are you sure you wish to delete the category?";
                }
                else
                {
                    msg += "in the " + categoryList.Count + " selected categories" +
                        ". Are you sure you wish to delete the categories?";
                }

                DialogResult dr = MessageBox.Show( ownerWindow,
                     msg, "Delete Category", MessageBoxButtons.YesNo );
                if ( dr == DialogResult.No )
                    return false;
            }
            return true;
        }

        /**
         * Deletes a category and its subcategories.
         */

        public static void DeleteCategory( IResource category )
        {
            IResourceList subcategories = category.GetLinksTo( "Category", Core.Props.Parent );
			category.Delete();
			foreach( IResource subcategory in subcategories )
            {
                DeleteCategory( subcategory );
            }
		}

        public static void DeleteCategories( IResourceList categoryList )
        {
            foreach( IResource category in categoryList )
            {
                if ( category.Type == "Category" )
                {
                    DeleteCategory( category );
                }
            }
            DeleteUnusedCategoryRoots();
        }

        public IResource FindOrCreateCategory( IResource parentCategory, string name )
        {
            if ( parentCategory == null )
                parentCategory = RootCategory;

            IResource category = FindCategory( parentCategory, name );
            if ( category != null )
            {
                return category;
            }
            return CreateCategory(name, parentCategory, parentCategory.GetStringProp( Core.Props.ContentType ));
        }

        public IResource FindCategory( IResource parentCategory, string name )
        {
            if ( parentCategory == null )
                parentCategory = RootCategory;

            IResourceList categories = parentCategory.GetLinksTo( "Category", Core.Props.Parent );
            foreach ( IResource category in categories )
            {
                if ( String.Compare( category.GetStringProp( Core.Props.Name ), name, true ) == 0 )
                {
                    return category;
                }
            }
            return null;
        }

        /**
         * Creates a category with the specified name and parent.
         */

        public static IResource CreateCategory( string name, IResource parent )
        {
            return CreateCategory( name, parent, null );
        }

        /**
         * Creates a category with the specified name, parent and content type.
         */

        public static IResource CreateCategory( string name, IResource parent, string contentType )
        {
            if ( parent == null )
                throw new ArgumentNullException( "parent" );

            if( parent.GetStringProp( Core.Props.ContentType ) != contentType )
            {
                throw new ArgumentException( "Content type does not match parent content type" );
            }

            IResource category = Core.ResourceStore.BeginNewResource( "Category" );
            category.SetProp( Core.Props.Name, name );
            category.SetProp( Core.Props.Parent, parent );

            if ( contentType != null )
            {
                category.SetProp( Core.Props.ContentType, contentType );
            }

            category.EndUpdate();
			return category;
        }

        /**
         * Checks if the new name of the category is valid, and if it is, renames
         * the category to the new name.
         */

        public bool CheckRenameCategory( IWin32Window parentWindow,
            IResource category, string newName )
        {
            if ( newName == null )
                return false;

            if ( newName.Trim().Length == 0 )
            {
                MessageBox.Show( parentWindow, "A category name may not be empty",
                                               "Rename Category", MessageBoxButtons.OK );
                return false;
            }

            IResource existingCategory = FindCategory( category.GetLinkProp( Core.Props.Parent ), newName );
            if ( existingCategory != null && existingCategory != category )
            {
                MessageBox.Show( parentWindow, "A category named " + newName + " already exists",
                                               "Rename Category", MessageBoxButtons.OK );
                return false;
            }

            new ResourceProxy( category ).SetProp( Core.Props.Name, newName );
            return true;
        }

        /**
         * Returns the number of resources not matching the specified content type
         * in the specified category and its subcategories.
         */

        public int GetUnmatchingResources( IResource category, string contentType )
        {
            int result = 0;
            foreach( IResource res in category.GetLinksOfType( null, _propCategory ) )
            {
                if ( res.Type != contentType )
                    result++;
            }
            foreach( IResource child in category.GetLinksTo( "Category", Core.Props.Parent ) )
            {
                result += GetUnmatchingResources( child, contentType );
            }
            return result;
        }

        /**
         * Sets the specified content type for a category and its subcategories.
         */

        public void SetContentTypeRecursive( IResource res, string contentType )
        {
            if ( contentType != null )
            {
                new ResourceProxy( res ).SetProp( Core.Props.ContentType, contentType );
            }
            else
            {
                new ResourceProxy( res ).DeleteProp( Core.Props.ContentType );
            }

            foreach( IResource child in res.GetLinksTo( "Category", Core.Props.Parent ) )
            {
                SetContentTypeRecursive( child, contentType );
            }
        }

        /// <summary>
        /// Deletes resource-specific category roots which do not have any categories under them.
        /// </summary>
        public static void DeleteUnusedCategoryRoots()
        {
            foreach( IResource res in Core.ResourceStore.GetAllResources( "ResourceTreeRoot" ) )
            {
                string contentType = res.GetStringProp( "RootResourceType" );
                if ( contentType != null && contentType.StartsWith( "Category:" ) &&
                     res.GetLinksTo( null, Core.Props.Parent ).Count == 0 )
                {
                    res.Delete();
                }
            }
        }
	}
}
