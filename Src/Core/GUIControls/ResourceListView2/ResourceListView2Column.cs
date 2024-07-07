// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
    public class ResourceListView2Column: ResourcePropsColumn
    {
        private PropertyToTextConverter[] _propToTextConverters;
        private IResourceList _ownerList;

        public ResourceListView2Column( int[] propIds )
            : base( propIds )
        {
        }

        public IResourceList OwnerList
        {
            get { return _ownerList; }
            set { _ownerList = value; }
        }

        public void SetPropToTextConverter( int propId, PropertyToTextConverter converter )
        {
            if ( converter != null )
            {
                if ( _propToTextConverters == null )
                {
                    _propToTextConverters = new PropertyToTextConverter[ _propIds.Length ];
                }
                for( int i=0; i<_propIds.Length; i++ )
                {
                    if ( _propIds [i] == propId )
                    {
                        _propToTextConverters [i] = converter;
                    }
                }
            }
        }

        protected override string GetItemText( object item )
        {
            return GetItemText( item, -1 );
        }

        protected override string GetItemText( object item, int width )
        {
            IResource res = (IResource) item;
            if ( Core.State == CoreState.ShuttingDown )
            {
                return "";
            }

            string result = null;
            if ( _propIds != null )
            {
                for( int i=0; i<_propIds.Length; i++ )
                {
                    int propID = _propIds [i];
                    if ( _propToTextConverters != null && _propToTextConverters [i] != null )
                    {
                        if ( res.HasProp( propID ) )
                        {
                            int widthInChars = 0;
                            if ( OwnerControl != null && width > 0 )
                            {
                                using( Graphics g = OwnerControl.CreateGraphics() )
                                {
                                    widthInChars = width / OwnerControl.ControlPainter.MeasureText( g, "a", OwnerControl.Font ).Width;
                                }
                            }

                            result = _propToTextConverters [i].GetPropertyText( res, propID, widthInChars );
                        }
                        else
                        {
                            result = null;
                        }
                    }
                    else if ( propID == ResourceProps.DisplayName )
                    {
                        result = res.DisplayName;
                    }
                    else if ( propID == ResourceProps.Type )
                    {
                        result = "";
                    }
                    else
                    {
                        if ( _ownerList != null )
                        {
                            if ( _ownerList.HasProp( res, propID ) )
                                result = _ownerList.GetPropText( res, propID );
                            else
                                result = null;
                        }
                        else
                        {
                            if ( res.HasProp( propID ) )
                                result = res.GetPropText( propID );
                            else
                                result = null;
                        }
                    }
                    if ( result != null )
                        break;
                }
            }
            if ( result != null )
            {
                if ( result.IndexOf( '\n' ) >= 0 )
                {
                    result = result.Replace( '\n', ' ' );
                }
                return result;

            }
            return String.Empty;
        }
    }

    /// <summary>
    /// Common wrapper for two versions of the PropertyToTextCallback delegate.
    /// </summary>
    public class PropertyToTextConverter
    {
        private PropertyToTextCallback _callback;
        private PropertyToTextCallback2 _callback2;

        public PropertyToTextConverter( PropertyToTextCallback callback )
        {
            _callback = callback;
        }

        public PropertyToTextConverter( PropertyToTextCallback2 callback2 )
        {
            _callback2 = callback2;
        }

        public string GetPropertyText( IResource res, int propId, int widthInChars )
        {
            if ( _callback != null )
            {
                return _callback( res, propId );
            }
            else
            {
                return _callback2( res, propId, widthInChars );
            }
        }
    }
}
