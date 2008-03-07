/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;

namespace JetBrains.Omea.OpenAPI
{
	/// <summary><seealso cref="ICore.ResourceStore"/><seealso cref="IResource"/>
	/// A helper class that automatically marshals resource write operations to the resource thread.
	/// </summary>
	/// <remarks>
	/// <para>Do not modify resources in non-resource threads! At most times when Omea is running (except for the time when the
	/// <see cref="IPlugin.Register"/> method is called), only one thread is designated
	/// as the resource store write thread, and all operations that modify the resource store
	/// (creating resources, changing resource properties, deleting resources) must be executed
	/// in that thread. The <see cref="ResourceProxy"/> class provides an easy way to invoke a resource write
	/// operation in the resource thread synchronously or asynchronously.</para>
	/// <para>Even though a proxy is not needed when working on a resource thread, proxying resource modifications will not affect the performance significantly in this case.</para>
	/// <para>There is no global ResourceProxy instance. A new proxy object should be created each time you're going to modify another resource. A proxy instance is bound to a resource passed to its constructor for the whole proxy lifetime, and the wrapped resource can be extracted with <see cref="ResourceProxy.Resource"/>.</para>
	/// <para>There are two resource proxy usage scenarios: <i>Immediate</i> and <i>Batch</i>.</para>
	/// <list type="bullet">
	/// <item>
	/// <term>Immediate Mode</term>
	/// <description>
	/// <para>In the Immediate case a property gets modified or deleted immediately, either synchronously or asynchronously. <see cref="ResourceProxy.BeginUpdate"/> should not be called.</para>
	/// </description>
	/// </item>
	/// <item>
	/// <term>Batch Mode</term>
	/// <description>
	/// <para>In the Batch case a pair of (<see cref="ResourceProxy.BeginUpdate"/> and <see cref="ResourceProxy.EndUpdate"/>) or (<see cref="ResourceProxy.BeginUpdate"/> and <see cref="ResourceProxy.EndUpdateAsync"/>) calls is made. Any property modifications invoked inside this pair are not applied immediately but are deferred until one of the End… functions is called. When it occurs, all the modifications are committed either synchronously or asynchronously.</para>
	/// <para>To check if a particular modification function supports working in a batch, see its description. Note that Batch mode does not affect the property modification notifications that arrive to the event listeners. The only difference is that all the notifications happen not before the batch is committed with either End… function. It is unspecified whether all the modifications of a single property in a batch are optimized into a single modification or occur one by one as invoked (that also applies to property change notifications).</para>
	/// </description>
	/// </item>
	/// </list>
	/// <para>Property modifications occur synchronously and asynchronously. Here <i>synchronous modification</i> means that the calling thread halts execution until the property assignment completes in the resource thread, while <i>asynchronous modification</i> just gets queued to the resource thread job list and execution continues without waiting for the property change to occur.</para>
	/// <para>You may control the priority of asynchronous property assignment by altering the <see cref="ResourceProxy.AsyncPriority"/> proeprty value. It is recommended to assign the highest priority (<see cref="JobPriority.Immediate"/>) to assignments initiated from the user interface in order to decrease the UI response lag, and execute other assignments with lower priority values. See the constructor overload that takes the priority parameter if you plan on using the “inline” version.</para>
	/// <para>Most of the <see cref="ResourceProxy"/> functions have both asynchronous and synchronous versions indicated by the presence or absence of "…Async" suffix. Note that synchronous or asynchronous versions of individual property assignments behave identically when working in Batch mode (after calling <see cref="ResourceProxy.BeginUpdate"/>) and synchronous or asynchronous nature of the final commit operation is ruled by the End… function that you call, either <see cref="ResourceProxy.EndUpdate"/> for synchronous commit or <see cref="ResourceProxy.EndUpdateAsync"/> for asynchronous commit.</para>
	/// </remarks>
	/// <example>
	/// <para>Schedule an asynchronous resource modification and continue execution, “inline” usage:</para>
	/// <code>
	/// // Assign a string value to a property accessed by its property name
	/// // Execution is asynchronous, in immediate mode
	/// new ResourceProxy( resource ).SetPropAsync( "Name", "John Doe" );
	/// </code>
	/// <para>Modify a property and wait until the modification completes in the resource thread:</para>
	/// <code>
	/// // Assign a boolean value to a property accessed by its ID
	/// // Execution is synchronous, in immediate mode
	/// ResourceProxy proxy = new ResourceProxy( resource );
	/// proxy.SetProp( 239, true );
	/// </code>
	/// <para>Schedule a modification of a set of properties:</para>
	/// <code>
	/// ResourceProxy proxy = new ResourceProxy( resource );
	/// 
	/// // Enter Batch mode
	/// proxy.BeginUpdate();
	/// 
	/// // Schedule property modifications
	/// // Note that the resource does not get modified immediately when we
	/// //    execute SetProp and notification events do not fire
	/// // Using SetPropAsync instead of SetProp would produce the same effect here
	/// //    because we are in the Batch mode
	/// proxy.SetProp( "Name", "John Doe" );
	/// proxy.SetProp( "Age", 45 );
	/// proxy.SetProp( "Permitted", true );
	/// 
	/// // Commit the changes, do not wait for completion
	/// // After this call the notification events will fire
	/// EndUpdateAsync();
	/// 
	/// // If we had used EndUpdate() here instead we would have waited for
	/// //    the modification to complete on the resource thread
	/// </code>
	/// </example>
	public class ResourceProxy
	{
		/// <summary>
		/// Type of the pending operation.
		/// </summary>
		internal enum OperationType
		{
			/// <summary>
			/// The Resource Proxy is about to assign a new property value to the encapsulated resource.
			/// </summary>
			SetProp,

