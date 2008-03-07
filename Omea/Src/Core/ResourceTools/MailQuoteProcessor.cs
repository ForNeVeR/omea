/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Text;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.MailParser
{
    /**
     * Performs smart quoting of a formatted message.
     */

	internal class MailQuoteProcessor
	{
        public string Quote( MailBodyParser parser, IResource origMail, QuoteSettings quoteSettings )
        {
            StringBuilder quoteBuilder = StringBuilderPool.Alloc();
            try 
            {
                string initials = "";

                if ( origMail != null )
                {
                    IResourceList senders = origMail.GetLinksOfType( "Contact", "From" );
                    if ( senders.Count > 0 )
                    {
                        IResource sender = senders [0];
                    
                        string name = sender.GetPropText( "FirstName" );
                        if ( name.Length == 0 )
                        {
                            name = sender.DisplayName;
                        }
                        if ( quoteSettings.PrefixInitials )
                        {
                            initials = GetInitials( sender );
                        }
                        if ( quoteSettings.GreetingInReplies )
                        {
                            quoteBuilder.Append( quoteSettings.GreetingString + " " + name + ",\r\n\r\n" );
                        }
                    }
                }

                if ( quoteSettings.UseSignature && quoteSettings.SignatureInReplies == SignaturePosition.BeforeQuote )
                {
                    quoteBuilder.Append( "\r\n" );
                    quoteBuilder.Append( quoteSettings.Signature );
                    quoteBuilder.Append( "\r\n\r\n" );
                }

                for( int i=0; i<parser.ParagraphCount; i++ )
                {
                    MailBodyParser.Paragraph para = parser.GetParagraph( i );
                    QuoteParagraph( quoteBuilder, initials, para, quoteSettings );
                }

                if ( quoteSettings.UseSignature && quoteSettings.SignatureInReplies == SignaturePosition.AfterQuote )
                {
                    quoteBuilder.Append( quoteSettings.Signature );
                }

                return quoteBuilder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( quoteBuilder );
            }
        }

        private string GetInitials( IResource sender )
        {
            string result = "";
            string firstName = sender.GetPropText( "FirstName" );
            if ( firstName.Length > 0 )
            {
                result += firstName [0];
            }
            string lastName = sender.GetPropText( "LastName" );
            if ( lastName.Length > 0 )
            {
                result += lastName [0];
            }
            return result;
        }

        private void QuoteParagraph( StringBuilder quoteBuilder, string initials, 
            MailBodyParser.Paragraph para, QuoteSettings settings )
        {
            if ( para.Type != ParagraphType.Sig )
            {
                string newQuotePrefix;
                if ( para.OutlookQuote )
                {
                    newQuotePrefix = ">> ";
                }
                else if ( para.QuoteLevel == 0 )
                {
                    newQuotePrefix = initials + "> ";
                }
                else
                {
                    newQuotePrefix = para.QuotePrefix + new string( '>', para.QuoteLevel + 1 ) + " ";
                }
                QuoteParagraphWithPrefix( quoteBuilder, newQuotePrefix, para.Text, settings );
                if ( para.Type == ParagraphType.Plain )
                {
                    quoteBuilder.Append( newQuotePrefix );
                    quoteBuilder.Append( "\r\n" );
                }
            }
        }

        private void QuoteParagraphWithPrefix( StringBuilder quoteBuilder, string prefix, string text,
            QuoteSettings settings )
        {
            int lineLength = settings.QuoteMargin - prefix.Length;
            int pos = 0;
            while( pos < text.Length )
            {
                int newPos = pos + lineLength;
                if ( newPos > text.Length )
                {
                    newPos = text.Length;
                }
                else if ( newPos < text.Length )
                {
                    while( newPos > pos && !Char.IsWhiteSpace( text, newPos ) )
                    {
                        newPos--;
                    }
                    if ( newPos == pos )  // no word break found, split word
                    {
                        newPos = pos + lineLength;
                    }
                }
                
                quoteBuilder.Append( prefix );
                quoteBuilder.Append( text.Substring( pos, newPos-pos ).Trim() );
                quoteBuilder.Append( "\r\n" );

                while( newPos < text.Length && Char.IsWhiteSpace( text, newPos ) )
                {
                    newPos++;
                }
                pos = newPos;
            }
        }
	}
}
