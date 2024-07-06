// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace JetBrains.Omea.OpenAPI
{
	/// <summary>
    /// Specifies a format of a plain or rich text string.
    /// </summary>
    public enum TextFormat
    {
        /// <summary>
        /// The string contains plain text.
        /// </summary>
        PlainText,

        /// <summary>
        /// The string is a valid HTML fragment.
        /// </summary>
        Html,

        /// <summary>
        /// The string contains rich text in the RTF format.
        /// </summary>
        Rtf
    };

    // -- Interfaces that are implemented by plugins -----------------------------------

    /// <summary>
    /// The main interface that must be implemented by every Omea plugin. Manages
    /// plugin startup and shutdown.
    /// </summary>
    /// <remarks>If there are several classes which implement IPlugin in an assembly,
    /// all of them are loaded as plugins.</remarks>
    public interface IPlugin
    {
        /// <summary>
        /// Registers the plugin resource types, actions and other services.
        /// </summary>
        /// <remarks><para>This is the first method called after the plugin is loaded. It should
        /// be used to register any resource types or services that could be used by other plugins.</para>
        /// <para>To access the services provided by the core, methods of the static class
        /// <see cref="Core"/> can be used. All core services are already available when this
        /// method is called.</para>
        /// </remarks>
        void Register();

        /// <summary>
        /// Performs the longer initialization activities of the plugin and starts up
        /// background activities, if any are necessary.
        /// </summary>
        /// <remarks><para>This is the second method called in the plugin startup sequence.
        /// It is called after the <see cref="Register"/> method has already been called for
        /// all plugins, so the code in this method can use the services provided by other
        /// plugins.</para>
        /// <para>To access the services provided by the core, methods of the static class
        /// <see cref="Core"/> can be used. All core services are already available when this
        /// method is called.</para>
        /// </remarks>
        void Startup();

        /// <summary>
        /// Terminates the plugin.
        /// </summary>
        /// <remarks>If the plugin needs any shutdown activities (like deleting temporary
        /// files), these should be performed in these method. All <see cref="Core"/> services
        /// are still available when the method is called.</remarks>
        void Shutdown();
    }

    /// <summary>
    /// The interface which must be implemented by plugins which display their resources
    /// in the resource browser preview pane.
    /// </summary>
    /// <remarks>Implementations of this interface are registered through
    /// <see cref="IPluginLoader.RegisterResourceDisplayer"/>.</remarks>
    public interface IResourceDisplayer
    {
        /// <summary>
        /// Creates a display pane for displaying resources of a specific type.
        /// </summary>
        /// <param name="resourceType">The type of resources to be displayed.</param>
        /// <returns>The instance of the display pane, or null if displaying resources of
        /// that type is not supported.</returns>
        /// <remarks>The instance of the display pane is used while the user is viewing
        /// resources of the same type and disposed when the user switches to a resource
        /// of different type.</remarks>
        IDisplayPane CreateDisplayPane( string resourceType );
    }

    /// <summary>
    /// Allows a component to execute commands related to its current state.
    /// </summary>
    /// <remarks>IDs of some standard commands are defined in the <see cref="DisplayPaneCommands"/>
    /// class. Additional command IDs can be supported by different components.</remarks>
    public interface ICommandProcessor
    {
        /// <summary>
        /// Checks if the command with the specified ID can be executed in the current state
        /// of the control.
        /// </summary>
        /// <param name="command">The ID of the command.</param>
        /// <returns>true if the ID of the command is known to the control and can be
        /// executed; false otherwise.</returns>
        bool CanExecuteCommand( string command );

        /// <summary>
        /// Executes the command with the specified ID.
        /// </summary>
        /// <param name="command">ID of the command to execute.</param>
        void ExecuteCommand( string command );
    }

    /// <summary>
    /// The interface of the classes which manage the displaying of plugin resources.
    /// </summary>
    /// <remarks>Implementations of this interface are returned from
    /// <see cref="IResourceDisplayer.CreateDisplayPane"/>.</remarks>
    public interface IDisplayPane: ICommandProcessor
    {
        /// <summary>
        /// Returns the instance of the control which is embedded in the preview pane
        /// when the resource is displayed.
        /// </summary>
        /// <returns>The control used to display the resource.</returns>
        Control GetControl();

        /// <summary>
        /// Shows the data of the specified resource in the display pane.
        /// </summary>
        /// <param name="resource">The resource to display.</param>
        void DisplayResource( IResource resource );

        /// <summary>
        /// Highlights the specified search result words in the resource text.
        /// </summary>
        /// <param name="words">The array of references to words to be highlighted.</param>
        /// <remarks>
        /// <para>The array is sorted by section and then by offset within the section.</para>
        /// <para>Use the <see cref="IDisplayPane2.DisplayResource(IResource, WordPtr[])"/> overload to load the resource and request words highlighting with a single call. This provides for the optimized highlighting implementation which applies at the stage of loading the resource.</para>
        /// </remarks>
		[ Obsolete( "Use the IDisplayPane2.HighlightWords(IResource, WordPtr[]) overload instead.", false ) ]
        void HighlightWords( WordPtr[] words );

        /// <summary>
        /// Ends the display of the specific resource in the display pane.
        /// </summary>
        /// <param name="resource">The resource which was displayed in the pane.</param>
        /// <remarks>After <c>EndDisplayResource</c> is called, if the user switches to a resource
        /// of the same type, <see cref="DisplayResource"/> is called to display the resource in the
        /// same display pane instance. If the user switches to a resource of a different type,
        /// <see cref="DisposePane"/> is called to dispose of the pane.</remarks>
        void EndDisplayResource( IResource resource );

        /// <summary>
        /// Disposes of the display pane.
        /// </summary>
        /// <remarks>After the core has called <c>DisposePane</c>, it no longer uses the pane instance.
        /// If a resource of the same type needs to be displayed again, a new pane instance is created
        /// by a call to <see cref="IResourceDisplayer.CreateDisplayPane"/>.</remarks>
        void DisposePane();

        /// <summary>
        /// Returns the text currently selected in the display pane as an RTF, HTML or plain-text
        /// string.
        /// </summary>
        /// <param name="format">The format in which the method returns the string.</param>
        /// <returns>The selected text, or null if there is no selection.</returns>
        string GetSelectedText( ref TextFormat format );

        /// <summary>
        /// Returns the text currently selected in the display pane as a plain-text string.
        /// </summary>
        /// <returns>The selected text in plain-text format, or null if there is no selection.</returns>
        string GetSelectedPlainText();
    }

    /// <summary>
    /// Defines additional functions for display panes.
    /// </summary>
    /// <since>2.0</since>
    public interface IDisplayPane2: IDisplayPane
    {
        /// <summary>
        /// Shows the data of the specified resource in the display pane and highlights
        /// the specified words in its text.
        /// </summary>
        /// <param name="resource">The resource to display.</param>
        /// <param name="wordsToHighlight">The array of references to words to be highlighted. May be <c>null</c>, which means that no words should be highlighted and Display Pane's behavior should default to <see cref="IDisplayPane.DisplayResource(IResource)"/>.</param>
        /// <remarks>
        /// <para>The array is sorted by section and then by offset within the section.</para>
        /// <para>This overload incapsulates the behavior of <see cref="IDisplayPane.DisplayResource(IResource)"/> and <see cref="IDisplayPane.HighlightWords"/> in order to provide the optimized performance.</para>
        /// </remarks>
        void DisplayResource( IResource resource, WordPtr[] wordsToHighlight );
    }

    /// <summary>
    /// The interface of the classes which return the text of the resources for plain-text indexing.
    /// </summary>
    public interface IResourceTextProvider
    {
        /// <summary>
        /// Passes the text of the resource as a series of plain-text fragments to the specified
        /// consumer.
        /// </summary>
        /// <param name="res">The resource for which the text is retrieved.</param>
        /// <param name="consumer">The consumer which accepts the text fragments.</param>
        /// <returns>true if the text was retrieved successfully, false if it was not possible
        /// to retrieve the text.</returns>
        /// <remarks>The plugin may decide to return or not return the text based on the purpose
        /// of the text request (<see cref="IResourceTextConsumer.Purpose"/>). For example, if
        /// retrieving the text takes a long time, the plugin can decide to return the text for
        /// indexing and not to return the text for search result context display (and return
        /// false in this case).</remarks>
        bool ProcessResourceText( IResource res, IResourceTextConsumer consumer );
    }

    /// <summary>
    /// Allows to disable text indexing for specific resources.
    /// </summary>
    /// <remarks>Used internally to disable indexing of file resources after a file folder has been
    /// excluded from indexing.</remarks>
    /// <since>2.0</since>
    public interface IResourceTextIndexingPermitter
    {
        /// <summary>
        /// Checks if indexing the specified resource is allowed.
        /// </summary>
        /// <param name="res">The resource to be indexed.</param>
        /// <returns>true if the resource should be indexed, false if the request for indexing the
        /// resource should be discarded.</returns>
        bool CanIndexResource( IResource res );
    }

    /// <summary>
    /// Provides icons for a specified resource type.
    /// </summary>
    /// <remarks>Implementations of this interface are registered through
    /// <see cref="IResourceIconManager.RegisterResourceIconProvider(string,IResourceIconProvider)"/>.</remarks>
    public interface IResourceIconProvider
    {
        /// <summary>
        /// Returns the icon for the current state of the specified resource.
        /// </summary>
        /// <param name="resource">The resource for which the icon is returned.</param>
        /// <returns>The icon, or null if the default icon should be used.</returns>
        Icon GetResourceIcon( IResource resource );

        /// <summary>
        /// Returns the default icon for resources of the specified type.
        /// </summary>
        /// <param name="resType">The resource type.</param>
        /// <returns>The icon, or null if the default icon should be used.</returns>
        Icon GetDefaultIcon( string resType );
    }

    /// <summary>
    /// Provides overlay icons that are drawn over the standard icons for the specified resource type.
    /// </summary>
    /// <remarks><para>Implementations of this interface are registered through
    /// <see cref="IResourceIconManager.RegisterOverlayIconProvider(string,IOverlayIconProvider)"/>.</para>
    /// <para>Implementations of this interface are chained - all overlay icon providers registered for
    /// the specified resource type are queried for icons, and all icons returned by them are shown.</para></remarks>
    /// <since>2.0</since>
    public interface IOverlayIconProvider
    {
        /// <summary>
        /// Returns the list of icons that should be overlaid on the specified resource.
        /// </summary>
        /// <param name="res">The resource for which the icons are requested.</param>
        /// <returns>The array of icons, or null if no overlay icons are available.</returns>
        Icon[] GetOverlayIcons( IResource res );
    }

    /// <summary>
    /// Allows a source resource to return data streams for resources coming from this source.
    /// </summary>
    /// <remarks><para>An example source resource is FileFolder. If a file resource has a link with
    /// the <see cref="PropTypeFlags.SourceLink"/> flag to a FileFolder resource,
    /// when the system needs to retrieve the contents of the file, it invokes the IStreamProvider
    /// registered for the FileFolder resource type and passes the file resource to its GetResourceStream
    /// method.</para>
    /// <para>Implementations of this interface are registered through
    /// <see cref="IPluginLoader.RegisterStreamProvider"/>.</para></remarks>
    public interface IStreamProvider
    {
        /// <summary>
        /// Returns the stream containing the contents of the specified resource.
        /// </summary>
        /// <param name="resource">The resource for which the contents are retrieved.</param>
        /// <returns>The stream containing the resource contents, or null if it was impossible
        /// to retrieve the contents.</returns>
        Stream GetResourceStream( IResource resource );
    }

    /// <summary>
    /// IDs of the standard commands which can be executed through <see cref="ICommandProcessor.ExecuteCommand"/>.
    /// </summary>
    public class DisplayPaneCommands
    {
		/// <summary>
		/// Go back (in the History). Applies either to the Web browser history, or (in case it is empty) to Omea views.
		/// </summary>
        public const string Back       = "Back";

		/// <summary>
		/// Go forward.
		/// </summary>
        public const string Forward    = "Forward";

		/// <summary>
		/// Find a token in the resource currently being displayed.
		/// </summary>
        public const string FindInPage = "FindInPage";

		/// <summary>
		/// Cut to Clipboard.
		/// </summary>
        public const string Cut        = "Cut";

		/// <summary>
		/// Copy to Clipboard.
		/// </summary>
        public const string Copy       = "Copy";

		/// <summary>
		/// Paste from Clipboard.
		/// </summary>
        public const string Paste      = "Paste";

		/// <summary>
		/// Print the resource currently being displayed.
		/// </summary>
        public const string Print      = "Print";

		/// <summary>
		/// Scroll the content displayed in the preview pane one page down.
		/// </summary>
        public const string PageDown   = "PageDown";

        /// <summary>
        /// Select all the content in the current view.
        /// </summary>
        /// <since>2.0</since>
        public const string SelectAll  = "SelectAll";

        /// <summary>
        /// In-place edit the item currently selected in the current view.
        /// </summary>
        /// <since>2.0</since>
        public const string RenameSelection = "RenameSelection";

        /// <summary>
        /// Navigate to the next search result within the document.
        /// </summary>
        /// <since>2.0</since>
        public const string NextSearchResult = "NextSearchResult";

        /// <summary>
        /// Navigate to the previous search result within the document.
        /// </summary>
        /// <since>2.0</since>
        public const string PrevSearchResult = "PrevSearchResult";
    }

    /// <summary>
    /// Allows to handle a number of user interface events in <see cref="IResourceTreePane"/>,
    /// resource browser and other places.
    /// </summary>
    /// <remarks>Implementations of this interface are registered through
    /// <see cref="IPluginLoader.RegisterResourceUIHandler"/>.</remarks>
    public interface IResourceUIHandler
    {
        /// <summary>
        /// Called when the specified resource is selected in a sidebar pane.
        /// </summary>
        /// <param name="res">The resource which is selected.</param>
        /// <remarks>The typical action for this method is to call
        /// <see cref="IResourceBrowser.DisplayResourceList"/> with the list of resources
        /// somehow linked to the selected resource.</remarks>
        void ResourceNodeSelected( IResource res );

        /// <summary>
        /// Checks if in-place rename for the specified resource is available.
        /// </summary>
        /// <param name="res">The resource which is renamed.</param>
        /// <returns>true if the rename is possible, false otherwise.</returns>
        /// <remarks>The in-place rename can be performed both from the resource tree view
        /// and the resource browser.</remarks>
        bool CanRenameResource( IResource res );

        /// <summary>
        /// Processes the in-place rename for the specified resource.
        /// </summary>
        /// <param name="res">The resource which was renamed.</param>
        /// <param name="newName">The string entered by the user as the new name of the resource.</param>
        /// <returns>true if the rename was successful, false otherwise (for example, if the
        /// string entered by the user is not a legal name for the resource).</returns>
        /// <remarks>The in-place rename can be performed both from the resource tree view
        /// and the resource browser.</remarks>
        bool ResourceRenamed( IResource res, string newName );

        /// <summary>
        /// Checks if the specified resources can be dropped on the specified target resource.
        /// </summary>
        /// <param name="targetResource">The target resource for the drop, or null if
        /// the handler is registered as a handler for dropping on empty space.</param>
        /// <param name="dragResources">The dragged resources.</param>
        /// <returns>true if the drop is allowed, false otherwise.</returns>
        bool CanDropResources( IResource targetResource, IResourceList dragResources );

        /// <summary>
        /// Processes the drop of the specified resources on the specified target resource.
        /// </summary>
        /// <param name="targetResource">The target resource for the drop, or null if
        /// the handler is registered as a handler for dropping on empty space.</param>
        /// <param name="droppedResources">The dropped resources.</param>
        void ResourcesDropped( IResource targetResource, IResourceList droppedResources );
    }

    /// <summary>
    /// Allows to perform complete handling of resource drag and drop in
    /// <see cref="IResourceTreePane"/>, resource browser and other places. This
    /// interface takes precedence over <see cref="IResourceUIHandler"/> - if a
    /// drag and drop handler is registered, the UI handler is not called to
    /// process drag and drop.
    /// </summary>
    /// <remarks>Implementations of this interface are registered through
    /// <see cref="IPluginLoader.RegisterResourceDragDropHandler"/>.</remarks>
    /// <since>2.0</since>
    public interface IResourceDragDropHandler
    {
        /// <summary>
        /// Called to supply data in additional formats when the specified resources are being dragged.
        /// </summary>
        /// <param name="dragResources">The dragged resources.</param>
        /// <param name="dataObject">The drag data object.</param>
        void AddResourceDragData( IResourceList dragResources, IDataObject dataObject );

        /// <summary>
        /// Called to return the drop effect when the specified data object is dragged over the
        /// specified resource.
        /// </summary>
        /// <param name="targetResource">The resource over which the drag happens.</param>
        /// <param name="data">The <see cref="IDataObject"/> containing the dragged data.</param>
        /// <param name="allowedEffect">The drag-and-drop operations which are allowed by the
        /// originator (or source) of the drag event.</param>
        /// <param name="keyState">The current state of the SHIFT, CTRL, and ALT keys,
        /// as well as the state of the mouse buttons.</param>
        /// <returns>The target drop effect.</returns>
        DragDropEffects DragOver( IResource targetResource, IDataObject data, DragDropEffects allowedEffect,
            int keyState );

        /// <summary>
        /// Called to handle the drop of the specified data object on the specified resource.
        /// </summary>
        /// <param name="targetResource">The drop target resource.</param>
        /// <param name="data">The <see cref="IDataObject"/> containing the dragged data.</param>
        /// <param name="allowedEffect">The drag-and-drop operations which are allowed by the
        /// originator (or source) of the drag event.</param>
        /// <param name="keyState">The current state of the SHIFT, CTRL, and ALT keys,
        /// as well as the state of the mouse buttons.</param>
        void Drop( IResource targetResource, IDataObject data, DragDropEffects allowedEffect, int keyState );
    }

    /// <summary>
    /// Allows to perform complete handling of resource rename in
    /// <see cref="IResourceTreePane"/>, resource browser and other places. This
    /// interface takes precedence over <see cref="IResourceUIHandler"/> - if a
    /// rename handler is registered, the UI handler is not called to
    /// process renames.
    /// </summary>
    /// <remarks>Implementations of this interface are registered through
    /// <see cref="IPluginLoader.RegisterResourceRenameHandler"/>.</remarks>
    /// <since>2.0</since>
    public interface IResourceRenameHandler
    {
        /// <summary>
        /// Checks if in-place rename for the specified resource is available.
        /// </summary>
        /// <param name="res">The resource which is renamed.</param>
        /// <param name="editText">The text to be displayed in the in-place edit box.</param>
        /// <returns>true if the rename is possible, false otherwise.</returns>
        bool CanRenameResource( IResource res, ref string editText );

        /// <summary>
        /// Processes the in-place rename for the specified resource.
        /// </summary>
        /// <param name="res">The resource which was renamed.</param>
        /// <param name="newName">The string entered by the user as the new name of the resource.</param>
        /// <returns>true if the rename was successful, false otherwise (for example, if the
        /// string entered by the user is not a legal name for the resource).</returns>
        bool ResourceRenamed( IResource res, string newName );
    }

    /// <summary>
    /// The reason for expanding a thread in a threaded view.
    /// </summary>
    /// <since>2.0</since>
    public enum ThreadExpandReason
    {
        /// <summary>
        /// The expand is requested during enumeration of items in the list view (going to
        /// next unread item, Select All, or a similar operation). No expensive operations like
        /// comment download should be performed.
        /// </summary>
        Enumerate,

        /// <summary>
        /// The expand is requested because the user clicked on the [+] button in the thread.
        /// All operations required to get the children need to be performed.
        /// </summary>
        Expand
    }

    /// <summary>
    /// Allows a plugin to control how a threaded list is built from its resources.
    /// </summary>
    /// <since>2.0</since>
    public interface IResourceThreadingHandler
    {
        /// <summary>
        /// Returns the resource which is a parent of the specified resource in the thread,
        /// or null if the resource is a thread root.
        /// </summary>
        /// <param name="res">The resource for which the parent is returned.</param>
        /// <returns>The thread parent.</returns>
        IResource GetThreadParent( IResource res );

        /// <summary>
        /// Returns the list of resources which are children of the specified resource
        /// in the thread, or an empty resource list if the resource has no children.
        /// </summary>
        /// <param name="res">The resource for which the children are returned.</param>
        /// <returns>The list of children.</returns>
        IResourceList GetThreadChildren( IResource res );

        /// <summary>
        /// Checks if the specified change to a resource caused it to move to a different thread.
        /// </summary>
        /// <param name="res">The resource which has changed.</param>
        /// <param name="changeSet">The description of changes to the resource.</param>
        /// <returns>true if the thread has changed, false otherwise.</returns>
        bool IsThreadChanged( IResource res, IPropertyChangeSet changeSet );

        /// <summary>
        /// Checks if the specified resource can be expanded even if it does not contain
        /// actual children in the resource list.
        /// </summary>
        /// <param name="res">The resource to check.</param>
        /// <param name="reason">A reason for expanding the resource defined by the <see cref="ThreadExpandReason"/>
        /// enumeration members. Possible reasons include UI expanding of the branch and enumeration of node children.</param>
        /// <returns>true if the resource can be expanded, false otherwise.</returns>
        bool CanExpandThread( IResource res, ThreadExpandReason reason );

        /// <summary><seealso cref="ThreadExpandReason"/>
        /// Called when the thread starting at the specified resource is first
        /// expanded. Can be used to add real children to nodes which had only
        /// virtual children.
        /// </summary>
        /// <param name="res">The resource representing a node that is expanded.</param>
        /// <param name="reason">A reason for expanding the resource defined by the <see cref="ThreadExpandReason"/>
        /// enumeration members. Possible reasons include UI expanding of the branch and enumeration of node children.
        /// If expansion is considered to be a costly operation (for example, supposes downloading of additional resources),
        /// it should not be actually initiated by enumeration operations (eg, only if user explicitly orders to expand the node should
        /// the download occur). This parameter provides for judging such cases.</param>
        /// <return><c>true</c> if the expand was handled, <c>false</c> if the expand with that reason
        /// was not handled and the expand can be requested again later.</return>
        /// <since>2.0</since>
        bool HandleThreadExpand( IResource res, ThreadExpandReason reason );
    }

    /// <summary>
    /// Allows to perform custom painting and event handling for a column in the resource browser.
    /// </summary>
    /// <remarks>Implementations of this interface are registered through
    /// <see cref="IDisplayColumnManager.RegisterCustomColumn"/>.</remarks>
    public interface ICustomColumn
    {
        /// <summary>
        /// Draws the value of the column for the specified resource.
        /// </summary>
        /// <param name="res">The resource for which the value is drawn.</param>
        /// <param name="g">The Graphics on which the drawing is performed.</param>
        /// <param name="rc">The rectangle in which the value is drawn.</param>
        void Draw( IResource res, Graphics g, Rectangle rc );

        /// <summary>
        /// Draws the header of the column.
        /// </summary>
        /// <param name="g">The Graphics on which the drawing is performed.</param>
        /// <param name="rc">The rectangle in which the value is drawn.</param>
        void DrawHeader( Graphics g, Rectangle rc );

        /// <summary>
        /// Handles the click of the left mouse button in the column.
        /// </summary>
        /// <param name="res">The resource on which the mouse is clicked.</param>
        /// <param name="pt">The coordinates, relative to the top left corner of the
        /// column rectangle for the specified resource, where the mouse was clicked.</param>
        void MouseClicked( IResource res, Point pt );

        /// <summary>
        /// Allows to show a custom context menu for the column.
        /// </summary>
        /// <param name="context">The action context for the context menu actions.</param>
        /// <param name="ownerControl">The control owning the context menu.</param>
        /// <param name="pt">The coordinates, relative to the control, where the context
        /// menu should be shown.</param>
        /// <returns>true if a custom menu was shown; false if the standard menu should be shown.</returns>
        bool ShowContextMenu( IActionContext context, Control ownerControl, Point pt );

        /// <summary>
        /// Returns the tooltip for the column value of specified resource.
        /// </summary>
        /// <param name="res">The resource for which the tooltip is retrieved.</param>
        /// <returns>The tooltip text or null if no tooltip is required.</returns>
        string GetTooltip( IResource res );
    }

    /// <summary>
    /// Specifies possible modes of serializing properties to the XML representation of resources.
    /// </summary>
    public enum SerializationMode
    {
        /// <summary>
        /// The serialization mode specified by property type flags <see cref="PropTypeFlags.AskSerialize"/>
        /// and <see cref="PropTypeFlags.NoSerialize"/> is used.
        /// </summary>
        Default,

        /// <summary>
        /// The value of the property is not included in the XML serialization.
        /// </summary>
        NoSerialize,

        /// <summary>
        /// A checkbox for the property is shown in the "Send Resources" dialog, and the property value
        /// is serialized if the checkbox is checked by the user.
        /// </summary>
        AskSerialize,

        /// <summary>
        /// The value of the property is included in the XML serialization.
        /// </summary>
        Serialize
    }

    /// <summary>
    /// Allows to perform custom serialization and deserialization of resources in the XML format.
    /// </summary>
    /// <remarks><para>Implementations of this interface are registered through
    /// <see cref="IPluginLoader.RegisterResourceSerializer"/>.</para>
    /// <para>The XML serialization of resources is used by the "Send Resources" feature to transmit
    /// resources between different Omea database instances.</para></remarks>
    public interface IResourceSerializer
    {
        /// <summary>
        /// Called after the properties of a resource have been serialized to the specified XML node.
        /// </summary>
        /// <param name="parentResource">The parent in the serialization tree of the resource being
        /// serialized.</param>
        /// <param name="res">The resource which has been serialized.</param>
        /// <param name="node">The node to which the resource has been serialized.</param>
        /// <remarks>The method can be used to perform custom serialization of property values
        /// which are not serialized by default, or to serialize data (like e-mail bodies) which
        /// is not stored in the resource properties.</remarks>
        void AfterSerialize( IResource parentResource, IResource res, XmlNode node );

        /// <summary>
        /// Called to match a temporary resource received from XML to an existing resource in the
        /// Omea database, and to restore the custom data which was serialized by <see cref="AfterSerialize"/>.
        /// </summary>
        /// <param name="parentResource">The parent in the serialization tree of the resource being
        /// serialized.</param>
        /// <param name="res">The temporary representation of the resource which has been restored
        /// from the XML serialization.</param>
        /// <param name="node">The XML node containing the serialization of the resource.</param>
        /// <returns>The received resource <c>res</c>, or an existing resource in the Omea database
        /// which matches the received resource.</returns>
        /// <remarks>If the received resource matches a resource already existing in the Omea database
        /// (for example, there is already a news article with the same ID), the method should copy
        /// new information (for example, properties which do not exist) to the existing resource and return
        /// it. If the resource doesn't match any existing resources, the received instance should be
        /// returned from the method.</remarks>
        IResource AfterDeserialize( IResource parentResource, IResource res, XmlNode node );

        /// <summary>
        /// Allows to specify the serialization mode of resource properties on per-resource basis.
        /// </summary>
        /// <param name="res">The resource the properties of which are being serialized.</param>
        /// <param name="propertyType">The name of the type of the property which is being serialized.</param>
        /// <returns>The serialization mode.</returns>
        SerializationMode GetSerializationMode( IResource res, string propertyType );
    }

    /// <summary>
    /// Allows to perform "semantic delete" of resources, to move resources to the Deleted Items
    /// view and to recover them from that view.
    /// </summary>
    /// <remarks>Implementations of this interface are registered through
    /// <see cref="IPluginLoader.RegisterResourceDeleter"/>.</remarks>
    /// <since>2.0</since>
    public interface IResourceDeleter
    {
        /// <summary>
        /// Shows a dialog to confirm the deletion of the specified resources.
        /// </summary>
        /// <param name="resources">The resources to delete.</param>
        /// <param name="permanent">If true, confirmation for permanent delete is requested.</param>
        /// <param name="showCancel">If true, the delete confirmation dialog should include
        /// the "Cancel" button.</param>
        /// <returns>DialogResult.Yes if the resources should be deleted, DialogResult.No if the
        /// deletion of these resources should be skipped, DialogResult.Cancel if the entire delete
        /// operation is cancelled.</returns>
        DialogResult ConfirmDeleteResources( IResourceList resources, bool permanent, bool showCancel );

        /// <summary>
        /// Checks if deleting of the specified resource is allowed.
        /// </summary>
        /// <param name="res">The resource to delete. Can be null, in that case checks whether deletion of arbitrary resource in specified mode is allowed.</param>
        /// <param name="permanent">Mode of requested resource deletion (permanent or non-permanent).</param>
        /// <returns>true if the delete is allowed, false if the delete action should be disabled.</returns>
        bool CanDeleteResource( IResource res, bool permanent );

        /// <summary>
        /// Answers whether non-permanent deletion can be ignored. This is the general mechanism
        /// to exclude potentially dangerous behavior if permanent deletion performs unrevertable results
        /// like cleaning information on remote server. This has two effects - user explicitely has to
        /// press "Shift" key for permanent deletion and rules can not blindly call permanent deletion.
        /// </summary>
        /// <returns>true if the non-permanent deletion can be ignored.</returns>
        /// <since>2.1</since>
        bool CanIgnoreRecyclebin();

        /// <summary>
        /// Deletes the specified resource.
        /// </summary>
        /// <param name="res">The resource to delete.</param>
        /// <remarks>If the resource is currently not in the Deleted Items view, moves it to the Deleted
        /// Items view. If the resource is already in the Deleted Items view, deletes it permanently.</remarks>
        void DeleteResource( IResource res );

        /// <summary>
        /// Deletes the specified resource permanently.
        /// </summary>
        /// <param name="res">The resource to delete.</param>
        void DeleteResourcePermanent( IResource res );

        /// <summary>
        /// Moves the specified resource from the Deleted Items view to its original view.
        /// </summary>
        /// <param name="res">The resource to undelete.</param>
        void UndeleteResource( IResource res );
    }

    /// <summary>
    /// Allows to define the HTML representation of the resources of a particular plugin.
    /// </summary>
    /// <since>2.0</since>
    public interface INewspaperProvider
    {
        /// <summary>
        /// Returns the text that is included in the styles section of the header for the
        /// specified resource type.
        /// </summary>
        /// <param name="resourceType">The resource type for which the styles are being requested.</param>
        /// <param name="writer">Writer to which the additional styles should be submitted. If no additional styles are required, leave it intact.</param>
        void GetHeaderStyles( string resourceType, TextWriter writer );

        /// <summary>
        /// Returns the complete HTML representation of the specified item, including its title, body, and footer (if any).
        /// </summary>
        /// <param name="item">The item to show as HTML.</param>
        /// <param name="writer">A writer to which the HTML representation of the given item should be dumped.</param>
        void GetItemHtml( IResource item, TextWriter writer );
    }

	/// <summary>
	/// A generic delegate for calling a function with one parameter of type <see cref="IResource"/>.
	/// </summary>
    public delegate void ResourceDelegate( IResource res );

	/// <summary>
	/// A generic delegate for calling a function with one parameter of type <see cref="IResourceList"/>.
	/// </summary>
    public delegate void ResourceListDelegate( IResourceList resList );

    // -- Interfaces that are implemented by the core and can be used by plugins -----

	/// <summary><seealso cref="IUIManager.GetStatusWriter"/>
	/// Identifies a pane of the status bar which presents status and progress messages related to a specific category of tasks (UI, resource, network, etc).
	/// </summary>
	/// <remarks>
	/// <para>To write messages to the status bar, you need to obtain an instance of the status bar writer for the specific status bar pane by calling the <see cref="IUIManager.GetStatusWriter"/> method of the <see cref="IUIManager"/> interface which can be obtained from <see cref="ICore.UIManager"/>.</para>
	/// </remarks>
	public enum StatusPane
	{
		/// <summary>
		/// A status bar pane that displays the UI descriptions and hints.
		/// </summary>
		UI,

		/// <summary>
		/// A status bar pane to display the network-related messages, such as download progress and status.
		/// </summary>
		Network,

		/// <summary>
		/// A status bar pane to display the resource operations details.
		/// </summary>
		ResourceBrowser
	};

    //-------------------------------------------------------------------------
    /// <summary>
    /// Implements custom calculation of unread counts for a view.
    /// </summary>
    /// <remarks>Implementations of this interface are registered through
    /// <see cref="IUnreadManager.RegisterUnreadCountProvider"/>.</remarks>
    public interface IUnreadCountProvider
    {
        /// <summary>
        /// Returns the list of resources in the specified view.
        /// </summary>
        /// <remarks>The returned list of resources is intersected with the list of resources
        /// which have the IsUnread flag.
        /// </remarks>
        /// <param name="viewResource">The view for which the unread count is calculated.</param>
        /// <returns>The list of resources matching the view.</returns>
        IResourceList GetResourcesForView( IResource viewResource );
    }

    /// <summary>
    /// Manages the updating of unread counts for resources.
    /// </summary>
    public interface IUnreadManager
    {
        /// <summary>
        /// Registers a provider of unread counts for the specified resource type.
        /// </summary>
        /// <remarks>Providers are used when the unread counts are computed in a more
        /// complicated way than simply counting resources with the IsUnread property
        /// linked to the resource. For example, resources of type SearchView have an
        /// unread count provider.</remarks>
        /// <param name="resType">The resource type for which the unread count provider
        /// is registered.</param>
        /// <param name="provider">The unread count provider implementation.</param>
        void RegisterUnreadCountProvider( string resType, IUnreadCountProvider provider );

        /// <summary>
        /// Returns the unread counter which is displayed for the specified resource
        /// in the current unread state.
        /// </summary>
        /// <param name="res">The resource to get the count for.</param>
        /// <returns>The count of unread resources for the specified resource.</returns>
        int GetUnreadCount( IResource res );

        /// <summary>
        /// Invalidates unread counters of all states for the specified resource,
        /// forcing them to be recalculated when they are next accessed.
        /// </summary>
        /// <param name="res">The resource for which the counters are invalidated.</param>
        void InvalidateUnreadCounter( IResource res );
    }

    /// <summary>
    /// Controls the progress window which is displayed at program startup and when
    /// <see cref="IUIManager.RunWithProgressWindow"/> is used.
    /// </summary>
    public interface IProgressWindow
    {
        /// <summary>
        /// Displays the specified text in the progress window.
        /// </summary>
        /// <param name="percentage">The percentage to be shown in the progress bar (0 to 100).</param>
        /// <param name="message">The message to be displayed in the first line of the progress window.</param>
        /// <param name="timeMessage">The message to be displayed in the second line of the progress window,
        /// after the "Elapsed Time" indicator, or null if no custom message should be displayed in the second line.</param>
        void UpdateProgress( int percentage, string message, string timeMessage );
    }

    /// <summary><seealso cref="IUIManager.GetStatusWriter"/>
	/// Represents a facility for writing messages into the Omea main window status bar. Use <see cref="IUIManager.GetStatusWriter"/> to obtain this interface.
    /// </summary>
	/// <remarks>
	/// <para>A status writer is thread-safe and may be invoked from any thread.
	/// The functions won't do synchronous cross-thread calls or somehow else allow a reentrancy to occur.</para>
	/// <para>When a status writer (an object providing the <see cref="IStatusWriter"/> interface) is created with <see cref="IUIManager.GetStatusWriter"/>, it gets bound to a specific status bar pane, as specified by a <see cref="StatusPane"/> enumeration member passed to the method. Generally, the <see cref="StatusPane.UI"/> status pane should be used for displaying status messages related to actions originating directly from user input. Background tasks should occupy the other panes, depending on their type (resource-processing-related or network-operations-related), or the thread they are executing on.</para>
	/// <para>The status writers should not be shared between different simultaneous tasks, instead, <see cref="IUIManager.GetStatusWriter"/> should be called individually, each time producing a new instance of the status writer with a particular task set as owner.</para>
	/// <para>It's important to understand the Omea status bar usage model which significantly differs from one of the orthodox "Application.StatusBar" property. In the case of that property you would just throw it a new value to be displayed immediately, and forget about it. The value would be shown until some other caller supplied an alternative.</para>
	/// <para>The Omea Status Writer was specially designed for simultaneous use by many consumers. It struggles for the status bar display time for you. Once given a string, it then tries to show it again and again when the status bar is not occupied by another task. Other status bar consumers may override your value, but as soon as they need the status bar no more, your setting will return to view. Due to this, you must explicitly resign your use of status bar (by calling <see cref="IStatusWriter.ClearStatus"/>) when you don't need to display the text any more, so that others could display their information. Also, failure to do this will result in infinite persistence of your message in the status bar because it will not be overwritten by other statusbar consumers.</para>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Get (create) a status bar writer
	/// // Attach to this class, display in the Network pane
	/// IStatusWriter	sw = Core.UIManager.GetStatusWriter( this, StatusPane.Network );
	///
	/// // Submit a message to the status bar
	/// sw.ShowStatus( "Downloading…" );
	///
	/// Trace.WriteLine( sw.LastMessage );	// Should print "Downloading…"
	///
	/// // Do some lengthy operation (possibly by running async jobs)
	/// …
	/// …
	///
	/// // Resign use of status bar
	/// // Unless you do it, your message will be kept in status bar infinitely
	/// sw.ClearStatus();
	///
	/// Trace.WriteLine( sw.LastMessage == null );	// Should be True
	/// </code>
	/// </example>
    public interface IStatusWriter
    {
        /// <summary><seealso cref="ClearStatus"/><seealso cref="LastMessage"/><seealso cref="IUIManager.GetStatusWriter"/>
		/// Shows a status bar message in the appropriate status bar pane.
        /// </summary>
		/// <param name="message">Message to be displayed in the status bar. May be <c>Null</c>.</param>
		/// <remarks>
		/// <para>Don't forget to call <see cref="IStatusWriter.ClearStatus"/> when you need the submitted message no more so that other tasks could display their messages as well.</para>
		/// <para>The pane to which the messages go is defined at the status writer creation, see <see cref="IUIManager.GetStatusWriter"/> for details.</para>
		/// <para>If <paramref name="message"/> is <c>Null</c> or an empty string (<c>""</c>), the status writer clears its text in the status bar the way <see cref="ClearStatus"/> would do.</para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Get (create) a status bar writer
		/// // Attach to this class, display in the Network pane
		/// IStatusWriter	sw = Core.UIManager.GetStatusWriter( this, StatusPane.Network );
		///
		/// // Submit a message to the status bar
		/// sw.ShowStatus( "Downloading…" );
		///
		/// Trace.WriteLine( sw.LastMessage );	// Should print "Downloading…"
		///
		/// // Do some lengthy operation (possibly by running async jobs)
		/// …
		/// …
		///
		/// // Resign use of status bar
		/// // Unless you do it, your message will be kept in status bar infinitely
		/// sw.ClearStatus();
		///
		/// Trace.WriteLine( sw.LastMessage == null );	// Should be True
		/// </code>
		/// </example>
        void ShowStatus( string message );

        /// <summary><seealso cref="ClearStatus"/><seealso cref="LastMessage"/><seealso cref="IUIManager.GetStatusWriter"/>
		/// Shows a status bar message in the appropriate status bar pane and optionally forces it to redraw immediately.
        /// </summary>
		/// <param name="message">Message to be displayed in the status bar. May be <c>Null</c>.</param>
		/// <param name="repaint">Forces the status bar to repaint and display the updated text immediately.
		/// May be useful if a lengthy operation is running on the UI thread and the application has no chance to process the painting messages normally.</param>
		/// <remarks>
		/// <para>Don't forget to call <see cref="IStatusWriter.ClearStatus"/> when you need the submitted message no more so that other tasks could display their messages as well.</para>
		/// <para>The pane to which the messages go is defined at the status writer creation, see <see cref="IUIManager.GetStatusWriter"/> for details.</para>
		/// <para>If <paramref name="message"/> is <c>Null</c> or an empty string (<c>""</c>), the status writer clears its text in the status bar the way <see cref="ClearStatus"/> would do.</para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Get (create) a status bar writer
		/// // Attach to this class, display in the Network pane
		/// IStatusWriter	sw = Core.UIManager.GetStatusWriter( this, StatusPane.Network );
		///
		/// // Submit a message to the status bar
		/// sw.ShowStatus( "Downloading…", false );	// Do not repaint
		///
		/// Trace.WriteLine( sw.LastMessage );	// Should print "Downloading…"
		///
		/// // Do some lengthy operation (possibly by running async jobs)
		/// …
		/// …
		///
		/// // Resign use of status bar
		/// // Unless you do it, your message will be kept in status bar infinitely
		/// sw.ClearStatus();
		///
		/// Trace.WriteLine( sw.LastMessage == null );	// Should be True
		/// </code>
		/// </example>
        void ShowStatus( string message, bool repaint );

        /// <summary><seealso cref="ShowStatus"/><seealso cref="LastMessage"/>
		/// Cancels the status bar message submitted through this status bar writer and allows other tasks to display their messages in this status bar pane.
        /// </summary>
		/// <remarks>
		/// <para>It's important to understand the Omea status bar usage model which significantly differs from one of the orthodox "Application.StatusBar" property. In the case of that property you would just throw it a new value to be displayed immediately, and forget about it. The value would be shown until some other caller supplied an alternative.</para>
		/// <para>The Omea Status Writer was specially designed for simultaneous use by many consumers. It struggles for the status bar display time for you. Once given a string, it then tries to show it again and again when the status bar is not occupied by another task. Other status bar consumers may override your value, but as soon as they need the status bar no more, your setting will return to view. Due to this, you must explicitly resign your use of status bar (by calling <see cref="IStatusWriter.ClearStatus"/>) when you don't need to display the text any more, so that others could display their information. Also, failure to do this will result in infinite persistence of your message in the status bar because it will not be overwritten by other statusbar consumers.</para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Get (create) a status bar writer
		/// // Attach to this class, display in the Network pane
		/// IStatusWriter	sw = Core.UIManager.GetStatusWriter( this, StatusPane.Network );
		///
		/// // Submit a message to the status bar
		/// sw.ShowStatus( "Downloading…" );
		///
		/// Trace.WriteLine( sw.LastMessage );	// Should print "Downloading…"
		///
		/// // Do some lengthy operation (possibly by running async jobs)
		/// …
		/// …
		///
		/// // Resign use of status bar
		/// // Unless you do it, your message will be kept in status bar infinitely
		/// sw.ClearStatus();
		///
		/// Trace.WriteLine( sw.LastMessage == null );	// Should be True
		/// </code>
		/// </example>
        void ClearStatus();

        /// <summary><seealso cref="ShowStatus"/><seealso cref="ClearStatus"/>
        /// The last message supplied to this status writer thru <see cref="ShowStatus"/> that is currently being displayed in the appropriate status pane, or <c>Null</c>, if the status writer has been cleared with <see cref="ClearStatus"/> or as a result of a call to <see cref="ShowStatus"/> with an empty string (<c>""</c>) or <c>Null</c> parameter.
        /// </summary>
		/// <remarks>
		/// <para>Setting status text to an empty string (<c>""</c>) through the <see cref="ShowStatus"/> method causes this property to return the <c>Null</c> value.</para>
		/// <para>It's important to understand the Omea status bar usage model which significantly differs from one of the orthodox "Application.StatusBar" property. In the case of that property you would just throw it a new value to be displayed immediately, and forget about it. The value would be shown until some other caller supplied an alternative.</para>
		/// <para>The Omea Status Writer was specially designed for simultaneous use by many consumers. It struggles for the status bar display time for you. Once given a string, it then tries to show it again and again when the status bar is not occupied by another task. Other status bar consumers may override your value, but as soon as they need the status bar no more, your setting will return to view. Due to this, you must explicitly resign your use of status bar (by calling <see cref="IStatusWriter.ClearStatus"/>) when you don't need to display the text any more, so that others could display their information. Also, failure to do this will result in infinite persistence of your message in the status bar because it will not be overwritten by other statusbar consumers.</para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Get (create) a status bar writer
		/// // Attach to this class, display in the Network pane
		/// IStatusWriter	sw = Core.UIManager.GetStatusWriter( this, StatusPane.Network );
		///
		/// // Submit a message to the status bar
		/// sw.ShowStatus( "Downloading…" );
		///
		/// Trace.WriteLine( sw.LastMessage );	// Should print "Downloading…"
		///
		/// // Do some lengthy operation (possibly by running async jobs)
		/// …
		/// …
		///
		/// // Resign use of status bar
		/// // Unless you do it, your message will be kept in status bar infinitely
		/// sw.ClearStatus();
		///
		/// Trace.WriteLine( sw.LastMessage == null );	// Should be True
		/// </code>
		/// </example>
        string LastMessage{ get; }

		/// <summary><seealso cref="ClearStatus"/><seealso cref="LastMessage"/><seealso cref="IUIManager.GetStatusWriter"/>
		/// Shows a status bar message in the appropriate status bar pane and automatically removes it after a given time span.
		/// </summary>
		/// <param name="message">Message to be displayed in the status bar. May be <c>Null</c>.</param>
		/// <param name="nSecondsToKeep">Expiration period, in seconds, after which the message will be automatically revoked from the status bar. <c>0</c> means no expiration.</param>
		/// <remarks><para>Use of this function makes it unnecessary to call the <see cref="ClearStatus"/> function to reveal the status bar of your message.</para>
		/// <para>You should not mix calls to the timed and untimed versions of <see cref="ShowStatus"/> because the timed version will erase the status message even if gets overwritten by a non-timed call by that moment. In the contrary, multiple calls to the timed function will prolongate the time-to-live of the status message accordingly, regardless of whether their time spans overlap or not.</para>
		/// </remarks>
		/// <example>
		/// <code>
		/// // Get (create) a status bar writer
		/// // Attach to this class, display in the Network pane
		/// IStatusWriter	sw = Core.UIManager.GetStatusWriter( this, StatusPane.Network );
		///
		/// // Submit a message to the status bar
		/// // It will be automatically revoked ten seconds later
		/// sw.ShowStatus( "Download completed.", 10 );
		/// </code>
		/// </example>
		/// <since>2.1</since>
		void ShowStatus(string message, int nSecondsToKeep);
    }

    /// <summary>
    /// Allows to hide certain nodes from an <see cref="IResourceTreePane"/> and other tree views.
    /// </summary>
    /// <remarks>If the filter conditions change, the tre can be refiltered with a call to
    /// <see cref="IResourceTreePane.UpdateNodeFilter"/>.</remarks>
    public interface IResourceNodeFilter
    {
        /// <summary>
        /// Checks if the node for the specified resource should be displayed in the tree.
        /// </summary>
        /// <param name="res">The resource to display.</param>
        /// <param name="level">The depth of the resource in the tree (0 if the resource is
        /// a top-level node, 1 if it's a child of a top-level node, and so on).</param>
        /// <returns>true if the resource should be displayed, false if it should be hidden.</returns>
        bool AcceptNode( IResource res, int level );
    }

    /// <summary>
    /// Allows to control sidebar panes which display trees of resources.
    /// </summary>
    /// <remarks>Instances of this class are obtained through <see cref="ISidebarSwitcher.RegisterTreeViewPane"/>
    /// and related methods.</remarks>
    public interface IResourceTreePane
    {
        /// <summary>
        /// Registers a toolbar button for the local toolbar in the tree pane. The button
        /// icon is specified as an Icon instance.
        /// </summary>
        /// <param name="action">The action which is registered.</param>
        /// <param name="icon">The icon shown on the button.</param>
        /// <param name="text">The text shown on the button, or null if the button contains only an icon.</param>
        /// <param name="tooltip">The tooltip shown for the button.</param>
        /// <param name="filters">
        ///   The filters which provide additional control over the state of the action. Can be equal to
        ///   null or an empty array if no additional filters are needed.
        ///   If the filters have marked the presentation as hidden, the Update() method of the
        ///   action itself is not called.
        /// </param>
        void RegisterToolbarAction( IAction action, Icon icon, string text, string tooltip,
            IActionStateFilter[] filters );

		/// <summary>
        /// Registers a toolbar button for the local toolbar in the tree pane. The button
        /// icon is specified as an Icon instance.
        /// </summary>
        /// <param name="action">The action which is registered.</param>
        /// <param name="icon">The icon shown on the button.</param>
        /// <param name="text">The text shown on the button, or null if the button contains only an icon.</param>
        /// <param name="tooltip">The tooltip shown for the button.</param>
        /// <param name="filters">
        ///   The filters which provide additional control over the state of the action. Can be equal to
        ///   null or an empty array if no additional filters are needed.
        ///   If the filters have marked the presentation as hidden, the Update() method of the
        ///   action itself is not called.
        /// </param>
        void RegisterToolbarAction( IAction action, Image icon, string text, string tooltip,
            IActionStateFilter[] filters );

        /// <summary>
        /// Adds a node filter that will show or hide certain nodes in the tree.
        /// </summary>
        /// <param name="nodeFilter">The node filter instance.</param>
        void AddNodeFilter( IResourceNodeFilter nodeFilter );

        /// <summary>
        /// Clears and recreates the tree when the conditions of a node filter have changed.
        /// </summary>
        /// <param name="keepSelection">If true, the selection is restored to the same node
        /// after the rebuild. If false, all branches are collapsed after the rebuild, and the
        /// default selection is used.</param>
        void UpdateNodeFilter( bool keepSelection );

        /// <summary>
        /// Sets the selection to the specified resource in the tree and, if necessary,
        /// expands its parent branches.
        /// </summary>
        /// <param name="res">The resource to select.</param>
        void SelectResource( IResource res );

        /// <summary>
        /// Initiates in-place editing for the specified resource node.
        /// </summary>
        /// <param name="res">The resource for which the display name should be edited.</param>
        void EditResourceLabel( IResource res );

        /// <summary>
        /// Expands the parent nodes of the specified resource node.
        /// </summary>
        /// <param name="res">The resource for which the parents should be expanded.</param>
        void ExpandParents( IResource res );

        /// <summary>
        /// Allows dropping resources on an empty space in the pane and sets the specified handler
        /// to process such drops.
        /// </summary>
        /// <param name="emptyDropHandler">The handler which is invoked when resources are
        /// dropped over the empty space.</param>
        [Obsolete]
        void EnableDropOnEmpty( IResourceUIHandler emptyDropHandler );

        /// <summary>
        /// Allows dropping resources on an empty space in the pane and sets the specified drag
        /// and drop handler to process such drops.
        /// </summary>
        /// <param name="emptyDropHandler">The handler which is invoked when resources are
        /// dropped over the empty space.</param>
        /// <since>2.0</since>
        void EnableDropOnEmpty( IResourceDragDropHandler emptyDropHandler );

        /// <summary>
        /// If true, new nodes which are added to the tree are automatically selected.
        /// </summary>
        bool SelectAddedItems { get; set; }

        /// <summary>
        /// Gets and sets the ID of the directed link property which links a resource node
        /// to its parent.
        /// </summary>
        int ParentProperty { get; set; }

        /// <summary>
        /// Gets and sets the resource types which are used to filter the pane content when
        /// a workspace is active.
        /// </summary>
        /// <remarks>When a workspace is active, the top-level resources displayed in the pane
        /// are resources of the specified types directly linked to the workspace resource.</remarks>
        string[] WorkspaceFilterTypes { get; set; }

        /// <summary>
        /// Gets the resource currently selected in the tree pane.
        /// </summary>
        IResource SelectedNode { get; }

        /// <summary>
        /// Gets or sets the callback used to return text of tooltips for items shown in the tree pane.
        /// </summary>
        /// <since>2.0</since>
        ResourceToolTipCallback ToolTipCallback { get; set; }
    }

    /// <summary>
    /// The callback used to get the tooltips to show for IResourceTreePane items.
    /// </summary>
    /// <since>2.0</since>
    public delegate string ResourceToolTipCallback( IResource res );

    /// <summary>
    /// Allows to control the sidebar at the right part of the screen.
    /// </summary>
    public interface ISidebar
    {
        /// <summary>
        /// Registers a pane to be displayed in the right sidebar.
        /// </summary>
        /// <param name="viewPane">The pane instance.</param>
        /// <param name="paneId">The ID of the pane.</param>
        /// <param name="caption">The caption of the pane.</param>
        /// <param name="icon">The icon displayed for the pane in the small toolbar at the
        /// bottom of the sidebar. For the icon, an image in PNG 24x24 format is required. If image has
        /// different format, it will be adjusted to fit 24x24.</param>
        void RegisterPane( AbstractViewPane viewPane, string paneId, string caption, Image icon );

        /// <summary>
        /// Changes the caption of the pane with the specified ID.
        /// </summary>
        /// <param name="paneId">The ID of the pane.</param>
        /// <param name="caption">The new caption of the pane.</param>
        /// <since>2.0</since>
        void SetPaneCaption( string paneId, string caption );

        /// <summary>
        /// Checks if the specified pane in the right sidebar is currently expanded.
        /// </summary>
        /// <param name="paneId">The ID of the pane to check.</param>
        /// <returns>true if the pane is expanded, false otherwise.</returns>
        bool IsPaneExpanded( string paneId );

        /// <summary>
        /// Expands or collapses the specified pane in the right sidebar.
        /// </summary>
        /// <param name="paneId">The ID of the pane to expand or collapse.</param>
        /// <param name="expanded">true if the pane should be expanded, false if it should be collapsed.</param>
        void SetPaneExpanded( string paneId, bool expanded );

        /// <summary>
        /// Gets the number of panes registered in the right sidebar.
        /// </summary>
        int PanesCount { get; }

        /// <summary>
        /// Returns the instance of the pane with the name.
        /// </summary>
        /// <param name="name">The name of the pane to retrieve.</param>
        /// <returns>The pane instance, or null if there is no such pane in the current tab.</returns>
        /// <since>2.1</since>
        AbstractViewPane GetPane( string name );
    }

    /// <summary>
    /// Allows to control the left sidebar switcher (which supports showing different sidebars
    /// depending on the active tab).
    /// </summary>
    public interface ISidebarSwitcher
    {
        /// <summary>
        /// Registers a custom view pane in the left sidebar.
        /// </summary>
        /// <param name="paneId">ID of the pane which is registered.</param>
        /// <param name="tabId">ID of the tab in which the pane is shown.</param>
        /// <param name="caption">User-visible caption of the pane.</param>
        /// <param name="icon">Icon to show on the button which expands/collapses the pane.</param>
        /// <param name="viewPane">The pane instance.</param>
        void RegisterViewPane( string paneId, string tabId, string caption, Image icon, AbstractViewPane viewPane );

        /// <summary>
        /// Registers a standard view pane displaying a tree of resources in the left sidebar.
        /// </summary>
        /// <param name="paneId">ID of the pane which is registered.</param>
        /// <param name="tabId">ID of the tab in which the pane is shown.</param>
        /// <param name="caption">User-visible caption of the pane.</param>
        /// <param name="icon">Icon to show on the button which expands/collapses the pane.</param>
        /// <param name="rootResource">The root of the resource tree displayed in the pane.</param>
        /// <returns>The instance of the tree view pane.</returns>
        IResourceTreePane RegisterTreeViewPane( string paneId, string tabId, string caption, Image icon,
                                                IResource rootResource );

        /// <summary>
        /// Registers a custom view pane in the left sidebar which serves as the resource
        /// structure pane.
        /// </summary>
        /// <param name="paneId">ID of the pane which is registered.</param>
        /// <param name="tabId">ID of the tab in which the pane is shown.</param>
        /// <param name="caption">User-visible caption of the pane.</param>
        /// <param name="icon">Icon to show on the button which expands/collapses the pane.</param>
        /// <param name="viewPane">The pane instance.</param>
        /// <remarks>The resource structure pane is the pane in which Omea tries to find the resource
        /// registered as the resource location through <see cref="IUIManager.RegisterResourceLocationLink"/>.
        /// </remarks>
        void RegisterResourceStructurePane( string paneId, string tabId, string caption, Image icon,
                                            AbstractViewPane viewPane );

        /// <summary>
        /// Registers a standard pane displaying a tree of resources, which also serves as the resource
        /// structure pane, in the left sidebar.
        /// </summary>
        /// <param name="paneId">ID of the pane which is registered.</param>
        /// <param name="tabId">ID of the tab in which the pane is shown.</param>
        /// <param name="caption">User-visible caption of the pane.</param>
        /// <param name="icon">Icon to show on the button which expands/collapses the pane.</param>
        /// <param name="rootResource">The root of the resource tree displayed in the pane.</param>
        /// <returns>The instance of the tree view pane.</returns>
        /// <remarks>The resource structure pane is the pane in which Omea tries to find the resource
        /// registered as the resource location through <see cref="IUIManager.RegisterResourceLocationLink"/>.</remarks>
        IResourceTreePane RegisterResourceStructureTreePane( string paneId, string tabId, string caption,
                                                             Image icon, IResource rootResource );

        /// <summary>
        /// Registers a standard pane displaying a tree of resources, which also serves as the resource
        /// structure pane, in the left sidebar. The pane is automatically filtered by workspace resource
        /// type when a workspace is active.
        /// </summary>
        /// <param name="paneId">ID of the pane which is registered.</param>
        /// <param name="tabId">ID of the tab in which the pane is shown.</param>
        /// <param name="caption">User-visible caption of the pane.</param>
        /// <param name="icon">Icon to show on the button which expands/collapses the pane.</param>
        /// <param name="rootResType">The resource type the root of which is used as the root
        /// resource of the pane.</param>
        /// <returns>The instance of the tree view pane.</returns>
        /// <remarks><para>The resource structure pane is the pane in which Omea tries to find the resource
        /// registered as the resource location through <see cref="IUIManager.RegisterResourceLocationLink"/>.</para>
        /// <para>The root resource for a resource type is obtained through
        /// <see cref="IResourceTreeManager.GetRootForType"/>.</para>
        /// <para>When a workspace is active, the pane displays on the top level the resources
        /// of <c>rootResType</c> type linked to the workspace. If resources of additional types
        /// should be displayed, <see cref="IResourceTreePane.WorkspaceFilterTypes"/> should be used.</para>
        /// </remarks>
        IResourceTreePane RegisterResourceStructureTreePane( string paneId, string tabId, string caption,
                                                             Image icon, string rootResType );

        /// <summary>
        /// Registers a keyboard shortcut for activating the pane with the specified ID.
        /// </summary>
        /// <param name="paneId">The pane ID for which the shortcut is registeed.</param>
        /// <param name="shortcut">The keyboard shortcut.</param>
        void RegisterViewPaneShortcut( string paneId, Keys shortcut );

        /// <summary>
        /// Returns the instance of the "Views and Categories" pane for the active tab.
        /// </summary>
        IResourceTreePane DefaultViewPane { get; }

        /// <summary>
        /// Activates a pane with the specified ID.
        /// </summary>
        /// <param name="paneId">ID of the pane to activate.</param>
        /// <remarks>If the pane was hidden, it is shown. Then the focus is transferred to it.</remarks>
        void ActivateViewPane( string paneId );

        /// <summary>
        /// If the active tab has the specified ID, activates the pane with the specified ID (if present)
        /// and returns the pane instance. If the active tab is different, returns null.
        /// </summary>
        /// <param name="tabId">The tab which should be active.</param>
        /// <param name="paneId">The pane ID to activate.</param>
        /// <returns>The pane instance or null if the activation failed.</returns>
        AbstractViewPane ActivateViewPane( string tabId, string paneId );

        /// <summary>
        /// Returns the ID of the pane which is currently active in the current tab.
        /// </summary>
        /// <remarks>The active pane is the pane which is currently focused, or which was last
        /// focused if the focus is not currently in the sidebar.</remarks>
        string ActivePaneId { get; }

        /// <summary>
        /// Returns the instance of the pane with the specified ID in the current tab.
        /// </summary>
        /// <param name="paneId">The ID of the pane to retrieve.</param>
        /// <returns>The pane instance, or null if there is no such pane in the current tab.</returns>
        AbstractViewPane GetPane( string paneId );

        /// <summary>
        /// Returns the instance of the pane with the specified ID in the specified tab.
        /// </summary>
        /// <param name="tabId">The ID of the tab for which the pane is retrieved.</param>
        /// <param name="paneId">The ID of the pane to retrieve.</param>
        /// <returns>The pane instance, or null if there is no such pane in the specified tab.</returns>
        AbstractViewPane GetPane( string tabId, string paneId );

        /// <summary>
        /// Returns the ID of the pane which has been registered as the resource structure pane
        /// for the specified tab.
        /// </summary>
        /// <param name="tabId">The ID of the tab for which the resource structure pane is retrieved.</param>
        /// <returns>The ID of the pane, or null if no resource structure pane was registered.</returns>
        string GetResourceStructurePaneId( string tabId );
    }

    /// <summary>
    /// Allows to query the information about a registered resource type tab.
    /// </summary>
    public interface IResourceTypeTab
    {
        /// <summary>
        /// The ID of the tab.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// The name of the tab (the text displayed in the tab caption).
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns the resource types which are displayed in the tab.
        /// </summary>
        string[] GetResourceTypes();

        /// <summary>
        /// The link property for which the resources are displayed in the tab.
        /// </summary>
        int LinkPropId { get; }

        /// <summary>
        /// Returns the list of all resources displayed in the tab.
        /// </summary>
        /// <paran name="live">true if a live list is requested, false otherwise.</paran>
        /// <returns>The list of resources displayed in the tab, or null if the filter
        /// list is requested for the "All Resources" tab.</returns>
        IResourceList GetFilterList( bool live );
    }

    /// <summary>
    /// The collection of registered resource type tabs.
    /// </summary>
    public interface IResourceTypeTabCollection
    {
        /// <summary>
        /// The count of registered resource type tabs.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Returns the resource type tab at the given index.
        /// </summary>
        /// <remarks>The tab at index 0 is 'All Resources'; the first tab registered by a plugin
        /// is at index 1.</remarks>
        IResourceTypeTab this [int index] { get; }

        /// <summary>
        /// Returns the resource type tab with the specified ID.
        /// </summary>
        IResourceTypeTab this [string tabId] { get; }
    }

    /// <summary>
    /// Allows to register resource type tabs and to get information about the
    /// registered resource type tabs.
    /// </summary>
    public interface ITabManager
    {
        /// <summary>
        /// Gets the collection of registered resource type tabs.
        /// </summary>
        IResourceTypeTabCollection Tabs { get; }

        /// <summary>
        /// Registers a resource type tab which displays resources of a single type.
        /// </summary>
        /// <param name="tabId">ID of the tab (non-localized).</param>
        /// <param name="tabName">User-visible name of the tab (localized).</param>
        /// <param name="resType">The resource type displayed in the tab.</param>
        /// <param name="order">Order of the tab relative to other tabs.</param>
        void RegisterResourceTypeTab( string tabId, string tabName, string resType, int order );

        /// <summary>
        /// Registers a resource type tab which displays resources of multiple types.
        /// </summary>
        /// <param name="tabId">ID of the tab (non-localized).</param>
        /// <param name="tabName">User-visible name of the tab (localized).</param>
        /// <param name="resTypes">The resource types displayed in the tab.</param>
        /// <param name="order">Order of the tab relative to other tabs.</param>
        void RegisterResourceTypeTab( string tabId, string tabName, string[] resTypes, int order );

        /// <summary>
        /// Registers a resource type tab which displays resources of multiple types and
        /// the resources which have the specified link property.
        /// </summary>
        /// <param name="tabId">ID of the tab (non-localized).</param>
        /// <param name="tabName">User-visible name of the tab (localized).</param>
        /// <param name="resTypes">The resource types displayed in the tab.</param>
        /// <param name="linkPropId">ID of the link property for the resources displayed in the tab.</param>
        /// <param name="order">Order of the tab relative to other tabs.</param>
        /// <remarks><para>This is used, for example, for the "Web" tab. The actual bookmarks
        /// displayed in the tab may have different types (HTML file, plain-text file, picture etc.),
        /// but they all have a link of type "Source", which distinguishes them from local files, Outlook
        /// attachments and other file-based resources.</para>
        /// <para>The resources displayed in the tab either have one of the specified resource
        /// types or a link of the specified type.</para>
        /// </remarks>
        void RegisterResourceTypeTab( string tabId, string tabName, string[] resTypes, int linkPropId,
            int order );

        /// <summary>
        /// Sets the resource which is selected in the sidebar when the specified tab is first
        /// activated.
        /// </summary>
        /// <param name="tabId">ID of the tab for which the resource is set.</param>
        /// <param name="res">The default selected resource.</param>
        /// <remarks>First, an attempt to select the resource in the resource structure pane
        /// for the tab is made. If there is no resource structure pane, or if that pane does not
        /// contain the resource, an attempt is made to select the resource in the "Views and
        /// Categories" tab.</remarks>
        void SetDefaultSelectedResource( string tabId, IResource res );

        /// <summary>
        /// Selects the tab where resources of the specified type are displayed.
        /// </summary>
        /// <param name="resType">The resource type of the tab to switch to.</param>
        /// <remarks>If there is no tab registered for the specified resource type, the
        /// method switches to the "All Resources" tab.</remarks>
        void SelectResourceTypeTab( string resType );

        /// <summary>
        /// Selects the tab where resources with the specified link property are displayed.
        /// </summary>
        /// <param name="linkPropId">The property of the resources in the tab to switch to.</param>
        /// <remarks>If there is no tab registered for the specified link property, the
        /// method switches to the "All Resources" tab.</remarks>
        void SelectLinkPropTab( int linkPropId );

        /// <summary>
        /// Returns the ID of the tab where the resources of the specified type are displayed.
        /// </summary>
        /// <param name="resType">The resource type of the tab to find.</param>
        /// <returns>The ID of the tab, or null if there is no tab registered for the
        /// specified resource type.</returns>
        string FindResourceTypeTab( string resType );

        /// <summary>
        /// Returns the ID of the tab where the resources with the specified link property are displayed.
        /// </summary>
        /// <param name="linkPropId">The ID of the link property for the tab to find.</param>
        /// <returns>The ID of the tab, or null if there is no tab registered for the
        /// specified link property type.</returns>
        string FindLinkPropTab( int linkPropId );

        /// <summary>
        /// Gets the ID of the currently selected tab, and allows to switch to a different tab.
        /// </summary>
        string CurrentTabId { get; set; }

        /// <summary>
        /// Returns the information about the currently selected tab.
        /// </summary>
        IResourceTypeTab CurrentTab { get; }

        /// <summary>
        /// Switches to the specified tab and verifies that the tab switch was completed correctly.
        /// </summary>
        /// <param name="tabId">The tab to activate.</param>
        /// <returns>true if the tab was activated, false if the user switched to a different
        /// tab while the tab switch was being processed.</returns>
        /// <since>1.0.2</since>
        bool ActivateTab( string tabId );

        /// <summary>
        /// Returns the ID of the tab in which the specified resource is visible.
        /// </summary>
        /// <param name="res">The resource to check.</param>
        /// <returns>The ID of the tab in which the resource is visible, or null if
        /// the resource is only visible in the "All Resources" tab.</returns>
        string GetResourceTab( IResource res );

        /// <summary>
        /// Fired after the user has switched to a different resource type tab.
        /// </summary>
        event EventHandler TabChanged;
    }

    /// <summary>
    /// Contains IDs of the standard sidebar panes which are registered by the Omea core.
    /// </summary>
    public class StandardViewPanes
    {
        /// <summary>
        /// ID of the "Views and Categories" pane.
        /// </summary>
        public const string ViewsCategories = "ViewsCategories";

        /// <summary>
        /// ID of the "Correspondents" pane.
        /// </summary>
        public const string Correspondents = "Correspondents";
    }

    /// <summary>
    /// Allows any component to return an action context for its current state.
    /// </summary>
    public interface IContextProvider
    {
        /// <summary>
        /// Returns the action context for the current state of the control.
        /// </summary>
        /// <param name="kind">The kind of action which is invoked (keyboard, menu and so on).</param>
        /// <returns>The action context for the specified kind and the current state.</returns>
        IActionContext GetContext( ActionContextKind kind );
    }

    /// <summary>
    /// Defines flags which specify additional properties of a column in multiline view.
    /// </summary>
    /// <since>2.0</since>
    [Flags]
    public enum MultiLineColumnFlags
    {
        /// <summary>
        /// The left edge of the column is anchored to the left side of the view area.
        /// </summary>
        AnchorLeft = 1,

        /// <summary>
        /// The right edge of the column is anchored to the right side of the view area.
        /// </summary>
        AnchorRight = 2,

        /// <summary>
        /// The column is hidden and other columns are stretched if the resource does not have
        /// the property displayed in the column.
        /// </summary>
        HideIfNoProp = 4
    }

    /// <summary>
    /// Allows to define grouping for resources displayed in the resource browser.
    /// </summary>
    /// <since>2.0</since>
    public interface IResourceGroupProvider
    {
        /// <summary>
        /// Returns the name of the group in which the specified resource is found.
        /// </summary>
        /// <param name="res">The resource shown in resource browser.</param>
        /// <returns>The name of the group.</returns>
        string GetGroupName( IResource res );
    }

    /// <summary>
    /// Manages the default resource list columns used when displaying resource lists
    /// and the columns which are available in the "Configure Columns" dialog.
    /// </summary>
    public interface IDisplayColumnManager
    {
        /// <summary>
        /// Registers a column which will be included in the list when the resources of the specified
        /// type are displayed.
        /// </summary>
        /// <param name="resourceType">The type for which the column is registered, or null if
        /// the column is registered for resources of all types.</param>
        /// <param name="index">The position of the column relative to other columns registered
        /// for the same resource type.</param>
        /// <param name="column">The descriptor specifying the properties displayed in the column,
        /// its width and flags.</param>
        /// <remarks>If the displayed list contains resources of several types and columns with
        /// the same index have been registered for those types, the relative position of those
        /// columns is undefined.</remarks>
        void RegisterDisplayColumn( string resourceType, int index, ColumnDescriptor column );

        /// <summary>
        /// Makes the specified column available for selection for a specific resource type
        /// or all resource types, even if the properties shown in the column are not present
        /// on any of the resources in the current resource list.
        /// </summary>
        /// <param name="resourceType">The resource type for which the column is registered,
        /// or null if the column is registered for all resource types.</param>
        /// <param name="column">The descriptor specifying the properties displayed in the column,
        /// its width and flags.</param>
        void RegisterAvailableColumn( string resourceType, ColumnDescriptor column );

        /// <summary>
        /// Removes the column with the specified property names from the list of columns
        /// registered with <see cref="RegisterAvailableColumn"/>.
        /// </summary>
        /// <param name="resourceType">The resource type for which the column was registered,
        /// or null if it was registered for all resource types.</param>
        /// <param name="propName">The property name of the column to be removed.</param>
        void RemoveAvailableColumn( string resourceType, string propName );

        /// <summary>
        /// Registers a custom handler for drawing the values of a specified property and
        /// handling events in the column displaying the property.
        /// </summary>
        /// <param name="propId">The ID of the property for which the custom column is
        /// registered.</param>
        /// <param name="customColumn">The custom column implementation.</param>
        void RegisterCustomColumn( int propId, ICustomColumn customColumn );

        /// <summary>
        /// Registers a column visible in the multi-line view for the specified resource.
        /// </summary>
        /// <param name="resourceType">The type of the resource.</param>
        /// <param name="propId">The ID of property displayed in the column.</param>
        /// <param name="startRow">The row in which the column rectangle starts.</param>
        /// <param name="endRow">The row in which the column rectangle ends.</param>
        /// <param name="startX">The X position where the column data starts.</param>
        /// <param name="width">The width of the column in pixels.</param>
        /// <param name="flags">The flags specifying additional options for the column.</param>
        /// <param name="textColor">The color of text displayed in the column.</param>
        /// <param name="textAlign">The alignment of text within the column rectangle.</param>
        /// <since>2.0</since>
        void RegisterMultiLineColumn( string resourceType, int propId, int startRow, int endRow,
            int startX, int width, MultiLineColumnFlags flags, Color textColor, HorizontalAlignment textAlign );

        /// <summary>
        /// Registers a column visible in the multi-line view for the specified resource.
        /// </summary>
        /// <param name="resourceType">The type of the resource.</param>
        /// <param name="propIds">The IDs of properties displayed in the column.</param>
        /// <param name="startRow">The row in which the column rectangle starts.</param>
        /// <param name="endRow">The row in which the column rectangle ends.</param>
            /// <param name="startX">The X position where the column data starts.</param>
        /// <param name="width">The width of the column in pixels.</param>
        /// <param name="flags">The flags specifying additional options for the column.</param>
        /// <param name="textColor">Color of the text rendered into this column.</param>
        /// <param name="textAlign">Horizontal alignment of the text rendered into this column.</param>
        /// <since>2.0</since>
        void RegisterMultiLineColumn( string resourceType, int[] propIds, int startRow, int endRow,
            int startX, int width, MultiLineColumnFlags flags, Color textColor, HorizontalAlignment textAlign );

        /// <summary>
        /// Sets the value indicating whether top-level items in threads of the specified resource type
        /// are always aligned even if the items have no children.
        /// </summary>
        /// <param name="resourceType">The resource type for which the setting is performed.</param>
        /// <param name="align">If true, the threads are always aligned. If false, top level items
        /// with no children are displayed with zero indent and not with a single indent step.</param>
        /// <since>2.0</since>
        void SetAlignTopLevelItems( string resourceType, bool align );

        /// <summary>
        /// Registers a custom callback which will be used to convert the values of the specified
        /// property to text displayed in the resource list view.
        /// </summary>
        /// <param name="propId">The ID of the property for which the callback is registered.</param>
        /// <param name="propToText">The callback which returns a text string given a resource
        /// and a property ID.</param>
        void RegisterPropertyToTextCallback( int propId, PropertyToTextCallback propToText );

        /// <summary>
        /// Registers a custom callback which will be used to convert the values of the specified
        /// property to text displayed in the resource list view, depending on the space available
        /// for displaying the property value in the list.
        /// /// </summary>
        /// <param name="propId">The ID of the property for which the callback is registered.</param>
        /// <param name="propToText">The callback which returns a text string given a resource,
        /// a property ID and a width value.</param>
        /// <since>2.0</since>
        void RegisterPropertyToTextCallback( int propId, PropertyToTextCallback2 propToText );

        /// <summary>
        /// Returns the list of default columns used for displaying the specified resource list.
        /// </summary>
        /// <param name="resList">The resource list for which the columns are retrieved.</param>
        /// <returns>The array of column descriptors describing the list columns.</returns>
        /// <remarks>The returned array includes columns registered for every resource type
        /// in the list and columns registered for any resource type.</remarks>
        ColumnDescriptor[] GetDefaultColumns( IResourceList resList );

        /// <summary>
        /// Adds the columns registered for any resource type to the specified list of
        /// column descriptors.
        /// </summary>
        /// <param name="columnDescriptors">An array of column descriptors.</param>
        /// <returns>An array containing all the column descriptors in the original
        /// array and descriptors for all columns registered for any resource type.</returns>
        ColumnDescriptor[] AddAnyTypeColumns( ColumnDescriptor[] columnDescriptors );
    }

    /// <summary><seealso cref="Core.SettingStore"/>
    /// The main setting store that stores settings for Omea application and all the plugins.
    /// </summary>
    /// <remarks>
    /// <para>Use <see cref="Core.SettingStore"/> to get access to the setting store.</para>
    /// <para>For your plugins, you should use <see cref="Core.SettingStore"/> instead of developing cutsom settings-management facilities.</para>
    /// </remarks>
    public interface ISettingStore
    {
        /// <summary><seealso cref="Core.SettingStore"/>
        /// Writes a <see cref="System.String">string</see> option to the setting store.
        /// </summary>
        /// <param name="section">Section (group, category) of the option.</param>
        /// <param name="key">Option unique name.</param>
        /// <param name="value">Option value.</param>
        void WriteString( string section, string key, string value );

        /// <summary><seealso cref="Core.SettingStore"/>
        /// Writes an <see cref="int">integer</see> option to the setting store.
        /// </summary>
        /// <param name="section">Section (group, category) of the option.</param>
        /// <param name="key">Option unique name.</param>
        /// <param name="value">Option value.</param>
        void WriteInt( string section, string key, int value );

        /// <summary><seealso cref="Core.SettingStore"/>
        /// Writes a <see cref="System.Boolean">boolean</see> option to the setting store.
        /// </summary>
        /// <param name="section">Section (group, category) of the option.</param>
        /// <param name="key">Option unique name.</param>
        /// <param name="value">Option value.</param>
        void WriteBool( string section, string key, bool value );

        /// <summary><seealso cref="Core.SettingStore"/>
        /// Writes a <see cref="System.DateTime">date/time</see> option to the setting store.
        /// </summary>
        /// <param name="section">Section (group, category) of the option.</param>
        /// <param name="key">Option unique name.</param>
        /// <param name="value">Option value.</param>
        void WriteDate( string section, string key, DateTime value );

        /// <summary><seealso cref="Core.SettingStore"/>
        /// Reads a <see cref="System.String">string</see> value from the settings store.
        /// If there is no such option in the setting store, an empty string is returned.
        /// </summary>
        /// <param name="section">Section (group, category) of the option.</param>
        /// <param name="key">Option unique name.</param>
        /// <returns>Option value.</returns>
        string ReadString( string section, string key );

        /// <summary><seealso cref="Core.SettingStore"/>
        /// Reads a <see cref="System.String">string</see> value from the settings store.
        /// If there is no such option in the setting store, the default value is returned.
        /// </summary>
        /// <param name="section">Section (group, category) of the option.</param>
        /// <param name="key">Option unique name.</param>
        /// <param name="defaultValue">The default value to be used if the option is not defined.</param>
        /// <returns>Option value.</returns>
        string ReadString( string section, string key, string defaultValue );

        /// <summary><seealso cref="Core.SettingStore"/>
        /// Reads an <see cref="int">integer</see> value from the settings store.
        /// If there is no such option in the setting store, the default value is returned.
        /// </summary>
        /// <param name="section">Section (group, category) of the option.</param>
        /// <param name="key">Option unique name.</param>
        /// <param name="defaultValue">The default value to be used if the option is not defined.</param>
        /// <returns>Option value.</returns>
        int ReadInt( string section, string key, int defaultValue );

        /// <summary><seealso cref="Core.SettingStore"/>
        /// Reads a <see cref="System.Boolean">boolean</see> value from the settings store.
        /// If there is no such option in the setting store, the default value is returned.
        /// </summary>
        /// <param name="section">Section (group, category) of the option.</param>
        /// <param name="key">Option unique name.</param>
        /// <param name="defaultValue">The default value to be used if the option is not defined.</param>
        /// <returns>Option value.</returns>
        bool ReadBool( string section, string key, bool defaultValue );

        /// <summary><seealso cref="Core.SettingStore"/>
        /// Reads a <see cref="System.DateTime">date/time</see> value from the settings store.
        /// If there is no such option in the setting store, the default value is returned.
        /// </summary>
        /// <param name="section">Section (group, category) of the option.</param>
        /// <param name="key">Option unique name.</param>
        /// <param name="defaultValue">The default value to be used if the option is not defined.</param>
        /// <returns>Option value.</returns>
        DateTime ReadDate( string section, string key, DateTime defaultValue );
    }

    /// <summary>
    /// Manages the registration of interfaces for handling resources of specific types
    /// provided by plugins.
    /// </summary>
    public interface IPluginLoader
    {
        /// <summary>
        /// Registers the <see cref="IResourceUIHandler">user interface action handler</see>
        /// for the specific resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the handler is registered.</param>
        /// <param name="handler">The handler implementation.</param>
        /// <remarks>
        /// <para>The user interface handler is responsible for handling operations like
        /// drag and drop, in-place rename and selection in the sidebar panes.</para>
        /// <para>If the handler was already registered, the new handler replaces the
        /// previously registered handler.</para>
        /// </remarks>
        void RegisterResourceUIHandler( string resType, IResourceUIHandler handler );

        /// <summary>
        /// Returns the user interface handler registered for the specified resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the handler is requested.</param>
        /// <returns>The handler implementation, or null if there was no handler registered.</returns>
        /// <since>2.0</since>
        IResourceUIHandler GetResourceUIHandler( string resType );

        /// <summary>
        /// Returns the user interface handler registered for the specified resource.
        /// </summary>
        /// <param name="res">The resource for which the handler is requested.</param>
        /// <returns>The handler implementation, or null if there was no handler registered.</returns>
        /// <remarks>If there is no handler registered for the type of the resource but the
        /// resource has a <see cref="PropTypeFlags.SourceLink">source link</see>, the handler
        /// for the source resource is returned.</remarks>
        IResourceUIHandler GetResourceUIHandler( IResource res );

        /// <summary>
        /// Registers the <see cref="IResourceDragDropHandler">drag and drop handler</see>
        /// for the specific resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the handler is registered.</param>
        /// <param name="handler">The handler implementation.</param>
        /// <remarks>
        /// <para>The implementation allows registering more than one handler for each of the resource types.
        /// Upon execution (drag-over or drop), all the handlers for this resource type are queried in order of registration,
        /// until one is found that agrees to handle the operation. The remaining handlers do not get queried.</para>
        /// <para>When the drop operation is performed, the handler is queried for a drag-over and the result
        /// should be positive for the drop to execute, ie the effect must not be <see cref="DragDropEffects.None"/>.</para>
        /// </remarks>
        /// <since>2.0</since>
        void RegisterResourceDragDropHandler( string resType, IResourceDragDropHandler handler );

        /// <summary>
        /// Returns the drag and drop handler registered for the specified resource.
        /// </summary>
        /// <param name="res">The resource for which the handler is requested.</param>
        /// <returns>The handler implementation, or null if there was no handler registered.</returns>
        /// <remarks>If there is no handler registered for the type of the resource but the
        /// resource has a <see cref="PropTypeFlags.SourceLink">source link</see>, the handler
        /// for the source resource is returned.</remarks>
        /// <since>2.0</since>
        IResourceDragDropHandler GetResourceDragDropHandler( IResource res );

        /// <summary>
        /// Returns the drag and drop handler registered for the specified resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the handler is requested.</param>
        /// <returns>The handler implementation, or null if there was no handler registered.</returns>
        /// <since>2.0</since>
        IResourceDragDropHandler GetResourceDragDropHandler( string resType );

        /// <summary>
        /// Registers the <see cref="IResourceRenameHandler">rename handler</see>
        /// for the specific resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the handler is registered.</param>
        /// <param name="handler">The handler implementation.</param>
        /// <remarks>
        /// <para>If the handler was already registered, the new handler replaces the
        /// previously registered handler.</para>
        /// </remarks>
        void RegisterResourceRenameHandler( string resType, IResourceRenameHandler handler );

        /// <summary>
        /// Returns the rename handler registered for the specified resource.
        /// </summary>
        /// <param name="res">The resource for which the handler is requested.</param>
        /// <returns>The handler implementation, or null if there was no handler registered.</returns>
        /// <remarks>If there is no handler registered for the type of the resource but the
        /// resource has a <see cref="PropTypeFlags.SourceLink">source link</see>, the handler
        /// for the source resource is returned.</remarks>
        /// <since>2.0</since>
        IResourceRenameHandler GetResourceRenameHandler( IResource res );

        /// <summary>
        /// Registers a custom threading handler for the specified resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the handler is registered.</param>
        /// <param name="handler">The handler implementation.</param>
        /// <since>2.0</since>
        void RegisterResourceThreadingHandler( string resType, IResourceThreadingHandler handler );

        /// <summary>
        /// Registers a custom threading handler for resources with the specified property.
        /// </summary>
        /// <param name="propId">The ID of the property which must be present on the resources for which
        /// the handler is used.</param>
        /// <param name="handler">The handler implementation.</param>
        /// <since>2.0</since>
        void RegisterResourceThreadingHandler( int propId, IResourceThreadingHandler handler );

        /// <summary>
        /// Registers a default threading handler with the specified reply link type for the
        /// specified resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the handler is registered.</param>
        /// <param name="replyProp">The ID of the reply link property.</param>
        /// <since>2.0</since>
        void RegisterDefaultThreadingHandler( string resType, int replyProp );

        /// <summary>
        /// Returns the threading handler registered for the specified resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the handler is requested.</param>
        /// <returns>The handler implementation, or null if no handler is registered.</returns>
        IResourceThreadingHandler GetResourceThreadingHandler( string resType );

        /// <summary>
        /// Gets the composite threading handler which can perform threading of resources of all
        /// types based on the registered threading handlers.
        /// </summary>
        /// <since>2.0</since>
        IResourceThreadingHandler CompositeThreadingHandler { get; }

        /// <summary>
        /// Registers a <see cref="IResourceTextProvider">text provider</see> for the specified
        /// resource type or for any resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the provider is registered,
        /// or null if the provider is registered for all resource types.</param>
        /// <param name="provider">The provider implementation.</param>
        /// <remarks>When a resource is indexed, first all the providers which have been
        /// registered for the type of the resource are invoked, and then all the providers
        /// registered for all resource types.</remarks>
        void RegisterResourceTextProvider( string resType, IResourceTextProvider provider );

        /// <summary>
        /// Checks whether any <see cref="IResourceTextProvider">text provider</see> for
        /// the specified resource type is registered.
        /// </summary>
        /// <param name="resType">The resource type for which the provider is queried. <b>Can not be null.</b></param>
        /// <returns>True if any text provider was registered for the specified resource type.</returns>
        bool HasTypedTextProvider( string resType );

        /// <summary>
        /// Invokes all registered resource text providers for the specified resource.
        /// </summary>
        /// <param name="res">The resource for which the text is requested.</param>
        /// <param name="consumer">The consumer to which the text fragments are passed.</param>
        void InvokeResourceTextProviders( IResource res, IResourceTextConsumer consumer );

        /// <summary>
        /// Registers a <see cref="IResourceDisplayer">resource displayer</see> for the specified
        /// resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the resource displayer is registered.</param>
        /// <param name="displayer">The resource displayer implementation.</param>
        /// <remarks><para>If the resource displayer was already registered, the new implementation
        ///  replaces the previously registered implementation.</para></remarks>
        void RegisterResourceDisplayer( string resType, IResourceDisplayer displayer );

        /// <summary>
        /// Returns the resource displayer registered for the specified resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the displayer is requested.</param>
        /// <returns>The resource displayer implementation, or null if none was registered.</returns>
        IResourceDisplayer GetResourceDisplayer( string resType );

        /// <summary><seealso cref="INewspaperProvider"/><seealso cref="GetNewspaperProvider"/>
        /// Registers a <see cref="INewspaperProvider">newspaper provider</see> for the specified
        /// resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the provider is registered.</param>
        /// <param name="provider">The provider implementation.</param>
        /// <since>2.0</since>
        void RegisterNewspaperProvider( string resType, INewspaperProvider provider );

        /// <summary><seealso cref="INewspaperProvider"/><seealso cref="RegisterNewspaperProvider"/>
        /// Returns the newspaper provider registered for the specified resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the provider is requested.</param>
        /// <returns>The newspaper provider implementation, or null if none was registered.</returns>
        /// <since>2.0</since>
        INewspaperProvider GetNewspaperProvider( string resType );

        /// <summary>
        /// Returns the <see cref="IStreamProvider">stream provider</see> registered for the specified
        /// resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the provider is registered.</param>
        /// <param name="provider">The provider implementation.</param>
        /// <remarks>If the provider for the resource type was already registered, the new
        /// provider replaces the previously registered provider.</remarks>
        void RegisterStreamProvider( string resType, IStreamProvider provider );

        /// <summary>
        /// Returns the stream provider registered for the specified resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the provider is requested.</param>
        /// <returns>The provider implementation, or null if none was registered.</returns>
        IStreamProvider GetStreamProvider( string resType );

        /// <summary>
        /// Registers a plugin service.
        /// </summary>
        /// <param name="pluginService">The plugin service implementation.</param>
        /// <remarks><para>Plugin services are interfaces which can be used by plugins to provide APIs
        /// to other plugins. For example, the Outlook plugin registers an <see cref="IEmailService">
        /// email service</see> which allows other plugins to send e-mail.</para>
        /// <para>If several services are registered which implement the same interface, the last
        /// registered one is used.</para></remarks>
        void RegisterPluginService( object pluginService );

        /// <summary>
        /// Returns an implementation of the specified plugin service interface.
        /// </summary>
        /// <param name="serviceType">The type of the interface to return.</param>
        /// <returns>The implementation of the interface, or null if none was registered.</returns>
        object GetPluginService( Type serviceType );

        /// <summary>
        /// Registers a <see cref="IResourceSerializer">resource serializer</see> for the
        /// specified resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the serializer is registered.</param>
        /// <param name="serializer">The serializer implementation.</param>
        /// <remarks>If the serializer for the resource type was already registered, the new
        /// serializer replaces the previously registered serializer.</remarks>
        void RegisterResourceSerializer( string resType, IResourceSerializer serializer );

        /// <summary>
        /// Returns the resource serializer registered for the specified resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the serializer is requested.</param>
        /// <returns>The serializer implementation, or null if none was registered.</returns>
        IResourceSerializer GetResourceSerializer( string resType );

        /// <summary>
        /// Register a <see cref="IViewsConstructor">view constructor</see> implementation.
        /// </summary>
        /// <param name="constructor">The view constructor implementation.</param>
        void RegisterViewsConstructor( IViewsConstructor constructor );

        /// <summary>
        /// Returns the list of all registered view constructors.
        /// </summary>
        /// <returns>ArrayList containing all registered <see cref="IViewsConstructor"/>
        /// implementations.</returns>
        ArrayList GetViewsConstructors();

        /// <summary>
        /// Registers a <see cref="IResourceDeleter">resource deleter</see> for the specified
        /// resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the deleter is registered.</param>
        /// <param name="deleter">The resource deleter implementation.</param>
        void RegisterResourceDeleter( string resType, IResourceDeleter deleter );

        /// <summary>
        /// Returns the resource deleter registered for the specified resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the deleter is requested.</param>
        /// <returns>The deleter implementation, or null if none was registered for the
        /// specified resource type.</returns>
        IResourceDeleter GetResourceDeleter( string resType );
    }

    /// <summary>
    /// Manages registration and retrieval of icons for resources.
    /// </summary>
    /// <remarks>All small (16x16) resource icons are put in one global image list,
    /// which can be accessed through the <see cref="ImageList"/>property.</remarks>
    public interface IResourceIconManager
    {
        /// <summary>
        /// Registers a provider of icons for the specified resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the provider is registered.</param>
        /// <param name="provider">The provider implementation.</param>
        void RegisterResourceIconProvider( string resType, IResourceIconProvider provider );

        /// <summary>
        /// Registers a provider of icons for the specified resource types.
        /// </summary>
        /// <param name="resTypes">The resource types for which the provider is registered.</param>
        /// <param name="provider">The provider implementation.</param>
        void RegisterResourceIconProvider( string[] resTypes, IResourceIconProvider provider );

        /// <summary>
        /// Registers a provider of overlay icons for the specified resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the provider is registered, or null
        /// if the provider is registered for all resource types.</param>
        /// <param name="provider">The provider implementation.</param>
        /// <since>2.0</since>
        void RegisterOverlayIconProvider( string resType, IOverlayIconProvider provider );

        /// <summary>
        /// Returns the provider of icons for the specified resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the provider is returned.</param>
        /// <returns>The provider implementation, or null if no provider has been registered.</returns>
        IResourceIconProvider GetResourceIconProvider( string resType );

        /// <summary>
        /// Returns the index in the global image list of the icon for the specified resource.
        /// </summary>
        /// <param name="res">The resource for which the icon is returned.</param>
        /// <returns>The icon index, or the index of the default icon if no icon provider
        /// has been registered.</returns>
        int GetIconIndex( IResource res );

        /// <summary>
        /// Returns the index in the global image list of the default icon for the specified
        /// resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the icon is returned.</param>
        /// <returns>The icon index, or the index of the default icon if no icon provider
        /// has been registered.</returns>
        int GetDefaultIconIndex( string resType );

        /// <summary>
        /// Returns the indexes in the global image list of the overlay icons for the specified
        /// resource.
        /// </summary>
        /// <param name="res">The resource for which the icons are returned.</param>
        /// <returns>The array of icon indexes, or an empty array if the specified resource
        /// has no overlay icons.</returns>
        /// <since>2.0</since>
        int[] GetOverlayIconIndices( IResource res );

        /// <summary>
        /// Registers an icon for the specified property type.
        /// </summary>
        /// <param name="propId">The ID of the property type for which the icon is registered.</param>
        /// <param name="icon">The icon which is registered.</param>
        void RegisterPropTypeIcon( int propId, Icon icon );

        /// <summary>
        /// Returns the index of the icon for the specified property type.
        /// </summary>
        /// <param name="propId">The ID of the property type.</param>
        /// <returns>The index of the icon, or the default property or link icon if no custom
        /// icon has been registered.</returns>
        int GetPropTypeIconIndex( int propId );

        /// <summary>
        /// Registers a large (32x32) icon for the specified resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the icon is registered.</param>
        /// <param name="icon">The icon which is registered.</param>
        /// <remarks>The large icon is used as the icon of the resource edit window where
        /// the resource is edited.</remarks>
        void RegisterResourceLargeIcon( string resType, Icon icon );

        /// <summary>
        /// Returns the large (32x32) icon for the specified resource type.
        /// </summary>
        /// <param name="resType">The resource type for which the icon is returned.</param>
        /// <returns>The icon, or null if no icon has been registered.</returns>
        Icon GetResourceLargeIcon( string resType );

        /// <summary>
        /// Returns the image list containing all 16x16 resource icons.
        /// </summary>
        ImageList  ImageList      { get; }

        /// <summary>
        /// Returns the color depth of the icon image list.
        /// </summary>
        /// <remarks>The image list is 8-bit when running under Windows 2000 and 32-bit
        /// when running under Windows XP.</remarks>
        ColorDepth IconColorDepth { get; }

        Hashtable  CollectAssemblyIcons();
    }

    /// <summary>
    /// Describes the behavior of a resource type when added to a workspace.
    /// </summary>
    public enum WorkspaceResourceType
    {
        /// <summary>
        /// Drag and drop adds resource recursively. If added recursively, pulls resources by link type
        /// specified at registration. Linked resources are not shown in Other view.
        /// Examples: Newsgroup, Outlook folder.
        /// </summary>
        Container,

        /// <summary>
        /// Drag and drop adds resource non-recursively. Linked resources are shown in Other view.
        /// Examples: Category, Contact.
        /// </summary>
        Filter,

        /// <summary>
        /// Drag and drop adds resource recursively. If added recursively, pulls resources by
        /// Parent link. Linked resources are not shown in Other view.
        /// Examples: RSS feed group, News folder.
        /// </summary>
        Folder,

        /// <summary>
        /// Drag and drop adds resource non-recursively. Pulls linked resources by link types
        /// specified at registration.
        /// Examples: Email.
        /// </summary>
        None
    }

    /// <summary>
    /// Allows to register the relationships of resources to workspaces and to perform
    /// different operations on workspaces.
    /// </summary>
    public interface IWorkspaceManager
    {
        /// <summary>
        /// Registers a resource type which can interact with workspaces.
        /// </summary>
        /// <param name="resType">The resource type which is registered.</param>
        /// <param name="linkPropIDs">The IDs of the link properties which are followed to make
        /// other resources visible in the workspace when the resource is added to the workspace.</param>
        /// <param name="workspaceResourceType">The behavior of the resource type when added to the workspace.</param>
        void RegisterWorkspaceType( string resType, int[] linkPropIDs, WorkspaceResourceType workspaceResourceType );

        /// <summary>
        /// Registers a resource type which can interact with workspaces as a <see cref="WorkspaceResourceType.Container"/>.
        /// </summary>
        /// <param name="resType">The resource type which is registered.</param>
        /// <param name="linkPropIds">The IDs of the link properties which are followed to make
        /// other resources visible in the workspace when the resource is added to the workspace.</param>
        /// <param name="recurseLinkPropId">The ID of the link property which is followed to recurse
        /// the container hierarchy when a resource is added to the </param>
        void RegisterWorkspaceContainerType( string resType, int[] linkPropIds, int recurseLinkPropId );

        /// <summary>
        /// Registers a resource type which can interact with workspaces as a <see cref="WorkspaceResourceType.Folder"/>.
        /// </summary>
        /// <param name="resType">The resource type which is registered.</param>
        /// <param name="contentType">Obsolete; ignored.</param>
        /// <param name="linkPropIDs">The IDs of the link properties which are followed to make
        /// other resources visible in the workspace when the resource is added to the workspace.</param>
        void RegisterWorkspaceFolderType( string resType, string contentType, int[] linkPropIDs );

        /// <summary>
        /// Registers the filter which is used to restrict the resources shown in the tree-based
        /// selector of the workspace configuration dialog. The same filter is used for both
        /// "Available" and "In Workspace" trees.
        /// </summary>
        /// <param name="resType">The resource type (registered with <see cref="RegisterWorkspaceType"/>)
        /// for which the filter is used.</param>
        /// <param name="filter">The filter instance.</param>
        void RegisterWorkspaceSelectorFilter( string resType, IResourceNodeFilter filter );

        /// <summary>
        /// Registers the filters which are used to restrict the resources shown in the tree-based
        /// selector of the workspace configuration dialog. The filters used for "Available" and
        /// "In Workspace" trees are configured separately.
        /// </summary>
        /// <param name="resType">The resource type (registered with <see cref="RegisterWorkspaceType"/>)
        /// for which the filter is used.</param>
        /// <param name="availTreeFilter">The filter instance used for the "Available" tree.</param>
        /// <param name="workspaceTreeFilter">The filter instance used for the "In Workspace" tree.</param>
        /// <since>1.0.2</since>
        void RegisterWorkspaceSelectorFilter( string resType, IResourceNodeFilter availTreeFilter,
            IResourceNodeFilter workspaceTreeFilter );

        /// <summary>
        /// Creates a workspace with the specified name.
        /// </summary>
        /// <param name="name">The name of the workspace to create.</param>
        /// <returns>The workspace resource.</returns>
        IResource CreateWorkspace( string name );

        /// <summary>
        /// Deletes the specified workspace.
        /// </summary>
        /// <param name="workspace">A resource of type Workspace.</param>
        void DeleteWorkspace( IResource workspace );

        /// <summary>
        /// Adds the specified resource to a workspace.
        /// </summary>
        /// <param name="workspace">The workspace to which the resource is added.</param>
        /// <param name="res">The resource to add.</param>
        void AddResourceToWorkspace( IResource workspace, IResource res );

        /// <summary>
        /// Adds the specified resource and its child resources to a workspace.
        /// </summary>
        /// <param name="workspace">The workspace to which the resource is added.</param>
        /// <param name="res">The resource to add.</param>
        void AddResourceToWorkspaceRecursive( IResource workspace, IResource res );

        /// <summary>
        /// Adds all resources in the specified list to the workspace.
        /// </summary>
        /// <param name="workspace">The workspace to which the resources are added.</param>
        /// <param name="resList">The resources to add.</param>
        void AddResourcesToWorkspace( IResource workspace, IResourceList resList );

        /// <summary>
        /// If a workspace is active, adds the specified resource to the active workspace.
        /// </summary>
        /// <param name="res">The resource to add.</param>
        void AddToActiveWorkspace( IResource res );

        /// <summary>
        /// If a workspace is active, adds the specified resource and its children to the
        /// active workspace.
        /// </summary>
        /// <param name="res">The resource to add.</param>
        void AddToActiveWorkspaceRecursive( IResource res );

        /// <summary>
        /// Removes the specified resource from a workspace.
        /// </summary>
        /// <param name="workspace">The workspace from which the resource is removed.</param>
        /// <param name="res">The resource to be removed.</param>
        void RemoveResourceFromWorkspace( IResource workspace, IResource res );

        /// <summary>
        /// Removes all resources in the specified list from a workspace.
        /// </summary>
        /// <param name="workspace">The workspace from which the resources are removed.</param>
        /// <param name="resList">The resources to be removed.</param>
        void RemoveResourcesFromWorkspace( IResource workspace, IResourceList resList );

        /// <summary>
        /// Returns the list of resources visible in the specified workspace.
        /// </summary>
        /// <param name="workspace">The workspace for which the list is returned.</param>
        /// <returns>The list of resources visible in the workspace.</returns>
        IResourceList GetFilterList( IResource workspace );

        /// <summary>
        /// Returns the non-live list of resources added to the workspace which have the specified type.
        /// </summary>
        /// <param name="workspace">The workspace for which resources are returned.</param>
        /// <param name="resType">The resource type, or null if all resources should be returned.</param>
        /// <returns>The list of resources added to the workspace.</returns>
        /// <remarks>The returned list does not include resources which are visible in a workspace
        /// because their container or another linked resource was added to the workspace. For example,
        /// if an email folder was added to the workspace, the returned list includes only the folder,
        /// but not the emails in the folder.</remarks>
        IResourceList GetWorkspaceResources( IResource workspace, string resType );

        /// <summary>
        /// Returns the live list of resources added to the workspace which have the specified type.
        /// </summary>
        /// <param name="workspace">The workspace for which resources are returned.</param>
        /// <param name="resType">The resource type, or null if all resources should be returned.</param>
        /// <returns>The list of resources added to the workspace.</returns>
        /// <remarks>The returned list does not include resources which are visible in a workspace
        /// because their container or another linked resource was added to the workspace. For example,
        /// if an email folder was added to the workspace, the returned list includes only the folder,
        /// but not the emails in the folder.</remarks>
        IResourceList GetWorkspaceResourcesLive( IResource workspace, string resType );

        /// <summary>
        /// Returns the behavior of a resource type when added to the workspace.
        /// </summary>
        /// <param name="resType">The resource type name.</param>
        /// <returns>The behavior value, or <see cref="WorkspaceResourceType.None"/> if the resource
        /// type was not registered for workspace usage.</returns>
        WorkspaceResourceType GetWorkspaceResourceType( string resType );

        /// <summary>
        /// Returns the link type which is used for recursively traversing the workspace
        /// container resources with the specified content type, or the ID of the "Parent"
        /// link if no specific link has been registered.
        /// </summary>
        /// <param name="resType">The resource type registered for using in workspaces.</param>
        /// <returns>The ID of the link property type.</returns>
        int GetRecurseLinkPropId( string resType );

        /// <summary>
        /// Checks if the specified resource or one of its parents is recursively added to the specified
        /// workspace.
        /// </summary>
        /// <param name="workspace">The workspace to check.</param>
        /// <param name="res">The resource to check.</param>
        /// <returns>true if the resource or one of its parents is recursively added to the specified
        /// workspace, false otherwise.</returns>
        /// <since>2.0</since>
        bool IsInWorkspaceRecursive( IResource workspace, IResource res );

        /// <summary>
        /// Returns the list of workspaces to which the specified resource belongs.
        /// </summary>
        /// <param name="resource">The resource which is checked.</param>
        /// <returns>The array of workspaces, or an empty array if the resource does
        /// not belong to any workspace or no workspaces are defined.</returns>
        IResourceList GetResourceWorkspaces( IResource resource );

        /// <summary>
        /// Returns a live resource list of active workspaces.
        /// </summary>
        /// <returns></returns>
        IResourceList GetAllWorkspaces();

        /// <summary>
        /// Sets the name of the tab in Workspaces dialog in which the selector for the specified
        /// resource type is displayed.
        /// </summary>
        /// <remarks>If multiple resource types have the same tab name, they are displayed in the same tab.
        /// </remarks>
        /// <param name="resourceType">The workspace resource type.</param>
        /// <param name="tabName">The name of the tab in which it is displayed.</param>
        /// <since>2.0</since>
        void SetWorkspaceTabName( string resourceType, string tabName );

        /// <summary>
        /// If the parent of the specified resource is recursively added to a workspace,
        /// deletes the direct links from this resource to the workspace.
        /// </summary>
        /// <param name="res">The resource for which the links should be cleaned.</param>
        void CleanWorkspaceLinks( IResource res );

        /// <summary>
        /// Gets or sets the active workspace.
        /// </summary>
        /// <value>The active workspace resource, or null if no workspace is active.</value>
        IResource ActiveWorkspace { get; set; }

        /// <summary>
        /// Occurs when the active workspace changes.
        /// </summary>
        event EventHandler WorkspaceChanged;
    }

    /// <summary>
    /// Allows to perform operations with resource categories.
    /// </summary>
    public interface ICategoryManager
    {
        /// <summary>
        /// Returns the list of categories to which the specified resource belongs.
        /// </summary>
        /// <param name="resource">The resource for which the categories are retrieved.</param>
        /// <returns>The list of resources of type "Category".</returns>
        IResourceList GetResourceCategories( IResource resource );

        /// <summary>
        /// Adds the specified resource to the specified category.
        /// </summary>
        /// <param name="res">The resource to add.</param>
        /// <param name="category">The resource of type Category specifying the category
        /// to which the resource is added.</param>
        void AddResourceCategory( IResource res, IResource category );

        /// <summary>
        /// Set the specified category to the specified resource.
        /// </summary>
        /// <param name="res">The resource to add.</param>
        /// <param name="category">The resource of type Category specifying the category
        /// to which the resource is added.</param>
        void SetResourceCategory( IResource res, IResource category );

        /// <summary>
        /// Removes the specified resource from the specified category.
        /// </summary>
        /// <param name="res">The resource to remove.</param>
        /// <param name="category">The resource of type Category specifying the category
        /// from which the resource is removed.</param>
        void RemoveResourceCategory( IResource res, IResource category );

        /// <summary>
        /// Finds a category which has the specified parent category and name.
        /// </summary>
        /// <param name="parentCategory">The parent for the category to find,
        /// or null if top-level categories for all resource types should be searched.</param>
        /// <param name="name">The name of the category to find.</param>
        /// <returns>The category, or null if there is no category with the specified
        /// name at the specified level.</returns>
        IResource FindCategory( IResource parentCategory, string name );

        /// <summary>
        /// Finds a category with the specified name under the specified category,
        /// and creates a new category if no category with that name exists.
        /// </summary>
        /// <param name="parentCategory">The parent category.</param>
        /// <param name="name">The name of the category to find or create.</param>
        /// <returns>A resource of type Category.</returns>
        IResource FindOrCreateCategory( IResource parentCategory, string name );

        /// <summary>
        /// Returns the resource which is used as a root of the categories for the
        /// specified resource type.
        /// </summary>
        /// <remarks>The resource is created automatically if necessary.</remarks>
        /// <param name="resType">The resource type.</param>
        /// <returns>A resource of type ResourceTreeRoot.</returns>
        IResource GetRootForTypedCategory( string resType );

        /// <summary>
        /// Finds and returns the resource which is used as a root of the categories for the
        /// specified resource type.
        /// </summary>
        /// <remarks>If the resource is not found, returns null.</remarks>
        /// <param name="resType">The resource type.</param>
        /// <returns>A resource of type ResourceTreeRoot, or null if the specified root
        /// has not been created.</returns>
        /// <since>2.0</since>
        IResource FindRootForTypedCategory( string resType );

        /// <summary>
        /// Gets the resource which is the root of the untyped category tree.
        /// </summary>
        /// <since>2.0</since>
        IResource RootCategory
        {
            get;
        }

        /// <summary>
        /// Internal method, should not be used by plugins.
        /// </summary>
        bool CheckRenameCategory( IWin32Window parentWindow, IResource category, string newName );
    }

    /// <summary>
    /// Allows to receive notifications about changes in a specified set of resources.
    /// </summary>
    /// <remarks>Implementations of this interface can be used with
    /// <see cref="IResourceTreeManager.RegisterTreeListener"/> and
    /// <see cref="IResourceTreeManager.UnregisterTreeListener"/>.</remarks>
    /// <since>2.0</since>
    public interface IResourceListListener
    {
        /// <summary>
        /// Called when a resource is added to the set.
        /// </summary>
        /// <param name="res">The added resource.</param>
        void ResourceAdded( IResource res );

        /// <summary>
        /// Called before a resource is removed from the set.
        /// </summary>
        /// <param name="res">The resource being removed.</param>
        void ResourceDeleting( IResource res );

        /// <summary>
        /// Called when a resource belonging to the set is changed.
        /// </summary>
        /// <param name="res">The changed resource.</param>
        /// <param name="cs">The details of the changes to the resource.</param>
        void ResourceChanged( IResource res, IPropertyChangeSet cs );
    }

    /// <summary>
    /// Manages the hierarchies of resources in Omea.
    /// </summary>
    public interface IResourceTreeManager
    {
        /// <summary>
        /// Gets the resource which is the root of the tree displayed in the Views and Categories pane.
        /// </summary>
        IResource ResourceTreeRoot { get; }

        /// <summary>
        /// Gets the resource which is the root of the hierarchy of resources of the specified type.
        /// </summary>
        /// <remarks>The resource is created if necessary. A plugin can create a hierarchy for any
        /// resource type, if necessary.</remarks>
        /// <param name="resType">The resource type for which the root is returned.</param>
        /// <returns>The resource of type ResourceTreeRoot.</returns>
        IResource GetRootForType( string resType );

        /// <summary>
        /// Links the specified resource to the root of the Views and Categories resource tree
        /// at the specified position.
        /// </summary>
        /// <param name="res">The resource to be linked to the root.</param>
        /// <param name="index">
        /// <para>The relative position of the resource in the list. Resources
        /// are sorted by index in ascending order.</para>
        /// <para>The special values <see cref="int.MinValue"/> and <see cref="int.MaxValue"/> insert to the beginning and the end of the list, respectively.</para>
        /// </param>
        void LinkToResourceRoot( IResource res, int index );

        /// <summary>
        /// Specifies that the children of the specified node are sorted by values of the specified
        /// property.
        /// </summary>
        /// <remarks>The sorting mode is applied hierarchically down the tree unless a different
        /// sorting mode is specified for a child node.</remarks>
        /// <param name="node">The node for which the sorting mode is specified.</param>
        /// <param name="sortProps">The string containing a list of property names,
        /// in the format used by <see cref="IResourceList.Sort(string)"/>.</param>
        void SetResourceNodeSort( IResource node, string sortProps );

        /// <summary>
        /// Returns the list of properties by which the children of the specified node are
        /// sorted in the tree.
        /// </summary>
        /// <param name="node">The node for which the sorting mode is retrieved.</param>
        /// <returns>The list of property names, or null if no sorting mode has been specified
        /// for the specified node or any of its parents.</returns>
        string GetResourceNodeSort( IResource node );

        /// <summary>
        /// Specifies that the specified resource type has a separate hierarchy of views
        /// which does not contain the standard views defined for all resources.
        /// </summary>
        /// <param name="resType">The resource type which has a separate hierarchy.</param>
        void SetViewsExclusive( string resType );

        /// <summary>
        /// Checks if the specified resource type has a separate hierarchy of views.
        /// </summary>
        /// <param name="resType">The resource type to check.</param>
        /// <returns>true if the resource type has a separate hierarchy of views, false otherwise.</returns>
        bool AreViewsExclusive( string resType );

        /// <summary>
        /// <seealso cref="UnregisterTreeListener"/>
        /// Registers a listener for receiving notifications about changes in the children
        /// of the specified resource.
        /// </summary>
        /// <remarks>Using this method is more efficient than creating a separate resource list
        /// for monitoring changes to each node in the tree.</remarks>
        /// <param name="parent">The parent resource.</param>
        /// <param name="parentProp">The ID of the link property between the parent and the children.</param>
        /// <param name="listener">The listener which is being registered.</param>
        /// <since>2.0</since>
        void RegisterTreeListener( IResource parent, int parentProp, IResourceListListener listener );

        /// <summary>
        /// <seealso cref="RegisterTreeListener"/>
        /// Unregisters a listener for receiving notifications about changes in the children
        /// of the specified resource.
        /// </summary>
        /// <param name="parent">The parent resource.</param>
        /// <param name="parentProp">The ID of the link property between the parent and the children.</param>
        /// <param name="listener">The listener which is being unregistered.</param>
        /// <since>2.0</since>
        void UnregisterTreeListener( IResource parent, int parentProp, IResourceListListener listener );
    }

    /// <summary>
    /// Manages the rules that are used for notifying the user about arriving resources.
    /// </summary>
    public interface INotificationManager
    {
        /// <summary>
        /// Registers a resource type for which the "Notify Me" feature can be used.
        /// </summary>
        /// <param name="resType">Type of the resources for which "Notify Me" is invoked.</param>
        /// <param name="ruleResType">
        ///   Type of the resources processed by "Notify Me" rules for the resource type.
        /// </param>
        void RegisterNotifyMeResourceType( string resType, string ruleResType );

        /// <summary>
        /// Registers the condition template that is offered to the user in the "Notify Me"
        /// dialog when it is invoked for a resource of the specific type. Multiple conditions
        /// can be registered for the same resource type - in this case, the conditions are shown
        /// with checkboxes.
        /// </summary>
        /// <param name="resType">The resource type for which the notify condition is registered.</param>
        /// <param name="conditionTemplate">The template which will be used in the notification rule.</param>
        /// <param name="linkPropId">ID of the link type which links the target resource to the
        /// resource which should be used as the condition parameter. (Specify 0 if the target resource
        /// itself should be used as the parameter.) For example, if you want to offer "From 'sender'"
        /// as the condition, specify the ID of the "From" property in the parameter.</param>
        /// <remarks>The notification registration is not stored in the resource store and needs to be
        /// performed on every run of the plugin.</remarks>
        void RegisterNotifyMeCondition( string resType, IResource conditionTemplate, int linkPropId );

        /// <summary>
        /// Returns the list of "Notify Me" condition templates for the specified resource type.
        /// </summary>
        /// <param name="resType"></param>
        /// <returns>Array of resources of type ConditionTemplate</returns>
        IResource[] GetNotifyMeConditions( string resType );

        /// <summary>
        /// Returns the link type which links the target resource to the resource which should be used
        /// as the condition parameter, for the specified resource type and condition template.
        /// </summary>
        /// <param name="resType"></param>
        /// <param name="conditionTemplate"></param>
        /// <returns>Link type ID, or 0 if the resource itself should be used as the condition parameter.</returns>
        int GetConditionLinkType( string resType, IResource conditionTemplate );

        /// <summary>
        /// Returns the resource type for which the rules are created, given the resource type
        /// for which the dialog is invoked.
        /// </summary>
        /// <param name="resType">The resource type for which the dialog is invoked.</param>
        /// <returns>The resource type for which the rules are created.</returns>
        string GetRuleResourceType( string resType );
    }

    /// <summary>
    /// Represents a method which is called when the resource edited through
    /// <see cref="IUIManager.OpenResourceEditWindow"/> is saved.
    /// </summary>
    public delegate void EditedResourceSavedDelegate( IResource res, object tag );

    /// <summary>
    /// Represents a method which is called to validate a string entered in
    /// the dialog shown by <see cref="IUIManager.InputString"/>.
    /// </summary>
    public delegate void ValidateStringDelegate( string value, ref string validateErrorMessage );

    /// <summary>
    /// Interface for managing calls, which could be invoked from outside of Omea
    /// process via RPC calls.
    /// </summary>
    /// <since>2.0</since>
    public interface IRemoteControlManager
    {
        /// <summary>
        /// Add new call, whcih could be invoked via RPC mechanism.
        /// </summary>
        /// <param name="rcName">Name of proceudre, exported to clients.</param>
        /// <param name="method">Method to call.</param>
        /// <returns>true if call was registered, false if method with this name already exists.</returns>
        /// <remarks>
        /// Procedure can have any number of parameters of types string, int and
        /// bool. It must return string with XML as result or throw exception with
        /// meaningful message in case of error.
        /// </remarks>
        /// <exception cref="ArgumentException">Will be throwed if passed delegate
        /// have arguments of unknown type or return type is not string.</exception>
        bool AddRemoteCall(string rcName, Delegate method);
    }
}
