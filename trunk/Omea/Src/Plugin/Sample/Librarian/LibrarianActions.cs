/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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

