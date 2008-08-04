using System;
using System.Threading;

using JetBrains.Annotations;

namespace System.Runtime.CompilerServices
{
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly)]
	public sealed class ExtensionAttribute : Attribute
	{
	} // TODO: move to netfx35.dll
}

namespace JetBrains.Util
{
	/// <summary>
	/// String class extension methods.
	/// </summary>
	public static class StringEx
	{
		#region Operations

		/// <summary>
		/// Works like <see cref="string.Format(string,object[])"/>, but surrounds the space-containing arguments with quotes.
		/// </summary>
		[NotNull]
		[StringFormatMethod("format")]
		public static string FormatQuoted([NotNull] string format, [NotNull] params object[] args)
		{
			if(format == null)
				throw new ArgumentNullException("format");
			if(args == null)
				throw new ArgumentNullException("args");

			for(int a = 0; a < args.Length; a++)
			{
				if(args[a] == null)
				{
					args[a] = "<NULL>";
					continue;
				}
				string s = args[a] is string ? (string)args[a] : args[a].ToString();
				args[a] = s.QuoteIfNeeded();
			}

			return string.Format(format, args);
		}

		/// <summary>
		/// Tell if the string is either <c>Null</c> or an empty string.
		/// </summary>
		public static bool IsEmpty([CanBeNull] this string s)
		{
			return string.IsNullOrEmpty(s);
		}

		/// <summary>
		/// If the string contains spaces, surrounds it with quotes.
		/// </summary>
		[NotNull]
		public static string QuoteIfNeeded([NotNull] this string s)
		{
			if(s == null)
				throw new ArgumentNullException("s");

			return s.Contains(" ") ? '“' + s + '”' : s;
		}

		/// <summary>
		/// Formats the identity of a thread.
		/// </summary>
		public static string ToThreadString([CanBeNull] this Thread thread)
		{
			if(thread == null)
				return "<NULL>";

			return FormatQuoted("{0}:{1}", thread.Name, thread.ManagedThreadId);
		}

		#endregion
	}
}