			/// <summary>
			/// The Resource Proxy is about to change the user interface name of the encapsulated resource.
			/// </summary>
			SetDisplayName,

			/// <summary>
			/// The Resource Proxy is about to link the encapsulated resource to another one.
			/// </summary>
			AddLink,

			/// <summary>
			/// The Resource Proxy is about to unlink the encapsulated resource from another one.
			/// </summary>
			DeleteLink
		} ;

		private class PendingOperation
		{
			private int _propID;

			private object _target;

			private OperationType _opType;

			internal PendingOperation( int propID, object target, OperationType opType )
			{
				_propID = propID;
				_target = target;
				_opType = opType;
			}

			internal int PropID
			{
				get { return _propID; }
			}

			internal object Target
			{
				get { return _target; }
			}

			internal OperationType OpType
			{
				get { return _opType; }
			}
		}

		private IResource _resource;

		private string _newResourceType = null;

		private ArrayList _pendingUpdates = null;

		private int _batchUpdateCount = 0;

		private JobPriority _asyncPriority = JobPriority.Normal;

		/// <summary><seealso cref="Resource"/>
		/// Creates a resource proxy for the specified resource.
		/// </summary>
		/// <param name="resource">Encapsulated resource.</param>
		/// <remarks>
		/// <para>After you create a proxy that encapsulates the specific resource, all the calls that modify that resource will be done through this proxy instance.</para>
		/// <para>Use the <see cref="Resource"/> property to access the encapsulated resource at a later time.</para>
		/// <para>Even though a proxy is not needed when working on a resource thread, proxying resource modifications will not affect the performance.</para>
		/// </remarks>
		public ResourceProxy( IResource resource )
		{
			if( resource == null )
				throw new ArgumentNullException( "resource" );

			_resource = resource;
		}

		/// <summary><seealso cref="Resource"/>
		/// Creates a resource proxy for the specified resource.
		/// </summary>
		/// <param name="resource">Encapsulated resource.</param>
		/// <param name="asyncpriority">Priority for the asynchonous operations, see <see cref="AsyncPriority"/> for details.</param>
		/// <remarks>
		/// <para>Use this constructor overload if you need to specify the priority of asynchronous operations, for example, when the modification is done in response to a user action and must occur as soon as possible. In this case, the <see cref="JobPriority.Immediate"/> value must be used.</para>
		/// <para>After you create a proxy that encapsulates the specific resource, all the calls that modify that resource will be done through this proxy instance.</para>
		/// <para>Use the <see cref="Resource"/> property to access the encapsulated resource at a later time. <see cref="AsyncPriority"/> allows to get or set the priority level after the proxy is created.</para>
		/// <para>Even though a proxy is not needed when working on a resource thread, proxying resource modifications will not affect the performance.</para>
		/// </remarks>
		/// <example><code>new ResourceProxy(res, JobPriority.Immediate).SetPropAsync("Count", 239);</code></example>
		/// <since>2.1</since>
		public ResourceProxy(IResource resource, JobPriority asyncpriority)
		{
			if( resource == null )
				throw new ArgumentNullException( "resource" );

			_resource = resource;
			_asyncPriority = asyncpriority;
		}

		/// <summary>
		/// Internal ctor, used in the BeginNewResource static function
		/// </summary>
		private ResourceProxy( string resType )
		{
			_newResourceType = resType;
			_resource = null;
		}

		/// <summary>
		/// Creates a new ResourceProxy instance for asynchronously creating an instance
		/// of a resource of a specified type.
		/// </summary>
		/// <param name="resType">The type of the resource to create.</param>
		/// <returns>The proxy instance.</returns>
		/// <remarks>You can set properties and add links to the resource proxy instance.
		/// The actual resource is created and saved when <see cref="EndUpdate"/> or
		/// <see cref="EndUpdateAsync"/> is called.</remarks>
		public static ResourceProxy BeginNewResource( string resType )
		{
			if( resType == null )
				throw new ArgumentNullException( "resType" );

			ResourceProxy proxy = new ResourceProxy( resType );
			proxy._batchUpdateCount++;
			return proxy;
		}

		/// <summary>
		/// The resource wrapped by this <see cref="ResourceProxy"/> instance.
		/// </summary>
		/// <value>
		/// The resource wrapped by this <see cref="ResourceProxy"/> instance.
		/// </value>
		/// <remarks>This is the resource which is modified when you call methods of this proxy instance.</remarks>
		/// <example>
		/// <code>
		/// IResource   resource = …
		/// 
		/// ResourceProxy   proxy = new ResourceProxy( resource );
		/// Debug.Assert( Object.ReferenceEquals( resource, proxy.Resource ) );
		/// </code>
		/// </example>
		public IResource Resource
		{
			get { return _resource; }
		}

		/// <summary>
		/// Priority of the asynchronous job that performs modifications of the resource.
		/// </summary>
		/// <value>
		/// Priority of the asynchronous job that performs modifications of the resource.
		/// </value>
		/// <remarks>
		/// <para>All the resource modifications must be performed in the resource thread. <see cref="ResourceProxy"/> implements the marshalling for you. Synchronous modifications (by either <see cref="SetProp"/>, <see cref="SetDisplayName"/>, <see cref="DeleteProp"/>, <see cref="AddLink"/>, or <see cref="DeleteLink"/> without prior call to <see cref="BeginUpdate"/>; or by a call to <see cref="EndUpdate"/>) are executed with <see cref="IAsyncProcessor.RunUniqueJob"/> and the calling thread waits for the modification to complete. The priority setting represented by <see cref="AsyncPriority"/> does not affect this execution.</para>
		/// <para>The asynchronous modifications (by either <see cref="SetPropAsync"/>, <see cref="DeletePropAsync"/>, or <see cref="DeleteAsync"/> without prior call to <see cref="BeginUpdate"/>; or by a call to <see cref="EndUpdateAsync"/>) are queued for execution with <see cref="IAsyncProcessor.QueueJob"/> and the calling thread does not wait for the job to complete. The priority value from <see cref="AsyncPriority"/> is assigned to the job and affects its execution.</para>
		/// <para>It is recommended that you specify the highest priority (<see cref="JobPriority.Immediate"/>) for all property assignments initiated by user interface in order to decrease the UI response lag, while background operations could employ lower priority values.</para>
		/// </remarks>
		/// <example>
		/// <code>
		/// IResource resource = …
		/// 
		/// ResourceProxy proxy = new ResourceProxy( resource );
		/// proxy.AsyncPriority = JobPriority.Immediate;
		/// proxy.SetPropAsync( "Unread", true );
		/// </code>
		/// </example>
		public JobPriority AsyncPriority
		{
			get { return _asyncPriority; }
			set { _asyncPriority = value; }
		}

