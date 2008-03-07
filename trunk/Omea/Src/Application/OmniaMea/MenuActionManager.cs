/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;
using System.Collections;
using System.Windows.Forms;
using JetBrains.DataStructures;

namespace JetBrains.Omea
{
	/**
     * Class for managing a menu consisting of actions.
     */

    public class MenuActionManager
	{
        internal class MenuAction
        {
            private string      _resourceType;
            private string      _name;
            private IAction     _action;
            private IActionStateFilter[] _filters;

            public MenuAction( string resourceType, string name, IAction action, IActionStateFilter[] filters )
            {
                _resourceType = resourceType;
                _name         = name;
                _action       = action;
                _filters      = filters;
            }

            internal string      ResourceType { get { return _resourceType; } }
            internal string      Name         { get { return _name; } set { _name = value; } }
            internal IAction     Action       { get { return _action; } }
            internal IActionStateFilter[] Filters { get { return _filters; } }
        }

        internal class MenuActionGroup
        {
            private string _name;
            private string _submenuName;
            private AnchoredList _actions = new AnchoredList();

            public MenuActionGroup( string name, string submenuName )
            {
                _actions.AllowDuplicates = true;
                _name        = name;
                _submenuName = submenuName;
            }

            internal void Add( MenuAction action, ListAnchor anchor )
            {
                _actions.Add( action.Action.ToString(), action, anchor );
            }

            internal string Name
            {
                get { return _name; }
            }

            internal string SubmenuName
            {
                get { return _submenuName; }
            }

            public IEnumerable Actions
            {
                get { return _actions; }
            }

            public bool RemoveAction( IAction action )
            {
                foreach( MenuAction menuAction in _actions )
                {
                    if ( menuAction.Action == action )
                    {
                        _actions.Remove( menuAction );
                        return true;
                    }
                }
                return false;
            }
        }

        private Menu.MenuItemCollection _menuItems;
        private AnchoredList            _actionGroups = new AnchoredList();    // <MenuActionGroup>
        private Hashtable               _itemToActionMap = new Hashtable();    // MenuItem -> MenuAction
        private IActionContext          _lastContext;
        private bool                    _disableUnmatchingTypeActions;
        private bool                    _mnemonicsAssigned;
        private HashSet                 _usedMnemonics = new HashSet();
        private bool                    _persistentMnemonics = true;
        private HashSet                 _suppressedSeparators = new HashSet();

		public MenuActionManager( Menu.MenuItemCollection menuItems, bool disableUnmatchingTypeActions )
		{
			_menuItems = menuItems;
            _disableUnmatchingTypeActions = disableUnmatchingTypeActions;
        }

        public bool PersistentMnemonics
        {
            get { return _persistentMnemonics; }
            set { _persistentMnemonics = value; }
        }

        public void RegisterGroup( string name, string submenuName, ListAnchor anchor )
        {
            if ( _actionGroups.FindByKey( name ) == null )
            {
                _actionGroups.Add( name, new MenuActionGroup( name, submenuName ), anchor );
            }
        }

        public bool ContainsGroup( string groupName )
        {
        	return _actionGroups.FindByKey( groupName ) != null;
        }

        /**
         * Registers an action in an action group of the menu.
         */
        
        public void RegisterAction( IAction action, string groupId, ListAnchor anchor, string text,
            string resourceType, IActionStateFilter[] filters )
        {
            MenuActionGroup group = (MenuActionGroup) _actionGroups.FindByKey( groupId );
            if ( group != null )
            {
                _mnemonicsAssigned = false;
                group.Add( new MenuAction( resourceType, text, action, filters ), anchor );
            }
            else
            {
                throw new ArgumentException( "Invalid action group name " + groupId, "groupId" );
            }
        }

        private void AssignMnemonics()
        {
            ResetUsedMnemonics();

            foreach( MenuActionGroup group in _actionGroups )
            {
                foreach( MenuAction action in group.Actions )
                {
                     action.Name = AssignMnemonic( action.Name );
                }
            }
        }

        private void ResetUsedMnemonics()
        {
            _usedMnemonics.Clear();
            foreach( MenuActionGroup group in _actionGroups )
            {
                foreach( MenuAction action in group.Actions )
                {
                    CheckExistingMnemonic( action.Name );
                }
            }
        }

        /**
         * Automatically assigns a mnemonic for a menu action.
         */

