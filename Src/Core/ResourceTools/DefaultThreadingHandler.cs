﻿// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceTools
{
	/// <summary>
	/// Default implementation of a threading handler because of a reply property.
	/// </summary>
	public class DefaultThreadingHandler: IResourceThreadingHandler
	{
        private readonly int _threadReplyProp;

	    public DefaultThreadingHandler( int threadReplyProp )
	    {
	        _threadReplyProp = threadReplyProp;
	    }

	    public virtual IResource GetThreadParent( IResource res )
	    {
	        return res.GetLinkProp( _threadReplyProp );
	    }

	    public virtual IResourceList GetThreadChildren( IResource res )
	    {
	        return res.GetLinksTo( null, _threadReplyProp );
	    }

	    public virtual bool IsThreadChanged( IResource res, IPropertyChangeSet changeSet )
	    {
            return changeSet.IsPropertyChanged( _threadReplyProp );
        }

	    public virtual bool CanExpandThread( IResource res, ThreadExpandReason reason )
	    {
	        return res.HasProp( -_threadReplyProp );
	    }

	    public virtual bool HandleThreadExpand( IResource res, ThreadExpandReason reason )
	    {
            return true;
	    }
	}
}
