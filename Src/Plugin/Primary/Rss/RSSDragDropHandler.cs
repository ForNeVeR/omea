// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.IO;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
	/// <summary>
	/// Drag and drop handler for RSS items.
	/// </summary>
	internal class RSSDragDropHandler: IResourceDragDropHandler
	{
	    private const string CLIPBOARD_FORMAT_URL = "UniformResourceLocator";
	    private const string CLIPBOARD_FORMAT_URL_W = "UniformResourceLocatorW";

	    public void AddResourceDragData( IResourceList dragResources, IDataObject dataObject )
	    {
            if ( dragResources.Count == 1 )
            {
                IResource dragResource = dragResources [0];
                if ( dragResource.Type == "RSSFeed" && dragResource.HasProp( Props.URL ) )
                {
                    dataObject.SetData( DataFormats.Text, dragResource.GetStringProp( Props.URL ) );
                }
            }
	    }

	    public DragDropEffects DragOver( IResource targetResource, IDataObject data, DragDropEffects allowedEffect,
	                                     int keyState )
	    {
	    	if( data.GetDataPresent( typeof(IResourceList) ) )
	    	{
	    		// The resources we're dragging
	    		IResourceList dragResources = (IResourceList)data.GetData( typeof(IResourceList) );

				// Check the droptarget, it must be either a folder or a tree root
				if(!((targetResource.Type == "RSSFeedGroup") || (targetResource == Core.ResourceTreeManager.GetRootForType( "RSSFeed" ))))
					return DragDropEffects.None;

	    		// Collect all the direct and indirect parents of the droptarget; then we'll check to avoid dropping parent on its children
	    		IntArrayList parentList = new IntArrayList();
	    		IResource parent = targetResource;
	    		while( parent != null )
	    		{
	    			parentList.Add( parent.Id );
	    			parent = parent.GetLinkProp( Core.Props.Parent );
	    		}

	    		bool bAllDroppable = true; // Feeds or groups are being dragged
	    		foreach( IResource res in dragResources )
	    		{
	    			// Dropping parent over its child?
	    			if( parentList.IndexOf( res.Id ) >= 0 )
	    				return DragDropEffects.None;

	    			// Constraint the resource types of the resources being dropped
	    			bAllDroppable = bAllDroppable && ( (res.Type == "RSSFeed") || (res.Type == "RSSFeedGroup") );
	    		}
	    		return bAllDroppable ? DragDropEffects.Move : DragDropEffects.None;
	    	}
            else if ( data.GetDataPresent( CLIPBOARD_FORMAT_URL ) || data.GetDataPresent( CLIPBOARD_FORMAT_URL_W ) )
	    	{
	    		return DragDropEffects.Copy;
	    	}

	    	return DragDropEffects.None;
	    }

		public void Drop( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState )
        {
            if ( targetResource == null )
                return;

            if ( data.GetDataPresent( typeof(IResourceList) ) )
            {
                IResourceList droppedResources = (IResourceList) data.GetData( typeof(IResourceList) );
                foreach( IResource res in droppedResources )
                {
					// Drop only feeds and newsgroups, and don't drop on self
                    if ( (res.Id != targetResource.Id) && ( (res.Type == "RSSFeed") || (res.Type == "RSSFeedGroup") ) )
                    {
                        Core.ResourceAP.QueueJob( JobPriority.Immediate, new SetParentDelegate( SetFeedParent ), res, targetResource );
                    }
                }
            }
            else if ( data.GetDataPresent( CLIPBOARD_FORMAT_URL_W ) )
            {
                ProcessUrlDrop( targetResource, data, CLIPBOARD_FORMAT_URL_W, Encoding.Unicode );
            }
            else if ( data.GetDataPresent( CLIPBOARD_FORMAT_URL ) )
            {
                ProcessUrlDrop( targetResource, data, CLIPBOARD_FORMAT_URL, Encoding.Default );
            }
        }

	    private void SetFeedParent( IResource res, IResource parent )
	    {
            res.SetProp( Core.Props.Parent, parent );
            Core.WorkspaceManager.AddToActiveWorkspace( res );
            Core.WorkspaceManager.CleanWorkspaceLinks( res );
	    }

	    private delegate void SetParentDelegate( IResource res, IResource parent );

	    private void ProcessUrlDrop( IResource targetResource, IDataObject data, string format, Encoding encoding )
	    {
	        Stream dataStream = (Stream) data.GetData( format );
            if ( dataStream != null )
            {
                string url = Utils.StreamToString( dataStream, encoding );
                RSSPlugin.GetInstance().ShowAddFeedWizard( url, targetResource );
            }
        }
	}
}
