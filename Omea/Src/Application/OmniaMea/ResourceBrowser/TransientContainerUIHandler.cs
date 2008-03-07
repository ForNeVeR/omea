/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
	/// Dummy IResourceUIHandler implementation for the TransientContainer resource.
	/// </summary>
	internal class TransientContainerUIHandler: IResourceUIHandler
	{
	    public void ResourceNodeSelected( IResource res )
	    {
	    }

	    public bool CanRenameResource( IResource res )
	    {
            return false;
	    }

	    public bool ResourceRenamed( IResource res, string newName )
	    {
            return false;
	    }

	    public bool CanDropResources( IResource targetResource, IResourceList dragResources )
	    {
	        return false;
	    }

	    public void ResourcesDropped( IResource targetResource, IResourceList droppedResources )
	    {
	    }
	}
}
