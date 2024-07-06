// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Drawing;
using JetBrains.Omea.Conversations;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.RichText;

namespace JetBrains.Omea.Nntp
{
    internal class NewsNodeDecorator : IResourceNodeDecorator
    {
        private readonly TextStyle _unreadTextStyle = new TextStyle( FontStyle.Regular, Color.Blue, SystemColors.Window );
        private readonly TextStyle _draftsTextStyle = new TextStyle( FontStyle.Regular, Color.Green, SystemColors.Window );
        private readonly List<IResource> _updatedResources;
        private readonly IResourceList _allUnread;
        private readonly IResourceList _unreadArticles;

        public event ResourceEventHandler DecorationChanged;

        public NewsNodeDecorator()
        {
            _updatedResources = new List<IResource>();
            IResourceStore store = Core.ResourceStore;
            _allUnread = Core.ResourceStore.FindResourcesWithPropLive( null, Core.Props.IsUnread ).Minus(
                         Core.ResourceStore.FindResourcesWithPropLive( null, Core.Props.IsDeleted ) );
            _unreadArticles = store.FindResourcesWithPropLive( NntpPlugin._newsArticle, Core.Props.IsUnread );
            _unreadArticles = _unreadArticles.Union( store.GetAllResourcesLive( NntpPlugin._newsLocalArticle ) );
            _unreadArticles.ResourceAdded += _unreadArticles_Updated;
            _unreadArticles.ResourceChanged += _unreadArticles_ResourceChanged;
            _unreadArticles.ResourceDeleting += _unreadArticles_Updated;
            Core.ResourceAP.JobFinished += ResourceAP_JobFinished;
            Core.WorkspaceManager.WorkspaceChanged += WorkspaceManager_WorkspaceChanged;
        }

        public bool DecorateNode( IResource res, RichText nodeText )
        {
            if( res.Type == NntpPlugin._newsFolder || res.Type == NntpPlugin._newsServer )
            {
                int count;
                if( NewsFolders.IsDefaultFolder( res ) )
                {
                    IResourceList folderItems = NntpPlugin.CollectArticles( res, false );
                    IResource wsp = Core.WorkspaceManager.ActiveWorkspace;
                    if ( wsp != null )
                    {
                        folderItems = folderItems.Intersect( wsp.GetLinksOfType( null, "WorkspaceVisible" ), true );
                    }
                    if( NewsFolders.IsSentItems( res ) )
                    {
                        folderItems = folderItems.Intersect( _allUnread, true );
                    }
                    else
                    {
                        folderItems = folderItems.Minus( Core.ResourceStore.FindResourcesWithProp( null, Core.Props.IsDeleted ) );
                    }
                    count = folderItems.Count;
                }
                else
                {
                    IResourceList groups = new NewsTreeNode( res ).Groups;
                    IResource wsp = Core.WorkspaceManager.ActiveWorkspace;
                    if ( wsp != null )
                    {
                        groups = groups.Intersect( wsp.GetLinksOfType( null, "WorkspaceVisible" ), true );
                    }
                    IResourceList articles = Core.ResourceStore.EmptyResourceList;
                    foreach( IResource group in groups.ValidResources )
                    {
                        articles = articles.Union(
                            group.GetLinksTo( null, NntpPlugin._propTo ).Intersect( _allUnread ), true );
                    }
                    count = articles.Count;
                }
                if( count != 0 )
                {
                    nodeText.Append( " " );
                    nodeText.SetStyle( FontStyle.Bold, 0, res.DisplayName.Length );

                    /////////////////////////////////////////////////////////////////////
                    /// NewsFolders.IsDefaultFolder doesn't synchronously create folders,
                    /// so the check is necessary to avoid RunJobs to resource thread
                    /////////////////////////////////////////////////////////////////////
                    if( NewsFolders.IsDrafts( res ) )
                    {
                        nodeText.Append( "[" + count + "]", _draftsTextStyle );
                    }
                    else
                    {
                        nodeText.Append( "(" + count + ")", _unreadTextStyle );
                    }
                }
                return true;
            }
            return false;
        }

        public string DecorationKey
        {
            get { return UnreadNodeDecorator.Key; }
        }

        private void _unreadArticles_Updated( object sender, ResourceIndexEventArgs e )
        {
            _updatedResources.Add( e.Resource );
        }

        private void _unreadArticles_ResourceChanged( object sender, ResourcePropIndexEventArgs e )
        {
            if( e.ChangeSet.IsPropertyChanged( Core.Props.IsDeleted ) )
            {
                _updatedResources.Add( e.Resource );
            }
        }

        private void ResourceAP_JobFinished( object sender, EventArgs e )
        {
            if( DecorationChanged != null )
            {
                foreach( IResource res in _updatedResources )
                {
                    DecorateArticle( res );
                }
            }
            _updatedResources.Clear();
        }

        private void DecorateArticle( IResource article )
        {
            IResourceList groups = article.GetLinksFrom( null, NntpPlugin._propTo );
            foreach( IResource group in groups )
            {
                IResource res = group;
                do
                {
                    if( res.Type != NntpPlugin._newsGroup )
                    {
                        Core.UserInterfaceAP.QueueJob( new ResourceDelegate( DecorateResource ), res );
                    }
                    res = new NewsTreeNode( res ).Parent;
                }
                while( res != null && res != NewsFolders.Root );
            }
        }

