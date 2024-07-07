// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.Drawing;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;
using System.Collections;
using System.Windows.Forms;
using JetBrains.DataStructures;

namespace JetBrains.Omea.GUIControls
{
    public class MenuHostWrapper
    {
        private readonly object _menuParent;

        public MenuHostWrapper( ToolStripDropDown menuStrip, MethodInvoker action )
        {
            _menuParent = menuStrip;
            menuStrip.Opening += delegate { action.Invoke(); };
        }
        public MenuHostWrapper( ToolStripDropDownItem menuItem, MethodInvoker action )
        {
            _menuParent = menuItem;
            menuItem.DropDownOpening += delegate{ action.Invoke(); };
        }

        public ToolStripItemCollection Items
        {
            get {  return (_menuParent is ContextMenuStrip) ?
                            ((ContextMenuStrip)_menuParent).Items :
                            ((ToolStripMenuItem)_menuParent).DropDownItems;  }
        }
    }

    public abstract class MenuActionManager
	{
        internal class MenuAction
        {
            private readonly string  _resourceType;
            private readonly IAction _action;
            private readonly IActionStateFilter[] _filters;
            private readonly Image   _actionIcon;

            public MenuAction( string resourceType, string name, Image icon, IAction action, IActionStateFilter[] filters )
            {
                _resourceType = resourceType;
                Name         = name;
                _action       = action;
                _filters      = filters;
                _actionIcon   = icon;
            }

            internal string   ResourceType  { get { return _resourceType; } }
            internal string   Name          { get; set; }
            internal IAction  Action        { get { return _action; } }
            internal Image    MenuIcon      { get { return _actionIcon; } }
            internal IActionStateFilter[] Filters { get { return _filters; } }
        }

        internal class MenuActionGroup
        {
            private readonly string _name;
            private readonly string _submenuName;
            private readonly AnchoredList _actions = new AnchoredList();

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

            internal string Name        {  get { return _name;      } }
            internal string SubmenuName {  get { return _submenuName; }  }
            public IEnumerable Actions  {  get { return _actions;   } }

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

        protected bool                  _persistentMnemonics = true;
        protected bool                  _allowVisibleResTypeMismatched;

        private bool                    _mnemonicsAssigned;
        private readonly MenuHostWrapper _menu;
        private readonly AnchoredList   _actionGroups = new AnchoredList();    // <MenuActionGroup>
        private readonly HashSet        _usedMnemonics = new HashSet();
        private readonly HashSet        _suppressedSeparators = new HashSet();

        // MenuItem -> MenuAction
        private readonly Dictionary<ToolStripMenuItem,MenuAction> _itemToActionMap = new Dictionary<ToolStripMenuItem,MenuAction>();

        protected abstract IActionContext CurrentContext {  get;  }

        #region Ctor and Properties
        protected MenuActionManager( ToolStripDropDownItem menu )
		{
            _menu = new MenuHostWrapper( menu, UpdateMenuActions );
        }

        protected MenuActionManager( ToolStripDropDown menu )
		{
            _menu = new MenuHostWrapper( menu, UpdateMenuActions );
        }
        #endregion Ctor and Properties

        #region Register/Unregister Group and Action
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

        /// <summary>
        /// Registers an action in an action group of the menu.
        /// </summary>
        public void RegisterAction( IAction action, string groupId, ListAnchor anchor, string text,
                                    Image iconRes, string resourceType, IActionStateFilter[] filters )
        {
            MenuActionGroup group = (MenuActionGroup) _actionGroups.FindByKey( groupId );
            if ( group == null )
                throw new ArgumentException( "ContextMenuManager -- Invalid action group name " + groupId, "groupId" );

            _mnemonicsAssigned = false;
            group.Add( new MenuAction( resourceType, text, iconRes, action, filters ), anchor );
        }

        ///  If the specified action is present in one of the action groups of the manager,
        ///  removes it from there.
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

