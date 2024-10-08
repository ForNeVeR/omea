﻿// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.1378
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

//
// This source code was auto-generated by xsd, Version=3.5.20706.1.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace JetBrains.Build.GuidCache
{
	public partial class GuidCacheXml
	{
		#region Attributes

		/// <summary>
		/// Gets the XSD for the GuidCache.xml.
		/// </summary>
		public static XmlSchema XmlSchema
		{
			get
			{
				using(Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JetBrains.Build.GuidCache.GuidCache.xsd"))
					return XmlSchema.Read(stream, null);
			}
		}

		/// <summary>
		/// Looks up a GUID by its loose name.
		/// </summary>
		public Guid this[string name]
		{
			get
			{
				if(Loose != null)
				{
					foreach(LooseGuidXml entry in Loose)
					{
						if(entry.Name == name)
							return new Guid(entry.Value);
					}
				}
				throw new InvalidOperationException(string.Format("Could not find a GUID Cache database entry by the “{0}” name.", name));
			}
		}

		/// <summary>
		/// Looks up a GUID by its strict id.
		/// </summary>
		public Guid this[GuidIdXml id]
		{
			get
			{
				if(Strict != null)
				{
					foreach(StrictGuidXml entry in Strict)
					{
						if(entry.Id == id)
							return new Guid(entry.Value);
					}
				}
				throw new InvalidOperationException(string.Format("Could not find a GUID Cache database entry by the “{0}” id.", id));
			}
		}

		#endregion

		#region Operations

		public static GuidCacheXml Load(Stream stream)
		{
			var settings = new XmlReaderSettings();
			settings.ValidationType = ValidationType.Schema;
			settings.Schemas.Add(XmlSchema);

			// Load the AllAssembliesXml, validating against the schema
			GuidCacheXml retval;
			using(XmlReader xmlrValidating = XmlReader.Create(stream, settings))
				retval = (GuidCacheXml)new XmlSerializer(typeof(GuidCacheXml)).Deserialize(xmlrValidating);

			// Validate for duplicates
			var guids = new Dictionary<Guid, bool>();
			if(retval.Strict != null)
			{
				foreach(StrictGuidXml entry in retval.Strict)
				{
					if(guids.ContainsKey(new Guid(entry.Value)))
						throw new InvalidOperationException(string.Format("Duplicate GUID “{0}” in the GUID cache.", entry.Value));
					guids.Add(new Guid(entry.Value), true);
				}
			}

			var names = new Dictionary<string, bool>();
			if(retval.Loose != null)
			{
				foreach(LooseGuidXml entry in retval.Loose)
				{
					if(guids.ContainsKey(new Guid(entry.Value)))
						throw new InvalidOperationException(string.Format("Duplicate GUID “{0}” in the GUID cache.", entry.Value));
					guids.Add(new Guid(entry.Value), true);
					if(!names.ContainsKey(entry.Name))
						throw new InvalidOperationException(string.Format("Duplicate loose name “{0}” in the GUID cache.", entry.Name));
					names.Add(entry.Name, true);
				}
			}

			return retval;
		}

		#endregion
	}

	#region LooseGuidXml Type

	public partial class LooseGuidXml
	{
	}

	#endregion

	#region StrictGuidXml Type

	public partial class StrictGuidXml
	{
	}

	#endregion
}
