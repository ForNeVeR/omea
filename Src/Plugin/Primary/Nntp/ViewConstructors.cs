// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.DataStructures;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.Nntp;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Nntp
{
    public class NewsViewsConstructor : IViewsConstructor
    {
        public const string   AuthorPostedArticleName = "Author posted a news article";
        public const string   AuthorPostedArticleDeep = "postedarticle";
        public const string   AppearedInNewsgroupName = "Appeared in the %specified% newsgroup(s)";
        public const string   AppearedInNewsgroupDeep = "innewsgroup";
        public const string   AppearedInThreadName = "Appeared in the %specified% thread(s)";
        public const string   AppearedInThreadDeep = "inthread";
        public const string   PostInMyThreadName = "Post is a reply in my thread";
        public const string   PostInMyThreadDeep = "mythread";
        public const string   PostInFlaggedThreadName = "Post is a reply in a flagged thread";
        public const string   PostInFlaggedThreadDeep = "flaggedthread";

        #region IViewsConstructor Members
        public void RegisterViewsFirstRun()
        {
            IResource res;
            IFilterRegistry fMgr = Core.FilterRegistry;

            //  Conditions/Templates
            IResource myResType = Core.ResourceStore.FindUniqueResource( "ResourceType", "Name", NntpPlugin._newsArticle );
            res = fMgr.CreateStandardCondition( AuthorPostedArticleName, AuthorPostedArticleDeep, new string[]{ "Contact" },
                                                "LinkedResourcesOfType", ConditionOp.In, myResType.ToResourceList() );
            fMgr.AssociateConditionWithGroup( res, "Address and Contact Conditions" );
        }

        public void RegisterViewsEachRun()
        {
            IFilterRegistry fMgr = Core.FilterRegistry;
            INotificationManager notifMgr = Core.NotificationManager;
            string[]  _applTypes = new string[] { NntpPlugin._newsArticle };

            //  Conditions/Templates necessary for notification rules.
            IResource template = fMgr.CreateConditionTemplate( AppearedInNewsgroupName, AppearedInNewsgroupDeep,
                                                            _applTypes, ConditionOp.In, NntpPlugin._newsGroup, "Newsgroups" );
            fMgr.AssociateConditionWithGroup( template, "News Conditions" );

            IResource threadTemplate = fMgr.CreateConditionTemplate( AppearedInThreadName, AppearedInThreadDeep,
                                                                    _applTypes, ConditionOp.In, NntpPlugin._newsArticle, "Reply*" );
            fMgr.AssociateConditionWithGroup( threadTemplate, "News Conditions" );

            IResource res = fMgr.RegisterCustomCondition( PostInMyThreadName, PostInMyThreadDeep, _applTypes, new RepliesToMyPosts() );
            fMgr.AssociateConditionWithGroup( res, "News Conditions" );

            res = fMgr.RegisterCustomCondition( PostInFlaggedThreadName, PostInFlaggedThreadDeep, _applTypes, new RepliesToFlaggedPosts() );
            fMgr.AssociateConditionWithGroup( res, "News Conditions" );

            //  Notifications
            notifMgr.RegisterNotifyMeResourceType( NntpPlugin._newsGroup, NntpPlugin._newsArticle );
            notifMgr.RegisterNotifyMeCondition( NntpPlugin._newsArticle, fMgr.Std.FromContactX, Core.ResourceStore.GetPropId( "From" ) );
            notifMgr.RegisterNotifyMeCondition( NntpPlugin._newsGroup, template, 0 );
            notifMgr.RegisterNotifyMeCondition( NntpPlugin._newsArticle, threadTemplate, 0 );

            Core.FilterEngine.RegisterRuleApplicableResourceType( NntpPlugin._newsArticle );
        }
        #endregion
    }

    public class NewsUpgrade1ViewsConstructor : IViewsConstructor
    {
        #region IViewsConstructor Members
        public void RegisterViewsFirstRun()
        {
            IResource res;
            string[]  _applTypes = new string[] { NntpPlugin._newsArticle };

            //-----------------------------------------------------------------
            //  All conditions, templates and actions must have their deep names
            //-----------------------------------------------------------------
            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, Core.Props.Name, NewsViewsConstructor.AuthorPostedArticleName );
            if( res != null )
                res.SetProp( "DeepName", NewsViewsConstructor.AuthorPostedArticleDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, Core.Props.Name, NewsViewsConstructor.PostInMyThreadName );
            if( res != null )
                res.SetProp( "DeepName", NewsViewsConstructor.PostInMyThreadDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, Core.Props.Name, NewsViewsConstructor.AppearedInNewsgroupName );
            if( res != null )
                res.SetProp( "DeepName", NewsViewsConstructor.AppearedInNewsgroupDeep );

            res = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, Core.Props.Name, NewsViewsConstructor.AppearedInThreadName );
            if( res != null )
                res.SetProp( "DeepName", NewsViewsConstructor.AppearedInThreadDeep );

            //  Tray icon rules.
            Core.TrayIconManager.RegisterTrayIconRule( "Unread News Articles", _applTypes,
                                                       new IResource[] { Core.FilterRegistry.Std.ResourceIsUnread },
                                                       null, NntpPlugin.LoadNewsIcon( "article.ico" ) );
        }

        public void RegisterViewsEachRun()
        {}
        #endregion
    }

    #region Custom Conditions and Actions
    public class RepliesToMyPosts : ICustomCondition
    {
        public bool MatchResource( IResource res )
        {
            if( res.Type == NntpPlugin._newsArticle )
            {
                IntHashSet childs = new IntHashSet();
                do
                {
                    if( childs.Contains( res.Id ) )
                    {
                        break; // cycle found
                    }
                    childs.Add( res.Id );
                    IResource author = res.GetLinkProp( Core.ContactManager.Props.LinkFrom );
                    if( author != null && author.HasProp( "Myself" ) )
                    {
                        return true;
                    }
                    res = res.GetLinkProp( Core.Props.Reply );
                }
                while( res != null );
            }
            return false;
        }

        public IResourceList Filter( string resType )
        {
            return Core.ResourceStore.FindResourcesWithProp( null, "IsSelfThread" );
        }
    }

    public class RepliesToFlaggedPosts : ICustomCondition
    {
        public bool MatchResource( IResource res )
        {
            if( res.Type == NntpPlugin._newsArticle )
            {
                IntHashSet childs = new IntHashSet();
                do
                {
                    if( childs.Contains( res.Id ) )  // cycle found
                        break;

                    childs.Add( res.Id );
                    res = res.GetLinkProp( Core.Props.Reply );
                    if( res != null && res.HasProp( "Flag" ) )
                        return true;
                }
                while( res != null );
            }
            return false;
        }

        public IResourceList Filter( string resType )
        {
            IntHashSet CollectedIds = new IntHashSet();
            IntHashSet resultSet = new IntHashSet();

            IResourceList heads = Core.ResourceStore.FindResourcesWithProp( resType, "Flag" );
            foreach( IResource res in heads )
            {
                CollectedIds.Add( res.Id );
            }

            CollectResources( resType, resultSet, CollectedIds, 1 );

            int[] ids = new int[ resultSet.Count ];
            int count = 0;
            foreach( IntHashSet.Entry e in resultSet )
            {
                ids[ count++ ] = e.Key;
            }

            return Core.ResourceStore.ListFromIds( ids, false ).Union( heads, true );
        }

        private static void  CollectResources( string resType, IntHashSet result, IntHashSet source, int level )
        {
            IntHashSet temp = new IntHashSet();
            foreach( IntHashSet.Entry e in source )
            {
                IResourceList children = Core.ResourceStore.LoadResource( e.Key ).GetLinksTo( resType, Core.Props.Reply );
                for( int i = 0; i < children.Count; i++ )
                {
                    int chid = children[ i ].Id;
                    if( !source.Contains( chid ) && !result.Contains( chid ) )
                        temp.Add( chid );
                }
            }

            foreach( IntHashSet.Entry e in temp )
                result.Add( e.Key );

            if( temp.Count > 0 )
                CollectResources( resType, result, temp, level + 1 );
        }
    }
    #endregion Custom Conditions and Actions
}
