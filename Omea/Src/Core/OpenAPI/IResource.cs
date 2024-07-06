// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.IO;
using JetBrains.Annotations;

namespace JetBrains.Omea.OpenAPI
{
    public class PropId<T>
    {
        private readonly int _id;

        public PropId(int id)
        {
            _id = id;
        }

        public int Id
        {
            get { return _id; }
        }
    }

    /// <summary>
    /// Represents a single data object in the resource store. A resource has a type,
    /// a collection of typed properties and can have links to other resources.
    /// </summary>
    /// <remarks>
    /// <para>Only one <c>IResource</c> instance can exist at the same time for every given resource ID.</para>
    /// <para>For non-transient resources, all methods which modify the resource can only be called
    /// from the resource thread.</para>
    /// </remarks>
    public interface IResource
    {
        /// <summary>
        /// Identifier of a resource. A positive number for a valid resource, or -1 if the
        /// resource has been deleted.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// For deleted resources - the identifier of the resource before it was deleted. For
        /// existing resources - the same as <see cref="Id"/>.
        /// </summary>
        int OriginalId { get; }

        /// <summary>
        /// Name of the type of the resource.
        /// </summary>
        string Type { get; }

        /// <summary>
        /// Identifier of the type of the resource.
        /// </summary>
        int TypeId { get; }

        /// <summary>
        /// The default string representation of the resource in the user interface.
        /// </summary>
        /// <remarks>By default, the display name is automatically generated from the properties of the resource,
        /// based on the template which is specified when the resource type is registered.
        /// If a display name for a resource is assigned explicitly, it overrides the default generated
        /// display name, but an explicitly assigned display name is not updated automatically
        /// when the properties of a resource are changed.
        /// </remarks>
        string DisplayName { get; set; }

        /// <summary>
        /// Gets the collection which allows to enumerate the properties of a resource.
        /// </summary>
        /// <remarks><para>Enumerating the collection returns objects of type <see cref="IResourceProperty"/>.</para>
        /// <para>For getting or setting properties, Get*Prop() and SetProp() methods should be used.</para></remarks>
        IPropertyCollection Properties { get; }

        /// <summary>
        /// Clears all properties of a transient resource.
        /// </summary>
        /// <remarks>Has no effect for a persistent resource.</remarks>
        ///<since>840</since>
        void ClearProperties();

        /// <summary>
        /// Locks the resource in blocking mode.
        /// </summary>
        /// <since>862</since>
        /// <remarks>For IResource instance, never use the lock statement or Monitor.Enter().</remarks>
        void Lock();

        /// <summary>
        /// Tries to lock the resource in non-blocking mode.
        /// </summary>
        /// <returns>True is resource was locked.</returns>
        /// <since>862</since>
        bool TryLock();

        /// <summary>
        /// Unlocks the resource.
        /// </summary>
        /// <since>862</since>
        void UnLock();

        /// <summary>
        /// Returns true if the Delete() method was called for a resource.
        /// </summary>
        /// <remarks>
        /// The resource is actually deleted only after it has been removed from all live resource
        /// lists, so this method can return true even if the resource still exists (and is being
        /// removed from resource lists).
        /// </remarks>
        bool IsDeleting { get; }

        /// <summary>
        /// Returns true if the resource has been deleted from the resource store.
        /// </summary>
        /// <remarks>
        /// For deleted resources, calling all reader methods returns empty data, and calling
        /// all writer methods throws a <see cref="ResourceDeletedException"/>.
        /// </remarks>
        bool IsDeleted { get; }

        /// <summary>
        /// Returns true if the resource is transient and not yet saved to the resource store.
        /// </summary>
        /// <remarks>
        /// Transient resources are created with <see cref="IResourceStore.NewResourceTransient"/>
        /// and exist only in memory, until the <see cref="EndUpdate"/> method has been called on them.
        /// Transient resources can be created and modified in any thread, but the <see cref="EndUpdate"/>
        /// method must be called in the resource thread.
        /// </remarks>
        bool IsTransient { get; }

