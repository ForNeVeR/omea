// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using Microsoft.Office.Core;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Base;
using System.Globalization;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.COM;

namespace JetBrains.Omea.MSPowerPointPlugin
{
    public class MSPowerPointPlugin : IPlugin, IResourceDisplayer, IDisplayPane, IResourceTextProvider
    {
        private const int _FileCopyBufSize = 4096;
        private object FALSE = false;
        private object TRUE = true;
        PowerPoint.Application _powerPoint;
        private object MissingValue = Missing.Value;
        private AxIEBrowser.AxCIEBrowserCtl _preview = null;
        private string _strFileName = null;
        private bool _navigateComplete = false;
        private bool _highlightWords = false;
        private WordPtr[] _words;
        private Tracer _tracer = new Tracer( "MSPowerPointPlugin" );

        private void InitializePowerPoint()
        {
            if ( _powerPoint != null ) return;
            try
            {
                _powerPoint = new PowerPoint.ApplicationClass();
                if ( _powerPoint != null )
                {
                    _powerPoint.AutomationSecurity = MsoAutomationSecurity.msoAutomationSecurityForceDisable;
                }
            }
            catch ( Exception ex )
            {
                _tracer.Trace( "Cannot load MSPowerPoint" );
                _tracer.TraceException( ex );
            }
        }

        private void ShutdownPowerPoint( )
        {
            if ( _powerPoint == null ) return;
            try
            {
                _powerPoint.Quit( );
            }
            catch ( System.Runtime.InteropServices.COMException ex )
            {
                _tracer.TraceException( ex );
            }
            catch ( Exception ex )
            {
                _tracer.TraceException( ex );
            }
            COM_Object.Release( _powerPoint );
        }

        #region IPlugin Members

        public void Register()
        {
            _core = ICore.Instance;
            RegisterTypes();
            _core.PluginLoader.RegisterResourceTextProvider( "MSPowerPointFile", this );
            _core.PluginLoader.RegisterResourceDisplayer( "MSPowerPointFile", this );
        }

        public void Startup()
        {
        }

        public void Shutdown()
        {
            Core.FileResourceManager.DeregisterFileResourceType( "MSPowerPointFile" );
        }

        #endregion

        #region IResourceDisplayer members

        IDisplayPane IResourceDisplayer.CreateDisplayPane( string resourceType )
        {
            if ( _preview == null )
            {
                _preview = new AxIEBrowser.AxCIEBrowserCtl();
                this._preview.NavigateComplete += new AxIEBrowser._IIEBrowserCtlEvents_NavigateCompleteEventHandler( this.NavigateComplete2 );
            }
            return this;
        }

        Control IDisplayPane.GetControl()
        {
            return _preview;
        }

        PowerPoint.Presentation _presentation;

        private void NavigateComplete2( object sender, AxIEBrowser._IIEBrowserCtlEvents_NavigateCompleteEvent e )
        {
            object o = e.document;
            object oDocument = o.GetType().InvokeMember("Document",BindingFlags.GetProperty,null,o,null);
            _presentation = (PowerPoint.Presentation)oDocument;
            _navigateComplete = true;
            if ( _highlightWords == true )
            {
                HighlightWords();
                _navigateComplete = false;
                _highlightWords = false;
            }
        }

        void IDisplayPane.DisplayResource( IResource resource )
        {
            _navigateComplete = false;
            _highlightWords = false;
            _strFileName = Core.FileResourceManager.GetSourceFile( resource );
            if ( _strFileName != null )
            {
                _preview.DLControl = (int) (DLCTL.DLIMAGES | DLCTL.NO_SCRIPTS | DLCTL.NO_BEHAVIORS );
                _preview.Navigate( _strFileName );
            }
        }

        void IDisplayPane.EndDisplayResource( IResource resource )
        {
            if ( _strFileName != null )
            {
                try
                {
                    Core.FileResourceManager.CleanupSourceFile( resource, _strFileName );
                }
                catch ( Exception exception )
                {
                    _tracer.TraceException( exception );
                }
            }
        }

        void IDisplayPane.DisposePane()
        {
        }

        public string GetSelectedText( ref TextFormat format )
        {
            return null;
        }

        public string GetSelectedPlainText()
        {
            return null;
        }