        public void SuppressGroupSeparator( string groupId1, string groupId2 )
        {
            _suppressedSeparators.Add( groupId1 + ":" + groupId2 );
            _suppressedSeparators.Add( groupId2 + ":" + groupId1 );
        }
        #endregion Register/Unregister Group and Action

        #region Menonics Assignment

        private void AssignMnemonics()
        {
            ResetUsedMnemonics();

            foreach( MenuActionGroup group in _actionGroups )
            foreach( MenuAction action in group.Actions )
            {
                 action.Name = AssignMnemonic( action.Name );
            }
        }

        private void ResetUsedMnemonics()
        {
            _usedMnemonics.Clear();

            foreach( MenuActionGroup group in _actionGroups )
            foreach( MenuAction action in group.Actions )
            {
                CheckMnemonicAssigned( action.Name );
            }
        }

        /// <summary>
        /// If an '&' char is already present in the menu item text, store it
        /// in order next mnemonics are not clashed with them.
        /// </summary>
        private bool CheckMnemonicAssigned( string text )
        {
            int  i = text.IndexOf( '&', 0, Math.Max( text.Length - 1, 0 ) );
            bool found = (i != -1) && (text[ i + 1 ] != '&');
            if( found )
                _usedMnemonics.Add( Char.ToLower( text[ i + 1 ] ) );

            return found;
        }

        /// <summary>
        /// Automatically assigns a mnemonic for a menu action.
        /// </summary>
        private string AssignMnemonic( string text )
        {
            if ( CheckMnemonicAssigned( text ) )
            {
                return text;
            }

            // try to assign mnemonics on word start characters
            for( int i = 0; i < text.Length - 1; i++ )
            {
                if ( Char.IsWhiteSpace( text, i ) && !Char.IsWhiteSpace( text, i + 1 ) )
                {
                    string res = CheckAndAssign( text, i + 1 );
                    if( res != null )
                        return res;
                }
            }

            // then, try any character
            for( int i = 0; i < text.Length; i++ )
            {
                if ( !Char.IsWhiteSpace( text, i ) )
                {
                    string res = CheckAndAssign( text, i );
                    if( res != null )
                        return res;
                }
            }

            return text;
        }

        private string CheckAndAssign( string text, int i )
        {
            char candidate = Char.ToLower( text [ i ] );
            if ( !_usedMnemonics.Contains( candidate ) )
            {
                _usedMnemonics.Add( candidate );
                return text.Substring( 0, i ) + '&' + text.Substring( i );
            }
            return null;
        }
        #endregion Menonics Assignment

        #region FillMenu

        /// <summary>
        /// Fills the menu with actions, depending on the specified context.
        /// </summary>
        public void FillMenuIfNecessary( IActionContext context )
        {
            if( _menu.Items.Count == 0 )
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

                FillMenuImpl( context );
            }
        }

        private void FillMenuImpl( IActionContext context )
        {
            bool haveDefaultAction = false;
            HashSet usedShortcuts = new HashSet();
            MenuActionGroup lastGroup = null;
            IResourceList selection = context.SelectedResources;
            ToolStripItemCollection items = _menu.Items;

            foreach( MenuActionGroup group in _actionGroups )
            {
                if( !IsSeparatorSuppressedBetweenGroups( lastGroup, group ) &&
                    IsParentContainerKeeped( items, lastGroup, group ) )
                    items.Add( new ToolStripSeparator() );

                if( !IsParentContainerKeeped( items, lastGroup, group ) )
                    items = _menu.Items;

                if( group.SubmenuName != null )
                {
                    if ( lastGroup == null || group.SubmenuName != lastGroup.SubmenuName )
                    {
                        ToolStripMenuItem subMenu = new ToolStripMenuItem( group.SubmenuName );
                        items.Add( subMenu );
                        items = subMenu.DropDownItems;
                    }
                }

                lastGroup = group;

                foreach( MenuAction action in group.Actions )
                {
                    ToolStripMenuItem item = AddActionToMenu( action, items, context, usedShortcuts );
                    if( !haveDefaultAction && selection.Count == 1 &&
                        Core.ActionManager.GetDoubleClickAction( selection[ 0 ] ) == action.Action )
                    {
                        item.Font = new Font( item.Font, FontStyle.Bold );
                        haveDefaultAction = true;
                    }
                }
            }
        }