        /// <summary>
        /// Sets the property with the specified name to the specified value.
        /// </summary>
        /// <param name="propName">Name of the property to set.</param>
        /// <param name="propValue">The value to which the property is set.</param>
        /// <remarks>
        /// <para>The type of propValue must match the data type of the property: Int32
        /// for Int properties, String for String properties, DateTime for Date properties,
        /// Double for Double properties, Boolean for Bool properties, Stream for Blob properties,
        /// IResource for Link properties.
        /// </para>
        /// <para>String list properties cannot be set by SetProp. To set the value of a string
        /// list property, call <see cref="GetStringListProp(string)"/> and use
        /// <see cref="IStringList"/> methods to modify the value.</para>
        /// <para>Setting the value of a link property deletes all other links of the same type
        /// and in the same direction. If the link type is directed and there is a link of the same type
        /// from the link target to the resource on which the link is set, that link is deleted
        /// as well.</para>
        /// <para>Setting the property value to null is equivalent to <see cref="DeleteProp(string)"/>.</para>
        /// </remarks>
        void SetProp( string propName, object propValue );

        /// <summary>
        /// Sets the property with the specified ID to the specified value.
        /// </summary>
        /// <param name="propId">ID of the property to set.</param>
        /// <param name="propValue">The value to which the property is set.</param>
        /// <remarks>
        /// <para>The type of propValue must match the data type of the property: Int32
        /// for Int properties, String for String properties, DateTime for Date properties,
        /// Double for Double properties, Boolean for Bool properties, Stream for Blob properties,
        /// IResource for Link properties.
        /// </para>
        /// <para>String list properties cannot be set by SetProp. To set the value of a string
        /// list property, call <see cref="GetStringListProp(int)"/> and use
        /// <see cref="IStringList"> methods to modify the value.</see></para>
        /// <para>Setting the value of a link property deletes all other links of the same type
        /// and in the same direction. If the link type is directed and there is a link of the same type
        /// from the link target to the resource on which the link is set, that link is deleted
        /// as well.</para>
        /// <para>Setting the property value to null is equivalent to <see cref="DeleteProp(int)"/>.</para>
        /// <para>Since version 2.2 it is possible to set a string value to a blob property.</para>
        /// </remarks>
        void SetProp( int propId, object propValue );

        void SetProp<T>( PropId<T> propId, T value );

        void SetReverseLinkProp(PropId<IResource> propId, IResource propValue);

        /// <summary>
        /// Deletes the property with the specified name.
        /// </summary>
        /// <param name="propName">Name of the property to delete.</param>
        /// <remarks>For link properties, this works as <see cref="DeleteLinks(string)"/>.</remarks>
        void DeleteProp( string propName );

        /// <summary>
        /// Deletes the property with the specified ID.
        /// </summary>
        /// <param name="propId">ID of the property to delete.</param>
        /// <remarks>For link properties, this works as <see cref="DeleteLinks(int)"/>.</remarks>
        void DeleteProp( int propId );

        /// <summary>
        /// Adds a link with the specified property name to the specified target resource.
        /// </summary>
        /// <param name="propName">Name of the link property.</param>
        /// <param name="target">Resource to which the link is added.</param>
        /// <remarks>If the target resource is transient, the link will not be visible
        /// in the list of the links of the current resource until <see cref="EndUpdate"/>
        /// is called on the target.</remarks>
        void AddLink( string propName, IResource target );

        /// <summary>
        /// Adds a link with the specified property ID to the specified target resource.
        /// </summary>
        /// <param name="propId">ID of the link property.</param>
        /// <param name="target">Resource to which the link is added.</param>
        /// <remarks>If the target resource is transient, the link will not be visible
        /// in the list of the links of the current resource until <see cref="EndUpdate"/>
        /// is called on the target.</remarks>
        void AddLink( int propId, IResource target );

        void AddLink(PropId<IResource> propId, IResource target);

        /// <summary>
        /// Deletes a link with the specified property name to the specified resource.
        /// </summary>
        /// <param name="propName">Name of the link property.</param>
        /// <param name="target">Resource to which the link is deleted.</param>
        /// <remarks>Deleting a link which does not exist is not an error and has no effect.</remarks>
        void DeleteLink( string propName, IResource target );

