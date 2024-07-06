// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.MIME;

namespace JetBrains.Omea.Nntp
{
    internal enum ProductType
    {
        Unknown,
        Pro,
        Reader
    }

    internal class ParseTools
    {
        public static string EscapeCaseSensitiveString( string str )
        {
            int length = str.Length;
            StringBuilder strBuilder = new StringBuilder( length );
            for( int i = 0; i < length; ++i )
            {
                char c = str[ i ];
                if( Char.IsUpper( c ) )
                {
                    strBuilder.Append( '\x01' );
                    c = Char.ToLower( c );
                }
                strBuilder.Append( c );
            }
            return strBuilder.ToString();
        }

        public static string UnescapeCaseSensitiveString( string str )
        {
            int length = str.Length;
            StringBuilder strBuilder = new StringBuilder( length );
            for( int i = 0; i < length; ++i )
            {
                char c = str[ i ];
                if( c == '\x01' )
                {
                    c = Char.ToUpper( str[ ++i ] );
                }
                strBuilder.Append( c );
            }
            return strBuilder.ToString();
        }

        public static string ParseMIMEHeader( string header )
        {
            StringBuilder result = StringBuilderPool.Alloc();
            try
            {
                int len = header.Length;

                for( int i = 0; i < len; ++i )
                {
                    char c = header[ i ];
                    if( c == '=' && i < len - 1 && header[ i + 1 ] == '?' )
                    {
                        int mimeStrEnd = i + 1;
                        for( int j = 0; mimeStrEnd > 0 && j < 3; ++j )
                        {
                            mimeStrEnd = header.IndexOf( '?', mimeStrEnd + 1 );
                        }
                        if( mimeStrEnd > 0 && mimeStrEnd < len - 1 && header[ mimeStrEnd + 1 ] == '=' )
                        {
                            result.Append(
                                MIMEParser.DecodeMIMEString( header.Substring( i, mimeStrEnd - i + 2 ) ) );
                            i = mimeStrEnd + 1;
                        }
                        else
                        {
                            result.Append( MIMEParser.DecodeMIMEString( header.Substring( i ) ) );
                            i = len;
                        }
                    }
                    else
                    {
                        result.Append( c );
                    }
                }
                return result.ToString();
            }
            catch
            {
            }
            finally
            {
                StringBuilderPool.Dispose( result );
            }
            return header;
        }

        public static string GenerateArticleId( IResource article, string domainName )
        {
            StringBuilder builder = StringBuilderPool.Alloc();
            try
            {
                builder.Append( '<' );
                builder.AppendFormat( "{0:x4}", Environment.MachineName.ToLower().GetHashCode() );
                builder.AppendFormat( "{0:x4}", article.Id );
                builder.AppendFormat( "{0:x8}", DateTime.Now.Ticks );
                builder.Append( '@' );
                builder.Append( domainName.ToLower() );
                builder.Append( '>' );
                return builder.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( builder );
            }
        }

        public static string NNTPDateString( DateTime date )
        {
            return date.ToUniversalTime().ToString( "yyMMdd HHmmss" ) + " GMT";
        }
    }

    public class NewgroupsSelectPane : ResourceTreeSelectPane
    {
        NewgroupsSelectPane() { _resourceTree.AddNodeFilter( new FoldersFilter() ); }

        public class FoldersFilter : IResourceNodeFilter
        {
            public bool AcceptNode( IResource res, int level )
            {
                return !NewsFolders.IsDefaultFolder( res );
            }
        }
    }

    internal class NewsContactHelper
    {
        public static string RestoreFromField( IResource article )
        {
            string from = string.Empty;
            string email = string.Empty;
            IContactManager contactManager = Core.ContactManager;
            IResource contactName = article.GetLinkProp( "NameFrom" );
            if( contactName != null )
            {
                from = contactName.DisplayName;
                IResource emailAccount = contactName.GetLinkProp( contactManager.Props.LinkEmailAcct );
                if( emailAccount != null )
                {
                    email = emailAccount.DisplayName;
                }
            }
            else
            {
                IResource fromContact = article.GetLinkProp( contactManager.Props.LinkFrom );
                if( fromContact != null )
                {
                    from = fromContact.DisplayName;
                }
                IResource emailAccount = article.GetLinkProp( "EmailAccountFrom" );
                if( emailAccount != null )
                {
                    email = emailAccount.DisplayName;
                }
                else if( article.HasProp( NntpPlugin._propEmailAddress ) )
                {
                    email = article.GetPropText( NntpPlugin._propEmailAddress );
                }
            }
            if( email.Length > 0 )
            {
                from = from + " <" + email + '>';
            }
            return from.Trim();
        }
    }

    internal class NewsArticleHelper
    {
        public static void SetArticleNumber( IResource article, IResource group, string number )
        {
            string numbersString = article.GetPropText( NntpPlugin._propArticleNumbers );
            string[] numbers = numbersString.Split( ';' );
            string groupId = group.Id.ToString();
            foreach( string numberStr in numbers )
            {
                if( numberStr.IndexOf( ':' ) >= 0 && numberStr.Split( ':' )[ 0 ] == groupId )
                {
                    return;
                }
            }
            article.SetProp( NntpPlugin._propArticleNumbers,
                ( numbersString + ';' + groupId + ':' + number.Trim() ).TrimStart( ';' ) );
        }

        public static string GetArticleNumber(  IResource article, IResource group )
        {
            if( group != null )
            {
                string numbersString = article.GetPropText( NntpPlugin._propArticleNumbers );
                string[] numbers = numbersString.Split( ';' );
                string groupId = group.Id.ToString();
                foreach( string numberStr in numbers )
                {
                    if( numberStr.IndexOf( ':' ) >= 0 )
                    {
                        string[] pair = numberStr.Split( ':' );
                        if( pair.Length == 2 && pair[ 0 ] == groupId )
                        {
                            return pair[ 1 ];
                        }
                    }
                }
            }
            return null;
        }