        private void WorkspaceManager_WorkspaceChanged( object sender, EventArgs e )
        {
            ResourceDelegate job = DecorateResource;
            Core.UserInterfaceAP.QueueJob( job, NewsFolders.Drafts );
            Core.UserInterfaceAP.QueueJob( job, NewsFolders.Outbox );
        }

        private void DecorateResource( IResource res )
        {
            if( DecorationChanged != null )
            {
                DecorationChanged( this, new ResourceEventArgs( res ) );
            }
        }
    }

    internal class WatchedArticlesDecorator : IResourceNodeDecorator
    {
        private const string _cKey = "WatchedUnreadArticlesDecorator";

        private readonly TextStyle _watchedTextStyle = new TextStyle( FontStyle.Bold, Color.Red, SystemColors.Window );
        private readonly IResourceList _unreadArticles;

        private IResourceList _heads;
        private readonly IResourceList _formattingRules;
        private readonly Dictionary<IResource, List<IResource>> _groups2watchedHeads = new Dictionary<IResource, List<IResource>>();

        public event ResourceEventHandler DecorationChanged;

        public WatchedArticlesDecorator()
        {
            IResourceStore store = Core.ResourceStore;

            _unreadArticles = store.FindResourcesWithPropLive( NntpPlugin._newsArticle, Core.Props.IsUnread );
            _unreadArticles = _unreadArticles.Minus( store.FindResourcesWithPropLive( null, Core.Props.IsDeleted ));
            _unreadArticles.ResourceAdded += _unreadArticles_Updated;
            _unreadArticles.ResourceDeleting += _unreadArticles_Updated;

            _formattingRules = Core.FilterRegistry.GetFormattingRules( false );
            _formattingRules.ResourceAdded += RuleConditionsListChanged;
            _formattingRules.ResourceDeleting += RuleConditionsListChanged;
            _formattingRules.ResourceChanged += RuleConditionsChanged;
            HeadsChanged();
        }

        public bool DecorateNode( IResource res, RichText nodeText )
        {
            if( res.Type == NntpPlugin._newsGroup )
            {
                int count = 0;

                if( _groups2watchedHeads.ContainsKey( res ))
                {
                    IResourceList groupItems = NntpPlugin.CollectArticles( res, false );
                    groupItems = groupItems.Intersect( _unreadArticles );

                    List<IResource> heads = _groups2watchedHeads[ res ];
                    foreach( IResource head in heads )
                    {
                        IResourceList thread = ConversationBuilder.UnrollConversationFromCurrent( head );
                        count += thread.Intersect( groupItems ).Count;
                    }
                }

                if( count != 0 )
                {
                    nodeText.Append( " !", _watchedTextStyle );
                }
                return count != 0;
            }
            return false;
        }

        public string DecorationKey
        {
            get { return _cKey; }
        }

        private void _unreadArticles_Updated( object sender, ResourceIndexEventArgs e )
        {
            if( DecorationChanged != null )
            {
                foreach( IResource head in _heads )
                {
                    if( ConversationBuilder.AreLinked( head, e.Resource ) )
                    {
                       DecorationChanged( this, new ResourceEventArgs( e.Resource ) );
                    }
                }
            }
        }

        private void RuleConditionsChanged(object sender, ResourcePropIndexEventArgs e)
        {
            HeadsChanged();
            NotifyGroups();
        }

        private void RuleConditionsListChanged( object sender, ResourceIndexEventArgs e )
        {
            HeadsChanged();
            NotifyGroups();
        }

        private void HeadsChanged()
        {
            _groups2watchedHeads.Clear();

           _heads = RecollectThreadHeads();
           foreach( IResource head in _heads )
            {
                IResourceList groups = head.GetLinksOfType( NntpPlugin._newsGroup, NntpPlugin._propTo );
                foreach( IResource group in groups )
                {
                    List<IResource> heads;
                    if( !_groups2watchedHeads.TryGetValue( group, out heads ) )
                    {
                        heads = new List<IResource>();
                        _groups2watchedHeads.Add( group, heads );
                    }

                    heads.Add( head );
                }
            }
        }

        private IResourceList RecollectThreadHeads()
        {
            IResource template = Core.FilterRegistry.Std.MessageIsInThreadOfX;
            IResourceList heads = Core.ResourceStore.EmptyResourceList;
            IResourceList conditions = Core.ResourceStore.EmptyResourceList;

            foreach( IResource rule in _formattingRules )
            {
                conditions = conditions.Union( Core.FilterRegistry.GetConditionsPlain( rule ) );
            }
            conditions = conditions.Intersect( Core.FilterRegistry.GetLinkedConditions( template ) );

            foreach( IResource condition in conditions )
            {
                IResourceList conditionHeads = condition.GetLinksOfType( NntpPlugin._newsArticle,
                                                                         Core.FilterRegistry.Props.SetValueLink );
                heads = heads.Union( conditionHeads );
            }

            return heads;
        }

        private void NotifyGroups()
        {
            if( DecorationChanged != null )
            {
                foreach( IResource group in _groups2watchedHeads.Keys )
                   DecorationChanged( this, new ResourceEventArgs( group ) );
            }
        }
   }
}