		/// <summary>
		/// Begins a batch update of the resource.
		/// </summary>
		/// <remarks>Operations accumulated during a batch update are executed in a single resource
		/// thread operation when <see cref="EndUpdate"/> or <see cref="EndUpdateAsync"/> is called.</remarks>
		public void BeginUpdate()
		{
			_batchUpdateCount++;
		}

		/// <summary><seealso cref="BeginUpdate"/><seealso cref="SetPropAsync"/><seealso cref="DeleteProp"/>
		/// Assigns a new value to the resource property synchronously, or schedules the property update if <see cref="BeginUpdate"/> has been called.
		/// </summary>
		/// <param name="propName">Name of the property.</param>
		/// <param name="propValue">New value for the property, may be <c>null</c>.</param>
		/// <remarks>
		/// <para><see cref="SetProp"/> marshals resource property modification to the resource thread, as resource properties can be modified in the resource thread only. The resource being modified is passed to the proxy constructor and can be retrieved from the read-only <see cref="Resource"/> property. Because it cannot be changed during the proxy lifetime, you have to create a new proxy instance if you want to modify another resource.</para>
		/// <para>If used without prior calling <see cref="BeginUpdate"/>, the function executes immediately and synchronously, that is, the property update action is marshalled to the resource thread and execution of the current thread is stopped until it completes.</para>
		/// <para>If you want to update more than one resource property at a time, it's recommended to use the batch update scheme via <see cref="BeginUpdate"/>. In this case, all the property modifications inside the <see cref="BeginUpdate"/> and <see cref="EndUpdate"/>/<see cref="EndUpdateAsync"/> pair do not execute immediately but are deferred to be executed when any of the End… funcitons is called. See <see cref="BeginUpdate"/> for details and examples.</para>
		/// <para>If the property value in <paramref name="propValue"/> is <c>null</c>, the property is deleted from the resource (same as <see cref="DeleteProp"/>).</para>
		/// <para>Another version of this function that accepts a property ID instead of property name, <see cref="SetProp(int, object)"/>, is considered to have better performance because it need not lookup the property ID.</para>
		/// </remarks>
		/// <example>
		/// <para>This example covers the immediate synchronous use of <see cref="SetProp"/>, without a <see cref="BeginUpdate"/> call. For batch update case, see an example for <see cref="BeginUpdate"/> function.</para>
		/// <code>
		/// IResource resource = …
		/// 
		/// ResourceProxy proxy = new ResourceProxy( resource );	// Wrap the resource with a resource proxy
		/// …
		/// proxy.<b>SetProp</b>( "Name", "John Doe" ); // Access a property by its name
		/// …
		/// proxy.<b>SetProp</b>( "Age", 45 );
		/// …
		/// proxy.<b>SetProp</b>( "Permitted", true );
		/// </code>
		/// </example>
		public void SetProp( string propName, object propValue )
		{
			SetProp( Core.ResourceStore.GetPropId( propName ), propValue, false );
		}

		/// <summary><seealso cref="BeginUpdate"/><seealso cref="SetPropAsync"/><seealso cref="DeleteProp"/>
		/// Assigns a new value to the resource property synchronously, or schedules the property update if <see cref="BeginUpdate"/> has been called.
		/// </summary>
		/// <param name="propId">ID of the property.</param>
		/// <param name="propValue">New value for the property, may be <c>null</c>.</param>
		/// <remarks>
		/// <para><see cref="SetProp"/> marshals resource property modification to the resource thread, as resource properties can be modified in the resource thread only. The resource being modified is passed to the proxy constructor and can be retrieved from the read-only <see cref="Resource"/> property. Because it cannot be changed during the proxy lifetime, you have to create a new proxy instance if you want to modify another resource.</para>
		/// <para>If used without prior calling <see cref="BeginUpdate"/>, the function executes immediately and synchronously, that is, the property update action is marshalled to the resource thread and execution of the current thread is stopped until it completes.</para>
		/// <para>If you want to update more than one resource property at a time, it's recommended to use the batch update scheme via <see cref="BeginUpdate"/>. In this case, all the property modifications inside the <see cref="BeginUpdate"/> and <see cref="EndUpdate"/>/<see cref="EndUpdateAsync"/> pair do not execute immediately but are deferred to be executed when any of the End… funcitons is called. See <see cref="BeginUpdate"/> for details and examples.</para>
		/// <para>If the property value in <paramref name="propValue"/> is <c>null</c>, the property is deleted from the resource (same as <see cref="DeleteProp"/>).</para>
		/// <para>Another version of this function that accepts a property name instead of property ID, <see cref="SetProp(string, object)"/>, is considered to have worse performance because it needs to make a property ID lookup.</para>
		/// </remarks>
		/// <example>
		/// <para>This example covers the immediate synchronous use of <see cref="SetProp"/>, without a <see cref="BeginUpdate"/> call. For batch update case, see an example for <see cref="BeginUpdate"/> function.</para>
		/// <code>
		/// IResource resource = …
		/// 
		/// ResourceProxy proxy = new ResourceProxy( resource );	// Wrap the resource with a resource proxy
		/// 
		/// proxy.<b>SetProp</b>( 239, true );  // Access a property by its ID
		/// </code>
		/// </example>
		public void SetProp( int propId, object propValue )
		{
			SetProp( propId, propValue, false );
		}

