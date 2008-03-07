/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

using JetBrains.Annotations;
using JetBrains.Interop.WinApi;
using JetBrains.Omea.OpenAPI;

using Microsoft.Win32;

namespace JetBrains.Omea.Charsets
{
	[Flags]
	public enum CharsetFlags
	{
		None = 0,
		NntpCharset = 1
	}

	/// <summary>
	/// Enumerates charsets stored in registry and applicable to internet mail, news or web pages.
	/// </summary>
	public class CharsetsEnum : IEnumerable<CharsetsEnum.Charset>
	{
		#region Data

		/// <summary>
		/// Name of the Registry value that, for an alias charset, gets the name of the destination charset.
		/// </summary>
		protected static readonly string AliasForCharsetValue = "AliasForCharset";

		/// <summary>
		/// A Registry key with the list of charsets, by their names, with links to the appropriate codepages; and aliases to the charsets.
		/// </summary>
		protected static readonly string KeyCharsets = @"MIME\Database\Charset";

		/// <summary>
		/// A Registry key with the list of codepages, to which we jump from the charset to retrieve the user-friendly name of the encoding.
		/// </summary>
		protected static readonly string KeyCodepages = @"MIME\Database\Codepage";

		/// <summary>
		/// Name of the Registry value that gets the user-friendly description of a code page.
		/// </summary>
		protected static readonly string ValueDescription = "Description";

		/// <summary>
		/// Name of the Registry value that gives the codepage family.
		/// </summary>
		protected static readonly string ValueFamily = "Family";

		/// <summary>
		/// Name of the Registry value that gets the codepage associated with a charset.
		/// </summary>
		protected static readonly string ValueInternetEncoding = "InternetEncoding";

		protected CharsetFlags _flags;

		#endregion

		#region Init

		public CharsetsEnum()
		{
			_flags = CharsetFlags.None;
		}

		public CharsetsEnum(CharsetFlags flags)
		{
			_flags = flags;
		}

		#endregion

		#region Operations

		/// <summary><seealso cref="TryGetCharset"/>
		/// Looks up a charset by its name. Throws an exception on failure.
		/// </summary>
		/// <returns>The charset identified by the given charset name.</returns>
		public static Charset GetCharset(string name)
		{
			Charset retval = Charset.TryGetCharset(name);
			if(retval == null)
				throw new InvalidOperationException(String.Format("Unable to create the charset object. There is no information available in the registry for the “{0}” charset.", name));
			return retval;
		}

		/// <summary>
		/// Returns the <see cref="Charset"/> object for the charset that is registered as the system's default for message bodies.
		/// Note that this it does not necessarily correspond to the system's ANSI code page, eg for Cyrillic systems it will be “koi8-r” whilst having the default ANSI codepage set to “windows-1251”.
		/// </summary>
		/// <returns>The default body charset.</returns>
		public static Charset GetDefaultBodyCharset()
		{
			return GetCharset(Encoding.Default.BodyName);
		}

		/// <summary>
		/// Returns the <see cref="Charset"/> object for the charset that is registered as the system's default for Web and local use.
		/// Unlike <see cref="GetDefaultBodyCharset"/>, usually corresponds to the system's ANSI codepage.
		/// </summary>
		/// <returns>The default web charset.</returns>
		public static Charset GetDefaultWebCharset()
		{
			return GetCharset(Encoding.Default.WebName);
		}

		/// <summary><seealso cref="GetCharset"/>
		/// Looks up a charset by its name. Returns <c>Null</c> on failure and does not throw an exception.
		/// </summary>
		/// <returns>The charset identified by the given charset name,
		/// or a <c>Null</c> value if such a charset is not registered in the system.</returns>
		public static Charset TryGetCharset(string name)
		{
			return Charset.TryGetCharset(name);
		}

		#endregion

		#region IEnumerable<Charset> Members

		///<summary>
		///Returns an enumerator that iterates through the collection.
		///</summary>
		///
		///<returns>
		///A <see cref="T:System.Collections.Generic.IEnumerator`1"></see> that can be used to iterate through the collection.
		///</returns>
		///<filterpriority>1</filterpriority>
		public IEnumerator<Charset> GetEnumerator()
		{
			// Start enumerating the charsets (subkeys)
			RegistryKey key = Registry.ClassesRoot.OpenSubKey(KeyCharsets, false);
			string[] arCharsetNames = key != null ? key.GetSubKeyNames() : new string[] {};

			// Jump to the next charset that is not an alias
			foreach(string sCharset in arCharsetNames)
			{
				Charset current = Charset.TryGetCharset(sCharset);
				if((current != null) && (current.Name == sCharset)) // Name may differ if it was an alias, such ones we skip
					yield return current; // Gotten the next value OK
			}
		}

		///<summary>
		///Returns an enumerator that iterates through a collection.
		///</summary>
		///
		///<returns>
		///An <see cref="T:System.Collections.IEnumerator"></see> object that can be used to iterate through the collection.
		///</returns>
		///<filterpriority>2</filterpriority>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region Charset Type

