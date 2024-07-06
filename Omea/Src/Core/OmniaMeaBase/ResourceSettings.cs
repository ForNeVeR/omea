// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Base
{
    public delegate void ResourceSettingChanged( IResource resource, int propId, object oldValue, object newValue );
    public abstract class ResourceSetting : Setting
    {
        private IResource _resource;
        private int _propId;
        private ResourceSettingChanged _callBack = null;

        public ResourceSetting( IResource resource, int propId, object Default, ResourceSettingChanged callBack ) : base ( Default )
        {
            Guard.NullArgument( resource, "resource" );
            _resource = resource;
            _propId = propId;
            _callBack = callBack;
        }

        private void InvokeCallBack( IResource resource, int propId, object oldValue, object newValue )
        {
            if ( _callBack != null )
            {
                Core.ResourceAP.QueueJob( JobPriority.Immediate, _callBack, resource, propId, oldValue, newValue );
            }
        }
        public IResource Resource { get { return _resource; } }
        public int PropId { get { return _propId; } }
        protected override object Read()
        {
            if ( !_resource.HasProp( _propId ) )
            {
                return Default;
            }
            return _resource.GetProp( _propId );
        }
        protected virtual void WriteImpl( object value )
        {
            ResourceProxy proxy = new ResourceProxy( _resource );
            proxy.AsyncPriority = JobPriority.Immediate;
            proxy.SetProp( _propId, value );
        }
        protected override void Write( object value )
        {
            object oldValue = null;
            if ( _callBack != null )
            {
                oldValue = _resource.GetProp( _propId );
            }
            WriteImpl( value );
            if ( _callBack != null )
            {
                InvokeCallBack( _resource, _propId, oldValue, value  );
            }
        }
        public override bool Defined
        {
            get
            {
                return _resource.HasProp( _propId );
            }
        }
    }

    public class BoolResourceSetting : ResourceSetting
    {
        public BoolResourceSetting( IResource resource, int propId, bool Default ):
            base( resource, propId, Default, null )
        {
        }
        public BoolResourceSetting( IResource resource, int propId, bool Default, ResourceSettingChanged callBack ):
            base( resource, propId, Default, callBack )
        {
        }
        public override object Value { get { return Resource.HasProp( PropId ); } }
        protected override object Read()
        {
            return Resource.HasProp( PropId );
        }
        public override bool Defined { get { return true; } }

        protected override void WriteImpl( object value )
        {
            new ResourceProxy( Resource ).SetProp( PropId, value );
        }
        public static implicit operator bool( BoolResourceSetting setting )
        {
            return (bool)setting.Value;
        }
    }
    public class IntResourceSetting : ResourceSetting
    {
        public IntResourceSetting( IResource resource, int propId, int Default ):
            base( resource, propId, Default, null )
        {}
        public IntResourceSetting( IResource resource, int propId, int Default, ResourceSettingChanged callBack ):
            base( resource, propId, Default, callBack )
        {}
        public static implicit operator int( IntResourceSetting setting )
        {
            return (int)setting.Value;
        }
    }
    public class DateResourceSetting : ResourceSetting
    {
        public DateResourceSetting( IResource resource, int propId, DateTime Default ):
            base( resource, propId, Default, null )
        {}
        public DateResourceSetting( IResource resource, int propId, DateTime Default, ResourceSettingChanged callBack ):
            base( resource, propId, Default, callBack )
        {}
        public static implicit operator DateTime( DateResourceSetting setting )
        {
            return (DateTime)setting.Value;
        }
    }
    public class StringResourceSetting : ResourceSetting
    {
        private bool _deletePropIfStringEmpty;
        public StringResourceSetting( IResource resource, int propId, String Default, bool deletePropIfStringEmpty ):
            base( resource, propId, Default, null )
        {
            _deletePropIfStringEmpty = deletePropIfStringEmpty;
        }
        public StringResourceSetting( IResource resource, int propId, String Default, bool deletePropIfStringEmpty, ResourceSettingChanged callBack ):
            base( resource, propId, Default, callBack )
        {
            _deletePropIfStringEmpty = deletePropIfStringEmpty;
        }
        public static implicit operator string( StringResourceSetting setting )
        {
            return (string)setting.Value;
        }
        protected override void WriteImpl( object value )
        {
            if ( value != null && value.Equals( string.Empty ) && _deletePropIfStringEmpty)
            {
                ResourceProxy proxy = new ResourceProxy( Resource );
                proxy.AsyncPriority = JobPriority.Immediate;
                proxy.DeleteProp( PropId );
            }
            else
            {
                base.WriteImpl( value );
            }
        }
        protected override object Read()
        {
            if ( !_deletePropIfStringEmpty )
            {
                return base.Read();
            }
            string value = Resource.GetPropText( PropId );
            if ( value.Length == 0 )
            {
                return Default;
            }
            return value;
        }
    }

    public abstract class SettingArray : Setting
    {
        ArrayList _settings = new ArrayList();

        public SettingArray( object Default ) : base( Default )
        {}
        public virtual void AddSetting( Setting setting )
        {
            Guard.NullArgument( setting, "setting" );
            _settings.Add( setting );
        }
        public override bool Different
        {
            get
            {
                object value = null;
                foreach ( Setting setting in _settings )
                {
                    setting.Load();
                    object curSetting = setting.Value;
                    if ( value != null && ((IComparable)value).CompareTo( curSetting ) != 0 )
                    {
                        return true;
                    }
                    if ( value == null || ((IComparable)value).CompareTo( curSetting ) == 0 )
                    {
                        value = curSetting;
                    }
                }
                return false;
            }
        }

        public override bool Defined
        {
            get
            {
                bool defined = false;
                bool notDefined = false;
                foreach ( Setting setting in _settings )
                {
                    bool settingDefined = setting.Defined;
                    if ( notDefined && settingDefined )
                    {
                        return false;
                    }
                    if ( defined && !settingDefined )
                    {
                        return false;
                    }
                    if ( settingDefined )
                    {
                        defined = true;
                    }
                    else
                    {
                        notDefined = true;
                    }
                }
                return defined;
            }
        }

        protected override object Read()
        {
            object value = null;
            foreach ( Setting setting in _settings )
            {
                setting.Load();
                object curSetting = setting.Value;
                if ( value != null && ((IComparable)value).CompareTo( curSetting ) != 0)
                {
                    return null;
                }
                if ( value == null || ((IComparable)value).CompareTo( curSetting ) == 0 )
                {
                    value = curSetting;
                }
            }
            return value;
        }

        protected override void Write(object value)
        {
            foreach ( Setting setting in _settings )
            {
                setting.Save( value );
            }
        }
        public override object Value
        {
            get
            {
                return Read();
            }
        }

        public static SettingArray FromResourceList( IResourceList resourceList, int propId, string Default, bool deletePropIfStringEmpty )
        {
            return FromResourceList( resourceList, propId, Default, deletePropIfStringEmpty, null );
        }

        public static SettingArray FromResourceList( IResourceList resourceList, int propId, string Default, bool deletePropIfStringEmpty, ResourceSettingChanged callBack )
        {
            SettingArray settingArray = new StringSettingArray( Default );
            foreach ( IResource resource in resourceList )
            {
                settingArray.AddSetting( new StringResourceSetting( resource, propId, Default, deletePropIfStringEmpty, callBack ) );
            }
            return settingArray;
        }
        public static SettingArray FromResourceList( IResourceList resourceList, int propId, int Default )
        {
            return FromResourceList( resourceList, propId, Default, null );
        }
        public static SettingArray FromResourceList( IResourceList resourceList, int propId, int Default, ResourceSettingChanged callBack )
        {
            SettingArray settingArray = new IntSettingArray( Default );
            foreach ( IResource resource in resourceList )
            {
                settingArray.AddSetting( new IntResourceSetting( resource, propId, Default, callBack ) );
            }
            return settingArray;
        }

        public static SettingArray IntAsBoolFromResourceList( IResourceList resourceList, int propId, bool Default )
        {
            return IntAsBoolFromResourceList( resourceList, propId, Default, null );
        }
        public static SettingArray IntAsBoolFromResourceList( IResourceList resourceList, int propId, bool Default, ResourceSettingChanged callBack )
        {
            SettingArray settingArray = new IntAsBoolSettingArray( Default );
            foreach ( IResource resource in resourceList )
            {
                settingArray.AddSetting( new IntAsBoolResourceSetting( resource, propId, Default, callBack ) );
            }
            return settingArray;
        }
        public static SettingArray FromResourceList( IResourceList resourceList, int propId, bool Default )
        {
            SettingArray settingArray = new BoolSettingArray( Default );
            foreach ( IResource resource in resourceList )
            {
                settingArray.AddSetting( new BoolResourceSetting( resource, propId, Default ) );
            }
            return settingArray;
        }
        public static SettingArray FromResourceList( IResourceList resourceList, int propId, DateTime Default )
        {
            SettingArray settingArray = new DateSettingArray( Default );
            foreach ( IResource resource in resourceList )
            {
                settingArray.AddSetting( new DateResourceSetting( resource, propId, Default ) );
            }
            return settingArray;
        }
    }

    public class StringSettingArray : SettingArray
    {
        public StringSettingArray( string Default ) : base( Default ){}
        public static implicit operator string( StringSettingArray setting )
        {
            return (string)setting.Value;
        }
    }
    public class IntSettingArray : SettingArray
    {
        public IntSettingArray( int Default ) : base( Default ){}
        public static implicit operator int( IntSettingArray setting )
        {
            return (int)setting.Value;
        }
    }
    public class BoolSettingArray : SettingArray
    {
        public BoolSettingArray( bool Default ) : base( Default ){}
        public static implicit operator bool( BoolSettingArray setting )
        {
            return (bool)setting.Value;
        }
    }
    public class DateSettingArray : SettingArray
    {
        public DateSettingArray( DateTime Default ) : base( Default ){}
        public static implicit operator DateTime( DateSettingArray setting )
        {
            return (DateTime)setting.Value;
        }
    }
    public class IntAsBoolSettingArray : SettingArray
    {
        public IntAsBoolSettingArray( bool Default ) : base( Default ){}
        public static implicit operator bool( IntAsBoolSettingArray setting )
        {
            return (bool)setting.Value;
        }
    }
    public class IntAsBoolResourceSetting : Setting
    {
        private IntResourceSetting _setting = null;

        public IntAsBoolResourceSetting( IResource setting, int propId, bool Default, ResourceSettingChanged callBack ) : base ( Default )
        {
            _setting = new IntResourceSetting( setting, propId, 0, callBack );
        }
        public IntAsBoolResourceSetting( IResource setting, int propId, bool Default ) : base ( Default )
        {
            _setting = new IntResourceSetting( setting, propId, 0 );
        }

        public override bool Defined
        {
            get { return _setting.Defined; }
        }

        protected override object Read()
        {
            _setting.Load();
            if ( !_setting.Defined )
            {
                return Default;
            }
            int value = 0;
            object objValue = _setting.Value;
            if ( objValue != null )
            {
                value = (int)objValue;
            }
            if ( value == 0 )
            {
                return Default;
            }
            return ((int)_setting.Value) > 0 ? true : false;
        }
        public static implicit operator bool( IntAsBoolResourceSetting setting )
        {
            return (bool)setting.Value;
        }

        public override object Value { get { return Read(); } }

        protected override void Write( object value )
        {
            _setting.Save( (bool)value ? 1 : -1 );
        }
    }
}