        private string AssignMnemonic( string text )
        {
            if ( CheckExistingMnemonic( text ) )
            {
                return text;
            }

            // try to assign mnemonics on word start characters
            for( int i=0; i<text.Length-1; i++ )
            {
                if ( Char.IsWhiteSpace( text, i ) && !Char.IsWhiteSpace( text, i+1 ) )
                {
                    char candidate = Char.ToLower( text [i+1] );
                    if ( !_usedMnemonics.Contains( candidate ) )
                    {
                        _usedMnemonics.Add( candidate );
                        Debug.WriteLine( "Assigned word start mnemonic for " + text );
                        return text.Substring( 0, i+1 ) + '&' + text.Substring( i+1 );
                    }
                }
            }

            // then, try any character
            for( int i=0; i<text.Length; i++ )
            {
                if ( !Char.IsWhiteSpace( text, i ) )
                {
                    char candidate = Char.ToLower( text [i] );
                    if ( !_usedMnemonics.Contains( candidate ) )
                    {
                        _usedMnemonics.Add( candidate );
                        Debug.WriteLine( "Assigned regular character mnemonic for " + text );
                        return text.Substring( 0, i ) + '&' + text.Substring( i );
                    }
                }
            }

            return text;
        }

        private bool CheckExistingMnemonic( string text )
        {
            // check if the mnemonic is already assigned
            for( int i=0; i<text.Length-1; i++ )   // the last character cannot be &
            {
                if ( text [i] == '&' && text [i+1] != '&' )
                {
                    _usedMnemonics.Add( Char.ToLower( text [i+1] ) );
                    return true;
                }
            }
            return false;
        }

        /**
         * If the specified action is present in one of the action groups of the manager,
         * removes it from there.
         */
        
        public bool UnregisterAction( IAction action )
        {
            foreach( MenuActionGroup group in _actionGroups )
            {
                if ( group.RemoveAction( action ) )
                {
                    return true;
                }
            }
            return false;
        }

        public void ResetMnemonics()
        {
            _mnemonicsAssigned = false;
        }

        /**
         * Fills the menu with actions, depending on the specified context.
         */

        public void FillMenu( IActionContext context )
        {
            if ( _persistentMnemonics )
            {
                if ( !_mnemonicsAssigned )
                {
                    AssignMnemonics();
                    _mnemonicsAssigned = true;
                }
            }
            else
            {
                ResetUsedMnemonics();
            }
            _lastContext = context;

            MenuActionGroup lastGroup = null;

            MenuItem curSubmenu = null;
            Menu.MenuItemCollection curMenuItems = _menuItems;
            
            _menuItems.Clear();
            _itemToActionMap.Clear();
            HashSet usedShortcuts = new HashSet();
            int submenuVisibleActions = 0, submenuEnabledActions = 0;
            bool haveDefaultAction = false;

            string[] resTypes = context.SelectedResources.GetAllTypes();
            
            foreach( MenuActionGroup group in _actionGroups )
            {
                if ( lastGroup != null && !IsSeparatorSuppressed( group, lastGroup ) )
                {
                    _menuItems.Add( "-" );
                }
                        
                if ( group.SubmenuName != null )
                {
                    if ( lastGroup == null || group.SubmenuName != lastGroup.SubmenuName )
                    {
                        curSubmenu = _menuItems.Add( group.SubmenuName );
                        curMenuItems = curSubmenu.MenuItems;
                        submenuVisibleActions = 0;
                        submenuEnabledActions = 0;
                    }
                    else
                    {
                        curMenuItems.Add( "-" );
                    }
                }
                else
                {
                    if ( curSubmenu != null )
                    {
                        if ( submenuVisibleActions == 0 )
                        {
                            curSubmenu.Visible = false;
                        }
                        else if ( submenuEnabledActions == 0 )
                        {
                            curSubmenu.Enabled = false;
                        }
                    }
                    curMenuItems = _menuItems;
                    curSubmenu = null;
                }
                lastGroup = group;

                foreach( MenuAction action in group.Actions )
                {
                    if ( action.ResourceType != null )
                    {
                        if ( resTypes.Length != 1 || resTypes [0] != action.ResourceType )
                        {
                            if ( _disableUnmatchingTypeActions )
                            {
                                MenuItem stubItem = curMenuItems.Add( action.Name );
                                stubItem.Enabled = false;
                                submenuVisibleActions++;
                            }
                            continue;                            
                        }
                    }
                    
                    MenuItem item = AddActionToMenu( action, curMenuItems, context, usedShortcuts );
                    if ( !haveDefaultAction && context.SelectedResources.Count == 1 && 
                        Core.ActionManager.GetDoubleClickAction( context.SelectedResources [0] ) == action.Action )
                    {
                        item.DefaultItem = true;
                        haveDefaultAction = true;
                    }
                    if ( item.Visible )
                    {
                        submenuVisibleActions++;
                    }
                    if ( item.Enabled )
                    {
                        submenuEnabledActions++;                        
                    }
                }
            }

            if ( curSubmenu != null )
            {
                if ( submenuVisibleActions == 0 )
                {
                    curSubmenu.Visible = false;
                }
                else if ( submenuEnabledActions == 0 )
                {
                    curSubmenu.Enabled = false;
                }
            }

            UpdateSeparatorVisibility( _menuItems );
        }

        /**
         * Adds an item to the menu based on a MenuAction.
         */

