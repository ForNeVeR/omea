/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using JetBrains.JetListViewLibrary;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.DataStructures;

namespace JetBrains.Omea
{
    /**
     * Manages the columns displayed in the ResourceBrowser.
     */
    
    internal class DisplayColumnManager: IDisplayColumnManager, IDisplayColumnManagerEx
    {
        internal class DisplayColumn
        {
            private int              _index;
            private ColumnDescriptor _descriptor;

            public DisplayColumn( int index, ColumnDescriptor descriptor )
            {
                _index      = index;
                _descriptor = descriptor;
            }

            internal DisplayColumn MergePropNames( DisplayColumn other )
            {
                ArrayList myPropNames = new ArrayList( _descriptor.PropNames );
                string[] otherPropNames = other._descriptor.PropNames;
                ArrayList mergedPropNames = null;
                foreach( string propName in otherPropNames )
                {
                    if ( !myPropNames.Contains( propName ) )
                    {
                        if ( mergedPropNames == null )
                        {
                            mergedPropNames = (ArrayList) myPropNames.Clone();
                        }
                        mergedPropNames.Add( propName );
                    }
                }
                if ( mergedPropNames == null )
                {
                    return this;
                }
                return new DisplayColumn( _index, new ColumnDescriptor(
                    (string[]) mergedPropNames.ToArray( typeof(String) ), _descriptor.Width ) );
            }

            public int      Index              { get { return _index; } }
            public string[] PropNames          { get { return _descriptor.PropNames; } }
            public ColumnDescriptor Descriptor { get { return _descriptor; } }
        }

        internal class ResourceColumnScheme
        {
            private readonly int[] _propIds;
            private readonly int _startRow;
            private readonly int _endRow;
            private readonly int _startX;
            private readonly int _width;
            private readonly MultiLineColumnFlags _flags;
            private readonly Color _textColor;
            private readonly HorizontalAlignment _textAlign;

            public ResourceColumnScheme( int[] propIds, int startRow, int endRow, int startX, int width, 
                MultiLineColumnFlags flags, Color textColor, HorizontalAlignment textAlign )
            {
                _propIds   = propIds;
                _startRow  = startRow;
                _endRow    = endRow;
                _startX    = startX;
                _width     = width;
                _flags     = flags;
                _textColor = textColor;
                _textAlign = textAlign;
            }

            public int[] PropIds
            {
                get { return _propIds; }
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
            }

            public int Width
            {
                get { return _width; }
            }

