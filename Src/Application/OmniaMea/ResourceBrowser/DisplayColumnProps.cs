// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{

    /// <summary>
    /// Resource types and properties used for storing the display column settings.
    /// </summary>
    internal class DisplayColumnProps
    {
        internal const string ColumnSchemeResource = "ColumnScheme";
        internal const string ColumnDescriptorResource = "ColumnDescriptor";

        private int _propColumnProps;
        private int _propColumnSortProps;
        private int _propColumnDescriptor;
        private int _propColumnOrder;
        private int _propColumnWidth;
        private int _propColumnFlags;
        private int _propColumnSortAsc;
        private int _propColumnKeyTypes;
        private int _propColumnSchemeOwner;
        private int _propColumnSchemeTab;
        private int _propGroupItems;

        internal int ColumnProps { get { return _propColumnProps; } }
        internal int ColumnSortProps { get { return _propColumnSortProps; } }
        internal int ColumnDescriptor { get { return _propColumnDescriptor; } }
        internal int ColumnOrder { get { return _propColumnOrder; } }
        internal int ColumnWidth { get { return _propColumnWidth; } }
        internal int ColumnFlags { get { return _propColumnFlags; } }
        internal int ColumnSortAsc { get { return _propColumnSortAsc; } }
        internal int ColumnKeyTypes { get { return _propColumnKeyTypes; } }
        internal int ColumnSchemeOwner { get { return _propColumnSchemeOwner; } }
        internal int ColumnSchemeTab { get { return _propColumnSchemeTab; } }
        internal int GroupItems { get { return _propGroupItems; } }

        internal DisplayColumnProps( IResourceStore store )
        {
            _propColumnProps = store.PropTypes.Register( "ColumnProps", PropDataType.StringList,
                PropTypeFlags.Internal );
            _propColumnSortProps = store.PropTypes.Register( "ColumnSortProps", PropDataType.StringList,
                PropTypeFlags.Internal );
            _propColumnDescriptor = store.PropTypes.Register( "ColumnDescriptor", PropDataType.Link,
                PropTypeFlags.Internal );
            _propColumnOrder = store.PropTypes.Register( "ColumnOrder", PropDataType.Int,
                PropTypeFlags.Internal );
            _propColumnWidth = store.PropTypes.Register( "ColumnWidth", PropDataType.Int,
                PropTypeFlags.Internal );
            _propColumnFlags = store.PropTypes.Register( "ColumnFlags", PropDataType.Int,
                PropTypeFlags.Internal );
            _propColumnSortAsc = store.PropTypes.Register( "ColumnSortAsc", PropDataType.Bool,
                PropTypeFlags.Internal );
            _propColumnKeyTypes = store.PropTypes.Register( "ColumnKeyTypes", PropDataType.String,
                PropTypeFlags.Internal );
            _propColumnSchemeOwner = store.PropTypes.Register( "ColumnSchemeOwner", PropDataType.Link,
                PropTypeFlags.Internal );
            _propColumnSchemeTab = store.PropTypes.Register( "ColumnSchemeTab", PropDataType.String,
                PropTypeFlags.Internal );
            _propGroupItems = store.PropTypes.Register( "GroupItems", PropDataType.Bool,
                PropTypeFlags.Internal );

            store.ResourceTypes.Register( ColumnSchemeResource, "", "",
                ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal );
            store.ResourceTypes.Register( ColumnDescriptorResource, "", "",
                ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal );

            store.RegisterUniqueRestriction( ColumnSchemeResource, _propColumnKeyTypes );
        }
    }
}
