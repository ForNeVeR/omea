// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