		/// <summary>
		/// Represents a single charset in the charsets enumeration.
		/// </summary>
		/// <remarks>The comparer sorts charsets first by the family, then lexicographically by the description within a family.</remarks>
		public class Charset : IComparable
		{
			#region Data

			private const string RegexDll = "Dll";

			private const string RegexResId = "ResId";

			/// <summary>
			/// Regex that matches charset descriptions referencing an external resource in a DLL.
			/// Example: “<c>@%SystemRoot%\system32\mlang.dll,-4643</c>” (for utf-8).
			/// </summary>
			[NotNull]
			protected static readonly Regex _regexExternalDescription = new Regex(string.Format(@"^@(?<{0}>.+),(?<{1}>-?\d+)", RegexDll, RegexResId), RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline);

			/// <summary>
			/// Codepage identifier for this charset.
			/// It is a codepage for which this charset is the InternetEncoding.
			/// </summary>
			protected readonly int _codepage;

			/// <summary>
			/// User-friendly description for this charset.
			/// </summary>
			protected readonly string _description;

			/// <summary>
			/// Internet string ID for the charset.
			/// </summary>
			protected readonly string _name;

			/// <summary>
			/// Some codepages are gathered into a family, eg all the Cyrillic encodings, all the Unicodes, and so on.
			/// For such a family, this is a codepage for the main charset in the family.
			/// For those charsets that don't have a family, it's merely the same value as <see cref="_codepage"/>.
			/// If <see cref="_nFamilyCodepage"/> is equal to <see cref="_codepage"/>,
			/// then it's either the main charset of the family or a standalone charset without a family.
			/// </summary>
			protected readonly int _nFamilyCodepage;

			#endregion

			#region Init

			/// <summary>
			/// Constructs the charset object, specifying all of its parameters explicitly.
			/// For internal use only. Call <see cref="TryGetCharset"/> instead.
			/// </summary>
			/// <param name="sName">Name of the charset (eg “Windows-1251”).</param>
			/// <param name="sDescription">User-friendly description of the encoding (eg “Cyrillic (Windows)”).</param>
			/// <param name="nCodepage">Personal codepage of this charset (eg 1251 for “windows-1251” but 20866 for “koi8-r”).</param>
			/// <param name="nFamilyCodepage">Family codepage for those charsets that have a family, or self codepage for the rest.</param>
			internal Charset(string sName, int nCodepage, string sDescription, int nFamilyCodepage)
			{
				_name = sName;
				_description = sDescription;
				_codepage = nCodepage;
				_nFamilyCodepage = nFamilyCodepage;
			}

			#endregion

			#region Attributes

			/// <summary>
			/// Gets the numeric codepage identifier for this charset.
			/// It is a codepage for which this charset is the InternetEncoding.
			/// </summary>
			public int Codepage
			{
				get
				{
					return _codepage;
				}
			}

			/// <summary>
			/// Gets the user-friendly description for this charset.
			/// </summary>
			public string Description
			{
				get
				{
					return _description;
				}
			}

			/// <summary>
			/// Some codepages are gathered into a family, eg all the Cyrillic encodings, all the Unicodes, and so on.
			/// For such a family, this is a codepage for the main charset in the family.
			/// For those charsets that don't have a family, it's merely the same value as <see cref="_codepage"/>.
			/// If <see cref="_nFamilyCodepage"/> is equal to <see cref="_codepage"/>,
			/// then it's either the main charset of the family or a standalone charset without a family.
			/// </summary>
			public int FamilyCodepage
			{
				get
				{
					return _nFamilyCodepage;
				}
			}

			/// <summary>
			/// Gets whether the charset is the default for this system/user.
			/// </summary>
			public bool IsDefaultBodyCharset
			{
				get
				{
					return _name == Encoding.Default.BodyName;
				}
			}

			/// <summary>
			/// Gets whether the charset is the default for this system/user.
			/// </summary>
			public bool IsDefaultWebCharset
			{
				get
				{
					return _name == Encoding.Default.WebName;
				}
			}

			/// <summary>
			/// Gets the Internet string ID for the charset.
			/// </summary>
			public string Name
			{
				get
				{
					return _name;
				}
			}

			#endregion

			#region Operations

