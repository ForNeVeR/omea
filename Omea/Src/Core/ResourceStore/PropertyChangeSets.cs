// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using JetBrains.Omea.OpenAPI;
using JetBrains.DataStructures;

namespace JetBrains.Omea.ResourceStore
{
    /**
     * Base class for all interfaces implementing IPropertyChangeSet.
     */

    internal abstract class PropertyChangeSetBase: IPropertyChangeSet
    {
        protected bool _newResource;
        protected bool _displayNameAffected;

        protected PropertyChangeSetBase( bool newResource )
        {
            _newResource = newResource;
        }

        public bool IsNewResource
        {
            get { return _newResource; }
        }

        public bool IsDisplayNameAffected
        {
            get { return _displayNameAffected; }
        }

        public IPropertyChangeSet Merge( IPropertyChangeSet other )
        {
            MultiPropChangeSet mergeResult = new MultiPropChangeSet( _newResource || other.IsNewResource );
            MergeWith( mergeResult );

            (other as PropertyChangeSetBase).MergeWith( mergeResult );
            return mergeResult;

        }

        public abstract bool IsPropertyChanged( int propID );

        public bool IsPropertyChanged<T>(PropId<T> propId)
        {
            return IsPropertyChanged(propId.Id);
        }

        public abstract int[] GetChangedProperties();
        public abstract object GetOldValue( int propID );

        public abstract LinkChange[] GetLinkChanges( int propID );
        public abstract LinkChangeType GetLinkChange( int propID, int targetID );

        protected abstract void MergeWith( MultiPropChangeSet cs );
        internal abstract bool Intersects( BitArray propBits );
    }

    /**
     * The changeset which describes the change of a single property.
     */

    internal class SinglePropChangeSet: PropertyChangeSetBase
    {
        private int            _propID;
        private object         _oldValue;
        private int            _linkTargetID;
        private LinkChangeType _linkChangeType;

        internal SinglePropChangeSet( int propID, object oldValue, bool newResource,
            bool displayNameAffected )
            : base( newResource )
        {
            _propID = propID;
            _oldValue = oldValue;
            _linkTargetID = -1;
            _displayNameAffected = displayNameAffected;
        }

        internal SinglePropChangeSet( int propID, int linkTargetID, LinkChangeType linkChangeType,
            bool displayNameAffected )
            : base( false )
        {
            _propID = propID;
            _linkTargetID = linkTargetID;
            _linkChangeType = linkChangeType;
            _displayNameAffected = displayNameAffected;
        }

        public override int[] GetChangedProperties()
        {
            return new int[] { _propID };
        }

        public override bool IsPropertyChanged( int propID )
        {
            return _propID == propID;
        }

        public override object GetOldValue( int propID )
        {
            if ( _propID == propID )
            {
                return _oldValue;
            }

            return null;
        }

        public override LinkChange[] GetLinkChanges( int propID )
        {
            if ( MyPalStorage.Storage.PropTypes [propID].DataType != PropDataType.Link )
                throw new StorageException( "GetLinkChanges() can only be called for link properties" );

            if ( _propID == propID )
            {
                return new LinkChange[] { new LinkChange( _linkTargetID, _linkChangeType ) };
            }

            return new LinkChange[] {};
        }

        public override LinkChangeType GetLinkChange( int propID, int targetID )
        {
            if ( propID != _propID )
            {
                return LinkChangeType.None;
            }
            if ( _linkTargetID == -1 )
            {
                throw new StorageException( "IsLinkAdded() can be called only on link properties" );
            }

            if ( _linkTargetID == targetID )
            {
                return _linkChangeType;
            }
            return LinkChangeType.None;
        }

        protected override void MergeWith( MultiPropChangeSet cs )
        {
            if ( _linkTargetID == -1 )
            {
                cs.AddChangedProp( _propID, _oldValue );
            }
            else
            {
                cs.AddChangedLink( _propID, _linkTargetID, _linkChangeType );
            }
            if ( _displayNameAffected )
            {
                cs.SetDisplayNameAffected();
            }
        }

        internal override bool Intersects( BitArray propBits )
        {
            int propID = Math.Abs( _propID );
            return propBits.Length > propID && propBits [propID];
        }

        public override string ToString()
        {
            if ( _linkTargetID != -1 )
            {
                return "SinglePropChangeSet: link " + _propID + " to " + _linkTargetID +
                    ((_linkChangeType == LinkChangeType.Add) ? " added" : " deleted");
            }
            return "SinglePropChangeSet: property " + _propID + " changed";
        }
    }

