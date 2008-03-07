/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using System.Xml;

using JetBrains.Interop.WinApi;

using Microsoft.Win32;

namespace JetBrains.Omea.InstantMessaging.Trillian
{

	/// <summary>
	/// Manages working with Trillian profile and log files.
	/// </summary>
	public class TrillianProfileManager
	{
		private readonly ArrayList _profiles = new ArrayList();

		/// <summary>
		/// Finds the Trillian installation path and initializes the list of available profiles.
		/// </summary>
		public TrillianProfileManager()
		{
			string trillianDir = GetTrillianDirectory();
			if(trillianDir == null)
				return;

			string globalDir = GetGlobalDirectory(trillianDir);
			if(globalDir != null)
			{
				string iniPath = Path.Combine(globalDir, "profiles.ini");
				var profileCount = (int)Kernel32Dll.GetPrivateProfileIntW("Profiles", "num", 0, iniPath);
				for(int i = 0; i < profileCount; i++)
				{
					string section = "Profile" + i.ToString("D3");

					string sName = Kernel32Dll.Helpers.GetProfileString(iniPath, section, "Name", "");
					if(sName.Length > 0)
					{
						var prefType = (int)Kernel32Dll.GetPrivateProfileIntW(section, "Preferences Type", 0, iniPath);
						var profile = new TrillianProfile(trillianDir, sName, prefType);
						if(profile.IsValid())
							_profiles.Add(profile);
					}
				}
			}
		}

/// <summary>
/// Returns the number of available Trillian profiles.
/// </summary>
		public int ProfileCount
		{
			get
			{
				return _profiles.Count;
			}
		}

/// <summary>
/// Returns the enumerator for the collection of profiles.
/// </summary>
		public IEnumerable Profiles
		{
			get
			{
				return _profiles;
			}
		}

/// <summary>
/// Returns the directory where the global profiles.ini file is stored.
/// </summary>
		private static string GetGlobalDirectory(string trillianDir)
		{
			string iniPath = Path.Combine(trillianDir, "trillian.ini");

			string globalDir = Kernel32Dll.Helpers.GetProfileString(iniPath, "General", "Global Directory", "");
			if(!Directory.Exists(globalDir))
				return null;

			return globalDir;
		}

		/// <summary>
		/// Returns the Trillian installation directory.	
		/// </summary>
		private static string GetTrillianDirectory()
		{
			string uninstKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Trillian";
			using(RegistryKey regKey = Registry.LocalMachine.OpenSubKey(uninstKey))
			{
				if(regKey == null)
					return null;

				var uninstString = (string)regKey.GetValue("UninstallString");
				if(uninstString == null)
					return null;

				int index = uninstString.LastIndexOf('\\');
				if(index < 0)
					return null;

				return uninstString.Substring(0, index);
			}
		}
	}


	/// <summary>
	/// A single Trillian profile.
	/// </summary>
	public class TrillianProfile
	{
		private readonly string _name;

		private readonly string _profileDir;

		private TrillianBuddyGroup _buddies;

		private readonly string _aimIniPath;

		private string _icqProfileSection;


		/// <summary>
		/// Initializes the root directory for the specified profile.
		/// </summary>
		public TrillianProfile(string trillianDir, string name, int prefType)
		{
			_name = name;
			string profileRoot = null;
			switch(prefType)
			{
			case 0:
				profileRoot = Path.Combine(trillianDir, "users");
				break;

			case 1:
				profileRoot = Environment.GetFolderPath(Environment.SpecialFolder.Personal); // My Documents
				profileRoot = Path.Combine(Path.GetDirectoryName(profileRoot), "Trillian\\User Settings");
				break;
			}

			if(profileRoot != null)
			{
				_profileDir = Path.Combine(profileRoot, name);
				_aimIniPath = Path.Combine(_profileDir, "aim.ini");
			}
		}


		/// <summary>
		/// Checks if the profile directory actually points to a valid Trillian profile.
		/// </summary>
		public bool IsValid()
		{
			return _profileDir != null && Directory.Exists(_profileDir);
		}

		public string Name
		{
			get
			{
				return _name;
			}
		}


		/// <summary>
		/// Returns the root buddy group of the Trillian contact list.
		/// </summary>
		public TrillianBuddyGroup Buddies
		{
			get
			{
				if(_buddies == null)
					LoadBuddies();
				return _buddies;
			}
		}

/// <summary>
/// Returns the list of logs for the specified protocol.
/// </summary>
		public TrillianLog[] GetLogs(string protocol)
		{
			string logDir = Path.Combine(_profileDir, Path.Combine("Logs", protocol));
			if(!Directory.Exists(logDir))
				return new TrillianLog[] {};

			string[] fileNames = Directory.GetFiles(logDir);
			var result = new TrillianLog[fileNames.Length];
			for(int i = 0; i < fileNames.Length; i++)
				result[i] = new TrillianLog(Path.Combine(logDir, fileNames[i]));
			return result;
		}


