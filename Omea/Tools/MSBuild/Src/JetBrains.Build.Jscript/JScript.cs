// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;

namespace JetBrains.Build
{
	/// <summary>
	/// An MSBuild task that compiles the IL projects.
	/// </summary>
	public class JScript : ManagedCompiler
	{
		#region Init

		public JScript()
		{
			HostCompilerSupportsAllParameters = false;
			NoConfig = false;
		}

		#endregion

		#region Attributes

		/// <summary>
		/// Automatically reference assemblies based on imported namespaces and fully-qualified names (on by default).
		/// </summary>
		public bool Autoref
		{
			get
			{
				return (bool)Bag["Autoref"];
			}
			set
			{
				Bag["Autoref"] = value;
			}
		}

		/// <summary>
		/// Disable language features to allow better code generation.
		/// </summary>
		public bool Fast
		{
			get
			{
				return (bool)Bag["Fast"];
			}
			set
			{
				Bag["Fast"] = value;
			}
		}

		/// <summary>
		/// Do not import standard library (mscorlib.dll) and change autoref default to off.
		/// </summary>
		public string NoStandardLib
		{
			get
			{
				return (string)base.Bag["NoStandardLib"];
			}
			set
			{
				base.Bag["NoStandardLib"] = value;
			}
		}

		/// <summary>
		/// Limit which platforms this code can run on; must be x86, Itanium, x64, or anycpu, which is the default.
		/// </summary>
		public string Platform
		{
			get
			{
				return (string)base.Bag["Platform"];
			}
			set
			{
				base.Bag["Platform"] = value;
			}
		}

		/// <summary>
		/// Set warning level (0-4).
		/// </summary>
		public int WarningLevel
		{
			get
			{
				return (int)Bag["WarningLevel"];
			}
			set
			{
				Bag["WarningLevel"] = value;
			}
		}

		#endregion

		#region Implementation

		internal static void CommandLine_AppendSwitchAliased(CommandLineBuilderExtension commandLine, string switchName, string alias, string parameter)
		{
			commandLine.AppendSwitchIfNotNull(switchName, alias + "=");
			commandLine.GetType().InvokeMember("AppendTextWithQuoting", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, commandLine, new object[] {parameter});
		}

		private static void CommandLine_AppendPlusOrMinusSwitch(string sSwitchName, Hashtable bag, string sBagPropertyName, CommandLineBuilderExtension commandLine)
		{
			commandLine.GetType().InvokeMember("AppendPlusOrMinusSwitch", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, commandLine, new object[] {sSwitchName, bag, sBagPropertyName});
		}

		private static void CommandLine_AppendSwitchWithInteger(string sSwitchName, Hashtable bag, string sBagPropertyName, CommandLineBuilderExtension commandLine)
		{
			commandLine.GetType().InvokeMember("AppendSwitchWithInteger", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod, null, commandLine, new object[] {sSwitchName, bag, sBagPropertyName});
		}