    /**
     * A ChangeSet which describes the change of multiple properties at the same time.
     * NOTE: The changeset is filled only in the resource thread, but after the ResourceSaved
     * event is fired, it can be accessed from several threads as the event is processed (OM-7003).
     * Because of this, locking is only necessary on read methods.
     */

    internal class MultiPropChangeSet: PropertyChangeSetBase
    {
        private IntHashTable _oldValues = new IntHashTable();
        private int _updateCounter = 1;

        internal MultiPropChangeSet( bool newResource ): base( newResource )
        {
        }

        internal void AddChangedProp( int propID, object oldValue )
        {
            if ( !_oldValues.ContainsKey( propID ) )
            {
                _oldValues [propID] = oldValue;
            }
        }

        internal void AddChangedLink( int propID, int targetID, LinkChangeType changeType )
        {
            ArrayList list = (ArrayList) _oldValues [propID];
            if ( list == null )
            {
                list = new ArrayList();
                _oldValues [propID] = list;
            }
            list.Add( new LinkChange( targetID, changeType ) );
        }

        internal void SetDisplayNameAffected()
        {
            _displayNameAffected = true;
        }

        internal void BeginUpdate()
        {
            _updateCounter++;
        }

        internal int EndUpdate()
        {
            return --_updateCounter;
        }

        internal bool IsEmpty()
        {
            return _oldValues.Count == 0;
        }

        internal int GetUpdateCounter()
        {
            return _updateCounter;
        }

        public override int[] GetChangedProperties()
        {
            int[] result = new int [_oldValues.Count];
            lock( _oldValues )
            {
                int i=0;
                foreach( IntHashTable.Entry e in _oldValues )
                {
                    result [i++] = e.Key;
                }
            }
            return result;
        }

        public override bool IsPropertyChanged( int propID )
        {
            lock( _oldValues )
            {
                return _oldValues.ContainsKey( propID );
            }
        }

        public override object GetOldValue( int propID )
        {
            lock( _oldValues )
            {
                return _oldValues [propID];
            }
        }

        public override LinkChange[] GetLinkChanges( int propID )
        {
            if ( MyPalStorage.Storage.PropTypes [propID].DataType != PropDataType.Link )
                throw new StorageException( "GetLinkOldValue() can only be called for link properties" );

            ArrayList changeList;
            lock( _oldValues )
            {
                changeList = (ArrayList) _oldValues [propID];
            }
            if ( changeList == null )
                return new LinkChange[] {};

            return (LinkChange[]) changeList.ToArray( typeof(LinkChange) );
        }

        public override LinkChangeType GetLinkChange( int propID, int targetID )
        {
            if ( MyPalStorage.Storage.GetPropDataType( propID ) != PropDataType.Link )
                throw new StorageException( "IsLinkAdded() can only be called for link properties" );

            ArrayList linkList;
            lock( _oldValues )
            {
                linkList = (ArrayList) _oldValues [propID];
            }
            if ( linkList == null )
                return LinkChangeType.None;

            for( int i=0; i<linkList.Count; i++ )
            {
                LinkChange change = (LinkChange) linkList [i];
                if ( change.TargetId == targetID )
                {
                    return change.ChangeType;
                }
            }
            return LinkChangeType.None;
        }

        protected override void MergeWith( MultiPropChangeSet cs )
        {
            lock( _oldValues )
            {
                foreach( IntHashTable.Entry ie in _oldValues )
                {
                    if ( ie.Value is ArrayList )
                    {
                        foreach( LinkChange change in (ArrayList) ie.Value )
                        {
                            cs.AddChangedLink( ie.Key, change.TargetId, change.ChangeType );
                        }
                    }
                    else
                    {
                        cs.AddChangedProp( ie.Key, ie.Value );
                    }
                }
            }
            if ( _displayNameAffected )
            {
                cs.SetDisplayNameAffected();
            }
        }

        internal override bool Intersects( BitArray propBits )
        {
            int len = propBits.Length;
            lock( _oldValues )
            {
                foreach( IntHashTable.Entry ie in _oldValues )
                {
                    int propId = (ie.Key < 0) ? -ie.Key : ie.Key;
                    if ( propId < len && propBits [propId] )
                        return true;
                }
            }
            return false;
        }
    }
}
