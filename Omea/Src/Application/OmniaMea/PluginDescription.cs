using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

using GUIControls.RichText;

using JetBrains.Annotations;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Util;

namespace OmniaMea
{
	/// <summary>
	/// Static information about a plugin.
	/// </summary>
	public class PluginDescription
	{
		#region Data

		/// <summary>
		/// The generic icon for a plugin for which an icon is not [yet] available.
		/// </summary>
		[NotNull]
		public static readonly ImageSource GenericPluginIcon = Utils.LoadResourceImage("Icons/PluginGenericIcon.png");

		private static readonly Regex _regexPluginClassLocalName = new Regex("Plugin$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

		#endregion

		#region Attributes

		/// <summary>
		/// Gets or sets the name of the plugin vendor. This could be a company name or a name of the plugin developer. Should not contain anything but the name; the contact and copyright information should go to the larger <see cref="Description"/> section.
		/// </summary>
		public string Author { get; set; }

		/// <summary>
		/// Gets or sets the freehand description for the plugin.
		/// </summary>
		public FlowDocument Description { get; set; }

		/// <summary>
		/// Gets or sets the <see cref="ImageSource"/> for the plugin icon, a raster or vector image that will be primarily rendered 16x16.
		/// </summary>
		public ImageSource Icon { get; set; }

		/// <summary>
		/// Gets or sets the user-friendly short title of the plugin.
		/// This can be an empty string, in which case the local name of the plugin class should be considered.
		/// The assembly name is not used as a plugin name because there might be more than one plugin in an assembly. However, the enable/disable feature works per-assembly, not per plugin class, and the Plugin Options page lists assemblies, not plugins.
		/// </summary>
		public string Title { get; set; }

		#endregion

		/// <summary>
		/// Extracts plugin description from the given plugin type.
		/// </summary>
		[NotNull]
		public static PluginDescription CreateFromPluginType([NotNull] Type plugintype)
		{
			if(plugintype == null)
				throw new ArgumentNullException("plugintype");

			return new PluginDescription {Title = GetPluginTitle(plugintype), Author = GetPluginAuthor(plugintype), Description = GetPluginDesriptionDocument(plugintype), Icon = GetPluginIcon(plugintype)};
		}

		#region Operations

		/// <summary>
		/// Retrieves the plugin author name, or the default text if the name is not specified.
		/// </summary>
		/// <param name="plugintype">The type that implements a plugin.</param>
		/// <returns>A non-empty string with the author name or some stub text.</returns>
		public static string GetPluginAuthor([NotNull] Type plugintype)
		{
			if(plugintype == null)
				throw new ArgumentNullException("plugintype");

			PluginDescriptionAttribute attr;
			try
			{
				attr = TryGetPluginDescriptionAttribute(plugintype);
			}
			catch(Exception ex)
			{
				Trace.WriteLine(ex.ToString());
				return Stringtable.PluginAuthorUnavailableText;
			}

			if(attr == null)
				return Stringtable.PluginAuthorUnavailableText;

			return !string.IsNullOrEmpty(attr.Author) ? attr.Author : Stringtable.PluginAuthorUnavailableText;
		}

		/// <summary>
		/// Retrieves the plugin description text, or the default text if the description is not specified.
		/// </summary>
		/// <param name="plugintype">The type that implements a plugin.</param>
		/// <returns>A non-empty string with the author name or some stub text.</returns>
		public static FlowDocument GetPluginDesriptionDocument([NotNull] Type plugintype)
		{
			if(plugintype == null)
				throw new ArgumentNullException("plugintype");

			try
			{
				// Descriptor
				PluginDescriptionAttribute attr = TryGetPluginDescriptionAttribute(plugintype);

				// No data?
				if((attr == null) || (attr.Description.IsEmpty()))
					return new FlowDocument(new Paragraph(new Italic(new Run(Stringtable.PluginDescriptionUnavailableText))) {TextAlignment = TextAlignment.Center});

				// Render
				switch(attr.DescriptionFormat)
				{
				case PluginDescriptionFormat.PlainText:
					return RichContentConverter.DocumentFromPlainText(attr.Description);
				case PluginDescriptionFormat.Rtf:
					return RichContentConverter.DocumentFromRtf(attr.Description);
				case PluginDescriptionFormat.XamlInline:
					return RichContentConverter.DocumentFromInlineXaml(attr.Description);
				case PluginDescriptionFormat.XamlFlowDocumentPackUri:
					return RichContentConverter.DocumentFromResource(attr.Description, plugintype.Assembly);
				default:
					throw new InvalidOperationException(string.Format("The Description Format value {0} is unsupported.", attr.DescriptionFormat));
				}
			}
			catch(Exception ex)
			{
				Core.ReportBackgroundException(ex);
				return RichContentConverter.DocumentFromException(ex);
			}
		}

		/// <summary>
		/// Retrieves the plugin icon as an image source, or returns the default plugin icon if not available.
		/// </summary>
		/// <param name="plugintype">The type that implements a plugin.</param>
		[NotNull]
		public static ImageSource GetPluginIcon([NotNull] Type plugintype)
		{
			if(plugintype == null)
				throw new ArgumentNullException("plugintype");

			try
			{
				PluginDescriptionAttribute attr = TryGetPluginDescriptionAttribute(plugintype);
				if((attr != null) && (!attr.IconSrc.IsEmpty()))
				{
					var uri = new Uri(attr.IconSrc, UriKind.RelativeOrAbsolute);
					if(!uri.IsAbsoluteUri)
						uri = Utils.MakeResourceUri(attr.IconSrc, plugintype.Assembly);
					return Utils.LoadResourceImage(uri);
				}
			}
			catch(Exception ex)
			{
				Core.ReportBackgroundException(ex);
			}

			return GenericPluginIcon;
		}

		/// <summary>
		/// Retrieves the plugin user-friendly title text, or the plugin class local name if not specified.
		/// </summary>
		/// <param name="plugintype">The type that implements a plugin.</param>
		/// <returns>A non-empty string with the title.</returns>
		public static string GetPluginTitle([NotNull] Type plugintype)
		{
			if(plugintype == null)
				throw new ArgumentNullException("plugintype");

			// See if we can get a meaningful title from the plugin
			try
			{
				PluginDescriptionAttribute attr = TryGetPluginDescriptionAttribute(plugintype);
				if((attr != null) && (!attr.Title.IsEmpty()))
					return attr.Title;
			}
			catch(Exception ex)
			{
				Core.ReportBackgroundException(ex);
			}

			// Use the local class name (remove the common “Plugin” suffix)
			return _regexPluginClassLocalName.Replace(plugintype.Name, "");
		}

		#endregion

		#region Implementation

		/// <summary>
		/// By the plugin type, looks up the attribute.
		/// Throws on errors, returns <c>Null</c> if there're no attributes.
		/// </summary>
		[CanBeNull]
		private static PluginDescriptionAttribute TryGetPluginDescriptionAttribute([NotNull] ICustomAttributeProvider plugintype)
		{
			if(plugintype == null)
				throw new ArgumentNullException("plugintype");

			foreach(PluginDescriptionAttribute attr in plugintype.GetCustomAttributes(typeof(PluginDescriptionAttribute), true))
				return attr;

			return null;
		}

		#endregion
	}
}