/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Allows to exclude certain link types, links and links pane actions from the links
    /// pane for a specific resource.
    /// </summary>
    /// <remarks>
    /// A links pane filter can be registered for a resource type, or applied on an ad-hoc
    /// basis for displaying a specific resource. Only one links pane filter can be active
    /// at the same time.
    /// </remarks>
    public interface ILinksPaneFilter
    {
        /// <summary>
        /// Allows to hide all links of a specified type, or to change the display name for
        /// a link type shown in the links pane.
        /// </summary>
        /// <param name="displayedResource">Currently displayed resource.</param>
        /// <param name="propId">ID of the link type (negative for reverse links).</param>
        /// <param name="displayName">Display name of the link type (can be changed).</param>
        /// <returns>true if the link type should be displayed, false otherwise.</returns>
        bool AcceptLinkType( IResource displayedResource, int propId, ref string displayName );

        /// <summary>
        /// Allows to hide a specific link from the links pane.
        /// </summary>
        /// <param name="displayedResource">Currently displayed resource.</param>
        /// <param name="propId">ID of the link type (negative for reverse links).</param>
        /// <param name="targetResource">Resource the link to which is displayed.</param>
        /// <param name="linkTooltip">Tooltip shown for the link label.</param>
        /// <returns>true if the link should be displayed, false otherwise.</returns>
        bool AcceptLink( IResource displayedResource, int propId, IResource targetResource,
            ref string linkTooltip );

        /// <summary>
        /// Allows to hide a specific action from the links pane.
        /// </summary>
        /// <param name="displayedResource">Currently displayed resource.</param>
        /// <param name="action">The action displayed in the links pane.</param>
        /// <returns>true if the action should be displayed, false otherwise.</returns>
        bool AcceptAction( IResource displayedResource, IAction action );
    }

    /// <summary>
    /// The mode of auto-previewing items in a list.
    /// </summary>
    /// <since>2.0</since>
    public enum AutoPreviewMode
    {
        /// <summary>
        /// Auto-preview is not shown.
        /// </summary>
        Off, 

        /// <summary>
        /// Auto-preview is shown for all items.
        /// </summary>
        AllItems, 

        /// <summary>
        /// Auto-preview is shown for unread items.
        /// </summary>
        UnreadItems
    };

    /// <summary>
    /// Manages the display of resources and resource lists in Omea.
    /// </summary>
    public interface IResourceBrowser: IContextProvider, ICommandProcessor
    {
        /// <summary>
        /// Displays the specified resource in a full-height preview pane (with the
        /// resource list hidden).
        /// </summary>
        /// <param name="res">The resource to display.</param>
        void DisplayResource( IResource res );
        
        /// <summary>
        /// Displays the specified resource in a full-height preview pane (with the
        /// resource list hidden), and allows to control the action performed when the
        /// resource is deleted.
        /// </summary>
        /// <param name="res">The resource to display.</param>
        /// <param name="backOnDelete">If true, ResourceBrowser goes to the previous resource in the
        /// browse stack when the resource is deleted. If false, ResourceBrowser performs no additional
        /// action, which allows other components to handle the deletion.</param>
        /// <since>2.0</since>
        void DisplayResource( IResource res, bool backOnDelete );

        /// <summary>
        /// Displays the specified resource list.
        /// </summary>
        /// <param name="ownerResource">The resource on which the resource list is based
        /// (typically, the resource selected in the sidebar), or null if not applicable.</param>
        /// <param name="resList">The resource list to display.</param>
        /// <param name="caption">The caption of the resource list.</param>
        /// <param name="columns">The columns to use for displaying the list, or null if
        /// a default set of columns should be used.</param>
        void DisplayResourceList( IResource ownerResource, IResourceList resList, string caption, 
            ColumnDescriptor[] columns );

        /// <summary>
        /// Displays the specified resource list and sets the selection on the specified resource.
        /// </summary>
        /// <param name="ownerResource">The resource on which the resource list is based
        /// (typically, the resource selected in the sidebar), or null if not applicable.</param>
        /// <param name="resList">The resource list to display.</param>
        /// <param name="caption">The caption of the resource list.</param>
        /// <param name="columns">The columns to use for displaying the list, or null if
        /// a default set of columns should be used.</param>
        /// <param name="selectedResource">The resource which is selected in the list.</param>
        void DisplayResourceList( IResource ownerResource, IResourceList resList, string caption, 
            ColumnDescriptor[] columns, IResource selectedResource );

        /// <summary>
        /// Displays the specified resource list, sets the selection on the specified resource
        /// and uses the specified provider for obtaining search result highlighting data.
        /// </summary>
        /// <param name="ownerResource">The resource on which the resource list is based
        /// (typically, the resource selected in the sidebar), or null if not applicable.</param>
        /// <param name="resList">The resource list to display.</param>
        /// <param name="caption">The caption of the resource list.</param>
        /// <param name="columns">The columns to use for displaying the list, or null if
        /// a default set of columns should be used.</param>
        /// <param name="selectedResource">The resource which is selected in the list.</param>
        /// <param name="highlightProvider">The provider for the search result highlighting data.</param>
        void DisplayResourceList( IResource ownerResource, IResourceList resList, string caption, 
            ColumnDescriptor[] columns, IResource selectedResource, IHighlightDataProvider highlightProvider );

        /// <summary>
        /// Displays the specified resource list with the specified display options.
        /// </summary>
        /// <param name="ownerResource">The resource on which the resource list is based
        /// (typically, the resource selected in the sidebar), or null if not applicable.</param>
        /// <param name="resList">The resource list to display.</param>
        /// <param name="options">The parameters for displaying the resource list.</param>
        /// <since>2.0</since>
        void DisplayResourceList( IResource ownerResource, IResourceList resList, 
            ResourceListDisplayOptions options );

        /// <summary>
        /// Displays the specified resource list with the specified display options and with
        /// additional options defined by the properties of the owner resource.
        /// </summary>
        /// <param name="ownerResource">The resource on which the resource list is based
        /// (typically, the resource selected in the sidebar), or null if not applicable.</param>
        /// <param name="resList">The resource list to display.</param>
        /// <param name="options">The parameters for displaying the resource list.</param>
        /// <since>2.0</since>
        void DisplayConfigurableResourceList( IResource ownerResource, IResourceList resList,
            ResourceListDisplayOptions options );

        /// <summary>
        /// Displays a resource list in a threaded (hierarchical) view.
        /// </summary>
        /// <param name="ownerResource">The resource on which the resource list is based
        /// (typically, the resource selected in the sidebar), or null if not applicable.</param>
        /// <param name="resList">The resource list to display.</param>
        /// <param name="caption">The caption of the resource list.</param>
        /// <param name="sortProp">The space-separated list of property names by which the
        /// resource list is sorted.</param>
        /// <param name="replyProp">The ID of a directed link property which links a reply to
        /// its original resource.</param>
        /// <param name="columns">The columns to use for displaying the list, or null if
        /// a default set of columns should be used.</param>
        /// <param name="selectedResource">The resource which is selected in the list.</param>
        void DisplayThreadedResourceList( IResource ownerResource, IResourceList resList, string caption, 
            string sortProp, int replyProp, ColumnDescriptor[] columns, IResource selectedResource );

        /// <summary>
        /// Displays the specified resource list without applying tab or workspace filters.
        /// </summary>
        /// <param name="ownerResource">The resource on which the resource list is based
        /// (typically, the resource selected in the sidebar), or null if not applicable.</param>
        /// <param name="resList">The resource list to display.</param>
        /// <param name="caption">The caption of the resource list.</param>
        /// <param name="columns">The columns to use for displaying the list, or null if
        /// a default set of columns should be used.</param>
        void DisplayUnfilteredResourceList( IResource ownerResource, IResourceList resList, string caption, 
            ColumnDescriptor[] columns );
       
        /// <summary>
        /// Displays the conversation of resources linked to the specified resource.
        /// </summary>
        /// <param name="res">The resource which is a part of a conversation.</param>
        /// <remarks>The standard "Reply" link is used to get the replies of resources.</remarks>
        void DisplayConversation( IResource res );

        /// <summary>
        /// Registers a callback which allows to display a different resource in the display pane
        /// when a specific resource is selected in the resource browser.
        /// </summary>
        /// <param name="resType">The resource type for which the forwarder is registered.</param>
        /// <param name="forwarder">The forwarder callback.</param>
        /// <remarks>The callback is currently used for Web link resources. The resource which
        /// is selected in the resource browser is the Web link resource, and the resource which
        /// is actually displayed is its content resource (HtmlFile, for example).</remarks>
        void RegisterResourceDisplayForwarder( string resType, ResourceDisplayForwarderCallback forwarder );
        
        /// <summary>
        /// Refreshes the resource currently displayed in the resource browser.
        /// </summary>
        /// <remarks>The method works both for resources selected in a full-height preview
        /// pane and for the selected resource when a resource list is displayed.</remarks>
        void RedisplaySelectedResource();

        /// <summary>
        /// Sets the focus to the resource list control.
        /// </summary>
        void FocusResourceList();

        /// <summary>
        /// Adds an information link label above the resource list.
        /// </summary>
        /// <param name="text">The text to be displayed.</param>
        /// <param name="clickHandler">The handler to execute when the text is clicked.
        /// If not null, the text is displayed as a clickable link.</param>
        void AddStatusLine( string text, EventHandler clickHandler );
        
        /// <summary>
        /// Hides the information label above the resource list.
        /// </summary>
        void HideStatusLine();

        /// <summary>
        /// Shows the See Also bar and populates it with links to resources in the
        /// specified resource list.
        /// </summary>
        /// <param name="resList">The resource list for which the See Also links are displayed.</param>
        void ShowSeeAlsoBar( IResourceList resList );

        /// <summary>
        /// Shows the See Also bar and populates it with links to resources in the
        /// specified resource list.
        /// </summary>
        /// <param name="resList">The resource list for which the See Also links are displayed.</param>
        /// <param name="prepared">Indicates whether the resource list is necessary to be filtered
        /// with deleted resources.</param>
        /// <since>2.1</since>
        void ShowSeeAlsoBar( IResourceList resList, bool prepared );

        /// <summary>
        /// Sets the selection in the resource list to the specified resource.
        /// </summary>
        /// <param name="res">The resource to select.</param>
        /// <returns>true if the resource was selected successfully, false if it was
        /// not found in the list.</returns>
        bool SelectResource( IResource res );

        /// <summary>
        /// Begins the in-place editing of the specified resource.
        /// </summary>
        /// <param name="res">The resource to edit.</param>
        /// <remarks>For the in-place editing to work, a 
        /// <see cref="IResourceUIHandler">resource UI handler</see> must be registered
        /// for the resource.</remarks>
        void EditResourceLabel( IResource res );

        /// <summary>
        /// Expands the specified conversation node.
        /// </summary>
        /// <param name="res">The resource to expand.</param>
        /// <remarks>The method will not work if the resource is not a root node of a
        /// conversation but a reply to another resource, and the root of the thread
        /// has never been expanded.</remarks>
        void ExpandConversation( IResource res );

        /// <summary>
        /// Shows the URL bar and the URL edit box with the specified URL.
        /// </summary>
        /// <param name="url">The URL to display in the URL edit box.</param>
        void ShowUrlBar( string url );

        /// <summary>
        /// Begins a batch update of the resource browser.
        /// <seealso cref="EndUpdate"/>
        /// </summary>
        /// <remarks>The toolbar, URL bar and status line of the browser are not updated while
        /// a batch update is in progress.</remarks>
        void BeginUpdate();

        /// <summary>
        /// Ends a batch update of the resource browser.
        /// <seealso cref="BeginUpdate"/>
        /// </summary>
        /// <remarks>The toolbar, URL bar and status line of the browser are not updated while
        /// a batch update is in progress.</remarks>
        void EndUpdate();

        /// <summary>
        /// Registers a links pane filter which is used for all resources of the specified type.
        /// </summary>
        /// <param name="resourceType">Type for which the filter is registered.</param>
        /// <param name="filter">The filter instance.</param>
        void RegisterLinksPaneFilter( string resourceType, ILinksPaneFilter filter );

        /// <summary>
        /// Registers a group of link types in the links pane.
        /// </summary>
        /// <param name="groupId">The ID of the group.</param>
        /// <param name="propTypes">List of link type IDs (positive only) which are included in the group.</param>
        /// <param name="anchor">Position of the group relative to other groups.</param>
        /// <remarks>If a group with the same ID has already been registered, the new property types
        /// are appended to the property types which were previously registered for the group.</remarks>
        void RegisterLinksGroup( string groupId, int[] propTypes, ListAnchor anchor );

        /// <summary>
        /// Returns the resource which is displayed above the specified resource
        /// in the resource list.
        /// </summary>
        /// <param name="res">The resource for which the previous resource is requested.</param>
        /// <returns>The previous resource, or null if the specified resource is the
        /// first in the resource list or not displayed in the resource list at all.</returns>
        /// <since>2.0</since>
        IResource GetResourceAbove( IResource res );

        /// <summary>
        /// Returns the resource which is displayed below the specified resource
        /// in the resource list.
        /// </summary>
        /// <param name="res">The resource for which the next resource is requested.</param>
        /// <returns>The next resource, or null if the specified resource is the
        /// last in the resource list or not displayed in the resource list at all.</returns>
        /// <since>2.0</since>
        IResource GetResourceBelow( IResource res );

        /// <summary>
        /// Returns the list of resources currently displayed in the resource browser. Note that "visible" does not imply on "items currently fitting on screen" or "items in the expanded threads", nor it concerns the items' rendering aspects in any way.
        /// </summary>
        IResourceList VisibleResources { get; }
        
        /// <summary>
        /// Returns the list of resources currently selected in the resource browser.
        /// </summary>
        IResourceList SelectedResources { get; }
        
        /// <summary>
        /// Returns the resource on which the currently displayed resource list is based.
        /// </summary>
        /// <remarks>Returns the resource which was passed in the ownerResource parameter
        /// to one of the methods which display resource lists.</remarks>
        IResource OwnerResource { get; }

        /// <summary>
        /// The resource list which is intersected with any resource list displayed in the browser.
        /// </summary>
        /// <since>2.0</since>
        IResourceList FilterResourceList { get; }
        
        /// <summary>
        /// The filter resource list which was used when the last resource list was displayed.
        /// </summary>
        /// <since>2.0</since>
        IResourceList LastFilterResourceList { get; }

        /// <summary>
        /// Returns the resource currently displayed in a full-height preview pane, or null
        /// if a resource list is currently displayed.
        /// </summary>
        IResource DisplayedResource { get; }

        /// <summary>
        /// Gets or sets the value indicating whether the resource browser is currently in
        /// the Web page layout mode.
        /// </summary>
        /// <remarks>In the Web page mode, the URL bar is displayed, and the resource list,
        /// links pane and the regular toolbar are hidden.</remarks>
        bool WebPageMode { get; set; }

        /// <summary>
        /// Controls the visible state of the links pane which is located to the right of the Preview area.
        /// </summary>
        bool LinksPaneExpanded { get; set; }
        
        /// <summary>
        /// Gets or sets the value indicating whether the resource list is currently collapsed.
        /// </summary>
        bool ResourceListExpanded { get; set; }

        /// <summary>
        /// Returns true if the resource browser is currently displaying a resource list, or false
        /// if a single resource or a newspaper is currently displayed.
        /// </summary>
        bool ResourceListVisible { get; }

        /// <summary>
        /// Returns true if the resource browser is currently displaying a resource list and
        /// it contains current focus.
        /// </summary>
        /// <since>2.1</since>
        bool ResourceListFocused { get; }

        /// <summary>
        /// Returns true if the resource browser is currently displaying a threaded resource list.
        /// </summary>
        /// <since>2.0</since>
        bool IsThreaded { get; }

        /// <summary>
        /// Returns true if the resource browser is currently displaying a newspaper.
        /// </summary>
        /// <since>2.0</since>
        bool NewspaperVisible { get; }

        /// <summary>
        /// Gets or sets the value indicating whether the annotation window is displayed automatically
        /// when an annotated resource is selected in the resource browser.
        /// </summary>
        bool ViewAnnotations{ get; set; }
        
        /// <summary>
        /// Starts editing the annotation of the specified resource.
        /// </summary>
        /// <param name="res">The resource for which the annotation should be edited.</param>
        void EditAnnotation( IResource res );

        /// <summary>
        /// Fired when the resource browser displays different content (content of a different
        /// resource in full-page mode, or a different resource list).
        /// </summary>
        event EventHandler ContentChanged;

        /// <summary>
        /// Selects the next unread item in the current view or in the next view which contains
        /// unread items.
        /// </summary>
        /// <returns>Whether there were unread items to be selected or not.</returns>
        /// <since>1.0.3</since>
        bool GotoNextUnread();

        /// <summary>
        /// Goes to the previous item in the current browse stack.
        /// </summary>
        void GoBack();

        /// <summary>
        /// Goes to the next item in the current browse stack.
        /// </summary>
        void GoForward();

        /// <summary>
        /// Sets the default view settings for a specified tab.
        /// </summary>
        /// <param name="tabId">The ID of the tab for which the settings are defined.</param>
        /// <param name="autoPreviewMode">The default auto-preview mode.</param>
        /// <param name="verticalLayout">The default preview pane layout.</param>
        /// <since>2.0</since>
        void SetDefaultViewSettings( string tabId, AutoPreviewMode autoPreviewMode, bool verticalLayout );
    }

    /// <summary>
    /// Specifies the options for displaying a resource list in the resource browser.
    /// </summary>
    /// <since>2.0</since>
    public class ResourceListDisplayOptions
    {
        private string _caption;
        private string _captionTemplate;
        private ColumnDescriptor[] _columns;
        private IResource _selectedResource;
        private IHighlightDataProvider _highlightDataProvider;
        private IResourceThreadingHandler _threadingHandler;
        private SortSettings _sortSettings;
        private bool _tabFilter = true;
        private bool _seeAlsoBar = false;
        private bool _suppressContexts = false;
        private bool _showNewspaper = false;
        private string _statusLine;
        private EventHandler _statusLineClickHandler;
        private string _emptyText;
        private IResource _transientContainerParent;
        private string _transientContainerPaneId;
        private bool _defaultGroupItems = true;

        public ResourceListDisplayOptions()
        {
        }

        public ResourceListDisplayOptions( ResourceListDisplayOptions options )
        {
            _caption               = options.Caption;
            _captionTemplate       = options.CaptionTemplate;
            _columns               = options.Columns;
            _selectedResource      = options.SelectedResource;
            _highlightDataProvider = options.HighlightDataProvider;
            _threadingHandler      = options.ThreadingHandler;
            _sortSettings          = options.SortSettings;
            _tabFilter             = options.TabFilter;
            _seeAlsoBar            = options.SeeAlsoBar;
            _suppressContexts      = options.SuppressContexts;
            _statusLine            = options.StatusLine;
            _statusLineClickHandler= options.StatusLineClickHandler;
            _emptyText             = options.EmptyText;
            _transientContainerPaneId = options._transientContainerPaneId;
            _transientContainerParent = options._transientContainerParent;
        }

        /// <summary>
        /// Gets or sets the caption to display for the resource list.
        /// </summary>
        public string Caption
        {
            get { return _caption; }
            set { _caption = value; }
        }

        /// <summary>
        /// Gets or sets the template of the caption to display for the resource list.
        /// The string "%OWNER%" in the template is replaced with the display name of the owner resource.
        /// </summary>
        public string CaptionTemplate
        {
            get { return _captionTemplate; }
            set { _captionTemplate = value; }
        }

        /// <summary>
        /// Gets or sets the columns to display for the resource list.
        /// </summary>
        public ColumnDescriptor[] Columns
        {
            get { return _columns; }
            set { _columns = value; }
        }

        /// <summary>
        /// Gets or sets the resource which is initially selected in the list.
        /// </summary>
        public IResource SelectedResource
        {
            get { return _selectedResource; }
            set { _selectedResource = value; }
        }

        /// <summary>
        /// Gets or sets the highlight data provider which is used to highlight found
        /// words and display contexts for search results.
        /// </summary>
        public IHighlightDataProvider HighlightDataProvider
        {
            get { return _highlightDataProvider; }
            set { _highlightDataProvider = value; }
        }

        /// <summary>
        /// Gets or sets the threading handler which is used to build a threading tree
        /// in the displayed resource list. If not set, a plain resource list is displayed.
        /// </summary>
        public IResourceThreadingHandler ThreadingHandler
        {
            get { return _threadingHandler; }
            set { _threadingHandler = value; }
        }

        /// <summary>
        /// Gets or sets the settings for sorting the resource list.
        /// </summary>
        public SortSettings SortSettings
        {
            get { return _sortSettings; }
            set { _sortSettings = value; }
        }

        /// <summary>
        /// Gets or sets the value indicating whether the resource list to display is
        /// intersected with the tab and workspace filter list.
        /// </summary>
        public bool TabFilter
        {
            get { return _tabFilter; }
            set { _tabFilter = value; }
        }

        /// <summary>
        /// Gets or sets the value indicating whether the see-also bar is displayed
        /// above the resource list.
        /// </summary>
        public bool SeeAlsoBar
        {
            get { return _seeAlsoBar; }
            set { _seeAlsoBar = value; }
        }

        /// <summary>
        /// Gets or sets the value indicating whether the contexts are not shown even if a valid
        /// highlight data provider is specified in the options.
        /// </summary>
        public bool SuppressContexts
        {
            get { return _suppressContexts; }
            set { _suppressContexts = value; }
        }

        /// <summary>
        /// Gets or sets the value indicating whether the resource list is shown as a newspaper.
        /// </summary>
        public bool ShowNewspaper
        {
            get { return _showNewspaper; }
            set { _showNewspaper = value; }
        }

        /// <summary>
        /// Gets or sets the text of the status line which is displayed above the resource list
        /// in the resource browser.
        /// </summary>
        public string StatusLine
        {
            get { return _statusLine; }
            set { _statusLine = value; }
        }

        /// <summary>
        /// Gets or sets the click handler which is called when the resource browser status line
        /// is clicked.
        /// </summary>
        public EventHandler StatusLineClickHandler
        {
            get { return _statusLineClickHandler; }
            set { _statusLineClickHandler = value; }
        }

        /// <summary>
        /// The text displayed in the resource browser when the list is empty.
        /// </summary>
        public string EmptyText
        {
            get { return _emptyText; }
            set { _emptyText = value; }
        }

        /// <summary>
        /// Returns the parent for the transient container which is selected when the resource
        /// list is displayed.
        /// </summary>
        public IResource TransientContainerParent
        {
            get { return _transientContainerParent; }
        }
        
        /// <summary>
        /// Returns the pane containing the transient container which is selected when the resource
        /// list is displayed.
        /// </summary>
        public string TransientContainerPaneId
        {
            get { return _transientContainerPaneId; }
        }

        /// <summary>
        /// Specifies that a transient container should be created for displaying the resource
        /// list, and deleted when the user switches to another view.
        /// </summary>
        /// <param name="parent">The parent for the transient container resource.</param>
        /// <param name="paneId">The ID of the pane in which the transient container resource is selected.</param>
        public void SetTransientContainer( IResource parent, string paneId )
        {
            _transientContainerParent = parent;
            _transientContainerPaneId = paneId;
        }

        /// <summary>
        /// Specifies whether item grouping is by default enabled in the view. True by default.
        /// </summary>
        public bool DefaultGroupItems
        {
            get { return _defaultGroupItems; }
            set { _defaultGroupItems = value; }
        }
    }

    /// <summary>
    /// Allows to substitute a different resource to show in the display pane when
    /// a resource is selected in the resource browser.
    /// </summary>
    public delegate IResource ResourceDisplayForwarderCallback( IResource res );

    /// <summary>
    /// Allows to specify a custom text representation for a resource property.
    /// </summary>
    public delegate string PropertyToTextCallback( IResource res, int propId );

    /// <summary>
    /// Allows to specify a custom text representation for a resource property, depending
    /// on the space available for displaying the property value in the list.
    /// </summary>
    /// <since>2.0</since>
    public delegate string PropertyToTextCallback2( IResource res, int propId, int widthInChars );
}
