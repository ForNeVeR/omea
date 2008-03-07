/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.FiltersManagement;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.FiltersManagement
{
    public class TrayIconManager: ITrayIconManager
    {
        private enum TrayIconMode { Strict, Outlook };

        private IResourceList AllRules;
        private Hashtable    WatchedLists = new Hashtable();
        private Hashtable    WatchedIcons = new Hashtable();
        private ArrayList    WatchersOrder = new ArrayList();

        private NotifyIcon   NotIcon;
        private Icon         DefaultIcon;
        private string       DefaultTooltip;
        private TrayIconMode Mode = TrayIconMode.Strict;

        private delegate void AssignmentDelegate( IResource res, string name, Icon icon );

        #region Ctor and Initialization
        public TrayIconManager( NotifyIcon notifIcon )
        {
            NotIcon = notifIcon;
            DefaultIcon = notifIcon.Icon;
            DefaultTooltip = notifIcon.Text;
        }

        public void  RegisterTypes()
        {
            Core.ResourceStore.PropTypes.Register( "IsTrayIconFilter", PropDataType.Bool, PropTypeFlags.Internal );
            AllRules = Core.ResourceStore.FindResourcesLive( FilterManagerProps.ViewCompositeResName, "IsTrayIconFilter", true );
            //  This trace also immediately instantiates live list in order
            //  to fix OM-12295.
            Trace.WriteLine( "TrayIconManager -- registered a live list of rules, current count is " + AllRules.Count );

            Mode = Core.SettingStore.ReadBool( "TrayIconRules", "OutlookMode", false ) ? TrayIconMode.Outlook : TrayIconMode.Strict;
        }

        public void  Initialize()
        {
            foreach( IResource res in AllRules )
                RegisterIconWatcher( res );

            //-----------------------------------------------------------------
            //  Maintain the live list of tray icon rules in order to keep an
            //  eye on "RuleTurnedOff" property (controlled from the Rules
            //  Manager form) : unlike other types of rules, we have to react
            //  immediately by changing icon and tooltip text.
            //-----------------------------------------------------------------
            AllRules.ResourceChanged += AllRules_ResourceChanged;
            AllRules.AddPropertyWatch( Core.ResourceStore.PropTypes[ "RuleTurnedOff" ].Id );
        }
        #endregion Ctor and Initialization

        #region ITrayIconManager methods
        #region Old-style interface
        public IResource  RegisterTrayIconRule( string name, string[] types, 
                                                IResource[] conditions, IResource[] exceptions,
                                                Icon icon )
        {
            IResource[][] group = FilterManager.Convert2Group( conditions );
            return RegisterTrayIconRule( name, types, group, exceptions, icon );
        }
        public IResource  ReregisterTrayIconRule( IResource rule, string name, string[] types,
                                                  IResource[] conditions, IResource[] exceptions,
                                                  Icon icon )
        {
            IResource[][] group = FilterManager.Convert2Group( conditions );
            return ReregisterTrayIconRule( rule, name, types, group, exceptions, icon );
        }
        #endregion Old-style interface

        public IResource  RegisterTrayIconRule( string name, string[] types, 
                                                IResource[][] conditions, IResource[] exceptions,
                                                Icon icon )
        {
            IResource rule = FindRule( name );
            if( !WatchedLists.ContainsKey( name ) )
            {
                Trace.WriteLine( "TrayIconManager -- Registering publicly rule [" + name + "]." );

                ResourceProxy proxy = GetRuleProxy( rule );
                FilterManager.InitializeView( proxy, name, types, conditions, exceptions ); // proxy.EndUpdate inside
                Core.ResourceAP.RunJob( new AssignmentDelegate( AddSpecificParams ), proxy.Resource, name, icon );
                InitializeWatcher( proxy.Resource, name, icon );
                return proxy.Resource;
            }
            else
            {
                Trace.WriteLine( "TrayIconManager -- rule [" + name + "] is already registered." );
                return rule;
            }
        }

        public IResource  ReregisterTrayIconRule( IResource rule, string name, string[] types,
                                                  IResource[][] conditions, IResource[] exceptions,
                                                  Icon icon )
        {
            UnregisterIconWatcherImpl( rule.DisplayName, false );
            ResourceProxy proxy = new ResourceProxy( rule );
            proxy.BeginUpdate();
            FilterManager.InitializeView( proxy, name, types, conditions, exceptions );

            Core.ResourceAP.RunUniqueJob( new AssignmentDelegate( AddSpecificParams ), rule, name, icon );
            InitializeWatcher( rule, name, icon );
            return rule;
        }

        public void  UnregisterTrayIconRule( string name )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( name ) )
                throw new ArgumentNullException( "name", "TrayIconManager - Name of a rule is NULL or empty." );

            if( !WatchedLists.ContainsKey( name ) )
                throw new ArgumentException( "TrayIconManager - No such registered watcher [" + name + "]." );
            #endregion Preconditions

            UnregisterIconWatcherImpl( name, true );
        }

        #region Setting Mode
        public void  SetStrictMode()
        {
            Mode = TrayIconMode.Strict;
            Core.SettingStore.WriteBool( "TrayIconRules", "OutlookMode", false );
        }

        public void  SetOutlookMode()
        {
            Mode = TrayIconMode.Outlook;
            Core.SettingStore.WriteBool( "TrayIconRules", "OutlookMode", true );
        }

        public bool  IsOutlookMode
        {
            get{  return Mode == TrayIconMode.Outlook; }
        }
        #endregion Setting Mode

        #region Aux
        public IResource FindRule( string name )
        {
            IResourceList list = Core.ResourceStore.FindResources( null, Core.Props.Name, name );
            list = list.Intersect( AllRules, true );

            return (list.Count > 0) ? list[ 0 ] : null;
        }

        public bool  IsTrayIconRuleRegistered( string name )
        {
            return WatchedLists.ContainsKey( name );
        }

        public void  RenameRule( IResource rule, string newName )
        {
            #region Preconditions
            if( rule == null )
                throw new ArgumentNullException( "rule", "TrayIconManager -- Input rule resource is null." );

            if( rule.Type != FilterManagerProps.ViewCompositeResName || !rule.HasProp( "IsTrayIconFilter" ) )
                throw new InvalidOperationException( "TrayIconManager -- input resource is not a TrayIcon rule." );

            if( String.IsNullOrEmpty( newName ) )
                throw new ArgumentNullException( "newName", "TrayIconManager -- New name for a rule is null or empty." );

            if( FindRule( newName ) != null )
                throw new ArgumentException( "TrayIconManager -- An action rule with new name already exists." );
            #endregion Preconditions

            string oldName = rule.GetStringProp( Core.Props.Name );
            WatchedLists[ newName ] = WatchedLists[ oldName ];
            WatchedIcons[ newName ] = WatchedIcons[ oldName ];
            for( int i = 0; i < WatchersOrder.Count; i++ )
            {
                if( ((string)WatchersOrder[ i ]) == oldName )
                    WatchersOrder[ i ] = newName;
            }

            WatchedLists.Remove( oldName );
            WatchedIcons.Remove( oldName );
    
            new ResourceProxy( rule ).SetProp( Core.Props.Name, newName );
        }

        public IResource  CloneRule( IResource source, string newName )
        {
            #region Preconditions
            if( !source.HasProp( "IsTrayIconFilter" ) )
                throw new InvalidOperationException( "TrayIconManager -- input resource is not a TrayIcon rule." );

            IResource res = FindRule( newName );
            if( res != null )
                throw new AmbiguousMatchException( "TrayIconManager -- TrayIcon rule with such name already exists." );
            #endregion Preconditions

            ResourceProxy newRule = GetRuleProxy( null );
            FilterManager.CloneView( source, newRule, newName );
            CloneStaticInfo( source, newRule.Resource );
            RegisterIconWatcher( newRule.Resource );

            return newRule.Resource;
        }

        private delegate void DoubleResourceDelegate( IResource from, IResource to );

        private static void CloneStaticInfo( IResource source, IResource newRule )
        {
            if( !Core.ResourceAP.IsOwnerThread )
                Core.ResourceAP.RunJob( new DoubleResourceDelegate( CloneStaticInfo ), source, newRule );
            else
            {
                if( source.HasProp( "RuleTurnedOff" ) )
                    new ResourceProxy( newRule ).SetProp( "RuleTurnedOff", true );

                Stream   strm = source.GetBlobProp( "IconBlob" );
                if( strm == null )
                    throw new ApplicationException( "TrayIconManager -- Clonable rule " + source.DisplayName + " has no Icon resource" );

                AddSpecificParams( newRule, newRule.GetStringProp( Core.Props.Name ), new Icon( strm ) );
            }
        }
        #endregion Aux
        #endregion ITrayIconManager methods

        #region Impl Register/Unregister
        private void  RegisterIconWatcher( IResource rule )
        {
            //-----------------------------------------------------------------
            //  Some rules can be "rotten" due to different reasons (e.g. corrupted
            //  blob with icon). If this fact was somehow registered, the property
            //  "LastError" is set and contains the relevant problem description.
            //  So: do not register this rule if it contains any problems.
            //-----------------------------------------------------------------
            string error = rule.GetStringProp( Core.Props.LastError );
            if( String.IsNullOrEmpty( error ) )
            {
                string name = rule.GetStringProp( Core.Props.Name );
                try
                {
                    if( !WatchedLists.ContainsKey( name ) )
                    {
                        Icon icon = null;
                        Stream strm = rule.GetBlobProp( "IconBlob" );
                        if( strm != null )
                            icon = new Icon( strm );

                        InitializeWatcher( rule, name, icon );
                    }
                    else
                    {
                        Trace.WriteLine( "TrayIconManager -- rule [" + name + "] is already registered." );
                    }
                }
                catch( Exception )
                {
                    //---------------------------------------------------------
                    //  Here we catching either System.IO.FileNotFoundException or
                    //  "System.ArgumentException: The argument 'picture' must be a picture that can be used as a Icon"
                    //  The latter can be caused by database corruption when valid
                    //  icon content can not be parsed from binary representation.
                    //---------------------------------------------------------
                    Core.FilterManager.MarkRuleAsInvalid( rule, "Application did not manage to load a rule's icon." );
                    Core.UIManager.ShowSimpleMessageBox( "Tray Icon Manager", "Tray icon can not be loaded for a rule \"" +
                                                                            name + "\" - icon file is not found or corrupted.");
                }
            }
        }

        private void  InitializeWatcher( IResource rule, string name, Icon icon )
        {
            IResourceList watchList = Core.FilterManager.ExecView( rule );
            watchList = watchList.Minus( Core.ResourceStore.FindResourcesWithPropLive( null, Core.Props.IsDeleted ));

            WatchersOrder.Add( name );
            WatchedLists[ name ] = watchList;
            WatchedIcons[ name ] = icon;
            watchList.ResourceAdded += watchList_ResourceAdded;
            watchList.ResourceDeleting += watchList_ResourceDeleting;

            if( !rule.HasProp( "RuleTurnedOff") && watchList.Count > 0 )
            {
                FireIconOfWatcher( name );
                SetTooltipText();
            }
        }

        private void  UnregisterIconWatcherImpl( string name, bool deleteResource )
        {
            IResourceList watchList = (IResourceList) WatchedLists[ name ];
            watchList.ResourceAdded -= watchList_ResourceAdded;
            watchList.ResourceDeleting -= watchList_ResourceDeleting;
            watchList.Deinstantiate();

            if( deleteResource )
            {
                IResource rule = FindRule( name );
                Core.FilterManager.DeleteView( rule );
            }

            WatchedLists.Remove( name );
            WatchedIcons.Remove( name );
            WatchersOrder.Remove( name );
            ModifyIcon();
        }

        private static void  AddSpecificParams( IResource rule, string name, Icon icon )
        {
            rule.BeginUpdate();
            rule.SetProp( "IsTrayIconFilter", true );
            rule.SetProp( "DeepName", name );
            rule.SetProp( "IsLiveMode", true );
            JetMemoryStream mstrm = new JetMemoryStream( 2048 );
            icon.Save( mstrm );
            rule.SetProp( "IconBlob", mstrm );
            rule.DeleteProp( Core.Props.LastError );
            rule.EndUpdate();
        }
        #endregion Impl Register/Unregister

        #region Events
        private void watchList_ResourceAdded(object sender, ResourceIndexEventArgs e)
        {
            string name = FindChangedList( (IResourceList) sender );
            if( isActiveWatcher( name ) )
            {
                int count = WatchersOrder.Count;

                //  Avoid setting new icon if it is already set.
                if( count == 0 || 
                    ((IResourceList) sender).Count == 1 ||
                    (string)WatchersOrder[ count - 1 ] != name )
                {
                    FireIconOfWatcher( name );
                    WatchersOrder.Remove( name );
                    WatchersOrder.Add( name );
                }
                SetTooltipText();
            }
        }

        private void watchList_ResourceDeleting(object sender, ResourceIndexEventArgs e)
        {
            string  name = FindChangedList( (IResourceList) sender );
            IResourceList watchList = (IResourceList) WatchedLists[ name ];
            if( Mode == TrayIconMode.Outlook || watchList.Count == 1 )
            {
                WatchersOrder.Remove( name );
                ModifyIcon();
                if( WatchersOrder.Count == 0 )
                    return;
            }
            SetTooltipText();
        }

        private void AllRules_ResourceChanged(object sender, ResourcePropIndexEventArgs e)
        {
            ModifyIcon();
        }
        #endregion Events

        #region Impl misc
        private void ModifyIcon()
        {
            for( int index = WatchersOrder.Count - 1; index >= 0; index-- )
            {
                string  ruleName = (string) WatchersOrder[ index ];
                if( isActiveWatcher( ruleName ) )
                {
                    IResourceList list = (IResourceList) WatchedLists[ ruleName ];
                    if( list.Count > 0 )
                    {
                        FireIconOfWatcher( ruleName );
                        SetTooltipText();
                        return;
                    }
                }
            }

            //  If no watcher currently is active or all watchers do not
            //  satisfy their conditions.
            NotIcon.Icon = DefaultIcon;
            NotIcon.Text = DefaultTooltip;
        }
        private string  FindChangedList( IResourceList changedList )
        {
            foreach( string name in WatchedLists.Keys )
            {
                if( changedList == (IResourceList) WatchedLists[ name ] )
                    return name;
            }
            Debug.Assert( false );
            return null;
        }

        private void  FireIconOfWatcher( string watcherName )
        {
            Icon icon = (Icon) WatchedIcons[ watcherName ];
            //  Until I've not found the reason - just a workaround (OM-13073/13063):
            try
            {
                NotIcon.Icon = icon;
            }
            catch( NullReferenceException )
            {
                //  Nothing to do yet.
            }
        }

        private void  SetTooltipText()
        {
            if( WatchedLists.Count == 0 )
            {
                NotIcon.Text = DefaultTooltip;
            }
            else
            {
                string newTooltip = string.Empty;
                foreach( string name in WatchersOrder )
                {
                    if( isActiveWatcher( name ) )
                    {
                        IResourceList list = (IResourceList) WatchedLists[ name ];
                        if( list.Count > 0 )
                            newTooltip += name + ": " + list.Count + "\n";
                    }
                }
                newTooltip = newTooltip.TrimEnd( '\n' );

                //  In the case when no resources fit into the current set of
                //  rules, restore default tooltip.
                if( newTooltip == string.Empty )
                    newTooltip = DefaultTooltip;
                else
                if( newTooltip.Length >= 64 )
                    newTooltip = newTooltip.Substring( 0, 60 ) + "...";

                NotIcon.Text = newTooltip;
            }
        }

        private bool  isActiveWatcher( string name )
        {
            foreach( IResource rule in AllRules )
            {
                if( rule.GetStringProp( Core.Props.Name ) == name )
                    return !rule.HasProp( "RuleTurnedOff" );
            }
            Debug.Assert( false );
            return false;
        }

        private static ResourceProxy GetRuleProxy( IResource rule )
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
        #endregion Impl misc
    }
}