		/// <summary><seealso cref="BeginUpdate"/><seealso cref="SetProp"/><seealso cref="DeletePropAsync"/>
		/// Assigns a new value to the resource property asynchronously, or schedules the property update if <see cref="BeginUpdate"/> has been called.
		/// </summary>
		/// <param name="propName">Name of the property.</param>
		/// <param name="propValue">New value for the property, may be <c>null</c>.</param>
		/// <remarks>
		/// <para><see cref="SetPropAsync"/> marshals resource property modification to the resource thread, as resource properties can be modified in the resource thread only. The resource being modified is passed to the proxy constructor and can be retrieved from the read-only <see cref="Resource"/> property. Because it cannot be changed during the proxy lifetime, you have to create a new proxy instance if you want to modify another resource.</para>
		/// <para>If used without prior calling <see cref="BeginUpdate"/>, the function executes immediately and asynchronously, that is, the property update action is marshalled to the resource thread and the current thread continues its execution without waiting for the property assignment to complete. The marshalling is done by executing an asynchronous job on the resource thread, and you can modify the priority of this job by changing the <see cref="AsyncPriority"/> property. It is recommended that you specify the highest priority (<see cref="JobPriority.Immediate"/>) for all property assignments initiated by user interface in order to decrease the UI response lag, while background operations could employ lower priority values.</para>
		/// <para>If you want to update more than one resource property at a time, it's recommended to use the batch update scheme via <see cref="BeginUpdate"/>. In this case, all the property modifications inside the <see cref="BeginUpdate"/> and <see cref="EndUpdate"/>/<see cref="EndUpdateAsync"/> pair do not execute immediately but are deferred to be executed when any of the End… funcitons is called. See <see cref="BeginUpdate"/> for details and examples.</para>
		/// <para>If the property value in <paramref name="propValue"/> is <c>null</c>, the property is deleted from the resource (same as <see cref="DeleteProp"/>).</para>
		/// <para>Another version of this function that accepts a property ID instead of property name, <see cref="SetPropAsync(int, object)"/>, is considered to have better performance because it need not lookup the property ID.</para>
		/// </remarks>
		/// <example>
		/// <para>This example covers the immediate asynchronous use of <see cref="SetPropAsync"/>, without a <see cref="BeginUpdate"/> call. For batch update case, see an example for <see cref="BeginUpdate"/> function.</para>
		/// <code>
		/// IResource resource = …
		/// 
		/// ResourceProxy proxy = new ResourceProxy( resource );	// Wrap the resource with a resource proxy
		/// …
		/// proxy.<b>SetPropAsync</b>( "Name", "John Doe" ); // Access a property by its name
		/// …
		/// proxy.<b>SetPropAsync</b>( "Age", 45 );
		/// …
		/// proxy.<b>SetPropAsync</b>( "Permitted", true );
		/// </code>
		/// <para>or,</para>
		/// <code>
		/// IResource resource = …
		/// 
		/// // Wrap the resource with a resource proxy and use it in-place
		/// new ResourceProxy( resource ).<b>SetPropAsync</b>( "Company", "JetBrains" );
		/// </code>
		/// </example>
		public void SetPropAsync( string propName, object propValue )
		{
			SetProp( Core.ResourceStore.GetPropId( propName ), propValue, true );
		}

		/// <summary><seealso cref="BeginUpdate"/><seealso cref="SetProp"/><seealso cref="DeletePropAsync"/>
		/// Assigns a new value to the resource property asynchronously, or schedules the property update if <see cref="BeginUpdate"/> has been called.
		/// </summary>
		/// <param name="propId">ID of the property.</param>
		/// <param name="propValue">New value for the property, may be <c>null</c>.</param>
		/// <remarks>
		/// <para><see cref="SetPropAsync"/> marshals resource property modification to the resource thread, as resource properties can be modified in the resource thread only. The resource being modified is passed to the proxy constructor and can be retrieved from the read-only <see cref="Resource"/> property. Because it cannot be changed during the proxy lifetime, you have to create a new proxy instance if you want to modify another resource.</para>
		/// <para>If used without prior calling <see cref="BeginUpdate"/>, the function executes immediately and asynchronously, that is, the property update action is marshalled to the resource thread and the current thread continues its execution without waiting for the property assignment to complete. The marshalling is done by executing an asynchronous job on the resource thread, and you can modify the priority of this job by changing the <see cref="AsyncPriority"/> property. It is recommended that you specify the highest priority (<see cref="JobPriority.Immediate"/>) for all property assignments initiated by user interface in order to decrease the UI response lag, while background operations could employ lower priority values.</para>
		/// <para>If you want to update more than one resource property at a time, it's recommended to use the batch update scheme via <see cref="BeginUpdate"/>. In this case, all the property modifications inside the <see cref="BeginUpdate"/> and <see cref="EndUpdate"/>/<see cref="EndUpdateAsync"/> pair do not execute immediately but are deferred to be executed when any of the End… funcitons is called. See <see cref="BeginUpdate"/> for details and examples.</para>
		/// <para>If the property value in <paramref name="propValue"/> is <c>null</c>, the property is deleted from the resource (same as <see cref="DeleteProp"/>).</para>
		/// <para>Another version of this function that accepts a property name instead of property ID, <see cref="SetPropAsync(string, object)"/>, is considered to have worse performance because it needs to make a property ID lookup.</para>
		/// </remarks>
		/// <example>
		/// <para>This example covers the immediate asynchronous use of <see cref="SetPropAsync"/>, without a <see cref="BeginUpdate"/> call. For batch update case, see an example for <see cref="BeginUpdate"/> function.</para>
		/// <code>
		/// IResource resource = …
		/// 
		/// ResourceProxy proxy = new ResourceProxy( resource );	// Wrap the resource with a resource proxy
		/// …
		/// proxy.<b>SetPropAsync</b>( 239, true ); // Access a property by its ID
		/// </code>
		/// <para>or,</para>
		/// <code>
		/// IResource resource = …
		/// 
		/// // Wrap the resource with a resource proxy and use it in-place
		/// new ResourceProxy( resource ).<b>SetPropAsync</b>( 239, true );
		/// </code>
		/// </example>
		public void SetPropAsync( int propId, object propValue )
		{
			SetProp( propId, propValue, true );
		}

