// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using JetBrains.Annotations;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Flags that define the input string options.
    /// </summary>
    [Flags]
    public enum InputStringFlags
    {
        /// <summary>
        /// No special options.
        /// </summary>
        None = 0,

        /// <summary>
        /// An empty string is a valid input.
        /// </summary>
        AllowEmpty = 1
    }

    /// <summary>
    /// Allows to implement custom handling of <see cref="IUIManager.DisplayResourceInContext"/>.
    /// </summary>
    /// <since>2.0</since>
    public interface IDisplayInContextHandler
    {
        /// <summary>
        /// Displays the resource in its appropriate context in Omea.
        /// </summary>
        /// <param name="res">The resource to display in context.</param>
        void DisplayResourceInContext( IResource res );
    }

    /// <summary><see cref="Core.UIManager"/>
    /// The User Interface manager that provides access to UI elements and their settings.
    /// </summary>
    /// <remarks>This interface can be accessed through <see cref="Core.UIManager"/>.</remarks>
    public interface IUIManager
    {
        /// <summary>
        /// Gets an application icon.
        /// </summary>
        /// <since>2.1</since>
        Icon ApplicationIcon { get; }

        /// <summary><seealso cref="RegisterOptionsPane"/><seealso cref="IsOptionsGroupRegistered"/>
        /// Registers a group of options panes with accompanying text.
        /// </summary>
        /// <param name="group">Name of the options group as it will appear in the options tree and as it should be referenced in <see cref="RegisterOptionsPane"/>.</param>
        /// <param name="prompt">Description for this options group.</param>
        /// <remarks>
        /// <para>You should always register an option group before adding panes that reference it. There are no predefined option groups, and any "standard" option group is not guaranteed to exist at the moment when your plugin is registering.</para>
        /// <para>This function won't fail if a group with such a name already exists.</para>
        /// <para>To avoid rewriting the prompt of an existing group when re-registering it, use <see cref="IsOptionsGroupRegistered"/> to check if such a group has already been registered.</para>
        /// </remarks>
        void RegisterOptionsGroup( string group, string prompt );

        /// <summary><seealso cref="RegisterOptionsGroup"/>
        /// Tells whether an option group with specific name is registered or not.
        /// </summary>
        /// <param name="group"></param>
        /// <returns><code>True</code> if there is such a group or <code>False</code> if it has not been registered.</returns>
        bool IsOptionsGroupRegistered( string group );

        /// <summary><seealso cref="RegisterOptionsGroup"/><seealso cref="IPlugin"/>
        /// Registers an options pane that will be shown in the options dialog.
        /// </summary>
        /// <param name="group">Category (options tree node) to which the options page will be added.<see cref="RegisterOptionsGroup"/>.</param>
        /// <param name="header">Options pane title to be shown in the options tree and atop the options page.</param>
        /// <param name="creator">Facility that creates a new page instance as a user control derived from <see cref="AbstractOptionsPane"/> that renders the pane.</param>
        /// <param name="prompt">Description for this options page.</param>
        /// <remarks>
        /// <para>Typically, you would add options panes on plugin startup in the <see cref="IPlugin.Register"/> method. Once registered, option panes cannot be removed.</para>
        /// <para>You should always register an option group before adding panes that reference it. There are no predefined option groups, and any "standard" option group is not guaranteed to exist at the moment when your plugin is registering.</para>
        /// <para>This function won't fail if a group with such a name already exists.</para>
        /// <para>If the pane contains options, which are essential for the plugin and must be set before its first run, the pane should also be submitted to the <see cref="RegisterWizardPane"/> funciton to ensure that it comes up when the plugin is run for the first time.</para>
        /// </remarks>
        void RegisterOptionsPane( string group, string header, OptionsPaneCreator creator, string prompt );

        /// <summary><seealso cref="RegisterOptionsPane"/><seealso cref="RemoveOptionsChangesListener"/>
        /// Registers a callback which is called when any changes are made in the Options dialog pane
        /// with the specified header.
        /// </summary>
        /// <param name="group">Options group to which the desired Options dialog pane belongs.</param>
        /// <param name="header">Options pane header registered by <see cref="RegisterOptionsPane"/>.</param>
        /// <param name="handler">Handler which is called after the changes.</param>
        /// <remarks>The handler should be removed with <see cref="RemoveOptionsChangesListener"/> when it
        /// is no longer needed.</remarks>
        void AddOptionsChangesListener( string group, string header, EventHandler handler );

        /// <summary><seealso cref="RegisterOptionsPane"/><seealso cref="AddOptionsChangesListener"/>
        /// Unregisters a callback which is called when any changes are made in the Options dialog pane
        /// with the specified header.
        /// </summary>
        /// <param name="group">Options group to which the desired Options dialog pane belongs.</param>
        /// <param name="header">Options pane header registered by <see cref="RegisterOptionsPane"/>.</param>
        /// <param name="handler">Handler registered by <see cref="AddOptionsChangesListener"/>.</param>
        void RemoveOptionsChangesListener( string group, string header, EventHandler handler );

        /// <summary><seealso cref="IPlugin"/><seealso cref="RegisterOptionsPane"/><seealso cref="AbstractOptionsPane"/>
        /// Registers a plugin options pane as a Startup Wizard page.
        /// </summary>
        /// <param name="header">Header of the wizard page that will appear in the page title.</param>
        /// <param name="creator">Facility that creates a new page instance as a user control derived from <see cref="AbstractOptionsPane"/> that renders the pane.</param>
        /// <param name="order">Controls the order in which the pages appear in the startup wizard. Pages are sorted in the ascending order by this parameter. Pages that have equal <paramref name="order"/> value appear in order of submission.</param>
        /// <remarks>
        /// <para>This function accepts the same option panes as <see cref="RegisterOptionsPane"/>, which adds panes to the Options dialog hierarchy. Some of these panes contain options that are essential for the plugin and which must be set before it is run for the first time. Such panes should be added to the Startup Wizard by submitting them to <see cref="RegisterWizardPane"/>.</para>
        /// <para><since>552</since>To register subpage of a parent wizard page, construct its header as concatetation of header of parent wizard page, backslash and name of the subpage. In that case, the order parameter affects subpages of parent page.</para>
        /// <para><see cref="RegisterWizardPane"/> ensures that the particular options pane will be displayed to the user at Omea start and user will be forced to supply valid values for it to continue using Omea.</para>
        /// <para>Omea controls automatically that each page is shown no more than one time. When you supply the same page to <see cref="RegisterWizardPane"/> on the next plugin initialization, it is just skipped if it has already been shown on the previous run.</para>
        /// </remarks>
        void RegisterWizardPane( string header, OptionsPaneCreator creator, int order );

        /// <summary>
        /// DeRegisters a Startup Wizard page.
        /// </summary>
        /// <param name="header">Header of the wizard page.</param>
        void DeRegisterWizardPane( string header );

        /// <summary><seealso cref="RegisterOptionsPane"/><seealso cref="RegisterOptionsGroup"/>
        /// Displays the options dialog.
        /// </summary>
        /// <remarks>Use <see cref="RegisterOptionsPane"/> and <see cref="RegisterOptionsGroup"/> to add new option groups and panes.</remarks>
        void ShowOptionsDialog();

        /// <summary><seealso cref="RegisterOptionsPane"/><seealso cref="RegisterOptionsGroup"/>
        /// Displays the options dialog opened at a specific options pane.
        /// </summary>
        /// <param name="group">Options group to which the desired pane belongs.</param>
        /// <param name="paneHeader">Header (name) of the options pane to be opened.</param>
        /// <remarks>Option groups and panes can be added with <see cref="RegisterOptionsGroup"/> and <see cref="RegisterOptionsPane"/>, respectively.</remarks>
        void ShowOptionsDialog( string group, string paneHeader );

        void ShowSimpleMessageBox( string header, string message );

        /// <summary><seealso cref="DeRegisterIndicatorLight"/>
        /// Adds a status bar indicator light.
        /// </summary>
        /// <param name="name">The name which is used to identify this indicator light.</param>
        /// <param name="processor">The asynchronous processor doing the job that is being indicated.</param>
        /// <param name="stuckTimeout">A timeout, in seconds, after which the job is considered stuck.</param>
        /// <remarks>
        /// <para>Use <see cref="DeRegisterIndicatorLight"/> to remove an indicator light.</para>
        /// </remarks>
        void RegisterIndicatorLight( string name, IAsyncProcessor processor, int stuckTimeout );

        /// <summary><seealso cref="DeRegisterIndicatorLight"/>
        /// Adds a status bar indicator light.
        /// </summary>
        /// <param name="name">The name which is used to identify this indicator light.</param>
        /// <param name="processor">The asynchronous processor doing the job that is being indicated.</param>
        /// <param name="stuckTimeout">A timeout, in seconds, after which the job is considered stuck.</param>
        /// <param name="icons">Array of icons displayed in the following states of asynchronous processor: idle, busy and stuck.</param>
        /// <remarks>
        /// <para>Use <see cref="DeRegisterIndicatorLight"/> to remove an indicator light.</para>
        /// </remarks>
        /// <since>2.0</since>
        void RegisterIndicatorLight( string name, IAsyncProcessor processor, int stuckTimeout, params Icon[] icons );

        /// <summary><seealso cref="RegisterIndicatorLight"/>
        /// Removes a status bar indicator light.
        /// </summary>
        /// <param name="name">The name which is used to identify this indicator light and which was passed to <see cref="RegisterIndicatorLight"/>.</param>
        /// <remarks>
        /// <para>Use <see cref="RegisterIndicatorLight"/> to add an indicator light.</para>
        /// </remarks>
        void DeRegisterIndicatorLight( string name );

        /// <summary>
        /// Shows the dialog to select a single resource of the specified type.
        /// </summary>
        /// <param name="type">The type of resource to select.</param>
        /// <param name="dialogCaption">The caption of the dialog.</param>
        /// <returns>The selected resource, or null if the dialog was cancelled.</returns>
        IResource SelectResource( string type, string dialogCaption );

        /// <summary>
        /// Shows the dialog to select a single resource of the specified type, with specified
        /// initial selection.
        /// </summary>
        /// <param name="type">The type of resource to select.</param>
        /// <param name="dialogCaption">The caption of the dialog.</param>
        /// <param name="initialSelection">The resource which is initially selected in the dialog.</param>
        /// <returns>The selected resource, or null if the dialog was cancelled.</returns>
        IResource SelectResource( string type, string dialogCaption, IResource initialSelection );

        /// <summary>
        /// Shows the dialog to select a single resource of the specified type, with specified dialog
        /// owner window.
        /// </summary>
        /// <param name="ownerWindow">The owner window of the dialog.</param>
        /// <param name="type">The type of resource to select.</param>
        /// <param name="dialogCaption">The caption of the dialog.</param>
        /// <returns>The selected resource, or null if the dialog was cancelled.</returns>
        IResource SelectResource( IWin32Window ownerWindow, string type, string dialogCaption );

        /// <summary>
        /// Shows the dialog to select a single resource of the specified type, with specified dialog
        /// owner window and initial selection.
        /// </summary>
        /// <param name="ownerWindow">The owner window of the dialog.</param>
        /// <param name="type">The type of resource to select.</param>
        /// <param name="dialogCaption">The caption of the dialog.</param>
        /// <param name="initialSelection">The resource which is initially selected in the dialog.</param>
        /// <returns>The selected resource, or null if the dialog was cancelled.</returns>
        IResource SelectResource( IWin32Window ownerWindow, string type, string dialogCaption,
            IResource initialSelection );

        /// <summary>
        /// Shows the dialog to select a single resource of the specified type, with specified dialog
        /// owner window, initial selection and a "Help" button.
        /// </summary>
        /// <param name="ownerWindow">The owner window of the dialog.</param>
        /// <param name="type">The type of resource to select.</param>
        /// <param name="dialogCaption">The caption of the dialog.</param>
        /// <param name="initialSelection">The resource which is initially selected in the dialog.</param>
        /// <param name="helpTopic">The help topic which is displayed when the "Help" button is pressed.</param>
        /// <returns>The selected resource, or null if the dialog was cancelled.</returns>
        IResource SelectResource( IWin32Window ownerWindow, string type, string dialogCaption,
            IResource initialSelection, string helpTopic );

        /// <summary>
        /// Shows the dialog to select multiple resources of the specified type.
        /// </summary>
        /// <param name="type">The type of resources to select.</param>
        /// <param name="dialogCaption">The caption of the dialog.</param>
        /// <returns>The selected resources, or null if the dialog was cancelled.</returns>
        IResourceList SelectResources( string type, string dialogCaption );

        /// <summary>
        /// Shows the dialog to select multiple resources of the specified type, with specified
        /// initial selection.
        /// </summary>
        /// <param name="type">The type of resources to select.</param>
        /// <param name="dialogCaption">The caption of the dialog.</param>
        /// <param name="initialSelection">The resources which are initially selected in the dialog.</param>
        /// <returns>The selected resources, or null if the dialog was cancelled.</returns>
        IResourceList SelectResources( string type, string dialogCaption, IResourceList initialSelection );

        /// <summary>
        /// Shows the dialog to select multiple resources of any of the specified types, with specified
        /// initial selection.
        /// </summary>
        /// <param name="types">The types of resources to select.</param>
        /// <param name="dialogCaption">The caption of the dialog.</param>
        /// <param name="initialSelection">The resources which are initially selected in the dialog.</param>
        /// <returns>The selected resources, or null if the dialog was cancelled.</returns>
        IResourceList SelectResources( string[] types, string dialogCaption, IResourceList initialSelection );

        /// <summary>
        /// Shows the dialog to select multiple resources from the specified list.
        /// </summary>
        /// <param name="resList">The list from which resources are selected.</param>
        /// <param name="dialogCaption">The caption of the dialog.</param>
        /// <returns>The selected resources, or null if the dialog was cancelled.</returns>
        IResourceList SelectResourcesFromList( IResourceList resList, string dialogCaption );

        /// <summary>
        /// Shows the dialog to select multiple resources of the specified type, with specified
        /// dialog owner window.
        /// </summary>
        /// <param name="ownerWindow">The owner window of the dialog.</param>
        /// <param name="type">The type of resources to select.</param>
        /// <param name="dialogCaption">The caption of the dialog.</param>
        /// <returns>The selected resources, or null if the dialog was cancelled.</returns>
        IResourceList SelectResources( IWin32Window ownerWindow, string type, string dialogCaption );

        /// <summary>
        /// Shows the dialog to select multiple resources of the specified type, with specified
        /// dialog owner window and initial selection.
        /// </summary>
        /// <param name="ownerWindow">The owner window of the dialog.</param>
        /// <param name="type">The type of resources to select.</param>
        /// <param name="dialogCaption">The caption of the dialog.</param>
        /// <param name="initialSelection">The resources which are initially selected in the dialog.</param>
        /// <returns>The selected resources, or null if the dialog was cancelled.</returns>
        IResourceList SelectResources( IWin32Window ownerWindow, string type, string dialogCaption,
            IResourceList initialSelection );

        /// <summary>
        /// Shows the dialog to select multiple resources of any of the specified types, with specified
        /// dialog owner window and initial selection.
        /// </summary>
        /// <param name="ownerWindow">The owner window of the dialog.</param>
        /// <param name="types">The types of resources to select.</param>
        /// <param name="dialogCaption">The caption of the dialog.</param>
        /// <param name="initialSelection">The resources which are initially selected in the dialog.</param>
        /// <returns>The selected resources, or null if the dialog was cancelled.</returns>
        IResourceList SelectResources( IWin32Window ownerWindow, string[] types, string dialogCaption,
            IResourceList initialSelection );

        /// <summary>
        /// Shows the dialog to select multiple resources from the specified list, with specified
        /// dialog owner window.
        /// </summary>
        /// <param name="ownerWindow">The owner window of the dialog.</param>
        /// <param name="resList">The list from which resources are selected.</param>
        /// <param name="dialogCaption">The caption of the dialog.</param>
        /// <returns>The selected resources, or null if the dialog was cancelled.</returns>
        IResourceList SelectResourcesFromList( IWin32Window ownerWindow, IResourceList resList, string dialogCaption );

        /// <summary>
        /// Shows the dialog to select multiple resources from the specified list, with specified
        /// dialog owner window.
        /// </summary>
        /// <param name="ownerWindow">The owner window of the dialog.</param>
        /// <param name="resList">The list from which resources are selected.</param>
        /// <param name="dialogCaption">The caption of the dialog.</param>
        /// <param name="initialSelection">The resources which are initially selected in the dialog.</param>
        /// <returns>The selected resources, or null if the dialog was cancelled.</returns>
        /// <since>2.0</since>
        IResourceList SelectResourcesFromList( IWin32Window ownerWindow, IResourceList resList,
            string dialogCaption, IResourceList initialSelection );

        /// <summary>
        /// Shows the dialog to select multiple resources of the specified type, with specified
        /// dialog owner window, initial selection and a "Help" button.
        /// </summary>
        /// <param name="ownerWindow">The owner window of the dialog.</param>
        /// <param name="type">The type of resources to select.</param>
        /// <param name="dialogCaption">The caption of the dialog.</param>
        /// <param name="initialSelection">The resources which are initially selected in the dialog.</param>
        /// <param name="helpTopic">The help topic which is displayed when the "Help" button is pressed.</param>
        /// <returns>The selected resources, or null if the dialog was cancelled.</returns>
        IResourceList SelectResources( IWin32Window ownerWindow, string type, string dialogCaption,
            IResourceList initialSelection, string helpTopic );

        /// <summary>
        /// Shows the dialog to select multiple resources of any of the specified types, with specified
        /// dialog owner window, initial selection and a "Help" button.
        /// </summary>
        /// <param name="ownerWindow">The owner window of the dialog.</param>
        /// <param name="types">The types of resources to select.</param>
        /// <param name="dialogCaption">The caption of the dialog.</param>
        /// <param name="initialSelection">The resources which are initially selected in the dialog.</param>
        /// <param name="helpTopic">The help topic which is displayed when the "Help" button is pressed.</param>
        /// <returns>The selected resources, or null if the dialog was cancelled.</returns>
        IResourceList SelectResources( IWin32Window ownerWindow, string[] types, string dialogCaption,
            IResourceList initialSelection, string helpTopic );

        /// <summary>
        /// Shows the dialog to select multiple resources from the specified list, with specified
        /// dialog owner window, initial selection and a "Help" button.
        /// </summary>
        /// <param name="ownerWindow">The owner window of the dialog.</param>
        /// <param name="resList">The list from which resources are selected.</param>
        /// <param name="dialogCaption">The caption of the dialog.</param>
        /// <param name="helpTopic">The help topic which is displayed when the "Help" button is pressed.</param>
        /// <returns>The selected resources, or null if the dialog was cancelled.</returns>
        IResourceList SelectResourcesFromList( IWin32Window ownerWindow, IResourceList resList,
            string dialogCaption, string helpTopic );

        /// <summary>
        /// Displays a dialog for establishing a link between two resources.
        /// </summary>
        /// <param name="res1">The first resource to link.</param>
        /// <param name="res2">The second resource to link.</param>
        void ShowAddLinkDialog( IResource res1, IResource res2 );

        /// <summary>
        /// Displays a dialog for establishing a link between two or more resources (many-to-one).
        /// </summary>
        /// <param name="sourceList">One or more objects at the left side.</param>
        /// <param name="target">One object at the right side.</param>
        void ShowAddLinkDialog( IResourceList sourceList, IResource target );

        /// <summary>
        /// Displays a dialog for establishing a link between two resources.
        /// </summary>
        /// <param name="ownerWindow">The window owning the dialog.</param>
        /// <param name="res1">The first resource to link.</param>
        /// <param name="res2">The second resource to link.</param>
        /// <since>2.0</since>
        void ShowAddLinkDialog( IWin32Window ownerWindow, IResource res1, IResource res2 );

        /// <summary>
        /// Displays a dialog for establishing a link between two or more resources (many-to-one).
        /// </summary>
        /// <param name="ownerWindow">The window owning the dialog.</param>
        /// <param name="sourceList">One or more objects at the left side.</param>
        /// <param name="target">One object at the right side.</param>
        /// <since>2.0</since>
        void ShowAddLinkDialog( IWin32Window ownerWindow, IResourceList sourceList, IResource target );

        /// <summary>
        /// Shows the dialog for creating a new category.
        /// </summary>
        /// <param name="defaultName">The default name for the new category</param>
        /// <param name="defaultParent">The default parent category for the new category </param>
        /// <param name="defaultContentType">The default content type for the new category</param>
        /// <returns>The created category, or null if the user cancelled the dialog.</returns>
        IResource ShowNewCategoryDialog( string defaultName, IResource defaultParent, string defaultContentType );

        /// <summary>
        /// Shows the dialog for creating a new category with the specified owner window.
        /// </summary>
        /// <param name="ownerWindow">The owner window for the dialog.</param>
        /// <param name="defaultName">The default name for the new category</param>
        /// <param name="defaultParent">The default parent category for the new category </param>
        /// <param name="defaultContentType">The default content type for the new category</param>
        /// <returns>The created category, or null if the user cancelled the dialog.</returns>
        /// <since>2.0</since>
        IResource ShowNewCategoryDialog( IWin32Window ownerWindow, string defaultName, IResource defaultParent,
            string defaultContentType );

        /// <summary>
        /// Shows the "Assign Categories" dialog for the specified list of resources.
        /// </summary>
        /// <comment>Inportant! This dialog has the side-effect - it assigns the selected categories
        /// on successful closing. If the edited resource(s) is not completed this may violate
        /// the internal logic of some plugins (e.g. Outlook plugin which starts synchronization
        /// process with Outlook).</comment>
        /// <param name="ownerWindow">The owner window for the dialog.</param>
        /// <param name="resources">The resources for which the categories are assigned.</param>
        /// <returns>The result of the dialog (OK or Cancel).</returns>
        /// <since>2.0</since>
        DialogResult ShowAssignCategoriesDialog( IWin32Window ownerWindow, IResourceList resources );

        /// <summary>
        /// Shows the "Assign Categories" dialog for the specified list of categories.
        /// </summary>
        /// <comment>Inportant! This dialog has the side-effect - it assigns the selected categories
        /// on successful closing. If the edited resource(s) is not completed this may violate
        /// the internal logic of some plugins (e.g. Outlook plugin which starts synchronization
        /// process with Outlook).</comment>
        /// <param name="ownerWindow">The owner window for the dialog.</param>
        /// <param name="resources">The resources for which the categories are assigned. Used only for
        /// filling the textual information and creation of content type filters.</param>
        /// <param name="currCategories">The list of category resources which are preselected in the dialog.</param>
        /// <param name="resultCategories">The list of category resources which are checked upon successful dialog exit.</param>
        /// <returns>The result of the dialog (OK or Cancel).</returns>
        /// <since>2.1.5</since>
        DialogResult ShowAssignCategoriesDialog( IWin32Window ownerWindow, IResourceList resources,
                                                 IResourceList currCategories, out IResourceList resultCategories );

        /// <summary>
        /// Displays the resource in its appropriate context in Omea.
        /// </summary>
        /// <param name="res">The resource to display.</param>
        /// <remarks><para>To display the resource in context, the system tries to select its location
        /// resource in a sidebar pane and then select the resource in the list of resources which
        /// is displayed when the location resource is selected. For example, to display a news
        /// article in context, the system switches to the News tab, activates the Newsgroups
        /// sidebar pane (which is registered as the resource structure pane for that tab), tries
        /// to select the newsgroup linked to the article in the Newsgroups pane, and then tries
        /// to select the article in the list of newsgroup articles.</para>
        /// <para>By default, if the resource is present in the list of resources currently displayed
        /// in the resource browser, the system selects it there and does not perform any tab or
        /// sidebar pane switches. To override this behavior, use <see cref="DisplayResourceInContext(IResource, bool)"/>.
        /// </para></remarks>
        void DisplayResourceInContext( IResource res );

        /// <summary>
        /// Displays the resource in its appropriate context in Omea, optionally performing a navigation even
        /// if the resource is visible in the currently displayed list.
        /// </summary>
        /// <param name="res">The resource to display.</param>
        /// <param name="skipCurrentList">If true, the system performs a navigation even if the
        /// resource is visible in the currently displayed list.</param>
        /// <remarks><para>To display the resource in context, the system tries to select its location
        /// resource in a sidebar pane and then select the resource in the list of resources which
        /// is displayed when the location resource is selected. For example, to display a news
        /// article in context, the system switches to the News tab, activates the Newsgroups
        /// sidebar pane (which is registered as the resource structure pane for that tab), tries
        /// to select the newsgroup linked to the article in the Newsgroups pane, and then tries
        /// to select the article in the list of newsgroup articles.</para></remarks>
        void DisplayResourceInContext( IResource res, bool skipCurrentList );

        /// <summary>
        /// Registers the specified custom handler for <see cref="DisplayResourceInContext"/>.
        /// </summary>
        /// <param name="resType">The resource type for which the handler is registered.</param>
        /// <param name="handler">The handler implementation.</param>
        /// <since>2.0</since>
        void RegisterDisplayInContextHandler( string resType, IDisplayInContextHandler handler );

        /// <summary>
        /// Registers the link type between a resource and its location resource, used to display
        /// the resource in context.
        /// <seealso cref="DisplayResourceInContext(IResource)"/>
        /// <seealso cref="GetResourcesInLocation(IResource)"/>
        /// </summary>
        /// <param name="resType">The resource type for which the location link is registered.</param>
        /// <param name="propId">The ID of a link property type which links the resource to its location.</param>
        /// <param name="locationResType">The type of the location resource.</param>
        void RegisterResourceLocationLink( string resType, int propId, string locationResType );

        /// <summary>
        /// Registers the default location resource for the specified resource type.
        /// <seealso cref="DisplayResourceInContext(IResource)"/>
        /// </summary>
        /// <param name="resType">The resource type for which the default location is registered.</param>
        /// <param name="location">The location resource.</param>
        /// <remarks>The default location is used when there is no inherent container or location for
        /// resources of a specified type. For example, tasks are not organized in any structure by
        /// default. Thus, the "All Tasks" view is registered as the default location for the Task
        /// resources. When the system is requested to display a task in context, it switches to
        /// the Tasks tab, selects the All Tasks view in the sidebar and selects the task in the list.</remarks>
        void RegisterResourceDefaultLocation( string resType, IResource location );

        /// <summary>
        /// Returns the list of resources in the specified resource location,
        /// </summary>
        /// <param name="location">The location resource.</param>
        /// <returns>The resources in the specified location, or an empty list if it was not
        /// possible to determine the list of resources in the location.</returns>
        /// <since>2.0</since>
        IResourceList GetResourcesInLocation( IResource location );

        /// <summary>
        /// Calls the <see cref="IResourceUIHandler"/> to determine if the specified resources
        /// can be dropped on the specified target resource.
        /// </summary>
        /// <param name="targetRes">The drop target resource.</param>
        /// <param name="dropList">The dragged resources.</param>
        /// <returns>true if the drop is allowed, false otherwise.</returns>
        bool CanDropResource( IResource targetRes, IResourceList dropList );

        /// <summary>
        /// Processes the drop of the specified resources on the specified target resource.
        /// </summary>
        /// <param name="targetRes">The drop target resource.</param>
        /// <param name="dropList">The dragged resources.</param>
        /// <remarks>If there is no <see cref="IResourceUIHandler"/> registered
        /// for the drop target resource, the drop is processed as adding a custom
        /// link between the dragged resources and the drop target resource.</remarks>
        void ProcessResourceDrop( IResource targetRes, IResourceList dropList );

        /// <summary>
        /// Processes the drag of the specified data object over the specified target resource.
        /// </summary>
        /// <param name="targetRes">The resource over which the drag happens.</param>
        /// <param name="data">The dragged data object.</param>
        /// <param name="allowedEffect">The drag/drop effects allowed by the drag source.</param>
        /// <param name="keyState">The keyboard state.</param>
        /// <param name="sameView">If true, the drag was started from the same view over which the
        /// resource is currently being dragged. If false, the drag was started from a different view.</param>
        /// <returns>The drag/drop effects to display.</returns>
        /// <since>2.0</since>
        DragDropEffects ProcessDragOver( IResource targetRes, IDataObject data, DragDropEffects allowedEffect,
            int keyState, bool sameView );

        /// <summary>
        /// Processes the drop of the specified data object over the specified target resource.
        /// </summary>
        /// <param name="targetRes">The resource over which the drag happens.</param>
        /// <param name="data">The dragged data object.</param>
        /// <param name="allowedEffect">The drag/drop effects allowed by the drag source.</param>
        /// <param name="keyState">The keyboard state.</param>
        /// <since>2.0</since>
        void ProcessDragDrop( IResource targetRes, IDataObject data, DragDropEffects allowedEffect, int keyState );

        /// <summary>
        /// Opens a window with the specified edit pane for editing the specified resource.
        /// </summary>
        /// <param name="editPane">The resource edit pane which is displayed in the window.</param>
        /// <param name="res">The resource to edit.</param>
        /// <param name="newResource">If true, the resource is deleted when the editing is cancelled.</param>
        void OpenResourceEditWindow( AbstractEditPane editPane, IResource res, bool newResource );

        /// <summary>
        /// Opens a window with the specified edit pane for editing the specified resource
        /// and fires a callback when the resource is saved.
        /// </summary>
        /// <param name="editPane">The resource edit pane which is displayed in the window.</param>
        /// <param name="res">The resource to edit.</param>
        /// <param name="newResource">If true, the resource is deleted when the editing is cancelled.</param>
        /// <param name="savedDelegate">The callback which is fired when the resource is saved.</param>
        /// <param name="savedDelegateTag">The additional data passed to the callback.</param>
        void OpenResourceEditWindow( AbstractEditPane editPane, IResource res, bool newResource,
            EditedResourceSavedDelegate savedDelegate, object savedDelegateTag );

        /// <summary>
        /// Registers the class which is used as the select pane class for selecting resources
        /// of the specified type.
        /// </summary>
        /// <param name="resType">The resource type for which the select pane class is registered.</param>
        /// <param name="resourceSelectPaneType">The type of the select pane class.</param>
        /// <remarks>The select pane class must implement the <see cref="IResourceSelectPane"/> interface.</remarks>
        void RegisterResourceSelectPane( string resType, Type resourceSelectPaneType );

        /// <summary>
        /// Returns the class of the select pane which has been registered for the specified type.
        /// </summary>
        /// <param name="resType">The resource type.</param>
        /// <returns>The select pane class, or null if no select pane type has been registered
        /// for the class.</returns>
        /// <since>2.0</since>
        Type GetResourceSelectPaneType( string resType );

        /// <summary>
        /// Creates an instance of the resource select pane for the specified resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the select pane is created.</param>
        /// <returns>The select pane instance.</returns>
        /// <remarks>If no select pane class was registered through <see cref="RegisterResourceSelectPane"/>,
        /// the default list-based select pane implementation is used.</remarks>
        IResourceSelectPane CreateResourceSelectPane( string resType );

        /// <summary>
        /// Begins a batch navigation operation.
        /// <seealso cref="EndUpdateSidebar"/>
        /// <seealso cref="IsSidebarUpdating"/>
        /// </summary>
        /// <remarks>During batch navigation operations, tab and sidebar pane switches do not cause
        /// redisplaying of resource lists in the resource browser.</remarks>
        void BeginUpdateSidebar();

        /// <summary>
        /// Ends a batch navigation operation. During batch navigation operations, tab and sidebar
        /// pane switches do not cause redisplaying of resource lists in the resource browser.
        /// <seealso cref="BeginUpdateSidebar"/>
        /// <seealso cref="IsSidebarUpdating"/>
        /// </summary>
        /// <remarks><para>During batch navigation operations, tab and sidebar pane switches do not cause
        /// redisplaying of resource lists in the resource browser.</para>
        /// <para>Note that ending a batch navigation operation does not cause an immediate
        /// update of the resource browser. The resource browser must be updated explicitly,
        /// for example, by selecting a resource in a sidebar pane.</para></remarks>
        void EndUpdateSidebar();

        /// <summary>
        /// Checks if a batch navigation operation is currently in progress.
        /// <seealso cref="BeginUpdateSidebar"/>
        /// <seealso cref="EndUpdateSidebar"/>
        /// </summary>
        /// <returns>true if a batch navigation operation is in progress, false otherwise.</returns>
        /// <remarks>During batch navigation operations, tab and sidebar pane switches do not cause
        /// redisplaying of resource lists in the resource browser.</remarks>
        bool IsSidebarUpdating();

        /// <summary>
        /// Creates or returns a status writer instance for displaying text in the status bar.
        /// </summary>
        /// <param name="owner">The object which owns the status writer.</param>
        /// <param name="pane">The status bar pane in which the status writer displays the data.</param>
        /// <returns>The status writer instance.</returns>
        /// <remarks><para>If the status writer for the same owner is requested multiple times,
        /// the same instance is returned.</para>
        /// <para>When the status writer is no longer needed, the <see cref="IStatusWriter.ClearStatus"/>
        /// method must be called.</para>
        /// </remarks>
        IStatusWriter GetStatusWriter( object owner, StatusPane pane );

        /// <summary>
        /// Queues a job that is to be executed asynchronously in the UI thread.
        /// </summary>
        /// <param name="method">A delegate to the method that will be called on the UI thread to perform the action.</param>
        /// <param name="args">Arguments that will be passed to the method represented by <paramref name="method"/></param>
        /// <remarks>You may use <see cref="QueueUIJob"/> if you need to execure an UI action from the resource thread.</remarks>
        void QueueUIJob( Delegate method, params object[] args );

        /// <summary>
        /// Queues a job that is to be executed asynchronously in the UI thread.
        /// </summary>
        /// <param name="action">The callback which will be called on the UI thread to perform the action.</param>
        /// <remarks>You may use <see cref="QueueUIJob"/> if you need to execure an UI action from the resource thread.</remarks>
        void QueueUIJob([NotNull] Action action);

        /// <summary>
        /// Shows the progress window and runs the specified delegate while the progress window
        /// is visible.
        /// </summary>
        /// <param name="progressTitle">Title of the progress window.</param>
        /// <param name="action">The method which is executed.</param>
        void RunWithProgressWindow([NotNull] string progressTitle, [NotNull] Action action );

    	/// <summary>
        /// Shows the dialog prompting a user to enter a string.
        /// </summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="prompt">The prompt displayed above the edit box in the dialog.</param>
        /// <param name="initialValue">The value initially displayed in the edit box, or null
        /// if the edit box is initially empty.</param>
        /// <param name="validateDelegate">The callback to validate the value entered by the
        /// user, or null if validation is not required.</param>
        /// <param name="ownerWindow">The window owning the dialog, or null if the dialog is
        /// owned by the main Omea window.</param>
        /// <returns>The string entered by the user, or null if the dialog was cancelled.</returns>
        string InputString( string title, string prompt, string initialValue,
            ValidateStringDelegate validateDelegate, IWin32Window ownerWindow );

        /// <summary>
        /// Shows the dialog prompting a user to enter a string, with possibility to specify extra options.
        /// </summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="prompt">The prompt displayed above the edit box in the dialog.</param>
        /// <param name="initialValue">The value initially displayed in the edit box, or null
        /// if the edit box is initially empty.</param>
        /// <param name="validateDelegate">The callback to validate the value entered by the
        /// user, or null if validation is not required.</param>
        /// <param name="ownerWindow">The window owning the dialog, or null if the dialog is
        /// owned by the main Omea window.</param>
        /// <param name="flags">Additional options for the dialog.</param>
        /// <returns>The string entered by the user, or null if the dialog was cancelled.</returns>
        string InputString( string title, string prompt, string initialValue,
            ValidateStringDelegate validateDelegate, IWin32Window ownerWindow, InputStringFlags flags );

        /// <summary>
        /// Shows the dialog prompting a user to enter a string, with possibility to specify
        /// extra options and a Help button.
        /// </summary>
        /// <param name="title">The title of the dialog.</param>
        /// <param name="prompt">The prompt displayed above the edit box in the dialog.</param>
        /// <param name="initialValue">The value initially displayed in the edit box, or null
        /// if the edit box is initially empty.</param>
        /// <param name="validateDelegate">The callback to validate the value entered by the
        /// user, or null if validation is not required.</param>
        /// <param name="ownerWindow">The window owning the dialog, or null if the dialog is
        /// owned by the main Omea window.</param>
        /// <param name="flags">Additional options for the dialog.</param>
        /// <param name="helpTopic">The name of the help topic which is displayed when the
        /// user presses the "Help" button.</param>
        /// <returns>The string entered by the user, or null if the dialog was cancelled.</returns>
        string InputString( string title, string prompt, string initialValue,
            ValidateStringDelegate validateDelegate, IWin32Window ownerWindow, InputStringFlags flags,
            string helpTopic );

        /// <summary>
        /// Shows a desktop alert displaying the specified resource.
        /// </summary>
        /// <param name="res">The resource to show in the desktop alert.</param>
        /// <since>2.0</since>
        void ShowDesktopAlert( IResource res );

        /// <summary>
        /// Shows a desktop alert displaying the specified data.
        /// </summary>
        /// <param name="imageList">The image list from which the desktop alert icon is taken.</param>
        /// <param name="imageIndex">The index of the desktop alert icon in the image list.</param>
        /// <param name="from">The from name displayed in the desktop alert.</param>
        /// <param name="subject">The subject displayed in the desktop alert.</param>
        /// <param name="body">The body displayed in the desktop alert.</param>
        /// <param name="clickHandler">The handler which is called when the link in the desktop
        /// alert is clicked.</param>
        /// <since>2.0</since>
        void ShowDesktopAlert( ImageList imageList, int imageIndex, string from, string subject, string body,
            EventHandler clickHandler );

        /// <summary>
        /// Whether the sidebar at the left side of the screen is collapsed.
        /// </summary>
        bool LeftSidebarExpanded { get; set; }

        /// <summary>
        /// Whether the sidebar at the right side of the screen is collapsed.
        /// </summary>
        bool RightSidebarExpanded { get; set; }

        /// <summary>
        /// Whether the shortcut bar is visible or hidden.
        /// </summary>
        bool ShortcutBarVisible { get; set; }

        /// <summary>
        /// Whether the workspace bar is visible or hidden.
        /// </summary>
        bool WorkspaceBarVisible { get; set; }

        /// <summary>
        /// If the Omea window is minimized or hidden to the system tray, makes it visible
        /// and active.
        /// </summary>
        void RestoreMainWindow();

        /// <summary>
        /// Closes the main Omea window.
        /// </summary>
        /// <since>2.0</since>
        void CloseMainWindow();

        /// <summary>
        /// Adds a shortcut to the specified resource to the Shortcut Bar.
        /// </summary>
        /// <param name="res">The resource to which the shortcut is created.</param>
        void CreateShortcutToResource( IResource res );

        /// <summary>
        /// Fired when the application becomes idle after a resource operation or
        /// an input event from the user. Unlike Application.Idle, this event is not
        /// fired in a loop all the time the application is idle.
        /// </summary>
        event EventHandler EnterIdle;

        /// <summary>
        /// Fired when the main window receives the WM_EXITMENULOOP message.
        /// </summary>
        event EventHandler ExitMenuLoop;

        /// <summary>
        /// Fired before the Omea main window is closed. Setting Cancel to true allows
        /// you to prevent closing Omea.
        /// </summary>
        /// <since>1.0.2</since>
        event CancelEventHandler MainWindowClosing;

        /// <summary>
        /// Gets the name and path of the Omea .CHM help file.
        /// </summary>
        string HelpFileName { get; }

        /// <summary>
        /// Gets or sets the default font for all formatted output in the
        /// classes implementing IDisplayPane.
        /// </summary>
        /// <since>2.1</since>
        Font DefaultFormattingFont { get; set; }

        /// <summary>
        /// Gets the face name of the default font.
        /// </summary>
        /// <since>2.1</since>
        string  DefaultFontFace  { get; }

        /// <summary>
        /// Gets the size of the default font.
        /// </summary>
        /// <since>2.1</since>
        float   DefaultFontSize  { get; }

		/// <summary>
		/// Opens the given URI in a new window of an external browser.
		/// </summary>
		/// <param name="uri">URI of the document to open in the new window. This may be a Web page address, a file pathname, etc.</param>
		/// <since>2.0</since>
		/// <remarks>
		/// <para>This function always opens the resource in a new window of the external browser.
		/// Omea options regarding opening of the links in the internal browser are ignored.
		/// The browser is encouraged to open a new window for the link,
		/// regardless of the browser's settings for reusing the existing window for opening the new links.</para>
		/// <para>It depends on Omea settings whether the new Web page is opened thru DDE or Shell Run. The default is to use the DDE.</para>
		/// </remarks>
		/// <example>
		/// <code>Core.UIManager.OpenInNewBrowserWindow("http://www.jetbrains.com/omea");</code>
		/// </example>
		void OpenInNewBrowserWindow( string uri);

		/// <summary>
		/// Opens the given URI in a new window of an external browser.
		/// </summary>
		/// <param name="uri">URI of the document to open in the new window. This may be a Web page address, a file pathname, etc.</param>
		/// <param name="advanced">Specifies whether the function is allowed to use the advanced techiques when opening the resource in a new browser window. For the short <see cref="OpenInNewBrowserWindow(string)"/> version, this parameter is assumed to be <c>True</c>.</param>
		/// <since>2.0</since>
		/// <remarks>
		/// <para>This function always opens the resource in a new window of the external browser.
		/// Omea options regarding opening of the links in the internal browser are ignored.
		/// If the <paramref name="advanced"/> parameter is set to <c>True</c>, then the browser is encouraged to open a new window for the link, regardless of the browser's settings for reusing the existing window for opening the new links.</para>
		/// If the <paramref name="advanced"/> is <c>False</c>, then the Shell Run is used for opening the link, and it depends on the browser settings whether a new window will be opened for the link, or not.
		/// </remarks>
		/// <example>
		/// <code>
		/// Core.UIManager.OpenInNewBrowserWindow("http://www.jetbrains.com/omea", true);	// Recommended usage
		/// Core.UIManager.OpenInNewBrowserWindow("http://www.jetbrains.com/omea", false);
		/// </code>
		/// </example>
		void OpenInNewBrowserWindow( string uri, bool advanced) ;
    }
}
