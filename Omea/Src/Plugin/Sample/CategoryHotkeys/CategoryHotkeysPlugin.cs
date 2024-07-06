// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.CategoryHotkeys
{
	/// <summary>
	/// The main class of the plugin which allows to assign hotkeys to categories.
	/// </summary>
	public class CategoryHotkeysPlugin: IPlugin
	{
        private static Hashtable _assignActions = new Hashtable();   // resource ID -> IAction
        private static Hashtable _removeActions = new Hashtable();   // resource ID -> IAction

	    /// <summary>
	    /// Registers the property types used for storing hotkeys and the hotkey actions
	    /// for categories which had their hotkeys assigned during previous sessions.
	    /// </summary>
        public void Register()
	    {
            PropTypes.Register();

	        RegisterActions( PropTypes.HotkeyAssign, _assignActions, true );
            RegisterActions( PropTypes.HotkeyRemove, _removeActions, false );

            Core.ActionManager.RegisterContextMenuAction( new AssignHotkeysAction(),
                ActionGroups.ITEM_MODIFY_ACTIONS, ListAnchor.Last, "Assign Hotkeys...", "Category", null );
	    }

	    /// <summary>
	    /// Registers one type of category hotkey actions (assign or remove).
	    /// </summary>
	    /// <param name="propId">The ID of the property in which the hotkey is stored.</param>
	    /// <param name="actions">The hashtable in which the mapping between resources and actions is stored.</param>
	    /// <param name="isAssign">Whether the action assigns or removes the category.</param>
        private void RegisterActions( int propId, Hashtable actions, bool isAssign )
	    {
	        foreach( IResource category in Core.ResourceStore.FindResourcesWithProp( "Category", propId ) )
	        {
	            RegisterCategoryHotkey( category, propId, isAssign, actions );
	        }
	    }

	    public void Startup()
	    {
	    }

	    public void Shutdown()
	    {
	    }

	    public static void UpdateCategoryHotkeys( IResource category )
	    {
	        UnregisterCategoryHotkey( category, _assignActions );
            UnregisterCategoryHotkey( category, _removeActions );
            RegisterCategoryHotkey( category, PropTypes.HotkeyAssign, true, _assignActions );
            RegisterCategoryHotkey( category, PropTypes.HotkeyRemove, false, _removeActions );
	    }

        /// <summary>
        /// Registers the assign or remove hotkey for the specified category resource.
        /// </summary>
        private static void RegisterCategoryHotkey( IResource category, int propId, bool isAssign, Hashtable actions )
        {
            string hotkey = category.GetStringProp( propId );
            if ( hotkey != null )
            {
                KeysConverter converter = new KeysConverter();
                Keys key = (Keys) converter.ConvertFrom( hotkey );
                IAction action = new CategoryAction( category, isAssign );
                Core.ActionManager.RegisterKeyboardAction( action, key, null, null );
                actions [category.Id] = action;
            }
        }

        /// <summary>
        /// Unregisters the assign or remove hotkey for the specified category resource.
        /// </summary>
        private static void UnregisterCategoryHotkey( IResource category, Hashtable actions )
	    {
	        IAction action = (IAction) actions [category.Id];
	        if ( action != null )
	        {
	            Core.ActionManager.UnregisterKeyboardAction( action );
	            actions.Remove( category.Id );
	        }
	    }
	}

    /// <summary>
    /// The class holding the property types used by the plugin.
    /// </summary>
    internal class PropTypes
    {
        private static int _propHotkeyAssign;
        private static int _propHotkeyRemove;

        internal static void Register()
        {
            _propHotkeyAssign = Core.ResourceStore.PropTypes.Register( "JetBrains.CategoryHotkeysPlugin.HotkeyAssign",
                PropDataType.String, PropTypeFlags.Internal );
            _propHotkeyRemove = Core.ResourceStore.PropTypes.Register( "JetBrains.CategoryHotkeysPlugin.HotkeyRemove",
                PropDataType.String, PropTypeFlags.Internal );
        }

        public static int HotkeyAssign { get { return _propHotkeyAssign; } }
        public static int HotkeyRemove { get { return _propHotkeyRemove; } }
    }

    /// <summary>
    /// The action for assigning or removing a specific category on a set of selected resources.
    /// </summary>
    internal class CategoryAction: ActionOnResource
    {
        private IResource _category;
        private bool _isAssign;

        public CategoryAction( IResource category, bool isAssign )
        {
            _category = category;
            _isAssign = isAssign;
        }

        /// <summary>
        /// Executes the action.
        /// </summary>
        /// <param name="context"></param>
        public override void Execute( IActionContext context )
        {
            foreach( IResource res in context.SelectedResources )
            {
                if ( !Core.ResourceStore.ResourceTypes [res.Type].HasFlag( ResourceTypeFlags.Internal ) )
                {
                    if ( _isAssign )
                    {
                        Core.CategoryManager.AddResourceCategory( res, _category );
                    }
                    else
                    {
                        Core.CategoryManager.RemoveResourceCategory( res, _category );
                    }
                }
            }
        }

        /// <summary>
        /// Disables the action when the category to which is is related has been deleted.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="presentation"></param>
        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if ( _category.IsDeleted )
            {
                presentation.Enabled = false;
            }
        }
    }

    /// <summary>
    /// The action to assign the hotkey for the selected category.
    /// </summary>
    internal class AssignHotkeysAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            using( AssignHotkeysDlg dlg = new AssignHotkeysDlg() )
            {
                IResource category = context.SelectedResources [0];
                if ( dlg.ShowAssignHotkeysDialog( category ) )
                {
                    CategoryHotkeysPlugin.UpdateCategoryHotkeys( category );
                }
            }
        }
    }
}
