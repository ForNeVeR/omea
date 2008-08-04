/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Collections;
using EMAPILib;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    public class ExportCategories : AbstractNamedJob
    {
        private IResource _mail;
        private string _entryId;
        private string _storeId;
        static private Tracer _tracer = new Tracer("[EXPORT_CATEGORIES]");
        private ExportCategories( IResource mail, string entryId, string storeId )
        {
            _mail = mail;
            _entryId = entryId;
            _storeId = storeId;
        }
        public static void Do( JobPriority jobPriority, IResource mail )
        {
            string storeId = null;
            string entryId = null;
            if ( IsDataCorrect( mail, out entryId, out storeId ) )
            {
                OutlookSession.OutlookProcessor.QueueJob( jobPriority, new ExportCategories( mail, entryId, storeId ) );
            }
        }
        private static bool IsDataCorrect( IResource mail, out string entryId, out string storeId )
        {
            storeId = null;
            entryId = null;
            if ( Mail.MailInIMAP( mail ) )
            {
                return false;
            }
            entryId = mail.GetStringProp( "EntryID" );
            storeId = string.Empty;
            IResource MAPIStore = mail.GetLinkProp("OwnerStore");
            if ( MAPIStore == null )
            {
                return false;
            }
            else
            {
                storeId = MAPIStore.GetStringProp("StoreID");
            }
            return true;
        }
        #region IUnitOfWork Members

        protected override void Execute()
        {
            IEMessage message = OutlookSession.OpenMessage( _entryId, _storeId );
            if ( message == null ) return;
            using ( message ) 
            {
                if ( ProcessCategories( message, _mail ) )
                {
                    OutlookSession.SaveChanges( "Export categoryies for resource id = " + _mail.Id, message, _entryId );
                }
            }
        }

        static public bool ProcessCategories( IEMAPIProp message, IResource resource )
        {
            return ProcessCategories( message, GetCategoriesList( resource ) );
        }

        static public bool ProcessCategories( IEMAPIProp message, ArrayList categories )
        {
            ArrayList storedCategories = OutlookSession.GetCategories( message );
    
            if ( categories.Count > 0 )
            {
                return UpdateCategories( categories, storedCategories, message );
            }
            else
            {
                return RemoveCategories( storedCategories, message );
            }
        }

        private static bool UpdateCategories( ArrayList categories, ArrayList storedCategories, IEMAPIProp message )
        {
            bool changes = false;
            if ( storedCategories == null && categories == null )
            {
                changes = false;
            }
            else if ( storedCategories != null && categories != null && categories.Count != storedCategories.Count )
            {
                changes = true;
            }
            else if ( storedCategories == null && categories != null && categories.Count > 0 )
            {
                changes = true;
            }
            else if ( categories == null && storedCategories != null && storedCategories.Count > 0 )
            {
                changes = true;
            }
            if ( storedCategories != null && !changes )
            {
                storedCategories.Sort();
                categories.Sort();
                for ( int i = 0; i < categories.Count; i++ )
                {
                    if ( (string)categories[i] != (string)storedCategories[i] )
                    {
                        changes = true;
                        break;   
                    }
                }
            }
            if ( changes )
            {
                _tracer.Trace( "TO BE EXPORT FOR CATEGORIES" );
                _tracer.Trace( "Old categories:" );
                if ( storedCategories != null )
                {
                    foreach ( string str in storedCategories )
                    {
                        _tracer.Trace( str );
                    }
                }
                _tracer.Trace( "New categories:" );
                if ( categories != null )
                {
                    foreach ( string str in categories )
                    {
                        _tracer.Trace( str );
                    }
                }
                _tracer.Trace( "TO BE EXPORT FOR CATEGORIES END" );
                OutlookSession.SetCategories( message, categories );
            }
            return changes;
        }

        private static bool RemoveCategories( ArrayList storedCategories, IEMAPIProp message )
        {
            if ( storedCategories != null )
            {
                OutlookSession.SetCategories( message, null );
                return true;
            }
            return false;
        }
        static public void LoadCategoriesArrayList( IResourceList categoriesList, ArrayList categories )
        {
            foreach ( IResource categoryRes in categoriesList )
            {
                string category = categoryRes.GetStringProp( "Name" );
                IResource parentCategory = categoryRes.GetLinkProp( "Parent" );
                while ( parentCategory != null && parentCategory.Type != "ResourceTreeRoot" )
                {
                    category = parentCategory.GetStringProp( "Name" ) + "\\" + category;
                    parentCategory = parentCategory.GetLinkProp( "Parent" );
                }
                categories.Add( category );
            }
        }
        static private ArrayList GetCategoriesList( IResource resource )
        {
            IResourceList categoriesList = Core.CategoryManager.GetResourceCategories( resource );
            ArrayList categories = new ArrayList( categoriesList.Count );
            LoadCategoriesArrayList( categoriesList, categories );
            return categories;
        }

        #endregion

        public override string Name
        {
            get { return "Export Categories for mail"; }
        }
    }
}