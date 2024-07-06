// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;

namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Service for performing operations on Bookmarks in Omea.
    /// </summary>
    public interface IBookmarkProfile: IDisposable
    {
        /// <summary>
        /// Name of the profile.
        /// </summary>
        /// <remarks>May consist of several parts splitted with '/' or '\'.</remarks>
        string Name { get; }

        /// <summary>
        /// Starts importing of bookmarks.
        /// </summary>
        void StartImport();

        /// <summary>
        /// Returns array of chars forbidden for usage in bookmark names.
        /// </summary>
        char[] InvalidNameChars { get; }

        /// <summary>
        /// Checks whether specified resource can be created or updated (exported).
        /// </summary>
        /// <param name="res">Weblink or folder resource to be exported.</param>
        /// <param name="error">Error message.</param>
        /// <returns>True if resource can be created.</returns>
        /// <remarks>If res is null then the function says whether the profile can export at all.</remarks>
        bool CanCreate( IResource res, out string error );

        /// <summary>
        /// Checks whether specified resource can be renamed.
        /// </summary>
        /// <param name="res">Weblink or folder resource to be renamed.</param>
        /// <param name="error">Error message.</param>
        /// <returns>True if resource can be renamed.</returns>
        bool CanRename( IResource res, out string error );

        /// <summary>
        /// Checks whether specified resource can be moved to the parent folder.
        /// </summary>
        /// <param name="res">Weblink or folder resource.</param>
        /// <param name="parent">Parent folder resource.</param>
        /// <param name="error">Error message.</param>
        /// <returns>True if resource can be moved.</returns>
        bool CanMove( IResource res, IResource parent, out string error );

        /// <summary>
        /// Checks whether specified resource can be deleted.
        /// </summary>
        /// <param name="res">Weblink or folder resource.</param>
        /// <param name="error">Error message.</param>
        /// <returns>True if resource can be deleted.</returns>
        bool CanDelete( IResource res, out string error );

        /// <summary>
        /// Exports newly created or changed resource.
        /// </summary>
        /// <param name="res">Weblink or folder resource.</param>
        void Create( IResource res );

        /// <summary>
        /// Exports resource which is been renamed.
        /// </summary>
        /// <param name="res">Weblink or folder resource.</param>
        /// <param name="newName">New name of the resoruce.</param>
        /// <remarks>When the method is called, the resource is still not reanmed.</remarks>
        void Rename( IResource res, string newName );

        /// <summary>
        /// Exports moved resource.
        /// </summary>
        /// <param name="res">Weblink or folder resource.</param>
        /// <param name="parent">Parent folder resource.</param>
        /// <param name="oldParent">Old parent folder resource.</param>
        void Move( IResource res, IResource parent, IResource oldParent );

        /// <summary>
        /// Exports deletion of a resource.
        /// </summary>
        /// <param name="res">Weblink or folder resource to be deleted.</param>
        void Delete( IResource res );
    }

    /// <summary>
    /// Service for performing operations on Bookmarks in Omea.
    /// </summary>
    public interface IBookmarkService
    {
        /// <summary>
        /// Registers bookmark profile.
        /// </summary>
        /// <param name="profile">Profile to register.</param>
        void RegisterProfile( IBookmarkProfile profile );

        /// <summary>
        /// Deregisters bookmark profile.
        /// </summary>
        /// <param name="profile">Profile to deregister.</param>
        void DeRegisterProfile( IBookmarkProfile profile );

        /// <summary>
        /// Returns all registered profiles.
        /// </summary>
        IBookmarkProfile[] Profiles { get; }

        /// <summary>
        /// Gets owner profile.
        /// </summary>
        /// <param name="res">Weblink or folder resource.</param>
        /// <returns>Returns owner profile for a weblink or folder resource,
        /// or null is resource is not owned.</returns>
        IBookmarkProfile GetOwnerProfile( IResource res );

        /// <summary>
        /// Root resource for all weblinks, owned by profiles or not.
        /// </summary>
        IResource BookmarksRoot { get; }

        /// <summary>
        /// Gets root resource for a profile with specified name.
        /// </summary>
        /// <param name="profileName">Bookmark profile name.</param>
        IResource GetProfileRoot( string profileName );

        /// <summary>
        /// Gets root resource for specified profile.
        /// </summary>
        /// <param name="profile">Bookmark profile.</param>
        IResource GetProfileRoot( IBookmarkProfile profile );

        /// <summary>
        /// Gets list of all bookmarks.
        /// </summary>
        IResourceList GetBookmarks();

        /// <summary>
        /// Gets list of bookmarks of specified profile.
        /// </summary>
        /// <param name="profile">Bookmark profile</param>
        IResourceList GetBookmarks( IBookmarkProfile profile );

        IResource FindOrCreateBookmark( IResource parent, string name, string url );
        IResource FindOrCreateFolder( IResource parent, string name );

        /// <summary>
        /// Sets the name of weblink or folder.
        /// </summary>
        /// <param name="res">Weblink or folder resource.</param>
        /// <param name="name">New name of resource.</param>
        void SetName( IResource res, string name );
        /// <summary>
        /// Sets the URL of weblink resource.
        /// </summary>
        /// <param name="res">Weblink resource.</param>
        /// <param name="url">URL of weblink.</param>
        void SetUrl( IResource res, string url );
        /// <summary>
        /// Sets the parent folder of weblink or folder resource.
        /// </summary>
        /// <param name="res">Weblink or folder resource.</param>
        /// <param name="parent">Parent folder resource.</param>
        void SetParent( IResource res, IResource parent );

        void DeleteBookmark( IResource res );
        void DeleteBookmarks( IResourceList resources );
        void DeleteFolder( IResource res );
        void DeleteFolders( IResourceList resources );
    }
}
