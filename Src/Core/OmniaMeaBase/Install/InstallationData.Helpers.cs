// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// Pair for the auto-generated RegistryData.cs, provides the helper ctors and methods.

using System;
using System.Collection.Generic;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Schema;
using JetBrains.Annotations;

namespace JetBrains.Build.InstallationData
{
	public partial class InstallationDataXml
	{
		#region Init

		public InstallationDataXml([NotNull] RegistryXml registry, [NotNull] params FolderXml[] folders)
		{
			if(registry == null)
				throw new ArgumentNullException("registry");
			if(folders == null)
				throw new ArgumentNullException("folders");

			Registry = registry;
			Files = folders;
		}

		public InstallationDataXml()
			: this(new RegistryXml(), new FolderXml[] {})
		{
		}

		public InstallationDataXml(RegistryXml registry)
			: this(registry, new FolderXml[] {})
		{
		}

		public InstallationDataXml(FolderXml folderxml)
			: this(new RegistryXml(), folderxml)
		{
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Gets the XSD for the Registry Data XML files.
		/// </summary>
		public static XmlSchema RegistryDataXmlSchema
		{
			get
			{
				using(Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("JetBrains.Build.RegistryData.RegistryData.xsd"))
					return XmlSchema.Read(stream, null);
			}
		}

		#endregion

		#region Operations

		public void AssertValid()
		{
			if(Registry == null)
				throw new InvalidOperationException(string.Format("The Registry must not be Null."));
			Registry.AssertValid();

			if(Files == null)
				throw new InvalidOperationException(string.Format("The Files collection must not be Null."));
			var hashFolderIds = new Dictionary<string, bool>();
			foreach(FolderXml folderxml in Files)
			{
				folderxml.AssertValid();
				if(hashFolderIds.ContainsKey(folderxml.Id))
					throw new InvalidOperationException(string.Format("Duplicate folder ID “{0}”.", folderxml.Id));
				hashFolderIds.Add(folderxml.Id, true);
			}
		}

		/// <summary>
		/// Makes sure all of the collections are non-Null (but may be empty).
		/// </summary>
		public void EnsureNotNull()
		{
			if(Registry == null)
				Registry = new RegistryXml();
			Registry.EnsureNotNull();
			if(Files == null)
				Files = new FolderXml[] {};
		}

		/// <summary>
		/// Merges the <paramref name="addon"/> installation data into the host's, and destroys the former.
		/// All of the collections are guaranteed to be non-Null
		/// </summary>
		public void MergeWith(InstallationDataXml addon)
		{
			EnsureNotNull();

			Registry.MergeWith(addon.Registry);

			// Merge files
			var files = new List<FolderXml>(Files);
			files.AddRange(addon.Files);
			Files = files.ToArray();
		}

		/// <summary>
		/// Checks for duplicate keys and values, removes, if any.
		/// </summary>
		public void RemoveDuplicates()
		{
			EnsureNotNull();

			if(Registry != null)
				Registry.RemoveDuplicates();

			// Remove file duplicates (from the target point of view)
			var hashFiles = new Dictionary<string, bool>();
			foreach(FolderXml folder in Files)
			{
				if(folder.Files == null)
					continue;
				foreach(FileXml file in folder.Files)
				{
					string sFileId = folder.TargetRoot + ":" + folder.TargetDir.Trim() + ":" + file.TargetName;
					if(hashFiles.ContainsKey(sFileId))
						continue;
					hashFiles.Add(sFileId, true);
				}
			}
		}

		#endregion
	}

	public partial class RegistryXml
	{
		#region Init

		/// <summary>
		/// The default ctor, leaves the collections empty.
		/// </summary>
		public RegistryXml()
			: this(new RegistryKeyXml[] {}, new RegistryValueXml[] {})
		{
		}

