/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.FiltersManagement
{
    //-------------------------------------------------------------------------
    // Class which manages conditions, criteria and filters on the
    // available resources.
    //-------------------------------------------------------------------------

    public class FilterManagerProps : IFilterManagerProps
    {
        public const string  ViewResName               = "SearchView";
        public const string  ViewCompositeResName      = "ViewComposite";
        public const string  RuleResName               = "FilterRule";
        public const string  ViewFolderResName         = "ViewFolder";
        public const string  ConditionResName          = "SearchCondition";
        public const string  ConditionTemplateResName  = "ConditionTemplate";
        public const string  ConditionGroupResName     = "ConditionGroup";
        public const string  RuleActionResName         = "RuleAction";
        public const string  RuleActionTemplateResName = "RuleActionTemplate";
        public const string  RegisteredInitializer     = "RegisteredInitializer";
        public const string  ViewUnreadDeepName        = "Unread";
        public const string  ViewDeletedItemsDeepName  = "Deleted Resources";
        public const string  SearchResultsViewName     = "CurrentSearchResultsViewResource";
        public const string  ConjunctionGroup          = "ConjunctionGroup";
        public const string  SearchConditionSetValueName = "ConditionSetValue";

        public int SetValueLink     {  get {  return _setValueLinkProp;     } }
        public int TemplateLink     {  get {  return _templateLink;         } }

        public int OpProp           {  get {  return _opProp;               } }
        public int Invisible        {  get {  return _invisibleProp;        } }

        public int LinkedConditions {  get {  return _linkedConditionsLink; } }
        public int LinkedExceptions {  get {  return _linkedExceptionsLink; } }
        public int LinkedActions    {  get {  return _linkedActionsLink;    } }

        internal int   _setValueLinkProp;
        internal int   _linkedConditionsLink, _linkedExceptionsLink, _linkedActionsLink;
        internal int   _appliedToNameProp;
        internal int   _chooseFromResProp;
        internal int   _opProp;

        internal int   _conditionTypeProp;
        internal int   _conditionValProp;
        internal int   _conditionValLowerProp, _conditionValUpperProp;

        internal int   _linkUsedByRule;
        internal int   _templateLink;
        internal int   _invisibleProp;

        internal FilterManagerProps( IResourceStore store )
        {
            _setValueLinkProp = store.PropTypes.Register( "LinkedSetValue", PropDataType.Link, PropTypeFlags.Internal );
            _linkedActionsLink = store.PropTypes.Register( "LinkedAction", PropDataType.Link, PropTypeFlags.Internal );
            _linkedConditionsLink = store.PropTypes.Register( "LinkedCondition", PropDataType.Link, PropTypeFlags.Internal );
            _linkedExceptionsLink = store.PropTypes.Register( "LinkedNegativeCondition", PropDataType.Link, PropTypeFlags.Internal );

            _opProp = store.PropTypes.Register( "ConditionOp", PropDataType.Int, PropTypeFlags.Internal );
            _appliedToNameProp = store.PropTypes.Register( "ApplicableToProp", PropDataType.String, PropTypeFlags.Internal );
            _chooseFromResProp = store.PropTypes.Register( "ChooseFromResourceType", PropDataType.String, PropTypeFlags.Internal );
            _templateLink = store.PropTypes.Register( "TemplateLink", PropDataType.Link, PropTypeFlags.Internal | PropTypeFlags.DirectedLink );
            _invisibleProp = store.PropTypes.Register( "Invisible", PropDataType.Bool, PropTypeFlags.Internal );

            _linkUsedByRule = store.PropTypes.Register( "UsedByRule", PropDataType.Link, PropTypeFlags.DirectedLink );
            store.PropTypes.RegisterDisplayName( _linkUsedByRule, "Used In Rule", "Uses As Parameter" );

            _conditionTypeProp = store.PropTypes.Register( "ConditionType", PropDataType.String, PropTypeFlags.Internal );
            _conditionValProp = store.PropTypes.Register( "ConditionVal", PropDataType.String, PropTypeFlags.Internal );
            _conditionValLowerProp = store.PropTypes.Register( "ConditionValLower", PropDataType.String, PropTypeFlags.Internal );
            _conditionValUpperProp = store.PropTypes.Register( "ConditionValUpper", PropDataType.String, PropTypeFlags.Internal );
        }
    }

	public class FilterManager: IFilterManager
	{
        #region Ctor and Initialization
        public  FilterManager( IResourceStore store )
        {
            Store = store;
            RegisterTypes();
            _props = new FilterManagerProps( store );
            SetupMidnightViewUpdate();
        }

        public IStandardConditions Std
        {
            get { return StandardConditions; }
        }

	    public IFilterManagerProps Props
	    {
	        get
	        {
                if( _props == null )
                    throw new ApplicationException( "FilterManager (InternalError) -- Filter PROPS are used before initialization." );
	            return _props;
	        }
	    }

	    private static void SetupMidnightViewUpdate()
        {
            DateTime    startingTime = DateTime.Today.AddDays( 1.0 );
            Core.ResourceAP.QueueJobAt( startingTime, new MethodInvoker( SetupMidnightViewUpdate ) );
            _UIHandler.ResetSelection();
        }

        public void     InitializeCriteria()
        {
            Core.TextIndexManager.SetUpdateResultHandler( NextUpdateFinishedFromFullTextIndexer );
            IsVerbose = Core.SettingStore.ReadBool( "Rules", "Verbose", false );

            //  RegisterUnreadCallbacks must be called only after the text index has
            //  already been constructed. Otherwise, any calls to conditions with
            //  query searches will fail.
        }
        #endregion Ctor and Initialization

        #region Types and Props Registration
        private void  RegisterTypes()
        {
            Store.PropTypes.Register( "DeepName", PropDataType.String, PropTypeFlags.Internal );
            Store.PropTypes.Register( "TimingView", PropDataType.Int, PropTypeFlags.Internal );
            Store.PropTypes.Register( "IsDeletedResourcesView", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "SearchView", PropDataType.Int, PropTypeFlags.Internal );
            Store.PropTypes.Register( "IsSingleSelection", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "IsOnlyForRule", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "ShowInAllTabs", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "IsLiveMode", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "ContentLinks", PropDataType.String, PropTypeFlags.Internal );
            Store.PropTypes.Register( "InternalView", PropDataType.Int, PropTypeFlags.Internal );
            Store.PropTypes.Register( "DefaultSort", PropDataType.String, PropTypeFlags.Internal );
            Store.PropTypes.Register( "QueryRulesRerunRequired", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "RunToTabIfSingleTyped", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "ShowTotalItems", PropDataType.Bool, PropTypeFlags.Internal );

            Store.PropTypes.Register( "IsViewLinked", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "IsAdvSearchLinked", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "IsActionRuleLinked", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "IsFormRuleLinked", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "IsTrayRuleLinked", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "IsExpirationRuleLinked", PropDataType.Bool, PropTypeFlags.Internal );

            Store.ResourceTypes.Register( FilterManagerProps.ViewResName, "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            Store.ResourceTypes.Register( FilterManagerProps.ViewCompositeResName, "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            Store.ResourceTypes.Register( FilterManagerProps.ConditionResName, "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            Store.ResourceTypes.Register( FilterManagerProps.ConditionTemplateResName, "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            Store.ResourceTypes.Register( FilterManagerProps.ViewFolderResName, "View Folder", "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex | ResourceTypeFlags.ResourceContainer );
            Store.ResourceTypes.Register( FilterManagerProps.SearchConditionSetValueName, "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            Store.ResourceTypes.Register( FilterManagerProps.RegisteredInitializer, "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            Store.ResourceTypes.Register( FilterManagerProps.ConjunctionGroup, "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );

            Store.ResourceTypes.Register( FilterManagerProps.RuleResName, "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            Store.ResourceTypes.Register( FilterManagerProps.RuleActionResName, "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            Store.ResourceTypes.Register( FilterManagerProps.RuleActionTemplateResName, "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            Store.ResourceTypes.Register( RuleApplicableResourceTypeResName, "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            Store.ResourceTypes.Register( FilterManagerProps.ConditionGroupResName, "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex | ResourceTypeFlags.ResourceContainer );
            IResourceList list = Core.ResourceStore.GetAllResources( RuleApplicableResourceTypeResName );
            list.DeleteAll();

            int deepNameId = Store.GetPropId( "DeepName" );
            Store.RegisterUniqueRestriction( FilterManagerProps.ViewResName, Core.Props.Name );
            Store.RegisterUniqueRestriction( FilterManagerProps.ConditionResName, Core.Props.Name );
            Store.RegisterUniqueRestriction( FilterManagerProps.ConditionTemplateResName, Core.Props.Name );
            Store.DeleteUniqueRestriction  ( FilterManagerProps.ViewFolderResName, deepNameId );
            Store.RegisterUniqueRestriction( FilterManagerProps.ViewFolderResName, Core.Props.Name );
            Store.RegisterUniqueRestriction( FilterManagerProps.ConditionGroupResName, Core.Props.Name );

            Store.RegisterUniqueRestriction( FilterManagerProps.RuleResName, Core.Props.Name );
            Store.RegisterUniqueRestriction( FilterManagerProps.RuleActionResName, deepNameId );
            Store.RegisterUniqueRestriction( FilterManagerProps.RuleActionTemplateResName, Core.Props.Name );
            Store.RegisterUniqueRestriction( FilterManagerProps.ConditionResName, deepNameId );
            Store.RegisterUniqueRestriction( FilterManagerProps.ConditionTemplateResName, deepNameId );
            Store.RegisterUniqueRestriction( FilterManagerProps.RegisteredInitializer, Core.Props.Name );

            Store.PropTypes.Register( "SurfaceConditionVal", PropDataType.String, PropTypeFlags.Internal );

            Store.PropTypes.Register( "ExpirationRuleLink", PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.Internal );
            Store.PropTypes.Register( "ExpirationRuleOnDeletedLink", PropDataType.Link, PropTypeFlags.DirectedLink | PropTypeFlags.Internal );

            Store.PropTypes.Register( "ActionActivationTime", PropDataType.Int, PropTypeFlags.Internal );
            Store.PropTypes.Register( "EventName", PropDataType.String, PropTypeFlags.Internal );
            Store.PropTypes.Register( "IsActionFilter", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "IsFormattingFilter", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "IsExpirationFilter", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "IsTrayIconFilter", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "IconBlob", PropDataType.Blob, PropTypeFlags.Internal );
            Store.PropTypes.Register( "CountRestriction", PropDataType.Int, PropTypeFlags.Internal );

            Store.PropTypes.Register( "ShowContexts", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "ForceExec", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "RuleTurnedOff", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "IsQueryContained", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "DeleteRelatedContact", PropDataType.Bool, PropTypeFlags.Internal );
            Store.PropTypes.Register( "DisableDefaultGroupping", PropDataType.Bool, PropTypeFlags.Internal );

            //-----------------------------------------------------------------
            CreateConditionGroup( cAllConditionGroups );
            CreateConditionGroup( "Other" );

            //-----------------------------------------------------------------
            //  Register two standard (core) events with their display names.
            //-----------------------------------------------------------------
            RegisterActivationEvent( StandardEvents.ResourceReceived, "Resource is received" );
            RegisterActivationEvent( StandardEvents.CategoryAssigned, "Category is assigned" );

            if ( Core.ResourceStore.PropTypes.Exist( "SearchRank", "Proximity" ) )
            {
                IDisplayColumnManager colMgr = Core.DisplayColumnManager;
                colMgr.RegisterDisplayColumn( null, 11000, 
                    new ColumnDescriptor( "SearchRank", 50, ColumnDescriptorFlags.ShowIfNotEmpty ) );
                colMgr.RegisterDisplayColumn( null, 12000, 
                    new ColumnDescriptor( "Proximity", 70, ColumnDescriptorFlags.ShowIfNotEmpty ) );
            }

            Core.PluginLoader.RegisterResourceUIHandler( FilterManagerProps.ViewResName, _UIHandler );
            Core.PluginLoader.RegisterResourceUIHandler( FilterManagerProps.ViewFolderResName, _UIHandler );
			Core.PluginLoader.RegisterResourceDragDropHandler( FilterManagerProps.ViewResName, new SearchViewDragDropHandler() );
			Core.PluginLoader.RegisterResourceDragDropHandler( FilterManagerProps.ViewFolderResName, new ViewFolderDragDropHandler() );
        }
        #endregion Types and Props Registration

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
            Trace.WriteLine( "FilterManager (NextUpdateFinishedProcessor) -- Called by delegate for " + docIds.Length + " documents " );
            IResourceList   list = Core.ResourceStore.ListFromIds( docIds, false );
            if( list != null && list.Count > 0 )
            {
                ExecTIRules( list );
            }
        }
        #endregion DelegateRegistration

        #region Conditions
        #region QueryCondition
        //---------------------------------------------------------------------
        //  Register search condition using visual parameters, return the ID of
        //  the resource, created for this condition.
        //  Check that all operations get the corresponding number of parameters
        //---------------------------------------------------------------------
        public IResource   CreateQueryCondition( string name, string deepName, string[] types,
                                                 string query, string section )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( name ) )
                throw new ArgumentNullException( "name", "FilterManager -- Name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterManager -- Deep Name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( query ) )
                throw new ArgumentNullException( "query", "FilterManager -- Query string is NULL or empty" );
            #endregion Preconditions

            IResource condition = Store.FindUniqueResource( FilterManagerProps.ConditionResName, "DeepName", deepName );
            if( condition == null )
                condition = RecreateQueryCondition( name, deepName, types, query, section );
            return condition;
        }

        public IResource   RecreateQueryCondition( string name, string deepName, string[] types,
                                                   string query, string section )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( name ))
                throw new ArgumentNullException( "name", "FilterManager -- Name of a condition is NULL or empty." );
            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterManager -- Deep Name of a condition is NULL or empty." );
            if( String.IsNullOrEmpty( query ) )
                throw new ArgumentNullException( "query", "FilterManager -- Query string is NULL or empty" );
            #endregion Preconditions

            return RecreateStandardCondition( name, deepName, types, query, ConditionOp.QueryMatch, section );
        }
        #endregion QueryCondition

        #region StandardCondition
        //---------------------------------------------------------------------
        //  Do not create the condition if there is one with such name.
        //  NB: but if there are several of them - this is bug caused by resource
        //      store versioning. Remove all old versions of the condition.
        //---------------------------------------------------------------------
        public  IResource CreateStandardCondition( string name, string deepName, string[] resTypes,
                                                   string propName, ConditionOp op, params string[] val )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( name ))
                throw new ArgumentNullException( "name", "FilterManager -- Name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterManager -- Deep Name of a condition is NULL or empty." );
            #endregion Preconditions

            IResourceList condList = Store.FindResources( FilterManagerProps.ConditionResName, "DeepName", deepName );
            if( condList.Count > 1 )
                condList.DeleteAll();

            if ( condList.Count == 1 )
                return condList[ 0 ];

            return RecreateStandardCondition( name, deepName, resTypes, propName, op, val );
        }

        public  IResource CreateStandardCondition( string name, string deepName, string[] types,
                                                   string propName, ConditionOp op, IResourceList val )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( name ))
                throw new ArgumentNullException( "name", "FilterManager -- Name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterManager -- Deep Name of a condition is NULL or empty." );
            #endregion Preconditions

            IResource condition = Store.FindUniqueResource( FilterManagerProps.ConditionResName, "DeepName", deepName );
            if( condition == null )
                condition = RecreateStandardCondition( name, deepName, types, propName, op, val );
            return condition;
        }

        public  IResource RecreateStandardCondition( string name, string deepName, string[] types,
                                                     string propName, ConditionOp op, params string[] val )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( name ))
                throw new ArgumentNullException( "name", "FilterManager -- Name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterManager -- Deep Name of a condition is NULL or empty." );
            #endregion Preconditions

            //-----------------------------------------------------------------
            //  Remove the condition with such name if it exists. Thus, this
            //  method always overwrites existing resource. To create condition
            //  only if it does not exist, use "GetOrCreateStandardCondition"
            //-----------------------------------------------------------------
            IResource cond;
            try
            {
                cond = Store.FindUniqueResource( FilterManagerProps.ConditionResName, "DeepName", deepName );
                if( cond != null )
                    DeleteResource( cond );
            }
            catch( Exception )
            {
                //  possibly there are several conditions with such name. This
                //  happens when the system suddenly becomes "sad" because of
                //  crashes or internal bugs.
                IResourceList conds = Store.FindResources( FilterManagerProps.ConditionResName, "DeepName", deepName );
                foreach( IResource res in conds )
                    DeleteResource( res );
            }

            cond = RecreateStandardConditionImpl( types, propName, op, val );
            cond.SetProp( Core.Props.Name, name );
            cond.SetProp( "DeepName", deepName );
            return cond;
        }

        public IResource RecreateStandardCondition( string name, string deepName, string[] types,
                                                    string propName, ConditionOp op, IResourceList val )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( name ))
                throw new ArgumentNullException( "name", "FilterManager -- Name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterManager -- Deep Name of a condition is NULL or empty." );

            if( val == null )
                throw new ArgumentNullException( "val", "FilterManager -- List of Resource parameters can not be NULL." );

            if(( val.Count == 0 ) || ( op != ConditionOp.Eq && op != ConditionOp.In ))
                throw new ArgumentException( "FilterManager -- Number or type of parameters mismatches the type of Operation" );
            #endregion Preconditions

            //-----------------------------------------------------------------
            //  Remove the condition with such name if it exists. Thus, this
            //  method always overwrites existing resource. To create condition
            //  only if it does not exist, use "GetOrCreateStandardCondition"
            //-----------------------------------------------------------------
            IResource cond = Store.FindUniqueResource( FilterManagerProps.ConditionResName, "DeepName", deepName );
            if( cond != null )
                DeleteResource( cond );
            cond = RecreateStandardConditionImpl( types, propName, op, val );
            cond.SetProp( Core.Props.Name, name );
            cond.SetProp( "DeepName", deepName );

            return cond;
        }
        #endregion StandardCondition
        
        #region StandardConditionAux
        public  IResource CreateStandardConditionAux( string[] types, string propName,
                                                      ConditionOp op, params string[] val )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( propName ) )
                throw new ArgumentNullException( "propName", "FilterManager -- Property name can not be NULL or empty." );
            #endregion Preconditions

            IResource res = RecreateStandardConditionImpl( types, propName, op, val );
            SetProperty( res, "InternalView", 1 );
            return( res );
        }
        public  IResource CreateStandardConditionAux( string[] types, string propName,
                                                      ConditionOp op, IResourceList val )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( propName ) )
                throw new ArgumentNullException( "propName", "FilterManager -- Property name can not be NULL or empty." );
            #endregion Preconditions

            IResource res = RecreateStandardConditionImpl( types, propName, op, val );
            SetProperty( res, "InternalView", 1 );
            return( res );
        }
        public  IResource CreateQueryConditionAux( string[] types, string query, string sectionName )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( query ) )
                throw new ArgumentNullException( "query", "FilterManager -- Query can not be NULL or empty" );

            if( sectionName != null &&
                Store.FindUniqueResource( "DocumentSection", Core.Props.Name, sectionName ) == null )
                throw new ArgumentException( "Illegal name of the section " + sectionName + " while creating new QueryCondition" );
            #endregion Preconditions

            IResource res = RecreateStandardConditionImpl( types, query, ConditionOp.QueryMatch, sectionName );
            SetProperty( res, "InternalView", 1 );
            return( res );
        }
        #endregion StandardConditionAux

        #region Impl
        private IResource RecreateStandardConditionImpl( string[] types, string propName,
                                                         ConditionOp op, params string[] val )
        {
            #region Preconditions
            //  Property name must be valid string
            if( String.IsNullOrEmpty( propName ) )
                throw new ArgumentNullException( "propName", "FilterManager -- Property name is NULL or empty." );

            //  Cyclic properties can be used only in conjunction with
            //  ConditionOp.In operation
            if( propName.IndexOf( '*' ) == propName.Length - 1 &&
               ( op != ConditionOp.In && op != ConditionOp.QueryMatch ))
                throw new ArgumentException( "FilterManager -- Cyclic properties can be used only in conjunction with In operation" );

            //  "hasNoProp" conditions are not applied to NULL resource types
            if( op == ConditionOp.HasNoProp && ( types == null || types.Length == 0 ))
                throw new ArgumentException( "FilterManager -- [hasNoProp] operation cannot be applied to NULL resource types" );

            //  If present, all arguments in "val" array must be valid strings.
            //  Exception is made for Query conditions, which accept NULL as
            //  section name parameter.
            if( op != ConditionOp.QueryMatch )
            {
                foreach( string str in val )
                {
                    if( String.IsNullOrEmpty( str ))
                        throw new ArgumentNullException( "op", "FilterManager -- Each operation parameter must be a valid string (not NULL or empty)." );
                }
            }
            #endregion Preconditions

            ResourceProxy   proxy = ResourceProxy.BeginNewResource( FilterManagerProps.ConditionResName );
            proxy.SetProp( _props._opProp, (int)op );

            if(( val.Length == 0 ) && ( op == ConditionOp.HasLink ||
                 op == ConditionOp.HasProp || op == ConditionOp.HasNoProp ))
                CreateStandardPredicateCondition( proxy, propName );
            else
            if(( op == ConditionOp.QueryMatch ) && ( val.Length <= 1 ))
                CreateQueryCondition( proxy, propName, (val.Length == 1) ? val[ 0 ] : null );
            else
            if(( val.Length == 1 ) && ( op == ConditionOp.Eq ))
                CreateStandardTripleCondition( proxy, propName, val[ 0 ] );
            else
            if(( val.Length >= 1 ) && ( op == ConditionOp.Eq || op == ConditionOp.In ))
                CreateStandardSetCondition( proxy, propName, val );
            else
            if(( val.Length == 2 ) && ( op == ConditionOp.InRange ))
                CreateStandardRangeCondition( proxy, propName, val[ 0 ], val[ 1 ] );
            else
            if(( val.Length == 1 ) &&
               ( op == ConditionOp.Gt || op == ConditionOp.Lt || op == ConditionOp.Has ))
                CreateStandardTripleCondition( proxy, propName, val[ 0 ] );
            else
                throw new ArgumentException( "FilterManager -- Number of value parameters mismatches type of Operation" );
            AssociateConditionWithGroup( proxy, "Other" );
            SetApplicableResType( proxy, types );
            proxy.EndUpdate();

            return( proxy.Resource );
        }

        private IResource RecreateStandardConditionImpl( string[] types, string propName,
                                                         ConditionOp op, IResourceList val )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( propName ) )
                throw new ArgumentNullException( "propName", "FilterManager -- Name of a property is NULL or empty." );
            #endregion Preconditions

            ResourceProxy   proxy = ResourceProxy.BeginNewResource( FilterManagerProps.ConditionResName );
            proxy.SetProp( _props._opProp, (int)op );
            CreateDirectSetCondition( proxy, propName, val );
            AssociateConditionWithGroup( proxy, "Other" );
            SetApplicableResType( proxy, types );
            proxy.EndUpdate();

            return( proxy.Resource );
        }

        //---------------------------------------------------------------------
        //  Standard query condition is a condition with one operand (query 
        //  string) and the section specificator.
        //---------------------------------------------------------------------
        private void CreateQueryCondition( ResourceProxy proxy, string query, string sectionName )
        {
            proxy.SetProp( _props._conditionTypeProp, "predicate" );
            proxy.SetProp( _props._appliedToNameProp, query );

            if( sectionName != null )
            {
                if( sectionName.Length == 0 )
                    throw new ArgumentException( "FilterManager -- Section name length is 0." );

                IResource section = Store.FindUniqueResource( "DocumentSection", Core.Props.Name, sectionName );
                if( section == null )
                    throw new InvalidOperationException( "FilterManager -- Illegal name of the section " + sectionName + " in Filter Manager core" );

                int  sectionId = section.GetIntProp( "SectionOrder" );
                if( sectionId != 0 ) //  we do not process default section - whole text
                    SetProperty( proxy, Store.PropTypes[ "SectionOrder" ].Id, sectionId );
            }
        }

        //---------------------------------------------------------------------
        //  Standard triple condition is a condition with two operands and
        //  an operation (less, greater,... )
        //---------------------------------------------------------------------
        private static void  CreateStandardTripleCondition( ResourceProxy proxy,
                                                            string propName, string val )
        {
            proxy.SetProp( _props._conditionTypeProp, "standard" );
            proxy.SetProp( _props._appliedToNameProp, propName );
            proxy.SetProp( _props._conditionValProp, val );
        }

        //---------------------------------------------------------------------
        //  Standard range condition is a condition with three operands and 
        //  an operation InRange
        //---------------------------------------------------------------------
        private static void CreateStandardRangeCondition( ResourceProxy proxy, string propName,
                                                          string lower, string upper )
        {
            proxy.SetProp( _props._conditionTypeProp, "standard-range" );
            proxy.SetProp( _props._appliedToNameProp, propName );
            proxy.SetProp( _props._conditionValLowerProp, lower );
            proxy.SetProp( _props._conditionValUpperProp, upper );
        }

        //---------------------------------------------------------------------
        //  Standard set condition is a condition with two operands, of which
        //  the right one is a set and the operation is "in"
        //---------------------------------------------------------------------
        private static void CreateStandardSetCondition( ResourceProxy proxy, string propName,
                                                        params string[] values )
        {
            Debug.Assert( values.Length > 0, "Condition of type 'set' must have more than 0 parameters" );

            foreach( string Val in values )
            {
                ResourceProxy innerProxy = ResourceProxy.BeginNewResource( FilterManagerProps.SearchConditionSetValueName );
                innerProxy.SetProp( _props._conditionValProp, Val );
                innerProxy.EndUpdate();
                proxy.AddLink( _props.SetValueLink, innerProxy.Resource );
            }

            proxy.SetProp( _props._conditionTypeProp, "standard-set" );
            proxy.SetProp( _props._appliedToNameProp, propName );
        }

        private static void CreateDirectSetCondition( ResourceProxy proxy, string propName,
                                                      IResourceList values )
        {
            Debug.Assert( values.Count > 0, "Condition of type 'set' must have more than 0 parameters" );

            proxy.SetProp( _props._conditionTypeProp, "direct-set" );
            proxy.SetProp( _props._appliedToNameProp, propName );
            foreach( IResource res in values )
                proxy.AddLink( _props._setValueLinkProp, res );
        }

        //---------------------------------------------------------------------
        //  Predicate condition is a condition with one argument and a standard
        //  predicate (hasLink, hasProperty, hasNotProperty, conformQuery,...)
        //---------------------------------------------------------------------
        private static void  CreateStandardPredicateCondition( ResourceProxy proxy, string val )
        {
            proxy.SetProp( _props._conditionTypeProp, "predicate" );
            proxy.SetProp( _props._appliedToNameProp, val );
        }
        #endregion Impl

        #region CustomConditions
        //---------------------------------------------------------------------
        //  Custom condition is a condition which semantics is defined by the
        //  user-defined method somewhere inside the core or plugin.
        //---------------------------------------------------------------------
        public IResource RegisterCustomCondition( string name, string deepName,
                                                  string[] types, ICustomCondition filter )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( name ))
                throw new ArgumentNullException( "name", "FilterManager -- Name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterManager -- Deep Name of a condition is NULL or empty." );

            if( filter == null )
                throw new ArgumentNullException( "filter", "FilterManager -- ICustomCondition object is NULL." );
            #endregion Preconditions

            IResource condition;
            IResourceList list = Store.FindResources( FilterManagerProps.ConditionResName, "DeepName", deepName );
            if( list.Count == 0 )
            {
                condition = Store.BeginNewResource( FilterManagerProps.ConditionResName );
                condition.SetProp( _props._conditionTypeProp, "custom" );
                condition.SetProp( Core.Props.Name, name );
                condition.SetProp( "DeepName", deepName );
                condition.EndUpdate();
            }
            else
                condition = list[ 0 ];

            SetApplicableResType( condition, types );
            CustomConditions[ name ] = filter;

            return( condition );
        }
        #endregion CustomConditions

        public void  MarkConditionOnlyForRule( IResource condition )
        {
            #region Preconditions
            if( condition.Type != FilterManagerProps.ConditionResName && condition.Type != FilterManagerProps.ConditionTemplateResName )
                throw new ApplicationException( "FilterManager -- input resource type is not [Condition]" );
            #endregion Preconditions

            SetProperty( condition, "IsOnlyForRule", true );
        }

        public IResource  CloneCondition( IResource condition )
        {
            #region Preconditions
            if( condition.Type != FilterManagerProps.ConditionResName )
                throw new ApplicationException( "FilterManager -- input resource type is not [Condition]" );
            #endregion Preconditions

            //  If the condition is internal one - create a copy of it,
            //  otherwise return itself because all views and rules share
            //  non-internal conditions and do not create new copies of them.
            IResource result = condition;
            if( isInternal( condition ))
            {
                ResourceProxy proxy = ResourceProxy.BeginNewResource( FilterManagerProps.ConditionResName );
                proxy.BeginUpdate();
                AssignProperty( condition, proxy, "InternalView" );
                AssignProperty( condition, proxy, "ConditionOp" );
                AssignProperty( condition, proxy, "ConditionType" );
                AssignProperty( condition, proxy, "ApplicableToProp" );
                AssignProperty( condition, proxy, "SectionOrder" );
                AssignProperty( condition, proxy, "ConditionVal" );
                AssignProperty( condition, proxy, "ConditionValUpper" );
                AssignProperty( condition, proxy, "ConditionValLower" );
                CopyLinks( condition, proxy, _props._templateLink );

                //  Copy linked parameters - they are either linked directly
                //  or through resources-containers.
                if( condition.GetStringProp( _props._conditionTypeProp ) == "direct-set" )
                {
                    IResourceList linkedValues = condition.GetLinksOfType( null, _props._setValueLinkProp );
                    AddLinks( proxy, linkedValues, _props._setValueLinkProp );
                }
                else
                if( condition.GetStringProp( _props._conditionTypeProp ) == "standard-set" )
                {
                    IResourceList linkedValues = condition.GetLinksOfType( null, _props._setValueLinkProp );
                    IResource[]   newLinkedValues = new IResource[ linkedValues.Count ];
                    for( int i = 0; i < linkedValues.Count; i++ )
                    {
                        ResourceProxy newLinkedValue = ResourceProxy.BeginNewResource( FilterManagerProps.SearchConditionSetValueName );
                        newLinkedValue.SetProp( _props._conditionValProp, linkedValues[ i ].GetStringProp( _props._conditionValProp ) );
                        newLinkedValue.EndUpdate();
                        newLinkedValues[ i ] = newLinkedValue.Resource;
                    }
                    AddLinks( proxy, newLinkedValues, _props._setValueLinkProp, null );
                }
                proxy.EndUpdate();
                result = proxy.Resource;
            }
            return result;
        }

        public void  AssociateConditionWithGroup( IResource cond, string groupName )
        {
            #region Preconditions
            if( cond == null )
                throw new ArgumentNullException( "cond", "FilterManager -- Condition resource can not be NULL" );

            if( !Utils.IsValidString( groupName ) )
                throw new ArgumentNullException( "groupName", "FilterManager -- Invalid Group parameter passed - can not be NULL or empty." );

            if( cond.Type != FilterManagerProps.ConditionResName && cond.Type != FilterManagerProps.ConditionTemplateResName )
                throw new ArgumentException( "FilterManager -- Condition parameter has invalid type [" + cond.Type + "]" );
            #endregion Preconditions

            ResourceProxy proxy = new ResourceProxy( cond );
            proxy.BeginUpdate();
            AssociateConditionWithGroup( proxy, groupName );
            proxy.EndUpdate();
        }
        private void  AssociateConditionWithGroup( ResourceProxy proxy, string groupName )
        {
            IResource group = CreateConditionGroup( groupName );
            proxy.SetProp( Core.Props.Parent, group );
        } 
        #endregion

        #region ConditionTemplates
        //---------------------------------------------------------------------
        //  Condition template is a pattern for constructing of parameterized
        //  search conditions
        //---------------------------------------------------------------------
        public IResource   RecreateConditionTemplateWithUIHandler( string name, string deepName, string[] types,
                                                                   ITemplateParamUIHandler handler,
                                                                   ConditionOp op, params string[] parameters )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( name ))
                throw new ArgumentNullException( "name", "FilterManager -- Name of a condition can not be NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterManager -- Deep Name of a condition is NULL or empty." );

            if( handler == null )
                throw new ArgumentNullException( "handler", "FilterManager -- UI handler for the condition template is NULL." );
            #endregion Preconditions

            IResource template = RecreateConditionTemplate( name, deepName, types, op, parameters );
            RegisteredUIHandlers[ name ] = handler;

            return template;
        }

        public IResource   CreateConditionTemplateWithUIHandler( string name, string deepName, string[] types,
                                                                 ITemplateParamUIHandler handler,
                                                                 ConditionOp op, params string[] parameters )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( name ))
                throw new ArgumentNullException( "name", "FilterManager -- Name of a condition can not be NULL or empty." );

            if( !Utils.IsValidString( deepName ))
                throw new ArgumentNullException( "deepName", "FilterManager -- Deep Name of a condition is NULL or empty." );

            if( handler == null )
                throw new ArgumentNullException( "handler", "FilterManager -- UI handler for the condition template is NULL." );
            #endregion Preconditions

            IResource template = Store.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "DeepName", deepName );
            if( template == null )
                template = RecreateConditionTemplate( name, deepName, types, op, parameters );
            RegisteredUIHandlers[ name ] = handler;

            return template;
        }

        public IResource  CreateConditionTemplate( string name, string deepName, string[] types,
                                                   ConditionOp op, params string[] parameters )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( name ))
                throw new ArgumentNullException( "name", "FilterManager -- Name of a condition can not be NULL or empty" );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterManager -- Deep Name of a condition is NULL or empty." );
            #endregion Preconditions

            IResource template = Store.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "DeepName", deepName );
            if( template == null )
                template = RecreateConditionTemplate( name, deepName, types, op, parameters );

            return template;
        }

        public IResource  RecreateConditionTemplate( string name, string deepName, string[] types,
                                                     ConditionOp op, params string[] parameters )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( name ))
                throw new ArgumentNullException( "name", "FilterManager -- Name of a condition can not be NULL or empty" );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterManager -- Deep Name of a condition is NULL or empty." );

            //  If Op is not query, there MUST be at least one argument!!!
            if(( op != ConditionOp.QueryMatch ) && ( parameters == null ))
                throw new ArgumentNullException( "parameters", "FilterManager -- Operation for a template requires arguments." );

            //  Requires that all parameters are valid strings
            if( parameters != null )
            {
                foreach( string str in parameters )
                {
                    if( String.IsNullOrEmpty( str ))
                        throw new ArgumentNullException( "parameters", "FilterManager -- Each operation parameter must be a valid string (not NULL or empty)." );
                }
            }
            #endregion Preconditions

            //-----------------------------------------------------------------
            //  Remove the condition with such name if it exists. Thus, this
            //  method always overwrites existing resource. To create condition
            //  only if it does not exist, use "GetOrCreateStandardCondition"
            //-----------------------------------------------------------------
            IResource template = Store.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "DeepName", deepName );
            if( template != null )
                template.Delete();

            //-----------------------------------------------------------------
            template = Store.BeginNewResource( FilterManagerProps.ConditionTemplateResName );
            template.SetProp( Core.Props.Name, name );
            template.SetProp( "DeepName", deepName );
            template.DisplayName = name.Replace( "%", "" );
            template.SetProp( _props._opProp, (int)op );
            SetApplicableResType( template, types );

            if(( op == ConditionOp.QueryMatch ) && ( parameters.Length == 0 ))
            {}
            else
            if(( op == ConditionOp.Has ) && ( parameters.Length == 1 ))
            {
                template.SetProp( _props._appliedToNameProp, parameters[ 0 ] );
            }
            else
            if(( op == ConditionOp.QueryMatch ) && ( parameters.Length == 1 ))
            {
                //  caution! application can be run without text index!!!
                if( Store.ResourceTypes.Exist( "DocumentSection" ))
                {
                    uint sectionId = DocSectionHelper.OrderByFullName( parameters[ 0 ] );
                    template.SetProp( "SectionOrder", (int)sectionId );
                }
            }
            else
            if(( op == ConditionOp.In ) && ( parameters.Length == 2 ))
            {
                template.SetProp( _props._chooseFromResProp, parameters[ 0 ] );
                template.SetProp( _props._appliedToNameProp, parameters[ 1 ] );
            }
            else
            if(( op == ConditionOp.InRange ) && (( parameters.Length == 3 ) || ( parameters.Length == 1 )))
            {
                //  Check that we can apply range (comparison) operations
                //  to the data type behind the property.
                IPropType pt = Core.ResourceStore.PropTypes[ parameters[ 0 ] ];
                if( pt == null )
                    throw new ArgumentException( "FilterManager -- Property " + parameters[ 0 ] + " does not exist." );
                if( pt.DataType != PropDataType.Int && pt.DataType != PropDataType.Date )
                    throw new ArgumentException( "FilterManager -- Type of the property " + parameters[ 0 ] + " does not allow comparison operations." );

                template.SetProp( _props._appliedToNameProp, parameters[ 0 ] );
                if( parameters.Length == 3 )
                {
                    template.SetProp( _props._conditionValLowerProp, parameters[ 1 ] );
                    template.SetProp( _props._conditionValUpperProp, parameters[ 2 ] );
                }
                else
                {
                    template.SetProp( _props._conditionValLowerProp, Int32.MinValue.ToString() );
                    template.SetProp( _props._conditionValUpperProp, Int32.MaxValue.ToString() );
                }
            }
            else
            if(( op == ConditionOp.Eq ) && ( parameters.Length == 1 ))
            {
                template.SetProp( _props._appliedToNameProp, parameters[ 0 ] );
            }
            else
                throw new ArgumentException( "Operation [" + op + "] with such set of parameters is not supported now" );
            template.EndUpdate();

            return( template );
        }

        public IResource   CreateCustomConditionTemplate( string name, string deepName, string[] resTypes,
                                                          ICustomConditionTemplate filter, ConditionOp op,
                                                          params string[] values )
        {
            IResource template = CreateConditionTemplate( name, deepName, resTypes, op, values );
            CustomConditions[ deepName ] = filter;
            new ResourceProxy( template ).SetProp( _props._conditionTypeProp, "custom" );
            return template;
        }

        public static void  ReferCondition2Template( IResource res, string templateName )
        {
            #region Preconditions
            if( res == null )
                throw new ArgumentNullException( "res", "FilterManager -- Condition resource can not be NULL" );

            if( String.IsNullOrEmpty( templateName ))
                throw new ArgumentNullException( "templateName", "FilterManager -- Template name can not be NULL or empty" );

            IResource template = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, Core.Props.Name, templateName );
            if( template == null )
                throw new ArgumentException( "FilterManager -- No such condition template " + templateName + " for association" );
            #endregion Preconditions

            ReferCondition2Template( res, template );
        }

        public static void  ReferCondition2Template( IResource condition, IResource template )
        {
            SetProperty( condition, _props._templateLink, template );
        }

        public IResource  InstantiateConditionTemplate( IResource template, object param, string[] resTypes )
        {
            return FilterConvertors.InstantiateTemplate( template, param, null, resTypes );
        }

        public static ITemplateParamUIHandler GetUIHandler( string templateName )
        {
            #region Preconditions
            if( templateName == null )
                throw new ArgumentNullException( "templateName", "FilterManager -- Name of a template is NULL." );
            #endregion Preconditions

            return (ITemplateParamUIHandler) RegisteredUIHandlers[ templateName ];
        }
        #endregion ConditionTemplates

        #region Views
        //---------------------------------------------------------------------
        //  Search View is a named collection of search conditions,
        //  represented by links
        //---------------------------------------------------------------------
        public IResource RegisterView( string name, IResource[] conditions, IResource[] exceptions )
        {
            return RegisterView( name, null, conditions, exceptions );
        }

        public IResource RegisterView( string name, string[] types,
                                       IResource[] conditions, IResource[] exceptions )
        {
            IResource[][] groups = Convert2Group( conditions );
            return RegisterView( name, types, groups, exceptions );
        }

        public void   ReregisterView( IResource view, string name, string[] types,
                                      IResource[] conditions, IResource[] exceptions )
        {
            IResource[][] groups = Convert2Group( conditions );
            ReregisterView( view, name, types, groups, exceptions );
        }

        public IResource RegisterView( string name, string[] types,
                                       IResource[][] groups, IResource[] exceptions )
        {
            #region Preconditions
            if(( groups == null || groups.Length == 0 ) &&
               ( types == null || types.Length == 0 ))
                throw new ArgumentException( "FilterManager -- List of conditions can not be empty for NULL or empty list of resource types." );

            if( types != null )
            {
                bool allTypesValid = true;
                foreach( string str in types )
                {
                    allTypesValid = allTypesValid &&
                                    (Core.ResourceStore.ResourceTypes.Exist( str ) || Core.ResourceStore.PropTypes.Exist( str ));
                }
                if( !allTypesValid )
                    throw new ArgumentException( "FilterManager -- Resource types array contains illegal type." );
            }
            #endregion Preconditions

            ResourceProxy   proxy;
            IResource view = Store.FindUniqueResource( FilterManagerProps.ViewResName, Core.Props.Name, name );
            if( view != null )
            {
                proxy = new ResourceProxy( view );
                proxy.BeginUpdate();
            }
            else
                proxy = ResourceProxy.BeginNewResource( FilterManagerProps.ViewResName );

            InitializeView( proxy, name, types, groups, exceptions );
            return( proxy.Resource );
        }

        public void   ReregisterView( IResource view, string name, string[] types,
                                      IResource[][] groups, IResource[] exceptions )
        {
            #region Preconditions
            if( view == null )
                throw new ArgumentNullException( "view", "FilterManager -- View parameter is null." );

            if( !Utils.IsValidString( name ))
                throw new ArgumentException( "FilterManager -- Invalid view name" );

            if(( groups == null || groups.Length == 0 ) &&
               ( types == null || types.Length == 0 ))
                throw new ArgumentException( "FilterManager -- List of conditions and exceptions can not be NULL or empty for NULL or empty list of resource types." );

            if( types != null )
            {
                bool allTypesValid = true;
                foreach( string str in types )
                {
                    allTypesValid = allTypesValid &&
                                    (Core.ResourceStore.ResourceTypes.Exist( str ) || Core.ResourceStore.PropTypes.Exist( str ));
                }
                if( !allTypesValid )
                    throw new ArgumentException( "FilterManager -- Resource types array contains illegal type." );
            }
            #endregion Preconditions

            DeleteInternalConditions( view );

            ResourceProxy   proxy = new ResourceProxy( view );
            proxy.BeginUpdate();

            InitializeView( proxy, name, types, groups, exceptions );
            DeleteHangedConjunctionGroups();
        }

        public static void InitializeView( ResourceProxy proxy, string name, string[] types,
                                           IResource[] conditions, IResource[] exceptions )
        {
            IResource[][] groups = Convert2Group( conditions );
            InitializeView( proxy, name, types, groups, exceptions );
        }

        public static void InitializeView( ResourceProxy proxy, string name, string[] types,
                                           IResource[][] conditionGroups, IResource[] exceptions )
        {
            proxy.SetProp( Core.Props.Name, name );
            proxy.SetProp( "_DisplayName", name );
            proxy.SetProp( "DeepName", name );
            proxy.SetProp( "ForceExec", true );
            proxy.DeleteProp( Core.Props.LastError );
            proxy.DeleteLinks( _props._linkedConditionsLink ); // delete rest of links
            proxy.DeleteLinks( _props._linkedExceptionsLink ); 

            AddLinks( proxy, conditionGroups, _props._linkedConditionsLink, FilterManagerProps.ConditionResName );
            AddLinks( proxy, exceptions, _props._linkedExceptionsLink, FilterManagerProps.ConditionResName );

            if( IsAnyDateCondition( conditionGroups ) || IsAnyDateCondition( exceptions ))
                proxy.SetProp( "TimingView", 1 );
            else
                proxy.DeleteProp( "TimingView" );
            if( HasQueryCondition( conditionGroups ) || HasQueryCondition( exceptions ) )
                proxy.SetProp( "IsQueryContained", true );
            else
                proxy.DeleteProp( "IsQueryContained" );
            if( IsAnyDeletedResourcesCondition( conditionGroups ) )
                proxy.SetProp( "IsDeletedResourcesView", true );
            else
                proxy.DeleteProp( "IsDeletedResourcesView" );

            if( (proxy.Resource != null) && proxy.Resource.HasProp( "ShowInAllTabs" ) && (types != null) )
                proxy.DeleteProp( "ShowInAllTabs" );

            SetApplicableResType( proxy, types );
            proxy.EndUpdate();
        }

        public void  SetVisibleInAllTabs( IResource view )
        {
            #region Preconditions
            if( view == null )
                throw new ArgumentNullException( "view", "FilterManager -- View parameter is null." );
            if( view.Type != FilterManagerProps.ViewResName )
                throw new ArgumentException( "FilterManager -- View parameter has illegal type [" + view.Type + "]" );
            #endregion Preconditions

            new ResourceProxy( view ).SetProp( "ShowInAllTabs", true );
        }
        public bool  IsVisibleInAllTabs( IResource view )
        {
            #region Preconditions
            if( view == null )
                throw new ArgumentNullException( "view", "FilterManager -- View parameter is null." );
            if( view.Type != FilterManagerProps.ViewResName )
                throw new ArgumentException( "FilterManager -- View parameter has illegal type [" + view.Type + "]" );
            #endregion Preconditions

            return view.HasProp( "ShowInAllTabs" );
        }

        //---------------------------------------------------------------------
        public void  DeleteView( string viewName )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( viewName ) )
                throw new ArgumentNullException( "viewName", "FilterManager -- Name of a view can not be NULL" );
            #endregion Preconditions

            IResourceList views = Store.FindResources( SelectionType.Normal, FilterManagerProps.ViewResName, Core.Props.Name, viewName );
            foreach( IResource res in views )
                DeleteView( res );
        }
        public void  DeleteView( IResource view )
        {
            #region Preconditions
            if( view == null )
                throw new ArgumentNullException( "view", "FilterManager -- View resource can not be NULL" );
            #endregion Preconditions

            DeleteInternalConditions( view );
            DeleteResource( view );
            DeleteHangedConjunctionGroups();
        }

        public IResource CloneView( IResource from, string newName )
        {
            #region Preconditions
            if( from == null )
                throw new ArgumentNullException( "from", "FilterManager -- Source view is NULL." );

            if( String.IsNullOrEmpty( newName ) )
                throw new ArgumentNullException( "newName", "FilterManager -- New name is NULL or empty." );
            #endregion Preconditions

            IResource[][] conditions;
            IResource[]   exceptions;
            CloneConditionTypeLinks( from , out conditions, out exceptions );

            string[] formTypes = CompoundType( from );
            IResource newView = Core.FilterManager.RegisterView( newName, formTypes, conditions, exceptions );

            #region Copying of atomic properties
            ResourceProxy proxy = new ResourceProxy( newView );
            proxy.BeginUpdate();
            if( from.HasProp( "ShowDeletedItems" ) )
                proxy.SetProp( "ShowDeletedItems", true );
            if( from.HasProp( "ShowInAllTabs" ) )
                proxy.SetProp( "ShowInAllTabs", true );
            if( from.HasProp( "TimingView" ) )
                proxy.SetProp( "TimingView", 1 );
            if( from.HasProp( "IsQueryContained" ) )
                proxy.SetProp( "IsQueryContained", true );
            if( from.HasProp( "IsDeletedResourcesView" ) )
                proxy.SetProp( "IsDeletedResourcesView", true );
            proxy.EndUpdate();
            #endregion Copying of atomic properties

            return newView;
        }

        public static void  CloneView( IResource from, ResourceProxy to, string newName )
        {
            #region Preconditions
            if( from == null )
                throw new ArgumentNullException( "from", "FilterManager -- Source resource is NULL." );
            if( to == null )
                throw new ArgumentNullException( "to", "FilterManager -- Target resource is NULL." );
            if( String.IsNullOrEmpty( newName ) )
                throw new ArgumentNullException( "newName", "FilterManager -- New name is NULL or empty." );
            #endregion Preconditions

            IResource[][] conditions;
            IResource[]   exceptions;
            CloneConditionTypeLinks( from , out conditions, out exceptions );

            string[] formTypes = CompoundType( from );
            InitializeView( to, newName, formTypes, conditions, exceptions );
        }

        public static void  CloneConditionTypeLinks( IResource from,
                                                      out IResource[][] groups, out IResource[] exceptions )
        {
            IFilterManager mgr = Core.FilterManager;
            IResourceList sourceGroups = from.GetLinksOfType( FilterManagerProps.ConjunctionGroup, _props._linkedConditionsLink );
            ArrayList     newGroups = new ArrayList();
            foreach( IResource group in sourceGroups )
            {
                IResource[]   newConds = CloneConditionsList2Vector( mgr.GetConditions( group ) );
                newGroups.Add( newConds );
            }
            exceptions = CloneConditionsList2Vector( mgr.GetExceptions( from ) );
            groups = (IResource[][]) newGroups.ToArray( typeof( IResource[] ) );
        }

        private static IResource[]  CloneConditionsList2Vector( IResourceList list )
        {
            IResource[] newList = new IResource[ list.Count ];
            for( int i = 0; i < list.Count; i++ )
                newList[ i ] = Core.FilterManager.CloneCondition( list[ i ] );
            return newList;
        }
        #endregion Views

        #region ViewFolders
        //---------------------------------------------------------------------
        //  View folder stuff
        //---------------------------------------------------------------------
        public IResource  CreateViewFolder( string name, string baseFolderName, int order )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( name ) )
                throw new ArgumentNullException( "name", "FilterManager -- Folder name is NULL or empty" );

            if( baseFolderName != null && baseFolderName.Length == 0 )
                throw new ArgumentException( "FilterManager -- Base folder name is not NULL but its Length is 0" );
            #endregion Preconditions

            //-----------------------------------------------------------------
            //  Because of problems in previous versions we can met several
            //  view folder resources with the same name. Remove extra ones.
            //-----------------------------------------------------------------
            IResource folder;
            IResourceList folders = Store.FindResources( FilterManagerProps.ViewFolderResName, Core.Props.Name, name );
            if( folders.Count == 0 )
            {
                folder = CreateViewFolderImpl( name );
            }
            else
            {
                folder = folders[ 0 ];
                if( folders.Count > 1 )
                {
                    new ResourceProxy( folders[ 1 ] ).Delete();
                }
            }

            //-----------------------------------------------------------------
            if( baseFolderName == null )
                Core.ResourceTreeManager.LinkToResourceRoot( folder, order );
            else
            {
                IResource baseFolder = Store.FindUniqueResource( FilterManagerProps.ViewFolderResName, "DeepName", baseFolderName );
                if( baseFolder == null )
                {
                    baseFolder = CreateViewFolderImpl( baseFolderName );
                    Core.ResourceTreeManager.LinkToResourceRoot( baseFolder, 0 );
                }
                new ResourceProxy( folder ).SetProp( Core.Props.Parent, baseFolder );
            }
            return folder;
        }

        private static IResource CreateViewFolderImpl( string name )
        {
            ResourceProxy folder = ResourceProxy.BeginNewResource( FilterManagerProps.ViewFolderResName );
            folder.BeginUpdate();
            folder.SetProp( Core.Props.Name, name );
            folder.SetProp( "DeepName", name );
            folder.SetProp( "_DisplayName", name );
            folder.EndUpdate();

            return folder.Resource;
        }

        public void  AssociateViewWithFolder( IResource view, string folderName, int order )
        {
            #region Preconditions
            if( view == null )
                throw new ArgumentNullException( "view", "FilterManager -- input view resource is NULL" );

            if( view.Type != FilterManagerProps.ViewResName )
                throw new ArgumentException( "FilterManager -- input view resource has inappropriate type: [" + view.Type + "]" );

            if( folderName != null && Store.FindUniqueResource( FilterManagerProps.ViewFolderResName, Core.Props.Name, folderName ) == null )
                throw new ArgumentException( "FilterManager -- input View Folder does not exist" );
            #endregion Preconditions

            if( folderName == null )
                Core.ResourceTreeManager.LinkToResourceRoot( view, order );
            else
            {
                IResource folder = Store.FindUniqueResource( FilterManagerProps.ViewFolderResName, Core.Props.Name, folderName );
                view.SetProp( Core.Props.Parent, folder );
                view.SetProp( "RootSortOrder", order );
            }
        }
        #endregion ViewFolders

        #region Rule Registration
        public void  RegisterActivationEvent( string eventName, string displayName )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( eventName ) )
                throw new ArgumentNullException( "eventName", "FilterManager -- Event name is NULL or empty." );

            if( String.IsNullOrEmpty( displayName ) )
                throw new ArgumentNullException( "displayName", "FilterManager -- Display name of an event is NULL or empty." );

            if( RegisteredEvents.ContainsKey( eventName ) &&
               ((string)RegisteredEvents[ eventName ]) != displayName )
                throw new ArgumentException( "FilterManager -- Event name collision: another display name is already registered." );
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
                throw new ArgumentNullException( "type", "FilterManager -- Rule name can not be NULL or empty." );
            #endregion Preconditions

            if( Core.ResourceStore.FindResources( RuleApplicableResourceTypeResName, Core.Props.Name, type ).Count == 0 )
            {
                IResource res = Core.ResourceStore.BeginNewResource( RuleApplicableResourceTypeResName );
                res.SetProp( Core.Props.Name, type );
                res.EndUpdate();
            }                                                                                                    
        }

        public IResource   RegisterRule( string eventName, string name, string[] types,
                                         IResource[] conditions, IResource[] exceptions, IResource[] actions )
        {
            IResource[][] groups = Convert2Group( conditions );
            return RegisterRule( eventName, name, types, groups, exceptions, actions );
        }

        public void  ReregisterRule( string eventName, IResource exRule, string name, string[] types,
                                     IResource[] conditions, IResource[] exceptions, IResource[] actions )
        {
            IResource[][] groups = Convert2Group( conditions );
            ReregisterRule( eventName, exRule, name, types, groups, exceptions, actions );
        }

        public IResource   RegisterRule( string eventName, string name, string[] types,
                                         IResource[][] conditions, IResource[] exceptions, IResource[] actions )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( name ) )
                throw new ArgumentNullException( "name", "FilterManager -- Rule name can not be NULL or empty." );

            if( conditions == null && exceptions == null )
                throw new ArgumentException( "FilterManager -- conditions list and exceptions list can not be empty simultaneously" );
            #endregion Preconditions

            IResource rule = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleResName, Core.Props.Name, name );
            if( rule != null )
                DeleteRule( rule );
            
            ResourceProxy proxy = ResourceProxy.BeginNewResource( FilterManagerProps.RuleResName );
            InitializeRule( proxy, name, eventName, types, conditions, exceptions, actions );

            return( proxy.Resource );
        }

        public void  ReregisterRule( string eventName, IResource exRule, string name, string[] types,
                                     IResource[][] conditions, IResource[] exceptions, IResource[] actions )
        {
            #region Preconditions
            if( exRule == null )
                throw new ArgumentNullException( "exRule", "FilterManager -- Resource can not be NULL for reregistering." );

            if( conditions == null && exceptions == null )
                throw new ArgumentException( "FilterManager -- conditions list and exceptions list can not be empty simultaneously" );
            #endregion Preconditions

            CleanLinkedRuleParameters( exRule );

            ResourceProxy proxy = new ResourceProxy( exRule );
            InitializeRule( proxy, name, eventName, types, conditions, exceptions, actions );
            DeleteHangedConjunctionGroups();
        }

        private static void InitializeRule( ResourceProxy proxy, string name, string eventName, string[] types,
                                            IResource[][] conditionGroups, IResource[] exceptions, IResource[] actions )
        {
            SetMajorParameters( proxy, name, eventName, types, conditionGroups, exceptions, actions );

            LinkParametersAndRule( proxy.Resource, conditionGroups );
            LinkParametersAndRule( proxy.Resource, exceptions );
            LinkParametersAndRule( proxy.Resource, actions );
        }

        private static void  SetMajorParameters( ResourceProxy proxy, string name, string eventName, string[] types,
                                                 IResource[][] conditionGroups, IResource[] exceptions, IResource[] actions )
        {
            proxy.BeginUpdate();
            proxy.SetProp( Core.Props.Name, name );
            proxy.SetProp( "DeepName", name );
            proxy.SetProp( "EventName", eventName );
            proxy.SetProp( "IsActionFilter", true );
            proxy.DeleteProp( Core.Props.LastError );

            if( HasQueryCondition( conditionGroups ) || HasQueryCondition( exceptions ) )
                proxy.SetProp( "IsQueryContained", true );
            else
                proxy.DeleteProp( "IsQueryContained" );

            if( conditionGroups != null )
                AddLinks( proxy, conditionGroups, _props._linkedConditionsLink, FilterManagerProps.ConditionResName );
            else
                proxy.DeleteLinks( _props._linkedConditionsLink );

            if( exceptions != null )
                AddLinks( proxy, exceptions, _props._linkedExceptionsLink, FilterManagerProps.ConditionResName );
            else
                proxy.DeleteLinks( _props._linkedExceptionsLink );

            AddLinks( proxy, actions, _props._linkedActionsLink, FilterManagerProps.RuleActionResName );

            if( types != null && types.Length > 0 )
                proxy.SetProp( Core.Props.ContentType, Utils.MergeStrings( types, '|' ) );
            else
                proxy.DeleteProp( Core.Props.ContentType );
            proxy.EndUpdate();
        }

        private static void  LinkParametersAndRule( IResource rule, IEnumerable<IResource[]> conditionGroups )
        {
            if( conditionGroups != null )
            {
                foreach( IResource[] list in conditionGroups )
                {
                    LinkParametersAndRule( rule, list );
                }
            }
        }
        private static void  LinkParametersAndRule( IResource rule, IEnumerable<IResource> actions )
        {
            if( actions != null )
            {
                foreach( IResource res in actions )
                {
                    IResourceList paramResources = res.GetLinksOfType( null, _props._setValueLinkProp );
                    foreach( IResource parameter in paramResources )
                        new ResourceProxy( parameter ).SetProp( "UsedByRule", rule );
                }
            }
        }
        #endregion Rule Registration
        
        #region RuleAction Registration
        public IResource RegisterRuleAction( string name, string deepName, IRuleAction executor )
        {
            return RegisterRuleAction( name, deepName, executor, null );
        }

	    public IResource RegisterRuleAction( string name, string deepName, IRuleAction executor, string[] types )
	    {
            #region Preconditions
            if( String.IsNullOrEmpty( name ))
                throw new ArgumentNullException( "name", "FilterManager -- Name of an action can not be NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterManager -- Deep name of an action can not be NULL or empty." );

            if( executor == null )
                throw new ArgumentNullException( "executor", "FilterManager -- Execution handler for an action is NULL." );
            #endregion Preconditions

            IResource action = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleActionResName, "DeepName", deepName );
            if( action == null )
                action = Core.ResourceStore.NewResource( FilterManagerProps.RuleActionResName );

            action.SetProp( Core.Props.Name, name );
            action.SetProp( "DeepName", deepName );
            SetApplicableResType( action, types );
            RuleActions[ name ] = executor;

            return( action );
	    }

        public IResource  RegisterRuleActionTemplateWithUIHandler( string name, string deepName, IRuleAction executor,
                                                                   string[] types, ITemplateParamUIHandler handler,
                                                                   ConditionOp op, params string[] values )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( name ))
                throw new ArgumentNullException( "name", "FilterManager -- Name of an action template can not be NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterManager -- Deep name of an action template can not be NULL or empty." );

            if( handler == null )
                throw new ArgumentNullException( "handler", "FilterManager -- UI handler for the action template is NULL." );
            #endregion Preconditions

            IResource action = RegisterRuleActionTemplate( name, deepName, executor, types, op, values );
            RegisteredUIHandlers[ name ] = handler;

            return action;
        }

        public IResource RegisterRuleActionTemplate( string name, string deepName, IRuleAction executor,
                                                     ConditionOp op, params string[] parameters )
        {
            return RegisterRuleActionTemplate( name, deepName, executor, null, op, parameters );
        }

        public IResource RegisterRuleActionTemplate( string name, string deepName, IRuleAction executor,
                                                     string[] types, ConditionOp op, params string[] parameters )
        {
            //-----------------------------------------------------------------
            IResource template = Store.FindUniqueResource( FilterManagerProps.RuleActionTemplateResName, "DeepName", deepName );
            if( template == null )
                template = Core.ResourceStore.NewResource( FilterManagerProps.RuleActionTemplateResName );

            //-----------------------------------------------------------------
            template.BeginUpdate();
            template.SetProp( Core.Props.Name, name );
            template.SetProp( "DeepName", deepName );
            template.DisplayName = name.Replace( "%", "" );
            template.SetProp( _props._opProp, (int)op );
            SetApplicableResType( template, types );

            if(( op == ConditionOp.Eq ) && ( parameters.Length == 0 ))
            {}
            else
            if(( op == ConditionOp.In ) && ( parameters.Length >= 1 ))
            {
                template.SetProp( _props._chooseFromResProp, parameters[ 0 ] );
                if( parameters.Length == 2 )
                    template.SetProp( _props._appliedToNameProp, parameters[ 1 ] );
            }
            else
                throw new ArgumentException( "Operation [" + op + "] with such set of parameters is not supported now" );
            template.EndUpdate();

            RuleActions[ name ] = executor;
            return( template );
        }

        public static IResource RegisterRuleActionProxy( IResource actionTemplate, string param )
        {
            ResourceProxy proxy = ResourceProxy.BeginNewResource( FilterManagerProps.RuleActionResName );
            proxy.BeginUpdate();
            proxy.SetProp( _props._templateLink, actionTemplate );
            proxy.SetProp( _props._conditionValProp, param );
            proxy.SetProp( "Invisible", true );
            proxy.EndUpdate();

            return( proxy.Resource );
        }

        public static IResource RegisterRuleActionProxy( IResource actionTemplate, IResourceList parameters )
        {
            ResourceProxy proxy = ResourceProxy.BeginNewResource( FilterManagerProps.RuleActionResName );
            proxy.BeginUpdate();
            proxy.SetProp( _props._templateLink, actionTemplate );
            proxy.SetProp( "Invisible", true );
            AddLinks( proxy, parameters, _props._setValueLinkProp );
            proxy.EndUpdate();

            return( proxy.Resource );
        }

        public bool IsActionInstantiated( IResource action )
        {
            #region Preconditions
            if( action.Type != FilterManagerProps.RuleActionTemplateResName && action.Type != FilterManagerProps.RuleActionResName )
            {
                throw new InvalidOperationException( "FilterManager -- input parameter is not of type [" + 
                                                     FilterManagerProps.RuleActionTemplateResName + "] or [" + FilterManagerProps.RuleActionResName + "]" );
            }
            #endregion Preconditions

            string name;
            if( action.Type == FilterManagerProps.RuleActionTemplateResName )
                name = action.GetStringProp( Core.Props.Name );
            else
            {
                if( isProxyRuleAction( action ) )
                    name = action.GetLinkProp( _props._templateLink ).GetStringProp( Core.Props.Name );
                else
                    name = action.GetStringProp( Core.Props.Name );
            }

            return ( name != null && RuleActions.ContainsKey( name ) );
        }

        public void MarkActionTemplateAsSingleSelection( IResource actionTemplate )
        {
            #region Preconditions
            if( actionTemplate.Type != FilterManagerProps.RuleActionTemplateResName )
                throw new InvalidOperationException( "FilterManager -- input parameter is not of type [" + FilterManagerProps.RuleActionTemplateResName + "]" );
            #endregion Preconditions

            actionTemplate.SetProp( "IsSingleSelection", true );
        }

        public IResource  CloneAction( IResource action )
        {
            #region Preconditions
            if( action.Type != FilterManagerProps.RuleActionResName )
                throw new ApplicationException( "FilterManager -- input resource type is not Action" );
            #endregion Preconditions

            //  do not clone original actions which are not produced from some
            //  action template.
            IResource  result = action;
            if( isProxyRuleAction( action ))
            {
                ResourceProxy proxy = ResourceProxy.BeginNewResource( FilterManagerProps.RuleActionResName );
                proxy.BeginUpdate();
                proxy.SetProp( "Invisible", true );
                AssignProperty( action, proxy, "ConditionVal" );
                CopyLinks( action, proxy, _props._templateLink );
                CopyLinks( action, proxy, _props._setValueLinkProp );
                proxy.EndUpdate();
                result = proxy.Resource;
            }
            return result;
        }

        public IResource  InstantiateRuleActionTemplate( IResource template, object param, string representation )
        {
            return FilterConvertors.Template2Action( template, param, representation );
        }
        #endregion RuleAction Registration

        #region Miscellaneous Operations Over Rules
        public IResource   FindRule( string name )
        {
            IResourceList list = Core.ResourceStore.FindResources( FilterManagerProps.RuleResName, Core.Props.Name, name );
            list = list.Minus( Core.ResourceStore.FindResourcesWithProp( FilterManagerProps.RuleResName, "IsExpirationFilter" ) );
            return (list.Count > 0) ? list[ 0 ] : null;
        }

        public void  DeleteRule( string ruleName )
        {
            IResource rule = Store.FindUniqueResource( FilterManagerProps.RuleResName, Core.Props.Name, ruleName );
            if( rule != null )
                DeleteRule( rule );
        }
        public void  DeleteRule( IResource rule )
        {
            DeleteInternalConditions( rule );
            DeleteInternalActions( rule );
            DeleteResource( rule );
            DeleteHangedConjunctionGroups();
        }

        public void  RenameRule( IResource rule, string newName )
        {
            #region Preconditions
            if( rule == null )
                throw new ArgumentNullException( "rule", "FilterManager -- Input rule resource is null." );

            if( rule.Type != FilterManagerProps.RuleResName )
                throw new ArgumentException( "FilterManager -- Input rule resource has inappropriate type." );

            if( String.IsNullOrEmpty( newName ) )
                throw new ArgumentNullException( "newName", "FilterManager -- New name for a rule is null or empty." );

            if( FindRuleByName( newName, "IsActionFilter" ) != null )
                throw new ArgumentException( "FilterManager -- An action rule with new name already exists." );
            #endregion Preconditions

            new ResourceProxy( rule ).SetProp( Core.Props.Name, newName );
        }

        //  Accept not only Rules but Formatting rules as well (which are
        //  View internally)
        public void  ActivateRule( IResource rule )
        {
            #region Preconditions
            if( rule == null )
                throw new ArgumentNullException( "rule", "FilterManager -- Input rule resource is null." );
            #endregion Preconditions

            ResourceProxy proxy = new ResourceProxy( rule );
            proxy.DeleteProp( "RuleTurnedOff" );
        }

        //  Accept not only Rules but Formatting rules as well (which are
        //  View internally)
        public void  DeactivateRule( IResource rule )
        {
            #region Preconditions
            if( rule == null )
                throw new ArgumentNullException( "rule", "FilterManager -- Input rule resource is null." );
            #endregion Preconditions

            SetProperty( rule, "RuleTurnedOff", true );
        }

        public void  MarkRuleAsInvalid( IResource rule, string reason )
        {
            #region Preconditions
            if( rule == null )
                throw new ArgumentNullException( "rule", "FilterManager -- Input rule resource is null." );
            #endregion Preconditions
            
            DeactivateRule( rule );
            SetProperty( rule, Core.Props.LastError, reason );
        }

        public void  AssignOrderNumber( IResource rule, int num )
        {
            #region Preconditions
            if( rule == null )
                throw new ArgumentNullException( "rule", "FilterManager -- Input rule resource is null." );
            #endregion Preconditions

            SetProperty( rule, Core.Props.Order, num );
        }
        #endregion

        #region Rename
        //---------------------------------------------------------------------
        //  Rename created condition. This method must be called right after
        //  the condition with new name is [re]created.
        //---------------------------------------------------------------------
        public void  RenameCondition( string oldName, string newName )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( oldName ) )
                throw new ArgumentNullException( "oldName", "FilterManager -- Old name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( newName ) )
                throw new ArgumentNullException( "newName", "FilterManager -- New name of a condition is NULL or empty." );

            //  Check that the new condition has been actually created.
            IResource newRes = Store.FindUniqueResource( FilterManagerProps.ConditionResName, Core.Props.Name, newName );
            if( newRes == null )
            {
                throw new ConstraintException( "FilterManager -- A condition with new name must be already created before rename." );
            }
            #endregion Preconditions

            IResource oldRes = Store.FindUniqueResource( FilterManagerProps.ConditionResName, Core.Props.Name, oldName );

            //  If old condition still exists, move all its links to the new one:
            //  - _props._templateLink, LinkedCondition, LinkedNegativeCondition.
            if( oldRes != null )
            {
                RelinkCondition( "LinkedCondition", oldRes, newRes );
                RelinkCondition( "LinkedNegativeCondition", oldRes, newRes );
                oldRes.Delete();
            }
        }

        public void  RenameConditionTemplate( string oldName, string newName )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( oldName ) )
                throw new ArgumentNullException( "oldName", "FilterManager -- Old name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( newName ) )
                throw new ArgumentNullException( "newName", "FilterManager -- New name of a condition is NULL or empty." );

            //  Check that the new condition has been actually created.
            IResource newRes = Store.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, Core.Props.Name, newName );
            if( newRes == null )
            {
                throw new ConstraintException( "FilterManager -- A condition with new name must be already created before rename." );
            }
            #endregion Preconditions

            IResource oldRes = Store.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, Core.Props.Name, oldName );

            //  If old condition still exists, move all its links to the new one:
            //  - _props._templateLink, LinkedCondition, LinkedNegativeCondition.
            if( oldRes != null )
            {
                RelinkCondition( "_props._templateLink", oldRes, newRes );
                RelinkCondition( "LinkedCondition", oldRes, newRes );
                RelinkCondition( "LinkedNegativeCondition", oldRes, newRes );
                oldRes.Delete();
            }
        }

        private static void RelinkCondition( string linkName, IResource oldRes, IResource newRes )
        {
            IResourceList list = oldRes.GetLinksOfType( null, linkName );
            foreach( IResource res in list )
            {
                //  Take direction into consideration: FROM smth TO new condition.
                res.SetProp( linkName, newRes );
            }
        }

        public void  RenameRuleAction( string oldName, string newName )
        {
            IResource res = Store.FindUniqueResource( FilterManagerProps.RuleActionResName, Core.Props.Name, oldName );
            if( res != null )
            {
                IResource existingRes = Store.FindUniqueResource( FilterManagerProps.RuleActionResName, Core.Props.Name, newName );
                if( existingRes != null )
                    existingRes.Delete();
                res.SetProp( Core.Props.Name, newName );
                RuleActions[ newName ] = RuleActions[ oldName ];
            }
            else
            {
                res = Store.FindUniqueResource( FilterManagerProps.RuleActionTemplateResName, Core.Props.Name, oldName );
                if( res != null )
                {
                    IResourceList linked = Store.EmptyResourceList;
                    IResourceList existingRes = Store.FindResources( FilterManagerProps.RuleActionTemplateResName, Core.Props.Name, newName );
                    if( existingRes.Count > 0 )
                    {
                        foreach( IResource templ in existingRes )
                        {
                            linked = linked.Union( templ.GetLinksOfType( FilterManagerProps.RuleActionResName, "_props._templateLink" ), true );
                        }
                        existingRes.DeleteAll();
                    }

                    res.SetProp( Core.Props.Name, newName );
                    res.DisplayName = newName.Replace( "%", "" );
                    foreach( IResource action in linked )
                        action.SetProp( "_props._templateLink", res );
                    RuleActions[ newName ] = RuleActions[ oldName ];
                }
                else
                {
//                    throw new ArgumentException( "FilterManager -- No rule action or action template with name [" + oldName + "] exists" );
                    //  do not raise exception since rename to the inproper
                    //  names could not be repaired else.
                    Trace.WriteLine( "FilterManager -- No rule action or action template with name [" + oldName + "] exists for rename" );
                }
            }
        }
        #endregion Rename

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
                throw new ArgumentNullException( "view", "FilterManager -- View is NULL." );
            #endregion Preconditions

            //-----------------------------------------------------------------
            //  Sometimes view becomes invalid due to other bugs and there is
            //  absolutely no reason to thow an exception.
            //-----------------------------------------------------------------
            string name = Utils.IsValidString( viewName ) ? viewName : "Unnamed View";

            Highlighter = null;
            IResourceList result = ExecView( view );
            Trace.WriteLineIf( IsVerbose, "*** FilterManager -- [" + name + "] returned " + result.Count + " unfiltered results." );

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
                throw new ArgumentNullException( "view", "FilterManager -- View is NULL." );
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
	            throw new ArgumentNullException( "view", "FilterManager -- View is NULL." );

	        if( view.Type != FilterManagerProps.ViewResName && view.Type != FilterManagerProps.RuleResName && view.Type != FilterManagerProps.ViewCompositeResName )
	            throw new InvalidOperationException( "FilterManager -- Can not apply [ExecView] to inappropriate resource (of type" + view.Type + ")" );
            #endregion Preconditions
    
            //  Process special case when the initial set is empty, it is
            //  unambiguously matches the result.
            if( initialSet != null && initialSet.Count == 0 )
                return Store.EmptyResourceList;

	        IResourceList result = Store.EmptyResourceList;
    
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
	        if( !Utils.IsValidString( errorMsg ) )
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
                    Trace.WriteLine( "$$$ FilterManager -- View [" + view.DisplayName + "] caused an exception:");
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
            if( !Utils.IsValidString( errorMsg ) )
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
            if( !Utils.IsValidString( errorMsg ) )
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
                            if( !IsUnreadCondition( conditions[ i ] ) )
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
            //  Dummy condition is used in Expiration rules for FilterManager
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
                throw new ArgumentNullException( "res", "FilterManager - Resource is NULL in rule activation." );
            #endregion Preconditions

            Trace.WriteLineIf( IsVerbose, "*** FilterManager -- Rules are activated for a resource " + res.DisplayName );

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
                throw new ArgumentNullException( "rule", "FilterManager - Rule resource is NULL." );

            if( list == null )
                throw new ArgumentNullException( "list", "FilterManager - Source list of resources is NULL in rule activation." );
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
                if( HasQueryCondition( rule ) == isQueryRule )
                    result = result.Union( rule.ToResourceList(), true );
            }

            return result;
        }
