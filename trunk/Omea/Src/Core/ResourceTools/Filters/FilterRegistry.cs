/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections.Generic;
using System.Data;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using JetBrains.Omea.Base;
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

	public class FilterRegistry: IFilterRegistry
	{
        #region Ctor and Initialization
        public  FilterRegistry( IResourceStore store )
        {
            Store = store;
            RegisterTypes();
            _props = new FilterManagerProps( store );
        }

        public IStandardConditions Std
        {
            get { return StandardConditions; }
        }

	    public IFilterManagerProps Props
	    {
	        get
            {
                #region Preconditions
                if ( _props == null )
                    throw new ApplicationException( "FilterRegistry (InternalError) -- Filter PROPS are used before initialization." );
                #endregion Preconditions

	            return _props;
	        }
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
//            Store.PropTypes.Register( "QueryRulesRerunRequired", PropDataType.Bool, PropTypeFlags.Internal );
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
                throw new ArgumentNullException( "name", "FilterRegistry -- Name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterRegistry -- Deep Name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( query ) )
                throw new ArgumentNullException( "query", "FilterRegistry -- Query string is NULL or empty" );
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
                throw new ArgumentNullException( "name", "FilterRegistry -- Name of a condition is NULL or empty." );
            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterRegistry -- Deep Name of a condition is NULL or empty." );
            if( String.IsNullOrEmpty( query ) )
                throw new ArgumentNullException( "query", "FilterRegistry -- Query string is NULL or empty" );
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
                throw new ArgumentNullException( "name", "FilterRegistry -- Name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterRegistry -- Deep Name of a condition is NULL or empty." );
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
                throw new ArgumentNullException( "name", "FilterRegistry -- Name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterRegistry -- Deep Name of a condition is NULL or empty." );
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
                throw new ArgumentNullException( "name", "FilterRegistry -- Name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterRegistry -- Deep Name of a condition is NULL or empty." );
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
                throw new ArgumentNullException( "name", "FilterRegistry -- Name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterRegistry -- Deep Name of a condition is NULL or empty." );

            if( val == null )
                throw new ArgumentNullException( "val", "FilterRegistry -- List of Resource parameters can not be NULL." );

            if(( val.Count == 0 ) || ( op != ConditionOp.Eq && op != ConditionOp.In ))
                throw new ArgumentException( "FilterRegistry -- Number or type of parameters mismatches the type of Operation" );
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
                throw new ArgumentNullException( "propName", "FilterRegistry -- Property name can not be NULL or empty." );
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
                throw new ArgumentNullException( "propName", "FilterRegistry -- Property name can not be NULL or empty." );
            #endregion Preconditions

            IResource res = RecreateStandardConditionImpl( types, propName, op, val );
            SetProperty( res, "InternalView", 1 );
            return( res );
        }
        public  IResource CreateQueryConditionAux( string[] types, string query, string sectionName )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( query ) )
                throw new ArgumentNullException( "query", "FilterRegistry -- Query can not be NULL or empty" );

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
                throw new ArgumentNullException( "propName", "FilterRegistry -- Property name is NULL or empty." );

            //  Cyclic properties can be used only in conjunction with
            //  ConditionOp.In operation
            if( propName.IndexOf( '*' ) == propName.Length - 1 &&
               ( op != ConditionOp.In && op != ConditionOp.QueryMatch ))
                throw new ArgumentException( "FilterRegistry -- Cyclic properties can be used only in conjunction with In operation" );

            //  "hasNoProp" conditions are not applied to NULL resource types
            if( op == ConditionOp.HasNoProp && ( types == null || types.Length == 0 ))
                throw new ArgumentException( "FilterRegistry -- [hasNoProp] operation cannot be applied to NULL resource types" );

            //  If present, all arguments in "val" array must be valid strings.
            //  Exception is made for Query conditions, which accept NULL as
            //  section name parameter.
            if( op != ConditionOp.QueryMatch )
            {
                foreach( string str in val )
                {
                    if( String.IsNullOrEmpty( str ))
                        throw new ArgumentNullException( "op", "FilterRegistry -- Each operation parameter must be a valid string (not NULL or empty)." );
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
                throw new ArgumentException( "FilterRegistry -- Number of value parameters mismatches type of Operation" );
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
                throw new ArgumentNullException( "propName", "FilterRegistry -- Name of a property is NULL or empty." );
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
                    throw new ArgumentException( "FilterRegistry -- Section name length is 0." );

                IResource section = Store.FindUniqueResource( "DocumentSection", Core.Props.Name, sectionName );
                if( section == null )
                    throw new InvalidOperationException( "FilterRegistry -- Illegal name of the section " + sectionName + " in Filter Manager core" );

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
                throw new ArgumentNullException( "name", "FilterRegistry -- Name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterRegistry -- Deep Name of a condition is NULL or empty." );

            if( filter == null )
                throw new ArgumentNullException( "filter", "FilterRegistry -- ICustomCondition object is NULL." );
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
            _customConditions[ name ] = filter;

            return( condition );
        }
        #endregion CustomConditions

        public void  MarkConditionOnlyForRule( IResource condition )
        {
            #region Preconditions
            if( condition.Type != FilterManagerProps.ConditionResName && condition.Type != FilterManagerProps.ConditionTemplateResName )
                throw new ApplicationException( "FilterRegistry -- input resource type is not [Condition]" );
            #endregion Preconditions

            SetProperty( condition, "IsOnlyForRule", true );
        }

        public IResource  CloneCondition( IResource condition )
        {
            #region Preconditions
            if( condition.Type != FilterManagerProps.ConditionResName )
                throw new ApplicationException( "FilterRegistry -- input resource type is not [Condition]" );
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
                throw new ArgumentNullException( "cond", "FilterRegistry -- Condition resource can not be NULL" );

            if( string.IsNullOrEmpty( groupName ) )
                throw new ArgumentNullException( "groupName", "FilterRegistry -- Invalid Group parameter passed - can not be NULL or empty." );

            if( cond.Type != FilterManagerProps.ConditionResName && cond.Type != FilterManagerProps.ConditionTemplateResName )
                throw new ArgumentException( "FilterRegistry -- Condition parameter has invalid type [" + cond.Type + "]" );
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
                throw new ArgumentNullException( "name", "FilterRegistry -- Name of a condition can not be NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterRegistry -- Deep Name of a condition is NULL or empty." );

            if( handler == null )
                throw new ArgumentNullException( "handler", "FilterRegistry -- UI handler for the condition template is NULL." );
            #endregion Preconditions

            IResource template = RecreateConditionTemplate( name, deepName, types, op, parameters );
            _registeredUIHandlers[ name ] = handler;

            return template;
        }

        public IResource   CreateConditionTemplateWithUIHandler( string name, string deepName, string[] types,
                                                                 ITemplateParamUIHandler handler,
                                                                 ConditionOp op, params string[] parameters )
        {
            #region Preconditions
            if( string.IsNullOrEmpty( name ))
                throw new ArgumentNullException( "name", "FilterRegistry -- Name of a condition can not be NULL or empty." );

            if( string.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterRegistry -- Deep Name of a condition is NULL or empty." );

            if( handler == null )
                throw new ArgumentNullException( "handler", "FilterRegistry -- UI handler for the condition template is NULL." );
            #endregion Preconditions

            IResource template = Store.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, "DeepName", deepName );
            if( template == null )
                template = RecreateConditionTemplate( name, deepName, types, op, parameters );
            _registeredUIHandlers[ name ] = handler;

            return template;
        }

        public IResource  CreateConditionTemplate( string name, string deepName, string[] types,
                                                   ConditionOp op, params string[] parameters )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( name ))
                throw new ArgumentNullException( "name", "FilterRegistry -- Name of a condition can not be NULL or empty" );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterRegistry -- Deep Name of a condition is NULL or empty." );
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
                throw new ArgumentNullException( "name", "FilterRegistry -- Name of a condition can not be NULL or empty" );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterRegistry -- Deep Name of a condition is NULL or empty." );

            //  If Op is not query, there MUST be at least one argument!!!
            if(( op != ConditionOp.QueryMatch ) && ( parameters == null ))
                throw new ArgumentNullException( "parameters", "FilterRegistry -- Operation for a template requires arguments." );

            //  Requires that all parameters are valid strings
            if( parameters != null )
            {
                foreach( string str in parameters )
                {
                    if( String.IsNullOrEmpty( str ))
                        throw new ArgumentNullException( "parameters", "FilterRegistry -- Each operation parameter must be a valid string (not NULL or empty)." );
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
                    throw new ArgumentException( "FilterRegistry -- Property " + parameters[ 0 ] + " does not exist." );
                if( pt.DataType != PropDataType.Int && pt.DataType != PropDataType.Date )
                    throw new ArgumentException( "FilterRegistry -- Type of the property " + parameters[ 0 ] + " does not allow comparison operations." );

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
            _customConditions[ deepName ] = filter;
            new ResourceProxy( template ).SetProp( _props._conditionTypeProp, "custom" );
            return template;
        }

        public static void  ReferCondition2Template( IResource res, string templateName )
        {
            #region Preconditions
            if( res == null )
                throw new ArgumentNullException( "res", "FilterRegistry -- Condition resource can not be NULL" );

            if( String.IsNullOrEmpty( templateName ))
                throw new ArgumentNullException( "templateName", "FilterRegistry -- Template name can not be NULL or empty" );

            IResource template = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, Core.Props.Name, templateName );
            if( template == null )
                throw new ArgumentException( "FilterRegistry -- No such condition template " + templateName + " for association" );
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
                throw new ArgumentNullException( "templateName", "FilterRegistry -- Name of a template is NULL." );
            #endregion Preconditions

            return (ITemplateParamUIHandler) _registeredUIHandlers[ templateName ];
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
                throw new ArgumentException( "FilterRegistry -- List of conditions can not be empty for NULL or empty list of resource types." );

            if( types != null )
            {
                bool allTypesValid = true;
                foreach( string str in types )
                {
                    allTypesValid = allTypesValid &&
                                    (Core.ResourceStore.ResourceTypes.Exist( str ) || Core.ResourceStore.PropTypes.Exist( str ));
                }
                if( !allTypesValid )
                    throw new ArgumentException( "FilterRegistry -- Resource types array contains illegal type." );
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
                throw new ArgumentNullException( "view", "FilterRegistry -- View parameter is null." );

            if( string.IsNullOrEmpty( name ))
                throw new ArgumentException( "FilterRegistry -- Invalid view name" );

            if(( groups == null || groups.Length == 0 ) &&
               ( types == null || types.Length == 0 ))
                throw new ArgumentException( "FilterRegistry -- List of conditions and exceptions can not be NULL or empty for NULL or empty list of resource types." );

            if( types != null )
            {
                bool allTypesValid = true;
                foreach( string str in types )
                {
                    allTypesValid = allTypesValid &&
                                    (Core.ResourceStore.ResourceTypes.Exist( str ) || Core.ResourceStore.PropTypes.Exist( str ));
                }
                if( !allTypesValid )
                    throw new ArgumentException( "FilterRegistry -- Resource types array contains illegal type." );
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
                throw new ArgumentNullException( "view", "FilterRegistry -- View parameter is null." );
            if( view.Type != FilterManagerProps.ViewResName )
                throw new ArgumentException( "FilterRegistry -- View parameter has illegal type [" + view.Type + "]" );
            #endregion Preconditions

            new ResourceProxy( view ).SetProp( "ShowInAllTabs", true );
        }
        public bool  IsVisibleInAllTabs( IResource view )
        {
            #region Preconditions
            if( view == null )
                throw new ArgumentNullException( "view", "FilterRegistry -- View parameter is null." );
            if( view.Type != FilterManagerProps.ViewResName )
                throw new ArgumentException( "FilterRegistry -- View parameter has illegal type [" + view.Type + "]" );
            #endregion Preconditions

            return view.HasProp( "ShowInAllTabs" );
        }

        //---------------------------------------------------------------------
        public void  DeleteView( string viewName )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( viewName ) )
                throw new ArgumentNullException( "viewName", "FilterRegistry -- Name of a view can not be NULL" );
            #endregion Preconditions

            IResourceList views = Store.FindResources( SelectionType.Normal, FilterManagerProps.ViewResName, Core.Props.Name, viewName );
            foreach( IResource res in views )
                DeleteView( res );
        }
        public void  DeleteView( IResource view )
        {
            #region Preconditions
            if( view == null )
                throw new ArgumentNullException( "view", "FilterRegistry -- View resource can not be NULL" );
            #endregion Preconditions

            DeleteInternalConditions( view );
            DeleteResource( view );
            DeleteHangedConjunctionGroups();
        }

        public IResource CloneView( IResource from, string newName )
        {
            #region Preconditions
            if( from == null )
                throw new ArgumentNullException( "from", "FilterRegistry -- Source view is NULL." );

            if( String.IsNullOrEmpty( newName ) )
                throw new ArgumentNullException( "newName", "FilterRegistry -- New name is NULL or empty." );
            #endregion Preconditions

            IResource[][] conditions;
            IResource[]   exceptions;
            CloneConditionTypeLinks( from , out conditions, out exceptions );

            string[] formTypes = CompoundType( from );
            IResource newView = Core.FilterRegistry.RegisterView( newName, formTypes, conditions, exceptions );

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
                throw new ArgumentNullException( "from", "FilterRegistry -- Source resource is NULL." );
            if( to == null )
                throw new ArgumentNullException( "to", "FilterRegistry -- Target resource is NULL." );
            if( String.IsNullOrEmpty( newName ) )
                throw new ArgumentNullException( "newName", "FilterRegistry -- New name is NULL or empty." );
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
            IFilterRegistry mgr = Core.FilterRegistry;
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
                newList[ i ] = Core.FilterRegistry.CloneCondition( list[ i ] );
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
                throw new ArgumentNullException( "name", "FilterRegistry -- Folder name is NULL or empty" );

            if( baseFolderName != null && baseFolderName.Length == 0 )
                throw new ArgumentException( "FilterRegistry -- Base folder name is not NULL but its Length is 0" );
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
                throw new ArgumentNullException( "view", "FilterRegistry -- input view resource is NULL" );

            if( view.Type != FilterManagerProps.ViewResName )
                throw new ArgumentException( "FilterRegistry -- input view resource has inappropriate type: [" + view.Type + "]" );

            if( folderName != null && Store.FindUniqueResource( FilterManagerProps.ViewFolderResName, Core.Props.Name, folderName ) == null )
                throw new ArgumentException( "FilterRegistry -- input View Folder does not exist" );
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
                throw new ArgumentNullException( "name", "FilterRegistry -- Rule name can not be NULL or empty." );

            if( conditions == null && exceptions == null )
                throw new ArgumentException( "FilterRegistry -- conditions list and exceptions list can not be empty simultaneously" );
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
                throw new ArgumentNullException( "exRule", "FilterRegistry -- Resource can not be NULL for reregistering." );

            if( conditions == null && exceptions == null )
                throw new ArgumentException( "FilterRegistry -- conditions list and exceptions list can not be empty simultaneously" );
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
                throw new ArgumentNullException( "name", "FilterRegistry -- Name of an action can not be NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterRegistry -- Deep name of an action can not be NULL or empty." );

            if( executor == null )
                throw new ArgumentNullException( "executor", "FilterRegistry -- Execution handler for an action is NULL." );
            #endregion Preconditions

            IResource action = Core.ResourceStore.FindUniqueResource( FilterManagerProps.RuleActionResName, "DeepName", deepName );
            if( action == null )
                action = Core.ResourceStore.NewResource( FilterManagerProps.RuleActionResName );

            action.SetProp( Core.Props.Name, name );
            action.SetProp( "DeepName", deepName );
            SetApplicableResType( action, types );
            _ruleActions[ name ] = executor;

            return( action );
	    }

        public IResource  RegisterRuleActionTemplateWithUIHandler( string name, string deepName, IRuleAction executor,
                                                                   string[] types, ITemplateParamUIHandler handler,
                                                                   ConditionOp op, params string[] values )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( name ))
                throw new ArgumentNullException( "name", "FilterRegistry -- Name of an action template can not be NULL or empty." );

            if( String.IsNullOrEmpty( deepName ))
                throw new ArgumentNullException( "deepName", "FilterRegistry -- Deep name of an action template can not be NULL or empty." );

            if( handler == null )
                throw new ArgumentNullException( "handler", "FilterRegistry -- UI handler for the action template is NULL." );
            #endregion Preconditions

            IResource action = RegisterRuleActionTemplate( name, deepName, executor, types, op, values );
            _registeredUIHandlers[ name ] = handler;

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

            _ruleActions[ name ] = executor;
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
                throw new InvalidOperationException( "FilterRegistry -- input parameter is not of type [" + 
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

            return ( name != null && _ruleActions.ContainsKey( name ) );
        }

        public void MarkActionTemplateAsSingleSelection( IResource actionTemplate )
        {
            #region Preconditions
            if( actionTemplate.Type != FilterManagerProps.RuleActionTemplateResName )
                throw new InvalidOperationException( "FilterRegistry -- input parameter is not of type [" + FilterManagerProps.RuleActionTemplateResName + "]" );
            #endregion Preconditions

            actionTemplate.SetProp( "IsSingleSelection", true );
        }

        public IResource  CloneAction( IResource action )
        {
            #region Preconditions
            if( action.Type != FilterManagerProps.RuleActionResName )
                throw new ApplicationException( "FilterRegistry -- input resource type is not Action" );
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
                throw new ArgumentNullException( "rule", "FilterRegistry -- Input rule resource is null." );

            if( rule.Type != FilterManagerProps.RuleResName )
                throw new ArgumentException( "FilterRegistry -- Input rule resource has inappropriate type." );

            if( String.IsNullOrEmpty( newName ) )
                throw new ArgumentNullException( "newName", "FilterRegistry -- New name for a rule is null or empty." );

            if( FindRuleByName( newName, "IsActionFilter" ) != null )
                throw new ArgumentException( "FilterRegistry -- An action rule with new name already exists." );
            #endregion Preconditions

            new ResourceProxy( rule ).SetProp( Core.Props.Name, newName );
        }

        //  Accept not only Rules but Formatting rules as well (which are
        //  View internally)
        public void  ActivateRule( IResource rule )
        {
            #region Preconditions
            if( rule == null )
                throw new ArgumentNullException( "rule", "FilterRegistry -- Input rule resource is null." );
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
                throw new ArgumentNullException( "rule", "FilterRegistry -- Input rule resource is null." );
            #endregion Preconditions

            SetProperty( rule, "RuleTurnedOff", true );
        }

        public void  MarkRuleAsInvalid( IResource rule, string reason )
        {
            #region Preconditions
            if( rule == null )
                throw new ArgumentNullException( "rule", "FilterRegistry -- Input rule resource is null." );
            #endregion Preconditions
            
            DeactivateRule( rule );
            SetProperty( rule, Core.Props.LastError, reason );
        }

        public void  AssignOrderNumber( IResource rule, int num )
        {
            #region Preconditions
            if( rule == null )
                throw new ArgumentNullException( "rule", "FilterRegistry -- Input rule resource is null." );
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
                throw new ArgumentNullException( "oldName", "FilterRegistry -- Old name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( newName ) )
                throw new ArgumentNullException( "newName", "FilterRegistry -- New name of a condition is NULL or empty." );

            //  Check that the new condition has been actually created.
            IResource newRes = Store.FindUniqueResource( FilterManagerProps.ConditionResName, Core.Props.Name, newName );
            if( newRes == null )
            {
                throw new ConstraintException( "FilterRegistry -- A condition with new name must be already created before rename." );
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
                throw new ArgumentNullException( "oldName", "FilterRegistry -- Old name of a condition is NULL or empty." );

            if( String.IsNullOrEmpty( newName ) )
                throw new ArgumentNullException( "newName", "FilterRegistry -- New name of a condition is NULL or empty." );

            //  Check that the new condition has been actually created.
            IResource newRes = Store.FindUniqueResource( FilterManagerProps.ConditionTemplateResName, Core.Props.Name, newName );
            if( newRes == null )
            {
                throw new ConstraintException( "FilterRegistry -- A condition with new name must be already created before rename." );
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
                _ruleActions[ newName ] = _ruleActions[ oldName ];
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
                    _ruleActions[ newName ] = _ruleActions[ oldName ];
                }
                else
                {
//                    throw new ArgumentException( "FilterRegistry -- No rule action or action template with name [" + oldName + "] exists" );
                    //  do not raise exception since rename to the inproper
                    //  names could not be repaired else.
                    Trace.WriteLine( "FilterRegistry -- No rule action or action template with name [" + oldName + "] exists for rename" );
                }
            }
        }
        #endregion Rename

        #region Auxiliaries

        #region Predicates
        private static bool  isInternal( IResource res )
        {
            return (res != null) && (res.GetIntProp( "InternalView" ) == 1);
        }

        public static bool IsViewOrFolder( IResource res )
        {
            return (res != null) && (res.Type == FilterManagerProps.ViewResName || res.Type == FilterManagerProps.ViewFolderResName);
        }

        internal static bool  isProxyRuleAction( IResource res )
        {
            #region Preconditions
            if ( res.Type != FilterManagerProps.RuleActionResName )
                throw new InvalidOperationException( "FilterRegistry -- illegal type of resource. RuleAction expected." );
            #endregion Preconditions

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

        internal static bool  HasQueryCondition( IEnumerable<IResource[]> conditionGroups )
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

        internal static bool  IsUnreadCondition( IResource res )
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
                        throw new InvalidOperationException( "FilterRegistry -- Illegal type of resource is given for linking: " + 
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
                throw new InvalidOperationException( "FilterRegistry -- input resource is not a view or a rule" );
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
                throw new InvalidOperationException( "FilterRegistry -- input resource is not a view or a rule" );
            #endregion Preconditions

            return res.GetLinksOfType( FilterManagerProps.ConditionResName, _props._linkedConditionsLink );
        }

        public IResourceList GetExceptions( IResource res )
        {
            #region Preconditions
            if( res.Type != FilterManagerProps.RuleResName && res.Type != FilterManagerProps.ViewResName && res.Type != FilterManagerProps.ViewCompositeResName )
                throw new InvalidOperationException( "FilterRegistry -- input resource is not a view or a rule" );
            #endregion Preconditions

            return res.GetLinksOfType( FilterManagerProps.ConditionResName, _props._linkedExceptionsLink );
        }

        public IResourceList GetActions( IResource ruleOrView )
        {
            #region Preconditions
            if( ruleOrView.Type != FilterManagerProps.RuleResName )
                throw new InvalidOperationException( "FilterRegistry -- input resource is not a rule" );
            #endregion Preconditions

            return ruleOrView.GetLinksOfType( FilterManagerProps.RuleActionResName, _props._linkedActionsLink );
        }

        public IResourceList GetLinkedConditions( IResource conditionTemplate )
        {
            #region Preconditions
            if( conditionTemplate.Type != FilterManagerProps.ConditionTemplateResName )
                throw new InvalidOperationException( "FilterRegistry -- input resource is not a condition template" );
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
            //        possible to call this method befor FilterRegistry's initialisation.
            string query = res.GetStringProp( "ApplicableToProp" ); /*FilterRegistry.ApplicableToProp*/
            if( res.HasProp( "SectionOrder" ))
            {
                uint order = (uint)res.GetIntProp( "SectionOrder" );
                query = "(" + query + ") [" + DocSectionHelper.FullNameByOrder( order ) + "]";
            }
            return query;
        }
        #endregion Auxiliaries

	    internal static Hashtable CustomConditions
	    {
	        get {  return _customConditions;  }
	    }

	    internal static Hashtable RuleActions
	    {
	        get {  return _ruleActions;  }
	    }
        #region Attributes

        public string  ViewNameForSearchResults    {  get{ return( FilterManagerProps.SearchResultsViewName ); }  }

        public  const string  ExternalFileTag = "ExternalFile";
        public  const string  ExternalDirTag = "ExternalDir";
        public  const string  RuleApplicableResourceTypeResName = "RuleApplicableResourceType";
        private const string  cAllConditionGroups = "AllConditionGroups";

        private static readonly IStandardConditions  StandardConditions = new FilterManagerStandards();
        internal static readonly FilterManagerUIHandler _UIHandler = new FilterManagerUIHandler();

        private static FilterManagerProps   _props;

        private static readonly Hashtable   _customConditions = new Hashtable();
        private static readonly Hashtable   _ruleActions = new Hashtable();
        private static readonly Hashtable   _registeredUIHandlers = new Hashtable();

        private readonly IResourceStore  Store;

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
                throw new ArgumentException( "FilterRegistry -- RuleAction or SearchCondition resource type is expected for transformation" );
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

