/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Nntp
{
    /// <summary>
    /// Business object encapsulating features and properties of a newsserver resource
    /// </summary>
    internal class ServerResource: NewsTreeNode
    {
        public ServerResource( IResource server )
            : base( server )
        {
            if( server == null )
            {
                throw new ArgumentNullException( "server" );
            }
            if( server.Type != NntpPlugin._newsServer )
            {
                throw new ArgumentException( "ServerResource cannot be initialized with resource of type different from NewsServer" );
            }
        }

        #region server settings

        public string Name
        {
            get { return Resource.GetPropText( Core.Props.Name ); }
            set
            {
                if( !Resource.IsDeleted )
                {
                    if( Name != value )
                    {
                        Resource.SetProp( Core.Props.Name, value );
                    }
                }
            }
        }

        public int Port
        {
            get
            {
                int port = Resource.GetIntProp( NntpPlugin._propPort );
                if( port == 0 )
                {
                    port = 119;
                }
                return port;
            }
            set
            {
                if( !Resource.IsDeleted )
                {
                    if( Port != value )
                    {
                        Resource.SetProp( NntpPlugin._propPort, value );
                    }
                }
            }
        }

        public string UserEmailAddress
        {
            get
            {
                string emailAddress = Resource.GetPropText( NntpPlugin._propEmailAddress );
                if( emailAddress.Length == 0 )
                {
                    emailAddress = Core.ContactManager.MySelf.DefaultEmailAddress;
                    if( emailAddress == null )
                    {
                        emailAddress = string.Empty;
                    }
                }
                return emailAddress;
            }
            set
            {
                if( !Resource.IsDeleted )
                {
                    Resource.SetProp( NntpPlugin._propEmailAddress, value );
                }
            }
        }

        public static void AbbreviateLevelChanged( IResource resource, int propId, object oldValue, object newValue )
        {
            if ( propId == NntpPlugin._propAbbreviateLevel )
            {
                new ServerResource( resource ).SetAbbreviateLevel( (int)newValue );
            }
        }

        protected void SetAbbreviateLevel( int value )
        {
            if( Resource.IsDeleted )
            {
                return;
            }
            IResourceList groups = Groups;
            foreach( IResource group in groups )
            {
                new NewsgroupResource( group ).InvalidateDisplayName( value );
            }
            Resource.SetProp( NntpPlugin._propAbbreviateLevel, value );
        }

        public int AbbreviateLevel
        {
            get { return Resource.GetIntProp( NntpPlugin._propAbbreviateLevel ); }
            set
            {
                if( AbbreviateLevel != value )
                {
                    SetAbbreviateLevel( value );
                }
            }
        }

        public string LoginName
        {
            get { return Resource.GetPropText( NntpPlugin._propUsername ); }
            set
            {
                if( !Resource.IsDeleted )
                {
                    if( LoginName != value )
                    {
                        Resource.SetProp( NntpPlugin._propUsername, value );
                    }
                }
            }
        }

        public string Password
        {
            get { return Resource.GetPropText( NntpPlugin._propPassword ); }
            set
            {
                if( !Resource.IsDeleted )
                {
                    if( Password != value )
                    {
                        Resource.SetProp( NntpPlugin._propPassword, value );
                    }
                }
            }
        }

        public bool SSL3Enabled
        {
            get { return Resource.HasProp( NntpPlugin._propSsl3Enabled ); }
            set
            {
                if( !Resource.IsDeleted )
                {
                    Resource.SetProp( NntpPlugin._propSsl3Enabled, value );
                }
            }
        }

        public int CountToDownloadAtTime
        {
            get
            {
                int result = Resource.GetIntProp( NntpPlugin._propCountToDownloadAtTime );
                if( result <= 0 )
                {
                    result = Settings.ArticlesPerGroup;
                }
                return result;
            }
            set
            {
                if( !Resource.IsDeleted )
                {
                    if( CountToDownloadAtTime != value )
                    {
                        Resource.SetProp( NntpPlugin._propCountToDownloadAtTime, value );
                    }
                }
            }
        }

        public bool DeliverOnStartup
        {
            get
            {
                return GetBoolOption( NntpPlugin._propDeliverOnStartup, Settings.DeliverOnStartup );
            }
            set
            {
                if( !Resource.IsDeleted )
                {
                    if( DeliverOnStartup != value )
                    {
                        Resource.SetProp( NntpPlugin._propDeliverOnStartup, ( value ) ? 1 : -1 );
                    }
                }
            }
        }

        public int DeliverFreq
        {
            get
            {
                int prop = Resource.GetIntProp( NntpPlugin._propDeliverFreq );
                if( prop == 0 )
                {
                    return Settings.DeliverNewsPeriod;
                }
                if( prop < 0 )
                {
                    return 0;
                }
                return prop;
            }
            set
            {
                if( !Resource.IsDeleted )
                {
                    if( DeliverFreq != value )
                    {
                        Resource.SetProp( NntpPlugin._propDeliverFreq, ( value > 0 ) ? value : -1 );
                    }
                }
            }
        }

        public bool MarkFromMeAsRead
        {
            get
            {
                return GetBoolOption( NntpPlugin._propMarkFromMeAsRead, Settings.MarkFromMeAsRead );
            }
            set
            {
                if( !Resource.IsDeleted )
                {
                    if( MarkFromMeAsRead != value )
                    {
                        Resource.SetProp( NntpPlugin._propDownloadBodiesOnDeliver, ( value ) ? 1 : -1 );
                    }
                }
            }
        }

        public bool DownloadBodiesOnDeliver
        {
            get
            {
                return GetBoolOption( NntpPlugin._propDownloadBodiesOnDeliver, Settings.DownloadBodiesOnDeliver );
            }
            set
            {
                if( !Resource.IsDeleted )
                {
                    if( DownloadBodiesOnDeliver != value )
                    {
                        Resource.SetProp( NntpPlugin._propDownloadBodiesOnDeliver, ( value ) ? 1 : -1 );
                    }
                }
            }
        }

        public bool DownloadBodyOnSelection
        {
            get
            {
                return GetBoolOption( NntpPlugin._propDownloadBodyOnSelection, Settings.DownloadBodyOnSelection );
            }
            set
            {
                if( !Resource.IsDeleted )
                {
                    if( DownloadBodyOnSelection != value )
                    {
                        Resource.SetProp( NntpPlugin._propDownloadBodyOnSelection, ( value ) ? 1 : -1 );
                    }
                }
            }
        }

        public string Charset
        {
            get
            {
                string result = Resource.GetPropText( Core.FileResourceManager.PropCharset );
                if( result.Length == 0 )
                {
                    result = Settings.Charset;
                }
                return result;
            }
            set
            {
                if( !Resource.IsDeleted )
                {
                    if( Charset != value )
                    {
                        Resource.SetProp( Core.FileResourceManager.PropCharset, value );
                    }
                }
            }
        }

        public string MailFormat
        {
            get
            {
                string result = Resource.GetPropText( NntpPlugin._propMailFormat );
                if( result.Length == 0 )
                {
                    result = Settings.Format;
                }
                return result;
            }
            set
            {
                if( !Resource.IsDeleted )
                {
                    if( MailFormat != value )
                    {
                        Resource.SetProp( NntpPlugin._propMailFormat, value );
                    }
                }
            }
        }

        public string MIMETextEncoding
        {
            get
            {
                string result = Resource.GetPropText( NntpPlugin._propMIMETextEncoding );
                if( result.Length == 0 )
                {
                    result = Settings.EncodeTextWith;
                }
                return result;
            }
            set
            {
                if( !Resource.IsDeleted )
                {
                    if( MIMETextEncoding != value )
                    {
                        Resource.SetProp( NntpPlugin._propMIMETextEncoding, value );
                    }
                }
            }
        }

        public bool OverrideSig
        {
            get
            {
                return Resource.HasProp( NntpPlugin._propOverrideSigSettings );
            }
            set
            {
                if( !Resource.IsDeleted )
                {
                    if( value )
                    {
                        Resource.SetProp( NntpPlugin._propOverrideSigSettings, true );
                    }
                    else
                    {
                        Resource.DeleteProp( NntpPlugin._propOverrideSigSettings );
                        Resource.DeleteProp( NntpPlugin._propUseSignature );
                        Resource.DeleteProp( NntpPlugin._propMailSignature );
                        Resource.DeleteProp( NntpPlugin._propReplySignaturePosition );
                    }
                }
            }
        }

        public bool UseSignature
        {
            get
            {
                if( Resource.HasProp( NntpPlugin._propUseSignature ) )
                {
                    return (Resource.GetIntProp( NntpPlugin._propUseSignature ) > 0);
                }
                return QuoteSettings.Default.UseSignature;
            }
            set
            {
                if( !Resource.IsDeleted && OverrideSig )
                {
                    Resource.SetProp( NntpPlugin._propUseSignature, ( value ) ? 1 : -1  );
                }
                else
                    throw new NotSupportedException( "Setting can not be set without \"Override Signature\" option." );
            }
        }

        public string MailSignature
        {
            get
            {
                if( Resource.HasProp( NntpPlugin._propMailSignature ) )
                {
                    return Resource.GetPropText( NntpPlugin._propMailSignature  );
                }
                return QuoteSettings.Default.Signature;
            }
            set
            {
                if( !Resource.IsDeleted && OverrideSig )
                {
                    Resource.SetProp( NntpPlugin._propMailSignature, value );
                }
                else
                    throw new NotSupportedException( "Setting can not be set without \"Override Signature\" option." );
            }
        }

        public SignaturePosition ReplySignaturePosition
        {
            get
            {
                if( Resource.HasProp( NntpPlugin._propReplySignaturePosition ) )
                {
                    return (SignaturePosition) Resource.GetIntProp( NntpPlugin._propReplySignaturePosition );
                }
                else
                {
                    return QuoteSettings.Default.SignatureInReplies;
                }
            }
            set
            {
                if( !Resource.IsDeleted && OverrideSig )
                {
                    Resource.SetProp( NntpPlugin._propReplySignaturePosition, (int) value );
                }
                else
                    throw new NotSupportedException( "Setting can not be set without \"Override Signature\" option." );
            }
        }
        #endregion

        public int AllGroupsCount
        {
            get
            {
                return Resource.GetStringListProp( NntpPlugin._propNewsgroupList ).Count;
            }
        }

        public int SubscribedGroupsCount
        {
            get
            {
                return Resource.GetStringListProp( NntpPlugin._propSubscribedNewsgroupList ).Count;
            }
        }

        public int NewGroupsCount
        {
            get
            {
                return Resource.GetStringListProp( NntpPlugin._propNewNewsgroupList ).Count;
            }
        }

        public bool ContainsGroup( string group )
        {
            IResourceList servers = Core.ResourceStore.FindResources( null, NntpPlugin._propNewsgroupList, group );
            return servers.IndexOf( Resource ) >= 0;
        }

        public void AddGroup( string group )
        {
            if( !Resource.IsDeleted )
            {
                if( !ContainsGroup( group ) )
                {
                    Resource.GetStringListProp( NntpPlugin._propNewsgroupList ).Add( group );
                    Resource.GetStringListProp( NntpPlugin._propNewNewsgroupList ).Add( group );
                }
            }
        }

        public void SubscribeToGroup( string group )
        {
            if( !Resource.IsDeleted )
            {
                if( ContainsGroup( group ) )
                {
                    IResourceList servers = Core.ResourceStore.FindResources(
                        null, NntpPlugin._propSubscribedNewsgroupList, group );
                    if( servers.IndexOf( Resource ) < 0 )
                    {
                        Resource.GetStringListProp( NntpPlugin._propSubscribedNewsgroupList ).Add( group );
                    }
                }
            }
        }

        public void UnsubscribeFromGroup( string group )
        {
            if( !Resource.IsDeleted )
            {
                Resource.GetStringListProp( NntpPlugin._propSubscribedNewsgroupList ).Remove( group );
                Resource.GetStringListProp( NntpPlugin._propNewNewsgroupList ).Remove( group );
            }
        }

        public void DisposeNewsgroupLists()
        {
            if( !Resource.IsDeleted )
            {
                Resource.GetStringListProp( NntpPlugin._propNewsgroupList ).Dispose();
                Resource.GetStringListProp( NntpPlugin._propNewNewsgroupList ).Dispose();
                Resource.GetStringListProp( NntpPlugin._propSubscribedNewsgroupList ).Dispose();
            }
        }

        public void ClearNewGroups()
        {
            if( !Resource.IsDeleted )
            {
                Resource.DeleteProp( NntpPlugin._propNewNewsgroupList );
            }
        }

        public DateTime LastUpdateTime
        {
            get { return Resource.GetDateProp( NntpPlugin._propLastUpdated ); }
            set { new ResourceProxy( Resource ).SetPropAsync( NntpPlugin._propLastUpdated, value ); }
        }

        private bool GetBoolOption( int prop, Setting setting )
        {
            int propValue = Resource.GetIntProp( prop );
            if( propValue == 0 )
            {
                return (bool)setting.Value;
            }
            if( propValue < 0 )
            {
                return false;
            }
            return true;
        }
    }
}
