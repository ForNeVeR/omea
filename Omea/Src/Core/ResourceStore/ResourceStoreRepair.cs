// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Database;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceStore
{
	/// <summary>
	/// The class which performs automatic diagnostics and repair of the resource store.
	/// </summary>
	public class ResourceStoreRepair
	{
        private IDatabase _db;
        private ITable _propTypes;
        private ITable _resTypes;
        private ITable _intProps;
        private ITable _stringProps;
        private ITable _longStringProps;
        private ITable _dateProps;
        private ITable _doubleProps;
        private ITable _blobProps;
        private ITable _boolProps;
        private ITable _resources;
        private ITable _links;
        private ITable _stringListProps;

        private int _errorCount = 0;
        private int _fixCount   = 0;
        private int _resCount = 0, _propCount = 0, _linkCount = 0;
        private bool _fixErrors;
        private bool _dumpStructure;

        private IntHashTable _propTypeMap   = new IntHashTable(); // property type ID -> name
        private HashSet      _propTypeNames = new HashSet();
        private IntHashTable _propDataTypes = new IntHashTable();
        private HashSet      _resTypeIDs    = new HashSet();
        private Hashtable    _resTypeNames  = new Hashtable();
        private Exception    _repairException = null;

	    public event RepairProgressEventHandler RepairProgress;

		public ResourceStoreRepair( IDatabase db )
		{
            _db = db;
		}

        private void ShowProgress( string message, params object[] args )
        {
            string result = message;
            if ( args.Length > 0 )
            {
                result = String.Format( message, args );
            }
            if ( RepairProgress != null )
            {
                RepairProgress( this, new RepairProgressEventArgs( result ) );
            }
        }

        private void ReportError( string msg )
        {
            if ( _errorCount > 100 && !_fixErrors )
                return;

            ShowProgress( msg );
            if ( _errorCount == 100 && !_fixErrors )
            {
                Console.WriteLine( "Too many errors found, stopping further report" );
            }
            _errorCount++;
        }

        public bool FixErrors
        {
            get { return _fixErrors; }
            set { _fixErrors = value; }
        }

	    public bool DumpStructure
	    {
	        get { return _dumpStructure; }
	        set { _dumpStructure = value; }
	    }

	    public Exception RepairException
	    {
	        get { return _repairException; }
	    }

	    public void Run()
        {
            _propTypes       = _db.GetTable( "PropTypes" );
            _resTypes        = _db.GetTable( "ResourceTypes" );
            _intProps        = _db.GetTable( "IntProps" );
            _stringProps     = _db.GetTable( "StringProps" );
            _longStringProps = _db.GetTable( "LongStringProps" );
            _stringListProps = _db.GetTable( "StringListProps" );
            _dateProps       = _db.GetTable( "DateProps" );
            _blobProps       = _db.GetTable( "BlobProps" );
            _doubleProps     = _db.GetTable( "DoubleProps" );
            _boolProps       = _db.GetTable( "BoolProps" );
            _resources       = _db.GetTable( "Resources" );
            _links           = _db.GetTable( "Links" );

            try
            {
                ShowProgress( "Processing property types..." );
                RepairPropTypes();
                ShowProgress( "Processing resource types..." );
                RepairResourceTypes();

                ShowProgress( "Processing resources..." );
                RepairResources();

                ShowProgress( "Processing IntProps..." );
                RepairProps( _intProps, PropDataType.Int );
                ShowProgress( "Processing StringProps..." );
                RepairProps( _stringProps, PropDataType.String );
                ShowProgress( "Processing LongStringProps..." );
                RepairProps( _longStringProps, PropDataType.LongString );
                ShowProgress( "Processing DateProps..." );
                RepairProps( _dateProps, PropDataType.Date );
                ShowProgress( "Processing BlobProps..." );
                RepairProps( _blobProps, PropDataType.Blob );
                ShowProgress( "Processing DoubleProps..." );
                RepairProps( _doubleProps, PropDataType.Double );
                ShowProgress( "Processing StringListProps..." );
                RepairProps( _stringListProps, PropDataType.StringList );
                ShowProgress( "Processing BoolProps..." );
                RepairProps( _boolProps, PropDataType.Bool );
                ShowProgress( "Processing links..." );
                RepairLinks();

                if (_fixErrors)
                {
                    ShowProgress("Repairing BlobFileSystem...");
                    _db.RepairBlobFileSystem();
                }
            }
            catch( Exception e )
            {
                ShowProgress( "Fatal error during repair: " + e.Message );
                _repairException = e;
                _errorCount++;
            }

            ShowProgress( "Closing database..." );
            _db.Shutdown();

            if ( _errorCount <= _fixCount )
            {
                MyPalStorage.OpenDatabase();
                try
                {
                    RepairRestrictions();

                    MyPalStorage.CloseDatabase();
                }
                catch( StorageException e )
                {
                    ShowProgress( e.ToString() );
                }
                finally
                {
                    ShowProgress( "done." );
                }
            }
            else
            {
                ShowProgress( "Link restrictions were not checked because errors were found on earlier stages" );
            }

            ShowProgress( "Processed {0} resources, {1} properties and {2} links",
                _resCount, _propCount, _linkCount );
            if ( _errorCount == 0 )
            {
                ShowProgress( "No errors found" );
            }
            else
            {
                ShowProgress( "{0} errors found, {1} errors fixed" , _errorCount, _fixCount );
            }
        }

        private void RepairPropTypes()
        {
            int propID       = GetPropId( "ID" );
            int propName     = GetPropId( "Name" );
            int propDataType = GetPropId( "DataType" );
            int propFlags    = GetPropId( "Flags" );
            int typePropType = GetResourceTypeId( "PropType" );

            bool needRebuildIndexes = false;
            if ( _propTypes.PeekNextID() > 65536 )
            {
                needRebuildIndexes = true;
            }

            using( IResultSet rs = _propTypes.CreateResultSet( 0 ) )
            {
                foreach( IRecord rec in rs )
                {
                    int typeId       = rec.GetIntValue( 0 );
                    string name  = rec.GetStringValue( 1 );
                    int dataType = rec.GetIntValue( 2 );
                    int flags    = rec.GetIntValue( 3 );

                    if ( typeId < 0 || typeId > 65536 )
                    {
                        ReportError( "Invalid property type ID " + typeId );
                        if ( _fixErrors )
                        {
                            rec.Delete();
                            _fixCount++;
                            needRebuildIndexes = true;
                            continue;
                        }
                    }

                    if ( _propTypeMap.Contains( typeId ) )
                    {
                        ReportError( "Duplicate property ID " + typeId );
                    }
                    if ( _propTypeNames.Contains( name ) )
                    {
                        ReportError( "Duplicate property name " + name );
                    }
                    _propTypeMap.Add( typeId, name );
                    _propTypeNames.Add( name );
                    _propDataTypes [typeId] = dataType;

                    if ( _dumpStructure )
                    {
                        ShowProgress( "Property type {0}, name {1}, dataType {2}, flags {3}",
                            typeId, name, dataType, (PropTypeFlags) flags );
                    }

                    int resID;
                    try
                    {
                        resID = FindResource( typePropType, _stringProps, propName, name );
                    }
                    catch( Exception e )
                    {
                        ReportError( "Property " + name + ": " + e.Message );
                        continue;
                    }
                    if ( resID == -1 )
                    {
                        ReportError( "Could not find matching resource for property name " + name );
                        if ( _fixErrors )
                        {
                            try
                            {
                                int resourceId = CreateResource( typePropType );
                                CreatePropValue( _stringProps, resourceId, propName, name );
                                CreatePropValue( _intProps, resourceId, propID, typeId );
                                CreatePropValue( _intProps, resourceId, propDataType, dataType );
                                CreatePropValue( _intProps, resourceId, propFlags, flags );
                                _fixCount++;
                            }
                            catch( Exception e )
                            {
                                ReportError( "Failed to create property type resource: " + e.Message );
                            }
                        }
                        continue;
                    }

                    object idValue = GetPropValueSafe( _intProps, resID, propID );
                    if ( idValue == null )
                    {
                        ReportError( "ID property not found for property " + name );
                    }
                    else if ( (int) idValue != typeId )
                    {
                        ReportError( "ID property for property " + name + " does not match ID value" );
                        if ( _fixErrors )
                        {
                            Trace.WriteLine( "Set ID property of PropType resource " + name + " to " + typeId );
                            UpdatePropValue( _intProps, resID, propID, typeId );
                            _fixCount++;
                        }
                    }

                    object dataTypeValue = GetPropValueSafe( _intProps, resID, propDataType );
                    if ( dataTypeValue == null )
                    {
                        ReportError( "DataType property not found for property " + name );
                    }
                    else if ( (int) dataTypeValue != dataType )
                    {
                        ReportError( "DataType property for property " + name + " does not match Type value" );
                    }

                    object flagsValue = GetPropValueSafe( _intProps, resID, propFlags );
                    if ( flagsValue == null )
                    {
                        ReportError( "Flags property not found for property " + name );
                    }
                    else if ( (int) flagsValue != flags )
                    {
                        ReportError( "Flags property for property " + name + " does not match Flags value. Fixing..." );
                        UpdatePropValue( _intProps, resID, propFlags, flags );
                        _fixCount++;
                    }
                }
            }

            if ( needRebuildIndexes )
            {
                _propTypes.RebuildIndexes( true );
            }
        }

        private void RepairResourceTypes()
        {
            int propID      = GetPropId( "ID" );
            int propName    = GetPropId( "Name" );
            int propDNMask  = GetPropId( "DisplayNameMask" );
            int typeResType = GetResourceTypeId( "ResourceType" );

            bool needRebuildIndexes = false;
            if ( _resTypes.PeekNextID() > 65536 )
            {
                needRebuildIndexes = true;
            }

            using( IResultSet rs = _resTypes.CreateResultSet( 0 ) )
            {
                foreach( IRecord rec in rs )
                {
                    int typeId             = rec.GetIntValue( 0 );
                    string name            = rec.GetStringValue( 1 );
                    string displayNameMask = rec.GetStringValue( 2 );

                    if ( _dumpStructure )
                    {
                        ShowProgress( "Resource type {0}, name {1}, displayNameMask {2}",
                            typeId, name, displayNameMask );
                    }

                    if ( typeId < 0 || typeId > 65536 )
                    {
                        ReportError( "Invalid resource type ID " + typeId );
                        if ( _fixErrors )
                        {
                            rec.Delete();
                            _fixCount++;
                            needRebuildIndexes = true;
                            continue;
                        }
                    }

                    if ( _resTypeIDs.Contains( typeId ) )
                    {
                        ReportError( "Duplicate resource type ID " + typeId );
                        if ( _fixErrors )
                        {
                            rec.Delete();
                            _fixCount++;
                            needRebuildIndexes = true;
                        }
                        continue;
                    }
                    if ( _resTypeNames.ContainsValue( name ) )
                    {
                        ReportError( "Duplicate resource type name " + name );
                        if ( _fixErrors )
                        {
                            rec.Delete();
                            _fixCount++;
                            needRebuildIndexes = true;
                        }
                        continue;
                    }
                    _resTypeIDs.Add( typeId );
                    _resTypeNames.Add( typeId, name );

                    int resID;
                    try
                    {
                        resID = FindResource( typeResType, _stringProps, propName, name );
                    }
                    catch( Exception e )
                    {
                        ReportError( "Resource type " + name + ": " + e.Message );
                        continue;
                    }
                    if ( resID == -1 )
                    {
                        ReportError( "Could not find matching resource for resource type name " + name );
                        if ( _fixErrors )
                        {
                            try
                            {
                                int resourceId = CreateResource( typeResType );
                                CreatePropValue( _stringProps, resourceId, propName, name );
                                CreatePropValue( _intProps, resourceId, propID, typeId );
                                CreatePropValue( _stringProps, resourceId, propDNMask, displayNameMask );
                                // we don't restore the flags, but they'll be reregistered on next run of Omea
                                _fixCount++;
                            }
                            catch( Exception e )
                            {
                                ReportError( "Failed to create resource type resource: " + e.Message );
                            }
                        }
                        continue;
                    }

                    object idValue = GetPropValueSafe( _intProps, resID, propID );
                    if ( idValue == null )
                    {
                        ReportError( "ID property not found for resource type " + name );
                    }
                    else if ( (int) idValue != typeId )
                    {
                        ReportError( "ID property for resource type " + name + " does not match ID value" );
                    }

                    object dnMaskValue = GetPropValueSafe( _stringProps, resID, propDNMask );
                    if ( dnMaskValue == null )
                    {
                        ReportError( "DisplayNameMask property not found for resource type " + name );
                        if ( _fixErrors )
                        {
                            CreatePropValue( _stringProps, resID, propDNMask, displayNameMask );
                            _fixCount++;
                        }
                    }
                    else if ( (string) dnMaskValue != displayNameMask )
                    {
                        ReportError( "DisplayNameMask property for resource type " + name + " does not match DisplayNameMask value. Fixing..." );
                        if ( _fixErrors )
                        {
                            UpdatePropValue( _stringProps, resID, propDNMask, displayNameMask );
                            _fixCount++;
                        }
                    }
                }
            }

            if ( needRebuildIndexes )
            {
                _resTypes.RebuildIndexes( true );
            }
        }

        private void RepairResources()
        {
            int maxResID = -1;
            HashSet resIDs = new HashSet();
            using( IResultSet rs = _resources.CreateResultSet( 0 ) )
            {
                foreach( IRecord rec in rs )
                {
                    _resCount++;

                    int resID = rec.GetIntValue( 0 );
                    int typeID = rec.GetIntValue( 1 );

                    if ( resID > maxResID )
                    {
                        maxResID = resID;
                    }

                    if ( _dumpStructure )
                    {
                        string typeName = (string) _resTypeNames [typeID];
                        if ( typeName == null )
                            typeName = Convert.ToString( typeID );
                        ShowProgress( "Resource " + resID + " of type " + typeName );
                    }

                    if ( !_resTypeIDs.Contains( typeID ) )
                    {
                        ReportError( "Found a resource of a non-existing type " + typeID );
                        if ( _fixErrors )
                        {
                            rec.Delete();
                            _fixCount++;
                        }
                        continue;
                    }
                    if ( resIDs.Contains( resID ) )
                    {
                        ReportError( "Duplicate resource ID " + resID );
                        if ( _fixErrors )
                        {
                            rec.Delete();
                            _fixCount++;
                        }
                        continue;
                    }
                    resIDs.Add( resID );
                }
            }

            int nextID = _resources.NextID();
            if ( nextID <= maxResID )
            {
                ReportError( "Next ID for table Resources " + nextID + " is smaller than maximum resource ID " + maxResID );
            }
        }

        private void RepairProps( ITable propTable, PropDataType dataType )
        {
            HashSet resPropTypes = new HashSet();
            int lastResID = -1;

            using( IResultSet rs = propTable.CreateResultSet( 0 ) )
            {
                IEnumerator enumerator = rs.GetEnumerator();
                try
                {
                    while( enumerator.MoveNext() )
                    {
                        IRecord rec;
                        try
                        {
                            rec = (IRecord) enumerator.Current;
                        }
                        catch( AttemptReadingDeletedRecordException )
                        {
                            ReportError( "Deleted record found in index for " + dataType + " property table" );
                            continue;
                        }

                        _propCount++;

                        int resID = rec.GetIntValue( 0 );
                        int propType = rec.GetIntValue( 1 );

                        if ( resID != lastResID )
                        {
                            lastResID = resID;
                            resPropTypes.Clear();
                        }

                        IRecord resRec = _resources.GetRecordByEqual( 0, resID );
                        if ( resRec == null )
                        {
                            ReportError( "Found a property of a non-existing resource " + resID );
                            if ( _fixErrors )
                            {
                                rec.Delete();
                                _fixCount++;
                            }
                            continue;
                        }

                        if ( !_propTypeMap.Contains( propType ) )
                        {
                            ReportError( "Found a property with an invalid type " + propType );
                            if ( _fixErrors )
                            {
                                rec.Delete();
                                _fixCount++;
                            }
                            continue;
                        }
                        if ( (int) _propDataTypes [propType] != (int) dataType )
                        {
                            ReportError( "Type of property " + propType + " does not match type of table " + dataType );
                            if ( _fixErrors )
                            {
                                rec.Delete();
                                _fixCount++;
                            }
                            continue;
                        }

                        string propTypeName = (string) _propTypeMap [propType];

                        if ( dataType != PropDataType.StringList && resPropTypes.Contains( propType ) )
                        {
                            ReportError( "Duplicate property " + propTypeName + " of resource " + resID );
                            if ( _fixErrors )
                            {
                                rec.Delete();
                                _fixCount++;
                            }
                            continue;
                        }

                        if (dataType == PropDataType.Blob)
                        {
                            IBLOB blob = rec.GetBLOBValue(2);
                            Stream stream;
                            try
                            {
                                stream = blob.Stream;
                            }
                            catch (IOException)
                            {
                                ReportError("Missing blob stream for property " + propTypeName + " of resource " + resID);
                                if (_fixErrors)
                                {
                                    rec.Delete();
                                    _fixCount++;
                                }
                                continue;
                            }
                            try
                            {
                                long length = stream.Length;
                                byte[] buffer = new byte[4096];
                                for (long bytesRead = 0; bytesRead < length; bytesRead += 4096)
                                {
                                    int bytesToRead = Math.Min(4096, (int)(length - bytesRead));
                                    stream.Read(buffer, 0, bytesToRead);
                                }
                            }
                            catch (IOException)
                            {
                                ReportError("Failed to read blob stream for property " + propTypeName + " of resource " + resID);
                                if (_fixErrors)
                                {
                                    rec.Delete();
                                    _fixCount++;
                                }
                            }
                            stream.Close();
                        }
                        else if (dataType == PropDataType.String || dataType == PropDataType.LongString)
                        {
                            try
                            {
                                rec.GetStringValue(2);
                            }
                            catch (IOException ex)
                            {
                                ReportError("Failed to read string value for property " + propTypeName + " of resource " + resID);
                                if (_fixErrors)
                                {
                                    rec.Delete();
                                    _fixCount++;
                                }
                            }
                        }

                        resPropTypes.Add( propType );
                    }
                }
                finally
                {
                    IDisposable disp = enumerator as IDisposable;
                    if ( disp != null )
                    {
                        disp.Dispose();
                    }
                }
            }
        }

        private class LinkData
        {
            int _id1;
            int _id2;
            int _linkType;

            internal LinkData( int id1, int id2, int linkType )
            {
                _id1 = id1;
                _id2 = id2;
                _linkType = linkType;
            }

            public override bool Equals( object obj )
            {
                if ( !(obj is LinkData) )
                    return false;

                LinkData other = (LinkData) obj;
                return _id1 == other._id1 && _id2 == other._id2 && _linkType == other._linkType;
            }

            public override int GetHashCode()
            {
                return _id1 ^ _id2 ^ (_linkType << 16);
            }
        }

        private void RepairLinks()
        {
            HashSet _existingLinks = new HashSet();
            using( IResultSet rs = _links.CreateResultSet( 0 ) )
            {
                foreach( IRecord rec in rs )
                {
                    _linkCount++;

                    int id1 = rec.GetIntValue( 0 );
                    int id2 = rec.GetIntValue( 1 );
                    int propType = rec.GetIntValue( 2 );

                    if ( !_propTypeMap.Contains( propType ) )
                    {
                        ReportError( "Found a link with an invalid type " + propType );
                        if ( _fixErrors )
                        {
                            rec.Delete();
                            _fixCount++;
                        }
                        continue;
                    }

                    string propTypeName = (string) _propTypeMap [propType];

                    if ( id1 == id2 )
                    {
                        ReportError( "Found a link of type " + propTypeName + " of resource " + id1 + " to itself" );
                        if ( _fixErrors )
                        {
                            rec.Delete();
                            _fixCount++;
                        }
                        continue;
                    }

                    if ( _existingLinks.Contains( new LinkData( id2, id1, propType ) ) )
                    {
                        ReportError( "Found a recursive link of type " + propTypeName + " between " + id1 + " and " + id2 );
                        if ( _fixErrors )
                        {
                            rec.Delete();
                            _fixCount++;
                        }
                        continue;
                    }

                    LinkData linkData = new LinkData( id1, id2, propType );
                    if ( _existingLinks.Contains( linkData ) )
                    {
                        ReportError( "Found a duplicate link of type " + propTypeName + " between " + id1 + " and " + id2 );
                        if ( _fixErrors )
                        {
                            rec.Delete();
                            _fixCount++;
                        }
                        continue;
                    }
                    _existingLinks.Add( linkData );

                    IRecord resRec = _resources.GetRecordByEqual( 0, id1 );
                    if ( resRec == null )
                    {
                        ReportError( "Found a link of type " + propTypeName + " of a non-existing resource " + id1 );
                        if ( _fixErrors )
                        {
                            rec.Delete();
                            _fixCount++;
                        }
                        continue;
                    }

                    resRec = _resources.GetRecordByEqual( 0, id2 );
                    if ( resRec == null )
                    {
                        ReportError( "Found a link of type " + propTypeName + " of a non-existing resource " + id2 );
                        if ( _fixErrors )
                        {
                            rec.Delete();
                            _fixCount++;
                        }
                        continue;
                    }

                    if ( (int) _propDataTypes [propType] != (int) PropDataType.Link )
                    {
                        ReportError( "Non-link property " + propTypeName + " found in Links table for resources " + id1 + " and " + id2 );
                        if ( _fixErrors )
                        {
                            rec.Delete();
                            _fixCount++;
                        }
                        continue;
                    }
                }
            }
        }

        public void RepairRestrictions()
        {
            ShowProgress( "Processing link restrictions" );
            RepairLinkRestrictions();

            ShowProgress( "Processing unique property value restrictions" );
            RepairUniqueRestrictions();
        }

        private void RepairLinkRestrictions()
        {
            IResourceStore store = MyPalStorage.Storage;
            IResourceList restrictionsList = store.GetAllResources( "LinkRestriction" );
            HashSet involvedResTypes = new HashSet();

            PropTypeCollection propTypes = (PropTypeCollection) MyPalStorage.Storage.PropTypes;
            foreach( IResource lr in restrictionsList )
            {
                int linkType = lr.GetIntProp( "LinkType" );
                if ( !propTypes.IsValidType( linkType ) )
                {
                    lr.Delete();
                }
                else
                {
                    string fromResourceType = lr.GetStringProp( "fromResourceType" );
                    if ( fromResourceType != null )
                    {
                        involvedResTypes.Add( fromResourceType );
                    }
                }
            }

            restrictionsList = store.GetAllResources( "LinkRestriction" );

            foreach( HashSet.Entry E in involvedResTypes )
            {
                string resType = (string) E.Key;
                ShowProgress( "Checking link restrictions for resources of type '{0}'...", resType );

                IResourceList resources = store.GetAllResources( resType );
                foreach( IResource resource in resources )
                {
                    foreach( IResource lr in restrictionsList )
                    {
                        if( lr.GetStringProp( "fromResourceType" ) == resource.Type )
                        {
                            RepairLinkRestriction( lr, resource );
                        }
                    }
                }
            }
        }

	    private void RepairLinkRestriction( IResource lr, IResource resource )
	    {
	        IResourceStore store = MyPalStorage.Storage;
            string toResourceType = lr.GetStringProp( "toResourceType" );
            string resName = !String.IsNullOrEmpty( resource.DisplayName ) ? resource.DisplayName : "<empty>";
	        int linkType = lr.GetIntProp( "LinkType" );
	        int minCount = lr.GetIntProp( "MinCount" );
	        int maxCount = lr.GetIntProp( "MaxCount" );
            string linkTypeName = (string) _propTypeMap [linkType];

	        IResourceList links;
            int linkTypeReverse;
	        if ( store.PropTypes [linkType].HasFlag( PropTypeFlags.DirectedLink) )
	        {
	            links = resource.GetLinksFrom( null, linkType );
                linkTypeReverse = -linkType;
	        }
	        else
	        {
	            links = resource.GetLinksOfType( null, linkType );
                linkTypeReverse = linkType;
	        }

	        /**
             * check destination resource types
             */
	        if( toResourceType != null )
	        {
	            foreach( IResource link in links )
	            {
	                if( link.Type != toResourceType )
	                {
	                    ReportError( "Restricted link found: resource ID=" + resource.Id +
	                        " [" + resName + "] link [" + linkTypeName + "] destination resource type=[" + link.Type + "]" );
	                    if( _fixErrors )
	                    {
                            if ( store.GetMinLinkCountRestriction( resource.Type, linkType ) > resource.GetLinkCount( linkType ) &&
                                store.GetMinLinkCountRestriction( link.Type, linkTypeReverse ) > link.GetLinkCount( linkTypeReverse ) )
                            {
                                resource.DeleteLink( linkType, link );
                                ShowProgress( "Link deleted" );
                                ++_fixCount;
                            }
                            else
                            {
                                ShowProgress( "Can not delete link due to min/max restrictions: {0}/{1} in store vs {2}/{3} in resource",
                                              store.GetMinLinkCountRestriction( resource.Type, linkType ).ToString(),
                                              store.GetMinLinkCountRestriction( link.Type, linkTypeReverse ).ToString(),
                                              resource.GetLinkCount( linkType ).ToString(),
                                              link.GetLinkCount( linkTypeReverse ).ToString() );
                                //  NB: rough hack. To be deleted after the problem is identified
                                //      in the source code.
                                if( resource.Type == "ContactName" )
                                {
                                    resource.Delete();
                                }
                            }
	                    }
	                }
	            }
	        }
	        /**
             * check counts
             */

            if( links.Count < minCount )
	        {
	            ReportError( String.Format( "Not enough links of type {3} for resource '{2}' {0} minimum, {1} found",
	                                        minCount, links.Count, resource, linkTypeName ) );
	            if( _fixErrors && IsSafeToDeleteResource( resource ) )
	            {
                    resource.Delete();
                    ++_fixCount;
	            }
	        }

	        if( maxCount >= 0 && links.Count > maxCount )
	        {
	            ReportError( String.Format( "Too many links of type {3} for resource '{2}': {0} maximum, {1} found",
	                                        maxCount, links.Count, resource, linkTypeName ) );
	            if( _fixErrors )
	            {
	                int linkCount = links.Count;
	                resource.BeginUpdate();
	                for( int i = links.Count-1; i >= 0 && linkCount > maxCount; i-- )
	                {
	                    IResource target = links [i];
	                    // make sure we don't corrupt the DB by deleting the link
	                    if ( target.GetLinksOfType( null, linkTypeReverse ).Count > store.GetMinLinkCountRestriction( target.Type, linkTypeReverse ) )
	                    {
	                        ShowProgress( "Deleting link to resource " + links [i] );
	                        resource.DeleteLink( linkType, links[ i ] );
	                        linkCount--;
	                    }
	                }
	                resource.EndUpdate();
	                ++_fixCount;
	            }
	        }
	    }

        /**
         * Checks if deleting the specified resource will break minimum link count
         * restrictions on linked resources.
         */

        private bool IsSafeToDeleteResource( IResource res )
        {
            foreach( int linkType in res.GetLinkTypeIds() )
            {
                IResourceList links = res.GetLinksOfType( null, linkType );
                for( int i=0; i<links.Count; i++ )
                {
                    IResource target = MyPalStorage.Storage.TryLoadResource( links.ResourceIds [i] );
                    if ( target == null )
                    {
                        continue;
                    }

                    int minCount = MyPalStorage.Storage.GetMinLinkCountRestriction( target.Type, linkType );
                    if ( minCount > 0 && target.GetLinkCount( linkType ) == minCount )
                    {
                        return false;
                    }
                }
            }
            return true;
        }

	    private void RepairUniqueRestrictions()
        {
            IResourceStore store = MyPalStorage.Storage;
            IResourceList restrictionsList = store.GetAllResources( "UniqueRestriction" );
            HashSet involvedResTypes = new HashSet();

            PropTypeCollection propTypes = (PropTypeCollection) MyPalStorage.Storage.PropTypes;
            foreach( IResource lr in restrictionsList )
            {
                int uniquePropId = lr.GetIntProp( "UniquePropId" );
                if ( !propTypes.IsValidType( uniquePropId ) )
                {
                    lr.Delete();
                }
                else
                {
                    string fromResourceType = lr.GetStringProp( "fromResourceType" );
                    if ( fromResourceType != null )
                    {
                        involvedResTypes.Add( fromResourceType );
                    }
                }
            }

            restrictionsList = store.GetAllResources( "UniqueRestriction" );

            foreach( HashSet.Entry E in involvedResTypes )
            {
                string resType = (string) E.Key;
                ShowProgress( "Checking unique restrictions for resources of type '{0}'...", resType );

                IResourceList resources = store.GetAllResources( resType );
                HashMap propValues = new HashMap();

                foreach( IResource resource in resources )
                {
                    IntHashSet propIds = new IntHashSet();
                    foreach( IResource ur in restrictionsList )
                    {
                        if( ur.GetStringProp( "fromResourceType" ) == resType )
                        {
                            int propId = ur.GetIntProp( "UniquePropId" );
                            if ( propIds.Contains( propId ) )  // do not process duplicate restrictions
                            {
                                continue;
                            }
                            propIds.Add( propId );
                            object propValue = resource.GetProp( propId );
                            if ( propValue != null )
                            {
                                HashSet propValueSet = (HashSet) propValues [propId];
                                if ( propValueSet == null )
                                {
                                    propValueSet = new HashSet();
                                    propValues [propId] = propValueSet;
                                }
                                if ( propValueSet.Contains( propValue ) )
                                {
                                    ReportError( "Unique property value restriction violated: resource ID="
                                        + resource.Id + " property " + store.PropTypes [propId].Name + ", value " + propValue );
                                    if( _fixErrors && IsSafeToDeleteResource( resource ) )
                                    {
                                        resource.Delete();
                                        ++_fixCount;
                                    }
                                }
                                propValueSet.Add( propValue );
                            }
                        }
                    }
                }
            }
        }

        private int FindResource( int typeID, ITable propTable, int propID, object propValue )
        {
            int foundID = -1;
            using( IResultSet rs = propTable.CreateResultSet( 1, propID, 2, propValue, false ) )
            {
                foreach( IRecord rec in rs )
                {
                    int id = rec.GetIntValue( 0 );
                    IRecord resRec = _resources.GetRecordByEqual( 0, id );
                    if ( resRec != null )
                    {
                        if ( resRec.GetIntValue( 1 ) == typeID )
                        {
                            foundID = id;
                            break;
                        }
                    }
                }
            }
            return foundID;
        }

        private int GetPropId( string name )
        {
            IRecord recID = _propTypes.GetRecordByEqual( 1, name );
            if ( recID == null )
            {
                throw new Exception( "Fatal error: '" + name + "' property not found" );
            }
            return recID.GetIntValue( 0 );
        }

        private int GetResourceTypeId( string name )
        {
            IRecord recPropType = _resTypes.GetRecordByEqual( 1, name );
            if ( recPropType == null )
            {
                throw new Exception( "Fatal error: '" + name + "' resource type not found" );
            }
            return recPropType.GetIntValue( 0 );
        }

        private object GetPropValueSafe( ITable propTable, int resId, int propId )
        {
            using( IResultSet rs = propTable.CreateResultSet( 0, resId, 1, propId, true ) )
            {
                foreach( IRecord rec in rs )
                {
                    return rec.GetValue( 2 );
                }
                return null;
            }
        }

        private void UpdatePropValue( ITable propTable, int resID, int propID, object value )
        {
            using( IResultSet rs = propTable.CreateResultSet( 0, resID, 1, propID, false ) )
            {
                bool hasDuplicates = false;
                foreach( IRecord rec in rs )
                {
                    if( hasDuplicates )
                    {
                        rec.Delete();
                    }
                    else
                    {
                        hasDuplicates = true;
                        rec.SetValue( 2, value );
                        rec.Commit();
                    }
                }
            }
        }

        private void CreatePropValue( ITable propTable, int resID, int propID, object value )
        {
            IRecord rec = propTable.NewRecord();
            rec.SetValue( 0, IntInternalizer.Intern( resID ) );
            rec.SetValue( 1, IntInternalizer.Intern( propID ) );
            rec.SetValue( 2, value );
            rec.Commit();
        }

        private int CreateResource( int resourceTypeId )
        {
            IRecord rec = _resources.NewRecord();
            rec.SetValue( 1, resourceTypeId );
            rec.Commit();
            return rec.GetID();
        }
	}

    public class RepairProgressEventArgs: EventArgs
    {
        private string _message;

        public RepairProgressEventArgs( string message )
        {
            _message = message;
        }

        public string Message
        {
            get { return _message; }
        }
    }

    public delegate void RepairProgressEventHandler( object sender, RepairProgressEventArgs e );
}