		/// <summary>
		/// Sets the display name of the target resource.
		/// </summary>
		/// <param name="displayName">The new display name value.</param>
		/// <remarks>By default, the display name is automatically generated from the properties of the resource,
		/// based on the template which is specified when the resource type is registered. 
		/// If a display name for a resource is assigned explicitly, it overrides the default generated
		/// display name, but an explicitly assigned display name is not updated automatically
		/// when the properties of a resource are changed.
		/// </remarks>
		public void SetDisplayName( string displayName )
		{
			if( _batchUpdateCount == 0 && Core.ResourceStore.IsOwnerThread() )
			{
				_resource.DisplayName = displayName;
				return;
			}
			AddPendingOperation( new PendingOperation( 0, displayName, OperationType.SetDisplayName ), true );
		}

		/// <summary>
		/// Internal implementation for all the <see cref="SetProp"/> and <see cref="SetPropAsync"/> overloads.
		/// </summary>
		/// <param name="propId">Property ID as passed by the caller or extracted from the property name.</param>
		/// <param name="propValue">New property value.</param>
		/// <param name="async">Whether the assignment should be executed synchronously or not.</param>
		private void SetProp( int propId, object propValue, bool async )
		{
			if( Core.ResourceStore.PropTypes[ propId ].DataType == PropDataType.Link &&
				propValue != null && propValue == _resource )
			{
				throw new StorageException( "Cannot link a resource to itself (resource type " + _resource.Type +
					", property type " + Core.ResourceStore.PropTypes[ propId ].Name + ")" );
			}

			if( _batchUpdateCount == 0 && Core.ResourceStore.IsOwnerThread() )
			{
				if( propValue == null )
				{
					_resource.DeleteProp( propId );
				}
				else
				{
					_resource.SetProp( propId, propValue );
				}
				return;
			}

			AddPendingOperation( new PendingOperation( propId, propValue, OperationType.SetProp ), async );
		}

		/// <summary><seealso cref="BeginUpdate"/><seealso cref="DeletePropAsync"/><seealso cref="SetProp"/>
		/// Deletes the specified resource property synchronously, or schedules the property deletion if <see cref="BeginUpdate"/> has been called.
		/// </summary>
		/// <param name="propName">Name of the property to be deleted.</param>
		/// <remarks>
		/// <para><see cref="DeleteProp"/> marshals resource property deletion to the resource thread, as resource properties can be modified in the resource thread only. The resource being modified is passed to the proxy constructor and can be retrieved from the read-only <see cref="Resource"/> property. Because it cannot be changed during the proxy lifetime, you have to create a new proxy instance if you want to modify another resource.</para>
		/// <para>If used without prior calling <see cref="BeginUpdate"/>, the function executes immediately and synchronously, that is, the property deletion action is marshalled to the resource thread and execution of the current thread is stopped until it completes.</para>
		/// <para>If you want to update more than one resource property at a time, it's recommended to use the batch update scheme via <see cref="BeginUpdate"/>. In this case, all the property modifications inside the <see cref="BeginUpdate"/> and <see cref="EndUpdate"/>/<see cref="EndUpdateAsync"/> pair do not execute immediately but are deferred to be executed when any of the End… funcitons is called. See <see cref="BeginUpdate"/> for details and examples.</para>
		/// <para>It is also safe to delete a property by supplying a <c>null</c> as a property value to one of the <see cref="SetProp"/> functions.</para>
		/// <para>Another version of this function that accepts a property ID instead of property name, <see cref="DeleteProp(int)"/>, is considered to have better performance because it need not lookup the property ID.</para>
		/// </remarks>
		/// <example>
		/// <para>This example covers the immediate synchronous use of <see cref="DeleteProp"/>, without a <see cref="BeginUpdate"/> call. For batch update case, see an example for <see cref="BeginUpdate"/> function.</para>
		/// <code>
		/// IResource resource = …
		/// 
		/// ResourceProxy proxy = new ResourceProxy( resource );	// Wrap the resource with a resource proxy
		/// proxy.<b>DeleteProp</b>( "Comment" ); // Access a property by its name
		/// </code>
		/// <para>Another way to delete this property is to call <c>proxy.<b>SetProp</b>( "Comment", null );</c>.</para>
		/// </example>
		public void DeleteProp( string propName )
		{
			SetProp( propName, null );
		}

