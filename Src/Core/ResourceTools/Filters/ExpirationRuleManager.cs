// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.FiltersManagement
{
    public class ExpirationRuleManager : IExpirationRuleManager
    {
        private readonly int    ExpRuleLinkId, ExpRuleOnDeletedLinkId;
        private readonly IResourceList   ExpRules;
        private readonly IResource[]     DummyConditionList;

        private readonly Hashtable       TracedLinks = new Hashtable();
        private readonly Hashtable       TracedLists = new Hashtable();
        private readonly HashMap         _resourceTypesForDefaultExpirationRules = new HashMap( 2 );

        private static readonly Hashtable TypesMap = new Hashtable();

        #region Ctor
        public ExpirationRuleManager()
        {
            IResource dummyCond = Core.ResourceStore.FindUniqueResource( FilterManagerProps.ConditionResName, "Name", "DummyExpCondition" );
            if( dummyCond == null )
            {
                dummyCond = Core.FilterRegistry.CreateStandardCondition( "DummyExpCondition", "DummyExpCondition", null, "Id", ConditionOp.Gt, "0" );
                dummyCond.SetProp( "Invisible", true );
            }
            DummyConditionList = new IResource[ 1 ] { dummyCond };

            ExpRuleLinkId = Core.ResourceStore.PropTypes[ "ExpirationRuleLink" ].Id;
            ExpRuleOnDeletedLinkId = Core.ResourceStore.PropTypes[ "ExpirationRuleOnDeletedLink" ].Id;
            ExpRules = Core.ResourceStore.FindResourcesWithPropLive( FilterManagerProps.RuleResName, "IsExpirationFilter" );
            ExpRules.AddPropertyWatch( ExpRuleLinkId );
            ExpRules.ResourceChanged += RuleLinkageChanged;

            //  Duplicate registering from FilterManage due to the
            //  indeterminicity of the order of calls from the Pico contaner
            //  to its managed classes.
            Core.ResourceStore.PropTypes.Register( "DeleteRelatedContact", PropDataType.Bool );

            Core.ResourceAP.QueueJobAt( DateTime.Now.AddMinutes( 10.0 ), new MethodInvoker( CleanDeletedItems ) );
        }
        #endregion Ctor

        #region Register
        public IResource  RegisterRule( IResourceList folders, IResource[] conds, IResource[] excepts, IResource[] actions )
        {
            return RegisterRuleImpl( folders, -1, conds, excepts, actions );
        }

        public IResource  RegisterRule( IResource baseType, IResource[] conds, IResource[] excepts, IResource[] actions )
        {
            return RegisterRuleImpl( false, baseType, -1, conds, excepts, actions );
        }

        public IResource  RegisterRuleForDeletedItems( IResource baseType, IResource[] conds, IResource[] excepts, IResource[] actions )
        {
            return RegisterRuleImpl( true, baseType, -1, conds, excepts, actions );
        }

        public void  ReregisterRule( IResource rule, IResourceList folders, IResource[] conds, IResource[] excepts, IResource[] actions )
        {
            ReregisterRuleImpl( rule, folders, -1, conds, excepts, actions );
        }

        public void  ReregisterRule( IResource rule, IResource baseType, IResource[] conds, IResource[] excepts, IResource[] actions )
        {
            ReregisterRuleImpl( false, rule, baseType, -1, conds, excepts, actions );
        }

        public void  ReregisterRuleForDeletedItems( IResource rule, IResource baseType, IResource[] conds, IResource[] excepts, IResource[] actions )
        {
            ReregisterRuleImpl( true, rule, baseType, -1, conds, excepts, actions );
        }

        public IResource  RegisterRule( IResourceList folders, int count, IResource[] excepts, IResource[] actions )
        {
            return RegisterRuleImpl( folders, count, null, excepts, actions );
        }

        public IResource  RegisterRule( IResource baseType, int count, IResource[] excepts, IResource[] actions )
        {
            return RegisterRuleImpl( false, baseType, count, null, excepts, actions );
        }

        public IResource  RegisterRuleForDeletedItems( IResource baseType, int count, IResource[] excepts, IResource[] actions )
        {
            return RegisterRuleImpl( true, baseType, count, null, excepts, actions );
        }

        public void  ReregisterRule( IResource rule, IResourceList folders, int count, IResource[] excepts, IResource[] actions )
        {
            ReregisterRuleImpl( rule, folders, count, null, excepts, actions );
        }

        public void  ReregisterRule( IResource rule, IResource baseType, int count, IResource[] excepts, IResource[] actions )
        {
            ReregisterRuleImpl( false, rule, baseType, count, null, excepts, actions );
        }

        public void  ReregisterRuleForDeletedItems( IResource rule, IResource baseType, int count, IResource[] excepts, IResource[] actions )
        {
            ReregisterRuleImpl( true, rule, baseType, count, null, excepts, actions );
        }
        #endregion

        #region IExpirationRuleManager interface misc
        public void  UnregisterRule( string name )
        {
            Core.FilterRegistry.DeleteRule( name );
        }

        //  User registers the resource type of the "folder" and the link
        //  by which actual resources are linked to the folders.
        public void  RegisterResourceType( int linkId, string containerType, string itemType )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( containerType ))
                throw new ArgumentNullException( "containerType", "ExpirationRuleManager -- Folder resource type is NULL or empty." );
            if( String.IsNullOrEmpty( itemType ))
                throw new ArgumentNullException( "itemType", "ExpirationRuleManager -- Item resource type is NULL or empty." );
            #endregion Preconditions

            //  Construct a live list of the specified type and listen to
            //  the event "resource changed".

            IResourceList list = Core.ResourceStore.GetAllResourcesLive( containerType );
            list.ResourceChanged += BaseResourceChanged;

            TracedLinks[ linkId ] = containerType;
            TracedLists[ list ] = containerType;
            TypesMap[ containerType ]= itemType;
        }

        public void  RenameRule( IResource rule, string newName )
        {
            #region Preconditions
            if( rule == null )
                throw new ArgumentNullException( "rule", "ExpirationRuleManager -- Input rule resource is null." );

            if( rule.Type != FilterManagerProps.RuleResName || !rule.HasProp( "IsExpirationFilter" ) )
                throw new InvalidOperationException( "ExpirationRuleManager -- input resource is not a TrayIcon rule." );

            if( String.IsNullOrEmpty( newName ) )
                throw new ArgumentNullException( "newName", "ExpirationRuleManager -- New name for a rule is null or empty." );
            #endregion Preconditions

            if( FilterRegistry.FindRuleByName( newName, "IsExpirationFilter" ) != null )
                throw new ArgumentException( "ExpirationRuleManager -- An action rule with new name already exists." );

            new ResourceProxy( rule ).SetProp( Core.Props.Name, newName );
        }
        #endregion IExpirationRuleManager interface misc

        #region Public Methods
        public static string  ItemFromContainerType( string containerType )
        {
            #region Preconditions
            if ( String.IsNullOrEmpty( containerType ) )
                throw new ArgumentNullException( "containerType", "ExpirationRuleManager -- Name of a container resource type is null or empty." );
            #endregion Preconditions

            return (string) TypesMap[ containerType ];
        }

        public static void  RunRule( IResource rule )
        {
            #region Preconditions
            if( rule == null )
                throw new ArgumentNullException( "rule", "ExpirationRuleManager -- Input rule resource is null." );

            if( rule.Type != FilterManagerProps.RuleResName || !rule.HasProp( "IsExpirationFilter" ) )
                throw new InvalidOperationException( "ExpirationRuleManager -- input resource is not a TrayIcon rule." );
            #endregion Preconditions

            int linkId = Core.ResourceStore.PropTypes[ "ExpirationRuleLink" ].Id;
            int onDeletedLinkId = Core.ResourceStore.PropTypes[ "ExpirationRuleOnDeletedLink" ].Id;

            IResourceList folders = rule.GetLinksOfType( null, linkId );
            IResourceList delType = rule.GetLinksOfType( null, onDeletedLinkId );

            if( folders.Count > 0 )
            {
                if(( folders.Count == 1 ) && ( folders[ 0 ].Type == "ResourceType" ))
                {
                    //  This is a resource type of a base folders. Get all resources of
                    //  this type and apply rule to them.
                    string typeName = folders[ 0 ].GetStringProp( Core.Props.Name );
                    folders = Core.ResourceStore.GetAllResources( typeName );
                }

                foreach( IResource folder in folders )
                {
                    UpdateFolder( rule, folder );
                }
            }
            else
            if( delType.Count == 1 )
            {
                //  This is a resource type. Get all resources of this type and
                //  apply rule to them.
                string typeName = delType[ 0 ].GetStringProp( Core.Props.Name );

                IResourceList list = Core.ResourceStore.FindResourcesWithProp( typeName, Core.Props.IsDeleted );
                Core.FilterEngine.ExecRule( rule, list );
            }
            else
                throw new ApplicationException( "ExpirationFilterManager -- Unknown link between a rule and resources." );
        }
        #endregion Public Methods

        #region Impl Register
        private IResource  RegisterRuleImpl( IResourceList folders, int count,
                                             IResource[] conds, IResource[] excepts, IResource[] actions )
        {
            #region Precondition
            if( folders == null || folders.Count == 0 )
                throw new ArgumentNullException( "folders", "ExpirationRuleManager -- Base folders list is null or empty." );
            #endregion Precondition

            string  name = "", fullName = "";
            ConstructName( folders, ref name, ref fullName );
            IResource[]  conditions = (count == -1 && conds != null && conds.Length > 0) ? conds : DummyConditionList;

            IResource rule = Core.FilterRegistry.RegisterRule( StandardEvents.ResourceReceived, name, null,
                                                              conditions, excepts, actions );
            SetCommonProps( rule, fullName, count );
            LinkRuleToDependents( rule, folders );

            return rule;
        }

        private IResource  RegisterRuleImpl( bool isForDel, IResource baseType, int count,
                                             IResource[] conds, IResource[] excepts, IResource[] actions )
        {
            #region Precondition
            if( baseType == null )
                throw new ArgumentNullException( "baseType", "ExpirationRuleManager -- Base rule type is null." );

            if( baseType.Type != "ResourceType" )
                throw new ArgumentNullException( "ExpirationRuleManager -- Base rule type has inappropriate type (" + baseType.Type + ")." );
            #endregion Precondition

            string  name = "", fullName = "";
            ConstructName( baseType, isForDel, ref name, ref fullName );
            IResource[]  conditions = (count == -1 && conds != null && conds.Length > 0) ? conds : DummyConditionList;

            IResource rule = Core.FilterRegistry.RegisterRule( StandardEvents.ResourceReceived, name, null,
                                                              conditions, excepts, actions );
            SetCommonProps( rule, fullName, count );
            LinkRuleToDependents( rule, baseType, isForDel ? ExpRuleOnDeletedLinkId : ExpRuleLinkId );

            return rule;
        }

        private void ReregisterRuleImpl( bool isForDel, IResource rule, IResource baseType, int count,
                                         IResource[] conds, IResource[] excepts, IResource[] actions )
        {
            #region Precondition
            if( rule == null )
                throw new ArgumentNullException( "rule", "ExpirationRuleManager -- Base rule resource is null." );

            if( baseType == null )
                throw new ArgumentNullException( "baseType", "ExpirationRuleManager -- Base rule type is null." );

            if( baseType.Type != "ResourceType" )
                throw new ArgumentNullException( "baseType", "ExpirationRuleManager -- Base rule type has inappropriate type (" + baseType.Type + ")." );
            #endregion Precondition

            string  name = "", fullName = "";
            ConstructName( baseType, isForDel, ref name, ref fullName );
            IResource[]  conditions = (count == -1 && conds != null && conds.Length > 0) ? conds : DummyConditionList;

            Core.FilterRegistry.ReregisterRule( StandardEvents.ResourceReceived, rule, name, null,
                                               conditions, excepts, actions );
            SetCommonProps( rule, fullName, count );
            LinkRuleToDependents( rule, baseType, isForDel ? ExpRuleOnDeletedLinkId : ExpRuleLinkId );
        }

        private void  ReregisterRuleImpl( IResource rule, IResourceList folders, int count,
                                          IResource[] conds, IResource[] excepts, IResource[] actions )
        {
            #region Precondition
            if( rule == null )
                throw new ArgumentNullException( "rule", "ExpirationRuleManager -- Base rule resource is null." );

            if( folders == null || folders.Count == 0 )
                throw new ArgumentNullException( "folders", "ExpirationRuleManager -- Base folders list is null or empty." );
            #endregion Precondition

            string  name = "", fullName = "";
            ConstructName( folders, ref name, ref fullName );
            IResource[]  conditions = (count == -1 && conds != null && conds.Length > 0) ? conds : DummyConditionList;

            Core.FilterRegistry.ReregisterRule( StandardEvents.ResourceReceived, rule, name, null,
                                               conditions, excepts, actions );
            SetCommonProps( rule, fullName, count );
            LinkRuleToDependents( rule, folders );
        }
        #endregion Impl Register

        #region Impl Misc
        private static void  SetCommonProps( IResource rule, string fullName, int restictionCount )
        {
            ResourceProxy proxy = new ResourceProxy( rule );
            proxy.BeginUpdate();
            proxy.SetProp( "DeepName", fullName );
            proxy.SetProp( "IsExpirationFilter", true );

            //  Remove default property so that rule of this type are not recognized
            //  as Action Rules, and thus are not activated in the inproper moment.
            proxy.DeleteProp( "IsActionFilter" );
            if( restictionCount > 0 )
                proxy.SetProp( "CountRestriction", restictionCount );
            else
                proxy.DeleteProp( "CountRestriction" );

            proxy.EndUpdate();
        }

        private static void ConstructName( IResourceList folders, ref string name, ref string fullName )
        {
            fullName = name = "Exp.Rule for ";
            if( folders.Count < 4 )
            {
                foreach( IResource res in folders )
                    name = name + res.DisplayName + ", ";
                fullName = name = name.Substring( 0, name.Length - 2 );
            }
            else
            {
                name = name + folders[ 0 ].DisplayName + ", " + folders[ 1 ].DisplayName +
                    " and " + (folders.Count - 2) + " more folders";
                foreach( IResource res in folders )
                    fullName = fullName + res.DisplayName + ", ";
                fullName = fullName.Substring( 0, fullName.Length - 2 );
            }
        }
        private static void  ConstructName( IResource baseResType, bool forDeletedItems,
                                            ref string name, ref string fullName )
        {
            if( !forDeletedItems )
                name = fullName = "Default Expiration Rule for all " + baseResType.DisplayName + " resources";
            else
                name = fullName = "Default Expiration Rule for deleted resources from " + baseResType.DisplayName;
        }

        private static void  LinkRuleToDependents( IResource rule, IResourceList folders )
        {
            int  linkId = Core.ResourceStore.PropTypes[ "ExpirationRuleLink" ].Id;
            IResourceList prevFolders = rule.GetLinksOfType( null, linkId );

            foreach( IResource folder in folders )
            {
                new ResourceProxy( folder ).SetProp( "ExpirationRuleLink", rule );
            }
            for( int i = 0; i < prevFolders.Count; i++ )
            {
                if( folders.IndexOf( prevFolders[ i ].Id ) == -1 )
                {
                    new ResourceProxy( prevFolders[ i ] ).DeleteLink( linkId, rule );
                }
            }
        }
        private static void  LinkRuleToDependents( IResource rule, IResource baseType, int linkId )
        {
            IResource prevType = rule.GetLinkProp( -linkId );
            new ResourceProxy( baseType ).SetProp( linkId, rule );
            if( prevType != null && prevType != baseType )
            {
                new ResourceProxy( prevType ).DeleteLink( linkId, rule );
            }
        }
        #endregion Impl

        #region Event Analysis
        internal delegate void  ExpRuleDelegate( IResource rule, IResource folder );
        private void  BaseResourceChanged( object sender, ResourcePropIndexEventArgs e )
        {
            foreach( int linkId in TracedLinks.Keys )
            {
                LinkChange[] changes;

                //  For undirected links take always a positive value.
                if( Core.ResourceStore.PropTypes[ linkId ].HasFlag( PropTypeFlags.DirectedLink ) )
                    changes = e.ChangeSet.GetLinkChanges( -linkId );
                else
                    changes = e.ChangeSet.GetLinkChanges( linkId );

                foreach( LinkChange change in changes )
                    ProcessLinkChange( linkId, e.Resource, change );
            }
        }

        private void  ProcessLinkChange( int linkId, IResource target, LinkChange change )
        {
            if( change.ChangeType == LinkChangeType.Add )
            {
                if( target.Type == (string) TracedLinks[ linkId ] )
                {
                    IResource rule = target.GetLinkProp( "ExpirationRuleLink" );
                    if ( rule == null )
                    {
                        IResource resType = null;
                        HashMap.Entry entry = _resourceTypesForDefaultExpirationRules.GetEntry( target.Type );
                        if ( entry != null )
                        {
                            resType = (IResource)entry.Value;
                        }
                        else
                        {
                            resType = Core.ResourceStore.FindUniqueResource( "ResourceType", Core.Props.Name, target.Type );
                            _resourceTypesForDefaultExpirationRules.Add( target.Type, resType );
                        }
                        if ( resType != null )
                        {
                            rule = resType.GetLinkProp( "ExpirationRuleLink" );
                        }
                    }
                    if( rule != null )
                    {
                        //  If we submit repeatable job with the same signature, they are
                        //  merged in the queue. Thus we fire the actual rule only once.
                        Core.ResourceAP.QueueJobAt( DateTime.Now.AddSeconds( 20.0 ), new ExpRuleDelegate( UpdateFolder ), rule, target );
                    }
                }
            }
        }

        private static void  UpdateFolder( IResource rule, IResource folder )
        {
            IResourceList list = Core.UIManager.GetResourcesInLocation( folder );

            //  Phase 1. Collect a set of resources from the folder which
            //           match conditions from the rule.
            IResourceList matched = Core.FilterEngine.ExecView( rule, list );

            //  Phase 2. If we have resource-count restriction on the rule,
            //           reduce the amount of matched resources to this count.
            if( rule.HasProp( "CountRestriction" ))
            {
                int  countMargin = rule.GetIntProp( "CountRestriction" );
                if( countMargin > 0 && matched.Count > countMargin )
                {
                    matched.Sort( new SortSettings( Core.Props.Date, false ) );

                    List<int> ids = new List<int>( matched.Count - countMargin );
                    for( int i = countMargin; i < matched.Count; i++ )
                    {
                        ids.Add( matched[ i ].Id );
                    }
                    matched = Core.ResourceStore.ListFromIds( ids, false );
                }
            }

            //  Phase 3. Collect authors of the matched resources if necessary.
            int propDelContact = Core.ResourceStore.PropTypes[ "DeleteRelatedContact" ].Id;
            IResourceList contacts = Core.ResourceStore.EmptyResourceList;
            if( rule.HasProp( propDelContact ) )
            {
                foreach( IResource res in matched )
                {
                    IResourceList froms = res.GetLinksOfType( "Contact", Core.ContactManager.Props.LinkFrom );
                    contacts = contacts.Union( froms, true );
                }
            }

            //  Phase 4. Apply actions to the matched resources. If necessary,
            //           remove their authors
            for( int i = matched.Count - 1; i >= 0; i-- )
            {
                Core.FilterEngine.ApplyActions( rule, matched[ i ] );
            }

            if( rule.HasProp( propDelContact ) )
            {
                Core.ContactManager.DeleteUnusedContacts( contacts );
            }
        }

        //---------------------------------------------------------------------
        //  Trace the event when a link between a resource (e.g. folder) and
        //  an expiration rule is removed. When there is no link between a rule
        //  and other resources, the rule must be deleted automatically.
        //---------------------------------------------------------------------
        private void RuleLinkageChanged(object sender, ResourcePropIndexEventArgs e)
        {
            bool checkRes = false;
            LinkChange[] changes = e.ChangeSet.GetLinkChanges( -ExpRuleLinkId );
            foreach( LinkChange change in changes )
            {
                if( change.ChangeType == LinkChangeType.Delete )
                    checkRes = true;
            }

            int linksCount = e.Resource.GetLinksOfType( null, ExpRuleLinkId ).Count;
            if( checkRes && linksCount == 0 )
                e.Resource.Delete();
        }

        //---------------------------------------------------------------------
        //  Method is called at specified moments of time: after the application
        //  has started and afterwards once per hour (since it is the minimal
        //  unit of time user can specify for exp rule).
        //---------------------------------------------------------------------
        private void  CleanDeletedItems()
        {
            IResourceList resTypes = Core.ResourceStore.GetAllResources( "ResourceType" );
            foreach( IResource type in resTypes )
            {
                string typeName = type.GetStringProp( Core.Props.Name );
                IResource rule = type.GetLinkProp( ExpRuleOnDeletedLinkId );
                if( rule != null )
                {
                    IResourceList list = Core.ResourceStore.FindResourcesWithProp( typeName, Core.Props.IsDeleted );
                    Core.FilterEngine.ExecRule( rule, list );
                    Core.ResourceAP.QueueJobAt( DateTime.Now.AddHours( 1.0 ), new MethodInvoker( CleanDeletedItems ) );
                }
            }
        }
        #endregion Event Analysis
    }
}
