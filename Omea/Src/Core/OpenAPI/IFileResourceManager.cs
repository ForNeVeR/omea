/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.IO;

namespace JetBrains.Omea.OpenAPI
{
	/// <summary>
	/// Manages the relationship between file extensions, MIME types and format resources.
	/// </summary>
	public interface IFileResourceManager
	{
        /// <summary>
        /// Registers a resource type for a file format resource.
        /// </summary>
        /// <param name="fileResType">The name of the resource type.</param>
        /// <param name="displayName">The display name of the resource type.</param>
        /// <param name="resourceDisplayNameTemplate">The template for the display names of resources
        /// having the resource type.</param>
        /// <param name="flags">The flags of the resource type.</param>
        /// <param name="ownerPlugin">The plugin which owns the resource type.</param>
        /// <param name="extensions">The extensions corresponding to the resource type.</param>
        void RegisterFileResourceType( string fileResType,
            string displayName,
            string resourceDisplayNameTemplate,
            ResourceTypeFlags flags,
            IPlugin ownerPlugin,
            params string[] extensions );
        
        /// <summary>
        /// Removes the registration of the specified resource type as a file format
        /// resource type.
        /// </summary>
        /// <param name="fileResType">The resource type for which the registration is removed.</param>
        void DeregisterFileResourceType( string fileResType );

        /// <summary>
        /// Associates a MIME type with a file format resource type.
        /// </summary>
        /// <param name="fileResType">The file format resource type which is associated.</param>
        /// <param name="contentType">The MIME type which is associated.</param>
        void SetContentType( string fileResType, string contentType );

        /// <summary>
        /// Returns the type of the file format resource which has been registered for the
        /// specified extension.
        /// </summary>
        /// <param name="extension">The extension for which the resource type is retrieved.</param>
        /// <returns>Resource type name, or null if no resource type has been registered
        /// for the extension.</returns>
        string GetResourceTypeByExtension( string extension );

        /// <summary>
        /// Returns the stream from which the content of the specified file format resource
        /// can be loaded.
        /// </summary>
        /// <param name="resource">The resource for which the stream is retrieved.</param>
        /// <returns>The stream, or null if it was not possible to retrieve the stream.</returns>
        Stream GetStream( IResource resource );

        /// <summary>
        /// Returns the stream reader from which the content of the specified file format
        /// resource can be loaded.
        /// </summary>
        /// <remarks>The reader uses the encoding stored as the Charset property of the
        /// file format resource (if any) or the default encoding if the Charset property
        /// is not specified or is not a valid encoding name..</remarks>
        /// <param name="resource">The resource for which the reader is retrieved.</param>
        /// <returns>The stream, or null if it was not possible to retrieve the reader.</returns>
        StreamReader GetStreamReader( IResource resource );

        /// <summary>
        /// Returns the name of a disk file containing the data of the specified
        /// file format resource.
        /// </summary>
        /// <remarks><para>If the source of the specified resource is a file, returns the name 
        /// of that file. If it's a different resource, saves the stream of the
        /// resource to a temporary file and returns the name of that file.</para>
        /// <para>After the plugin is done using the file, it must call 
        /// <see cref="CleanupSourceFile"/> to delete the temporary files which may have been
        /// created.</para>
        /// </remarks>
        /// <param name="fileResource">The resource for which the file name is returned.</param>
        /// <returns>The name of the file containing the data, or null if it was not possible
        /// to save the resource data to the file.</returns>
        string GetSourceFile( IResource fileResource );

        /// <summary>
        /// Deletes the temporary files created by <see cref="GetSourceFile"/>, if any.
        /// </summary>
        /// <param name="fileResource">The resource for which the data was retrieved.</param>
        /// <param name="fileName">The name of the file returned by <see cref="GetSourceFile"/>.</param>
        void CleanupSourceFile( IResource fileResource, string fileName );

        /// <summary>
        /// Opens the specified format file in its associated application.
        /// </summary>
        /// <remarks>If the source of the specified format resource is not a disk file,
        /// the resource is saved to a temporary file, which is then opened and deleted
        /// when the associated application is closed.</remarks>
        /// <param name="fileResource">The format resource to open.</param>
        void OpenSourceFile( IResource fileResource );

        /// <summary>
        /// Creates temp directory with unique name and returns its name.
        /// </summary>
        /// <remarks></remarks>
        /// <since>1.0.2</since>
        string GetUniqueTempDirectory();
        
        /// <summary>
        /// ID of the "Charset" property type.
        /// </summary>
        int PropCharset { get; }
	}
}
