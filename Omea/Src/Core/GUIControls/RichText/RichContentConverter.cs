// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Resources;
using System.Xml;

using System35;

using JetBrains.Annotations;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Util;

namespace GUIControls.RichText
{
	/// <summary>
	/// Converts rich content between various formats.
	/// </summary>
	public static class RichContentConverter
	{
		#region Operations

		/// <summary>
		/// <para>Returns a document that contains a presentation of the exception message.</para>
		/// <para>Useful for wrapping the presentation methods execution.</para>
		/// </summary>
		[NotNull]
		public static FlowDocument DocumentFromException([NotNull] Exception ex)
		{
			if(ex == null)
				throw new ArgumentNullException("ex");

			// TODO: monospaced font?
			return new FlowDocument(new Paragraph(new Run(ex.Message) {Foreground = Brushes.Red}));
		}

		/// <summary>
		/// <para>Parses inline XAML and returns a flow document with that content inside.</para>
		/// <para>�Inline� means inline XAML elements, in the same syntax as inside a <see cref="TextBlock"/>.</para>
		/// <para>Suppose that your content will be wrapped into a <see cref="Paragraph"/> or a <see cref="TextBlock"/> element and parsed with a XAML reader.</para>
		/// <para>Basically, this can be a string with formatting elements inside.</para>
		/// </summary>
		/// <example><code>Hello, &lt;Bold&gt;World&lt;/Bold&gt;!</code></example>
		/// <param name="xaml">XAML string.</param>
		[NotNull]
		public static FlowDocument DocumentFromInlineXaml([NotNull] string xaml)
		{
			if(xaml == null)
				throw new ArgumentNullException("xaml");

			if(xaml.IsEmpty())
				return new FlowDocument();

			// Get the XMLNS to use with the fragment
			/*
			string xmlns = typeof(Paragraph).Assembly.GetCustomAttributes(typeof(XmlnsDefinitionAttribute), true).Cast<XmlnsDefinitionAttribute>().First(attr => attr.ClrNamespace == typeof(Paragraph).Namespace).XmlNamespace; // TODO: use LINQ
*/
			string xmlns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

			// Wrap the fragment into a top-level element to make some valid XML from it
			string content = string.Format("<Paragraph xmlns=\"{1}\">{0}</Paragraph>", xaml, xmlns);

			// Parse
			return new FlowDocument((Paragraph)XamlReader.Load(XmlReader.Create(new StringReader(content))));
		}

		/// <summary>
		/// Creates a flow document that contains the specified unformatted text.
		/// </summary>
		/// <param name="text">The text content.</param>
		[NotNull]
		public static FlowDocument DocumentFromPlainText([NotNull] string text)
		{
			if(text == null)
				throw new ArgumentNullException("text");

			if(text.IsEmpty())
				return new FlowDocument();

			return new FlowDocument(new Paragraph(new Run(text)));

			// TODO: make paragraphs out of text lines?..
		}

		/// <summary>
		/// Tries to load a flow document from a XAML Flow Document resource.
		/// </summary>
		/// <param name="sUri">Either an absolute or a relative URI. Relative URIs will be resolved against the <paramref name="assembly"/>.</param>
		/// <param name="assembly">The assembly for handling relative URIs.</param>
		[NotNull]
		public static FlowDocument DocumentFromResource([NotNull] string sUri, [NotNull] Assembly assembly)
		{
			if(sUri.IsEmpty())
				throw new ArgumentNullException("sUri");
			if(assembly == null)
				throw new ArgumentNullException("assembly");

			// Adapt the URI to support relative URIs
			Uri uri = Uri.IsWellFormedUriString(sUri, UriKind.Absolute) ? new Uri(sUri) : Utils.MakeResourceUri(sUri, assembly);

			// Locate the resource by its URI
			StreamResourceInfo stream = Application.GetResourceStream(uri);
			if(stream == null)
				throw new InvalidOperationException(string.Format("Could not load the document from {0}. The resource could not be found.", sUri.QuoteIfNeeded()));

			// Try loading the resource (XML and XAML exceptions might be thrown)
			object resdata = XamlReader.Load(stream.Stream);

			// Check the type of the loaded data
			var retval = resdata as FlowDocument;
			if(retval == null)
				throw new InvalidOperationException(string.Format("The resource located at {0} is not a flow document.", uri));

			return retval;
		}

		/// <summary>
		/// Converts the RTF text into a flow document.
		/// </summary>
		[NotNull]
		public static FlowDocument DocumentFromRtf([NotNull] string rtf)
		{
			if(rtf == null)
				throw new ArgumentNullException("rtf");

			if(rtf.IsEmpty())
				return new FlowDocument();

			Run run;
			using(var stream = new MemoryStream())
			{
				// Serialize (with all the encoding-dependent BOMs)
				var sw = new StreamWriter(stream, Encoding.UTF8);
				sw.Write(rtf);

				// Deserialize as XAML
				run = new Run();
				new TextRange(run.ContentStart, run.ContentStart).Load(stream, DataFormats.Rtf);
			}

			return new FlowDocument(new Paragraph(run));
		}

		/// <summary>
		/// Invokes <paramref name="funcGetDocument"/> to format the document, and returns that document. In case <paramref name="funcGetDocument"/> fails with an exception, returns that exception message as a document.
		/// </summary>
		/// <param name="bReportBackgroundException">Whether to report the exception as a background Omea exception.</param>
		/// <param name="funcGetDocument">The function that produces the document.</param>
		[NotNull]
		public static FlowDocument DocumentOrException(bool bReportBackgroundException, [NotNull] Func<FlowDocument> funcGetDocument)
		{
			if(funcGetDocument == null)
				throw new ArgumentNullException("funcGetDocument");

			try
			{
				return funcGetDocument();
			}
			catch(Exception ex)
			{
				if(bReportBackgroundException)
					Core.ReportBackgroundException(ex);
				return DocumentFromException(ex);
			}
		}

		#endregion
	}
}
