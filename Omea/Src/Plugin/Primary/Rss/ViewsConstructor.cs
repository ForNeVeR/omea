// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
    public class RSSDataUpgrade : IViewsConstructor
    {
        public void RegisterViewsFirstRun()
        {
            int           percent = 0;
            const string  Message = "Upgrading RSS Feeds Database";
            IResourceList allItems = Core.ResourceStore.GetAllResources( Props.RSSItemResource );

            if( Core.ProgressWindow != null )
                Core.ProgressWindow.UpdateProgress( 0, Message, null );

            for( int i = 0; i < allItems.Count; i++ )
            {
                allItems[ i ].DeleteProp( Props.Author );

                if( Core.ProgressWindow != null )
                {
                    int newPercent = i * 100 / allItems.Count;
                    if( newPercent != percent )
                    {
                        percent = newPercent;
                        if( Core.ProgressWindow != null )
                            Core.ProgressWindow.UpdateProgress( percent, Message, null );
                    }
                }
            }
            if( Core.ProgressWindow != null )
                Core.ProgressWindow.UpdateProgress( 100, Message, null );
        }
        public void RegisterViewsEachRun()
        {}
    }

    public class RSSDataUpgrade2 : IViewsConstructor
    {
        public void RegisterViewsFirstRun()
        {
            int           percent = 0;
            const string  Message = "Upgrading RSS Feeds Links";
            IResourceList allItems = Core.ResourceStore.FindResourcesWithProp( Props.RSSItemResource, Core.Props.Reply );

            if( Core.ProgressWindow != null )
                Core.ProgressWindow.UpdateProgress( 0, Message, null );

            //  Substitute "Reply" links between feed items with a more semantic-neutral
            //  "LinkedPost" link. This also removes "Reply" link from the pointless
            //  Link Pane and adds necessary anchors directly into the feed post
            //  representation.
            for( int i = 0; i < allItems.Count; i++ )
            {
                IResourceList linked = allItems[ i ].GetLinksFrom( Props.RSSItemResource, Core.Props.Reply );

                allItems[ i ].DeleteLinks( Core.Props.Reply );
                foreach( IResource res in linked )
                    allItems[ i ].AddLink( Props.LinkedPost, res );

                if( Core.ProgressWindow != null )
                {
                    int newPercent = i * 100 / allItems.Count;
                    if( newPercent != percent )
                    {
                        percent = newPercent;
                        if( Core.ProgressWindow != null )
                            Core.ProgressWindow.UpdateProgress( percent, Message, null );
                    }
                }
            }

            if( Core.ProgressWindow != null )
                Core.ProgressWindow.UpdateProgress( 100, Message, null );
        }
        public void RegisterViewsEachRun()
        {}
    }

    public class RSSViewsConstructor : IViewsConstructor
    {
        public const string  RSSConditionsGroup = "RSS Conditions";

        public const string  AuthorWrotePostName = "Author wrote an rss post";
        public const string  AuthorWrotePostDeep = "wrotepost";
        public const string  AuthorHasFeedName = "Author has an rss feed";
        public const string  AuthorHasFeedDeep = "hasfeed";
        public const string  PostHasEnclosuredName = "Post has enclosure";
        public const string  PostHasEnclosuredDeep = "hasenclosure";
        public const string  PostHasCommentName = "Post has comment(s)";
        public const string  PostHasCommentDeep = "hascomment";
        public const string  PostIsACommentName = "Post is a comment";
        public const string  PostIsACommentDeep = "postiscomment";
        public const string  PostIsAuthorsCommentName = "Post is a comment from blog author";
        public const string  PostIsAuthorsCommentDeep = "postisauthorcomment";

        public const string  DownloadFailedName = "Enclosure downloading is failed";
        public const string  DownloadFailedDeep = "encfailed";
        public const string  DownloadCompletedName = "Enclosure downloading is completed";
        public const string  DownloadCompletedDeep = "enccompleted";
        public const string  DownloadNotName = "Enclosure is not downloaded";
        public const string  DownloadNotDeep = "notdownloaded";
        public const string  DownloadPlannedName = "Enclosure downloading is planned";
        public const string  DownloadPlannedDeep = "encplanned";

        public const string  PostInFeedName = "Post is in %feed%";
        public const string  PostInFeedDeep = "postinfeed";
        public const string  FeedInFolderName = "Post's feed is in the %folder%";
        public const string  FeedInFolderDeep = "FeedInFolder";
        public const string  PostInSearchFeedName = "Post is in the search feed";
        public const string  PostInSearchFeedDeep = "postinsearchfeed";
        public const string  PostInCategoryName = "Post in %publisher's category%";
        public const string  PostInCategoryDeep = "postincategory";
        public const string  EnclosureSizeName = "Enclosure size is %in range% (in bytes)";
        public const string  EnclosureSizeDeep = "encsize";
        public const string  EnclosureTypeName = "Enclosure is of %specified% type";
        public const string  EnclosureTypeDeep = "enctype";

        public const string  DownloadEnclosureName = "Download enclosures";
        public const string  DownloadEnclosureDeep = "downloadenclosure";
        public const string  DownloadEnclosureToName = "Download enclosures to %folder%";
        public const string  DownloadEnclosureToDeep = "downloadenclosureto";

        #region IViewsConstructor Members
        /// <summary>
        /// Method is called when a plugin that implements this interface is loaded first time.
        /// </summary>
        public void  RegisterViewsFirstRun()
        {
            IResource       res;
            string[]        applType = new string[] { "RSSItem" }, contactType = new string[] {"Contact"};
            IFilterRegistry  fMgr = Core.FilterRegistry;
            IResource       myResType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", "RSSItem" );

            res = fMgr.CreateStandardCondition( AuthorWrotePostName, AuthorWrotePostDeep, contactType,
                                                "LinkedResourcesOfType", ConditionOp.In, myResType.ToResourceList() );
            fMgr.AssociateConditionWithGroup( res, "Address and Contact Conditions" );

            res = fMgr.CreateStandardCondition( AuthorHasFeedName, AuthorHasFeedDeep, contactType, "Weblog", ConditionOp.HasLink );
            fMgr.AssociateConditionWithGroup( res, "Address and Contact Conditions" );

            res = fMgr.CreateStandardCondition( PostHasEnclosuredName, PostHasEnclosuredDeep, applType, "EnclosureURL", ConditionOp.HasProp );
            fMgr.AssociateConditionWithGroup( res, RSSConditionsGroup );

            res = fMgr.CreateStandardCondition( PostHasCommentName, PostHasCommentDeep, applType, "CommentCount", ConditionOp.Gt, "0" );
            fMgr.AssociateConditionWithGroup( res, RSSConditionsGroup );

            res = fMgr.CreateStandardCondition( PostIsACommentName, PostIsACommentDeep, applType, "FeedComment", ConditionOp.HasLink );
            fMgr.AssociateConditionWithGroup( res, RSSConditionsGroup );

            res = fMgr.CreateStandardCondition( DownloadFailedName, DownloadFailedDeep, applType,
                                                "EnclosureDownloadingState", ConditionOp.In, "3" );
            fMgr.RenameCondition( "Enclosure downloading failed", "Enclosure downloading is failed" );
            fMgr.AssociateConditionWithGroup( res, RSSConditionsGroup );

            res = fMgr.CreateStandardCondition( DownloadCompletedName, DownloadCompletedDeep, applType,
                                                "EnclosureDownloadingState", ConditionOp.In, "2" );
            fMgr.RenameCondition( "Enclosure downloading completed", "Enclosure downloading is completed" );
            fMgr.AssociateConditionWithGroup( res, RSSConditionsGroup );

            res = fMgr.CreateStandardCondition( DownloadNotName, DownloadNotDeep, applType,
                                                "EnclosureDownloadingState", ConditionOp.In, "0" );
            fMgr.RenameCondition( "Enclosure not downloaded", "Enclosure is not downloaded" );
            fMgr.AssociateConditionWithGroup( res, RSSConditionsGroup );

            res = fMgr.CreateStandardCondition( DownloadPlannedName, DownloadPlannedDeep, applType,
                                                "EnclosureDownloadingState", ConditionOp.In, "1", "4" );
            fMgr.RenameCondition( "Enclosure downloading planned", "Enclosure downloading is planned" );
            fMgr.AssociateConditionWithGroup( res, RSSConditionsGroup );

            res = fMgr.CreateConditionTemplate( EnclosureSizeName, EnclosureSizeDeep, applType,
                                                ConditionOp.InRange, "EnclosureSize", "0", Int32.MaxValue.ToString() );
            fMgr.AssociateConditionWithGroup( res, RSSConditionsGroup );

            res = fMgr.CreateConditionTemplate( EnclosureTypeName, EnclosureTypeDeep, applType, ConditionOp.Eq, "EnclosureType" );
            fMgr.AssociateConditionWithGroup( res, RSSConditionsGroup );
        }

        /// <summary>
        /// Method is called when a plugin that implements this interface is loaded.
        /// Usually this method contains code which creates rule actions and performs
        /// corrections to the resources created during the first start of the plugin.
        /// </summary>
        public void RegisterViewsEachRun()
        {
            string[]        applType = new string[] { "RSSItem" };
            IFilterRegistry  fMgr = Core.FilterRegistry;
            INotificationManager notifMgr = Core.NotificationManager;

            notifMgr.RegisterNotifyMeResourceType( "RSSFeed", "RSSItem" );
            notifMgr.RegisterNotifyMeResourceType( "RSSItem", "RSSItem" );

            //  Conditions/Templates
            IResource feedCondition = fMgr.CreateConditionTemplate( PostInFeedName, PostInFeedDeep, applType, ConditionOp.In, "RSSFeed", "RSSItem" );
            fMgr.AssociateConditionWithGroup( feedCondition, RSSConditionsGroup );
            notifMgr.RegisterNotifyMeCondition( "RSSFeed", feedCondition, 0 );
            notifMgr.RegisterNotifyMeCondition( "RSSItem", feedCondition, -Props.RSSItem );

            feedCondition = fMgr.RegisterCustomCondition( PostInSearchFeedName, PostInSearchFeedDeep, applType, new PostInSearchFeedCondition() );
            fMgr.AssociateConditionWithGroup( feedCondition, RSSConditionsGroup );

            feedCondition = fMgr.RegisterCustomCondition( PostIsAuthorsCommentName, PostIsAuthorsCommentDeep, applType, new PostIsAuthorsCommentCondition() );
            fMgr.AssociateConditionWithGroup( feedCondition, RSSConditionsGroup );

            //  Rule Actions
            Core.FilterEngine.RegisterRuleApplicableResourceType( "RSSItem" );
            fMgr.RegisterRuleAction( DownloadEnclosureName, DownloadEnclosureDeep, new EnclosureDownloadRuleAction(), applType );

            fMgr.RegisterRuleActionTemplate( DownloadEnclosureToName, DownloadEnclosureToDeep, new EnclosureDownloadToDirRuleActionTemplate(),
                                             applType, ConditionOp.In, "ExternalDir" );
        }
        #endregion
    }

    public class RSSUgrade1ViewsConstructor : IViewsConstructor
    {
        #region IViewsConstructor Members
        public void RegisterViewsFirstRun()
        {
            IResource       res;
            string[]        applType = new string[] { "RSSItem" };
            IFilterRegistry  fMgr = Core.FilterRegistry;

            //-----------------------------------------------------------------
            //  All conditions, templates and actions must have their deep names
            //-----------------------------------------------------------------
            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "Name", RSSViewsConstructor.AuthorWrotePostName );
            if( res != null )
                res.SetProp( "DeepName", RSSViewsConstructor.AuthorWrotePostDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "Name", RSSViewsConstructor.AuthorHasFeedName );
            if( res != null )
                res.SetProp( "DeepName", RSSViewsConstructor.AuthorHasFeedDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "Name", RSSViewsConstructor.PostHasEnclosuredName );
            if( res != null )
                res.SetProp( "DeepName", RSSViewsConstructor.PostHasEnclosuredDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "Name", RSSViewsConstructor.PostHasCommentName );
            if( res != null )
                res.SetProp( "DeepName", RSSViewsConstructor.PostHasCommentDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "Name", RSSViewsConstructor.DownloadFailedName );
            if( res != null )
                res.SetProp( "DeepName", RSSViewsConstructor.DownloadFailedDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "Name", RSSViewsConstructor.DownloadCompletedName );
            if( res != null )
                res.SetProp( "DeepName", RSSViewsConstructor.DownloadCompletedDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "Name", RSSViewsConstructor.DownloadNotName );
            if( res != null )
                res.SetProp( "DeepName", RSSViewsConstructor.DownloadNotDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "Name", RSSViewsConstructor.DownloadPlannedName );
            if( res != null )
                res.SetProp( "DeepName", RSSViewsConstructor.DownloadPlannedDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name", RSSViewsConstructor.PostInFeedName );
            if( res != null )
                res.SetProp( "DeepName", RSSViewsConstructor.PostInFeedDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name", RSSViewsConstructor.PostInCategoryName );
            if( res != null )
                res.SetProp( "DeepName", RSSViewsConstructor.PostInCategoryDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name", RSSViewsConstructor.EnclosureSizeName );
            if( res != null )
                res.SetProp( "DeepName", RSSViewsConstructor.EnclosureSizeDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "Name", RSSViewsConstructor.EnclosureTypeName );
            if( res != null )
                res.SetProp( "DeepName", RSSViewsConstructor.EnclosureTypeDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleActionResName, "Name", RSSViewsConstructor.DownloadEnclosureName );
            if( res != null )
                res.SetProp( "DeepName", RSSViewsConstructor.DownloadEnclosureDeep );

            //  Tray Icon Rules and Notifications
            Core.TrayIconManager.RegisterTrayIconRule( "Unread RSS/ATOM Posts", applType, new IResource[] { fMgr.Std.ResourceIsUnread },
                                                       null, RSSPlugin.LoadIconFromAssembly( "RSSItemUnread.ico" ) );
        }

        public void RegisterViewsEachRun()
        {}
        #endregion
    }

    public class RSSUgrade2ViewsConstructor : IViewsConstructor
    {
        public void RegisterViewsFirstRun()
        {
            IResourceList list = Core.ResourceStore.FindResources( FilterManagerProps.ConditionTemplateResName, "Name", RSSViewsConstructor.PostInCategoryName );
            IResource one = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "DeepName", RSSViewsConstructor.PostInCategoryName );
            if( one != null )
            {
                list = list.Minus( one.ToResourceList() );
                list.DeleteAll();
                one.SetProp( "DeepName", RSSViewsConstructor.PostInCategoryDeep );
            }
            else
            {
                IFilterRegistry mgr = Core.FilterRegistry;
                IResource res = mgr.CreateConditionTemplate( RSSViewsConstructor.PostInCategoryName, RSSViewsConstructor.PostInCategoryDeep,
                                                             new string[] {"RSSItem"}, ConditionOp.Eq, "RSSCategory" );
                mgr.AssociateConditionWithGroup( res, RSSViewsConstructor.RSSConditionsGroup );
            }
        }

        public void RegisterViewsEachRun()
        {}
    }

    public class RSSUgrade3ViewsConstructor : IViewsConstructor
    {
        public void RegisterViewsFirstRun()
        {
            IFilterRegistry mgr = Core.FilterRegistry;
            IResource res = mgr.CreateStandardCondition( RSSViewsConstructor.PostIsACommentName, RSSViewsConstructor.PostIsACommentDeep,
                                                         new string[] {"RSSItem"}, "FeedComment", ConditionOp.HasLink );
            mgr.AssociateConditionWithGroup( res, RSSViewsConstructor.RSSConditionsGroup );
        }

        public void RegisterViewsEachRun()
        {
            IFilterRegistry fMgr = Core.FilterRegistry;
            IResource res = fMgr.CreateConditionTemplate( RSSViewsConstructor.FeedInFolderName, RSSViewsConstructor.FeedInFolderDeep,
                                                          null, ConditionOp.In, Props.RSSFeedGroupResource, "RssItem>Parent" );
            fMgr.AssociateConditionWithGroup( res, RSSViewsConstructor.RSSConditionsGroup );
        }
    }

    #region Rule Conditions/Actions
    public class PostInSearchFeedCondition : ICustomCondition
    {
        public bool MatchResource( IResource res )
        {
            IResource feed = res.GetLinksTo( Props.RSSFeedResource, Props.RSSItem )[ 0 ];
            return feed.HasProp( Props.RSSSearchPhrase );
        }

        public IResourceList Filter( string resType )
        {
            IResourceList posts = Core.ResourceStore.EmptyResourceList;
            //  FindResourcesWithProps does not support LongString type for prop,
            //  thus we have to iterate over the whole feeds list.
            foreach( IResource feed in Core.ResourceStore.GetAllResources( Props.RSSFeedResource ) )
            {
                if( feed.HasProp( Props.RSSSearchPhrase ))
                {
                    posts = posts.Union( feed.GetLinksOfType( null, Props.RSSItem ), true );
                }
            }
            return posts;
        }
    }

    public class PostIsAuthorsCommentCondition : ICustomCondition
    {
        public bool MatchResource( IResource res )
        {
            IResource parent = res.GetLinkProp( Props.ItemComment );
            if( parent != null )
            {
                IResource c1 = parent.GetLinkProp( Core.ContactManager.Props.LinkFrom );
                IResource c2 = res.GetLinkProp( Core.ContactManager.Props.LinkFrom );
                return ( c1 != null && c2 != null && c1.Id == c2.Id );
            }
            return false;
        }

        public IResourceList Filter( string resType )
        {
            int          fromId = Core.ContactManager.Props.LinkFrom;
            IntArrayList list = new IntArrayList();

            IResourceList comments = Core.ResourceStore.FindResourcesWithProp( Props.RSSFeedResource, Props.ItemComment );
            IResourceList authors = Core.ResourceStore.FindResourcesWithProp( "Contact", Props.Weblog );

            foreach( IResource comment in comments )
            {
                IResource contact = comment.GetLinkProp( fromId );
                if( contact != null && authors.Contains( contact ) )
                {
                    list.Add( comment.Id );
                }
            }

            return Core.ResourceStore.ListFromIds( list, false );
        }
    }

    public class EnclosureDownloadRuleAction : IRuleAction
    {
        public void Exec( IResource res, IActionParameterStore actionStore )
        {
            Guard.NullArgument( res, "res" );
            Guard.NullArgument( actionStore, "actionStore" );
            if ( res.Type != "RSSItem" )
            {
                throw new ArgumentException( "EnclosureDownloadRuleAction was registered for RSSItem only but there resource with type = '" + res.Type + "'" );
            }
            Tracer._Trace( "Execute rule: EnclosureDownloadRuleAction" );

            if ( res.HasProp( Props.EnclosureURL ) )
            {
                EnclosureDownloadManager.PlanToDownload( res );
            }
        }
    }

    public class EnclosureDownloadToDirRuleActionTemplate : IRuleAction
    {
        public void Exec( IResource res, IActionParameterStore actionStore )
        {
            Guard.NullArgument( res, "res" );
            Guard.NullArgument( actionStore, "actionStore" );
            if ( res.Type != "RSSItem" )
            {
                throw new ArgumentException( "EnclosureDownloadRuleAction was registered for RSSItem only but there resource with type = '" + res.Type + "'" );
            }
            Tracer._Trace( "Execute rule: EnclosureDownloadRuleAction" );

            if ( res.HasProp( Props.EnclosureURL ) )
            {
                string folder = actionStore.ParameterAsString();
                EnclosureDownloadManager.PlanToDownload( res, folder );
            }
        }
    }
    #endregion Rule Conditions/Actions
}
