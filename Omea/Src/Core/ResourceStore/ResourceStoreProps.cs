// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceStore
{
	/// <summary>
	/// Standard properties used by ResourceStore.
	/// </summary>
	internal class ResourceStoreProps
	{
        private ResourceTypeCollection _resourceTypes;
        private PropTypeCollection _propTypes;

        private int _propName = -1;
        private int _propId = -1;
        private int _propDataType = -1;
        private int _propDisplayNameMask = -1;
        private int _propFlags = -1;
        private int _propDisplayName = -1;
        private int _propPropDisplayName = -1;
        private int _propInternal = -1;
        private int _propNoIndex = -1;
        private int _propFileFormat = -1;
        private int _propResourceContainer = -1;
        private int _propCanBeUnread = -1;

		internal ResourceStoreProps( ResourceTypeCollection resTypes, PropTypeCollection propTypes )
		{
            _resourceTypes = resTypes;
            _propTypes = propTypes;
		}

	    internal void Initialize()
	    {
	        // during initial bootstrap - use two-step setting of display name template
	        // during later startups - use correct template from the start to avoid extra ResourceTypes writes
	        string defaultDisplayNameTemplate = "";
	        if ( _propTypes.Exist( "Name" ) && _propTypes.Exist( "PropDisplayName" ) )
	        {
	            defaultDisplayNameTemplate = "PropDisplayName | Name";
	        }

	        bool propTypeNewType, resourceTypeNewType;
	        int resPropType = _resourceTypes.RegisterResourceTypeInternal( "PropType", defaultDisplayNameTemplate,
                ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex, true, out propTypeNewType );
	        int resResourceType = _resourceTypes.RegisterResourceTypeInternal( "ResourceType", defaultDisplayNameTemplate,
                ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex, true, out resourceTypeNewType );

	        bool nameNewType, idNewType, dataTypeNewType, displayNameMaskNewType, flagsNewType,
                propDisplayNameNewType, internalNewType, noIndexNewType, resourceContainerNewType,
                fileFormatNewType, canBeUnreadNewType;
	        _propName = _propTypes.RegisterPropTypeInternal( "Name", PropDataType.String, PropTypeFlags.Normal,
                true, out nameNewType );
	        _propId = _propTypes.RegisterPropTypeInternal( "ID", PropDataType.Int, PropTypeFlags.Internal,
                true, out idNewType );
	        _propDataType = _propTypes.RegisterPropTypeInternal( "DataType", PropDataType.Int, PropTypeFlags.Internal,
                true, out dataTypeNewType );
	        _propDisplayNameMask = _propTypes.RegisterPropTypeInternal( "DisplayNameMask", PropDataType.String,
                PropTypeFlags.Internal, true, out displayNameMaskNewType );
	        _propFlags = _propTypes.RegisterPropTypeInternal( "Flags", PropDataType.Int, PropTypeFlags.Internal,
                true, out flagsNewType );
	        _propPropDisplayName = _propTypes.RegisterPropTypeInternal( "PropDisplayName", PropDataType.String, PropTypeFlags.Internal,
                true, out propDisplayNameNewType );
            _propInternal = _propTypes.RegisterPropTypeInternal( "Internal", PropDataType.Int, PropTypeFlags.Internal,
                true, out internalNewType );
            _propNoIndex = _propTypes.RegisterPropTypeInternal( "NoIndex", PropDataType.Int, PropTypeFlags.Internal,
                true, out noIndexNewType );
            _propFileFormat = _propTypes.RegisterPropTypeInternal( "FileFormat", PropDataType.Int, PropTypeFlags.Internal,
                true, out fileFormatNewType );
            _propResourceContainer = _propTypes.RegisterPropTypeInternal( "ResourceContainer", PropDataType.Int, PropTypeFlags.Internal,
                true, out resourceContainerNewType );
            _propCanBeUnread = _propTypes.RegisterPropTypeInternal( "CanBeUnread", PropDataType.Bool, PropTypeFlags.Internal,
                true, out canBeUnreadNewType );

	        _resourceTypes.CreateOrUpdateResourceTypeResource( "PropType", "PropType", defaultDisplayNameTemplate,
                ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex, null, resPropType, propTypeNewType );
	        _resourceTypes.CreateOrUpdateResourceTypeResource( "ResourceType", "ResourceType", defaultDisplayNameTemplate,
                ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex, null, resResourceType, resourceTypeNewType );

	        _propTypes.CreateOrUpdatePropTypeResource( "Name", PropDataType.String, PropTypeFlags.Normal,
                null, _propName, nameNewType );
	        _propTypes.CreateOrUpdatePropTypeResource( "ID", PropDataType.Int, PropTypeFlags.Internal,
                null, _propId, idNewType );
	        _propTypes.CreateOrUpdatePropTypeResource( "DataType", PropDataType.Int, PropTypeFlags.Internal,
                null, _propDataType, dataTypeNewType );
	        _propTypes.CreateOrUpdatePropTypeResource( "DisplayNameMask", PropDataType.String, PropTypeFlags.Internal,
                null, _propDisplayNameMask, displayNameMaskNewType );
	        _propTypes.CreateOrUpdatePropTypeResource( "Flags", PropDataType.Int, PropTypeFlags.Internal,
                null, _propFlags, flagsNewType );
	        _propTypes.CreateOrUpdatePropTypeResource( "PropDisplayName", PropDataType.String, PropTypeFlags.Internal,
                null, _propPropDisplayName, propDisplayNameNewType );
            _propTypes.CreateOrUpdatePropTypeResource( "Internal", PropDataType.Int, PropTypeFlags.Internal,
                null, _propInternal, internalNewType );
            _propTypes.CreateOrUpdatePropTypeResource( "NoIndex", PropDataType.Int, PropTypeFlags.Internal,
                null, _propNoIndex, noIndexNewType );
            _propTypes.CreateOrUpdatePropTypeResource( "FileFormat", PropDataType.Int, PropTypeFlags.Internal,
                null, _propFileFormat, fileFormatNewType );
            _propTypes.CreateOrUpdatePropTypeResource( "ResourceContainer", PropDataType.Int, PropTypeFlags.Internal,
                null, _propResourceContainer, resourceContainerNewType );
            _propTypes.CreateOrUpdatePropTypeResource( "CanBeUnread", PropDataType.Bool, PropTypeFlags.Internal,
                null, _propCanBeUnread, canBeUnreadNewType );

	        _propDisplayName = _propTypes.Register( "_DisplayName", PropDataType.String, PropTypeFlags.Internal );

	        _propTypes.Register( "Custom", PropDataType.Int, PropTypeFlags.Internal );
	        _propTypes.Register( "ReverseDisplayName", PropDataType.String, PropTypeFlags.Internal );
	        _propTypes.Register( "OwnerPluginList", PropDataType.StringList, PropTypeFlags.Internal );

            if ( defaultDisplayNameTemplate == "" )
            {
                _resourceTypes ["ResourceType"].ResourceDisplayNameTemplate = "PropDisplayName | Name";
                _resourceTypes ["PropType"].ResourceDisplayNameTemplate = "PropDisplayName | Name";
            }

	        ResourceRestrictions.RegisterTypes();

	        MyPalStorage.Storage.RegisterUniqueRestriction( "ResourceType", _propName );
	        MyPalStorage.Storage.RegisterUniqueRestriction( "PropType", _propName );
	        MyPalStorage.Storage.RegisterUniqueRestriction( "PropType", _propId );
	    }

	    public int Name            { get { return _propName; } }
        public int TypeId          { get { return _propId; } }
        public int DataType        { get { return _propDataType; } }
        public int DisplayNameMask { get { return _propDisplayNameMask; } }
        public int Flags           { get { return _propFlags; } }
        public int DisplayName     { get { return _propDisplayName; } }
        public int PropDisplayName { get { return _propPropDisplayName; } }
	}
}