		/// <summary>
		/// Creates a <see cref="RegistryXml"/> object and fills it with data.
		/// </summary>
		/// <param name="keys">Keys.</param>
		/// <param name="values">Values.</param>
		public RegistryXml([NotNull] ICollection<RegistryKeyXml> keys, [NotNull] ICollection<RegistryValueXml> values)
		{
			if(keys == null)
				throw new ArgumentNullException("keys");
			if(values == null)
				throw new ArgumentNullException("values");

			Key = new RegistryKeyXml[keys.Count];
			keys.CopyTo(Key, 0);
			Value = new RegistryValueXml[values.Count];
			values.CopyTo(Value, 0);
		}

		#endregion

		#region Operations

		public void AssertValid()
		{
			if(Key == null)
				throw new InvalidOperationException(string.Format("The keys collection must not be Null."));
			foreach(RegistryKeyXml keyxml in Key)
				keyxml.AssertValid();

			if(Value == null)
				throw new InvalidOperationException(string.Format("The values collection must not be Null."));
			foreach(RegistryValueXml valuexml in Value)
				valuexml.AssertValid();

			// Note: the macros collection is fake and should not be validated.
		}

		/// <summary>
		/// Makes sure all of the collections are non-Null (but may be empty).
		/// </summary>
		public void EnsureNotNull()
		{
			if(Key == null)
				Key = new RegistryKeyXml[] {};
			if(Value == null)
				Value = new RegistryValueXml[] {};
		}

		/// <summary>
		/// Merges the <paramref name="addon"/> <see cref="RegistryXml"/> keys and values into the host's, and destroys the former.
		/// The <see cref="Key"/> or <see cref="Value"/> colections may be <c>Null</c> on either parameter, but on return they're guaranteed to be non-<c>Null</c> in this object.
		/// </summary>
		public void MergeWith([NotNull] RegistryXml addon)
		{
			if(addon == null)
				throw new ArgumentNullException("addon");

			var keys = new List<RegistryKeyXml>(Key ?? new RegistryKeyXml[] {});
			var values = new List<RegistryValueXml>(Value ?? new RegistryValueXml[] {});

			foreach(RegistryKeyXml key in addon.Key ?? new RegistryKeyXml[] {})
				keys.Add(key);
			foreach(RegistryValueXml value in addon.Value ?? new RegistryValueXml[] {})
				values.Add(value);

			addon.Key = new RegistryKeyXml[] {};
			addon.Value = new RegistryValueXml[] {};

			Key = keys.ToArray();
			Value = values.ToArray();
		}

		/// <summary>
		/// Checks for duplicate keys and values, removes, if any.
		/// </summary>
		public void RemoveDuplicates()
		{
			IList<RegistryKeyXml> keysOld = Key ?? new RegistryKeyXml[] {};
			IList<RegistryValueXml> valuesOld = Value ?? new RegistryValueXml[] {};

			var keysNew = new List<RegistryKeyXml>(keysOld.Count);
			var valuesNew = new List<RegistryValueXml>(valuesOld.Count);

			var hashKeys = new HashSet<RegistryKeyXml>();
			var hashValues = new HashSet<RegistryValueXml>();

			foreach(RegistryKeyXml keyxml in keysOld)
			{
				if(hashKeys.Contains(keyxml))
					continue;
				keysNew.Add(keyxml);
				hashKeys.Add(keyxml);
			}
			foreach(RegistryValueXml valuexml in valuesOld)
			{
				if(hashValues.Contains(valuexml))
					continue;
				valuesNew.Add(valuexml);
				hashValues.Add(valuexml);
			}

			Key = keysNew.ToArray();
			Value = valuesNew.ToArray();
		}

		/// <summary>
		/// Wraps into the installation data object.
		/// </summary>
		/// <returns></returns>
		public InstallationDataXml ToInstallationData()
		{
			return new InstallationDataXml(this);
		}

		#endregion
	}

	public partial class RegistryKeyXml : IEquatable<RegistryKeyXml>
	{
		#region Init

