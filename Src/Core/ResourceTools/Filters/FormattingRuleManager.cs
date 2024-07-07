// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Reflection;
using JetBrains.Omea.Base;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.FiltersManagement
{
    public class FormattingRuleManager: IFormattingRuleManager
    {
        #region Attributes

        private class FormattingRule
        {
            internal ItemFormat ItemFormat;
            internal IResource[] Conditions, NegConditions;
            internal string ResourceType;
            internal IResource Resource;
        }

        private IResourceList _formattingRuleList;
        private readonly IFilterRegistry _fmgr;
        private readonly ArrayList _formattingRules = new ArrayList();
        public event EventHandler FormattingRulesChanged;

        #endregion Attributes

        #region Ctor and Initialization

        //  IFilterRegistry in params is mostly for PicoContainer dependency.
        public FormattingRuleManager( IFilterRegistry fmgr )
        {
            _fmgr = fmgr;
            RegisterTypes();
            CheckValidInvisibleRules();
            SetupFormattingRules();
        }

        public void  RegisterTypes()
        {
            Core.ResourceStore.PropTypes.Register( "IsFormattingFilter", PropDataType.Bool, PropTypeFlags.Internal );
            Core.ResourceStore.PropTypes.Register( "IsBold", PropDataType.Bool, PropTypeFlags.Internal );
            Core.ResourceStore.PropTypes.Register( "IsItalic", PropDataType.Bool, PropTypeFlags.Internal );
            Core.ResourceStore.PropTypes.Register( "IsUnderline", PropDataType.Bool, PropTypeFlags.Internal );
            Core.ResourceStore.PropTypes.Register( "IsStrikeout", PropDataType.Bool, PropTypeFlags.Internal );
            Core.ResourceStore.PropTypes.Register( "ForeColor", PropDataType.String, PropTypeFlags.Internal );
            Core.ResourceStore.PropTypes.Register( "BackColor", PropDataType.String, PropTypeFlags.Internal );
        }
        #endregion Ctor and Initialization

        #region IFormattingRuleManager methods
        public IResource RegisterRule( string name, string[] types, IResource[] conditions, IResource[] exceptions,
                                       bool isBold, bool isItalic, bool isUnderlined, bool isStrikeout,
                                       string foreColor, string backColor )
        {
            IResource rule = FindRule( name );
            ResourceProxy proxy = GetRuleProxy( rule );
            FilterRegistry.InitializeView( proxy, name, types, conditions, exceptions );
            AddSpecificParams( proxy.Resource, name, isBold, isItalic, isUnderlined, isStrikeout, foreColor, backColor );
            CheckRuleInvisiblity( proxy.Resource, conditions );
            return proxy.Resource;
        }

        public IResource ReregisterRule( IResource baseRes, string name, string[] types,
                                         IResource[] conditions, IResource[] exceptions,
                                         bool isBold, bool isItalic, bool isUnderlined, bool isStrikeout,
                                         string foreColor, string backColor )
        {
            ResourceProxy proxy = new ResourceProxy( baseRes );
            proxy.BeginUpdate();
            FilterRegistry.InitializeView( proxy, name, types, conditions, exceptions );
            AddSpecificParams( baseRes, name, isBold, isItalic, isUnderlined, isStrikeout, foreColor, backColor );
            return baseRes;
        }

        public void  UnregisterRule( string name )
        {
            IResource rule = FindRule( name );
            Core.FilterRegistry.DeleteView( rule );
        }

        public IResource FindRule( string name )
        {
            IResourceList list = Core.ResourceStore.FindResourcesWithProp( FilterManagerProps.ViewCompositeResName, "IsFormattingFilter" );
            IResourceList named = Core.ResourceStore.FindResources( null, Core.Props.Name, name );
            list = list.Intersect( named, true );

            return (list.Count > 0) ? list[ 0 ] : null;
        }

        public bool  IsRuleRegistered( string name )
        {
            IResource rule = FindRule( name );
            return rule != null;
        }

        public void  RenameRule( IResource rule, string newName )
        {
            #region Preconditions
            if( rule == null )
                throw new ArgumentNullException( "rule", "FormattingRuleManager -- Input rule resource is null." );

            if( rule.Type != FilterManagerProps.ViewCompositeResName || !rule.HasProp( "IsFormattingFilter" ) )
                throw new InvalidOperationException( "FormattingRuleManager -- input resource is not a Formatting rule." );

            if( String.IsNullOrEmpty( newName ) )
                throw new ArgumentNullException( "newName", "FormattingRuleManager -- New name for a rule is null or empty." );

            if( FindRule( newName ) != null )
                throw new ArgumentException( "FormattingRuleManager -- An action rule with new name already exists." );
            #endregion Preconditions

            new ResourceProxy( rule ).SetProp( Core.Props.Name, newName );
        }

        public IResource  CloneRule( IResource sourceRule, string newName )
        {
            #region Preconditions
            if( !sourceRule.HasProp( "IsFormattingFilter" ) )
                throw new InvalidOperationException( "FormattingRuleManager -- input resource is not a Formatting rule." );

            IResource res = FindRule( newName );
            if( res != null )
                throw new AmbiguousMatchException( "FormattingRuleManager -- A Formatting rule with such name already exists." );
            #endregion Preconditions

            ResourceProxy newRule = ResourceProxy.BeginNewResource( FilterManagerProps.ViewCompositeResName );
            FilterRegistry.CloneView( sourceRule, newRule, newName );
            CloneFormatting( sourceRule, newRule.Resource );

            return newRule.Resource;
        }

        public void  CloneFormatting( IResource source, IResource dest )
        {
            #region Preconditions
            if( dest.Type != FilterManagerProps.ViewCompositeResName )
                throw new InvalidOperationException( "FormattingRuleManager -- input resource is not a Formatting View" );
            #endregion Preconditions

            ResourceProxy proxy = new ResourceProxy( dest );
            proxy.BeginUpdate();
            proxy.SetProp( "IsFormattingFilter", true );
            if( source.HasProp( "IsBold" ) )
                proxy.SetProp( "IsBold", true );
            if( source.HasProp( "IsItalic" ) )
                proxy.SetProp( "IsItalic", true );
            if( source.HasProp( "IsUnderline" ) )
                proxy.SetProp( "IsUnderline", true );
            if( source.HasProp( "IsStrikeout" ) )
                proxy.SetProp( "IsStrikeout", true );

            proxy.SetProp( "ForeColor", source.GetStringProp( "ForeColor" ) );
            proxy.SetProp( "BackColor", source.GetStringProp( "BackColor" ) );
            proxy.EndUpdate();
        }

        public ItemFormat GetFormattingInfo( IResource res )
        {
            lock( _formattingRules )
            {
                foreach( FormattingRule rule in _formattingRules )
                {
                    if( _fmgr.IsRuleActive( rule.Resource ) &&
                        Core.FilterEngine.MatchView( rule.Resource, res, false ) )
                    {
                        return rule.ItemFormat;
                    }
                }
            }
            return null;
        }
        #endregion IFormattingRuleManager methods

        #region Impl
        private static void AddSpecificParams( IResource rule, string name, bool isBold, bool isItalic,
                                               bool isUnderlined, bool isStrikeout, string foreColor, string backColor )
        {
            ResourceProxy proxy = new ResourceProxy( rule );
            proxy.BeginUpdate();
            proxy.SetProp( "IsFormattingFilter", true );
            proxy.SetProp( "DeepName", name );
            if( isBold )
                proxy.SetProp( "IsBold", true );
            else
                proxy.DeleteProp( "IsBold" );
            if( isItalic )
                proxy.SetProp( "IsItalic", true );
            else
                proxy.DeleteProp( "IsItalic" );
            if( isUnderlined )
                proxy.SetProp( "IsUnderline", true );
            else
                proxy.DeleteProp( "IsUnderline" );
            if( isStrikeout )
                proxy.SetProp( "IsStrikeout", true );
            else
                proxy.DeleteProp( "IsStrikeout" );
            proxy.SetProp( "ForeColor", foreColor );
            proxy.SetProp( "BackColor", backColor );

            //  Keep also the date of creation/last modification.
            //  It will allow us to order rules in the future.
            proxy.SetProp( Core.Props.Date, DateTime.Now );

            proxy.EndUpdate();
        }

        /// <summary>
        /// If the formatting rule comes from "Watch Thread"-like contexts,
        /// it uses hidden condition template "Message Is in the Thread Of smth"
        /// and thus must also be hidden from the user in the Rules Manager.
        /// </summary>
        private static void  CheckRuleInvisiblity( IResource rule, IResource[] conditions )
        {
            foreach( IResource cond in conditions )
            {
                IResource template = cond.GetLinkProp( Core.FilterRegistry.Props.TemplateLink );
                if( template != null &&
                    template.Id == Core.FilterRegistry.Std.MessageIsInThreadOfX.Id )
                {
                    new ResourceProxy( rule ).SetProp( Core.FilterRegistry.Props.Invisible, true );
                }
            }
        }

        /// <summary>
        /// For every invisible formatting rule - check the validness of
        /// their conditions. If any condition referres to an invalid
        /// resource or no resources at all - remove this rule.
        /// </summary>
        private static void  CheckValidInvisibleRules()
        {
            IResourceList list = Core.ResourceStore.FindResourcesWithProp( FilterManagerProps.ViewCompositeResName, "IsFormattingFilter" );
            list = list.Intersect( Core.ResourceStore.FindResourcesWithProp( FilterManagerProps.ViewCompositeResName, Core.FilterRegistry.Props.Invisible ) );

            for( int i = 0; i < list.Count; i++ )
            {
                bool  valid = true;
                IResourceList conds = Core.FilterRegistry.GetConditionsPlain( list[ i ] );
                foreach( IResource cond in conds )
                {
                    IResource template = cond.GetLinkProp( Core.FilterRegistry.Props.TemplateLink );
                    if( template != null &&
                        template.Id == Core.FilterRegistry.Std.MessageIsInThreadOfX.Id )
                    {
                        IResourceList paramList = cond.GetLinksOfType( null, "LinkedSetValue" );
                        if( paramList.Count == 0 )
                        {
                            valid = false;
                            break;
                        }
                    }
                }

                //-----------------------------------------------------------------------
                //  One of the conditions of the rule is already invalid - most probably
                //  the resource which was the condition's parameter has been already
                //  deleted. Thus the whole formatting rule must be deleted.
                //-----------------------------------------------------------------------
                if( !valid )
                {
                    list[ i ].Delete();
                }
            }
        }

        private void SetupFormattingRules()
        {
            _formattingRuleList = Core.ResourceStore.FindResourcesWithPropLive( FilterManagerProps.ViewCompositeResName, "IsFormattingFilter" );
            _formattingRuleList.ResourceAdded += OnFormattingRuleAdded;
            _formattingRuleList.ResourceChanged += OnFormattingRuleChanged;
            _formattingRuleList.ResourceDeleting += OnFormattingRuleDeleting;

            _formattingRuleList.Sort( new int[]  { Core.FilterRegistry.Props.Invisible, Core.Props.Date, Core.Props.Order },
                                      new bool[] { false, false, true } );
            ReloadFormattingRules( null );
        }

	    private void OnFormattingRuleDeleting( object sender, ResourceIndexEventArgs e )
	    {
	        ReloadFormattingRules( e.Resource );
	    }

	    private void OnFormattingRuleChanged( object sender, ResourcePropIndexEventArgs e )
	    {
	        ReloadFormattingRules( null );
	    }

	    private void OnFormattingRuleAdded( object sender, ResourceIndexEventArgs e )
        {
            ReloadFormattingRules( null );
        }

	    private void ReloadFormattingRules( IResource exceptRule )
	    {
	        lock( _formattingRules )
	        {
                _formattingRules.Clear();
                foreach( IResource res in _formattingRuleList )
                {
                    if ( res == exceptRule )
                        continue;

                    FormattingRule fmtRule = new FormattingRule();
                    fmtRule.ItemFormat = GetRuleFormat( res );
                    fmtRule.ResourceType = res.GetStringProp( Core.Props.ContentType );
                    fmtRule.Resource = res;

                    IResourceList conditions = res.GetLinksOfType( FilterManagerProps.ConditionResName, "LinkedCondition" );
                    fmtRule.Conditions = new IResource[ conditions.Count ];
                    for( int i = 0; i < conditions.Count; i++ )
                        fmtRule.Conditions[ i ] = conditions[ i ];

                    conditions = res.GetLinksOfType( FilterManagerProps.ConditionResName, "LinkedNegativeCondition" );
                    fmtRule.NegConditions = new IResource[ conditions.Count ];
                    for( int i = 0; i < conditions.Count; i++ )
                        fmtRule.NegConditions[ i ] = conditions[ i ];

                    _formattingRules.Add( fmtRule );
                }
	        }
            if ( FormattingRulesChanged != null )
            {
                FormattingRulesChanged( this, EventArgs.Empty );
            }
	    }

        private static ItemFormat GetRuleFormat( IResource rule )
        {
            ItemFormat fi = new ItemFormat();
            if( rule.HasProp( "IsBold"))
                fi.FontStyle |= FontStyle.Bold;
            if( rule.HasProp( "IsItalic"))
                fi.FontStyle |= FontStyle.Italic;
            if( rule.HasProp( "IsUnderline"))
                fi.FontStyle |= FontStyle.Underline;
            if( rule.HasProp( "IsStrikeout"))
                fi.FontStyle |= FontStyle.Strikeout;
            if( rule.HasProp( "ForeColor" ))
                fi.ForeColor = Utils.ColorFromString( rule.GetStringProp( "ForeColor" ) );
            else
                fi.ForeColor = Color.Black;
            if( rule.HasProp( "BackColor" ))
                fi.BackColor = Utils.ColorFromString( rule.GetStringProp( "BackColor" ) );
            else
                fi.BackColor = Color.White;

            return fi;
        }

        private static ResourceProxy  GetRuleProxy( IResource rule )
        {
            ResourceProxy proxy;
            if( rule != null )
            {
                proxy = new ResourceProxy( rule );
                proxy.BeginUpdate();
            }
            else
                proxy = ResourceProxy.BeginNewResource( FilterManagerProps.ViewCompositeResName );
            return proxy;
        }
        #endregion Impl
    }
}
