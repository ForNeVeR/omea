// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.IO;
using Ini;
using JetBrains.Omea.Base;
using JetBrains.Omea.HTML;
using JetBrains.DataStructures;

namespace JetBrains.Omea.Favorites
{
    /// <summary>
    /// the MozillaBookmark class describes properties of a bookmark
    /// </summary>
    internal class MozillaBookmark
    {
        public MozillaBookmark( string id, string Url, string name, int level )
        {
            _id = id;
            _url = Url;
            _name = name;
            _level = level;
        }
        public MozillaBookmark( string id, string folder, int level )
        {
            _id = id;
            _folder = folder;
            _level = level;
        }

        public bool IsFolder
        {
            get { return _folder != null; }
        }
        public string Id
        {
            get { return _id; }
        }
        public string Folder
        {
            get { return _folder; }
        }
        public string Url
        {
            get { return _url; }
        }
        public string Name
        {
            get { return _name; }
        }
        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }
        public int Level
        {
            get { return _level; }
        }

        private string  _id;
        private string  _folder;
        private string  _url;
        private string  _name;
        private string  _description;
        private int     _level;
    }

    /// <summary>
    /// the MozillaProfile class encapsulates properties of Mozilla or Firefox profile
    /// allows to enumerate its bookmarks
    /// </summary>
    internal class MozillaProfile : IEnumerable
    {
        public MozillaProfile( string path )
        {
            _name = null;
            _path = path;
            DirectoryInfo di = IOTools.GetParent( path );
            if( di != null )
            {
                di = IOTools.GetParent( IOTools.GetFullName( di ) );
            }
            string firefoxDir = IOTools.Combine( Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData ), @"Mozilla\Firefox" );
            string profileDir = IOTools.GetFileName( path );
            if( di != null )
            {
                if( String.Compare( di.Name, "Firefox", true ) == 0 )
                {
                    firefoxDir = IOTools.GetFullName( di );
                }
                else if( String.Compare( di.Name, "Profiles", true ) == 0 )
                {
                    _name = di.Name + " - " + profileDir; // Mozilla
                    firefoxDir = null;
                }
            }
            if( firefoxDir != null )
            {
                Ini.IniFile profilesIni = new IniFile( IOTools.Combine( firefoxDir, "profiles.ini" ) );
                for( int i = 0; ; ++i )
                {
                    string profile = "Profile" + i;
                    string dir = profilesIni.ReadString( profile, "Path" );
                    if( dir == null || dir.Length == 0 )
                    {
                        break;
                    }
                    if( profilesIni.ReadBool( profile, "IsRelative", false ) )
                    {
                        dir = IOTools.Combine( firefoxDir, dir ).Replace( '/', '\\' );
                    }
                    if( String.Compare( dir, path, true ) == 0 )
                    {
                        _name = "Firefox - " + profilesIni.ReadString( profile, "Name" );
                        break;
                    }
                }
            }
        }

        public string Name
        {
            get { return _name; }
        }
        public string Path
        {
            get { return _path; }
        }

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return new BookmarkEnumerator( _path );
        }

        private class BookmarkEnumerator : IEnumerator, IDisposable
        {
            public BookmarkEnumerator( string path )
            {
                DirectoryInfo[] dirs = IOTools.GetDirectories( path, "*.slt" );
                if( dirs != null && dirs.Length > 0 )
                {
                    path = dirs[ 0 ].FullName;
                }
                _path = path;
                Reset();
            }

            #region IEnumerator Members

            public void Reset()
            {
                FileInfo[] bookmarkFiles = IOTools.GetFiles( _path, "bookmarks.html" );
                if( bookmarkFiles == null || bookmarkFiles.Length == 0 )
                {
                    _parser = null;
                }
                else
                {
                    _parser = new HTMLParser( new StreamReader( bookmarkFiles[ 0 ].FullName ), true );
                    _parser.BreakWords = false;
                    _parser.AddTagHandler( "dl", new HTMLParser.TagHandler( OnDLTag ) );
                    _parser.AddTagHandler( "/dl", new HTMLParser.TagHandler( OnDLClosedTag ) );
                    _parser.AddTagHandler( "h3", new HTMLParser.TagHandler( OnHeaderTag ) );
                    _parser.AddTagHandler( "/h3", new HTMLParser.TagHandler( OnHeaderClosedTag ) );
                    _parser.AddTagHandler( "a", new HTMLParser.TagHandler( OnLinkTag ) );
                    _parser.AddTagHandler( "/a", new HTMLParser.TagHandler( OnLinkClosedTag ) );
                    _parser.AddTagHandler( "dd", new HTMLParser.TagHandler( OnDescriptionTag ) );
                    _level = 0;
                    _inHeader = _inLink = _inDescription = false;
                }
            }
            public object Current
            {
                get
                {
                    if( _parser == null )
                    {
                        return null;
                    }
                    if( _description.Length > 0 )
                    {
                        return _description;
                    }
                    if( _folder.Length > 0 )
                    {
                        return new MozillaBookmark( _id, _folder, _level );
                    }
                    return new MozillaBookmark( _id, _url, _name, _level );
                }
            }
            public bool MoveNext()
            {
                if( _parser != null )
                {
                    _id = _folder = _url = _name = _description = string.Empty;
                    _isFeed = false;
                    for( ; ; )
                    {
                        if( _parser.Finished )
                        {
                            _parser.Dispose();
                            _parser = null;
                            break;
                        }
                        string frag = _parser.ReadNextFragment();
                        if( !_isFeed )
                        {
                            if( _inLink )
                            {
                                _name = frag;
                                return true;
                            }
                            if( _inHeader )
                            {
                                _folder = frag;
                                return true;
                            }
                            if( _inDescription )
                            {
                                _description = frag;
                                _inDescription = false;
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            #endregion

            #region HTML tag handlers

            private void OnDLTag( HTMLParser instance, string tag )
            {
                ++_level;
            }

            private void OnDLClosedTag( HTMLParser instance, string tag )
            {
                --_level;
            }

            private void OnHeaderTag( HTMLParser instance, string tag )
            {
                _inHeader = true;
                HashMap attrMap = instance.ParseAttributes( tag );
                if( ( _id = (string) attrMap[ "id" ] ) == null )
                {
                    _id = string.Empty;
                }
            }

            private void OnHeaderClosedTag( HTMLParser instance, string tag )
            {
                _inHeader = false;
            }

            private void OnLinkTag( HTMLParser instance, string tag )
            {
                _inLink = true;
                HashMap attrMap = instance.ParseAttributes( tag );
                if( ( _url = (string) attrMap[ "href" ] ) == null )
                {
                    _url = string.Empty;
                }
                if( ( _id = (string) attrMap[ "id" ] ) == null )
                {
                    _id = string.Empty;
                }
                _isFeed = attrMap[ "feedurl" ] != null;
            }

            private void OnLinkClosedTag( HTMLParser instance, string tag )
            {
                _inLink = false;
            }

            private void OnDescriptionTag( HTMLParser instance, string tag )
            {
                _inDescription = true;
            }

            #endregion

            #region IDisposable Members

            public void Dispose()
            {
                if( _parser != null )
                {
                    _parser.Dispose();
                }
            }

            #endregion

            private string      _path;
            private string      _folder = string.Empty;
            private string      _url = string.Empty;
            private string      _id = string.Empty;
            private string      _name = string.Empty;
            private string      _description = string.Empty;
            private int         _level;
            private bool        _inHeader;
            private bool        _inLink;
            private bool        _inDescription;
            private bool        _isFeed;
            private HTMLParser  _parser;
        }

        #endregion

        private string      _name;
        private string      _path;
    }

    /// <summary>
    /// the MozillaProfiles class enumerates Mozilla and Firefox profiles
    /// </summary>
    internal class MozillaProfiles
    {
        public static bool PresentOnComputer
        {
            get { return new RelativeProfilesEnumerator( _mozillaFolder ).MoveNext() ||
                         new RelativeProfilesEnumerator( _firefoxFolder ).MoveNext() ||
                         new RelativeProfilesEnumerator( _firefox09Folder ).MoveNext() ||
                         new AbsoluteFirefoxProfilesEnumerator().MoveNext(); }
        }

        public static IEnumerable GetMozillaProfiles()
        {
            return new RelativeProfilesCollection( _mozillaFolder );
        }

        public static IEnumerable GetFirefoxProfiles()
        {
            return new RelativeProfilesCollection( _firefoxFolder );
        }

        public static IEnumerable GetFirefox09Profiles()
        {
            return new RelativeProfilesCollection( _firefox09Folder );
        }

        public static IEnumerable GetAbsoluteFirefoxProfiles()
        {
            return new AbsoluteFirefoxProfilesCollection();
        }

        public static MozillaProfile GetProfile( string name )
        {
            MozillaProfile profile = SearchForProfile( GetMozillaProfiles(), name );
            if( profile == null )
            {
                profile = SearchForProfile( GetFirefoxProfiles(), name );
                if( profile == null )
                {
                    profile = SearchForProfile( GetFirefox09Profiles(), name );
                    if( profile == null )
                    {
                        profile = SearchForProfile( GetAbsoluteFirefoxProfiles(), name );
                    }
                }
            }
            return profile;
        }

        private static MozillaProfile SearchForProfile( IEnumerable profiles, string name )
        {
            foreach( MozillaProfile profile in profiles )
            {
                if( String.Compare( profile.Name, name, true ) == 0 )
                {
                    return profile;
                }
            }
            return null;
        }

        private class RelativeProfilesCollection : IEnumerable
        {
            public RelativeProfilesCollection( string profileType )
            {
                _profileType = profileType;
            }

            #region IEnumerable Members

            public IEnumerator GetEnumerator()
            {
                return new RelativeProfilesEnumerator( _profileType );
            }

            #endregion

            private string _profileType;
        }

        private class RelativeProfilesEnumerator : IEnumerator
        {
            public RelativeProfilesEnumerator( string profileType )
            {
                _path = IOTools.Combine(
                    Environment.GetFolderPath( Environment.SpecialFolder.ApplicationData ), profileType );
                _path = IOTools.Combine( _path, "Profiles" );
                Reset();
            }

            #region IEnumerator Members

            public void Reset()
            {
                DirectoryInfo[] directories = IOTools.GetDirectories( _path );
                if( directories == null )
                {
                    _dirEnumerator = null;
                }
                else
                {
                    _dirEnumerator = directories.GetEnumerator();
                    _dirEnumerator.Reset();
                }
            }

            public object Current
            {
                get
                {
                    return ( _dirEnumerator == null ) ? null : _currentProfile;
                }
            }

            public bool MoveNext()
            {
                _currentProfile = null;
                if( _dirEnumerator != null )
                {
                    while( _dirEnumerator.MoveNext() )
                    {
                        _currentProfile = new MozillaProfile( ( (DirectoryInfo) _dirEnumerator.Current ).FullName );
                        if( _currentProfile.Name != null )
                        {
                            return true;
                        }
                    }
                }
                return false;
            }

            #endregion

            private string          _path;
            private MozillaProfile  _currentProfile;
            private IEnumerator     _dirEnumerator;
        }

        private class AbsoluteFirefoxProfilesCollection : IEnumerable
        {
            #region IEnumerable Members

            public IEnumerator GetEnumerator()
            {
                return new AbsoluteFirefoxProfilesEnumerator();
            }

            #endregion
        }

        private class AbsoluteFirefoxProfilesEnumerator : IEnumerator
        {
            public AbsoluteFirefoxProfilesEnumerator()
            {
                _path = IOTools.Combine( Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData ), @"Mozilla\Firefox\profiles.ini" );
                Reset();
            }

            #region IEnumerator Members

            public void Reset()
            {
                _profilesIni = new IniFile( _path );
                _profileNumber = 0;
            }

            public object Current
            {
                get
                {
                    return _currentProfile;
                }
            }

            public bool MoveNext()
            {
                string profile = "Profile" + _profileNumber++;
                _profilePath = _profilesIni.ReadString( profile, "Path" );
                if( _profilePath == null || _profilePath.Length == 0 )
                {
                    return false;
                }
                if( _profilesIni.ReadBool( profile, "IsRelative", false ) )
                {
                    return MoveNext();
                }
                _currentProfile = new MozillaProfile( _profilePath );
                if( _currentProfile.Name == null )
                {
                    return MoveNext();
                }
                return true;
            }

            #endregion

            private string          _path;
            private Ini.IniFile     _profilesIni;
            private int             _profileNumber;
            private string          _profilePath;
            private MozillaProfile  _currentProfile;
        }

        private const string    _mozillaFolder = "Mozilla";
        private const string    _firefoxFolder = "Phoenix";
        private const string    _firefox09Folder = @"Mozilla\Firefox";
    }
}
