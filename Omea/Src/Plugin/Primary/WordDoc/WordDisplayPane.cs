/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.UI.Interop;

namespace JetBrains.Omea.WordDocPlugin
{
	/// <summary>
	/// Pane for displaying Word or RTF documents.
	/// </summary>
	public class WordDisplayPane : UserControl, IDisplayPane, IContextProvider
	{
        private JetRichTextBox _richTextBox;
        private System.Windows.Forms.Timer _wordHighlightTimer;
        private System.ComponentModel.IContainer components;
        private WordPtr[] _wordsToHighlight;
        private int _lastHighlightIndex;
        private int _lastHighlightOffset;
        private IResource _resource;
        private string _sourceFileName;
        private Process _converterProcess;
        private string _convertedFileName;
        private StreamReader _converterOutputReader;
        private StreamWriter _converterOutputWriter;
        private bool _killedConverter = false;

	    public WordDisplayPane()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

            Controls.Add( Core.WebBrowser );
            Core.WebBrowser.Dock = DockStyle.Fill;
            Core.WebBrowser.Visible = false;
			Core.WebBrowser.ContextProvider = this;

			_richTextBox.ContextProvider = this;
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
            this.components = new System.ComponentModel.Container();
            this._richTextBox = new GUIControls.JetRichTextBox();
            this._wordHighlightTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // _richTextBox
            // 
            this._richTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this._richTextBox.Location = new System.Drawing.Point(0, 0);
            this._richTextBox.Name = "_richTextBox";
            this._richTextBox.ReadOnly = true;
            this._richTextBox.Size = new System.Drawing.Size(150, 150);
            this._richTextBox.TabIndex = 0;
            this._richTextBox.Text = "";
            this._richTextBox.Visible = false;
			_richTextBox.BackColor = SystemColors.Window;
			_richTextBox.BorderStyle = BorderStyle.None;
			// 
            // _wordHighlightTimer
            // 
            this._wordHighlightTimer.Interval = 250;
            this._wordHighlightTimer.Tick += new System.EventHandler(this._wordHighlightTimer_Tick);
            // 
            // WordDisplayPane
            // 
            this.Controls.Add(this._richTextBox);
            this.Name = "WordDisplayPane";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.WordDisplayPane_KeyDown);
            this.ResumeLayout(false);

        }
		#endregion

	    public Control GetControl()
	    {
	        return this;
	    }

	    public void DisplayResource( IResource resource )
	    {
            _wordsToHighlight = null;
			_resource = resource;

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
                    if ( WordDocPlugin.IsRtfFile( _sourceFileName ) )
                    {
                        LoadRtf( _sourceFileName );
                        Core.FileResourceManager.CleanupSourceFile( resource, _sourceFileName );
                    }
                    else
                        LoadHtml( _sourceFileName );                
                }
                catch( Exception ex )
                {
                    ShowError( "Failed to open " + _sourceFileName + ". " + ex.Message );
                }
            }
	    }

	    private void ShowError( string error )
	    {
            _richTextBox.Visible = false;
            Core.WebBrowser.Visible = true;
			StringWriter	sw = new StringWriter();
			sw.WriteLine("<html><body style=\"font-family: Tahoma; font-size: 8pt; text-align: center;\">");
			sw.WriteLine("<p style=\"color: red;\">The document could not be displayed.</p>");
			sw.WriteLine("<p>{0}</p>", error);
			sw.WriteLine("</body></html>");
			Core.WebBrowser.ShowHtml( sw.ToString(), WebSecurityContext.Restricted, null );
		}

        private void LoadRtf( string fileName )
        {
            Core.WebBrowser.Visible = false;
            _richTextBox.Visible = true;
            _richTextBox.Clear();
            FileStream fs = new FileStream( fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite );
            try
            {
                StreamReader reader = new StreamReader( fs, Encoding.Default );                
                _richTextBox.Rtf = Utils.StreamReaderReadToEnd( reader );
            }
            finally
            {
                fs.Close();
            }
        }

        private void LoadHtml( string fileName )
        {
            _richTextBox.Visible = false;
            Core.WebBrowser.Visible = true;

			Core.WebBrowser.ShowHtml( "<html><body style=\"font-family: Tahoma; font-size: 8pt; text-align: center; margin-top: 14pt;\">Please wait while the document is being converted…</body></html>", WebSecurityContext.Restricted, null );
			_killedConverter = false;
            _converterProcess = WordDocPlugin.CreateWvWareProcess( fileName, "wvHtml.xml", false );
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
					"WVWare conversion failed. Could not start convertion process: converter not found.");
				return;
			}

            _convertedFileName = Path.Combine( WordDocPlugin.TempDir, 
                Path.GetFileNameWithoutExtension( fileName ) + ".html" );
            _converterOutputReader = new StreamReader( _converterProcess.StandardOutput.BaseStream, 
                Encoding.UTF8 );
            _converterOutputWriter = new StreamWriter( _convertedFileName, false, Encoding.UTF8 );
            new Thread( ProcessConverterOutput ).Start();
        }

	    private void ProcessConverterOutput()
	    {
            try
            {
                string str;
                while ((str = _converterOutputReader.ReadLine()) != null)
                {
                    _converterOutputWriter.Write( str );
                }
            }
            finally
            {
                _converterOutputWriter.Close();
            }
	    }

	    private void _converterProcess_OnExited( object sender, EventArgs e )
	    {
            if ( _killedConverter )
                return;
            
            if ( _converterProcess.ExitCode == 0 )
            {
				Core.WebBrowser.Stop();	// If we do not, hilite will be applied to the "please wait" banner
                Core.WebBrowser.NavigateInPlace( _convertedFileName );
                if ( _wordsToHighlight != null )
                {
                    Core.WebBrowser.HighlightWords( _wordsToHighlight, 0 );
                }
            }
            else if ( _converterProcess.ExitCode == -2 )
            {
                Core.UIManager.QueueUIJob( new ShowErrorDelegate( ShowError ),
                    Path.GetFileName( _sourceFileName ) + " is a password-protected document." );
            }
            else if ( _converterProcess.ExitCode == -5 )
            {
                Core.UIManager.QueueUIJob( new ShowErrorDelegate( ShowError ),
                    Path.GetFileName( _sourceFileName ) + " is not a valid Microsoft Word document or a Rich Text Format file." );
            }
            else if ( _converterProcess.ExitCode == -7 )
            {
                Core.UIManager.QueueUIJob( new ShowErrorDelegate( ShowError ),
                    "No space left on disk to convert " + Path.GetFileName( _sourceFileName ) + "." );
            }
            else
            {
                string error = Utils.StreamReaderReadToEnd( _converterProcess.StandardError );
                Core.UIManager.QueueUIJob( new ShowErrorDelegate( ShowError ),
                    "WVWare conversion failed. " + error );
            }

            Core.FileResourceManager.CleanupSourceFile( _resource, _sourceFileName );
        }

        private delegate void ShowErrorDelegate( string error );

	    public void HighlightWords( WordPtr[] words )
	    {
            words = DocumentSection.RestrictResults( words, DocumentSection.BodySection );
            if ( _richTextBox.Visible )
				_richTextBox.HighlightWords( words );
            else
            {
                Core.WebBrowser.HighlightWords( words, 0 );
                _wordsToHighlight = words;
            }
	    }

		// TODO: remove?
