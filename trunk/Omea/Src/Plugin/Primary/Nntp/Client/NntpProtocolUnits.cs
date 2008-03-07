/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.Net;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Nntp
{
    /// <summary>
    /// nntp authentication
    /// </summary>
    internal class NntpAuthenticateUnit: AsciiProtocolUnit
    {
        public NntpAuthenticateUnit( string username, string password )
        {
            _username = username;
            _password = password;
        }

        public bool Succeeded
        {
            get { return _succeeded; }
        }

        public string ResponseLine
        {
            get { return _responseLine; }
        }

        protected override void Start( AsciiTcpConnection connection )
        {
            _connection = connection;
            AsciiSendLineGetLineUnit sendUsernameUnit =
                new AsciiSendLineGetLineUnit( "authinfo user " + _username );
            sendUsernameUnit.Finished += new AsciiProtocolUnitDelegate( sendUsernameUnit_Finished );
            StartUnit( sendUsernameUnit, connection );
        }

        private void sendUsernameUnit_Finished( AsciiProtocolUnit unit )
        {
            AsciiSendLineGetLineUnit sendUsernameUnit = (AsciiSendLineGetLineUnit) unit;
            if( sendUsernameUnit.LineSent )
            {
                _responseLine = sendUsernameUnit.ResponseLine;
                if( _responseLine != null )
                {
                    if( _responseLine.StartsWith( "381" ) ) // password required
                    {
                        AsciiSendLineGetLineUnit sendPasswordUnit =
                            new AsciiSendLineGetLineUnit( "authinfo pass " + _password );
                        sendPasswordUnit.Finished += new AsciiProtocolUnitDelegate( sendPasswordUnit_Finished );
                        StartUnit( sendPasswordUnit, _connection );
                        return;
                    }
                    if( _responseLine.StartsWith( "281" ) ) // password not required (wonder if this can occur)
                    {
                        _succeeded = true;
                    }
                }
            }
            FireFinished();
        }

        private void sendPasswordUnit_Finished( AsciiProtocolUnit unit )
        {
            AsciiSendLineGetLineUnit sendPasswordUnit = (AsciiSendLineGetLineUnit) unit;
            if( sendPasswordUnit.LineSent )
            {
                _responseLine = sendPasswordUnit.ResponseLine;
                if( _responseLine != null && _responseLine.StartsWith( "281" ) )
                {
                    _succeeded = true;
                }
            }
            FireFinished();
        }

        private string              _username;
        private string              _password;
        private bool                _succeeded;
        private string              _responseLine;
        private AsciiTcpConnection  _connection;
    }

    /// <summary>
    /// downloading group names
    /// </summary>
    internal class NntpDownloadGroupsUnit: AsciiProtocolUnit
    {
        public NntpDownloadGroupsUnit( IResource server, bool refresh, JobPriority priority )
        {
            Interlocked.Increment( ref NntpPlugin._deliverNewsUnitCount );
            _serverResource = new ServerResource( server );
            Core.UIManager.GetStatusWriter( typeof( NntpDownloadGroupsUnit ),
                StatusPane.Network ).ShowStatus( "Downloading groups from " + _serverResource.DisplayName + "..." );
            _priority = priority;
            _nntpCmd = "list";
            if( !refresh )
            {
                DateTime lastUpdated = _serverResource.LastUpdateTime;
                if( lastUpdated > DateTime.MinValue )
                {
                    _nntpCmd = "newgroups " + ParseTools.NNTPDateString( lastUpdated );
                }
            }
            _count = 0;
            _responseChecked = false;
            _groupList = new ArrayList();
            _groupListLock = new SpinWaitLock();
            _flushGroupListDelegate = new MethodInvoker( FlushGroupList );
        }

        /// <summary>
        /// A group downloaded by a unit
        /// </summary>
        public delegate void DroupDownloadedDelegate( string group, NntpDownloadGroupsUnit unit );

        public event DroupDownloadedDelegate GroupDownloaded;

        /// <summary>
        /// Number of currently downloaded groups
        /// </summary>
        public int Count
        {
            get { return _count; }
        }

        public ServerResource Server
        {
            get { return _serverResource; }
        }

        protected override void Start( AsciiTcpConnection connection )
        {
            _connection = connection;
            _enumGroupsUnit = new AsciiSendLineAndApplyMethodUnit(
                _nntpCmd, ".\r\n", new LineDelegate( ProcessGroupLine ) );
            _enumGroupsUnit.Finished += new AsciiProtocolUnitDelegate( enumGroupsUnit_Finished );
            _startUpdateDate = DateTime.Now;
            StartUnit( _enumGroupsUnit, connection );
        }

        protected override void FireFinished()
        {
            FlushGroupList();
            Interlocked.Decrement( ref NntpPlugin._deliverNewsUnitCount );
            if( _enumGroupsUnit != null )
            {
                _enumGroupsUnit.Finished -= new AsciiProtocolUnitDelegate( enumGroupsUnit_Finished );
                FireFinished( _enumGroupsUnit );
            }
            GroupDownloaded = null;
            Core.UIManager.GetStatusWriter( typeof( NntpDownloadGroupsUnit ), StatusPane.Network ).ClearStatus();
            base.FireFinished();
        }

        private void ProcessGroupLine( string line )
        {
            if( _serverResource.Resource.IsDeleted )
            {
                FireFinished();
            }
            else
            {
                if( !_responseChecked )
                {
                    _responseChecked = true;
                    if( !line.StartsWith( "215" ) && !line.StartsWith( "231" ) && !line.StartsWith( "221" ) )
                    {
                        if( line.Length > 4 )
                        {
                            new ResourceProxy( _serverResource.Resource ).SetPropAsync(
                                Core.Props.LastError, line.Substring( 4 ).TrimEnd( '\r', '\n' ) );
                            Core.NetworkAP.QueueJob( new MethodInvoker( _connection.Close ) );
                        }
                        FireFinished();
                    }
                }
                else
                {
                    int index = line.IndexOf( ' ' );
                    if( index < 0 )
                    {
                        index = line.Length;
                        while( index > 0 )
                        {
                            char c = line[ index - 1 ];
                            if( c != '\r' && c != '\n' )
                            {
                                break;
                            }
                            --index;
                        }
                    }
                    if( index > 0 )
                    {
                        string group = line.Substring( 0, index );
                        bool needFlush = false;
                        _groupListLock.Enter();
                        try
                        {
                            _groupList.Add( group );
                            needFlush = _groupList.Count > 16;
                        }
                        finally
                        {
                            _groupListLock.Exit();
                        }
                        if( needFlush )
                        {
                            FlushGroupList();
                        }
                        ++_count;
                        if( GroupDownloaded != null )
                        {
                            GroupDownloaded( group, this );
                        }
                    }
                }
            }
        }

        private void FlushGroupList()
        {
            IAsyncProcessor ap = Core.ResourceAP;
            if( !ap.IsOwnerThread )
            {
                ap.QueueJob( _priority, _flushGroupListDelegate );
            }
            else
            {
                _groupListLock.Enter();
                try
                {
                    foreach( string group in _groupList )
                    {
                        _serverResource.AddGroup( group );
                    }
                    _groupList.Clear();
                }
                finally
                {
                    _groupListLock.Exit();
                }
            }
        }

        private void enumGroupsUnit_Finished( AsciiProtocolUnit unit )
        {
            _serverResource.LastUpdateTime = _startUpdateDate;
            _enumGroupsUnit = null;
            FireFinished();
        }

        private ServerResource                  _serverResource;
        private JobPriority                     _priority;
        private string                          _nntpCmd;
        private AsciiTcpConnection              _connection;
        private AsciiSendLineAndApplyMethodUnit _enumGroupsUnit;
        private int                             _count;
        private bool                            _responseChecked;
        private DateTime                        _startUpdateDate;
        private ArrayList                       _groupList;
        private SpinWaitLock                    _groupListLock;
        private MethodInvoker                   _flushGroupListDelegate;
    }

    /// <summary>
    /// set current group
    /// </summary>
    internal class NntpSetGroupUnit: AsciiProtocolUnit
    {
        public NntpSetGroupUnit( NewsgroupResource group )
        {
            _group = group;
        }

        public string ResponseLine
        {
            get { return _responseLine; }
        }

        protected override void Start( AsciiTcpConnection connection )
        {
            _connection = connection;
            AsciiSendLineGetLineUnit setGroupUnit = new AsciiSendLineGetLineUnit( "group " + _group.Name );
            setGroupUnit.Finished += new AsciiProtocolUnitDelegate( setGroupUnit_Finished );
            StartUnit( setGroupUnit, connection );
        }

        private void setGroupUnit_Finished( AsciiProtocolUnit unit )
        {
            AsciiSendLineGetLineUnit setGroupUnit = (AsciiSendLineGetLineUnit) unit;
            _responseLine = setGroupUnit.ResponseLine;
            string error = null;
            if( _responseLine == null )
            {
                if( _connection.LastSocketException != null )
                {
                    error = _connection.LastSocketException.Message;
                }
                else
                {
                    error = "Newsgroup could not be processed";
                }
            }
            else if( !_responseLine.StartsWith( "211" ) )
            {
                if( _responseLine.Length > 4 )
                {
                    error = _responseLine.Substring( 4 );
                }
                else
                {
                    error = "Newsgroup could not be processed";
                }
            }
            ResourceProxy proxy = new ResourceProxy( _group.Resource );
            if( error != null )
            {
                proxy.SetPropAsync( Core.Props.LastError, error );
            }
            else
            {
                proxy.DeletePropAsync( Core.Props.LastError );
            }
            FireFinished();
        }

        private NewsgroupResource   _group;
        private string              _responseLine;
        private AsciiTcpConnection  _connection;
    }

    /// <summary>
    /// base class downloading article headers
    /// override the SetArticleNumbersRange() factory method
    /// </summary>
    internal abstract class NntpDownloadHeadersUnitBase: AsciiProtocolUnit
    {
        protected NntpDownloadHeadersUnitBase( NewsgroupResource group, JobPriority priority )
        {
            _group = group;
            _priority = priority;
        }

        protected override void Start( AsciiTcpConnection connection )
        {
            Core.UIManager.GetStatusWriter( typeof( NntpDownloadHeadersUnitBase ),
                StatusPane.Network ).ShowStatus( "Downloading headers from " + _group.DisplayName + "..." );
            _connection = connection;
            if( !_group.IsSubscribed )
            {
                FireFinished();
            }
            else
            {
                NntpSetGroupUnit downloadHeadersSetGroupUnit = new NntpSetGroupUnit( _group );
                downloadHeadersSetGroupUnit.Finished += new AsciiProtocolUnitDelegate( downloadHeadersSetGroupUnit_Finished );
                StartUnit( downloadHeadersSetGroupUnit, connection );
            }
        }

        protected override void FireFinished()
        {
            if( _getHeadersUnit != null )
            {
                _getHeadersUnit.Finished -= new AsciiProtocolUnitDelegate( getHeadersUnit_Finished );
                FireFinished( _getHeadersUnit );
            }
            Core.UIManager.GetStatusWriter(
                typeof( NntpDownloadHeadersUnitBase ), StatusPane.Network ).ClearStatus();
            base.FireFinished();
        }


        /// <summary>
        /// Factory method to set range of numbers of articles which we are going to download
        /// When entering the method, firstArticle and lastArticle are equal to what the server
        /// has responded on the GROUP command
        /// </summary>
        protected abstract void SetArticleNumbersRange( ref int firstArticle, ref int lastArticle );

        protected virtual int GetHeadersCount()
        {
            int count = _group.CountToDownloadAtTime;
            if( count > _lastArticle - _firstArticle + 1 )
            {
                count = _lastArticle - _firstArticle + 1;
            }
            return count;
        }

        private void downloadHeadersSetGroupUnit_Finished( AsciiProtocolUnit unit )
        {
            NntpSetGroupUnit setGroupUnit = (NntpSetGroupUnit) unit;
            string response = setGroupUnit.ResponseLine;
            if( response != null )
            {
                while( response.StartsWith( "211" ) )
                {
                    string[] parts = response.Split( ' ' );
                    _firstArticleCopy = _firstArticle = Int32.Parse( parts[ 2 ] );
                    _lastArticle = Int32.Parse( parts[ 3 ] );
                    SetArticleNumbersRange( ref _firstArticle, ref _lastArticle );
                    if( _lastArticle == 0  || _lastArticle < _firstArticle )
                    {
                        break;
                    }
                    _getHeadersUnit = new AsciiSendLineAndApplyMethodUnit(
                        "xover " + _firstArticle + '-' + _lastArticle, ".\r\n", new LineDelegate( ProcessHeadersLine ) );
                    _getHeadersUnit.Finished += new AsciiProtocolUnitDelegate( getHeadersUnit_Finished );
                    StartUnit( _getHeadersUnit, _connection );
                    return;
                }   
            }
            FireFinished();
        }

        private void ProcessHeadersLine( string line )
        {
            if( !_responseChecked )
            {
                _responseChecked = true;
                if( !line.StartsWith( "224" ) )
                {
                    FireFinished();
                }
            }
            else
            {
                Core.ResourceAP.QueueJob(
                    _priority, "Creating news header", _createArticlesFromHeadersMethod, line, _group.Resource );
            }
        }

        private void getHeadersUnit_Finished( AsciiProtocolUnit unit )
        {
            if( _getHeadersUnit.LineSent )
            {
                SetGroupNumbers();
            }
            _getHeadersUnit = null;
            FireFinished();
        }

        private void SetGroupNumbers()
        {
            if( !Core.ResourceStore.IsOwnerThread() )
            {
                Core.ResourceAP.QueueJob(
                    _priority, "Updating newsgroup structure", new MethodInvoker( SetGroupNumbers ) );
            }
            else
            {
                IResource group = _group.Resource;
                if( !group.IsDeleted )
                {
                    group.BeginUpdate();
                    try
                    {
                        _group.LastArticle = _lastArticle;
                        _group.FirstArticle = _firstArticle;
                        if( _firstArticleCopy >= _group.FirstArticle )
                        {
                            group.SetProp( NntpPlugin._propNoMoreHeaders, true );
                        }
                    }
                    finally
                    {
                        group.EndUpdate();
                    }
                }
            }
        }

        protected NewsgroupResource             _group;
        private JobPriority                     _priority;
        private int                             _firstArticle;
        private int                             _firstArticleCopy;
        private int                             _lastArticle;        
        private bool                            _responseChecked;
        private AsciiSendLineAndApplyMethodUnit _getHeadersUnit;
        private AsciiTcpConnection              _connection;
        private static CreateArticleFromHeadersDelegate _createArticlesFromHeadersMethod =
            new CreateArticleFromHeadersDelegate( NewsArticleParser.CreateArticleFromHeaders );
    }

    /// <summary>
    /// download new headers
    /// </summary>
    internal class NntpDownloadHeadersUnit : NntpDownloadHeadersUnitBase
    {
        public NntpDownloadHeadersUnit( NewsgroupResource group, JobPriority priority )
            : base( group, priority ) {}

        protected override void SetArticleNumbersRange( ref int firstArticle, ref int lastArticle )
        {
            int headersCount2Get = GetHeadersCount();
            int currentLastArticle = _group.LastArticle;
            // first time last article is not set
            if( currentLastArticle == 0 )
            {
                currentLastArticle = lastArticle - headersCount2Get;
                if( currentLastArticle < firstArticle - 1 )
                {
                    currentLastArticle = firstArticle - 1;
                }

            }
            else
            {
                if( firstArticle > currentLastArticle )
                {
                    currentLastArticle = firstArticle - 1;
                }
                else if( _group.FirstArticle > lastArticle )
                {
                    ResourceProxy proxy = new ResourceProxy( _group.Resource );
                    proxy.BeginUpdate();
                    try
                    {
                        proxy.DeleteProp( NntpPlugin._propFirstArticle );
                        proxy.DeleteProp( NntpPlugin._propLastArticle );
                    }
                    finally
                    {
                        proxy.EndUpdateAsync();
                    }
                }
            }
            if( headersCount2Get < lastArticle - currentLastArticle )
            {
                lastArticle = currentLastArticle + headersCount2Get;
                firstArticle = currentLastArticle + 1;
            }
            else
            {
                headersCount2Get = lastArticle - currentLastArticle;
                if( headersCount2Get <= 0 )
                {
                    lastArticle = 0;
                }
                else
                {
                    firstArticle = currentLastArticle + 1;
                }
            }
        }
    }

    /// <summary>
    /// download next headers
    /// </summary>
    internal class NntpDownloadNextHeadersUnit: NntpDownloadHeadersUnitBase
    {
        public NntpDownloadNextHeadersUnit( NewsgroupResource group, JobPriority priority )
            : base( group, priority ) {}

        protected override void SetArticleNumbersRange( ref int firstArticle, ref int lastArticle )
        {
            int headersCount2Get = GetHeadersCount();
            int currentFirstArticle = _group.FirstArticle;
            // first time first article is not set
            if( currentFirstArticle == Int32.MaxValue )
            {
                currentFirstArticle = lastArticle + 1;
            }
            if( headersCount2Get > currentFirstArticle - firstArticle )
            {
                headersCount2Get = currentFirstArticle - firstArticle;
            }
            if( headersCount2Get <= 0 )
            {
                lastArticle = 0;
            }
            else
            {
                lastArticle = currentFirstArticle - 1;
                firstArticle = currentFirstArticle - headersCount2Get;
            }
        }
    }

    /// <summary>
    /// download all headers
    /// </summary>
    internal class NntpDownloadAllHeadersUnit: NntpDownloadHeadersUnitBase
    {
        public NntpDownloadAllHeadersUnit( NewsgroupResource group, JobPriority priority )
            : base( group, priority ) {}

        protected override void SetArticleNumbersRange( ref int firstArticle, ref int lastArticle )
        {
        }
    }

    /// <summary>
    /// download an article
    /// </summary>
    internal class NntpDownloadArticleUnit: AsciiProtocolUnit
    {
        public NntpDownloadArticleUnit( IResource article,
                                        IResource group,
                                        JobPriority priority,
                                        bool setGroup )
        {
            _article = article;
            _group = group;
            _priority = priority;
            _setGroup = setGroup;
        }

        public bool ArticleAvailable
        {
            get { return _articleAvailable; }
        }

        public IResource Article
        {
            get { return _article; }
        }

        public delegate void ProgressDelegate( NntpDownloadArticleUnit sender, string line );

        public event ProgressDelegate OnProgress;

        protected override void Start( AsciiTcpConnection connection )
        {
            _connection = connection;
            if( _setGroup )
            {
                NntpSetGroupUnit setGroupUnit = new NntpSetGroupUnit( new NewsgroupResource( _group ) );
                setGroupUnit.Finished += new AsciiProtocolUnitDelegate( setGroupUnit_Finished );
                StartUnit( setGroupUnit, connection );
            }
            else
            {
                GetArticleById();
            }
        }

        protected override void FireFinished()
        {
            if( _getArticleUnit != null )
            {
                _getArticleUnit.Finished -= new AsciiProtocolUnitDelegate( getArticleUnit_Finished );
                FireFinished( _getArticleUnit );
            }
            OnProgress = null;
            if( !_articleAvailable )
            {
                Core.ResourceAP.QueueJob( _priority,
                    new SetErrorDelegate( SetError ), "Connection closed.", false );
            }
            base.FireFinished();
        }

        private void setGroupUnit_Finished( AsciiProtocolUnit unit )
        {
            NntpSetGroupUnit setGroupUnit = (NntpSetGroupUnit) unit;
            string response = setGroupUnit.ResponseLine;
            if( response == null || !response.StartsWith( "211" ) )
            {
                FireFinished();
            }
            else
            {
                GetArticleById();
            }
        }

        private void GetArticleById()
        {
            _articleAvailable = false;
            _articleId = ParseTools.UnescapeCaseSensitiveString( _article.GetPropText( NntpPlugin._propArticleId ) );
            _getArticleUnit = new AsciiSendLineAndApplyMethodUnit(
                "article " + _articleId, ".\r\n", new LineDelegate( getArticleByIdUnitProcessLine ) );
            _getArticleUnit.Finished += new AsciiProtocolUnitDelegate( getArticleUnit_Finished );
            StartUnit( _getArticleUnit, _connection );
        }

        private void getArticleByIdUnitProcessLine( string line )
        {
            if( !_articleAvailable )
            {
                if( !line.StartsWith( "220" ) )
                {
                    string articleNumber = NewsArticleHelper.GetArticleNumber( _article, _group );
                    if( articleNumber == null )
                    {
                        ProcessErrorResponse( line );
                    }
                    else
                    {
                        _getArticleUnit.Finished -= new AsciiProtocolUnitDelegate( getArticleUnit_Finished );
                        FireFinished( _getArticleUnit );
                        GetArticleByNumber( articleNumber );
                    }
                }
                else
                {
                    _lines = new ArrayList();
                    _articleAvailable = true;
                }
            }
            else
            {
                ProcessLine( line );
            }
        }

        private void GetArticleByNumber( string articleNumber )
        {
            _articleAvailable = false;
            _getArticleUnit = new AsciiSendLineAndApplyMethodUnit(
                "article " + articleNumber, ".\r\n", new LineDelegate( getArticleByNumberUnitProcessLine ) );
            _getArticleUnit.Finished += new AsciiProtocolUnitDelegate( getArticleUnit_Finished );
            StartUnit( _getArticleUnit, _connection );
        }

        private void getArticleUnit_Finished( AsciiProtocolUnit unit )
        {
            if( _articleAvailable )
            {
                IAsyncProcessor ap = Core.ResourceAP;
                if( _group == null )
                {
                    ap.RunUniqueJob( "Creating news article",
                        new CreateArticleByProtocolHandlerDelegate( NewsArticleParser.CreateArticleByProtocolHandler ),
                        _lines.ToArray( typeof( string ) ), _article );
                }
                else
                {
                    ap.QueueJob( _priority, "Creating news article",
                        new CreateArticleDelegate( NewsArticleParser.CreateArticle ),
                        _lines.ToArray( typeof( string ) ), _group, _articleId );
                }
            }
            _getArticleUnit = null;
            FireFinished();
        }

        private void getArticleByNumberUnitProcessLine( string line )
        {
            if( !_articleAvailable )
            {
                if( !line.StartsWith( "220" ) )
                {
                    ProcessErrorResponse( line );
                }
                else
                {
                    _lines = new ArrayList();
                    _articleAvailable = true;
                }
            }
            else
            {
                ProcessLine( line );
            }
        }

        private const string LINES = "lines: ";
        private void ProcessLine( string line )
        {
            if( _lineCount == 0 )
            {
                if( string.Compare( line, 0, LINES, 0, LINES.Length, true ) == 0 )
                {
                    try
                    {
                        _lineCount = Int32.Parse( line.Substring( 6 ).Trim().TrimEnd( '\r', '\n' ) );
                    }
                    catch
                    {
                        _lineCount = Int32.MaxValue;
                    }
                    _lineIndex = -1;
                }
            }
            else
            {
                if( _lineIndex == -1 )
                {
                    if( line.TrimEnd( '\r', '\n' ).Length == 0 )
                    {
                        _lineIndex = 0;
                    }
                }
                else
                {
                    if( OnProgress != null )
                    {
                        int percent = _lineIndex * 100 / _lineCount;
                        if( percent > 100 )
                        {
                            percent = 100;
                        }
                        OnProgress( this, percent.ToString() );
                    }
                    ++_lineIndex;
                }
            }
            _lines.Add( line );
        }

        private void ProcessErrorResponse( string line )
        {
            /// Never download the article again only in case of the following responses:
            /// "430 No such article" and "423 Bad article number".

            bool neverDownloadAgain = line.StartsWith( "430" ) || line.StartsWith( "423" );

            Core.ResourceAP.QueueJob( _priority,
                new SetErrorDelegate( SetError ), line.Remove( 0, 3 ).Trim() + '.', neverDownloadAgain );
            FireFinished();
        }

        private delegate void SetErrorDelegate( string error, bool neverDownloadAgain );

        private void SetError( string error, bool neverDownloadAgain )
        {
            if( !_article.IsDeleted && _article.HasProp( NntpPlugin._propHasNoBody ) )
            {
                _article.SetProp( Core.Props.LongBody, error );
                if( neverDownloadAgain )
                {
                    _article.DeleteProp( NntpPlugin._propHasNoBody );
                }
            }
        }

        private IResource                       _article;
        private IResource                       _group;
        private JobPriority                     _priority;
        private bool                            _setGroup;
        private string                          _articleId;
        private bool                            _articleAvailable;
        private int                             _lineCount;
        private int                             _lineIndex;
        private ArrayList                       _lines;
        private AsciiSendLineAndApplyMethodUnit _getArticleUnit;
        private AsciiTcpConnection              _connection;
    }

    /// <summary>
    /// headers delivering unit
    /// </summary>
    internal class NntpDeliverHeadersFromGroupsUnit : AsciiProtocolUnit
    {
        public NntpDeliverHeadersFromGroupsUnit( IResourceList groups, IResource groupToIgnore )
        {
            Interlocked.Increment( ref NntpPlugin._deliverNewsUnitCount );
            _groups = groups;
            _groupToIgnore = groupToIgnore;
        }

        protected override void Start( AsciiTcpConnection connection )
        {
            _connection = connection;
            _groupIndex = 0;
            StartNextGroup();
        }

        protected override void FireFinished()
        {
            Interlocked.Decrement( ref NntpPlugin._deliverNewsUnitCount );
            base.FireFinished();
        }

        private void StartNextGroup()
        {
            if( _groupIndex >= _groups.Count )
            {
                FireFinished();
            }
            else
            {
                IResource group;
                try
                {
                    group = _groups[ _groupIndex++ ];
                }
                catch( InvalidResourceIdException )
                {
                    group = _groupToIgnore;
                }
                if( group != _groupToIgnore && !group.IsDeleted && !group.IsDeleting )
                {
                    StartGroup( group );
                }
                else
                {
                    StartNextGroup();
                }
            }
        }

        private void StartGroup( IResource group )
        {
            NewsgroupResource groupResource = new NewsgroupResource( group );
            NntpDownloadHeadersUnit unit = new NntpDownloadHeadersUnit( groupResource, JobPriority.Normal );
            unit.Finished += new AsciiProtocolUnitDelegate( group_Finished );
            StartUnit( unit, _connection );
        }

        private void group_Finished( AsciiProtocolUnit unit )
        {
            StartNextGroup();
        }

        private IResourceList       _groups;
        private IResource           _groupToIgnore;
        private AsciiTcpConnection  _connection;
        private int                 _groupIndex;
    }

    /// <summary>
    /// headers delivering unit
    /// </summary>
    internal class NntpDeliverEmptyArticlesFromGroupsUnit : AsciiProtocolUnit
    {
        public NntpDeliverEmptyArticlesFromGroupsUnit( IResourceList groups, IResource groupToIgnore )
        {
            Interlocked.Increment( ref NntpPlugin._deliverNewsUnitCount );
            _groups = groups;
            _groupToIgnore = groupToIgnore;
        }

        protected override void Start( AsciiTcpConnection connection )
        {
            _connection = connection;
            _groupIndex = 0;
            Core.ResourceAP.QueueJob( new MethodInvoker( StartNextGroupMarshalled ) );
        }

        protected override void FireFinished()
        {
            Core.UIManager.GetStatusWriter( typeof( NntpDeliverEmptyArticlesFromGroupsUnit ),
                StatusPane.Network ).ClearStatus();
            Interlocked.Decrement( ref NntpPlugin._deliverNewsUnitCount );
            base.FireFinished();
        }

        private void StartNextGroup()
        {
            if( _groupIndex >= _groups.Count )
            {
                FireFinished();
            }
            else
            {
                IResource group;
                try
                {
                    group = _groups[ _groupIndex++ ];
                }
                catch( InvalidResourceIdException )
                {
                    group = _groupToIgnore;
                }
                if( group != _groupToIgnore && !group.IsDeleted && !group.IsDeleting )
                {
                    StartGroup( group );
                }
                else
                {
                    StartNextGroup();
                }
            }
        }

        private void StartNextGroupMarshalled()
        {
            Core.NetworkAP.QueueJob( JobPriority.Immediate, new MethodInvoker( StartNextGroup ) );
        }

        private void StartGroup( IResource group )
        {
            _group = new NewsgroupResource( group );
            if( !_group.IsSubscribed )
            {
                StartNextGroup();
            }
            else
            {
                Core.UIManager.GetStatusWriter( typeof( NntpDeliverEmptyArticlesFromGroupsUnit ),
                    StatusPane.Network ).ShowStatus( "Downloading articles from " + _group.DisplayName + "..." );
                NntpSetGroupUnit setGroupUnit = new NntpSetGroupUnit( _group );
                setGroupUnit.Finished += new AsciiProtocolUnitDelegate( setGroupUnit_Finished );
                StartUnit( setGroupUnit, _connection );
            }
        }

        private void setGroupUnit_Finished( AsciiProtocolUnit unit )
        {
            NntpSetGroupUnit setGroupUnit = (NntpSetGroupUnit) unit;
            string response = setGroupUnit.ResponseLine;
            if( response == null || !response.StartsWith( "211" ) )
            {
                StartNextGroup();
            }
            else
            {
                _emptyArticles = _group.Resource.GetLinksTo( null, NntpPlugin._propTo );
                _emptyArticles = _emptyArticles.Intersect(
                    Core.ResourceStore.FindResourcesWithProp( null, NntpPlugin._propHasNoBody ), true );
                _emptyArticles.Sort( "Date", false );
                _articleIndex = 0;
                StartNextArticle();
            }
        }

        private void StartNextArticle()
        {
            if( _articleIndex >= _emptyArticles.Count || _articleIndex >= _group.CountToDownloadAtTime )
            {
                StartNextGroup();
            }
            else
            {
                IResource article;
                try
                {
                    article = _emptyArticles[ _articleIndex++ ];
                }
                catch( InvalidResourceIdException )
                {
                    article = null;
                }
                if( article == null || article.IsDeleted || article.IsDeleting )
                {
                    StartNextArticle();
                }
                else
                {
                    NntpDownloadArticleUnit downloadArticleUnit =
                        new NntpDownloadArticleUnit( article, _group.Resource, JobPriority.Normal, false );
                    downloadArticleUnit.Finished += new AsciiProtocolUnitDelegate( downloadArticleUnit_Finished );
                    StartUnit( downloadArticleUnit, _connection );
                }
            }
        }

        private void downloadArticleUnit_Finished( AsciiProtocolUnit unit )
        {
            StartNextArticle();
        }

        private IResourceList       _groups;
        private IResource           _groupToIgnore;
        private int                 _groupIndex;
        private int                 _articleIndex;
        private NewsgroupResource   _group;
        private IResourceList       _emptyArticles;
        private AsciiTcpConnection  _connection;
    }

    /// <summary>
    /// posting article unit
    /// </summary>
    internal class NntpPostArticleUnit: AsciiProtocolUnit
    {
        /// <summary>
        /// Creates protocol unit for posting article to server
        /// </summary>
        /// <param name="draftArticle"></param>
        /// <param name="server"></param>
        /// <param name="finishedMethod">can be null</param>
        public NntpPostArticleUnit( IResource draftArticle,
                                    IResource server,
                                    AsciiProtocolUnitDelegate finishedMethod,
                                    bool invokedByUser )
        {
            _draftArticle = draftArticle;
            _server = new ServerResource( server );
            _invokedByUser = invokedByUser;
            _error = "Failed to connect, posting abandoned.";
            if( finishedMethod != null )
            {
                Finished += finishedMethod;
            }
        }

        public IResource DraftArticle
        {
            get { return _draftArticle; }
        }

        /// <summary>
        /// if null then no error
        /// </summary>
        public string Error
        {
            get { return _error; }
        }

        protected override void Start( AsciiTcpConnection connection )
        {
            _connection = connection;
            Core.UIManager.GetStatusWriter( _server.Name, StatusPane.Network ).ShowStatus(
                "Posting article to " + _server.DisplayName );
            Core.ResourceAP.QueueJob( _invokedByUser ? JobPriority.Immediate : JobPriority.BelowNormal,
                "Preparing articles for posting", new MethodInvoker( PreparePosting ) );
        }

        protected override void FireFinished()
        {
            if( _error == null )
            {
                NewsFolders.PlaceResourceToFolder( _draftArticle, NewsFolders.SentItems );
            }
            Core.UIManager.GetStatusWriter( _server.Name, StatusPane.Network ).ClearStatus();
            base.FireFinished();
        }

        #region strings for building RFC-822 datetime

        private readonly string[] _daysOfTheWeek =
            { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
        private readonly string[] _months =
            { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        #endregion

        private void PreparePosting()
        {
            if( NewsFolders.IsInFolder( _draftArticle, NewsFolders.SentItems ) )
            {
                Core.NetworkAP.QueueJob(
                    JobPriority.Immediate, "Finish posting", new MethodInvoker( FireFinished ) );
            }
            else
            {
                NewsFolders.PlaceResourceToFolder( _draftArticle, NewsFolders.Outbox );
                _nntpBody = _draftArticle.GetPropText( NntpPlugin._propNntpText );
                if( !_draftArticle.HasProp( NntpPlugin._propArticleId ) )
                {
                    StringBuilder builder = new StringBuilder( _nntpBody.Length + 64 );
                    builder.Append( "Date: " );
                    DateTime postTime = DateTime.UtcNow;
                    builder.Append( _daysOfTheWeek[ (int)postTime.DayOfWeek ] );
                    builder.Append( ", " );
                    builder.Append( postTime.Day );
                    builder.Append( ' ' );
                    builder.Append( _months[ postTime.Month - 1 ] );
                    builder.Append( ' ' );
                    builder.Append( postTime.Year );
                    builder.Append( ' ' );
                    builder.Append( postTime.Hour.ToString().PadLeft( 2, '0' ) );
                    builder.Append( ':' );
                    builder.Append( postTime.Minute.ToString().PadLeft( 2, '0' ) );
                    builder.Append( ':' );
                    builder.Append( postTime.Second.ToString().PadLeft( 2, '0' ) );
                    builder.Append( " +0000 (UTC)" );
                    builder.Append( "\r\nMessage-ID: " );
                    string message_id = ParseTools.GenerateArticleId( _draftArticle, _server.Name );
                    builder.Append( message_id );
                    builder.Append( "\r\n" );
                    builder.Append( _nntpBody );
                    _nntpBody = builder.ToString();
                    _draftArticle.SetProp( NntpPlugin._propArticleId, message_id );
                }
                AsciiSendLineGetLineUnit initPostUnit = new AsciiSendLineGetLineUnit( "post" );
                initPostUnit.Finished += new AsciiProtocolUnitDelegate( initPostUnit_Finished );
                Core.NetworkAP.QueueJob( JobPriority.Immediate, "Posting articles",
                    new StartUnitDelegate( StartUnit ), initPostUnit, _connection );
            }
        }

        private delegate void StartUnitDelegate( AsciiProtocolUnit unit, AsciiTcpConnection connection );

        private void initPostUnit_Finished( AsciiProtocolUnit unit )
        {
            AsciiSendLineGetLineUnit initPostUnit = (AsciiSendLineGetLineUnit) unit;
            string response = initPostUnit.ResponseLine;
            if( !initPostUnit.LineSent || ( response != null && !response.StartsWith( "340" ) ) )
            {
                ExtractError( response );
                FireFinished();
            }
            else
            {
                AsciiSendLineGetLineUnit postUnit = new AsciiSendLineGetLineUnit( _nntpBody );
                postUnit.Finished += new AsciiProtocolUnitDelegate( postUnit_Finished );
                StartUnit( postUnit, _connection );
            }
        }

        private void postUnit_Finished( AsciiProtocolUnit unit )
        {
            AsciiSendLineGetLineUnit postUnit = (AsciiSendLineGetLineUnit) unit;
            string response = postUnit.ResponseLine;
            if( !postUnit.LineSent || response   == null || !response.StartsWith( "240" ) )
            {
                ExtractError( response );
                FireFinished();
            }
            else
            {
                _error = null;
                FireFinished();
            }
        }

        private void ExtractError( string response )
        {
            if( response != null )
            {
                _error = ( response.Length > 3 ) ? response.Substring( 3 ).Trim() : "Timeout occurred";
            }
            else
            {
                Exception exception = _connection.LastSocketException;
                _error = ( exception != null ) ? exception.Message : "Indeterminate error";
            }
        }

        private IResource                   _draftArticle;
        private ServerResource              _server;
        private AsciiTcpConnection          _connection;
        private string                      _nntpBody;
        private string                      _error;
        private bool                        _invokedByUser;
    }
}