            public MultiLineColumnFlags Flags
            {
                get { return _flags; }
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

        private HashMap _displayColumns = new HashMap();
        private HashMap _availableColumns = new HashMap();
        private HashMap _columnState = new HashMap();       // string[] -> ResourceListState
        private ArrayList _allTypeColumns = new ArrayList();
        private IntHashTable _customColumns = new IntHashTable();
        private IntHashTable _propToTextConverters = new IntHashTable();  // int -> PropertyToTextConverter
        private DisplayColumnProps _props;
        private Hashtable _columnSchemeMap = new Hashtable();  // resource type -> ArrayList<ResourceColumnScheme>
        private HashSet _alignTopLevelItemsTypes = new HashSet();  // <resource type>

        public DisplayColumnManager()
        {
            if ( Core.ResourceStore.IsOwnerThread() )
            {
                RegisterProps();
            }
            else
            {
                Core.ResourceAP.RunJob( new MethodInvoker( RegisterProps ) );
            }

            RegisterAllTypesMultiLineColumn( ResourceProps.Type, 0, 0, 0, 24, 
                MultiLineColumnFlags.AnchorLeft, SystemColors.ControlText, HorizontalAlignment.Left );

            RegisterDefaultMultiLineColumn( ResourceProps.DisplayName, 0, 0, 0, 120, 
                MultiLineColumnFlags.AnchorLeft | MultiLineColumnFlags.AnchorRight, SystemColors.ControlText, HorizontalAlignment.Left );
            RegisterDefaultMultiLineColumn( Core.Props.Date, 0, 0, 120, 80,
                MultiLineColumnFlags.AnchorRight, SystemColors.ControlText, HorizontalAlignment.Right );
        }

        private void RegisterProps()
        {
            _props = new DisplayColumnProps( Core.ResourceStore );
        }

        public DisplayColumnProps Props
        {
            get { return _props; }
        }

        /**
         * Registers a column which is used for displaying the resources of
         * the specified type.
         */

        public void RegisterDisplayColumn( string resourceType, int index, ColumnDescriptor descriptor )
        {
            if ( resourceType != null && !Core.ResourceStore.ResourceTypes.Exist( resourceType ) )
            {
                throw new ArgumentException( "Invalid resource type " + resourceType, "resourceType" );
            }
            PropNamesToIDs( descriptor.PropNames, false );  // validates the column descriptor

            DisplayColumn col = new DisplayColumn( index, descriptor );
            if ( resourceType == null )
            {
                _allTypeColumns.Add( col );
            }
            else
            {
                AddColumnToList( _displayColumns, resourceType, col );
            }
        }

        /**
         * Registers a column that is not by default included in the list of
         * visible columns for the specified resource type, but can always be
         * added to the list (even if none of the resources in the list have that
         * property).
         */

        public void RegisterAvailableColumn( string resourceType, ColumnDescriptor descriptor )
        {
            if ( resourceType != null && !Core.ResourceStore.ResourceTypes.Exist( resourceType ) )
            {
                throw new ArgumentException( "Invalid resource type " + resourceType, "resourceType" );
            }
            PropNamesToIDs( descriptor.PropNames, false );  // validates the column descriptor

            DisplayColumn col = new DisplayColumn( -1, descriptor );
            if ( resourceType == null )
            {
                AddColumnToList( _availableColumns, "", col );
            }
            else
            {
                AddColumnToList( _availableColumns, resourceType, col );
            }
        }

        public void RemoveAvailableColumn( string resourceType, string propNames )
        {
            string key = (resourceType == null) ? "" : resourceType;
            ArrayList columns = (ArrayList) _availableColumns [key];
            if ( columns != null )
            {
                for( int i=0; i<columns.Count; i++ )
                {
                    DisplayColumn col = (DisplayColumn) columns [i];
                    if ( col.Descriptor.PropNames [0] == propNames )
                    {
                        columns.RemoveAt( i );
                        break;
                    }
                }
            }
        }
        
        /**
         * Register a custom draw handler for a property column.
         */

        public void RegisterCustomColumn( int propId, ICustomColumn customColumn )
        {
            _customColumns [propId] = customColumn;
        }

        /**
         * Registers a custom property to string converter for a property column.
         */

        public void RegisterPropertyToTextCallback( int propId, PropertyToTextCallback propToText )
        {
            _propToTextConverters [propId] = new PropertyToTextConverter( propToText );
        }

        public void RegisterPropertyToTextCallback( int propId, PropertyToTextCallback2 propToText )
        {
            _propToTextConverters [propId] = new PropertyToTextConverter( propToText );
        }

        public void RegisterAllTypesMultiLineColumn( int propId, int startRow, int endRow, 
            int startX, int width, MultiLineColumnFlags flags, Color textColor, HorizontalAlignment textAlign )
        {
            RegisterMultiLineColumn( "?", new int[] { propId }, startRow, endRow, startX, width, flags, 
                textColor, textAlign );
        }

        public void RegisterDefaultMultiLineColumn( int propId, int startRow, int endRow, 
            int startX, int width, MultiLineColumnFlags flags, Color textColor, HorizontalAlignment textAlign )
        {
            RegisterMultiLineColumn( "*", new int[] { propId }, startRow, endRow, startX, width, flags, 
                textColor, textAlign );
        }

        public void RegisterDefaultMultiLineColumn( int[] propIds, int startRow, int endRow, 
            int startX, int width, MultiLineColumnFlags flags, Color textColor, HorizontalAlignment textAlign )
        {
            RegisterMultiLineColumn( "*", propIds, startRow, endRow, startX, width, flags, 
                textColor, textAlign );
        }

        public void RegisterMultiLineColumn( string resourceType, int propId, int startRow, int endRow, 
            int startX, int width, MultiLineColumnFlags flags, Color textColor, HorizontalAlignment textAlign )
        {
            RegisterMultiLineColumn( resourceType, new int[] { propId }, startRow, endRow,
                startX, width, flags, textColor, textAlign );
        }

        public void RegisterMultiLineColumn( string resourceType, int[] propIds, int startRow, int endRow, 
            int startX, int width, MultiLineColumnFlags flags, Color textColor, HorizontalAlignment textAlign )
        {
            ArrayList list = (ArrayList) _columnSchemeMap [resourceType];
            if ( list == null )
            {
                list = new ArrayList();
                _columnSchemeMap [resourceType] = list;
            }
            list.Add( new ResourceColumnScheme( propIds, startRow, endRow, startX, width, flags, textColor, textAlign ) );
        }

        public void SetAlignTopLevelItems( string resourceType, bool align )
        {
            if ( align )
            {
                _alignTopLevelItemsTypes.Add( resourceType );
            }
            else
            {
                _alignTopLevelItemsTypes.Remove( resourceType );
            }
        }

        internal bool GetAlignTopLevelItems( string resourceType )
        {
            return _alignTopLevelItemsTypes.Contains( resourceType );
        }

        internal ArrayList GetResourceColumnSchemes( string resourceType )
        {
            ArrayList result = (ArrayList) _columnSchemeMap [resourceType];
            if ( result == null )
            {
                result = (ArrayList) _columnSchemeMap ["*"];
            }
            return result;
        }

        /**
         * Adds a column to the specified map of columns.
         */

        private void AddColumnToList( HashMap map, string key, DisplayColumn column )
        {
            ArrayList columns = (ArrayList) map [key];
            if ( columns == null )
            {
                columns = new ArrayList();
                map [key] = columns;
            }
            int pos = 0;
            while ( pos < columns.Count && ((DisplayColumn) columns [pos]).Index < column.Index )
                pos++;
            columns.Insert( pos, column );
        }
        
        /**
         * Returns the list of available columns for the specified resource list.
         */

        public IntArrayList GetAvailableColumns( IResourceList resList )
        {
            IntArrayList result = new IntArrayList();
            GetAvailableColumnsFromList( result, (ArrayList) _availableColumns [""] );
            foreach( string type in resList.GetAllTypes() )
            {
                GetAvailableColumnsFromList( result, (ArrayList) _availableColumns [type] );
            }
            GetAvailableColumnsFromList( result, _allTypeColumns );
            return result;
        }

        /**
         * Adds the property IDs of the available columns from the specified list to the
         * specified IntArrayList.
         */

        private void GetAvailableColumnsFromList( IntArrayList outList, ArrayList columnList )
        {
            if ( columnList == null )
                return;

            foreach( DisplayColumn col in columnList )
            {
                int[] propIDs = PropNamesToIDs( col.PropNames, true );
                for( int i=0; i<propIDs.Length; i++ )
                {
                    if ( outList.IndexOf( propIDs [i] ) < 0 )
                    {
                        outList.Add( propIDs [i] );
                    }
                }
            }
        }

        /**
         * Parses the specified property name string into an array of property IDs.
         */

        public int[] PropNamesToIDs( string[] propNames, bool ignoreErrors )
        {
            IntArrayList propIDs = IntArrayListPool.Alloc();
            try
            {
                for ( int i=0; i<propNames.Length; i++ )
                {
                    string propName = propNames [i].Trim();

                    if ( String.Compare( propName, "DisplayName", true, CultureInfo.InvariantCulture ) == 0 )
                    {
                        propIDs.Add( ResourceProps.DisplayName );
                    }
                    else if ( String.Compare( propName, "Type", true, CultureInfo.InvariantCulture ) == 0 )
                    {
                        propIDs.Add( ResourceProps.Type );
                    }
                    else
                    {
                        int direction = 1;
                        if ( propName.StartsWith( "-" ) )
                        {
                            propName = propName.Substring( 1 );
                            direction = -1;
                        }
                        try
                        {
                            propIDs.Add( Core.ResourceStore.GetPropId( propName ) * direction );
                        }
                        catch( StorageException )
                        {
                            if ( !ignoreErrors )
                            {
                                throw new ArgumentException( "Invalid property name " + propName );
                            }
                        }
                    }
                }
                return propIDs.ToArray();
            }
            finally
            {
                IntArrayListPool.Dispose(  propIDs );
            }
        }
        
        /**
         * Returns the list of columns for the specified resource list.
         */

        public ColumnDescriptor[] GetDefaultColumns( IResourceList resList )
        {
            if ( resList == null )
                throw new ArgumentNullException( "resList" );
            
            return GetColumnsForTypes( resList.GetAllTypes() );
        }

        /**
         * Returns the list of columns for the specified list of resource types.
         */

        public ColumnDescriptor[] GetColumnsForTypes( string[] resTypes )
        {
            ArrayList allColumns = new ArrayList();
            foreach( string resType in resTypes )
            {
                ArrayList columns = (ArrayList) _displayColumns [resType];
                if ( columns == null )
                    continue;

                foreach( DisplayColumn col in columns )
                {
                    MergeColumnIntoList( col, allColumns );
                }
            }
            if ( allColumns.Count == 0 )
            {
                allColumns.Add( new DisplayColumn( 0, new ColumnDescriptor( "DisplayName", 300 ) ) );
            }

            foreach( DisplayColumn col in _allTypeColumns )
            {
                MergeColumnIntoList( col, allColumns );
            }

            return DisplayColumnsToDescriptors( allColumns );
        }

        public ColumnDescriptor[] AddAnyTypeColumns( ColumnDescriptor[] columnDescriptors )
        {
            ArrayList result = new ArrayList( columnDescriptors );
            foreach( DisplayColumn col in _allTypeColumns )
            {
                result.Add( col.Descriptor );
            }
            return (ColumnDescriptor[]) result.ToArray( typeof(ColumnDescriptor) );
        }

        private void MergeColumnIntoList( DisplayColumn col, ArrayList allColumns )
        {
            int foundIndex = -1;
            for( int i = 0; i < allColumns.Count; i++ )
            {
                DisplayColumn oldCol = (DisplayColumn) allColumns [ i ];
                if( PropNamesMatchStrictly( oldCol, col ) ||
                   (( IsDisplayNameColumn( oldCol ) || IsDisplayNameColumn( col ) ) && PropNamesMatch( oldCol, col ) ))
                {
                    foundIndex = i;
                    break;
                }
            }

            if ( foundIndex >= 0 )
            {
                allColumns[ foundIndex ] = ((DisplayColumn) allColumns [ foundIndex ]).MergePropNames( col );
            }
            else
            {
                int pos = 0;
                while ( pos < allColumns.Count && ((DisplayColumn) allColumns [ pos ]).Index < col.Index )
                    pos++;
                allColumns.Insert( pos, col );
            }
        }

        private static bool PropNamesMatch( DisplayColumn col1, DisplayColumn col2 )
        {
            ArrayList props2 = new ArrayList( col2.PropNames );
            foreach( string propName in col1.PropNames )
            {
                if ( props2.Contains( propName ) )
                    return true;
            }
            return false;
        }

        private static bool PropNamesMatchStrictly( DisplayColumn col1, DisplayColumn col2 )
        {
            if( col2.PropNames.Length == col1.PropNames.Length )
            {
                ArrayList props2 = new ArrayList( col2.PropNames );
                foreach( string propName in col1.PropNames )
                {
                    if ( !props2.Contains( propName ) )
                        return false;
                }
                return true;
            }
            return false;
        }

        private static bool IsDisplayNameColumn( DisplayColumn col )
        {
            foreach( string propName in col.PropNames )
            {
                if( propName == "DisplayName" )
                    return true;
            }
            return false;
        }
        private static ColumnDescriptor[] DisplayColumnsToDescriptors( ArrayList columns )
        {
            ColumnDescriptor[] result = new ColumnDescriptor[columns.Count];
            for( int i = 0; i < columns.Count; i++ )
            {
            	result [i] = ((DisplayColumn) columns [i]).Descriptor;
            }
            return result;
        }

        /**
         * Returns the column descriptor of any registered column which shows
         * the specified property.
         */

        internal bool FindColumnDescriptor( string propName, ref ColumnDescriptor descriptor )
        {
            if ( FindDescriptorInList( propName, _allTypeColumns, ref descriptor ) )
                return true;

            foreach( HashMap.Entry entry in _displayColumns )
            {
                if ( FindDescriptorInList( propName, (ArrayList) entry.Value, ref descriptor ) )
                    return true;
            }

            foreach( HashMap.Entry entry in _availableColumns )
            {
                if ( FindDescriptorInList( propName, (ArrayList) entry.Value, ref descriptor ) )
                    return true;
            }

            return false;
        }

        private static bool FindDescriptorInList( string propName, ArrayList columnList, 
                                                  ref ColumnDescriptor descriptor )
        {
            foreach( DisplayColumn col in columnList )
            {
                if ( Array.IndexOf( col.Descriptor.PropNames, propName ) >= 0 )
                {
                    descriptor = col.Descriptor;
                    return true;
                }
            }
            return false;
        }

        /**
         * Saves the state of the list view (column widths) under the key that is
         * specified as a sequence of columns.
         */

        internal void SaveListViewState( ResourceListView2 listView, ResourceListDataProvider dataProvider,
            ResourceListState state, bool async  )
        {
            ColumnDescriptor[] columns = ColumnDescriptorsFromList( listView ); 
            columns = UpdateColumnsFromState( columns, state );
            state.SaveState( columns, dataProvider.SortSettings, async );
        }
        
        /// <summary>
        /// Returns an array of ColumnDescriptors describing the current column configuration
        /// of the ListView.
        /// </summary>
        /// <param name="listView">The list view for which the column configuration is returned.</param>
        /// <returns>An array of column descriptors.</returns>
        internal ColumnDescriptor[] ColumnDescriptorsFromList( ResourceListView2 listView )
        {
            ArrayList columnDescriptors = new ArrayList();
            foreach( JetListViewColumn col in listView.Columns )
            {
                ResourcePropsColumn propsCol = col as ResourcePropsColumn;
                if ( propsCol != null )
                {
                    string[] propNames = new string[ propsCol.PropIds.Length ];
                    for( int i=0; i<propsCol.PropIds.Length; i++ )
                    {
                        int propId = propsCol.PropIds [i];
                        if ( propId < 0 )
                        {
                            propNames [i] = "-" + Core.ResourceStore.PropTypes [-propId].Name;
                        }
                        else
                        {
                            propNames [i] = Core.ResourceStore.PropTypes [propId].Name;
                        }
                    }

                    int width;
                    ColumnDescriptorFlags flags = 0;
                    if ( col.FixedSize )
                    {
                        flags |= ColumnDescriptorFlags.FixedSize;
                    }
                    if ( col.AutoSize )
                    {
                        flags |= ColumnDescriptorFlags.AutoSize;
                        width = col.AutoSizeMinWidth;
                    }
                    else
                    {
                        width = col.Width;
                    }
                    columnDescriptors.Add( new ColumnDescriptor( propNames, width, flags ) );
                }
            }
            return (ColumnDescriptor[]) columnDescriptors.ToArray( typeof (ColumnDescriptor) );
        }

        internal ResourceListState GetListViewState( IResource ownerResource, IResourceList resList, 
                                                     ColumnDescriptor[] defaultColumns, bool defaultGroupItems )
        {
            ResourceListState state;
            IResource columnScheme = null;
            if ( ownerResource != null )
            {
                IResourceList columnSchemes = ownerResource.GetLinksOfType( "ColumnScheme", Props.ColumnSchemeOwner );
                foreach( IResource aScheme in columnSchemes )
                {
                    if ( aScheme.GetStringProp( Props.ColumnSchemeTab ) == Core.TabManager.CurrentTabId )
                    {
                        columnScheme = aScheme;
                        break;
                    }
                }
            }
    
            if ( columnScheme != null )
            {
                state = ResourceListState.FromResource( columnScheme );
            }
            else
            {
                state = StateFromList( resList, defaultColumns, defaultGroupItems );
            }
            return state;
        }

        /// <summary>
        /// Locates and, if necessary, creates a resource list state which applies to all views
        /// with resources of types matching the contents of the resource list.
        /// </summary>
        internal ResourceListState StateFromList( IResourceList resList, ColumnDescriptor[] defaultColumns,
            bool defaultGroupItems )
        {
            string[] keyTypes = resList.GetAllTypes();
            keyTypes = CollapseFileTypes( keyTypes );
            ComparableArrayList keyTypeList = new ComparableArrayList( keyTypes );
            ResourceListState state = (ResourceListState) _columnState [keyTypeList];
            if ( state == null )
            {
                IResource stateResource = Core.ResourceStore.FindUniqueResource( "ColumnScheme",
                    _props.ColumnKeyTypes, String.Join( ";", keyTypes ) );
                if ( stateResource != null )
                {
                    state = ResourceListState.FromResource( stateResource );
                    // filter out invalid data
                    if ( state.Columns.Length == 0 )
                    {
                        state.Columns = defaultColumns;
                    }
                }
                else
                {
                    state = new ResourceListState( defaultColumns, null, defaultGroupItems );
                    state.KeyTypes = keyTypes;
                }
                _columnState [keyTypeList] = state;
            }
            return state;
        }

        /// <summary>
        /// Replaces all FileFormat resource types in the specified array of resource types
        /// with one pseudo-resource type "File".
        /// </summary>
        internal string[] CollapseFileTypes( string[] keyTypes )
        {
            for( int i=0; i<keyTypes.Length; i++ )
            {
                if ( Core.ResourceStore.ResourceTypes [keyTypes [i]].HasFlag( ResourceTypeFlags.FileFormat ) )
                {
                    ArrayList noFileResourceTypes = new ArrayList();
                    foreach( string type in keyTypes )
                    {
                        if ( !Core.ResourceStore.ResourceTypes [type].HasFlag( ResourceTypeFlags.FileFormat ) )
                        {
                            noFileResourceTypes.Add( type );
                        }
                    }
                    noFileResourceTypes.Add( "File" );
                    return (string[]) noFileResourceTypes.ToArray( typeof (string) );
                }
            }
            return keyTypes;
        }

        /// <summary>
        /// Adds a Type column to the specified column list if it is not present there.
        /// </summary>
        internal ColumnDescriptor[] CreateTypeColumn(ColumnDescriptor[] columns)
        {
            if ( columns.Length > 0 )
            {
                foreach( ColumnDescriptor desc in columns )
                {
                    if ( desc.PropNames.Length > 0 && desc.PropNames [0] == "Type" )
                    {
                        return columns;
                    }
                }

                ColumnDescriptor[] newColumns = new ColumnDescriptor[ columns.Length+1 ];
                int destIndex = 0;
                while( (columns [destIndex].Flags & ColumnDescriptorFlags.FixedSize) != 0 )
                {
                    newColumns [destIndex] = columns [destIndex];
                    destIndex++;
                }

                newColumns [destIndex] = new ColumnDescriptor( "Type", 20, ColumnDescriptorFlags.FixedSize );
                Array.Copy( columns, destIndex, newColumns, destIndex+1, columns.Length-destIndex );
                return newColumns;
            }

            return columns;
        }

        /**
         * For columns in the keyColumns array which have custom comparers, assign these
         * comparers to the columns with the same property IDs in the second array.
         */

        internal void RestoreCustomComparers( ColumnDescriptor[] keyColumns, ref ColumnDescriptor[] columns )
        {
            foreach( ColumnDescriptor keyColDesc in keyColumns )
            {
                if ( keyColDesc.CustomComparer != null )
                {
                    for( int i=0; i<columns.Length; i++ )
                    {
                        if ( columns [i].EqualsIgnoreWidth( keyColDesc ) )
                        {
                            columns [i].CustomComparer = keyColDesc.CustomComparer;
                            columns [i].GroupProvider = keyColDesc.GroupProvider;
                            columns [i].SortMenuAscText = keyColDesc.SortMenuAscText;
                            columns [i].SortMenuDescText = keyColDesc.SortMenuDescText;
                            break;
                        }
                    }
                }
            }
        }

        /**
         * Shows the specified list of columns in the list view.
         */

        internal void ShowListViewColumns( ResourceListView2 listView, 
            ColumnDescriptor[] columns, ResourceListDataProvider dataProvider, bool setGroupProviders )
        {
            ColumnDescriptor[] oldColumns = ColumnDescriptorsFromList( listView );
            bool hadTreeColumn = HasTreeColumn( listView );
            bool needTreeColumn = (dataProvider is ConversationDataProvider);

            if ( !new ComparableArrayList( columns ).Equals( new ComparableArrayList( oldColumns ) ) ||
                 hadTreeColumn != needTreeColumn )
            {
                RecreateListViewColumns( listView, columns, dataProvider, setGroupProviders );
            }
            else
            {
                UpdateListViewColumns( listView, columns, dataProvider, setGroupProviders );
            }
        }

        private void RecreateListViewColumns( ResourceListView2 listView, ColumnDescriptor[] columns, 
                                              ResourceListDataProvider dataProvider, bool setGroupProviders )
        {
            bool haveTreeColumn = false;
            listView.Columns.BeginUpdate();
            try
            {
                listView.Columns.Clear();
                foreach( ColumnDescriptor desc in columns )
                {
                    int[] propIds = PropNamesToIDs( desc.PropNames, true );
                    ResourcePropsColumn colHdr;

                    if ( dataProvider is ConversationDataProvider && !haveTreeColumn && 
                        ((propIds.Length == 1 && propIds [0] == ResourceProps.Type) || ( desc.Flags & ColumnDescriptorFlags.FixedSize ) == 0 ) )
                    {
                        haveTreeColumn = true;
                        ConversationStructureColumn col = new ConversationStructureColumn( dataProvider as ConversationDataProvider );
                        listView.Columns.Add( col );
                    }
                    
                    if ( propIds.Length == 1 && propIds [0] == ResourceProps.Type )
                    {
                        colHdr = listView.AddIconColumn();
                        if ( dataProvider is ConversationDataProvider )
                        {
                            colHdr.ShowHeader = false;
                        }
                    }
                    else
                    {
                        ICustomColumn[] customColumns = BuildCustomColumns( propIds );
                        if ( customColumns == null )
                        {
                            colHdr = listView.AddColumn( propIds );
                            colHdr.Text = GetColumnText( propIds );
                            SetPropertyToTextCallbacks( colHdr as ResourceListView2Column );
                            (colHdr as ResourceListView2Column).OwnerList = dataProvider.ResourceList;
                        }
                        else
                        {
                            colHdr = listView.AddCustomColumn( propIds, customColumns );
                        }
                        FillSortMenuText( colHdr, desc, propIds );
                        colHdr.CustomComparer = desc.CustomComparer;
                        colHdr.Alignment = GetDefaultAlignment( propIds );
                    }

                    if ( ( desc.Flags & ColumnDescriptorFlags.AutoSize ) != 0 )
                    {
                        colHdr.AutoSize = true;
                        colHdr.AutoSizeMinWidth = desc.Width;
                    }
                    else
                    {
                        colHdr.Width = desc.Width;
                        if ( ( desc.Flags & ColumnDescriptorFlags.FixedSize ) != 0 )
                        {
                            colHdr.FixedSize = true;
                        }
                    }

                    UpdateGroupProvider( setGroupProviders, desc, colHdr );
                }
            }
            finally
            {
                listView.Columns.EndUpdate();                    
            }
        }

        private void UpdateListViewColumns( ResourceListView2 listView, ColumnDescriptor[] columns, 
            ResourceListDataProvider dataProvider, bool setGroupProviders )
        {
            foreach( JetListViewColumn col in listView.Columns )
            {
                ConversationStructureColumn convStructureColumn = col as ConversationStructureColumn;
                if ( convStructureColumn != null )
                {
                    convStructureColumn.DataProvider = dataProvider as ConversationDataProvider;
                }

                ResourcePropsColumn propsCol = col as ResourcePropsColumn;
                if ( propsCol != null )
                {
                    foreach( ColumnDescriptor colDesc in columns )
                    {
                        int[] propIds = PropNamesToIDs( colDesc.PropNames, true );
                        if ( propsCol.PropIdsEqual( propIds ) )
                        {
                            UpdateGroupProvider( setGroupProviders, colDesc, propsCol );
                            break;
                        }
                    }
                }

                ResourceListView2Column rlvCol = col as ResourceListView2Column;
                if ( rlvCol != null )
                {
                    rlvCol.OwnerList = dataProvider.ResourceList;
                }
            }
        }

        private static void UpdateGroupProvider( bool setGroupProviders, ColumnDescriptor desc,
                                                 ResourcePropsColumn colHdr )
        {
            if ( setGroupProviders )
            {
                if ( desc.GroupProvider != null )
                {
                    colHdr.GroupProvider = new GroupProviderAdapter( desc.GroupProvider );
                }
                else
                {
                    colHdr.GroupProvider = GetGroupProvider( colHdr.PropIds );
                }
            }
            else
            {
                colHdr.GroupProvider = null;
            }
        }

        private void FillSortMenuText( JetListViewColumn column, ColumnDescriptor desc, int[] propIds )
        {
            column.SortMenuText = GetColumnText( propIds );
            if ( desc.SortMenuAscText != null )
            {
                column.SortMenuAscText = desc.SortMenuAscText;
                column.SortMenuDescText = desc.SortMenuDescText;
            }
            else
            {
                for( int i=0; i<propIds.Length; i++ )
                {
                    PropDataType propType = Core.ResourceStore.PropTypes [propIds [i]].DataType;
                    if ( propType == PropDataType.Date )
                    {
                        column.SortMenuAscText = "Oldest on top";
                        column.SortMenuDescText = "Newest on top";
                        break;
                    }
                    if ( propType == PropDataType.String || propType == PropDataType.LongString || 
                        propType == PropDataType.Link )
                    {
                        column.SortMenuAscText = "A on top";
                        column.SortMenuDescText = "Z on top";
                        break;
                    }
                }
            }
        }

        private static IGroupProvider GetGroupProvider( int[] propIds )
        {
            if ( propIds.Length > 0 )
            {
                int propId = propIds [0];
                if ( propId == ResourceProps.Type )
                {
                    return new ResourceTypeGroupProvider();
                }
                if ( propId == ResourceProps.DisplayName )
                {
                    return new DisplayNameGroupProvider();
                }
                PropDataType propType = Core.ResourceStore.PropTypes [propId].DataType;
                if ( propType == PropDataType.Date )
                {
                    return new DateGroupProvider( propId );
                }
                if ( propType == PropDataType.String || propType == PropDataType.LongString || propType == PropDataType.Link )
                {
                    return new PropTextGroupProvider( propId );
                }
            }
            return null;
        }

        /**
         * Returns the default alignment for the specified column names.
         */

        private static HorizontalAlignment GetDefaultAlignment( int[] propIDs )
        {
            for( int i=0; i<propIDs.Length; i++ )
            {
                PropDataType propType = Core.ResourceStore.PropTypes [ propIDs [i] ].DataType;
                if ( propType == PropDataType.Double ) 
                    return HorizontalAlignment.Right;
            }
            return HorizontalAlignment.Left;
        }

        private ICustomColumn[] BuildCustomColumns( int[] propIds )
        {
            ICustomColumn[] result = null;
            for( int i=0; i<propIds.Length; i++ )
            {
                int propId = propIds [i];
                ICustomColumn customColumn = (ICustomColumn) _customColumns [propId];
                if ( customColumn != null )
                {
                    if ( result == null )
                    {
                        result = new ICustomColumn[ propIds.Length ];
                    }
                    result [i] = customColumn;
                }
            }
            return result;
        }

        private void SetPropertyToTextCallbacks( ResourceListView2Column column )
        {
            for( int i=0; i<column.PropIds.Length; i++ )
            {
                column.SetPropToTextConverter( column.PropIds [i], 
                    (PropertyToTextConverter) _propToTextConverters [column.PropIds [i]] );
            }
        }

        public ICustomColumn GetCustomColumn( int propId )
        {
            return (ICustomColumn) _customColumns [propId];
        }

        /**
         * Returns the array of displaynames for the specified list of property IDs.
         */

        public string GetColumnText( int[] propIds )
        {
            if ( propIds.Length == 0 )
            {
                return "";
            }

            ArrayList propNames = new ArrayList();

            for( int i=0; i<propIds.Length; i++ )
            {
                if ( propIds [i] != ResourceProps.Type )
                {
                    if ( propIds [i] == ResourceProps.DisplayName && propIds.Length > 1 )
                    {
                        continue;
                    }

                    string propDisplayName = Core.ResourceStore.PropTypes.GetPropDisplayName( propIds [i] );
                    if ( !propNames.Contains( propDisplayName ) )
                    {
                        propNames.Add( propDisplayName );
                    }
                }
            }
            return String.Join( ", ", (string[]) propNames.ToArray( typeof (string) ) );
        }

        internal ColumnDescriptor[] UpdateColumnsFromState( ColumnDescriptor[] descriptors, ResourceListState state )
        {
            bool[] usedColumns = new bool [state.Columns.Length];
            ArrayList result = new ArrayList();
            for( int i=0; i<descriptors.Length; i++ )
            {
                bool found = false;
                for( int j=0; j<state.Columns.Length; j++ )
                {
                    if ( state.Columns [j].PropNamesEqual( descriptors [i] ) )
                    {
                        ColumnDescriptor desc = descriptors [i];
                        desc.Flags = state.Columns [j].Flags;
                        result.Add( desc );
                        found = true;
                        usedColumns [j] = true;
                        break;
                    }
                }
                if ( !found )
                {
                    result.Add( descriptors [i] );
                }
            }

            for( int i=0; i<state.Columns.Length; i++ )
            {
                if ( !usedColumns [i] )
                {
                    int index = (i == 0) 
                        ? 0 
                        : FindColumn( result, state.Columns [i-1] )+1;
                    result.Insert( index, state.Columns [i] );
                }
            }

            return (ColumnDescriptor[]) result.ToArray( typeof (ColumnDescriptor) );
        }

        private static int FindColumn( ArrayList list, ColumnDescriptor columnDescriptor )
        {
            for( int i=0; i<list.Count; i++ )
            {
                ColumnDescriptor col = (ColumnDescriptor) list [i];
                if ( col.PropNamesEqual( columnDescriptor ) )
                {
                    return i;
                }
            }
            return -1;
        }

        private static bool HasTreeColumn(ResourceListView2 listView)
        {
            foreach( JetListViewColumn col in listView.Columns )
            {
                if ( col is ConversationStructureColumn )
                {
                    return true;
                }
            }
            return false;
        }

        private class GroupProviderAdapter: IGroupProvider
        {
            private readonly IResourceGroupProvider _groupProvider;

            public GroupProviderAdapter( IResourceGroupProvider groupProvider )
            {
                _groupProvider = groupProvider;
            }

            public string GetGroupName( object item )
            {
                return _groupProvider.GetGroupName( (IResource) item );
            }
        }
    }
}
