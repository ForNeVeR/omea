/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using EMAPILib;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class MailBodyView : MessageDisplayPane
    {
        private const string _WaitMessage = "<html><body style=\"font-family: Tahoma; font-size: 8pt; text-align: center;\">" + 
                                            "Loading message body...</body></html>";
        private const string _NoPreviewTemplate = "No preview text available...";
        private const string _Pattern1 = "%%1%%";

        private const string _IniSection = "Outlook";
        private const string _StylePath = "OutlookPlugin.Styles.AttachmentStyle.txt";
        private const string _ScriptPath = "OutlookPlugin.Styles.ExpandScript.txt";
        private const string _AttachIconFileName = "OutlookPlugin.Icons.attachment.png";
        private const string _LargeImageIndicatorFileName = "OutlookPlugin.Icons.zoom.png";

//        private const int _SmallFrameSize = 40;
//        private const int _LargeFrameSize = 80;

        private System.ComponentModel.Container _components = null;

        /// <summary>
        /// A Web browser security context that is to be applied when displaying email 
        /// messages with the highest security, prohibiting downloading external images.
        /// Embedded images are to be displayed by default.
        /// </summary>
        private readonly WebSecurityContext _ctxRestrictedWoImages;
        /// <summary>
        /// A Web browser security context that is to be applied when displaying email
        /// messages with the nearly-highest security, showing all the external content,
        /// but still prohibiting most actions that may compromise the security.
        /// </summary>
        private readonly WebSecurityContext _ctxRestrictedWithImages;

        private Panel        _panel1;
        private JetLinkLabel _linkShowPictures;
        private Panel        _panel2;
        private JetLinkLabel _linkReceiveAttachedResources;

        private bool _disposed = false;
        private int _lastResourceId;

        private IResource _resource;
        private static string _font;
        private static int    _fontSize;
        private static string _expandScript, _attachmentStyle;
        private static string _largeImageIndicatorPath;

        //  This is a background of the frames for displaying the application-
        //  dependendent shell icons.
        private static readonly Brush _backBrush = new SolidBrush(Color.FromArgb(0xEB, 0xEB, 0xEB));

        /// <summary>
        /// Holds words that should be highlighted so that we could apply the highlighting when the mail body gets loaded at last.
        /// </summary>
        private WordPtr[] _wordsToHighlight = null;

        public MailBodyView()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();
            _panel2.Visible = false;
            _panel1.Visible = false;

            // Initialize the contexts
            // • Without images — work offline, show pictures that come from the file system of browser's cache only
            _ctxRestrictedWoImages = WebSecurityContext.Restricted;
            _ctxRestrictedWoImages.WorkOffline = true;
            _ctxRestrictedWoImages.ShowPictures = Core.SettingStore.ReadBool( "General", "MailShowCachedPictures", false );

            // • With images — work online, show pictures that come from the file system or are downloaded from the Internet
            _ctxRestrictedWithImages = WebSecurityContext.Restricted;
            _ctxRestrictedWithImages.WorkOffline = false;
            _ctxRestrictedWithImages.ShowPictures = true;
		
            ReadMailFontAttributes();

            Core.UIManager.AddOptionsChangesListener( "MS Outlook", "Outlook General", ReadMailFontAttributesHandler );
            Core.UIManager.AddOptionsChangesListener( "Omea", "General", ReadMailFontAttributesHandler );
        }

        private static void  ReadMailFontAttributesHandler( object sender, EventArgs args )
        {
            ReadMailFontAttributes();
            Core.ResourceBrowser.RedisplaySelectedResource();
        }
        private static void  ReadMailFontAttributes()
        {
            //  Initialize font from local settings or from the Core.UIManager.
            _font = Core.UIManager.DefaultFontFace;
            _fontSize = (int)Core.UIManager.DefaultFontSize;

            bool overriden = Core.SettingStore.ReadBool( _IniSection, "MailFontOverride", false );
            if( overriden )
            {
                _font = Core.SettingStore.ReadString( _IniSection, "MailFont", Core.UIManager.DefaultFontFace );
                _fontSize = Core.SettingStore.ReadInt( _IniSection, "MailFontSize", (int)Core.UIManager.DefaultFontSize );
            }
        }

        private void CheckDisposed()
        {
            if ( _disposed )
            {
                throw new ObjectDisposedException( "MailBodyView was disposed" );
            }
        }

        protected override void Dispose( bool disposing )
        {
            if ( !_disposed )
            {
                if( disposing )
                {
                    if(_components != null)
                    {
                        _components.Dispose();
                    }
                }
                base.Dispose( disposing );
            }
            _disposed = true;
        }

        #region Component Designer generated code
        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this._linkReceiveAttachedResources = new JetLinkLabel();
            this._linkShowPictures = new JetLinkLabel();
            this._panel1 = new Panel();
            this._panel2 = new Panel();
            this.SuspendLayout();
            // 
            // label1
            // 
            this._linkReceiveAttachedResources.BackColor = SystemColors.Info;
            this._linkReceiveAttachedResources.Dock = System.Windows.Forms.DockStyle.Top;
            this._linkReceiveAttachedResources.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._linkReceiveAttachedResources.Location = new System.Drawing.Point(0, 24);
            this._linkReceiveAttachedResources.Name = "_linkReceiveAttachedResources";
            this._linkReceiveAttachedResources.Size = new System.Drawing.Size(560, 24);
            this._linkReceiveAttachedResources.TabIndex = 3;
            this._linkReceiveAttachedResources.TabStop = true;
            this._linkReceiveAttachedResources.Text = "Click to receive attached resources";
            this._linkReceiveAttachedResources.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._linkReceiveAttachedResources.Click += new System.EventHandler(this.OnReceiveResources);
            //
            // _panel1
            //
            this._panel1.BackColor = SystemColors.Info;
            this._panel1.BorderStyle = BorderStyle.FixedSingle;
            this._panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this._panel1.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._panel1.Location = new System.Drawing.Point(0, 0);
            this._panel1.Name = "_panel1";
            this._panel1.Size = new System.Drawing.Size(560, 24);
            //
            // _panel2
            //
            this._panel2.BackColor = SystemColors.Info;
            this._panel2.BorderStyle = BorderStyle.FixedSingle;
            this._panel2.Dock = System.Windows.Forms.DockStyle.Top;
            this._panel2.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Underline, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._panel2.Location = new System.Drawing.Point(0, 0);
            this._panel2.Name = "_panel2";
            this._panel2.Size = new System.Drawing.Size(560, 24);
            // 
            // linkLabel1
            // 
            this._linkShowPictures.BackColor = SystemColors.Info;
            this._linkShowPictures.Dock = System.Windows.Forms.DockStyle.Top;
            this._linkShowPictures.Font = new System.Drawing.Font("Tahoma", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._linkShowPictures.Location = new System.Drawing.Point(0, 0);
            this._linkShowPictures.Name = "_linkShowPictures";
            this._linkShowPictures.Size = new System.Drawing.Size(560, 24);
            this._linkShowPictures.TabIndex = 4;
            this._linkShowPictures.TabStop = true;
            this._linkShowPictures.Text = "Click to show pictures";
            this._linkShowPictures.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this._linkShowPictures.Click += new System.EventHandler(this.OnShowPictures);
            // 
            // MailBodyView
            // 
            this.Controls.Add(this._panel1);
            this.Controls.Add(this._panel2);
            _panel1.Controls.Add(this._linkShowPictures);
            _panel2.Controls.Add(this._linkReceiveAttachedResources);
            this.Name = "MailBodyView";
            this.Size = new System.Drawing.Size(560, 408);
            this.ResumeLayout(false);

        }
        #endregion

        #region Event Handlers
        private void OnReceiveResources(object sender, EventArgs e)
        {
            IResourceList attachments = _resource.GetLinksOfType( null, PROP.InternalAttachment );
            foreach ( IResource attachment in attachments.ValidResources )
            {
                if ( attachment.HasProp( PROP.ResourceTransfer ) )
                {
                    UnpackResourcesAction action = new UnpackResourcesAction();
                    action.Unpack( attachment );
                    break;                    
                }
            }
        }

        private void OnShowPictures(object sender, EventArgs e)
        {
            new ResourceProxy( _resource ).SetProp( PROP.ShowPictures, true );
            _panel1.Visible = false;
            DisplayResource( _resource );
        }
        #endregion Event Handlers

        #region DisplayResource
        private static string Script
        {
            get
            {
                if( _expandScript == null )
                {
                    Assembly theAssm = Assembly.GetExecutingAssembly();
                    Stream strm = theAssm.GetManifestResourceStream( _ScriptPath );
                    _expandScript = Utils.StreamToString( strm );
                }
                return _expandScript;
            }
        }

        private static string Style
        {
            get
            {
                if( _attachmentStyle == null )
                {
                    Assembly theAssm = Assembly.GetExecutingAssembly();
                    Stream strm = theAssm.GetManifestResourceStream( _StylePath );
                    _attachmentStyle = Utils.StreamToString( strm );

                    //  Parameterize style with the actual location of an icon which
                    //  indicates resizeable images.
                    _attachmentStyle = _attachmentStyle.Replace( _Pattern1, LargeImageIndicatorPath );
                }
                return _attachmentStyle;
            }
        }

        private static string LargeImageIndicatorPath
        {
            get
            {
                if( _largeImageIndicatorPath == null )
                {
                    _largeImageIndicatorPath = Path.Combine( Path.GetTempPath(), _LargeImageIndicatorFileName );

                    Assembly theAssm = Assembly.GetExecutingAssembly();
                    Image img = Utils.GetResourceImageFromAssembly( theAssm, _LargeImageIndicatorFileName);
                    img.Save( _largeImageIndicatorPath, ImageFormat.Png );
                }
                return _largeImageIndicatorPath;
            }
        }
        public override void DisplayResource(IResource res, WordPtr[] wordsToHighlight)
        {
            base.DisplayResource( res, wordsToHighlight );
            _lastResourceId = res.Id;

            CheckDisposed();
            ShowHtml( _WaitMessage, WebSecurityContext.Restricted, null);
            _wordsToHighlight = wordsToHighlight;	// When the item gets loaded, have a chance to apply
            _resource = res;
            if ( res.Type == STR.Email || res.Type == STR.EmailFile )
            {
                IResource folder = _resource.GetLinkProp( PROP.MAPIFolder );
                if ( folder != null )
                {
                    new ResourceProxy( folder ).SetPropAsync( PROP.SelectedInFolder, _resource );
                }

                ShowSubject( res.GetStringProp( Core.Props.Subject ), wordsToHighlight );
                ShowMessageItem( res, MailMessage.Get( res ) );
                ShowTransferredResourcesPane( res );
            }
        }

        private void ShowMessageItem( IResource res, MailMessage messageItem )
        {
            #region Preconditions
            CheckDisposed();
            Guard.NullArgument( messageItem, "messageItem" );
            #endregion Preconditions

            int loadingResourceId = _lastResourceId;
            
            // this performs an asynchronous operation for loading the body;
            // during this operation, the form may be disposed or may switch
            // to a different message (#6012)
            BodyType messageBodyType = messageItem.BodyType;

            if ( _disposed || _lastResourceId != loadingResourceId )
                return;

            bool hidePictures = _panel1.Visible = !res.HasProp( PROP.ShowPictures ) && 
                                                  !Settings.ShowEmbedPics && messageItem.HasPictures;
            int attchmCount = res.GetLinksOfType( null, PROP.Attachment ).Count;
            WebSecurityContext context = ( attchmCount > 0 ) ? WebSecurityContext.Trusted : 
                                            ( hidePictures ? _ctxRestrictedWoImages : _ctxRestrictedWithImages );
            if ( messageBodyType == BodyType.HTML )
            {
                string body = Core.MessageFormatter.GetFormattedHtmlBody( res, messageItem.Body, ref _wordsToHighlight );
                body = InlineAttachments( res, body );
                body = ReplaceContentID( res, body );
                ShowHtml( body, context, _wordsToHighlight );
                
                _wordsToHighlight = null;
                // Control whether we allow downloading pictures or not
            }
            else
            if ( messageBodyType == BodyType.RTF )
            {
                ShowRtf( messageItem.Body, _wordsToHighlight );
                InsertOLEObjects( res );
            }
            else
            {
                if ( !res.HasProp( PROP.NoFormat ) )
                {
                    IResource origMsg = res.GetLinkProp( "Reply" );
                    string origBody = (origMsg != null) ? MailMessage.Get( origMsg ).Body : null;

                    // Apply formatting to the plaintext item to present it in a fancy way. Also, remap the offsets if needed
                    string body = Core.MessageFormatter.GetFormattedBody( res, messageItem.Body, origBody,
                                                                          ref _wordsToHighlight, _font, _fontSize );
                    body = InlineAttachments( res, body );

                    ShowHtml( body, WebSecurityContext.Trusted, _wordsToHighlight );
                    _wordsToHighlight = null;
                }
                else
                {
                    string body = Core.MessageFormatter.GetFormattedBody( res, messageItem.Body, null, ref _wordsToHighlight );
                    ShowHtml( body, WebSecurityContext.Trusted, _wordsToHighlight );
                }
            }
        }

        private static string InlineAttachments( IResource res, string body )
        {
            IResourceList attachments = res.GetLinksOfType( null, PROP.Attachment );
            if( attachments.Count == 0 )
                return body;

            string suffix = string.Empty;
            bool corrected = false;

            foreach( IResource attachment in attachments.ValidResources )
            {
                if (!corrected)
                {
                    int index = body.LastIndexOf( "</BODY>", StringComparison.InvariantCultureIgnoreCase );
                    if (index > 0)
                    {
                        suffix = body.Substring(index);
                        body = body.Substring(0, index);
                    }
                    corrected = true;
                    body += Script;
                    body += "<div id=\"AttachmentsBlock\">";
                    body += Style;
                    body += "<br><h2>Attachments (" + attachments.Count + ")</h2>\n";
                }

                body += "\t<div class=\"attachment\">\n";
                body += AttachmentHeader( attachment );
                if (attachment.Type == "Picture")
                {
                    body += AttachmentPicture( res, attachment );
                }
                else
                {
                    body += AttachmentUnknown( attachment );
                }
                body += "\t</div>\n";
            }
            if( suffix.Length > 0 )
            {
                body += "</div>";
                body += suffix;
                Trace.WriteLine( "\n\n\n\n" + body + "\n\n\n\n" );
            }
            return body;
        }

        private static string AttachmentHeader( IResource attach )
        {
            string fragment = "<h3>" + attach.GetStringProp( Core.Props.Name ) + "</h3>";
            fragment += "<p class=\"size\">" + attach.GetIntProp( Core.Props.Size ) + "</p>\n";
            return fragment;
        }

        private static string AttachmentUnknown( IResource attach )
        {
            string iconFileName = Path.Combine( Path.GetTempPath(), _AttachIconFileName );
            string attachFileName = attach.GetStringProp( Core.Props.Name );
            if( !String.IsNullOrEmpty( attachFileName ) )
            {
                Icon icon = FileIcons.GetFileLargeIcon( attachFileName );
                if( icon != null )
                {
                    Image img = GraphicalUtils.ConvertIco2Bmp( icon, _backBrush );
                    img.Save( iconFileName, ImageFormat.Png );
                }
                else
                    CreateUnknownTypeAttachmentIconPath( iconFileName );
            }
            else
            {
                CreateUnknownTypeAttachmentIconPath( iconFileName );
            }

            string fragment = "<div class=\"picture ico\"><p><img  src=\"" + iconFileName + "\" /></p></div>\n";
            fragment += "<p class=\"content\">";

            string preview = attach.GetPropText( Core.Props.PreviewText );
            fragment += !String.IsNullOrEmpty( preview ) ? preview : _NoPreviewTemplate;
            fragment += "</p>\n\n";

            return fragment;
        }

        private static void CreateUnknownTypeAttachmentIconPath( string filePath )
        {
            if( !File.Exists( filePath ) )
            {
                Assembly theAssm = Assembly.GetExecutingAssembly();
                Image img = Utils.GetResourceImageFromAssembly( theAssm, "OutlookPlugin.Icons.attachment32.png" );
                img.Save( filePath, ImageFormat.Png );
            }
        }

        private static string AttachmentPicture( IResource res, IResource attach )
        {
            bool isConveted = false;
            int  picWidth, picHeight;
            string cachedFileName = Path.Combine( Path.GetTempPath(), attach.GetStringProp( Core.Props.Name ) );

            //  Browser completely dislikes ".ico" files (I can only agree with it...) - 
            //  convert them to ".png" files for uniformity.
            //  TODO: correct the icons background corresponding to html div's background.

            string ext = Path.GetExtension( cachedFileName );
            if( ext.Equals( ".ico", StringComparison.OrdinalIgnoreCase ) )
            {
                int index = cachedFileName.LastIndexOf( ".ico", StringComparison.OrdinalIgnoreCase );
                cachedFileName = cachedFileName.Substring( 0, index ) + ".png";
                isConveted = true;
            }

            //  Check whether the attachment has been already downloaded once, and
            //  if so do not request the attachment stream from the Outlook (since
            //  this is rather costly).
            if( File.Exists( cachedFileName ) &&
                attach.HasProp( PROP.AttachmentPicWidth ) && attach.HasProp( PROP.AttachmentPicHeight ) )
            {
                picWidth = attach.GetIntProp( PROP.AttachmentPicWidth );
                picHeight = attach.GetIntProp( PROP.AttachmentPicHeight );
            }
            else
            {
                IStreamProvider provider = Core.PluginLoader.GetStreamProvider( res.Type );
                Stream attachStream = provider.GetResourceStream( attach );
                Image img = Image.FromStream( attachStream );

                if( isConveted )
                    img.Save( cachedFileName, ImageFormat.Png );
                else
                    img.Save( cachedFileName );

                picWidth = img.Width;
                picHeight = img.Height;
                new ResourceProxy( attach ).SetPropAsync( PROP.AttachmentPicWidth, picWidth );
                new ResourceProxy( attach ).SetPropAsync( PROP.AttachmentPicHeight, picHeight );
            }

            StringBuilder sb = StringBuilderPool.Alloc();
            if( picWidth <= 70 && picHeight <= 70 )
            {
                sb.Append( "\t\t<div class=\"picture ico\"><p><img src=\"" ).Append( cachedFileName ).Append( "\" /></p></div>" );
            }
            else
            {
                int maxSide = Math.Max( picWidth, picHeight );
                double ratio = maxSide / 70.0;
                int thumbWidth = (int)(picWidth / ratio),
                    thumbHeight = (int)(picHeight / ratio);
                sb.Append( "\t\t<div class=\"picture\">\n\t\t\t<img src=\"" ).Append( cachedFileName ).Append( "\"" );
                sb.Append( " width=\"" ).Append( thumbWidth ).Append( "\" height=\"" ).Append( thumbHeight ).Append( "\" " ).
                   Append( " onclick=\"resize(this.id, " ).Append( thumbWidth ).Append( ", " ).Append( thumbHeight ).Append( ", " ).
                   Append( picWidth ).Append( ", " ).Append( picHeight ).Append( ")\" id=\"" ).Append( attach.GetStringProp( Core.Props.Name ) ).Append( "\"" );
                sb.Append( " /><p class=\"fullSize\">.</p>\n" );
            }
            return sb.ToString();
        }

        private static string ReplaceContentID( IResource res, string body )
        {
            IResourceList attachments = res.GetLinksOfType( null, PROP.Attachment );
            foreach ( IResource attach in attachments.ValidResources )
            {
                string contentID = attach.GetStringProp( CommonProps.ContentId );
                if ( contentID != null )
                {
                    string path = Core.FileResourceManager.GetSourceFile( attach );
                    body = body.Replace( "cid:" + contentID, path );
                }
            }
            return body;
        }

        private void InsertOLEObjects( IResource res )
        {
            IResourceList attachments = res.GetLinksOfType( null, PROP.Attachment );
            foreach ( IResource attach in attachments.ValidResources )
            {
                if ( attach.GetIntProp( PROP.AttachMethod ) == AttachMethod.ATTACH_OLE )
                {
                    IEAttach att = new OutlookAttachment( attach ).OpenAttach();
                    if ( att == null ) return;
                    using ( att )
                    {
                        int pos = att.GetLongProp( MAPIConst.PR_RENDERING_POSITION, true );
                        if ( pos != -9999 )
                        {
                            try
                            {
                                att.InsertOLEIntoRTF( _editRtfBody.Handle.ToInt32(), pos );
                            }
                            catch ( Exception exception )
                            {
                                Tracer._TraceException( exception );
                            }
                        }
                    }
                }
            }
        }

        private void ShowTransferredResourcesPane( IResource res )
        {
            IResourceList attachments = res.GetLinksOfType( null, PROP.InternalAttachment );
            bool resourceTransferVisible = false;
            foreach ( IResource attachment in attachments.ValidResources )
            {
                if ( attachment.HasProp( PROP.ResourceTransfer ) )
                {
                    resourceTransferVisible = true;
                    break;                    
                }
            }
            _panel2.Visible = resourceTransferVisible;
        }
        #endregion DisplayResource
    }
}
