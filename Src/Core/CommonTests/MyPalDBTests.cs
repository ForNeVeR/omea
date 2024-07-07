// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using NUnit.Framework;
using JetBrains.Omea.ResourceStore;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.TextIndex;

namespace CommonTests
{
    /**
     * Utility functions for tests using the MyPal storage.
     */

    public class MyPalDBTests
    {
        protected MyPalStorage _storage;

        public static void RemoveDBFiles()
        {
            if ( !Directory.Exists( MyPalStorage.DBPath ) )
                return;

            try
            {
                RemoveFilesWithExt( "*.dbUtil" );
                RemoveFilesWithExt( "*.blob" );
            }
            catch ( Exception exc )
            {
                Assert.Fail( exc.Message );
            }
        }

        private static void RemoveFilesWithExt( string mask )
        {
            string[] files = System.IO.Directory.GetFiles( MyPalStorage.DBPath, mask );
            foreach ( string fileName in files )
            {
                System.IO.File.Delete( fileName );
            }
        }

        public static void RemoveTextIndexFiles()
        {
            try
            {
                string[] files = System.IO.Directory.GetFiles(OMEnv.WorkDir, "_*");
                foreach ( string fileName in files )
                {
                    System.IO.File.Delete( fileName );
                }
            }
            catch ( Exception exc )
            {
                Assert.Fail( exc.Message );
            }
        }

        protected void InitStorage()
        {
            RemoveDBFiles();
            MyPalStorage.ResourceCacheSize = 16;
            MyPalStorage.CreateDatabase();
            MyPalStorage.OpenDatabase();
            _storage = MyPalStorage.Storage;
            IndexConstructor.WorkDir = MyPalStorage.DBPath;
        }

        protected void CloseStorage()
        {
            MyPalStorage.CloseDatabase();
            _storage = null;
            try
            {
                RemoveDBFiles();
            }
            catch ( Exception e )
            {
                Console.WriteLine( " Error cleaning DB files: " + e.Message );
            }
        }

        protected void ReopenStorage()
        {
            MyPalStorage.CloseDatabase();
            MyPalStorage.OpenDatabase();
            _storage = MyPalStorage.Storage;
        }

        protected int _propSubject;
        protected int _propSize;
        protected int _propReceived;
        protected int _propAuthor;
        protected int _propReply;
        protected int _propFrom;
        protected int _propTo;
        protected int _propFirstName;
        protected int _propLastName;
        protected int _propReplyTo;
        protected int _propSimilarity;
        protected int _propBody;
        protected int _propUnread;
        protected int _propValueList;

        protected void RegisterResourcesAndProperties()
        {
            _propSubject = _storage.PropTypes.Register( "Subject", PropDataType.String );
            _propFirstName = _storage.PropTypes.Register( "FirstName", PropDataType.String );
            _propLastName = _storage.PropTypes.Register( "LastName", PropDataType.String );

            _storage.ResourceTypes.Register( "Email", "Email", "Subject" );
            _storage.ResourceTypes.Register( "Person", "Person", "FirstName LastName" );
            _propSize = _storage.PropTypes.Register( "Size", PropDataType.Int );
            _propReceived = _storage.PropTypes.Register( "Received", PropDataType.Date );
            _propAuthor = _storage.PropTypes.Register( "Author", PropDataType.Link );
            _propReply = _storage.PropTypes.Register( "Reply", PropDataType.Link, PropTypeFlags.DirectedLink );
            _propSimilarity = _storage.PropTypes.Register( "Similarity", PropDataType.Double );
            _propBody = _storage.PropTypes.Register( "Body", PropDataType.Blob );
            _propUnread = _storage.PropTypes.Register( "IsUnread", PropDataType.Bool );
            _propValueList = _storage.PropTypes.Register( "ValueList", PropDataType.StringList );
            _storage.PropTypes.Register( "Responsible", PropDataType.Link );

            _propFrom = _storage.PropTypes.Register( "From", PropDataType.Link, PropTypeFlags.DirectedLink );
            _propTo = _storage.PropTypes.Register( "To", PropDataType.Link, PropTypeFlags.DirectedLink );
        }

        protected IResource CreatePerson( string firstName, string lastName )
        {
            IResource person = _storage.NewResource( "Person" );
            if ( firstName != null )
                person.SetProp( "FirstName", firstName );
            if ( lastName != null )
                person.SetProp( "LastName", lastName );
            return person;
        }

        protected IResource CreateEmail( string subject )
        {
            IResource res = _storage.NewResource( "Email" );
            res.SetProp( _propSubject, subject );
            return res;
        }

    }
}
