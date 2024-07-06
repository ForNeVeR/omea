// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using Microsoft.Office.Core;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OmniaMea.Base;
using OmniaMea.Diagnostics;
using OmniaMea.FileTypes;
using OmniaMea.OpenAPI;
using OmniaMea.COM;

namespace OmniaMea.MSWordPlugin
{
    public class MSWordPlugin: IPlugin, IResourceDisplayer
    {
        private const int _FileCopyBufSize = 4096;
        private Word.Application _MSWord;
        private object FALSE = false;
        private object TRUE = true;
        private object MissingValue = Missing.Value;
        private string _strFileName = null;
        private AxIEBrowser.AxCIEBrowserCtl _preview = null;
        private Word._Document _document;
        private bool _navigateComplete = false;
        private bool _highlightWords = false;
        private object[] _tokenOffsets;
        private string[] _wordTokens;
        private bool _startupStatus = true;
        private bool _cleanup = false;
        private IResource _cleanupResource = null;
        private Tracer _tracer = new Tracer( "MSWordPlugin" );
        private static string cMSWordFile = "MSWordFile";
        private int _savedEditFlags = -1;
        private Timer _refreshTimer = new Timer();
        private object _automationSecurity;

        #region IPlugin Members

        public string[] Register( IPluginEnvironment pluginEnvironment )
        {
            _startupStatus = LoadWordInstance();
            StoreAutomationSecurity();
            _pluginEnvironment = pluginEnvironment;
            RegisterTypes();

            _refreshTimer.Interval = 1000;
            _refreshTimer.Tick += new EventHandler(_refreshTimer_Tick);

            return new string[] { cMSWordFile };
        }

        public bool Startup()
        {
            return _startupStatus;
        }

        public void Dispose()
        {
            FileTypesMap.GetInstance().DeregisterFileResourceType( cMSWordFile, _pluginEnvironment );
            try
            {
                if ( _MSWord != null )
                {
                    _MSWord.Quit( ref FALSE, ref MissingValue, ref MissingValue );
                }
            }
            catch ( Exception exception )
            {
                _tracer.TraceException( exception );
            }
        }

        #endregion

        #region IResourceDisplayer members

        Control IResourceDisplayer.CreateDisplayPane(string resourceType)
        {
            if ( _preview == null )
            {
                _preview = new AxIEBrowser.AxCIEBrowserCtl();
                this._preview.NavigateComplete += new AxIEBrowser._IIEBrowserCtlEvents_NavigateCompleteEventHandler( this.NavigateComplete2 );
            }
            return _preview;
        }

        public void NavigateComplete2( object sender, AxIEBrowser._IIEBrowserCtlEvents_NavigateCompleteEvent e )
        {
            try
            {
                if ( _cleanup )
                {
                    if ( _cleanupResource != null ) FileResource.CleanupSourceFile( _cleanupResource, _strFileName );
                    _cleanupResource = null;
                    _cleanup = false;
                    return;
                }
                object o = e.document;
                object oDocument = o.GetType().InvokeMember("Document",BindingFlags.GetProperty,null,o,null);
                _document = (Word._Document)oDocument;
                _navigateComplete = true;
                if ( _highlightWords == true )
                {
                    HighlightWords();
                    _navigateComplete = false;
                    _highlightWords = false;
                }
            }
            catch ( Exception exception )
            {
                _tracer.TraceException( exception );
            }
        }

        void IResourceDisplayer.DisplayResource( Control displayPane, IResource resource )
        {
            _navigateComplete = false;
            _highlightWords = false;
            if ( _MSWord != null )
            {
                try
                {
                    _MSWord.Quit( ref FALSE, ref MissingValue, ref MissingValue );
                }
                catch ( Exception exception )
                {
                    _tracer.TraceException( exception );
                }
                _MSWord = null;
            }
            _strFileName = FileResource.GetSourceFile( resource, _pluginEnvironment );
            if ( _strFileName != null )
            {
                _savedEditFlags = FileTypesMap.GetEditFlags( "Word.Document" );
                FileTypesMap.SetEditFlags( "Word.Document", _savedEditFlags | 0x10000 );   // FTA_OpenIsSafe
                _refreshTimer.Start();

                AxIEBrowser.AxCIEBrowserCtl preview = ( AxIEBrowser.AxCIEBrowserCtl ) displayPane;
                preview.Navigate( _strFileName );
            }
        }
        void IResourceDisplayer.EndDisplayResource( Control displayPane, IResource resource )
        {
            if ( _savedEditFlags != -1 )
            {
                FileTypesMap.SetEditFlags( "Word.Document", _savedEditFlags );
                _savedEditFlags = -1;
            }

            if ( _strFileName != null )
            {
                try
                {
                    _cleanup = true;
                    _cleanupResource = resource;
                    _preview.Navigate( "about:blank" );
                }
                catch ( Exception exception )
                {
                    _tracer.TraceException( exception );
                }
            }
        }

