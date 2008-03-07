/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.SamplePlugins.SccPlugin
{

    /// <summary>
    /// Definitions for the resource types and property types used by the plugin.
    /// </summary>
    internal class Props
    {
        internal const string RepositoryResource = "jetbrains.scc.Repository";
        internal const string ChangeSetResource = "jetbrains.scc.ChangeSet";
        internal const string FolderResource = "jetbrains.scc.Folder";
        internal const string FileChangeResource = "jetbrains.scc.FileChange";
        internal const string UserToRepositoryMapResource = "jetbrains.scc.UserToRepositoryMap";
        internal const string LinkRegexResource = "jetbrains.scc.LinkRegex";

        private static int _propRepositoryType;
        private static int _propChangeSetNumber;
        private static int _propP4Client;
        private static int _propAffectsFolder;
        private static int _propChangeType;
        private static int _propRevision;
        private static int _propDiff;
        private static int _propChange;
        private static int _propBinary;
        private static int _propChangeSetRepository;
        private static int _propP4IgnoreChanges;
        private static int _propP4WebUrl;
        private static int _propP4ServerPort;
        private static int _propPathsToWatch;
        private static int _propLastRevision;
        private static int _propUserRepository;
        private static int _propUserContact;
        private static int _propUserId;
        private static int _propRepositoryUrl;
        private static int _propRepositoryRoot;
        private static int _propUserName;
        private static int _propPassword;
        private static int _propRegexMatch;
        private static int _propRegexReplace;
        private static int _propLastError;
        private static int _propShowSubfolderContents;

        internal static int RepositoryType { get { return _propRepositoryType; } }
        internal static int ChangeSetNumber { get { return _propChangeSetNumber; } }
        internal static int P4Client { get { return _propP4Client; } }
        internal static int AffectsFolder { get { return _propAffectsFolder; } }
        internal static int ChangeType { get { return _propChangeType; } }
        internal static int Revision { get { return _propRevision; } }
        internal static int Diff { get { return _propDiff; } }

        /// <summary>
        /// Links a ChangeSet to individual FileChange resources contained in it.
        /// </summary>
        internal static int Change { get { return _propChange; } }
        internal static int Binary { get { return _propBinary; } }
        internal static int ChangeSetRepository { get { return _propChangeSetRepository; } }
        internal static int P4IgnoreChanges { get { return _propP4IgnoreChanges; } }
        internal static int P4WebUrl { get { return _propP4WebUrl; } }
        internal static int P4ServerPort { get { return _propP4ServerPort; } }
        internal static int PathsToWatch { get { return _propPathsToWatch; } }
        internal static int LastRevision { get { return _propLastRevision; } }
        internal static int UserRepository { get { return _propUserRepository; } }
        internal static int UserContact { get { return _propUserContact; } }
        internal static int UserId { get { return _propUserId; } }
        internal static int RepositoryUrl { get { return _propRepositoryUrl; } }
        internal static int RepositoryRoot { get { return _propRepositoryRoot; } }
        internal static int UserName { get { return _propUserName; } }
        internal static int Password { get { return _propPassword; } }
        internal static int RegexMatch { get { return _propRegexMatch; } }
        internal static int RegexReplace { get { return _propRegexReplace; } }
        internal static int LastError { get { return _propLastError; } }
        internal static int ShowSubfolderContents { get { return _propShowSubfolderContents; } }

        internal static void Register( SccPlugin host )
        {
            IResourceStore store = Core.ResourceStore;
            _propRepositoryType = store.PropTypes.Register( "jetbrains.scc.RepositoryType", PropDataType.String,
                PropTypeFlags.Internal );
            _propChangeSetNumber = store.PropTypes.Register( "jetbrains.scc.ChangeSetNumber", PropDataType.Int );
            store.PropTypes.RegisterDisplayName( _propChangeSetNumber, "Change Set Number" );

            _propP4Client = store.PropTypes.Register( "jetbrains.scc.Client", PropDataType.String );

            _propAffectsFolder = store.PropTypes.Register( "jetbrains.scc.AffectsFolder", PropDataType.Link,
                PropTypeFlags.Internal | PropTypeFlags.CountUnread );
            _propChangeType = store.PropTypes.Register( "jetbrains.scc.ChangeType", PropDataType.String );
            store.PropTypes.RegisterDisplayName( _propChangeType, "Change Type" );

            _propRevision = store.PropTypes.Register( "jetbrains.scc.Revision", PropDataType.Int );

            _propDiff = store.PropTypes.Register( "jetbrains.scc.Diff", PropDataType.LongString,
                PropTypeFlags.Internal );
            _propChange = store.PropTypes.Register( "jetbrains.scc.Change", PropDataType.Link,
                PropTypeFlags.Internal | PropTypeFlags.DirectedLink );
            _propBinary = store.PropTypes.Register( "jetbrains.scc.Binary", PropDataType.Bool,
                PropTypeFlags.Internal );
            _propChangeSetRepository = store.PropTypes.Register( "jetbrains.scc.ChangeSetRepository", PropDataType.Link,
                PropTypeFlags.Internal );
            _propP4IgnoreChanges = store.PropTypes.Register( "jetbrains.scc.P4IgnoreChanges", PropDataType.LongString,
                PropTypeFlags.Internal );
            _propP4WebUrl = store.PropTypes.Register( "jetbrains.scc.P4WebUrl", PropDataType.LongString,
                PropTypeFlags.Internal );
            _propP4ServerPort = store.PropTypes.Register( "jetbrains.scc.P4ServerPort", PropDataType.LongString,
                PropTypeFlags.Internal );
            _propPathsToWatch = store.PropTypes.Register( "jetbrains.scc.PathsToWatch", PropDataType.LongString,
                PropTypeFlags.Internal );
            _propLastRevision = store.PropTypes.Register( "jetbrains.scc.LastRevision", PropDataType.Int );
            store.PropTypes.RegisterDisplayName( _propLastRevision, "Last Revision" );

            _propUserRepository = store.PropTypes.Register( "jetbrains.scc.UserRepository", PropDataType.Link,
                PropTypeFlags.Internal );
            _propUserContact = store.PropTypes.Register( "jetbrains.scc.UserContact", PropDataType.Link,
                PropTypeFlags.Internal );
            _propUserId = store.PropTypes.Register( "jetbrains.scc.UserId", PropDataType.String,
                PropTypeFlags.Internal );
            _propRepositoryUrl = store.PropTypes.Register( "jetbrains.scc.RepositoryUrl", PropDataType.String,
                PropTypeFlags.Internal );
            _propRepositoryRoot = store.PropTypes.Register( "jetbrains.scc.RepositoryRoot", PropDataType.String,
                PropTypeFlags.Internal );
            _propUserName = store.PropTypes.Register( "jetbrains.scc.UserName", PropDataType.String,
                PropTypeFlags.Internal );
            _propPassword = store.PropTypes.Register( "jetbrains.scc.Password", PropDataType.String,
                PropTypeFlags.Internal );
            _propRegexMatch = store.PropTypes.Register( "jetbrains.scc.RegexMatch", PropDataType.String,
                PropTypeFlags.Internal );
            _propRegexReplace = store.PropTypes.Register( "jetbrains.scc.RegexReplace", PropDataType.String,
                PropTypeFlags.Internal );
            _propLastError = store.PropTypes.Register( "jetbrains.scc.LastError", PropDataType.String,
                PropTypeFlags.Internal );
            _propShowSubfolderContents = store.PropTypes.Register( "jetbrains.scc.ShowSubfolderContents", PropDataType.Bool,
                PropTypeFlags.Internal );

            //  NB!: The modifications in the register method (adding hosting plugin
            //       which owns resources of these types) make <plugin.xml> unusable
            //       since it will siply overwrite these changes.
            store.ResourceTypes.Register( RepositoryResource, "Repository", "Name",
                ResourceTypeFlags.ResourceContainer | ResourceTypeFlags.NoIndex, host );
            store.ResourceTypes.Register( ChangeSetResource, "Changeset", "Subject",
                ResourceTypeFlags.CanBeUnread, host );
            store.ResourceTypes.Register( FolderResource, "Folder", "Name",
                ResourceTypeFlags.ResourceContainer | ResourceTypeFlags.NoIndex, host );
            store.ResourceTypes.Register( FileChangeResource, "", "",
                ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex, host );
            store.ResourceTypes.Register( UserToRepositoryMapResource, "", "",
                ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex, host );
            store.ResourceTypes.Register( LinkRegexResource, "", "",
                ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex, host );

            store.RegisterLinkRestriction( UserToRepositoryMapResource, _propUserRepository, RepositoryResource, 1, 1 );
            store.RegisterLinkRestriction( UserToRepositoryMapResource, _propUserContact, "Contact", 1, 1 );
        }
    }
}
