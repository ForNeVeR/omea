// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Specifies possible places from which an action could be invoked.
    /// </summary>
    public enum ActionContextKind
    {
    	/// <summary>
    	/// The action is invoked from the main menu.
    	/// </summary>
        MainMenu,

        /// <summary>
        /// The action is invoked from the context menu.
        /// </summary>
        ContextMenu,

        /// <summary>
        /// The action is invoked from the main toolbar, the URL bar or the toolbar in a resource
        /// tree pane.
        /// </summary>
        Toolbar,

        /// <summary>
        /// The action is invoked from a keyboard shortcut.
        /// </summary>
        Keyboard,

        /// <summary>
        /// The action is invoked from a link in the Actions group of the links pane.
        /// </summary>
        LinksPane,

        /// <summary>
        /// The action is invoked from a different, unspecified location.
        /// </summary>
        Other=255
    }

    /// <summary>
    /// Provides the information about the context where the action is invoked and
    /// the resources to which it can be applied.
    /// </summary>
    public interface IActionContext
    {
        /// <summary>
        /// Gets the type of control which invoked the action (context menu, toolbar etc.)
        /// </summary>
        ActionContextKind Kind               { get; }

        /// <summary>
        /// Gets the specific instance of the control which invoked the action. The exact
        /// value of this property depends on the control which invoked the action.
        /// </summary>
        object            Instance           { get; }

        /// <summary>
        /// Gets the list of resources to which the action should be applied.
        /// </summary>
        /// <remarks>The value of the property is never null, but can be an empty
        /// resource list.</remarks>
        IResourceList     SelectedResources  { get; }

        /// <summary>
        /// Gets the expanded list of resources to which the action should be applied.
        /// <remarks>If the selection contained collapsed threads, the list returned by
        /// this property contains all resources in the collapsed threads, even if only
        /// the top-level resource was actually selected. If the selection did not contain
        /// collapsed threads, returns the same list as <see cref="SelectedResources"/>.</remarks>
        /// </summary>
        IResourceList     SelectedResourcesExpanded { get; }

        /// <summary>
        /// If the action was invoked from the resource browser - the resource owning
        /// the currently displayed resource list.
        /// </summary>
        /// <remarks>The owner resource is set by the plugin which displayed the resource
        /// list. Generally, it is the resource which is currently selected in the left sidebar
        /// (for example, a newsgroup resource if the list of newsgroup articles is currently
        /// displayed).</remarks>
        IResource         ListOwnerResource  { get; }

        /// <summary>
        /// If the action was invoked by right-clicking a resource link label - the ID of the
        /// link property displayed by the link label. Otherwise, -1.
        /// </summary>
        int               LinkPropId         { get; }

        /// <summary>
        /// If the action was invoked by right-clicking a resource link label - the resource on
        /// the other end of the clicked link. Otherwise, null.
        /// </summary>
        /// <remarks>If the link label in the resource links pane is clicked,
        /// <see cref="SelectedResources"/> holds the resource the link to which was clicked,
        /// and <code>LinkTargetResource</code> holds the resource currently displayed
        /// in the resource browser.</remarks>
        IResource         LinkTargetResource { get; }

        /// <summary>
        /// If a Web page is currently displayed in Omea - the URL of that Web page.
        /// Otherwise, null.
        /// </summary>
        string            CurrentUrl         { get; }

        /// <summary>
        /// If a Web page is currently displayed in Omea - the title of that Web page.
        /// Otherwise, null.
        /// </summary>
        /// <since>2.0</since>
        string            CurrentPageTitle   { get; }

        /// <summary>
        /// The command processor which can execute the local (context-dependent) commands
        /// for the current context.
        /// </summary>
        /// <remarks>A partial list of the commands which can be executed through the
        /// <code>CommandProcessor</code> is defined by the <see cref="DisplayPaneCommands"/>
        /// class.</remarks>
        ICommandProcessor CommandProcessor   { get; }

        /// <summary>
        /// The form in which the action was invoked.
        /// </summary>
        /// <since>1.0.3</since>
        Form OwnerForm { get; }

        /// <summary>
        /// The text currently selected in the active display pane (in plain-text format), or
        /// null if the action was not invoked from a display pane.
        /// </summary>
        string            SelectedPlainText  { get; }

        /// <summary>
        /// The format of the text currently selected in the active display pane. Defined only
        /// if <see cref="SelectedText"/> is not null.
        /// </summary>
        TextFormat        SelectedTextFormat { get; }

        /// <summary>
        /// The text currently selected in the active display pane (in plain-text, HTML or
        /// rich-text format, as specified by <see cref="SelectedTextFormat"/>, or null if
        /// the action was not invoked from a display pane.
        /// </summary>
        string            SelectedText       { get; }
    }

    /// <summary>
    /// The default implementation of the <see cref="IActionContext"/> interface.
    /// </summary>
    public class ActionContext: IActionContext
    {
    	private ActionContextKind _kind;
        private object _instance;
        private IResourceList _selectedResources;
        private IResourceList _selectedResourcesExpanded;
        private IResource _listOwnerResource;
        private string _currentURL;
        private string _currentPageTitle;
        private ICommandProcessor _commandProcessor;
        private string _selectedText;
        private string _selectedPlainText;
        private TextFormat _selectedTextFormat;
        private int _linkPropId = -1;
        private IResource _linkTargetResource;
        private Form _ownerForm;

        private class DummyCommandProcessor: ICommandProcessor
        {
            public bool CanExecuteCommand( string action )
            {
                return false;
            }

            public void ExecuteCommand( string action )
            {
            }
        }

        private static DummyCommandProcessor _dummyCommandProcessor = new DummyCommandProcessor();

    	/// <summary>
    	/// Initializes the action context with an unspecified context kind and the specified
    	/// list of selected resources.
    	/// </summary>
    	/// <param name="selectedResources">The list of selected resources in the action context,
    	/// or null if there are no selected resources.</param>
        public ActionContext( IResourceList selectedResources )
    	{
            _kind = ActionContextKind.Other;
    		_selectedResources = selectedResources;
    	}

    	/// <summary>
    	/// Initializes the action context with the specified kind, invoker instance and list of
    	/// selected resources.
    	/// </summary>
    	/// <param name="kind">The kind of the action context.</param>
    	/// <param name="instance">The instance of the control invoking the action, or null if
    	/// not required.</param>
        /// <param name="selectedResources">The list of selected resources in the action context,
        /// or null if there are no selected resources.</param>
        public ActionContext( ActionContextKind kind, object instance, IResourceList selectedResources )
    	{
    		_kind = kind;
    		_instance = instance;
            _selectedResources = selectedResources;
    	}

        /// <summary>
        /// Creates a copy of the specified action context and replaces the selected resources
        /// list with the specified list.
        /// </summary>
        /// <param name="other">The context to copy.</param>
        /// <param name="selectedResources">The resource list to use as selected resources.</param>
        /// <since>1.0.3</since>
        public ActionContext( IActionContext other, IResourceList selectedResources )
        {
    	    _kind               = other.Kind;
            _instance           = other.Instance;
            _selectedResources  = selectedResources;
            _listOwnerResource  = other.ListOwnerResource;
            _currentURL         = other.CurrentUrl;
            _currentPageTitle   = other.CurrentPageTitle;
            _commandProcessor   = other.CommandProcessor;
            _selectedText       = other.SelectedText;
            _selectedPlainText  = other.SelectedPlainText;
            _selectedTextFormat = other.SelectedTextFormat;
            _linkPropId         = other.LinkPropId;
            _linkTargetResource = other.LinkTargetResource;
            _ownerForm          = other.OwnerForm;
        }

        /// <summary>
        /// Gets the type of control which invoked the action (context menu, toolbar etc.)
        /// </summary>
        public ActionContextKind Kind
    	{
    		get { return _kind; }
    	}

        /// <summary>
        /// Gets the specific instance of the control which invoked the action. The exact
        /// value of this property depends on the control which invoked the action.
        /// </summary>
        public object Instance
    	{
    		get { return _instance; }
    	}

        /// <summary>
        /// Gets the list of resources to which the action should be applied.
        /// </summary>
        /// <remarks>The value of the property is never null, but can be an empty
        /// resource list.</remarks>
        public IResourceList SelectedResources
        {
        	get
            {
                if ( _selectedResources == null ||
                    ( _selectedResources.Count == 1 && _selectedResources.ResourceIds [0] == -1 ) )
                {
                	return Core.ResourceStore.EmptyResourceList;
                }
                return _selectedResources;
            }
        }

        /// <summary>
        /// Gets the expanded list of resources to which the action should be applied.
        /// <remarks>If the selection contained collapsed threads, the list returned by
        /// this property contains all resources in the collapsed threads, even if only
        /// the top-level resource was actually selected. If the selection did not contain
        /// collapsed threads, returns the same list as <see cref="SelectedResources"/>.</remarks>
        /// </summary>
        public IResourceList SelectedResourcesExpanded
        {
            get
            {
                if ( _selectedResourcesExpanded != null )
                {
                    return _selectedResourcesExpanded;
                }
                return SelectedResources;
            }
        }

        /// <summary>
        /// Sets the list of selected resources in the action context to the specified value.
        /// </summary>
        /// <param name="selectedResources">The list of selected resources, or null if there
        /// are no selected resources.</param>
        public void SetSelectedResources( IResourceList selectedResources )
        {
            _selectedResources = selectedResources;
        }

        /// <summary>
        /// Sets the expanded list of selected resources in the action context to the specified
        /// value.
        /// </summary>
        /// <param name="selectedResourcesExpanded">The expanded list of selected resources, or null
        /// if there are no selected resources.</param>
        public void SetSelectedResourcesExpanded( IResourceList selectedResourcesExpanded )
        {
            _selectedResourcesExpanded = selectedResourcesExpanded;
        }

        /// <summary>
        /// If the action was invoked from the resource browser - the resource owning
        /// the currently displayed resource list.
        /// </summary>
        /// <remarks>The owner resource is set by the plugin which displayed the resource
        /// list. Generally, it is the resource which is currently selected in the left sidebar
        /// (for example, a newsgroup resource if the list of newsgroup articles is currently
        /// displayed).</remarks>
        public IResource ListOwnerResource
        {
            get { return _listOwnerResource; }
        }

        /// <summary>
        /// Sets the resource owning the visible resource list.
        /// </summary>
        /// <param name="res">The resource owning the visible resource list, or null if
        /// there is none.</param>
        /// <remarks><seealso cref="IActionContext.ListOwnerResource"/></remarks>
        public void SetListOwner( IResource res )
        {
            _listOwnerResource = res;
        }

        /// <summary>
        /// If the action was invoked by right-clicking a resource link label - the ID of the
        /// link property displayed by the link label. Otherwise, -1.
        /// </summary>
        public int LinkPropId
        {
            get { return _linkPropId; }
        }

        /// <summary>
        /// If the action was invoked by right-clicking a resource link label - the resource on
        /// the other end of the clicked link. Otherwise, null.
        /// </summary>
        /// <remarks>If the link label in the resource links pane is clicked,
        /// <see cref="SelectedResources"/> holds the resource the link to which was clicked,
        /// and <code>LinkTargetResource</code> holds the resource currently displayed
        /// in the resource browser.</remarks>
        public IResource LinkTargetResource
        {
            get { return _linkTargetResource; }
        }

        /// <summary>
        /// Sets the ID of the link property and the target resource for actions which are
        /// invoked by right-clicking a resource link label.
        /// </summary>
        /// <param name="linkPropId">The ID of the link property.</param>
        /// <param name="linkTargetResource">The target of the link.</param>
        public void SetLinkTarget( int linkPropId, IResource linkTargetResource )
        {
            _linkPropId = linkPropId;
            _linkTargetResource = linkTargetResource;
        }

        /// <summary>
        /// If a Web page is currently displayed in Omea - the URL of that Web page.
        /// Otherwise, null.
        /// </summary>
        public string CurrentUrl
    	{
    		get { return _currentURL; }
    	}

        /// <summary>
        /// If a Web page is currently displayed in Omea - the title of that Web page.
        /// Otherwise, null.
        /// </summary>
        /// <since>2.0</since>
        public string CurrentPageTitle
        {
            get { return _currentPageTitle; }
        }

        /// <summary>
        /// Sets the URL of the Web page currently displayed in Omea.
        /// </summary>
        /// <param name="url">Web page URL, or null if not applicable.</param>
        public void SetCurrentUrl( string url )
        {
        	_currentURL = url;
        }

        /// <summary>
        /// Sets the title of the Web page currently displayed in Omea.
        /// </summary>
        /// <param name="title">Web page title, or null if not applicable.</param>
        /// <since>2.0</since>
        public void SetCurrentPageTitle( string title )
        {
            _currentPageTitle = title;
        }

        /// <summary>
        /// The command processor which can execute the local (context-dependent) commands
        /// for the current context.
        /// </summary>
        /// <remarks>A partial list of the commands which can be executed through the
        /// <code>CommandProcessor</code> is defined by the <see cref="DisplayPaneCommands"/>
        /// class.</remarks>
        public ICommandProcessor CommandProcessor
    	{
    		get
            {
                if ( _commandProcessor == null )
                    return _dummyCommandProcessor;

                return _commandProcessor;
            }
    	}

        /// <summary>
        /// Sets the command processor which can execute the local (context-dependent) commands
        /// for the current context.
        /// </summary>
        /// <param name="commandProcessor">The command processor implementation, or null if
        /// no command processing is required.</param>
        public void SetCommandProcessor( ICommandProcessor commandProcessor )
        {
        	_commandProcessor = commandProcessor;
        }

        /// <summary>
        /// The text currently selected in the active display pane (in plain-text, HTML or
        /// rich-text format, as specified by <see cref="SelectedTextFormat"/>, or null if
        /// the action was not invoked from a display pane.
        /// </summary>
        public string SelectedText
        {
            get { return _selectedText; }
        }

        /// <summary>
        /// The text currently selected in the active display pane (in plain-text format), or
        /// null if the action was not invoked from a display pane.
        /// </summary>
        public string SelectedPlainText
        {
            get { return _selectedPlainText; }
        }

        /// <summary>
        /// The format of the text currently selected in the active display pane. Defined only
        /// if <see cref="SelectedText"/> is not null.
        /// </summary>
        public TextFormat SelectedTextFormat
        {
            get { return _selectedTextFormat; }
        }

        /// <summary>
        /// Sets the selected text and its format.
        /// </summary>
        /// <param name="selectedText">The selected text in a plain-text, RTF or HTML format.</param>
        /// <param name="selectedPlainText">The selected text in plain-text format.</param>
        /// <param name="format">The format of text in <c>selectedText</c>.</param>
        public void SetSelectedText( string selectedText, string selectedPlainText, TextFormat format )
        {
            _selectedText = selectedText;
            _selectedPlainText = selectedPlainText;
            _selectedTextFormat = format;
        }

        /// <summary>
        /// The form in which the action was invoked.
        /// </summary>
        /// <since>1.0.3</since>
        public Form OwnerForm
        {
            get { return _ownerForm; }
        }

        /// <summary>
        /// Sets the form in which the action was invoked.
        /// </summary>
        /// <param name="form">The form.</param>
        /// <since>1.0.3</since>
        public void SetOwnerForm( Form form )
        {
            _ownerForm = form;
        }
    }

    /// <summary>
    /// The structure which holds the information about the state of the control
    /// representing an action in the UI. Instances of this structure are passed
    /// to the Update() method of actions and action state filters.
    /// </summary>
    public struct ActionPresentation
    {
    	private bool _visible;
        private bool _enabled;
        private bool _checked;
        private bool _textChanged;
        private string _text;
        private string _toolTip;

        /// <summary>
        /// Changes the presentation to a default (unmodified) state.
        /// </summary>
        public void Reset()
        {
            ResetState();
            _text = null;
            _toolTip = null;
        }

        /// <summary>
        /// Resets only the state flags of the presentation to the default value,
        /// while leaving Text and ToolTip unmodified.
        /// </summary>
        public void ResetState()
        {
            _visible = true;
            _enabled = true;
            _checked = false;
            _textChanged = false;
        }

     	/// <summary>
     	/// Gets or sets a value indicating whether the control representing the action
     	/// is displayed.
     	/// </summary>
        public bool Visible
    	{
    		get { return _visible; }
    		set { _visible = value; }
    	}

    	/// <summary>
        /// Gets or sets a value indicating whether the control representing the action
        /// is enabled.
    	/// </summary>
        public bool Enabled
    	{
    		get { return _enabled; }
    		set { _enabled = value; }
    	}

    	/// <summary>
        /// Gets or sets a value indicating whether the control representing the action
        /// has a check mark displayed next to it. (This is used only if the action is
        /// represented by a menu item).
    	/// </summary>
        public bool Checked
    	{
    		get { return _checked; }
    		set { _checked = value; }
    	}

        /// <summary>
        /// Gets or sets the text of the control representing the action in the UI.
        /// </summary>
        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }

        public bool TextChanged
        {
            get { return _textChanged; }
            set { _textChanged = value; }
        }

        /// <summary>
        /// Gets or sets the tooltip of the control representing the action in the UI.
        /// (This is used only if the action is represented by a toolbar button.)
        /// </summary>
        public string ToolTip
        {
            get { return _toolTip; }
            set { _toolTip = value; }
        }
    }

    /// <summary>
    /// A class which controls the enabled state of an action. When an action is registered,
    /// multiple filters can be registered together with it.
    /// </summary>
    public interface IActionStateFilter
    {
        /// <summary>
        /// For the specified context, updates the presentation state of an action.
        /// </summary>
        /// <param name="context">
        ///   Context, containing information about resources to which the action will be applied.
        /// </param>
        /// <param name="presentation">
        ///   The state of the UI element which presents the action to the user. For the
        ///   first filter in the chain, the presentation is initialized with the default
        ///   values. For subsequent filters, it contains the data set by previous filters.
        /// </param>
        void Update( IActionContext context, ref ActionPresentation presentation );
    }

    /// <summary>
    /// An action is a function which can be invoked by the user through one of the user
    /// interface controls - menu items, toolbar buttons, keyboard shortcuts and so on.
    /// </summary>
    public interface IAction
    {
        /// <summary>Executes the action.</summary>
        /// <param name="context">
        /// The context for executing the action. Describes the objects to which the action
        /// is applied, like the list of selected resources.
        /// </param>
        void Execute( IActionContext context );
        /// <summary>For the specified context, updates the presentation state of an action.</summary>
        /// <param name="context">
        /// The context for executing the action. Describes the objects to which the action
        /// is applied, like the list of selected resources.
        /// </param>
        /// <param name="presentation">
        /// The state of the UI element which presents the action to the user. If filters are
        /// registered for the action, this presentation state is already updated by the filters.
        /// If the action was hidden by the filters, the Update() method of the action is not
        /// called.
        /// </param>
        void Update( IActionContext context, ref ActionPresentation presentation );
    }

    /// <summary>
    /// An action which is always enabled and visible.
    /// </summary>
    public abstract class SimpleAction: IAction
    {
        public abstract void Execute( IActionContext context );

        public virtual void Update( IActionContext context, ref ActionPresentation presentation )
        {
        }
    }

    /// <summary>
    /// An action which is visible only when there is at least one resource selected.
    /// </summary>
    public abstract class ActionOnResource: IAction
    {
        public abstract void Execute( IActionContext context );
        public virtual void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources == null || context.SelectedResources.Count == 0 )
            {
                presentation.Visible = false;
            }
        }
    }

    /// <summary>
    /// An action which is visible when exactly one resource is selected.
    /// </summary>
    public abstract class ActionOnSingleResource: IAction
    {
        public abstract void Execute( IActionContext context );
        public virtual void Update( IActionContext context, ref ActionPresentation presentation )
        {
            presentation.Visible = (context.SelectedResources.Count == 1);
        }
    }

    /// <summary>
    /// An action which is executed in the resource thread.
    /// </summary>
    public abstract class ResourceAction: IAction
    {
        private IResourceList _selectedResources;
        private bool          _asynchronous;

        /// <summary>
        /// Creates an instance of <see cref="ResourceAction"/> which is executed asynchronously.
        /// </summary>
        protected ResourceAction()
        {
            _asynchronous = true;
        }

        /// <summary>
        /// Creates an instance of <see cref="ResourceAction"/> with the specified execution mode.
        /// </summary>
        /// <param name="asynchronous">true if the action is executed asynchronously, false otherwise.</param>
        protected ResourceAction( bool asynchronous )
        {
            _asynchronous = asynchronous;
        }

        void IAction.Execute( IActionContext context )
        {
            _selectedResources = context.SelectedResources;
            if ( _asynchronous )
            {
                ICore.Instance.ResourceAP.QueueJob( new MethodInvoker( ExecuteOperation ) );
            }
            else
            {
                ICore.Instance.ResourceAP.RunJob( new MethodInvoker( ExecuteOperation ) );
            }
        }

        public virtual void Update( IActionContext context, ref ActionPresentation presentation )
        {
        }

        public abstract void Execute( IResourceList selectedResources );

        private void ExecuteOperation()
        {
            Execute( _selectedResources );
        }
    }

    /// <summary>
    /// Specifies the mode of positioning an item relatively to another item in a list.
    /// </summary>
    public enum AnchorType
    {
        /// <summary>
        /// The item is placed in the beginning of the list.
        /// </summary>
        First,

        /// <summary>
        /// The item is placed in the end of the list.
        /// </summary>
        Last,

        /// <summary>
        /// The item is placed before the specified item in the list.
        /// </summary>
        Before,

        /// <summary>
        /// The item is placed after the specified item in the list.
        /// </summary>
        After
    };

    /// <summary>
    /// Specifies the position of an item relative to other items in a list.
    /// </summary>
    public class ListAnchor
    {
    	private string _refId;
        private AnchorType _anchorType;

        private static ListAnchor _anchorLast = new ListAnchor( AnchorType.Last );
        private static ListAnchor _anchorFirst = new ListAnchor( AnchorType.First );

    	/// <summary>
    	/// Initializes a new instance which specifies a position before or after another
    	/// item in the list.
    	/// </summary>
    	/// <param name="anchorType">Type of the position (before or after).</param>
    	/// <param name="refId">ID of the item relative to which the position is specified.</param>
        public ListAnchor( AnchorType anchorType, string refId )
    	{
            _anchorType = anchorType;
            _refId = refId;
    	}

    	/// <summary>
    	/// Initializes a new instance which specifies a position in the beginning or end
    	/// of the list.
    	/// </summary>
    	/// <param name="anchorType">Type of the position (First or Last).</param>
        public ListAnchor( AnchorType anchorType )
        {
            if ( _anchorType == AnchorType.Before || _anchorType == AnchorType.After )
            {
                throw new ArgumentException( "refId must be specified for Before and After anchors" );
            }
            _anchorType = anchorType;
    	}

    	/// <summary>
    	/// Gets or sets the ID of the item relative to which the position is specified.
    	/// If the position type is First or Last, the RefId is null.
    	/// </summary>
        public string RefId
    	{
    		get { return _refId; }
    		set { _refId = value; }
    	}

    	/// <summary>
    	/// Gets or sets the type of the position.
    	/// </summary>
        public AnchorType AnchorType
    	{
    		get { return _anchorType; }
    		set { _anchorType = value; }
    	}

        /// <summary>
        /// Returns the standard anchor "end of the list".
        /// </summary>
        public static ListAnchor Last
        {
            get { return _anchorLast; }
        }

        /// <summary>
        /// Returns the standard anchor "beginning of the list".
        /// </summary>
        public static ListAnchor First
        {
            get { return _anchorFirst; }
        }
    }

    /// <summary>
    /// Allows to register actions - functions which can be invoked by the user through
    /// different user interface controls (menu items, toolbar buttons, keyboard and so on).
    /// </summary>
    public interface IActionManager
    {
        #region MainMenu

        /// <summary>
        /// Registers a top-level menu item in the main menu.
        /// </summary>
        /// <param name="menuName">Name of the menu item to register.</param>
        /// <param name="anchor">Position of the menu item relative to other menu items.</param>
        void RegisterMainMenu( string menuName, ListAnchor anchor );

        /// <summary>
        /// Registers a group for main menu actions. Actions from different groups are divided
        /// by separators.
        /// </summary>
        /// <param name="groupId">
        ///   The identifier of the group. It is used to specify the group when registering actions
        ///   and to reference this group when specifying relative position of action groups.
        /// </param>
        /// <param name="menuName">
        ///   Name of the top-level menu under which the action group is placed. Must contain the
        ///   name of one of the existing menus (File, Edit, View, Go, Tools, Actions or Help),
        ///   or a menu registered with <see cref="RegisterMainMenu"/>.
        /// </param>
        /// <param name="anchor">
        ///   Anchor for specifying the position of this group relative to other groups.
        /// </param>
        void RegisterMainMenuActionGroup( string groupId, string menuName, ListAnchor anchor );

        /// <summary>
        /// Registers a group for actions in a submenu of a main menu.
        /// </summary>
        /// <param name="groupId">
        ///   The identifier of the group. It is used to specify the group when registering actions
        ///   and to reference this group when specifying relative position of action groups.
        /// </param>
        /// <param name="menuName">
        ///   Name of the top-level menu under which the action group is placed. Must contain the
        ///   name of one of the existing menus (File, Edit, View, Go, Tools Actions or Help).
        /// </param>
        /// <param name="submenuName">
        ///   Name of the submenu under the top-level menu in which the action group is placed.
        ///   If two adjacent action groups have the same submenu name, they are displayed in the
        ///   same submenu and delimited with a separator.
        /// </param>
        /// <param name="anchor">
        ///   Anchor for specifying the position of this group relative to other groups.
        /// </param>
        void RegisterMainMenuActionGroup( string groupId, string menuName, string submenuName, ListAnchor anchor );

        /// <summary>
        /// Registers an action which is executed when a main menu item is clicked.
        /// </summary>
        /// <param name="action">The action which is registered.</param>
        /// <param name="groupId">
        ///   Identifier of the main menu action group where the action is placed. Must not be null.
        ///   (The menu in which the action is placed is determined by the action group.)
        /// </param>
        /// <param name="anchor">The relative position of the action within the group.</param>
        /// <param name="text">Caption of the menu item.</param>
        /// <param name="icon">Menu item icon to be shown on the left strip.</param>
        /// <param name="resourceType">
        ///   The resource type to which the action applies, or null if the action applies to all
        ///   resource types. If a type is specified, the menu item is disabled if the selection
        ///   is empty or contains resources of other types.
        /// </param>
        /// <param name="filters">
        ///   The filters which provide additional control over the state of the action. Can be equal to
        ///   null or an empty array if no additional filters are needed.
        ///   If the filters have marked the presentation as hidden, the Update() method of the
        ///   action itself is not called.
        /// </param>
        void RegisterMainMenuAction( IAction action, string groupId, ListAnchor anchor, string text,
                                     Image icon, string resourceType, IActionStateFilter[] filters );

        /// <summary>
        /// Removes an action and its menu item from the main menu.
        /// </summary>
        /// <param name="action">The action to remove.</param>
        void UnregisterMainMenuAction( IAction action );

        /// <summary>
        /// Suppresses the separator between the two specified groups in the main menu.
        /// </summary>
        /// <param name="groupId1">The group on one side of the separator.</param>
        /// <param name="groupId2">The group on the other side of the separator.</param>
        /// <since>2.0</since>
        void SuppressMainMenuGroupSeparator( string groupId1, string groupId2 );

        #endregion MainMenu

        #region ToolBar

        /// <summary>
        /// Registers a group for toolbar actions. Actions from different groups are divided by
        /// separators on the toolbar.
        /// </summary>
        /// <param name="groupId">
        ///   The identifier of the group. It is used to specify the group when registering actions
        ///   and to reference this group when specifying relative position of action groups.
        /// </param>
        /// <param name="anchor">
        ///   Anchor for specifying the position of this group relative to other groups.
        /// </param>
        void RegisterToolbarActionGroup( string groupId, ListAnchor anchor );

        /// <summary>
        /// Registers an action which is executed when a toolbar button is clicked. The button
        /// icon is specified as an Icon instance.
        /// </summary>
        /// <param name="action">The action which is registered.</param>
        /// <param name="groupId">
        ///   Identifier of the toolbar action group where the action is placed.
        ///   If null, the default group is used (it is placed before all other groups).
        /// </param>
        /// <param name="anchor">The relative position of the action within the group.</param>
        /// <param name="icon">The icon shown on the button.</param>
        /// <param name="text">The text shown on the button, or null if the button contains only an icon.</param>
        /// <param name="tooltip">The tooltip shown for the button.</param>
        /// <param name="resourceType">
        ///   The resource type to which the action applies, or null if the action applies to all
        ///   resource types. If a type is specified, the toolbar button is disabled if the selection
        ///   is empty or contains resources of other types.
        /// </param>
        /// <param name="filters">
        ///   The filters which provide additional control over the state of the action. Can be equal to
        ///   null or an empty array if no additional filters are needed.
        ///   If the filters have marked the presentation as hidden, the Update() method of the
        ///   action itself is not called.
        /// </param>
        void RegisterToolbarAction( IAction action, string groupId, ListAnchor anchor,
            Icon icon, string text, string tooltip, string resourceType, IActionStateFilter[] filters );

        /// <summary>
        /// Registers an action which is executed when a toolbar button is clicked. The button
        /// icon is specified as an Image instance.
        /// </summary>
        /// <param name="action">The action which is registered.</param>
        /// <param name="groupId">
        ///   Identifier of the toolbar action group where the action is placed.
        ///   If null, the default group is used (it is placed before all other groups).
        /// </param>
        /// <param name="anchor">The relative position of the action within the group.</param>
        /// <param name="icon">The icon shown on the button.</param>
        /// <param name="text">The text shown on the button, or null if the button contains only an icon.</param>
        /// <param name="tooltip">The tooltip shown for the button.</param>
        /// <param name="resourceType">
        ///   The resource type to which the action applies. The toolbar button is disabled if the selection
        ///   contains resources of other types.
        /// </param>
        /// <param name="filters">
        ///   The filters which provide additional control over the state of the action. Can be equal to
        ///   null or an empty array if no additional filters are needed.
        /// </param>
        void RegisterToolbarAction( IAction action, string groupId, ListAnchor anchor,
            Image icon, string text, string tooltip, string resourceType, IActionStateFilter[] filters );

        /// <summary>
        /// Registers an additional filter for a previously registered toolbar action.
        /// </summary>
        /// <param name="actionId">The identifier of the action for which the filter is registered
        /// (the string returned by the ToString() method of the action).</param>
        /// <param name="filter">The filter to be registered.</param>
        void RegisterToolbarActionFilter( string actionId, IActionStateFilter filter );

        /// <summary>
        /// Removes an action and its menu item from the toolbar.
        /// </summary>
        /// <param name="action">The action to remove.</param>
        void UnregisterToolbarAction( IAction action );

        #endregion ToolBar

        #region UrlBar

        /// <summary>
        /// Registers a group for URL bar actions. The URL bar is the toolbar displayed to the
        /// left of the Web address box when a Web page is viewed. Actions from different groups
        /// are divided by separators on the toolbar.
        /// </summary>
        /// <param name="groupId">
        ///   The identifier of the group. It is used to specify the group when registering actions
        ///   and to reference this group when specifying relative position of action groups.
        /// </param>
        /// <param name="anchor">
        ///   Anchor for specifying the position of this group relative to other groups.
        /// </param>
        void RegisterUrlBarActionGroup( string groupId, ListAnchor anchor );

        /// <summary>
        /// Registers an action which is executed when a button on the URL bar is clicked. The button
        /// icon is specified as an Icon instance.
        /// </summary>
        /// <param name="action">The action which is registered.</param>
        /// <param name="groupId">
        ///   Identifier of the toolbar action group where the action is placed.
        ///   If null, the default group is used (it is placed before all other groups).
        /// </param>
        /// <param name="anchor">The relative position of the action within the group.</param>
        /// <param name="icon">The icon shown on the button.</param>
        /// <param name="text">The text shown on the button, or null if the button contains only an icon.</param>
        /// <param name="tooltip">The tooltip shown for the button.</param>
        /// <param name="filters">
        ///   The filters which provide additional control over the state of the action. Can be equal to
        ///   null or an empty array if no additional filters are needed.
        /// </param>
        void RegisterUrlBarAction( IAction action, string groupId, ListAnchor anchor,
            Icon icon, string text, string tooltip, IActionStateFilter[] filters );

        /// <summary>
        /// Registers an action which is executed when a button on the URL bar is clicked. The button
        /// icon is specified as an Image instance.
        /// </summary>
        /// <param name="action">The action which is registered.</param>
        /// <param name="groupId">
        ///   Identifier of the toolbar action group where the action is placed.
        ///   If null, the default group is used (it is placed before all other groups).
        /// </param>
        /// <param name="anchor">The relative position of the action within the group.</param>
        /// <param name="icon">The icon shown on the button.</param>
        /// <param name="text">The text shown on the button, or null if the button contains only an icon.</param>
        /// <param name="tooltip">The tooltip shown for the button.</param>
        /// <param name="filters">
        ///   The filters which provide additional control over the state of the action. Can be equal to
        ///   null or an empty array if no additional filters are needed.
        /// </param>
        void RegisterUrlBarAction( IAction action, string groupId, ListAnchor anchor,
            Image icon, string text, string tooltip, IActionStateFilter[] filters );

        /// <summary>
        /// Removes an action and its menu item from the URL bar.
        /// </summary>
        /// <param name="action">The action to remove.</param>
        void UnregisterUrlBarAction( IAction action );

        #endregion UrlBar

        #region ContextMenu

        /// <summary>
        /// Registers a group for context menu actions. Actions from different groups are divided
        /// by separators.
        /// </summary>
        /// <param name="groupId">
        ///   The identifier of the group. It is used to specify the group when registering actions
        ///   and to reference this group when specifying relative position of action groups.
        /// </param>
        /// <param name="anchor">
        ///   Anchor for specifying the position of this group relative to other groups.
        /// </param>
        void RegisterContextMenuActionGroup( string groupId, ListAnchor anchor );

        /// <summary>
        /// Registers a group for actions in a submenu of the context menu.
        /// </summary>
        /// <param name="groupId">
        ///   The identifier of the group. It is used to specify the group when registering actions
        ///   and to reference this group when specifying relative position of action groups.
        /// </param>
        /// <param name="submenuName">
        ///   Name of the submenu in which the action group is placed.
        ///   If two adjacent action groups have the same submenu name, they are displayed in the
        ///   same submenu and delimited with a separator.
        /// </param>
        /// <param name="anchor">
        ///   Anchor for specifying the position of this group relative to other groups.
        /// </param>
        void RegisterContextMenuActionGroup( string groupId, string submenuName, ListAnchor anchor );

        /// <summary><seealso cref="IAction"/><seealso cref="UnregisterContextMenuAction"/>
        /// Registers an action which is executed when a context menu item is clicked.
        /// </summary>
        /// <param name="action">The action which is registered.</param>
        /// <param name="groupId">
        ///   Identifier of the context menu action group where the action is placed. Must not be null.
        /// </param>
        /// <param name="anchor">The relative position of the action within the group.</param>
        /// <param name="text">Caption of the menu item.</param>
        /// <param name="icon">Menu item icon to be shown on the left strip.</param>
        /// <param name="resourceType">
        ///   The resource type to which the action applies, or null if the action applies to all
        ///   resource types. If a type is specified, the menu item is hidden if the selection
        ///   is empty or contains resources of other types.
        /// </param>
        /// <param name="filters">
        ///   The filters which provide additional control over the state of the action. Can be equal to
        ///   null or an empty array if no additional filters are needed.
        ///   If the filters have marked the presentation as hidden, the Update() method of the
        ///   action itself is not called.
        /// </param>
        void RegisterContextMenuAction( IAction action, string groupId, ListAnchor anchor, string text,
                                        Image icon, string resourceType, IActionStateFilter[] filters );

        /// <summary><seealso cref="IAction"/><seealso cref="RegisterContextMenuAction"/>
        /// Removes an action and its menu item from the context menu.
        /// </summary>
        /// <param name="action">The action to remove.</param>
        void UnregisterContextMenuAction( IAction action );

        /// <summary>
        /// Suppresses the separator between the two specified groups in the context menu.
        /// </summary>
        /// <param name="groupId1">The group on one side of the separator.</param>
        /// <param name="groupId2">The group on the other side of the separator.</param>
        /// <since>2.0</since>
        void SuppressContextMenuGroupSeparator( string groupId1, string groupId2 );

        #endregion ContextMenu

        #region DoubleClick Actions

        /// <summary>
        /// Registers an action which is executed when a resource is double-clicked.
        /// There can be only one action registered for each specific resource type,
        /// and multiple actions which apply to all types. The actions applying to all
        /// types are tried only if no resource-type-specific action is registered for the
        /// type of the double-clicked resource, and the first action which is not disabled
        /// or hidden is executed.
        /// </summary>
        /// <param name="action">The action which is registered.</param>
        /// <param name="resourceType">
        ///   Type of the resources to which the action applies, or null if the action applies
        ///   to all resource types.
        /// </param>
        /// <param name="filters">
        ///   The filters which provide additional control over the state of the action. Can be equal to
        ///   null or an empty array if no additional filters are needed.
        ///   If the filters have marked the presentation as hidden, the Update() method of the
        ///   action itself is not called.
        /// </param>
        void RegisterDoubleClickAction( IAction action, string resourceType, IActionStateFilter[] filters );

        /// <summary>
        /// Removes an action from the list of actions executed by double click.
        /// </summary>
        /// <param name="action">The action to remove.</param>
        void UnregisterDoubleClickAction( IAction action );

        #endregion DoubleClick Actions

        #region Keyboard Actions

        /// <summary>
        /// Registers an action which is executed when a keyboard shortcut is pressed. There can
        /// be multiple actions associated with the same keyboard shortcut. The actions are enumerated
        /// in undefined order until one is found which matches the selection resource type and is not
        /// disabled or hidden. That action is executed, and remaining actions are not processed.
        /// </summary>
        /// <param name="action">The action which is registered.</param>
        /// <param name="shortcutKey">The shortcut key for which the action is registered.</param>
        /// <param name="resourceType">
        ///   Type of the resources to which the action applies, or null if the action applies
        ///   to all resource types.
        /// </param>
        /// <param name="filters">
        ///   The filters which provide additional control over the state of the action. Can be equal to
        ///   null or an empty array if no additional filters are needed.
        ///   If the filters have marked the presentation as hidden, the Update() method of the
        ///   action itself is not called.
        /// </param>
        void RegisterKeyboardAction( IAction action, Keys shortcutKey, string resourceType,
            IActionStateFilter[] filters );

        /// <summary>
        /// Removes an action from the list of actions executed by a keyboard shortcut.
        /// </summary>
        /// <param name="action">The action to remove.</param>
        void UnregisterKeyboardAction( IAction action );

        #endregion Keyboard Actions

        #region LinkClick Actions

        /// <summary>
        /// Registers an action which is executed when a link to a resource of the specified type
        /// is clicked in the links pane or the shortcut bar. Only one link click action can be
        /// registered for each resource type; if the function is called multiple times, the
        /// last registered action is used.
        /// </summary>
        /// <param name="action">The action which is registered.</param>
        /// <param name="resourceType">
        ///   The resource type for which the action is registered. May not be null.
        /// </param>
        /// <param name="filters">
        ///   The filters which provide additional control over the state of the action. Can be equal to
        ///   null or an empty array if no additional filters are needed.
        ///   If the filters have marked the presentation as hidden, the Update() method of the
        ///   action itself is not called.
        /// </param>
        void RegisterLinkClickAction( IAction action, string resourceType, IActionStateFilter[] filters );

        /// <summary>
        /// Removes an action from the list of actions executed by a link click.
        /// </summary>
        /// <param name="action">The action to remove.</param>
        void UnregisterLinkClickAction( IAction action );

        #endregion LinkClick Actions

        #region LinkPane Actions

        /// <summary>
        /// Registers an action which is shown as a link label on the links pane for a resource, and
        /// executed when the link label is clicked.
        /// </summary>
        /// <param name="action">The action which is registered.</param>
        /// <param name="text">The text of the link label.</param>
        /// <param name="resourceType">
        ///   Type of the resources to which the action applies, or null if the action applies
        ///   to all resource types.
        /// </param>
        /// <param name="filters">
        ///   The filters which provide additional control over the state of the action. Can be equal to
        ///   null or an empty array if no additional filters are needed.
        ///   If the filters have marked the presentation as hidden, the Update() method of the
        ///   action itself is not called.
        /// </param>
        void RegisterLinksPaneAction( IAction action, string text, string resourceType, IActionStateFilter[] filters );

        /// <summary>
        /// Removes an action from the list of link pane label actions.
        /// </summary>
        /// <param name="action">The action to remove.</param>
        void UnregisterLinksPaneAction( IAction action );

        #endregion LinkPane Actions

        #region Action Execution

        /// <summary>
        /// Shows the context menu with actions for the specified context.
        /// </summary>
        /// <param name="context">The context for which the menu is shown.</param>
        /// <param name="ownerControl">The control with which the context menu is associated.</param>
        /// <param name="x">
        ///   The X coordinate at which the menu should be shown, relative to ownerControl.
        /// </param>
        /// <param name="y">
        ///   The X coordinate at which the menu should be shown, relative to ownerControl.
        /// </param>
        void ShowResourceContextMenu( IActionContext context, Control ownerControl, int x, int y );

        /// <summary>
        /// Executes the double-click action for the specified resource.
        /// </summary>
        /// <param name="res">The resource for which the double-click action should be executed.</param>
        void ExecuteDoubleClickAction( IResource res );

        /// <summary>
        /// Executes the double-click action for every resource in the specified list.
        /// </summary>
        /// <param name="selectedResources">The resources for which the double-click action should be executed.</param>
        void ExecuteDoubleClickAction( IResourceList selectedResources );

        /// <summary>
        /// Executes the keyboard action for the specified shortcut key and context.
        /// </summary>
        /// <param name="context">The context in which the action is executed.</param>
        /// <param name="shortcutKey">The shortcut key for which the action is executed.</param>
        /// <returns>
        ///   true if an enabled action for this shortcut key was found and executed, false otherwise.
        /// </returns>
        bool ExecuteKeyboardAction( IActionContext context, Keys shortcutKey );

        /// <summary>
        /// Executes the action which is registered for clicking the link to the specified resource.
        /// </summary>
        /// <param name="context">The context in which the action is executed.</param>
        /// <returns>
        ///   true if the action was found and executed, false if there is no action for the type
        ///   of the specified resource or if the action was hidden or disabled.
        /// </returns>
        bool ExecuteLinkClickAction( IActionContext context );

        #endregion Action Execution

        /// <summary>
        /// Returns the double-click action registered for the specified resource.
        /// </summary>
        /// <param name="res">The resource for which the action is returned.</param>
        /// <returns>The double-click action instance, or null if there is no action registered.</returns>
        IAction GetDoubleClickAction( IResource res );

        /// <summary>
        /// Returns the string representation of the keyboard shortcut for which the specified
        /// action is registered. If the action was registered for multiple keyboard shortcuts,
        /// returns the one which was most recently registered.
        /// </summary>
        /// <param name="action">The action for which the shortcut should be returned.</param>
        /// <returns>
        ///   The shortcut for which the action is registered, or an empty string if the action
        ///   has no keyboard shortcut.
        /// </returns>
        string GetKeyboardShortcut( IAction action );

        /// <summary>
        /// Returns the string representation of the keyboard shortcut for which the specified
        /// action is registered, if the shortcut is available in the specified context.
        /// If the action was registered for multiple keyboard shortcuts, returns the one which
        /// was most recently registered.
        /// </summary>
        /// <param name="action">The action for which the shortcut should be returned.</param>
        /// <param name="context">The context in which the shortcut is checked for validity.</param>
        /// <returns>
        ///   The shortcut for which the action is registered, or an empty string if the action
        ///   has no keyboard shortcut or the shortcut is disabled in the specified context.
        /// </returns>
        string GetKeyboardShortcut( IAction action, IActionContext context );
        Keys?  GetKeyboardShortcutEx( IAction action, IActionContext context );

        /// <summary>
        /// Registers a component which is contributed to a composite action. A composite action
        /// is an action which is represented by a single UI control (for example, toolbar button
        /// or menu item) but executes different things depending on the context (for example,
        /// the Delete action runs different code for deleting categories, emails and news articles).
        /// Several standard composite actions (Reply, Forward, Delete) are defined in the core of
        /// the application.
        /// </summary>
        /// <param name="action">The action which is registered.</param>
        /// <param name="compositeId">The ID of the composite action to which the component is contributed.</param>
        /// <param name="resourceType">
        ///   Type of the resources to which the action applies, or null if the action applies
        ///   to all resource types.
        /// </param>
        /// <param name="filters">
        ///   The filters which provide additional control over the state of the action. Can be equal to
        ///   null or an empty array if no additional filters are needed.
        ///   If the filters have marked the presentation as hidden, the Update() method of the
        ///   action itself is not called.
        /// </param>
        void RegisterActionComponent( IAction action, string compositeId, string resourceType,
            IActionStateFilter[] filters );

        /// <summary>
        /// Specifies that the XML action configuration for the specified plugin assembly
        /// should not be loaded
        /// </summary>
        /// <remarks>This can be useful if the plugin startup fails and it's not possible to
        /// execute any of the plugin actions.</remarks>
        /// <param name="pluginAssembly">The plugin assembly</param>
        void DisableXmlActionConfiguration( Assembly pluginAssembly );

        /// <summary>
        /// Returns the current action context.
        /// </summary>
        /// <since>3.0</since>
        IActionContext GetMainMenuActionContext();
    }

    public class ActionGroups
    {
        public const string ITEM_OPEN_ACTIONS   = "ItemOpenActions";
        public const string ITEM_FIND_ACTIONS   = "ItemFindActions";
        public const string ITEM_MODIFY_ACTIONS = "ItemModifyActions";

        public const string VIEW_GOTO_ACTIONS     = "ViewGotoActions";
        public const string VIEW_VIEWPANE_ACTIONS   = "ViewViewpaneActions";

        public const string GO_TAB_ACTIONS          = "GoTabActions";
        public const string TOOLS_OPTIONS_ACTIONS   = "ToolsOptionsActions";
        public const string ACTION_STANDARD_ACTIONS = "ActionStandardActions";
    }

    /// <summary>
    /// The exception is thrown when an error happens in the action manager.
    /// </summary>
    [Serializable]
    public class ActionException: Exception
    {
        public ActionException()
            : base() { }

        public ActionException( string message )
            : base( message ) { }

        public ActionException( string message, Exception innerException )
            : base( message, innerException ) { }

        protected ActionException( SerializationInfo info, StreamingContext context )
            : base( info, context ) { }
    }
}
