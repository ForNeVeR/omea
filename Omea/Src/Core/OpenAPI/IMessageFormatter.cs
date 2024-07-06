// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only


namespace JetBrains.Omea.OpenAPI
{
    /// <summary>
    /// Allows a plugin to return custom auto-preview text for a resource.
    /// </summary>
    /// <since>2.0</since>
    public interface IPreviewTextProvider
    {
        /// <summary>
        /// Returns the auto-preview text for the specified resource.
        /// </summary>
        /// <param name="res">The resource for which the text is requested.</param>
        /// <param name="lines">The number of lines of preview text to return. Can be used
        /// as a guide to restrict the amount of text extracted from the document.</param>
        /// <returns>The auto-preview text.</returns>
        string GetPreviewText( IResource res, int lines );
    }

    /// <summary>
    /// Provides services for pretty-printing (HTML formatting) and quoting of plain-text messages.
    /// </summary>
    public interface IMessageFormatter
    {
        /// <summary>
        /// Retrieves and pretty-prints the body of the specified resource
        /// using default font and attributes.
        /// </summary>
        /// <param name="resource">The resource for which the body is pretty-printed.</param>
        /// <param name="bodyProp">The ID of the property holding the resource body.</param>
        /// <param name="replyLink">The ID of the property linking the resource to its replies.</param>
        /// <returns>The string containing the HTML-formatted message.</returns>
        string GetFormattedBody( IResource resource, int bodyProp, int replyLink );

        /// <summary>
        /// Retrieves and pretty-prints the body of the specified resource
        /// using given font and attributes.
        /// </summary>
        /// <param name="resource">The resource for which the body is pretty-printed.</param>
        /// <param name="bodyProp">The ID of the property holding the resource body.</param>
        /// <param name="replyLink">The ID of the property linking the resource to its replies.</param>
        /// <param name="fontFace">Name of a font face (family).</param>
        /// <param name="fontSize">Size of a font.</param>
        /// <returns>The string containing the HTML-formatted message.</returns>
        /// <since>2.1</since>
        string GetFormattedBody( IResource resource, int bodyProp, int replyLink, string fontFace, int fontSize );

        /// <summary>
        /// Pretty-prints the specified string which is the body of the specified
        /// resource using default font and attributes.
        /// </summary>
        /// <param name="resource">The resource for which the body is pretty-printed.</param>
        /// <param name="body">The text of the resource body.</param>
        /// <param name="replyToBody">The body of the resource to which the specified resource
        /// is a reply, or null if the specified resource is not a reply.</param>
        /// <returns>The string containing the HTML-formatted message.</returns>
        string GetFormattedBody( IResource resource, string body, string replyToBody );

        /// <summary>
        /// Pretty-prints the specified string which is the body of the specified
        /// resource using given font and attributes.
        /// </summary>
        /// <param name="resource">The resource for which the body is pretty-printed.</param>
        /// <param name="body">The text of the resource body.</param>
        /// <param name="replyToBody">The body of the resource to which the specified resource
        /// is a reply, or null if the specified resource is not a reply.</param>
        /// <param name="fontFace">Name of a font face (family).</param>
        /// <param name="fontSize">Size of a font.</param>
        /// <returns>The string containing the HTML-formatted message.</returns>
        /// <since>2.1</since>
        string GetFormattedBody( IResource resource, string body, string replyToBody,
                                 string fontFace, int fontSize );

        /// <summary>
        /// Retrieves and pretty-prints the body of the specified resource,
        /// using default font and attributes and maintaining a list of offsets in the text.
        /// </summary>
        /// <param name="resource">The resource for which the body is pretty-printed.</param>
        /// <param name="bodyProp">The ID of the property holding the resource body.</param>
        /// <param name="replyLink">The ID of the property linking the resource to its replies.</param>
        /// <param name="maintainedOffsets">An array of offsets in the text that should be updated accordingly
        /// to the formatting applied to the content so that if the offset points to a specific word
        /// before formatting, it would point to the same word after formatting as well. May be <c>null</c>.</param>
        /// <returns>The string containing the HTML-formatted message.</returns>
        /// <since>2.0</since>
        string GetFormattedBody( IResource resource, int bodyProp, int replyLink, ref WordPtr[] maintainedOffsets );

