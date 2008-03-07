/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using System.Drawing;
using JetBrains.DataStructures;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.RichText;

namespace JetBrains.Omea.RSSPlugin
{
    internal class GroupUnreadCountDecorator: IResourceNodeDecorator
    {
        private TextStyle _unreadTextStyle = new TextStyle( FontStyle.Regular,
            Color.Blue, SystemColors.Window );
        private IResourceList _feeds;
        private IResourceList _feedGroups;
        private IntHashTableOfInt _groupUnreadCounts = new IntHashTableOfInt();  // group ID -> unread count

        public event ResourceEventHandler DecorationChanged;

        private int _propUnreadCount;
        private int _propWorkspaceVisible;
        private IResource _activeWs;

        public GroupUnreadCountDecorator()
        {
            _groupUnreadCounts.MissingKeyValue = 0;
            _propUnreadCount = Core.ResourceStore.PropTypes ["UnreadCount"].Id;
            _propWorkspaceVisible = Core.ResourceStore.PropTypes ["WorkspaceVisible"].Id;
            
            _feeds = Core.ResourceStore.FindResourcesWithPropLive( "RSSFeed", Core.Props.Parent );
            _feeds.ResourceAdded += new ResourceIndexEventHandler( _feeds_ResourceAdded );
            _feeds.ResourceDeleting += new ResourceIndexEventHandler( _feeds_ResourceDeleting );
            _feeds.ResourceChanged += new ResourcePropIndexEventHandler( _feeds_ResourceChanged );

            _feedGroups = Core.ResourceStore.FindResourcesWithPropLive( "RSSFeedGroup", Core.Props.Parent );
            _feedGroups.ResourceChanged += new ResourcePropIndexEventHandler( _feedGroups_ResourceChanged );

            Core.WorkspaceManager.WorkspaceChanged += new EventHandler( HandleWorkspaceChanged );
        }

        private int GetGroupUnreadCount( IResource res )
        {
            lock( _groupUnreadCounts )
            {
                return _groupUnreadCounts [res.Id];
            }
        }

        private void SetGroupUnreadCount( IResource res, int count )
        {
            lock( _groupUnreadCounts )
            {
                _groupUnreadCounts [res.Id] = count;
            }
        }

        public bool DecorateNode( IResource res, RichText nodeText )
        {
            if ( res.Type == "RSSFeedGroup" )
            {
                int unreadCount = GetGroupUnreadCount( res );
                if ( unreadCount > 0 )
                {
                    nodeText.Append( " " );
                    nodeText.SetStyle( FontStyle.Bold, 0, res.DisplayName.Length );
                    nodeText.Append( "(" + unreadCount + ")", _unreadTextStyle );
                }
                return true;
            }
            return false;
        }

        public string DecorationKey
        {
            get { return UnreadNodeDecorator.Key; }
        }

        private void _feeds_ResourceAdded( object sender, ResourceIndexEventArgs e )
        {
            AdjustGroupUnreadCount( e.Resource, e.Resource.GetIntProp( _propUnreadCount ) );
        }

        private void _feeds_ResourceDeleting( object sender, ResourceIndexEventArgs e )
        {
            AdjustGroupUnreadCount( e.Resource, -e.Resource.GetIntProp( _propUnreadCount ) );
        }

        private void _feeds_ResourceChanged( object sender, ResourcePropIndexEventArgs e )
        {
            if ( e.ChangeSet == null )
            {
                return;
            }
            if ( e.ChangeSet.IsPropertyChanged( Core.Props.Parent ) || 
                (_activeWs != null && e.ChangeSet.GetLinkChange( _propWorkspaceVisible, _activeWs.Id ) != LinkChangeType.None  ) )
            {
                UpdateGroupUnreadCount( true );
            }
            else if ( e.ChangeSet.IsPropertyChanged( _propUnreadCount ) )
            {
                object oldValue = e.ChangeSet.GetOldValue( _propUnreadCount );
                int oldCount = (oldValue == null) ? 0 : (int) oldValue;
                int newCount = e.Resource.GetIntProp( _propUnreadCount );
                AdjustGroupUnreadCount( e.Resource, newCount - oldCount );
            }
        }

