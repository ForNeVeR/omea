// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
    /// <summary>
    /// A complete state of a resource list.
    /// </summary>
    internal class ResourceListState
    {
        private ColumnDescriptor[] _columns;
        private SortSettings _sortSettings;
        private IResource _columnSchemeResource;
        private string[] _keyTypes;
        private IResource _ownerResource;
        private string _ownerTab;
        private bool _groupItems;

        internal ResourceListState( ColumnDescriptor[] columns, SortSettings sortSettings, bool groupItems )
        {
            _columns = columns;
            _sortSettings = sortSettings;
            _groupItems = groupItems;
        }

        public ColumnDescriptor[] Columns
        {
            get { return _columns; }
            set { _columns = value; }
        }

        public SortSettings SortSettings
        {
            get { return _sortSettings; }
        }

        public bool GroupItems
        {
            get { return _groupItems; }
            set { _groupItems = value; }
        }

        public string[] KeyTypes
        {
            get { return _keyTypes; }
            set { _keyTypes = value; }
        }

        public IResource OwnerResource
        {
            get { return _ownerResource; }
        }

        public void SetOwner( IResource resource, string tabId )
        {
            _keyTypes = null;
            _ownerResource = resource;
            _ownerTab = tabId;
        }

        internal void Delete()
        {
            new ResourceProxy( _columnSchemeResource ).DeleteAsync();
            _columnSchemeResource = null;
        }

        internal static ResourceListState FromResource( IResource res )
        {
            DisplayColumnProps props = (Core.DisplayColumnManager as DisplayColumnManager).Props;

            IResourceList columnDescriptorResources = res.GetLinksOfType( "ColumnDescriptor", props.ColumnDescriptor );
            columnDescriptorResources.Sort( new int[] { props.ColumnOrder }, true );
            ColumnDescriptor[] columns = new ColumnDescriptor [columnDescriptorResources.Count];
            for( int i=0; i<columnDescriptorResources.Count; i++ )
            {
                columns [i] = LoadColumnDescriptor( columnDescriptorResources [i] );
            }

            SortSettings sortSettings = LoadSortSettings( res, props );

            ResourceListState result = new ResourceListState( columns, sortSettings,
                res.HasProp( props.GroupItems ) );
            result._columnSchemeResource = res;

            if ( res.HasProp( props.ColumnKeyTypes ) )
            {
                result.KeyTypes = res.GetPropText( props.ColumnKeyTypes ).Split( ';' );
            }
            if ( res.HasProp( props.ColumnSchemeOwner ) )
            {
                result.SetOwner( res.GetLinkProp( props.ColumnSchemeOwner ),
                    res.GetStringProp( props.ColumnSchemeTab ) );
            }

            return result;
        }

        public void SaveState( ColumnDescriptor[] columns, SortSettings sortSettings, bool async  )
        {
            _columns   = columns;
            _sortSettings = sortSettings;

            if ( async )
            {
                Core.ResourceAP.QueueJob( JobPriority.Immediate, new MethodInvoker( DoSaveState ) );
            }
            else
            {
                Core.ResourceAP.RunUniqueJob( new MethodInvoker( DoSaveState ) );
            }
        }

        private void DoSaveState()
        {
            DisplayColumnProps props = (Core.DisplayColumnManager as DisplayColumnManager).Props;

            IResourceList oldColumnDescriptors;
            if ( _columnSchemeResource == null )
            {
                _columnSchemeResource = Core.ResourceStore.BeginNewResource( "ColumnScheme" );
                oldColumnDescriptors = Core.ResourceStore.EmptyResourceList;
            }
            else
            {
                if ( _columnSchemeResource.IsDeleted )
                {
                    return;
                }

                _columnSchemeResource.BeginUpdate();
                oldColumnDescriptors = _columnSchemeResource.GetLinksOfType( "ColumnDescriptor",
                    props.ColumnDescriptor );
                oldColumnDescriptors.Sort( new int[] { props.ColumnOrder }, true );
            }

            for( int i=0; i < _columns.Length; i++ )
            {
                IResource columnDescriptorResource;
                if ( i < oldColumnDescriptors.Count )
                {
                    columnDescriptorResource = oldColumnDescriptors [i];
                    columnDescriptorResource.BeginUpdate();
                }
                else
                {
                    columnDescriptorResource = Core.ResourceStore.BeginNewResource( "ColumnDescriptor" );
                }
                SaveColumnDescriptor( i, _columns [i], columnDescriptorResource );

                if ( i >= oldColumnDescriptors.Count )
                {
                    _columnSchemeResource.AddLink( props.ColumnDescriptor, columnDescriptorResource );
                }

                columnDescriptorResource.EndUpdate();
            }
            for( int i=_columns.Length; i < oldColumnDescriptors.Count; i++ )
            {
                new ResourceProxy( oldColumnDescriptors [i] ).Delete();
            }

            if ( _sortSettings != null )
            {
                string[] sortPropNames = new string[_sortSettings.SortProps.Length];
                for( int i = 0; i < _sortSettings.SortProps.Length; i++ )
                {
                    int sortProp = _sortSettings.SortProps[ i ];
                    if( sortProp < 0 )
                    {
                        sortPropNames[ i ] = "-" + Core.ResourceStore.PropTypes[ -sortProp ].Name;
                    }
                    else
                    {
                        sortPropNames[ i ] = Core.ResourceStore.PropTypes[ sortProp ].Name;
                    }
                }
                SetStringListProp( _columnSchemeResource, props.ColumnSortProps, sortPropNames );
                _columnSchemeResource.SetProp( props.ColumnSortAsc, _sortSettings.SortAscending );
            }
            else
            {
                _columnSchemeResource.DeleteProp( props.ColumnSortProps );
            }

            if ( _keyTypes != null )
            {
                _columnSchemeResource.SetProp( props.ColumnKeyTypes, String.Join( ";", _keyTypes ) );
            }
            else if ( _ownerResource != null )
            {
                _columnSchemeResource.AddLink( props.ColumnSchemeOwner, _ownerResource );
                _columnSchemeResource.SetProp( props.ColumnSchemeTab, _ownerTab );
            }
            _columnSchemeResource.SetProp( props.GroupItems, _groupItems );

            _columnSchemeResource.EndUpdate();
        }

        private static SortSettings LoadSortSettings( IResource res, DisplayColumnProps props )
        {
            IStringList sortPropList = res.GetStringListProp( props.ColumnSortProps );
            IntArrayList sortProps = IntArrayListPool.Alloc();
            try
            {
                for( int i=0; i<sortPropList.Count; i++ )
                {
                    string sortPropName = sortPropList [i];
                    if ( sortPropName == "DisplayName" )
                    {
                        sortProps.Add( ResourceProps.DisplayName );
                    }
                    else if ( sortPropName == "Type" )
                    {
                        sortProps.Add( ResourceProps.Type );
                    }
                    else if ( sortPropName.StartsWith( "-" ) )
                    {
                        sortPropName = sortPropName.Substring( 1 );
                        if ( Core.ResourceStore.PropTypes.Exist( sortPropName ) )
                        {
                            sortProps.Add( -Core.ResourceStore.PropTypes [sortPropName].Id );
                        }
                    }
                    else
                    {
                        if ( Core.ResourceStore.PropTypes.Exist( sortPropName ) )
                        {
                            sortProps.Add( Core.ResourceStore.PropTypes [sortPropName].Id );
                        }
                    }
                }

                bool sortAsc = res.HasProp( props.ColumnSortAsc );
                return new SortSettings( sortProps.ToArray(), sortAsc );
            }
            finally
            {
                IntArrayListPool.Dispose( sortProps );
            }
        }

        private static ColumnDescriptor LoadColumnDescriptor( IResource resource )
        {
            DisplayColumnProps props = (Core.DisplayColumnManager as DisplayColumnManager).Props;

            IStringList columnPropList = resource.GetStringListProp( props.ColumnProps );
            string[] columnProps = new string [columnPropList.Count];
            for( int i=0; i<columnPropList.Count; i++ )
            {
                columnProps [i] = columnPropList [i];
            }

            int width = resource.GetIntProp( props.ColumnWidth );
            ColumnDescriptorFlags flags = (ColumnDescriptorFlags) resource.GetIntProp( props.ColumnFlags );
            return new ColumnDescriptor( columnProps, width, flags );
        }

        private static void SaveColumnDescriptor( int order, ColumnDescriptor descriptor, IResource resource )
        {
            DisplayColumnProps props = (Core.DisplayColumnManager as DisplayColumnManager).Props;

            resource.SetProp( props.ColumnOrder, order );

            SetStringListProp( resource, props.ColumnProps, descriptor.PropNames );

            resource.SetProp( props.ColumnWidth, descriptor.Width );
            resource.SetProp( props.ColumnFlags, (int) descriptor.Flags );
        }

        private static void SetStringListProp( IResource resource, int propId, string[] values )
        {
            bool columnPropsOK = false;
            IStringList columnProps = resource.GetStringListProp( propId );
            if ( columnProps.Count == values.Length )
            {
                columnPropsOK = true;
                for( int i=0; i<columnProps.Count; i++ )
                {
                    if ( columnProps [i] != values [i] )
                    {
                        columnPropsOK = false;
                        break;
                    }
                }
            }

            if ( !columnPropsOK )
            {
                columnProps.Clear();
                for( int i=0; i<values.Length; i++ )
                {
                    columnProps.Add( values [i] );
                }
            }
        }

        public static void DeleteAllStates()
        {
            Core.ResourceStore.GetAllResources( "ColumnScheme" ).DeleteAll();
            Core.ResourceStore.GetAllResources( "ColumnDescriptor" ).DeleteAll();
        }
    }
}
