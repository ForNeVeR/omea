// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceTools
{
	/// <summary>
	/// Default implementation of the IResourceDeleter interface.
	/// </summary>
	public class DefaultResourceDeleter: IResourceDeleter
	{
	    public virtual DialogResult ConfirmDeleteResources( IResourceList resources, bool permanent, bool showCancel )
	    {
	        string message;
            if ( permanent )
            {
                message = "Are you sure you wish to permanently delete ";
            }
            else
            {
                message = "Are you sure you wish to delete ";
            }
            if ( resources.Count == 1 )
            {
                message += "'" + resources [0].DisplayName + "'?";
            }
            else
            {
                message += resources.Count + " " + Core.ResourceStore.ResourceTypes [resources [0].Type].DisplayName + "s?";
            }

            return MessageBox.Show( Core.MainWindow, message, "Delete Resources",
                showCancel ? MessageBoxButtons.YesNoCancel : MessageBoxButtons.YesNo, MessageBoxIcon.Question );
	    }

	    public virtual bool CanDeleteResource( IResource res, bool permanent )
        {
            return true;
        }

        public virtual bool CanIgnoreRecyclebin()
        {
            return true;
        }

        public virtual void DeleteResource( IResource res )
	    {
	        if ( res.IsDeleted )
	        {
	            return;
	        }

            if ( res.HasProp( Core.Props.IsDeleted ) )
	        {
	            DeleteResourcePermanent( res );
	        }
            else
	        {
                new ResourceProxy(res).SetProp(Core.Props.IsDeleted, true);
                new ResourceProxy(res).SetProp(Core.Props.DeleteDate, DateTime.Now);
            }
	    }

	    public virtual void DeleteResourcePermanent( IResource res )
	    {
            if ( res.IsDeleted )
            {
                return;
            }

            new ResourceProxy( res ).Delete();
	    }

	    public virtual void UndeleteResource( IResource res )
	    {
	        new ResourceProxy( res ).DeleteProp( Core.Props.IsDeleted );
            new ResourceProxy( res ).DeleteProp( Core.Props.DeleteDate );
        }
	}
}