        /// <summary>
        /// Deletes a link with the specified property ID to the specified resource.
        /// </summary>
        /// <param name="propId">ID of the link property.</param>
        /// <param name="target">Resource to which the link is deleted.</param>
        /// <remarks>Deleting a link which does not exist is not an error and has no effect.</remarks>
        void DeleteLink( int propId, IResource target );

        /// <summary>
        /// Deletes all links with the specified property name.
        /// </summary>
        /// <param name="propName">Name of the property for which the links are deleted.</param>
        /// <remarks>If <paramref name="propName"/> is a directed link, only the links from the
        /// resource are deleted.</remarks>
        void DeleteLinks( string propName );

        /// <summary>
        /// Deletes all links with the specified property ID.
        /// </summary>
        /// <param name="propId">ID of the property for which the links are deleted.</param>
        /// <remarks>If <paramref name="propId"/> is a directed link, only the links from the
        /// resource are deleted. To delete links to the resource, specify a negative property
        /// ID (for example, -5 instead of 5).</remarks>
        void DeleteLinks( int propId );

        /// <summary>
        /// Returns the value of the property with the specified ID.
        /// </summary>
        /// <param name="propId">ID of the property.</param>
        /// <returns>Value of the property, or null if no value was assigned for the current
        /// resource.</returns>
        /// <remarks>For bool properties, a non-null value (<c>true</c> or <c>false</c>) is always returned.
        /// <para>For link properties, the return value is the same as for <see cref="GetLinkProp(int)"/></para>.
        /// </remarks>
        /// <seealso cref="HasProp(int)"/>
        object GetProp( int propId );

        /// <summary>
        /// Returns the value of the property with the specified name.
        /// </summary>
        /// <param name="propName">Name of the property.</param>
        /// <returns>Value of the property, or null if no value was assigned for the current
        /// resource.</returns>
        /// <remarks>For bool properties, a non-null value (true or false) is always returned.
        /// <para>For link properties, the return value is the same as for <see cref="GetLinkProp(string)"/></para>.
        /// </remarks>
        /// <seealso cref="HasProp(string)"/>
        object GetProp( string propName );

        T GetProp<T>( PropId<T> propId );

        /// <summary>
        /// Returns the value of the string property with the specified ID.
        /// </summary>
        /// <param name="propId">ID of the property.</param>
        /// <returns>Value of the property, or null if no value was assigned for the current
        /// resource.</returns>
        string GetStringProp( int propId );

        /// <summary>
        /// Returns the value of the string property with the specified name.
        /// </summary>
        /// <param name="propName">Name of the property.</param>
        /// <returns>Value of the property, or null if no value was assigned for the current
        /// resource.</returns>
        string GetStringProp( string propName );

        /// <summary>
        /// Returns the value of the int property with the specified ID.
        /// </summary>
        /// <param name="propId">ID of the property.</param>
        /// <returns>Value of the property, or 0 if no value was assigned for the current
        /// resource.</returns>
        int GetIntProp( int propId );

        /// <summary>
        /// Returns the value of the int property with the specified name.
        /// </summary>
        /// <param name="propName">Name of the property.</param>
        /// <returns>Value of the property, or 0 if no value was assigned for the current
        /// resource.</returns>
        int GetIntProp( string propName );

        /// <summary>
        /// Returns the value of the date/time property with the specified ID.
        /// </summary>
        /// <param name="propId">ID of the property.</param>
        /// <returns>Value of the property, or <see cref="DateTime.MinValue"/> if no value
        /// was assigned for the current resource.</returns>
        DateTime GetDateProp( int propId );

        /// <summary>
        /// Returns the value of the date/time property with the specified name.
        /// </summary>
        /// <param name="propName">Name of the property.</param>
        /// <returns>Value of the property, or <see cref="DateTime.MinValue"/> if no value
        /// was assigned for the current resource.</returns>
        DateTime GetDateProp( string propName );

