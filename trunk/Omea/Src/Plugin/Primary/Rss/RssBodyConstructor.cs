/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.IO;
using System.Text;
using System.Web;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
    internal abstract class BodyWriter
    {
        public abstract void AppendText( string text );
    }

    internal class StringBuilderDecor : BodyWriter
    {
        private readonly StringBuilder _htmlText;
        public StringBuilderDecor( string htmlText )
        {
            _htmlText = new StringBuilder( htmlText );
        }
        public override void AppendText( string text )
        {
            _htmlText.Append( text );
        }
        public override string ToString()
        {
            return _htmlText.ToString();
        }
    }
    internal class TextWriterDecor : BodyWriter
    {
        private readonly TextWriter _writer;
        public TextWriterDecor( TextWriter writer )
        {
            Guard.NullArgument( writer, "writer" );
            _writer = writer;
        }
        public override void AppendText( string text )
        {
            _writer.Write( text );
        }
    }

    internal abstract class RssBodyConstructor
    {
        public static void ConstructSummary( IResource rssItem, string summaryStyle,
                                             string content, BodyWriter decor )
        {
            string summary = rssItem.GetPropText( Props.Summary );
            bool   showSummary = (summary.Length > 0) && (summary.Length != content.Length);
            if( showSummary )
            {
                decor.AppendText( "<style type=\"text/css\">" + summaryStyle + "</style>");
                decor.AppendText( "<div class=\"summary\">" );
                decor.AppendText( summary );
                decor.AppendText( "</div>" );
            }
        }

        public static void AppendLink( IResource item, BodyWriter decor )
        {
            AppendLink( item, decor, null );
        }
        public static void AppendLink( IResource item, BodyWriter decor, string alias )
        {
            if ( item.HasProp( Props.Link ) )
            {
                bool    visibleAlias = !string.IsNullOrEmpty( alias );
                string  link = HttpUtility.HtmlEncode( item.GetStringProp( Props.Link ) );

                decor.AppendText( "<a href=\"" );
                decor.AppendText( link );
                decor.AppendText( "\"" );

                if( visibleAlias )
                {
                    decor.AppendText( " title=\"" );
                    decor.AppendText( link );
                    decor.AppendText( "\"" );
                }
                decor.AppendText( ">" );
                decor.AppendText( visibleAlias ? HttpUtility.HtmlEncode( alias ) : link );
                decor.AppendText( "</a>" );
            }
        }
        public static void AppendSourceTag( IResource item, BodyWriter decor )
        {
            if ( item.HasProp( Props.RSSSourceTag ) )
            {
                decor.AppendText( "<p class=\"Origin\"><a href=\"" );
                string sourceUrl = HttpUtility.HtmlEncode( item.GetStringProp( Props.RSSSourceTagUrl ) );
                decor.AppendText( sourceUrl );
                decor.AppendText( "\">Source: " );
                decor.AppendText( item.GetPropText( Props.RSSSourceTag ) );
				decor.AppendText( "</a></p>" );
			}
        }
        public static void AppendCommentsTag( IResource item, BodyWriter decor )
        {
            if ( item.HasProp( Props.CommentURL ) )
            {
                decor.AppendText( "<p><a href=\"") ;
                decor.AppendText( item.GetStringProp( Props.CommentURL ) );
                decor.AppendText( "\">Comments" );
                if ( item.HasProp( Props.CommentCount ) )
                {
                    decor.AppendText( " (" );
                    decor.AppendText( item.GetProp( Props.CommentCount ).ToString() );
                    decor.AppendText( ")" );
                }
                decor.AppendText( "</a>" );
            }
        }
        public static void AppendEnclosure( IResource item, BodyWriter decor )
        {
            if ( item.HasProp( Props.EnclosureURL ) )
            {
                decor.AppendText( "<p>Enclosure: <a href=\"" );
                string enclosureUrl = HttpUtility.HtmlEncode( item.GetStringProp( Props.EnclosureURL ) );
                decor.AppendText( enclosureUrl );
                decor.AppendText( "\">" );
                decor.AppendText( enclosureUrl );
                if ( item.HasProp( Props.EnclosureSize ) )
                {
                    decor.AppendText( " (" );
                    decor.AppendText( Utils.SizeToString( item.GetIntProp( Props.EnclosureSize ) ) );
                    decor.AppendText( ")" );
                }
				decor.AppendText( "</a></p>" );
			}
        }

        public static void AppendRelatedPosts( IResource item, BodyWriter decor, bool verbose )
        {
            IResourceList relatedPosts = item.GetLinksOfType( Props.RSSLinkedPostResource, Props.LinkedPost );
            if ( relatedPosts.Count > 0 )
            {
                decor.AppendText( "<div class=\"related\"><p>Related readings:</p><ul>" );
                foreach( IResource res in relatedPosts )
                {
                    decor.AppendText( "<li>" );
                    AppendLinkText( res, decor, verbose );
                    decor.AppendText( "</li>" );
                }
                decor.AppendText( "</ul></div>" );
            }
        }

        private static void  AppendLinkText( IResource res, BodyWriter decor, bool verbose )
        {
            string url = res.GetPropText( Props.URL );
            string text = res.GetPropText( Core.Props.Name );

            decor.AppendText( "<a href=\"" );
            decor.AppendText( url );
            decor.AppendText( "\" title=\"" );
            decor.AppendText( verbose ? text : url );
            decor.AppendText( "\">" );
            decor.AppendText( verbose ? url : text );
            decor.AppendText( "</a>" );
        }
    }
}