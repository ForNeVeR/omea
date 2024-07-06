// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
    /// <summary>
    /// The pane which displays a single post in an RSS feed.
    /// </summary>
    internal class RSSItemView : MessageDisplayPane
    {
        private const string cLinkAlias = "Source";
        private const string _SummaryStylePath = "RSSPlugin.Styles.Summary.css";
        private const string _DefaultStylePath = "RSSPlugin.Styles.Default.css";
        private const string _LocalLinkIconPath = "RSSPlugin.Icons.Anchor.gif";

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private Container components = null;

        /// <summary>
        /// The Web Security Context that displays the RSS Item preview by default, in the restricted environment.
        /// </summary>
        private readonly WebSecurityContext _ctxRestricted;

        private string          _font;
        private int             _fontSize;
        private bool            _useDetailedURLs;
        private bool            _showSummary;
        private static string   _summaryStyle, _defaultStyle;
        private static string   _iconPath;

        public RSSItemView()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            // Initialize the security context
            _ctxRestricted = WebSecurityContext.Restricted;
            _ctxRestricted.WorkOffline = false;	// Enable downloading of the referenced content

            ReadRssItemFontAttributes();
            ReadItemFormattingOption();

            Core.UIManager.AddOptionsChangesListener( "Internet", "Feeds", ReadRssItemFontAttributesHandler);
            Core.UIManager.AddOptionsChangesListener( "Omea", "General", ReadRssItemFontAttributesHandler);
        }

        private void ReadRssItemFontAttributesHandler( object sender, EventArgs args )
        {
            ReadRssItemFontAttributes();
            ReadItemFormattingOption();
            Core.ResourceBrowser.RedisplaySelectedResource();
        }
        private void ReadRssItemFontAttributes()
        {
            //  Initialize font from local settings or from the Core.UIManager.
            _font = Core.UIManager.DefaultFontFace;
            _fontSize = (int)Core.UIManager.DefaultFontSize;

            bool overriden = Core.SettingStore.ReadBool( IniKeys.Section, "RSSPostFontOverride", false );
            if( overriden )
            {
                _font = Core.SettingStore.ReadString( IniKeys.Section, "RSSPostFont", Core.UIManager.DefaultFontFace );
                _fontSize = Core.SettingStore.ReadInt( IniKeys.Section, "RSSPostFontSize", (int)Core.UIManager.DefaultFontSize );
            }
        }
        private void ReadItemFormattingOption()
        {
            _useDetailedURLs = Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.UseDetailedURLs, false );
            _showSummary = Core.SettingStore.ReadBool( IniKeys.Section, IniKeys.ShowSummary, false );
        }

        public static string SummaryStyle
        {
            set{ _summaryStyle = value; }
            get
            {
                if( _summaryStyle == null )
                    _summaryStyle = LoadStyle( _SummaryStylePath );

                return _summaryStyle;
            }
        }

        public static string DefaultStyle
        {
            set{ _defaultStyle = value; }
            get
            {
                if( _defaultStyle == null )
                    _defaultStyle = LoadStyle( _DefaultStylePath );

                return _defaultStyle;
            }
        }

        public static string IconPath
        {
            get
            {
                if( _iconPath == null )
                {
                    Assembly theAssm = Assembly.GetExecutingAssembly();
                    Stream strm = theAssm.GetManifestResourceStream( _LocalLinkIconPath );
                    byte[] content = new BinaryReader( strm ).ReadBytes( (int) strm.Length );

                    string  tempPath = Path.Combine( Path.GetTempPath(), "Anchor.gif" );
                    FileStream outstream = null;
                    try
                    {
                        outstream = new FileStream( tempPath, FileMode.Create );
                    }
                    catch( DirectoryNotFoundException )
                    {
                        try
                        {
                            tempPath = Path.Combine( Application.UserAppDataPath, "Anchor.gif" );
                            outstream = new FileStream( tempPath, FileMode.Create );
                        }
                        catch( DirectoryNotFoundException )
                        {
                            //  do nothing. May be try out another path later.
                        }
                    }
                    if( outstream != null )
                    {
                        BinaryWriter writer = new BinaryWriter( outstream );
                        writer.Write( content );
                        writer.Close();
                        _iconPath = tempPath;
                    }
                }
                return _iconPath;
            }
        }

        private static string  LoadStyle( string path )
        {
            Assembly theAssm = Assembly.GetExecutingAssembly();
            Stream strm = theAssm.GetManifestResourceStream( path );
            return Utils.StreamToString( strm );
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if(components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
        }
        #endregion

        public override void DisplayResource( IResource rssItem, WordPtr[] wordsToHighlight )
        {
            string subject = rssItem.GetPropText( Core.Props.Subject );
            ShowSubject( subject, wordsToHighlight );

            IResource feed = rssItem.GetLinkProp( -Props.RSSItem );
            if ( feed != null && feed.HasProp( Props.AutoFollowLink ) && rssItem.GetPropText( Props.Link ).Length > 0 )
            {
                AttachWebBrowser();
                Core.WebBrowser.NavigateInPlace( rssItem.GetPropText( Props.Link ) );
            }
            else
            {
                StringBuilderDecor decor = new StringBuilderDecor( "<html>" );
                decor.AppendText( GetItemStyle() );
                decor.AppendText( Core.MessageFormatter.StandardStyledHeader( subject ) );

                string body = rssItem.GetPropText( Core.Props.LongBody );
                if( _showSummary )
                {
                    RssBodyConstructor.ConstructSummary( rssItem, SummaryStyle, body, decor );
                }

                //-------------------------------------------------------------
                // Update the search results offsets
                //-------------------------------------------------------------
                if( wordsToHighlight != null )
                {
                    int	inc = decor.ToString().Length;	// Prepended length

                    for( int a = 0; a < wordsToHighlight.Length; a++ )
                        wordsToHighlight[a].StartOffset += inc;
                }

                ProcessBody( decor, body, rssItem );

                //-------------------------------------------------------------
                if( !_useDetailedURLs )
                    RssBodyConstructor.AppendLink( rssItem, decor, cLinkAlias );
                else
                    RssBodyConstructor.AppendLink( rssItem, decor );

                RssBodyConstructor.AppendRelatedPosts( rssItem, decor, _useDetailedURLs );
                RssBodyConstructor.AppendEnclosure( rssItem, decor );
                RssBodyConstructor.AppendSourceTag( rssItem, decor );
                RssBodyConstructor.AppendCommentsTag( rssItem, decor );
                decor.AppendText( "</body></html>" );
                ShowHtml( decor.ToString(), _ctxRestricted, wordsToHighlight );
            }

            IResourceList feeds = rssItem.GetLinksOfType( Props.RSSFeedResource, Props.RSSItem );
            IResource owner = Core.ResourceBrowser.OwnerResource;
            if (( owner != null && owner.Type == Props.RSSFeedGroupResource ) ||
                ( feeds.Count > 0 && owner == feeds [ 0 ] ) )
            {
                RSSPlugin.GetInstance().RememberSelection( owner, rssItem );
            }
        }

        private string GetItemStyle()
        {
            string style = "<body style=\"font-family: " + _font + "; font-size: " + _fontSize + "pt; \">\n";
            style += "<style type=\"text/css\">" + DefaultStyle + "</style>\n";
            style += Core.MessageFormatter.DualMediaSubjectStyle + "\n";
            return style;
        }

        /// <summary>
        /// Iterate over "LinkedPost" links and insert an anchors to local
        /// resources right after the normal http links withing the post body.
        /// </summary>
        private static void  ProcessBody( BodyWriter decor, string body, IResource rssItem )
        {
            IResourceList linked = rssItem.GetLinksFrom( Props.RSSItemResource, Props.LinkedPost );
            foreach( IResource res in linked )
            {
                string link = res.GetStringProp( Props.Link );
                if( !string.IsNullOrEmpty( link ) )
                {
                    string  template = "href=\"" + link + "\"";
                    int     indexHref = body.IndexOf( template );
                    if( indexHref != -1 )
                    {
                        int indexClose = body.IndexOf( "</a>", indexHref + template.Length );
                        int indexImmClose = body.IndexOf( "/>", indexHref + template.Length );
                        int indexAnyHref = body.IndexOf( "href=", indexHref + template.Length );

                        //  We do not expect any extra "href" link nor immediate close tag
                        //  inside current "<a href" and its closing "</a>" tag.
                        if( indexClose != -1 && ( indexAnyHref == -1 || indexAnyHref > indexClose ) &&
                            ( indexImmClose == -1 || indexImmClose >= indexClose ))
                        {
                            InsertLocalLink( ref body, res, indexClose );
                        }
                    }
                }
            }
            decor.AppendText( body );
            decor.AppendText( "<p><hr/>" );
        }

        private static void  InsertLocalLink( ref string body, IResource res, int closeTagPos )
        {
            string derTitle = res.DisplayName.Replace( '"', ' ' );
            string subst;
            if( IconPath != null )
            {
                subst = "<a class=\"insidelink\" title=\"" + derTitle + "\" href=\"omea://" +
                        res.Id + "\"><img border=\"0\" src=\"" + IconPath + "\"></a>";
            }
            else
            {
                //  In the trouble cases: if we did not manage to construct a work or
                //  application path or did not write the necessary resource into a file.
                subst = "<a class=\"insidelink\" title=\"" + derTitle + "\" href=\"omea://" +
                        res.Id + "\">&nbsp(local link)&nbsp</a>";
            }

            body = body.Substring( 0, closeTagPos + 4 ) + subst + body.Substring( closeTagPos + 4 );
        }
    }
}
