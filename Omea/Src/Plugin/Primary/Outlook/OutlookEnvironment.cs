// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collections;
using JetBrains.DataStructures;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class CategorySetter
    {
        private ArrayList _categories;
        private IResource _resMail;
        private HashSet _categoriesSet;

        private CategorySetter( ArrayList categories, IResource resMail )
        {
            _categories = categories;
            _resMail = resMail;
            _categoriesSet = new HashSet();
        }
        public static void DoJob( ArrayList categories, IResource resMail )
        {
            new CategorySetter( categories, resMail ).DoJob();
        }
        public void DoJob()
        {
            foreach ( IResource category in Core.CategoryManager.GetResourceCategories( _resMail ).ValidResources )
            {
                _categoriesSet.Add( category );
            }
            if ( _categories != null && _categories.Count != 0 )
            {
                ProcessCategories();
            }
            foreach ( HashSet.Entry entry in _categoriesSet )
            {
				IResource resCategory = (IResource)entry.Key;
                Core.CategoryManager.RemoveResourceCategory( _resMail, resCategory );
			}
        }
        private void ProcessCategories()
        {
            foreach ( string strOutlookCategory in _categories )
            {
                string[] strCategories = strOutlookCategory.Split( '\\' );
                IResource resCategory = FindOrCreateCategory( strCategories );
                if ( resCategory != null )
                {
                    continue;
                }
                foreach ( string strCategory in strCategories )
                {
                    resCategory = Core.CategoryManager.FindOrCreateCategory( resCategory, strCategory );
                }
                if ( resCategory != null )
                {
                    Core.CategoryManager.AddResourceCategory( _resMail, resCategory );
					_categoriesSet.Remove( resCategory );
                }
            }
        }
        private IResource FindOrCreateCategory( string[] strCategories )
        {
            IResource resCategory = Core.CategoryManager.GetRootForTypedCategory( _resMail.Type );
            for ( int i = 0; i < strCategories.Length; ++i )
            {
                string strCategory = strCategories[i];
                if ( i == 0 )
                {
                    IResource topCategory = resCategory;
                    resCategory = Core.CategoryManager.FindCategory( resCategory, strCategory );
                    if ( resCategory == null )
                    {
                        resCategory = Core.CategoryManager.FindCategory( null, strCategory );
                        if ( resCategory == null )
                        {
                            resCategory = Core.CategoryManager.FindOrCreateCategory( topCategory, strCategory );
                        }
                    }
                }
                else
                {
                    resCategory = Core.CategoryManager.FindOrCreateCategory( resCategory, strCategory );
                }
            }
            Core.CategoryManager.AddResourceCategory( _resMail, resCategory );
            _categoriesSet.Remove( resCategory );
            return resCategory;
        }
    }
}
