/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
	/// <summary>
	/// Summary description for FeedTreeCommiter.
	/// </summary>
    internal class FeedsTreeCommiter
    {
        internal delegate void ConfirmImport( IResource previewRoot, IResource importRoot );
        internal static void DoConfirmImport( IResource previewRoot, IResource importRoot )
        {
            foreach( IResource res in previewRoot.GetLinksTo( null, Core.Props.Parent ) )
            {
                if ( res.Type == "RSSFeedGroup" )
                {
                    int count = ConfirmImportRecursive( res );
                    if ( count == 0 )
                    {
                        res.Delete();
                        continue;
                    }
                    if ( RelinkExistingGroup( res, importRoot ) )
                    {
                        continue;
                    }
                    Core.WorkspaceManager.AddToActiveWorkspaceRecursive( res );
                }
                
                if ( res.Type == "RSSFeed" && res.GetIntProp( Props.Transient ) == 1 )
                {
                    // Delete all items
                    IResourceList items = res.GetLinksOfType( "RSSItem", Props.RSSItem );
                    items.DeleteAll();
                    res.Delete();
                }
                else
                {
                    res.DeleteProp( Props.Transient );
                    res.SetProp( Core.Props.Parent, importRoot );
                    if ( res.Type == "RSSFeed" )
                    {
                        Core.WorkspaceManager.AddToActiveWorkspace( res );
                        if ( RSSPlugin.GetInstance() != null )
                        {
                            RSSPlugin.GetInstance().QueueFeedUpdate( res );
                        }
                    }
                }
            }
            previewRoot.Delete();
        }

        private static bool RelinkExistingGroup( IResource newGroup, IResource existingParent )
        {
            foreach( IResource existingGroup in existingParent.GetLinksTo( "RSSFeedGroup", Core.Props.Parent) )
            {
                if ( existingGroup.GetStringProp( "Name" ) == newGroup.GetStringProp( "Name" ) )
                {
                    foreach( IResource newGroupChild in newGroup.GetLinksTo( null, "Parent" ) )
                    {
                        if ( newGroupChild.Type != "RSSFeedGroup" || !RelinkExistingGroup( newGroupChild, existingGroup ) )
                        {
                            newGroupChild.SetProp( "Parent", existingGroup );
                        }
                    }
                    newGroup.Delete();
                    return true;
                }
            }
            return false;
        }

        private static int ConfirmImportRecursive( IResource res )
        {
            int count = 0;
            foreach( IResource child in res.GetLinksTo( null, Core.Props.Parent ) )
            {
                if ( child.Type == "RSSFeedGroup" )
                {
                    int childCount = ConfirmImportRecursive( child );
                    if ( childCount == 0 )
                    {
                        child.Delete();
                    }
                    count += childCount;
                }
                else if ( child.GetIntProp( Props.Transient ) == 1 )
                {
                    IResourceList items = child.GetLinksOfType( "RSSItem", Props.RSSItem );
                    items.DeleteAll();
                    child.Delete();
                }
                else
                {
                    if ( RSSPlugin.GetInstance() != null )
                    {
                        RSSPlugin.GetInstance().QueueFeedUpdate( child );
                    }
                    count++;
                }
            }
            return count;
        }
    }
}
