/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
	/// Provides multiline column schemes for views displaying Omea resources.
	/// </summary>
	internal class ResourceColumnSchemeProvider: IColumnSchemeProvider
    {
        private DisplayColumnManager _displayColumnManager;
        private ResourceListView2 _resourceListView;
	    private Hashtable _multiLineColumnSchemes = new Hashtable();
        private ColumnSchemeKey _key = new ColumnSchemeKey();

	    public ResourceColumnSchemeProvider( DisplayColumnManager displayColumnManager, 
            ResourceListView2 resourceListView )
	    {
	        _displayColumnManager = displayColumnManager;
	        _resourceListView = resourceListView;
	    }

	    public MultiLineColumnScheme GetColumnScheme( object item )
	    {
	        IResource res = (IResource) item;
            int hiddenColumnMask = BuildHiddenColumnMask( res );
            _key.Init( res.Type, hiddenColumnMask );
            MultiLineColumnScheme scheme = (MultiLineColumnScheme) _multiLineColumnSchemes[ _key ];
            if ( scheme == null )
            {
                scheme = BuildColumnScheme( res );
                _multiLineColumnSchemes[ _key.Clone() ] = scheme;
            }
            return scheme;
	    }

	    private MultiLineColumnScheme BuildColumnScheme( IResource res )
	    {
	        MultiLineColumnScheme scheme = new MultiLineColumnScheme();
            scheme.AlignTopLevelItems = _displayColumnManager.GetAlignTopLevelItems( res.Type );
            
            ArrayList allTypesColumnSchemes = _displayColumnManager.GetResourceColumnSchemes( "?" );
            ArrayList resourceColumnSchemes = _displayColumnManager.GetResourceColumnSchemes( res.Type );
            
            ConversationStructureColumn structureColumn = FindConversationColumn();
            if ( structureColumn != null )
            {
                structureColumn.Indent = 8;
                scheme.AddColumn( structureColumn, 0, 0, 0, 0, ColumnAnchor.Left, SystemColors.ControlText, 
                                  HorizontalAlignment.Left );
            }

            int allTypesIndent = 0;
            foreach( DisplayColumnManager.ResourceColumnScheme resScheme in allTypesColumnSchemes )
            {
                if ( resScheme.StartX >= 0 )
                {
                    JetListViewColumn col = FindColumn( resScheme.PropIds );
                    if ( col != null )
                    {
                        scheme.AddColumn( col, resScheme.StartRow, resScheme.EndRow, resScheme.StartX, resScheme.Width,
                            AnchorFromFlags( resScheme.Flags ), resScheme.TextColor, resScheme.TextAlign );
                        allTypesIndent = resScheme.StartX + resScheme.Width;
                    }
                }
            }

            ArrayList hiddenColumns = null;
            int maxWidth = 0;
            foreach( DisplayColumnManager.ResourceColumnScheme resScheme in resourceColumnSchemes )
            {
                if ( IsHiddenColumn( res, resScheme ) )
                {
                    if ( hiddenColumns == null )
                    {
                        hiddenColumns = new ArrayList();
                    }
                    hiddenColumns.Add( resScheme );
                }
                else
                {
                    int width = resScheme.StartX + allTypesIndent + resScheme.Width;
                    if ( width > maxWidth )
                    {
                        maxWidth = width;
                    }

                    JetListViewColumn col = FindColumn( resScheme.PropIds );
                    if ( col != null )
                    {
                        scheme.AddColumn( col, resScheme.StartRow, resScheme.EndRow, resScheme.StartX + allTypesIndent, resScheme.Width,
                            AnchorFromFlags( resScheme.Flags ), resScheme.TextColor, resScheme.TextAlign );
                    }
                }
            }

            if ( hiddenColumns != null )
            {
                foreach( DisplayColumnManager.ResourceColumnScheme resScheme in hiddenColumns )
                {
                    StretchColumnsToHidden( scheme, resScheme, allTypesIndent );
                }
            }

            foreach( DisplayColumnManager.ResourceColumnScheme resScheme in allTypesColumnSchemes )
            {
                if ( resScheme.StartX < 0 )
                {
                    JetListViewColumn col = FindColumn( resScheme.PropIds );
                    if ( col != null )
                    {
                        scheme.AddColumn( col, resScheme.StartRow, resScheme.EndRow, maxWidth + resScheme.StartX, resScheme.Width,
                            AnchorFromFlags( resScheme.Flags ), resScheme.TextColor, resScheme.TextAlign );
                    }
                }
            }

            return scheme;
	    }

        private int BuildHiddenColumnMask( IResource res )
        {
            int result = 0;
            ArrayList resourceColumnSchemes = _displayColumnManager.GetResourceColumnSchemes( res.Type );
            for( int i=0; i<resourceColumnSchemes.Count; i++ )
            {
                if ( IsHiddenColumn( res, (DisplayColumnManager.ResourceColumnScheme) resourceColumnSchemes [i] ) )
                {
                    result |= (1 << i);
                }
            }
            return result;
        }

        private JetListViewColumn FindColumn( int[] propIds )
	    {
	        foreach( JetListViewColumn col in _resourceListView.Columns )
	        {
	            ResourcePropsColumn propsCol = col as ResourcePropsColumn;
                if ( propsCol != null )
                {
                    for( int i=0; i<propIds.Length; i++ )
                    {
                        if ( Array.IndexOf( propsCol.PropIds, propIds [i] ) >= 0 )
                        {
                            return propsCol;
                        }
                    }
                }
	        }
            return null;
	    }

	    private ConversationStructureColumn FindConversationColumn()
	    {
            foreach( JetListViewColumn col in _resourceListView.Columns )
            {
                if ( col is ConversationStructureColumn )
                {
                    return col as ConversationStructureColumn;
                }
            }
            return null;
	    }

	    private ColumnAnchor AnchorFromFlags( MultiLineColumnFlags flags )
	    {
	        ColumnAnchor anchor = 0;
            if ( (flags & MultiLineColumnFlags.AnchorLeft ) != 0 )
            {
                anchor |= ColumnAnchor.Left;
            }
            if ( (flags & MultiLineColumnFlags.AnchorRight ) != 0 )
            {
                anchor |= ColumnAnchor.Right;
            }
            return anchor;
        }

	    private bool IsHiddenColumn( IResource res, DisplayColumnManager.ResourceColumnScheme scheme )
	    {
	        if ( ( scheme.Flags & MultiLineColumnFlags.HideIfNoProp ) != 0 )
	        {
	            for( int i=0; i<scheme.PropIds.Length; i++ )
	            {
                    int propId = scheme.PropIds [i];
                    if ( res.HasProp( propId ) )
                    {
                        return false;
                    }
	            }
                return true;
	        }
            return false;
	    }

	    private void StretchColumnsToHidden( MultiLineColumnScheme scheme, 
	                                         DisplayColumnManager.ResourceColumnScheme resScheme, int indent )
	    {
            foreach( MultiLineColumnSetting setting in scheme.ColumnSettings )
            {
                if ( setting.StartRow == resScheme.StartRow && setting.EndRow == resScheme.EndRow &&
                    setting.StartX < resScheme.StartX + indent )
                {
                    if ( ( setting.Anchor & ( ColumnAnchor.Left | ColumnAnchor.Right ) ) != 0 )
                    {
                        setting.Width += resScheme.Width;
                    }
                    else if ( ( setting.Anchor & ColumnAnchor.Right ) != 0 )
                    {
                        setting.StartX += resScheme.Width;                        
                    }
                }
            }
	    }

        private class ColumnSchemeKey : ICloneable
        {
            private string _resType;
            int _hiddenColumnMask;

            public void Init( string resType, int hiddenColumnMask )
            {
                _resType = resType;
                _hiddenColumnMask = hiddenColumnMask;
            }


            public override bool Equals( object obj )
            {
                ColumnSchemeKey rhs = (ColumnSchemeKey) obj;
                return _resType == rhs._resType && _hiddenColumnMask == rhs._hiddenColumnMask;
            }

            public override int GetHashCode()
            {
                return _resType.GetHashCode() + _hiddenColumnMask;
            }

            #region ICloneable Members

            public object Clone()
            {
                ColumnSchemeKey result = new ColumnSchemeKey();
                result.Init( _resType, _hiddenColumnMask );
                return result;
            }

            #endregion
        }
    }
}