			/// <summary>
			/// Constructs and returns a charset object for the given codepage,
			/// or a <c>Null</c> value if such a charset is not registered in the system.
			/// Note that the <see cref="Name"/> property of the resulting charset may differ from the <paramref name="sCharset"/>
			/// parameter value, in case the latter specifies an alias to a charset.
			/// </summary>
			/// <param name="sCharset">Name of the charset to construct. Case-insensitive.</param>
			/// <returns>A <see cref="Charset"/> instance, in case such a charset exists; <c>Null</c> if an error has occured.</returns>
			public static Charset TryGetCharset(string sCharset)
			{
				#region Preconditions

				if(sCharset == null)
					throw new ArgumentNullException("sCharset", "Input charset string is NULL.");

				#endregion Preconditions

				// Get the Codepage associated with this charset
				int nCodepage;
				RegistryKey keyCharset; // Registry Key for the charset
				if((keyCharset = Registry.ClassesRoot.OpenSubKey(KeyCharsets + '\\' + sCharset, false)) == null)
					return null;

				using(keyCharset)
				{
					// Alias?
					object oValue;
					if(((oValue = keyCharset.GetValue(AliasForCharsetValue)) != null) && (oValue is string))
						return TryGetCharset((string)oValue); // Resolve the alias

					// Get the integer codepage
					if((nCodepage = (int)keyCharset.GetValue(ValueInternetEncoding, -1)) == -1)
						return null; // Neither an alias, nor a normal charset %-/
				}

				// Open the codepage key and retrieve the description and family info
				string sDescription = null;
				int nFamily = -1;
				RegistryKey keyCodepage = Registry.ClassesRoot.OpenSubKey(KeyCodepages + '\\' + nCodepage, false);
				if(keyCodepage != null) // There's such an encoding 
				{
					using(keyCodepage)
					{
						sDescription = keyCodepage.GetValue(ValueDescription) as string;
						nFamily = (int)keyCodepage.GetValue(ValueFamily, -1);
					}
				}
				// Supply with the default values
				if(sDescription == null) // Either no such codepage-key or no description value under it
					sDescription = "Unknown (" + sCharset + ")";
				else
					sDescription = TryParseDescriptionFromResources(sDescription, sCharset);
				if(nFamily == -1)
					nFamily = nCodepage; // No family => either a family root, or not a member of a family; use self codepage in both cases

				// Fill in the charset data
				return new Charset(sCharset, nCodepage, sDescription, nFamily);
			}

			#endregion

			#region Implementation

			/// <summary>
			/// Some charset descriptions are stored in DLL resources, starting with WinNT 6.
			/// If such is the case, rip out the description.
			/// </summary>
			/// <param name="description">The description that might be a resource reference.</param>
			/// <param name="sCharset">Charset name, to use as a backup in case the <see cref="description"/> points to a missing resource.</param>
			/// <returns>Either the original description, or the results of an attempt to resolve the resource reference.</returns>
			[NotNull]
			private static string TryParseDescriptionFromResources([NotNull] string description, [NotNull] string sCharset)
			{
				if(description == null)
					throw new ArgumentNullException("description");
				if(sCharset == null)
					throw new ArgumentNullException("sCharset");
				try
				{
					// Is a reference?
					Match match = _regexExternalDescription.Match(description);
					if(!match.Success)
						return description;

					// Reference parts
					string sDll = match.Groups[RegexDll].Value;
					string sResId = match.Groups[RegexResId].Value;

					// The ID "AS IS" may be negative, but negative values are not allowed
					// We'll try bitwise-turning it into UINT, and taking an ABS from it
					int nResId = int.Parse(sResId);

					// Try loading
					foreach(uint uid in new[] {unchecked((uint)nResId), unchecked((uint)-nResId)})
					{
						string resourcestring = User32Dll.Helpers.TryLoadStringResource(sDll, uid);
						if(resourcestring != null)
							return resourcestring;
					}
				}
				catch(Exception ex)
				{
					Core.ReportException(ex, false);
				}

				return sCharset;
			}

			#endregion

			#region Overrides

			/// <summary>
			/// Compare charsets by their ID.
			/// </summary>
			public override bool Equals(object obj)
			{
				var other = obj as Charset;
				return other == null ? false : Name.Equals(other.Name);
			}

			/// <summary>
			/// Hashes the charset ID.
			/// </summary>
			public override int GetHashCode()
			{
				return Name.GetHashCode();
			}

			/// <summary>
			/// Hands out the charset name / ID.
			/// </summary>
			public override string ToString()
			{
				return Name;
			}

			#endregion

			#region ERROR

			public static bool operator ==(Charset α, Charset β)
			{
				if(ReferenceEquals(α, null))
					return ReferenceEquals(β, null) ? true : false;
				if(ReferenceEquals(β, null))
					return false;
				return α.Name == β.Name;
			}

			public static bool operator !=(Charset ξ, Charset η)
			{
				if(ReferenceEquals(ξ, null))
					return ReferenceEquals(η, null) ? false : true;
				if(ReferenceEquals(η, null))
					return true;
				return ξ.Name != η.Name;
			}

			#endregion

			#region IComparable Members

			/// <summary>
			/// The comparer sorts charsets first by the family, then lexicographically by the description within a family.
			/// </summary>
			public int CompareTo(object obj)
			{
				var other = obj as Charset;
				if(other == null)
					throw new ArgumentNullException();

				// If the charsets have equal IDs, don't perform further comparisons.
				if(Name.CompareTo(other.Name) == 0)
					return 0;

				int diff;

				// Level 1: Family Codepage
				if((diff = FamilyCodepage.CompareTo(other.FamilyCodepage)) != 0)
					return diff;

				return Description.CompareTo(other.Description);
			}

			#endregion
		}

		#endregion
	}
}