		/// <summary>
		/// Reads the specified setting for the ICQ profile from aim.ini.
		/// </summary>
		public string ReadICQSetting(string setting)
		{
			if(_icqProfileSection == null)
				_icqProfileSection = FindAIMorICQProfile(true);

			if(_icqProfileSection.Length == 0)
				return "";

			return Kernel32Dll.Helpers.GetProfileString(_aimIniPath, _icqProfileSection, setting, "");
		}

		/**
         * Loads and parses the buddy.xml file.
         */

		private void LoadBuddies()
		{
			string buddyFile = Path.Combine(_profileDir, "Buddies.xml");
			Trace.WriteLine("Loading buddy file " + buddyFile);
			try
			{
				var doc = new XmlDocument();
				doc.Load(buddyFile);
				_buddies = new TrillianBuddyGroup(doc.SelectSingleNode("/buddies/section"));
			}
			catch(Exception ex)
			{
				Trace.WriteLine("Failed to load Buddies.xml: " + ex);
			}
		}

		/// <summary>
		/// Scans through aim.ini to find which of the profiles in it is an AIM or ICQ profile.
		/// </summary>
		private string FindAIMorICQProfile(bool needICQ)
		{
			for(int nProfile = 0; nProfile < 0x100; nProfile++)
			{
				string section = "profile " + nProfile;
				string sName = Kernel32Dll.Helpers.GetProfileString(_aimIniPath, section, "name", "");
				if(sName.Length == 0)
					break;

				// An all-numeric name is the UIN of an ICQ account; any other name is
				// a screen name of an AIM account.
				int tmp;
				bool isICQ = Int32.TryParse(sName, out tmp);

				if(isICQ == needICQ)
					return section;
			}
			return "";
		}
	}

	/**
     * A group of buddies in a Trillian profile.
     */

	public class TrillianBuddyGroup
	{
		private string _title;

		private readonly ArrayList _buddies = new ArrayList();

		private readonly ArrayList _groups = new ArrayList();

		/**
         * Loads the buddy group data from the specified XML node.
         */

		internal TrillianBuddyGroup(XmlNode node)
		{
			if(node == null)
				return;

			foreach(XmlNode childNode in node.ChildNodes)
			{
				Debug.WriteLine("Buddy group child node: " + childNode.Name);
				if(childNode.Name == "buddy")
				{
					var buddy = new TrillianBuddy(childNode);
					if(buddy.IsValid())
						_buddies.Add(buddy);
				}
				else if(childNode.Name == "group")
					_groups.Add(new TrillianBuddyGroup(childNode));
				else if(childNode.Name == "title")
					_title = childNode.Value;
			}
		}

		/**
         * Returns the IEnumerable for enumerating the buddies in a group.
         */

		public IEnumerable Buddies
		{
			get
			{
				return _buddies;
			}
		}

		/**
         * Returns the IEnumerable for enumerating the child groups of a group.
         */

		public IEnumerable Groups
		{
			get
			{
				return _groups;
			}
		}
	}

	/**
     * A single buddy in a Trillian profile.
     */

	public class TrillianBuddy
	{
		private readonly string _protocol;

		private readonly string _address;

		private readonly string _nick;

		/**
         * Loads the buddy data from the specified XML node.
         */

		internal TrillianBuddy(XmlNode node)
		{
			XmlAttribute url = node.Attributes["uri"];
			if(url == null)
				return;

			var separators = new[] {':'};
			string[] parts = url.Value.Split(separators, 2);
			if(parts.Length != 2)
				return;

			_protocol = parts[0];
			byte[] partBytes = HttpUtility.UrlDecodeToBytes(parts[1]);
			parts = Encoding.Default.GetString(partBytes).Split(separators, 3);
			if(parts.Length != 3)
				return;

			_address = parts[1];
			_nick = parts[2];
		}

		public bool IsValid()
		{
			return _protocol != null && _address != null && _nick != null;
		}

		public string Protocol
		{
			get
			{
				return _protocol;
			}
		}

		public string Address
		{
			get
			{
				return _address;
			}
		}

		public string Nick
		{
			get
			{
				return _nick;
			}
		}
	}
}