        /// <summary>
        /// Returns the value of the double property with the specified ID.
        /// </summary>
        /// <param name="propId">ID of the property.</param>
        /// <returns>Value of the property, or 0.0 if no value
        /// was assigned for the current resource.</returns>
        double GetDoubleProp( int propId );

        /// <summary>
        /// Returns the value of the double property with the specified name.
        /// </summary>
        /// <param name="propName">Name of the property.</param>
        /// <returns>Value of the property, or 0.0 if no value
        /// was assigned for the current resource.</returns>
        double GetDoubleProp( string propName );

        /// <summary>
        /// Returns the value of the blob property with the specified ID.
        /// </summary>
        /// <param name="propId">ID of the property.</param>
        /// <returns>Value of the property, or null if no value
        /// was assigned for the current resource.</returns>
        /// <remarks>A new stream is created every time the property is accessed. All
        /// streams are opened over the same backing file for reading and writing, with
        /// full sharing allowed, so there is a possibility that the stream is modified
        /// concurrently. It is strongly recommended to close the stream after you've finished
        /// working with it.</remarks>
        Stream GetBlobProp( int propId );

        /// <summary>
        /// Returns the value of the blob property with the specified name.
        /// </summary>
        /// <param name="propName">Name of the property.</param>
        /// <returns>Value of the property, or null if no value
        /// was assigned for the current resource.</returns>
        /// <remarks>A new stream is created every time the property is accessed. All
        /// streams are opened over the same backing file for reading and writing, with
        /// full sharing allowed, so there is a possibility that the stream is modified
        /// concurrently. It is strongly recommended to close the stream after you've finished
        /// working with it.</remarks>
        Stream GetBlobProp( string propName );

        /// <summary>
        /// Returns the value of the string list property with the specified ID.
        /// </summary>
        /// <param name="propId">ID of the property.</param>
        /// <returns>Value of the property.</returns>
        /// <remarks>A non-null value is always returned. If no value was assigned to
        /// the property, an empty list is returned.</remarks>
        IStringList GetStringListProp( int propId );

        /// <summary>
        /// Returns the value of the string list property with the specified name.
        /// </summary>
        /// <param name="propName">Name of the property.</param>
        /// <returns>Value of the property.</returns>
        /// <remarks>A non-null value is always returned. If no value was assigned to
        /// the property, an empty list is returned.</remarks>
        IStringList GetStringListProp( string propName );

        /// <summary>
        /// Returns a resource to which the current resource is linked with the link of specified
        /// name.
        /// </summary>
        /// <param name="propName">Name of the link property.</param>
        /// <returns>A resource linked to the current resource, or null if there are no
        /// such resources.</returns>
        /// <remarks>
        /// <para>If there are several resources linked to the current one with the specified
        /// link type, an arbitrary one is returned.</para>
        /// <para>For directed links, GetLinkProp() returns only links from the resource on which
        /// the method was called, not links to it.</para>
        /// </remarks>
        IResource GetLinkProp( string propName );

        /// <summary>
        /// Returns a resource to which the current resource is linked with the link of specified
        /// property ID.
        /// </summary>
        /// <param name="propId">ID of the link property.</param>
        /// <returns>A resource linked to the current resource, or null if there are no
        /// such resources.</returns>
        /// <remarks>
        /// <para>If there are several resources linked to the current one with the specified
        /// link type, an arbitrary one is returned.</para>
        /// <para>For directed links, GetLinkProp() returns only links from the resource on which
        /// the method was called, not links to it.</para>
        /// </remarks>
        IResource GetLinkProp( int propId );

        /// <summary>
        /// Returns a resource which is linked to this resource with a directed link property of the
        /// specified type.
        /// </summary>
        /// <param name="propId">ID of the directed link property type.</param>
        /// <returns>A resource linked to this resource or null.</returns>
        [CanBeNull]
        IResource GetReverseLinkProp([NotNull] PropId<IResource> propId);

        /// <summary>
        /// Returns the textual representation of the value of the property with the specified name.
        /// </summary>
        /// <param name="propName">Name of the property.</param>
        /// <returns>The textual (user-readable) representation of the value, or an empty string
        /// if no value was assigned for the current resource.</returns>
        /// <remarks><para>For link properies, the representation consists of display names of all resources
        /// linked to the current resource, separated with commas. For directed links, only links from
        /// the current resource are included.</para>
        /// <para>For string list properties, the representation consists of all strings in the list,
        /// separated with commas.</para>
        /// </remarks>
        string GetPropText( string propName );

