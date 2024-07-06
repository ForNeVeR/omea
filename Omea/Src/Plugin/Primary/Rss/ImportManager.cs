// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
	internal class ImportManager
	{
        private const string _importPaneName = "Import Feeds Subscriptions";
        private Hashtable _feedImporters = null;
        private HashSet _selectedFeedImporters = null;
	    private WizardForm _parentWizard = null;
        private bool _imported = false;
        private bool _importedCache = false;
	    private IResourceList _oldRSSFeedList = null;

	    internal ImportManager( WizardForm parentWizard, Hashtable importers )
		{
            _feedImporters = importers;
            _parentWizard = parentWizard;
		}

        internal static string ImportPaneName
        {
            get { return _importPaneName; }
        }

        internal WizardForm Wizard
        {
            get { return _parentWizard; }
        }

        internal Hashtable Importers { get { return _feedImporters; } }

        internal void SelectImporter(string name, bool select)
        {
            if( ! _feedImporters.ContainsKey( name ) )
            {
                return;
            }

            if( _selectedFeedImporters == null )
            {
                _selectedFeedImporters = new HashSet();
            }

            if( select )
            {
                _selectedFeedImporters.Add( name );
            }
            else
            {
                _selectedFeedImporters.Remove( name );
            }
        }

        internal bool ImportPossible
        {
            get { return _selectedFeedImporters != null && _selectedFeedImporters.Count > 0; }
        }

	    internal AbstractOptionsPane GetImportWizardPane()
	    {
            return new FeedsImportPane( this );
        }

        internal void DoImport( IResource importRoot, bool addToWorkspace )
        {
            if ( _selectedFeedImporters != null && ! _imported )
            {
                Core.ResourceAP.RunJob( new ImportJob( PerformImport ), importRoot, addToWorkspace );
            }
        }

        internal void DoImportCache()
        {
            if ( _selectedFeedImporters != null && _imported && ! _importedCache )
            {
                Core.ResourceAP.RunJob( new MethodInvoker( PerformImportCache ) );
            }
        }

        internal void RepeatImport()
        {
            _imported = false;
        }

        private delegate void ErrorReportJob( string importer, string message );

        private delegate void ImportJob( IResource importRoot, bool addToWorkspace );
        private void PerformImport( IResource importRoot, bool addToWorkspace )
        {
            _imported = true;
            if( addToWorkspace )
            {
                _oldRSSFeedList = Core.ResourceStore.GetAllResources( "RSSFeed" );
                // Instantinate
                _oldRSSFeedList.IndexOf( importRoot );
            }
            foreach ( HashSet.Entry importerName in  _selectedFeedImporters )
            {
                IFeedImporter importer = _feedImporters[ importerName.Key as string ] as IFeedImporter;
                try
                {
                    importer.DoImport( importRoot, addToWorkspace );
                }
                catch( Exception ex )
                {
                    Core.UIManager.QueueUIJob( new ErrorReportJob( ReportImportError ), importerName.Key as string, ex.Message );
                }
            }
            // Schedule all feeds to update
            if( addToWorkspace )
            {
                ScheduleForUpdate( importRoot );
                _oldRSSFeedList = null;
            }
        }

        private void ScheduleForUpdate( IResource root )
	    {
            RSSPlugin plug = RSSPlugin.GetInstance();
            if( plug == null )
            {
                return;
            }
            foreach( IResource res in root.GetLinksTo( null, Core.Props.Parent ).ValidResources )
            {
                if ( res.Type == "RSSFeedGroup" )
                {
                    ScheduleForUpdate( res );
                }
                else if ( res.Type == "RSSFeed" && _oldRSSFeedList.IndexOf( res ) == -1 )
                {
                    if( ! res.HasProp( Props.UpdateFrequency  ) || ! res.HasProp( Props.UpdatePeriod ) )
                    {
                        res.SetProp( Props.UpdateFrequency, 4 );
                        res.SetProp( Props.UpdatePeriod, UpdatePeriods.Hourly );
                    }
                    plug.ScheduleFeedUpdate( res );
                }
            }
        }

	    private void PerformImportCache()
        {
            _importedCache = true;
            foreach ( HashSet.Entry importerName in  _selectedFeedImporters )
            {
                IFeedImporter importer = _feedImporters[ importerName.Key as string ] as IFeedImporter;
                try
                {
                    importer.DoImportCache();
                }
                catch( Exception ex )
                {
                    Core.UIManager.QueueUIJob( new ErrorReportJob( ReportCacheImportError ), importerName.Key as string, ex.Message );
                }
            }
            _selectedFeedImporters = null;
        }

	    public bool FeedsImported
	    {
	        get { return _imported; }
	    }

        private void ReportImportError( string importer, string message )
        {
            message = "Error occured when subscription was imported from '" + importer + "':\n" + message;
            MessageBox.Show( message, "Feeds Subscription Import", MessageBoxButtons.OK, MessageBoxIcon.Error );
        }

        private void ReportCacheImportError( string importer, string message )
        {
            message = "Error occured when items cache was imported from '" + importer + "':\n" + message;
            MessageBox.Show( message, "Feeds Subscription Import", MessageBoxButtons.OK, MessageBoxIcon.Error );
        }

    }
}
