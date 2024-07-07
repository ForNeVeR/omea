// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceTools
{
    public class ResourceTypeHelper
	{
        private static IResourceList _customProperties;
        private static ArrayList _customPropTypes;
        private static IResourceList _fileResourceTypeList;
        private static string[] _fileResourceTypes;

        public static int UpdatePropTypeRegistration( string name, PropDataType dataType, PropTypeFlags flags )
        {
            int propID;
            IResource propres = Core.ResourceStore.FindUniqueResource( "PropType", "Name", name );
            if ( propres != null )
            {
                propID = propres.GetIntProp( "ID" );
                propres.SetProp( "Flags", (int) flags );
            }
            else
            {
                propID = Core.ResourceStore.PropTypes.Register( name, dataType, flags );
            }
            return propID;
        }

        public static IResourceList GetVisibleResourceTypes()
        {
            IResourceList resTypes = Core.ResourceStore.GetAllResources( "ResourceType" );
            resTypes = resTypes.Minus( Core.ResourceStore.FindResources( "ResourceType", "Internal", 1 ) );

            resTypes.Sort( new SortSettings( ResourceProps.DisplayName, true ) );

            return resTypes;
        }

        /// <summary>
        /// Method iterates over the list of resources and collects an unsorted
        /// array of their resource types.
        /// </summary>
        /// <param name="list">List of resources which types are to be collected.</param>
        /// <returns>Unsorted list of resource types.</returns>
        public static string[] GetUnderlyingResourceTypes( IResourceList list )
        {
            ArrayList strs = new ArrayList();
            foreach( IResource res in list )
            {
                if( strs.IndexOf( res.Type ) == -1 )
                    strs.Add( res.Type );
            }
            return ( strs.Count > 0 ) ? (string[])strs.ToArray( typeof( string )) : null;
        }

        ///<summary>
        /// Checks if all resources in the specified resource list have the
        /// Internal flag.
        ///</summary>
        public static bool AllResourcesInternal( IResourceList resList )
        {
            bool allInternal = true;
            foreach( IResource res in resList.ValidResources )
            {
                if( !Core.ResourceStore.ResourceTypes[ res.Type ].HasFlag( ResourceTypeFlags.Internal ) )
                    allInternal = false;
            }
            return allInternal;
        }

        ///<summary>
        /// Checks if any resources in the specified resource list have the
        /// Internal flag.
        ///</summary>
        public static bool AnyResourcesInternal( IResourceList resList )
        {
            foreach( IResource res in resList.ValidResources )
            {
                if( Core.ResourceStore.ResourceTypes[ res.Type ].HasFlag( ResourceTypeFlags.Internal ) )
                    return true;
            }
            return false;
        }

        public static bool AllResourcesHaveProp( IResourceList list, int propId )
        {
            bool allHaveProp = false;
            if( list != null && list.Count > 0 )
            {
                allHaveProp = true;
                foreach( IResource res in list.ValidResources )
                    allHaveProp = allHaveProp && res.HasProp( propId );
            }
            return allHaveProp;
        }

        public static string[] GetFileResourceTypes()
        {
            if ( _fileResourceTypeList == null )
            {
                _fileResourceTypeList = Core.ResourceStore.FindResourcesLive( "ResourceType", "FileFormat", 1 );
                _fileResourceTypeList.ResourceAdded += UpdateFileResourceTypes;
                _fileResourceTypeList.ResourceDeleting += UpdateFileResourceTypes;
                UpdateFileResourceTypes( null, null );
            }
            return _fileResourceTypes;
        }

        private static void UpdateFileResourceTypes( object sender, ResourceIndexEventArgs e )
        {
            ArrayList types = new ArrayList();
            foreach( IResource res in _fileResourceTypeList )
            {
                if ( !res.IsDeleting )
                {
                    types.Add( res.GetStringProp( "Name" ) );
                }
            }
            _fileResourceTypes = (string[]) types.ToArray( typeof (string) );
        }

        /// <summary>
        /// Returns true if given resource type exist and the plugin which
        /// is responsible for it is loaded.
        /// </summary>
        /// <param name="resType">String representing a resource type name.</param>
        /// <returns></returns>
        public static bool IsResourceTypeActive( string resType )
        {
            if( resType == null )
                return true;

            string[] types = resType.Split( '|', '#' );
            foreach( string type in types )
            {
                if( IsBaseResourceTypeActive( type ))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if any of the given resource types does not exist
        /// or the plugin which is responsible for it is not loaded.
        /// </summary>
        /// <param name="resType">String representing a compound resource type name.</param>
        /// <returns></returns>
        public static bool IsResourceTypePassive( string resType )
        {
            if( resType == null )
                return false;

            string[] types = resType.Split( '|', '#' );
            foreach( string type in types )
            {
                if( !IsBaseResourceTypeActive( type ))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Return true if a resource type name exists (registered) and the plugin which
        /// registered this resource type is loaded.
        /// </summary>
        /// <param name="type">Resource type name.</param>
        public static bool IsBaseResourceTypeActive( string type )
        {
            return  type == null ||
                   ( Core.ResourceStore.ResourceTypes.Exist( type ) &&
                    Core.ResourceStore.ResourceTypes[ type ].OwnerPluginLoaded );
        }

        /// <summary>
        /// Return true if a resource type name exists (registered) and it represents the
        /// formatted files objects.
        /// </summary>
        /// <param name="type">Resource type name.</param>
        public static bool IsFileFormatResType( string type )
        {
            return Core.ResourceStore.ResourceTypes.Exist( type ) &&
                   Core.ResourceStore.ResourceTypes[ type ].HasFlag( ResourceTypeFlags.FileFormat );
        }

        /// <summary>
        /// Return true if property with such name exists (registered) in the system and
        /// its underlying type is Date.
        /// </summary>
        /// <param name="propName">Name of a property.</param>
        public static bool IsDateProperty( string propName )
        {
            if( propName == null )
                return false;

            IPropTypeCollection propTypes = Core.ResourceStore.PropTypes;
            return( propTypes.Exist( propName ) && ( propTypes[ propName ].DataType == PropDataType.Date ));
        }

        /// <summary>
        /// Return true if property with such Id exists (registered) in the system and
        /// its underlying type is string.
        /// </summary>
        /// <param name="propId">Id of a property.</param>
        public static bool IsStringProp( int propId )
        {
            IPropType type = Core.ResourceStore.PropTypes[ propId ];
            return type != null && type.DataType == PropDataType.String;
        }

        /// <summary>
        /// Return true if link with the given Id is an "account" link, that
        /// is connects a Contact and Account resource types.
        /// </summary>
        /// <param name="linkId">Id of a link.</param>
        public static bool IsAccountLink( int linkId )
        {
            IPropType type = Core.ResourceStore.PropTypes[ linkId ];
            return (type != null) && type.HasFlag( PropTypeFlags.ContactAccount );
        }

        public static void ExtractFields( string str, out string[] formats, out string[] resTypes, out string[] linkTypes )
        {
            ArrayList typesList = new ArrayList(), formatsList = new ArrayList(), linksList = new ArrayList();
            if( str != null )
            {
                string[] allTypes = str.Split( '|', '#' ); //  '#' is a rudiment from the earlier versions.
                foreach( string type in allTypes )
                {
                    if( IsFileFormatResType( type ) )
                        formatsList.Add( type );
                    else
                    if( Core.ResourceStore.ResourceTypes.Exist( type ) )
                        typesList.Add( type );
                    else
                        linksList.Add( type );
                }
            }
            formats =  (string[]) formatsList.ToArray( typeof( string ));
            resTypes = (string[]) typesList.ToArray( typeof( string ));
            linkTypes = (string[]) linksList.ToArray( typeof( string ));
        }

        public static void ExtractFormatFields( string str, out string[] types, out string[] formats )
        {
            ArrayList resTypesList = new ArrayList(), formatsList = new ArrayList();
            if( str != null )
            {
                string[] allTypes = str.Split( '|', '#' ); //  '#' is a rudiment from the earlier versions.
                foreach( string type in allTypes )
                {
                    if( Core.ResourceStore.ResourceTypes.Exist( type ) )
                    {
                        if( Core.ResourceStore.ResourceTypes[ type ].HasFlag( ResourceTypeFlags.FileFormat ) )
                            formatsList.Add( type );
                        else
                            resTypesList.Add( type );
                    }
                }
            }
            types = (string[]) resTypesList.ToArray( typeof( string ));
            formats =  (string[]) formatsList.ToArray( typeof( string ));
        }
        /// <summary>
        /// Split a sequence of resource type and link type names into two arrays,
        /// which keep ordinary resource types and these names separately.
        /// Each resource type is represented by its deep (not display) name.
        /// </summary>
        /// <param name="allTypes">Sequence of resource type and link type names.</param>
        /// <param name="resTypes">Output string array containing ordinary resource types.</param>
        /// <param name="linkTypes">Output string array containing link resource types.</param>
        public static void ExtractFields( string[] allTypes, out string[] resTypes, out string[] linkTypes )
        {
            ArrayList resTypesList = new ArrayList(), linkTypesList = new ArrayList();
            foreach( string type in allTypes )
            {
                if( Core.ResourceStore.ResourceTypes.Exist( type ) )
                    resTypesList.Add( type );
                else
                    linkTypesList.Add( type );
            }
            resTypes = (string[]) resTypesList.ToArray( typeof( string ));
            linkTypes = (string[]) linkTypesList.ToArray( typeof( string ));
        }

        /// <summary>
        /// Extract resources of the specified type only and return them as
        /// another resource list.
        /// </summary>
        public static IResourceList  ExtractListForType( IResourceList list, string type )
        {
            List<int> ids = new List<int>();
            foreach( IResource res in list )
            {
                if( res.Type == type )
                    ids.Add( res.Id );
            }
            return Core.ResourceStore.ListFromIds( ids, false );
        }

        public static bool IsResourceTypeByDisplayName( string name )
        {
            IResourceTypeCollection resTypes = Core.ResourceStore.ResourceTypes;
            foreach( IResourceType rt in resTypes )
            {
                if( rt.DisplayName == name )
                    return true;
            }
            return false;
        }

        public static string ResTypeDisplayName( string deepName )
        {
            IResourceTypeCollection resTypes = Core.ResourceStore.ResourceTypes;
            foreach( IResourceType rt in resTypes )
            {
                if( rt.Name == deepName )
                    return rt.DisplayName;
            }
            throw new ApplicationException( "No such resource type for conversion" );
        }

        public static string LinkTypeDisplayName( string deepName )
        {
            IPropTypeCollection propTypes = Core.ResourceStore.PropTypes;
            foreach( IPropType pt in propTypes )
            {
                if( pt.Name == deepName )
                    return pt.DisplayName;
            }
            throw new ApplicationException( "No such link type for conversion" );
        }

        public static string LinkTypeReversedDisplayName( string deepName )
        {
            IPropTypeCollection propTypes = Core.ResourceStore.PropTypes;
            foreach( IPropType pt in propTypes )
            {
                if( pt.Name == deepName )
                {
                    if( pt.ReverseDisplayName != null )
                        return pt.ReverseDisplayName;
                    else
                        return pt.DisplayName;
                }
            }
            throw new ApplicationException( "No such link type for conversion" );
        }

        public static string ResTypeDeepName( string displayName )
        {
            IResourceTypeCollection resTypes = Core.ResourceStore.ResourceTypes;
            foreach( IResourceType rt in resTypes )
            {
                if( rt.DisplayName == displayName )
                    return rt.Name;
            }
            throw new ApplicationException( "No such resource type for conversion" );
        }

        public static IResourceList ExcludeUnloadedPluginResources( IResourceList resList )
        {
            ArrayList unloadedResourceTypes = null;
            IResourceList excludedTypes = null;
            foreach( IResourceType rt in Core.ResourceStore.ResourceTypes )
            {
                if ( !rt.OwnerPluginLoaded )
                {
                    if ( unloadedResourceTypes == null )
                    {
                        unloadedResourceTypes = new ArrayList();
                    }
                    unloadedResourceTypes.Add( rt.Name );
                }
            }
            if ( unloadedResourceTypes != null )
            {
                string[] resTypes = (string[]) unloadedResourceTypes.ToArray( typeof(string) );
                excludedTypes = Core.ResourceStore.GetAllResourcesLive( resTypes );
            }

            foreach( IPropType pt in Core.ResourceStore.PropTypes )
            {
                if ( pt.HasFlag( PropTypeFlags.SourceLink ) && !pt.OwnerPluginLoaded )
                {
                    excludedTypes = Core.ResourceStore.FindResourcesWithProp( null, pt.Id ).Union( excludedTypes );
                }
            }
            if ( excludedTypes != null )
            {
                return resList.Minus( excludedTypes );
            }
            return resList;
        }

        #region Custom Properties
        public static IResourceList GetCustomProperties()
        {
            if ( _customProperties == null )
            {
                IResourceStore store = ICore.Instance.ResourceStore;
                _customProperties = store.FindResourcesLive( "PropType", "Custom", 1 );
                _customProperties = _customProperties.Minus( store.FindResources( "PropType",
                    "DataType", (int) PropDataType.Link ) );
                _customProperties.ResourceAdded += OnCustomPropertyAdded;
                _customProperties.ResourceDeleting += OnCustomPropertyDeleting;

                _customPropTypes = new ArrayList();
                foreach( IResource res in _customProperties )
                {
                    int propID = res.GetIntProp( "ID" );
                    _customPropTypes.Add( store.PropTypes [propID] );
                }
            }

            return _customProperties;
        }

	    private static void OnCustomPropertyAdded( object sender, ResourceIndexEventArgs e )
	    {
            int propID = e.Resource.GetIntProp( "ID" );
            _customPropTypes.Add( ICore.Instance.ResourceStore.PropTypes [propID] );
	    }

        private static void OnCustomPropertyDeleting( object sender, ResourceIndexEventArgs e )
        {
            foreach( IPropType propType in _customPropTypes )
            {
                if ( propType.Id == e.Resource.GetIntProp( "ID" ) )
                {
                    _customPropTypes.Remove( propType );
                    break;
                }
            }
        }

	    public static ArrayList GetCustomPropTypes()
	    {
            return _customPropTypes;
	    }

        /// <summary>
        /// Return true if property with such Id represents a custom type.
        /// </summary>
        /// <param name="propID">Id of a property.</param>
        public static bool IsCustomPropType( int propID )
        {
            GetCustomProperties();
            for( int i = 0; i < _customPropTypes.Count; i++ )
            {
                IPropType propType = (IPropType) _customPropTypes [i];
                if ( propType.Id == propID )
                    return true;
            }
            return false;
        }
        #endregion Custom Properties
	}
}
