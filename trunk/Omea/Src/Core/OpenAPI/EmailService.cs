/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

namespace JetBrains.Omea.OpenAPI
{
	/// <summary>
	/// Specifies possible formats for emails created through <see cref="IEmailService.CreateEmail"/>.
	/// </summary>
    public enum EmailBodyFormat 
    { 
        /// <summary>
        /// The body is plain text.
        /// </summary>
        PlainText, 
        
        /// <summary>
        /// The body is HTML text.
        /// </summary>
        Html 
    };

    /// <summary>
    /// Specifies the name and address information of a recipient for an e-mail message.
    /// </summary>
    /// <since>2.0</since>
    public struct EmailRecipient
    {
        /// <summary>
        /// The name of the recipient.
        /// </summary>
        public string Name;
        
        /// <summary>
        /// The e-mail address of the recipient.
        /// </summary>
        public string EmailAddress;

        /// <summary>
        /// Creates a new recipient with the specified name and e-mail address.
        /// </summary>
        /// <param name="name">The name of the recipient.</param>
        /// <param name="emailAddress">The e-mail address of the recipient.</param>
        public EmailRecipient( string name, string emailAddress )
        {
            Name = name;
            EmailAddress = emailAddress;
        }
    }
    
    /// <summary>
    /// Allows plugins to create e-mail messages in the system e-mail client.
    /// </summary>
    public interface IEmailService
	{
        /// <summary>
        /// Creates an e-mail message in the system e-mail client.
        /// </summary>
        /// <param name="subject">The subject of the message.</param>
        /// <param name="body">The body of the message, in plain text or HTML format.</param>
        /// <param name="bodyFormat">The format of the message body.</param>
        /// <param name="recipients">A list of resources of types Contact or EmailAccount which
        /// specifies the recipients of the message.</param>
        /// <param name="attachments">A list of names of files to be attached to the message.</param>
        /// <param name="addSignature">If true, the user's default signature is appended to the message text.</param>
        void CreateEmail( string subject, string body, EmailBodyFormat bodyFormat, 
            IResourceList recipients, string[] attachments, bool addSignature );

        /// <summary>
        /// Creates an e-mail message in the system e-mail client.
        /// </summary>
        /// <param name="subject">The subject of the message.</param>
        /// <param name="body">The body of the message, in plain text or HTML format.</param>
        /// <param name="bodyFormat">The format of the message body.</param>
        /// <param name="recipients">A list of recipients of the message.</param>
        /// <param name="attachments">A list of names of files to be attached to the message.</param>
        /// <param name="addSignature">If true, the user's default signature is appended to the message text.</param>
        /// <since>2.0</since>
        void CreateEmail( string subject, string body, EmailBodyFormat bodyFormat, 
            EmailRecipient[] recipients, string[] attachments, bool addSignature );
    }
}
