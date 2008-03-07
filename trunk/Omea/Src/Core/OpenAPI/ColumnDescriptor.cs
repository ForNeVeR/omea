/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Specifies possible flags for a column.
    /// </summary>
    [Flags]
    public enum ColumnDescriptorFlags
    {
        /// <summary>
        /// No special flags.
        /// </summary>
        None = 0, 
        
        /// <summary>
        /// The column cannot be resized by the user.
        /// </summary>
        FixedSize = 1,

        /// <summary>
        /// The column is shown only if at least one resource in the displayed resource list
        /// has a value in the column.
        /// </summary>
        /// <since>2.0</since>
        ShowIfNotEmpty = 2,

        /// <summary>
        /// The column is shown only if the resources in the displayed resource list have
        /// different values for the column.
        /// </summary>
        /// <since>2.0</since>
        ShowIfDistinct = 4,

        /// <summary>
        /// The column is resized so that the available space in the resource list is divided
        /// between the autosize columns and no scroll bars appear.
        /// </summary>
        /// <since>2.0</since>
        AutoSize = 8
    }

    /// <summary>
    /// The descriptor for a single column which can be displayed in the resource browser.
    /// Contains the property names, width, flags and a custom comparer for the column.
    /// </summary>
    public struct ColumnDescriptor
    {
        /// <summary>
        /// The names of properties displayed in the column.
        /// </summary>
        public string[] PropNames;
        
        /// <summary>
        /// The width of the column in pixels.
        /// </summary>
        public int Width;
        
        /// <summary>
        /// The flags of the column.
        /// </summary>
        public ColumnDescriptorFlags Flags;
        
        /// <summary>
        /// The custom comparer used for sorting by the column.
        /// </summary>
        public IResourceComparer CustomComparer;

        /// <summary>
        /// The group provider used for grouping by the column.
        /// </summary>
        /// <since>2.0</since>
        public IResourceGroupProvider GroupProvider;

        /// <summary>
        /// The text which is displayed in the sort direction header when the multiline
        /// view list is sorted by this column in ascending order.
        /// </summary>
        /// <since>2.0</since>
        public string SortMenuAscText;

        /// <summary>
        /// The text which is displayed in the sort direction header when the multiline
        /// view list is sorted by this column in descending order.
        /// </summary>
        /// <since>2.0</since>
        public string SortMenuDescText;

        /// <summary>
        /// Creates a column descriptor for displaying a single property with default flags.
        /// </summary>
        /// <param name="propName">The name of the property displayed in the column.</param>
        /// <param name="width">The width of the column in pixels.</param>
        public ColumnDescriptor( string propName, int width )
            : this( propName, width, ColumnDescriptorFlags.None ) {}

        /// <summary>
        /// Creates a column descriptor for displaying multiple properties with default flags.
        /// </summary>
        /// <param name="propNames">The names of the properties displayed in the column.</param>
        /// <param name="width">The width of the column in pixels.</param>
        public ColumnDescriptor( string[] propNames, int width )
            : this( propNames, width, ColumnDescriptorFlags.None ) {}

        /// <summary>
        /// Creates a column descriptor for displaying a single property with specified flags.
        /// </summary>
        /// <param name="propName">The name of the property displayed in the column.</param>
        /// <param name="width">The width of the column in pixels.</param>
        /// <param name="flags">The flags of the column.</param>
        public ColumnDescriptor( string propName, int width, ColumnDescriptorFlags flags )
        {
            PropNames = new string[] { propName };
            Width = width;
            Flags = flags;
            CustomComparer = null;
            GroupProvider = null;
            SortMenuAscText = null;
            SortMenuDescText = null;
        }

        /// <summary>
        /// Creates a column descriptor for displaying multiple properties with default flags.
        /// </summary>
        /// <param name="propNames">The names of the properties displayed in the column.</param>
        /// <param name="width">The width of the column in pixels.</param>
        /// <param name="flags">The flags of the column.</param>
        public ColumnDescriptor( string[] propNames, int width, ColumnDescriptorFlags flags )
        {
            PropNames = propNames;
            Width     = width;
            Flags     = flags;
            CustomComparer = null;
            GroupProvider = null;
            SortMenuAscText = null;
            SortMenuDescText = null;
        }

        public override string ToString()
        {
            string result = String.Join( "|", PropNames ) + ":" + Width;
            if ( ( Flags & ColumnDescriptorFlags.FixedSize ) != 0 )
            {
                result += "F";
            }
            return result;
        }

        public override bool Equals( object obj )
        {
            if ( obj == null || !(obj is ColumnDescriptor) )
                return false;

            ColumnDescriptor rhs = (ColumnDescriptor) obj;
            if ( !EqualsIgnoreWidth( rhs ) )
                return false;

            if ( Width != rhs.Width )
                return false;

            return true;
        }

        public bool EqualsIgnoreWidth( ColumnDescriptor rhs )
        {
            if ( !PropNamesEqual( rhs ) )
                return false;
            if ( Flags != rhs.Flags )
                return false;

            return true;
        }

        public bool PropNamesEqual( ColumnDescriptor rhs )
        {
            if ( rhs.PropNames.Length != PropNames.Length )
                return false;

            for( int i=0; i<PropNames.Length; i++ )
            {
                if ( PropNames [i] != rhs.PropNames [i] )
                    return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            int hash = Width | ((int) Flags << 16);
            for( int i=0; i<PropNames.Length; i++ )
            {
                hash ^= PropNames [i].GetHashCode();
            }
            return hash;
        }
    }
}