        bool ICommandProcessor.CanExecuteCommand( string action )
        {
            return false;
        }

        void ICommandProcessor.ExecuteCommand( string action )
        {
        }

        private static CompareInfo cmp = CultureInfo.CurrentCulture.CompareInfo;

        private void HighlightWords()
        {
            if ( _words == null ) return;
            PowerPoint.Slides slides = _presentation.Slides;
            int count = 0;
            foreach ( PowerPoint.Slide slide in slides )
            {
                count++;
                PowerPoint.Shapes shapes = slide.Shapes;
                foreach ( PowerPoint.Shape shape in shapes )
                {
                    if ( shape.HasTextFrame == MsoTriState.msoTrue )
                    {
                        if ( cmp.Compare( shape.TextFrame.TextRange.Text, _words [0].Text, CompareOptions.StringSort | CompareOptions.IgnoreCase ) == 0 )
                        {
                            _presentation.SlideShowWindow.View.GotoSlide( count, MsoTriState.msoTrue );
                            break;
                        }
                        //if ( shape.TextFrame.TextRange.Text.CompareTo() )
                        //_presentation.SlideShowWindow.View.GotoSlide( count, MsoTriState.msoTrue );
                        //shape.Select( MsoTriState.msoTrue );
                        //slide.MoveTo( 1 );
                        //break;
                    }
                }
            }
        }

        void IDisplayPane.HighlightWords( WordPtr[] words )
        {
            _words = words;
            _highlightWords = true;
            if ( _navigateComplete == true )
            {
                HighlightWords();
                _highlightWords = false;
                _navigateComplete = false;
            }
        }

        #endregion

        bool IResourceTextProvider.ProcessResourceText( IResource resource, IResourceTextConsumer consumer )
        {
            consumer.AddDocumentFragment( resource.Id, GetResourceText( resource ) );
            return true;
        }
        public void  RejectResult() {}

        #region implementation details

        /**
         * if stream is null it is requested from corresponding IStreamProvider using FileResource helper
         */
        private string GetResourceText( IResource resource )
        {
            string fileName = Core.FileResourceManager.GetSourceFile( resource );
            if ( fileName != null )
            {
                string result = GetDocumentText( fileName );
                Core.FileResourceManager.CleanupSourceFile( resource, fileName );
                return result;
            }

            return String.Empty;
        }

        private string GetDocumentText( object path )
        {
            StringBuilder result = new StringBuilder();
            try
            {
                InitializePowerPoint();
                if ( _powerPoint != null )
                {
                    PowerPoint.Presentation presentation =
                        _powerPoint.Presentations.Open( (string)path, MsoTriState.msoTrue, MsoTriState.msoTrue, MsoTriState.msoFalse );
                    PowerPoint.Slides slides = presentation.Slides;
                    int count = slides.Count;
                    foreach ( PowerPoint.Slide slide in slides )
                    {
                        PowerPoint.Shapes shapes = slide.Shapes;
                        count = shapes.Count;
                        foreach ( PowerPoint.Shape shape in shapes )
                        {
                            if ( shape.HasTextFrame == MsoTriState.msoTrue )
                            {
                                result.Append( shape.TextFrame.TextRange.Text ).Append( " " );
                            }
                        }
                        //foreach ( PowerPoint.Comment comment in slide.Comments )
                        //{
                        //comment.Text;
                        //}
                    }
                }
            }
            catch ( Exception exception )
            {
                _tracer.TraceException( exception );
            }
            ShutdownPowerPoint( );

            return result.ToString();
        }

        private void RegisterTypes()
        {
            string exts = _core.SettingStore.ReadString( "FilePlugin", "MSPowerPointExts" );
            exts = ( exts.Length == 0 ) ? ".ppt" : exts + ",ppt";
            string[] extsArray = exts.Split( ',' );
            for( int i = 0; i < extsArray.Length; ++i )
            {
                extsArray[ i ] = extsArray[ i ].Trim();
            }
            Core.FileResourceManager.RegisterFileResourceType( "MSPowerPointFile", "PowerPoint Presentation",
                "Name", ResourceTypeFlags.FileFormat, this, extsArray );
        }

        ICore      _core;
        #endregion
    }

}