        private bool IsSeparatorSuppressedBetweenGroups( MenuActionGroup lastGroup, MenuActionGroup group )
        {
            return (lastGroup == null) || IsSeparatorSuppressed( group, lastGroup );
        }

        private bool IsParentContainerKeeped( ToolStripItemCollection items,
                                              MenuActionGroup lastGroup, MenuActionGroup group )
        {
            return (items == _menu.Items) ||
                   (group.SubmenuName != null) && (lastGroup != null) &&
                   (group.SubmenuName.Equals( lastGroup.SubmenuName ));
        }

        private ToolStripMenuItem AddActionToMenu( MenuAction menuAction, ToolStripItemCollection curMenuItems,
                                                   IActionContext context, HashSet usedShortcuts )
        {
            ActionPresentation presentation = new ActionPresentation();

            presentation.Reset();

            ToolStripMenuItem item = new ToolStripMenuItem();
            item.Text = menuAction.Name;

            Keys? keyShortcut = Core.ActionManager.GetKeyboardShortcutEx( menuAction.Action, context );
            if ( keyShortcut != null && !usedShortcuts.Contains( keyShortcut ) )
            {
                item.ShortcutKeys = (Keys)keyShortcut;
                usedShortcuts.Add( keyShortcut );
            }

            if( menuAction.MenuIcon != null )
            {
                item.Image = menuAction.MenuIcon;
            }

            item.Click += ExecuteMenuAction;

            curMenuItems.Add( item );
            _itemToActionMap[ item ] = menuAction;
            return item;
        }
        #endregion FillMenu

        #region Update Menu

        /// <summary>
        /// Updates the visible and enabled state of actions in the menu without rebuilding
        /// the menu entirely.
        /// If there is no menu item in the menu, then we need to fill it for a first time
        /// from the registered groups and actions.
        /// </summary>
        public void UpdateMenuActions()
        {
            IActionContext context = CurrentContext;

            string[] resTypes = context.SelectedResources.GetAllTypes();
            FillMenuIfNecessary( context );
            UpdateMenuActions( context, resTypes, _menu.Items );
        }

        private bool UpdateMenuActions( IActionContext context, string[] resTypes,
                                        ToolStripItemCollection items )
        {
            bool anyVisible = false;
            bool anyItemSinceSeparator = false;
            ToolStripSeparator lastSeparator = null;
            ActionPresentation presentation = new ActionPresentation();

            if( !_persistentMnemonics )
                ResetUsedMnemonics();

            foreach( ToolStripItem item in items )
            {
                presentation.Reset();
                if( item is ToolStripMenuItem )
                {
                    if( _itemToActionMap.ContainsKey( (ToolStripMenuItem)item ) )
                    {
                        MenuAction menuAction = _itemToActionMap[ (ToolStripMenuItem)item ];
                        UpdateAction( menuAction, context, resTypes, ref presentation );
                    }
                    else
                    if( HaveGroupBySubName( item.Text ) )
                    {
                        presentation.Visible = UpdateMenuActions( context, resTypes, ((ToolStripMenuItem)item).DropDownItems );
                    }

                    anyItemSinceSeparator = (anyItemSinceSeparator || presentation.Visible);
                    anyVisible = (anyVisible || presentation.Visible);

                    SetActionFlags( item, presentation );
                }
                else // ToolStripSeparator
                {
                    item.Visible = anyItemSinceSeparator;
                    lastSeparator = anyItemSinceSeparator ? (ToolStripSeparator)item : null;
                    anyItemSinceSeparator = false;
                }
            }

            //  Remove a separator if it is the last item in the list.
            if( !anyItemSinceSeparator && (lastSeparator != null) )
                lastSeparator.Visible = false;

            return anyVisible;
        }