		public RegistryKeyXml()
		{
		}

		/// <summary>
		/// Creates a <see cref="RegistryValueXml"/> object.
		/// </summary>
		/// <param name="hive">Hive.</param>
		/// <param name="key">Path to the key under the hive.</param>
		public RegistryKeyXml(RegistryHiveXml hive, [NotNull] string key)
		{
			if(key == null)
				throw new ArgumentNullException("key");
			Hive = hive;
			Key = key;
		}

		#endregion

		#region Operations

		/// <summary>
		/// Creates a new Registry Value that derives its Hive and Key path from the current Registry key.
		/// </summary>
		/// <param name="name">Name of the value.</param>
		/// <param name="value">Value of the value, must be either a <see cref="string"/> or an <see cref="int"/>.</param>
		/// <returns>The new registry value.</returns>
		public RegistryValueXml CreateValue([NotNull] string name, [NotNull] object value)
		{
			return new RegistryValueXml(Hive, Key, name, value);
		}

		/// <summary>
		/// Creates a new array consisting of only this object.
		/// </summary>
		public RegistryKeyXml[] ToArray()
		{
			return new[] {this};
		}

		#endregion

		#region Overrides

		public override bool Equals(object obj)
		{
			if(this == obj)
				return true;
			return Equals(obj as RegistryKeyXml);
		}

#pragma warning disable RedundantOverridenMember
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
#pragma warning restore RedundantOverridenMember

		///<summary>
		///Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		///</summary>
		///
		///<returns>
		///A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		///</returns>
		///<filterpriority>2</filterpriority>
		public override string ToString()
		{
			return string.Format("<RegistryKeyXml Hive=“{0}” Key=“{1}”>", Hive, Key);
		}

		#endregion

		#region IEquatable<RegistryKeyXml> Members

		public bool Equals(RegistryKeyXml registryKeyXml)
		{
			if(registryKeyXml == null)
				return false;
			if(!base.Equals(registryKeyXml))
				return false;
			return true;
		}

		#endregion
	}

	public partial class RegistryBaseXml : IEquatable<RegistryBaseXml>
	{
		#region Operations

		public virtual void AssertValid()
		{
			if(string.IsNullOrEmpty(Key))
				throw new InvalidOperationException(string.Format("The key must not be empty."));
			if(Key.IndexOf("/") >= 0)
				throw new InvalidOperationException(string.Format("The slashes in the key must be reverse."));
		}

		#endregion

		#region Overrides

		public override bool Equals(object obj)
		{
			if(this == obj)
				return true;
			return Equals(obj as RegistryBaseXml);
		}

		public override int GetHashCode()
		{
			return Hive.GetHashCode() + 29 * (Key != null ? Key.GetHashCode() : 0);
		}

		#endregion

		#region IEquatable<RegistryBaseXml> Members

		public bool Equals(RegistryBaseXml registryBaseXml)
		{
			if(registryBaseXml == null)
				return false;
			return Equals(Hive, registryBaseXml.Hive) && Equals(Key, registryBaseXml.Key);
		}

		#endregion
	}

	public partial class FileXml
	{
		#region Init

		public FileXml(string name)
			: this(name, name)
		{
		}

		public FileXml(string sSourceName, string sTargetName)
		{
			SourceName = sSourceName;
			TargetName = sTargetName;
		}

		#endregion

		#region Operations

		public void AssertValid()
		{
			if(string.IsNullOrEmpty(SourceName))
				throw new InvalidOperationException(string.Format("The source name must be specified."));

			if(TargetName == null) // Target name is allowed to be empty
				throw new InvalidOperationException(string.Format("The target name must not be Null."));
		}

		#endregion
	}

	public partial class FolderXml
	{
		#region Operations

