/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Specifies the data type of a property.
    /// </summary>
    public enum PropDataType
    {
        /// <summary>
        /// Values of the property are 32-bit integers.
        /// </summary>
        Int, 

        /// <summary>
        /// Values of the property are Unicode strings.
        /// </summary>
        String, 

        /// <summary>
        /// Values of the property are <see cref="DateTime"/> values.
        /// </summary>
        Date, 
        
        /// <summary>
        /// Values of the property are links to other resources.
        /// </summary>
        Link, 
        
        /// <summary>
        /// Values of the property are BLOBs (binary large objects), accessible as
        /// read-write streams.
        /// </summary>
        Blob, 
        
        /// <summary>
        /// Values of the property are double precision floating-point values.
        /// </summary>
        Double, 
        
        /// <summary>
        /// Values of the property are Unicode strings which cannot be used in
        /// searches by value (for example, text of email messages).
        /// </summary>
        LongString, 
        
        /// <summary>
        /// Values of the property are boolean (true/false).
        /// </summary>
        Bool, 
        
        /// <summary>
        /// Values of the property are variable-sized lists of strings which
        /// preserve the ordering.
        /// </summary>
        StringList
    };

    public class PropDataTypeGeneric<T>
    {
        private PropDataType _type;

        internal PropDataTypeGeneric(PropDataType type)
        {
            _type = type;
        }

        public PropDataType Type
        {
            get { return _type; }
        }
    }

    public class PropDataTypes
    {
        public static readonly PropDataTypeGeneric<int> Int = new PropDataTypeGeneric<int>(PropDataType.Int);
        public static readonly PropDataTypeGeneric<string> String = new PropDataTypeGeneric<string>(PropDataType.String);
        public static readonly PropDataTypeGeneric<DateTime> DateTime = new PropDataTypeGeneric<DateTime>(PropDataType.Date);
        public static readonly PropDataTypeGeneric<IResource> Link = new PropDataTypeGeneric<IResource>(PropDataType.Link);
        public static readonly PropDataTypeGeneric<Stream> Blob = new PropDataTypeGeneric<Stream>(PropDataType.Blob);
        public static readonly PropDataTypeGeneric<double> Double = new PropDataTypeGeneric<double>(PropDataType.Double);
        public static readonly PropDataTypeGeneric<string> LongString = new PropDataTypeGeneric<string>(PropDataType.LongString);
        public static readonly PropDataTypeGeneric<bool> Bool = new PropDataTypeGeneric<bool>(PropDataType.Bool);
        public static readonly PropDataTypeGeneric<IStringList> StringList = new PropDataTypeGeneric<IStringList>(PropDataType.StringList);
    }
    

    /// <summary>
    /// Specifies the special attributes of a property type.
    /// </summary>
    [Flags]
    public enum PropTypeFlags
    {
        /// <summary>
        /// The property type has no special attributes.
        /// </summary>
        Normal = 0, 
        
        /// <summary>
        /// Properties of the type are internal to the system and are never shown to
        /// the user by the core UI components.
        /// </summary>
        Internal = 1, 
        
        /// <summary>
        /// Properties of the type link a contact to its accounts (email, ICQ and others).
        /// The flag can only be set on link properties.
        /// </summary>
        ContactAccount = 2, 
        
        /// <summary>
        /// Properties of the type are directed links. The flag can only be set on link
        /// properties.
        /// </summary>
        DirectedLink = 4, 
        
        /// <summary>
        /// Properties of the type link resources which can be unread to containers which
        /// maintain unread counts. Changing the IsUnread property of the resource on one
        /// end of the link increments or decrements the UnreadCount property of the resource
        /// on the other end of the link. The flag can only be set on link properties.
        /// </summary>
        CountUnread = 16,

        /// <summary>
        /// Properties of the type are never included in the XML serialization of resources.
        /// </summary>
        NoSerialize = 32, 
        
        /// <summary>
        /// The user is prompted whether the properties of the type should be included in the
        /// XML serialization of resources. The flag can only be set on non-link properties
        /// (the prompting behavior is the default for link properties).
        /// </summary>
        AskSerialize = 64, 
        
        /// <summary>
        /// Properties of the type link a file format resource (TXT, HTML, PDF and so on)
        /// to a resource which provides the stream from which the format resource is 
        /// loaded (file folder, email attachment, cached file from the Web and so on).
        /// The flag can only be set on link properties.
        /// </summary>
        SourceLink = 128,

        /// <summary>
        /// Properties of the type can only be returned by virtual property providers and never exist
        /// on actual resources.
        /// </summary>
        Virtual = 256
    }

    /// <summary>
    /// Specifies the special attributes of a property type.
    /// </summary>
    [Flags]
    public enum ResourceTypeFlags
    {
        /// <summary>
        /// The property type has no special attributes.
        /// </summary>
        Normal = 0, 

        /// <summary>
        /// Resources of the type are internal to the system and are never shown to
        /// the user by the core UI components.
        /// </summary>
        Internal = 1, 

        /// <summary>
        /// Resources of the type can have Parent links from other resources, and thus
        /// can be expanded in the resource tree views.
        /// </summary>
        ResourceContainer = 2,
        
        /// <summary>
        /// Resources of the type never provide text which is indexed by the full-text
        /// indexer.
        /// </summary>
        NoIndex = 4, 
        
        /// <summary>
        /// Resources of the type can be unread (have the IsUnread property set).
        /// </summary>
        CanBeUnread = 8, 
        
        /// <summary>
        /// Resources of the type are file format resources (TXT, HTML, PDF and so on),
        /// which get their content streams from stream provider resources.
        /// </summary>
        FileFormat = 16
    }

    /// <summary>
    /// Represents a single property type registered in the resource store.
    /// </summary>
    public interface IPropType
    {
        /// <summary>
        /// Gets the numeric ID of the property type.
        /// </summary>
        int           Id                 { get; }

        /// <summary>
        /// Gets the internal (non-localized) name of the property type.
        /// </summary>
        string        Name               { get; }
        
        /// <summary>
        /// The data type of the property type.
        /// </summary>
        PropDataType  DataType           { get; }

        /// <summary>
        /// Gets or sets the flags of the property type.
        /// </summary>
        PropTypeFlags Flags { get; set; }

        /// <summary>
        /// The user-visible name of the property type, or the name displayed at the "From" end
        /// of a directed link.
        /// </summary>
        string        DisplayName        { get; }

        /// <summary>
        /// For directed links - the name displayed at the "To" end of a directed link.
        /// </summary>
        string        ReverseDisplayName { get; }

        /// <summary>
        /// Gets the value signifying whether the plugin owning the property type is loaded.
        /// </summary>
        /// <value>true if no owner plugin has been assigned for the property type or if
        /// at least one of the owner plugins is loaded; false otherwise.</value>
        bool OwnerPluginLoaded { get; }

        /// <summary>
        /// Checks if the property type has the specified flag.
        /// </summary>
        /// <param name="flag">The flag to check.</param>
        /// <returns>true if the property type has the flag, false otherwise.</returns>
        bool HasFlag( PropTypeFlags flag );
    }

    /// <summary>
    /// The collection of all property types registered in the resource store.
    /// </summary>
    public interface IPropTypeCollection: IEnumerable
    {
        /// <summary>
        /// Returns the property type with the specified ID.
        /// </summary>
        IPropType this [int id] { get; }

        /// <summary>
        /// Returns the property type with the specified name.
        /// </summary>
        IPropType this [string name] { get; }

        /// <summary>
        /// Returns the count of property types registered in the resource store.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Checks if all property types in the specified list are registered.
        /// </summary>
        /// <param name="propNames">The name or names of property types to check.</param>
        /// <returns>true if all the specified property type names are registered, false otherwise.</returns>
        bool Exist( params string[] propNames );
        
        /// <summary>
        /// Registers a new property type with the default flags.
        /// </summary>
        /// <param name="name">The name of the property type to register.</param>
        /// <param name="dataType">The data type of the property.</param>
        /// <returns>The ID of the property type.</returns>
        /// <remarks><para>If a property type with the specified name already exists, the function
        /// checks if it is compatible with the existing definition. If the data type does not match,
        /// an exception is thrown.</para>
        /// <para>To avoid conflicts with properties used by Omea core and standard plugins, 
        /// it is recommended to prefix the names of the plugin-defined properties with the domain 
        /// name of the plugin Web site (for example, <c>com.jetbrains.Name</c>). 
        /// Standard properties do not have any dotted prefixes. Custom properties defined by the
        /// user have the prefix <c>Custom.</c></para></remarks>
        int Register( string name, PropDataType dataType );

        PropId<T> Register<T>(string name, PropDataTypeGeneric<T> dataType);
        
        /// <summary>
        /// Registers a new property type with the specified flags.
        /// </summary>
        /// <param name="name">The name of the property type to register.</param>
        /// <param name="dataType">The data type of the property.</param>
        /// <param name="flags">The flags of the property type.</param>
        /// <returns>The ID of the property type.</returns>
        /// <remarks><para>If a property type with the specified name already exists, the function
        /// checks if it is compatible with the existing definition. If the data type does not match,
        /// an exception is thrown. If the flags do not match, the existing flags are ORed with
        /// the flags specified as the method parameter.</para>
        /// <para>To avoid conflicts with properties used by Omea core and standard plugins, 
        /// it is recommended to prefix the names of the plugin-defined properties with the domain 
        /// name of the plugin Web site (for example, <c>com.jetbrains.Name</c>). 
        /// Standard properties do not have any dotted prefixes. Custom properties defined by the
        /// user have the prefix <c>Custom.</c></para></remarks>
        int Register( string name, PropDataType dataType, PropTypeFlags flags );

        PropId<T> Register<T>(string name, PropDataTypeGeneric<T> dataType, PropTypeFlags flags);

        /// <summary>
        /// Registers a new property type with the specified flags and owner plugin.
        /// </summary>
        /// <param name="name">The name of the property type to register.</param>
        /// <param name="dataType">The data type of the property.</param>
        /// <param name="flags">The flags of the property type.</param>
        /// <param name="ownerPlugin">The plugin which owns the property type. The property type
        /// and the properties of that type will be hidden from several places in the program
        /// interface if the owner plugin is not loaded.</param>
        /// <returns>The ID of the property type.</returns>
        /// <remarks><para>If a property type with the specified name already exists, the function
        /// checks if it is compatible with the existing definition. If the data type does not match,
        /// an exception is thrown. If the flags do not match, the existing flags are ORed with
        /// the flags specified as the method parameter.</para>
        /// <para>To avoid conflicts with properties used by Omea core and standard plugins, 
        /// it is recommended to prefix the names of the plugin-defined properties with the domain 
        /// name of the plugin Web site (for example, <c>com.jetbrains.Name</c>). 
        /// Standard properties do not have any dotted prefixes. Custom properties defined by the
        /// user have the prefix <c>Custom.</c></para></remarks>
        int Register( string name, PropDataType dataType, PropTypeFlags flags, IPlugin ownerPlugin );

        PropId<T> Register<T>(string name, PropDataTypeGeneric<T> dataType, PropTypeFlags flags, IPlugin ownerPlugin);

        /// <summary>
        /// Registers a user-visible name for a property type.
        /// </summary>
        /// <param name="propId">The ID of the property type for which the display name
        /// is registered.</param>
        /// <param name="displayName">The display name to be used for the property.</param>
        /// <remarks>This overload should be used for non-link properties and for not directed
        /// links. For directed links, the <see cref="RegisterDisplayName(int, string, string)">
        /// three-parameter overload</see> must be used.</remarks>
        void RegisterDisplayName( int propId, string displayName );

        void RegisterDisplayName<T>(PropId<T> propId, string displayName);
        
        /// <summary>
        /// Registers a user-visible name for a directed link property type.
        /// </summary>
        /// <param name="propId">The ID of the property type for which the display name
        /// is registered.</param>
        /// <param name="fromDisplayName">The display name shown at the source end of the
        /// link.</param>
        /// <param name="toDisplayName">The display name shown at the target end of the
        /// link.</param>
        void RegisterDisplayName( int propId, string fromDisplayName, string toDisplayName );

        void RegisterDisplayName(PropId<IResource> propId, string fromDisplayName, string toDisplayName);
        
        /// <summary>
        /// Deletes a property type.
        /// </summary>
        /// <param name="id">The ID of the property type to delete.</param>
        /// <remarks>Deleting a property type deletes all properties of that type.</remarks>
        void Delete( int id );

        /// <summary>
        /// Returns the display name for the property with the specified ID.
        /// </summary>
        /// <param name="propId">ID of the property for which the display name is checked.</param>
        /// <returns>The display name of the property.</returns>
        /// <remarks>If <paramref name="propId"/> is negative and specifies the ID of a
        /// directed link property, the display name for the link target end is returned.</remarks>
        string GetPropDisplayName( int propId );
    }

    /// <summary>
    /// Represents a single resource type registered in the resource store.
    /// </summary>
    public interface IResourceType
    {
        /// <summary>
        /// Gets the numeric ID of the resource type.
        /// </summary>
        int               Id                  { get; }

        /// <summary>
        /// Gets the internal (non-localized) name of the resource type.
        /// </summary>
        /// <remarks>This is the string which is used as "resource type" parameter for all core 
        /// methods.</remarks>
        string            Name                { get; }

        /// <summary>
        /// Gets the user-visible name of the resource type.
        /// </summary>
        /// <remarks>By default, returns the same string as <see cref="Name"/>.</remarks>
        string            DisplayName         { get; set; }

        /// <summary>
        /// Gets or sets the resource display name template of the resource type.
        /// </summary>
        string ResourceDisplayNameTemplate    { get; set; }
        
        /// <summary>
        /// Gets the flags of the resource type.
        /// </summary>
        ResourceTypeFlags Flags               { get; set; }

        /// <summary>
        /// Gets the value signifying whether the plugin owning the resource type is loaded.
        /// </summary>
        /// <value>true if no owner plugin has been assigned for the resource type or if
        /// at least one of the owner plugins is loaded; false otherwise.</value>
        bool OwnerPluginLoaded                { get; }

        /// <summary>
        /// Checks if the resource type has the specified flag.
        /// </summary>
        /// <param name="flag">The flag to check.</param>
        /// <returns>true if the resource type has the flag, false otherwise.</returns>
        bool HasFlag( ResourceTypeFlags flag );
    }

    /// <summary>
    /// The collection of all resource types registered in the resource store.
    /// </summary>
    public interface IResourceTypeCollection: IEnumerable
    {
        /// <summary>
        /// Returns the resource type with the specified ID.
        /// </summary>
        IResourceType this [int id]      { get; }
        
        /// <summary>
        /// Returns the resource type with the specified name.
        /// </summary>
        IResourceType this [string name] { get; }

        /// <summary>
        /// Returns the count of resource types registered in the resource store.
        /// </summary>
        int Count { get; }
        
        /// <summary>
        /// Checks if all resource types in the specified list are registered.
        /// </summary>
        /// <param name="resourceTypeNames">The name or names of resource types to check.</param>
        /// <returns>true if all the specified resource type names are registered, false otherwise.</returns>
        bool Exist( params string[] resourceTypeNames );

        /// <summary>
        /// Registers a new resource type with the default flags.
        /// </summary>
        /// <param name="name">The name of the resource type to register.</param>
        /// <param name="resourceDisplayNameTemplate">The template for building display names
        /// of resources having that type.</param>
        /// <returns>The ID of the registered resource type.</returns>
        /// <remarks><para>If a resource type with the specified name already exists, the function
        /// checks if it is compatible with the existing definition. If the display name template
        /// does not match, an exception is thrown.</para>
        /// <para>To avoid conflicts with resource types used by Omea core and standard plugins, 
        /// it is recommended to prefix the names of the plugin-defined resource types with the domain 
        /// name of the plugin Web site (for example, <c>com.jetbrains.Email</c>). 
        /// Standard resource types do not have any dotted prefixes.</para>
        /// <para>The display name template is a space-separated list of property names from which 
        /// the display names of resources are constructed. The display name template can contain
        /// several alternatives, separated with the | character. Alternatives are evaluated in sequence,
        /// until one is found which produces a non-empty result. For example, if the display name
        /// template is <c>FirstName LastName | EmailAcct</c>, the display name of the resource will
        /// be composed of the values of <c>FirstName</c> and <c>LastName</c> properties if
        /// at least one of these properties is set, or of the value of the <c>EmailAcct</c> property
        /// if neither first name or last name are specified.</para>
        /// <para>If a link property is used in a display name template, it is evaluated as the display 
        /// name of the resource on the other end of the link (or the names of all resources on the other 
        /// end of the link, separated with commas).</para>
        /// <para>All property types referenced in the display name template must be registered before
        /// the resource type is registered.</para></remarks>
        int Register( string name, string resourceDisplayNameTemplate );

        /// <summary>
        /// Registers a new resource type with the specified flags.
        /// </summary>
        /// <param name="name">The name of the resource type to register.</param>
        /// <param name="resourceDisplayNameTemplate">The template for building display names
        /// of resources having that type.</param>
        /// <param name="flags">The flags of the resource type.</param>
        /// <returns>The ID of the registered resource type.</returns>
        /// <remarks><para>If a resource type with the specified name already exists, the function
        /// checks if it is compatible with the existing definition. If the display name template
        /// does not match, an exception is thrown. If the flags do not match, the existing flags 
        /// are ORed with the flags specified as the method parameter.</para>
        /// <para>To avoid conflicts with resource types used by Omea core and standard plugins, 
        /// it is recommended to prefix the names of the plugin-defined resource types with the domain 
        /// name of the plugin Web site (for example, <c>com.jetbrains.Email</c>). 
        /// Standard resource types do not have any dotted prefixes.</para>
        /// <para>The display name template is a space-separated list of property names from which 
        /// the display names of resources are constructed. The display name template can contain
        /// several alternatives, separated with the | character. Alternatives are evaluated in sequence,
        /// until one is found which produces a non-empty result. For example, if the display name
        /// template is <c>FirstName LastName | EmailAcct</c>, the display name of the resource will
        /// be composed of the values of <c>FirstName</c> and <c>LastName</c> properties if
        /// at least one of these properties is set, or of the value of the <c>EmailAcct</c> property
        /// if neither first name or last name are specified.</para>
        /// <para>If a link property is used in a display name template, it is evaluated as the display 
        /// name of the resource on the other end of the link (or the names of all resources on the other 
        /// end of the link, separated with commas).</para>
        /// <para>All property types referenced in the display name template must be registered before
        /// the resource type is registered.</para></remarks>
        int Register( string name, string resourceDisplayNameTemplate, ResourceTypeFlags flags );

        /// <summary>
        /// Registers a new resource type with the default flags and the specified user-visible name.
        /// </summary>
        /// <param name="name">The name of the resource type to register.</param>
        /// <param name="displayName">The user-visible name of the resource type.</param>
        /// <param name="resourceDisplayNameTemplate">The template for building display names
        /// of resources having that type.</param>
        /// <returns>The ID of the registered resource type.</returns>
        /// <remarks><para>If a resource type with the specified name already exists, the function
        /// checks if it is compatible with the existing definition. If the display name template
        /// does not match, an exception is thrown.</para>
        /// <para>To avoid conflicts with resource types used by Omea core and standard plugins, 
        /// it is recommended to prefix the names of the plugin-defined resource types with the domain 
        /// name of the plugin Web site (for example, <c>com.jetbrains.Email</c>). 
        /// Standard resource types do not have any dotted prefixes.</para>
        /// <para>The display name template is a space-separated list of property names from which 
        /// the display names of resources are constructed. The display name template can contain
        /// several alternatives, separated with the | character. Alternatives are evaluated in sequence,
        /// until one is found which produces a non-empty result. For example, if the display name
        /// template is <c>FirstName LastName | EmailAcct</c>, the display name of the resource will
        /// be composed of the values of <c>FirstName</c> and <c>LastName</c> properties if
        /// at least one of these properties is set, or of the value of the <c>EmailAcct</c> property
        /// if neither first name or last name are specified.</para>
        /// <para>If a link property is used in a display name template, it is evaluated as the display 
        /// name of the resource on the other end of the link (or the names of all resources on the other 
        /// end of the link, separated with commas).</para>
        /// <para>All property types referenced in the display name template must be registered before
        /// the resource type is registered.</para></remarks>
        int Register( string name, string displayName, string resourceDisplayNameTemplate );

        /// <summary>
        /// Registers a new resource type with the specified flags and user-visible name.
        /// </summary>
        /// <param name="name">The name of the resource type to register.</param>
        /// <param name="displayName">The user-visible name of the resource type.</param>
        /// <param name="resourceDisplayNameTemplate">The template for building display names
        /// of resources having that type.</param>
        /// <param name="flags">The flags of the resource type.</param>
        /// <returns>The ID of the registered resource type.</returns>
        /// <remarks><para>If a resource type with the specified name already exists, the function
        /// checks if it is compatible with the existing definition. If the display name template
        /// does not match, an exception is thrown. If the flags do not match, the existing flags 
        /// are ORed with the flags specified as the method parameter.</para>
        /// <para>To avoid conflicts with resource types used by Omea core and standard plugins, 
        /// it is recommended to prefix the names of the plugin-defined resource types with the domain 
        /// name of the plugin Web site (for example, <c>com.jetbrains.Email</c>). 
        /// Standard resource types do not have any dotted prefixes.</para>
        /// <para>The display name template is a space-separated list of property names from which 
        /// the display names of resources are constructed. The display name template can contain
        /// several alternatives, separated with the | character. Alternatives are evaluated in sequence,
        /// until one is found which produces a non-empty result. For example, if the display name
        /// template is <c>FirstName LastName | EmailAcct</c>, the display name of the resource will
        /// be composed of the values of <c>FirstName</c> and <c>LastName</c> properties if
        /// at least one of these properties is set, or of the value of the <c>EmailAcct</c> property
        /// if neither first name or last name are specified.</para>
        /// <para>If a link property is used in a display name template, it is evaluated as the display 
        /// name of the resource on the other end of the link (or the names of all resources on the other 
        /// end of the link, separated with commas).</para>
        /// <para>All property types referenced in the display name template must be registered before
        /// the resource type is registered.</para></remarks>
        int Register( string name, string displayName, string resourceDisplayNameTemplate, 
            ResourceTypeFlags flags );

        /// <summary>
        /// Registers a new resource type with the specified flags, user-visible name
        /// and owner plugin.
        /// </summary>
        /// <param name="name">The name of the resource type to register.</param>
        /// <param name="displayName">The user-visible name of the resource type.</param>
        /// <param name="resourceDisplayNameTemplate">The template for building display names
        /// of resources having that type.</param>
        /// <param name="flags">The flags of the resource type.</param>
        /// <param name="ownerPlugin">The plugin which owns the resource type. The resource type
        /// and the resources of that type will be hidden from several places in the program
        /// interface if the owner plugin is not loaded.</param>
        /// <returns>The ID of the registered resource type.</returns>
        /// <remarks><para>If a resource type with the specified name already exists, the function
        /// checks if it is compatible with the existing definition. If the display name template
        /// does not match, an exception is thrown. If the flags do not match, the existing flags 
        /// are ORed with the flags specified as the method parameter.</para>
        /// <para>To avoid conflicts with resource types used by Omea core and standard plugins, 
        /// it is recommended to prefix the names of the plugin-defined resource types with the domain 
        /// name of the plugin Web site (for example, <c>com.jetbrains.Email</c>). 
        /// Standard resource types do not have any dotted prefixes.</para>
        /// <para>The display name template is a space-separated list of property names from which 
        /// the display names of resources are constructed. The display name template can contain
        /// several alternatives, separated with the | character. Alternatives are evaluated in sequence,
        /// until one is found which produces a non-empty result. For example, if the display name
        /// template is <c>FirstName LastName | EmailAcct</c>, the display name of the resource will
        /// be composed of the values of <c>FirstName</c> and <c>LastName</c> properties if
        /// at least one of these properties is set, or of the value of the <c>EmailAcct</c> property
        /// if neither first name or last name are specified.</para>
        /// <para>If a link property is used in a display name template, it is evaluated as the display 
        /// name of the resource on the other end of the link (or the names of all resources on the other 
        /// end of the link, separated with commas).</para>
        /// <para>All property types referenced in the display name template must be registered before
        /// the resource type is registered.</para></remarks>
        int Register( string name, string displayName, string resourceDisplayNameTemplate, 
            ResourceTypeFlags flags, IPlugin ownerPlugin );

        /// <summary>
        /// Deletes the resource type with the specified name.
        /// </summary>
        /// <param name="name">The name of the resource type to delete.</param>
        /// <remarks>Deleting a resource type deletes all resources with that type.</remarks>
        void Delete( string name );
    }

    /// <summary>
    /// Specifies the update notification mode of a resource list.
    /// </summary>
    public enum SelectionType
    {
        /// <summary>
        /// Indicates that no update notifications are sent for the resource list after
        /// it has been instantiated.
        /// </summary>
        Normal, 
        
        /// <summary>
        /// Indicates that all changes in a resource list (new resources, deleted resources,
        /// resource changes) cause update notifications to be sent for the resource list.
        /// </summary>
        Live, 
        
        /// <summary>
        /// Applies only to resource lists which have been returned by one of the
        /// <see cref="IResourceStore.FindResources(string, int, object)"/> family 
        /// of methods. Indicates that  all changes in a resource list cause update 
        /// notifications to be sent, with one exception: changes of the property 
        /// by which the selection was made never cause the resources to be deleted from
        /// the list, even if the new property value no longer matches the selection
        /// condition.
        /// </summary>
        LiveSnapshot
    };

    /// <summary><seealso cref="ResourceProxy"/>
    /// The main interface for creating and accessing resources.
    /// </summary>
    /// <remarks>At most times when Omea is running (except for the time when the
    /// <see cref="IPlugin.Register"/> method is called), only one thread is designated
    /// as the resource store write thread, and all operations that modify the resource store
    /// (creating resources, changing resource properties, deleting resources) must be executed
    /// in that thread. The <see cref="ResourceProxy"/> class provides an easy way to run a resource write
    /// operation synchronously or asynchronously.</remarks>
    public interface IResourceStore
    {
        /// <summary>
        /// Returns the ID of the property with the specified name.
        /// </summary>
        /// <param name="name">Name of the property to look up.</param>
        /// <returns>The ID of the property with the specified name.</returns>
        /// <remarks>If no property with that name has been registered, an exception
        /// is thrown.</remarks>
        int GetPropId( string name );
        
        /// <summary>
        /// Returns the collection of all property types registered in the resource store.
        /// </summary>
        IPropTypeCollection     PropTypes     { get; }

        /// <summary>
        /// Returns the collection of all resource types registered in the resource store.
        /// </summary>
        IResourceTypeCollection ResourceTypes { get; }

        /// <summary>
        /// Creates a new resource of the specified type.
        /// </summary>
        /// <param name="type">The type of the resource to create.</param>
        /// <returns>The new resource instance.</returns>
        IResource NewResource( string type );

        /// <summary>
        /// Creates a new resource of the specified type and begins a batch update operation for it.
        /// </summary>
        /// <param name="type">The type of the resource to create.</param>
        /// <returns>The new resource instance.</returns>
        /// <remarks>After the initial properties are set, <see cref="IResource.EndUpdate"/> must
        /// be called. Only after that notifications about the resource creation will be processed.
        /// </remarks>
        IResource BeginNewResource( string type );

        /// <summary>
        /// Creates a new in-memory resource of the specified type.
        /// </summary>
        /// <param name="type">The type of the resource to create.</param>
        /// <returns>The new resource instance.</returns>
        /// <remarks>The resource exists only in the memory, and is not returned by any queries,
        /// until the <see cref="IResource.EndUpdate"/> method is called. Transient resources
        /// can be created, modified and deleted in any thread (since all these operations are
        /// performed in memory and do not involve any actual resource store modifications), but
        /// the <see cref="IResource.EndUpdate"/> must be run in the resource store write thread.</remarks>
        IResource NewResourceTransient( string type );
        
        /// <summary>
        /// Returns the existing resource with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the resource to load.</param>
        /// <returns>The instance of the resource.</returns>
        /// <remarks>If the resource exists in the resource cache, the cached copy is
        /// returned. Otherwise, the resource is loaded from the disk.
        /// </remarks>
        IResource LoadResource( int id );

        /// <summary>
        /// Returns the resource with the specified ID if the resource is valid, or null
        /// if the resource with the specified ID has been deleted or no longer exists.
        /// </summary>
        /// <param name="id">The ID of the resource to load.</param>
        /// <returns>The instance of the resource or null.</returns>
        /// <since>1.0.2</since>
        IResource TryLoadResource( int id );

        /// <summary>
        /// Returns the non-live list of resources for which the property with the specified ID 
        /// has the specified value.
        /// </summary>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propId">The ID of the property for which the selection is done.</param>
        /// <param name="propValue">The value of the property.</param>
        /// <returns>The list of resources, or an empty resource list if no resources match the condition.</returns>
        /// <remarks>Selection on bool properties is only supported if <paramref name="propValue"/> is true.
        /// Selection on link, long string, double and blob properties is not supported.</remarks>
        IResourceList FindResources( string resType, int propId, object propValue );

        /// <summary>
        /// Returns the non-live list of resources for which the property with the specified name
        /// has the specified value.
        /// </summary>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propName">The name of the property for which the selection is done.</param>
        /// <param name="propValue">The value of the property.</param>
        /// <returns>The list of resources, or an empty resource list if no resources match the condition.</returns>
        /// <remarks>Selection on bool properties is only supported if <paramref name="propValue"/> is true.
        /// Selection on link, long string, double and blob properties is not supported.</remarks>
        IResourceList FindResources( string resType, string propName, object propValue );

        IResourceList FindResources<T>(string resType, PropId<T> propId, T propValue);

        BusinessObjectList<T> FindResources<T, V>(ResourceTypeId<T> resType, PropId<V> propId, V propValue) where T : BusinessObject;
        
        /// <summary>
        /// Returns the live list of resources for which the property with the specified ID 
        /// has the specified value.
        /// </summary>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propId">The ID of the property for which the selection is done.</param>
        /// <param name="propValue">The value of the property.</param>
        /// <returns>The list of resources, or an empty resource list if no resources match the condition.</returns>
        /// <remarks>Selection on bool properties is only supported if <paramref name="propValue"/> is true.
        /// Selection on link, long string, double and blob properties is not supported.</remarks>
        IResourceList FindResourcesLive( string resType, int propId, object propValue );

        /// <summary>
        /// Returns the live list of resources for which the property with the specified name
        /// has the specified value.
        /// </summary>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propName">The name of the property for which the selection is done.</param>
        /// <param name="propValue">The value of the property.</param>
        /// <returns>The list of resources, or an empty resource list if no resources match the condition.</returns>
        /// <remarks>Selection on bool properties is only supported if <paramref name="propValue"/> is true.
        /// Selection on link, long string, double and blob properties is not supported.</remarks>
        IResourceList FindResourcesLive( string resType, string propName, object propValue );

        IResourceList FindResourcesLive<T>(string resType, PropId<T> propName, T propValue);

        /// <summary>
        /// Returns an optionally live list of resources for which the property with the specified ID 
        /// has the specified value.
        /// </summary>
        /// <param name="selectionType">Type of the selection (non-live, live or live snapshot).</param>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propId">The ID of the property for which the selection is done.</param>
        /// <param name="propValue">The value of the property.</param>
        /// <returns>The list of resources, or an empty resource list if no resources match the condition.</returns>
        /// <remarks>Selection on bool properties is only supported if <paramref name="propValue"/> is true.
        /// Selection on link, long string, double and blob properties is not supported.</remarks>
        IResourceList FindResources( SelectionType selectionType, string resType, int propId, object propValue );

        /// <summary>
        /// Returns an optionally live list of resources for which the property with the specified name
        /// has the specified value.
        /// </summary>
        /// <param name="selectionType">Type of the selection (non-live, live or live snapshot).</param>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propName">The name of the property for which the selection is done.</param>
        /// <param name="propValue">The value of the property.</param>
        /// <returns>The list of resources, or an empty resource list if no resources match the condition.</returns>
        /// <remarks>Selection on bool properties is only supported if <paramref name="propValue"/> is true.
        /// Selection on link, long string, double and blob properties is not supported.</remarks>
        IResourceList FindResources( SelectionType selectionType, string resType, string propName, object propValue );

        /// <summary>
        /// Returns the non-live list of resources for which the property with the specified ID 
        /// has a value in the specified range.
        /// </summary>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propId">The ID of the property for which the selection is done.</param>
        /// <param name="minValue">The minimum matching value of the property.</param>
        /// <param name="maxValue">The maximum matching value of the property.</param>
        /// <returns>The list of resources, or an empty resource list if no resources match the condition.</returns>
        /// <remarks>Range selection is only supported for int and date properties.</remarks>
        IResourceList FindResourcesInRange( string resType, int propId, object minValue, object maxValue );

        /// <summary>
        /// Returns the non-live list of resources for which the property with the specified name
        /// has a value in the specified range.
        /// </summary>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propName">The name of the property for which the selection is done.</param>
        /// <param name="minValue">The minimum matching value of the property.</param>
        /// <param name="maxValue">The maximum matching value of the property.</param>
        /// <returns>The list of resources, or an empty resource list if no resources match the condition.</returns>
        /// <remarks>Range selection is only supported for int and date properties.</remarks>
        IResourceList FindResourcesInRange( string resType, string propName, object minValue, object maxValue );
        
        /// <summary>
        /// Returns the live list of resources for which the property with the specified ID 
        /// has a value in the specified range.
        /// </summary>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propId">The ID of the property for which the selection is done.</param>
        /// <param name="minValue">The minimum matching value of the property.</param>
        /// <param name="maxValue">The maximum matching value of the property.</param>
        /// <returns>The list of resources, or an empty resource list if no resources match the condition.</returns>
        /// <remarks>Range selection is only supported for int and date properties.</remarks>
        IResourceList FindResourcesInRangeLive( string resType, int propId, object minValue, object maxValue );

        /// <summary>
        /// Returns the live list of resources for which the property with the specified name
        /// has a value in the specified range.
        /// </summary>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propName">The name of the property for which the selection is done.</param>
        /// <param name="minValue">The minimum matching value of the property.</param>
        /// <param name="maxValue">The maximum matching value of the property.</param>
        /// <returns>The list of resources, or an empty resource list if no resources match the condition.</returns>
        /// <remarks>Range selection is only supported for int and date properties.</remarks>
        IResourceList FindResourcesInRangeLive( string resType, string propName, object minValue, object maxValue );
        
        /// <summary>
        /// Returns an optionally live list of resources for which the property with the specified ID 
        /// has a value in the specified range.
        /// </summary>
        /// <param name="selectionType">Type of the selection (non-live, live or live snapshot).</param>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propId">The ID of the property for which the selection is done.</param>
        /// <param name="minValue">The minimum matching value of the property.</param>
        /// <param name="maxValue">The maximum matching value of the property.</param>
        /// <returns>The list of resources, or an empty resource list if no resources match the condition.</returns>
        /// <remarks>Range selection is only supported for int and date properties.</remarks>
        IResourceList FindResourcesInRange( SelectionType selectionType, string resType, int propId, object minValue, object maxValue );

        /// <summary>
        /// Returns an optionally live list of resources for which the property with the specified name
        /// has a value in the specified range.
        /// </summary>
        /// <param name="selectionType">Type of the selection (non-live, live or live snapshot).</param>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propName">The name of the property for which the selection is done.</param>
        /// <param name="minValue">The minimum matching value of the property.</param>
        /// <param name="maxValue">The maximum matching value of the property.</param>
        /// <returns>The list of resources, or an empty resource list if no resources match the condition.</returns>
        /// <remarks>Range selection is only supported for int and date properties.</remarks>
        IResourceList FindResourcesInRange( SelectionType selectionType, string resType, string propName, object minValue, object maxValue );

        /// <summary>
        /// Returns the non-live list of resources which have the property with the specified ID.
        /// </summary>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propId">The ID of the property to search for.</param>
        /// <returns>The list of resources which have the property.</returns>
        /// <remarks>This method works for any property type, except for long string properties.
        /// It also works for link properties.
        /// For directed links, a resource is included in the result only if there are links
        /// from that resource to other resources. (If there are only links to the resource from
        /// other resources, it is not included.)</remarks>
        IResourceList FindResourcesWithProp( string resType, int propId );

        /// <summary>
        /// Returns the non-live list of resources which have the property with the specified name.
        /// </summary>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propName">The name of the property to search for.</param>
        /// <returns>The list of resources which have the property.</returns>
        /// <remarks>This method works for any property type, except for long string properties.
        /// It also works for link properties.
        /// For directed links, a resource is included in the result only if there are links
        /// from that resource to other resources. (If there are only links to the resource from
        /// other resources, it is not included.)</remarks>
        IResourceList FindResourcesWithProp( string resType, string propName );

        IResourceList FindResourcesWithProp<T>(string resType, PropId<T> propId);
        
        /// <summary>
        /// Returns the live list of resources which have the property with the specified ID.
        /// </summary>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propId">The ID of the property to search for.</param>
        /// <returns>The list of resources which have the property.</returns>
        /// <remarks>This method works for any property type, except for long string properties.
        /// It also works for link properties.
        /// For directed links, a resource is included in the result only if there are links
        /// from that resource to other resources. (If there are only links to the resource from
        /// other resources, it is not included.)</remarks>
        IResourceList FindResourcesWithPropLive( string resType, int propId );

        /// <summary>
        /// Returns the live list of resources which have the property with the specified name.
        /// </summary>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propName">The name of the property to search for.</param>
        /// <returns>The list of resources which have the property.</returns>
        /// <remarks>This method works for any property type, except for long string properties.
        /// It also works for link properties.
        /// For directed links, a resource is included in the result only if there are links
        /// from that resource to other resources. (If there are only links to the resource from
        /// other resources, it is not included.)</remarks>
        IResourceList FindResourcesWithPropLive( string resType, string propName );

        IResourceList FindResourcesWithPropLive<T>(string resType, PropId<T> propId);
        
        /// <summary>
        /// Returns an optionally live list of resources which have the property with the specified ID.
        /// </summary>
        /// <param name="selectionType">Type of the selection (non-live, live or live snapshot).</param>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propId">The ID of the property to search for.</param>
        /// <returns>The list of resources which have the property.</returns>
        /// <remarks>This method works for any property type, except for long string properties.
        /// It also works for link properties.
        /// For directed links, a resource is included in the result only if there are links
        /// from that resource to other resources. (If there are only links to the resource from
        /// other resources, it is not included.)</remarks>
        IResourceList FindResourcesWithProp( SelectionType selectionType, string resType, int propId );

        /// <summary>
        /// Returns an optionally live list of resources which have the property with the specified name.
        /// </summary>
        /// <param name="selectionType">Type of the selection (non-live, live or live snapshot).</param>
        /// <param name="resType">The type of resources to return, or null if resources of 
        /// any type should be returned.</param>
        /// <param name="propName">The name of the property to search for.</param>
        /// <returns>The list of resources which have the property.</returns>
        /// <remarks>This method works for any property type, except for long string properties.
        /// It also works for link properties.
        /// For directed links, a resource is included in the result only if there are links
        /// from that resource to other resources. (If there are only links to the resource from
        /// other resources, it is not included.)</remarks>
        IResourceList FindResourcesWithProp( SelectionType selectionType, string resType, string propName );

        /// <summary>
        /// Returns a non-live list of all resources of the specified type.
        /// </summary>
        /// <param name="resType">The type of resources to return.</param>
        /// <returns>The list of all resources of that type.</returns>
        IResourceList GetAllResources( string resType );

        BusinessObjectList<T> GetAllResources<T>(ResourceTypeId<T> resType) where T : BusinessObject;

        /// <summary>
        /// Returns a live list of all resources of the specified type.
        /// </summary>
        /// <param name="resType">The type of resources to return.</param>
        /// <returns>The list of all resources of that type.</returns>
        IResourceList GetAllResourcesLive( string resType );

        /// <summary>
        /// Returns a non-live list of all resources of any of the specified types.
        /// </summary>
        /// <param name="resTypes">The types of resources to return.</param>
        /// <returns>The list of all resources of those types.</returns>
        /// <since>2.0</since>
        IResourceList GetAllResources( string[] resTypes );

        /// <summary>
        /// Returns a non-live list of all resources of any of the specified types.
        /// </summary>
        /// <param name="resTypes">The types of resources to return.</param>
        /// <returns>The list of all resources of those types.</returns>
        /// <since>2.0</since>
        IResourceList GetAllResourcesLive( string[] resTypes );

        /// <summary>
        /// Returns an empty resource list instance.
        /// </summary>
        IResourceList EmptyResourceList { get; }
        
        /// <summary>
        /// Returns an optionally live list containing the resources with the specified IDs.
        /// </summary>
        /// <param name="resourceIds">The resource IDs from which the list is constructed.</param>
        /// <param name="live">If <c>True</c>, a live list is returned.</param>
        /// <returns>The list of resources.</returns>
        /// <remarks>Use a live list if you need to get notified when a resource with any
        /// of the specified IDs is changed or deleted.</remarks>
        IResourceList ListFromIds( int[] resourceIds, bool live );
        
        /// <summary>
        /// Returns an optionally live list containing the resources with the specified IDs.
        /// </summary>
        /// <param name="resourceIds">The resource IDs from which the list is constructed.</param>
        /// <param name="live">If <c>True</c>, a live list is returned.</param>
        /// <returns>The list of resources.</returns>
        /// <remarks>Use a live list if you need to get notified when a resource with any
        /// of the specified IDs is changed or deleted.</remarks>
        IResourceList ListFromIds( ICollection resourceIds, bool live );

        /// <summary>
        /// Returns a unique resource for which the property with the specified ID has the
        /// specified value.
        /// </summary>
        /// <param name="resType">Type of the resource to return, or null if a resource of
        /// any type should be returned.</param>
        /// <param name="propId">The ID of the property to search for.</param>
        /// <param name="propValue">The value of the property.</param>
        /// <returns>The resource matching the condition, or null if no resources match
        /// the condition.</returns>
        /// <remarks><para>If multiple resources match the condition, an exception is thrown.</para>
        /// <para>In order to guarantee safe usage of the method, it is recommended to use
        /// <see cref="RegisterUniqueRestriction"/> to register a unique restriction for the
        /// resource and property types used in the call.</para></remarks>
        IResource FindUniqueResource( string resType, int propId, object propValue );

        /// <summary>
        /// Returns a unique resource for which the property with the specified name has the
        /// specified value.
        /// </summary>
        /// <param name="resType">Type of the resource to return, or null if a resource of
        /// any type should be returned.</param>
        /// <param name="propName">The name of the property to search for.</param>
        /// <param name="propValue">The value of the property.</param>
        /// <returns>The resource matching the condition, or null if no resources match
        /// the condition.</returns>
        /// <remarks><para>If multiple resources match the condition, an exception is thrown.</para>
        /// <para>In order to guarantee safe usage of the method, it is recommended to use
        /// <see cref="RegisterUniqueRestriction"/> to register a unique restriction for the
        /// resource and property types used in the call.</para></remarks>
        IResource FindUniqueResource( string resType, string propName, object propValue );
        
        /// <summary>
        /// Returns true if the current thread is allowed to perform resource store write
        /// operations.
        /// </summary>
        /// <returns>true if write operations are allowed (no write thread was assigned or
        /// the current thread is the write thread), false if the write operations must
        /// be marshalled to the write thread.</returns>
        /// <remarks>The <see cref="ResourceProxy"/> class can be used for easy marshalling
        /// of resource write operations to the resource thread.</remarks>
        bool IsOwnerThread();

        /// <summary>
        /// Registers a restriction on the links of the specified type for resources of the specified
        /// type.
        /// </summary>
        /// <param name="fromResourceType">Type of the resource for which the restriction is registered.</param>
        /// <param name="linkType">ID of the link property type for which the restriction is registered.</param>
        /// <param name="toResourceType">The resource type which must be on the other end of the link,
        /// or null if the link can point to a resource of any type.</param>
        /// <param name="minCount">The minimum count of links going from the resource, or 0 if there
        /// is no limit.</param>
        /// <param name="maxCount">The maximum count of links going from the resource, or 
        /// <c>Int32.MaxValue</c> if there is no limit.</param>
        void RegisterLinkRestriction( string fromResourceType, int linkType,
            string toResourceType, int minCount, int maxCount );

        void RegisterLinkRestriction(string fromResourceType, PropId<IResource> linkType,
            string toResourceType, int minCount, int maxCount);

        /// <summary>
        /// Returns the minimum link count restriction for the specified resource type and property type.
        /// </summary>
        /// <param name="fromResourceType">Type of the resource for which the restriction is queried.</param>
        /// <param name="linkType">ID of the link property type for which the restriction is queried.</param>
        /// <returns>The minimum count of links, or 0 if there is no limit.</returns>
        int GetMinLinkCountRestriction( string fromResourceType, int linkType );

        /// <summary>
        /// Returns the maximum link count restriction for the specified resource type and property type.
        /// </summary>
        /// <param name="fromResourceType">Type of the resource for which the restriction is queried.</param>
        /// <param name="linkType">ID of the link property type for which the restriction is queried.</param>
        /// <returns>The maximum count of links, or <c>Int32.MaxValue</c> if there is no limit.</returns>
        int GetMaxLinkCountRestriction( string fromResourceType, int linkType );
        
        /// <summary>
        /// Returns the target resource type restriction for links with specified property type going
        /// from resources of specified resource type.
        /// </summary>
        /// <param name="fromResourceType">Type of the resource for which the restriction is queried.</param>
        /// <param name="linkType">ID of the link property type for which the restriction is queried.</param>
        /// <returns>The type of the resource which must be on the other end of the link, or null if
        /// the link can point to a resource of any type.</returns>
        string GetLinkResourceTypeRestriction( string fromResourceType, int linkType );

        /// <summary>
        /// Registers a unique restriction for the specified resource type and property type.
        /// </summary>
        /// <param name="resourceType">The resource type for which the restriction is registered.</param>
        /// <param name="propId">ID of the property type for which the restriction is registered.</param>
        /// <remarks>A unique restriction specifies that the values of the specified property are unique
        /// among resources of the specified type (for example, ArticleId values are unique among news articles).
        /// A unique restriction ensures correct operation of the <see cref="FindUniqueResource"/> method.</remarks>
        void RegisterUniqueRestriction( string resourceType, int propId );

        /// <summary>
        /// Deletes a unique restriction for the specified resource type and property type.
        /// </summary>
        /// <param name="resourceType">The resource type for which the restriction is deleted.</param>
        /// <param name="propId">ID of the property type for which the restriction is deleted.</param>
        void DeleteUniqueRestriction( string resourceType, int propId );

        /// <summary>
        /// Registers a custom restriction for the specified resource type and property type.
        /// </summary>
        /// <param name="resourceType">The resource type for which the restriction is registered.</param>
        /// <param name="propId">ID of the property type for which the restriction is registered.</param>
        /// <param name="restriction">The restriction implementation.</param>
        /// <remarks><para>The restriction is checked when a new resource is created or when the specified
        /// property of theresource is changed.</para>
        /// <para>If the plugin which registered the restriction on a previous run of Omea is not loaded
        /// on the current run, all operations which would be checked by the restriction always fail.</para></remarks>
        void RegisterCustomRestriction( string resourceType, int propId, IResourceRestriction restriction );

        /// <summary>
        /// Deletes a custom restriction for the specified resource type and property type.
        /// </summary>
        /// <param name="resourceType">The resource type for which the restriction is deleted.</param>
        /// <param name="propId">ID of the property type for which the restriction is deleted.</param>
        void DeleteCustomRestriction( string resourceType, int propId );

        /// <summary>
        /// Registers a custom restriction which checks the possibility to delete resources of the specified
        /// type.
        /// </summary>
        /// <param name="resourceType">The resource type for which the restriction is registered.</param>
        /// <param name="restriction">The restriction implementation.</param>
        /// <remarks>Restrictions on resource delete may be required to enforce that deleting resources
        /// of the specified type is performed through the "semantic delete" API and does not bypass it
        /// by deleting resources directly from the resource store.</remarks>
        void RegisterRestrictionOnDelete( string resourceType, IResourceRestriction restriction );

        /// <summary>
        /// Deletes a custom restriction which checks the possibility to delete resources of the specified
        /// type.
        /// </summary>
        /// <param name="resourceType">The resource type for which the restriction is deleted.</param>
        void DeleteRestrictionOnDelete( string resourceType );

        /// <summary>
        /// Registers a custom display name provider.
        /// </summary>
        /// <since>2.0</since>
        void RegisterDisplayNameProvider( IDisplayNameProvider provider );
        
        /// <summary>
        /// Occurs after a single property or a batch of properties of any resource
        /// is changed.
        /// </summary>
        event ResourcePropEventHandler ResourceSaved;

        /// <summary>
        /// Obsolete, not recommended to be used by plugins. Please use <see cref="ResourceSaved"/> instead.
        /// </summary>
        event LinkEventHandler LinkAdded;

        /// <summary>
        /// Obsolete, not recommended to be used by plugins. Please use <see cref="ResourceSaved"/> instead.
        /// </summary>
        event LinkEventHandler LinkDeleted;
    }

    /// <summary>
    /// Defines the interface of a custom resource restriction.
    /// </summary>
    /// <since>2.0</since>
    public interface IResourceRestriction
    {
        /// <summary>
        /// Checks if the specified resource matches the restriction.
        /// </summary>
        /// <param name="res">The resource to check.</param>
        /// <remarks><para>The method should throw an exception (<see cref="ResourceRestrictionException"/>
        /// or an exception derived from that class) if the resource does not match the restriction.</para> 
        /// <para>Custom restrictions are checked on every resource change or <see cref="IResource.EndUpdate"/>, 
        /// so the code in this method must not perform any expensive checks.</para></remarks>
        void CheckResource( IResource res );
    }

    /// <summary>
    /// Defines the interface for a custom provider of resource display names.
    /// </summary>
    /// <remarks>The custom providers are registered through <see cref="IResourceStore.RegisterDisplayNameProvider"/>.
    /// Custom providers are invoked after an attempt to calculate the display name based on the standard
    /// display name template fails (returns an empty result).</remarks>
    /// <since>2.0</since>
    public interface IDisplayNameProvider
    {
        /// <summary>
        /// Returns the display name for the specified resource, or null if the provider cannot
        /// provide a display name for the resource.
        /// </summary>
        /// <param name="res">The resource for which the display name is requested.</param>
        /// <returns>The calculated display name or null.</returns>
        string GetDisplayName( IResource res );
    }

    /// <summary>
    /// Contains IDs for special resource properties which can be used when sorting lists.
    /// </summary>
    public class ResourceProps
    {
        /// <summary>
        /// The ID of a pseudo-property the value of which is equal to the type of the resource.
        /// </summary>
        public const int Type        = Int32.MaxValue-1;

        /// <summary>
        /// The ID of a pseudo-property the value of which is equal to the display name of the resource.
        /// </summary>
        public const int DisplayName = Int32.MaxValue-2;

        /// <summary>
        /// The ID of a pseudo-property the value of which is equal to the ID of the resource.
        /// </summary>
        public const int Id          = Int32.MaxValue-3;
    }

    /// <summary>
    /// Specifies the type of a link change in a <see cref="IPropertyChangeSet"/>.
    /// </summary>
    public enum LinkChangeType
    {
        /// <summary>
        /// The link to the specified resource was added.
        /// </summary>
        Add, 
        
        /// <summary>
        /// The link to the specified resource was deleted.
        /// </summary>
        Delete, 
        
        /// <summary>
        /// The link to the specified resource was neither added nor deleted.
        /// </summary>
        None
    };

    /// <summary>
    /// Describes a single change of a link in a <see cref="IPropertyChangeSet"/>.
    /// </summary>
    public struct LinkChange
    {
        private int _targetId;
        private LinkChangeType _changeType;

        public LinkChange( int targetId, LinkChangeType changeType )
        {
            _targetId = targetId;
            _changeType = changeType;
        }

        /// <summary>
        /// Gets the ID of the resource the link to which was added or deleted.
        /// </summary>
        public int TargetId
        {
            get { return _targetId; }
        }

        /// <summary>
        /// Gets the link change type (whether the link was added or deleted).
        /// </summary>
        public LinkChangeType ChangeType
        {
            get { return _changeType; }
        }
    }

    /// <summary>
    /// Describes a batch of changes to links and properties of a resource.
    /// </summary>
    public interface IPropertyChangeSet
    {
        /// <summary>
        /// Checks if the change set describes the creation of a new resource.
        /// </summary>
        bool IsNewResource         { get; }
        
        /// <summary>
        /// Checks if the changes described by the change set caused the display name
        /// of the resource to change.
        /// </summary>
        bool IsDisplayNameAffected { get; }

        /// <summary>
        /// Returns the list of properties affected by the change.
        /// </summary>
        /// <returns>An array of changed property IDs.</returns>
        int[] GetChangedProperties();

        /// <summary>
        /// Checks if the property with the specified ID was changed.
        /// </summary>
        /// <param name="propId">The ID of the property to check.</param>
        /// <returns>true if the property was changed, false otherwise.</returns>
        [Obsolete]
        bool IsPropertyChanged( int propId );

        bool IsPropertyChanged<T>(PropId<T> propId);
        
        /// <summary>
        /// Returns the value of the specified property before the change.
        /// </summary>
        /// <param name="propId">The ID of the property to check.</param>
        /// <returns>The value of the property before the change, or null if the
        /// property did not exist before the change.</returns>
        /// <remarks>The method also returns null if the property was not affected
        /// by the change. The return value of the method for link properties is not
        /// defined.</remarks>
        object GetOldValue( int propId );

        /// <summary>
        /// Checks if the link to the specified resource was created or deleted
        /// in the change set. 
        /// </summary>
        /// <param name="propId">The ID of the link property to check.</param>
        /// <param name="targetId">The ID of the resource the link to which
        /// is checked. </param>
        /// <returns>The type of the change.</returns>
        LinkChangeType GetLinkChange( int propId, int targetId );
        
        /// <summary>
        /// Returns all link changes for the specified link type.
        /// </summary>
        /// <param name="propId">The ID of the link property for which
        /// the changes are checked.</param>
        /// <returns>The array of link change structures, or an empty
        /// array if no links of the specified type were added or deleted
        /// in the change set.</returns>
        LinkChange[] GetLinkChanges( int propId );

        /// <summary>
        /// Returns a change set which contains all the changes from this
        /// change set and another change set.
        /// </summary>
        /// <param name="other">The change set to merge the current change set with.</param>
        /// <returns>The change set which is the result of the merge.</returns>
        IPropertyChangeSet Merge( IPropertyChangeSet other );
    }

    /// <summary>
    /// Provides data for events related to a single resource.
    /// </summary>
    public class ResourceEventArgs : EventArgs
    {
        private IResource _resource;
        public ResourceEventArgs( IResource resource )
        {
            _resource = resource;
        }
        public IResource Resource
        {
            get
            {
                return _resource;
            }
        }
    }

    /// <summary>
    /// Provides data for events related to a resource and the changes which occurred to it.
    /// </summary>
    public class ResourcePropEventArgs: EventArgs
    {
        private IResource          _resource;
        private IPropertyChangeSet _changeSet;

        [DebuggerStepThrough]
        public ResourcePropEventArgs( IResource resource, IPropertyChangeSet changeSet )
        {
            _resource = resource;
            _changeSet = changeSet;
        }

        /// <summary>
        /// Gets the resource to which the event is related.
        /// </summary>
        public IResource          Resource  { [DebuggerStepThrough] get { return _resource; } }

        /// <summary>
        /// Gets the change set describing the specific changes to the resource.
        /// </summary>
        public IPropertyChangeSet ChangeSet { [DebuggerStepThrough] get { return _changeSet;   } }
    }

    /// <summary>
    /// Provides data for events related to a resource and its position in a list.
    /// </summary>
    public class ResourceIndexEventArgs: EventArgs
    {
        private IResource _resource;
        private int _index;

        public ResourceIndexEventArgs( IResource resource, int index )
        {
            _resource = resource;
            _index = index;
        }

        /// <summary>
        /// Gets the resource to which the event is related.
        /// </summary>
        public IResource Resource  { get { return _resource; } }

        /// <summary>
        /// Gets the index of the resource in the list to which the event is related.
        /// </summary>
        public int Index           { get { return _index; } }
    }

    /// <summary>
    /// Provides data for events related to a resource, its position in the list
    /// and the changes which occurred to it.
    /// </summary>
    public class ResourcePropIndexEventArgs: EventArgs
    {
        private IResource _resource;
        private int _index;
        private IPropertyChangeSet _changeSet;

        public ResourcePropIndexEventArgs( IResource resource, int index, IPropertyChangeSet changeSet )
        {
            _resource  = resource;
            _index     = index;
            _changeSet = changeSet;
        }

        /// <summary>
        /// Gets the resource to which the event is related.
        /// </summary>
        public IResource          Resource  { get { return _resource; } }

        /// <summary>
        /// Gets the index of the resource in the list to which the event is related.
        /// </summary>
        public int                Index     { get { return _index; } }

        /// <summary>
        /// Gets the change set describing the specific changes to the resource.
        /// </summary>
        public IPropertyChangeSet ChangeSet { get { return _changeSet; } } 
    }

    /// <summary>
    /// Provides data for events related to a resource list.
    /// </summary>
    public class ResourceListEventArgs: System.EventArgs
    {
        private IResourceList _resList;

        public ResourceListEventArgs( IResourceList resList )
        {
            _resList = resList;
        }

        public IResourceList ResourceList { get { return _resList; } }
    }
	
    public class LinkEventArgs: EventArgs
    {                         
        private IResource _source;
        private IResource _target;
        private int       _propType;

        public LinkEventArgs( IResource source, IResource target, int propType )
        {
            _source = source;
            _target = target;
            _propType = propType;
        }

        public IResource Source
        {
            get { return _source; }
        }

        public IResource Target
        {
            get { return _target; }
        }

        public int PropType
        {
            get { return _propType; }
        }
    }

    /// <summary>
    /// Provides information about the change of a virtual property provided by
    /// a <see cref="IPropertyProvider"/>.
    /// </summary>
    public class PropertyProviderChangeEventArgs: EventArgs
    {
        private int _resourceID;
        private int _propID;
        private object _oldValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyProviderChangeEventArgs"/> class.
        /// </summary>
        /// <param name="resourceId">The ID of the resource for which the property value has changed.</param>
        /// <param name="propId">The ID of the changed property.</param>
        /// <param name="oldValue">The value of the property before the change.</param>
        public PropertyProviderChangeEventArgs( int resourceId, int propId, object oldValue )
        {
            _resourceID = resourceId;
            _propID     = propId;
            _oldValue   = oldValue;
        }
        
        /// <summary>
        /// Gets the ID of the resource for which the property value has changed.
        /// </summary>
        public int    ResourceId { get { return _resourceID; } }
        
        /// <summary>
        /// Gets the ID of the changed property.
        /// </summary>
        public int    PropId     { get { return _propID; } }
        
        /// <summary>
        /// Gets the value of the property before the change.
        /// </summary>
        public object OldValue   { get { return _oldValue; } }
    }
	
    public delegate void ResourceEventHandler( object sender, ResourceEventArgs e );
    public delegate void ResourcePropEventHandler( object sender, ResourcePropEventArgs e );
    public delegate void ResourceIndexEventHandler( object sender, ResourceIndexEventArgs e );
    public delegate void ResourcePropIndexEventHandler( object sender, ResourcePropIndexEventArgs e );
    public delegate void ResourceListEventHandler( object sender, ResourceListEventArgs e );
    public delegate void LinkEventHandler( object sender, LinkEventArgs e );
    public delegate void PropertyProviderChangeEventHandler( object sender, PropertyProviderChangeEventArgs e );

    /// <summary>
    /// Base class for all exceptions thrown by the resource store.
    /// </summary>
    [Serializable]
    public class StorageException: Exception
    {
        public StorageException() 
            : base() { }
        
        public StorageException( string message )
            : base( message ) { }

        public StorageException( string message, Exception innerException )
            : base( message, innerException ) { }

        protected StorageException( SerializationInfo info, StreamingContext context )
            : base( info, context ) { }
    }

    /// <summary>
    /// The exception is thrown when an attempt is made to load a deleted resource.
    /// </summary>
    [Serializable]
    public class ResourceDeletedException: StorageException
    {   
        private int _resourceId = -1;

        public ResourceDeletedException()
            : base( "Attempt to perform an operation on a deleted resource" ) { }

        public ResourceDeletedException( int resourceId, string resourceType )
            : base( "The resource with ID=" + resourceId + " of type " + resourceType + " has been deleted" ) 
        {
            _resourceId = resourceId;
        }

        public ResourceDeletedException( string message )
            : base( message ) { }    

        public ResourceDeletedException( string message, Exception innerException )
            : base( message, innerException ) { }

        protected ResourceDeletedException( SerializationInfo info, StreamingContext context )
            : base( info, context )
        {
            _resourceId = info.GetInt32( "_resourceId" );
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter=true)]
        public override void GetObjectData( SerializationInfo info, StreamingContext context )
        {
            base.GetObjectData( info, context );
            info.AddValue( "_resourceId", _resourceId );
        }

        public int ResourceId
        {
            get { return _resourceId; }
        }
    }

    /// <summary>
    /// The exception is thrown when an attempt is made to load a resource with a non-existing ID.
    /// </summary>
    [Serializable]
    public class InvalidResourceIdException: StorageException
    {
        private int _resourceId;

        public InvalidResourceIdException()
            : base( ) { }

        public InvalidResourceIdException( int resourceId )
            : base( "Invalid resource ID " + resourceId )
        {
            _resourceId = resourceId;
        }

        public InvalidResourceIdException( int resourceId, string message )
            : base( message )
        {
            _resourceId = resourceId;
        }

        public InvalidResourceIdException( string message )
            : base( message ) { }    

        public InvalidResourceIdException( string message, Exception innerException )
            : base( message, innerException ) { }

        protected InvalidResourceIdException( SerializationInfo info, StreamingContext context )
            : base( info, context )
        {
            _resourceId = info.GetInt32( "_resourceId" );
        }

        [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter=true)]
        public override void GetObjectData( SerializationInfo info, StreamingContext context )
        {
            base.GetObjectData( info, context );
            info.AddValue( "_resourceId", _resourceId );
        }

        public int ResourceId
        {
            get { return _resourceId; }
        }
    }

    /// <summary>
    /// The exception is thrown when a resource restriction is violated.
    /// </summary>
    [Serializable]
    public class ResourceRestrictionException: StorageException
    {
        public ResourceRestrictionException()
            : base( ) { }

        public ResourceRestrictionException( string message )
            : base( message ) { }

        public ResourceRestrictionException( string message, Exception innerException )
            : base( message, innerException ) { }

        protected ResourceRestrictionException( SerializationInfo info, StreamingContext context )
            : base( info, context ) { }
    }
}