/*
	    private void HighlightRtfWords( WordPtr[] words )
	    {
            // we use plain text for indexing, so the offsets will not match => 
            // use token-based highlighting

            _wordsToHighlight = words;
            _lastHighlightOffset = 0;
            for( _lastHighlightIndex = 0; _lastHighlightIndex < words.Length; _lastHighlightIndex++ )
            {
                // if there are too many words to highlight - use async highlighting, in
                // order not to lock up the UI for too long
                if ( _lastHighlightIndex == 5 )
                {
                    _wordHighlightTimer.Start();
                    break;
                }
                HighlightNextWord();
            }
        }
*/

	    private void HighlightNextWord()
	    {
            if ( _wordsToHighlight [_lastHighlightIndex].Section != DocumentSection.BodySection )
            {
                return;
            }

            int offset = _richTextBox.Find( _wordsToHighlight [_lastHighlightIndex].Text, 
                _lastHighlightOffset, RichTextBoxFinds.WholeWord );
            if ( offset >= 0 )
            {
                int highlightLength = _wordsToHighlight [_lastHighlightIndex].Text.Length;
                _lastHighlightOffset = offset + highlightLength;

                _richTextBox.Select( offset, highlightLength );
                
                CHARFORMAT2 fmt = new CHARFORMAT2();
                fmt.cbSize = Marshal.SizeOf( fmt );
                fmt.dwMask = CFM.BACKCOLOR | CFM.COLOR;
                fmt.crBackColor = ColorTranslator.ToWin32( SystemColors.Highlight );
                fmt.crTextColor = ColorTranslator.ToWin32( SystemColors.HighlightText );

                Win32Declarations.SendMessage( _richTextBox.Handle, EditMessage.SETCHARFORMAT, SCF.SELECTION, ref fmt );
            }
	    }

        private void _wordHighlightTimer_Tick( object sender, EventArgs e )
        {
            if ( _lastHighlightIndex < _wordsToHighlight.Length )
            {
                HighlightNextWord();
                _lastHighlightIndex++;
                if ( _lastHighlightIndex == _wordsToHighlight.Length )
                {
                    _wordHighlightTimer.Stop();
                }
            }
        }

        public void EndDisplayResource( IResource resource )
	    {
            if ( _converterProcess != null  )
            {
                bool exited = true;
                try
                {
                    exited = _converterProcess.HasExited;
                }
                catch( InvalidOperationException )
                {
                    // Really, not started at all.
                    _killedConverter = true;
                }
                if( ! exited )
                {
                    _converterProcess.Kill();
                    _killedConverter = true;
                }
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
            if ( _richTextBox.Visible )
            {
                format = TextFormat.Rtf;
                return _richTextBox.SelectedRtf;
            }
            else
            {
                format = TextFormat.Html;
                return Core.WebBrowser.SelectedHtml;
            }
	    }

	    public string GetSelectedPlainText()
	    {
            if ( _richTextBox.Visible )
            {
                return _richTextBox.SelectedText;
            }
            else
            {
                return Core.WebBrowser.SelectedText;
            }
	    }

	    public bool CanExecuteCommand( string command )
	    {
            if ( _richTextBox.Visible )
            {
                return _richTextBox.CanExecuteCommand( command );
            }
            else
            {
                return Core.WebBrowser.CanExecuteCommand( command );
            }
	    }

	    public void ExecuteCommand( string command )
	    {
            if ( _richTextBox.Visible )
            {
                _richTextBox.ExecuteCommand( command );
            }
            else
            {
                Core.WebBrowser.ExecuteCommand( command );
            }
	    }

        private void WordDisplayPane_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = Core.ActionManager.ExecuteKeyboardAction(
                new ActionContext( ActionContextKind.Keyboard, null, _resource.ToResourceList() ), e.KeyData );
        }

		#region IContextProvider Members

		public IActionContext GetContext( ActionContextKind kind )
		{
			return new ActionContext(kind, this, (_resource != null ? _resource.ToResourceList() : null));
		}

		#endregion

    }
}
