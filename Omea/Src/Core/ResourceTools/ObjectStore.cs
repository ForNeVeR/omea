// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceTools
{
    public class ObjectStore
    {
        private ObjectStore()
        {
            _store = Core.ResourceStore;
            _resourceAP = Core.ResourceAP;
            _store.ResourceTypes.Register( _sectionRes,
                string.Empty, ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal );
            _store.ResourceTypes.Register( _valueRes,
                string.Empty, ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal );
            _propIntValue = _store.PropTypes.Register( "IntValue", PropDataType.Int, PropTypeFlags.Internal );
            _propBoolValue = _store.PropTypes.Register( "BoolValue", PropDataType.Bool, PropTypeFlags.Internal );
            _propDateValue = _store.PropTypes.Register( "DateValue", PropDataType.Date, PropTypeFlags.Internal );
            _propStringValue = _store.PropTypes.Register( "StringValue", PropDataType.String, PropTypeFlags.Internal );
        }

        #region Public Interface
        public static int ReadInt( string section, string key, int defaultValue )
        {
            return Instance.ReadIntImpl( section, key, defaultValue );
        }

        public static bool ReadBool( string section, string key, bool defaultValue )
        {
            return Instance.ReadBoolImpl( section, key, defaultValue );
        }

        public static DateTime ReadDate( string section, string key, DateTime defaultValue )
        {
            return Instance.ReadDateImpl( section, key, defaultValue );
        }

        public static string ReadString( string section, string key )
        {
            return Instance.ReadStringImpl( section, key );
        }

        public static void WriteInt( string section, string key, int intValue )
        {
            Instance.WriteIntImpl( section, key, intValue );
        }

        public static void WriteBool( string section, string key, bool boolValue )
        {
            Instance.WriteBoolImpl( section, key, boolValue );
        }

        public static void WriteDate( string section, string key, DateTime dateValue )
        {
            Instance.WriteDateImpl( section, key, dateValue );
        }

        public static void WriteString( string section, string key, string stringValue )
        {
            Instance.WriteStringImpl( section, key, stringValue );
        }

        public static void DeleteSection( string section )
        {
            Instance.DeleteSectionImpl( section );
        }
        #endregion Public Interface

        #region implementation details

        private static ObjectStore Instance
        {
            get
            {
                if( _instance == null )
                {
                    _instance = (ObjectStore) Core.ResourceAP.RunUniqueJob(
                        new CreateInstanceDelegate( CreateInstance ) );
                }
                return _instance;
            }
        }

        private delegate ObjectStore CreateInstanceDelegate();
        private static ObjectStore CreateInstance()
        {
            return new ObjectStore();
        }

        private int ReadIntImpl( string section, string key, int defaultValue )
        {
            IResource value = GetValueResource( section, key, false );
            if( value == null || !value.HasProp( _propIntValue ) )
            {
                return defaultValue;
            }
            return value.GetIntProp( _propIntValue );
        }

        private bool ReadBoolImpl( string section, string key, bool defaultValue )
        {
            IResource value = GetValueResource( section, key, false );
            if( value == null )
            {
                return defaultValue;
            }
            return value.HasProp( _propBoolValue );
        }

        private DateTime ReadDateImpl( string section, string key, DateTime defaultValue )
        {
            IResource value = GetValueResource( section, key, false );
            if( value == null || !value.HasProp( _propDateValue ) )
            {
                return defaultValue;
            }
            return value.GetDateProp( _propDateValue );
        }

        private string ReadStringImpl( string section, string key )
        {
            IResource value = GetValueResource( section, key, false );
            return ( value == null ) ? string.Empty : value.GetPropText( _propStringValue );
        }

        private delegate void WriteIntDelegate( string section, string key, int intValue );

        private void WriteIntImpl( string section, string key, int intValue )
        {
            if( !_store.IsOwnerThread() )
            {
                _resourceAP.QueueJob(
                    JobPriority.Immediate, new WriteIntDelegate( WriteIntImpl ), section, key, intValue );
            }
            else
            {
                IResource value = GetValueResource( section, key, true );
                value.SetProp( _propIntValue, intValue );
            }
        }

        private delegate void WriteBoolDelegate( string section, string key, bool boolValue );

        private void WriteBoolImpl( string section, string key, bool boolValue )
        {
            if( !_store.IsOwnerThread() )
            {
                _resourceAP.QueueJob(
                    JobPriority.Immediate, new WriteBoolDelegate( WriteBoolImpl ), section, key, boolValue );
            }
            else
            {
                IResource value = GetValueResource( section, key, true );
                value.SetProp( _propBoolValue, boolValue );
            }
        }

        private delegate void WriteDateDelegate( string section, string key, DateTime dateValue );

        private void WriteDateImpl( string section, string key, DateTime dateValue )
        {
            if( !_store.IsOwnerThread() )
            {
                _resourceAP.QueueJob(
                    JobPriority.Immediate, new WriteDateDelegate( WriteDateImpl ), section, key, dateValue );
            }
            else
            {
                IResource value = GetValueResource( section, key, true );
                value.SetProp( _propDateValue, dateValue );
            }
        }

        private delegate void WriteStringDelegate( string section, string key, string stringValue );

        private void WriteStringImpl( string section, string key, string stringValue )
        {
            if( !_store.IsOwnerThread() )
            {
                _resourceAP.QueueJob(
                    JobPriority.Immediate, new WriteStringDelegate( WriteStringImpl ), section, key, stringValue );
            }
            else
            {
                IResource value = GetValueResource( section, key, true );
                value.SetProp( _propStringValue, stringValue );
            }
        }

        private delegate void DeleteSectionDelegate( string section );

        private void DeleteSectionImpl( string section )
        {
            if( !_store.IsOwnerThread() )
            {
                _resourceAP.QueueJob( JobPriority.Immediate, new DeleteSectionDelegate( DeleteSectionImpl ), section );
            }
            else
            {
                IResourceList sections = _store.FindResources( _sectionRes, Core.Props.Name, section );
                foreach( IResource sectionRes in sections )
                {
                    sectionRes.GetLinksTo( null, Core.Props.Parent ).DeleteAll();
                    sectionRes.Delete();
                }
            }
        }

        private static IResource GetValueResource( string section, string key, bool createIfNotFound )
        {
            IResourceList sections = _store.FindResources( _sectionRes, Core.Props.Name, section );
            IResource result = null;
            foreach( IResource sectionRes in sections )
            {
                if( result == null )
                {
                    result = sectionRes;
                }
                else
                {
                    if( createIfNotFound || Core.ResourceStore.IsOwnerThread() )
                    {
                        UpdateObsoleteSection( sectionRes, result );
                    }
                    else
                    {
                        Core.ResourceAP.RunUniqueJob(
                            new UpdateObsoleteSectionDelegate( UpdateObsoleteSection ), sectionRes, result );
                    }
                }
            }
            if( result == null && createIfNotFound )
            {
                result = _store.BeginNewResource( _sectionRes );
                result.SetProp( Core.Props.Name, section );
                result.EndUpdate();
            }
            if( result != null )
            {
                IResourceList values = result.GetLinksTo( null, Core.Props.Parent ).Intersect(
                    _store.FindResources( _valueRes, Core.Props.Name, key ), true );
                if( values.Count == 0 )
                {
                    if( !createIfNotFound )
                    {
                        result = null;
                    }
                    else
                    {
                        IResource sect = result;
                        result = _store.BeginNewResource( _valueRes );
                        result.AddLink( Core.Props.Parent, sect );
                        result.SetProp( Core.Props.Name, key );
                        result.EndUpdate();
                    }
                }
                else
                {
                    result = values[ 0 ];
                }
            }
            return result;
        }

        private delegate void UpdateObsoleteSectionDelegate( IResource sectionRes, IResource result );

        private static void UpdateObsoleteSection( IResource sectionRes, IResource result )
        {
            foreach( IResource res in sectionRes.GetLinksTo( null, Core.Props.Parent ) )
            {
                res.AddLink( Core.Props.Parent, result );
            }
            sectionRes.Delete();
        }

        private static ObjectStore      _instance;
        private static IResourceStore   _store;
        private static IAsyncProcessor  _resourceAP;
        private static int              _propIntValue;
        private static int              _propBoolValue;
        private static int              _propDateValue;
        private static int              _propStringValue;
        private static readonly string  _sectionRes = "ObjectStoreSection";
        private static readonly string  _valueRes = "ObjectStoreValue";

        #endregion
    }
}