		/// <summary><seealso cref="BeginUpdate"/><seealso cref="DeletePropAsync"/><seealso cref="SetProp"/>
		/// Deletes the specified resource property synchronously, or schedules the property deletion if <see cref="BeginUpdate"/> has been called.
		/// </summary>
		/// <param name="propId">ID of the property to be deleted.</param>
		/// <remarks>
		/// <para><see cref="DeleteProp"/> marshals resource property deletion to the resource thread, as resource properties can be modified in the resource thread only. The resource being modified is passed to the proxy constructor and can be retrieved from the read-only <see cref="Resource"/> property. Because it cannot be changed during the proxy lifetime, you have to create a new proxy instance if you want to modify another resource.</para>
		/// <para>If used without prior calling <see cref="BeginUpdate"/>, the function executes immediately and synchronously, that is, the property deletion action is marshalled to the resource thread and execution of the current thread is stopped until it completes.</para>
		/// <para>If you want to update more than one resource property at a time, it's recommended to use the batch update scheme via <see cref="BeginUpdate"/>. In this case, all the property modifications inside the <see cref="BeginUpdate"/> and <see cref="EndUpdate"/>/<see cref="EndUpdateAsync"/> pair do not execute immediately but are deferred to be executed when any of the End… funcitons is called. See <see cref="BeginUpdate"/> for details and examples.</para>
		/// <para>It is also safe to delete a property by supplying a <c>null</c> as a property value to one of the <see cref="SetProp"/> functions.</para>
		/// <para>Another version of this function that accepts a property name instead of property ID, <see cref="DeleteProp(string)"/>, is considered to have worse performance because it needs to make a property ID lookup.</para>
		/// </remarks>
		/// <example>
		/// <para>This example covers the immediate synchronous use of <see cref="DeleteProp"/>, without a <see cref="BeginUpdate"/> call. For batch update case, see an example for <see cref="BeginUpdate"/> function.</para>
		/// <code>
		/// IResource resource = …
		/// 
		/// ResourceProxy proxy = new ResourceProxy( resource );	// Wrap the resource with a resource proxy
		/// proxy.<b>DeleteProp</b>( 239 ); // Access a property by its ID
		/// </code>
		/// <para>Another way to delete this property is to call <c>proxy.<b>SetProp</b>( 239, null );</c>.</para>
		/// </example>
		public void DeleteProp( int propId )
		{
			SetProp( propId, null );
		}

		/// <summary><seealso cref="BeginUpdate"/><seealso cref="DeleteProp"/><seealso cref="SetPropAsync"/>
		/// Assigns a new value to the resource property asynchronously, or schedules the property update if <see cref="BeginUpdate"/> has been called.
		/// </summary>
		/// <param name="propName">Name of the property to be deleted.</param>
		/// <remarks>
		/// <para><see cref="SetPropAsync"/> marshals resource property deletion to the resource thread, as resource properties can be modified in the resource thread only. The resource being modified is passed to the proxy constructor and can be retrieved from the read-only <see cref="Resource"/> property. Because it cannot be changed during the proxy lifetime, you have to create a new proxy instance if you want to modify another resource.</para>
		/// <para>If used without prior calling <see cref="BeginUpdate"/>, the function executes immediately and asynchronously, that is, the property deletion action is marshalled to the resource thread and the current thread continues its execution without waiting for the property assignment to complete. The marshalling is done by executing an asynchronous job on the resource thread, and you can modify the priority of this job by changing the <see cref="AsyncPriority"/> property. It is recommended that you specify the highest priority (<see cref="JobPriority.Immediate"/>) for all property assignments initiated by user interface in order to decrease the UI response lag, while background operations could employ lower priority values.</para>
		/// <para>If you want to update more than one resource property at a time, it's recommended to use the batch update scheme via <see cref="BeginUpdate"/>. In this case, all the property modifications inside the <see cref="BeginUpdate"/> and <see cref="EndUpdate"/>/<see cref="EndUpdateAsync"/> pair do not execute immediately but are deferred to be executed when any of the End… funcitons is called. See <see cref="BeginUpdate"/> for details and examples.</para>
		/// <para>It is also safe to delete a property by supplying a <c>null</c> as a property value to one of the <see cref="SetProp"/> functions.</para>
		/// <para>Another version of this function that accepts a property ID instead of property name, <see cref="DeletePropAsync(int)"/>, is considered to have better performance because it need not lookup the property ID.</para>
		/// </remarks>
		/// <example>
		/// <para>This example covers the immediate asynchronous use of <see cref="DeletePropAsync"/>, without a <see cref="BeginUpdate"/> call. For batch update case, see an example for <see cref="BeginUpdate"/> function.</para>
		/// <code>
		/// IResource resource = …
		/// 
		/// // Access a property by its name
		/// new ResourceProxy( resource ).<b>DeletePropAsync</b>( "Comment" );	// Wrap the resource with a resource proxy
		/// </code>
		/// <para>or,</para>
		/// <code>
		/// IResource resource = …
		/// 
		/// // Delete the property by assigning it a null value
		/// new ResourceProxy( resource ).SetPropAsync( "Comment", null );
		/// </code>
		/// </example>
		public void DeletePropAsync( string propName )
		{
			SetProp( Core.ResourceStore.GetPropId( propName ), null, true );
		}

