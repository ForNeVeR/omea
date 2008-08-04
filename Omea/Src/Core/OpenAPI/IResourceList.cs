/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using JetBrains.Annotations;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// An ordered collection of resources, which can optionally
    /// represent the result of a live query.
    /// </summary>
    /// <remarks>
    /// <para>The liveness of a resource list depends on the method which was used
    /// to obtain it. The result of a list combination (<see cref="Union(IResourceList)"/>,
    /// <see cref="Intersect(IResourceList)"/> or <see cref="Minus"/>) is live if
    /// at least one of the source lists is live.</para>
    /// <para>When a resource list is created, in most cases it contains only 
    /// the query condition and not the actual list of resource IDs. Such lists
    /// can be created and combined (intersected, unioned and so on) with other
    /// resource lists very cheapy. The resource list is instantiated (the actual
    /// query is performed) only when the resources themselves (or the count of resources)
    /// are accessed.</para>
    /// <para>The resource list stores only resource IDs, not resources themselves.
    /// Resources are loaded when they are actually accessed.</para>
    /// <para>Resources in the resource list are unique - that is, the resource list
    /// can contain each resource only once.</para>
    /// </remarks>
    public interface IResourceList: IDisposable, IEnumerable
    {
        /// <summary>
        /// The total number of resources in the list.
        /// </summary>
        /// <example>
        /// <para>Checks if a list is empty.</para>
        /// <code>
		/// IResourceList	list = …
		/// 
		/// if( list.Count == 0 )
		///     Trace.WriteLine( "The list is empty." );
		/// else
		///     Trace.WriteLine( "There are resources in the list." );
		/// </code>
		/// <para>Enumerates resources in the list using a for loop.</para>
        /// <code>
		/// IResourceList	list = …
		/// 
		/// for( int a = 0; a &lt; list.Count; a++ )
		///     Trace.WriteLine( list[a].DisplayName );
		/// </code>
		/// </example>
        int Count { get; }
        
        /// <summary>
        /// Returns the resource at the specified position in the list.
        /// </summary>
        /// <example>
		/// <para>Enumerates resources in the list using a for loop.</para>
        /// <code>
		/// IResourceList	list = …
		/// 
		/// for( int a = 0; a &lt; list.Count; a++ )
		///     Trace.WriteLine( list[a].DisplayName );
		/// </code>
		/// </example>
        IResource this[ int index ] { get; }
        
        /// <summary>
        /// Provides direct access to the IDs of resources stored in the list.
        /// </summary>
        /// <example>
        /// Prints out IDs of resources in the list.
        /// <code>
		/// IResourceList	list = …
		/// 
		/// foreach( int id in list.ResourceIds )
		///     Trace.WriteLine( id );				
		/// </code>
        /// </example>
        IResourceIdCollection ResourceIds { get; }

        /// <summary>
        /// Returns the index of the specified resource in the list.
        /// </summary>
        /// <param name="res">The resource to locate.</param>
        /// <returns>The index (0-based) of the resource in the list, or -1 if the resource
        /// is not found.</returns>
		/// <example>
		/// <code>
		/// IResourceList list = …
		/// 
		/// Debug.Assert( list.IndexOf( list[0] ) == 0 );	// The first element has the 0 index
		/// </code>
		/// </example>
		int IndexOf( IResource res );

        /// <summary>
        /// Returns the index of the resource with the specified ID in the list.
        /// </summary>
        /// <param name="resId">The resource ID to locate.</param>
        /// <returns>The index (0-based) of the resource in the list, or -1 if the resource
        /// is not found.</returns>
        /// <example>
        /// <code>
        /// IResourceList list = …
        /// 
        /// int id = list[0].Id;	// ID of the first element
        /// Debug.Assert( list.IndexOf(id) == 0 );	// The first element is returned from this ID
        /// </code>
        /// </example>
        int IndexOf( int resId );

        /// <summary>
        /// Checks if the specified resource is present in the list.
        /// </summary>
        /// <param name="res">The resource to locate.</param>
        /// <returns><c>true</c> if the resource is present in the list, <c>false</c> otherwise.</returns>
        /// <remarks>This method matches the resource against the predicate (query condition)
        /// of the list; thus, its execution time does not depend on the size of the list but
        /// does depend on the query complexity.</remarks>
        /// <example>
        /// <para>Checks whether a resource is contained in the resource list.</para>
        /// <code>
		/// IResourceList list = …
		/// IResource res = …
		/// 
        /// if( list.Contains( res ) )
		///     Trace.WriteLine( "Present." );
		/// else
		///     Trace.WriteLine( "Absent." );
		/// </code>
        /// </example>
        bool Contains( IResource res );

		/// <summary><seealso cref="SortPropIDs"/><seealso cref="SortDirections"/><seealso cref="SortAscending"/>
		/// A space-separated list of properties that serve as sorting keys for this resource list, or an empty string if the list is not sorted.
		/// </summary>
        /// <remarks>
        /// <para><see cref="IResourceList.SortSettings"/> should be used instead.</para>
        /// <para>The value consists of property names
        /// separated by spaces. In addition to the registered property names, the following
        /// special property names can be used: <c>Type</c> (equal to the resource type
        /// name), <c>ID</c> (equal to the resource ID) and <c>DisplayName</c>
        /// (equal to the resource display name). Each property name can be followed by a
        /// <c>-</c> sign, which reverses the direction of sorting by that property.</para>
        /// <para>The properties are processed in order: if the values of the first sort
        /// property are equal, the values of the second sort property are compared.</para>
        /// </remarks>
        [Obsolete] string SortProps     { get; }

        /// <summary><seealso cref="SortProps"/><seealso cref="SortDirections"/><seealso cref="SortAscending"/>
        /// An array of IDs of properties that serve as sorting keys for this resource list.
        /// </summary>
 		/// <remarks>
 		/// <para><see cref="IResourceList.SortSettings"/> should be used instead.</para>
		/// <para>The first array item is the most significant one as it represents the primary sorting key.</para>
		/// <para>To determine the sort order, use <see cref="SortDirections"/> or <see cref="SortAscending"/>.</para>
		/// </remarks>
       [Obsolete] int[] SortPropIDs    { get; }

        /// <summary><seealso cref="SortPropIDs"/><seealso cref="SortProps"/><seealso cref="SortAscending"/>
        /// Information about the sort order, either ascending (1) or descending (-1), for each sorting key. To get a corresponding property id/name, use <see cref="SortPropIDs"/> or <see cref="SortProps"/>.
        /// </summary>
        /// <remarks>
        /// <para><see cref="IResourceList.SortSettings"/> should be used instead.</para>
        /// <para>Each array item is either 1 for ascending or -1 for descending sort order.</para>
        /// <para>The first array item is the most significant one as it corresponds to the primary sorting key. If you are interested in this value only, use <see cref="SortAscending"/>.</para>
        /// </remarks>
        [Obsolete] int[] SortDirections { get; }

        /// <summary><seealso cref="SortDirections"/>
        /// The sort order for the primary sorting key.
        /// </summary>
        /// <remarks><para><see cref="IResourceList.SortSettings"/> should be used instead.</para>
        /// <para>Provides a shortcut to the first item of the <see cref="SortDirections"/> array.</para></remarks>
        [Obsolete] bool SortAscending   { get; }

        /// <summary>
        /// Returns the sort settings for the resource list, or null if the resource list is not sorted.
        /// </summary>
        /// <since>2.0</since>
        SortSettings SortSettings { get; }

        /// <summary>
        /// Returns the union of the current list with another list.
        /// </summary>
        /// <param name="other">The list with which the current list is unioned, or <c>null</c>.</param>
        /// <returns>A new resource list which is the union of the current list and the <paramref name="other"/> list.</returns>
        /// <remarks>
        /// <para><paramref name="other"/> may be <c>null</c>; in this case, the original list is returned.</para>
        /// <para>Note that neither of the lists taking part in the union is modified and you should explicitly assign the union result to a new list. The result of <code>one.Union( another )</code> will not be put in the <c>one</c> list, instead, it will be dropped because the return value is not utilized. You should use either the <code>res = one.Union( another )</code> form, or, possibly, <code>one = one.Union( another )</code> if you wish to reuse the variable. However, in the latter case you should consider taking on another overload of this function, <see cref="Union(IResourceList, bool)"/>, passing <c>True</c> as its second parameter, to allow the resources be actually merged into existing <c>one</c> resource list object. Otherwise, a new object instance will be created and then assigned to <c>one</c> on return, which will drop the performance.</para>
        /// <para>The sorting order for the union result is determined as follows:</para>
        /// <list type="bullet">
        /// <item><description>If one of the lists participating in the union is sorted and the other is not, then the sorting will be propagated to the union product.</description></item>
		/// <item><description>If both lists have the same sorting then the product is sorted accordingly.</description></item>
		/// <item><description>If neither of the lists is sorted, the sorting of the product is undefined.</description></item>
		/// <item><description>If both lists are sorted and their sortings disagree, the union product sorting is undefined.</description></item>
		/// </list>
        /// </remarks>
		/// <example>
		/// <code>
		/// IResourceList	listMailSentToMe = …	// Acquire mail that was sent to me (personally)
		/// IResourceList	listMailFromMe = …	// Acquire mail that was sent by me
		/// 
		/// // Make a list of all my mail
		/// IResourceList	listMyMail = listMailSentToMe.Union( listMailFromMe );
		/// </code>
		/// <para>Note that neither <c>listMailSentToMe</c> nor <c>listMailFromMe</c> resource lists are changed in the above sample. If you do not intend to use them afterwards, consider using <see cref="Union(IResourceList, bool)"/> with the second parameter set to <c>True</c> instead to optimize performance.</para>
		/// </example>
        IResourceList Union( IResourceList other );

        /// <summary>
        /// Returns the union of the current list with another list, optionally reusing
        /// one of the source lists.
        /// </summary>
        /// <param name="other">The list with which the current list is unioned, or <c>null</c>.</param>
        /// <param name="allowMerge">If <c>true</c>, and if the current list or 
        /// <paramref name="other"/> is already a union, does not create a new list, but
        /// instead adds the new list to the union condition of the existing union list. </param>
        /// <returns>The union of the current list and the <paramref name="other"/> list.</returns>
        /// <remarks>
        /// <para>If a union of multiple lists is calculated, and the intermediate lists
        /// are not used by themselves, setting <paramref name="allowMerge"/> to <c>True</c> improves
        /// the performance of instantiating and maintaining the list. If its value is <c>False</c>, this overload behaves exactly the same way as a one-parameter <see cref="Union(IResourceList)"/>.</para>
        /// <para><paramref name="other"/> may be <c>null</c>; in this case, the original list
        /// is returned.</para>
        /// <para>The sorting order for the union result is determined as follows:</para>
        /// <list type="bullet">
        /// <item><description>If one of the lists participating in the union is sorted and the other is not, then the sorting will be propagated to the union product.</description></item>
		/// <item><description>If both lists have the same sorting then the product is sorted accordingly.</description></item>
		/// <item><description>If neither of the lists is sorted, the sorting of the product is undefined.</description></item>
		/// <item><description>If both lists are sorted and their sortings disagree, the union product sorting is undefined.</description></item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <para>Collect resources from multiple resource lists in a single list, reusing its object.</para>
		/// <code>
		/// // Some source lists
		/// IResourceList	list1, list2, list3, list4, list5, …
		/// 
		/// // Collect all their items here
		/// IResourceList	collection;
		/// 
		/// // Add the first list
		/// collection = list1;
		/// 
		/// // Add the second list (do not reuse now because this will affect the list1
		/// //    which we do not want to happen)
		/// collection = collection.Union( list2, false );
		/// 
		/// // Now we have a brand new resource list created for the collection object
		/// //    and we may overwrite it freely
		/// // Add the remaining lists
		/// collection.Union( list3, true );
		/// collection.Union( list4, true );
		/// collection.Union( list5, true );
		/// …	// Go on for all the lists
		/// </code>
		/// </example>
        IResourceList Union( IResourceList other, bool allowMerge );
        
        /// <summary>
        /// Returns the intersection of the current list with another list.
        /// </summary>
        /// <param name="other">The list with which the current list is intersected, or <c>null</c>.</param>
        /// <returns>A new resource list which is the intersection of the current list and
        /// the <paramref name="other"/> list.</returns>
        /// <remarks>
        /// <para><paramref name="other"/> may be <c>null</c>; in this case, the original list
        /// is returned.</para>
		/// <para>Note that neither of the lists taking part in the intersection is modified and you should explicitly assign the intersection result to a new list. The result of <code>one.Intersect( another )</code> will not be put in the <c>one</c> list, instead, it will be dropped because the return value is not utilized. You should use either the <code>res = one.Intersect( another )</code> form, or, possibly, <code>one = one.Intersect( another )</code> if you wish to reuse the variable. However, in the latter case you should consider taking on another overload of this function, <see cref="Intersect(IResourceList, bool)"/>, passing <c>True</c> as its second parameter, to allow the resources be actually merged into existing <c>one</c> resource list object. Otherwise, a new object instance will be created and then assigned to <c>one</c> on return, which will drop the performance.</para>
		/// <para>The sorting order for the intersection result is determined as follows:</para>
        /// <list type="bullet">
        /// <item><description>If one of the lists participating in the intersection is sorted and the other is not, then the sorting will be propagated to the intersection product.</description></item>
		/// <item><description>If both lists have the same sorting then the product is sorted accordingly.</description></item>
		/// <item><description>If neither of the lists is sorted, the sorting of the product is undefined.</description></item>
		/// <item><description>If both lists are sorted and their sortings disagree, the intersection product sorting is undefined.</description></item>
		/// </list>
        /// </remarks>
		/// <example>
		/// <code>
		/// IResourceList	annotated = …	// Acquire the annotated items
		/// IResourceList	flagged = …	// Acquire the flagged items
		/// 
		/// // Make a list of items that are both flagged and annotated
		/// IResourceList	both = annotated.Intersect( flagged );
		/// </code>
		/// <para>Note that neither <c>annotated</c> nor <c>flagged</c> resource lists are changed in the above sample. If you do not intend to use them afterwards, consider using <see cref="Intersect(IResourceList, bool)"/> with the second parameter set to <c>True</c> instead to optimize performance.</para>
		/// </example>
		IResourceList Intersect( IResourceList other );

        /// <summary>
        /// Returns the intersection of the current list with another list, optionally reusing
        /// one of the source lists.
        /// </summary>
        /// <param name="other">The list with which the current list is intersected, or <c>null</c>.</param>
        /// <param name="allowMerge">If <c>true</c>, and if the current list or 
        /// <paramref name="other"/> is already an intersection, does not create a new list, but
        /// instead adds the new list to the intersect condition of the existing intersection list.</param>
        /// <returns>The intersection of the current list and the <paramref name="other"/> list.</returns>
        /// <remarks>
        /// <para>If an intersection of multiple lists is calculated, and the intermediate lists
        /// are not used by themselves, setting <paramref name="allowMerge"/> to <c>True</c> improves
        /// the performance of instantiating and maintaining the list. If its value is <c>False</c>, this overload behaves exactly the same way as a one-parameter <see cref="Union(IResourceList)"/>.</para>
        /// <para><paramref name="other"/> may be <c>null</c>; in this case, the original list
        /// is returned.</para>
		/// <para>The sorting order for the intersection result is determined as follows:</para>
		/// <list type="bullet">
		/// <item><description>If one of the lists participating in the intersection is sorted and the other is not, then the sorting will be propagated to the intersection product.</description></item>
		/// <item><description>If both lists have the same sorting then the product is sorted accordingly.</description></item>
		/// <item><description>If neither of the lists is sorted, the sorting of the product is undefined.</description></item>
		/// <item><description>If both lists are sorted and their sortings disagree, the intersection product sorting is undefined.</description></item>
		/// </list>
		/// </remarks>
		/// <example>
		/// <para>Collect in a single list the resources that are common for several resource lists, reusing the target object.</para>
		/// <code>
		/// // Some source lists
		/// IResourceList	list1, list2, list3, list4, list5, …
		/// 
		/// // Collect the items that are common for them, here
		/// IResourceList	commons;
		/// 
		/// // Take the first list
		/// commons = list1;
		/// 
		/// // Intersect with the second list (do not reuse now because this will affect the list1
		/// //    which we do not want to happen)
		/// commons = commons.Intersect( list2, false );
		/// 
		/// // Now we have a brand new resource list created for the commons object
		/// //    and we may overwrite it freely
		/// // Intersect with the remaining lists
		/// commons.Intersect( list3, true );
		/// commons.Intersect( list4, true );
		/// commons.Intersect( list5, true );
		/// …	// Go on for all the lists
		/// </code>
		/// </example>
		IResourceList Intersect( IResourceList other, bool allowMerge );
        
        /// <summary>
        /// Returns the difference of the current resource list and another resource list.
        /// </summary>
        /// <param name="other">The resource list which is subtracted, or <c>null</c>.</param>
        /// <returns>A new resource list which is the difference of this and <paramref name="other"/>.</returns>
        /// <remarks>
        /// <para>The returned lists contains all resources which are contained in the
        /// current list but not in the <paramref name="other"/> list.</para>
        /// <para><paramref name="other"/> may be <c>null</c>; in this case, the original list
        /// is returned.</para>
        /// <para>The sorting order of the new list is undefined.</para>
        /// </remarks>
        /// <example>
        /// <para>Collect all the items that are flagged, but not annotated.</para>
        /// <code>
		/// IResourceList	annotated = …	// Acquire the annotated items
		/// IResourceList	flagged = …	// Acquire the flagged items
		/// 
		/// // Produce the list of flagged-but-not-annotated items
		/// IResourceList	diff = flagged.Minus( annotated );
		/// </code>
		/// <para>Note that neither <c>annotated</c> nor <c>flagged</c> resource lists are changed in the above sample.</para>
		/// </example>
        IResourceList Minus( IResourceList other );

        /// <summary>
        /// Checks if any of the resources in the list has the property with the specified name,
        /// or if the property is provided by one of the virtual property providers attached
        /// to the list.
        /// </summary>
        /// <param name="propName">Name of the property to check.</param>
        /// <returns><c>true</c> if at least one resource has the property, <c>false</c> otherwise.</returns>
        bool HasProp( string propName );

        /// <summary>
        /// Checks if any of the resources in the list has the property with the specified ID,
        /// or if the property is provided by one of the virtual property providers attached
        /// to the list.
        /// </summary>
        /// <param name="propId">ID of the property to check.</param>
        /// <returns><c>true</c> if at least one resource has the property, <c>false</c> otherwise.</returns>
        bool HasProp( int propId );

        /// <summary>
        /// Checks if all resources in the resource list are of the same type.
        /// </summary>
        /// <param name="type">The type that is checked.</param>
        /// <returns><c>true</c> if all the resources have the type <paramref name="type"/>, <c>false</c> otherwise.</returns>
        bool AllResourcesOfType( string type );
        
        /// <summary>
        /// Returns the array of all distinct resource types encountered in the resource list, arranged in lexicographical order.
        /// </summary>
        /// <returns>The sorted array of resource type names.</returns>
        string[] GetAllTypes();

        /// <summary>
        /// Attaches a virtual property provider to the resource list.
        /// </summary>
        /// <param name="provider">The provider to attach.</param>
        /// <remarks>Multiple providers can be attached to the resource list, providing
        /// values for different virtual properties.</remarks>
        void AttachPropertyProvider( IPropertyProvider provider );

        /// <summary>
        /// Returns the text of the property with the specified name for the resource
        /// at the specified index of the list.
        /// </summary>
        /// <param name="index">The index of the resource for which the property text
        /// is requested.</param>
        /// <param name="propName">The name of the property which is requested.</param>
        /// <returns>The value of the property.</returns>
        /// <remarks>If the resource has the property, it returns the actual property
        /// value. If it does not, the virtual property providers attached to the list
        /// are requested to provide the value.</remarks>
        string GetPropText( int index, string propName );

        /// <summary>
        /// Returns the text of the property with the specified ID for the resource
        /// at the specified index of the list.
        /// </summary>
        /// <param name="index">The index of the resource for which the property text
        /// is requested.</param>
        /// <param name="propId">The ID of the property which is requested.</param>
        /// <returns>The value of the property.</returns>
        /// <remarks>If the resource has the property, it returns the actual property
        /// value. If it does not, the virtual property providers attached to the list
        /// are requested to provide the value.</remarks>
        string GetPropText( int index, int propId );

        /// <summary>
        /// Returns the text of the property with the specified ID for the specified
        /// resource in the list.
        /// </summary>
        /// <param name="res">The resource for which the property text is requested.</param>
        /// <param name="propId">The ID of the property which is requested.</param>
        /// <returns>The value of the property.</returns>
        /// <remarks>If the resource has the property, it returns the actual property
        /// value. If it does not, the virtual property providers attached to the list
        /// are requested to provide the value.</remarks>
        /// <since>2.0</since>
        string GetPropText( IResource res, int propId );

        /// <summary>
        /// Checks if the resource at the specified index of the list has the property
        /// with the specified name.
        /// </summary>
        /// <param name="index">The index of the resource.</param>
        /// <param name="propName">The name of the property which is checked.</param>
        /// <returns><c>true</c> if the resource has the property, <c>false</c> otherwise.</returns>
        /// <remarks>The method returns <c>true</c> if either the resource actually has the
        /// property or a value can be provided by a virtual property provider.</remarks>
        bool HasProp( int index, string propName );

        /// <summary>
        /// Checks if the resource at the specified index of the list has the property
        /// with the specified ID.
        /// </summary>
        /// <param name="index">The index of the resource.</param>
        /// <param name="propId">The name of the property which is checked.</param>
        /// <returns><c>true</c> if the resource has the property, <c>false</c> otherwise.</returns>
        /// <remarks>The method returns <c>true</c> if either the resource actually has the
        /// property or a value can be provided by a virtual property provider.</remarks>
        bool HasProp( int index, int propId );

        /// <summary>
        /// Checks if the specified resource in the list has the property with the specified ID.
        /// </summary>
        /// <param name="res">The resource.</param>
        /// <param name="propId">The name of the property which is checked.</param>
        /// <returns><c>true</c> if the resource has the property, <c>false</c> otherwise.</returns>
        /// <remarks>The method returns <c>true</c> if either the resource actually has the
        /// property or a value can be provided by a virtual property provider.</remarks>
        /// <since>2.0</since>
        bool HasProp( IResource res, int propId );

        /// <summary>
        /// Sorts the list with the specified sort settings.
        /// </summary>
        /// <param name="sortSettings">The settings for sorting the resource list.</param>
        void Sort( SortSettings sortSettings );

        /// <summary>
        /// Sorts the list by the values of the specified properties.
        /// </summary>
        /// <param name="propNames">The string specifying the names of the properties
        /// by which the list is sorted.</param>
        /// <remarks>
        /// <para><see cref="IResourceList.Sort(SortSettings)"/> should be used instead.</para>
        /// <para>The <paramref name="propNames"/> string consists of property names
        /// separated by spaces. In addition to the registered property names, the following
        /// special property names can be used: <c>Type</c> (equal to the resource type
        /// name), <c>ID</c> (equal to the resource ID) and <c>DisplayName</c>
        /// (equal to the resource display name). Each property name can be followed by a
        /// <c>-</c> sign, which reverses the direction of sorting by that property.</para>
        /// <para>The properties are processed in order: if the values of the first sort
        /// property are equal, the values of the second sort property are compared.</para>
        /// </remarks>
        [Obsolete] void Sort( string propNames );

        /// <summary>
        /// Sorts the list by the values of the specified properties in either ascending or descending
        /// direction.
        /// </summary>
        /// <param name="propNames">The string specifying the names of the properties
        /// by which the list is sorted.</param>
        /// <param name="ascending"><c>true</c> if the list should be sorted in ascending direction
        /// (A to Z), <c>false</c> otherwise.</param>
        /// <remarks>
        /// <para><see cref="IResourceList.Sort(SortSettings)"/> should be used instead.</para>
        /// <para>The <paramref name="propNames"/> string consists of property names
        /// separated by spaces. In addition to the registered property names, the following
        /// special property names can be used: <c>Type</c> (equal to the resource type
        /// name), <c>ID</c> (equal to the resource ID) and <c>DisplayName</c>
        /// (equal to the resource display name). Each property name can be followed by a
        /// <c>-</c> sign, which reverses the direction of sorting by that property.</para>
        /// <para>The properties are processed in order: if the values of the first sort
        /// property are equal, the values of the second sort property are compared.</para>
        /// </remarks>
        [Obsolete] void Sort( string propNames, bool ascending );

        /// <summary>
        /// Sorts the list by the value of the properties with the specified IDs.
        /// </summary>
        /// <param name="propIds">Array of the IDs of properties by which the list is sorted.</param>
        /// <param name="ascending"><c>true</c> if the sort direction used for all properties
        /// is ascending (A to Z), <c>false</c> if the direction is descending (Z to A).</param>
        /// <remarks><para>The array of property IDs may contain IDs of special properties,
        /// as defined in the <see cref="ResourceProps"/> class.</para>
        /// <para>The properties are processed in order: if the values of the first sort
        /// property are equal, the values of the second sort property are compared.</para>
        /// </remarks>
        void Sort( int[] propIds, bool ascending );

        /// <summary>
        /// Sorts the list by the value of the properties with the specified IDs, in
        /// either ordered or equivalent mode.
        /// </summary>
        /// <param name="propIds">Array of the IDs of properties by which the list is sorted.</param>
        /// <param name="ascending"><c>true</c> if the sort direction used for all properties
        /// is ascending (A to Z), <c>false</c> if the direction is descending (Z to A).</param>
        /// <param name="propsEquivalent">If <c>false</c>, properties are compared in order.
        /// If <c>true</c>, the value of the first found non-<c>null</c> property is used for comparing
        /// every resource.</param>
        /// <remarks><para>The array of property IDs may contain IDs of special properties,
        /// as defined in the <see cref="ResourceProps"/> class.</para>
        /// <para>If <paramref name="propsEquivalent"/> is <c>false</c>, the properties are 
        /// processed in order: if the values of the first sort property are equal, the 
        /// values of the second sort property are compared.</para>
        /// <para>If <paramref name="propsEquivalent"/> is <c>true</c>, the properties are
        /// considered equivalent. For every resource, the array of properties is scanned
        /// until one property is found for which a resource has a non-<c>null</c> value. Then that
        /// value is compared with a single value which is found in the same way for
        /// another resource. </para>
        /// </remarks>
        void Sort( int[] propIds, bool ascending, bool propsEquivalent );

        /// <summary>
        /// Sorts the list by the value of the properties with the specified IDs, with
        /// a direction specified for each property.
        /// </summary>
        /// <param name="propIds">Array of the IDs of properties by which the list is sorted.</param>
        /// <param name="sortDirections">The sort direction which is used for comparing the values
        /// of each property. If <c>true</c>, the sort direction used for the property
        /// is ascending (A to Z), if <c>false</c> - the direction is descending (Z to A).</param>
        /// <remarks><para>The array of property IDs may contain IDs of special properties,
        /// as defined in the <see cref="ResourceProps"/> class.</para>
        /// <para>The properties are processed in order: if the values of the first sort
        /// property are equal, the values of the second sort property are compared.</para>
        /// </remarks>
        void Sort( int[] propIds, bool[] sortDirections );
        
        /// <summary>
        /// Sorts the list with the specified custom comparer.
        /// </summary>
        /// <param name="customComparer">The comparer which is used for comparing resources.</param>
        /// <param name="ascending">If <c>true</c>, the sort direction is ascending. If <c>false</c>,
        /// the direction is descending (comparison values returned by the comparer are inverted).</param>
        void Sort( IResourceComparer customComparer, bool ascending );

        /// <summary>
        /// Deletes all the resources in the resource list from the resource store.
        /// </summary>
        void DeleteAll();

        /// <summary>
        /// Tells that the client of the resource list wants to receive notifications 
        /// on the change of the specified property. If there are no watches set, 
        /// the client receives notifications about all the properties.
        /// </summary>
        /// <param name="propId">ID of the property to be monitored for changes.</param>
        /// <remarks>By default, each property notifies the listeners on value change. 
        /// This functionality can be limited if the list of properties of interest is 
        /// specified by calling <see cref="AddPropertyWatch"/> sequentially for each property. 
        /// After a first call, other properties but the <paramref name="propId"/> stop sending 
        /// notifications. Any subsequent call to this function enables notifications for one 
        /// more property.</remarks>
        void AddPropertyWatch( int propId );

        /// <summary>
        /// Tells that the client of the resource list wants to receive notifications 
        /// on the change of the specified properties. If there are no watches set, 
        /// the client receives notifications about all the properties.
        /// </summary>
        /// <param name="propIds">ID of the properies to be monitored for changes.</param>
        /// <remarks>By default, each property notifies the listeners on value change. </remarks>
        /// <since>2.0</since>
        void AddPropertyWatch( int[] propIds );

        /// <summary>
        /// Disconnects the handlers of the resource list and moves it back to predicate state.
        /// </summary>
        /// <remarks>Before the resource list items are first accessed, it exists only as a predicate structure describing which resources should be requested to populate the list contents. This function provides for returning the list into this state.</remarks>
        void Deinstantiate();

        /// <summary>
        /// Finds a resource matching the specified condition in the list.
        /// </summary>
        /// <param name="predicate">The condition to check for each resource.</param>
        /// <returns>First resource matching the condition, or null if none was found.</returns>
        [CanBeNull]
        IResource Find(Predicate<IResource> predicate);

        /// <summary>
        /// Returns an enumerable which enumerates only existing and not deleted resources
        /// in the resource list.
        /// </summary>
        /// <since>2.0</since>
        IEnumerable ValidResources { get; }

        /// <summary>
        /// Occurs when a resource is added to a live resource list.
        /// </summary>
        event ResourceIndexEventHandler ResourceAdded;

        /// <summary>
        /// Occurs before a resource is removed from a live resource list.
        /// </summary>
        event ResourceIndexEventHandler ResourceDeleting;

        /// <summary>
        /// Occurs when a resource in a live resource list is changed.
        /// </summary>
        event ResourcePropIndexEventHandler ResourceChanged;

        /// <summary>
        /// Occurs when a resource in a live resource list is removed from the list because
        /// a change in its properties caused it to no longer match the list conditions.
        /// </summary>
        /// <since>2.0</since>
        /// <remarks>This event is fired after the regular <see cref="ResourceDeleting"/> event.</remarks>
        event ResourcePropIndexEventHandler ChangedResourceDeleting;
    }

    /// <summary>
    /// Defines the interface for the provider of "virtual" properties for a resource list.
    /// </summary>
    /// <remarks>"Virtual" properties are properties that are not actually present on 
    /// resources in the list but can be displayed in the resource browser. Examples
    /// of such properties are search rank and similarity.</remarks>
    public interface IPropertyProvider
    {
        /// <summary>
        /// Checks if the provider can provide the values for the specified property ID.
        /// </summary>
        /// <param name="propId">ID of the property to check.</param>
        /// <returns><c>true</c> if the provider can provide values for that property,
        /// <c>false</c> otherwise.</returns>
        bool HasProp( int propId );

        /// <summary>
        /// Returns the value of a virtual property for a resource.
        /// </summary>
        /// <param name="res">The resource for which the property value is requested.</param>
        /// <param name="propId">The ID of the property which is requested.</param>
        /// <returns>The value of the property for the resource, or <c>null</c> if the value
        /// cannot be provided.</returns>
        object GetPropValue( IResource res, int propId );

        /// <summary>
        /// Notifies the resource list that the value of a virtual property for a
        /// specific resource has changed.
        /// </summary>
        event PropertyProviderChangeEventHandler ResourceChanged;
    }

    /// <summary>
    /// The interface for custom comparison of resources.
    /// </summary>
    /// <remarks>Instances of this interface can be passed to 
    /// <see cref="IResourceList.Sort(IResourceComparer, bool)"/>.</remarks>
    public interface IResourceComparer
    {
        /// <summary>
        /// Compares the specified two resources.
        /// </summary>
        /// <param name="r1">The first resource to compare.</param>
        /// <param name="r2">The second resource to compare.</param>
        /// <returns>A negative value if <paramref name="r1"/> is smaller than
        /// <paramref name="r2"/>; zero if they are equal; a positive value if 
        /// <paramref name="r1"/> is greater.</returns>
        int CompareResources( IResource r1, IResource r2 );
    }

    /// <summary>
    /// The collection of IDs of resources stored in a resource list.
    /// </summary>
    public interface IResourceIdCollection: ICollection
    {
		/// <summary>
		/// The ID of a resource at an index specified by <paramref name="index"/>.
		/// </summary>
        int this [int index] { get; }
    }
}