/*
        private void  ApplyQueryRule( IResource rule, IResourceList list )
        {
            string  eventName = rule.GetStringProp( "EventName" );
            if( eventName == StandardEvents.ResourceReceived )
            {
                IResourceList resMatched = ExecView( rule ).Intersect( list, true );

                //  There may be a situation when we are trying to exec
                //  query-containing rules just in the moment of exiting
                //  the application. This is considered normal and we just have
                //  to clear the error flag and rerun rules after text index
                //  is initialized next time.
                if( !rule.HasProp( Core.Props.LastError ) )
                {
                    for( int i = resMatched.Count - 1; i >= 0; i-- )
                        ApplyActions( rule, resMatched[ i ] );
                }
                else
                {
                    rule.DeleteProp( Core.Props.LastError );
                    foreach( IResource res in list.ValidResources )
                        res.SetProp( "QueryRulesRerunRequired", true );
                }
            }
        }

        private bool  ApplyNonQueryRule( IResource rule, string eventName, IResource res )
        {
            bool matched = false;
            string  ruleEventName = rule.GetStringProp( "EventName" );
            if( IsRuleActive( rule ) && ( ruleEventName == eventName ) && !HasQueryCondition( rule ))
            {
                if( MatchView( rule, res, false ) )
                {
                    ApplyActions( rule, res );
                    matched = true;
                }
            }
            return matched;
        }
*/

        #region Actions Enumeration
        private delegate void StringParamMethodInvoker( string name );

        public void  ApplyActions( IResource rule, IResource res )
        {
            #region Preconditions
            if( rule == null )
                throw new ArgumentNullException( "rule", "FilterManager -- Rule resource is null" );

            if( res == null )
                throw new ArgumentNullException( "res", "FilterManager -- Input resource is null" );
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
            string         name;
            IRuleAction    action;
            if( isProxyRuleAction( actionRes ))
            {
                IResource template = actionRes.GetLinkProp( _props._templateLink );
                if( template == null )
                    throw new ApplicationException( "FilterManager -- Template link is broken." );
                name = template.GetStringProp( Core.Props.Name );
            }
            else
                name = actionRes.GetStringProp( Core.Props.Name );
            action = (IRuleAction)RuleActions[ name ];

            if ( action != null )
            {
                action.Exec( res, new ActionParameterStore( actionRes ));
            }
            else
            {
                Trace.WriteLine( "*** FilterManager --     Action [" + name + "] is not registered" );
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
                throw new ArgumentException( "FilterManager -- Input condition can not be null" );

            if( condition.Type != FilterManagerProps.ConditionResName )
                throw new ArgumentException( "FilterManager -- Input parameter is not of [SearchCondition] type" );
            #endregion Preconditions

            IResourceList   result = Store.EmptyResourceList;
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
                    Trace.WriteLine( "FilterManager -- exception caught while executing view [" + 
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
            IResourceList   result = Store.EmptyResourceList;
            IResourceList   linkedResult = Store.EmptyResourceList;
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
                        IResourceList thoseHavingLinks = Store.FindResourcesWithPropLive( null, linkTypes[ i ] );
                        temp = temp.Intersect( thoseHavingLinks, true );
                        linkedResult = linkedResult.Union( temp, true );
                    }
                }
                if( formats.Length > 0 )
                {
                    IResourceList linkedRestr = Store.EmptyResourceList;
                    foreach( string type in formats )
                    {
                        linkedRestr = linkedRestr.Union(
                            linkedResult.Intersect( Store.GetAllResources( type ), true ), true );
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
            IResourceList   result = Store.EmptyResourceList;

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
                object  handler = CustomConditions[ name ];
                if( handler == null )
                    throw new ApplicationException( "FilterManager -- Custom ConditionTemplate handler [" + name + "] has not been initialized." );
                if( !( handler is ICustomConditionTemplate ))
                    throw new ApplicationException( "FilterManager -- Non-internal condition template of Custom type does not support ICustomConditionTemplate interface." );

                ICustomConditionTemplate  exec = (ICustomConditionTemplate) handler;
                result = exec.Filter( resType, new ActionParameterStore( condition ) );
            }
            else
            {
                string  name = condition.GetStringProp( Core.Props.Name );
                object  handler = CustomConditions[ name ];
                if( handler == null )
                    throw new ApplicationException( "FilterManager -- Custom Condition handler [" + name + "] has not been initialized." );
                if( !( handler is ICustomCondition ))
                    throw new ApplicationException( "FilterManager -- Non-internal condition of Custom type does not support ICustomCondition interface." );

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
            IResourceList   result = Store.EmptyResourceList;
            string          normProp = (prop[ 0 ] == '-') ? prop.Substring( 1, prop.Length - 1 ) : prop;

            if(( op == ConditionOp.QueryMatch ) || ( Store.PropTypes.Exist( new string[]{ normProp } )))
            {
                if(( condType != "predicate" ) && ( condType != "direct-set" ) &&
                   ( Store.PropTypes [ normProp ].DataType == PropDataType.Link ))
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
                    Trace.WriteLine( "FilterManager -- Exception while processing " + condType + " condition: " + exc );
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
                throw new ArgumentNullException( "resType", "FilterManager -- Accidental NULL value of resource type in HasNoProp condition." );
            #endregion Preconditions

            IResourceList  result;
            if( op == ConditionOp.QueryMatch )
                result = ExecQueryCondition( condition, resType );
            else
            {
                int propId;
                if( op == ConditionOp.HasProp || op == ConditionOp.HasNoProp )
                    propId = Store.PropTypes[ prop ].Id;
                else
                if( prop[ 0 ] != '-' )
                    propId = Store.PropTypes[ prop ].Id;
                else
                {
                    string par = prop.Substring( 1, prop.Length - 1 );
                    propId = -Store.PropTypes[ par ].Id;
                }

                result = Store.FindResourcesWithProp( mode, resType, propId );
                if( op == ConditionOp.HasNoProp )
                    result = Store.GetAllResourcesLive( resType ).Minus( result );
            }

            return( result );
        }
        private IResourceList ExecQueryCondition( IResource condition, string resType )
        {
            IResourceList   result = Store.EmptyResourceList;
            if( Core.TextIndexManager.IsIndexPresent() )
            {
                string query = ConstructQuery( condition );
                _lastQueryError = null;
                result = Core.TextIndexManager.ProcessQuery( query, null, out Highlighter, out _lastStopList, out _lastQueryError );
                if( resType != null )
                    result = result.Intersect( Store.GetAllResourcesLive( resType ), true);
            }
            return result;
        }

        private IResourceList ExecStandardCondition( IResource condition, SelectionType mode,
                                                     string resType, string propName )
        {
            string      propValue = condition.GetStringProp( _props._conditionValProp );
            int         propID = Store.GetPropId( propName );
            ConditionOp op = (ConditionOp)condition.GetIntProp( _props._opProp );
            IResourceList   result = Store.EmptyResourceList;

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
                        result = Store.FindResourcesInRange( mode, resType, propID,
                            typeIsInt ? (int)propValueCasted + 1 : propValueCasted, propMaxValueCasted );
                    }
                    else
                    {
                        result = Store.FindResourcesInRange( mode, resType, propID, propMinValueCasted,
                            typeIsInt ? (int)propValueCasted - 1 : propValueCasted );
                    }
                }
            }
            else
                if( op == ConditionOp.Has )
            {
                IntArrayList ids = null;
                result = Store.FindResourcesWithProp( mode, resType, propName );
                lock( result )
                {
                    if ( result.Count > 0 )
                    {
                        ids = new IntArrayList();
                        foreach( IResource res in result )
                        {
                            string  resPropVal = res.GetStringProp( propName );
                            if( Utils.IndexOf( resPropVal, propValue, true ) >= 0 )
                                ids.Add( res.Id );
                        }
                    }
                }
                if ( ids != null )
                {
                    result = Store.ListFromIds( ids, true );
                }
                else
                {
                    result = Store.EmptyResourceList;
                }
            }
            else
                throw new NotSupportedException( "FilterManager -- Unknown type of operation" + op + " in the Triple condition " + condition.DisplayName );

            return( result );
        }

        private IResourceList ExecRangeCondition( IResource condition, SelectionType mode,
                                                  string resType, string propName )
        {
            string  propLower = condition.GetStringProp( _props._conditionValLowerProp );
            string  propUpper = condition.GetStringProp( _props._conditionValUpperProp );
            int     propID = Store.GetPropId( propName );
            object  propLowerCasted = CastValueToPropertyType( propName, propLower );
            object  propUpperCasted = CastValueWithRangeSpec( propName, propUpper, propLowerCasted );
            IResourceList   result = Store.EmptyResourceList;

            if(( propLowerCasted != null ) && ( propUpperCasted != null ))
                result = Store.FindResourcesInRange( mode, resType, propID, propLowerCasted, propUpperCasted );

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
            int             resID = Store.GetPropId( propName );
            IResourceList   result = Core.ResourceStore.EmptyResourceList;
            for( int i = 0; i < propValues.Length; i++ )
            {
                result = result.Union( Store.FindResources(
                    mode, resType, resID, CastValueToPropertyType( propName, propValues[ i ] ) ), true );
            }

            return( result );
        }
        
        private IResourceList  ExecDirectSetCondition( IResource condition, string resType, string propName )
        {
            ConditionOp op = (ConditionOp)condition.GetIntProp( _props._opProp );
            Debug.Assert( op == ConditionOp.In || op == ConditionOp.Eq || op == ConditionOp.Has, 
                          "Mismatch between operation type and condition type" );

            IResourceList   result = Store.EmptyResourceList;
            IResourceList   setResources = condition.GetLinksOfType( null, _props._setValueLinkProp );

            foreach( IResource res in setResources )
            {
                result = result.Union( res.GetLinksOfTypeLive( resType, propName ), true );
            }
            return( result );
        }

        private static IResourceList  ExecDirectSetOnCyclicProperty( IResource condition, string linkName )
        {
            IResourceList  result, children;
            result = children = condition.GetLinksOfTypeLive( null, _props._setValueLinkProp );

            foreach( IResource res in children )
            {
                result = result.Union( CollectLinkedResources( res, linkName ), true );
            }
            return result;
        }

        private static IResourceList  CollectLinkedResources( IResource root, string linkName )
        {
            IResourceList  result, children;
            result = children = root.GetLinksToLive( null, linkName );

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
            if( resLinks == null && !Utils.IsValidString( resTypeCompound ) )
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
                object  handler = CustomConditions[ name ];
                if( handler == null )
                    throw new ApplicationException( "FilterManager -- Custom ConditionTemplate handler [" + name + "] has not been initialized." );
                if( !( handler is ICustomConditionTemplate ))
                    throw new ApplicationException( "FilterManager -- Non-internal condition template of Custom type does not support ICustomConditionTemplate interface." );

                ICustomConditionTemplate  exec = (ICustomConditionTemplate) handler;
                return exec.MatchResource( res, new ActionParameterStore( condition ) );
            }
            else
            {
                string name = condition.GetStringProp( Core.Props.Name );
                ICustomCondition  conditionExec = (ICustomCondition)CustomConditions[ name ];
                if( conditionExec == null )
                    throw new ApplicationException( "FilterManager -- No execution handler found for [" + name + "]." );

                return conditionExec.MatchResource( res );
            }
        }

        private bool  MatchConditionOnProperty( IResource condition, string prop, IResource res )
        {
            bool    result = false;
            string  conditionType = condition.GetStringProp( "ConditionType" );
            string  normProp = (prop[ 0 ] == '-') ? prop.Substring( 1, prop.Length - 1 ) : prop;

            if( condition.GetIntProp( _props._opProp ) == (int)ConditionOp.QueryMatch ||
                Store.PropTypes.Exist( normProp ) )
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
                    Debug.Assert( false, "FilterManager -- Exception while processing condition (Debug purpose): " + exc.Message );
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
                    propId = Store.PropTypes[ prop ].Id;
                else
                {
                    string par = prop.Substring( 1, prop.Length - 1 );
                    propId = -Store.PropTypes[ par ].Id;
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
                string query = ConstructQuery( condition );
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

        private static bool MatchSetCondition( IResource condition, string propName, IResource res )
        {
            ConditionOp  op = (ConditionOp)condition.GetIntProp( _props._opProp );
            Debug.Assert( op == ConditionOp.In || op == ConditionOp.Eq || op == ConditionOp.Has, 
                          "Mismatch between operation type and condition type" );

            string[]    valuesSet = CollectSetValuesFromCondition( condition );
            bool        result = MatchEqualityCondition( res, propName, valuesSet );
            return( result );
        }

        private static bool  MatchDirectSetCondition( IResource condition, string propName, IResource res )
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

        private static bool  MatchDirectSetOnCyclicProperty( IResource condition, string propName, IResource res )
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

        private static bool  MatchDirectSetConditionDirected( IResource condition, string propName, IResource res )
        {
            ConditionOp  op = (ConditionOp)condition.GetIntProp( _props._opProp );
            Debug.Assert( op == ConditionOp.In || op == ConditionOp.Eq || op == ConditionOp.Has, 
                          "Mismatch between operation type and condition type" );

            IResourceList   possible = condition.GetLinksOfType( null, _props._setValueLinkProp );
            IResourceList   resValues = res.GetLinksFrom( null, propName );

            return( resValues.Intersect( possible, true ).Count > 0 );
        }
        #endregion ExecCondition: Scalar Context

        #region Auxiliaries
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
            PropDataType  type = Store.PropTypes [propName].DataType;
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
                                throw new ArgumentException( "FilterManager -- Current contract accepts only negative intervals for hours." );
                            if( (DateTime)propLowerCasted < DateTime.Now )
                                throw new ArgumentException( "FilterManager -- Current contract accepts only [Tomorrow] as the anchor for hour(s) ranges." );

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
                    Trace.WriteLine( "FilterManager -- error in conversion of upper range value with lower=" + 
                                     propLowerCasted + " and CastedInt=" + propUpper );
                }
            }

            return( propValueCasted );
        }
        #endregion Casting

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

        #region Predicates
        private static bool  isInternal( IResource res )
        {
            return (res != null) && (res.GetIntProp( "InternalView" ) == 1);
        }

        private static bool IsDummy( IResource condition )
        {
            return condition.GetStringProp( Core.Props.Name ) == FilterManagerStandards.DummyConditionName;
        }

        public static bool IsViewOrFolder( IResource res )
        {
            return (res != null) && (res.Type == FilterManagerProps.ViewResName || res.Type == FilterManagerProps.ViewFolderResName);
        }

        private static bool  isProxyRuleAction( IResource res )
        {
            if( res.Type != FilterManagerProps.RuleActionResName )
                throw new InvalidOperationException( "FilterManager -- illegal type of resource. RuleAction expected." );

            return res.GetLinkCount( _props._templateLink ) == 1;
        }

        public bool  IsRuleActive( IResource rule )
        {
            return !rule.HasProp( "RuleTurnedOff" );
        }

        public static bool  HasQueryCondition( IResource view )
        {
            return view.HasProp( "IsQueryContained" );
        }

        private static bool  HasQueryCondition( IEnumerable<IResource> conditions )
        {
            if( conditions != null )
            {
                foreach( IResource res in conditions )
                {
                    if( res.GetIntProp( _props._opProp ) == (int) ConditionOp.QueryMatch )
                        return true;
                }
            }
            return false;
        }

        private static bool  HasQueryCondition( IEnumerable<IResource[]> conditionGroups )
        {
            bool hasQueryCondition = false;
            if( conditionGroups != null )
            {
                foreach( IResource[] list in conditionGroups )
                {
                    hasQueryCondition = hasQueryCondition || HasQueryCondition( list );
                }
            }
            return hasQueryCondition;
        }

        private static bool  IsUnreadCondition( IResource res )
        {
            string name = res.GetStringProp( Core.Props.Name );
            return ( name != null ) && ( name == StandardConditions.ResourceIsUnreadName );
        }

        private static bool IsAnyDeletedResourcesCondition( IEnumerable<IResource[]> conditionGroups )
        {
            bool hasDelResCondition = false;
            if( conditionGroups != null )
            {
                foreach( IResource[] list in conditionGroups )
                {
                    hasDelResCondition = hasDelResCondition || IsAnyDeletedResourcesCondition( list );
                }
            }
            return hasDelResCondition;
        }

        private static bool IsAnyDeletedResourcesCondition( IEnumerable<IResource> conditions )
        {
            bool hasDateCondition = false;
            if( conditions != null )
            {
                foreach( IResource res in conditions )
                    hasDateCondition = hasDateCondition || IsDeletedResourcesCondition( res );
            }
            return hasDateCondition;
        }

        private static bool  IsDeletedResourcesCondition( IResource res )
        {
            string name = res.GetStringProp( Core.Props.Name );
            return ( name != null ) && ( name == StandardConditions.ResourceIsDeletedName );
        }

        private static bool IsAnyDateCondition( IEnumerable<IResource> conditions )
        {
            bool hasDateCondition = false;
            if( conditions != null )
            {
                foreach( IResource res in conditions )
                {
                    string  propName = res.GetStringProp( _props._appliedToNameProp );
                    hasDateCondition = hasDateCondition || ResourceTypeHelper.IsDateProperty( propName );
                }
            }
            return hasDateCondition;
        }

        private static bool IsAnyDateCondition( IEnumerable<IResource[]> conditionGroups )
        {
            bool hasDateCondition = false;
            if( conditionGroups != null )
            {
                foreach( IResource[] list in conditionGroups )
                {
                    hasDateCondition = hasDateCondition || IsAnyDateCondition( list );
                }
            }
            return hasDateCondition;
        }

	    internal static bool IsSearchResultsView( IResource view )
        {
            string deepName = view.GetStringProp( "DeepName" );
            return (deepName != null) && (deepName == FilterManagerProps.SearchResultsViewName);
        }

        internal static bool IsDeletedResourcesView( IResource view )
        {
            return (view != null) && (view.Type == FilterManagerProps.ViewResName) &&
                   (view.GetStringProp( "DeepName" ) == FilterManagerProps.ViewDeletedItemsDeepName);
        }

        internal static bool isParentRelationship( IResource res1, IResource res2 )
        {
            IResource parent = res1.GetLinkProp( Core.Props.Parent );
            while( parent != null )
            {
                if( parent.Id == res2.Id )
                    return true;
                parent = parent.GetLinkProp( Core.Props.Parent );
            }
            return false;
        }

        private static bool  isLinkPresent( string resLinks, IResource res )
        {
            bool        hasLink = false;
            string[]    links = resLinks.Split( '|' );
            foreach( string link in links )
                hasLink = hasLink || (res.GetLinksOfType( null, link ).Count > 0);
            return hasLink;
        }
        #endregion Predicates

        public static IResource[][]  Convert2Group( IResource condition )
        {
            IResource[][] group = new IResource[ 1 ][];
            group[ 0 ] = new IResource[ 1 ] { condition };
            return group;
        }

        public static IResource[][]  Convert2Group( IResource[] list )
        {
            IResource[][] groups = null;
            if( list != null && list.Length > 0 )
                groups = new IResource[ 1 ][] { list };

            return groups;
        }

        private static string[]  CollectSetValuesFromCondition( IResource condition )
        {
            IResourceList   setValuesResources = condition.GetLinksOfType( null, _props._setValueLinkProp );
            Debug.Assert( setValuesResources.Count > 0, "Illegal number of set values in the condition - must be positive" );
            string[]        setValues = new string[ setValuesResources.Count ];

            for( int i = 0; i < setValuesResources.Count; i++ )
                setValues[ i ] = (string)(setValuesResources[ i ].GetProp( _props._conditionValProp ));

            return( setValues );
        }

        #region Delete Conditions/Actions/Resources
        public static void  DeleteInternalConditions( IResource view )
        {
            if( !Core.ResourceAP.IsOwnerThread )
                Core.ResourceAP.RunJob( new ResourceDelegate( DeleteInternalConditions ), view );
            else
            {
                IResourceList   groups = view.GetLinksOfType( FilterManagerProps.ConjunctionGroup, _props._linkedConditionsLink );
                IResourceList   conditions = Core.ResourceStore.EmptyResourceList;
                foreach( IResource group in groups )
                    conditions = conditions.Union( group.GetLinksOfType( FilterManagerProps.ConditionResName, _props._linkedConditionsLink ) );
                conditions = conditions.Union( view.GetLinksOfType( FilterManagerProps.ConditionResName, _props._linkedExceptionsLink ), true );
                conditions = conditions.Intersect( Core.ResourceStore.FindResources( null, "InternalView", 1 ) );
                conditions.DeleteAll();

                //  Removal of internal conditions is related to the destroy process of
                //  a view. Thus we do not need conjunction groups anyway since they
                //  will be recreated anew later.
                groups.DeleteAll();
            }
        }

        private static void  DeleteInternalActions( IResource rule )
        {
            if( !Core.ResourceAP.IsOwnerThread )
                Core.ResourceAP.RunJob( new ResourceDelegate( DeleteInternalActions ), rule );
            else
            {
                IResourceList  actions = rule.GetLinksOfType( FilterManagerProps.RuleActionResName, _props._linkedActionsLink );
                foreach( IResource res in actions )
                {
                    if(( res.Type == FilterManagerProps.RuleActionResName ) && ( res.GetLinkCount( _props._templateLink ) == 1 ))
                        res.Delete();
                }
            }
        }

        private static void  DeleteResource( IResource res )
        {
            new ResourceProxy( res ).Delete();
        }

        //---------------------------------------------------------------------
        //  Remove links from all resources which are used as parameters to the
        //  condition/action templates.
        //  NB: Links "UsedByRule" are directed from the outside resource to the
        //      rule under modification.
        //---------------------------------------------------------------------
        private static void CleanLinkedRuleParameters( IResource rule )
        {
            if( !Core.ResourceAP.IsOwnerThread )
                Core.ResourceAP.RunJob( new ResourceDelegate( CleanLinkedRuleParameters ), rule );
            else
            {
                IResourceList linkedParams = rule.GetLinksOfType( null, "UsedByRule" );
                foreach( IResource res in linkedParams )
                    res.DeleteLink( "UsedByRule", rule );
            }
        }

        //---------------------------------------------------------------------
        //  When a rule or view is modified (re-registered), then we recreate a
        //  new structure over the existed one (only cleaning the proxy conditions
        //  to templates). Thus sometimes we have old conjunction groups hanging
        //  which are linked to the existing conditions but not to views/rules.
        //  It is more explicit to delete them all together here than spread that
        //  over different operations.
        //---------------------------------------------------------------------
        private static void  DeleteHangedConjunctionGroups()
        {
            if( !Core.ResourceAP.IsOwnerThread )
                Core.ResourceAP.RunJob( new MethodInvoker( DeleteHangedConjunctionGroups ) );
            else
            {
                IResourceList groups = Core.ResourceStore.GetAllResources( FilterManagerProps.ConjunctionGroup );
                foreach( IResource group in groups )
                {
                    IResourceList linked = group.GetLinksOfType( FilterManagerProps.ViewResName, _props._linkedConditionsLink ).Union(
                                           group.GetLinksOfType( FilterManagerProps.RuleResName, _props._linkedConditionsLink )).Union(
                                           group.GetLinksOfType( FilterManagerProps.ViewCompositeResName, _props._linkedConditionsLink ));
                    if( linked.Count == 0 )
                        group.Delete();
                }
            }
        }
        #endregion Delete Conditions/Actions/Resources

        #region SetProperty
        private static void SetProperty( IResource res, string propName, object propValue )
        {
            SetProperty( res, Core.ResourceStore.PropTypes[ propName ].Id, propValue );
        }
        private static void SetProperty( IResource res, int propId, object propValue )
        {
            ResourceProxy   proxy = new ResourceProxy( res );
            SetProperty( proxy, propId, propValue );
        }
        private static void SetProperty( ResourceProxy proxy, int propId, object propValue )
        {
            proxy.BeginUpdate();
            proxy.SetProp( propId, propValue );
            proxy.EndUpdate();
        }
        #endregion SetProperty

        #region Add/Copy Links
        private static void  CopyLinks( IResource res, ResourceProxy proxyRes, int linkId )
        {
            IResourceList list = res.GetLinksOfType( null, linkId );
            AddLinks( proxyRes, list, linkId );
        }

        private static void AddLinks( ResourceProxy proxyView, IResource[][] groups, int linkId, string checkType )
        {
            if( groups != null && groups.Length > 0 )
            {
                proxyView.DeleteLinks( linkId );
                foreach( IResource[] list in groups )
                {
                    ResourceProxy group = ResourceProxy.BeginNewResource( FilterManagerProps.ConjunctionGroup );
                    group.EndUpdate();

                    AddLinks( group, list, linkId, checkType );
                    proxyView.AddLink( linkId, group.Resource );
                }
            }
        }
        private static void AddLinks( ResourceProxy proxyView, IResource[] list, int linkId, string checkType )
        {
            proxyView.DeleteLinks( linkId );
            if( list != null )
            {
                foreach( IResource res in list )
                {
                    if( checkType != null && res.Type != checkType )
                        throw new InvalidOperationException( "FilterManager -- Illegal type of resource is given for linking: " + 
                                                             res.Type + " (type " + checkType + " was expected)" );
                    proxyView.AddLink( linkId, res );
                }
            }
        }
        private static void AddLinks( ResourceProxy proxyView, IResourceList list, int prop )
        {
            proxyView.DeleteLinks( prop );
            if( list != null )
            {
                foreach( IResource res in list )
                    proxyView.AddLink( prop, res );
            }
        }
        #endregion Add/Copy Links

        #region ResourceType format setting/conversion
        private static void SetApplicableResType( IResource res, string[] resTypes )
        {
            ResourceProxy   proxy = new ResourceProxy( res );
            proxy.BeginUpdate();
            SetApplicableResType( proxy, resTypes );
            proxy.EndUpdate();
        }
        private static void  SetApplicableResType( ResourceProxy proxy, string[] allResTypes )
        {
            if( allResTypes != null && allResTypes.Length > 0 )
            {
                string[] resTypes, linkTypes;
                ResourceTypeHelper.ExtractFields( allResTypes, out resTypes, out linkTypes );

                proxy.SetProp( Core.Props.ContentType, (resTypes.Length > 0) ? String.Join( "|", resTypes ) : null );
                proxy.SetProp( "ContentLinks", (linkTypes.Length > 0) ? String.Join( "|", linkTypes ) : null );
            }
            else
            {
                proxy.DeleteProp( Core.Props.ContentType );
                proxy.DeleteProp( "ContentLinks" );
            }
        }

        public static string[]  CompoundType( IResource view )
        {
            string resTypes = view.GetStringProp( Core.Props.ContentType ),
                   linkTypes = view.GetStringProp( "ContentLinks" );
            if( linkTypes != null )
            {
                if( resTypes != null )
                    resTypes = resTypes + '|';
                resTypes += linkTypes;
            }

            return (resTypes == null) ? null : resTypes.Split( '|' );
        }
        #endregion ResourceType format setting/conversion

        private static void  AssignProperty( IResource res, ResourceProxy proxy, string propName )
        {
            object o = res.GetProp( propName );
            if( o != null )
                proxy.SetProp( propName, o );
        }

        private IResourceList GetResourcesOfType( string resTypeCompound, string resLinks )
        {
            IResourceList   result = Store.EmptyResourceList;

            if( resTypeCompound != null || resLinks != null )
            {
                string[]  formats, resTypes;
                string[]  linkTypes = null; 
                if ( resLinks != null )
                {
                    linkTypes = resLinks.Split( '|' );   
                }
                ResourceTypeHelper.ExtractFormatFields( resTypeCompound, out resTypes, out formats );

                IResourceList linkResult = Store.EmptyResourceList;
                for( int i = 0; i < resTypes.Length; i++ )
                {
                    result = result.Union( Store.GetAllResourcesLive( resTypes[ i ] ), true );
                }

                if ( linkTypes != null )
                {
                    for( int i = 0; i < linkTypes.Length; i++ )
                    {
                        linkResult = linkResult.Union( Store.FindResourcesWithPropLive( null, linkTypes[ i ] ), true );
                    }
                }

                if( formats.Length > 0 )
                {
                    IResourceList linkRestr = Store.EmptyResourceList;
                    foreach( string type in formats )
                    {
                        linkRestr = linkRestr.Union(
                            linkResult.Intersect( Store.GetAllResources( type ), true ), true );
                    }

                    linkResult = linkRestr;
                }
                result = result.Union( linkResult, true );
            }

            return result;
        }

        public IResourceList   GetViews()
        {
            IResourceList result = Core.ResourceStore.GetAllResourcesLive( FilterManagerProps.ViewResName );
            result = result.Minus( Core.ResourceStore.FindResourcesWithProp( null, "IsTrayIconFilter" ) );
            result = result.Minus( Core.ResourceStore.FindResourcesWithProp( null, "IsFormattingFilter" ) );
            return result;
        }

        public IResourceList GetFormattingRules( bool visible )
        {
            IResourceList result = Core.ResourceStore.FindResourcesLive( null, "IsFormattingFilter", true ).Intersect( 
                                   Core.ResourceStore.FindResourcesLive( null, _props._invisibleProp, (!visible) ), true );
            return result;
        }

        public IResourceList GetConditionsPlain( IResource res )
        {
            #region Preconditions
            if( res.Type != FilterManagerProps.RuleResName && res.Type != FilterManagerProps.ViewResName && res.Type != FilterManagerProps.ViewCompositeResName )
                throw new InvalidOperationException( "FilterManager -- input resource is not a view or a rule" );
            #endregion Preconditions

            IResourceList conditions = Core.ResourceStore.EmptyResourceList;
            IResourceList groups = res.GetLinksOfType( FilterManagerProps.ConjunctionGroup, _props._linkedConditionsLink );
            foreach( IResource group in groups )
            {
                conditions = conditions.Union( group.GetLinksOfType( FilterManagerProps.ConditionResName, _props._linkedConditionsLink ));
            }

            return conditions;
        }

        public IResourceList GetConditions( IResource res )
        {
            #region Preconditions
            if( res.Type != FilterManagerProps.RuleResName && res.Type != FilterManagerProps.ViewResName &&
                res.Type != FilterManagerProps.ViewCompositeResName && res.Type != FilterManagerProps.ConjunctionGroup )
                throw new InvalidOperationException( "FilterManager -- input resource is not a view or a rule" );
            #endregion Preconditions

            return res.GetLinksOfType( FilterManagerProps.ConditionResName, _props._linkedConditionsLink );
        }

        public IResourceList GetExceptions( IResource res )
        {
            #region Preconditions
            if( res.Type != FilterManagerProps.RuleResName && res.Type != FilterManagerProps.ViewResName && res.Type != FilterManagerProps.ViewCompositeResName )
                throw new InvalidOperationException( "FilterManager -- input resource is not a view or a rule" );
            #endregion Preconditions

            return res.GetLinksOfType( FilterManagerProps.ConditionResName, _props._linkedExceptionsLink );
        }

        public IResourceList GetActions( IResource ruleOrView )
        {
            #region Preconditions
            if( ruleOrView.Type != FilterManagerProps.RuleResName )
                throw new InvalidOperationException( "FilterManager -- input resource is not a rule" );
            #endregion Preconditions

            return ruleOrView.GetLinksOfType( FilterManagerProps.RuleActionResName, _props._linkedActionsLink );
        }

        public IResourceList GetLinkedConditions( IResource conditionTemplate )
        {
            #region Preconditions
            if( conditionTemplate.Type != FilterManagerProps.ConditionTemplateResName )
                throw new InvalidOperationException( "FilterManager -- input resource is not a condition template" );
            #endregion Preconditions

            return conditionTemplate.GetLinksOfType( FilterManagerProps.ConditionResName, _props._templateLink );
        }

        private IResource  CreateConditionGroup( string groupName )
        {
            IResource group = Store.FindUniqueResource( FilterManagerProps.ConditionGroupResName, Core.Props.Name, groupName );
            if( group == null )
            {
                ResourceProxy proxy = ResourceProxy.BeginNewResource( FilterManagerProps.ConditionGroupResName );
                proxy.BeginUpdate();
                proxy.SetProp( Core.Props.Name, groupName );

                if( groupName != cAllConditionGroups )
                {
                    IResource groupsRoot = Store.FindUniqueResource( FilterManagerProps.ConditionGroupResName, Core.Props.Name, cAllConditionGroups );
                    proxy.SetProp( Core.Props.Parent, groupsRoot );
                }
                proxy.EndUpdate();
                group = proxy.Resource;
            }
            return group;
        }

        private static void ShowMessageBox( string ruleName )
        {
             MessageBox.Show( "FilterManager - Rule [" + ruleName + "] has broken actions. Please, reenter the rule." );
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

        public static IResource  FindRuleByName( string name, string filterType )
        {
            IResourceList rules = Core.ResourceStore.FindResourcesWithProp( null, filterType );
            foreach( IResource res in rules )
            {
                if( res.GetStringProp( Core.Props.Name ) == name )
                    return res;
            }
            return null;
        }

        public static string ConstructQuery( IResource res )
        {
            //  NOTE: do not change "ApplicableToProp" to int since it is
            //        possible to call this method befor FilterManager's initialisation.
            string query = res.GetStringProp( "ApplicableToProp" ); /*FilterManager.ApplicableToProp*/
            if( res.HasProp( "SectionOrder" ))
            {
                uint order = (uint)res.GetIntProp( "SectionOrder" );
                query = "(" + query + ") [" + DocSectionHelper.FullNameByOrder( order ) + "]";
            }
            return query;
        }
        #endregion Auxiliaries

        #region Attributes

        public string  ViewNameForSearchResults    {  get{ return( FilterManagerProps.SearchResultsViewName ); }  }

        public  const   string  ExternalFileTag = "ExternalFile";
        public  const   string  ExternalDirTag = "ExternalDir";
        public  const   string  RuleApplicableResourceTypeResName = "RuleApplicableResourceType";
        private const   char    cPropertyPathDelimiter = '>';
        private const   string  cAllConditionGroups = "AllConditionGroups";
        //                                                     Dec Jan Feb Mar Apr May Jun Jul Aug Sep Oct Nov
        private readonly int[]  DaysPerMonth = new int[ 12 ] { 31, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30 };

        //---------------------------------------------------------------------
        private static readonly IStandardConditions  StandardConditions = new FilterManagerStandards();
        private static readonly FilterManagerUIHandler _UIHandler = new FilterManagerUIHandler();

        private static FilterManagerProps _props;

        private DateTime        Today, Yesterday, Tomorrow, WeekStart, NextWeekStart, MonthStart, YearStart;
        private readonly Hashtable   CustomConditions = new Hashtable();
        private readonly Hashtable   RuleActions = new Hashtable();
        private readonly Hashtable   RegisteredEvents = new Hashtable();
        private static readonly Hashtable RegisteredUIHandlers = new Hashtable();

        private readonly IResourceStore  Store;
        private bool            IsVerbose = false;

        private static string[] _lastStopList;
        internal static string  _lastQueryError;
        internal static IHighlightDataProvider Highlighter = null;
        #endregion Attributes
    }

    #region ActionParameterStore
    public class ActionParameterStore : IActionParameterStore
    {
        public ActionParameterStore( IResource action )
        {
            #region Preconditions
            if( action.Type != FilterManagerProps.RuleActionResName &&
                action.Type != FilterManagerProps.ConditionResName )
            {
                throw new ArgumentException( "FilterManager -- RuleAction or SearchCondition resource type is expected for transformation" );
            }
            #endregion Preconditions

            Action = action;
        }
        public IResourceList   ParametersAsResList()
        {
            return Action.GetLinksOfType( null, "LinkedSetValue" );
        }
        public string  ParameterAsString()
        {
            return Action.GetStringProp( "ConditionVal" );
        }

        private readonly IResource  Action;
    }
    #endregion ActionParameterStore
}

