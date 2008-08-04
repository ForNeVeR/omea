/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.RSSPlugin
{
    /// <summary>
    /// If a feed item ("RSSItem" resource type) has an enclosure attached (has property <see cref="JetBrains.Omea.RSSPlugin.Props.EnclosureURL"/>), indicates the downloading state.
    /// Defines possible values for the property stored in <see cref="JetBrains.Omea.RSSPlugin.Props.EnclosureDownloadingState"/>.
    /// </summary>
    public enum EnclosureDownloadState : int
    {
        /// <summary>
        /// Minimum value (inclusive).
        /// </summary>
        MinValue = 0,
        /// <summary>
        /// The enclosure is available, but has not been downloaded yet, and the downloading has not been planned.
        /// </summary>
        NotDownloaded = 0,
        /// <summary>
        /// Schedulled for downloading.
        /// </summary>
        Planned = 1,
        /// <summary>
        /// The downloading has completed successfully.
        /// </summary>
        Completed = 2,
        /// <summary>
        /// Failed downloading an enclosure, enclosure not available locally.
        /// </summary>
        Failed = 3,
        /// <summary>
        /// The enclosure is currently being downloaded.
        /// </summary>
        InProgress = 4,
        /// <summary>
        /// The limiting value (non-inclusive).
        /// </summary>
        MaxValue = 5
    }

    /// <summary>
    /// Wraps <see cref="EnclosureDownloadState"/> into a class. 
    /// (H) Dunno why, maybe for late binding?..
    /// </summary>
    public abstract class DownloadState
    {
        public const int NotDownloaded = (int)EnclosureDownloadState.NotDownloaded;
        public const int Planned = (int)EnclosureDownloadState.Planned;
        public const int Completed = (int)EnclosureDownloadState.Completed;
        public const int Failed = (int)EnclosureDownloadState.Failed;
        public const int InProgress = (int)EnclosureDownloadState.InProgress;
    }

    /// <summary>
    /// Implements the enclosures support.
    /// </summary>
    public class EnclosureDownloadManager
    {
        public static DateTime GetStartDownloadDateTime()
        {
            DateTime  result = DateTime.Now;
            DateTimeFormatInfo info = CultureInfo.CurrentCulture.DateTimeFormat;

            if ( Settings.UseEclosureDownloadPeriod )
            {
                try
                {
                    string startStr = Settings.EnclosureDownloadStartHour;
                    DateTime start = new DateTime( DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0 );
                    DateTime startTime = DateTime.Parse( startStr, info );
                    start = start.AddHours( startTime.Hour ).AddMinutes( startTime.Minute );

                    string finishStr = Settings.EnclosureDownloadFinishHour;
                    DateTime finish = new DateTime( DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0 );
                    DateTime finishTime = DateTime.Parse( finishStr, info );
                    finish = finish.AddHours( finishTime.Hour ).AddMinutes( finishTime.Minute );

                    if ( start >= finish )
                    {
                        finish = finish.AddDays( 1 );
                    }
                    if ( DateTime.Now < start )
                    {
                        result = start;
                    }
                    if ( DateTime.Now > finish )
                    {
                        result = start.AddDays( 1 );
                    }
                }
                catch( Exception )
                {
                    result = DateTime.Now;
                }
            }
            return result;
        }

        public static void PlanToDownload( IResource feedItem )
        {
            PlanToDownload( feedItem, null );
        }
        public static void PlanToDownload( IResource feedItem, string folder )
        {
            if ( feedItem.GetPropText( Props.EnclosureURL ).Trim().Length > 0 )
            {
                ResourceProxy proxy = new ResourceProxy( feedItem );
                proxy.BeginUpdate();
                proxy.SetProp( Props.EnclosureDownloadingState, DownloadState.Planned );
                proxy.DeleteProp( Props.EnclosureFailureReason );
                proxy.DeleteProp( Props.EnclosureTempFile );
                proxy.DeleteProp( Props.EnclosureDownloadedSize );

                if( !string.IsNullOrEmpty( folder ) )
                    proxy.SetProp( Props.EnclosurePath, folder );
                else
                    proxy.DeleteProp( Props.EnclosurePath );

                proxy.EndUpdate();
                DownloadNextEnclosure();
            }
        }
        public static void CancelDownload( IResource feedItem )
        {
            new ResourceProxy( feedItem ).SetProp( Props.EnclosureDownloadingState, DownloadState.NotDownloaded );
        }
        private static void QueueToDownload( IResource feedItem )
        {
            if ( DownloadEnclosure.Queued )
            {
                if ( Core.ResourceStore.FindResources( "RSSItem", Props.EnclosureDownloadingState, DownloadState.InProgress ).Count == 0 )
                {
                    Core.ResourceAP.CancelJobs( new JobFilter( DownloadEnclosure.CancelJob ) );
                    Core.ResourceAP.CancelTimedJobs( new JobFilter( DownloadEnclosure.CancelJob ) );
                    DownloadEnclosure.Queued = false;
                }
            }

            if ( !DownloadEnclosure.Queued )
            {
                DownloadEnclosure.Do( GetStartDownloadDateTime(), feedItem );
            }
        }
        public static void DownloadNextEnclosure()
        {
            IResourceList list = 
                Core.ResourceStore.FindResources( "RSSItem", Props.EnclosureDownloadingState, DownloadState.InProgress );
            if ( list.Count > 0 )
            {
                QueueToDownload( list[0] );
                return;
            }

            list = 
                Core.ResourceStore.FindResources( "RSSItem", Props.EnclosureDownloadingState, DownloadState.Planned );
            if ( list.Count > 0 )
            {
                QueueToDownload( list[0] );
            }
        }

        #region Enclosure State Icons

        /// <summary>
        /// Returns a 16 by 16 icon that indicates a particular state of downloading the enclosure.
        /// </summary>
        /// <param name="state">Enclosure download state.</param>
        /// <returns>The icon (the same instance for the same parameters).</returns>
        public static Icon GetEnclosureStateIcon( EnclosureDownloadState state )
        {
            if( !Core.UserInterfaceAP.IsOwnerThread )
                throw new InvalidOperationException( "This method must be accessed only from the User Interface Async Processor thread." );

            // Load the icons on the first call
            if( _arEnclosureStateIcons == null )
            {
                _arEnclosureStateIcons = new Icon[5];

                String[] sNames = new[] {"NotDownloaded", "Planned", "Completed", "Failed", "InProgress"};
                for( int a = 0; a < sNames.Length; a++ )
                    _arEnclosureStateIcons[ a ] = RSSPlugin.LoadIconFromAssembly( string.Format( "download{0}.ico", sNames[ a ] ) );
            }

            // Range check
            if( (state < EnclosureDownloadState.MinValue) || (state >= EnclosureDownloadState.MaxValue) )
                throw new ArgumentException( "The enclosure download state is out of range." );

            // Hand out the icon
            return _arEnclosureStateIcons[ (int)state ];
        }

        /// <summary>
        /// Caches the enclosure state icons to avoid loading them more than once
        /// </summary>
        protected static Icon[] _arEnclosureStateIcons = null;

        #endregion
    }

    internal class DownloadEnclosure : DownloadFileJob
    {
        private readonly IResource _resource;
        private static bool _queued = false;
        private readonly string _directory;
        private readonly int    _startPosition = 0;
        private static readonly char[] addedIllChars = {'\\', '/', ':', '?', '\"' };

        private DownloadEnclosure( IResource resource, FileStream file, string directory, int startPosition )
            : base( resource.GetStringProp( Props.EnclosureURL ), file, startPosition )
        {
            Guard.NullArgument(resource, "resource");
            Guard.EmptyStringArgument(directory, "directory");

            _resource = resource;
            _directory = ValidatePath(directory);
            _startPosition = startPosition;
        }

        private static string ValidatePath(string path)
        {
            Guard.EmptyStringArgument( path, "path" );
            foreach ( char invalid in Path.InvalidPathChars )
                path = path.Replace( invalid, ' ' );

            return path;
        }

        //---------------------------------------------------------------------
        //  Path.InvalidPathChars used in the ValidatePath:
        //  1. Obsolete in .Net 2/3
        //  2. Does not contain several obviously errorneous symbols which
        //     prevent creating directory (at least on XP)
        //  This method perfroms simple and compatible workaround.
        //---------------------------------------------------------------------
        private static string ValidateName( string name )
        {
            name = ValidatePath( name );
            foreach ( char invalid in addedIllChars )
                name = name.Replace( invalid, ' ' );

            return name;
        }

        public static bool CancelJob( AbstractJob job )   // returns true - cancel, returns false - do not cancel
        {
            return job is DownloadEnclosure;
        }

        public static void Do( DateTime at, IResource feedItem )
        {
            string enclosureUrl = feedItem.GetPropText( Props.EnclosureURL ).Trim();
            if ( enclosureUrl.Length == 0 )
            {
                return;
            }
            IResource feed = feedItem.GetLinkProp( -Props.RSSItem );
            string directory = FindDownloadDirectory( feedItem, feed );

            try
            {
                Directory.CreateDirectory( directory );
                string destFullPath = null;
                FileStream file = null;
                int startPosition = 0;
                if ( feedItem.GetIntProp( Props.EnclosureDownloadingState ) == DownloadState.InProgress )
                {
                    string enclosureTempFile = feedItem.GetPropText( Props.EnclosureTempFile );
                    if ( File.Exists( enclosureTempFile ) )
                    {
                        try
                        {
                            file = File.OpenWrite( enclosureTempFile );
                            destFullPath = enclosureTempFile;
                            startPosition = (int)file.Length;
                            file.Seek( startPosition, SeekOrigin.Begin );
                        }
                        catch ( Exception exception )
                        {
                            Tracer._TraceException( exception );
                        }
                    }
                }
                if ( destFullPath == null && file == null )
                {
                    destFullPath = FindFreeFileName( enclosureUrl, directory, true );
                    file = File.Create( destFullPath );
                }

                new ResourceProxy( feedItem ).SetProp( Props.EnclosureTempFile, destFullPath );
                Core.NetworkAP.QueueJobAt( at, new DownloadEnclosure( feedItem, file, directory, startPosition ) );
                _queued = true;
            }
            catch ( Exception exception )
            {
                _queued = false;
                ResourceProxy proxy = new ResourceProxy( feedItem );
                proxy.BeginUpdate();
                proxy.SetProp( Props.EnclosureDownloadingState, DownloadState.Failed );
                proxy.SetProp( Props.EnclosureFailureReason, exception.Message );
                proxy.EndUpdate();
                ShowDesktopAlert( DownloadState.Failed, "Downloading Failed", feedItem.DisplayName, exception.Message );
                EnclosureDownloadManager.DownloadNextEnclosure();
            }
        }

        //---------------------------------------------------------------------
        //  First look at the feed item - if it is downloaded through the rule
        //  action then the path is set. Otherwise, the specific path can be
        //  set for a particular feed. Finally take the default path from settings
        //  and (if necessary) append the name of the feed.
        //---------------------------------------------------------------------
        private static string FindDownloadDirectory( IResource feedItem, IResource feed )
        {
            string dir = null;
            if( feedItem.HasProp( Props.EnclosurePath ) )
            {
                dir = feedItem.GetStringProp( Props.EnclosurePath );
            }
            else
            {
                dir = feed.GetStringProp( Props.EnclosurePath );
                if ( string.IsNullOrEmpty( dir ) )
                {
                    dir = Settings.EnclosurePath;
                    string feedName = feed.GetPropText( Core.Props.Name );
                    if ( Settings.CreateSubfolderForEveryFeed && !string.IsNullOrEmpty( feedName ))
                    {
                        dir = Path.Combine( dir, ValidateName( feedName ) );
                    }
                }
                dir = ValidatePath( dir );
            }
            return dir;
        }

        private static void ShowDesktopAlert( int downloadState, string from, string subject, string body )
        {
            if ( !Settings.ShowDesktopAlertWhenEncosureDownloadingComplete && downloadState == DownloadState.Completed )
            {
                return;
            }
            if ( !Settings.ShowDesktopAlertWhenEncosureDownloadingFailed && downloadState == DownloadState.Failed )
            {
                return;
            }
            Core.UIManager.ShowDesktopAlert( EnclosureDownloadStateColumn.Instance.ImageList, downloadState, from, 
                subject, body, null );
        }

        private static string FindFreeFileName( string enclosureUrl, string directory, bool isPart )
        {
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension( enclosureUrl );
            string extension = Path.GetExtension( enclosureUrl );
            if ( isPart )
                extension += ".part";

            fileNameWithoutExtension = ValidateName( fileNameWithoutExtension );
            extension = ValidateName( extension );
            string destFullPath = Path.Combine( directory, fileNameWithoutExtension );
            
            destFullPath += extension;
            int postFix = 0;
            while ( File.Exists( destFullPath ) )
            {
                ++postFix;
                destFullPath = Path.Combine( directory, fileNameWithoutExtension + postFix + extension );
            }
            destFullPath = ValidatePath( destFullPath );

            return destFullPath;
        }

        protected override void Execute()
        {
            if ( _resource.Id != -1 && _resource.GetIntProp( Props.EnclosureDownloadingState ) != DownloadState.NotDownloaded )
            {
                // Determine if we want update some of the resource properties (create proxy for that)
                int nTotalLength = GetLength();
                bool bUpdateState = _resource.GetIntProp( Props.EnclosureDownloadingState ) != DownloadState.InProgress;
                bool bUpdateLength = ((nTotalLength > 0) && (nTotalLength != _resource.GetIntProp( Props.EnclosureSize )));
                if( (bUpdateState) || (bUpdateLength) )
                {
                    ResourceProxy proxy = new ResourceProxy( _resource );
                    proxy.BeginUpdate();
                    if( bUpdateState )
                        proxy.SetProp( Props.EnclosureDownloadingState, DownloadState.InProgress );
                    if( bUpdateLength )
                        proxy.SetProp( Props.EnclosureSize, nTotalLength + _startPosition );
                    proxy.EndUpdateAsync();
                }

                // Check if the downloaded size should be updated
                // As we take the actual size from the disc file, not from the property, its accuracy is not needed; overwrite the property only if the percent value changes, as it's used only for the percentage display
                if( nTotalLength > 0 ) // The total-size info is available (if it's not, we do not have the percentage anyway)
                {
                    int nSize = GetDownloadedSize(); // Size we've downloaded
                    int nOldSize = _resource.GetIntProp( Props.EnclosureDownloadedSize ); // The prev size property value
                    if( (nSize * 100 / nTotalLength) != (nOldSize * 100 / nTotalLength) ) // Percentage has changed, should write a new value
                        Core.UserInterfaceAP.QueueJob( "Update Enclosure Downloaded Size", new UpdateDownloadedSizeDelegate( UpdateDownloadedSize ), nSize ); // Schedulle to the UI AP so that it merges the too-frequent updates for enclosures that download too fast, that reduces flicker
                }

                // Do the download step
                base.Execute();
            }
            else
            {
                InvokeAfterWait( null, null );
                new ResourceProxy( _resource ).SetProp( Props.EnclosureDownloadingState, DownloadState.NotDownloaded );
                _queued = false;
                EnclosureDownloadManager.DownloadNextEnclosure();
            }
        }

        public static bool Queued { get { return _queued; } set { _queued = value; } }

        protected override void Ready( )
        {
            ResourceProxy proxy = new ResourceProxy( _resource );
            proxy.BeginUpdate();
            if ( !Successfull )
            {
                proxy.SetProp( Props.EnclosureDownloadingState, DownloadState.Failed );
                string lastException = null;
                if ( LastException != null )
                {
                    lastException = LastException.Message;
                }
                proxy.SetProp( Props.EnclosureFailureReason, lastException );
                proxy.DeleteProp( Props.EnclosureTempFile );
                ShowDesktopAlert( DownloadState.Failed, "Downloading Failed", _resource.DisplayName, lastException );
            }
            else
            {
                proxy.SetProp( Props.EnclosureDownloadingState, DownloadState.Completed );
                ShowDesktopAlert( DownloadState.Completed, "Downloading Completed", _resource.DisplayName, null );
            }
            proxy.EndUpdate();
            _queued = false;
            EnclosureDownloadManager.DownloadNextEnclosure();
        }

        protected override void StoreStream( Stream stream )
        {
            string path = string.Empty;
            FileStream file = FileStream;
            file.Close();
            try
            {
                path = FindFreeFileName( Url, _directory, false );
                File.Move( file.Name, path );
                new ResourceProxy( _resource ).SetProp( Props.EnclosureTempFile, path );
            }
            catch ( IOException exception )
            {
                Tracer._TraceException( exception );
            }
            catch ( ArgumentException exception )
            {
                Tracer._TraceException( exception );
                Core.UIManager.ShowSimpleMessageBox( "Download Enclosure Failed", "Illegal characters in path: \'" + path + "\'" );
            }
        }

        /// <summary>
        /// Updates the downloaded size of an enclosure.
        /// </summary>
        protected void UpdateDownloadedSize(int nNewSize)
        {
            if( _resource.IsDeleted )
                return;
            new ResourceProxy( _resource ).SetPropAsync( Props.EnclosureDownloadedSize, nNewSize );
        }
        /// <summary>
        /// Delegate for <see cref="UpdateDownloadedSize"/>.
        /// </summary>
        public delegate void UpdateDownloadedSizeDelegate(int nNewSize);
    }
}