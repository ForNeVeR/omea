// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using JetBrains.Omea.GUIControls;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
    internal class ActionManager: IActionManager
    {
        internal class FilteredAction
        {
            private readonly IAction _action;
            private readonly IActionStateFilter[] _filters;

            public FilteredAction( IAction action, IActionStateFilter[] filters )
            {
                _action = action;
                _filters = filters;
            }

            public bool Execute( IActionContext context )
            {
                if ( !CanExecute( context ) )
                    return false;

                _action.Execute( context );
                return true;
            }

            public bool CanExecute( IActionContext context )
            {
                ActionPresentation presentation = new ActionPresentation();
                presentation.Reset();

                if ( _filters != null )
                {
                    foreach( IActionStateFilter filter in _filters )
                    {
                        filter.Update( context, ref presentation );
                        if ( !presentation.Visible )
                            break;
                    }
                }

                if ( presentation.Visible )
                {
                    _action.Update( context, ref presentation );
                }

                if ( !presentation.Visible || !presentation.Enabled )
                    return false;

                return true;
            }

            public IAction Action
            {
                get { return _action; }
            }
        }

        internal class KeyboardAction: FilteredAction
        {
            private readonly string _resourceType;
            private readonly Keys _shortcutKey;

            public KeyboardAction( IAction action, Keys shortcutKey, string resourceType,
                IActionStateFilter[] filters )
                : base( action, filters )
            {
                _resourceType = resourceType;
                _shortcutKey  = shortcutKey;
            }

            internal string  ResourceType { get { return _resourceType; } }
            internal Keys    ShortcutKey  { get { return _shortcutKey; } }
        }

        private readonly ContextMenuStrip         _contextMenu;
        private readonly ContextMenuActionManager _contextMenuActionManager;

        private readonly MenuStrip      _mainMenu;
        private readonly Hashtable      _mainMenuActionManagers = new Hashtable();    // menu name -> MenuActionManager

        private readonly ToolbarActionManager _toolbarManager;
        private readonly ResourceBrowser    _resourceBrowser;

        private readonly Hashtable      _doubleClickActions   = new Hashtable();
        private readonly Hashtable      _linkClickActions     = new Hashtable();
        private readonly Hashtable      _keyToAction          = new Hashtable();
        private readonly Hashtable      _actionToKey          = new Hashtable();
        private readonly ArrayList      _genericDoubleClickActions = new ArrayList();
        private readonly KeysConverter  _keysConverter = new KeysConverter();
        private readonly HashMap        _xmlActionCache = new HashMap();    // class name -> IAction
        private readonly HashMap        _actionDefNodes     = new HashMap();    // action def ID -> XmlNode
        private readonly HashMap        _compositeActions = new HashMap();  // id -> CompositeAction
        private readonly HashSet        _excludedActionAssemblies = new HashSet();
        private readonly HashMap        _assemblyNameCache = new HashMap();

        public ActionManager( MenuStrip mainMenu, ContextMenuStrip contextMenu, ResourceBrowser resourceBrowser )
        {
            _mainMenu        = mainMenu;
            _contextMenu     = contextMenu;
            _resourceBrowser = resourceBrowser;

            if ( _resourceBrowser != null )
                _toolbarManager = resourceBrowser.ToolBarActionManager;

            _contextMenuActionManager = new ContextMenuActionManager( _contextMenu );
            Core.StateChanged += Core_StateChanged;
        }

        /// <summary>
        /// Activate menu initialization right after the Omea has initialized all its
        /// plugins and they have inserted their corresponding menus into the common
        /// menu framework.
        /// </summary>
        private void Core_StateChanged(object sender, EventArgs e)
        {
            if( Core.State == CoreState.Running )
            {
                Core.StateChanged -= Core_StateChanged;
                MenuInitializationFinished();
            }
        }

        public void EndUpdateActions()
        {
            _toolbarManager.Dispose();
        }

        /// <summary>
        /// When after the Omea initialization the event queue becomes empty
        /// (for the first time) we can prefill main menu submenus.
        /// </summary>
        public void MenuInitializationFinished()
        {
            foreach( DictionaryEntry de in _mainMenuActionManagers )
            {
            	MenuActionManager manager = (MenuActionManager) de.Value;
                manager.FillMenuIfNecessary( GetMainMenuActionContext() );
            }
        }

        internal void RegisterCoreActions( bool noTextIndex )
        {
        	ListAnchor anchorLast = new ListAnchor( AnchorType.Last );
            RegisterContextMenuActionGroup( ActionGroups.ITEM_OPEN_ACTIONS, anchorLast );
            RegisterContextMenuActionGroup( ActionGroups.ITEM_FIND_ACTIONS, anchorLast );
            RegisterContextMenuActionGroup( ActionGroups.ITEM_MODIFY_ACTIONS, anchorLast );
            RegisterContextMenuActionGroup( "", anchorLast );

            RegisterMainMenuActionGroup( ActionGroups.VIEW_VIEWPANE_ACTIONS,   "View",   "Panes", anchorLast );
            RegisterMainMenuActionGroup( ActionGroups.GO_TAB_ACTIONS,          "Go",   anchorLast );
            RegisterMainMenuActionGroup( ActionGroups.ACTION_STANDARD_ACTIONS, "Actions", anchorLast );
            RegisterMainMenuActionGroup( ActionGroups.TOOLS_OPTIONS_ACTIONS,   "Tools", anchorLast );
        }

        private static string StripMenuName( string name )
        {
            return name.Replace( "&", "" );
        }

        public void RegisterMainMenu( string menuName, ListAnchor anchor )
        {
            // check if the menu is already registered
            foreach( ToolStripItem item in _mainMenu.Items )
            {
                if ( StripMenuName( item.Text ) == StripMenuName( menuName ) )
                    return;
            }

            int index = FindMainMenuInsertIndex( anchor );
            ArrayList itemStack = new ArrayList();
            while( _mainMenu.Items.Count > index )
            {
                int lastIndex = _mainMenu.Items.Count - 1;
                itemStack.Insert( 0, _mainMenu.Items[ lastIndex ] );
                _mainMenu.Items.RemoveAt( lastIndex );
            }

            ToolStripMenuItem newSubMenu = new ToolStripMenuItem( menuName );
            _mainMenu.Items.Add( newSubMenu );
            _mainMenu.Items.AddRange( (ToolStripItem[]) itemStack.ToArray( typeof( ToolStripItem ) ) );
        }

        private int FindMainMenuInsertIndex( ListAnchor anchor )
        {
            if ( anchor.AnchorType == AnchorType.First )
                return 0;
            if ( anchor.AnchorType == AnchorType.Last )
                return _mainMenu.Items.Count;

            for( int i = 0; i < _mainMenu.Items.Count; i++ )
            {
                if ( StripMenuName( _mainMenu.Items [ i ].Text ) == StripMenuName( anchor.RefId ) )
                {
                    return anchor.AnchorType == AnchorType.After ? i + 1 : i;
                }
            }
            throw new ActionException( "Referended menu " + anchor.RefId + " not found" );
        }


        public void RegisterMainMenuActionGroup( string groupId, string menuName, ListAnchor anchor )
        {
            RegisterMainMenuActionGroup( groupId, menuName, null, anchor );
        }

        public void RegisterMainMenuActionGroup( string groupId, string menuName, string submenuName, ListAnchor anchor )
        {
            menuName = StripMenuName( menuName );
        	ToolStripMenuItem mainMenuItem = FindMainMenuItem( menuName );
        	if ( mainMenuItem == null )
            {
                throw new ActionException( "Top-level menu " + menuName + " not found" );
            }

            MenuActionManager manager = (MenuActionManager) _mainMenuActionManagers [menuName];
            if ( manager == null )
            {
                manager = new MainMenuActionManager( mainMenuItem );
                _mainMenuActionManagers[ menuName ] = manager;
            }
            manager.RegisterGroup( groupId, submenuName, anchor );
        }

        public void SuppressMainMenuGroupSeparator( string groupId1, string groupId2 )
        {
            foreach( DictionaryEntry de in _mainMenuActionManagers )
            {
                MenuActionManager manager = (MenuActionManager) de.Value;
                if ( manager.ContainsGroup( groupId1 ) && manager.ContainsGroup( groupId2 ) )
                {
                    manager.SuppressGroupSeparator( groupId1, groupId2 );
                    return;
                }
            }
            throw new ArgumentException( "Groups '" + groupId1 + "' and '" + groupId2 +
                                         " ' not found or found in different menus" );
        }

        public void RegisterMainMenuAction( IAction action, string groupId, ListAnchor anchor, string text,
                                            Image icon, string resourceType, IActionStateFilter[] filters )
        {
            foreach( DictionaryEntry de in _mainMenuActionManagers )
            {
                MenuActionManager manager = (MenuActionManager) de.Value;
                if ( manager.ContainsGroup( groupId ) )
                {
                	manager.RegisterAction( action, groupId, anchor, text, icon, resourceType, filters );
                    return;
                }
            }
            throw new ArgumentException( "Invalid action group name " + groupId, "groupId" );
        }

        public void UnregisterMainMenuAction( IAction action )
        {
            foreach( DictionaryEntry de in _mainMenuActionManagers )
            {
                MenuActionManager manager = (MenuActionManager) de.Value;
                if ( manager.UnregisterAction( action ) )
                {
                    break;
                }
            }
        }

        private ToolStripMenuItem FindMainMenuItem( string name )
        {
            foreach( ToolStripMenuItem item in _mainMenu.Items )
            {
                if ( item.Text.Replace( "&", "" ) == name.Replace( "&", "") )
                {
                    return item;
                }
            }
            return null;
        }

        public IActionContext GetMainMenuActionContext()
        {
        	Form frm = (Form) Core.MainWindow;
            Control ctl = frm.ActiveControl;

            if ( ctl != null )
            {
                bool foundFocusedChild;
                do
                {
                    foundFocusedChild = false;
                    foreach( Control child in ctl.Controls )
                    {
                        if ( child.ContainsFocus )
                        {
                            ctl = child;
                            foundFocusedChild = true;
                            break;
                        }
                    }
                } while( foundFocusedChild );
            }

            while( ctl != null )
            {
                IContextProvider provider = ctl as IContextProvider;
                if ( provider != null )
                {
                	IActionContext context = provider.GetContext( ActionContextKind.MainMenu );
                    if ( context != null )
                    {
                        return context;
                    }
                }
                ctl = ctl.Parent;
            }
            return new ActionContext( ActionContextKind.MainMenu, null, null );
        }

        /**
         * Registers a group for toolbar buttons.
         */

        public void RegisterToolbarActionGroup( string groupId, ListAnchor anchor )
        {
            _toolbarManager.RegisterActionGroup( groupId, anchor );
        }

        /**
         * Adds an action button with an Icon image to the toolbar.
         */

        public void RegisterToolbarAction( IAction action, string groupId, ListAnchor anchor,
            Icon icon, string text, string tooltip, string resourceType, IActionStateFilter[] filters )
        {
            _toolbarManager.RegisterAction( action, groupId, anchor, icon, text, tooltip, resourceType, filters );
        }

        /**
         * Adds an action button to the toolbar.
         */

        public void RegisterToolbarAction( IAction action, string groupId, ListAnchor anchor,
            Image icon, string text, string tooltip, string resourceType, IActionStateFilter[] filters )
        {
            _toolbarManager.RegisterAction( action, groupId, anchor, icon, text, tooltip, resourceType, filters );
        }

        public void RegisterToolbarActionFilter( string actionId, IActionStateFilter filter )
        {
            _toolbarManager.RegisterActionFilter( actionId, filter );
        }

        public void UnregisterToolbarAction( IAction action )
        {
            _toolbarManager.UnregisterAction( action );
        }

        /**
         * Registers an action group for the URL bar.
         */

        public void RegisterUrlBarActionGroup( string groupId, ListAnchor anchor )
        {
            _resourceBrowser.RegisterUrlBarActionGroup( groupId, anchor );
        }

        /**
         * Adds an action button with an Icon image to the URL bar.
         */

        public void RegisterUrlBarAction( IAction action, string groupId, ListAnchor anchor,
            Icon icon, string text, string tooltip, IActionStateFilter[] filters )
        {
            _resourceBrowser.RegisterUrlBarAction( action, groupId, anchor, icon, text, tooltip, filters );
        }

        public void RegisterUrlBarAction( IAction action, string groupId, ListAnchor anchor,
            Image icon, string text, string tooltip, IActionStateFilter[] filters )
        {
            _resourceBrowser.RegisterUrlBarAction( action, groupId, anchor, icon, text, tooltip, filters );
        }

        public void UnregisterUrlBarAction( IAction action )
        {
            _resourceBrowser.UnregisterUrlBarAction( action );
        }

        /**
         * Registers a group of actions used in the popup menu.
         */

        public void RegisterContextMenuActionGroup( string name, ListAnchor anchor )
        {
            RegisterContextMenuActionGroup( name, null, anchor );
        }

        public void RegisterContextMenuActionGroup( string name, string submenuName, ListAnchor anchor )
        {
            _contextMenuActionManager.RegisterGroup( name, submenuName, anchor );
        }

        public void SuppressContextMenuGroupSeparator( string groupId1, string groupId2 )
        {
            _contextMenuActionManager.SuppressGroupSeparator( groupId1, groupId2 );
        }

        /**
         * Registers an action in a specified group for the specified resource types.
         */

        public void RegisterContextMenuAction( IAction action, string groupId, ListAnchor anchor,
                                               string text, Image icon, string resourceType, IActionStateFilter[] filters )
        {
            _contextMenuActionManager.RegisterAction( action, groupId, anchor, text, icon, resourceType, filters );
        }

        public void UnregisterContextMenuAction( IAction action )
        {
            _contextMenuActionManager.UnregisterAction( action );
        }

        internal static void ExecuteAction( IAction action, ActionContext context )
        {
            if ( action != null )
            {
                try
                {
                    action.Execute( context );
                }
                catch( Exception e )
                {
                    Core.ReportException( e, false );
                }
            }
        }

        /**
         * Registers an action that is executed when a resource is double-clicked.
         */

        public void RegisterDoubleClickAction( IAction action, string resourceType, IActionStateFilter[] filters )
        {
            if ( resourceType == null )
                _genericDoubleClickActions.Add( new FilteredAction( action, filters ) );
            else
                _doubleClickActions [resourceType] = new FilteredAction( action, filters );
        }

        public void ExecuteDoubleClickAction( IResource res )
        {
            if ( res.IsDeleted )
            {
                return;
            }

            IAction action = GetDoubleClickAction( res );
            if ( action != null )
            {
                ActionContext context = new ActionContext( ActionContextKind.Other, null, res.ToResourceList() );
                action.Execute( context );
            }
        }

        public void ExecuteDoubleClickAction( IResourceList resList )
        {
            for( int i = 0; i < resList.Count; i++ )
            {
                IResource res;
                try
                {
                    res = resList[ i ];
                }
                catch( StorageException )
                {
                    continue;
                }
                ExecuteDoubleClickAction( res );
            }
        }

        public IAction GetDoubleClickAction( IResource res )
        {
            ActionContext context = new ActionContext( ActionContextKind.Other, null, res.ToResourceList() );
            FilteredAction action = (FilteredAction) _doubleClickActions [res.Type];
            if ( action != null && action.CanExecute( context ) )
            {
                return action.Action;
            }

            foreach( FilteredAction genericAction in _genericDoubleClickActions )
            {
                if ( genericAction.CanExecute( context ) )
                {
                    return genericAction.Action;
                }
            }
            return null;
        }

        public void UnregisterDoubleClickAction( IAction action )
        {
            foreach( FilteredAction fAction in _genericDoubleClickActions )
            {
                if ( fAction.Action == action )
                {
                    _genericDoubleClickActions.Remove( fAction );
                    return;
                }
            }

            foreach( DictionaryEntry de in _doubleClickActions )
            {
                FilteredAction fAction = (FilteredAction) de.Value;
                if ( fAction.Action == action )
                {
                    _doubleClickActions.Remove( de.Key );
                    break;
                }
            }
        }

        /**
         * Registers an action which is executed when a link is clicked in the
         * links pane.
         */

        public void RegisterLinkClickAction( IAction action, string resourceType, IActionStateFilter[] filters )
        {
            #region Preconditions
            if ( resourceType == null )
                throw new ArgumentNullException( "resourceType" );
            #endregion Preconditions

            _linkClickActions [resourceType] = new FilteredAction( action, filters );
        }

        public void UnregisterLinkClickAction( IAction action )
        {
            foreach( DictionaryEntry de in _linkClickActions )
            {
                FilteredAction fAction = (FilteredAction) de.Value;
                if ( fAction.Action == action )
                {
                    _linkClickActions.Remove( de.Key );
                    break;
                }
            }
        }

        public bool ExecuteLinkClickAction( IActionContext context )
        {
            #region Preconditions
            if ( context == null )
                throw new ArgumentNullException( "context" );
            #endregion Preconditions

            if ( context.SelectedResources.Count > 0 )
            {
                FilteredAction action = (FilteredAction) _linkClickActions [context.SelectedResources [0].Type];
                if ( action == null )
                    return false;

                return action.Execute( context );
            }
            return false;
        }

        /**
         * Shows the context menu for the specified resources.
         */

        void IActionManager.ShowResourceContextMenu( IActionContext context, Control ownerControl, int x, int y )
        {
            #region Preconditions
            if ( context == null )
                throw new ArgumentNullException( "context" );
            #endregion Preconditions

            _contextMenuActionManager.ActionContext = context;
            _contextMenu.Show( ownerControl, new Point( x, y ) );
        }

        #region Keyboard Actions
        /**
         * Registers a keyboard shortcut for the action.
         */

        public void RegisterKeyboardAction( IAction action, Keys shortcutKey, string resourceType,
                                            IActionStateFilter[] filters )
        {
            KeyboardAction kbdAction = new KeyboardAction( action, shortcutKey, resourceType, filters );

            ArrayList keyActions = (ArrayList) _keyToAction [shortcutKey];
            if ( keyActions == null )
            {
                keyActions = new ArrayList();
                _keyToAction [shortcutKey] = keyActions;
            }
            keyActions.Add( kbdAction );

            if ( !_actionToKey.ContainsKey( action ) )
            {
                _actionToKey[ action ] = shortcutKey;
            }
        }

        public void UnregisterKeyboardAction( IAction action )
        {
            // one action may be registered for multiple shortcuts, so we can't use the information
            // in _actionToKey to determine from what lists the action should be removed
            _actionToKey.Remove( action );
            foreach( DictionaryEntry de in _keyToAction )
            {
                ArrayList keyActions = (ArrayList) de.Value;
                foreach( KeyboardAction kbdAction in keyActions )
                {
                    if ( kbdAction.Action == action )
                    {
                        keyActions.Remove( kbdAction );
                        break;
                    }
                }
            }
        }

        /**
         * Returns the keyboard shortcut assigned to the specified action (if there
         * are many, returns a random one).
         */

        public string GetKeyboardShortcut( IAction action )
        {
            if ( _actionToKey.ContainsKey( action ) )
            {
                Keys key = (Keys) _actionToKey [action];
                string result = (string) _keysConverter.ConvertTo( key, typeof(string) );

                // KeysConverter converts Ctrl+digit combinations to Ctrl+D#,
                // instead of the correct Ctrl+#
                if ( result.Length >= 2 && Char.IsDigit( result, result.Length-1 ) &&
                    result [result.Length-2] == 'D' )
                {
                    result = result.Substring( 0, result.Length-2 ) + result.Substring( result.Length-1 );
                }
                return result;
            }
            return "";
        }

        public string GetKeyboardShortcut( IAction action, IActionContext context )
        {
            if ( _actionToKey.ContainsKey( action ) )
            {
                Keys key = (Keys) _actionToKey [action];
                ArrayList kbdActions = (ArrayList) _keyToAction [key];
                foreach( KeyboardAction kbdAction in kbdActions )
                {
                    if ( kbdAction.Action.Equals( action ) )
                    {
                        return GetKeyboardShortcut( action );
                    }
                }
            }
            return "";
        }

        public Keys? GetKeyboardShortcutEx( IAction action, IActionContext context )
        {
            if ( _actionToKey.ContainsKey( action ) )
            {
                Keys key = (Keys) _actionToKey [action];
                ArrayList kbdActions = (ArrayList) _keyToAction [key];
                foreach( KeyboardAction kbdAction in kbdActions )
                {
                    if ( kbdAction.Action.Equals( action ) )
                    {
                        return key;
                    }
                }
            }
            return null;
        }

        /**
         * Tries to find a keyboard action registered for the specified shortcut
         * key and resources. Returns true if the action has been successfully
         * executed and false otherwise.
         */

        bool IActionManager.ExecuteKeyboardAction( IActionContext context, Keys shortcutKey )
        {
            if ( context == null )
                context = new ActionContext( ActionContextKind.Keyboard, null, null );

            ArrayList keyActions = (ArrayList) _keyToAction [shortcutKey];
            if ( keyActions != null )
            {
                string[] resTypes = context.SelectedResources.GetAllTypes();
                foreach( KeyboardAction keyAction in keyActions )
                {
                    if ( keyAction.ResourceType != null &&
                        (resTypes.Length != 1 || resTypes [0] != keyAction.ResourceType ) )
                    {
                        continue;
                    }

                    if ( keyAction.Execute( context ) )
                    {
						return true;
					}
                }
            }
            return false;
        }
        #endregion Keyboard Actions

        #region LinksPane Actions
        /**
         * Registers an action for the Links pane.
         */

        public void RegisterLinksPaneAction( IAction action, string text, string resourceType, IActionStateFilter[] filters )
        {
            LinksPaneActionManager.GetManager().RegisterAction( action, text, resourceType, filters );
        }

        public void UnregisterLinksPaneAction( IAction action )
        {
            LinksPaneActionManager.GetManager().UnregisterAction( action );
        }
        #endregion LinksPane Actions

        internal void RegisterCompositeAction( string id, CompositeAction action )
        {
        	_compositeActions [id] = action;
        }

        /**
         * Registers a component that is contributed by a plugin to a composite action.
         */

        public void RegisterActionComponent( IAction action, string compositeId,
                                             string resourceType, IActionStateFilter[] filters )
        {
            CompositeAction composite = (CompositeAction) _compositeActions [compositeId];
            if ( composite == null )
            {
            	throw new ArgumentException( "Invalid composite ID " + compositeId, "compositeId" );
            }

            composite.AddComponent( resourceType, action, filters );
        }

        #region XML configuration

        public void DisableXmlActionConfiguration( Assembly pluginAssembly )
        {
            _excludedActionAssemblies.Add( pluginAssembly );
        }

        /**
         * Loads the XML configuration for actions.
         */

        internal void LoadXmlConfiguration( Assembly pluginAssembly, XmlNode node )
        {
            if ( _excludedActionAssemblies.Contains( pluginAssembly ) )
            {
                return;
            }

            XmlAttribute attr = node.Attributes ["namespace"];
            string ns = (attr == null) ? "" : attr.Value;

            foreach( XmlNode childNode in node.ChildNodes )
            {
                try
                {
                    switch( childNode.Name )
                    {
                        case "actiondef":    LoadActionDef( childNode );             break;
                        case "main-menu":    LoadMainMenuActions( pluginAssembly, childNode, ns );       break;
                        case "popup-menu":   LoadContextMenuActions( pluginAssembly, childNode, ns );    break;
                        case "context-menu": LoadContextMenuActions( pluginAssembly, childNode, ns );      break;
                        case "keyboard":     LoadKeyboardActions( pluginAssembly, childNode, ns );       break;
                        case "double-click": LoadDoubleClickActions( pluginAssembly, childNode, ns );    break;
                        case "toolbar":      LoadToolbarActions( pluginAssembly, childNode, ns, false ); break;
                        case "urlbar":       LoadToolbarActions( pluginAssembly, childNode, ns, true );  break;
                        case "links-pane":   LoadLinksPaneActions( pluginAssembly, childNode, ns );      break;
                        case "link-click":   LoadLinkClickActions( pluginAssembly, childNode, ns );      break;
                        case "composite":    LoadCompositeActions( pluginAssembly, childNode, ns );      break;
                    }
                }
                catch( XmlToolsException e )
                {
                    throw new ActionException( "Error loading <" + childNode.Name + "> for " +
                        pluginAssembly.GetName().Name + ": " + e.Message );
                }
                catch( ActionException e )
                {
                    throw new ActionException( "Error loading <" + childNode.Name + "> for " +
                        pluginAssembly.GetName().Name + ": " + e.Message );
                }
            }
            _actionDefNodes.Clear();
        }

        /**
         * Loads an action with a set of parameters and filters which can be reused later.
         */

        private void LoadActionDef( XmlNode node )
        {
            string id = XmlTools.GetRequiredAttribute( node, "id" );
            _actionDefNodes [id] = node;
        }

        /**
         * If the specified node contains an actiondef reference, returns the actiondef
         * node. Otherwise, returns the node itself.
         */

        private XmlNode GetActionDefNode( XmlNode node )
        {
            XmlAttribute attr = node.Attributes ["ref"];
            if ( attr != null )
            {
                XmlNode actionDefNode = (XmlNode) _actionDefNodes [attr.Value];
                if ( actionDefNode == null )
                {
                    throw new ActionException( "Unknown action reference " + attr.Value );
                }
                return actionDefNode;
            }
            return node;
        }

        /**
         * Loads the main menu actions from the XML file.
         */

        private void LoadMainMenuActions( Assembly pluginAssembly, XmlNode node, string ns )
        {
            foreach( XmlNode menuNode in node.SelectNodes( "menu" ) )
            {
                string menuName = XmlTools.GetRequiredAttribute( menuNode, "name" );
                ListAnchor anchor = LoadListAnchor( menuNode );
                RegisterMainMenu( menuName, anchor );
            }
            foreach( XmlNode groupNode in node.SelectNodes( "group" ) )
            {
                string groupId = XmlTools.GetRequiredAttribute( groupNode, "id" );

                XmlAttribute attrMenu = groupNode.Attributes ["menu"];
                if ( attrMenu != null )
                {
                    XmlAttribute attrSubmenu = groupNode.Attributes ["submenu"];
                    ListAnchor anchor = LoadListAnchor( groupNode );
                    RegisterMainMenuActionGroup( groupId, attrMenu.Value,
                        ( attrSubmenu == null ) ? null : attrSubmenu.Value, anchor );
                }

                XmlAttribute keepWith = groupNode.Attributes ["keepwith"];
                if ( keepWith != null )
                {
                    SuppressMainMenuGroupSeparator( groupId, keepWith.Value );
                }

                foreach( XmlNode actionNode in groupNode.SelectNodes( "action" ) )
                {
                    ListAnchor anchor = LoadListAnchor( actionNode );
                    string text = XmlTools.GetRequiredAttribute( actionNode, "name" );
                    string resType = XmlTools.GetOptionalAttribute( actionNode, "type" );
                    string iconPath = XmlTools.GetOptionalAttribute( actionNode, "icon" );
//                    Image icon = ( iconPath == null ) ? null : Utils.TryGetEmbeddedResourceImageFromAssembly( pluginAssembly.GetName().Name, iconPath );
                    Image icon = ( iconPath == null ) ? null : Utils.TryGetEmbeddedResourceImageFromAssembly( pluginAssembly, iconPath );

                    XmlNode actionDefNode = GetActionDefNode( actionNode );
                    try
                    {
                        IAction action = CreateActionFromXml( pluginAssembly, actionDefNode, ns );
                        IActionStateFilter[] filters = LoadFiltersFromXml( pluginAssembly, actionDefNode, ns, TabFilterMode.None );

                        RegisterMainMenuAction( action, groupId, anchor, text, icon, resType, filters );
                    }
                    catch( Exception ex )
                    {
                        Core.ReportException( ex, false );
                    }
                }
            }
        }

        /**
         * Loads the context menu actions from the XML file.
         */

        private void LoadContextMenuActions( Assembly pluginAssembly, XmlNode node, string ns )
        {
            foreach( XmlNode groupNode in node.SelectNodes( "group" ) )
            {
                string groupId = XmlTools.GetRequiredAttribute( groupNode, "id" );

                ListAnchor anchor = LoadListAnchor( groupNode );
                XmlAttribute attrSubmenu = groupNode.Attributes ["submenu"];
                RegisterContextMenuActionGroup( groupId, (attrSubmenu == null) ? null : attrSubmenu.Value, anchor );

                XmlAttribute keepWith = groupNode.Attributes ["keepwith"];
                if ( keepWith != null )
                {
                    SuppressContextMenuGroupSeparator( groupId, keepWith.Value );
                }

                foreach( XmlNode actionNode in groupNode.SelectNodes( "action" ) )
                {
                    string attrName = XmlTools.GetRequiredAttribute( actionNode, "name" );
                    string resType = XmlTools.GetOptionalAttribute( actionNode, "type" );
                    string iconPath = XmlTools.GetOptionalAttribute( actionNode, "icon" );
//                    Image icon = ( iconPath == null ) ? null : Utils.TryGetEmbeddedResourceImageFromAssembly( pluginAssembly.GetName().Name, iconPath );
                    Image icon = ( iconPath == null ) ? null : Utils.TryGetEmbeddedResourceImageFromAssembly( pluginAssembly, iconPath );

                    XmlNode actionDefNode = GetActionDefNode( actionNode );
                    try
                    {
                        IAction action = CreateActionFromXml( pluginAssembly, actionDefNode, ns );

                        anchor = LoadListAnchor( actionNode );
                        IActionStateFilter[] filters = LoadFiltersFromXml( pluginAssembly, actionDefNode, ns, TabFilterMode.None );

                        RegisterContextMenuAction( action, groupId, anchor, attrName, icon, resType, filters );
                    }
                    catch( Exception ex )
                    {
                        Core.ReportException( ex, false );
                    }
                }
            }
        }

        /**
         * Loads the keyboard actions from the XML file.
         */

        private void LoadKeyboardActions( Assembly pluginAssembly, XmlNode node, string ns )
        {
            foreach( XmlNode actionNode in node.SelectNodes( "action" ) )
            {
                string key = XmlTools.GetRequiredAttribute( actionNode, "key" );
                XmlAttribute attrResType = actionNode.Attributes ["type"];
                Keys shortcut = (Keys) _keysConverter.ConvertFromInvariantString( key );
                XmlNode actionDefNode = GetActionDefNode( actionNode );
                try
                {
                    IAction action = CreateActionFromXml( pluginAssembly, actionDefNode, ns );
                    IActionStateFilter[] filters = LoadFiltersFromXml( pluginAssembly, actionDefNode, ns,
                                                                       TabFilterMode.TabOnly );

                    RegisterKeyboardAction( action, shortcut,
                                            attrResType == null ? null : attrResType.Value, filters );
                }
                catch( Exception ex )
                {
                    Core.ReportException( ex, false );
                }
            }
        }

        /**
         * Loads the double-click actions from the XML file.
         */

        private void LoadDoubleClickActions( Assembly pluginAssembly, XmlNode node, string ns )
        {
            foreach( XmlNode actionNode in node.SelectNodes( "action" ) )
            {
                XmlAttribute attrResType = actionNode.Attributes ["type"];
                XmlNode actionDefNode = GetActionDefNode( actionNode );
                try
                {
                    IAction action = CreateActionFromXml( pluginAssembly, actionDefNode, ns );
                    IActionStateFilter[] filters = LoadFiltersFromXml( pluginAssembly, actionDefNode, ns,
                                                                       TabFilterMode.None );

                    RegisterDoubleClickAction( action,
                                               (attrResType == null ? null : attrResType.Value), filters );
                }
                catch( Exception ex )
                {
                    Core.ReportException( ex, false );
                }
            }
        }

        /**
         * Loads the toolbar actions from the XML file.
         */

        private void LoadToolbarActions( Assembly pluginAssembly, XmlNode node, string ns, bool urlbar )
        {
            XmlAttribute attrIconPrefix = node.Attributes ["iconprefix"];
            string iconPrefix = (attrIconPrefix == null) ? "" : attrIconPrefix.Value + ".";

            foreach( XmlNode groupNode in node.SelectNodes( "group" ) )
            {
                string groupId = XmlTools.GetRequiredAttribute( groupNode, "id" );
                ListAnchor anchor = LoadListAnchor( groupNode );
                if ( urlbar )
                {
                    RegisterUrlBarActionGroup( groupId, anchor );
                }
                else
                {
                    RegisterToolbarActionGroup( groupId, anchor );
                }

                foreach( XmlNode actionNode in groupNode.SelectNodes( "action" ) )
                {
                    anchor = LoadListAnchor( actionNode );
                    string tooltip = XmlTools.GetRequiredAttribute( actionNode, "tooltip" );
                    string icon = XmlTools.GetRequiredAttribute( actionNode, "icon" );
                    Stream stream = pluginAssembly.GetManifestResourceStream( iconPrefix + icon );
                    if ( stream == null )
                        throw new ActionException( "Icon " + icon + "not found" );

                    XmlAttribute attrType = actionNode.Attributes ["type"];
                    XmlAttribute attrText = actionNode.Attributes ["text"];
                    string text = (attrText == null) ? null : attrText.Value;

                    XmlNode actionDefNode = GetActionDefNode( actionNode );
                    try
                    {
                        IActionStateFilter[] filters = LoadFiltersFromXml( pluginAssembly, actionDefNode, ns,
                                                                           TabFilterMode.TabOnly );
                        IAction action = CreateActionFromXml( pluginAssembly, actionDefNode, ns );

                        if ( urlbar )
                        {
                            RegisterUrlBarAction( action, groupId, anchor, new Icon( stream ), text, tooltip, filters );
                        }
                        else
                        {
                            string type = ( attrType == null ) ? null : attrType.Value;
                            RegisterToolbarAction( action, groupId, anchor, new Icon( stream ), text, tooltip,
                                                   type, filters );
                        }
                    }
                    catch( Exception ex )
                    {
                        Core.ReportException( ex, false );
                    }
                }
            }
        }

        /**
         * Loads the links pane actions from the XML file.
         */

        private void LoadLinksPaneActions( Assembly pluginAssembly, XmlNode node, string ns )
        {
            foreach( XmlNode actionNode in node.SelectNodes( "action" ) )
            {
                string name = XmlTools.GetRequiredAttribute( actionNode, "name" );
                XmlAttribute attrType = actionNode.Attributes ["type"];
                XmlNode actionDefNode = GetActionDefNode( actionNode );
                try
                {
                    IAction action = CreateActionFromXml( pluginAssembly, actionDefNode, ns );
                    IActionStateFilter[] filters = LoadFiltersFromXml( pluginAssembly, actionDefNode, ns,
                                                                       TabFilterMode.None );

                    RegisterLinksPaneAction( action, name, (attrType == null ) ? null : attrType.Value, filters );
                }
                catch( Exception ex )
                {
                    Core.ReportException( ex, false );
                }
            }
        }

        /**
         * Loads the link click actions from the XML file.
         */

        private void LoadLinkClickActions( Assembly pluginAssembly, XmlNode node, string ns )
        {
            foreach( XmlNode actionNode in node.SelectNodes( "action" ) )
            {
                string resType = XmlTools.GetRequiredAttribute( actionNode, "type" );
                XmlNode actionDefNode = GetActionDefNode( actionNode );
                try
                {
                    IAction action = CreateActionFromXml( pluginAssembly, actionDefNode, ns );
                    IActionStateFilter[] filters = LoadFiltersFromXml( pluginAssembly, actionDefNode, ns,
                                                                       TabFilterMode.None );

                    RegisterLinkClickAction( action, resType, filters );
                }
                catch( Exception ex )
                {
                    Core.ReportException( ex, false );
                }
            }
        }

        /**
         * Loads the composite actions from the XML file.
         */

        private void LoadCompositeActions( Assembly pluginAssembly, XmlNode node, string ns )
        {
        	foreach( XmlNode actionNode in node.SelectNodes( "component" ) )
        	{
        		string id = XmlTools.GetRequiredAttribute( actionNode, "id" );
                XmlAttribute attrType = actionNode.Attributes ["type"];
                XmlNode actionDefNode = GetActionDefNode( actionNode );
        	    try
        	    {
        	        IAction action = CreateActionFromXml( pluginAssembly, actionDefNode, ns );
        	        IActionStateFilter[] filters = LoadFiltersFromXml( pluginAssembly, actionNode, ns,
        	                                                           TabFilterMode.TabOrAll );

        	        RegisterActionComponent( action, id, ( attrType == null ) ? null : attrType.Value,
        	                                 filters );
        	    }
        	    catch( Exception ex )
        	    {
        	        Core.ReportException( ex, false );
        	    }
        	}
        }

        /**
         * Creates an IAction instance from the specified XML node.
         */

        private IAction CreateActionFromXml( Assembly pluginAssembly, XmlNode actionNode, string ns )
        {
            Type actionType = FindActionType( pluginAssembly, actionNode, ns );
            string cacheKey = GetActionClass( actionNode, ns );
            object[] actionParams = null;

            XmlNodeList paramNodes = actionNode.SelectNodes( "param" );
            if ( paramNodes.Count > 0 )
            {
                actionParams = ParseActionParameters( paramNodes, ref cacheKey );
            }

            if ( _xmlActionCache.Contains( cacheKey ) )
            {
                return (IAction) _xmlActionCache [cacheKey];
            }

            IAction action = ( actionParams == null )
                ? (IAction) Activator.CreateInstance( actionType )
                : (IAction) Activator.CreateInstance( actionType, actionParams );

            _xmlActionCache.Add( cacheKey, action );
            return action;
        }

        /**
         * Finds an instance of the action class for the specified default assembly,
         * class name and default namespace.
         */

        private Type FindActionType( Assembly pluginAssembly, XmlNode actionNode, string ns )
        {
            string actionClass = GetActionClass( actionNode, ns );
            Assembly actionAssembly = pluginAssembly;
            XmlAttribute attrAsm = actionNode.Attributes ["assembly"];
            if ( attrAsm != null )
            {
                actionAssembly = (Assembly) _assemblyNameCache [attrAsm.Value];
                if ( actionAssembly == null )
                {
                    actionAssembly = Utils.FindAssembly( attrAsm.Value );
                    _assemblyNameCache [attrAsm.Value] = actionAssembly;
                }
            }

            return actionAssembly.GetType( actionClass, true );
        }

        private static string GetActionClass( XmlNode actionNode, string ns )
        {
            string actionClass = XmlTools.GetRequiredAttribute( actionNode, "class" );
            if ( ns != "" && actionClass.IndexOf( '.') < 0 )
            {
                actionClass = ns + "." + actionClass;
            }
            return actionClass;
        }

        /**
         * Parses the action constructor parameters from the specified collection of nodes.
         */

        private static object[] ParseActionParameters( XmlNodeList paramNodes, ref string cacheKey )
        {
            ArrayList paramList = new ArrayList();
            foreach( XmlNode node in paramNodes )
            {
                string paramType = XmlTools.GetRequiredAttribute( node, "type" );
                string paramValue = XmlTools.GetRequiredAttribute( node, "value" );
                switch( paramType )
                {
                    case "string": paramList.Add( paramValue ); break;
                    case "int":    paramList.Add( Int32.Parse( paramValue ) ); break;
                    case "bool":   paramList.Add( Boolean.Parse( paramValue ) ); break;
                    default:
                        throw new ActionException( "Invalid or unsupported action parameter type " + paramType );
                }
                cacheKey += "," + paramValue;
            }
            return paramList.ToArray();
        }

        private static ListAnchor LoadListAnchor( XmlNode node )
        {
        	XmlAttribute attr = node.Attributes ["anchor"];
            if ( attr == null )
            {
            	return new ListAnchor( AnchorType.Last );
            }

            string anchor = attr.Value;
            string anchorRef = null;
            int pos = anchor.IndexOf( ':' );
            if ( pos >= 0 )
            {
            	anchorRef = anchor.Substring( pos+1 );
                anchor = anchor.Substring( 0, pos );
            }

            AnchorType type;
            switch( anchor )
            {
            	case "first":  type = AnchorType.First;  break;
                case "last":   type = AnchorType.Last;   break;
                case "before": type = AnchorType.Before; break;
                case "after":  type = AnchorType.After;  break;
                default:
                    throw new ActionException( "Invalid anchor type " + anchor );
            }

            return new ListAnchor( type, anchorRef );
        }

        private enum TabFilterMode { None, TabOnly, TabOrAll };

        /**
         * Loads an array of action state filters from the specified XML node.
         */

        private IActionStateFilter[] LoadFiltersFromXml( Assembly pluginAssembly, XmlNode node, string ns,
                                                         TabFilterMode tabFilterMode )
        {
            ArrayList filters = null;
            if ( tabFilterMode != TabFilterMode.None )
            {
                XmlAttribute attrTab = node.Attributes ["tab"];
                if ( attrTab != null )
                {
                    filters = new ArrayList();
                    filters.Add( new ActiveTabFilter( attrTab.Value, (tabFilterMode == TabFilterMode.TabOrAll) ) );
                }
            }

            foreach( XmlNode filterNode in node.SelectNodes( "filter" ) )
            {
                Type filterType = FindActionType( pluginAssembly, filterNode, ns );

                object[] filterParams = null;
                XmlNodeList paramNodes = filterNode.SelectNodes( "param" );
                if ( paramNodes.Count > 0 )
                {
                    string cacheKey = "";
                    filterParams = ParseActionParameters( paramNodes, ref cacheKey );
                }

                IActionStateFilter filter;
                if ( filterParams != null )
                {
                    filter = (IActionStateFilter) Activator.CreateInstance( filterType, filterParams );
                }
                else
                {
                    filter = (IActionStateFilter) Activator.CreateInstance( filterType );
                }

                if ( filters == null )
                {
                    filters = new ArrayList();
                }
                filters.Add( filter );
            }

            if ( filters != null )
            {
                return (IActionStateFilter[]) filters.ToArray( typeof(IActionStateFilter) );
            }
            return null;
        }

        #endregion
    }
}
