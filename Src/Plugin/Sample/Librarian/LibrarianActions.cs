// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.Librarian
{
	/// <summary>
	/// Action for creating a new book.
	/// </summary>
	public class NewBookAction: SimpleAction
	{
	    public override void Execute( IActionContext context )
	    {
	        IResource res = Core.ResourceStore.NewResourceTransient( ResourceTypes.Book );
            Core.UIManager.OpenResourceEditWindow( new BookEditPane(), res, true );
	    }
	}

    public class EditBookAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            Core.UIManager.OpenResourceEditWindow( new BookEditPane(),
                context.SelectedResources [0], false );
        }
    }

    public class DeleteBookAction: ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            foreach( IResource res in context.SelectedResources )
            {
                new ResourceProxy( res ).DeleteAsync();
            }
        }
    }
}

