/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Text;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Nntp
{
    /// <summary>
    /// Business object encapsulating features and properties of a newsgroup resource
    /// </summary>
    internal class NewsgroupResource: NewsTreeNode
    {
        public NewsgroupResource( IResource group )
            : base( group )
        {
            if( group == null )
            {
                throw new ArgumentNullException( "group" );
            }
            if( group.Type != NntpPlugin._newsGroup )
            {
                throw new ArgumentException( "NewsgroupResource cannot be initialized with resource of type different from NewsGroup" );
            }
        }

        public string Name
        {
            get
            {
                return Resource.GetPropText( Core.Props.Name );
            }
            set
            {
                Resource.SetProp( Core.Props.Name, value );
            }
        }


        public void InvalidateDisplayName()
        {
            InvalidateDisplayName( new ServerResource( Server ).AbbreviateLevel );
        }

        public void InvalidateDisplayName( int abbreviateLevel )
        {
            StringBuilder builder = StringBuilderPool.Alloc();
            try 
            {
                InvalidateDisplayName( abbreviateLevel, builder );
            }
            finally
            {
                StringBuilderPool.Dispose( builder );
            }
        }

        private void InvalidateDisplayName( int abbreviateLevel, StringBuilder builder )
        {
            string name = Resource.GetPropText( Core.Props.Name );
            try
            {
                if( UserDisplayName.Length != 0 && UserDisplayName.ToLower() != name.ToLower() )
                {
                    builder.Append( UserDisplayName );
                }
                else
                {
                    if( abbreviateLevel == 0 )
                    {
                        builder.Append( name );
                    }
                    else
                    {
                        string[] parts = name.Split( '.' );
                        for( int i = 0; i < parts.Length; ++i )
                        {
                            string part = parts[ i ];
                            if( part.Length > 0 )
                            {
                                if( i > 0 )
                                {
                                    builder.Append( '.' );
                                }
                                builder.Append( ( i < abbreviateLevel ) ? part.Substring( 0, 1 ) : part );
                            }
                        }
                    }
                }
                if( IsSubscribed )
                {
                    Resource.SetProp( NntpPlugin._propNewsSortOrder, 0 );
                }
                else
                {
                    Resource.SetProp( NntpPlugin._propNewsSortOrder, 1 );
                    builder.Append( " (unsubscribed)" );
                }
                DisplayName = builder.ToString();
            }
            finally
            {
                builder.Length = 0;
            }
        }

        public IResource Server
        {
            get
            {
                if( _server == null )
                {
                    IResource server = Parent;
                    while( server != null && server.Type != NntpPlugin._newsServer )
                    {
                        server = new NewsTreeNode( server ).Parent;
                    }
                    _server = server;
                }
                if( _server != null && _server.IsDeleted )
                {
                    _server = null;
                }
                return _server;
            }
        }

        public bool IsSubscribed
        {
            get
            {
                IResource server = Server;
                if( server == null )
                {
                    return false;
                }
                return server.GetStringListProp( NntpPlugin._propSubscribedNewsgroupList ).IndexOf( Name ) >= 0;
            }
        }

        public bool IsNew
        {
            get
            {
                IResource server = Server;
                if( server == null )
                {
                    return false;
                }
                return server.GetStringListProp( NntpPlugin._propNewNewsgroupList ).IndexOf( Name ) >= 0;
            }
        }

        public int CountToDownloadAtTime
        {
            get
            {
                IResource server = Server;
                if( server != null )
                {
                    return new ServerResource( Server ).CountToDownloadAtTime;
                }
                return Settings.ArticlesPerGroup;
            }
        }

        public int FirstArticle
        {
            get
            {
                int result = Resource.GetIntProp( NntpPlugin._propFirstArticle );
                if( result == 0 )
                {
                    result = Int32.MaxValue;
                }
                return result;
            }
            set
            {
                if( FirstArticle > value )
                {
                    Resource.SetProp( NntpPlugin._propFirstArticle, value );
                }
            }
        }

        public int LastArticle
        {
            get
            {
                return Resource.GetIntProp( NntpPlugin._propLastArticle );
            }
            set
            {
                if( LastArticle < value )
                {
                    Resource.SetProp( NntpPlugin._propLastArticle, value );
                }
            }
        }

        public bool BelongsToServer( IResource server )
        {
            return new ServerResource( server ).ContainsGroup( Name );
        }

        public static bool AllUnsubscribed( IResourceList groups )
        {
            bool allUnsubscribed = true;
            foreach( IResource group in groups )
            {
                if( new NewsgroupResource( group ).IsSubscribed )
                {
                    allUnsubscribed = false;
                    break;
                }
            }
            return allUnsubscribed;
        }
        
        private IResource       _server;
    }
}