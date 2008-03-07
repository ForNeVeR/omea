/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.TextIndex;
using JetBrains.UI.Interop;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// The RTF text box control.
	/// </summary>
	public class JetRichTextBox : RichTextBox, ICommandProcessor, IContextProvider
	{
		#region Data

		private IContextProvider _contextProvider;

		private bool _showContextMenu = true;

		private static Regex _rxProtocol = new Regex( "^[A-Za-z]+:" );

		/// <summary>
		/// A set of background and foreground colors for highlighting the words in HTML text
		/// </summary>
		protected static BiColor[] _colorsHighlight = InitColors();

		public class BiColor
		{
			public Color ForeColor;

			public Color BackColor;

			public BiColor( Color colorFore, Color colorBack )
			{
				ForeColor = colorFore;
				BackColor = colorBack;
			}
		}

		/// <summary>
		/// Stores the list of search hits (derived from the WordPtr passed in for highlighting via either scheme) to navigate them and scroll to the first entry when the page loads.
		/// The <see cref="WordPtr.StartOffset"/> field contains the ready-for-use offset in the RTF content.
		/// Should be <c>Null</c> when displaying content without the search terms and non-<c>Null</c> if there are search terms present.
		/// </summary>
		protected WordPtr[] _wordsSearchHits = null;

		/// <summary>
		/// The current search hit. This variable is used for navigating to a prev/next search hit in the document, and is updated upon the navigation.
		/// <c>-1</c> means that either there are no search hits, or there are ones, but we're currently positioned at none (before the first or after the last one).
		/// </summary>
		protected int _nCurrentSearchHit = -1;

		/// <summary>
		/// If an asynchronous highlighting procedure is in progress, persists its state between the operations. Otherwise, <c>Null</c>.
		/// </summary>
		protected AsyncHighlightState _stateHilite = null;

		/// <summary>
		/// While the highlighting is in progress, indicates it.
		/// </summary>
		protected IStatusWriter _statuswriter = null;

		/// <summary>
		/// Flag is <c>True</c> when the KeyDown event has been suppressed and the following KeyPress one should be suppressed as well.
		/// </summary>
		protected bool _isKeyPressHandled = false;

		#endregion

		#region Construction

		public JetRichTextBox()
		{
			HideSelection = false;
			DetectUrls = true;
		}

		#endregion

		#region Attributes

		public IContextProvider ContextProvider
		{
			get { return _contextProvider; }
			set { _contextProvider = value; }
		}

		[DefaultValue( true )]
		public bool ShowContextMenu
		{
			get { return _showContextMenu; }
			set { _showContextMenu = value; }
		}

		/// <summary>
		/// Rich text property which does not use EM_STREAMOUT and EM_STREAMIN for getting
		/// and setting the text.
		/// </summary>
		[Browsable( false )]
		public string RichText
		{
			set
			{
				_wordsSearchHits = null;

				SETTEXTEX ste = new SETTEXTEX();
				ste.flags = 0;
				ste.codepage = 0;

				byte[] data = Encoding.Default.GetBytes( value );
				byte[] pszData = new byte[data.Length + 1];
				Array.Copy( data, pszData, data.Length );
				pszData[ data.Length ] = 0;

				int rc = Win32Declarations.SendMessage( Handle, EditMessage.SETTEXTEX, ref ste, pszData );
				if( rc != 1 )
				{
					throw new Exception( "Setting RichEdit rich text failed" );
				}
			}
		}

		/// <summary>
		/// Plain text property which does not use EM_STREAMOUT and EM_STREAMIN for getting
		/// and setting the text.
		/// </summary>
		[Browsable( false )]
		public string PlainText
		{
			get
			{
				int textLength = Win32Declarations.SendMessage( Handle, Win32Declarations.WM_GETTEXTLENGTH,
				                                                IntPtr.Zero, IntPtr.Zero );
				StringBuilder result = new StringBuilder( textLength );
				Win32Declarations.SendMessage( Handle, Win32Declarations.WM_GETTEXT,
				                               (IntPtr)textLength, result );
				return result.ToString();
			}
		}

		#endregion

		#region Overrides

		protected override void Dispose( bool disposing )
		{
			base.Dispose( disposing );
			_stateHilite = null; // Stop the currently-running highlighting
			if( _statuswriter != null )
			{
				_statuswriter.ClearStatus();
				_statuswriter = null;
			}
			_wordsSearchHits = null;
			_nCurrentSearchHit = -1;
		}

		protected override void WndProc( ref Message m )
		{
			base.WndProc( ref m );
			if( m.Msg == Win32Declarations.WM_CONTEXTMENU && _showContextMenu )
			{
				Point pnt = new Point( m.LParam.ToInt32() );
				if( pnt.X == -1 && pnt.Y == -1 )
				{
					DisplayContextMenu( 4, 4 );
				}
				else
				{
					DisplayContextMenu( pnt.X, pnt.Y );
				}
			}
		}

		protected override void OnMouseUp( MouseEventArgs e )
		{
			base.OnMouseUp( e );
			if( e.Button == MouseButtons.Right && _showContextMenu )
			{
				DisplayContextMenu( e.X, e.Y );
			}
		}

		protected override void OnLinkClicked( LinkClickedEventArgs e )
		{
			string url = e.LinkText;

			if( !_rxProtocol.IsMatch( url ) )
			{
				url = "http://" + url;
			}
			Core.UIManager.OpenInNewBrowserWindow( url );
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			_isKeyPressHandled = false;

			// If this is not an editor-specific key, pass it for processing to the action manager
			if((!JetTextBox.IsEditorKey(e.KeyData)) && (Core.ActionManager != null))
			{
				if(Core.ActionManager.ExecuteKeyboardAction( GetContext(ActionContextKind.Keyboard), e.KeyData ))
					e.Handled = _isKeyPressHandled = true;
			}

			// If not an Omea shortcut, pass it to the editbox implementation
			if(! e.Handled )
				base.OnKeyDown( e );
		}

		protected override void OnKeyPress( KeyPressEventArgs e )
		{
			if( _isKeyPressHandled )
				e.Handled = true;

			if( !e.Handled )
				base.OnKeyPress( e );
		}

		#endregion

		#region Implementation

		private void DisplayContextMenu( int x, int y )
		{
			// Check if the control is visible (that's required to show the context menu)
			if( !Visible )
			{
				Trace.WriteLine( "Cannot show a context menu for the invisible control.", "[JRTB]" );
				return;
			}

			// TODO: remove the debug trace
			Trace.WriteLine( "Rendering context menu for the RTF control.", "[JRTB]" );

			// Show the context menu
			Core.ActionManager.ShowResourceContextMenu( GetContext( ActionContextKind.ContextMenu ), this, x, y );
		}

		#endregion

		#region Highlighting Support

		#region Class AsyncHighlightState — Stores information about an async highlighting procedure in progress.

		/// <summary>
		/// Stores information about an async highlighting procedure in progress.
		/// </summary>
		protected class AsyncHighlightState
		{
			/// <summary>
			/// Search hits to be highlighted.
			/// </summary>
			public WordPtr[] Words;

			/// <summary>
			/// RTF helper struct.
			/// </summary>
			public CHARFORMAT2 Fmt;

			/// <summary>
			/// Enumerates whatever appropriate for the current highlighting scheme.
			/// </summary>
			public IEnumerator Enum = null;

			/// <summary>
			/// A function that that implements a single highlighting step, either for main or backup scheme.
			/// This defines whether the highlighting will apply for main or backup scheme.
			/// </summary>
			public StepHiliteAny StepHiliteDelegate;

			/// <summary>
			/// A delegate type for the function that implements a single highlighting step, either for main or backup scheme.
			/// </summary>
			public delegate bool StepHiliteAny( AsyncHighlightState state );

			/// <summary>
			/// Backup sceme stores its hits in here.
			/// A list of WordPtr objects that represent the search hits in the document; they may not correspond to the words list passed in.
			/// This new list is sorted and stored for search result navigation, etc.
			/// </summary>
			public ArrayList ActualSearchHitsCache;

			/// <summary>
			/// The final version of the search hits list, as they were encountered in the document.
			/// </summary>
			public WordPtr[] ActualSearchHits;

			/// <summary>
			/// Current position in the backup highlighting scheme.
			/// </summary>
			public int CurPos;

			/// <summary>
			/// Maps the original word forms to the corresponding color for highlighting.
			/// Holds the original search string entries that we have met, mapped to the indexes of highlighting colors. 
			/// Provides for highlighing tokens produced from the same search entry with the same color.
			/// </summary>
			public Hashtable HashSources;

			/// <summary>
			/// Stores all the target word forms in the way they should be encountered in the text, for the backup hilite to search for them, one by one.
			/// Maps the word form (key) to the whole <see cref="WordPtr"/> that references it (value).
			/// </summary>
			public Hashtable HashWordForms;

			/// <summary>
			/// Stores the time of a prev repaint.
			/// </summary>
			public uint LastRepaintTime = 0;

			/// <summary>
			/// Initializes the object to an indeterminate state.
			/// </summary>
			public AsyncHighlightState( WordPtr[] words )
			{
				Words = words;

				Fmt = new CHARFORMAT2();
				Fmt.cbSize = Marshal.SizeOf( Fmt );
				Fmt.dwMask = CFM.BACKCOLOR | CFM.COLOR;

				HashSources = new Hashtable();
			}

			/// <summary>
			/// Chooses the color that corresponds to the given source form string, or assigns a new one if not specified yet.
			/// Applies this color to the RTF structures.
			/// </summary>
			public void PickNextColor( string source )
			{
				// Choose a color
				BiColor color;
				if( HashSources.ContainsKey( source ) ) // Source already known and has a color assigned
					color = (BiColor)HashSources[ source ];
				else // Assign a new color
				{
					color = _colorsHighlight[ HashSources.Count % _colorsHighlight.Length ];
					HashSources[ source ] = color;
				}
				Fmt.crBackColor = ColorTranslator.ToWin32( color.BackColor );
				Fmt.crTextColor = ColorTranslator.ToWin32( color.ForeColor );
			}
		}

		#endregion

		/// <summary>
		/// Initiates the async highlighting of the search hits.
		/// </summary>
		/// <param name="words">Words to be highlighted. MUST belong to a single document section.</param>
		public void HighlightWords( WordPtr[] words )
        {
            #region Preconditions
            if ( words == null )
                return;

			// Validness check
			WordPtr.AssertValid( words, true );
            #endregion Preconditions

			_wordsSearchHits = null; // Here the real search hits will be stored; in case of main highlighting they correspond to the ones passed in
			_statuswriter = Core.UIManager.GetStatusWriter( this, StatusPane.UI );
			_statuswriter.ShowStatus( "Highlighting search hits in the document…" );

			// Initiate the async hilite process
			_stateHilite = new AsyncHighlightState( words );
			if( StartHiliteMain( _stateHilite ) )
				Core.UserInterfaceAP.QueueJobAt( DateTime.Now.AddMilliseconds( 500 ), "Highlight the search hits.", new MethodInvoker( StepHilite ) ); // Succeeded — queue execution
			else
			{ // Failed — deinitialize
				_stateHilite = null;
				_statuswriter.ClearStatus();
				_statuswriter = null;
				Trace.WriteLine( "Failed to initiate the main highlighting scheme.", "[JRTB]" );
			}
		}

		/// <summary>
		/// Does the asynchronous highlighting step.
		/// </summary>
		private void StepHilite()
		{
			if( _stateHilite == null )
				return; // Has been shut down

			uint dwStart = Win32Declarations.GetTickCount();
			uint dwLimit = 222; // Allow running for this much milliseconds continuously

			// Freeze the control
			Win32Declarations.SendMessage( Handle, Win32Declarations.WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero );

			try
			{
				int nIterations;
				for( nIterations = 0; Win32Declarations.GetTickCount() - dwStart < dwLimit; nIterations++ ) // Work for some limited time
				{
					if( !_stateHilite.StepHiliteDelegate( _stateHilite ) ) // Invoke the individual highlighting step
					{ // Highlighting Completed!

						// Reset the status bar dials
						_statuswriter.ClearStatus();
						_statuswriter = null;

						// Retrieve the values
						_wordsSearchHits = _stateHilite.ActualSearchHits;
						_nCurrentSearchHit = -1;

						// Deinitialize the hilite search
						_stateHilite = null;

						// Jump to the next search hit
						GotoNextSearchHit( true, false );

						// Invalidate
						Win32Declarations.SendMessage( Handle, Win32Declarations.WM_SETREDRAW, (IntPtr)1, IntPtr.Zero );
						Invalidate();

						// Done!
						Trace.WriteLine( String.Format( "The JetRichTextBox has completed the async highlighting with {0} hits total.", (_wordsSearchHits != null ? _wordsSearchHits.Length.ToString() : "#ERROR#") ), "[JRTB]" );
						return;
					}
				}
				Trace.WriteLine( String.Format( "The JetRichTextBox async highlighting has done {0} highlightings on this step.", nIterations ), "[JRTB]" );
			}
			finally
			{
				// Unfreeze the events and repaint
				Win32Declarations.SendMessage( Handle, Win32Declarations.WM_SETREDRAW, (IntPtr)1, IntPtr.Zero );
				if( (_stateHilite != null) && (Win32Declarations.GetTickCount() - _stateHilite.LastRepaintTime > 2000) ) // Repaint rarely
				{
					Invalidate();
					_stateHilite.LastRepaintTime = Win32Declarations.GetTickCount();
				}
			}

			// Requeue the rest of execution
			Application.DoEvents(); // Without this, the painting events won't occur
			Core.UserInterfaceAP.QueueJob( "Highlight the search hits.", new MethodInvoker( StepHilite ) );

		}

		/// <summary>
		/// Starts applying the main highlighting scheme that uses the provided offsets for highlighting the text.
		/// In case the offsets do not hit the expected words, aborts the highlighting and returns <c>Null</c>,
		/// indicating that the backup scheme should be used.
		/// </summary>
		/// <returns>Success flag.</returns>
		protected bool StartHiliteMain( AsyncHighlightState state )
		{
			state.StepHiliteDelegate = new AsyncHighlightState.StepHiliteAny( StepHiliteMain );

			// Assign the entries for highlighting
			state.Enum = state.Words.GetEnumerator();

			return true;
		}

		/// <summary>
		/// Starts applying the backup hilite scheme that applies when the main scheme fails.
		/// It searches for the entries in the text and ignores the offsets given as they're assumed to be incorrect.
		/// </summary>
		/// <returns>Success flag.</returns>
		protected bool StartHiliteBackup( AsyncHighlightState state )
		{
			state.StepHiliteDelegate = new AsyncHighlightState.StepHiliteAny( StepHiliteBackup );

			// Make a hash of the words to highlight (the particular word forms)
			state.HashWordForms = new Hashtable( state.Words.Length );
			foreach( WordPtr word in state.Words )
				state.HashWordForms[ word.Text ] = word;
			Trace.WriteLine( String.Format( "{0} unique forms were picked out of {1} original word-ptrs.", state.HashWordForms.Count, state.Words.Length ), "[JRTB]" );

			// Seed the process
			state.Enum = state.HashWordForms.Keys.GetEnumerator();
			state.ActualSearchHitsCache = new ArrayList( state.HashWordForms.Count );
			state.CurPos = -1;

			// Go on
			return true;
		}

		/// <summary>
		/// Applies one step of the async highlighting against the main scheme.
		/// </summary>
		/// <returns>Whether another step should be called.</returns>
		protected bool StepHiliteMain( AsyncHighlightState state )
		{
			if( !state.Enum.MoveNext() )
			{
				state.ActualSearchHits = state.Words; // As the main scheme has succeeded, the original word list is the same as the final one
				return false; // Over!
			}

			// Get the current word
			WordPtr word = (WordPtr)state.Enum.Current;

			if( word.StartOffset < 0 )
				return StartHiliteBackup( state ); // Fallback to the backup scheme

			// Choose a color for highlighting this word
			state.PickNextColor( word.Original );

			// Select the supposed search hit location
			Select( word.StartOffset, word.Text.Length );

			// Check whether we've selected the proper thing
			if( String.Compare( SelectedText, word.Text, true, CultureInfo.InvariantCulture ) != 0 )
			{
				Trace.WriteLine( String.Format( "Main highlighting expected to find \"{0}\" but got \"{1}\", aborting.", word.Text, SelectedText ), "[JRTB]" );
				return StartHiliteBackup( state ); // Fallback to the backup scheme
			}

			// Apply the coloring!!
			Win32Declarations.SendMessage( Handle, EditMessage.SETCHARFORMAT, SCF.SELECTION, ref state.Fmt );

			return true; // Call more
		}

		/// <summary>
		/// Applies one step of the async highlighting against the backup scheme.
		/// </summary>
		/// <returns>Whether another step should be called.</returns>
		protected bool StepHiliteBackup( AsyncHighlightState state )
		{
			if( state.CurPos < 0 ) // Should we pick a new word form for searching it?
			{
				if( !state.Enum.MoveNext() )
				{ // Completed!!

					// Sort the search hits in order of appearance and supply to the storage
					state.ActualSearchHitsCache.Sort( new WordPtrOffsetComparer() );
					state.ActualSearchHits = (WordPtr[])state.ActualSearchHitsCache.ToArray( typeof(WordPtr) ); // Take the search hits
					return false; // Finish it
				}
				state.CurPos = 0; // Start looking for it from the beginning
			}
			string sOriginal = (string)state.Enum.Current;
			WordPtr wordSearchHit = (WordPtr)state.HashWordForms[ sOriginal ];

			// Choose a color for highlighting the hits of this text
			state.PickNextColor( wordSearchHit.Original );

			// Look for the next entry, starting from the very place we left it the prev time
			int	nOldPos = state.CurPos;
			state.CurPos = Find( wordSearchHit.Text, state.CurPos, RichTextBoxFinds.NoHighlight | RichTextBoxFinds.WholeWord );
			if( state.CurPos < 0 ) // If not found, will be negative
				return true; // Switch to looking for the next entry, or complete the process if there are no more
			if(state.CurPos <= nOldPos)	// Sometimes the Find function will return a result BEFORE the search start :)
			{	// Switch to looking for the next entry, or complete the process if there are no more
				state.CurPos = -1;
				return true;
			}

			// Add the search hit data
			WordPtr hit = wordSearchHit; // Make a copy of the hit
			hit.StartOffset = state.CurPos; // The actual starting offset
			state.ActualSearchHitsCache.Add( hit );

			// Select the supposed search hit location
			Select( state.CurPos, wordSearchHit.Text.Length );
			state.CurPos += wordSearchHit.Text.Length; // Skip the already-found entry (otherwise we'll keep finding it again and again, eternally)

			// Apply the coloring!!
			Win32Declarations.SendMessage( Handle, EditMessage.SETCHARFORMAT, SCF.SELECTION, ref state.Fmt );

			return true; // Try looking for the next entry
		}

		#region Hilite — Navigation

		/// <summary>
		/// Determines whether the GotoNextSearchHit function can perform its action at this time.
		/// </summary>
		public bool CanGotoNextSearchHit( bool bForward )
		{
			if( _wordsSearchHits == null ) // Search has not been performed
				return false;

			// If the search is currently positioned "beyond", allow if there are any search hits
			if( _nCurrentSearchHit == -1 )
				return _wordsSearchHits.Length != 0;

			// Check for each of the directions
			return ((bForward) && (_nCurrentSearchHit < _wordsSearchHits.Length - 1)) || ((!bForward) && (_nCurrentSearchHit > 0));
		}

		/// <summary>
		/// If there are search hits in the document, navigates to either previous or next one, depending on the parameter value.
		/// Does not loop around the end. Never throws an exception.
		/// </summary>
		/// <param name="hilite">If <c>True</c>, highlights the search hit with selection; otherwise, just scrolls to it.</param>
		private void GotoNextSearchHit( bool bForward, bool hilite )
		{
			// Check if allowed
			if( !CanGotoNextSearchHit( bForward ) )
				return;

			// Position at a new hit
			if( _nCurrentSearchHit == -1 ) // Beyond?
				_nCurrentSearchHit = bForward ? 0 : _wordsSearchHits.Length - 1; // Position at the end
			else // Positioned at some of the hits already
				_nCurrentSearchHit += (bForward ? 1 : 0) * 2 - 1; // Move in the direction appropriate

			// Goto
			Select( _wordsSearchHits[ _nCurrentSearchHit ].StartOffset, (hilite ? _wordsSearchHits[ _nCurrentSearchHit ].Text.Length : 0) );
			ScrollToCaret();
		}

		#endregion

		#region Class WordPtrOffsetComparer — Compares the <see cref="WordPtr"/> structures by their start-offsets.

		/// <summary>
		/// Compares the <see cref="WordPtr"/> structures by their start-offsets.
		/// </summary>
		internal class WordPtrOffsetComparer : IComparer
		{
			public int Compare( object x, object y )
			{
				return ((WordPtr)x).StartOffset.CompareTo( ((WordPtr)y).StartOffset ); // Compare by the starting offsets, using the int's comparer
			}
		}

		#endregion

		/// <summary>
		/// Initializes the colors that are used for highlighting the search keywords in the page text
		/// </summary>
		protected static BiColor[] InitColors()
		{
			BiColor[] colorsHighlight = new BiColor[8];
			colorsHighlight[ 0 ] = new BiColor( Color.Black, Color.FromArgb( 0xF5, 0xC6, 0x8E ) );
			colorsHighlight[ 1 ] = new BiColor( Color.Black, Color.FromArgb( 0xAA, 0xA6, 0xDD ) );
			colorsHighlight[ 2 ] = new BiColor( Color.Black, Color.FromArgb( 0xE0, 0xA2, 0xE1 ) );
			colorsHighlight[ 3 ] = new BiColor( Color.Black, Color.FromArgb( 0x94, 0xDC, 0xEE ) );
			colorsHighlight[ 4 ] = new BiColor( Color.Black, Color.FromArgb( 0xF4, 0xFF, 0x84 ) );
			colorsHighlight[ 5 ] = new BiColor( Color.Black, Color.FromArgb( 0xB4, 0xF5, 0x8E ) );
			colorsHighlight[ 6 ] = new BiColor( Color.Black, Color.FromArgb( 0xF5, 0x95, 0x8E ) );
			colorsHighlight[ 7 ] = new BiColor( Color.Black, Color.FromArgb( 0x8E, 0xF5, 0xD1 ) );

			return colorsHighlight;
		}

		#endregion

		#region ICommandProcessor Interface

		public void ExecuteCommand( string command )
		{
			switch( command )
			{
			case DisplayPaneCommands.Copy:
				Copy();
				break;
			case DisplayPaneCommands.Cut:
				Cut();
				break;
			case DisplayPaneCommands.Paste:
				Paste();
				break;

			case DisplayPaneCommands.SelectAll:
				SelectAll();
				break;
			case DisplayPaneCommands.PrevSearchResult:
				GotoNextSearchHit( false, true );
				break;
			case DisplayPaneCommands.NextSearchResult:
				GotoNextSearchHit( true, true );
				break;

			case "Undo":
				Undo();
				break;
			case "Redo":
				Redo();
				break;
			}
		}

		public bool CanExecuteCommand( string command )
		{
			switch( command )
			{
			case DisplayPaneCommands.Copy:
				return (SelectionLength != 0);
			case DisplayPaneCommands.Cut:
				return (SelectionLength != 0);
			case DisplayPaneCommands.Paste:
				return true;

			case DisplayPaneCommands.SelectAll:
				return true;

			case DisplayPaneCommands.PrevSearchResult:
				return CanGotoNextSearchHit( false );
			case DisplayPaneCommands.NextSearchResult:
				return CanGotoNextSearchHit( true );

			case "Undo":
				return CanUndo;
			case "Redo":
				return CanRedo;

			default:
				return false;
			}
		}

		#endregion

		#region IContextProvider Members

		public IActionContext GetContext( ActionContextKind kind )
		{
			ActionContext context;
			if( _contextProvider != null )
				context = (ActionContext)_contextProvider.GetContext( kind );
			else
			{
				context = new ActionContext( kind, this, null );
				context.SetCommandProcessor( this );
			}

			context.SetSelectedText( SelectedRtf, SelectedText, TextFormat.Rtf );

			return context;
		}

		#endregion
	}
}