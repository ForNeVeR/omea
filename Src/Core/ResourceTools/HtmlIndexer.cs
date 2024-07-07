// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.IO;
using JetBrains.Omea.Base;
using JetBrains.Omea.HTML;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceTools
{
	/// <summary>
	/// Services for indexing the HTML text.
	/// </summary>
	public class HtmlIndexer
	{
		/// <summary>
		/// Performs indexing of an HTML text for the specified resource, providing that the offsets stored in the text index correspond to the offsets in the source HTML representation.
		/// </summary>
        /// <param name="res">Resource for which the indexing is being performed.</param>
        /// <param name="html">Html text to be indexed.</param>
		/// <param name="consumer">Consumer that would receive the tokens for indexing.</param>
		/// <param name="section">Document section to which the content being indexed belongs, see <see cref="DocumentSection"/> for some possible values. Passing <c>null</c> impplies on the <see cref="DocumentSection.BodySection"/>.</param>
		/// <remarks>
		/// <para>The indexer extracts plaintext contents from the HTML data and passes to the consumer, ensuring that offsets in the indexed content correspond to the offsets in the HTML text.</para>
		/// <para>If you have indexed other sections before, you should restart the offsets counting by calling the <see cref="IResourceTextConsumer.RestartOffsetCounting"/> manually. This function does not assume that offsets should be reset.</para>
		/// </remarks>
        public static void IndexHtml( IResource res, string html, IResourceTextConsumer consumer, string section )
		{
            IndexHtml( res.Id, html, consumer, section );
		}

        /// <summary>
        /// Performs indexing of an HTML text for the specified resource, providing that the offsets stored in the text index correspond to the offsets in the source HTML representation.
        /// </summary>
        /// <param name="resourceId">ID of the resource for which the indexing is being performed.</param>
        /// <param name="html">Html text to be indexed.</param>
        /// <param name="consumer">Consumer that would receive the tokens for indexing.</param>
        /// <param name="section">Document section to which the content being indexed belongs, see <see cref="DocumentSection"/> for some possible values. Passing <c>null</c> impplies on the <see cref="DocumentSection.BodySection"/>.</param>
        /// <remarks>
        /// <para>The indexer extracts plaintext contents from the HTML data and passes to the consumer, ensuring that offsets in the indexed content correspond to the offsets in the HTML text.</para>
        /// <para>If you have indexed other sections before, you should restart the offsets counting by calling the <see cref="IResourceTextConsumer.RestartOffsetCounting"/> manually. This function does not assume that offsets should be reset.</para>
        /// </remarks>
        public static void IndexHtml(int resourceId, string html, IResourceTextConsumer consumer, string section)
        {
            if( html == null )
                throw new ArgumentNullException( "html", "HTML body must not be null." );

            int nPrependedChars = 0; // Number of characters added to the content by this method

            // Check the section
            if( section == null )
                section = DocumentSection.BodySection;

            // Add a body tag if it's absent, because it's needed for the HTML parser to mark content as body part content
            if( Utils.IndexOf( html, "<html>", true ) < 0 || Utils.IndexOf( html, "<body", true ) < 0 ) // Case-insensitive check
            {
                html = "<html><body>" + html + "</body></html>"; // Add this stuff. The problem is that we cannot process correctly the HTML fragments that are not equipped with a <body/> tag
                nPrependedChars += "<html><body>".Length;
            }

            using( HTMLParser parser = new HTMLParser( new StringReader( html ) ) )
            {
                // Breaking fragments into words provides that for each word the offset is guaranteed to be valid
                // Otherwise, after the first entity-reference within the block it would have been shifted from the proper value
                parser.BreakWords = true;

                IResourceTextConsumer consumer2 = consumer as IResourceTextConsumer;
                Debug.Assert( consumer2 != null ); // We should succeed (more or less) even if the consumer passed in does not implement the needed interface
                int nBeforeHtmlWord; // Positioned before the current HTML word in the HTML stream
                int nAfterHtmlWord = nPrependedChars; // Positioned after the current HTML word in the HTML stream. Seed by positioning after the prepended content
                int nWordDifference = 0; // Difference in the length of the HTML and text representation of the current word, given by nAfterHtmlWord - nBeforeHtmlWord - fragment.Length
                string fragment;
                while( !parser.Finished )
                {
                    fragment = parser.ReadNextFragment( out nBeforeHtmlWord );
                    if( fragment.Length > 0 ) // Zero-length fragments are completely ignored
                    {
                        // Adjust the offset
                        if
                            (
                            (consumer2 != null)	// The consumer is capable of increasing the offset
                            &&
                            ( // Increment offsets for indexing and context extraction only
                            (consumer.Purpose == TextRequestPurpose.Indexing)
                            || (consumer.Purpose == TextRequestPurpose.ContextExtraction)
                            )
                            && ( nBeforeHtmlWord - nAfterHtmlWord + nWordDifference != 0)	// Prevent from making dummy calls
                            )
                            consumer2.IncrementOffset( nBeforeHtmlWord - nAfterHtmlWord + nWordDifference ); // For nBeforeHtmlWord, we use the current value (for the current word), nAfterHtmlWord and nWordDifference are taken from the previous step and provide for calculating the introduced difference between the text and HTML representations caused by both entities substitution in the word (nWordDifference) and HTML tags skipped in between (nBeforeHtmlWord - nAfterHtmlWord)

                        // Process next word
                        consumer.AddDocumentFragment( resourceId, fragment, section );

                        // Adjust pointers
                        nAfterHtmlWord = parser.Position;
                        nWordDifference = nAfterHtmlWord - nBeforeHtmlWord - fragment.Length;
                    }
                }
            }
        }
	}
}