        /// <summary>
        /// Retrieves and pretty-prints the body of the specified resource,
        /// using given font and attributes and maintaining a list of offsets in the text.
        /// </summary>
        /// <param name="resource">The resource for which the body is pretty-printed.</param>
        /// <param name="bodyProp">The ID of the property holding the resource body.</param>
        /// <param name="replyLink">The ID of the property linking the resource to its replies.</param>
        /// <param name="maintainedOffsets">An array of offsets in the text that should be updated accordingly
        /// to the formatting applied to the content so that if the offset points to a specific word
        /// before formatting, it would point to the same word after formatting as well. May be <c>null</c>.</param>
        /// <param name="fontFace">Name of a font face (family).</param>
        /// <param name="fontSize">Size of a font.</param>
        /// <returns>The string containing the HTML-formatted message.</returns>
        /// <since>2.1</since>
        string GetFormattedBody( IResource resource, int bodyProp, int replyLink,
                                 ref WordPtr[] maintainedOffsets, string fontFace, int fontSize );

        /// <summary>
        /// Pretty-prints the specified string which is the body of the specified resource,
        /// using default font and attributes and maintaining a list of offsets in the text.
        /// </summary>
        /// <param name="resource">The resource for which the body is pretty-printed.</param>
        /// <param name="body">The text of the resource body.</param>
        /// <param name="replyToBody">The body of the resource to which the specified resource
        /// is a reply, or null if the specified resource is not a reply.</param>
        /// <param name="maintainedOffsets">An array of offsets in the text that should be updated accordingly
        /// to the formatting applied to the content so that if the offset points to a specific word
        /// before formatting, it would point to the same word after formatting as well. May be <c>null</c>.</param>
        /// <returns>The string containing the HTML-formatted message.</returns>
        /// <since>2.0</since>
        string GetFormattedBody( IResource resource, string body, string replyToBody, ref WordPtr[] maintainedOffsets );

        /// <summary>
        /// Pretty-prints the specified string which is the body of the specified resource,
        /// using given font and attributes and maintaining a list of offsets in the text.
        /// </summary>
        /// <param name="resource">The resource for which the body is pretty-printed.</param>
        /// <param name="body">The text of the resource body.</param>
        /// <param name="replyToBody">The body of the resource to which the specified resource
        /// is a reply, or null if the specified resource is not a reply.</param>
        /// <param name="offsets">An array of offsets in the text that should be updated accordingly
        /// to the formatting applied to the content so that if the offset points to a specific word
        /// before formatting, it would point to the same word after formatting as well. May be <c>null</c>.</param>
        /// <param name="fontFace">Name of a font face (family).</param>
        /// <param name="fontSize">Size of a font.</param>
        /// <returns>The string containing the HTML-formatted message.</returns>
        /// <since>2.1</since>
        string GetFormattedBody( IResource resource, string body, string replyToBody,
                                 ref WordPtr[] offsets, string fontFace, int fontSize );

        /// <since>2.3</since>
        string GetFormattedHtmlBody( IResource res, string body, ref WordPtr[] offsets );

        /// <summary>
        /// Retrieves and quotes the body of the specified resource with default settings.
        /// </summary>
        /// <param name="resource">The resource for which the body is quoted.</param>
        /// <param name="bodyProp">The ID of the property holding the resource body.</param>
        /// <returns>The string containing the quoted resource body.</returns>
        string QuoteMessage( IResource resource, int bodyProp );

        /// <summary>
        /// Quotes the specified string which is the body of the specified resource with default
        /// settings.
        /// </summary>
        /// <param name="resource">The resource for which the body is quoted.</param>
        /// <param name="body">The text of the resource body.</param>
        /// <returns>The string containing the quoted resource body.</returns>
        string QuoteMessage( IResource resource, string body );

        /// <summary>
        /// Retrieves and quotes the body of the specified resource with the specified settings.
        /// </summary>
        /// <param name="resource">The resource for which the body is quoted.</param>
        /// <param name="bodyProp">The ID of the property holding the resource body.</param>
        /// <param name="settings">The settings for quoting.</param>
        /// <returns>The string containing the quoted resource body.</returns>
        /// <since>2.0</since>
        string QuoteMessage( IResource resource, int bodyProp, QuoteSettings settings );

        /// <summary>
        /// Quotes the specified string which is the body of the specified resource
        /// with the specified settings
        /// </summary>
        /// <param name="resource">The resource for which the body is quoted.</param>
        /// <param name="body">The text of the resource body.</param>
        /// <param name="settings">The settings for quoting.</param>
        /// <returns>The string containing the quoted resource body.</returns>
        /// <since>2.0</since>
        string QuoteMessage( IResource resource, string body, QuoteSettings settings );

