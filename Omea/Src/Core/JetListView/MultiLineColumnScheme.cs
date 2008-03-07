/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;

namespace JetBrains.JetListViewLibrary
{
    /// <summary>
    /// Specifies the possible modes of anchoring the column in a multiline column scheme.
    /// </summary>
    [Flags]
    public enum ColumnAnchor
    {
        /// <summary>
        /// The left edge of the column is anchored to the left side of the view area.
        /// </summary>
        Left = 1, 
        
        /// <summary>
        /// The right edge of the column is anchored to the right side of the view area.
        /// </summary>
        Right = 2
    }
    
    /// <summary>
    /// Describes the layout of a single column in a multiline column scheme.
    /// </summary>
    public class MultiLineColumnSetting
    {
        private JetListViewColumn _column;
        private readonly int _startRow;
        private readonly int _endRow;
        private int _startX;
        private int _width;
        private readonly ColumnAnchor _anchor;
        private readonly Color _textColor;
        private HorizontalAlignment _textAlign;

        public MultiLineColumnSetting( JetListViewColumn column, int startRow, int endRow,
            int startX, int width, ColumnAnchor anchor, Color textColor, HorizontalAlignment textAlign )
        {
            _column    = column;
            _startRow  = startRow;
            _endRow    = endRow;
            _startX    = startX;
            _width     = width;
            _anchor    = anchor;
            _textColor = textColor;
            _textAlign = textAlign;
        }

        public MultiLineColumnSetting( MultiLineColumnSetting rhs )
        {
            _column    = rhs._column;
            _startRow  = rhs._startRow;
            _endRow    = rhs._endRow;
            _startX    = rhs._startX;
            _width     = rhs._width;
            _anchor    = rhs._anchor;
            _textColor = rhs._textColor;
            _textAlign = rhs._textAlign;
        }

        public JetListViewColumn Column
        {
            get { return _column; }
        }

        public int StartRow
        {
            get { return _startRow; }
        }

        public int EndRow
        {
            get { return _endRow; }
        }

        public int StartX
        {
            get { return _startX; }
            set { _startX = value; }
        }

        public int Width
        {
            get { return _width; }
            set { _width = value; }
        }

        public ColumnAnchor Anchor
        {
            get { return _anchor; }
        }

        public Color TextColor
        {
            get { return _textColor; }
        }

        public HorizontalAlignment TextAlign
        {
            get { return _textAlign; }
        }
    }

    /// <summary>
	/// Describes the layout of columns for a single item in a multi-column layout.
	/// </summary>
	public class MultiLineColumnScheme
	{
        private ArrayList _columnSettings;
        private ArrayList _columns;
        private bool _alignTopLevelItems = false;

        public MultiLineColumnScheme()
        {
            _columnSettings = new ArrayList();
            _columns = new ArrayList();
        }

        public MultiLineColumnScheme( MultiLineColumnScheme rhs )
        {
            _columnSettings = new ArrayList( rhs._columnSettings.Count );
            foreach( MultiLineColumnSetting setting in rhs._columnSettings )
            {
                _columnSettings.Add( new MultiLineColumnSetting( setting ) );
            }
            _columns = new ArrayList( rhs._columns );
        }

        public IEnumerable ColumnSettings
        {
            get { return _columnSettings; }
        }

        public ICollection Columns
        {
            get { return _columns; }
        }

        /// <summary>
        /// Adds a column to the multiline column scheme.
        /// </summary>
        /// <param name="column">The column to add.</param>
        /// <param name="startRow">The row in which the column rectangle starts.</param>
        /// <param name="endRow">The row in which the column rectangle ends.</param>
        /// <param name="startX">The X position where the column data starts.</param>
        /// <param name="width">The width of the column in pixels.</param>
        /// <param name="anchor">The anchoring of the columnn to the edges of the view area.</param>
        /// <param name="textColor">The color of the text displayed in the column.</param>
        public void AddColumn( JetListViewColumn column, int startRow, int endRow, int startX, int width,
            ColumnAnchor anchor, Color textColor, HorizontalAlignment textAlign )
        {
            _columns.Add( column );
            _columnSettings.Add( new MultiLineColumnSetting( column, startRow, endRow, startX, width, anchor, 
                textColor, textAlign ) );
        }

        /// <summary>
        /// Returns the base width of the column scheme.
        /// </summary>
        internal int BaseWidth
        {
            get
            {
                int maxY = 0;
                for( int i = 0; i < _columnSettings.Count; ++i )
                {
                    MultiLineColumnSetting setting = (MultiLineColumnSetting) _columnSettings[ i ];
                    int lastY = setting.StartX + setting.Width;
                    if ( lastY > maxY )
                    {
                        maxY = lastY;
                    }
                }
                return maxY;
            }
        }
        
        /// <summary>
        /// Returns the number of rows in the column scheme.
        /// </summary>
        public int RowCount
        {
            get
            {
                int endRow = 0;
                for( int i = 0; i < _columnSettings.Count; ++i )
                {
                    MultiLineColumnSetting setting = (MultiLineColumnSetting) _columnSettings[ i ];
                    if ( setting.EndRow > endRow )
                    {
                        endRow = setting.EndRow;
                    }
                }
                return endRow+1;
            }
        }

        /// <summary>
        /// Gets or sets the value indicating whether top-level items in a thread
        /// are aligned even if items have no children.
        /// </summary>
        public bool AlignTopLevelItems
        {
            get { return _alignTopLevelItems; }
            set { _alignTopLevelItems = value; }
        }
	}

    /// <summary>
    /// Allows to return the column scheme used for the specified item.
    /// </summary>
    public interface IColumnSchemeProvider
    {
        MultiLineColumnScheme GetColumnScheme( object item );
    }
}
