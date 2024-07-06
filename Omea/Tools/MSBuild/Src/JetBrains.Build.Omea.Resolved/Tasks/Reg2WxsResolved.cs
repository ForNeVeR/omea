// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using JetBrains.Build.Omea.Infra;
using JetBrains.Build.Omea.Resolved.Infra;

using Microsoft.Tools.WindowsInstallerXml.Serialize;

using Directory=Microsoft.Tools.WindowsInstallerXml.Serialize.Directory;

namespace JetBrains.Build.Omea.Resolved.Tasks
{
	/// <summary>
	/// Converts a .reg file into a .wxs file with Registry entries and a dummy structure around them to provide for an XSD-valid file.
	/// </summary>
	public class Reg2WxsResolved : TaskResolved
	{
		#region Implementation

		private static void ParseRegFile(string sRegFile, List<RegistryKey> wixKeys, List<RegistryValue> wixValues)
		{
			using(var reader = new StreamReader(sRegFile, true))
			{
				string line;

				RegistryRootType rootCurrent = RegistryRootType.HKMU; // The current registry Root to read the values into
				string sCurrentKey = ""; // The current key path under the current root to read the values into

				var regexJustRoot = new Regex(@"^HKEY_[^\\]+$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
				var regexRootAndKey = new Regex(@"^(?<Root>HKEY_.+?)\\(?<Key>.*)$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
				var regexValueQuotedQuoted = new Regex("^\"(?<Name>.+?)(?<!\\\\)\"\\s*=\\s*\"(?<Value>.+)\"$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
				var regexDefaultValueQuoted = new Regex("^@=\"(?<Value>.+)\"$", RegexOptions.IgnoreCase | RegexOptions.Singleline);
				while((line = reader.ReadLine()) != null)
				{
					line = line.Trim();
					if(string.IsNullOrEmpty(line))
						continue;

					// A key?
					Match match;
					if(line[0] == '[')
					{
						if(line[line.Length - 1] != ']')
							throw new InvalidOperationException(string.Format("There's a key opening bracket, but no closing one."));

						string key = line.Substring(1, line.Length - 2);

						// Only HKEY_SMTH? Not interested
						if(regexJustRoot.IsMatch(key))
							continue;
						// Break into the root and the key
						match = regexRootAndKey.Match(key);
						if(!match.Success)
							throw new InvalidOperationException(string.Format("Failed to parse the key “{0}”.", key));
						string sKeyPrefix = "";
						string sRootValue = match.Groups["Root"].Value.ToUpperInvariant();
						switch(sRootValue)
						{
						case "HKEY_CLASSES_ROOT":
							rootCurrent = RegistryRootType.HKMU;
							sKeyPrefix = "Software\\Classes\\";
							break;
						case "HKEY_LOCAL_MACHINE":
							rootCurrent = RegistryRootType.HKLM;
							break;
						case "HKEY_CURRENT_USER":
							rootCurrent = RegistryRootType.HKCU;
							break;
						case "HKEY_USERS":
							rootCurrent = RegistryRootType.HKU;
							break;
						default:
							throw new InvalidOperationException(string.Format("Unsupported registry root “{0}”.", sRootValue));
						}
						sCurrentKey = sKeyPrefix + match.Groups["Key"].Value;

						// List the key
						var wixKey = new RegistryKey();
						wixKey.Root = rootCurrent;
						wixKey.Key = sCurrentKey;
						wixKey.Action = RegistryKey.ActionType.createAndRemoveOnUninstall;
						if(wixKey.Root != RegistryRootType.HKU)
							wixKeys.Add(wixKey);

						continue;
					}

					// A value, otherwise
					// Parse the value
					string sValueName = null;
					object oValueValue = null;
					match = null;
					foreach(Regex regex in new[] {regexValueQuotedQuoted, regexDefaultValueQuoted,})
					{
						match = regex.Match(line);
						if(!match.Success)
							continue;

						if(match.Groups["Name"] != null)
							sValueName = match.Groups["Name"].Value;
						if(match.Groups["Value"] != null)
							oValueValue = match.Groups["Value"].Value;

						break;
					}
					if((match == null) || (!match.Success))
						throw new InvalidOperationException(string.Format("Could not parse the Registry value “{0}”.", line));

					// Create the value
					var wixValue = new RegistryValue();
					wixValue.Root = rootCurrent;
					wixValue.Key = sCurrentKey;
					if(!string.IsNullOrEmpty(sValueName))
						wixValue.Name = sValueName;
					wixValue.Value = oValueValue != null ? oValueValue.ToString() : null;
					wixValue.Type = RegistryValue.TypeType.@string;
					wixValue.Action = RegistryValue.ActionType.write;
					if(wixValue.Root != RegistryRootType.HKU)
						wixValues.Add(wixValue);
				}
			}
		}

		private static void WriteWxsFile(string sOutputWxs, List<RegistryKey> wixKeys, List<RegistryValue> wixValues)
		{
			var wix = new Wix();
			var wixFragment = new Fragment();
			wix.AddChild(wixFragment);
			var wixDirectory = new Directory();
			wixFragment.AddChild(wixDirectory);
			wixDirectory.Id = "";
			var wixComponent = new Component();
			wixDirectory.AddChild(wixComponent);
			wixComponent.Id = "";
			wixComponent.Guid = "*";

			foreach(RegistryKey wixKey in wixKeys)
				wixComponent.AddChild(wixKey);
			foreach(RegistryValue wixValue in wixValues)
				wixComponent.AddChild(wixValue);

			// Save to the output file
			using(var xw = new XmlTextWriter(new FileStream(sOutputWxs, FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.UTF8))
			{
				xw.Formatting = Formatting.Indented;
				wix.OutputXml(xw);
			}
		}

		#endregion

		#region Overrides

		/// <summary>
		/// Actions under the resolver.
		/// </summary>
		protected override void ExecuteTaskResolved()
		{
			string sInputReg = Bag.GetString(AttributeName.InputFile);
			string sOutputWxs = Bag.GetString(AttributeName.OutputFile);

			var wixKeys = new List<RegistryKey>();
			var wixValues = new List<RegistryValue>();

			ParseRegFile(sInputReg, wixKeys, wixValues);
			WriteWxsFile(sOutputWxs, wixKeys, wixValues);
		}

		#endregion
	}
}