        /// <summary>
        /// Returns the textual representation of the value of the property with the specified ID.
        /// </summary>
        /// <param name="propId">ID of the property.</param>
        /// <returns>The textual (user-readable) representation of the value, or an empty string
        /// if no value was assigned for the current resource.</returns>
        /// <remarks><para>For link properies, the representation consists of display names of all resources
        /// linked to the current resource, separated with commas. For directed links, only links from
        /// the current resource are included.</para>
        /// <para>For string list properties, the representation consists of all strings in the list,
        /// separated with commas.</para>
        /// </remarks>
        string GetPropText( int propId );

        /// <summary>
        /// Returns the count of links with the specified property name from the current resource.
        /// </summary>
        /// <param name="propName">Name of the link property.</param>
        /// <returns>Count of links.</returns>
        /// <remarks>If <paramref name="propName"/> is a directed link, only the links from the
        /// resource are counted.</remarks>
        int GetLinkCount( string propName );

        /// <summary>
        /// Returns the count of links with the specified property ID from the current resource.
        /// </summary>
        /// <param name="propId">ID of the link property.</param>
        /// <returns>Count of links.</returns>
        /// <remarks>If <paramref name="propId"/> is a directed link, only the links from the
        /// resource are counted. To count links to the resource, specify a negative property
        /// ID (for example, -5 instead of 5).</remarks>
        int GetLinkCount( int propId );

        /// <summary>
        /// Returns the non-live list of the resources of the specified type linked to the current
        /// resource with the link with the specified property name.
        /// </summary>
        /// <param name="resType">Type of the resources which are returned, or null if resources
        /// of all types should be returned.</param>
        /// <param name="propName">Name of the link property.</param>
        /// <returns>The list of linked resources, or an empty list if there are no linked resources.</returns>
        /// <remarks>For directed links, both links from and to the resource are returned.</remarks>
        IResourceList GetLinksOfType( string resType, string propName );

        /// <summary>
        /// Returns the non-live list of the resources of the specified type linked to the current
        /// resource with the link with the specified property ID.
        /// </summary>
        /// <param name="resType">Type of the resources which are returned, or null if resources
        /// of all types should be returned.</param>
        /// <param name="propId">ID of the link property.</param>
        /// <returns>The list of linked resources, or an empty list if there are no linked resources.</returns>
        /// <remarks>For directed links, both links from and to the resource are returned.</remarks>
        IResourceList GetLinksOfType( string resType, int propId );

        IResourceList GetLinksOfType(string resType, PropId<IResource> propId);

        BusinessObjectList<T> GetLinksOfType<T>(ResourceTypeId<T> resType, PropId<IResource> propId)
            where T : BusinessObject;

        /// <summary>
        /// Returns the live list of the resources of the specified type linked to the current
        /// resource with the link with the specified property name.
        /// </summary>
        /// <param name="resType">Type of the resources which are returned, or null if resources
        /// of all types should be returned.</param>
        /// <param name="propName">Name of the link property.</param>
        /// <returns>The list of linked resources, or an empty list if there are no linked resources.</returns>
        /// <remarks>For directed links, both links from and to the resource are returned.</remarks>
        IResourceList GetLinksOfTypeLive( string resType, string propName );

        /// <summary>
        /// Returns the live list of the resources of the specified type linked to the current
        /// resource with the link with the specified property ID.
        /// </summary>
        /// <param name="resType">Type of the resources which are returned, or null if resources
        /// of all types should be returned.</param>
        /// <param name="propId">ID of the link property.</param>
        /// <returns>The list of linked resources, or an empty list if there are no linked resources.</returns>
        /// <remarks>For directed links, both links from and to the resource are returned.</remarks>
        IResourceList GetLinksOfTypeLive( string resType, int propId );

        IResourceList GetLinksOfTypeLive(string resType, PropId<IResource> propId);

