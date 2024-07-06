// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.TextIndex;
using JetBrains.UI.Interop;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// <p>An extended TextBox with the possibility to show a grayed string when the
	/// text box is empty and does not have focus, and with a built-in incremental
	/// search timer.</p>
	/// <p>This control is capable of processing the relevant Omea commands.</p>
	/// </summary>
	public class JetTextBox: TextBox, ICommandProcessor, IContextProvider
	{
		protected string _emptyText;
        protected Timer _incSearchTimer = new Timer();
		/// <summary>
		/// Indicates the current running-state of the <see cref="_incSearchTimer"/> timer;
		/// the timer's own <see cref="Timer.Enabled"/> property is updated asynchronously
		/// to avoid fucking reentrancy on <see cref="Timer.Stop"/>.
		/// </summary>
		protected bool	_bSearchTimerRunning = false;

		/// <summary>
		/// Flag is <c>True</c> when the KeyDown event has been suppressed and the following KeyPress one should be suppressed as well.
		/// </summary>
		protected bool _isKeyPressHandled = false;

        public event EventHandler IncrementalSearchUpdated;

		/// <summary>
		/// A map of keyboard shortcuts that must always be handled by the editing control itself and should never go to ActionManager for processing.
		/// </summary>
		protected static IntHashSet	_hashEditorKeys;

		/// <summary>
		/// Context provider.
		/// </summary>
		protected IContextProvider	_contextProvider;

        public JetTextBox()
		{
            _incSearchTimer.Interval = 300;
            _incSearchTimer.Tick += new EventHandler( OnIncrementalSearchTick );
		}

		static JetTextBox()
		{
			// Keys that should not be fed to the action processor
			_hashEditorKeys = new IntHashSet();

			lock(_hashEditorKeys)
			{
				_hashEditorKeys.Add( (int) Keys.Delete);
				_hashEditorKeys.Add( (int) Keys.Back);
				_hashEditorKeys.Add( (int) Keys.Space);
				_hashEditorKeys.Add( (int) Keys.Left);
				_hashEditorKeys.Add( (int) Keys.Right);
				_hashEditorKeys.Add( (int) Keys.Up);
				_hashEditorKeys.Add( (int) Keys.Down);
				_hashEditorKeys.Add( (int) (Keys.Back | Keys.Control));
				_hashEditorKeys.Add( (int) (Keys.Delete | Keys.Control));
			}
		}

		/// <summary>
		/// Checks if the key passed in should not be submitted to action processor due to being a text editor key.
		/// </summary>
		public static bool IsEditorKey(Keys key)
		{
			lock(_hashEditorKeys)
				return _hashEditorKeys.Contains( (int)key );
		}

		public string EmptyText
	    {
	        get { return _emptyText; }
	        set
	        {
                _emptyText = value;
                if ( Text == "" && _emptyText != null && _emptyText != "" && !ContainsFocus )
                {
                    Text = _emptyText;
                    ForeColor = SystemColors.GrayText;
                }
	        }
	    }

		/// <summary>
		/// Context provider for this control.
		/// </summary>
		public IContextProvider ContextProvider
		{
			get { return _contextProvider; }
			set { _contextProvider = value; }
		}

		protected override void OnEnter( EventArgs e )
        {
            base.OnEnter( e );
            if ( Text == _emptyText )
            {
                Text = "";
                ForeColor = SystemColors.ControlText;
            }
        }

	    protected override void OnLeave( EventArgs e )
	    {
	        base.OnLeave( e );
            if ( Text == "" && _emptyText != null )
            {
                Text = _emptyText;
                ForeColor = SystemColors.GrayText;
            }
	    }

	    protected override void OnTextChanged( EventArgs e )
	    {
	        base.OnTextChanged( e );
            if ( Text != _emptyText )
            {
                StopSearchTimer();
                StartSearchTimer();
            }
            else if ( Text == "" && !ContainsFocus )
            {
                Text = _emptyText;
            }
	    }

	    public bool IsEmpty()
	    {
	        return Text.Trim().Length == 0 || Text == _emptyText;
	    }

        private void OnIncrementalSearchTick( object sender, EventArgs e )
        {
			if(!_bSearchTimerRunning)	// Is already being stopped async
				return;
            StopSearchTimer();
			try
			{
				if ( Text != _emptyText )
				{
					if ( IncrementalSearchUpdated != null )
					{
						IncrementalSearchUpdated( this, EventArgs.Empty );
					}
				}
			}
			catch(Exception ex)
			{
				Core.ReportException( ex, ExceptionReportFlags.AttachLog );
			}
        }

	    protected override void OnKeyDown( KeyEventArgs e )
	    {
	    	_isKeyPressHandled = false;

			// If this is not an editor-specific key, pass it for processing to the action manager
			if((!IsEditorKey(e.KeyData)) && (Core.ActionManager != null))
			{
				if(Core.ActionManager.ExecuteKeyboardAction( GetContext(ActionContextKind.Keyboard), e.KeyData ))
					e.Handled = _isKeyPressHandled = true;
			}

			// Slava's code to provide proper Ctrl+Backspace handling
			if((!e.Handled) && (e.KeyData == (Keys.Back | Keys.Control)))
			{
				int length = SelectionLength;
				int start = SelectionStart;
				string text = Text;

				if((text.Length != 0)
					&& (start >= 0) && (start <= text.Length) && (start + length <= text.Length)	// Selection info validness (may be invalid sometimes)
					&& ((length != 0) || (start > 0)))	// There is something to delete, actually
				{
					e.Handled = _isKeyPressHandled = true;

					// The selected part is always killed; look for the symbols before the cursor to be killed: all the whitespace, then all the non-whitespace

					// Mark for killing: whitespace
					for(; (start > 0) && (TextDelimitingCategories.IsDelimiter(text[start - 1])); --start, ++length)
						;

					// Mark for killing: alphanumerics
					for(; (start > 0) && (!TextDelimitingCategories.IsDelimiter(text[start - 1])); --start, ++length)
						;

					// Kill marked
					if( length > 0 )
					{
						Text = text.Remove( start, length );
						SelectionStart = start;
					}
				}
			}

			// Similar code for the Ctrl+Del keystroke
			if((!e.Handled) && (e.KeyData == (Keys.Delete | Keys.Control)))
			{
				int length = SelectionLength;
				int start = SelectionStart;
				string text = Text;

				if((text.Length != 0)
					&& (start >= 0) && (start <= text.Length) && (start + length <= text.Length)	// Selection info validness (may be invalid sometimes)
					&& ((length != 0) || (start <= text.Length - 1)))	// There is something to delete, actually
				{
					e.Handled = _isKeyPressHandled = true;

					// The selected part is always killed; look for the symbols after the selection end to be killed: all the non-whitespace, then all the whitespace

					// Mark for killing: whitespace
					for(; (start + length < text.Length) && (!TextDelimitingCategories.IsDelimiter(text[start + length])); ++length)
						;

					// Mark for killing: alphanumerics
					for(; (start + length < text.Length) && (TextDelimitingCategories.IsDelimiter(text[start + length])); ++length)
						;

					// Kill marked
					if( length > 0 )
					{
						Text = text.Remove( start, length );
						SelectionStart = start;
					}
				}
			}

			// If not an Omea shortcut, pass it to the editbox implementation
			if(! e.Handled )
				base.OnKeyDown( e );
	    }

	    protected override void OnKeyPress( KeyPressEventArgs e )
	    {
            if ( _isKeyPressHandled )
                e.Handled = true;

			if(! e.Handled )
				base.OnKeyPress( e );
		}

	    protected override void WndProc( ref Message m )
	    {
	        if ( m.Msg == Win32Declarations.WM_SYSCHAR && _isKeyPressHandled )
	        {
	            return;
	        }
            base.WndProc( ref m );
	    }

		/// <summary>
		/// Safely starts the IncSearchTimer.
		/// </summary>
		public void StartSearchTimer()
		{
			if((ICore.Instance != null) && (Core.State == CoreState.Running))
			{
				_bSearchTimerRunning = true;
				Core.UserInterfaceAP.QueueJob( "Start the incremental search timer.", new MethodInvoker(_incSearchTimer.Start) );
			}
		}

		/// <summary>
		/// Safely stops the IncSearchTimer.
		/// </summary>
		public void StopSearchTimer()
		{
			if((ICore.Instance != null) && (Core.State == CoreState.Running))
			{
				_bSearchTimerRunning = false;
				Core.UserInterfaceAP.QueueJob( "Stop the incremental search timer.", new MethodInvoker(_incSearchTimer.Stop) );
			}
		}

	    #region ICommandProcessor Members

		public void ExecuteCommand(string command)
		{
			if(!CanExecuteCommand(command))
				return;	// Cannot execute a disabled command

			switch(command)
			{
				case DisplayPaneCommands.Copy:	Copy(); break;
				case DisplayPaneCommands.Cut:	Cut(); break;
				case DisplayPaneCommands.Paste:	Paste(); break;
				case DisplayPaneCommands.Back:	Undo(); break;
				case DisplayPaneCommands.FindInPage:	break;
				case DisplayPaneCommands.SelectAll:	SelectAll(); break;
			}
		}

		public bool CanExecuteCommand(string command)
		{
			switch(command)
			{
				case DisplayPaneCommands.Copy:	return (SelectionLength != 0);
				case DisplayPaneCommands.Cut:	return (SelectionLength != 0);
				case DisplayPaneCommands.Paste:	return true;
				case DisplayPaneCommands.Back:	return CanUndo;
				case DisplayPaneCommands.Forward:	return false;
				case DisplayPaneCommands.FindInPage:	return true;
				case DisplayPaneCommands.SelectAll:	return true;
				default:	return false;
			}
		}

		#endregion

		#region IContextProvider Members

		public IActionContext GetContext(ActionContextKind kind)
		{
			/*
			// Go up the chain to find a context-provider that will form a base for our context
			IActionContext	contextBase = null;
			Control	ctrlCurrent = this;
			while(ctrlCurrent.Parent != null)
			{
				if(ctrlCurrent.Parent as IContextProvider != null)
				{
					contextBase = ((IContextProvider)Parent).GetContext( kind );
					break;
				}

				ctrlCurrent = ctrlCurrent.Parent;	// Go up
			}

			// Create the context that will be returned
			ActionContext	context;
			if(contextBase != null)
				context = new ActionContext(contextBase, contextBase.SelectedResources);
			else
				context = new ActionContext(kind, this, null);

			// Override the necessary parameters by this instance's data
			context.SetCommandProcessor( this );
			context.SetSelectedText( Text, Text, TextFormat.PlainText );
			*/

			ActionContext context;
			if(_contextProvider != null)
				context = (ActionContext)_contextProvider.GetContext(kind);
			else
			{
				context = new ActionContext(kind, this, null);
				context.SetCommandProcessor(this);
			}

			///////////////////////////////////////////
			// Get the selected text in a safe manner
			// (the stock impl sometimes falls off the text)

			// Fetch
			int nSelStart = SelectionStart;
			int nSelLength = SelectionLength;
			string sText = Text;
			int nTextLength = sText.Length;

			// Validate
			nSelStart = nSelStart <= nTextLength - 1 ? nSelStart : nTextLength - 1;
			nSelLength = nSelLength <= nTextLength - nSelStart ? nSelLength : nTextLength - nSelStart;

			// Apply
			if((nSelStart >= 0) && (nSelLength > 0)) // There is likely to be some text selected
			{
				string text = sText.Substring(nSelStart, nSelLength);
				context.SetSelectedText(text, text, TextFormat.PlainText);
			}

			return context;
		}

		#endregion
	}
}
