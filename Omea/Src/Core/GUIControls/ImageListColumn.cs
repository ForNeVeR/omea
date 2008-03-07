/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.OpenAPI;
using System.Windows.Forms;
using System.Drawing;
using JetBrains.DataStructures;

namespace JetBrains.Omea.GUIControls
{
	/**
     * ResourceListView custom column which can draw icons from an imagelist.
     */

    public class ImageListColumn: ICustomColumn
	{
        private int          _propID;
        private ImageList    _imageList;
        private IntHashTable _valuesForIcons;
        private int          _headerIconIndex = -1;
        private int          _anyValueIconIndex = -1;
        private int          _noValueIconIndex = -1;
        private bool         _showTooltips;

        public event ResourceEventHandler ResourceClicked;

	    public ImageListColumn( int propID )
	    {
            _propID = propID;
            _imageList = new ImageList();
            _imageList.ColorDepth = ICore.Instance.ResourceIconManager.IconColorDepth;
            _valuesForIcons = new IntHashTable();
	    }

        public ImageList ImageList { get { return _imageList; } }

        public bool ShowTooltips
        {
            get { return _showTooltips; }
            set { _showTooltips = value; }
        }

	    public void AddIconValue( Icon icon, object propValue )
        {
            int iconIndex = _imageList.Images.Count;
            _imageList.Images.Add( icon );
            _valuesForIcons [iconIndex] = propValue;
        }

        public void SetHeaderIcon( Icon icon )
        {
            _headerIconIndex = AddIcon( icon );
        }

        public void SetAnyValueIcon( Icon icon )
        {
            _anyValueIconIndex = AddIcon( icon );
        }

        public void SetNoValueIcon( Icon icon )
        {
            _noValueIconIndex = AddIcon( icon );
        }

        private int AddIcon( Icon icon )
        {
            int iconIndex = _imageList.Images.Count;
            _imageList.Images.Add( icon );
            return iconIndex;
        }

        public virtual void Draw( IResource res, Graphics g, Rectangle rc )
        {
            int x = rc.Left + (rc.Width - _imageList.ImageSize.Width) / 2;
            
            if ( !res.HasProp( _propID ) )
            {
                if ( _noValueIconIndex != -1 )
                {
                    _imageList.Draw( g, x, rc.Top, _noValueIconIndex );
                }
            }
            else
            {
                if ( _anyValueIconIndex != -1 )
                {
                    _imageList.Draw( g, x, rc.Top, _anyValueIconIndex );
                }
                else
                {
                    object propValue = res.GetProp( _propID );
                    for( int i=0; i<_imageList.Images.Count; i++ )
                    {
                        if ( _valuesForIcons [i] == null )
                            continue;

                        if ( Object.Equals( propValue, _valuesForIcons [i] ) )
                        {
                            _imageList.Draw( g, x, rc.Top, i );
                        }
                    }
                }
            }
        }

        public void DrawHeader( Graphics g, Rectangle rc )
        {
            if ( _headerIconIndex >= 0 )
            {
                int x = rc.Left + (rc.Width - _imageList.ImageSize.Width) / 2;
                _imageList.Draw( g, x, rc.Top, _headerIconIndex );
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