        /// <summary>
        /// Returns the non-live list of the resources of the specified type to which there are directed links
        /// from the current resource with the link with the specified property name.
        /// </summary>
        /// <param name="resType">Type of the resources which are returned, or null if resources
        /// of all types should be returned.</param>
        /// <param name="propName">Name of the directed link property.</param>
        /// <returns>The list of linked resources, or an empty list if there are no linked resources.</returns>
        /// <remarks>This method can only be called for directed link properties.</remarks>
        IResourceList GetLinksFrom( string resType, string propName );

        /// <summary>
        /// Returns the non-live list of the resources of the specified type to which there are directed links
        /// from the current resource with the link with the specified property ID.
        /// </summary>
        /// <param name="resType">Type of the resources which are returned, or null if resources
        /// of all types should be returned.</param>
        /// <param name="propId">ID of the directed link property.</param>
        /// <returns>The list of linked resources, or an empty list if there are no linked resources.</returns>
        /// <remarks>This method can only be called for directed link properties.</remarks>
        IResourceList GetLinksFrom( string resType, int propId );

        IResourceList GetLinksFrom(string resType, PropId<IResource> propId);

        BusinessObjectList<T> GetLinksFrom<T>(ResourceTypeId<T> resType, PropId<IResource> propId)
            where T : BusinessObject;

        /// <summary>
        /// Returns the live list of the resources of the specified type to which there are directed links
        /// from the current resource with the link with the specified property name.
        /// </summary>
        /// <param name="resType">Type of the resources which are returned, or null if resources
        /// of all types should be returned.</param>
        /// <param name="propName">Name of the directed link property.</param>
        /// <returns>The list of linked resources, or an empty list if there are no linked resources.</returns>
        /// <remarks>This method can only be called for directed link properties.</remarks>
        IResourceList GetLinksFromLive( string resType, string propName );

        /// <summary>
        /// Returns the live list of the resources of the specified type to which there are directed links
        /// from the current resource with the link with the specified property ID.
        /// </summary>
        /// <param name="resType">Type of the resources which are returned, or null if resources
        /// of all types should be returned.</param>
        /// <param name="propId">ID of the directed link property.</param>
        /// <returns>The list of linked resources, or an empty list if there are no linked resources.</returns>
        /// <remarks>This method can only be called for directed link properties.</remarks>
        IResourceList GetLinksFromLive( string resType, int propId );

        /// <summary>
        /// Returns the non-live list of the resources of the specified type from which there are directed links
        /// to the current resource with the link with the specified property name.
        /// </summary>
        /// <param name="resType">Type of the resources which are returned, or null if resources
        /// of all types should be returned.</param>
        /// <param name="propName">Name of the directed link property.</param>
        /// <returns>The list of linked resources, or an empty list if there are no linked resources.</returns>
        /// <remarks>This method can only be called for directed link properties.</remarks>
        IResourceList GetLinksTo( string resType, string propName );

        /// <summary>
        /// Returns the non-live list of the resources of the specified type from which there are directed links
        /// to the current resource with the link with the specified property ID.
        /// </summary>
        /// <param name="resType">Type of the resources which are returned, or null if resources
        /// of all types should be returned.</param>
        /// <param name="propId">Name of the directed link property.</param>
        /// <returns>The list of linked resources, or an empty list if there are no linked resources.</returns>
        /// <remarks>This method can only be called for directed link properties.</remarks>
        IResourceList GetLinksTo( string resType, int propId );

        IResourceList GetLinksTo(string resType, PropId<IResource> propId);

        BusinessObjectList<T> GetLinksTo<T>(ResourceTypeId<T> resType, PropId<IResource> propId)
            where T : BusinessObject;

        /// <summary>
        /// Returns the live list of the resources of the specified type from which there are directed links
        /// to the current resource with the link with the specified property name.
        /// </summary>
        /// <param name="resType">Type of the resources which are returned, or null if resources
        /// of all types should be returned.</param>
        /// <param name="propName">Name of the directed link property.</param>
        /// <returns>The list of linked resources, or an empty list if there are no linked resources.</returns>
        /// <remarks>This method can only be called for directed link properties.</remarks>
        IResourceList GetLinksToLive( string resType, string propName );