		/// <summary><seealso cref="BeginUpdate"/><seealso cref="DeleteProp"/><seealso cref="SetPropAsync"/>
		/// Assigns a new value to the resource property asynchronously, or schedules the property update if <see cref="BeginUpdate"/> has been called.
		/// </summary>
		/// <param name="propId">ID of the property to be deleted.</param>
		/// <remarks>
		/// <para><see cref="SetPropAsync"/> marshals resource property deletion to the resource thread, as resource properties can be modified in the resource thread only. The resource being modified is passed to the proxy constructor and can be retrieved from the read-only <see cref="Resource"/> property. Because it cannot be changed during the proxy lifetime, you have to create a new proxy instance if you want to modify another resource.</para>
		/// <para>If used without prior calling <see cref="BeginUpdate"/>, the function executes immediately and asynchronously, that is, the property deletion action is marshalled to the resource thread and the current thread continues its execution without waiting for the property assignment to complete. The marshalling is done by executing an asynchronous job on the resource thread, and you can modify the priority of this job by changing the <see cref="AsyncPriority"/> property. It is recommended that you specify the highest priority (<see cref="JobPriority.Immediate"/>) for all property assignments initiated by user interface in order to decrease the UI response lag, while background operations could employ lower priority values.</para>
		/// <para>If you want to update more than one resource property at a time, it's recommended to use the batch update scheme via <see cref="BeginUpdate"/>. In this case, all the property modifications inside the <see cref="BeginUpdate"/> and <see cref="EndUpdate"/>/<see cref="EndUpdateAsync"/> pair do not execute immediately but are deferred to be executed when any of the End… funcitons is called. See <see cref="BeginUpdate"/> for details and examples.</para>
		/// <para>It is also safe to delete a property by supplying a <c>null</c> as a property value to one of the <see cref="SetProp"/> functions.</para>
		/// <para>Another version of this function that accepts a property name instead of property ID, <see cref="DeletePropAsync(string)"/>, is considered to have worse performance because it needs to make a property ID lookup.</para>
		/// </remarks>
		/// <example>
		/// <para>This example covers the immediate asynchronous use of <see cref="DeletePropAsync"/>, without a <see cref="BeginUpdate"/> call. For batch update case, see an example for <see cref="BeginUpdate"/> function.</para>
		/// <code>
		/// IResource resource = …
		/// 
		/// // Access a property by its id
		/// new ResourceProxy( resource ).<b>DeletePropAsync</b>( 239 );	// Wrap the resource with a resource proxy
		/// </code>
		/// <para>or,</para>
		/// <code>
		/// IResource resource = …
		/// 
		/// // Delete the property by assigning it a null value
		/// new ResourceProxy( resource ).SetPropAsync( 239, null );
		/// </code>
		/// </example>
		public void DeletePropAsync( int propId )
		{
			SetProp( propId, null, true );
		}

		/// <summary>
		/// Adds a link with the specified property name to the specified target resource.
		/// </summary>
		/// <param name="propName">Name of the link property.</param>
		/// <param name="target">Resource to which the link is added.</param>
		public void AddLink( string propName, IResource target )
		{
			AddLink( Core.ResourceStore.GetPropId( propName ), target );
		}

		/// <summary>
		/// Adds a link with the specified property ID to the specified target resource.
		/// </summary>
		/// <param name="propId">ID of the link property.</param>
		/// <param name="target">Resource to which the link is added.</param>
		public void AddLink( int propId, IResource target )
		{
			if( Core.ResourceStore.PropTypes[ propId ].DataType == PropDataType.Link &&
				target != null && target == _resource )
			{
				throw new StorageException( "Cannot link a resource to itself (resource type " + _resource.Type +
					", property type " + Core.ResourceStore.PropTypes[ propId ].Name + ")" );
			}

			if( _batchUpdateCount == 0 && Core.ResourceStore.IsOwnerThread() )
			{
				_resource.AddLink( propId, target );
				return;
			}

			AddPendingOperation( new PendingOperation( propId, target, OperationType.AddLink ), false );
		}

		/// <summary>
		/// Deletes a link with the specified property name to the specified resource.
		/// </summary>
		/// <param name="propName">Name of the link property.</param>
		/// <param name="target">Resource to which the link is deleted.</param>
		public void DeleteLink( string propName, IResource target )
		{
			DeleteLink( Core.ResourceStore.GetPropId( propName ), target );
		}

		/// <summary>
		/// Deletes a link with the specified property ID to the specified resource.
		/// </summary>
		/// <param name="propId">ID of the link property.</param>
		/// <param name="target">Resource to which the link is deleted.</param>
		public void DeleteLink( int propId, IResource target )
		{
			if( _batchUpdateCount == 0 && Core.ResourceStore.IsOwnerThread() )
			{
				_resource.DeleteLink( propId, target );
				return;
			}

			AddPendingOperation( new PendingOperation( propId, target, OperationType.DeleteLink ), false );
		}

		/// <summary>
		/// Deletes all links with the specified property name.
		/// </summary>
		/// <param name="propName">Name of the property for which the links are deleted.</param>
		public void DeleteLinks( string propName )
		{
			DeleteLinks( Core.ResourceStore.GetPropId( propName ) );
		}

		/// <summary>
		/// Deletes all links with the specified property ID.
		/// </summary>
		/// <param name="propId">ID of the property for which the links are deleted.</param>
		/// <remarks>If <paramref name="propId"/> is a directed link, only the links from the
		/// resource are deleted. To delete links to the resource, specify a negative property
		/// ID (for example, -5 instead of 5).</remarks>
		public void DeleteLinks( int propId )
		{
			if( _batchUpdateCount == 0 && Core.ResourceStore.IsOwnerThread() )
			{
				_resource.DeleteLinks( propId );
				return;
			}

			AddPendingOperation( new PendingOperation( propId, null, OperationType.DeleteLink ), false );
		}

		/// <summary>
		/// Synchronously commits a batch update of a resource.
		/// </summary>
		public void EndUpdate()
		{
			EndUpdate( false );
		}

		/// <summary>
		/// Asynchronously commits a batch update of the resource.
		/// </summary>
		public void EndUpdateAsync()
		{
			EndUpdate( true );
		}

		private void EndUpdate( bool async )
		{
			if( _batchUpdateCount == 0 )
			{
				throw new InvalidOperationException( "EndUpdate() called before BeginUpdate()" );
			}
			if( (_pendingUpdates != null && _pendingUpdates.Count > 0) || _resource == null )
			{
				if( Core.ResourceStore.IsOwnerThread() )
					ProcessPendingUpdates();
				else
				{
					string name = GetOperationName();
					if( async )
						Core.ResourceAP.QueueJob( _asyncPriority, name, new MethodInvoker( ProcessPendingUpdates ) );
					else
						Core.ResourceAP.RunUniqueJob( name, new MethodInvoker( ProcessPendingUpdates ) );
				}
			}
			_batchUpdateCount--;
		}

