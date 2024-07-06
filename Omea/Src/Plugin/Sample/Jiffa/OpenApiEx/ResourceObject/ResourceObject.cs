// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OpenApiEx
{
	/// <summary>
	/// A base class that wraps a resource.
	/// </summary>
	public abstract class ResourceObject : IResourceObject
	{
		/// <summary>
		/// Stores the resource that is wrapped by this business object.
		/// </summary>
		private IResource _resource;

		/// <summary>
		/// Stores whether all the write-operations to the resource properties should be asynchronous.
		/// </summary>
		private bool _async = true;

		/// <summary>
		/// In case <see cref="_async"/> is <c>True</c>, specifies the async operations priority.
		/// </summary>
		private JobPriority _priority = JobPriority.Normal;

		/// <summary>
		/// Inits the instance by attaching to an existing resource.
		/// </summary>
		/// <param name="resource">The resource to back the resource object, must not be Null.</param>
		public ResourceObject(IResource resource)
		{
			if(resource == null)
				throw new ArgumentNullException("resource");
			_resource = resource;
		}

		/// <summary>
		/// Gets the underlying resource storing the object data.
		/// </summary>
		[Browsable(false)]
		public virtual IResource Resource
		{
			get
			{
				return _resource;
			}
		}

		/// <summary>
		/// Gets the ID of the resource.
		/// </summary>
		[Browsable(true)]
		[Category("Database")]
		[Description("Omea database resource identifier.")]
		public virtual int Id
		{
			get
			{
				return Resource.Id;
			}
		}

		/// <summary>
		/// Gets or sets whether all the write-operations to the resource properties should be asynchronous.
		/// </summary>
		[Browsable(false)]
		public virtual bool Async
		{
			get
			{
				return _async;
			}
			set
			{
				_async = value;
			}
		}

		/// <summary>
		/// Gets or sets the async operations priority (applies only when <see cref="Async"/> is <c>True</c>).
		/// </summary>
		[Browsable(false)]
		public virtual JobPriority AsyncPriority
		{
			get
			{
				return _priority;
			}
			set
			{
				_priority = value;
			}
		}

		/// <summary>
		/// Writes a property value to the resource wrapped by the object, respecting the <see cref="Async"/> trigger.
		/// </summary>
		public virtual void WriteProp(string sPropName, object value)
		{
			if(Async)
				new ResourceProxy(Resource, AsyncPriority).SetPropAsync(sPropName, value);
			else
				new ResourceProxy(Resource).SetProp(sPropName, value);
		}

		/// <summary>
		/// Writes a property value to the resource wrapped by the object, respecting the <see cref="Async"/> trigger.
		/// </summary>
		public virtual void WriteProp(int nPropId, object value)
		{
			if(Async)
				new ResourceProxy(Resource, AsyncPriority).SetPropAsync(nPropId, value);
			else
				new ResourceProxy(Resource).SetProp(nPropId, value);
		}

		///<summary>
		///Compares the current instance with another object of the same type.
		///</summary>
		///
		///<returns>
		///A 32-bit signed integer that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance is less than obj. Zero This instance is equal to obj. Greater than zero This instance is greater than obj.
		///</returns>
		///
		///<param name="obj">An object to compare with this instance. </param>
		///<exception cref="T:System.ArgumentException">obj is not the same type as this instance. </exception><filterpriority>2</filterpriority>
		public int CompareTo(object obj)
		{
			IResourceObject other = obj as IResourceObject;
			if(other == null)
				return -1;

			return Resource.Id.CompareTo(other.Resource.Id);
		}

		public override bool Equals(object obj)
		{
			IResourceObject other = obj as IResourceObject;
			if(other == null)
				return false;

			return Resource.Id.Equals(other.Resource.Id);
		}

		///<summary>
		///Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
		///</summary>
		///
		///<returns>
		///A hash code for the current <see cref="T:System.Object"></see>.
		///</returns>
		///<filterpriority>2</filterpriority>
		public override int GetHashCode()
		{
			return Resource.Id.GetHashCode();
		}

		/// <summary>
		/// Marks the resource as deleted.
		/// </summary>
		public virtual void Delete()
		{
			new ResourceProxy(Resource, JobPriority.BelowNormal).SetPropAsync(Core.Props.IsDeleted, true);
		}

		/// <summary>
		/// Unmarks the resource as deleted.
		/// </summary>
		public virtual void Undelete()
		{
			new ResourceProxy(Resource, JobPriority.BelowNormal).DeletePropAsync(Core.Props.IsDeleted);
		}

		public static bool operator ==(ResourceObject α, ResourceObject β)
		{
			if((ReferenceEquals(α, null)) && (ReferenceEquals(β, null)))
				return true;
			if((ReferenceEquals(α, null)) || (ReferenceEquals(β, null)))
				return false;
			return α.Resource == β.Resource;
		}

		public static bool operator !=(ResourceObject α, ResourceObject β)
		{
			if((ReferenceEquals(α, null)) && (ReferenceEquals(β, null)))
				return false;
			if((ReferenceEquals(α, null)) || (ReferenceEquals(β, null)))
				return true;
			return α.Resource != β.Resource;
		}
	}
}
