// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using JetBrains.Omea.Base;
using JetBrains.Omea.Contacts;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.Plugins;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea
{
    public class ViewsInitializer : IViewsConstructor
    {
        #region Invokation
        public static void InitViewsConstructors()
        {
            IResourceList regIniters = Core.ResourceStore.GetAllResources( FilterManagerProps.RegisteredInitializer );
            regIniters.DeleteAll();
            InvokeViewsConstructors();
        }
        public static void InvokeViewsConstructors()
        {
            IResourceStore store = Core.ResourceStore;

            #region Invokation Mode Detection
            ArrayList initializers = Core.PluginLoader.GetViewsConstructors();
            IViewsConstructor firstChecker = new Upgrade0ViewsInitializer();
            initializers.Insert( 0, new Upgrade1ViewsInitializer() );
            initializers.Insert( 0, new ViewsInitializer() );
            initializers.Insert( 0, firstChecker );
            initializers.Add( new Upgrade2ViewsInitializer() );
            initializers.Add( new Upgrade3ViewsInitializer() );

            //-----------------------------------------------------------------
            //  NB: Name of a main initializer - JetBrains.Omea.ViewsInitializer
            //      is already both in EAP versions and in <= 469 builds
            //      (that is 1.04 and older versions). But they significantly
            //      differ from each other. To provide the consistency of
            //      declarations, all transitions to Omea 1.5 are made forcedly
            //      via the check of the presence of the name of the first upgrade
            //      initializer.
            //-----------------------------------------------------------------
            string    checkerName = firstChecker.GetType().FullName;
            IResource checkerMark = store.FindUniqueResource( FilterManagerProps.RegisteredInitializer, "Name", checkerName );
            bool      isStoreInit = (store.GetAllResources( FilterManagerProps.RegisteredInitializer ).Count == 0);

            if( isStoreInit )
                Trace.WriteLine( "ViewsInitializer at Invoke -- Store initialization detected." );
            else
                Trace.WriteLine( "ViewsInitializer at Invoke -- Running on the existing store." );
            if( checkerMark == null )
                Trace.WriteLine( "ViewsInitializer at Invoke -- [" + checkerName + "] initializer is absent - running full cycle." );

            isStoreInit = isStoreInit || (checkerMark == null);
            #endregion Invokation Mode Detection

            foreach( IViewsConstructor res in initializers )
            {
                string      viewInitName = res.GetType().FullName;
                IResource   initerAttr = store.FindUniqueResource( FilterManagerProps.RegisteredInitializer, "Name", viewInitName );
                bool        initerRegistered = (initerAttr != null);

                //  Call FirstRun if:
                //  - it is really first run for the application
                //  - if plugin is not registered yet in the list - that means that the
                //    separate plugin is installed in the system after the installation.
                //    NB: special case is when we run first time over the existing
                //        database - no registered attribute exist for any plugin.
                try
                {
                    if( isStoreInit || !initerRegistered )
                    {
                        Trace.WriteLine( "ViewsInitializer - [" + viewInitName + "] started FirstRun initialization" );
                        res.RegisterViewsFirstRun();
                    }
                    Trace.WriteLine( "ViewsInitializer - [" + viewInitName + "] started EachRun initialization" );
                    res.RegisterViewsEachRun();
                }
                catch( PluginLoader.CancelStartupException )
                {
                    throw;
                }
                catch( Exception ex )
                {
                    Core.ReportException( ex, false );
                    continue;
                }
                if( !initerRegistered )
                {
                    IResource r = store.BeginNewResource( FilterManagerProps.RegisteredInitializer );
                    r.SetProp( "Name", viewInitName );
                    r.EndUpdate();
                }
            }
        }
        #endregion Invokation

        #region FirstRun
        public void  RegisterViewsFirstRun()
        {
            //-----------------------------------------------------------------
            IFilterRegistry fMgr = Core.FilterRegistry;
            IStandardConditions std = fMgr.Std;

            //-----------------------------------------------------------------
            //  Standard atomic conditions
            //-----------------------------------------------------------------
            IResource unr = fMgr.CreateStandardCondition( std.ResourceIsUnreadName,  std.ResourceIsUnreadNameDeep, null, "IsUnread", ConditionOp.In, "true" );
            IResource fla = fMgr.CreateStandardCondition( std.ResourceIsFlaggedName, std.ResourceIsFlaggedNameDeep, null, "Flag", ConditionOp.HasLink );
            IResource ann = fMgr.CreateStandardCondition( std.ResourceIsAnnotatedName, std.ResourceIsAnnotatedNameDeep, null, "Annotation", ConditionOp.HasProp );
            IResource cli = fMgr.CreateStandardCondition( std.ResourceIsAClippingName, std.ResourceIsAClippingNameDeep, null, "IsClippingFakeProp", ConditionOp.HasProp );
            IResource del = fMgr.CreateStandardCondition( std.ResourceIsDeletedName, std.ResourceIsDeletedNameDeep, null, "IsDeleted", ConditionOp.HasProp );
            IResource foo = fMgr.CreateStandardCondition( FilterManagerStandards.DummyConditionName, FilterManagerStandards.DummyConditionName, null, "Id", ConditionOp.Gt, "0" );
            foo.SetProp( "Invisible", true );

            //-----------------------------------------------------------------
            //  Standard condition templates
            //-----------------------------------------------------------------
            fMgr.CreateConditionTemplate( std.ResourceIsFlaggedWithFlagXName, std.ResourceIsFlaggedWithFlagXNameDeep, null, ConditionOp.In, "Flag", "Flag" );
            fMgr.CreateConditionTemplate( std.SizeIsInTheIntervalXName, std.SizeIsInTheIntervalXNameDeep, null, ConditionOp.InRange, "Size", "0", Int32.MaxValue.ToString() );
            fMgr.CreateConditionTemplate( std.ResourceBelongsToWorkspaceXName, std.ResourceBelongsToWorkspaceXNameDeep, null, ConditionOp.In, "Workspace", "WorkspaceVisible" );

            IResource res;
            res = fMgr.CreateConditionTemplate( std.BodyMatchesSearchQueryXName, std.BodyMatchesSearchQueryXNameDeep, null, ConditionOp.QueryMatch );
            fMgr.AssociateConditionWithGroup( res, "Text Query Conditions" );
            res = fMgr.CreateConditionTemplate( std.SubjectMatchSearchQueryXName, std.SubjectMatchSearchQueryXNameDeep, null, ConditionOp.QueryMatch, DocumentSection.SubjectSection );
            fMgr.AssociateConditionWithGroup( res, "Text Query Conditions" );
            res = fMgr.CreateConditionTemplate( std.SourceMatchSearchQueryXName, std.SourceMatchSearchQueryXNameDeep, null, ConditionOp.QueryMatch, DocumentSection.SourceSection );
            fMgr.AssociateConditionWithGroup( res, "Text Query Conditions" );
            res = fMgr.CreateConditionTemplate( std.SubjectIsTextXName, std.SubjectIsTextXNameDeep, null, ConditionOp.Eq, "Subject" );
            fMgr.AssociateConditionWithGroup( res, "Text Query Conditions" );
            res = fMgr.CreateConditionTemplate( std.SubjectContainsTextXName, std.SubjectContainsTextXNameDeep, null, ConditionOp.Has, "Subject" );
            fMgr.AssociateConditionWithGroup( res, "Text Query Conditions" );

            res = fMgr.CreateConditionTemplate( std.FromContactXName, std.FromContactXNameDeep, null, ConditionOp.In, "Contact", "From" );
            fMgr.AssociateConditionWithGroup( res, "Address and Contact Conditions" );
            res = fMgr.CreateConditionTemplate( std.ToContactXName, std.ToContactXNameDeep, null, ConditionOp.In, "Contact", "To" );
            fMgr.AssociateConditionWithGroup( res, "Address and Contact Conditions" );
            res = fMgr.CreateConditionTemplate( std.CCContactXName, std.CCContactXNameDeep, null, ConditionOp.In, "Contact", "CC" );
            fMgr.AssociateConditionWithGroup( res, "Address and Contact Conditions" );

            res = fMgr.CreateConditionTemplate( std.InTheCategoryXName, std.InTheCategoryXNameDeep, null, ConditionOp.In, "Category", "Category" );
            fMgr.AssociateConditionWithGroup( res, "Category Conditions" );
            res = fMgr.CreateConditionTemplate( std.SenderIsInTheCategoryXName, std.SenderIsInTheCategoryXNameDeep, null, ConditionOp.In, "Category", "From>Category" );
            fMgr.AssociateConditionWithGroup( res, "Category Conditions" );
            res = fMgr.CreateStandardCondition( std.ReceivedAheadOfTodayName, std.ReceivedAheadOfTodayNameDeep, null, "Date", ConditionOp.Gt, "Tomorrow" );
            fMgr.AssociateConditionWithGroup( res, "Temporal Conditions" );
            res = fMgr.CreateConditionTemplate(std.DeletedInTheTimeSpanXName, std.DeletedInTheTimeSpanXNameDeep, null, ConditionOp.Eq, "DelDate");
            fMgr.AssociateConditionWithGroup(res, "Temporal Conditions");
            IResource dateRes = fMgr.CreateConditionTemplate(std.ReceivedInTheTimeSpanXName, std.ReceivedInTheTimeSpanXNameDeep, null, ConditionOp.Eq, "Date");
            fMgr.AssociateConditionWithGroup( dateRes, "Temporal Conditions" );

            //-----------------------------------------------------------------
            //  Standard conditions derived from standard templates
            //-----------------------------------------------------------------
            IResource todayCond = FilterConvertors.InstantiateTemplate( dateRes, "Today", null );
            IResource yesterdayCond = FilterConvertors.InstantiateTemplate( dateRes, "Yesterday", null );
            IResource thisWeekCond = FilterConvertors.InstantiateTemplate( dateRes, "This Week", null );
            IResource lastWeekCond = FilterConvertors.InstantiateTemplate( dateRes, "Last Week", null );
            IResource thisMonthCond = FilterConvertors.InstantiateTemplate( dateRes, "This Month", null );
            IResource lastMonthCond = FilterConvertors.InstantiateTemplate( dateRes, "Last Month", null );

            //-----------------------------------------------------------------
            //  Standard views
            //-----------------------------------------------------------------
            IResource unreadRes = fMgr.RegisterView( "Unread", new []{ unr }, null );
            IResource flaggedRes = fMgr.RegisterView( "Flagged", new []{ fla }, null );
            IResource annotatedRes = fMgr.RegisterView( "Annotated", new []{ ann }, null );
            IResource clippings = fMgr.RegisterView( "Clippings", new []{ cli }, null );
            IResource deletedRes = fMgr.RegisterView( "Deleted Resources", new []{ del }, null );
            deletedRes.SetProp( Core.Props.ShowDeletedItems, true );
            deletedRes.SetProp( "IsLiveMode", true );

            IResource today = fMgr.RegisterView( "Today", new []{ todayCond }, null );
            IResource yesterday = fMgr.RegisterView( "Yesterday", new []{ yesterdayCond }, null );
            IResource thisWeek = fMgr.RegisterView( "This week", new []{ thisWeekCond }, null );
            IResource lastWeek = fMgr.RegisterView( "Last week", new []{ lastWeekCond }, null );
            IResource thisMonth = fMgr.RegisterView( "This month", new []{ thisMonthCond }, null );
            IResource lastMonth = fMgr.RegisterView( "Last month", new []{ lastMonthCond }, null );

            fMgr.SetVisibleInAllTabs( flaggedRes );
            fMgr.SetVisibleInAllTabs( annotatedRes );
            fMgr.SetVisibleInAllTabs( clippings );
            fMgr.SetVisibleInAllTabs( deletedRes );

            //-----------------------------------------------------------------
            Core.FilterRegistry.CreateViewFolder( "Recent", null, 0 );

            fMgr.AssociateViewWithFolder( unreadRes, null, 1 );
            fMgr.AssociateViewWithFolder( deletedRes, null, 3 );
            fMgr.AssociateViewWithFolder( flaggedRes, null, 4 );
            fMgr.AssociateViewWithFolder( annotatedRes, null, 5 );
            fMgr.AssociateViewWithFolder( clippings, null, 7 );

            fMgr.AssociateViewWithFolder( today, "Recent", 0 );
            fMgr.AssociateViewWithFolder( yesterday, "Recent", 1 );
            fMgr.AssociateViewWithFolder( thisWeek, "Recent", 2 );
            fMgr.AssociateViewWithFolder( lastWeek, "Recent", 3 );
            fMgr.AssociateViewWithFolder( thisMonth, "Recent", 4 );
            fMgr.AssociateViewWithFolder( lastMonth, "Recent", 5 );

            //-----------------------------------------------------------------
            //  By default make all these views "threaded" due to the new
            //  possibilities to show attachments
            //-----------------------------------------------------------------
            today.SetProp( Core.Props.DisplayThreaded, true );
            yesterday.SetProp( Core.Props.DisplayThreaded, true );
            thisWeek.SetProp( Core.Props.DisplayThreaded, true );
            lastWeek.SetProp( Core.Props.DisplayThreaded, true );
            thisMonth.SetProp( Core.Props.DisplayThreaded, true );
            lastMonth.SetProp( Core.Props.DisplayThreaded, true );
        }
        #endregion FirstRun

        #region EachRun
        public void  RegisterViewsEachRun()
        {
            RegisterCustomConditions();
            RegisterRulesActions();
            RegisterViews();
            DeleteFileFormatGarbage();
            RegisterSearchQueryExtensions();
        }

        private static void DeleteFileFormatGarbage()
        {
            if( ObjectStore.ReadBool( "ViewsInitializer", "NeedToDeleteFormatFilesGarbageAfterVersion20", true ) )
            {
                IResourceStore store = Core.ResourceStore;
                foreach( IResourceType resType in Core.ResourceStore.ResourceTypes )
                {
                    if( resType.HasFlag( ResourceTypeFlags.FileFormat ) )
                    {
                        foreach( IResource file in store.GetAllResources( resType.Name ) )
                        {
                            if( file.GetLinkTypeIds().Length == 0 )
                            {
                                file.Delete();
                            }
                        }
                    }
                }
                ObjectStore.WriteBool( "ViewsInitializer", "NeedToDeleteFormatFilesGarbageAfterVersion20", false );
            }
        }

        private static void RegisterCustomConditions()
        {
            IResource res;
            IFilterRegistry fMgr = Core.FilterRegistry;

            #region Core.FilterRegistry.Std.ResourceIsCategorized Cleaning
            //  Fix previous version of this condition (it was implemented as
            //  predicate-type).
            IResourceList old = Core.ResourceStore.FindResources( FilterManagerProps.ConditionResName, "Name", Core.FilterRegistry.Std.ResourceIsCategorizedName );
            old = old.Intersect( Core.ResourceStore.FindResources( FilterManagerProps.ConditionResName, "ConditionType", "predicate" ), true );
            old.DeleteAll();
            #endregion Core.FilterRegistry.Std.ResourceIsCategorized Cleaning

            fMgr.RegisterCustomCondition( fMgr.Std.ResourceIsCategorizedName, fMgr.Std.ResourceIsCategorizedNameDeep,
                                          null, new ResourceIsCategorized() );
            res = fMgr.RegisterCustomCondition( fMgr.Std.ResourceHasEmptyContentName, fMgr.Std.ResourceHasEmptyContentNameDeep,
                                                null, new ResourceHasEmptyContent() );
            fMgr.MarkConditionOnlyForRule( res );

            res = fMgr.CreateStandardCondition( FilterManagerStandards.DummyConditionName, FilterManagerStandards.DummyConditionName,
                                                null, "Id", ConditionOp.Gt, "0" );
            res.SetProp( "Invisible", true );

            res = fMgr.CreateCustomConditionTemplate( fMgr.Std.InTheCategoryAndSubcategoriesXName, fMgr.Std.InTheCategoryAndSubcategoriesXNameDeep,
                                                      null, new MatchCategoryAndSubcategories(), ConditionOp.In, "Category", "Category" );
            fMgr.AssociateConditionWithGroup( res, "Category Conditions" );

            res = fMgr.CreateCustomConditionTemplate( fMgr.Std.FromToCCContactXName, fMgr.Std.FromToCCContactXNameDeep,
                                                      null, new CorrespondenceOfContact(), ConditionOp.In, "Contact", "Category" );
            fMgr.AssociateConditionWithGroup( res, "Address and Contact Conditions" );

            res = fMgr.CreateCustomConditionTemplate( fMgr.Std.FromToContactXName, fMgr.Std.FromToContactXNameDeep,
                                                      null, new FromToOfContact(), ConditionOp.In, "Contact", "Category" );
            fMgr.AssociateConditionWithGroup( res, "Address and Contact Conditions" );

            res = fMgr.CreateCustomConditionTemplate( fMgr.Std.ResourceContainsTextXName, fMgr.Std.ResourceContainsTextXNameDeep,
                                                      null, new ResourceContainsText(), ConditionOp.Eq, "Subject" );
            fMgr.AssociateConditionWithGroup( res, "Text Query Conditions" );
            fMgr.MarkConditionOnlyForRule( res );

            res = fMgr.CreateCustomConditionTemplate( fMgr.Std.MessageIsInThreadOfXName, fMgr.Std.MessageIsInThreadOfXNameDeep,
                                                      null, new MessageIsInThreadOf(), ConditionOp.In, "Email", "Category" );
            res.SetProp( "Invisible", true );

            //  Interversion consistency. In old build this condition was
            //  registered in other plugin(s). Since they now refer to Std,
            //  we have to register it again.
            res = fMgr.CreateConditionTemplate( fMgr.Std.FromContactXName, fMgr.Std.FromContactXNameDeep, null, ConditionOp.In, "Contact", "From" );
            fMgr.AssociateConditionWithGroup( res, "Address and Contact Conditions" );

            //  Upgrade for old versions.
            res = fMgr.CreateConditionTemplate( fMgr.Std.DeletedInTheTimeSpanXName, fMgr.Std.DeletedInTheTimeSpanXNameDeep, null, ConditionOp.Eq, "DelDate" );
            fMgr.AssociateConditionWithGroup(res, "Temporal Conditions");
        }

        private static void RegisterRulesActions()
        {
            IResource       res;
            IFilterRegistry  fmgr = Core.FilterRegistry;
            IStandardConditions std = fmgr.Std;

            //  Register them here since they require live object as parameter
            fmgr.RegisterRuleAction( std.MarkResourceAsImportantActionName, std.MarkResourceAsImportantActionNameDeep, new ImportantRuleAction() );
            fmgr.RegisterRuleAction( std.DeleteResourceActionName, std.DeleteResourceActionNameDeep, new DeleteResourceAction() );
            fmgr.RegisterRuleAction( std.DeleteResourcePermActionName, std.DeleteResourcePermActionNameDeep, new DeleteResourcePermanentlyAction() );
            fmgr.RegisterRuleAction( std.MarkResourceAsReadActionName, std.MarkResourceAsReadActionNameDeep, new MarkMessageAsReadAction() );
            fmgr.RegisterRuleAction( std.MarkResourceAsUnreadActionName, std.MarkResourceAsUnreadActionNameDeep, new MarkMessageAsUnreadAction() );
            fmgr.RegisterRuleAction( std.ShowDesktopAlertActionName, std.ShowDesktopAlertActionNameDeep, new BalloonNotificationAction() );
            fmgr.RegisterRuleAction( std.ShowAsPlainTextActionName, std.ShowAsPlainTextActionNameDeep, new ShowAsPlainTextAction() );

            fmgr.RegisterRuleActionTemplate( std.AssignCategoryActionName,  std.AssignCategoryActionNameDeep,
                                             new AssignCategoryAction(), ConditionOp.In, "Category" );
            fmgr.RegisterRuleActionTemplate( std.AssignCategoryToAuthorActionName, std.AssignCategoryToAuthorActionNameDeep,
                                             new AssignCategoryToMessageAuthorAction(), ConditionOp.In, "Category" );
            fmgr.RegisterRuleActionTemplate( std.PlaySoundFromFileActionName, std.PlaySoundFromFileActionNameDeep,
                                             new PlaySoundAction(), ConditionOp.In, "ExternalFile", "Sound files (*.wav)|*.wav|All files(*.*)|*.*" );
            fmgr.RegisterRuleActionTemplate( std.DisplayMessageBoxActionName, std.DisplayMessageBoxActionNameDeep,
                                             new MessageBoxNotificationAction(), ConditionOp.Eq );
            fmgr.RegisterRuleActionTemplate( std.RunApplicationActionName, std.RunApplicationActionNameDeep,
                                             new RunApplicationAction(), ConditionOp.In, "ExternalFile", "Execution files (*.exe)|*.exe|All files(*.*)|*.*" );

            res = fmgr.RegisterRuleActionTemplate( std.MarkMessageWithFlagActionName, std.MarkMessageWithFlagActionNameDeep,
                                                   new MarkMessageWithFlagAction(), ConditionOp.In, "Flag" );
            fmgr.MarkActionTemplateAsSingleSelection( res );

            Core.ActionManager.RegisterLinkClickAction( new EditRuleAction(), FilterManagerProps.RuleResName, null );
        }

        private static void RegisterViews()
        {
            IFilterRegistry  fMgr = Core.FilterRegistry;
            if( Core.ResourceStore.FindResources( FilterManagerProps.ViewResName, "DeepName", "Deleted Resources" ).Count == 0 )
            {
                Trace.WriteLine( "ViewsInitializer -- DeletedResources view is not found");
                IResource res = fMgr.RegisterView( "Deleted Resources", new []{ fMgr.Std.ResourceIsDeleted }, null );
                fMgr.AssociateViewWithFolder( res, null, 3 );
                fMgr.SetVisibleInAllTabs( res );
                res.SetProp( Core.Props.ShowDeletedItems, true );
                res.SetProp( "IsLiveMode", true );
            }
        }

        private static void RegisterSearchQueryExtensions()
        {
            Core.SearchQueryExtensions.RegisterSingleTokenRestriction( "in", "unread", Core.FilterRegistry.Std.ResourceIsUnread );
            Core.SearchQueryExtensions.RegisterSingleTokenRestriction( "in", "flagged", Core.FilterRegistry.Std.ResourceIsFlagged );
            Core.SearchQueryExtensions.RegisterSingleTokenRestriction( "in", "deleted", Core.FilterRegistry.Std.ResourceIsDeleted );
            Core.SearchQueryExtensions.RegisterSingleTokenRestriction( "in", "cats", Core.FilterRegistry.Std.ResourceIsCategorized );

            Core.SearchQueryExtensions.RegisterFreestyleRestriction( "in", new CategoryTokenMatcher() );
            Core.SearchQueryExtensions.RegisterFreestyleRestriction( "from", new FromTokenMatcher() );
            Core.SearchQueryExtensions.RegisterFreestyleRestriction( "on", new DateRangeTokenMatcher() );
        }

        #region Token Matchers
        private class CategoryTokenMatcher : IQueryTokenMatcher
        {
            public IResource ParseTokenStream( string stream )
            {
                IResource   condition = null;
                IResourceList categories = Core.ResourceStore.GetAllResources( "Category" );
                IResource template = Core.FilterRegistry.Std.InTheCategoryX;
                if( template != null )
                {
                    stream = stream.ToLower().Trim();
                    foreach( IResource res in categories.ValidResources )
                    {
                        if( res.GetStringProp( Core.Props.Name ).ToLower() == stream )
                        {
                            condition = FilterConvertors.InstantiateTemplate( template, res.ToResourceList(), null );
                            break;
                        }
                    }
                }

                return condition;
            }
        }

        private class FromTokenMatcher : IQueryTokenMatcher
        {
            public IResource ParseTokenStream( string stream )
            {
                IResource   condition = null;
                IResourceList candidates = Core.ResourceStore.EmptyResourceList;

                stream = stream.ToLower().Trim();
                if( stream == "me" || stream == "myself" )
                {
                    candidates = candidates.Union( Core.ContactManager.MySelf.Resource.ToResourceList() );
                }
                else
                {
                    string[] tokens = stream.Split( ' ' );
                    foreach( string token in tokens )
                    {
                        IResourceList list = Core.ResourceStore.FindResources( "Contact", ContactManager._propFirstName, token );
                        list = list.Union( Core.ResourceStore.FindResources( "Contact", ContactManager._propLastName, token ) );

                        candidates = (candidates.Count == 0) ? list : candidates.Intersect( list );
                    }
                }

                if( candidates.Count > 0 )
                {
                    IResource template = Core.FilterRegistry.Std.FromContactX;
                    if( template != null ) //  Everything is possible :-(
                    {
                        condition = Core.FilterRegistry.InstantiateConditionTemplate( template, candidates, null );
                    }
                }

                return condition;
            }
        }

        private class DateRangeTokenMatcher : IQueryTokenMatcher
        {
            public IResource ParseTokenStream( string stream )
            {
                IResource   condition = null;
                IResource template = Core.FilterRegistry.Std.ReceivedInTheTimeSpanX;
                if( template == null ) //  Everything is possible :-(
                    return condition;

                string  propName = template.GetStringProp( "ApplicableToProp" );
                stream = stream.ToLower().Trim();

                if( stream == "." || stream == "today" )
                {
                    condition = FilterConvertors.InstantiateTemplate( template, "Today", null );
                }
                else
                if( stream == ".." )
                {
                    condition = FilterConvertors.InstantiateTemplate( template, "Yesterday", null );
                }
                else
                if( stream == "tw" || stream == "thisw" || stream == "this week" )
                {
                    condition = FilterConvertors.InstantiateTemplate( template, "This Week", null );
                }
                else
                if( stream == "lw" || stream == "lastw" || stream == "last week" )
                {
                    condition = FilterConvertors.InstantiateTemplate( template, "Last Week", null );
                }
                else
                {
                    int indexInSet;
                    string  startDate, endDate;
                    if( isMonthName( stream, out indexInSet ) )
                    {
                        int monthNow = DateTime.Now.Month;
                        if( monthNow == indexInSet )
                        {
                            condition = FilterConvertors.InstantiateTemplate( template, "This Month", null );
                        }
                        else
                        {
                            int     yearNum = ( monthNow < indexInSet ) ? DateTime.Now.Year - 1 : DateTime.Now.Year;
                            startDate = new DateTime( yearNum, indexInSet, 1 ).ToString();
                            endDate = new DateTime( yearNum, indexInSet, DateTime.DaysInMonth( yearNum, indexInSet ) ).ToString();

                            condition = ((FilterRegistry)Core.FilterRegistry).CreateStandardConditionAux(
                                                null, propName, ConditionOp.InRange, startDate, endDate );
                        }
                    }
                    else
                    if( isYearNumber( stream, out indexInSet ) )
                    {
                        startDate = new DateTime( indexInSet, 1, 1 ).ToString();
                        endDate = new DateTime( indexInSet, 12, DateTime.DaysInMonth( indexInSet, 12 ) ).ToString();

                        condition = ((FilterRegistry)Core.FilterRegistry).CreateStandardConditionAux(
                                            null, propName, ConditionOp.InRange, startDate, endDate );
                    }
                }

                return condition;
            }

            private static bool isMonthName( string text, out int monthNum )
            {
                monthNum = -1;
                for( int i = 1; i <= 12; i++ )
                {
                    DateTime time = new DateTime( 1, i, 1 );
                    string name = time.ToString( "MMM" ).ToLower(), fullName = time.ToString( "MMMM" ).ToLower();
                    if( text == name || text == fullName )
                    {
                        monthNum = i;
                        break;
                    }
                }
                return (monthNum != -1);
            }

            private static bool isYearNumber( string text, out int year )
            {
                year = -1;
                try
                {
                    year = Int32.Parse( text );
                }
                catch{}
                return (year > 1980) && (year <= DateTime.Now.Year);
            }
        }
        #endregion Token Matchers
        #endregion EachRun
    }

    #region Custom Conditions and Rule Actions
    //-------------------------------------------------------------------------
    //  The necessity of this condition is due to the fact that
    //  all categorized resources are linked to any "Category" resource
    //  with non-directed link, thus if we simpy want to show all resources
    //  which have such link, "Category"-type resources are also selected.
    //  Thus we need to filter them out.
    //-------------------------------------------------------------------------
    public class ResourceIsCategorized : ICustomCondition
    {
        public bool MatchResource( IResource res )
        {
            return ( res.GetLinkCount( "Category" ) > 0 ) && ( res.Type != "Category" );
        }

        public IResourceList Filter( string resType )
        {
            IResourceList result = Core.ResourceStore.FindResourcesWithProp( SelectionType.LiveSnapshot, null, "Category" );
            result = result.Minus( Core.ResourceStore.GetAllResources( "Category" ) );
            return result;
        }
    }

    public class ResourceHasEmptyContent : ICustomCondition
    {
        public bool MatchResource( IResource res )
        {
            string longContent = res.GetPropText( Core.Props.LongBody );
            return longContent.Length == 0;
        }

        public IResourceList Filter( string resType )
        {
            return Core.ResourceStore.EmptyResourceList;
        }
    }

    public class MatchCategoryAndSubcategories : ICustomConditionTemplate
    {
        public bool MatchResource( IResource res, IActionParameterStore actionStore )
        {
            bool match = false;
            if( res.Type != "Category" )
            {
                IResourceList  linkedCategories = res.GetLinksOfType( "Category", "Category" );
                if( linkedCategories.Count > 0 )
                {
                    IResourceList matchCats = actionStore.ParametersAsResList();
                    matchCats = CategoriesTree( matchCats );
                    match = ( matchCats.Intersect( linkedCategories, true ).Count > 0 );
                }
            }
            return match;
        }

        public IResourceList Filter( string resType, IActionParameterStore actionStore )
        {
            IResourceList categories = actionStore.ParametersAsResList();
            categories = CategoriesTree( categories );
            IResourceList result = Core.ResourceStore.EmptyResourceList;
            foreach( IResource category in categories )
            {
                result = result.Union( category.GetLinksOfType( null, "Category" ));
            }
            result = result.Minus( Core.ResourceStore.GetAllResources( "Category" ) );
            return result;
        }

        private static IResourceList  CategoriesTree( IResourceList heads )
        {
            IResourceList result = Core.ResourceStore.EmptyResourceList;
            foreach( IResource category in heads )
            {
                result = result.Union( GetSubtree( category ) );
            }
            return result;
        }

        private static IResourceList GetSubtree( IResource headCategory )
        {
            IResourceList result = Core.ResourceStore.EmptyResourceList;
            IResourceList childs = headCategory.GetLinksTo( "Category", Core.Props.Parent );
            foreach( IResource category in childs )
            {
                result = result.Union( GetSubtree( category ) );
            }
            result = result.Union( headCategory.ToResourceList() );
            return result;
        }
    }

    public class CorrespondenceOfContact : ICustomConditionTemplate
    {
        public bool MatchResource( IResource res, IActionParameterStore actionStore )
        {
            IResourceList contacts = actionStore.ParametersAsResList();
            IResourceList linkedContacts = res.GetLinksOfType( null, Core.ContactManager.Props.LinkFrom );
            linkedContacts = linkedContacts.Union( res.GetLinksOfType( null, Core.ContactManager.Props.LinkTo ), true );
            linkedContacts = linkedContacts.Union( res.GetLinksOfType( null, Core.ContactManager.Props.LinkCC ), true );
            linkedContacts = linkedContacts.Intersect( contacts, true );
            return linkedContacts.Count > 0;
        }

        public IResourceList Filter( string resType, IActionParameterStore actionStore )
        {
            IResourceList contacts = actionStore.ParametersAsResList();
            IResourceList result = Core.ResourceStore.EmptyResourceList;
            lock( contacts )
            {
                foreach( IResource contact in contacts )
                {
                    result = result.Union( ContactManager.LinkedCorrespondence( contact ) );
                }
            }
            return result;
        }
    }

    public class FromToOfContact : ICustomConditionTemplate
    {
        public bool MatchResource( IResource res, IActionParameterStore actionStore )
        {
            IResourceList contacts = actionStore.ParametersAsResList();
            IResourceList linked = res.GetLinksOfType( null, Core.ContactManager.Props.LinkFrom );
            linked = linked.Union( res.GetLinksOfType( null, Core.ContactManager.Props.LinkTo ), true );
            linked = linked.Intersect( contacts, true );

            return linked.Count > 0;
        }

        public IResourceList Filter( string resType, IActionParameterStore actionStore )
        {
            IResourceList contacts = actionStore.ParametersAsResList();
            IResourceList result = Core.ResourceStore.EmptyResourceList;
            lock( contacts )
            {
                foreach( IResource contact in contacts )
                {
                    result = result.Union( ContactManager.LinkedCorrespondenceDirect( contact ) );
                }
            }
            return result;
        }
    }

    public class MessageIsInThreadOf : ICustomConditionTemplate
    {
        public bool MatchResource( IResource res, IActionParameterStore actionStore )
        {
            IResourceList threadHeads = actionStore.ParametersAsResList();
            IResource msg = res;
            while( msg != null && threadHeads.IndexOf( msg ) == -1 )
            {
                msg = msg.GetLinkProp( Core.Props.Reply );
            }
            return ( msg != null );
        }

        public IResourceList Filter( string resType, IActionParameterStore actionStore )
        {
            throw new NotSupportedException( "FilterRegistry -- this condition can be used only in Action or Formatting rules." );
        }
    }

    public class ResourceContainsText : ICustomConditionTemplate
    {
        private class TextConsumer : IResourceTextConsumer
        {
            internal void  Init()
            {
                RejectResult();
            }

            internal string Body {  get { return AccumulatedBody.ToString(); }  }

            #region IResourceTextConsumer2 interface
            public void   AddDocumentHeading( int docID, string text )
            {
                AddDocumentFragment( docID, text, DocumentSection.SubjectSection );
            }
            public void   AddDocumentFragment( int docID, string text )
            {
                AddDocumentFragment( docID, text, DocumentSection.BodySection );
            }
            public void   AddDocumentFragment( int docID, string text, string sectionName )
            {
                if( !String.IsNullOrEmpty( text ) )
                    AccumulatedBody.Append( text );
            }

            //  As was agreed with HtmlParser, this method is called exclusively
            //  for skipping tag information. Since we have to show the text "nicely",
            //  we subst large amount of blanks with just one for aesteics.
            public void  IncrementOffset( int count )
            {
                for( int i = 0; i < count; i++ )
                    AccumulatedBody.Append( ' ' );
            }

            public void  RestartOffsetCounting()
            {}

            public void  RejectResult()
            {
                AccumulatedBody.Length = 0;
                if( AccumulatedBody.Capacity > 16384 )
                {
                    AccumulatedBody.Capacity = 1024;
                }
            }
            public TextRequestPurpose Purpose
            {
                get{  return TextRequestPurpose.Indexing;  }
            }
            #endregion IResourceTextConsumer2 interface

            private readonly StringBuilder AccumulatedBody = new StringBuilder();
        }

        private readonly TextConsumer stream = new TextConsumer();

        public bool MatchResource( IResource res, IActionParameterStore actionStore )
        {
            string pattern = actionStore.ParameterAsString();
            stream.Init();
            Core.PluginLoader.InvokeResourceTextProviders( res, stream );
            return ( stream.Body.IndexOf( pattern ) != -1 );
        }

        public IResourceList Filter( string resType, IActionParameterStore actionStore )
        {
            throw new NotSupportedException( "FilterRegistry -- this condition can be used only in Action or Formatting rules." );
        }
    }
    #endregion Custom Conditions

    #region Rule Actions
    public class ImportantRuleAction : IRuleAction
    {
        public void  Exec( IResource res, IActionParameterStore actionStore )
        {
            ResourceProxy proxy = new ResourceProxy( res );
            proxy.BeginUpdate();
            proxy.SetProp( "Importance", 1 );
            proxy.EndUpdate();
        }
    }

    public class ShowAsPlainTextAction : IRuleAction
    {
        public void  Exec( IResource res, IActionParameterStore actionStore )
        {
            ResourceProxy proxy = new ResourceProxy( res );
            proxy.BeginUpdate();
            proxy.SetProp( "NoFormat", true );
            proxy.EndUpdate();
        }
    }

    public class AssignCategoryAction : IRuleAction
    {
        public void  Exec( IResource res, IActionParameterStore actionStore )
        {
            IResourceList categories = actionStore.ParametersAsResList();
            foreach( IResource category in categories )
            {
                Core.CategoryManager.AddResourceCategory( res, category );
            }
        }
    }

    public class AssignCategoryToMessageAuthorAction : IRuleAction
    {
        public void  Exec( IResource res, IActionParameterStore actionStore )
        {
            IResourceList categories = actionStore.ParametersAsResList();
            IResourceList authors = res.GetLinksOfType( null, Core.ContactManager.Props.LinkFrom );
            foreach( IResource author in authors )
            {
                IResourceType type = Core.ResourceStore.ResourceTypes[ author.TypeId ];

                //  Do not assign categories for resource types which are
                //  internal in the sence - they are not showable in the
                //  traditional ResourceListView pane. Thus, user can not
                //  benefit from setting a category to these internal types.

                if( !type.HasFlag( ResourceTypeFlags.Internal ))
                {
                    ResourceProxy proxy = new ResourceProxy( author );
                    proxy.BeginUpdate();
                    foreach( IResource category in categories )
                        proxy.AddLink( "Category", category );

                    proxy.EndUpdate();
                }
            }
        }
    }

    public class MarkMessageAsReadAction : IRuleAction
    {
        public void   Exec( IResource res, IActionParameterStore actionStore )
        {
            ResourceProxy proxy = new ResourceProxy( res );
            proxy.BeginUpdate();
            proxy.SetProp( "IsUnread", false );
            proxy.EndUpdate();
        }
    }

    public class MarkMessageAsUnreadAction : IRuleAction
    {
        public void   Exec( IResource res, IActionParameterStore actionStore )
        {
            ResourceProxy proxy = new ResourceProxy( res );
            proxy.BeginUpdate();
            proxy.SetProp( "IsUnread", true );
            proxy.EndUpdate();
        }
    }

    public class MarkMessageWithFlagAction : IRuleAction
    {
        public void   Exec( IResource res, IActionParameterStore actionStore )
        {
            IResourceList flags = actionStore.ParametersAsResList();
            ResourceProxy proxy = new ResourceProxy( res );
            proxy.BeginUpdate();
            proxy.SetProp( "Flag", flags[ 0 ] );
            proxy.EndUpdate();
        }
    }

    public class DeleteResourceAction : IRuleAction
    {
        public void  Exec( IResource res, IActionParameterStore actionStore )
        {
            IResourceDeleter deleter = Core.PluginLoader.GetResourceDeleter( res.Type );
            if( deleter != null )
                deleter.DeleteResource( res );
        }
    }

    public class DeleteResourcePermanentlyAction : IRuleAction
    {
        public void  Exec( IResource res, IActionParameterStore actionStore )
        {
            IResourceDeleter deleter = Core.PluginLoader.GetResourceDeleter( res.Type );
            if( deleter != null )
                deleter.DeleteResourcePermanent( res );
        }
    }

    public class PlaySoundAction : IRuleAction
    {
        public void   Exec( IResource res, IActionParameterStore actionStore )
        {
            string  soundFileName = actionStore.ParameterAsString();
            WindowsMultiMedia.PlaySound( soundFileName, (IntPtr)0, WindowsMultiMedia.SND_FILENAME );
        }
    }

    public class RunApplicationAction : IRuleAction
    {
        public void   Exec( IResource res, IActionParameterStore actionStore )
        {
            string  fileName = actionStore.ParameterAsString();
            Process process = new Process();
            Trace.WriteLine( "RunApplcationAction - starting " + fileName );

            try
            {
                process.StartInfo.FileName = fileName;
                process.StartInfo.WorkingDirectory = ".";
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                if( !process.Start() )
                    throw( new Exception( "RunApplcationAction - Aplication did not managed to" +
                                          "call Start for the process with filename: " + fileName ));
            }
            catch( Exception e )
            {
                Trace.WriteLine( "RunApplcationAction - failed to start application with reason: " + e.Message );
            }
        }
    }
    #endregion Rule Actions
}