        public static void RemoveArticleNumbers( IResource article )
        {
            article.DeleteProp( NntpPlugin._propArticleNumbers );
        }

        public static bool IsArticleDeleted( string articleId )
        {
            return Core.ResourceStore.FindResources( null, NntpPlugin._propDeletedList, articleId ).Count > 0;
        }
    }

    internal class NewsAttachmentFilter : ILinksPaneFilter
    {
        public bool AcceptLinkType( IResource displayedResource, int propId, ref string displayName )
        {
            return true;
        }

        public bool AcceptLink( IResource displayedResource, int propId, IResource targetResource, ref string linkTooltip )
        {
            if( propId == -NntpPlugin._propAttachment && targetResource.HasProp( Core.Props.Size ) )
            {
                linkTooltip = ( targetResource.GetIntProp( Core.Props.Size ) >> 10 ) + " KB";
            }
            return true;
        }

        public bool AcceptAction( IResource displayedResource, IAction action )
        {
            return true;
        }
    }

    internal class ArticleNewsgroupsFilter : ILinksPaneFilter
    {
        public bool AcceptLinkType( IResource displayedResource, int propId, ref string displayName )
        {
            return true;
        }

        public bool AcceptLink( IResource displayedResource, int propId, IResource targetResource, ref string linkTooltip )
        {
            if( propId == NntpPlugin._propTo && targetResource.Type == NntpPlugin._newsGroup )
            {
                linkTooltip = targetResource.DisplayName;
                IResource server = new NewsgroupResource( targetResource ).Server;
                if( server != null )
                {
                    linkTooltip = linkTooltip + " at " + server.DisplayName;
                }
            }
            return true;
        }

        public bool AcceptAction( IResource displayedResource, IAction action )
        {
            return true;
        }
    }


    public class AttachmentComparer: IResourceComparer, IResourceGroupProvider
    {
        private readonly int _propAttachment = 0;
        private readonly int _propNewsAttachment = 0;

        public AttachmentComparer()
        {
            if ( Core.ResourceStore.PropTypes.Exist( "Attachment" ) )
            {
                _propAttachment = Core.ResourceStore.GetPropId( "Attachment" );
            }
            _propNewsAttachment = Core.ResourceStore.GetPropId( "NewsAttachment" );
        }

        public int CompareResources( IResource r1, IResource r2 )
        {
            return HasAttachment( r1 ).CompareTo( HasAttachment( r2 ) );
        }

        private bool HasAttachment( IResource res )
        {
            return res.HasProp( -_propNewsAttachment ) ||
                ( _propAttachment != 0 && res.HasProp( -_propAttachment ) );
        }

        public string GetGroupName( IResource res )
        {
            return HasAttachment( res ) ? "With Attachments" : "No Attachments";
        }
    }

    internal class DisplayNewsgroupInContextHandler: IDisplayInContextHandler
    {
        public void DisplayResourceInContext( IResource res )
        {
            if( !res.HasProp( Core.Props.Parent ) )
            {
                if( MessageBox.Show( Core.MainWindow, "You are not subscribed to " + res.DisplayName +
                    ". Would you like to subscribe?", "Subscribe to Newsgroup",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question ) == DialogResult.Yes )
                {
                    SubscribeForm.SubscribeToGroups();
                }
            }
            else
            {
                Core.UIManager.BeginUpdateSidebar();
                try
                {
                    if ( !Core.TabManager.ActivateTab( "News" ) )
                    {
                        return;
                    }
                    Core.LeftSidebar.ActivateViewPane( "Newsgroups" );
                }
                finally
                {
                    Core.UIManager.EndUpdateSidebar();
                }
                AbstractViewPane pane = Core.LeftSidebar.GetPane( "Newsgroups" );
                if ( pane != null )
                {
                    pane.SelectResource( res, false );
                }
            }
        }
    }


    internal class NntpRenameHandler : IResourceRenameHandler
    {
        public bool CanRenameResource( IResource res, ref string editText )
        {
            if( res.Type == NntpPlugin._newsGroup || res.Type == NntpPlugin._newsServer ||
                ( res.Type == NntpPlugin._newsFolder && !NewsFolders.IsDefaultFolder( res ) ))
            {
                editText = res.GetStringProp( "_DisplayName" );
                return true;
            }
            return false;
        }

        public bool ResourceRenamed( IResource res, string newName )
        {
            if ( newName.Trim() == string.Empty )
            {
                MessageBox.Show( Core.MainWindow, "Please specify a name." );
                return false;
            }

            if( newName.Length == 0 && res.Type != NntpPlugin._newsGroup )
            {
                return false;
            }
            Core.ResourceAP.QueueJob( JobPriority.Immediate,
                new ResourceRenamedDelegate( ResourceRenamedImpl ), res, newName );
            return true;
        }

        private delegate void ResourceRenamedDelegate( IResource res, string newName );

        private static void ResourceRenamedImpl( IResource res, string newName )
        {
            if( res.IsDeleted )
            {
                return;
            }
            if( res.Type == NntpPlugin._newsServer )
            {
                res.DisplayName = newName;
            }
            else if( res.Type == NntpPlugin._newsGroup )
            {
                NewsgroupResource group = new NewsgroupResource( res );
                group.UserDisplayName = newName;
                group.InvalidateDisplayName();
            }
            else
            {
                res.SetProp( Core.Props.Name, newName );
            }
        }
    }
}