		private void AddPendingOperation( PendingOperation op, bool async )
		{
			lock( this )
			{
				if( _pendingUpdates == null )
				{
					_pendingUpdates = new ArrayList();
				}
				_pendingUpdates.Add( op );
			}

			if( _batchUpdateCount == 0 )
			{
				if( async )
					Core.ResourceAP.QueueJob( _asyncPriority, GetOperationName(), new MethodInvoker( ProcessPendingUpdates ) );
				else
					Core.ResourceAP.RunUniqueJob( GetOperationName(), new MethodInvoker( ProcessPendingUpdates ) );
			}
		}

		private string GetOperationName()
		{
			if( _newResourceType != null )
			{
				return "ResourceProxy for new resource of type " + _newResourceType;
			}
			else
			{
				return "ResourceProxy for resource " + _resource.Id + " of type " + _resource.Type;
			}
		}

		/// <summary>
		/// Executes the deferred property update tasks upon EndUpdate/EndUpdateAsync.
		/// </summary>
		private void ProcessPendingUpdates()
		{
			Debug.Assert( _resource != null || _newResourceType != null );
			if( _resource == null )
			{
				_resource = Core.ResourceStore.BeginNewResource( _newResourceType );
			}
			else
			{
				try
				{
					_resource.BeginUpdate();
				}
				catch( ResourceDeletedException )
				{
					// the resource may have been deleted since the proxy was created;
					// ignore and exit silently
					return;
				}
			}
			lock( this )
			{
				if( _pendingUpdates != null )
				{
					foreach( PendingOperation op in _pendingUpdates )
					{
						if( op.Target is IResource && ((IResource) op.Target).IsDeleted )
							continue;

						if( op.OpType == OperationType.AddLink )
						{
							IResource target = (IResource) op.Target;
							if( op.PropID < 0 )
							{
								target.AddLink( -op.PropID, _resource );
							}
							else
							{
								_resource.AddLink( op.PropID, target );
							}
						}
						else if( op.OpType == OperationType.DeleteLink )
						{
							if( op.Target == null )
							{
								_resource.DeleteLinks( op.PropID );
							}
							else
							{
								_resource.DeleteLink( op.PropID, (IResource) op.Target );
							}
						}
						else if( op.OpType == OperationType.SetDisplayName )
						{
							_resource.DisplayName = (string) op.Target;
						}
						else if( op.Target == null )
						{
							_resource.DeleteProp( op.PropID );
						}
						else
						{
							_resource.SetProp( op.PropID, op.Target );
						}
					}
					_pendingUpdates.Clear();
				}
			}
			_resource.EndUpdate();
		}

		/// <summary><seealso cref="DeleteAsync"/><seealso cref="DeleteProp"/>
		/// Immediately and synchronously deletes the encapsulated resource.
		/// </summary>
		/// <remarks>
		/// <para>This function does not support Batch mode execution (see <see cref="BeginUpdate"/>) because it is useless to batch any property modifications together with the whole resource deletion.</para>
		/// <para>When you call this function, the resource deletion job gets started in the resource thread and the function waits for it to complete. The calling thread execution is stopped until the job completes.</para>
		/// <para>After the resource is deleted, the proxy becomes invalid because property modifications are prohibited for the deleted resources. However, read-only resource operations can still be executed.</para>
		/// <para>You should not delete resources directly if you are executing in a thread other than the resource thread because this is not a read-only operation.</para>
		/// <para>To delete a particular resource property, use <see cref="DeleteProp"/>. It is also possible to delete a property by assigning it a <c>null</c> value with <see cref="SetProp"/>.</para>
		/// </remarks>
		/// <example>
		/// <code>
		/// IResource resource = …
		/// 
		/// // Wait for the resource to be deleted
		/// new ResourceProxy( resource ).Delete();
		/// </code>
		/// </example>
		public void Delete()
		{
			if( _resource != null )
			{
				Core.ResourceAP.RunUniqueJob( "ResourceProxy for deleting resource " + _resource.Id,
				                              new MethodInvoker( DoDelete ) );
			}
		}

		/// <summary><seealso cref="Delete"/><seealso cref="DeletePropAsync"/>
		/// Immediately and asynchronously deletes the encapsulated resource.
		/// </summary>
		/// <remarks>
		/// <para>This function does not support Batch mode execution (see <see cref="BeginUpdate"/>) because it is useless to batch any property modifications together with the whole resource deletion.</para>
		/// <para>When you call this function, the resource deletion job gets schedulled for execution on the resource thread and the function does not wait for it to complete. The calling thread execution continues immediately.</para>
		/// <para>After the resource is deleted, the proxy becomes invalid because property modifications are prohibited for the deleted resources. However, read-only resource operations can still be executed.</para>
		/// <para>You should not delete resources directly if you are executing in a thread other than the resource thread because this is not a read-only operation.</para>
		/// <para>To delete a particular resource property, use <see cref="DeletePropAsync"/>. It is also possible to delete a property by assigning it a <c>null</c> value with <see cref="SetPropAsync"/>.</para>
		/// </remarks>
		/// <example>
		/// <code>
		/// IResource resource = …
		/// 
		/// // Initiate resource deletion and continue execution
		/// new ResourceProxy( resource ).DeleteAsync();
		/// </code>
		/// </example>
		public void DeleteAsync()
		{
			if( _resource != null )
			{
				Core.ResourceAP.QueueJob( _asyncPriority,
				                          "ResourceProxy for deleting resource " + _resource.Id, new MethodInvoker( DoDelete ) );
			}
		}

		private void DoDelete()
		{
			if( _resource != null )
			{
				_resource.Delete();
			}
		}
	}
}