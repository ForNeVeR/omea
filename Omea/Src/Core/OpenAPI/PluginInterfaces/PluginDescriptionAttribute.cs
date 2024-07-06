// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Media;

using JetBrains.Annotations;

namespace JetBrains.Omea.OpenAPI
{
	/// <summary>
	/// Class is used as an attribute for specifying the �Author� and �Description�
	/// information fields for both JetBrains and custom plugins. This information is
	/// then shown in the "Tools | Options | Omea | Plugins" options pane.
	/// </summary>
	/// <example>
	/// [PluginDescriptionAttribute("Brad Pitt", "Best plugin of the year")]
	/// public class MyPlugin : IPlugin, IResourceDisplayer, IResourceTextProvider {}
	/// </example>
	public class PluginDescriptionAttribute : Attribute
	{
		#region Data

		private readonly string _author;

		private readonly string _description;

		private readonly PluginDescriptionFormat _descriptionformat;

		private readonly string _iconsrc;

		private readonly string _title;

		#endregion

		#region Init

		/// <summary>
		/// Creates a plain text format description.
		/// Class name will be used as the plugin title, and a generic image as the plugin icon..
		/// </summary>
		public PluginDescriptionAttribute([NotNull] string author, [NotNull] string description)
			: this("", author, description, PluginDescriptionFormat.PlainText, null)
		{
		}

		/// <summary>
		/// Creates a plugin description that specifies all of its characteristics.
		/// Recommended.
		/// </summary>
		/// <param name="title">
		/// The user-friendly short title of the plugin.
		/// This can be an empty string, in which case the local name of the plugin class should be considered.
		/// The assembly name is not used as a plugin name because there might be more than one plugin in an assembly. However, the enable/disable feature works per-assembly, not per plugin class, and the Plugin Options page lists assemblies, not plugins.
		/// </param>
		/// <param name="author">Name of the plugin vendor. This could be a company name or a name of the plugin developer. Should not contain anything but the name; the contact and copyright information should go to the larger <paramref name="description"/> section.</param>
		/// <param name="description">Freehand description for the plugin. The format of this string is defined by the <paramref name="descrformat"/> value.</param>
		/// <param name="descrformat">The <paramref name="description"/> format (plain text or rich text).</param>
		/// <param name="iconsrc">An <see cref="ImageSource"/> for the plugin icon, a raster or vector image that will be primarily rendered 16x16. This could be a relative resource name in the current assembly, or an absolute Pack URI. Note that this is not a name of a CLR Embedded Resource. Optional.</param>
		public PluginDescriptionAttribute([NotNull] string title, [NotNull] string author, [NotNull] string description, PluginDescriptionFormat descrformat, [CanBeNull] string iconsrc)
		{
			if(title == null)
				throw new ArgumentNullException("title");
			if(author == null)
				throw new ArgumentNullException("author");
			if(description == null)
				throw new ArgumentNullException("description");

			_title = title;
			_author = author;
			_description = description;
			_iconsrc = iconsrc;
			_descriptionformat = descrformat;
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets the name of the plugin vendor. This could be a company name or a name of the plugin developer. Should not contain anything but the name; the contact and copyright information should go to the larger <see cref="Description"/> section.
		/// </summary>
		[NotNull]
		public string Author
		{
			get
			{
				return _author;
			}
		}

		/// <summary>
		/// Gets the freehand description for the plugin.
		/// The format of this string is defined by the <see cref="DescriptionFormat"/> value.
		/// </summary>
		[NotNull]
		public string Description
		{
			get
			{
				return _description;
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="Description"/> format (plain text or rich text).
		/// </summary>
		[NotNull]
		public PluginDescriptionFormat DescriptionFormat
		{
			get
			{
				return _descriptionformat;
			}
		}

		/// <summary>
		/// Gets the <see cref="ImageSource"/> for the plugin icon, a raster or vector image that will be primarily rendered 16x16. This could be a relative resource name in the current assembly, or an absolute Pack URI. Note that this is not a name of a CLR Embedded Resource.
		/// </summary>
		[CanBeNull]
		public string IconSrc
		{
			get
			{
				return _iconsrc;
			}
		}

		/// <summary>
		/// Gets the user-friendly short title of the plugin.
		/// This can be an empty string, in which case the local name of the plugin class should be considered.
		/// The assembly name is not used as a plugin name because there might be more than one plugin in an assembly. However, the enable/disable feature works per-assembly, not per plugin class, and the Plugin Options page lists assemblies, not plugins.
		/// </summary>
		public string Title
		{
			get
			{
				return _title;
			}
		}

		#endregion
	}
}