        void IResourceDisplayer.DisposeDisplayPane( Control displayPane )
        {
        }

        private void HighlightWords()
        {
            try
            {
                if ( _wordTokens == null || _wordTokens.Length == 0 ) return;
                Word.Range range = _document.Content;
                Word.Find find = range.Find;
                object findText = _wordTokens[0];
                find.ClearFormatting();
                object wordWrap = Word.WdFindWrap.wdFindContinue;
                bool found = find.Execute( ref findText, ref FALSE, ref TRUE, ref MissingValue, ref MissingValue, ref MissingValue,
                    ref MissingValue, ref MissingValue, ref MissingValue, ref MissingValue, ref MissingValue, ref MissingValue,
                    ref MissingValue, ref MissingValue, ref MissingValue );
                if ( found )
                {
                    range.Select();
                }
            }
            catch ( Exception exception )
            {
                _tracer.TraceException( exception );
            }
        }

        void IResourceDisplayer.HighlightWords( Control displayPane, string[] wordTokens, object[] tokenOffsets )
        {

            _tokenOffsets = tokenOffsets;
            _wordTokens = wordTokens;
            _highlightWords = true;
            if ( _navigateComplete == true )
            {
                HighlightWords();
                _navigateComplete = false;
                _highlightWords = false;
            }
        }

        void IResourceDisplayer.FillResourceImageList( ImageList imgList, int imageSize, int bitDepth )
        {
            try
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream( "MSWordPlugin.Word.ico" );
                if( stream != null )
                {
                    Icon wordIcon = new Icon( stream );
                    imgList.Images.Add( wordIcon );
                }
            }
            catch( Exception exception )
            {
                _tracer.Trace( "Error loading Word icon resource..." );
                _tracer.TraceException( exception );
            }
        }

        int IResourceDisplayer.GetResourceIconIndex( IResource resource )
        {
            return GetDefaultIconIndex( resource.Type );
        }

        public int GetDefaultIconIndex( string resType )
        {
            return ( resType == cMSWordFile ) ? 0 : -1;
        }

        #endregion

        public void ProcessResourceText( int resID, IFullTextIndexer indexer )
        {
            IResource resource = _pluginEnvironment.ResourceStore.LoadResource( resID );
            ProcessResourceStream( resource, indexer );
        }

        #region implementation details

        private void ProcessResourceStream( IResource resource, IFullTextIndexer indexer )
        {
            indexer.AddDocumentFragment( resource.ID, GetResourceText( resource ) );
        }

        private string GetResourceText( IResource resource )
        {
            string result = string.Empty;
            string fileName = null;
            try
            {
                fileName = FileResource.GetSourceFile( resource, _pluginEnvironment );
                if ( fileName != null )
                {
                    result = GetDocumentText( fileName, 0 );
                    FileResource.CleanupSourceFile( _pluginEnvironment, resource, fileName );
                    return result;
                }
            }
            catch ( Exception exception )
            {
                _tracer.TraceException( exception );
            }

            return result;
        }

        private void StoreAutomationSecurity()
        {
            try
            {
                _automationSecurity = _MSWord.GetType().InvokeMember( "AutomationSecurity", BindingFlags.GetProperty, null, _MSWord, null );
            }
            catch ( Exception exception )
            {
                _tracer.TraceException( exception );
            }
        }

        private void DisableAutomationSecurity()
        {
            try
            {
                object[] Parameters = new Object[1];
                Parameters[0] = MsoAutomationSecurity.msoAutomationSecurityForceDisable;
                _MSWord.GetType().InvokeMember( "AutomationSecurity", BindingFlags.SetProperty, null, _MSWord, Parameters );
            }
            catch ( Exception exception )
            {
                _tracer.TraceException( exception );
            }
        }

        private void RestoreAutomationSecurity()
        {
            try
            {
                object[] Parameters = new Object[1];
                Parameters[0] = _automationSecurity;
                _MSWord.GetType().InvokeMember( "AutomationSecurity", BindingFlags.SetProperty, null, _MSWord, Parameters );
            }
            catch ( Exception exception )
            {
                _tracer.TraceException( exception );
            }
        }

