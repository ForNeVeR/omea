/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/**
     * ResourceListView custom column which can draw icons from an imagelist.
     */

    public class LinkIconManagerColumn: ICustomColumn
	{
        private int          _propID;
        private ImageList    _imageList, _imageHeader;
        private bool         _showTooltips;

        public event ResourceEventHandler ResourceClicked;

	    public LinkIconManagerColumn( int propID )
	    {
            _propID = propID;
            _imageHeader = new ImageList();
            _imageList = Core.ResourceIconManager.ImageList;
	    }

        public ImageList ImageList { get { return _imageList; } }

        public bool ShowTooltips
        {
            get { return _showTooltips; }
            set { _showTooltips = value; }
        }

        public void SetHeaderIcon( Icon icon )
        {
            _imageHeader.Images.Add( icon );
        }

        public virtual void Draw( IResource res, Graphics g, Rectangle rc )
        {
            int x = rc.Left + (rc.Width - _imageList.ImageSize.Width) / 2;
            
            if ( res.HasProp( _propID ) )
            {
                IResource linked = res.GetLinkProp( _propID );
                if( linked != null )
                {
                    int ind = Core.ResourceIconManager.GetIconIndex( linked );
                    _imageList.Draw( g, x, rc.Top, ind );
                }
            }
        }

        public void DrawHeader( Graphics g, Rectangle rc )
        {
            if ( _imageHeader.Images.Count >= 0 )
            {
                int x = rc.Left + (rc.Width - _imageHeader.ImageSize.Width) / 2;
                _imageHeader.Draw( g, x, rc.Top, 0 );
            }
        }

        public virtual void MouseClicked( IResource res, Point pt )
        {
            if ( ResourceClicked != null )
            {
                ResourceClicked( this, new ResourceEventArgs( res ) );
            }
        }

        public virtual string GetTooltip( IResource res )
        {
            if ( _showTooltips )
            {
                return res.GetPropText( _propID );
            }

            return null;
        }

        public virtual bool ShowContextMenu( IActionContext context, Control ownerControl, Point pt )
        {
            return false;        	
        }
	}

}