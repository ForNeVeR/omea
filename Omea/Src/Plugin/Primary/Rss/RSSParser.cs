/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.HTML;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
    public abstract class BaseFeedElementParser : IFeedElementParser
    {
        public abstract void ParseValue( IResource resource, XmlReader reader );
        public virtual bool SkipNextRead { get { return false; } }
    }

    #region RSS Elements Parsers
    public class FeedElementParser : BaseFeedElementParser
    {
        private readonly int _propId;
        private readonly bool _override;

        public FeedElementParser( int propID )
        {
            _propId = propID;
            _override = false;
        }

        public FeedElementParser( int propID, bool isOverride )
        {
            _propId = propID;
            _override = isOverride;
        }

        public override void ParseValue( IResource resource, XmlReader reader )
        {
            if ( _override || !resource.HasProp( _propId ) )
            {
                string strValue = reader.ReadString().Trim();
                if ( Core.ResourceStore.PropTypes[ _propId ].DataType == PropDataType.Int )
                {
                    try
                    {
                        resource.SetProp( _propId, Int32.Parse( strValue ) );
                    }
                    catch ( FormatException )
                    {
                        Trace.WriteLine( "Failed to parse integer value (Format)'" + strValue + "' for property " +
                            Core.ResourceStore.PropTypes[ _propId ].Name );
                    }
                    catch( OverflowException )
                    {
                        Trace.WriteLine( "Failed to parse integer value (Overflow)'" + strValue + "' for property " +
                            Core.ResourceStore.PropTypes[ _propId ].Name );
                    }
                }
                else if ( strValue.Length > 0 )
                {
                    resource.SetProp( _propId, strValue );
                }
            }
        }
    }

    public class FeedNameParser : FeedElementParser
    {
        public FeedNameParser() : base( Props.OriginalName ) {}
        
        public override void ParseValue( IResource resource, XmlReader reader )
        {
            base.ParseValue( resource, reader );

            if( !resource.HasProp( Core.Props.Name ) )
            {
                resource.SetProp( Core.Props.Name, resource.GetStringProp( Props.OriginalName ) );
            }
        }
    }

    internal class TitleParser : BaseFeedElementParser
    {
        public override void ParseValue( IResource resource, XmlReader reader )
        {
            resource.SetProp( Core.Props.Subject, HtmlTools.SafeHtmlDecode( reader.ReadString() ).Trim() );
        }
    }

    internal class GUIDParser : BaseFeedElementParser
    {
        public override void ParseValue( IResource resource, XmlReader reader )
        {
            bool isPermalink = true;
            if ( reader.MoveToAttribute( "isPermaLink" ) && reader.Value.ToLower() == "false" )
            {
                isPermalink = false;
                reader.MoveToContent();
            }
            string guid = reader.ReadString();
            resource.SetProp( Props.GUID, guid );
            if ( isPermalink && !resource.HasProp( Props.Link ) )
            {
                resource.SetProp( Props.Link, guid );
            }
        }
    }

    internal class SourceTagParser : FeedElementParser
    {
        public SourceTagParser() : base( Props.RSSSourceTag )
        {}

        public override void ParseValue( IResource resource, XmlReader reader )
        {
            if ( reader.MoveToAttribute( "url" ) )
            {
                resource.SetProp( Props.RSSSourceTagUrl, reader.Value );
                reader.MoveToContent();
            }
            base.ParseValue( resource, reader );
        }
    }

    internal class EnclosureParser : BaseFeedElementParser
    {
        public override void ParseValue( IResource resource, XmlReader reader )
        {
            if ( reader.MoveToAttribute( "url" ) )
            {
                resource.SetProp( Props.EnclosureURL, reader.Value );
                resource.SetProp( Props.EnclosureDownloadingState, DownloadState.NotDownloaded );
                reader.MoveToContent();
            }
            if ( reader.MoveToAttribute( "length" ) )
            {
                try
                {
                    int enclosureSize = Int32.Parse( reader.Value );
                    if ( enclosureSize < 0 )
                    {
                        enclosureSize = 0;
                    }
                    resource.SetProp( Props.EnclosureSize, enclosureSize );
                }
                catch ( FormatException )
                {
                    Trace.WriteLine( "Failed to parse enclosure size " + reader.Value );
                }
                catch ( OverflowException )
                {
                    Trace.WriteLine( "Enclosure size too large: " + reader.Value );
                }
                reader.MoveToContent();
            }
            if ( reader.MoveToAttribute( "type" ) )
            {
                resource.SetProp( Props.EnclosureType, reader.Value );
                reader.MoveToContent();
            }
        }
    }

    internal class ImageParser : BaseFeedElementParser
    {
        private const string RDFNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

        public override void ParseValue( IResource resource, XmlReader reader )
        {
            if ( reader.NodeType == XmlNodeType.Element && reader.Name == "image" && reader.IsEmptyElement )
            {
                // Try to extract "rdf:resource"
                if( reader.MoveToAttribute( "resource", RDFNamespace ) )
                {
                    resource.SetProp( Props.ImageURL, reader.Value );
                    reader.MoveToContent();
                }
                return;
            }
            while ( reader.Read() )
            {
                XmlNodeType type = reader.NodeType;
                if ( type == XmlNodeType.EndElement && reader.Name == "image" )
                {
                    break;
                }
                if ( type == XmlNodeType.Element && reader.Name == "title" )
                {
                    resource.SetProp( Props.ImageTitle, reader.ReadString() );
                }
                if ( type == XmlNodeType.Element && reader.Name == "url" )
                {
                    resource.SetProp( Props.ImageURL, reader.ReadString() );
                }
                if ( type == XmlNodeType.Element && reader.Name == "link" )
                {
                    resource.SetProp( Props.ImageLink, reader.ReadString() );
                }
            }
        }
    }

    internal class DCDateParser : BaseFeedElementParser
    {
        private class DateFragment
        {
            private readonly char _startChar;
            private readonly int _digitCount;
            private readonly bool _optional;
            private int _value;

            internal DateFragment( char startChar, int digitCount, bool optional )
            {
                _startChar = startChar;
                _digitCount = digitCount;
                _optional = optional;
            }

            internal int Parse( string dateStr, int startOffset )
            {
                if ( startOffset == dateStr.Length )
                {
                    _value = 0;
                    return startOffset;
                }

                int offset = startOffset;
                if ( _startChar != '\0' )
                {
                    if ( dateStr[ offset ] == _startChar )
                    {
                        offset++;
                    }
                    else
                    {
                        if ( _optional )
                        {
                            _value = 0;
                            return offset;
                        }
                        throw new Exception( "Failed to parse date: starting char " + _startChar + " not found" );
                    }
                }
                _value = 0;
                int foundDigits = 0;
                while ( offset < dateStr.Length && ( _digitCount == 0 || foundDigits < _digitCount ) )
                {
                    if ( !Char.IsDigit( dateStr, offset ) )
                    {
                        if ( _digitCount == 0 )
                        {
                            break;
                        }
                        throw new Exception( "Failed to find expected number of digits" );
                    }
                    _value = _value * 10 + dateStr[ offset ] - '0';
                    offset++;
                    foundDigits++;
                }
                return offset;
            }

            internal int Value { get { return _value; } }
        }

        private readonly int _propID;
        private readonly DateFragment[] _fragments = new DateFragment[7];
        private readonly DateFragment[] _tzFragments = new DateFragment[2];

        public DCDateParser( int propID )
        {
            _propID = propID;

            _fragments[ 0 ] = new DateFragment( '\0', 4, false );
            _fragments[ 1 ] = new DateFragment( '-', 2, false );
            _fragments[ 2 ] = new DateFragment( '-', 2, false );
            _fragments[ 3 ] = new DateFragment( 'T', 2, false );
            _fragments[ 4 ] = new DateFragment( ':', 2, false );
            _fragments[ 5 ] = new DateFragment( ':', 2, true );
            _fragments[ 6 ] = new DateFragment( '.', 0, true );

            _tzFragments[ 0 ] = new DateFragment( '\0', 2, false );
            _tzFragments[ 1 ] = new DateFragment( ':', 2, false );
        }

        public override void ParseValue( IResource resource, XmlReader reader )
        {
            string dateStr = reader.ReadString().Trim();
            try
            {
                int offset = 0;
                for ( int i = 0; i < 7; i++ )
                {
                    offset = _fragments[ i ].Parse( dateStr, offset );
                }

                int tzModifier = 0;
                if ( offset < dateStr.Length && ( dateStr[ offset ] == '+' || dateStr[ offset ] == '-' ) )
                {
                    tzModifier = ( dateStr[ offset ] == '+' ) ? -1 : 1;
                    offset++;
                    offset = _tzFragments[ 0 ].Parse( dateStr, offset );
                    offset = _tzFragments[ 1 ].Parse( dateStr, offset );
                }

                int msec = _fragments[ 6 ].Value;
                if ( msec > 999 ) // OM-7165
                {
                    msec = 0;
                }

                DateTime dt = new DateTime(
                    _fragments[ 0 ].Value,
                    _fragments[ 1 ].Value,
                    _fragments[ 2 ].Value,
                    _fragments[ 3 ].Value,
                    _fragments[ 4 ].Value,
                    _fragments[ 5 ].Value,
                    msec );
                dt = dt.AddHours( _tzFragments[ 0 ].Value * tzModifier );
                dt = dt.AddMinutes( _tzFragments[ 1 ].Value * tzModifier );

                resource.SetProp( _propID, dt.ToLocalTime() );
            }
            catch ( Exception e )
            {
                Trace.WriteLine( "Failed to parse dc:date " + dateStr + ": " + e.Message );
            }
        }
    }

    internal class RFCDateParser : BaseFeedElementParser
    {
        private readonly int _propID;

        public RFCDateParser( int propID )
        {
            _propID = propID;
        }

        public override void ParseValue( IResource resource, XmlReader reader )
        {
            string dateStr = reader.ReadString();

            try
            {
                resource.SetProp( _propID, RFC822DateParser.ParseDate( dateStr ) );
            }
            catch ( Exception e )
            {
                Trace.WriteLine( "Failed to parse RFC-822 date " + dateStr + ": " + e.Message );
            }
        }
    }

    internal class FeedAuthorParser : BaseFeedElementParser
    {
        private readonly Regex _creatorRX;
        private readonly Regex _creatorRX2;
        private readonly Regex _emailRX;

        public FeedAuthorParser()
        {
            _creatorRX = new Regex( @"([^@]+@[^@]+)\s+\(([^()]+)\)" ); // person@domain.com (Name)
            _creatorRX2 = new Regex( @"([^@]+)\s+\(([^@]+@[^@]+)\)" ); // name (person@domain.com)
            _emailRX = new Regex( @"[^@]+@[^@]+" );
        }

        public override void ParseValue( IResource feed, XmlReader reader )
        {
            string creator = reader.ReadString();
            ParseAuthorString( feed, creator );
        }

        internal void ParseAuthorString( IResource feed, string creator )
        {
            feed.SetProp( Props.Author, creator );
    
            string email = null;
            string name = null;
    
            Match m = _creatorRX.Match( creator );
            if ( m.Success )
            {
                email = m.Groups[ 1 ].Value;
                name = m.Groups[ 2 ].Value;
            }
            else
            {
                m = _creatorRX2.Match( creator );
                if ( m.Success )
                {
                    name = m.Groups[ 1 ].Value;
                    email = m.Groups[ 2 ].Value;
                }
                else
                {
                    m = _emailRX.Match( creator );
                    if ( m.Success )
                    {
                        email = m.Value;
                    }
                }
            }
    
            if ( email != null )
            {
                if ( !feed.HasProp( Props.AuthorEmail ) )
                {
                    IResource emailAcct = Core.ContactManager.FindOrCreateEmailAccount( email );
                    if ( emailAcct != null )
                    {
                        emailAcct.AddLink( Props.AuthorEmail, feed );
                    }
                }

                IContact contact = Core.ContactManager.FindOrCreateContact( email, name );
                contact.Resource.AddLink( Props.Weblog, feed );
            }
        }
    }

    internal class ItemAuthorParser : BaseFeedElementParser
    {
        public override void ParseValue( IResource resource, XmlReader reader )
        {
            string author = reader.ReadString();

            if ( author.Trim().Length > 0 )
            {
                IContact contact = Core.ContactManager.FindOrCreateContact( null, author );
                Core.ContactManager.LinkContactToResource(
                    Core.ResourceStore.PropTypes[ "From" ].Id, contact.Resource, resource, (IResource)null, author );
            }
        }
    }

    internal class XhtmlBodyParser : IFeedElementParser
    {
        public void ParseValue( IResource resource, XmlReader reader )
        {
            resource.SetProp( Core.Props.LongBody, reader.ReadInnerXml() );
        }

        public bool SkipNextRead { get { return true; } }
    }
    #endregion RSS Elements Parsers

    #region ATOM Elements Parsers
	/// <summary>
	/// Generic parser for ATOM Link constructs.
	/// Distinguish three distinct relations (by release 2.1.2), discriminated
	/// by the value of "rel" tag:
	/// - "alternate" value, which is an obligatory relation type
	///   for an Atom entry.
	/// - "related" value, which means any semantically close
	///   external (!) link (from the point of view of post author, not Atom
	///   standard).
	/// - "enclosure" value, which describes an enclosure location, size and type.
	///   
	///   NB: for a compatibility with illegally-formed ATOM feeds, there are
	///       entries which contain "link" relations without any "rel" attribute.
	/// </summary>
    internal class AtomEntryLinkParser : BaseFeedElementParser
    {
        public override void ParseValue( IResource resource, XmlReader reader )
        {
            string href = reader.GetAttribute( "href" );
            string rel = reader.GetAttribute( "rel" );
            if ( string.IsNullOrEmpty( rel ) )
            {
                rel = "alternate";
            }

            string linkBase = resource.GetPropText( Props.LinkBase );
            if ( linkBase.Length > 0 )
            {
                try
                {
                    href = new Uri( new Uri( linkBase ), href ).ToString();
                }
                catch( UriFormatException )
                {
                    // ignore
                }
            }

            if( rel == "enclosure" )
            {
                LinkEnclosureInformation( resource, href, reader );
            }
            else
            if ( rel == "related" )
            {
                LinkRelatedInformation( resource, href, reader );
            }
            else
            if ( rel == "alternate" )
            {
                resource.SetProp( Props.Link, href );
            }
        }

        private static void  LinkEnclosureInformation( IResource resource, string href, XmlReader reader )
        {
            string type = reader.GetAttribute( "type" );

            resource.SetProp( Props.EnclosureURL, href );
            if ( type.Length > 0 )
            {
                resource.SetProp( Props.EnclosureType, type );                    
            }
            //   resource.SetProp( Props.EnclosureType, type );
		                
            string length = reader.GetAttribute( "length" );
            if ( !string.IsNullOrEmpty( length ) )
            {
                try
                {
                    int enclosureSize = Int32.Parse( length );
                    if ( enclosureSize < 0 )
                    {
                        enclosureSize = 0;
                    }
                    resource.SetProp( Props.EnclosureSize, enclosureSize );
                }
                catch ( FormatException )
                {
                    Trace.WriteLine( "Failed to parse enclosure size " + reader.Value );
                }
                catch ( OverflowException )
                {
                    Trace.WriteLine( "Enclosure size too large: " + reader.Value );
                }
            }
            resource.SetProp( Props.EnclosureDownloadingState, DownloadState.NotDownloaded );
        }

        private static void  LinkRelatedInformation( IResource resource, string href, XmlReader reader )
        {
            string title = reader.GetAttribute( "title" );

            IResource newRelated = Core.ResourceStore.BeginNewResource( Props.RSSLinkedPostResource );
            try
            {
                newRelated.SetProp( Props.URL, href );
                if( !string.IsNullOrEmpty( title ))
                    newRelated.SetProp( Core.Props.Name, title );
            }
            finally
            {
                newRelated.EndUpdate();
            }

            resource.AddLink( Props.LinkedPost, newRelated );
        }
    }

	/// <summary>
	/// Parser for ATOM Channel Link constructs.
	/// </summary>
	internal class AtomChannelLinkParser : BaseFeedElementParser
	{
		private readonly int _propId;
		private const string _expectRel = "alternate";

		public AtomChannelLinkParser( int propId )
		{
			_propId = propId;
		}

		public override void ParseValue( IResource resource, XmlReader reader )
		{
			string rel = reader.GetAttribute( "rel" );
			string href = reader.GetAttribute( "href" );

			string linkBase = resource.GetPropText( Props.LinkBase );
			if ( linkBase.Length > 0 )
			{
				try
				{
					href = new Uri( new Uri( linkBase ), href ).ToString();
				}
				catch( UriFormatException )
				{
					// ignore
				}
			}

			if ( String.Compare(rel,_expectRel, true ) == 0 )
			{
				resource.SetProp( _propId, href );
			}
		}
	}

    /// <summary>
    /// Generic parser for ATOM Person constructs.
    /// </summary>
    internal class AtomPersonParser : BaseFeedElementParser
    {
        private readonly int _propId;

        public AtomPersonParser( int propID )
        {
            _propId = propID;
        }

        public override void ParseValue( IResource resource, XmlReader reader )
        {
            string name = null;
            string url = null;
            string email = null;

            int startDepth = reader.Depth;
            while ( reader.Read() )
            {
                if ( reader.NodeType == XmlNodeType.Element && 
                    ( reader.NamespaceURI == RSSParser.NamespaceATOM03 || reader.NamespaceURI == RSSParser.NamespaceATOM10 ) )
                {
                    if ( reader.LocalName == "name" )
                    {
                        name = reader.ReadString();
                    }
                    else if ( reader.LocalName == "url" || reader.LocalName == "uri" )
                    {
                        url = reader.ReadString();
                    }
                    else if ( reader.LocalName == "email" )
                    {
                        email = reader.ReadString();
                    }
                }
                else if ( reader.NodeType == XmlNodeType.EndElement )
                {
                    if ( reader.Depth == startDepth )
                    {
                        break;
                    }
                }
            }

            if ( !string.IsNullOrEmpty( name ) || !string.IsNullOrEmpty( email ) )
            {
                IContact contact = Core.ContactManager.FindOrCreateContact( email, name );
                if ( _propId == Core.ContactManager.Props.LinkFrom )
                {
                    // Fix OM-13266.
                    if( !resource.IsDeleting && !resource.IsDeleted )
                    {
                        Core.ContactManager.LinkContactToResource( Core.ContactManager.Props.LinkFrom,
                                                                   contact.Resource, resource, email, name );
                    }
                }
                else
                {
                    if ( _propId > 0 )
                    {
                        resource.AddLink( _propId, contact.Resource );
                    }
                    else
                    {
                        contact.Resource.AddLink( -_propId, resource );
                    }
                }

                if ( url != null && contact.HomePage == string.Empty )
                {
                    contact.HomePage = url;
                }
            }
        }

        public override bool SkipNextRead { get { return true; } }
    }

    /// <summary>
    /// Generic parser for ATOM 0.3 Content constructs.
    /// </summary>
    internal class AtomContentParser : BaseFeedElementParser
    {
        //  default for this implementation.
        private readonly TextFormat _expectedFormat = TextFormat.Html;
        private readonly int _propID;

        public AtomContentParser( int propID )
        {
            _propID = propID;
        }

        public AtomContentParser( int propID, TextFormat expectedFormat )
        {
            _propID = propID;
            _expectedFormat = expectedFormat;
        }

        public override void ParseValue( IResource resource, XmlReader reader )
        {
            string content = null;
            string mode = reader.GetAttribute( "mode" ) ?? "xml";

            if( String.Compare( mode, "xml", true ) == 0 )
            {
                reader.Read();
                if ( reader.NodeType == XmlNodeType.CDATA || reader.NodeType == XmlNodeType.Text )
                {
                    content = reader.ReadString();
                }
                else
                {
                    content = reader.ReadOuterXml();
                    if ( _expectedFormat == TextFormat.PlainText )
                    {
                        content = HtmlTools.StripHTML( content );
                    }
                }
            }
            else
            if( String.Compare( mode, "escaped", true ) == 0 )
            {
                content = HtmlTools.SafeHtmlDecode( reader.ReadString() );
            }
            else
            if( String.Compare( mode, "base64", true ) == 0 )
            {
/*
 * NB, LloiX (17.03.2008)
 * This is an arguable code. "base64" encoding is used to store data of a binary
 * nature. It is both illogical and invalid to convert the result "byte[]" intermediate
 * content to a string one.
 * As an alternative we can emulate the notion of an "attachment" for this part of the
 * content but that requires changes in the API and feed article processing.
                content = reader.ReadString();
                byte[] data = Convert.FromBase64String( content );
                XmlValidatingReader valReader = (XmlValidatingReader)reader;
                content = valReader.Encoding.GetString( data );
*/
                Core.ReportBackgroundException( new ApplicationException( "RssParser -- Processing base64-mode content for text." ) );
            }
            
            if ( content != null ) 
            {
                resource.SetProp( _propID, content );
            }
        }
    }

    internal class AtomTitleParser : AtomTextParser
    {
        public AtomTitleParser() : base( Props.OriginalName, TextFormat.PlainText ) {}
        public override void ParseValue( IResource resource, XmlReader reader )
        {
            base.ParseValue( resource, reader );
            if( !resource.HasProp( Core.Props.Name ) )
                resource.SetProp( Core.Props.Name, resource.GetStringProp( Props.OriginalName ) );
        }
    }

    /// <summary>
    /// Generic parser for ATOM 1.0 Text constructs.
    /// </summary>
    internal class AtomTextParser: BaseFeedElementParser
    {
        private readonly int _propId;
        private readonly TextFormat _expectFormat;

        public AtomTextParser( int propId, TextFormat expectFormat )
        {
            _propId = propId;
            _expectFormat = expectFormat;
        }

        public override void ParseValue( IResource resource, XmlReader reader )
        {
            string type = reader.GetAttribute( "type" ) ?? "text";

            string content = null;
            if( String.Compare( type, "text", true) == 0 )
            {
                content = (_expectFormat == TextFormat.PlainText) ? reader.ReadString() :
                                                                    HttpUtility.HtmlEncode( reader.ReadString() );
            }
            else if( String.Compare( type, "text/html", true ) == 0 ||
                     String.Compare( type, "html", true) == 0 )
            {
                content = (_expectFormat == TextFormat.PlainText) ? HtmlTools.StripHTML( reader.ReadString() ) :
                                                                    reader.ReadString();
            }
            else if( String.Compare( type, "xhtml", true ) == 0 )
            {
                reader.Read();   // move to the xhtml:div element
                content = reader.ReadInnerXml();
                if ( _expectFormat == TextFormat.PlainText )
                {
                    content = HtmlTools.StripHTML( content );
                }
            }
            
            if ( content != null )
            {
                resource.SetProp( _propId, content );
            }
        }
    }

    internal class AtomCategoryParser: BaseFeedElementParser
    {
        public override void ParseValue( IResource resource, XmlReader reader )
        {
            string category = reader.GetAttribute( "label" );
            if ( string.IsNullOrEmpty( category ) )
            {
                category = reader.GetAttribute( "term" );
            }
            if ( !string.IsNullOrEmpty( category ) )
            {
                resource.SetProp( Props.RSSCategory, category );
            }
        }
    }

    internal class AtomSourceParser: BaseFeedElementParser
    {
        public override void ParseValue( IResource resource, XmlReader reader )
        {}
    }
    #endregion ATOM Elements Parsers

    /**
     * General parsing framework for RSS and ATOM feeds.
     */

    internal class RSSParser
    {
        internal static IResource _nextItem = null;

        private readonly IResourceStore _store;
        private readonly IResource  _feed;
        private readonly bool       _allowEqualPosts;
        private readonly IResource  _commentItem;
        private readonly IResource  _commentFeed;
        private readonly IntHashSet _currentFeedItems = new IntHashSet();
        private bool _foundChannel;
        private HashSet _deletedItems;
        private HashSet _newDeletedItems;
        private DateTime _parseDate;
        private bool _uniqueLinks;
        private bool _uniqueLinksKnown = false;

        private const string NamespaceRSS09 = "http://my.netscape.com/rdf/simple/0.9/";
        private const string NamespaceRSS091 = "http://my.netscape.com/publish/formats/rss-0.91.dtd";
        private const string NamespaceRSS093 = "http://backend.userland.com/rss093";
        private const string NamespaceRSS10 = "http://purl.org/rss/1.0/";
        private const string NamespaceRSS10_1 = "http://www.purl.org/rss/1.0/";
        private const string NamespaceRSS20 = "http://backend.userland.com/rss2";
        private const string NamespaceDC = "http://purl.org/dc/elements/1.1/";
        private const string NamespaceHTML = "http://www.w3.org/1999/xhtml";
        private const string NamespaceContent = "http://purl.org/rss/1.0/modules/content/";
        private const string NamespaceSyndication = "http://purl.org/rss/1.0/modules/syndication/";
        private const string NamespaceSlash = "http://purl.org/rss/1.0/modules/slash/";
        public const string NamespaceATOM03 = "http://purl.org/atom/ns#";
        public const string NamespaceATOM10 = "http://www.w3.org/2005/Atom";
        private const string NamespaceWFW = "http://wellformedweb.org/CommentAPI/";

        private static readonly string[] _rssNamespaces = new[]
            {
                "", NamespaceRSS09, NamespaceRSS091, NamespaceRSS093,
                NamespaceRSS10, NamespaceRSS10_1, NamespaceRSS20
            };

        private static Hashtable _rssItemElements;
        private static Hashtable _rssChannelElements;
        private static Hashtable _atomItemElements;
        private static Hashtable _atomChannelElements;

        private static readonly Regex _rxLink = new Regex( "href=\"([^\"]+)\"" );
        private static readonly Regex _rxLink2 = new Regex( "href='([^']+)'" );

        internal event ResourceEventHandler ItemParsed;

        public RSSParser( IResource feed )
        {
            _store = Core.ResourceStore;
            CheckItemsRegister();

            _feed = feed;
            _allowEqualPosts = _feed.HasProp( Props.AllowEqualPosts );
            _commentItem = _feed.GetLinkProp( Props.ItemCommentFeed );
            _commentFeed = _feed.GetLinkProp( Props.FeedComment2Feed );
        }

        public void Dispose()
        {
            _rssItemElements = null;
        }

        #region Static Initialization
        private static void CheckItemsRegister()
        {
            if ( _rssItemElements == null )
            {
                RegisterRSSElements();
                RegisterAtomElements();
            }
        }

        /**
         * Registers the elements for the RSS feeds and items.
         */
        private static void RegisterRSSElements()
        {
            _rssItemElements = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _rssChannelElements = CollectionsUtil.CreateCaseInsensitiveHashtable();

            foreach ( string ns in _rssNamespaces )
            {
                RegisterRssStandardElements( ns );
            }

            _rssItemElements[ NamespaceContent + ":encoded" ] = new FeedElementParser( Core.Props.LongBody, true );
            _rssItemElements[ NamespaceDC + ":date" ] = new DCDateParser( Core.Props.Date );
            _rssItemElements[ NamespaceDC + ":creator" ] = new ItemAuthorParser();
            _rssItemElements[ NamespaceHTML + ":body" ] = new XhtmlBodyParser();
            _rssItemElements[ NamespaceSlash + ":comments" ] = new FeedElementParser( Props.CommentCount.Id );
            _rssItemElements[ NamespaceWFW + ":commentRSS" ] = new FeedElementParser( Props.CommentRSS );
            _rssItemElements[ NamespaceWFW + ":comment" ] = new FeedElementParser( Props.WfwComment );
            _rssChannelElements[ NamespaceDC + ":creator" ] = new FeedAuthorParser();
            _rssChannelElements[ NamespaceSyndication + ":updatePeriod" ] = new FeedElementParser( Props.UpdatePeriod );
            _rssChannelElements[ NamespaceSyndication + ":updateFrequency" ] = new FeedElementParser( Props.UpdateFrequency );

            RegisterCommonElements( _rssItemElements );
        }

        private static void RegisterRssStandardElements( string ns )
        {
            _rssItemElements[ ns + ":description" ] = new FeedElementParser( Core.Props.LongBody );
            _rssItemElements[ ns + ":link" ] = new FeedElementParser( Props.Link );
            _rssItemElements[ ns + ":category" ] = new FeedElementParser( Props.RSSCategory );
            _rssItemElements[ ns + ":comments" ] = new FeedElementParser( Props.CommentURL );
            _rssItemElements[ ns + ":guid" ] = new GUIDParser();
            _rssItemElements[ ns + ":pubDate" ] = new RFCDateParser( Core.Props.Date );
            _rssItemElements[ ns + ":title" ] = new TitleParser();
            _rssItemElements[ ns + ":enclosure" ] = new EnclosureParser();
            _rssItemElements[ ns + ":author" ] = new ItemAuthorParser();
            _rssItemElements[ ns + ":source" ] = new SourceTagParser();
    
            _rssChannelElements[ ns + ":managingEditor" ] = new FeedAuthorParser();
            _rssChannelElements[ ns + ":pubDate" ] = new RFCDateParser( Props.PubDate );
            _rssChannelElements[ ns + ":title" ] = new FeedNameParser();
            _rssChannelElements[ ns + ":link" ] = new FeedElementParser( Props.HomePage );
            _rssChannelElements[ ns + ":description" ] = new FeedElementParser( Props.Description );
            _rssChannelElements[ ns + ":image" ] = new ImageParser();
        }

        /**
         * Registers the elements for the Atom channels and items.
         */

        private static void RegisterAtomElements()
        {
            _atomItemElements = CollectionsUtil.CreateCaseInsensitiveHashtable();
            _atomChannelElements = CollectionsUtil.CreateCaseInsensitiveHashtable();

            _atomChannelElements[ NamespaceATOM03 + ":title" ] = new FeedNameParser();
            _atomChannelElements[ NamespaceATOM10 + ":title" ] = new AtomTitleParser();
            //
			_atomChannelElements[ NamespaceATOM03 + ":link" ] = new AtomChannelLinkParser( Props.HomePage );
			_atomChannelElements[ NamespaceATOM10 + ":link" ] = new AtomChannelLinkParser( Props.HomePage );
            _atomChannelElements[ NamespaceATOM03 + ":author" ] = new AtomPersonParser( -Props.Weblog );
            _atomChannelElements[ NamespaceATOM10 + ":author" ] = new AtomPersonParser( -Props.Weblog );
            _atomChannelElements[ NamespaceATOM03 + ":tagline" ] = new AtomContentParser( Props.Description );
            _atomChannelElements[ NamespaceATOM10 + ":subtitle" ] = new AtomTextParser( Props.Description, TextFormat.PlainText );
            _atomChannelElements[ NamespaceATOM10 + ":logo" ] = new FeedElementParser( Props.ImageURL );

            _atomItemElements[ NamespaceATOM03 + ":title" ] = new AtomContentParser( Core.Props.Subject, TextFormat.PlainText );
            _atomItemElements[ NamespaceATOM10 + ":title" ] = new AtomTextParser( Core.Props.Subject, TextFormat.PlainText );
			_atomItemElements[ NamespaceATOM03 + ":link" ] = new AtomEntryLinkParser();
			_atomItemElements[ NamespaceATOM10 + ":link" ] = new AtomEntryLinkParser();
            _atomItemElements[ NamespaceATOM03 + ":author" ] = new AtomPersonParser( Core.ContactManager.Props.LinkFrom );
            _atomItemElements[ NamespaceATOM10 + ":author" ] = new AtomPersonParser( Core.ContactManager.Props.LinkFrom );
            _atomItemElements[ NamespaceATOM03 + ":id" ] = new FeedElementParser( Props.GUID );
            _atomItemElements[ NamespaceATOM10 + ":id" ] = new FeedElementParser( Props.GUID );
            _atomItemElements[ NamespaceATOM03 + ":created" ] = new DCDateParser( Core.Props.Date );
            _atomItemElements[ NamespaceATOM10 + ":published" ] = new DCDateParser( Core.Props.Date );
            _atomItemElements[ NamespaceATOM03 + ":modified" ] = new DCDateParser( Props.DateModified );
            _atomItemElements[ NamespaceATOM10 + ":updated" ] = new DCDateParser( Props.DateModified );
            _atomItemElements[ NamespaceATOM03 + ":summary" ] = new AtomContentParser( Props.Summary );
            _atomItemElements[ NamespaceATOM10 + ":summary" ] = new AtomTextParser( Props.Summary, TextFormat.Html );
            _atomItemElements[ NamespaceATOM03 + ":content" ] = new AtomContentParser( Core.Props.LongBody );
            _atomItemElements[ NamespaceATOM10 + ":content" ] = new AtomTextParser( Core.Props.LongBody, TextFormat.Html );
            _atomItemElements[ NamespaceATOM10 + ":category" ] = new AtomCategoryParser();
            _atomItemElements[ NamespaceATOM10 + ":source" ] = new AtomSourceParser();

            RegisterCommonElements( _atomItemElements );
        }

        /**
         * Registers the elements common for RSS and ATOM feeds.
         */

        private static void RegisterCommonElements( Hashtable itemElements )
        {
            itemElements[ NamespaceDC + ":subject" ] = new FeedElementParser( Props.RSSCategory );
        }

        public static void RegisterChannelElementParser( FeedType type, string xmlNameSpace, string elementName,
                                                         IFeedElementParser parser )
        {
            CheckItemsRegister();
            switch ( type )
            {
                case FeedType.Rss:
                    _rssChannelElements[ xmlNameSpace + ":" + elementName ] = parser;
                    break;

                case FeedType.Atom:
                    _atomChannelElements[ xmlNameSpace + ":" + elementName ] = parser;
                    break;
            }
        }

        public static void RegisterItemElementParser( FeedType type, string xmlNameSpace, string elementName,
                                                      IFeedElementParser parser )
        {
            CheckItemsRegister();
            switch ( type )
            {
                case FeedType.Rss:
                    _rssItemElements[ xmlNameSpace + ":" + elementName ] = parser;
                    break;

                case FeedType.Atom:
                    _atomItemElements[ xmlNameSpace + ":" + elementName ] = parser;
                    break;
            }
        }
        #endregion Static Initialization

        public void Parse( Stream stream, Encoding encoding, bool parseItems )
        {
            _parseDate = DateTime.Now;
            _foundChannel = false;
            FillDeletedItemsSet();

            string encodingName = ( encoding == null ) ? null : encoding.BodyName;
            XmlPreparer preparer = new XmlPreparer( stream, encodingName );
            
            XmlTextReader baseReader;
            if ( preparer.PrepareXML() )
            {
                NameTable nt = new NameTable();
                XmlNamespaceManager nsmgr = new LooseNSManager( nt );
                XmlParserContext ctx = new XmlParserContext( nt, nsmgr, null, XmlSpace.None);
                string xml = preparer.GetXML();
                baseReader = new XmlTextReader( xml, XmlNodeType.Document, ctx );
            }
            else
            {
                Trace.WriteLine( "Can not process feed '" + _feed.DisplayName + "' with preparer\n" );
                baseReader = (encoding == null) ? new XmlTextReader( stream ) :
                                                  new XmlTextReader( new StreamReader( stream, encoding ) );
            }
            //  Following two lines are obsolete?
            baseReader.WhitespaceHandling = WhitespaceHandling.None;
            baseReader.XmlResolver = null;

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.None;
            settings.XmlResolver = null;
            XmlReader reader = XmlReader.Create( baseReader, settings );

            while ( reader.Read() )
            {
                if ( reader.NodeType == XmlNodeType.Element &&
                    ( reader.LocalName == "channel" || 
                    ( ( reader.NamespaceURI == NamespaceATOM03 || reader.NamespaceURI == NamespaceATOM10 ) && reader.LocalName == "feed" ) ) )
                {
                    if ( reader.LocalName == "channel" )
                    {
                        if ( reader.NamespaceURI.Length > 0 && Array.IndexOf( _rssNamespaces, reader.NamespaceURI ) < 0 )
                        {
                            RegisterRssStandardElements( reader.NamespaceURI );
                        }
                        ParseChannel( reader, parseItems, "channel", "item", _rssChannelElements, _rssItemElements );
                    }
                    else
                    {
                        ParseChannel( reader, parseItems, "feed", "entry", _atomChannelElements, _atomItemElements );
                    }
                    SaveDeletedItemsSet();
                    _foundChannel = true;
                }
            }
        }

        public bool FoundChannel { get { return _foundChannel; } }

        /**
         * Parses the DeletedItems property of a feed to get the HashSet of 
         * items which were marked as deleted after previous parse, and clears
         * the HashSet of items that will be marked as deleted after the current
         * parse.
         */

        private void FillDeletedItemsSet()
        {
            _deletedItems = new HashSet();
            _newDeletedItems = new HashSet();

            foreach ( string delItem in _feed.GetStringListProp( Props.DeletedItemHashList ) )
            {
                _deletedItems.Add( delItem );
            }
        }

        /**
         * Saves the new set of deleted item hashes to the feed property.
         */

        private void SaveDeletedItemsSet()
        {
            IStringList delItems = _feed.GetStringListProp( Props.DeletedItemHashList );
            foreach ( HashSet.Entry he in _deletedItems )
            {
                if ( !_newDeletedItems.Contains( he.Key ) )
                {
                    delItems.Remove( (string)he.Key );
                }
            }
            delItems.Dispose();
        }

        private bool GetUniqueLinks()
        {
            if ( !_uniqueLinksKnown )
            {
                if ( _feed.HasProp( Props.UniqueLinks ) )
                {
                    _uniqueLinks = _feed.GetIntProp( Props.UniqueLinks ) == 1;
                }
                else
                {
                    _uniqueLinks = CheckUniqueLinks();
                    if ( !_uniqueLinks )
                    {
                        _feed.SetProp( Props.UniqueLinks, 0 );
                    }
                    else if ( _feed.GetLinkCount( Props.RSSItem ) >= 50 )
                    {
                        _feed.SetProp( Props.UniqueLinks, 1 );
                    }
                }
                _uniqueLinksKnown = true;
            }
            return _uniqueLinks;
        }

        /**
         * Checks if all links in the feed point to different pages and can be
         * used t
         */

        private bool CheckUniqueLinks()
        {
            HashSet linkValues = new HashSet();
            foreach ( IResource item in _feed.GetLinksFrom( "RSSItem", Props.RSSItem ) )
            {
                string link = item.GetStringProp( Props.Link );
                if ( link != null )
                {
                    if ( linkValues.Contains( link ) )
                    {
                        return false;
                    }
                    linkValues.Add( link );
                }
            }
            return true;
        }

        /**
         * Parses an RSS or ATOM channel using the specified maps for channel and item elements.
         */

        private void ParseChannel( XmlReader reader, bool parseItems,
                                   string feedLocalName, string itemLocalName,
                                   Hashtable channelElements, Hashtable itemElements )
        {
            string xmlBase = reader.GetAttribute( "xml:base" );
            if ( !string.IsNullOrEmpty( xmlBase ) )
            {
                _feed.SetProp( Props.LinkBase, xmlBase );
            }

            int startDepth = reader.Depth;
            bool channelDone = false;
            while( Core.State != CoreState.ShuttingDown && reader.Read() )
            {
                if ( reader.NodeType == XmlNodeType.Element )
                {
                    if ( reader.LocalName == itemLocalName && parseItems )
                    {
                        if( _nextItem == null )
                        {
                            _nextItem = _store.NewResourceTransient( "RSSItem" );
                        }
                        if( _nextItem.HasProp( Props._propFake ))
                            throw new ApplicationException( "Feed-Post cleaning violation" );

                        _nextItem.SetProp( Props._propFake, _feed.Id );
                        ParseItem( reader, _nextItem, itemElements );
                        AddOrUpdateItemToFeed( _nextItem );
                    }
                    else if ( reader.Depth == startDepth + 1 && !channelDone )
                    {
                        string attrName = reader.NamespaceURI + ":" + reader.LocalName;
                        IFeedElementParser parser = (IFeedElementParser)channelElements[ attrName ];
                        if ( parser != null )
                        {
                            parser.ParseValue( _feed, reader );
                        }
                    }
                }
                else if ( reader.NodeType == XmlNodeType.EndElement && reader.LocalName == feedLocalName )
                {
                    channelDone = true;
                }
            }
        }

        /**
         * Parses an RSS or ATOM item using the specified map for item elements.
         */

        private void ParseItem( XmlReader reader, IResource item, Hashtable itemElements )
        {
            Trace.WriteLineIf( Settings.Trace, "Parsing new item" );

            string xmlBase = reader.GetAttribute( "xml:base" );
            if ( !string.IsNullOrEmpty( xmlBase ) )
            {
                string channelXmlBase = _feed.GetPropText( Props.LinkBase );
                if ( channelXmlBase.Length > 0 )
                {
                    try
                    {
                        xmlBase = new Uri( new Uri( channelXmlBase ), xmlBase ).ToString();
                    }
                    catch( UriFormatException )
                    {
                        // ignore
                    }
                }
                item.SetProp( Props.LinkBase, xmlBase );
            }

            int startDepth = reader.Depth;
            bool skipNextRead = false;
            while ( skipNextRead || reader.Read() )
            {
                if ( reader.NodeType == XmlNodeType.Element && reader.Depth == startDepth + 1 )
                {
                    string attrName = reader.NamespaceURI + ":" + reader.LocalName;
                    IFeedElementParser parser = (IFeedElementParser)itemElements[ attrName ];
                    if ( parser != null )
                    {
                        if( Settings.Trace ) 
                        {
                            Trace.WriteLine( "Invoking handler " + parser + " for element " + attrName );
                        }
                        parser.ParseValue( item, reader );
                        skipNextRead = parser.SkipNextRead;
                    }
                    else
                    {
                        if( Settings.Trace ) 
                        {
                            Trace.WriteLine( "Handler not found for element " + attrName );
                        }
                        skipNextRead = false;
                    }
                }
                else if ( reader.NodeType == XmlNodeType.EndElement )
                {
                    if ( reader.Depth == startDepth )
                    {
                        return;
                    }
                    skipNextRead = false;
                }
            }
        }

        private void AddOrUpdateItemToFeed( IResource item )
        {
            Guard.NullArgument( item, "item" );
            if( Settings.Trace ) 
            {
                Trace.WriteLine( "Parsed item with subject " + item.GetPropText( Core.Props.Subject ) );
            }
            if ( ItemParsed != null )
            {
                ItemParsed( this, new ResourceEventArgs( item ) );
                if ( item.IsDeleted )
                {
                    return;
                }
            }

            IResource oldItem = GetExistingItem( item );
            if ( oldItem == null )
            {
                AddItemToFeed( item );
            }
            else
            {
                UpdateItemToFeed( item, oldItem );
            }
        }

        private static void UpdateProp( IResource item, IResource oldItem, int propId )
        {
            if ( item.HasProp( propId ) )
            {
                oldItem.SetProp( propId, item.GetProp( propId ) );
            }
        }

        private static void UpdateProp<T>(IResource item, IResource oldItem, PropId<T> propId)
        {
            if (item.HasProp(propId))
            {
                oldItem.SetProp(propId, item.GetProp(propId));
            }
        }

        private void UpdateItemToFeed(IResource item, IResource oldItem)
        {
            try 
            {
                Guard.NullArgument( item, "item" );
                Guard.NullArgument( oldItem, "oldItem" );

                string subject = item.GetPropText( Core.Props.Subject );
                if ( subject.Length > 0 )
                {
                    oldItem.SetProp( Core.Props.Subject, subject );
                }
                UpdateBodyAndSize( item, oldItem );
                UpdateProp( item, oldItem, Props.CommentCount );
                UpdateProp( item, oldItem, Props.WfwComment );
                UpdateProp( item, oldItem, Props.RSSSourceTag );
                UpdateProp( item, oldItem, Props.RSSCategory );
                UpdateProp( item, oldItem, Props.RSSSourceTagUrl );
                if ( !oldItem.HasProp( Props.EnclosureDownloadingState ) && item.HasProp( Props.EnclosureDownloadingState ) )
                {
                    UpdateProp( item, oldItem, Props.EnclosureDownloadingState );
                }
            }
            finally 
            {
                int newId = item.GetIntProp( Props._propFake );
                int id = oldItem.GetLinksOfType( Props.RSSFeedResource, Props.RSSItem )[ 0 ].Id;
                item.ClearProperties();

                if( id != newId )
                    throw new ApplicationException( "Feed-Post update linkage violation - Feed ids do not coinside.");
                if( _feed.Id != newId )
                    throw new ApplicationException( "Feed-Post update linkage violation - Feed id do not coinside with newItem id.");
            }
        }

        private void AddItemToFeed( IResource item )
        {
            int linksCount = 0;
                Guard.NullArgument( item, "item" );
                _currentFeedItems.Add( item.Id );

            if ( IsDeletedItem( item ) )
            {
                item.ClearProperties();
                return;
            }
            try 
            {

                int feedIndex = _feed.GetIntProp( Props.LastItemIndex );
                item.SetProp( Props.IndexInFeed, feedIndex + 1 );
                _feed.SetProp( Props.LastItemIndex, feedIndex + 1 );
                _feed.AddLink( Props.RSSItem, item );

                SetItemDate( item );
                SetAuthor( item );
                ExtractLinksAndReplies( item );
                SetCommentLinks( item );
                AssignFeedCategories( item );

                item.SetProp( Core.Props.IsUnread, true );
                item.SetProp( Core.Props.LongBodyIsHTML, true );
                item.SetProp( Props.DownloadDate, DateTime.Now );

                item.EndUpdate();
                linksCount = item.GetLinksTo( Props.RSSFeedResource, Props.RSSItem ).Count;
            }
            finally
            {
                _nextItem = null;
                if( linksCount != 1 )
                    throw new ApplicationException( "Feed-Post linkage violation: amount of links exceeds 1 = " + linksCount );
                if( item.GetIntProp( Props._propFake ) != _feed.Id )
                    throw new ApplicationException( "Feed-Post linkage violation" );
            }

            Core.TextIndexManager.QueryIndexing( item.Id );
            Core.FilterEngine.ExecRules( StandardEvents.ResourceReceived, item );
        }

        private void  SetItemDate( IResource item )
        {
            if ( !item.HasProp( Core.Props.Date ) )
            {
                if ( item.HasProp( Props.DateModified ) )
                {
                    item.SetProp( Core.Props.Date, item.GetDateProp( Props.DateModified ) );
                }
                else if ( _feed.HasProp( Props.PubDate ) )
                {
                    item.SetProp( Core.Props.Date, _feed.GetDateProp( Props.PubDate ) );
                }
                else
                {
                    item.SetProp( Core.Props.Date, _parseDate );
                }
            }
        }

        private void  SetAuthor( IResource item )
        {
            if ( !item.HasProp( Core.ContactManager.Props.LinkFrom ) )
            {
                IResourceList authorList = _feed.GetLinksOfType( "Contact", Props.Weblog );
                if ( authorList.Count > 0 )
                {
                    item.AddLink( Core.ContactManager.Props.LinkFrom, authorList[ 0 ] );
                }
                else
                {
                    item.AddLink( Core.ContactManager.Props.LinkFrom, _feed );
                }
            }
            else
            {
                //  Author of the feed item is linked as the contact resource.
                //  Set it as feed author if it is not set yet.
                IResource author = item.GetLinkProp( Core.ContactManager.Props.LinkFrom );
                if ( !_feed.HasProp( Props.Author ) )
                {
                    _feed.SetProp( Props.Author, author.DisplayName );
                }
            }
        }

        private void  SetCommentLinks( IResource item )
        {
            if ( _commentItem != null )
            {
                item.AddLink( Props.ItemComment, _commentItem );
            }
            if ( _commentFeed != null )
            {
                item.AddLink( Props.FeedComment, _commentFeed );
            }
        }

        private void AssignFeedCategories( IResource item )
        {
            IResourceList feedCategs = _feed.GetLinksOfType( "Category", "Category" );
            foreach( IResource category in feedCategs )
            {
                item.AddLink( "Category", category );
            }
        }

        private IResource PrepareSubjectAndBody( IResource item )
        {
            if ( item.GetPropText( Core.Props.Subject ).Length == 0 )
            {
                CreateDefaultSubject( item );
            }

            int propId = item.HasProp( Core.Props.LongBody ) ? Core.Props.LongBody : Props.Summary;
            string longBody = item.GetPropText( propId );
            string subject = item.GetPropText( Core.Props.Subject );

            //  First try to find the duplicate without transformations of
            //  possible relative links sinch they are rare.
            IResource candidate = FindByHash( item, subject, longBody );
            if ( candidate == null )
            {
                string fixedBody = longBody;
                string baseUrl = item.GetPropText( Props.LinkBase );
                if ( baseUrl.Length == 0 )
                {
                    baseUrl = _feed.GetPropText( Props.URL );
                }
                if ( baseUrl.Length > 0 )
                {
                    fixedBody = HtmlTools.FixRelativeLinks( longBody, baseUrl );
                }

                if( fixedBody.Equals( longBody ) )
                {
                    UpdateBody( item, longBody, subject );
                }
                else
                {
                    UpdateBody( item, fixedBody, subject );
                    candidate = FindByHash( item, subject, fixedBody );
                }
            }

            return candidate;
        }

        private IResource FindByHash( IResource item, string subject, string longBody )
        {
            int hash = Utils.GetHashCodeInLowerCase( subject, longBody );
            item.SetProp( Props.RssLongBodyCRC, hash );
            IResourceList list = Core.ResourceStore.FindResources( null, Props.RssLongBodyCRC, hash );
            if( list.Count > 0 )
            {
                list = list.Intersect( _feed.GetLinksFrom( null, Props.RSSItem ), true );
                foreach ( IResource candidate in list.ValidResources )
                {
                    if ( subject.Equals( candidate.GetPropText( Core.Props.Subject ) ) && 
                        longBody.Equals( candidate.GetPropText( Core.Props.LongBody ) ) )
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private static void  UpdateBody( IResource item, string longBody, string subject )
        {
            item.SetProp( Core.Props.LongBody, longBody );

            int size = longBody.Length;
            if ( size == 0 )
                size = subject.Length;

            item.SetProp( Core.Props.Size, size );
        }

        private static void UpdateBodyAndSize( IResource fromItem, IResource toItem )
        {
            int propId = fromItem.HasProp( Core.Props.LongBody ) ? Core.Props.LongBody : Props.Summary;
            string body = fromItem.GetPropText( propId );

            toItem.SetProp( Core.Props.LongBody, body );
            toItem.SetProp( Props.RssLongBodyCRC, fromItem.GetProp( Props.RssLongBodyCRC ) );

            int size = body.Length;
            if ( size == 0 )
                size = toItem.GetPropText( Core.Props.Subject ).Length;

            toItem.SetProp( Core.Props.Size, size );
        }

        private IResource GetExistingItem( IResource item )
        {
            IResource oldItem = GetSamePropItem( item, Props.GUID, true );
            if ( oldItem == null )
            {
                if ( !item.HasProp( Props.GUID ) )
                {
                    bool hasLink = GetUniqueLinks() && item.HasProp( Props.Link );
                    if ( hasLink )
                    {
                        oldItem = GetSamePropItem( item, Props.Link, false );
                        if ( oldItem != null && Settings.Trace )
                        {
                            Trace.WriteLine( "Found item with same link " + item.GetPropText( Props.Link ) );
                        }
                    }
                    else if ( item.HasProp( Core.Props.Date ) )
                    {
                        oldItem = GetSamePropItem( item, Core.Props.Date, false );
                        if ( oldItem != null && Settings.Trace )
                        {
                            Trace.WriteLine( "Found item with same date " + item.GetPropText( Core.Props.Date ) );
                        }
                    }
                    else
                    {
                        oldItem = GetSamePropItem( item, Core.Props.Subject, false ); // the LongBody is not searchable
                        if ( oldItem != null && Settings.Trace )
                        {
                            Trace.WriteLine( "Found item with same subject " + item.GetPropText( Core.Props.Subject ) );
                        }
                    }
                }
            }
            else
            {
                if( Settings.Trace ) 
                {
                    Trace.WriteLine( "Found item with same GUID " + item.GetPropText( Props.GUID ) );
                }
            }

            IResource candidate = PrepareSubjectAndBody( item );
            
            if ( oldItem == null && !_allowEqualPosts )
            {
                oldItem = candidate;
                if ( candidate != null && Settings.Trace )
                {
                    Trace.WriteLine( "Found item by CRC " + item.GetPropText( Props.RssLongBodyCRC ) );
                }
            }

            return oldItem;
        }

        private IResource GetSamePropItem( IResource item, int propId, bool caseSensitive )
        {
            if ( item.HasProp( propId ) )
            {
                object itemProp = item.GetProp( propId );
                IResourceList list = _store.FindResources( "RSSItem", propId, itemProp );
                list = list.Intersect( _feed.GetLinksFrom( null, Props.RSSItem ), true );
                foreach ( IResource samePropItem in list )
                {
                    if ( caseSensitive && !samePropItem.GetProp( propId ).Equals(  itemProp ) )
                    {
                        continue;
                    }

                    // two items which are present in the feed at the same time are never duplicates
                    if ( samePropItem.Id != item.Id && !_currentFeedItems.Contains( samePropItem.Id ) )
                    {
                        return samePropItem;
                    }
                }
            }
            return null;
        }

        private static void CreateDefaultSubject( IResource item )
        {
            const int MAX_DESC_LENGTH = 100;
            if ( item.HasProp( Core.Props.LongBody ) )
            {
                string subj = HtmlTools.ReplaceLineBreaks( item.GetPropText( Core.Props.LongBody ) );

                subj = HtmlTools.StripHTML( subj ).Trim();

                int lineBreakIndex = subj.IndexOf( "\n" );
                if ( lineBreakIndex != -1 )
                {
                    subj = subj.Substring( 0, lineBreakIndex );
                }
                subj = HtmlTools.SafeHtmlDecode( subj );
                subj = NormalizeWhiteSpace( subj );

                if ( subj.Length > MAX_DESC_LENGTH )
                {
                    int pos = MAX_DESC_LENGTH;
                    while ( pos >= 0 && subj[ pos ] != ' ' )
                    {
                        pos--;
                    }
                    while ( pos >= 0 && subj[ pos ] == ' ' )
                    {
                        pos--;
                    }

                    if ( pos > 0 )
                    {
                        subj = subj.Substring( 0, pos + 1 ) + "...";
                    }
                    else
                    {
                        subj = subj.Substring( 0, MAX_DESC_LENGTH );
                    }
                }
                item.SetProp( Core.Props.Subject, subj );
            }
        }

        private static void ExtractLinksAndReplies( IResource item )
        {
            string link = item.GetStringProp( Props.Link );
            if ( link != null )
            {
                IResourceList replies = Core.ResourceStore.FindResources( "RSSItem", Props.LinkList, link );
                foreach ( IResource reply in replies )
                {
                    if ( !item.HasLink( Props.LinkedPost, reply ) )
                    {
                        reply.AddLink( Props.LinkedPost, item );
                    }
                }
            }

            IStringList linkList = item.GetStringListProp( Props.LinkList );
            AddLinkMatches( linkList, item, _rxLink );
            AddLinkMatches( linkList, item, _rxLink2 );
        }

        private static void AddLinkMatches( IStringList linkList, IResource item, Regex rxLink )
        {
            string body = item.GetPropText( Core.Props.LongBody );
            if ( body.Length == 0 )
            {
                return;
            }

            foreach ( Match m in rxLink.Matches( body ) )
            {
                string link = m.Groups[ 1 ].Value.Trim();
                linkList.Add( link );

                IResourceList repliesTo = Core.ResourceStore.FindResources( "RSSItem", Props.Link, link );
                foreach ( IResource res in repliesTo )
                {
                    if ( !res.HasLink( Props.LinkedPost, item ) )
                    {
                        item.AddLink( Props.LinkedPost, res );
                    }
                }
            }
        }

        private bool IsDeletedItem( IResource item )
        {
            string md5 = GetRSSItemMD5( item );
            if ( _deletedItems.Contains( md5 ) )
            {
                _newDeletedItems.Add( md5 );
                return true;
            }
            return false;
        }

        public static string GetRSSItemMD5( IResource item )
        {
            Debug.Assert( item.Type == "RSSItem" );
            string body = item.GetPropText( Core.Props.Subject ) + item.GetPropText( Core.Props.LongBody );
            MD5 md5 = MD5.Create();
            byte[] md5hash = md5.ComputeHash( Encoding.UTF8.GetBytes( body ) );
            return Convert.ToBase64String( md5hash );
        }

        private static string NormalizeWhiteSpace( string s )
        {
            StringBuilder result = StringBuilderPool.Alloc();
            try 
            {
                char lastChar = '\0';
                for ( int i = 0; i < s.Length; i++ )
                {
                    char c = s[ i ];
                    if ( c == '\n' || c == '\r' || c == '\t' )
                    {
                        c = ' ';
                    }
                    if ( c != ' ' || lastChar != ' ' )
                    {
                        lastChar = c;
                        result.Append( c );
                    }
                }
                return result.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( result );
            }
        }
    }
}