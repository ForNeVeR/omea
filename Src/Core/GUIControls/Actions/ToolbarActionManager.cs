// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Containers;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Establishes a binding between toolbar controls and corresponding actions by
	/// executing the action appropriate upon a button click and quering it for the
	/// button state periodically.
	/// </summary>
	/// <remarks>
	/// <para>Create a toolbar action manager and supply its the toolbar instance that it will be managing.
	/// The best place to do it is after the <c>InitializeComponent<c/> call within your form constructor.</para>
	/// <para>Having created the action manager, you should supply it with the context provider which will provide information
	/// about the environment and the command processor.</para>
	/// <para>The action manager wires up to the <see cref="IUIManager.EnterIdle">application idle event</see> automatically to update the controls state in the background.
	/// Controls are also updated when mouse enters the toolbar control.</para>
	/// </remarks>
	/// <example>
	/// <para>This sample shows how to create a toolbar and initialize it with actions which will be passed to the <see cref="ICommandProcessor">command processor</see> specified by the <see cref="IActionContext">action context</see> obtained from the <see cref="IContextProvider">context provider</see>.</para>
	/// <para>We suppose that there is an instance variable of type <see cref="ToolbarActionManager"/> and that this class implements the <see cref="IContextProvider"/> interface. <c>LoadImage</c> is some custom function that loads an icon by its short name.</para>
	/// <code>
	///
	/// // Create
	/// _actionmanager = new ToolbarActionManager( this );
	/// _actionmanager.ContextProvider = this;	// IContextProvider implemented by this class
	///
	/// // General Edit actions
	/// sGroup = "Edit";
	/// _actionmanager.RegisterActionGroup( sGroup, ListAnchor.Last );
	/// _actionmanager.RegisterAction( new CommandProcessorAction( "Undo" ), sGroup, ListAnchor.Last, LoadIcon( "Undo" ), "", "Undo", null, null );
	/// _actionmanager.RegisterAction( new CommandProcessorAction( "Redo" ), sGroup, ListAnchor.Last, LoadIcon( "Redo" ), "", "Redo", null, null );
	/// </code>
	/// </example>
    public class ToolbarActionManager: IDisposable
    {
        private class ToolbarAction
        {
//            internal readonly ToolBarButton _toolbarButton;
            internal readonly ToolStripItem _toolbarButton;
            internal readonly IAction   _action;
            internal readonly string    _defaultText;
            internal readonly string    _defaultToolTip;
            internal readonly string    _resType;
            internal IActionStateFilter[] _filters;

//            public ToolbarAction( IAction action, ToolBarButton toolbarButton, string defaultText,
            public ToolbarAction( IAction action, ToolStripItem toolbarButton, string defaultText,
                                  string defaultToolTip, string resType, IActionStateFilter[] filters )
            {
                _action = action;
                _toolbarButton = toolbarButton;
                _defaultText = defaultText;
                _defaultToolTip = defaultToolTip;
                _resType = resType;
                _filters = filters;
            }

            internal void AddFilter( IActionStateFilter filter )
            {
                if ( _filters == null || _filters.Length == 0 )
                {
                    _filters = new[] { filter };
                }
                else
                {
                    IActionStateFilter[] newFilters = new IActionStateFilter[ _filters.Length + 1];
                    Array.Copy( newFilters, _filters, _filters.Length );
                    newFilters [_filters.Length] = filter;
                    _filters = newFilters;
                }
            }
        }

        private class ToolbarActionGroup
        {
            private int                 _startIndex;
            private readonly string        _id;
            private readonly AnchoredList  _actions = new AnchoredList();   // <ToolbarAction>

            public ToolbarActionGroup( string id )
            {
                _id = id;
            }

            internal string ID { get { return _id; } }
            internal int StartIndex
            {
                get { return _startIndex; }
                set
                {
                    if ( value < 0 )
                    {
                        throw new ArgumentOutOfRangeException( "value", value,
                            "Toolbar action group start index may not be negative" );
                    }
                    _startIndex = value;
                }
            }

            internal int EndIndex
            {
                get { return _startIndex + _actions.Count + 1; }
            }

//            internal ToolBarButton Separator { get; set; }
            internal ToolStripSeparator Separator { get; set; }

            internal AnchoredList Actions
            {
                get { return _actions; }
            }

            internal ToolbarAction FindActionInstance( IAction action )
            {
                foreach( ToolbarAction tbAction in _actions )
                {
                    if ( tbAction._action == action )
                    {
                        return tbAction;
                    }
                }
                return null;
            }
        }

        private const string _defaultGroupId = "<default>";

        private readonly ToolStrip   _toolBar;
        private readonly ImageList _toolbarImages;
        private IContextProvider   _contextProvider;
        private int     _lastVisibleActions;
        private bool    _toolbarVisible = true;

        private readonly AnchoredList _actionGroups = new AnchoredList();        // <ToolbarActionGroup>

		/// <summary>
		/// Initializes the instance and attaches it to the particular <see cref="ToolBar"/> control.
		/// </summary>
		/// <param name="toolBar">The toolbar to be controlled.</param>
        public ToolbarActionManager( ToolStrip toolBar )
		{
			_toolBar = toolBar;
			_toolbarImages = new ImageList();
			if( ICore.Instance != null )
			{
				Core.StateChanged += OnCoreStateChanged;
				if( (Core.State != CoreState.Initializing) && (Core.State != CoreState.ShuttingDown) )
					InitializeToolbarImages();

				Core.UIManager.EnterIdle += IdleUpdateToolbarActions;
			}
			_toolBar.MouseEnter += IdleUpdateToolbarActions;
		}

		private void OnCoreStateChanged( object sender, EventArgs e )
        {
        	switch( Core.State )
        	{
        	case CoreState.StartingPlugins:
        		InitializeToolbarImages();
        		break;
        	case CoreState.ShuttingDown:
        		if( _toolBar != null )
        			_toolBar.Visible = false;
        		break;
        	}
        }

		private void InitializeToolbarImages()
        {
            _toolbarImages.ColorDepth = Core.ResourceIconManager.IconColorDepth;
            _toolBar.ImageList = _toolbarImages;
        }

        public void Dispose()
        {
            Core.StateChanged -= OnCoreStateChanged;
            Core.UIManager.EnterIdle -= IdleUpdateToolbarActions;
            _toolbarImages.Dispose();
        }

    	public IContextProvider ContextProvider
    	{
    		get { return _contextProvider; }
    		set { _contextProvider = value; }
    	}

        public bool ToolbarVisible
        {
            get { return _toolbarVisible; }
            set
            {
                _toolbarVisible = value;
                _toolBar.Visible = _toolbarVisible && _lastVisibleActions > 0;
            }
        }

        public void RegisterActionGroup( string groupId, ListAnchor anchor )
        {
            if ( FindActionGroup( groupId ) != null )
                return;

            ToolbarActionGroup newGroup = new ToolbarActionGroup( groupId );
            _actionGroups.Add( groupId, newGroup, anchor );
            int newGroupIndex = _actionGroups.IndexOf( newGroup );
            if ( newGroupIndex == 0 )
            {
                newGroup.StartIndex = 0;
            }
            else
            {
                newGroup.StartIndex = ((ToolbarActionGroup) _actionGroups [newGroupIndex-1]).EndIndex;
            }

            ToolStripSeparator separator = new ToolStripSeparator();
            _toolBar.Items.Insert( newGroup.StartIndex, separator );
            newGroup.Separator = separator;
            AdjustGroupIndices( newGroup, 1 );
        }

        private ToolbarActionGroup FindActionGroup( string id )
        {
            foreach( ToolbarActionGroup group in _actionGroups )
            {
                if ( group.ID == id )
                    return group;
            }
            return null;
        }

        private void AdjustGroupIndices( ToolbarActionGroup group, int delta )
        {
            int groupIndex = _actionGroups.IndexOf( group );
            for( int i = groupIndex + 1; i < _actionGroups.Count; i++ )
            {
                ToolbarActionGroup nextGroup = (ToolbarActionGroup) _actionGroups [ i ];
                nextGroup.StartIndex = nextGroup.StartIndex + delta;
            }
        }

		/// <summary>
		/// Registers a toolbar action and adds a toolbar control for it, using the given parameters to specify its look&feel.
		/// </summary>
		/// <param name="action">Action that executes on button clicks via its <see cref="IAction.Execute"/> method and that is queried for the button state via <see cref="IAction.Update"/>.</param>
		/// <param name="groupId">ID of the action group to which this action is added. You should register an action group at this toolbar with the same ID using the <see cref="RegisterActionGroup"/> method before using it. Note that action groups cause separators to be added in between the buttons of different action groups.</param>
		/// <param name="anchor">Placement within the group.</param>
		/// <param name="icon">Image for the toolbar button, <c>Null</c> for no image. Note that you have to cast a <c>Null</c> value explicitly for your call to be disambiguated to either of the overloads.</param>
		/// <param name="text">Button text. Once assigned, button text will be visible on the toolbar, either to the right of the button (the default) or below it.</param>
		/// <param name="tooltip">Tooltip to be used for the control. Specify a <c>Null</c> value if you wish to use the button text for tooltip text as well.</param>
		/// <param name="resourceType">Resource type that must be selected in the action context for this action to be available.</param>
		/// <param name="filters">Action filter that determines availability of the action.</param>
        public void RegisterAction( IAction action, string groupId, ListAnchor anchor,
                                    Image icon, string text, string tooltip, string resourceType, IActionStateFilter[] filters )
        {
            int imageIndex = -1;
            if ( icon != null )
            {
                _toolbarImages.Images.Add( icon );
                imageIndex = _toolbarImages.Images.Count-1;
            }
            CreateToolbarButton( action, groupId, anchor, imageIndex, text, tooltip, resourceType, filters );
        }

		/// <summary>
		/// Registers a toolbar action and adds a toolbar control for it, using the given parameters to specify its look&feel.
		/// </summary>
		/// <param name="action">Action that executes on button clicks via its <see cref="IAction.Execute"/> method and that is queried for the button state via <see cref="IAction.Update"/>.</param>
		/// <param name="groupId">ID of the action group to which this action is added. You should register an action group at this toolbar with the same ID using the <see cref="RegisterActionGroup"/> method before using it. Note that action groups cause separators to be added in between the buttons of different action groups.</param>
		/// <param name="anchor">Placement within the group.</param>
		/// <param name="icon">Icon for the toolbar button, <c>Null</c> for no icon. Note that you have to cast a <c>Null</c> value explicitly for your call to be disambiguated to either of the overloads.</param>
		/// <param name="text">Button text. Once assigned, button text will be visible on the toolbar, either to the right of the button (the default) or below it.</param>
		/// <param name="tooltip">Tooltip to be used for the control. Specify a <c>Null</c> value if you wish to use the button text for tooltip text as well.</param>
		/// <param name="resourceType">Resource type that must be selected in the action context for this action to be available.</param>
		/// <param name="filters">Action filter that determines availability of the action.</param>
        public void RegisterAction( IAction action, string groupId, ListAnchor anchor,
                                    Icon icon, string text, string tooltip, string resourceType, IActionStateFilter[] filters )
        {
            int imageIndex = -1;
            if ( icon != null )
            {
                _toolbarImages.Images.Add( icon );
                imageIndex = _toolbarImages.Images.Count-1;
            }
            CreateToolbarButton( action, groupId, anchor, imageIndex, text, tooltip, resourceType, filters );
        }

        private void CreateToolbarButton( IAction action, string groupId, ListAnchor anchor,
                                          int imageIndex, string text, string toolTip, string resourceType,
                                          IActionStateFilter[] filters )
        {
            if ( groupId == null )
            {
                RegisterActionGroup( _defaultGroupId, ListAnchor.First );
                groupId = _defaultGroupId;
            }

            ToolbarActionGroup group = FindActionGroup( groupId );
            if ( group == null )
                throw new ArgumentException( "\"" + groupId + "\" is not a registered toolbar action group.", "groupId" );

            ToolStripButton button = new ToolStripButton();
            button.Click += OnToolbarButtonClick;

			// Set the tooltip
			if(text == null)
				text = "";
			if(toolTip == null)	// Use button text for a tooltip by default
				toolTip = text;
            string kbdShortcut = Core.ActionManager.GetKeyboardShortcut( action );
            if ( kbdShortcut.Length > 0 )
            {
                button.ToolTipText = toolTip + " (" + kbdShortcut + ")";
            }
            else
            {
                button.ToolTipText = toolTip;
            }

            button.Tag = action;
            button.ImageIndex = imageIndex;
            if ( text.Length > 0 )
            {
                button.Text = text;
            }
            ToolbarAction tbAction = new ToolbarAction( action, button, text, toolTip, resourceType, filters );
            int index = group.Actions.Add( action.ToString(), tbAction, anchor );
            if ( index < 0 )
            {
                throw new ActionException( String.Format("Attempt to register a duplicate toolbar action \"{0}\" in group \"{1}\" as \"{2}\", which conflicts with action \"{3}\" in the same group.", text, groupId, action.ToString(), ((ToolbarAction)group.Actions.FindByKey( action.ToString()) )._defaultText) );
            }
            _toolBar.Items.Insert( group.StartIndex + index, button );
            AdjustGroupIndices( group, 1 );
        }

        public void RegisterActionFilter( string actionId, IActionStateFilter filter )
        {
            foreach( ToolbarActionGroup group in _actionGroups )
            {
                ToolbarAction action = (ToolbarAction) group.Actions.FindByKey( actionId );
                if ( action != null )
                {
                    action.AddFilter( filter );
                    break;
                }
            }
        }

        public void UnregisterAction( IAction action )
        {
            foreach( ToolbarActionGroup group in _actionGroups )
            {
                ToolbarAction tbAction = group.FindActionInstance( action );
                if ( tbAction != null )
                {
                    _toolBar.Items.Remove( tbAction._toolbarButton );
                    group.Actions.Remove( tbAction );
                    AdjustGroupIndices( group, -1 );
                    break;
                }
            }
        }

        private void OnToolbarButtonClick( object sender, EventArgs e )
        {
            IAction action = ( IAction ) ((ToolStripButton)sender).Tag;
            if ( action != null )
            {
                action.Execute( GetContext() );
            }
        }

        /**
         * Checks if any of the toolbar actions need to be updated or hidden.
         */

        private void IdleUpdateToolbarActions( object sender, EventArgs e )
        {
            //  Both conditions make it unnecessary to perform additional
            //  calls to ResourceStore or perform any actions at all.
            //  Comment: second condition is especially useful when working
            //           over a slow remote connection.
            if ( Core.ResourceStore == null || Core.State != CoreState.Running )
                return;

            try
            {
                UpdateToolbarActions();
            }
            catch( Exception ex )
            {
                Core.ReportBackgroundException( ex );
            }
        }

        public void UpdateToolbarActions()
        {
            IActionContext context = GetContext();
            IResourceList selectedResources = context.SelectedResources;
            if ( selectedResources == null )
            {
                return;
            }
            string[] resTypes = context.SelectedResources.GetAllTypes();
            _lastVisibleActions = 0;
            foreach( ToolbarActionGroup group in _actionGroups )
            {
                _lastVisibleActions += UpdateGroupButtons( group, context, resTypes );
            }
        }

        private int UpdateGroupButtons( ToolbarActionGroup group, IActionContext context, string[] resTypes )
        {
            int groupIndex = _actionGroups.IndexOf( group );
            int visibleActionsCount = 0;
            ActionPresentation presentation = new ActionPresentation();
            foreach( ToolbarAction tbAction in group.Actions )
            {
                presentation.Reset();
                presentation.Text = tbAction._defaultText;
                presentation.ToolTip = tbAction._defaultToolTip;
                ToolStripButton btn = (ToolStripButton)tbAction._toolbarButton;

                if ( tbAction._resType != null )
                {
                    if ( resTypes.Length != 1 || String.Compare( resTypes [0], tbAction._resType, true ) != 0 )
                    {
                        presentation.Visible = true;
                        presentation.Enabled = false;
                    }
                }

                IAction action = tbAction._action;
                if ( action == null )
                    continue;

                if ( tbAction._filters != null )
                {
                    foreach( IActionStateFilter filter in tbAction._filters )
                    {
                        filter.Update( context, ref presentation );
                        if ( !presentation.Visible )
                        {
                            break;
                        }
                    }
                }

                if ( presentation.Visible )
                {
                    action.Update( context, ref presentation );
                }

                try
                {
                    btn.Visible     = presentation.Visible;
                    btn.Enabled     = presentation.Enabled;
                    btn.Text        = presentation.Text;
                    btn.ToolTipText = presentation.ToolTip;
                    if ( presentation.Checked )
                    {
                        btn.CheckOnClick = true;
                    }
                    btn.Checked = presentation.Checked;
                }
                catch( SEHException )
                {
                    // ignore (#4686)
                }

                if ( presentation.Visible )
                {
                	visibleActionsCount++;
                }
            }

            try
            {
                group.Separator.Visible = (visibleActionsCount > 0 && groupIndex < _actionGroups.Count-1);
            }
            catch( SEHException )
            {
                // ignore (OM-5509)
            }

            return visibleActionsCount;
        }

        private IActionContext GetContext()
        {
        	if ( _contextProvider == null )
        		return new ActionContext( ActionContextKind.Toolbar, null, null );

            return _contextProvider.GetContext( ActionContextKind.Toolbar );
        }

        public int GetPreferredWidth()
        {
            int width = 0;
            foreach( ToolbarActionGroup group in _actionGroups )
            {
                width += group.Actions.Count * 24;
                if ( group.Actions.Count > 0 )
                    width += 4;
            }
            return width;
        }
    }
}
