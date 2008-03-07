/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Text;
using System.Web;
using JetBrains.Omea.Base;
using JetBrains.Omea.FileTypes;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.HTML;
using JetBrains.Omea.OpenAPI;
using System.IO;
using System.Windows.Forms;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea
{
    internal delegate void CreateFragmentDelegate( string subject, string text, string sourceUrl );
    internal delegate void DoCreateFragmentDelegate( string subject, string text, string sourceUrl, DateTime timestamp );

    /// <summary>
    /// Action for creating a fragment.
    /// </summary>
    public class CreateFragmentAction: IAction
    {
        public void Execute( IActionContext context )
        {
            IResource baseResource = null;

            if ( context.SelectedResources.Count > 0 )
            {
                baseResource = context.SelectedResources [0];
            }
            else if ( Core.ResourceBrowser.SelectedResources.Count > 0 )
            {
                baseResource = Core.ResourceBrowser.SelectedResources [0];
            }

            string fragmentName = null;
            if ( baseResource != null )
            {
                if ( baseResource.HasProp( Core.Props.Subject ) )
                {
                    fragmentName = "Clipping: " + baseResource.GetStringProp( Core.Props.Subject );
                }
                else if ( baseResource.HasProp( Core.Props.Name ) )
                {
                    fragmentName = "Clipping: " + baseResource.GetStringProp( Core.Props.Name );
                }
            }
            if ( fragmentName == null && context.CurrentPageTitle != null )
            {
                fragmentName = "Clipping: " + context.CurrentPageTitle;
            }
            if ( fragmentName == null )
            {
                fragmentName = "New Clipping";
            }

            DoCreateFragment( fragmentName, context.SelectedTextFormat, context.SelectedText,
                context.CurrentUrl, baseResource );
        }

        /// <summary>
        /// Creates a clipping and opens a window letting the user edit it.
        /// </summary>
        /// <param name="subject">The subject of the fragment.</param>
        /// <param name="text">The HTML text of the fragment</param>
        /// <param name="sourceUrl">The URL of the page from which the fragment is created.</param>
        public static void CreateHtmlFragment( string subject, string text, string sourceUrl )
        {
            // the timestamp is needed to prevent merging of identical clippings
            Core.UIManager.QueueUIJob( new DoCreateFragmentDelegate( DoCreateHtmlFragment ),
                subject, text, sourceUrl, DateTime.Now );
        }

        /// <summary>
        /// Creates and saves a clipping.
        /// </summary>
        /// <param name="subject">The subject of the fragment.</param>
        /// <param name="text">The HTML text of the fragment</param>
        /// <param name="sourceUrl">The URL of the page from which the fragment is created.</param>
        public static void CreateHtmlFragmentSilent( string subject, string text, string sourceUrl )
        {
            // the timestamp is needed to prevent merging of identical clippings
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new DoCreateFragmentDelegate( DoCreateHtmlFragmentSilent ),
                subject, text, sourceUrl, DateTime.Now );
        }

        private static void DoCreateHtmlFragment( string subject, string text, string sourceUrl, DateTime timestamp )
        {
            DoCreateFragment( subject, TextFormat.Html, text, sourceUrl, null );
        }

        private static void DoCreateHtmlFragmentSilent( string subject, string text, string sourceUrl, DateTime timestamp )
        {
            IResource res = CreateFragmentResource( subject, TextFormat.Html, text, sourceUrl, null );
            Core.WorkspaceManager.AddToActiveWorkspace( res );
            res.EndUpdate();
            Core.TextIndexManager.QueryIndexing( res.Id );
            Core.FilterManager.ExecRules( StandardEvents.ResourceReceived, res );
        }

        private static void DoCreateFragment( string subject, TextFormat textFormat, string text, 
            string sourceUrl, IResource baseResource )
        {
            IResource fragment = CreateFragmentResource( subject, textFormat, text, sourceUrl, baseResource );

            FragmentEditPane editPane = new FragmentEditPane();
            Core.UIManager.OpenResourceEditWindow( editPane, fragment, true, OnFragmentSaved, editPane );
        }

        private static IResource CreateFragmentResource( string subject, TextFormat textFormat, string text, string sourceUrl, IResource baseResource )
        {
            IResource clipping = Core.ResourceStore.NewResourceTransient( "Fragment" );
    
            string baseFileName = null;
            if ( baseResource != null && Core.ResourceStore.ResourceTypes [baseResource.Type].HasFlag( ResourceTypeFlags.FileFormat ))
            {
                string directory = baseResource.GetStringProp( "Directory" );
                if ( directory == null )
                {
                    IResource source = FileResourceManager.GetSource( baseResource );
                    if ( source != null )
                    {
                        directory = baseResource.GetStringProp( "Directory" );
                    }
                }
                if ( directory != null && baseResource.HasProp( "Name" ) )
                {
                    baseFileName = Path.Combine( directory, baseResource.GetStringProp( "Name" ) );
                }
            }

            if ( textFormat == TextFormat.Html )
            {
                StringBuilder htmlBuilder = new StringBuilder( "<html><body style=\"font-family: Verdana; font-size: 10pt; \">" );
                
                if ( baseFileName != null  )
                {
                    htmlBuilder.Append( text );
                    htmlBuilder.Append( "<p class=\"Clipping-Url\"><a href=\"" );
                    htmlBuilder.Append( HttpUtility.HtmlEncode( baseFileName ) );
                    htmlBuilder.Append( "\">" );
                    htmlBuilder.Append( HttpUtility.HtmlEncode( baseFileName ) );
                    htmlBuilder.Append( "</a>" );
                }
                else if ( sourceUrl != null )
                {
                    htmlBuilder.Append( HtmlTools.FixRelativeLinks( text, sourceUrl ) );
                    htmlBuilder.Append( "<p class=\"Clipping-Url\"><a href=\"" );
                    htmlBuilder.Append( sourceUrl );
                    htmlBuilder.Append( "\">" );
                    htmlBuilder.Append( sourceUrl );
                    htmlBuilder.Append( "</a>" );
                }
                else
                {
                    htmlBuilder.Append( text );
                }
                htmlBuilder.Append( "</body></html>" );

                clipping.SetProp( Core.Props.LongBody, htmlBuilder.ToString() );
                clipping.SetProp( Core.Props.LongBodyIsHTML, true );
            }
            else if ( textFormat == TextFormat.Rtf )
            {
                clipping.SetProp( Core.Props.LongBody, text );
                clipping.SetProp( Core.Props.LongBodyIsRTF, true );
            }
    
            clipping.SetProp( Core.Props.Subject, subject );
            clipping.SetProp( "IsClippingFakeProp", true );
    
            clipping.SetProp( Core.Props.Date, DateTime.Now );
            if ( baseResource != null )
            {
                clipping.SetProp( "ContentType", baseResource.Type );
                int[] linkTypeIds = baseResource.GetLinkTypeIds();
                foreach( int propId in linkTypeIds )
                {
                    if ( Core.ResourceStore.PropTypes [propId].HasFlag( PropTypeFlags.SourceLink ) )
                    {
                        clipping.SetProp( "ContentLinks", Core.ResourceStore.PropTypes [propId].Name );
                        break;
                    }
                }
                clipping.AddLink( "Fragment", baseResource );
            }
            else if ( sourceUrl != null )
            {
                clipping.SetProp( "ContentType", "Weblink" );
				clipping.SetProp( "Url", sourceUrl );
            }
            return clipping;
        }

        private static void OnFragmentSaved( IResource res, object tag )
        {
            Core.WorkspaceManager.AddToActiveWorkspace( res );
            Core.TextIndexManager.QueryIndexing( res.Id );
            Core.FilterManager.ExecRules( StandardEvents.ResourceReceived, res );
            FragmentEditPane editPane = (FragmentEditPane) tag;
            if ( editPane != null && editPane.OpenAfterSave )
            {
                Core.ResourceBrowser.DisplayResource( res );
                if ( !Core.ResourceBrowser.LinksPaneExpanded )
                {
                    Core.ResourceBrowser.ContentChanged += OnResorceBrowserContentChanged;
                }
                Core.ResourceBrowser.LinksPaneExpanded = true;
            }
        }

        private static void OnResorceBrowserContentChanged( object sender, EventArgs e )
        {
            IResourceBrowser resourceBrowser = ICore.Instance.ResourceBrowser;
            resourceBrowser.LinksPaneExpanded = false;
            resourceBrowser.ContentChanged -= OnResorceBrowserContentChanged;
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            bool hasSelectedText = ( context.SelectedText != null && 
                context.SelectedPlainText != null && context.SelectedPlainText.Length > 0 );
            if ( !hasSelectedText )
            {
                if ( context.Kind == ActionContextKind.ContextMenu )
                    presentation.Visible = false;
                else
                    presentation.Enabled = false;
            }
        }
    }
    
    /**
     * Action for editing the properties of a fragment.
     */

    public class EditFragmentAction: IAction
    {
        public void Execute( IActionContext context )
        {
            IResource fragment = context.SelectedResources [0];
            ICore.Instance.UIManager.OpenResourceEditWindow(
                new FragmentEditPane(), fragment, false );
        }
        
        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
            if ( context.SelectedResources.Count != 1 )
            {
                presentation.Visible = false;
            }
        }
    }

    public class FragmentTextProvider: IResourceTextProvider
    {
        private readonly JetRichTextBox _converterTextBox;

        public FragmentTextProvider()
        {
            _converterTextBox = new JetRichTextBox();
        }
        
        public bool ProcessResourceText( IResource res, IResourceTextConsumer consumer )
        {
            if ( res.HasProp( Core.Props.LongBodyIsRTF ) )
            {
                ProcessRTFFragment( res, consumer );
            }
            else
            {
                ProcessHTMLFragment( res, consumer );
            }
            return true;
        }

        private void ProcessRTFFragment( IResource res, IResourceTextConsumer consumer )
        {
            string body = res.GetPropText( Core.Props.LongBody );
            lock( this )
            {
                _converterTextBox.RichText = body;
                consumer.AddDocumentFragment( res.Id, _converterTextBox.PlainText );
            }
        }

        private static void ProcessHTMLFragment( IResource res, IResourceTextConsumer consumer )
        {
            HtmlIndexer.IndexHtml( res, res.GetPropText( Core.Props.LongBody ), consumer, null );
        }
    }

    /**
     * Action for sending a fragment by e-mail.
     */

    public class EmailFragmentAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            IResource fragment = context.SelectedResources [0];
            IEmailService emailService = (IEmailService) Core.PluginLoader.GetPluginService( typeof(IEmailService) );
            if ( emailService != null )
            {
                emailService.CreateEmail( fragment.GetPropText( Core.Props.Subject ),
                    fragment.GetPropText( Core.Props.LongBody ),
                    fragment.HasProp( Core.Props.LongBodyIsHTML ) ? EmailBodyFormat.Html : EmailBodyFormat.PlainText,
                    Core.ResourceStore.EmptyResourceList, new string[] {}, true );
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            base.Update( context, ref presentation );
            if ( context.SelectedResources.Count > 0 && 
                context.SelectedResources [0].HasProp( Core.Props.LongBodyIsRTF ) )
            {
                presentation.Enabled = false;
            }
        }
    }

    /// <summary>
    /// Action to save a clipping to a file.
    /// </summary>
    public class SaveFragmentAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            IResource fragment = context.SelectedResources [0];
            string fileName = fragment.GetPropText( Core.Props.Subject ).Trim();
            if ( fileName.Length == 0 )
            {
                fileName = "Clipping";
            }
            else
            {
                IOTools.MakeValidFileName( ref fileName );
            }
            fileName = fileName.TrimEnd( '.' );

            SaveFileDialog dlg = new SaveFileDialog();

            if ( fragment.HasProp( Core.Props.LongBodyIsHTML ) )
            {
                fileName = fileName + ".html";
                dlg.Filter = "HTML files (*.html)|*.html|All files|*.*";
            }
            else if ( fragment.HasProp( Core.Props.LongBodyIsRTF ) )
            {
                fileName = fileName + ".rtf";
                dlg.Filter = "RTF files (*.rtf)|*.rtf|All files|*.*";
            }
            else
            {
                fileName = fileName + ".txt";
                dlg.Filter = "Text files (*.txt)|*.txt|All files|*.*";
            }

            dlg.FileName = fileName;
            if ( dlg.ShowDialog( Core.MainWindow ) == DialogResult.OK )
            {
                using( FileStream fs = new FileStream( dlg.FileName, FileMode.Create ) )
                {
                    Encoding enc;
                    if ( fragment.HasProp( Core.Props.LongBodyIsHTML ) )
                    {
                        enc = Encoding.UTF8;
                        byte[] preamble = enc.GetPreamble();
                        fs.Write( preamble, 0, preamble.Length );
                    }
                    else
                    {
                        enc = Encoding.Default;
                    }
                
                    byte[] data = enc.GetBytes( fragment.GetPropText( Core.Props.LongBody ) );
                    fs.Write( data, 0, data.Length );
                }
            }
        }
    }

	public class ClippingNewspaperProvider : INewspaperProvider
	{
		/// <summary>
		/// Registers self as a newspaper provider for clippings
		/// </summary>
		public static void Register()
		{
			Core.PluginLoader.RegisterNewspaperProvider( "Fragment", new ClippingNewspaperProvider() );
		}

		internal ClippingNewspaperProvider()
		{
			
		}

		#region INewspaperProvider Members

		public void GetHeaderStyles(string resourceType, TextWriter writer )
		{
			writer.WriteLine( "p.Clipping-Url { border-top: solid 1px ButtonFace; text-align: left; }" );
		}

		public void GetItemHtml(IResource item, TextWriter writer)
		{
			// Title
			GenericNewspaperProvider.RenderCaption(item, writer);

			// Clipping body in either format
			if(item.HasProp( Core.Props.LongBodyIsRTF ))
				writer.WriteLine( "<em>Clipping has an RTF body that cannot be displayed by Newspaper View. Open the clipping to see it.</em>" );
			else if(item.HasProp( Core.Props.LongBodyIsHTML ))
				writer.WriteLine( "<div>{0}</div>", item.GetPropText( Core.Props.LongBody ) );	// TODO: ensure the html/body tags are stripped
			else
				writer.WriteLine( "<pre>{0}</pre>", HttpUtility.HtmlEncode( item.GetPropText( Core.Props.LongBody ) ) );
			
			if(item.HasProp( "Url" ))
				writer.WriteLine( "<div class=\"Clipping-Url\">{0}</div>", item.GetStringProp( "Url" ) );
		}

		#endregion
	}

}