		private static bool IsLegalIdentifier(string identifier)
		{
			if(identifier.Length == 0)
				return false;
			if(!TokenChar.IsLetter(identifier[0]) && (identifier[0] != '_'))
				return false;
			for(int i = 1; i < identifier.Length; i++)
			{
				char c = identifier[i];
				if(((!TokenChar.IsLetter(c) && !TokenChar.IsDecimalDigit(c)) && (!TokenChar.IsConnecting(c) && !TokenChar.IsCombining(c))) && !TokenChar.IsFormatting(c))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Adds JScript-specific command-line commands not handled by the base class.
		/// </summary>
		/// <param name="commandLine">The command line to popuplate.</param>
		protected void AddMoreCommandLineCommands(CommandLineBuilderExtension commandLine)
		{
			AddMoreCommandLineCommands_TargetType(commandLine);
		}

		/// <summary>
		/// The target type command line commands.
		/// </summary>
		/// <param name="commandLine">The command line to popuplate.</param>
		protected void AddMoreCommandLineCommands_TargetType(CommandLineBuilderExtension commandLine)
		{
			CommandLineAppendWhenTrue(commandLine, "/DLL", "Dll");
			CommandLineAppendWhenTrue(commandLine, "/EXE", "Exe");
		}

		/// <summary>
		/// Carries out an internal <c>CommandLineBuilderExtension.AppendWhenTrue</c> method.
		/// </summary>
		protected void CommandLineAppendWhenTrue(CommandLineBuilderExtension commandLine, string switchName, string parameterName)
		{
			object o = Bag[parameterName];
			if((o != null) && ((bool)o))
				commandLine.AppendSwitch(switchName);
		}

		internal string GetDefineConstantsSwitch(string originalDefineConstants)
		{
			if(originalDefineConstants != null)
			{
				var builder = new StringBuilder();
				foreach(string str in originalDefineConstants.Split(new[] {',', ';', ' '}))
				{
					if(IsLegalIdentifier(str))
					{
						if(builder.Length > 0)
							builder.Append(";");
						builder.Append(str);
					}
					else if(str.Length > 0)
						Log.LogWarning("Invalid define parameter “{0}”.", str);
				}
				if(builder.Length > 0)
					return builder.ToString();
			}
			return null;
		}

		/// <summary>
		/// Taken form the CSC compiler.
		/// </summary>
		private void AddReferencesToCommandLine(CommandLineBuilderExtension commandLine)
		{
			if((base.References != null) && (base.References.Length != 0))
			{
				foreach(ITaskItem item in base.References)
				{
					string metadata = item.GetMetadata("Aliases");
					if((metadata == null) || (metadata.Length == 0))
						commandLine.AppendSwitchIfNotNull("/reference:", item.ItemSpec);
					else
					{
						foreach(string str2 in metadata.Split(new[] {','}))
						{
							string str3 = str2.Trim();
							if(str2.Length != 0)
							{
								if(str3.IndexOfAny(new[] {',', ' ', ';', '"'}) != -1)
									throw new InvalidOperationException(string.Format("The assembly “{0}” alias “{1}” contains invalid characters.", item.ItemSpec, str3));
								if(string.Compare("global", str3, StringComparison.OrdinalIgnoreCase) == 0)
									commandLine.AppendSwitchIfNotNull("/reference:", item.ItemSpec);
								else
									CommandLine_AppendSwitchAliased(commandLine, "/reference:", str3, item.ItemSpec);
							}
						}
					}
				}
			}
		}

		#endregion

		#region Overrides

		///<summary>
		///Fills the specified <see cref="T:Microsoft.Build.Tasks.CommandLineBuilderExtension"></see> with the switches and other information that can go into a response file.
		///</summary>
		///
		///<param name="commandLine">The <see cref="T:Microsoft.Build.Tasks.CommandLineBuilderExtension"></see> to fill with switches and other information.</param>
		protected override void AddResponseFileCommands(CommandLineBuilderExtension commandLine)
		{
			commandLine.AppendSwitchIfNotNull("/platform:", Platform);
			CommandLine_AppendPlusOrMinusSwitch("/fast", Bag, "Fast", commandLine);
			CommandLine_AppendPlusOrMinusSwitch("/autoref", Bag, "Autoref", commandLine);
			commandLine.AppendSwitchIfNotNull("/lib:", AdditionalLibPaths, ",");
			commandLine.AppendSwitchIfNotNull("/win32res:", Win32Resource);
			CommandLine_AppendPlusOrMinusSwitch("/warnaserror", Bag, "TreatWarningsAsErrors", commandLine);
			CommandLine_AppendSwitchWithInteger("/warn:", Bag, "WarningLevel", commandLine);
			CommandLine_AppendPlusOrMinusSwitch("/nostdlib", Bag, "NoStandardLib", commandLine);
			commandLine.AppendSwitchUnquotedIfNotNull("/define:", GetDefineConstantsSwitch(DefineConstants));

			AddReferencesToCommandLine(commandLine);
			base.AddResponseFileCommands(commandLine);

			if(ResponseFiles != null)
			{
				foreach(ITaskItem item in ResponseFiles)
					commandLine.AppendSwitchIfNotNull("@", item.ItemSpec);
			}
		}

		///<summary>
		///Returns the fully qualified path to the executable file.
		///</summary>
		///
		///<returns>
		///The fully qualified path to the executable file.
		///</returns>
		///
		protected override string GenerateFullPathToTool()
		{
			string path = ToolLocationHelper.GetPathToDotNetFrameworkFile(ToolName, TargetDotNetFrameworkVersion.VersionLatest);
			if(path == null)
				Log.LogError("Could not locate the {0} compiler.", ToolName);
			return path;
		}

		///<summary>
		///Gets the name of the executable file to run.
		///</summary>
		///
		///<returns>
		///The name of the executable file to run.
		///</returns>
		///
		protected override string ToolName
		{
			get
			{
				return "jsc.exe";
			}
		}

		#endregion

		#region TokenChar Type

		internal static class TokenChar
		{
			// Methods

			#region Implementation

			internal static bool IsCombining(char c)
			{
				UnicodeCategory unicodeCategory = char.GetUnicodeCategory(c);
				if((unicodeCategory != UnicodeCategory.NonSpacingMark) && (unicodeCategory != UnicodeCategory.SpacingCombiningMark))
					return false;
				return true;
			}

			internal static bool IsConnecting(char c)
			{
				return (char.GetUnicodeCategory(c) == UnicodeCategory.ConnectorPunctuation);
			}

			internal static bool IsDecimalDigit(char c)
			{
				return (char.GetUnicodeCategory(c) == UnicodeCategory.DecimalDigitNumber);
			}

			internal static bool IsFormatting(char c)
			{
				return (char.GetUnicodeCategory(c) == UnicodeCategory.Format);
			}

			internal static bool IsHexDigit(char c)
			{
				if((((c < '0') || (c > '9')) && ((c < 'A') || (c > 'F'))) && ((c < 'a') || (c > 'f')))
					return false;
				return true;
			}

			internal static bool IsLetter(char c)
			{
				UnicodeCategory unicodeCategory = char.GetUnicodeCategory(c);
				if((((unicodeCategory != UnicodeCategory.UppercaseLetter) && (unicodeCategory != UnicodeCategory.LowercaseLetter)) && ((unicodeCategory != UnicodeCategory.TitlecaseLetter) && (unicodeCategory != UnicodeCategory.ModifierLetter))) && ((unicodeCategory != UnicodeCategory.OtherLetter) && (unicodeCategory != UnicodeCategory.LetterNumber)))
					return false;
				return true;
			}

			internal static bool IsNewLine(char c)
			{
				if(((c != '\r') && (c != '\n')) && (c != '\u2028'))
					return (c == '\u2029');
				return true;
			}

			internal static bool IsOctalDigit(char c)
			{
				return ((c >= '0') && (c <= '7'));
			}

			#endregion
		}

		#endregion
	}
}