        private MenuItem AddActionToMenu( MenuAction menuAction, Menu.MenuItemCollection curMenuItems, 
            IActionContext context, HashSet usedShortcuts )
        {
            ActionPresentation presentation = new ActionPresentation();
            presentation.Reset();
            UpdateAction( menuAction, context, ref presentation );

            MenuItem item = new MenuItem();

            string menuItemText;
            if ( !_persistentMnemonics && presentation.Visible )
            {
                menuItemText = AssignMnemonic( presentation.Text );
            }
            else
            {
                menuItemText = presentation.Text;
            }

            string keyShortcut = Core.ActionManager.GetKeyboardShortcut( menuAction.Action, context );
            if ( keyShortcut != "" && presentation.Visible && !usedShortcuts.Contains( keyShortcut ) )
            {
                item.Text = menuItemText + "\t" + keyShortcut;
                usedShortcuts.Add( keyShortcut );
            }
            else
            {
                item.Text = menuItemText;
            }

            item.Click += new EventHandler( ExecuteMenuAction );
            SetActionFlags( item, ref presentation );
            curMenuItems.Add( item );
            _itemToActionMap [item] = menuAction;
            return item;
        }

        private void UpdateAction( MenuAction menuAction, IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Text = menuAction.Name;
            try
            {
                if ( menuAction.Filters != null )
                {
                    foreach( IActionStateFilter filter in menuAction.Filters )
                    {
                        filter.Update( context, ref presentation );
                        if ( !presentation.Visible )
                            break;
                    }
                }
                if ( presentation.Visible )
                {
                    menuAction.Action.Update( context, ref presentation );
                }
            }
            catch( Exception ex )
            {
                ICore.Instance.ReportException( ex, false );
                presentation.Visible = false;
            }
        }

		private void ExecuteMenuAction( object sender, EventArgs e )
		{
            MenuAction menuAction = (MenuAction) _itemToActionMap [sender];
            if ( menuAction != null )
            {
                try
                {                                                      
					Core.UIManager.WriteToUsageLog( "[Action] *Menu* [" + menuAction.Action.ToString() + "] for [?]" );
					menuAction.Action.Execute( _lastContext );
                }
                catch( Exception ex )
                {
                    ICore.Instance.ReportException( ex, false );
                }
            }
        }

        /**
         * Sets the properties of a menu item from an ActionPresentation instance.
         */

        private void SetActionFlags( MenuItem item, ref ActionPresentation presentation )
        {
            item.Enabled = presentation.Enabled;
            item.Visible = presentation.Visible;
            item.Checked = presentation.Checked;
        }

        /// <summary>
        /// Hides duplicate separators or separators at edges of the visible area.
        /// </summary>
        private bool UpdateSeparatorVisibility( Menu.MenuItemCollection items )
        {
            if ( items.Count == 0 )
            {
                return true;
            }

            bool hadVisible = false;
            MenuItem lastSeparator = null;
            foreach( MenuItem item in items )
            {
                if ( item.Text == "-" )
                {
                    if ( !hadVisible )
                        item.Visible = false;
                    else
                    {
                        item.Visible = true;
                        lastSeparator = item;
                        hadVisible = false;
                    }
                }
                else
                {
                    if ( !UpdateSeparatorVisibility( item.MenuItems ) )
                    {
                        item.Visible = false;
                    }
                    if ( item.Visible )
                        hadVisible = true;
                }
            }
            if ( lastSeparator != null && !hadVisible )
            {
                lastSeparator.Visible = false;
            }
            return hadVisible;
        }

        /**
         * Updates the visible and enabled state of actions in the menu without rebuilding
         * the menu entirely.
         */
        
        public void UpdateMenuActions( IActionContext context )
        {
            if ( context == null )
                throw new ArgumentNullException( "context" );
            
            bool visibleChanged = false;
            ActionPresentation presentation = new ActionPresentation();

            foreach( DictionaryEntry de in _itemToActionMap )
            {
                presentation.Reset();
                MenuItem item = (MenuItem) de.Key;
                MenuAction menuAction = (MenuAction) de.Value;
                UpdateAction( menuAction, context, ref presentation );

                bool wasVisible = item.Visible;
                SetActionFlags( item, ref presentation );
                if ( wasVisible != item.Visible )
                {
                    visibleChanged = true;
                }
            }

            if ( visibleChanged )
            {
                UpdateSeparatorVisibility( _menuItems );
            }
        }

        public void SuppressGroupSeparator( string groupId1, string groupId2 )
        {
            _suppressedSeparators.Add( groupId1 + ":" + groupId2 );
            _suppressedSeparators.Add( groupId2 + ":" + groupId1 );
        }

        private bool IsSeparatorSuppressed( MenuActionGroup group1, MenuActionGroup group2 )
        {
            return _suppressedSeparators.Contains( group1.Name + ":" + group2.Name );
        }
    }
}
