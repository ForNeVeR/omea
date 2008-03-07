/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Collections;
using System.IO;
using System.Text;
using System.Xml;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin
{
    /**
     * Exports and imports hierarchical lists of feeds in OPML format.
     */

	public class OPMLProcessor
	{
        private static readonly IntHashTable   _exportedFolders = new IntHashTable();

        public static void Export( IResource rootGroup, IntArrayList listFeedIds, string fileName )
        {
            XmlTextWriter writer = new XmlTextWriter( fileName, new UTF8Encoding( false ) );
            writer.Formatting = Formatting.Indented;
            try
            {
                CollectExportableFolders( listFeedIds );
                writer.WriteStartDocument();
                writer.WriteStartElement( "opml" );
                writer.WriteAttributeString( "version", "1.0" );
                writer.WriteStartElement( "head" );
                writer.WriteElementString( "title", "Feed Subscriptions" );
                writer.WriteEndElement();
                writer.WriteStartElement( "body" );
                ExportGroup( writer, listFeedIds, rootGroup );

                writer.WriteEndDocument();
            }
            finally
            {
                writer.Close();
            }
        }

        public static void Export( IResource rootGroup, string fileName )
        {
            Export( rootGroup, null, fileName );
        }

        /**
         * Exports a feed group and its containing groups and feeds to the XML writer.
         */

        private static void ExportGroup( XmlTextWriter writer, IntArrayList feedsIds, IResource group )
        {
            //  OM-13643, workaround missed resources.
            IResourceList descendants = group.GetLinksTo( null, "Parent" );
            foreach( IResource res in descendants.ValidResources )
            {
                if ( res.Type == "RSSFeed" && IsFeedExportable( feedsIds, res ) )
                {
                    ExportFeed( writer, res );
                }
                else
                if ( res.Type == "RSSFeedGroup" && IsFeedGroupExportable( feedsIds, res ) )
                {
                    writer.WriteStartElement( "outline" );
                    writer.WriteAttributeString( "text", res.GetStringProp( "Name" ) );
                    ExportGroup( writer, feedsIds, res );
                    writer.WriteEndElement();
                }
            }
        }

        /**
         * Exports the data for a single feed to the XML writer.
         */

        private static void ExportFeed( XmlWriter writer, IResource feed )
        {
            writer.WriteStartElement( "outline" );
            //  "text" attribute is required by OPML 2.0, while "title" is required
            //  by several OPML aggregators.
            WriteFeedProp( writer, "title",       feed, Core.Props.Name );
            WriteFeedProp( writer, "text",        feed, Core.Props.Name );
            WriteFeedProp( writer, "description", feed, Props.Description );
            WriteFeedProp( writer, "xmlUrl",      feed, Props.URL );
            WriteFeedProp( writer, "htmlUrl",     feed, Props.HomePage );
            WriteFeedProp( writer, "language",    feed, Props.Language );
            //  By convention, it is written "rss" even for ATOM-based feeds.
            writer.WriteAttributeString( "type", "rss" );
            writer.WriteEndElement();
        }

        private static void WriteFeedProp( XmlWriter writer, string attr, IResource feed, int propId )
        {
            string propValue = feed.GetPropText( propId );
            if( propValue.Length > 0 )
            {
//                if( HttpTools.HttpReader.SupportedProtocol( propValue ) == HttpReader.URLType.Web )
//                    propValue = HttpUtility.UrlEncode( propValue );
                writer.WriteAttributeString( attr, propValue );
            }
        }

        public static bool Import( TextReader streamReader, IResource rootGroup, bool addToWorkspace )
        {
            return Import( streamReader, rootGroup, addToWorkspace, null );
        }

        public static bool Import( TextReader streamReader, IResource rootGroup, bool addToWorkspace, Hashtable namespaces )
        {
            bool hasOPML = false;
            bool lastOutlineIsGroup = false;
            try
            {
                NameTable nt = new NameTable();
                // Fill name table
                if( namespaces != null )
                {
                    foreach( string key in namespaces.Keys )
                    {
                        nt.Add( key );
                        nt.Add( namespaces[ key] as string );
                    }
                }
                XmlNamespaceManager nsmgr = new LooseNSManager( nt, namespaces );
                XmlParserContext ctx = new XmlParserContext( nt, nsmgr, null, XmlSpace.None);
                string           xml = Utils.StreamReaderReadToEnd( streamReader );
                XmlTextReader    reader = new XmlTextReader( xml, XmlNodeType.Document, ctx );

                try
                {
                    IResource curGroup = rootGroup;
                    while( reader.Read() )
                    {
                        if ( reader.NodeType == XmlNodeType.Element )
                        {
                            if ( reader.LocalName == "opml" )
                                hasOPML = true;
                            else
                            if ( hasOPML && reader.LocalName == "outline" )
                            {
                                if ( reader.MoveToAttribute( "xmlUrl" ) || reader.MoveToAttribute( "xmlurl" ) )
                                {
                                    ProcessFeed( reader, curGroup, addToWorkspace );
                                }
                                else if ( reader.MoveToAttribute( "text" ) || reader.MoveToAttribute( "title" ) )
                                {
                                    IResource group = FindOrCreateGroup( curGroup, reader.Value );
                                    reader.MoveToElement();
                                    if ( !reader.IsEmptyElement )
                                    {
                                        curGroup = group;
                                        lastOutlineIsGroup = true;
                                    }
                                }
                            }
                        }
                        else if ( reader.NodeType == XmlNodeType.EndElement )
                        {
                            if ( reader.LocalName == "outline" )
                            {
                                if ( lastOutlineIsGroup )
                                {
                                    curGroup = curGroup.GetLinkProp( "Parent" );
                                    lastOutlineIsGroup = true;
                                }
                            }
                        }
                    }
                }
                finally
                {
                    reader.Close();
                }
            }
            finally
            {
                streamReader.Close();
            }
            return hasOPML;
        }

        private static void  ProcessFeed( XmlReader reader, IResource group, bool addToWorkspace )
        {
            ImportFeed( reader, group, addToWorkspace );
            reader.MoveToElement();
            if ( !reader.IsEmptyElement )
            {
                while( reader.Read() && !IsOutlineEndTag( reader ))
                {}
            }
        }

        private static bool IsOutlineEndTag( XmlReader reader )
        {
            return ( reader.NodeType == XmlNodeType.EndElement ) &&
                   ( reader.LocalName == "outline" );
        }

        private static IResource FindOrCreateGroup( IResource parentGroup, string name )
        {
            IResourceList childGroups = parentGroup.GetLinksTo( "RSSFeedGroup", "Parent" );
            foreach( IResource child in childGroups )
            {
                if ( child.GetStringProp( "Name" ) == name )
                {
                    return child;
                }
            }

            return RSSPlugin.CreateFeedGroup( parentGroup, name );
        }

        private static void ImportFeed( XmlReader reader, IResource group, bool addToWorkspace )
        {
            if ( reader.MoveToAttribute( "xmlUrl" ) || reader.MoveToAttribute( "xmlurl" ) )
            {
                IResource existingFeed = Core.ResourceStore.FindUniqueResource( "RSSFeed", "URL", reader.Value );
                if ( existingFeed != null )
                    return;
            }
            else
                return;

            IResource feed = Core.ResourceStore.BeginNewResource( "RSSFeed" );
            feed.SetProp( Props.URL, reader.Value );
            reader.MoveToElement();

            SetFeedProp( feed, reader, Core.Props.Name, "title", "text" );
            SetFeedProp( feed, reader, Props.HomePage, "htmlUrl", "htmlurl" );
            SetFeedProp( feed, reader, Props.Description, "description" );
            SetFeedProp( feed, reader, Props.Language, "language" );

            feed.AddLink( "Parent", group );

            feed.EndUpdate();
            if ( addToWorkspace )
            {
                Core.WorkspaceManager.AddToActiveWorkspace( feed );
            }
        }

        private static void SetFeedProp( IResource feed, XmlReader reader, int feedProp, params string[] opmlProps )
        {
            foreach( string opmlProp in opmlProps )
            {
                if ( reader.MoveToAttribute( opmlProp ) )
                {
                    feed.SetProp( feedProp, reader.Value );
                    reader.MoveToElement();
                    break;
                }
            }
        }

        private static void CollectExportableFolders( IntArrayList listFeedIds )
        {
            _exportedFolders.Clear();

            //  All feeds?
            if( listFeedIds == null )
            {
                IResourceList allFeeds = Core.ResourceStore.GetAllResources( Props.RSSFeedResource );
                listFeedIds = new IntArrayList( allFeeds.ResourceIds );
            }

            foreach( int id in listFeedIds )
            {
                IResource feed = Core.ResourceStore.LoadResource( id );
                IResource parent = feed.GetLinkProp( Core.Props.Parent );
                while( parent != null )
                {
                    _exportedFolders[ parent.Id ] = 1;
                    parent = parent.GetLinkProp( Core.Props.Parent );
                }
            }
        }
        private static bool  IsFeedExportable( IntArrayList feedsIds, IResource feed )
        {
            return ( feedsIds == null ) || ( feedsIds.IndexOf( feed.Id ) != -1 );
        }

        private static bool  IsFeedGroupExportable( IntArrayList feedsIds, IResource group )
        {
            return ( feedsIds == null ) || _exportedFolders.ContainsKey( group.Id );
        }
    }
}
