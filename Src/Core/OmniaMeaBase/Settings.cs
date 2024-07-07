// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Base
{
    public class SettingsCollection
    {
        public SettingsCollection(){}
        public SettingsCollection( string traceCategory )
        {
            _tracer = new Tracer( traceCategory );
        }

        private readonly ArrayList _settings = new ArrayList();
        private Tracer _tracer = new Tracer( "Settings" );

        private void RegisterSetting( Setting setting )
        {
            _settings.Add( setting );
        }

        public BoolSetting Create( string section, string key, bool Default )
        {
            BoolSetting setting = new BoolSetting( section, key, Default );
            RegisterSetting( setting );
            return setting;
        }
        public DateSetting Create( string section, string key, DateTime Default )
        {
            DateSetting setting = new DateSetting( section, key, Default );
            RegisterSetting( setting );
            return setting;
        }
        public StringSetting Create( string section, string key )
        {
            StringSetting setting = new StringSetting( section, key );
            RegisterSetting( setting );
            return setting;
        }
        public StringSetting Create( string section, string key, string Default )
        {
            StringSetting setting = new StringSetting( section, key, Default );
            RegisterSetting( setting );
            return setting;
        }
        public IntSetting Create( string section, string key, int Default )
        {
            IntSetting setting = new IntSetting( section, key, Default );
            RegisterSetting( setting );
            return setting;
        }

        public void LoadSettings()
        {
            foreach ( Setting setting in _settings )
            {
                setting.Load();
            }
/*
            _tracer.Trace( "\n" );
            _tracer.Trace( "********************************************" );
            _tracer.Trace( ReportSettings() );
            _tracer.Trace( "********************************************" );
            _tracer.Trace( "\n" );
*/
        }
        public string ReportSettings()
        {
            string report = "\nSettings:\n";
            foreach ( INISetting setting in _settings )
            {
                report += setting.Section + "." + setting.Key + " = " +
                    setting.Value + "\n";
            }
            return report;
        }
    }
    public abstract class Setting
    {
        private object _setting;
        private object _default;

        public Setting( object Default )
        {
            _default = Default;
        }
        public virtual object Default { get { return _default; } }
        public abstract bool Defined { get; }
        public virtual bool Different { get { return false; } }
        public virtual object Value { get { return _setting; } }
        protected abstract object Read();
        protected abstract void Write( object value );
        public object Load()
        {
            _setting = Read();
            return _setting;
        }
        public void Save( object value )
        {
            _setting = value;
            Write( _setting );
        }
    }
    public abstract class INISetting : Setting
    {
        private readonly string _section;
        private readonly string _key;
        public INISetting( string section, string key, object Default ) : base ( Default )
        {
            _section = section;
            _key = key;
            Load();
        }
        public string Section { get { return _section; } }
        public string Key { get { return _key; } }
        public override bool Defined { get { return true; } }
    }
    public class BoolSetting : INISetting
    {
        public BoolSetting( string section, string key, bool Default ):
            base( section, key, Default )
        {}
        protected override object Read()
        {
            return Core.SettingStore.ReadBool( Section, Key, (bool)Default );
        }

        protected override void Write( object value )
        {
            Core.SettingStore.WriteBool( Section, Key, (bool)value );
        }
        public static implicit operator bool( BoolSetting setting )
        {
            return (bool)setting.Value;
        }
    }
    public class IntSetting : INISetting
    {
        public IntSetting( string section, string key, int Default ):
            base( section, key, Default )
        {}
        protected override object Read()
        {
            return Core.SettingStore.ReadInt( Section, Key, (int)Default );
        }

        protected override void Write( object value )
        {
            Core.SettingStore.WriteInt( Section, Key, (int)value );
        }
        public static implicit operator int( IntSetting setting )
        {
            return (int)setting.Value;
        }
    }
    public class DateSetting : INISetting
    {
        public DateSetting( string section, string key, DateTime Default ):
            base( section, key, Default )
        {}
        protected override object Read()
        {
            return Core.SettingStore.ReadDate( Section, Key, (DateTime)Default );
        }

        protected override void Write( object value )
        {
            Core.SettingStore.WriteDate( Section, Key, (DateTime)value );
        }
        public static implicit operator DateTime( DateSetting setting )
        {
            return (DateTime)setting.Value;
        }
    }
    public class StringSetting : INISetting
    {
        public StringSetting( string section, string key ):
            base( section, key, null )
        {}
        public StringSetting( string section, string key, string Default ):
            base( section, key, Default )
        {}
        protected override object Read()
        {
            string value = Core.SettingStore.ReadString( Section, Key, (string)Default );
            if ( value != null )
            {
                value = value.Trim();
                if ( value.Length == 0 )
                {
                    value = (string)Default;
                }
            }
            return value;
        }

        protected override void Write( object value )
        {
            Core.SettingStore.WriteString( Section, Key, (string)value );
        }
        public static implicit operator string( StringSetting setting )
        {
            return (string)setting.Value;
        }
    }
    public class CompositeSetting : Setting
    {
        private readonly Setting _setting = null;
        public CompositeSetting( Setting setting, Setting Default ) : base ( Default )
        {
            _setting = setting;
            Default.Load();
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
                return ((Setting)Default).Value;
            }
            return _setting.Value;
        }

        public override object Value { get { return Read(); } }

        protected override void Write( object value )
        {
            _setting.Save( value );
        }
    }
}
