// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Web;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceTools
{
	/// <summary>
	/// Implements the default newspaper provider.
	/// This provider is capable of rendering the simple newspaper content for items that do not have their own newspaper providers.
	/// Also, it exposes some static functions for rendering the common newspaper item elements, such as annotation and flag.
	/// </summary>
	public class GenericNewspaperProvider : INewspaperProvider
	{
		#region Ctor

		public GenericNewspaperProvider()
		{
		}

		#endregion

		#region INewspaperProvider Members

		public void GetHeaderStyles( string resourceType, TextWriter writer )
		{
		}

		public void GetItemHtml( IResource item, TextWriter writer )
		{
			// Render the item caption
			RenderCaption( item, writer );

			// Try to render some body
			if( (item.HasProp( Core.Props.LongBody )) && (!item.HasProp( Core.Props.LongBodyIsRTF )) && (!item.HasProp( Core.Props.LongBodyIsHTML )) )
				writer.WriteLine( HttpUtility.HtmlEncode( item.GetPropText( Core.Props.LongBody ) ) );
			else
				writer.WriteLine( "<em>No content is available for this resource to display in the newspaper. Open the resource to see it.</em>" );
		}

		#endregion

		/// <summary>
		/// Renders to the newspaper an icon of the resource passed as an argument.
		/// </summary>
		/// <param name="item">Resource whose icon is to be rendered. Icon provider will be used to get the icon on a per-resource basis.</param>
		/// <param name="writer">Output text writer.</param>
		public static void RenderIcon( IResource item, TextWriter writer )
		{
			// Add the icon
			writer.WriteLine( "<img class=\"ResourceIcon\" src=\"{0}\" width=\"16\" height=\"16\" alt=\"{1}\" title=\"{1}\" />", GetIconFileName( item ), Core.ResourceStore.ResourceTypes[ item.Type ].DisplayName );
		}

		/// <summary>
		/// Creates a local file for the resource icon of the given resource, and returns a path to that file.
		/// </summary>
		/// <param name="item">Resource whose icon file path is to be retrieved. Icon provider will be used to get the icon on a per-resource basis.</param>
		/// <returns>Path to the icon file.</returns>
		public static string GetIconFileName( IResource item )
		{
			string relation = item.IsDeleted ? "Deleted" : (item.HasProp( Core.Props.IsUnread ) ? "Unread" : "Read");
			return FavIconManager.GetIconFile( Core.ResourceIconManager.GetResourceIconProvider( item.Type ).GetResourceIcon( item ), item.TypeId, relation, true );
		}

		/// <summary>
		/// If the item passed in has an annotation attached, renders the annotation text to the newspaper.
		/// </summary>
		/// <param name="item">Resource whose annotation is to be rendered.</param>
		/// <param name="writer">Output text writer.</param>
		public static void RenderAnnotation( IResource item, TextWriter writer )
		{
			if( !item.HasProp( Core.Props.Annotation ) )
				return; // No annotation, no rendering

			string sAnnotation = item.GetStringProp( Core.Props.Annotation );
			if( sAnnotation.Length == 0 )
				return; // An empty annotation won't be rendered as well

			writer.WriteLine( "<form action=\"none\">" );
			writer.WriteLine( "<fieldset>" );
			writer.WriteLine( "<textarea cols=\"1\" rows=\"1\">{0}</textarea>", sAnnotation );
			writer.WriteLine( "</fieldset>" );
			writer.WriteLine( "</form>" );
		}

		/// <summary>
		/// If the item passed in has a flag assigned, renders the flag to the newspaper as a flag image.
		/// </summary>
		/// <param name="item">Resource whose flag is to be rendered.</param>
		/// <param name="writer">Output text writer.</param>
		public static void RenderFlag( IResource item, TextWriter writer )
		{
			IResourceList flags = item.GetLinksFrom( "Flag", "Flag" ); // Flags assigned to this icon
			if( flags.Count == 0 )
				return; // No flags, no icons
			IResource flag = flags[ 0 ];

			// Get path to the flag icon file
			Icon icon = Core.ResourceIconManager.GetResourceIconProvider( flag.Type ).GetResourceIcon( flag );
			string sIconPath = FavIconManager.GetIconFile( icon, flag.OriginalId, "Flag", true );
			writer.WriteLine( "<img class=\"Flag\" src=\"{0}\" width=\"{1}\" height=\"{2}\" alt=\"{3}\" title=\"{3}\" />", sIconPath, icon.Width, icon.Height, flag.DisplayName );
		}

		/// <summary>
		/// Renders a standard caption of the newspaper HTML item.
		/// </summary>
		/// <param name="item">Resource whose caption is to be rendered.</param>
		/// <param name="writer">Output text writer.</param>
		public static void RenderCaption( IResource item, TextWriter writer )
		{
			writer.WriteLine( "<table class=\"Caption\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\">" );
			writer.WriteLine( "<tr>" );

			/////////////////
			// Subject & Date
			writer.WriteLine( "<td class=\"Title\">" );

			writer.WriteLine( "<p class=\"Subject\">" );
			// Check if the resource has some Link/URL property (this is some heuristics …)
			string sLinkProp = null; // Name of the link prop, if any found
			foreach( string sProposedLinkProp in new string[] {"Link", "Uri", "Url", "URI", "URL"} )
			{
				if( !Core.ResourceStore.PropTypes.Exist( sProposedLinkProp ) )
					continue; // Skip the illegal properties
				if( item.HasProp( sProposedLinkProp ) )
				{
					sLinkProp = sProposedLinkProp;
					break;
				}
			}

			// Write the link/text
			if( sLinkProp != null ) // Open the anchor tag that makes the link
				writer.Write( "<a href=\"{0}\">", HttpUtility.HtmlEncode( item.GetPropText( sLinkProp ) ) );

			// Icon
			RenderIcon( item, writer ); // Icon

			// Text of the subject: if the item has a subject, use it; otherwise, take the display name
			writer.WriteLine( item.HasProp( Core.Props.Subject ) ? item.GetPropText( Core.Props.Subject ) : item.DisplayName );

			// Flag & Category (write to a string)
			StringWriter swFlagCat = new StringWriter();
			RenderFlag( item, swFlagCat );
			RenderCategoryIcon( item, swFlagCat );

			// End the link (if it was opened)
			if( sLinkProp != null )
				writer.Write( "</a>" );

			writer.WriteLine( "</p>" ); // End the Subject paragraph

			// Prepare the Date — date/time of this item
			string sDate = "";
			if( item.HasProp( Core.Props.Date ) )
			{
				DateTime date = item.GetDateProp( Core.Props.Date );
				if( date.Date == DateTime.Today ) // Special treatment for the Today's date
					sDate = "Today " + date.ToShortTimeString();
				else
					sDate = date.ToShortDateString() + ' ' + date.ToShortTimeString();
			}

			// Prepare the Origin — name of the feed author, etc
			string sOrigin = "";
			if( item.HasProp( Core.ContactManager.Props.LinkFrom ) )
				sOrigin = HttpUtility.HtmlEncode( item.GetPropText( Core.ContactManager.Props.LinkFrom ) );

			// Finally, write the Origin-Date block (if available)
			if( (sDate.Length > 0) || (sOrigin.Length > 0) || (swFlagCat.ToString().Length > 0) )
				writer.WriteLine( "<p class=\"Origin\">{3}{4}{0}{2}{1}</p>", sOrigin, sDate, ((sOrigin.Length > 0) && (sDate.Length > 0) ? " — " : ""), swFlagCat.ToString(), (swFlagCat.ToString().Length > 0 ? " " : "") ); // Origin (feed name) & Date

			writer.WriteLine( "</td>" );

			///////////////
			// Annotation
			string sAnnotation = item.HasProp( Core.Props.Annotation ) ? item.GetPropText( Core.Props.Annotation ) : ""; // Annotation text, or an empty string if it's not defined
			writer.WriteLine( "<td class=\"{0}\">", (sAnnotation.Length > 0 ? "Annotation" : "") ); // Do not assign a class if there's no annotation, so that the cell would be invisible
			writer.WriteLine( HttpUtility.HtmlEncode( sAnnotation ) );
			writer.WriteLine( "</td>" );

			writer.WriteLine( "</tr>" );
			writer.WriteLine( "</table>" );
		}

		/// <summary>
		/// If the passed item has any categories assigned, renders the category icon to the newspaper view.
		/// </summary>
		/// <param name="item">Resource.</param>
		/// <param name="writer">Output text writer.</param>
		public static void RenderCategoryIcon( IResource item, TextWriter writer )
		{
			// Add the category icon, if the category is ON
			IResourceList cats = item.GetLinksOfType( "Category", "Category" );
			if( cats.Count != 0 )
			{
				// Collect the cat names
				StringBuilder sb = StringBuilderPool.Alloc();
                try
                {
                    foreach( IResource cat in cats )
                    {
                        if( sb.Length > 0 )
                            sb.Append( ", " );
                        sb.Append( cat.DisplayName );
                    }

                    // Add the image
                    writer.WriteLine( " <img class=\"CategoryIcon\" src=\"{0}\" width=\"16\" height=\"16\" alt=\"{1}\" title=\"{1}\" />", GetIconFileName( cats[ 0 ] ), "Categories: " + sb.ToString() );
                }
                finally
                {
                    StringBuilderPool.Dispose( sb );
                }
			}
		}
	}
}