		public void AssertValid()
		{
			Normalize();

			if((Files == null) || (Files.Length == 0))
				throw new InvalidOperationException(string.Format("The files collection of the folder must not be empty."));

			if(string.IsNullOrEmpty(Id))
				throw new InvalidOperationException(string.Format("The ID of a folder must not be empty."));

			if(string.IsNullOrEmpty(MsiComponentGuid))
				throw new InvalidOperationException(string.Format("The GUID must be specified."));

			foreach(FileXml filexml in Files)
				filexml.AssertValid();
		}

		public void Normalize()
		{
			TargetDir = TargetDir.Trim().Trim('/', '\\').Replace('/', '\\').Trim();
			SourceDir = SourceDir.Trim().Trim('/', '\\').Replace('/', '\\').Trim();
		}

		public InstallationDataXml ToInstallationData()
		{
			return new InstallationDataXml(this);
		}

		#endregion
	}

	public partial class MacroXml
	{
	}

	public partial class RegistryValueXml : IEquatable<RegistryValueXml>
	{
		#region Init

		/// <summary>
		/// Creates a <see cref="RegistryValueXml"/> object.
		/// </summary>
		/// <param name="hive">Hive.</param>
		/// <param name="key">Path to the key under the hive.</param>
		/// <param name="name">Name of the value.</param>
		/// <param name="value">Value of the value, must be either a <see cref="string"/> or an <see cref="int"/>.</param>
		public RegistryValueXml(RegistryHiveXml hive, [NotNull] string key, [NotNull] string name, [NotNull] object value)
		{
			if(key == null)
				throw new ArgumentNullException("key");
			if(name == null)
				throw new ArgumentNullException("name");
			if(value == null)
				throw new ArgumentNullException("value");

			Hive = hive;
			Key = key;
			Name = name;
			if(value is string)
			{
				Type = RegistryValueTypeXml.String;
				Value = (string)value;
			}
			else if(value is int)
			{
				Type = RegistryValueTypeXml.Dword;
				Value = value.ToString();
			}
			else
				throw new InvalidOperationException(string.Format("The value type “{0}” is not supported.", value.GetType().AssemblyQualifiedName));
		}

		#endregion

		#region Operations

		/// <summary>
		/// Creates a new array consisting of only this object.
		/// </summary>
		public RegistryValueXml[] ToArray()
		{
			return new[] {this};
		}

		/// <summary>
		/// Creates a new Registry with just this value.
		/// </summary>
		public RegistryXml ToRegistry()
		{
			return new RegistryXml(new RegistryKeyXml[] {}, new[] {this});
		}

		#endregion

		#region Overrides

		public override void AssertValid()
		{
			base.AssertValid();

			if(Name == null)
				throw new InvalidOperationException(string.Format("The Name must not be Null, but it can be empty for the default value of the key."));
		}

		public override bool Equals(object obj)
		{
			if(this == obj)
				return true;
			return Equals(obj as RegistryValueXml);
		}

		public override int GetHashCode()
		{
			int result = base.GetHashCode();
			result = 29 * result + (Name != null ? Name.GetHashCode() : 0);
			result = 29 * result + (Value != null ? Value.GetHashCode() : 0);
			result = 29 * result + Type.GetHashCode();
			return result;
		}

		///<summary>
		///Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		///</summary>
		///
		///<returns>
		///A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
		///</returns>
		///<filterpriority>2</filterpriority>
		public override string ToString()
		{
			return string.Format("<RegistryValueXml Hive=“{0}” Key=“{1}” Name=“{2}” Value=“{3}”>", Hive, Key, Name, Value);
		}

		#endregion

		#region IEquatable<RegistryValueXml> Members

		public bool Equals(RegistryValueXml registryValueXml)
		{
			if(registryValueXml == null)
				return false;
			if(!base.Equals(registryValueXml))
				return false;
			if(!Equals(Name, registryValueXml.Name))
				return false;
			if(!Equals(Value, registryValueXml.Value))
				return false;
			if(!Equals(Type, registryValueXml.Type))
				return false;
			return true;
		}

		#endregion
	}
}
