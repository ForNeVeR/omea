// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.HTML;
using JetBrains.Omea.FileTypes;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.HTMLPlugin
{
    [PluginDescriptionAttribute("HTML Documents", "JetBrains Inc.", "Html files viewer and plain text extractor, for search capabilities", PluginDescriptionFormat.PlainText, "Icons/HtmlPluginIcon.png")]
    public class HTMLPlugin : IPlugin, IResourceDisplayer, IResourceTextProvider
    {
        #region IPlugin Members

        public void Register()
        {
            RegisterTypes();
            Core.PluginLoader.RegisterResourceTextProvider( "HtmlFile", this );
            Core.PluginLoader.RegisterResourceTextProvider( "TextFile", new PlainTextTextProvider() );
            Core.PluginLoader.RegisterResourceDisplayer( "HtmlFile", this );
            Core.PluginLoader.RegisterResourceDisplayer( "TextFile", new PlainTextDisplayer() );
            Core.ResourceIconManager.RegisterResourceIconProvider( "TextFile", new PlainTextIconProvider() );
        }

        public void Startup()
        {
            IUIManager UIMgr = Core.UIManager;
            if( UIMgr.IsOptionsGroupRegistered( "Folders & Files" ) )
            {
                UIMgr.AddOptionsChangesListener( "Folders & Files", "File Options", OnFileOptionsChanged );
            }
        }

        public void Shutdown()
        {
            Core.FileResourceManager.DeregisterFileResourceType( "HtmlFile" );
            Core.FileResourceManager.DeregisterFileResourceType( "TextFile" );
        }

        private void OnFileOptionsChanged( object sender, EventArgs e )
        {
            Core.ResourceAP.QueueJob(
                JobPriority.Immediate, "Register HTML Types", new MethodInvoker( RegisterTypes ) );
        }

        #endregion

        bool IResourceTextProvider.ProcessResourceText( IResource resource, IResourceTextConsumer consumer )
        {
            try
            {
                StreamReader reader = Core.FileResourceManager.GetStreamReader( resource );
                if( reader != null )
                {
                    using( reader )
                    {
                        // for weblinks, detect & set charset if it is not set
                        IResource source = resource.GetLinkProp( "Source" );
                        if( source != null )
                        {
                            string charset = source.GetPropText( Core.FileResourceManager.PropCharset );
                            if( charset.Length == 0 )
                            {
                                charset = HtmlTools.DetectCharset( reader );
                                new ResourceProxy( source ).SetPropAsync( Core.FileResourceManager.PropCharset, charset );
                                reader.BaseStream.Position = 0;
                            }
                        }
                        ProcessResourceStream( resource, source, reader, consumer );
                    }
                }
            }
            catch( ObjectDisposedException )
            {
                Core.TextIndexManager.QueryIndexing( resource.Id );
            }
            return true;
        }


        #region IResourceDisplayer Members

        public IDisplayPane CreateDisplayPane( string resourceType )
        {
            return new BrowserDisplayPane( DisplayHTML );
        }

        private static void DisplayHTML( IResource resource, AbstractWebBrowser browser, WordPtr[] wordsToHighlight )
        {
            try
            {
                StreamReader reader = Core.FileResourceManager.GetStreamReader( resource );
                if( reader != null )
                {
                    string sourceUrl = "";
                    string htmlText = Utils.StreamReaderReadToEnd( reader );
                    reader.BaseStream.Close();
                    IResource source = FileResourceManager.GetSource( resource );
                    if( source != null )
                    {
                        string url = string.Empty;
                        if( Core.ResourceStore.PropTypes.Exist( "URL" ) )
                        {
                            url = source.GetPropText( "URL" );
                        }
                        if( url.Length > 0 )
                        {
                            sourceUrl = url;
                            htmlText = HtmlTools.FixRelativeLinks( htmlText, url );
                        }
                        else
                        {
                            string directory = source.GetPropText( "Directory" );
                            if( directory.Length > 0 )
                            {
                                if( (!directory.EndsWith( "/" )) && (!directory.EndsWith( "\\" )) )
                                {
                                    directory += "/";
                                }
                                htmlText = HtmlTools.FixRelativeLinks( htmlText, directory );
                            }
                        }
                    }
                    try
                    {
                        WebSecurityContext context = WebSecurityContext.Restricted;
                        context.WorkOffline = false;
                        browser.ShowHtml( htmlText, context,
                            DocumentSection.RestrictResults( wordsToHighlight, DocumentSection.BodySection ) );
                        browser.CurrentUrl = sourceUrl;
                    }
                    catch( Exception e )
                    {
                        Trace.WriteLine( e.ToString(), "Html.Plugin" );
                    }
                }
            }
            catch( ObjectDisposedException ) {}
        }

        #endregion

        #region implementation details

        private void RegisterTypes()
        {
            string[] extsArray = LoadExtensionArray( "FilePlugin", "HtmlExts", ".html,.htm" );
            Core.FileResourceManager.RegisterFileResourceType(
                "HtmlFile", "HTML File", "Name", 0, this, extsArray );
            Core.FileResourceManager.SetContentType( "HtmlFile", "text/html" );

            extsArray = LoadExtensionArray( "FilePlugin", "PlainTextExts", ".txt" );
            Core.FileResourceManager.RegisterFileResourceType(
                "TextFile", "Text File", "Name", 0, this, extsArray );
            Core.FileResourceManager.SetContentType( "TextFile", "text/plain" );
        }

        private static string[] LoadExtensionArray( string section, string key, string defaultValue )
        {
            string exts = Core.SettingStore.ReadString( section, key );
            exts = ( exts.Length == 0 ) ? defaultValue : exts + "," + defaultValue;
            string[] extsArray = exts.Split( ',' );
            for( int i = 0; i < extsArray.Length; ++i )
            {
                extsArray[ i ] = extsArray[ i ].Trim();
            }
            return extsArray;
        }

        /**
         * processes html stream, splits it on fragments and indexes them
         */
        private IResource _currentIndexedRes;

        private void ProcessResourceStream( IResource resource, IResource source, TextReader reader,
            IResourceTextConsumer consumer )
        {
            _currentIndexedRes = resource;
            try
            {
                using( HTMLParser parser = new HTMLParser( reader ) )
                {
                    parser.CloseReader = false;
                    parser.AddTagHandler( "link", LinkHandler );
                    int docID = resource.Id;
                    string fragment;
                    while( !parser.Finished )
                    {
                        fragment = parser.ReadNextFragment();
                        if ( fragment.Length > 0 )
                        {
                            if( parser.InHeading )
                            {
                                consumer.AddDocumentHeading( docID, fragment );
                            }
                            else
                            {
                                consumer.AddDocumentFragment( docID, fragment );
                            }
                        }
                    }
                    // check whether source resource is favorite and has non-empty name property
                    // if it hasn't, or has name equyal to URL then set name from the title of HTML stream
                    if( source != null && source.Type == "Weblink" )
                    {
                        IBookmarkService service = (IBookmarkService) Core.PluginLoader.GetPluginService( typeof( IBookmarkService ) );
                        if( service != null )
                        {
                            string name = source.GetPropText( Core.Props.Name );
                            string url = string.Empty;
                            if( Core.ResourceStore.PropTypes.Exist( "URL" ) )
                            {
                                url = source.GetPropText( "URL" );
                                if( url.StartsWith( "http://" ) || url.StartsWith( "file://" ) )
                                {
                                    url = url.Substring( "http://".Length );
                                }
                                else if( url.StartsWith( "ftp://" ) )
                                {
                                    url = url.Substring( "ftp://".Length );
                                }
                            }
                            if( url.IndexOfAny( Path.GetInvalidPathChars() ) >= 0 )
                            {
                                foreach( char invalidChar in Path.GetInvalidPathChars() )
                                {
                                    url = url.Replace( invalidChar, '-' );
                                }
                            }
                            if( name.Length == 0 || url.StartsWith( name ) )
                            {
                                string title = parser.Title.Trim();
                                if( title.Length > 0 )
                                {
                                    IBookmarkProfile profile = service.GetOwnerProfile( source );
                                    string error;
                                    if( profile != null && profile.CanRename( source, out error ) )
                                    {
                                        profile.Rename( source, title );
                                        service.SetName( source, title );
                                    }
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                _currentIndexedRes = null;
            }
        }

        private void LinkHandler( HTMLParser instance, string tag )
        {
            if( _currentIndexedRes != null && instance.InHeading )
            {
                HashMap attributes = instance.ParseAttributes( tag );
                string rel = (string) attributes[ "rel" ];
                if( rel != null && rel.ToLower() == "shortcut icon" )
                {
                    new ResourceProxy( _currentIndexedRes ).SetPropAsync( "FaviconUrl", attributes[ "href" ] );
                }
            }
        }

        #endregion
    }
}