        /// <summary>
        /// Returns the live list of the resources of the specified type from which there are directed links
        /// to the current resource with the link with the specified property ID.
        /// </summary>
        /// <param name="resType">Type of the resources which are returned, or null if resources
        /// of all types should be returned.</param>
        /// <param name="propId">Name of the directed link property.</param>
        /// <returns>The list of linked resources, or an empty list if there are no linked resources.</returns>
        /// <remarks>This method can only be called for directed link properties.</remarks>
        IResourceList GetLinksToLive( string resType, int propId );

        /// <summary>
        /// Returns the array of distinct types of links from or to the resources.
        /// </summary>
        /// <returns>An array of link property IDs, or an empty array if there are no links.</returns>
        /// <remarks>For directed links, both links from and to the resource are returned, and
        /// all returned link type IDs are positive.</remarks>
        int[] GetLinkTypeIds();

        /// <summary>
        /// Returns true if the resource has the property with the specified name.
        /// </summary>
        /// <param name="propName">Name of the property.</param>
        /// <returns>true if the resource has the property.</returns>
        /// <remarks>
        /// <para>For directed link properties, <c>HasProp()</c> returns <c>true</c> only if there
        /// are links from the resource. </para>
        /// <para>For boolean properties, "the resource has the property" and "the value of the
        /// property is true" are the same thing. Thus, you can use <c>HasProp()</c> to get the
        /// value of boolean properties.</para>
        /// </remarks>
        bool HasProp( string propName );

        /// <summary>
        /// Returns true if the resource has the property with the specified ID.
        /// </summary>
        /// <param name="propId">ID of the property.</param>
        /// <returns>true if the resource has the property.</returns>
        /// <remarks>
        /// <para>For directed link properties, <c>HasProp()</c> returns <c>true</c> only if there
        /// are links from the resource. </para>
        /// <para>For boolean properties, "the resource has the property" and "the value of the
        /// property is true" are the same thing. Thus, you can use <c>HasProp()</c> to get the
        /// value of boolean properties.</para>
        /// </remarks>
        bool HasProp( int propId );

        bool HasProp<T>( PropId<T> propId );

        /// <summary>
        /// Returns true if the current resource is linked to the specified resource with a link
        /// with the specified property name.
        /// </summary>
        /// <param name="propName">Name of the link property.</param>
        /// <param name="target">Target resource. May not be null.</param>
        /// <returns>true if the link to <paramref name="target"/> exists.</returns>
        /// <remarks>For directed links, only the links from the current resource are checked.</remarks>
        bool HasLink( string propName, IResource target );

        /// <summary>
        /// Returns true if the current resource is linked to the specified resource with a link
        /// with the specified property ID.
        /// </summary>
        /// <param name="propId">ID of the link property.</param>
        /// <param name="target">Target resource. May not be null.</param>
        /// <returns>true if the link to <paramref name="target"/> exists.</returns>
        /// <remarks>For directed links, only the links from the current resource are checked.</remarks>
        bool HasLink( int propId, IResource target );

        /// <summary>
        /// Changes the type of the resource to the specified type.
        /// </summary>
        /// <param name="newType">New type of the resource.</param>
        void ChangeType( string newType );

        /// <summary>
        /// Deletes the resource.
        /// </summary>
        void Delete();

        /// <summary>
        /// Begins a batch update of the resource properties.
        /// </summary>
        /// <remarks>If several properties of a resource are updated in a single operation, it is
        /// recommended to surround them with <c>BeginUpdate()</c> and <see cref="EndUpdate()"/>
        /// to reduce the number of resource change notifications which are sent after the properties
        /// are changed.</remarks>
        /// <seealso cref="IResourceStore.BeginNewResource"/>
        /// /// <seealso cref="IResourceStore.NewResourceTransient"/>
        void BeginUpdate();

