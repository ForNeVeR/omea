// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ExcelDocPlugin
{
    /// <summary>
    /// Pane for displaying Excel documents.
    /// </summary>
    public class ExcelDisplayPane : UserControl, IDisplayPane2
    {
        private System.ComponentModel.IContainer components;
        private WordPtr[] _wordsToHighlight;
        private IResource _resource;
        private string _sourceFileName;
        private Process _converterProcess;
        private StreamReader _converterOutputReader;
        private StringWriter _converterOutputWriter;
        private bool _killedConverter = false;

        public ExcelDisplayPane()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            Controls.Add( Core.WebBrowser );
            Core.WebBrowser.Dock = DockStyle.Fill;
            Core.WebBrowser.Visible = true;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if(components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            //
            // ExcelDisplayPane
            //
            this.Name = "ExcelDisplayPane";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ExcelDisplayPane_KeyDown);

        }
        #endregion

        public Control GetControl()
        {
            return this;
        }

        public void DisplayResource( IResource resource )
        {
            //  If we got a sign that the resource's file was not parsed
            string errorBody = resource.GetPropText( Core.Props.LastError );
            if( !String.IsNullOrEmpty( errorBody ) )
            {
                ShowError( errorBody );
            }
            else
            {
                try
                {
                    _sourceFileName = Core.FileResourceManager.GetSourceFile( resource );
                    _resource = resource;
                    LoadHtml( _sourceFileName );
                }
                catch( Exception ex )
                {
                    ShowError( "Failed to open Excel document \"" + _sourceFileName + "\". " + ex.Message );
                }
            }
        }

        private void ExcelDisplayPane_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = Core.ActionManager.ExecuteKeyboardAction(
                new ActionContext( ActionContextKind.Keyboard, this, _resource.ToResourceList() ), e.KeyData );
        }

        private static void ShowError( string error )
        {
			error = error.Replace( "&", "&amp;" ).Replace( "\"", "&quot;" ).Replace( "<", "&lt;" ).Replace( ">", "&gt;" );
            Core.WebBrowser.Visible = true;
			Core.WebBrowser.ShowHtml( "<html><body style=\"font-family: Tahoma; font-size: 8pt; text-align: left; color: maroon;\"><pre>" + error + "</pre></body></html>", WebSecurityContext.Trusted, null );
        }

        private void LoadHtml( string fileName )
        {
            Core.WebBrowser.Visible = true;
            Core.WebBrowser.ShowHtml( "<html><body style=\"font-family: Tahoma; font-size: 8pt; text-align: center;\">Please wait while the file is being converted…</body></html>", WebSecurityContext.Trusted, null );
            _killedConverter = false;
            _converterProcess = ExcelDocPlugin.CreateConverterProcess( fileName, false );
            _converterProcess.EnableRaisingEvents = true;
            _converterProcess.Exited += _converterProcess_OnExited;
			try
			{
				if(!_converterProcess.Start())
					throw new Exception();
			}
			catch
			{
				_converterProcess = null;
				Core.UIManager.QueueUIJob( new ShowErrorDelegate( ShowError ),
					"Failed to convert Excel file to HTML. Cannot start the convertion process: converter is not found.");
				return;
			}

            _converterOutputReader = new StreamReader( _converterProcess.StandardOutput.BaseStream,
                Encoding.UTF8 );
            _converterOutputWriter = new StringWriter();
            new Thread( ProcessConverterOutput ).Start();
        }

        private void ProcessConverterOutput()
        {
            try
            {
                string str;
                while ((str = _converterOutputReader.ReadLine()) != null)
                {
                    _converterOutputWriter.WriteLine( str );
                }
            }
            finally
            {
                _converterOutputWriter.Close();
            }
        }

        private void _converterProcess_OnExited( object sender, EventArgs e )
        {
            if ( _killedConverter || null == _converterProcess)
                return;

            if ( _converterProcess.ExitCode == 0 )
            {
                Core.WebBrowser.ShowHtml( _converterOutputWriter.ToString(), WebSecurityContext.Restricted, _wordsToHighlight );
                _wordsToHighlight = null;
            }
            else
			{
                string error = Utils.StreamReaderReadToEnd( _converterProcess.StandardError );
                Core.UIManager.QueueUIJob( new ShowErrorDelegate( ShowError ),
                    "Failed to convert Excel file to HTML. " + error );
            }

            Core.FileResourceManager.CleanupSourceFile( _resource, _sourceFileName );
        }

        private delegate void ShowErrorDelegate( string error );

        public void HighlightWords( WordPtr[] words )
        {
            words = DocumentSection.RestrictResults( words, DocumentSection.BodySection );
	        Core.WebBrowser.HighlightWords( words, 0 );
            _wordsToHighlight = words;
        }

        public void EndDisplayResource( IResource resource )
        {
			if ( _converterProcess != null && !_converterProcess.HasExited )
			{
                //  In the case the process was already killed - nothing to do
                try { _converterProcess.Kill();  }
                catch( InvalidOperationException ){}

				_killedConverter = true;
			}
        }

        public void DisposePane()
        {
            Controls.Remove( Core.WebBrowser );
            Core.WebBrowser.Visible = true;
            Dispose();
        }

        public string GetSelectedText( ref TextFormat format )
        {
			format = TextFormat.Html;
			return Core.WebBrowser.SelectedHtml;
		}

        public string GetSelectedPlainText()
        {
			return Core.WebBrowser.SelectedText;
		}

        public bool CanExecuteCommand( string command )
        {
			return Core.WebBrowser.CanExecuteCommand( command );
		}

        public void ExecuteCommand( string command )
        {
			Core.WebBrowser.ExecuteCommand( command );
        }

    	public void DisplayResource( IResource resource, WordPtr[] wordsToHighlight )
    	{
			_wordsToHighlight = DocumentSection.RestrictResults( wordsToHighlight, DocumentSection.BodySection );
			DisplayResource( resource );
    	}
    }
}
