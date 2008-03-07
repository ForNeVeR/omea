/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.FileTypes;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Interop;

namespace JetBrains.Omea.ResourceTools
{
    public class FavIconManager
    {
        private int _propFavIcon;
        private int _propFavIconURL;
        private int _propFavIconError;
        private static HashMap _favIcons = new HashMap();

        public FavIconManager()
        {
            Core.ResourceAP.RunJob( new MethodInvoker( RegisterProps ) );
        }

        public void RegisterProps()
        {
            IResourceStore store = Core.ResourceStore;
            store.ResourceTypes.Register( FavIconResource, string.Empty, ResourceTypeFlags.NoIndex | ResourceTypeFlags.Internal );

            _propFavIcon = store.PropTypes.Register( "FavIcon", PropDataType.Blob, PropTypeFlags.Internal );
            _propFavIconError = store.PropTypes.Register( "FavIconError", PropDataType.Bool, PropTypeFlags.Internal );
            _propFavIconURL = store.PropTypes.Register( "FavIconURL", PropDataType.String, PropTypeFlags.Internal );

            store.RegisterUniqueRestriction( FavIconResource, _propFavIconURL );
        }

        public Icon GetResourceFavIcon( string resourceUrl )
        {
            Guard.EmptyStringArgument( resourceUrl, "resourceUrl" );
            string iconUrl = GetFavIconUrl( resourceUrl );
            if ( iconUrl == null )
            {
                return null;
            }
            return GetFavIcon( iconUrl );
        }

        private static string GetFavIconUrl( string resourceUrl )
        {
            Guard.EmptyStringArgument( resourceUrl, "resourceUrl" );
            try
            {
                string host = new Uri( resourceUrl ).Host;
                return resourceUrl.Substring( 0, resourceUrl.IndexOf( host ) + host.Length ) + "/favicon.ico";
            }
            catch ( Exception exception )
            {
                Tracer._TraceException( exception );
            }
            return null;
        }

        public Icon GetFavIcon( string favIconUrl )
        {
            Guard.EmptyStringArgument( favIconUrl, "favIconUrl" );
            HashMap.Entry entry = _favIcons.GetEntry( favIconUrl );
            if ( entry != null )
            {
                return (Icon)entry.Value;
            }
            try
            {
                IResource resFavIcon = GetFavIconResource( favIconUrl );
                if ( resFavIcon != null )
                {
                    Stream stream = resFavIcon.GetBlobProp( _propFavIcon );
                    if ( stream != null )
                    {
                        Icon icon = new Icon( stream );
                        icon = new Icon( icon, 16, 16 );
                        _favIcons.Add( favIconUrl, icon );
                        return icon;
                    }
                }
            }
            catch
            {
                _favIcons.Add( favIconUrl, null );
            }

            return null;
        }

        public void DownloadFavIcon( string resourceUrl )
        {
            Guard.EmptyStringArgument( resourceUrl, "resourceUrl" );
            string favIconUrl = GetFavIconUrl( resourceUrl );
            if ( favIconUrl == null )
            {
                return;
            }
            IResource resFavIcon = GetFavIconResource( favIconUrl );
            if ( resFavIcon != null && resFavIcon.HasProp( _propFavIconError ) )
            {
                return;
            }

            Core.ResourceAP.QueueJob( new FavIconDownloadJob( this, favIconUrl ) );
        }

        internal IResource GetFavIconResource( string favIconURL )
        {
            Guard.EmptyStringArgument( favIconURL, "favIconURL" );
            return Core.ResourceStore.FindUniqueResource( FavIconResource, _propFavIconURL, favIconURL );
        }
                                  
        public int PropFavIcon      { get { return _propFavIcon; } }
        public int PropFavIconUrl   { get { return _propFavIconURL; } }
        public int PropFavIconError { get { return _propFavIconError; } }
        public const string FavIconResource = "FavIcon";

		/// <summary>
		/// Returns a path to the file that contains the given icon.
		/// If the file does not exist yet, it is created in the Omea trash folder. If there is one already, it will be reused.
		/// </summary>
		/// <param name="icon">Icon that should be saved as a file.</param>
		/// <param name="origin">Resource that is somehow related to the icon. It is used to generate an unique file name for the icon. May be <c>Null</c>.</param>
		/// <param name="relation">Describes how the resource specified by <paramref name="origin"/> relates to the icon being saved. This helps to get an unique file name in case there is more than one icon related to the resource. May be <c>Null</c> if only one icon is expected for the resource.</param>
		/// <param name="bRenderToBitmap">If set to <c>False</c>, saves the icon into an icon file (.ico). If <c>True</c>, renders the icon to an in-memory bitmap, and saves the produced bitmap as a PNG file.</param>
		/// <returns>Full path to the icon file.</returns>
		public static string GetIconFile( Icon icon, object origin, object relation, bool bRenderToBitmap )
		{
			if( icon == null )
				throw new ArgumentNullException( "icon" );

			// Generate the file name
			string extension = bRenderToBitmap ? ".png" : ".ico";
			string name = String.Format( "Icon-{0:X8}-{1:X8}-{2:X8}{3}", icon.Handle.ToInt32(), (origin != null ? origin.GetHashCode() : 0), (relation != null ? relation.GetHashCode() : 0), extension );

			// Get access to the Omea trash folder
			string folderTemp = FileResourceManager.GetTrashDirectory();
			if( !Directory.Exists( folderTemp ) )
				Directory.CreateDirectory( folderTemp );
			string folder = Path.Combine( folderTemp, "RenderIcons" );
			if( !Directory.Exists( folder ) )
				Directory.CreateDirectory( folder );

			// Get the combined file path
			string path = Path.Combine( folder, name );

			// Save the icon (if it does not exist)
			if( !File.Exists( path ) )
			{
                try
                {
                    Bitmap bmpWrap = GraphicalUtils.ConvertIco2Bmp( icon );
                    using( FileStream fs = new FileStream( path, FileMode.Append, FileAccess.Write, FileShare.Read ) )
                        bmpWrap.Save( fs, ImageFormat.Png );
                }
                catch( Exception e )
                {
                    Core.ReportBackgroundException( e );
                }
			}

			return path;
		}

    }

    internal class FavIconDownloadJob : AbstractJob
    {
        private IResource _resFavIcon;
        private readonly string         _url;
        private readonly FavIconManager _favIconManager;
        
        public FavIconDownloadJob( FavIconManager favIconManager, string url )
        {
            Guard.EmptyStringArgument( url, "url" );
            Guard.NullArgument( favIconManager, "favIconManager" );
            _favIconManager = favIconManager;
            _url = url;
        }

        protected override void Execute()
        {
            _resFavIcon = _favIconManager.GetFavIconResource( _url );
            if ( _resFavIcon == null )
            {
                _resFavIcon = Core.ResourceStore.BeginNewResource( FavIconManager.FavIconResource );
                _resFavIcon.SetProp( _favIconManager.PropFavIconUrl, _url );
                _resFavIcon.EndUpdate();
            }
            if ( !_resFavIcon.HasProp( _favIconManager.PropFavIcon ) )
            {
                DownloadResourceBlobJob job = 
                    new DownloadResourceBlobJob( _resFavIcon, _favIconManager.PropFavIcon, _url, new ReadyDelegate( DownloadFavicon ) );
                Core.NetworkAP.QueueJob( JobPriority.AboveNormal, job );
            }
        }

        private void DownloadFavicon( bool ready )
        {
            if ( !ready )
            {
                new ResourceProxy( _resFavIcon ).SetProp( _favIconManager.PropFavIconError, true );
            }
        }
    }
}
