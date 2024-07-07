// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Nntp
{
    /// <summary>
    /// Describes a node of news resource tree
    /// </summary>
    internal class NewsTreeNode
    {
        public NewsTreeNode( IResource node )
        {
            _node = node;
        }

        public IResource Resource
        {
            get { return _node; }
        }

        public string DisplayName
        {
            get { return Resource.DisplayName; }
            set { Resource.DisplayName = value; }
        }

        /// <summary>
        /// <remarks>This property is shared by resources of different types with different semantics:
        ///  - for NewsServer, it means display name of user as it's set in the From: field of outgoing msgs
        ///  - for NewsGroup, it means user defined display name of a newsgroup
        /// </remarks>
        /// </summary>
        public string UserDisplayName
        {
            get
            {
                return Resource.GetPropText(
                    NntpPlugin._propUserDisplayName ).Replace( " (unsubscribed)", string.Empty );
            }
            set
            {
                if( value != null && value.Length > 0 )
                {
                    Resource.SetProp( NntpPlugin._propUserDisplayName, value );
                }
                else
                {
                    Resource.DeleteProp( NntpPlugin._propUserDisplayName );
                }
            }
        }

        public IResource Parent
        {
            get
            {
                return _node.GetLinkProp( Core.Props.Parent );
            }
            set
            {
                _node.SetProp( Core.Props.Parent, value );
            }
        }

        public IResourceList Children
        {
            get
            {
                return _node.GetLinksTo( null, Core.Props.Parent );
            }
        }

        public IResourceList AllSubNodes
        {
            get
            {
                IResourceList children = Children;
                IResourceList result = children;
                foreach( IResource child in children )
                {
                    result = result.Union( new NewsTreeNode( child ).AllSubNodes );
                }
                return result;
            }
        }

        /// <summary>
        /// <remarks>All the groups in the tree under the tree node</remarks>
        /// </summary>
        public IResourceList Groups
        {
            get
            {
                IntArrayList groupIds = new IntArrayList();
                GetGroupIds( Resource, groupIds );
                return Core.ResourceStore.ListFromIds( groupIds, false );
            }
        }

        private void GetGroupIds( IResource rootNode, IntArrayList groupIds )
        {
            foreach( IResource child in new NewsTreeNode( rootNode ).Children.ValidResources )
            {
                if( child.Type == NntpPlugin._newsGroup )
                {
                    groupIds.Add( child.Id );
                }
                else
                {
                    GetGroupIds( child, groupIds );
                }
            }
        }

        private IResource   _node;
    }

    /// <summary>
    /// Describes behaviour and controls actions with news folders
    /// </summary>
    internal class NewsFolders
    {
        /**
         * IResource article can be null, in that case the new article is created
         */
        public static IResource PlaceArticle( IResource article, IResource folder, IResourceList groups,
            string from, string subject, string text, string charset, string references,
            string nntpText, IResourceList attachments )
        {
            IResourceStore store = Core.ResourceStore;
            if( !store.IsOwnerThread() )
            {
                IAsyncProcessor resourceProcessor = Core.ResourceAP;
                article = (IResource) resourceProcessor.RunUniqueJob(
                        new PlaceArticleDelegate( PlaceArticle ), article, folder, groups, from,
                            subject, text, charset, references, nntpText, attachments );
            }
            else
            {
                if( article == null || article.IsDeleted )
                {
                    article = store.BeginNewResource( NntpPlugin._newsLocalArticle );
                }
                else
                {
                    article.BeginUpdate();
                    article.DeleteProp( NntpPlugin._propArticleId );
                }
                try
                {
                    article.SetProp( Core.Props.Subject, subject );
                    article.SetProp( Core.Props.Date, DateTime.Now );
                    IContact sender;
                    NewsArticleParser.ParseFrom( article, from, out sender );
                    NewsArticleParser.ParseReferences( article, references );
                    article.SetProp( Core.Props.LongBody, text );
                    article.SetProp( Core.FileResourceManager.PropCharset, charset );
                    article.SetProp( NntpPlugin._propNntpText, nntpText );
                    if( !folder.IsDeleted )
                    {
                        article.SetProp( NntpPlugin._propTo, folder );
                    }
                    if( groups != null )
                    {
                        foreach( IResource group in groups )
                        {
                            if( !group.IsDeleted )
                            {
                                article.AddLink( NntpPlugin._propTo, group );
                            }
                        }
                    }
                    if( attachments != null )
                    {
                        foreach( IResource attachment in attachments )
                        {
                            if( !attachment.IsDeleted )
                            {
                                attachment.AddLink( NntpPlugin._propAttachment, article );
                                string actualResourceType =
                                    Core.FileResourceManager.GetResourceTypeByExtension(
                                    IOTools.GetExtension( attachment.GetPropText( Core.Props.Name ) ) );
                                if( actualResourceType != null && attachment.Type != actualResourceType )
                                {
                                    attachment.ChangeType( actualResourceType );
                                }
                                if( attachment.IsTransient )
                                {
                                    attachment.EndUpdate();
                                }
                            }
                        }
                    }
                    Core.WorkspaceManager.AddToActiveWorkspace( article );
                }
                finally
                {
                    article.EndUpdate();
                    Core.TextIndexManager.QueryIndexing( article.Id );
                }
            }
            return article;
        }

        public static void PlaceResourceToFolder( IResource res, IResource folder )
        {
            IResourceStore store = Core.ResourceStore;
            if( !store.IsOwnerThread() )
            {
                Core.ResourceAP.QueueJob( JobPriority.Immediate,
                    new PlaceResourceToFolderDelegate( PlaceResourceToFolder ), res, folder );
            }
            else
            {
                if( !res.IsDeleted )
                {
                    IResourceList folders = res.GetLinksFrom( NntpPlugin._newsFolder, NntpPlugin._propTo );
                    foreach( IResource oldfolder in folders )
                    {
                        res.DeleteLink( NntpPlugin._propTo, oldfolder );
                    }
                    res.AddLink( NntpPlugin._propTo, folder );
                    if( folder == SentItems )
                    {
                        res.DeleteProp( NntpPlugin._propNntpText );
                    }
                }
            }
        }

        public static bool IsInFolder( IResource res, IResource folder )
        {
            return res.HasLink( NntpPlugin._propTo, folder );
        }

        public static IResource Drafts
        {
            get
            {
                if( _drafts != null && !_drafts.IsDeleted )
                {
                    return _drafts;
                }
                return ( _drafts = GetNewsFolderResource( Root, "Drafts", true ) );
            }
        }

        public static IResource Outbox
        {
            get
            {
                if( _outbox != null && !_outbox.IsDeleted )
                {
                    return _outbox;
                }
                return ( _outbox = GetNewsFolderResource( Root, "Outbox", true ) );
            }
        }

        public static IResource SentItems
        {
            get
            {
                if( _sentItems != null && !_sentItems.IsDeleted )
                {
                    return _sentItems;
                }
                return ( _sentItems = GetNewsFolderResource( Root, "Sent Items", true ) );
            }
        }

        /// <summary>
        /// Return <c>true</c> if a given resource represents one of three standard news folders:
        /// "Drafts", "Outbox" or "Sent Items"
        /// </summary>
        public static bool IsDefaultFolder( IResource folder )
        {
            return IsDrafts( folder ) || IsOutbox( folder ) || IsSentItems( folder );
        }

        public static bool IsDrafts( IResource folder )
        {
            if( _drafts == null )
            {
                _drafts = GetNewsFolderResource( Root, "Drafts", false );
            }
            return folder == _drafts;
        }

        public static bool IsOutbox( IResource folder )
        {
            if( _outbox == null )
            {
                _outbox = GetNewsFolderResource( Root, "Outbox", false );
            }
            return folder == _outbox;
        }

        public static bool IsSentItems( IResource folder )
        {
            if( _sentItems == null )
            {
                _sentItems = GetNewsFolderResource( Root, "Sent Items", false );
            }
            return folder == _sentItems;
        }

        public static IResource Root
        {
            get
            {
                if( _newsgroupsRoot == null )
                {
                    _newsgroupsRoot = Core.ResourceTreeManager.GetRootForType( NntpPlugin._newsGroup );
                }
                return _newsgroupsRoot;
            }
        }

        public static void AddToRoot( IResource child )
        {
            new NewsTreeNode( child ).Parent = Root;
        }

        public static IResource GetNewsFolderResource( IResource parent, string name, bool create )
        {
            IResourceStore store = Core.ResourceStore;
            IResourceList folders = new NewsTreeNode( parent ).Children;
            folders = folders.Intersect(
                store.FindResources( NntpPlugin._newsFolder, Core.Props.Name, name ), true );
            IResource folder = null;
            if( folders.Count > 0 )
            {
                folder = folders[ 0 ];
            }
            else
            {
                if( create )
                {
                    if( !store.IsOwnerThread() )
                    {
                        folder = (IResource) Core.ResourceAP.RunUniqueJob(
                            new GetNewsFolderResourceDelegate( GetNewsFolderResource ), parent, name, true );
                    }
                    else
                    {
                        folder = store.BeginNewResource( NntpPlugin._newsFolder );
                        try
                        {
                            folder.SetProp( Core.Props.Name, name );
                            new NewsTreeNode( folder ).Parent = parent;
                            if ( parent == _newsgroupsRoot )
                            {
                                folder.SetProp( "VisibleInAllWorkspaces", true );
                                folder.SetProp( NntpPlugin._propNewsSortOrder, Int32.MaxValue );
                            }
                            folder.SetProp( Core.Props.DisplayThreaded, parent.GetProp( Core.Props.DisplayThreaded ) );
                        }
                        finally
                        {
                            folder.EndUpdate();
                        }
                    }
                }
            }
            return folder;
        }

        private delegate IResource PlaceArticleDelegate(
            IResource article, IResource folder, IResourceList groups, string from, string subject,
            string text, string charset, string references, string nntpText, IResourceList attachments );

        private delegate void PlaceResourceToFolderDelegate( IResource resource, IResource folder );

        private delegate IResource GetNewsFolderResourceDelegate( IResource parent, string name, bool create );

        private static IResource _newsgroupsRoot = null;
        private static IResource _drafts = null;
        private static IResource _outbox = null;
        private static IResource _sentItems = null;
    }
}