        private bool LoadWordInstance()
        {
            if ( _MSWord != null )
            {
                try
                {
                    _MSWord.Quit( ref FALSE, ref MissingValue, ref MissingValue );
                }
                catch ( Exception exception )
                {
                    _tracer.TraceException( exception );
                }
                _MSWord = null;
            }
            try
            {
                _MSWord = new Word.ApplicationClass();
                /*
                try
                {
                    object[] Parameters = new Object[1];
                    Parameters[0] = MsoAutomationSecurity.msoAutomationSecurityForceDisable;
                    _MSWord.GetType().InvokeMember( "AutomationSecurity", BindingFlags.SetProperty, null, _MSWord, Parameters );
                }
                catch ( Exception exception )
                {
                    _tracer.TraceException( exception );
                }
                */
            }
            catch ( Exception exception )
            {
                _tracer.Trace( "Cannot load MSWord..." );
                _tracer.TraceException( exception );
                _MSWord = null;
                return false;
            }

            return true;
        }

        private string GetDocumentTextWithReloadingWord( object path, int tryCount )
        {
            if ( tryCount++ > 1 ) return string.Empty;
            if ( LoadWordInstance() )
                return GetDocumentText( path, tryCount );
            else
                return string.Empty;
        }

        private string GetDocumentText( object path, int tryCount )
        {
            if ( tryCount++ > 1 ) return string.Empty;
            Word.Documents documents = null;
            try
            {
                if ( _MSWord == null )
                {
                    if ( !LoadWordInstance() ) return string.Empty;
                }
                documents = _MSWord.Documents;
            }
            catch ( InvalidCastException exception )
            {
                _tracer.TraceException( exception );
                return GetDocumentTextWithReloadingWord( path, tryCount );
            }
            catch ( System.Runtime.InteropServices.COMException exception )
            {
                _tracer.TraceException( exception );
                if ( IsReloadingPossible( exception ) )
                {
                    return GetDocumentTextWithReloadingWord( path, tryCount );
                }
                return string.Empty;
            }
            catch ( Exception exception )
            {
                _tracer.TraceException( exception );
                return string.Empty;
            }

            Word.Document doc = null;
            string bodyText = string.Empty;
            object pass = "yanetormoz";
            try
            {
                DisableAutomationSecurity();
                doc = documents.Open( ref path, ref FALSE, ref TRUE, ref FALSE,
                    ref pass,
                    ref MissingValue, ref MissingValue, ref MissingValue, ref MissingValue,
                    ref MissingValue,
                    ref MissingValue, ref MissingValue );

                Word.Range range = doc.Content;
                bodyText = range.Text;
            }
            catch ( System.Runtime.InteropServices.COMException exception )
            {
                _tracer.TraceException( exception );
                if ( IsReloadingPossible( exception ) )
                {
                    _tracer.Trace( "Try to load new instance of MS Word" );
                    return GetDocumentTextWithReloadingWord( path, tryCount );
                }
                if ( COM_Error.CouldNotOpenMacroStorage( exception ) ) //This exception leads to not closed documents
                {
                    LoadWordInstance();
                    return string.Empty;
                }
            }
            catch ( InvalidCastException exception )
            {
                _tracer.TraceException( exception );
                return GetDocumentTextWithReloadingWord( path, tryCount );
            }
            catch ( Exception exception )
            {
                _tracer.TraceException( exception );
                return string.Empty;
            }
            RestoreAutomationSecurity();

            try
            {
                if ( doc != null )
                {
                    doc.Close( ref FALSE, ref MissingValue, ref MissingValue );
                }
                return bodyText;
            }
            catch ( System.Runtime.InteropServices.COMException exception )
            {
                if ( IsReloadingPossible( exception ) )
                {
                    return GetDocumentTextWithReloadingWord( path, tryCount );
                }
            }
            catch ( InvalidCastException exception )
            {
                _tracer.TraceException( exception );
                return GetDocumentTextWithReloadingWord( path, tryCount );
            }
            catch ( Exception exception )
            {
                _tracer.TraceException( exception );
            }
            return string.Empty;
        }

        private bool IsReloadingPossible( COMException exception )
        {
            _tracer.TraceException( exception );
            return ( COM_Error.IsRPC_ServerIsUnavailable( exception ) ||
                COM_Error.RemoteProcCallFailed( exception ) );
        }

        private void RegisterTypes()
        {
            string exts = _pluginEnvironment.SettingStore.ReadString( "FilePlugin", "MSWordExts" );
            exts = ( exts.Length == 0 ) ? ".doc,.rtf" : exts + ",.doc,.rtf";
            FileTypesMap.GetInstance().RegisterFileResourceType( cMSWordFile, "Source", exts.Split( ',') );
        }

        IPluginEnvironment      _pluginEnvironment;
        #endregion

        private void _refreshTimer_Tick( object sender, EventArgs e )
        {
            _refreshTimer.Stop();
            if ( _preview != null )
            {
                _preview.Refresh();
            }
        }
    }
}
