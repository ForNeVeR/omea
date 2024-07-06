// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.FiltersManagement
{
    public class FilterEngine : IFilterEngine
    {
        private const   char    cPropertyPathDelimiter = '>';

        private readonly IResourceStore _store;
        private readonly FilterManagerProps _props;

        internal static IHighlightDataProvider Highlighter;
        private readonly Hashtable   RegisteredEvents = new Hashtable();

        private static string[] _lastStopList;
        internal static string  _lastQueryError;
        private bool            _isVerbose;

        //                                      Dec Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov
        private readonly int[]  DaysPerMonth = { 31, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30 };

        private DateTime Today, Yesterday, Tomorrow, WeekStart, NextWeekStart, MonthStart, YearStart;

        #region ctor and initialization
        public FilterEngine( IResourceStore store, IFilterRegistry mgr )
        {
            _store = store;
            _props = (FilterManagerProps)mgr.Props;

            //-----------------------------------------------------------------
            //  Register two standard (core) events with their display names.
            //-----------------------------------------------------------------
            RegisterActivationEvent( StandardEvents.ResourceReceived, "Resource is received" );
            RegisterActivationEvent( StandardEvents.CategoryAssigned, "Category is assigned" );
        }

        public void  InitializeCriteria()
        {
            _isVerbose = Core.SettingStore.ReadBool( "Rules", "Verbose", false );
            SetupMidnightViewUpdate();

            Core.TextIndexManager.SetUpdateResultHandler( NextUpdateFinishedFromFullTextIndexer );
        }

        private static void SetupMidnightViewUpdate()
        {
            DateTime  startingTime = DateTime.Today.AddDays( 1.0 );

            Core.ResourceAP.QueueJobAt( startingTime, new MethodInvoker( SetupMidnightViewUpdate ) );
            FilterRegistry._UIHandler.ResetSelection();
        }
        #endregion ctor and initialization

        #region DelegateRegistration

        private delegate void OneParamMethodInvoker( int[] ids );

        //  Get a list of document Ids which just passed the text index
        //  phase. We have to run rules which contain conditions with
        //  text index queries.
        private void  NextUpdateFinishedFromFullTextIndexer( object sender, DocsArrayArgs args )
        {
            Core.ResourceAP.QueueJob( JobPriority.Lowest,
                                      new OneParamMethodInvoker( NextUpdateFinishedProcessor ),
                                      args.GetDocuments() );
        }

        private void  NextUpdateFinishedProcessor( int[] docIds )
        {
            Trace.WriteLine( "FilterRegistry (NextUpdateFinishedProcessor) -- Called by delegate for " + docIds.Length + " documents " );
            IResourceList   list = Core.ResourceStore.ListFromIds( docIds, false );
            if( list != null && list.Count > 0 )
            {
                ExecTIRules( list );
            }
        }
        #endregion DelegateRegistration

        #region ExecView/MatchView
        //---------------------------------------------------------------------
        //  Compute a view - enumerate its search conditions and intersect their
        //  results, since criterion is a resource conjunction.
        //  Finally, count the number of unread resources.
        //---------------------------------------------------------------------
        //  - ExecView( IResource, string ) is called only outside - upon
        //    explicit selection of the view in the views tree
        //  - ExecView( IResource ) is auxiliary.
        //---------------------------------------------------------------------

        public IResourceList ExecView( IResource view, string viewName )
        {
            #region Preconditions
            if( view == null )
                throw new ArgumentNullException( "view", "FilterRegistry -- View is NULL." );
            #endregion Preconditions

            //-----------------------------------------------------------------
            //  Sometimes view becomes invalid due to other bugs and there is
            //  absolutely no reason to thow an exception.
            //-----------------------------------------------------------------
            string name = !string.IsNullOrEmpty( viewName ) ? viewName : "Unnamed View";

            Highlighter = null;
            IResourceList result = ExecView( view );
            Trace.WriteLineIf( _isVerbose, "*** FilterRegistry -- [" + name + "] returned " + result.Count + " unfiltered results." );

            result = ResourceTypeHelper.ExcludeUnloadedPluginResources( result );

            if( view.HasProp( "DefaultSort" ))
            {
                int propId = Core.ResourceStore.PropTypes[ "DefaultSort" ].Id;
                result.Sort( new SortSettings( propId, true ) );
            }
            return result;
        }

        public IResourceList  ExecView( IResource view )
        {
            #region Preconditions
            if( view == null )
                throw new ArgumentNullException( "view", "FilterRegistry -- View is NULL." );
            #endregion Preconditions

            return ExecView( view, (IResourceList)null );
        }
        public IResourceList  ExecView( IResource view, IResourceList initialSet )
        {
            SelectionType   mode = view.HasProp( "IsLiveMode" ) ? SelectionType.Live : SelectionType.LiveSnapshot;

            return ExecView( view, initialSet, mode );
        }

	    public IResourceList ExecView( IResource view, IResourceList initialSet, SelectionType mode )
	    {
            #region Preconditions
	        if( view == null )
	            throw new ArgumentNullException( "view", "FilterRegistry -- View is NULL." );

	        if( view.Type != FilterManagerProps.ViewResName && view.Type != FilterManagerProps.RuleResName && view.Type != FilterManagerProps.ViewCompositeResName )
	            throw new InvalidOperationException( "FilterRegistry -- Can not apply [ExecView] to inappropriate resource (of type" + view.Type + ")" );
            #endregion Preconditions

            //  Process special case when the initial set is empty, it is
            //  unambiguously matches the result.
            if( initialSet != null && initialSet.Count == 0 )
                return _store.EmptyResourceList;

	        IResourceList result = _store.EmptyResourceList;

	        //-----------------------------------------------------------------
	        //  If an error occured while executing a view/rule, we reflect that
	        //  fact with setting the Error Message text into the "LastError" prop.
	        //  This helps us not to execute an error view (causing the exception
	        //  to be throwed more and more) until the case will be corrected in
	        //  the View/Rule Editor.
	        //  Prop "LastError" is cleared in the View/Rule Editor after the
	        //  successful reediting of the view.
	        //-----------------------------------------------------------------
	        string errorMsg = view.GetStringProp( Core.Props.LastError );
	        if( string.IsNullOrEmpty( errorMsg ) )
	        {
	            string  resTypes = view.GetStringProp( Core.Props.ContentType );
	            string  resLinks = view.GetStringProp( "ContentLinks" );

                try
                {
	                IResourceList   conditionGroups = view.GetLinksOfType( FilterManagerProps.ConjunctionGroup, _props._linkedConditionsLink );
                    if( conditionGroups.Count > 0 )
                    {
                        foreach( IResource group in conditionGroups )
                        {
                            IResourceList groupResult = ExecConditionsGroup( group, mode, resTypes, resLinks, initialSet );
                            result = result.Union( groupResult );
                        }
                    }
	                else
                    {
                        result = initialSet ?? GetResourcesOfType( resTypes, resLinks );
                    }
                }
                catch( Exception e )
                {
                    Trace.WriteLine( "$$$ FilterRegistry -- View [" + view.DisplayName + "] caused an exception:");
                    Trace.WriteLine( e.Message );
                }

	            IResourceList   exceptions = view.GetLinksOfType( FilterManagerProps.ConditionResName, _props._linkedExceptionsLink );
	            for( int i = 0; i < exceptions.Count; i++ )
	                result = result.Minus( ExecCondition( exceptions[ i ], mode, resTypes, resLinks ) );
	        }

            if( initialSet != null )
                result = result.Intersect( initialSet );

	        return result;
	    }

	    public bool  MatchView( IResource view, IResource res, bool checkWorkspace )
        {
            bool isMatched = false;

            //-----------------------------------------------------------------
            //  See the comment above on using marker prop "LastError"
            //-----------------------------------------------------------------
            string errorMsg = view.GetStringProp( Core.Props.LastError );
            if( string.IsNullOrEmpty( errorMsg ) )
            {
                string resTypes = view.GetStringProp( Core.Props.ContentType );
                string resLinks = view.GetStringProp( "ContentLinks" );
                isMatched = ( resTypes == null && resLinks == null ) ||
                            ( resTypes != null && resTypes.IndexOf( res.Type ) != -1 ) ||
                            ( resLinks != null && isLinkPresent( resLinks, res ) );

                if ( isMatched )
                {
                    IResourceList conditionGroups = view.GetLinksOfType( FilterManagerProps.ConjunctionGroup, _props._linkedConditionsLink );
                    foreach( IResource group in conditionGroups )
                    {
                        isMatched = true;
                        IResourceList conditions = group.GetLinksOfType( FilterManagerProps.ConditionResName, _props._linkedConditionsLink );
                        for( int i = 0; i < conditions.Count; ++i )
                        {
                            if ( !MatchCondition( conditions[ i ], res, resTypes, resLinks ) )
                            {
                                isMatched = false;
                                break;
                            }
                        }
                        if( isMatched )
                            break;
                    }
                    if( !isMatched )
                        return false;

                    IResourceList exceptions = view.GetLinksOfType( FilterManagerProps.ConditionResName, _props._linkedExceptionsLink );
                    for( int i = 0; i < exceptions.Count; ++i )
                    {
                        if ( MatchCondition( exceptions[ i ], res, resTypes, resLinks ) )
                        {
                            return false;
                        }
                    }

                    if ( checkWorkspace )
                    {
                        IResource workspace = Core.WorkspaceManager.ActiveWorkspace;
                        IResourceList list = Core.WorkspaceManager.GetFilterList( workspace );
                        if( list != null )
                        {
                            return ( list.IndexOf( res ) != -1 );
                        }
                    }
                }
            }
            return isMatched;
        }

        //  NB: Check workspace is always false.
        internal bool  MatchViewReduced( IResource view, IResource res )
        {
            bool   isMatched = false;

            //-----------------------------------------------------------------
            //  See the comment above on using marker prop "LastError"
            //-----------------------------------------------------------------
            string errorMsg = view.GetStringProp( Core.Props.LastError );
            if( string.IsNullOrEmpty( errorMsg ) )
            {
                string  resTypes = view.GetStringProp( Core.Props.ContentType );
                string  resLinks = view.GetStringProp( "ContentLinks" );
                isMatched = ( resTypes == null && resLinks == null ) ||
                            ( resTypes != null && resTypes.IndexOf( res.Type ) != -1 );

                if ( isMatched )
                {
                    IResourceList conditionGroups = view.GetLinksOfType( FilterManagerProps.ConjunctionGroup, _props._linkedConditionsLink );
                    foreach( IResource group in conditionGroups )
                    {
                        isMatched = true;
                        IResourceList conditions = group.GetLinksOfType( FilterManagerProps.ConditionResName, _props._linkedConditionsLink );
                        for( int i = 0; i < conditions.Count; ++i )
                        {
                            if( !FilterRegistry.IsUnreadCondition( conditions[ i ] ) )
                            {
                                if ( !MatchCondition( conditions[ i ], res, resTypes, resLinks ) )
                                {
                                    isMatched = false;
                                    break;
                                }
                            }
                        }
                        if( isMatched )
                            break;
                    }
                    if( !isMatched )
                        return false;

                    IResourceList exceptions = view.GetLinksOfType( FilterManagerProps.ConditionResName, _props._linkedExceptionsLink );
                    if( exceptions.Count > 0 )
                    {
                        for( int i = 0; i < exceptions.Count; ++i )
                        {
                            if ( MatchCondition( exceptions[ i ], res, resTypes, resLinks ) )
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            return( isMatched );
        }

        private IResourceList  ExecConditionsGroup( IResource group, SelectionType mode,
                                                    string resTypes, string resLinks, IResourceList initialSet )
        {
            IResourceList   result = initialSet;
	        IResourceList   conditions = group.GetLinksOfType( FilterManagerProps.ConditionResName, _props._linkedConditionsLink );

            //-----------------------------------------------------------------
            //  Dummy condition is used in Expiration rules for FilterRegistry
            //  contract consistency (most often they do not declare any
            //  conditions but rather only exceptions to be used over some
            //  given list of resources).
            //  NB: if we understand that condition configuration belongs to the
            //      expiration rule mode, just return the initial set as is,
            //      so it is intersected with the exceptions.
            //-----------------------------------------------------------------
            if( !( conditions.Count == 1 && IsDummy( conditions[ 0 ] )) )
            {
                result = ExecCondition( conditions[ 0 ], mode, resTypes, resLinks );
	            for( int i = 1; i < conditions.Count; i++ )
	                result = result.Intersect( ExecCondition( conditions[ i ], mode, resTypes, resLinks ), true );
            }
            return result;
        }
        #endregion ExecView/MatchView

        #region ExecRules/Actions
        ///<summary>
        ///<para>Rule Execution has two contexts - "scalar" context and "list" context.</para>
        ///<para>
        ///  Scalar context is called when new resource is created within/by a
        ///  plugin or core and a set of rules is to be executed for it.
        ///  List context is called when a set of resources is passed through
        ///  TextIndex module and is available for text searching. Second mode is
        ///  necessary since a resource creation and its availability in text
        ///  index are diversed in time significantly. Moreover, obviously, rules
        ///  that contain query condition(s) will beforehand fail in scalar context.
        ///  And since the rules in the list context are executed in batch mode
        ///  over the (potentially) large number of resources, this increases
        ///  the performance drastically.</para>
        ///<para>
        ///  Thus we deterministically split set of available rules in two subsets:
        ///  those which have no query conditions and those which have. First
        ///  subset is executed in scalar context, second one - in the list context.
        /// </para>
        /// </summary>
        public bool  ExecRules( string eventName, IResource res )
        {
            #region Preconditions
            if( res == null )
                throw new ArgumentNullException( "res", "FilterRegistry - Resource is NULL in rule activation." );
            #endregion Preconditions

            Trace.WriteLineIf( _isVerbose, "*** FilterRegistry -- Rules are activated for a resource " + res.DisplayName );

            bool  matched = ExecRulesImpl( res.ToResourceList(), eventName, false );
            return matched;
        }

        /// <summary>
        ///  Apply only TextIndex query-containing rules to a list of resources.
        ///  This happens only upon receiving the particular event form the TIMgr.
        /// </summary>
        private void  ExecTIRules( IResourceList list )
        {
            ExecRulesImpl( list, StandardEvents.ResourceReceived, true );
        }

        private bool ExecRulesImpl( IResourceList list, string eventName, bool queryRules )
        {
            bool  matched = false;
            IResourceList   rules = GetActiveRules( queryRules );

            lock( list ) lock( rules )
            {
                foreach( IResource rule in rules )
                {
                    foreach( IResource res in list.ValidResources )
                        matched |= ApplyRule( rule, eventName, res );
                }
            }
            return matched;
        }

        ///<summary>
        ///  Apply a rule to a list of resources. First, select resources which match the rule,
        ///  then apply rule actions to them.
        ///  Used in "Apply Rules..." dialog and via ExpirationRulesManager
        ///
        /// TODO: Since this call is performed by the user-call (and not by an Omea
        /// subsystem during the normal resource lifecycle), we can not rely on the
        /// "hot" resource information like the text conditions which were passed successfully
        /// during the last text index analysis. Since that method uses a batch "ExecView"
        /// on the conditional part of the rule instead of MatchView (per resource), some
        /// rules will not match completely due to the restricted nature of some conditions
        /// (several of them are suited ONLY for matching via MatchView).
        /// </summary>
        public void  ExecRule( IResource rule, IResourceList list )
        {
            #region Preconditions
            if( rule == null )
                throw new ArgumentNullException( "rule", "FilterRegistry - Rule resource is NULL." );

            if( list == null )
                throw new ArgumentNullException( "list", "FilterRegistry - Source list of resources is NULL in rule activation." );
            #endregion Preconditions

            IResourceList matchedOnRule = ExecView( rule, list );

            lock( matchedOnRule )
            {
                for( int i = matchedOnRule.Count - 1; i >= 0; i-- )
                {
                    //  Workaround of OM-12955 and related exceptions.
                    try
                    {
                        ApplyActions( rule, matchedOnRule[ i ] );
                    }
                    catch( InvalidResourceIdException )
                    {
                        //  Nothing to do, ignore resource.
                    }
                }
            }
        }

        private bool  ApplyRule( IResource rule, string eventName, IResource res )
        {
            bool matched = false;
            string  ruleEventName = rule.GetStringProp( "EventName" );
            if( String.IsNullOrEmpty( eventName ) || ruleEventName.Equals( eventName ) )
            {
                if( MatchView( rule, res, false ) )
                {
                    ApplyActions( rule, res );
                    matched = true;
                }
            }
            return matched;
        }

        /// <summary>
        /// Active rule: is not turned off, sorted by the order and either contains
        /// query conditions or not depending on the given parameter.
        /// </summary>
        private static IResourceList GetActiveRules( bool isQueryRule )
        {
            IResourceList rules = Core.ResourceStore.FindResourcesWithProp( FilterManagerProps.RuleResName, "IsActionFilter" );
            IResourceList passive = Core.ResourceStore.FindResourcesWithProp( null, "RuleTurnedOff" );
            rules = rules.Minus( passive );

            rules.Sort( new SortSettings( Core.Props.Order, true ) );

            IResourceList result = Core.ResourceStore.EmptyResourceList;
            foreach( IResource rule in rules )
            {
                if( FilterRegistry.HasQueryCondition( rule ) == isQueryRule )
                    result = result.Union( rule.ToResourceList(), true );
            }

            return result;
        }

        #region Actions Enumeration
        private delegate void StringParamMethodInvoker( string name );

        public void  ApplyActions( IResource rule, IResource res )
        {
            #region Preconditions
            if( rule == null )
                throw new ArgumentNullException( "rule", "FilterRegistry -- Rule resource is null" );

            if( res == null )
                throw new ArgumentNullException( "res", "FilterRegistry -- Input resource is null" );
            #endregion Preconditions

            IResourceList  actions = rule.GetLinksOfType( FilterManagerProps.RuleActionResName, _props._linkedActionsLink );
            foreach( IResource actionRes in actions )
            {
                try
                {
                    ProcessAction( actionRes, res );
                }
                catch( Exception )
                {
                    Core.UIManager.QueueUIJob( new StringParamMethodInvoker( ShowMessageBox ), rule.GetStringProp( Core.Props.Name ) );
                }
            }
        }

        private void  ProcessAction( IResource actionRes, IResource res )
        {
            string name;
            if( FilterRegistry.isProxyRuleAction( actionRes ))
            {
                IResource template = actionRes.GetLinkProp( _props._templateLink );
                if( template == null )
                    throw new ApplicationException( "FilterRegistry -- Template link is broken." );
                name = template.GetStringProp( Core.Props.Name );
            }
            else
                name = actionRes.GetStringProp( Core.Props.Name );

            IRuleAction action = (IRuleAction)FilterRegistry.RuleActions[ name ];
            if ( action != null )
            {
                action.Exec( res, new ActionParameterStore( actionRes ));
            }
            else
            {
                Trace.WriteLine( "*** FilterRegistry --     Action [" + name + "] is not registered" );
            }
        }
        #endregion Actions Enumeration
        #endregion ExecRules/Actions

        #region ExecCondition: ListContext
        private IResourceList   ExecCondition( IResource condition, SelectionType mode,
                                               string resTypes, string resLinks )
        {
            #region Preconditions
            if( condition == null )
                throw new ArgumentException( "FilterRegistry -- Input condition can not be null" );

            if( condition.Type != FilterManagerProps.ConditionResName )
                throw new ArgumentException( "FilterRegistry -- Input parameter is not of [SearchCondition] type" );
            #endregion Preconditions

            IResourceList   result = _store.EmptyResourceList;
            try
            {
                result = ExecConditionImpl( condition, mode, resTypes, resLinks );
            }
            catch( Exception e )
            {
                IResourceList parentViews = condition.GetLinksOfType( null, _props._linkedConditionsLink ).Union(
                                            condition.GetLinksOfType( null, _props._linkedExceptionsLink ));
                if( parentViews.Count == 1 )
                {
                    new ResourceProxy( parentViews[ 0 ] ).SetProp(
                            Core.Props.LastError, "Internal error has occured while executing view \"" +
                            parentViews[ 0 ].DisplayName + "\". " );
                    Trace.WriteLine( "FilterRegistry -- exception caught while executing view [" +
                                     parentViews[ 0 ].DisplayName + "] with reason [" + e.Message + "]." );
                }
            }
            return result;
        }

        //---------------------------------------------------------------------
        //  In the case when resource type is a set of several ones, apply the
        //  condition to every type in turn and union the results.
        //  Resource type can be of two three types:
        //  1. ordinal resource type
        //  2. link resource type
        //  3. formatted file resource type.
        //---------------------------------------------------------------------
        private IResourceList  ExecConditionImpl( IResource condition, SelectionType mode,
                                                  string resTypeStr, string resLinks )
        {
            IResourceList   result = _store.EmptyResourceList;
            IResourceList   linkedResult = _store.EmptyResourceList;
            if( resTypeStr == null && resLinks == null )
                result = ExecConditionOnType( condition, mode, null );
            else
            {
                string[]  formats, resTypes;
                string[]  linkTypes = null;
                if ( resLinks != null )
                {
                    linkTypes = resLinks.Split( '|' );
                }
                ResourceTypeHelper.ExtractFormatFields( resTypeStr, out resTypes, out formats );

                for( int i = 0; i < resTypes.Length; i++ )
                {
                    result = result.Union( ExecConditionOnType( condition, mode, resTypes[ i ] ), true );
                }

                if ( linkTypes != null )
                {
                    for( int i = 0; i < linkTypes.Length; i++ )
                    {
                        IResourceList temp = ExecConditionOnType( condition, mode, null );
                        IResourceList thoseHavingLinks = _store.FindResourcesWithPropLive( null, linkTypes[ i ] );
                        temp = temp.Intersect( thoseHavingLinks, true );
                        linkedResult = linkedResult.Union( temp, true );
                    }
                }
                if( formats.Length > 0 )
                {
                    IResourceList linkedRestr = _store.EmptyResourceList;
                    foreach( string type in formats )
                    {
                        linkedRestr = linkedRestr.Union(
                            linkedResult.Intersect( _store.GetAllResources( type ), true ), true );
                    }
                    linkedResult = linkedRestr;
                }

                result = result.Union( linkedResult, true );
            }
            return( result );
        }

        //---------------------------------------------------------------------
        //  Input property name can be of three types - cyclic, compound and single.
        //
        //  Single names refer directly to the objects which restrict the
        //  condition visibility (denoted by "resType" parameter).
        //
        //  Compound names use "dot" ('>') notation and allow to refer to resources,
        //  linked to the basic resources by links. In such case - apply
        //  condition to the most deep property in the chain and get the basic
        //  list by collecting back-referenced objects.
        //
        //  Cyclic properties make sence only in conjunction with "In" operation.
        //  Their semantics becomes not just to check the linked resource to the
        //  set defined in the condition, but to check all the path through the
        //  linked resources.
        //
        //  NB: custom properties are processed "as is"
        //---------------------------------------------------------------------

        private IResourceList  ExecConditionOnType( IResource condition, SelectionType mode, string resType )
        {
            IResourceList   result = _store.EmptyResourceList;

            if( condition.GetStringProp( "ConditionType" ) == "custom" )
            {
                result = ExecCustomCondition( condition, resType );
            }
            else
            {
                string  propName = condition.GetStringProp( _props._appliedToNameProp );
                ConditionOp op = (ConditionOp)condition.GetIntProp( _props._opProp );

                if(( propName.IndexOf( '*' ) == propName.Length - 1 ) &&
                   ( op != ConditionOp.QueryMatch ))
                {
                    Debug.Assert( op == ConditionOp.In );
                    propName = propName.Remove( propName.Length - 1, 1 );
                    result = ExecDirectSetOnCyclicProperty( condition, propName );
                }
                else
                if(( propName.IndexOf( cPropertyPathDelimiter ) != -1 ) &&
                   ( op != ConditionOp.QueryMatch ))
                {
                    string[]    propPath = propName.Split( cPropertyPathDelimiter );
                    IResourceList indirectList = ExecConditionOnProperty( condition, mode, null, propPath[ 1 ] );

                    foreach( IResource res in indirectList )
                    {
                        result = result.Union( res.GetLinksOfTypeLive( resType, propPath[ 0 ] ), true );
                    }
                }
                else
                    result = ExecConditionOnProperty( condition, mode, resType, propName );
            }

            return( result );
        }

        private IResourceList  ExecCustomCondition( IResource condition, string resType )
        {
            IResourceList result;
            if( condition.HasProp( "InternalView" ) )
            {
                IResource template = condition.GetLinkProp( _props._templateLink );
                string  name = template.GetStringProp( "DeepName" );
                object  handler = FilterRegistry.CustomConditions[ name ];
                if( handler == null )
                    throw new ApplicationException( "FilterRegistry -- Custom ConditionTemplate handler [" + name + "] has not been initialized." );
                if( !( handler is ICustomConditionTemplate ))
                    throw new ApplicationException( "FilterRegistry -- Non-internal condition template of Custom type does not support ICustomConditionTemplate interface." );

                ICustomConditionTemplate  exec = (ICustomConditionTemplate) handler;
                result = exec.Filter( resType, new ActionParameterStore( condition ) );
            }
            else
            {
                string  name = condition.GetStringProp( Core.Props.Name );
                object  handler = FilterRegistry.CustomConditions[ name ];
                if( handler == null )
                    throw new ApplicationException( "FilterRegistry -- Custom Condition handler [" + name + "] has not been initialized." );
                if( !( handler is ICustomCondition ))
                    throw new ApplicationException( "FilterRegistry -- Non-internal condition of Custom type does not support ICustomCondition interface." );

                ICustomCondition  conditionExec = (ICustomCondition) handler;
                result = conditionExec.Filter( resType );
            }
            return result;
        }

        private IResourceList  ExecConditionOnProperty( IResource condition, SelectionType mode,
                                                        string resType, string prop )
        {
            string          condType = condition.GetStringProp( "ConditionType" );
            ConditionOp     op = (ConditionOp)condition.GetIntProp( _props._opProp );
            IResourceList   result = _store.EmptyResourceList;
            string          normProp = (prop[ 0 ] == '-') ? prop.Substring( 1, prop.Length - 1 ) : prop;

            if(( op == ConditionOp.QueryMatch ) || ( _store.PropTypes.Exist( new string[]{ normProp } )))
            {
                if(( condType != "predicate" ) && ( condType != "direct-set" ) &&
                   ( _store.PropTypes [ normProp ].DataType == PropDataType.Link ))
                    throw new InvalidOperationException( "Condition is applied indirectly to the referenced object" );

                try
                {
                    if( condType == "predicate" )
                        result = ExecPredicateCondition( condition, mode, resType, prop );
                    else
                    if( condType == "standard" )
                        result = ExecStandardCondition( condition, mode, resType, prop );
                    else
                    if( condType == "standard-set" )
                        result = ExecSetCondition( condition, mode, resType, prop );
                    else
                    if( condType == "direct-set" )
                        result = ExecDirectSetCondition( condition, resType, prop );
                    else
                    if( condType == "standard-range" )
                        result = ExecRangeCondition( condition, mode, resType, prop );
                    else
                        throw new NotSupportedException( "Not supported type of condition - [" + condType + "]" );
                }
                catch( Exception exc )
                {
                    Trace.WriteLine( "FilterRegistry -- Exception while processing " + condType + " condition: " + exc );
                    IResource baseView = condition.GetLinkProp( _props._linkedConditionsLink );
                    if( baseView == null )
                        baseView = condition.GetLinkProp( _props._linkedExceptionsLink );
                    if( baseView != null )
                    {
                        new ResourceProxy( baseView ).SetProp( Core.Props.LastError, "Error occured while executing the view \"" +
                                                               baseView.DisplayName + "\"." );
                    }
                }
            }
            return( result );
        }

        #region Execution By Operation Type
        private IResourceList ExecPredicateCondition( IResource condition, SelectionType mode,
                                                      string resType, string prop )
        {
            ConditionOp  op = (ConditionOp)condition.GetIntProp( _props._opProp );
            #region Preconditions
            if( op != ConditionOp.QueryMatch && op != ConditionOp.HasLink &&
                op != ConditionOp.HasProp && op != ConditionOp.HasNoProp )
                throw new InvalidOperationException( "Unknown type of predicate condition" );
            if( op == ConditionOp.HasNoProp && resType == null )
                throw new ArgumentNullException( "resType", "FilterRegistry -- Accidental NULL value of resource type in HasNoProp condition." );
            #endregion Preconditions

            IResourceList  result;
            if( op == ConditionOp.QueryMatch )
                result = ExecQueryCondition( condition, resType );
            else
            {
                int propId;
                if( op == ConditionOp.HasProp || op == ConditionOp.HasNoProp )
                    propId = _store.PropTypes[ prop ].Id;
                else
                if( prop[ 0 ] != '-' )
                    propId = _store.PropTypes[ prop ].Id;
                else
                {
                    string par = prop.Substring( 1, prop.Length - 1 );
                    propId = -_store.PropTypes[ par ].Id;
                }

                result = _store.FindResourcesWithProp( mode, resType, propId );
                if( op == ConditionOp.HasNoProp )
                    result = _store.GetAllResourcesLive( resType ).Minus( result );
            }

            return( result );
        }
        private IResourceList ExecQueryCondition( IResource condition, string resType )
        {
            IResourceList   result = _store.EmptyResourceList;
            if( Core.TextIndexManager.IsIndexPresent() )
            {
                string query = FilterRegistry.ConstructQuery( condition );
                _lastQueryError = null;

                result = Core.TextIndexManager.ProcessQuery( query, null, out Highlighter, out _lastStopList, out _lastQueryError );
                if( resType != null )
                    result = result.Intersect( _store.GetAllResourcesLive( resType ), true);
            }
            return result;
        }

        private IResourceList ExecStandardCondition( IResource condition, SelectionType mode,
                                                     string resType, string propName )
        {
            string      propValue = condition.GetStringProp( _props._conditionValProp );
            int         propID = _store.GetPropId( propName );
            ConditionOp op = (ConditionOp)condition.GetIntProp( _props._opProp );
            IResourceList   result = _store.EmptyResourceList;

            if( op == ConditionOp.Eq )
            {
                result = ExecEqualityCondition( mode, propName, resType, new string[ 1 ] { propValue } );
            }
            else
            if( op == ConditionOp.Gt || op == ConditionOp.Lt )
            {
                object  propValueCasted, propMaxValueCasted, propMinValueCasted;
                CastValueToPropertyType( propName, propValue, out propValueCasted, out propMaxValueCasted, out propMinValueCasted );

                //  If property type is int we have to increment (decrement) interval
                //  margin to make the inequality correct.
                bool    typeIsInt = propValueCasted is int;

                if( propValueCasted != null )
                {
                    if( op == ConditionOp.Gt )
                    {
                        result = _store.FindResourcesInRange( mode, resType, propID,
                            typeIsInt ? (int)propValueCasted + 1 : propValueCasted, propMaxValueCasted );
                    }
                    else
                    {
                        result = _store.FindResourcesInRange( mode, resType, propID, propMinValueCasted,
                            typeIsInt ? (int)propValueCasted - 1 : propValueCasted );
                    }
                }
            }
            else
            if( op == ConditionOp.Has )
            {
                List<int> ids = null;
                result = _store.FindResourcesWithProp( mode, resType, propName );
                lock( result )
                {
                    if ( result.Count > 0 )
                    {
                        ids = new List<int>();
                        foreach( IResource res in result )
                        {
                            string  resPropVal = res.GetStringProp( propName );
                            if( Utils.IndexOf( resPropVal, propValue, true ) >= 0 )
                                ids.Add( res.Id );
                        }
                    }
                }
                result = (ids != null) ? _store.ListFromIds( ids, true ) :
                                         _store.EmptyResourceList;
            }
            else
                throw new NotSupportedException( "FilterRegistry -- Unknown type of operation" + op + " in the Triple condition " + condition.DisplayName );

            return( result );
        }

        private IResourceList ExecRangeCondition( IResource condition, SelectionType mode,
                                                  string resType, string propName )
        {
            string  propLower = condition.GetStringProp( _props._conditionValLowerProp );
            string  propUpper = condition.GetStringProp( _props._conditionValUpperProp );
            int     propID = _store.GetPropId( propName );
            object  propLowerCasted = CastValueToPropertyType( propName, propLower );
            object  propUpperCasted = CastValueWithRangeSpec( propName, propUpper, propLowerCasted );
            IResourceList   result = _store.EmptyResourceList;

            if(( propLowerCasted != null ) && ( propUpperCasted != null ))
                result = _store.FindResourcesInRange( mode, resType, propID, propLowerCasted, propUpperCasted );

            return( result );
        }

        private IResourceList ExecSetCondition( IResource condition, SelectionType mode, string resType, string propName )
        {
            ConditionOp op = (ConditionOp)condition.GetIntProp( _props._opProp );
            Debug.Assert( op == ConditionOp.In || op == ConditionOp.Eq || op == ConditionOp.Has,
                          "Mismatch between operation type and condition type" );

            string[]        valuesSet = CollectSetValuesFromCondition( condition );
            IResourceList   result = ExecEqualityCondition( mode, propName, resType, valuesSet );
            return( result );
        }

        private IResourceList  ExecEqualityCondition( SelectionType mode, string propName,
                                                      string resType, string[] propValues )
        {
            int             resID = _store.GetPropId( propName );
            IResourceList   result = Core.ResourceStore.EmptyResourceList;
            for( int i = 0; i < propValues.Length; i++ )
            {
                result = result.Union( _store.FindResources(
                    mode, resType, resID, CastValueToPropertyType( propName, propValues[ i ] ) ), true );
            }

            return( result );
        }

        private IResourceList  ExecDirectSetCondition( IResource condition, string resType, string propName )
        {
            ConditionOp op = (ConditionOp)condition.GetIntProp( _props._opProp );
            Debug.Assert( op == ConditionOp.In || op == ConditionOp.Eq || op == ConditionOp.Has,
                          "Mismatch between operation type and condition type" );

            IResourceList   result = _store.EmptyResourceList;
            IResourceList   setResources = condition.GetLinksOfType( null, _props._setValueLinkProp );

            foreach( IResource res in setResources )
            {
                result = result.Union( res.GetLinksOfTypeLive( resType, propName ), true );
            }
            return( result );
        }

        private IResourceList  ExecDirectSetOnCyclicProperty( IResource condition, string linkName )
        {
            IResourceList children;
            IResourceList result = children = condition.GetLinksOfTypeLive( null, _props._setValueLinkProp );

            foreach( IResource res in children )
            {
                result = result.Union( CollectLinkedResources( res, linkName ), true );
            }
            return result;
        }

        private static IResourceList  CollectLinkedResources( IResource root, string linkName )
        {
            IResourceList children;
            IResourceList result = children = root.GetLinksToLive( null, linkName );

            foreach( IResource res in children )
            {
                result = result.Union( CollectLinkedResources( res, linkName ), true );
            }
            return result;
        }
        #endregion
        #endregion ExecCondition: ListContext

        #region ExecCondition: Scalar Context
        //---------------------------------------------------------------------
        //  In the case when resource type is a set of several ones, apply the
        //  condition to every type in turn and union the results
        //---------------------------------------------------------------------
        private bool  MatchCondition( IResource condition, IResource res, string resTypeCompound, string resLinks )
        {
            bool  result;

            //  Check not only null strings but also empty ones for consistency
            //  with the bug in previous version.
            if( resLinks == null && string.IsNullOrEmpty( resTypeCompound ) )
                result = MatchConditionOnResource( condition, res );
            else
            {
                string[]  formats, resTypes;
                string[]  linkTypes = null;
                if (resLinks != null)
                {
                    linkTypes = resLinks.Split( '|' );
                }
                ResourceTypeHelper.ExtractFormatFields( resTypeCompound, out resTypes, out formats );

                result = (Array.IndexOf( resTypes, res.Type ) != -1) && MatchConditionOnResource( condition, res );

                if ( linkTypes != null )
                {
                    for( int i = 0; i < linkTypes.Length; i++ )
                        result = result || ( res.GetLinkCount( linkTypes[ i ] ) > 0 );
                }

                if( formats.Length > 0 )
                    result = result && (Array.IndexOf( formats, res.Type ) != -1);
            }

            return( result );
        }

        private bool  MatchConditionOnResource( IResource condition, IResource res )
        {
            bool    result = false;

            if( condition.GetStringProp( "ConditionType" ) == "custom" )
            {
                result = MatchCustomConditionOnResource( condition, res );
            }
            else
            {
                string      propName = condition.GetStringProp( _props._appliedToNameProp );
                ConditionOp op = (ConditionOp)condition.GetIntProp( _props._opProp );

                if(( propName.IndexOf( '*' ) == propName.Length - 1 ) &&
                   ( op != ConditionOp.QueryMatch ))
                {
                    Debug.Assert( op == ConditionOp.In );
                    propName = propName.Remove( propName.Length - 1, 1 );
                    result = MatchDirectSetOnCyclicProperty( condition, propName, res );
                }
                else
                if(( propName.IndexOf( cPropertyPathDelimiter ) != -1 ) &&
                   ( op != ConditionOp.QueryMatch ))
                {
                    string[]    propPath = propName.Split( cPropertyPathDelimiter );

                    IResourceList  linked = res.GetLinksOfType( null, propPath[ 0 ] );
                    for( int i = 0; i < linked.Count; i++ )
                        result |= MatchConditionOnProperty( condition, propPath[ 1 ], linked[ i ] );
                }
                else
                    result = MatchConditionOnProperty( condition, propName, res );
            }

            return( result );
        }

        private bool  MatchCustomConditionOnResource( IResource condition, IResource res )
        {
            if( condition.HasProp( "InternalView" ) )
            {
                IResource template = condition.GetLinkProp( _props._templateLink );
                string  name = template.GetStringProp( "DeepName" );
                object  handler = FilterRegistry.CustomConditions[ name ];
                if( handler == null )
                    throw new ApplicationException( "FilterRegistry -- Custom ConditionTemplate handler [" + name + "] has not been initialized." );
                if( !( handler is ICustomConditionTemplate ))
                    throw new ApplicationException( "FilterRegistry -- Non-internal condition template of Custom type does not support ICustomConditionTemplate interface." );

                ICustomConditionTemplate  exec = (ICustomConditionTemplate) handler;
                return exec.MatchResource( res, new ActionParameterStore( condition ) );
            }
            else
            {
                string name = condition.GetStringProp( Core.Props.Name );
                ICustomCondition  conditionExec = (ICustomCondition)FilterRegistry.CustomConditions[ name ];
                if( conditionExec == null )
                    throw new ApplicationException( "FilterRegistry -- No execution handler found for [" + name + "]." );

                return conditionExec.MatchResource( res );
            }
        }

        private bool  MatchConditionOnProperty( IResource condition, string prop, IResource res )
        {
            bool    result = false;
            string  conditionType = condition.GetStringProp( "ConditionType" );
            string  normProp = (prop[ 0 ] == '-') ? prop.Substring( 1, prop.Length - 1 ) : prop;

            if( condition.GetIntProp( _props._opProp ) == (int)ConditionOp.QueryMatch ||
                _store.PropTypes.Exist( normProp ) )
            {
                try
                {
                    if( conditionType == "predicate" )
                        result = MatchPredicateCondition( condition, prop, res );
                    else
                    if( res.HasProp( normProp ))
                    {
                        if( conditionType == "standard" )
                            result = MatchStandardCondition( condition, prop, res );
                        else
                        if( conditionType == "standard-set" )
                            result = MatchSetCondition( condition, prop, res );
                        else
                        if( conditionType == "standard-range" )
                            result = MatchRangeCondition( condition, prop, res );
                        else
                        if( conditionType == "direct-set" )
                            result = MatchDirectSetCondition( condition, prop, res );
                        else
                            throw new NotSupportedException( "Not supported type of condition - [" + conditionType + "] for property " + prop );
                    }
                    else
                    if( conditionType == "direct-set" )
                        result = MatchDirectSetCondition( condition, prop, res );
                }
                catch( Exception exc )
                {
                    Debug.Assert( false, "FilterRegistry -- Exception while processing condition (Debug purpose): " + exc.Message );
                    Core.ReportBackgroundException( exc );
                }
            }
            return( result );
        }

        private bool  MatchPredicateCondition( IResource condition, string prop, IResource res )
        {
            ConditionOp op = (ConditionOp)condition.GetIntProp( _props._opProp );

            #region Preconditions
            if( op != ConditionOp.HasLink && op != ConditionOp.HasProp &&
                op != ConditionOp.HasNoProp && op != ConditionOp.QueryMatch )
            {
                throw new InvalidOperationException( "Unknown type of predicate condition" );
            }
            #endregion Preconditions

            bool    result;
            if( op == ConditionOp.QueryMatch )
                result = MatchQueryCondition( condition, res );
            else
            {
                int propId;
                if( op == ConditionOp.HasProp || op == ConditionOp.HasNoProp || ( prop[ 0 ] != '-' ))
                    propId = _store.PropTypes[ prop ].Id;
                else
                {
                    string par = prop.Substring( 1, prop.Length - 1 );
                    propId = -_store.PropTypes[ par ].Id;
                }

                result = res.HasProp( propId );
                if( op == ConditionOp.HasNoProp )
                    result = !result;
            }

            return( result );
        }

        private static bool MatchQueryCondition( IResource condition, IResource res )
        {
//            bool result = false;
//            if( Core.TextIndexManager.IsIndexPresent() )
//            {
                string query = FilterRegistry.ConstructQuery( condition );
                bool result = Core.TextIndexManager.MatchQuery( query, res );
//            }
            return result;
        }

        private bool  MatchStandardCondition( IResource condition, string propName, IResource res )
        {
            bool        result = false;
            string      propValue = condition.GetStringProp( _props._conditionValProp );
            object      actualValue = res.GetProp( propName );
            ConditionOp op = (ConditionOp)condition.GetIntProp( _props._opProp );

            if( op == ConditionOp.Eq )
            {
                result = MatchEqualityCondition( res, propName, new string[ 1 ] { propValue } );
            }
            else
            if( op == ConditionOp.Gt || op == ConditionOp.Lt )
            {
                object  propValueCasted, propMaxValueCasted, propMinValueCasted;
                CastValueToPropertyType( propName, propValue, out propValueCasted, out propMaxValueCasted, out propMinValueCasted );
                if( propValueCasted != null )
                {
                    if( propValueCasted is DateTime )
                    {
                        if( op == ConditionOp.Gt )
                            result = ((DateTime)actualValue >= (DateTime)propValueCasted);
                        else
                            result = ((DateTime)actualValue <= (DateTime)propValueCasted);
                    }
                    else
                    if( propValueCasted is int )
                    {
                        if( op == ConditionOp.Gt )
                            result = ((int)actualValue >= (int)propValueCasted);
                        else
                            result = ((int)actualValue <= (int)propValueCasted);
                    }
                    else
                    if( propValueCasted is float )
                    {
                        if( op == ConditionOp.Gt )
                            result = ((float)actualValue >= (float)propValueCasted);
                        else
                            result = ((float)actualValue <= (float)propValueCasted);
                    }
                }
            }
            else
            if( op == ConditionOp.Has )
            {
                string  propActual = res.GetProp( propName ).ToString().ToLower();
                string  propValLower = propValue.ToLower();
                result = (propActual.IndexOf( propValLower ) != -1);
            }
            else
                throw new NotSupportedException( "Unknown type of operation in the Triple condition" );

            return( result );
        }

        private bool  MatchRangeCondition( IResource condition, string propName, IResource res )
        {
            string  propLower = condition.GetStringProp( _props._conditionValLowerProp );
            string  propUpper = condition.GetStringProp( _props._conditionValUpperProp );
            object  propLowerCasted = CastValueToPropertyType( propName, propLower );
            object  propUpperCasted = CastValueWithRangeSpec( propName, propUpper, propLowerCasted );
            bool    result = false;

            if( ( propLowerCasted != null ) && ( propUpperCasted != null ) && res.HasProp( propName ) )
            {
                object  actualValue = res.GetProp( propName );
                if( actualValue is DateTime )
                {
                    DateTime dt1 = (DateTime)propLowerCasted;
                    DateTime dt2 = (DateTime)propUpperCasted;
                    if ( dt1 < dt2 )
                        result = ((DateTime)actualValue >= dt1 ) && ((DateTime)actualValue <= dt2 );
                    else
                        result = ((DateTime)actualValue >= dt2 ) && ((DateTime)actualValue <= dt1 );
                }
                else
                if( actualValue is int )
                    result = ((int)actualValue >= (int)propLowerCasted ) &&
                             ((int)actualValue <= (int)propUpperCasted );
                else
                if( actualValue is float )
                    result = ((float)actualValue > (float)propLowerCasted ) &&
                             ((float)actualValue < (float)propUpperCasted );
            }

            return( result );
        }

        private bool MatchSetCondition( IResource condition, string propName, IResource res )
        {
            ConditionOp  op = (ConditionOp)condition.GetIntProp( _props._opProp );
            Debug.Assert( op == ConditionOp.In || op == ConditionOp.Eq || op == ConditionOp.Has,
                          "Mismatch between operation type and condition type" );

            string[]    valuesSet = CollectSetValuesFromCondition( condition );
            bool        result = MatchEqualityCondition( res, propName, valuesSet );
            return( result );
        }

        private bool  MatchDirectSetCondition( IResource condition, string propName, IResource res )
        {
            ConditionOp  op = (ConditionOp)condition.GetIntProp( _props._opProp );
            Debug.Assert( op == ConditionOp.In || op == ConditionOp.Eq,
                          "Mismatch between operation type and condition type" );

            IResourceList   possible = condition.GetLinksOfType( null, _props._setValueLinkProp );
            IResourceList   resValues = res.GetLinksOfType( null, propName );

            return( resValues.Intersect( possible, true ).Count > 0 );
        }

        private static bool  MatchEqualityCondition( IResource res, string propName, string[] propValues )
        {
            bool    result = false;
            string  propValue = res.GetProp( propName ).ToString();
            for( int i = 0; i < propValues.Length; i++ )
                result |= (propValue.ToLower() == propValues[ i ].ToLower());

            return( result );
        }

        private bool  MatchDirectSetOnCyclicProperty( IResource condition, string propName, IResource res )
        {
            bool    result = MatchDirectSetConditionDirected( condition, propName, res );
            if( !result )
            {
                IResourceList   parents = res.GetLinksFrom( null, propName );
                for( int i = 0; (i < parents.Count) && !result; i++ )
                    result |= MatchDirectSetOnCyclicProperty( condition, propName, parents[ i ] );
            }
            return result;
        }

        private bool  MatchDirectSetConditionDirected( IResource condition, string propName, IResource res )
        {
            ConditionOp  op = (ConditionOp)condition.GetIntProp( _props._opProp );
            Debug.Assert( op == ConditionOp.In || op == ConditionOp.Eq || op == ConditionOp.Has,
                          "Mismatch between operation type and condition type" );

            IResourceList   possible = condition.GetLinksOfType( null, _props._setValueLinkProp );
            IResourceList   resValues = res.GetLinksFrom( null, propName );

            return( resValues.Intersect( possible, true ).Count > 0 );
        }
        #endregion ExecCondition: Scalar Context

        #region Rule Events Registration
        public void  RegisterActivationEvent( string eventName, string displayName )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( eventName ) )
                throw new ArgumentNullException( "eventName", "FilterRegistry -- Event name is NULL or empty." );

            if( String.IsNullOrEmpty( displayName ) )
                throw new ArgumentNullException( "displayName", "FilterRegistry -- Display name of an event is NULL or empty." );

            if( RegisteredEvents.ContainsKey( eventName ) &&
               ((string)RegisteredEvents[ eventName ]) != displayName )
                throw new ArgumentException( "FilterRegistry -- Event name collision: another display name is already registered." );
            #endregion

            RegisteredEvents[ eventName ] = displayName;
        }

        public Hashtable  GetRegisteredEvents()
        {
            Hashtable copy = (Hashtable) RegisteredEvents.Clone();
            return copy;
        }

        //  Register resource type for which rules can be applied
        public void   RegisterRuleApplicableResourceType( string type )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( type ) )
                throw new ArgumentNullException( "type", "FilterRegistry -- Rule name can not be NULL or empty." );
            #endregion Preconditions

            if( Core.ResourceStore.FindResources( FilterRegistry.RuleApplicableResourceTypeResName, Core.Props.Name, type ).Count == 0 )
            {
                IResource res = Core.ResourceStore.BeginNewResource( FilterRegistry.RuleApplicableResourceTypeResName );
                res.SetProp( Core.Props.Name, type );
                res.EndUpdate();
            }
        }
        #endregion Rule Events Registration

        #region Casting
        //---------------------------------------------------------------------
        //  Cast condition value from "string" type to that defined by the value
        //  type of the resource property. If it is not possible to cast - return
        //  null. If possible, also get information about minimal and maximal
        //  possible value in the type value domain.
        //---------------------------------------------------------------------
        private object  CastValueToPropertyType( string propName, string propValue )
        {
            object  propValueCasted, FooMin, FooMax;
            return( CastValueToPropertyType( propName, propValue, out propValueCasted, out FooMin, out FooMax ) );
        }

        private object  CastValueToPropertyType( string propName, string propValue,
                                                 out object propValueCasted,
                                                 out object propMaxValueCasted,
                                                 out object propMinValueCasted )
        {
            string propValueLow = propValue.ToLower();
            propValueCasted = propMaxValueCasted = propMinValueCasted = "";
            PropDataType  type = _store.PropTypes [propName].DataType;
            RenewFixedDateAnchors();

            try
            {
                //  for link - comparison with DisplayName => string type
                if( type == PropDataType.String || type == PropDataType.Link )
                    propValueCasted = propValue;
                else
                if( type == PropDataType.Bool )
                {
                    propValueCasted = bool.Parse( propValue );
                    propMaxValueCasted = Int32.MaxValue;
                    propMinValueCasted = Int32.MinValue;
                }
                else
                if( type == PropDataType.Int )
                {
                    propValueCasted = Int32.Parse( propValue );
                    propMaxValueCasted = Int32.MaxValue;
                    propMinValueCasted = Int32.MinValue;
                }
                else
                if( type == PropDataType.Double )
                {
                    propValueCasted = double.Parse( propValue );
                    propMaxValueCasted = Double.MaxValue;
                    propMinValueCasted = Double.MinValue;
                }
                else
                if( type == PropDataType.Date )
                {
                    if( propValueLow == "today" )
                        propValueCasted = Today;
                    else
                    if( propValueLow == "yesterday" )
                        propValueCasted = Yesterday;
                    else
                    if( propValueLow == "tomorrow" )
                        propValueCasted = Tomorrow;
                    else
                    if( propValueLow == "weekstart" )
                        propValueCasted = WeekStart;
                    else
                    if( propValueLow == "nextweekstart" )
                        propValueCasted = NextWeekStart;
                    else
                    if( propValueLow == "monthstart" )
                        propValueCasted = MonthStart;
                    else
                    if( propValueLow == "yearstart" )
                        propValueCasted = YearStart;
                    else
                        propValueCasted = DateTime.Parse( propValue );
                    propMaxValueCasted = DateTime.MaxValue;
                    propMinValueCasted = DateTime.MinValue;
                }
            }
            catch( Exception )
            {
                Trace.WriteLine( "It is not possible to cast resource value [" + propValue + "] to the desired type" );
            }

            return( propValueCasted );
        }

        //---------------------------------------------------------------------
        //  Support absolute and relative values. Relative values start with
        //  "+" or "-". Unit of measurement is avalable for Datum ranges:
        //  "w" and "m". Non specified unit means days (for datum ranges only)
        //---------------------------------------------------------------------
        private object  CastValueWithRangeSpec( string propName, string propUpper, object propLowerCasted )
        {
            object  propValueCasted = null;
            propUpper = propUpper.TrimStart();

            //-----------------------------------------------------------------
            //  Currently for date doman we support units of measurements -
            //  hours, days, weeks and months (all have standard size, but months vary)
            //-----------------------------------------------------------------

            int  len = propUpper.Length;
            bool properSuffix = (len > 1 && Char.GetUnicodeCategory( propUpper[ len - 2 ] ) == UnicodeCategory.DecimalDigitNumber );
            bool isHoursUnit = propUpper.EndsWith( "h" ) && properSuffix;
            bool isMonthUnit = propUpper.EndsWith( "m" ) && properSuffix;
            bool isWeekUnit = propUpper.EndsWith( "w" ) && properSuffix;
            bool isYearUnit = propUpper.EndsWith( "y" ) && properSuffix;
            if( isHoursUnit || isWeekUnit || isMonthUnit || isYearUnit )
                propUpper = propUpper.Remove( propUpper.Length - 1, 1 );

            //-----------------------------------------------------------------
            //  For Date properties: Absolute or relative units?
            //-----------------------------------------------------------------
            if( !( propLowerCasted is DateTime ) ||
               ( propUpper.Length > 0 && propUpper[ 0 ] != '-'  &&  propUpper[ 0 ] != '+' ))
                propValueCasted = CastValueToPropertyType( propName, propUpper );
            else
            if( propLowerCasted != null )
            {
                try
                {
                    int     CastedInt = Int32.Parse( propUpper );
                    if( propLowerCasted is int )
                        propValueCasted = (int)propLowerCasted + CastedInt;
                    else
                    if( propLowerCasted is double )
                        propValueCasted = (double)propLowerCasted + (double)CastedInt;
                    else
                    if( propLowerCasted is DateTime )
                    {
                        if( isHoursUnit )
                        {
                            //  There is some limitation in hors processing, since
                            //  can process them only in "last XXX hours"-mode conditions
                            //  constructed automatically, where lower bound starts
                            //  from "Tomorrow" anchor.
                            if( CastedInt > 0 )
                                throw new ArgumentException( "FilterRegistry -- Current contract accepts only negative intervals for hours." );
                            if( (DateTime)propLowerCasted < DateTime.Now )
                                throw new ArgumentException( "FilterRegistry -- Current contract accepts only [Tomorrow] as the anchor for hour(s) ranges." );

                            TimeSpan ts = (DateTime)propLowerCasted - DateTime.Now;
                            propValueCasted = ((DateTime)propLowerCasted).AddHours( CastedInt ).AddHours( -ts.Hours );
                        }
                        else
                        {
                            if( isWeekUnit )
                                CastedInt *= 7;
                            else
                            if( isMonthUnit )
                                CastedInt = Month2Days( Today, CastedInt );
                            else
                            if( isYearUnit )
                                CastedInt *= 365;
                            propValueCasted = ((DateTime)propLowerCasted).AddDays( CastedInt );
                        }
                    }
                }
                catch( Exception )
                {
                    Trace.WriteLine( "FilterRegistry -- error in conversion of upper range value with lower=" +
                                     propLowerCasted + " and CastedInt=" + propUpper );
                }
            }

            return( propValueCasted );
        }
        #endregion Casting

        #region Misc
        private IResourceList GetResourcesOfType( string resTypeCompound, string resLinks )
        {
            IResourceList   result = _store.EmptyResourceList;

            if( resTypeCompound != null || resLinks != null )
            {
                string[]  formats, resTypes;
                string[]  linkTypes = null;
                if ( resLinks != null )
                {
                    linkTypes = resLinks.Split( '|' );
                }
                ResourceTypeHelper.ExtractFormatFields( resTypeCompound, out resTypes, out formats );

                IResourceList linkResult = _store.EmptyResourceList;
                for( int i = 0; i < resTypes.Length; i++ )
                {
                    result = result.Union( _store.GetAllResourcesLive( resTypes[ i ] ), true );
                }

                if ( linkTypes != null )
                {
                    for( int i = 0; i < linkTypes.Length; i++ )
                    {
                        linkResult = linkResult.Union( _store.FindResourcesWithPropLive( null, linkTypes[ i ] ), true );
                    }
                }

                if( formats.Length > 0 )
                {
                    IResourceList linkRestr = _store.EmptyResourceList;
                    foreach( string type in formats )
                    {
                        linkRestr = linkRestr.Union(
                            linkResult.Intersect( _store.GetAllResources( type ), true ), true );
                    }

                    linkResult = linkRestr;
                }
                result = result.Union( linkResult, true );
            }

            return result;
        }

        private void  RenewFixedDateAnchors()
        {
            Today = DateTime.Today;
            Yesterday = Today.AddDays( -1.0 );
            Tomorrow  = Today.AddDays( +1.0 );
            WeekStart = Today.AddDays( -((DateTime.Today.DayOfWeek == DayOfWeek.Sunday ) ? 7 : ((int)Today.DayOfWeek - 1)));
            NextWeekStart = Today.AddDays( (DateTime.Today.DayOfWeek == DayOfWeek.Sunday ) ? 1 : (8 - (int)Today.DayOfWeek));
            MonthStart = Today.AddDays( -Today.Day + 1 );
            YearStart = Today.AddDays( -Today.DayOfYear );
        }

        private int  Month2Days( DateTime today, int CastedInt )
        {
            int     dayInPeriod = 0;
            int     todaysMonth = today.Month; //  1..12
            CastedInt = (CastedInt >= 0) ? CastedInt : -CastedInt;
            for( int i = 1; i <= CastedInt; i++ )
            {
                int  monthBack = todaysMonth - i;
                if( monthBack < 0 )
                    monthBack += 12;
                dayInPeriod += DaysPerMonth[ monthBack ];
            }
            return( -dayInPeriod );
        }

        private string[]  CollectSetValuesFromCondition( IResource condition )
        {
            IResourceList   setValuesResources = condition.GetLinksOfType( null, _props._setValueLinkProp );
            Debug.Assert( setValuesResources.Count > 0, "Illegal number of set values in the condition - must be positive" );
            string[]        setValues = new string[ setValuesResources.Count ];

            for( int i = 0; i < setValuesResources.Count; i++ )
                setValues[ i ] = (string)(setValuesResources[ i ].GetProp( _props._conditionValProp ));

            return( setValues );
        }

        private static bool IsDummy( IResource condition )
        {
            return condition.GetStringProp( Core.Props.Name ) == FilterManagerStandards.DummyConditionName;
        }

        private static bool  isLinkPresent( string resLinks, IResource res )
        {
            bool        hasLink = false;
            string[]    links = resLinks.Split( '|' );
            foreach( string link in links )
                hasLink = hasLink || (res.GetLinksOfType( null, link ).Count > 0);
            return hasLink;
        }

        internal static string  VisualizeStopWords()
        {
            string summary = "";
            if( _lastStopList != null && _lastStopList.Length > 0 )
            {
                foreach( string str in _lastStopList )
                    summary += str + ", ";
                summary = summary.Remove( summary.Length - 2, 2 );

                if( _lastStopList.Length > 1 )
                    summary = "Words {" + summary + "} are stopwords and do not participate in the search";
                else
                    summary = "Word \"" + summary + "\" is a stopword and does not participate in the search";
            }
            return summary;
        }

        private static void ShowMessageBox( string ruleName )
        {
             MessageBox.Show( "FilterRegistry - Rule [" + ruleName + "] has broken actions. Please, reenter the rule." );
        }
        #endregion Misc
    }
}