        /// <summary>
        /// Returns the preview text of the specified resource (beginning of its full text with no
        /// formatting or spacing).
        /// </summary>
        /// <param name="res">The resource to get the text for.</param>
        /// <param name="lines">The number of lines of preview text to return.</param>
        /// <returns>The preview text, or an empty string if none is available.</returns>
        /// <since>2.0</since>
        string GetPreviewText( IResource res, int lines );

        /// <summary>
        /// Registers a custom preview text provider for the specified resource type.
        /// </summary>
        /// <param name="resourceType">The resource type for which the provider is registered.</param>
        /// <param name="provider">The provider implementation.</param>
        /// <since>2.0</since>
        void RegisterPreviewTextProvider( string resourceType, IPreviewTextProvider provider );

        /// <since>2.3</since>
        string DualMediaSubjectStyle {  get;  }

        /// <since>2.3</since>
        string StandardStyledHeader( string subject );
    }

    /// <summary>
    /// Settings for formatting a quoted message.
    /// </summary>
    /// <since>2.0</since>
    public class QuoteSettings
    {
        private bool    _prefixInitials;
        private bool    _greetingInReplies;
        private string  _greetingString;
        private SignaturePosition _signatureInReplies;
        private string  _signature;
        private bool    _useSignature;
        private int     _quoteMargin = 72;

        private const string  _cstrDefaultGreeting = "Hello";

        /// <summary>
        /// Gets or sets the value indicating whether each line of quoting is prefixed with
        /// the sender's initials.
        /// </summary>
        public bool PrefixInitials
        {
            get { return _prefixInitials; }
            set { _prefixInitials = value; }
        }

        /// <summary>
        /// Gets or sets the value indicating whether a greeting is included in replies.
        /// </summary>
        public bool GreetingInReplies
        {
            get { return _greetingInReplies; }
            set { _greetingInReplies = value; }
        }

        /// <summary>
        /// Gets or sets the greeting string
        /// </summary>
        public string GreetingString
        {
            get { return _greetingString; }
            set { _greetingString = value; }
        }

        /// <summary>
        /// Gets or sets the value indicating whether a signature is included in outgoing messages.
        /// </summary>
        public bool UseSignature
        {
            get { return _useSignature; }
            set { _useSignature = value; }
        }

        /// <summary>
        /// Gets or sets the position of the signature in reply messages.
        /// </summary>
        public SignaturePosition SignatureInReplies
        {
            get { return _signatureInReplies; }
            set { _signatureInReplies = value; }
        }

        /// <summary>
        /// Gets or sets the text of the signature included in a message.
        /// </summary>
        public string Signature
        {
            get { return _signature; }
            set { _signature = value; }
        }

        /// <summary>
        /// Gets or sets the value indicating the line length at which quoting is wrapped.
        /// </summary>
        public int QuoteMargin
        {
            get { return _quoteMargin; }
            set { _quoteMargin = value; }
        }

        /// <summary>
        /// Returns a copy of the default quote settings loaded from the setting store.
        /// </summary>
        public static QuoteSettings Default
        {
            get
            {
                QuoteSettings result = new QuoteSettings();
                result.PrefixInitials = Core.SettingStore.ReadBool( "MailFormat", "PrefixInitials", false );
                result.GreetingInReplies = Core.SettingStore.ReadBool( "MailFormat", "GreetingInReplies", true );
                result.GreetingString = Core.SettingStore.ReadString( "MailFormat", "GreetingString", _cstrDefaultGreeting );
                result.UseSignature = Core.SettingStore.ReadBool( "MailFormat", "UseSignature", false );
                result.SignatureInReplies = (SignaturePosition) Core.SettingStore.ReadInt( "MailFormat", "SignatureInReplies", 1 );
                result.Signature = Core.SettingStore.ReadString( "MailFormat", "Signature", "" );
                result.QuoteMargin = 72;
                return result;
            }
        }
    }

    /// <summary>
    /// Defines the possible position of a signature in a quoted message.
    /// </summary>
    /// <since>2.0</since>
    public enum SignaturePosition
    {
        /// <summary>
        /// The signature is not inserted.
        /// </summary>
        None,

        /// <summary>
        /// The signature is inserted before the quoted section.
        /// </summary>
        BeforeQuote,

        /// <summary>
        /// The signature is inserted after the quoted section.
        /// </summary>
        AfterQuote
    };
}
