// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
	/// <summary>
	/// Operation to import and preview importing of RSS feeds.
	/// </summary>
    internal class ImportFeedsOperation
    {
        private Stream _importStream;
        private IResource _importRoot;
        private string _importFileName;
        private bool _importPreview;
        private IResource _previewRoot;

        public ImportFeedsOperation( Stream importStream, IResource importRoot, string importFileName,
            bool importPreview )
        {
            _importStream = importStream;
            _importRoot = importRoot;
            _importFileName = importFileName;
            _importPreview = importPreview;
        }

        internal void Enqueue()
        {
            Core.ResourceAP.QueueJob( new MethodInvoker( ExecuteOperation ) );
        }

        internal void ExecuteOperation()
        {
            IResource rootGroup = _importRoot;
            if ( _importPreview )
            {
                _previewRoot = Core.ResourceStore.NewResource( "RSSFeedGroup" );
                rootGroup = _previewRoot;
            }

            bool hasOPML = true;
            try
            {
                hasOPML = OPMLProcessor.Import( new StreamReader( _importStream ), rootGroup, !_importPreview );
            }
            catch( Exception ex )
            {
                MessageBox.Show( Core.MainWindow,
                    "Error importing OPML file " + _importFileName + ":\n" + ex.Message,
                    "Import OPML", MessageBoxButtons.OK );
                // the import may have been partially successful, and we still want to
                // update the feeds that were imported successfully
            }

            if ( !hasOPML )
            {
                MessageBox.Show( Core.MainWindow,
                    _importFileName + " is not an OPML file", "Import OPML", MessageBoxButtons.OK );
                return;
            }

            if ( _importPreview )
            {
                if ( _previewRoot.GetLinksOfType( null, "Parent" ).Count > 0 )
                {
                    Core.UIManager.QueueUIJob( new MethodInvoker( ShowImportPreviewDialog ) );
                }
                else
                {
                    _previewRoot.Delete();
                }
            }
            else
            {
                foreach( IResource feed in Core.ResourceStore.GetAllResources( "RSSFeed" ) )
                {
                    if ( !feed.HasProp( Props.LastUpdateTime ) && !feed.HasProp( Props.ItemCommentFeed ))
                    {
                        RSSPlugin.GetInstance().QueueFeedUpdate( feed );
                    }
                }
            }
        }

        private void ShowImportPreviewDialog()
        {
            using( ImportPreviewDlg dlg = new ImportPreviewDlg() )
            {
                dlg.ShowImportPreview( _previewRoot );
                if ( dlg.ShowDialog( Core.MainWindow ) == DialogResult.Cancel )
                {
                    Core.ResourceAP.QueueJob( JobPriority.Immediate,
                        new MethodInvoker( CancelImport ) );
                }
                else
                {
                    Core.ResourceAP.QueueJob( JobPriority.Immediate,
                        new MethodInvoker( ConfirmImport ) );
                }
            }
        }

        private void CancelImport()
        {
            RemoveFeedsAndGroupsAction.DeleteFeedGroup( _previewRoot );
        }

        private void ConfirmImport()
        {
            FeedsTreeCommiter.DoConfirmImport( _previewRoot, _importRoot );
        }
    }

}
