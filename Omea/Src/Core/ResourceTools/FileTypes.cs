// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Base;
using JetBrains.DataStructures;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.FileTypes
{
    /**
     * FileTypeMap helps to operate with file types, its extensions, content types
     */
    public class FileResourceManager: IFileResourceManager
    {
        /**
         * Registering FileTypeMap resource types
         */
        public FileResourceManager()
        {
            _store = Core.ResourceStore;
            _store.ResourceTypes.Register( "FileTypeMap", "FileTypeMap", string.Empty,
                ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            _propExtension = _store.PropTypes.Register( "Extension", PropDataType.String, PropTypeFlags.Internal );
            _propResType = _store.PropTypes.Register( "ResType", PropDataType.String, PropTypeFlags.Internal );
            _propLastModified = _store.PropTypes.Register( "LastModified", PropDataType.Date, PropTypeFlags.Internal );
            _propCharset = _store.PropTypes.Register( "Charset", PropDataType.String, PropTypeFlags.Internal );
            _propSource = ResourceTypeHelper.UpdatePropTypeRegistration( "Source", PropDataType.Link, PropTypeFlags.Internal | PropTypeFlags.SourceLink );
            _propFileType = _store.PropTypes.Register( "FileType", PropDataType.String );
            _store.PropTypes.RegisterDisplayName( _propFileType, "File Type" );

            _store.RegisterUniqueRestriction( "FileTypeMap", _propExtension );
            _store.RegisterUniqueRestriction( "FileTypeMap", Core.Props.ContentType );
            _store.RegisterDisplayNameProvider( new SourceDisplayNameProvider() );
        }

        public static string GetTrashDirectory()
        {
            return Path.Combine( Path.GetTempPath(), _omeaTrashDirectoryName );
        }

        public string GetUniqueTempDirectory()
        {
            string tempDir = Path.Combine( GetTrashDirectory(), _randomizer.NextDouble().ToString().Substring( 2 ) );
            IOTools.CreateDirectory( tempDir );
            return tempDir;
        }

        public static void ClearTrashDirectory()
        {
            /**
             * delete recursively and create empty
             */
            IOTools.DeleteDirectory( GetTrashDirectory(), true );
        }

        public int propLastModified { get { return _propLastModified; } }
        public int propSource { get { return _propSource; } }
        public int PropCharset { get { return _propCharset; } }

        /**
         * Getting name of resource type by file extension
         */
        public string GetResourceTypeByExtension( string ext )
        {
            #region Preconditions
            Guard.NullArgument( ext, "FileTypes -- Input extension is null." );
            #endregion Preconditions

            ext = ext.ToLower();
            string fileResType;
            lock( _extension2ResTypeMap )
            {
                fileResType = (string) _extension2ResTypeMap[ ext ];
            }
            if( fileResType == null )
            {
                IResource fileMapEntry = _store.FindUniqueResource( "FileTypeMap", _propExtension, ext );
                fileResType = ( fileMapEntry == null ) ? null : fileMapEntry.GetStringProp( "ResType" );
                if( fileResType == null || !Core.ResourceStore.ResourceTypes[ fileResType ].OwnerPluginLoaded )
                {
                    fileResType = string.Empty;
                }
                lock( _extension2ResTypeMap )
                {
                    _extension2ResTypeMap[ ext ] = fileResType;
                }
            }
            return ( fileResType.Length > 0 ) ? fileResType : null;
        }

        /**
         * Getting name of resource type by HTTP response content type
         */
        public string GetResourceTypeByContentType( string contentType )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( contentType ) )
                throw new ArgumentNullException( "contentType", "FileTypes -- Input content type is null or empty." );
            #endregion Preconditions

            contentType = contentType.ToLower();

            // cut charset information from contentType
            int i = contentType.IndexOf( ';' );
            if( i > 0 )
            {
                contentType = contentType.Substring( 0, i );
            }

            string fileResType;
            lock( _contentType2ResTypeMap )
            {
                fileResType = (string) _contentType2ResTypeMap[ contentType ];
            }
            if( fileResType == null )
            {
                IResource fileMapEntry = _store.FindUniqueResource( "FileTypeMap", Core.Props.ContentType, contentType );
                fileResType = ( fileMapEntry == null ) ? null : fileMapEntry.GetStringProp( "ResType" );
                if( fileResType == null || !Core.ResourceStore.ResourceTypes[ fileResType ].OwnerPluginLoaded )
                {
                    fileResType = string.Empty;
                }
                lock( _contentType2ResTypeMap )
                {
                    _contentType2ResTypeMap[ contentType ] = fileResType;
                }
            }
            return ( fileResType.Length > 0 ) ? fileResType : null;
        }

        public string[] GetResourceTypes()
        {
            IResourceList fileMap = _store.GetAllResources( "FileTypeMap" );
            string[] result = new string[ fileMap.Count ];
            for( int i = 0; i < fileMap.Count; ++i )
            {
                result[ i ] = fileMap[ i ].GetPropText( "ResType" );
            }
            return result;
        }

        /**
         * Creating file resource by extension
         */
        public IResource CreateFileResourceByExtension( string ext )
        {
            return _store.NewResource( GetResourceTypeByExtension( ext ) );
        }

        /**
         * Creating file resource by HTTP response content type
         */
        public IResource CreateFileResourceByContentType( string contentType )
        {
            return _store.NewResource( GetResourceTypeByContentType( contentType ) );
        }

        /**
         * Registering of file resource type with corresponding list of extensions
         */
        public void RegisterFileResourceType( string fileResType,
                                              string displayName,
                                              string resourceDisplayNameTemplate,
                                              ResourceTypeFlags flags,
                                              IPlugin ownerPlugin,
                                              params string[] exts )
        {
            _store.ResourceTypes.Register( fileResType, displayName, resourceDisplayNameTemplate,
                                           flags | ResourceTypeFlags.FileFormat, ownerPlugin );
            RegisterFileResourceTypeImpl( fileResType, exts );
        }

        /**
         * Deregistering of file resource type leads to cleanup of corresponding entries from FileTypeMap
         */
        public void DeregisterFileResourceType( string fileResType )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( fileResType ) )
                throw new ArgumentNullException( "fileResType", "FileTypes -- Input file resource type is null or empty." );
            #endregion Preconditions

            IResourceList resList = _store.FindResources( "FileTypeMap", _propResType, fileResType );
            foreach( IResource res in resList )
            {
                if( res.HasProp( _propExtension ) )
                {
                    lock( _extension2ResTypeMap )
                    {
                        _extension2ResTypeMap.Remove( res.GetStringProp( _propExtension ) );
                    }
                }
                if( res.HasProp( Core.Props.ContentType ) )
                {
                    lock( _contentType2ResTypeMap )
                    {
                        _contentType2ResTypeMap.Remove( res.GetStringProp( Core.Props.ContentType ) );
                    }
                }
                Core.ResourceAP.RunUniqueJob( new MethodInvoker( res.Delete ) );
            }
        }

        /**
         * For file resource type, set corresponding HTTP response content type
         */
        public void SetContentType( string fileResType, string contentType )
        {
            #region Preconditions
            if( String.IsNullOrEmpty( fileResType ) )
                throw new ArgumentNullException( "fileResType", "FileTypes -- Input file resource type is null or empty." );

            if( String.IsNullOrEmpty( contentType ) )
                throw new ArgumentNullException( "contentType", "FileTypes -- Input content type is null or empty." );
            #endregion Preconditions

            string lower_contentType = contentType.ToLower();
            IResource fileMapEntry = _store.FindUniqueResource( "FileTypeMap", Core.Props.ContentType, lower_contentType );
            if( fileMapEntry == null )
            {
                fileMapEntry = _store.NewResource( "FileTypeMap" );
                fileMapEntry.SetProp( Core.Props.ContentType, lower_contentType );
                fileMapEntry.SetProp( _propResType, fileResType );
                lock( _contentType2ResTypeMap )
                {
                    _contentType2ResTypeMap[ lower_contentType ] = fileResType;
                }
            }
        }

        /**
         * If the source of the specified resource is a file, returns the name
         * of that file. If it's a different resource, saves the stream of the
         * resource to a temporary file and returns the name of that file.
         */

        public string GetSourceFile( IResource fileResource )
        {
            #region Preconditions
            if( fileResource == null )
                throw new ArgumentNullException( "fileResource", "FileTypes -- Input resource is null or empty." );
            #endregion Preconditions

            IResource sourceResource = GetSource( fileResource );
            string filename = fileResource.GetPropText( Core.Props.Name );
            if( sourceResource == null )
            {
                return IOTools.Combine( fileResource.GetPropText( "Directory" ), filename );
            }
            if( sourceResource.Type == "FileFolder" )
            {
                return IOTools.Combine( sourceResource.GetPropText( "Directory" ), filename );
            }
            Stream source = GetStream( fileResource );
            if ( source == null )
            {
                return IOTools.Combine( fileResource.GetPropText( "Directory" ), filename );
            }
            string tempDir = GetUniqueTempDirectory();
            filename = null;
            if( fileResource.HasProp( Core.Props.Name ) )
            {
                string name = fileResource.GetStringProp( Core.Props.Name );
                int pos = name.LastIndexOf( "\\" );
                if ( pos >= 0 )
                {
                    name = name.Substring( pos + 1 );
                }
                if ( name.IndexOfAny( Path.InvalidPathChars ) < 0 )
                {
                    filename = name;
                }
            }
            if( filename == null )
            {
                filename = _randomizer.NextDouble().ToString().Substring( 2 );
            }

            string tempFileName = Path.Combine( tempDir, filename );
            Stream target = IOTools.CreateFile( tempFileName );
            if( target != null )
            {
                try
                {
                    try
                    {
                        CopyStream( source, target );
                    }
                    catch( Exception ex )
                    {
                        throw new Exception( "Error copying stream for file resource of type " + fileResource.Type +
                            " and source resource of type " + sourceResource.Type, ex );
                    }
                    return tempFileName;
                }
                finally
                {
                    source.Close();
                    target.Close();
                }
            }
            return null;
        }

        public static IResource GetSource( IResource resource )
        {
            #region Preconditions
            if( resource == null )
                throw new ArgumentNullException( "resource", "FileTypes -- Input resource is null or empty." );
            #endregion Preconditions

            IResource source = null;
            /**
             * search for the SourceLink
             */
            int[] linkIDs = resource.GetLinkTypeIds();
            foreach( int linkID in linkIDs )
            {
                if( ( Core.ResourceStore.PropTypes[ linkID ].Flags & PropTypeFlags.SourceLink ) != 0 )
                {
                    source = resource.GetLinkProp( linkID );
                    if( source != null )
                    {
                        break;
                    }
                }
            }
            return source;
        }

        /**
         * gets stream for a file resource using IStreamProvider interface
         */
        public Stream GetStream( IResource resource )
        {
            IResource source = null;
            return GetFileStream( resource, ref source );
        }

        private static Stream GetFileStream( IResource resource, ref IResource source )
        {
            #region Preconditions
            if( resource == null )
                throw new ArgumentNullException( "resource", "FileTypes -- Input resource is null or empty." );
            #endregion Preconditions

            source = GetSource( resource );
            if( source != null )
            {
                IStreamProvider provider = Core.PluginLoader.GetStreamProvider( source.Type );
                if( provider != null )
                {
                    try
                    {
                        Stream resourceStream = provider.GetResourceStream( resource );
                        if ( !resourceStream.CanRead )
                        {
                            throw new Exception( "Resource stream returned for resource of type " +
                                resource.Type + ", source resource type " + source.Type + " cannot be read" );
                        }
                        return resourceStream;
                    }
                    catch {}
                }
            }
            return null;
        }
        /**
         * copies one stream to another
         * opening/closing stream is user task
         */
        public static void CopyStream( Stream source, Stream target )
        {
            MemoryStream memStream = source as MemoryStream;
            if ( memStream != null )
            {
                memStream.WriteTo( target );
            }
            else
            {
                JetMemoryStream jmStream = source as JetMemoryStream;
                if( jmStream != null )
                {
                    jmStream.WriteTo( target );
                }
                else
                {
                    byte[] buf = new byte[4096];
                    while( true )
                    {
                        int bytesRead = source.Read( buf, 0, 4096 );
                        if ( bytesRead == 0 )
                            break;

                        target.Write( buf, 0, bytesRead );
                    }
                }
            }
        }

        /**
         * gets stream reader for a file resource using IStreamProvider interface
         * and charset information if it is set for the source
         */
        public StreamReader GetStreamReader( IResource resource )
        {
            #region Preconditions
            if( resource == null )
                throw new ArgumentNullException( "resource", "FileTypes -- Input resource is null or empty." );
            #endregion Preconditions

            IResource source = null;
            Stream stream = GetFileStream( resource, ref source );
            if( stream != null )
            {
                if( !stream.CanRead )
                {
                    stream.Close();
                }
                else
                {
                    Encoding encoding = Encoding.Default;
                    if( source != null && source.Type == "Weblink" )
                    {
                        try
                        {
                            encoding = Encoding.GetEncoding( source.GetPropText( PropCharset ) );
                        }
                        catch {}
                    }
                    try
                    {
                        return new StreamReader( stream, encoding );
                    }
                    catch {}
                }
            }
            return null;
        }

        /**
         * If the name of the file returned by GetSourceFile was a temporary file,
         * deletes that temp file.
         */

        public void CleanupSourceFile( IResource fileResource, string fileName )
        {
            #region Preconditions
            if( fileResource == null )
                throw new ArgumentNullException( "fileResource", "FileTypes -- Input file resource is null or empty." );
            #endregion Preconditions

            IResource sourceResource = GetSource( fileResource );

            if ( sourceResource != null && sourceResource.Type != "FileFolder" && File.Exists( fileName ) )
            {
                Core.NetworkAP.QueueJobAt( DateTime.Now.AddSeconds( 10 ),
                    new CleanupSourceFileDelegate( CleanupSourceFile ), fileName );
            }
        }

        private delegate void CleanupSourceFileDelegate( string fileName );

        private static void CleanupSourceFile( string fileName )
        {
            DirectoryInfo di = IOTools.GetParent( fileName );
            IOTools.DeleteFile( fileName );
            if( di != null )
            {
                IOTools.DeleteDirectory( di.FullName );
            }
        }

        public void OpenSourceFile( IResource fileResource )
        {
            #region Preconditions
            if( fileResource == null )
                throw new ArgumentNullException( "fileResource", "FileTypes -- Input file resource is null or empty." );
            #endregion Preconditions

            try
            {
                string path = GetSourceFile( fileResource );
                Process.Start( path );
            }
            catch( Exception e )
            {
                Utils.DisplayException( e, "Can't open file" );
            }
        }

        /**
         * Returns the EditFlags registry value for the specified document type.
         */

        public static int GetEditFlags( string documentClass )
        {
            RegistryKey rk = OpenDocumentKey( documentClass, false );
            if ( rk == null )
                return -1;

            object editFlagsObj = rk.GetValue( "EditFlags" );
            rk.Close();
            if ( editFlagsObj == null )
                return -1;

            if ( editFlagsObj is Array )
            {
                JetMemoryStream ms = new JetMemoryStream( (byte[] ) editFlagsObj, true );
                BinaryReader br = new BinaryReader( ms );
                return br.ReadInt32();
            }

            return (int) editFlagsObj;
        }

        /**
         * Sets the EditFlags registry value for the specified document type.
         */

        public static void SetEditFlags( string documentClass, int editFlags )
        {
            if ( editFlags == -1 )
                return;

            RegistryKey rk = OpenDocumentKey( documentClass, true );
            if ( rk == null )
                return;

            rk.SetValue( "EditFlags", editFlags );
            rk.Close();
        }

        private void RegisterFileResourceTypeImpl( string fileResType,
                                                   params string[] exts )
        {
            foreach( string ext in exts )
            {
                if( ext.Length > 0 )
                {
                    string lower_ext = ext.ToLower();
                    IResource fileMapEntry = _store.FindUniqueResource( "FileTypeMap", _propExtension, lower_ext );
                    if( fileMapEntry == null )
                    {
                        fileMapEntry = _store.BeginNewResource( "FileTypeMap" );
                    }
                    else
                    {
                        fileMapEntry.BeginUpdate();
                    }
                    try
                    {
                        fileMapEntry.SetProp( _propExtension, lower_ext );
                        fileMapEntry.SetProp( _propResType, fileResType );
                        lock( _extension2ResTypeMap )
                        {
                            _extension2ResTypeMap[ lower_ext ] = fileResType;
                        }
                    }
                    finally
                    {
                        fileMapEntry.EndUpdate();
                    }
                }
            }

            RegisterFileTypeColumns( fileResType );
        }

        public void RegisterFileTypeColumns( string fileResType )
        {
            IDisplayColumnManager dcm = Core.DisplayColumnManager;
            dcm.RegisterDisplayColumn( fileResType, 0,
//                new ColumnDescriptor(new string[] { "DisplayName", "Subject" }, 300, ColumnDescriptorFlags.AutoSize));
                new ColumnDescriptor( new string[] { "Subject" },  300, ColumnDescriptorFlags.AutoSize ) );
            dcm.RegisterDisplayColumn(fileResType, 1,
                new ColumnDescriptor( "FileType", 120, ColumnDescriptorFlags.ShowIfNotEmpty ) );
            dcm.RegisterDisplayColumn( fileResType, 10, new ColumnDescriptor( "Size", 60 ) );
            dcm.RegisterDisplayColumn( fileResType, 12, new ColumnDescriptor( "Date", 120 ) );

            dcm.RegisterMultiLineColumn( fileResType, ResourceProps.DisplayName, 0, 0, 0, 210,
                                         MultiLineColumnFlags.AnchorLeft | MultiLineColumnFlags.AnchorRight, SystemColors.WindowText, HorizontalAlignment.Left );
            dcm.RegisterMultiLineColumn( fileResType, Core.Props.Date, 0, 0, 210, 90,
                                         MultiLineColumnFlags.AnchorRight, SystemColors.WindowText, HorizontalAlignment.Right );

            // spacer column (OM-9727)
            dcm.RegisterMultiLineColumn( fileResType, new int[] {}, 0, 0, 260, 40,
                MultiLineColumnFlags.AnchorRight, SystemColors.WindowText, HorizontalAlignment.Right );

            dcm.RegisterMultiLineColumn( fileResType, _propFileType, 1, 1, 0, 200,
                                         MultiLineColumnFlags.AnchorLeft | MultiLineColumnFlags.AnchorRight, Color.FromArgb( 112, 112, 112 ), HorizontalAlignment.Left );
            dcm.RegisterMultiLineColumn( fileResType, Core.Props.Size, 1, 1, 200, 60,
                                         MultiLineColumnFlags.AnchorRight, Color.FromArgb( 112, 112, 112 ), HorizontalAlignment.Right );
        }

        private static RegistryKey OpenDocumentKey( string documentClass, bool writable )
        {
            RegistryKey rk = Registry.ClassesRoot.OpenSubKey( documentClass, writable );
            if ( rk != null )
            {
                RegistryKey rkSubVer = rk.OpenSubKey( "CurVer" );
                if ( rkSubVer != null )
                {
                    documentClass = (string) rkSubVer.GetValue( null );
                    rkSubVer.Close();
                    rk.Close();

                    rk = Registry.ClassesRoot.OpenSubKey( documentClass, writable );
                }
            }
            return rk;
        }

        private class SourceDisplayNameProvider: IDisplayNameProvider
        {
            public string GetDisplayName( IResource res )
            {
                IResource source = res.GetLinkProp( (Core.FileResourceManager as FileResourceManager)._propSource );
                return ( source == null ) ? string.Empty : source.DisplayName;
            }
        }

        private static readonly string   _omeaTrashDirectoryName = "OmeaTrash";
        private static readonly Random   _randomizer = new Random();
        private readonly HashMap         _extension2ResTypeMap = new HashMap();
        private readonly HashMap         _contentType2ResTypeMap = new HashMap();
        private readonly IResourceStore  _store;
        private readonly int             _propExtension;
        private readonly int	         _propResType;
        private readonly int             _propLastModified;
        private readonly int             _propSource;
        private readonly int             _propCharset;
        private readonly int             _propFileType;
    }
}
