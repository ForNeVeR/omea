// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.OutlookPlugin
{
    public class ContactDragDropHandler : IResourceDragDropHandler
    {
        private static IResourceDragDropHandler parentHandler;
        public static void Register()
        {
            parentHandler = Core.PluginLoader.GetResourceDragDropHandler( "AddressBook" );
            Guard.NullMember( parentHandler, "parentHandler" );
            Core.PluginLoader.RegisterResourceDragDropHandler( "AddressBook", new ContactDragDropHandler() );
        }
        public void AddResourceDragData( IResourceList dragResources, IDataObject dataObject )
        {
            parentHandler.AddResourceDragData( dragResources, dataObject );
        }

        public DragDropEffects DragOver( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
        {
            if ( targetResource.HasProp( PROP.Imported ) )
            {
                IResourceList dragResources = data.GetData( typeof (IResourceList) ) as IResourceList;
                if ( dragResources.AllResourcesOfType( "Contact" ) )
                {
                    foreach ( IResource resource in dragResources.ValidResources )
                    {
                        if ( resource.HasProp( PROP.EntryID ) )
                        {
                            return DragDropEffects.Copy;
                        }
                    }
                }
            }
            return parentHandler.DragOver( targetResource, data, allowedEffect, keyState );
        }
        public void Drop( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
        {
            IResourceList dragResources = data.GetData( typeof (IResourceList) ) as IResourceList;
            if ( dragResources != null )
            {
                ResourcesDropped( targetResource, dragResources );
            }
        }
        public void ResourcesDropped( IResource targetResource, IResourceList droppedResources )
        {
            Core.ResourceAP.QueueJob( JobPriority.Immediate,
                                      new DropResourcesDelegate( DoDropResources ), targetResource, droppedResources );
        }

        private void DoDropResources( IResource targetResource, IResourceList droppedResources )
        {
            foreach ( IResource res in droppedResources.ValidResources )
            {
                if ( targetResource.HasProp( PROP.Imported ) && res.HasProp( PROP.EntryID ) )
                {
                    ImportedContactsChangeWatcher.ImportedContactAdded( res, targetResource );
                }
                else
                {
                    res.AddLink( "InAddressBook", targetResource );
                }
            }
        }

        private delegate void DropResourcesDelegate( IResource targetRes, IResourceList droppedResources );
    }

    public class OutlookUIHandler : IResourceUIHandler, IResourceDragDropHandler
    {
        private int _lastFolderID = -1;
        private IResourceList _currentResourceList = null;
        private DisplayMailsInFolder _displayMailsInFolderAction = new DisplayMailsInFolder();
        private MoveMessageToFolderAction _moveAction = new MoveMessageToFolderAction();
        private MoveFolderToFolderAction _moveFolderAction = new MoveFolderToFolderAction();
        private bool _lastDisplayUnread;

        public OutlookUIHandler()
        {}

    	#region IResourceUIHandler Members
        public void ResourceNodeSelected( IResource folder )
        {
            bool displayUnread = folder.HasProp( Core.Props.DisplayUnread );

            if ( _lastFolderID != folder.Id || displayUnread != _lastDisplayUnread )
            {
                Trace.WriteLine( ">>> OutlookUIHandler.ResourceNodeSelected" );
                _currentResourceList = CreateResourceList( folder );
                _lastFolderID = folder.Id;
                _lastDisplayUnread = displayUnread;
                if ( displayUnread )
                {
                    _currentResourceList = _currentResourceList.Intersect(
                        Core.ResourceStore.FindResourcesWithProp( SelectionType.LiveSnapshot, null, "IsUnread" ), true );
                }
            }
            _displayMailsInFolderAction.DisplayResourceList( folder, _currentResourceList );
            PairIDs folderIDs = PairIDs.Get( folder );
            if ( folderIDs != null )
            {
                OutlookSession.OutlookProcessor.QueueJob( JobPriority.Immediate, new RefreshFolderDelegate(RefreshFolder), folderIDs, folder );
            }
        }

        public bool CanDropResources( IResource targetResource, IResourceList dragResources )
        {
			return false;	// Moved to DragOver
        }

        public void ResourcesDropped( IResource targetResource, IResourceList droppedResources )
        {
			return;	// Moved to Drop
        }

        public bool CanRenameResource( IResource res )
        {
            if ( res.Type == STR.MAPIFolder )
            {
                return !Folder.IsDefault( res );
            }
            return false;
        }

        public bool ResourceRenamed( IResource res, string newName )
        {
            if ( res.Type == STR.MAPIFolder && newName != null && newName.Length > 0 )
            {
                new ResourceProxy( res ).SetProp( Core.Props.Name, newName );
                new RenameFolderProcessor( res, newName );
                return true;
            }
            return false;
		}
    	#endregion IResourceUIHandler Members

    	#region IResourceDragDropHandler Members
    	public void Drop( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
    	{
			if( data.GetDataPresent( typeof(IResourceList) ) ) // Dragging resources over
			{
				// The resources we're dragging
				IResourceList dragResources = (IResourceList)data.GetData( typeof(IResourceList) );

				if ( targetResource != null && dragResources != null && dragResources.Count > 0 )
				{
					if ( ListOfMailsOrLinkedAttachments( dragResources ) )
					{
                        IResourceList mailsOnly = ResourceTypeHelper.ExtractListForType( dragResources, STR.Email );
						_moveAction.DoMove( targetResource, mailsOnly );
					}
					else
                    if (  dragResources.AllResourcesOfType( STR.MAPIFolder ) )
					{
						_moveFolderAction.DoMove( targetResource, dragResources );
					}
				}
			}
    	}

    	public DragDropEffects DragOver( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
    	{
			if( data.GetDataPresent( typeof(IResourceList) ) ) // Dragging resources over
			{
				// The resources we're dragging
				IResourceList dragResources = (IResourceList)data.GetData( typeof(IResourceList) );

				if ( ListOfMailsOrLinkedAttachments( dragResources ) )
				{
					return DragDropEffects.Move;
				}
				if ( !dragResources.AllResourcesOfType( STR.MAPIFolder ) )
				{
					return DragDropEffects.None;
				}
				foreach ( IResource folder in dragResources.ValidResources )
				{
					//  A default folder cannot be moved, so it can be dropped
                    //  only to its own parent for the sake of reordering
					if( (Folder.IsDefault( folder )) && (Folder.GetParent( folder ) != targetResource) )
						return DragDropEffects.None;

					// Don't allow dropping to self and own children
					if((folder == targetResource) || ( Folder.IsAncestor( targetResource, folder ) ))
						return DragDropEffects.None;
				}
				return DragDropEffects.Move;
			}
			return DragDropEffects.None;
    	}

    	public void AddResourceDragData( IResourceList dragResources, IDataObject dataObject )
		{
			if( !dataObject.GetDataPresent( typeof(string) ) )
			{
				StringBuilder sb = StringBuilderPool.Alloc();
				try
				{
					foreach( IResource resource in dragResources )
					{
						if( sb.Length != 0 )
							sb.Append( ", " );
						string text = resource.DisplayName;
						if( text.IndexOf( ' ' ) > 0 )
							sb.Append( "“" + text + "”" );
						else
							sb.Append( text );
					}
					dataObject.SetData( sb.ToString() );
				}
				finally
				{
					StringBuilderPool.Dispose( sb );
				}
			}
		}
    	#endregion

        private delegate void RefreshFolderDelegate( PairIDs folderIDs, IResource folder );

        private void RefreshFolder( PairIDs folderIDs, IResource folder )
        {
            Guard.NullArgument( folderIDs, "folderIDs" );
            Guard.NullArgument( folder, "folder" );
            FolderDescriptor descriptor = FolderDescriptor.Get( folderIDs );
            if ( descriptor != null )
            {
                if ( !ProcessedFolders.IsFolderProcessed( folder.GetStringProp( PROP.EntryID ) ) )
                {
                    RefreshFolderDescriptor.Do( JobPriority.Normal, descriptor, Settings.IndexStartDate );
                }
            }
        }

        private IResourceList CreateResourceList( IResource folder )
        {
            return Folder.GetMailListLive( folder );
        }

        //  Proper list consist of either email resources or arbitrary
        //  others if they are proper attachments of emails from this list.
		private bool  ListOfMailsOrLinkedAttachments( IResourceList list )
		{
		    foreach( IResource res in list )
		    {
		        if( res.Type != STR.Email )
		        {
		            IResource parent = res.GetLinkProp( PROP.Attachment );
                    if( parent == null || list.IndexOf( parent ) == -1 )
                        return false;
		        }
		    }
            return true;
		}
    }

	#region OutlookRootDragDropHandler Class — Allows reordering at the root level of the resource tree

	internal class OutlookRootDragDropHandler : IResourceDragDropHandler
	{
		#region IResourceDragDropHandler Members

		public void Drop(IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState)
		{
			// Nothing to do, it's just reordering
		}

		public DragDropEffects DragOver(IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState)
		{
			if( data.GetDataPresent( typeof(IResourceList) ) ) // Dragging resources over
			{
				// The resources we're dragging
				IResourceList dragResources = (IResourceList)data.GetData( typeof(IResourceList) );

				IResource root = Core.ResourceTreeManager.GetRootForType( STR.MAPIFolder );
				if( targetResource == root )
				{
					// Dragging into empty space / tree root: allow only those that are already direct children of the root
					IResourceList parents;
					bool bAllUnderRoot = true;
					foreach( IResource res in dragResources )
					{
						if( ((parents = res.GetLinksFrom( root.Type, Core.Props.Parent )).Count != 1) || (parents[ 0 ] != root) )
						{
							bAllUnderRoot = false;
							break;
						}
					}
					return bAllUnderRoot ? DragDropEffects.Move : DragDropEffects.None;
				}
			}
			return DragDropEffects.None;
		}

		public void AddResourceDragData(IResourceList dragResources, IDataObject dataObject)
		{
		}

		#endregion

	}

	#endregion

	internal class OutlookFoldersFilter : IResourceNodeFilter
    {
        private bool _alwaysShowExcludedFolders = false;
        public OutlookFoldersFilter( )
        {}
        public OutlookFoldersFilter( bool alwaysShowExcludedFolders )
        {
            _alwaysShowExcludedFolders = alwaysShowExcludedFolders;
        }
        public bool AcceptNode( IResource res, int level )
        {
            if ( res.Type != STR.MAPIFolder )
            {
                return true;
            }
            if ( !_alwaysShowExcludedFolders && Folder.IsIgnored( res ) && !Settings.ShowExcludedFolders )
            {
                return !IsAllChildrenIgnored( res );
            }
            return res.HasProp( PROP.MAPIVisible );
        }
        private bool IsAllChildrenIgnored( IResource folder )
        {
            IResourceList subFolders = Folder.GetSubFolders( folder );
            foreach ( IResource subFolder in subFolders.ValidResources )
            {
                if ( !Folder.IsIgnored( subFolder ) || !IsAllChildrenIgnored( subFolder ) )
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class MAPIFoldersTreeSelectPane : ResourceTreeSelectPane
    {
        public MAPIFoldersTreeSelectPane()
        {
            _resourceTree.AddNodeFilter( new OutlookFoldersFilter() );
        }

    }
}
