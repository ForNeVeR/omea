/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

namespace JetBrains.Omea.OpenApiEx
{
	/// <summary>
	/// Arguments that define a resource object.
	/// </summary>
	public class ResourceObjectEventArgs : EventArgs
	{
		protected readonly IResourceObject _resourceobject;

		public ResourceObjectEventArgs(IResourceObject resourceobject)
		{
			if(resourceobject == null)
				throw new ArgumentNullException("resourceobject");

			_resourceobject = resourceobject;
		}

		/// <summary>
		/// Gets the resource object. It is guaranteed not to be <c>Null</c>.
		/// </summary>
		public IResourceObject ResourceObject
		{
			get
			{
				return _resourceobject;
			}
		}
	}

	/// <summary>
	/// Arguments that accept a resource object from the handler.
	/// </summary>
	public class ResourceObjectOutByNameEventArgs<T> : EventArgs where T : class, IResourceObject
	{
		protected readonly string _name;

		protected T _resourceobject = null;

		public ResourceObjectOutByNameEventArgs(string name)
		{
			_name = name;
		}

		/// <summary>
		/// Gets the name of the resource object to be returned by the handler.
		/// </summary>
		public string Name
		{
			get
			{
				return _name;
			}
		}

		/// <summary>
		/// Gets or sets the resource object.
		/// May be <c>Null</c> if not yet set.
		/// Must be specified by the handler.
		/// </summary>
		public T ResourceObject
		{
			get
			{
				return _resourceobject;
			}
			set
			{
				_resourceobject = value;
			}
		}
	}
}