        private void UpdateAction( MenuAction action, IActionContext context,
                                   string[] resTypes, ref ActionPresentation presentation )
        {
            presentation.Text = action.Name;
            try
            {
                if( action.ResourceType != null && !IsActionTypeMatches( resTypes, action ) )
                {
                    presentation.Visible = _allowVisibleResTypeMismatched;
                    presentation.Enabled = false;
                    return;
                }

                if ( action.Filters != null )
                {
                    foreach( IActionStateFilter filter in action.Filters )
                    {
                        filter.Update( context, ref presentation );
                        if ( !presentation.Visible )
                            break;
                    }
                }

                if ( presentation.Visible )
                {
                    string text = presentation.Text;
                    action.Action.Update( context, ref presentation );
                    if( !_persistentMnemonics && presentation.Visible )
                        presentation.Text = AssignMnemonic( presentation.Text );

                    if( text != presentation.Text )
                        presentation.TextChanged = true;
                }
            }
            catch( Exception ex )
            {
                Core.ReportException( ex, false );
                presentation.Visible = false;
            }
        }

        private static bool IsActionTypeMatches( string[] resTypes, MenuAction action )
        {
            return resTypes.Length == 1 && resTypes [ 0 ] == action.ResourceType;
        }

        private bool HaveGroupBySubName( string name )
        {
            foreach( MenuActionGroup gr in _actionGroups )
            {
                if( gr.SubmenuName == name )  return true;
            }
            return false;
        }

        /// Sets the properties of a menu item from an ActionPresentation instance.
        private static void SetActionFlags( ToolStripItem item, ActionPresentation presentation )
        {
            item.Enabled = presentation.Enabled;
            item.Visible = presentation.Visible;
            if( item is ToolStripMenuItem )
            {
                ((ToolStripMenuItem)item).Checked = presentation.Checked;
            }

            if( presentation.TextChanged )
                item.Text = presentation.Text;
        }

        private bool IsSeparatorSuppressed( MenuActionGroup group1, MenuActionGroup group2 )
        {
            return _suppressedSeparators.Contains( group1.Name + ":" + group2.Name );
        }
        #endregion Update Menu

        #region Execute

        private void ExecuteMenuAction( object sender, EventArgs e )
		{
            MenuAction menuAction = _itemToActionMap[ (ToolStripMenuItem) sender ];
            if ( menuAction != null )
            {
                try
                {
                    menuAction.Action.Execute( CurrentContext );
                }
                catch( Exception ex )
                {
                    Core.ReportException( ex, false );
                }
            }
        }
        #endregion Execute
    }

    public class MainMenuActionManager : MenuActionManager
    {
        public MainMenuActionManager( ToolStripDropDownItem menu ) : base( menu )
		{
            _persistentMnemonics = _allowVisibleResTypeMismatched = true;
        }

        protected override IActionContext CurrentContext
        {
            get {  return Core.ActionManager.GetMainMenuActionContext();  }
        }
    }

    public class ContextMenuActionManager : MenuActionManager
    {
        protected IActionContext  _lastContext;

		public ContextMenuActionManager( ToolStripDropDown menu ) : base( menu )
		{
            _persistentMnemonics = _allowVisibleResTypeMismatched = false;
        }

        protected override IActionContext CurrentContext
        {
            get
            {
                #region Preconditions
                if( _lastContext == null )
                    throw new InvalidOperationException( "ContextMenuActionManager -- ActionContext must be set before its usage." );
                #endregion Preconditions

                return _lastContext;
            }
        }

        public IActionContext ActionContext {  set {  _lastContext = value;  }   }
    }
}