        private void _feedGroups_ResourceChanged( object sender, ResourcePropIndexEventArgs e )
        {
            if ( e.ChangeSet.IsPropertyChanged( Core.Props.Parent ) ||
                ( _activeWs != null && e.ChangeSet.GetLinkChange( _propWorkspaceVisible, _activeWs.Id ) != LinkChangeType.None  ) )
            {
                UpdateGroupUnreadCount( true );
            }
        }

        private void HandleWorkspaceChanged( object sender, EventArgs e )
        {
            _activeWs = Core.WorkspaceManager.ActiveWorkspace;
            UpdateGroupUnreadCount( true );
        }

        private void AdjustGroupUnreadCount( IResource feed, int countDelta )
        {
            if ( countDelta == 0 )
                return;

            IResource group = feed.GetLinkProp( Core.Props.Parent );
            while( group != null && group.Type == "RSSFeedGroup" && 
                (_activeWs == null || group.HasLink( _propWorkspaceVisible, _activeWs ) ) )
            {
                int newUnreadCount = GetGroupUnreadCount( group ) + countDelta;
                Debug.Assert( newUnreadCount >= 0 );
                if ( newUnreadCount < 0 )
                {
                    newUnreadCount = 0;
                }
                SetGroupUnreadCount( group, newUnreadCount );

                if ( DecorationChanged != null )
                {
                    DecorationChanged( this, new ResourceEventArgs( group ) );
                }

                group = group.GetLinkProp( "Parent" );
            }
        }

        internal void UpdateGroupUnreadCount( bool fireDecorationChanged )
        {
            foreach( IResource group in RSSPlugin.RootFeedGroup.GetLinksTo( "RSSFeedGroup", Core.Props.Parent ) )
            {
                Trace.WriteLineIf( Settings.Trace, "Updating unread count for group " + group.DisplayName );
                UpdateGroupUnreadCountRecursive( group, fireDecorationChanged );
            }
        }

        private void UpdateGroupUnreadCountRecursive( IResource group, bool fireDecorationChanged )
        {
            Trace.WriteLineIf( Settings.Trace, "Recursive update of unread count for group " + group.DisplayName );
            int count = 0;


            IResourceList childGroups = group.GetLinksTo( "RSSFeedGroup", Core.Props.Parent );
            if ( Core.WorkspaceManager.ActiveWorkspace != null )
            {
                childGroups = childGroups.Intersect( Core.WorkspaceManager.ActiveWorkspace.GetLinksOfType( null, "WorkspaceVisible" ), true );
            }
            foreach( IResource childGroup in childGroups )
            {
                UpdateGroupUnreadCountRecursive( childGroup, fireDecorationChanged );
                int childUnreadCount = GetGroupUnreadCount( childGroup );
                Trace.WriteLineIf( Settings.Trace, group.DisplayName + ": Included " + childUnreadCount + " unreads from child group " + childGroup.DisplayName );
                count += childUnreadCount;
            }


            IResourceList childFeeds = group.GetLinksTo( "RSSFeed", Core.Props.Parent );
            if ( Core.WorkspaceManager.ActiveWorkspace != null )
            {
                childFeeds = childFeeds.Intersect( Core.WorkspaceManager.ActiveWorkspace.GetLinksOfType( null, "WorkspaceVisible" ), true );
            }
            foreach( IResource feed in childFeeds )
            {
                int feedCount = feed.GetIntProp( _propUnreadCount );
                Trace.WriteLineIf( Settings.Trace, group.DisplayName + ": Included " + feedCount + " unreads from feed " + feed.DisplayName );
                count += feedCount;
            }
            
            if ( GetGroupUnreadCount( group ) != count )
            {
                SetGroupUnreadCount( group, count );
                if ( fireDecorationChanged && DecorationChanged != null )
                {
                    DecorationChanged( this, new ResourceEventArgs( group ) );
                }
            }
            else
            {
                Trace.WriteLineIf( Settings.Trace, "Skipped update of group unread count for " + group.DisplayName + 
                                                   " because the value did not change" );
            }
        }
    }
}
