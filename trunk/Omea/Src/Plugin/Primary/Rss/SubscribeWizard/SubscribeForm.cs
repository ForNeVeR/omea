/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.HttpTools;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.RSSPlugin.SubscribeWizard
{
    /// <summary>
    /// Wizard for subscribing to RSS feeds.
    /// </summary>
    public class SubscribeForm : DialogBase
    {
        private Button _backButton;
        private Button _nextButton;
        private Button _cancelButton;
        private PictureBox _pictureBox;
        private Panel _bodyPanel;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private Container components = null;

        private FeedAddressPane _feedAddressPane;
        private TitleGroupPane _titleGroupPane;
        private MultipleResultsPane _multipleResultsPane;
        private SearchEnginesPane _searchEnginesPane;
        private ErrorPane _errorPane;
        private ResourceProxy _newFeedProxy;
        private ResourceProxy[] _feedsToSubscribe;
        private RSSUnitOfWork _rssUnitOfWork;
        private MultipleFeedsJob _rssMultipleUnitOfWork;
        private IResource _defaultGroup;
        private RSSDiscover _rssDiscover;
        private Control _visiblePage;
        private MethodInvoker _backClickHandler;
        private MethodInvoker _nextClickHandler;
        private Label _progressLabel;
        private bool _cancelled = false;
        private bool _isSearchFeed = false;

        public delegate void   CanMoveNextDelegate( bool state );

        public SubscribeForm()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();
            Icon = Core.UIManager.ApplicationIcon;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if ( disposing )
            {
                if ( components != null )
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(SubscribeForm));
            this._backButton = new System.Windows.Forms.Button();
            this._nextButton = new System.Windows.Forms.Button();
            this._cancelButton = new System.Windows.Forms.Button();
            this._pictureBox = new System.Windows.Forms.PictureBox();
            this._bodyPanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // _backButton
            // 
            this._backButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._backButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._backButton.Location = new System.Drawing.Point(296, 416);
            this._backButton.Name = "_backButton";
            this._backButton.TabIndex = 1;
            this._backButton.Text = "< &Back";
            this._backButton.Click += new System.EventHandler(this._backButton_Click);
            // 
            // _nextButton
            // 
            this._nextButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._nextButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._nextButton.Location = new System.Drawing.Point(376, 416);
            this._nextButton.Name = "_nextButton";
            this._nextButton.TabIndex = 2;
            this._nextButton.Text = "Next >";
            this._nextButton.Click += new System.EventHandler(this._nextButton_Click);
            // 
            // _cancelButton
            // 
            this._cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this._cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._cancelButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._cancelButton.Location = new System.Drawing.Point(464, 416);
            this._cancelButton.Name = "_cancelButton";
            this._cancelButton.TabIndex = 3;
            this._cancelButton.Text = "Cancel";
            this._cancelButton.Click += new System.EventHandler(this.OnCancel);
            // 
            // _pictureBox
            // 
            this._pictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left)));
            this._pictureBox.Image = ((System.Drawing.Image)(resources.GetObject("_pictureBox.Image")));
            this._pictureBox.Location = new System.Drawing.Point(0, 0);
            this._pictureBox.Name = "_pictureBox";
            this._pictureBox.Size = new System.Drawing.Size(168, 399);
            this._pictureBox.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this._pictureBox.TabIndex = 6;
            this._pictureBox.TabStop = false;
            // 
            // _bodyPanel
            // 
            this._bodyPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this._bodyPanel.BackColor = System.Drawing.SystemColors.Window;
            this._bodyPanel.Location = new System.Drawing.Point(168, 0);
            this._bodyPanel.Name = "_bodyPanel";
            this._bodyPanel.Size = new System.Drawing.Size(384, 399);
            this._bodyPanel.TabIndex = 0;
            // 
            // SubscribeForm
            // 
            this.AcceptButton = this._nextButton;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this._cancelButton;
            this.ClientSize = new System.Drawing.Size(552, 451);
            this.Controls.Add(this._bodyPanel);
            this.Controls.Add(this._pictureBox);
            this.Controls.Add(this._backButton);
            this.Controls.Add(this._nextButton);
            this.Controls.Add(this._cancelButton);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.MinimizeBox = true;
            this.MaximizeBox = false;
            this.ShowInTaskbar = true;
            this.MaximumSize = new System.Drawing.Size(1000, 485);
            this.MinimumSize = new System.Drawing.Size(552, 485);
            this.Name = "SubscribeForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Subscribe to Feed";
            this.SizeChanged += new System.EventHandler(this.SubscribeForm_SizeChanged);
            this.Closed += new System.EventHandler(this.OnClosed);
            this.ResumeLayout(false);

        }

        #endregion

        public void ShowAddFeedWizard( string url, IResource group )
        {
            _defaultGroup = group;
            _feedAddressPane = new FeedAddressPane();
            _feedAddressPane.NextPage += _nextButton_Click;
            _feedAddressPane.NextPredicate = ButtonStateChecker;
            ShowFeedAddressPane( url );
            RestoreSettings();
            Show();
        }

        public void ShowSearchFeedWizard( IResource group )
        {
            Text = "Subscribe to Search Feed";
            _isSearchFeed = true;
            _defaultGroup = group;
            _searchEnginesPane = new SearchEnginesPane();
            _searchEnginesPane.ClearProgress();
            _searchEnginesPane.ControlsEnabled = true;
            _searchEnginesPane.NextPage += _nextButton_Click;
            _searchEnginesPane.NextPredicate = ButtonStateChecker;
            _progressLabel = _searchEnginesPane.ProgressLabel;
            ShowPage( _searchEnginesPane, null, OnSearchDownloadClick, false );
            RestoreSettings();
            Show();
        }

        public void  ShowSearchFeedWizard()
        {
            Text = "Subscribe to Search Feed";
            if( _searchEnginesPane == null )
            {
                _searchEnginesPane = new SearchEnginesPane();
            }
            _searchEnginesPane.ClearProgress();
            _searchEnginesPane.ControlsEnabled = true;
            _searchEnginesPane.NextPage += _nextButton_Click;
            _searchEnginesPane.NextPredicate = ButtonStateChecker;
            _progressLabel = _searchEnginesPane.ProgressLabel;
            ShowPage( _searchEnginesPane, null, OnSearchDownloadClick, false );
            RestoreSettings();
        }

        private void ShowFeedAddressPane( string url )
        {
            if ( url == null )
            {
                return;
            }
            if ( url.Length > 0 )
            {
                _feedAddressPane.FeedUrl = url;
            }
            else
            {
                string defaultUrl = "";
                try
                {
                    IDataObject dataObj = Clipboard.GetDataObject();
                    if ( dataObj != null )
                    {
                        defaultUrl = (string)dataObj.GetData( typeof (string) );
                        if ( defaultUrl != null && defaultUrl.Length > 0 )
                        {
                            new Uri( defaultUrl ); // validates the scheme
                        }
                    }
                }
                catch ( Exception )
                {
                    defaultUrl = "";
                }


                if ( defaultUrl == null || defaultUrl.Length == 0 )
                {
                    _feedAddressPane.FeedUrl = "http://";
                    _feedAddressPane.UnselectFeedUrl();
                }
                else
                {
                    _feedAddressPane.FeedUrl = defaultUrl;
                }
            }

            _feedAddressPane.ProgressLabel.Text = "";
            _feedAddressPane.ControlsEnabled = true;
            _progressLabel = _feedAddressPane.ProgressLabel;
            ShowPage( _feedAddressPane, null, OnDownloadClick, false );
        }

        private void ShowPage( Control page, MethodInvoker backClickHandler, MethodInvoker nextClickHandler,
                               bool isFinish )
        {
            if ( _visiblePage != null )
            {
                _visiblePage.Visible = false;
            }
            _visiblePage = page;
            _visiblePage.Visible = true;
            if ( _visiblePage.Parent == null )
            {
                _bodyPanel.Controls.Add( _visiblePage );
            }

            _backClickHandler = backClickHandler;
            _backButton.Enabled = ( backClickHandler != null );

            _nextClickHandler = nextClickHandler;
            _nextButton.Enabled = ( nextClickHandler != null );
            _nextButton.Text = isFinish ? "Finish" : "Next >";

            _visiblePage.Focus();
        }

        private void OnSearchDownloadClick()
        {
            //  Check validity of result query urls.
            //  If some url is invalid, abort further processing.
            //  ToDo: just ignore invalid urls?
            string query = _searchEnginesPane.SearchQuery;
            string[] feedNames = _searchEnginesPane.CheckedFeedNames;
            string[] searchUrls = _searchEnginesPane.CheckedURLs;
            foreach( string url in searchUrls )
            {
                try
                {
                    new Uri( url + query );
                }
                catch ( Exception ex )
                {
                    _searchEnginesPane.ErrorMessage = ex.Message;
                    return;
                }
            }

            _progressLabel.Text = "Downloading...";
            _nextButton.Enabled = false;
            _searchEnginesPane.ControlsEnabled = false;

            if ( _newFeedProxy != null )
            {
                _newFeedProxy.DeleteAsync();
                _newFeedProxy = null;
            }

            _rssMultipleUnitOfWork = new MultipleFeedsJob( searchUrls, feedNames, query, _searchEnginesPane.SearchPhrase );
            _rssMultipleUnitOfWork.DownloadTitleProgress += OnDownloadTitleProgress;
            _rssMultipleUnitOfWork.DownloadProgress += OnDownloadProgress;
            _rssMultipleUnitOfWork.ParseDone += OnMultipleParseDone;
            Core.NetworkAP.QueueJob( JobPriority.Immediate, _rssMultipleUnitOfWork );
        }

        private void OnDownloadClick()
        {
            _feedAddressPane.SetExistingFeedLink( null );
            if( File.Exists( _feedAddressPane.FeedUrl ) )
            {
                _feedAddressPane.FeedUrl = "file://" + _feedAddressPane.FeedUrl;
            }
            else
            if( _feedAddressPane.FeedUrl.IndexOf( "://" ) < 0 )
            {
                _feedAddressPane.FeedUrl = "http://" + _feedAddressPane.FeedUrl;
            }
            else
            {
                string url = _feedAddressPane.FeedUrl.ToLower();
                if ( !HttpReader.IsSupportedProtocol( url ) )
                {
                    _feedAddressPane.ErrorMessage = "Unknown URL schema. Only http:, https: and file: are supported.";
                    return;
                }
            }

            try
            {
                new Uri( _feedAddressPane.FeedUrl );
            }
            catch ( Exception ex )
            {
                _feedAddressPane.ErrorMessage = ex.Message;
                return;
            }

            IResource existingFeed = RSSPlugin.GetExistingFeed( _feedAddressPane.FeedUrl );
            if ( existingFeed != null )
            {
                _feedAddressPane.ErrorMessage = "You are already subscribed to that feed.";
                _feedAddressPane.SetExistingFeedLink( existingFeed );
                return;
            }

            _progressLabel.Text = "Downloading...";
            _nextButton.Enabled = false;
            _feedAddressPane.ControlsEnabled = false;

            if ( _newFeedProxy != null )
            {
                _newFeedProxy.DeleteAsync();
                _newFeedProxy = null;
            }

            _newFeedProxy = CreateFeedProxy( _feedAddressPane.FeedUrl, null );

            _rssUnitOfWork = new RSSUnitOfWork( _newFeedProxy.Resource, false, true );
            _rssUnitOfWork.DownloadProgress += OnDownloadProgress;
            _rssUnitOfWork.ParseDone += OnParseDone;
            Core.NetworkAP.QueueJob( JobPriority.Immediate, _rssUnitOfWork );
        }

        private ResourceProxy CreateFeedProxy( string url, string name )
        {
            ResourceProxy proxy = ResourceProxy.BeginNewResource( "RSSFeed" );
            proxy.SetProp( Props.Transient, 1 );
            proxy.SetProp( Props.URL, url );
            if ( name != null )
            {
                proxy.SetProp( Core.Props.Name, name );
            }
            if ( _feedAddressPane.RequiresAuthentication )
            {
                proxy.SetProp( Props.HttpUserName, _feedAddressPane.UserName );
                proxy.SetProp( Props.HttpPassword, _feedAddressPane.Password );
            }
            proxy.EndUpdate();
            return proxy;
        }

        internal void OnDownloadProgress( object sender, DownloadProgressEventArgs e )
        {
            if ( InvokeRequired )
            {
                Core.UIManager.QueueUIJob( new DownloadProgressEventHandler( OnDownloadProgress ), new object[] {sender, e} );
            }
            else if ( _progressLabel != null )
            {
                _progressLabel.Visible = true;
                _progressLabel.Text = e.Message;
            }
        }

        internal void OnDownloadTitleProgress( object sender, DownloadProgressEventArgs e )
        {
            if ( InvokeRequired )
            {
                Core.UIManager.QueueUIJob( new DownloadProgressEventHandler( OnDownloadTitleProgress ), new object[] {sender, e} );
            }
            else
            if( _searchEnginesPane != null )
            {
                _searchEnginesPane.FeedTitle = e.Message;
            }
        }

        private void OnBackToFirstPage()
        {
            if ( _isSearchFeed )
            {
                _searchEnginesPane.ProgressLabel.Text = "";
                _searchEnginesPane.ControlsEnabled = true;
                _progressLabel = _searchEnginesPane.ProgressLabel;
                ShowPage( _searchEnginesPane, null, OnSearchDownloadClick, false );
            }
            else
            {
                ShowFeedAddressPane( _newFeedProxy.Resource.GetStringProp( Props.URL ) );
            }
        }

        private void OnParseDone( object sender, EventArgs e )
        {
            Core.UIManager.QueueUIJob( new MethodInvoker( ParseDone ) );
        }

        private void ParseDone()
        {
            if ( _cancelled )
            {
                return;
            }

            if ( _rssUnitOfWork.Status == RSSWorkStatus.Success )
            {
                _feedsToSubscribe = new ResourceProxy[] { _newFeedProxy };
                ShowTitleGroupPage( OnBackToFirstPage );
            }
            else if ( _rssUnitOfWork.Status == RSSWorkStatus.FoundHTML && !_isSearchFeed )
            {
                _rssDiscover = new RSSDiscover();
                _rssDiscover.DiscoverProgress += OnDownloadProgress;
                _rssDiscover.DiscoverDone += OnDiscoverDone;
                _rssDiscover.StartDiscover( _rssUnitOfWork.FeedURL, _rssUnitOfWork.ReadStream, _rssUnitOfWork.CharacterSet );
            }
            else
            {
                ShowErrorInformation( _rssUnitOfWork.Status, _rssUnitOfWork.LastException );
            }
        }

        private void OnMultipleParseDone( object sender, EventArgs e )
        {
            Core.UIManager.QueueUIJob( new MethodInvoker( MultipleParseDone ) );
        }

        private void MultipleParseDone()
        {
            if ( !_cancelled )
            {
                //-------------------------------------------------------------
                //  1. Collect all feeds that passed parsing
                //  2. If all feeds succeed, go directly to TitleGroupPage
                //  3. Otherwise, show an ErrorPage with a possibility to
                //     return back to the query handling and jump forth with
                //     the list of valid feeds.
                //-------------------------------------------------------------
                _feedsToSubscribe = new ResourceProxy[ _rssMultipleUnitOfWork.Feeds.Count ];
                for( int i = 0; i < _rssMultipleUnitOfWork.Feeds.Count; i++ )
                {
                    _feedsToSubscribe[ i ] = (ResourceProxy) _rssMultipleUnitOfWork.Feeds[ i ];
                }

                if ( _rssMultipleUnitOfWork.Status == RSSWorkStatus.Success )
                {
                    ShowTitleGroupPage( ShowSearchFeedWizard );
                }
                else
                {
                    string name = ((IResource) _rssMultipleUnitOfWork.FailedFeeds[ 0 ]).GetStringProp( Core.Props.Name );
                    string errorFeed = "The following feed(s) returned error:\n" + name;
                    for( int i = 1; i < _rssMultipleUnitOfWork.FailedFeeds.Count; i++ )
                    {
                        name = ((IResource) _rssMultipleUnitOfWork.FailedFeeds[ i ]).GetStringProp( Core.Props.Name );
                        errorFeed += "\n" + name;
                    }
                    errorFeed += "\n\nClick 'Back' to update the list of search feeds or 'Next' to proceed further with valid feeds.";

                    ShowErrorPage( errorFeed, ShowSearchFeedWizard, ShowTitleGroupPageAfterError );
                }
            }
        }

        private void  ShowErrorInformation( RSSWorkStatus status, Exception lastException )
        {
            if ( status == RSSWorkStatus.FoundXML )
                ShowErrorInformation( "The specified URL points to an XML file which is not an RSS or ATOM feed", string.Empty );
            else
            if ( status == RSSWorkStatus.XMLError )
                ShowErrorInformation( "The address does not point to a valid HTML or XML page", lastException.Message );
            else
            if ( lastException != null )
                ShowErrorInformation( lastException.Message, string.Empty );
            else
                ShowErrorInformation( "Unknown Error", string.Empty );
        }

        private void ShowErrorInformation( string text, string exceptionText )
        {
            if( !String.IsNullOrEmpty( exceptionText ))
                text += ": \"" + exceptionText + "\"";
            _feedAddressPane.ErrorMessage = text;
            _feedAddressPane.ProgressLabel.Text = "";
            _feedAddressPane.ControlsEnabled = _nextButton.Enabled = true;
        }

        private void ShowErrorPage( string message, MethodInvoker backStep, MethodInvoker nextStep )
        {
            if ( _errorPane == null )
            {
                _errorPane = new ErrorPane();
            }
            _errorPane.ErrorMessage = message;
            ShowPage( _errorPane, backStep, nextStep, false );
        }

        private void OnCancel( object sender, EventArgs e )
        {
            Close();
        }

        private void OnClosed( object sender, EventArgs e )
        {
            DoCancel();
        }

        private void DoCancel()
        {
            _cancelled = true;
            if ( _feedsToSubscribe != null )
            {
                foreach( ResourceProxy proxy in _feedsToSubscribe )
                {
                    if ( proxy == _newFeedProxy )
                    {
                        _newFeedProxy = null;
                    }
                    proxy.DeleteAsync();
                }
            }
            if ( _newFeedProxy != null )
            {
                _newFeedProxy.DeleteAsync();
            }
        }

        private void OnDiscoverDone( object sender, EventArgs e )
        {
            _rssDiscover.DiscoverDone -= OnDiscoverDone;
            if ( InvokeRequired )
            {
                Core.UIManager.QueueUIJob( new MethodInvoker( DiscoverDone ) );
            }
            else
            {
                DiscoverDone();
            }
        }

        private void DiscoverDone()
        {
            if ( _rssDiscover.Results.Count > 0 )
            {
                if ( _rssDiscover.Results.Count == 1 )
                {
                    IResource existingFeed = RSSPlugin.GetExistingFeed( _rssDiscover.Results[ 0 ].URL );
                    if ( existingFeed != null )
                    {
                        _feedAddressPane.ErrorMessage = "You are already subscribed to that feed.";
                        _feedAddressPane.SetExistingFeedLink( existingFeed );
                    }
                    else
                    {
                        _newFeedProxy.BeginUpdate();
                        _newFeedProxy.SetProp( Core.Props.Name, _rssDiscover.Results[ 0 ].Name );
                        _newFeedProxy.SetProp( Props.URL, _rssDiscover.Results[ 0 ].URL );
                        _newFeedProxy.EndUpdate();
                        _feedsToSubscribe = new ResourceProxy[] { _newFeedProxy };
                        ShowTitleGroupPage( OnBackToFirstPage );
                    }
                }
                else
                    ShowMultipleResultsPage();
            }
            else
                ShowErrorInformation( "Could't find a feed for the selected site", string.Empty );
        }

        private void ShowMultipleResultsPage()
        {
            if ( !_cancelled )
            {
                if ( _multipleResultsPane == null )
                {
                    _multipleResultsPane = new MultipleResultsPane();
                    _multipleResultsPane.NextPage += _nextButton_Click;
                    _multipleResultsPane.NextPredicate = ButtonStateChecker;
                }
                ShowPage( _multipleResultsPane, OnBackToFirstPage, OnSelectFeedClick, false );
                _multipleResultsPane.ShowResults( _rssDiscover );
                if ( !_multipleResultsPane.HaveAvailableResults() )
                {
                    _nextButton.Enabled = false;
                }
            }
        }

        private void OnSelectFeedClick()
        {
            RSSDiscover.RSSDiscoverResult[] results = _multipleResultsPane.GetSelectedResults();
            _feedsToSubscribe = new ResourceProxy[ results.Length ];
            for( int i = 0; i < results.Length; i++ )
            {
                _feedsToSubscribe[ i ] = CreateFeedProxy( results[ i ].URL, results[ i ].Name );
            }

            if ( _feedsToSubscribe.Length == 0 )
            {
                OnFinishClick();
            }
            else
            {
                ShowTitleGroupPage( ShowMultipleResultsPage );
            }
        }

        private void ShowTitleGroupPageAfterError()
        {
             ShowTitleGroupPage( ShowSearchFeedWizard );
        }

        private void ShowTitleGroupPage( MethodInvoker backHandler )
        {
            #region Preconditions
            if ( _feedsToSubscribe == null )
                throw new InvalidOperationException( "Trying to show title/group page with unknown feeds to subscribe" );
            #endregion Preconditions

            if ( _titleGroupPane == null )
            {
                _titleGroupPane = new TitleGroupPane();
                _titleGroupPane.NextPage += _nextButton_Click;
            }
            ShowPage( _titleGroupPane, backHandler, OnFinishClick, true );

            _backButton.Enabled = true;
            if ( _feedsToSubscribe.Length == 1 )
            {
                _titleGroupPane.FeedTitle = _feedsToSubscribe [0].Resource.GetStringProp( Core.Props.Name );
            }
            else
            {
                Trace.WriteLine( "SubscribeToSearchFeeds -- DisableFeed title." );
                _titleGroupPane.DisableFeedTitle();
            }
            if ( _defaultGroup == null )
            {
                _titleGroupPane.SelectedGroup = RSSPlugin.RootFeedGroup;
            }
            else
            {
                _titleGroupPane.SelectedGroup = _defaultGroup;
            }
        }

        #region Back/Next/Finish Handlers
        private void _backButton_Click( object sender, EventArgs e )
        {
            if ( _backClickHandler != null )
            {
                _backClickHandler.Invoke();
            }
        }

        private void _nextButton_Click( object sender, EventArgs e )
        {
            if ( _nextClickHandler != null )
            {
                _nextClickHandler.Invoke();
            }
        }

        private void ButtonStateChecker( bool state )
        {
            _nextButton.Enabled = state;
        }

        private void OnFinishClick()
        {
            Trace.WriteLine( "SubscribeToSearchFeeds -- OnFinishClick." );
            _nextButton.Enabled = false;
            if ( _feedsToSubscribe == null )
            {
                throw new InvalidOperationException( "Trying to finish wizard with unknown feeds to subscribe" );
            }
            if ( _feedsToSubscribe.Length == 0 )
            {
                DoCancel();
                Close();
                return;
            }
            if ( _feedsToSubscribe.Length == 1 && 
                RSSPlugin.GetExistingFeed( _feedsToSubscribe [0].Resource.GetStringProp( Props.URL ) ) != null )
            {
                MessageBox.Show( this, "You have already subscribed to this feed.",
                                       "Subscribe to Feed" );
                DoCancel();
                return;
            }

            IResource parentGroup = _titleGroupPane.SelectedGroup;
            if ( parentGroup == null )
            {
                parentGroup = RSSPlugin.RootFeedGroup;
            }

            Trace.WriteLine( "SubscribeToSearchFeeds -- Starting to link feeds to parent." );
            foreach( ResourceProxy proxy in _feedsToSubscribe )
            {
                if ( proxy == _newFeedProxy )
                {
                    _newFeedProxy = null;
                }
                proxy.BeginUpdate();
                try
                {
                    if ( _feedsToSubscribe.Length == 1 )
                    {
                        proxy.SetProp( Core.Props.Name, _titleGroupPane.FeedTitle );
                    }
                    proxy.DeleteProp( Props.Transient );
                    proxy.SetProp( Core.Props.Parent, parentGroup );
                    Trace.WriteLine( "SubscribeToSearchFeeds -- Link feed to parent [" + parentGroup.DisplayName + "]" );
                }
                finally
                {
                    proxy.EndUpdate();
                    Trace.WriteLine( "SubscribeToSearchFeeds -- EndUpdate called for a feed" );
                }
                Core.WorkspaceManager.AddToActiveWorkspace( proxy.Resource );
                Trace.WriteLine( "SubscribeToSearchFeeds -- AddToActiveWorkspace called for a feed." );
                RSSPlugin.GetInstance().QueueFeedUpdate( proxy.Resource );
                Trace.WriteLine( "SubscribeToSearchFeeds -- QueueFeedUpdate called for a feed." );
            }

            Core.UIManager.BeginUpdateSidebar();
            if ( Core.TabManager.ActivateTab( "Feeds" ) )
            {
                Core.LeftSidebar.ActivateViewPane( "Feeds" );
            }
            Core.UIManager.EndUpdateSidebar();
            RSSPlugin.RSSTreePane.SelectResource( _feedsToSubscribe [0].Resource );
            RSSPlugin.SaveSubscription();

            _newFeedProxy = null;
            _feedsToSubscribe = null;
            Close();
        }
        #endregion Back/Next/Finish Handlers

        private void SubscribeForm_SizeChanged(object sender, EventArgs e)
        {
            Height = 461;
        }
    }
}