// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Web;
using JetBrains.Omea.Base;
using JetBrains.Omea.HTML;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.RSSPlugin
{
	internal class RssNewspaperProvider : INewspaperProvider
	{
		/// <summary>
		/// Caches an icon that represents a link to the Web location of an enclosure.
		/// </summary>
		protected static Icon _iconEnclosureWeb = null;

		public void GetHeaderStyles( string resourceType, TextWriter writer )
		{
			writer.WriteLine( "div.date { font-family: Verdana; font-size: 8pt; font-style: italic; }" );
			writer.WriteLine( "div.comments { font-family: Verdana; font-size: 8pt; margin-top: 6pt; } \n" );
		}

		public void GetItemHtml( IResource item, TextWriter writer )
		{
			TextWriterDecor decor = new TextWriterDecor( writer );

			////////////////////////
			// Prepare the strings

			/*
        	// Date — date/time of this item
        	string sDate;
        	DateTime date = item.GetDateProp( Core.Props.Date );
        	if( date.Date == DateTime.Today )
        		sDate = "Today " + date.ToShortTimeString();
        	else
        		sDate = date.ToShortDateString() + ' ' + date.ToShortTimeString();

        	// Origin — name of the feed author, etc
        	string sOrigin = "";
        	if( item.HasProp( Core.ContactManager.Props.LinkFrom ) )
        		sOrigin = HttpUtility.HtmlEncode( item.GetPropText( Core.ContactManager.Props.LinkFrom ) );

        	//////////
        	// Title
        	writer.WriteLine( "<div class=\"title\">" );
        	GenericNewspaperProvider.RenderIcon( item, writer ); // Icon
        	RssBodyConstructor.AppendLink( item, decor, true ); // Title text & link
        	writer.WriteLine( "<em class=\"Origin\">{0}{2}{1}</em>", sOrigin, sDate, ((sOrigin.Length > 0) && (sDate.Length > 0) ? " — " : "") ); // Origin (feed name) & Date
        	writer.WriteLine( "</div>" ); // class=title

        	GenericNewspaperProvider.RenderFlag( item, writer ); // Flag (optional)
        	GenericNewspaperProvider.RenderAnnotation( item, writer ); // Annotation (optional)

        	writer.WriteLine( "<br class=\"clear\" />" );
			*/
			// TODO: remove

			IResource feed = item.GetLinkProp( -Props.RSSItem );
			GenericNewspaperProvider.RenderCaption( item, writer );

			//////////////
			// Item Body

			writer.WriteLine( "<div>" );
			if( feed != null && feed.HasProp( Props.URL ) )
				writer.WriteLine( HtmlTools.FixRelativeLinks( item.GetPropText( Core.Props.LongBody ), feed.GetStringProp( "URL" ) ) );
			else
				writer.WriteLine( item.GetPropText( Core.Props.LongBody ) );
			writer.WriteLine( "</div>" );

			// Enclosure info
			if( item.HasProp( Props.EnclosureURL ) )
			{
				writer.Write( "<p class=\"Origin\"><span title=\"Enclosure is an attachment to the RSS feed item.\">Enclosure</span>" );

				// Specify the enclosure size, if available
				if( item.HasProp( Props.EnclosureSize ) )
					writer.Write( " ({0})", Utils.SizeToString( item.GetIntProp( Props.EnclosureSize ) ) );

				writer.Write( ": " );

				// Add a link to the locally-saved enclosure file
				string sDownloadComment = null;	// Will contain an optional download comment
				if( item.HasProp( Props.EnclosureDownloadingState ) )
				{
					// Choose the tooltip text and whether the icon will be clickable, depending on the state
					string sText = null;
					bool bLink = false;
					EnclosureDownloadState nEnclosureDownloadState = (EnclosureDownloadState)item.GetIntProp( Props.EnclosureDownloadingState );
					switch( nEnclosureDownloadState )
					{
					case EnclosureDownloadState.Completed:
						sText = "The enclosure has been downloaded to your computer.\nClick to open the local file.";
						bLink = true;
						break;
					case EnclosureDownloadState.Failed:
						sText = "Failed to download the enclosure.\nUse the Web link to download manually.";
						break;
					case EnclosureDownloadState.InProgress:
						sText = "Downloading the enclosure, please wait…\nClick to open the partially-downloaded file.";
						// Write percentage to the comment
						if( item.HasProp( Props.EnclosureDownloadedSize ) )
						{
							if( item.HasProp( Props.EnclosureSize ) ) // The total size is available, as needed for the percentage
								sDownloadComment = String.Format( "({0}%)", item.GetIntProp( Props.EnclosureDownloadedSize ) * 100 / item.GetIntProp( Props.EnclosureSize ) );
							else // The total size is not available, percentage not available, show the size downloaded
								sDownloadComment = String.Format( "({0} downloaded so far)", Utils.SizeToString( item.GetIntProp( Props.EnclosureDownloadedSize ) ) );
						}
						bLink = true;
						break;
					case EnclosureDownloadState.NotDownloaded:
						sText = "The enclosure has not been downloaded.\nUse the Web link to download manually.";
						break;
					case EnclosureDownloadState.Planned:
						sText = "The enclosure has been schedulled for download.\nUse the Web link to download manually.";
						sDownloadComment = "(0%)";
						break;
					default:
						throw new Exception( "Unexpected enclosure download state." );
					}

					// Ensure that there's the path to the local file specified, if we're going to provide a link to it
					if( (bLink) && (!item.HasProp( Props.EnclosureTempFile )) )
					{
						Trace.WriteLine( "Warning: path to the downloaded or in-progress enclosure is missing, though the downloading state implies on it should be present." );
						bLink = false;
					}

					// Open the link (if available)
					if( bLink )
						writer.Write( "<a href=\"{0}\">", "file://" + HttpUtility.HtmlEncode( item.GetStringProp( Props.EnclosureTempFile ) ) );

					// Render the icon
					Icon icon = EnclosureDownloadManager.GetEnclosureStateIcon( nEnclosureDownloadState );
					writer.Write( "<img src=\"{0}\" align=\"top\" width=\"{1}\" height=\"{2}\" alt=\"{3}\" title=\"{3}\" />", FavIconManager.GetIconFile( icon, "EnclosureDownload", null, true ), icon.Width, icon.Height, sText, sText );

					// Close the link
					if( bLink )
						writer.Write( "</a>" );

					writer.Write( " " );
				}

				// Add a link to the Web location of the enclosure
				writer.Write( "<a href=\"{0}\">", HttpUtility.HtmlEncode( item.GetStringProp( Props.EnclosureURL ) ) );
				if( _iconEnclosureWeb == null )
					_iconEnclosureWeb = RSSPlugin.LoadIconFromAssembly( "BlogExtensionComposer.Submit.ico" );
				writer.Write( "<img src=\"{0}\" align=\"top\" width=\"{1}\" height=\"{2}\" alt=\"{3}\" title=\"{3}\" /> ", FavIconManager.GetIconFile( _iconEnclosureWeb, "EnclosureWeb", null, true ), _iconEnclosureWeb.Width, _iconEnclosureWeb.Height, "Download enclosure from the Web." );
				writer.Write( "</a>" );

				// Add the optional download comment
				if(sDownloadComment != null)
					writer.Write(" ");
				writer.Write(sDownloadComment);

				// Close the paragraph
				writer.WriteLine( "</p>" );
			}

			// Link to the Source
			RssBodyConstructor.AppendSourceTag( item, decor );

			// Link to the comments
			if( item.HasProp( Props.CommentURL ) )
			{
				decor.AppendText( "<p class=\"Origin\">" );
				RssBodyConstructor.AppendCommentsTag( item, decor );
				writer.WriteLine( "</p>" );
			}
		}
	}
}
