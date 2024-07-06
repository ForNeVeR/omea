// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
	/// Custom column "Annotation".
	/// </summary>
	internal class AnnotationColumn: ImageListColumn
	{
        private readonly ContextMenuStrip _annotationContextMenu;
        private readonly ContextMenuActionManager _annotationActionManager;

		public AnnotationColumn( int propId )
            : base( propId )
		{
            _annotationContextMenu = new ContextMenuStrip();
            _annotationActionManager = new ContextMenuActionManager( _annotationContextMenu );

            _annotationActionManager.RegisterGroup( "", null, ListAnchor.Last );
            _annotationActionManager.RegisterAction( new AnnotateResourceAction(), "", ListAnchor.Last, "Annotate Resource",
                                                     null, null, new IActionStateFilter[] { new InternalResourceFilter() } );
            _annotationActionManager.RegisterAction( new DeleteAnnotationAction(), "", ListAnchor.Last, "Delete Annotation",
                                                     null, null, new IActionStateFilter[] { new InternalResourceFilter() } );

            ShowTooltips = true;
            ResourceClicked += OnAnnotationClicked;
        }

	    public override void Draw( IResource res, Graphics g, Rectangle rc )
	    {
            if ( !Core.ResourceStore.ResourceTypes [res.Type].HasFlag( ResourceTypeFlags.Internal ) )
            {
                base.Draw( res, g, rc );
            }
	    }

	    public override void MouseClicked( IResource res, Point pt )
	    {
            if ( !Core.ResourceStore.ResourceTypes [res.Type].HasFlag( ResourceTypeFlags.Internal ) )
            {
                base.MouseClicked( res, pt );
            }
	    }

	    public override bool ShowContextMenu( IActionContext context, Control ownerControl, Point pt )
	    {
            _annotationActionManager.ActionContext = context;
            _annotationContextMenu.Show( ownerControl, pt );
            return true;
	    }

        private static void OnAnnotationClicked( object sender, ResourceEventArgs e )
        {
            Core.UIManager.QueueUIJob( new ResourceDelegate( Core.ResourceBrowser.EditAnnotation ), e.Resource );
        }
	}
}