        /// <summary>
        /// Ends a batch update of the resource properties, and saves transient resources.
        /// </summary>
        /// <remarks>For transient resources, this method saves the properties and links of the
        /// resource to disk, and must be called from the resource thread.</remarks>
        /// <seealso cref="IResourceStore.NewResourceTransient"/>
        void EndUpdate();

        /// <summary>
        /// Checks if any properties of the resource were changed after a call to <see cref="BeginUpdate()"/>.
        /// </summary>
        /// <returns>true if any properties were changed, false otherwise.</returns>
        /// <remarks>This method can only be called between <see cref="BeginUpdate()"/> and
        /// <see cref="EndUpdate()"/>. Setting a property to the same value as the current value
        /// of the property does not cause the resource to be changed.</remarks>
        bool IsChanged();

        /// <summary>
        /// Returns a non-live resource list containing only the current resource.
        /// </summary>
        /// <returns>The resource list instance.</returns>
        IResourceList ToResourceList();

        /// <summary>
        /// Returns a live resource list containing only the current resource.
        /// </summary>
        /// <returns>The resource list instance.</returns>
        /// <remarks>The liveness of the resource list allows to receive notifications
        /// when the resource is changed or deleted.</remarks>
        IResourceList ToResourceListLive();
    }

    /// <summary>
    /// Represents the value of a string list property of a resource.
    /// </summary>
    /// <remarks><para>The order of the strings in the list is persistent - the strings
    /// are enumerated in the list in the same order as they were added.</para>
    /// <para>The list may contain duplicate strings.</para></remarks>
    public interface IStringList: IDisposable, IEnumerable
    {
        /// <summary>
        /// Returns the number of strings in the list.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Returns the string at the specified (0-based) index.
        /// </summary>
        string this[ int index ] { get; }

        /// <summary>
        /// Adds a string to the list.
        /// </summary>
        /// <param name="value">The string to add.</param>
        void Add( string value );

        /// <summary>
        /// Removes the string at the specified index of the list.
        /// </summary>
        /// <param name="index">The zero-based index of the string to remove.</param>
        void RemoveAt( int index );

        /// <summary>
        /// Removes the specified string from the list.
        /// </summary>
        /// <param name="value">The string to remove.</param>
        /// <remarks>If the string is encountered multiple times in the list, only
        /// the first instance is removed.</remarks>
        void Remove( string value );

        /// <summary>
        /// Removes all strings from the list.
        /// </summary>
        void Clear();

        /// <summary>
        /// Returns the zero-based index of the first occurrence of a string in the list.
        /// </summary>
        /// <param name="value">The string to locate.</param>
        /// <returns>The zero-based index of the string, or -1 if the string is not
        /// present in the list.</returns>
        int IndexOf( string value );
    }

    /// <summary>
    /// A collection of properties of the resource.
    /// </summary>
    /// <remarks>Enumerating the collection returns objects of type <see cref="IResourceProperty"/>.</remarks>
    public interface IPropertyCollection: IEnumerable
    {
        /// <summary>
        /// Gets the count of properties of the resource.
        /// </summary>
        int Count { get; }
    }

    /// <summary>
    /// Represents a single property of a resource.
    /// </summary>
    /// <remarks>Resource properties can be accessed by enumerating the
    /// <see cref="IResource.Properties"/> collection. The <code>IResourceProperty</code>
    /// interface allows only read-only access to properties; to change property
    /// values, use methods on <see cref="IResource"/> like <see cref="IResource.SetProp(int, object)"/>,
    /// <see cref="IResource.DeleteProp(int)"/> and so on.</remarks>
    public interface IResourceProperty
    {
        /// <summary>
        /// The name of the property type.
        /// </summary>
        string Name   { get; }

        /// <summary>
        /// The numeric ID of the property type.
        /// </summary>
        int PropId    { get; }

        /// <summary>
        /// The data type of the property type.
        /// </summary>
        PropDataType DataType { get; }

        /// <summary>
        /// The value of the property for the resource.
        /// </summary>
        /// <remarks>For link properties, the return value is the same as that of
        /// <see cref="IResource.GetLinkProp(int)"/> - that is, one randomly selected
        /// resource linked to the current one.</remarks>
        object Value  { get; }
    }
}
