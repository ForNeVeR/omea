using System.Windows.Controls;
using System.Windows.Documents;

namespace JetBrains.Omea.OpenAPI
{
	/// <summary>
	/// Format for the plugin description rich content.
	/// </summary>
	public enum PluginDescriptionFormat // TODO: make something more generic, like "rich-content-format"; probably will be a redirection to a more generic enum, but do not change values
	{
		/// <summary>
		/// Plain text without any formatting metadata.
		/// </summary>
		PlainText = 0,
		/// <summary>
		/// Mirosoft RichTextFormat.
		/// </summary>
		/// <example><code>
		/// {\rtf1\ansi………
		/// </code></example>
		Rtf = 1,
		/// <summary>
		/// <para>Inline content in XAML format.</para>
		/// <para>Suppose that your content will be wrapped into a <see cref="Paragraph"/> or a <see cref="TextBlock"/> element and parsed with a XAML reader.</para>
		/// <para>Basically, this can be a string with formatting elements inside.</para>
		/// </summary>
		/// <example><code>Hello, &lt;Bold&gt;World&lt;/Bold&gt;!</code></example>
		XamlInline = 2,
		/// <summary>
		/// <para>A Pack Uri to a valid Flow Document that represents the content.</para>
		/// <para>If the document located in the plugin assembly resources, the Uri might be relative.</para>
		/// </summary>
		XamlFlowDocumentPackUri = 3
	}
}