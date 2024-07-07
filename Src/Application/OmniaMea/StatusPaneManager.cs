// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;
using System.Windows.Forms;
using System.Collections;

namespace JetBrains.Omea
{
	/**
	 * The class which manages output of status messages to a single status bar panel.
	 */

    public class StatusPaneManager
	{
        private StatusBar _statusBar;
        private StatusBarPanel _panel;
        private Hashtable _statusWriters = new Hashtable();
        private ArrayList _activeStatusWriters = new ArrayList();
        private bool _updatingStatusText = false;
        private bool _recursiveUpdateStatusText = false;
        private string _defaultText;

		public StatusPaneManager( StatusBar statusBar, StatusBarPanel panel, string defaultText )
		{
            _statusBar = statusBar;
            _panel = panel;
            _defaultText = defaultText;
		}

        public IStatusWriter GetStatusWriter( object owner )
        {
            lock( _statusWriters )
            {
                IStatusWriter writer = (IStatusWriter) _statusWriters [owner];
                if ( writer == null )
                {
                    writer = new StatusWriter( this, owner );
                    _statusWriters [owner] = writer;
                }
                return writer;
            }
        }

        public void RemoveStatusWriter( object owner )
        {
            lock( _statusWriters )
            {
                _statusWriters.Remove( owner );
            }
        }

        internal void UpdateStatus( IStatusWriter writer, string message, bool doEvents )
        {
            lock( _activeStatusWriters )
            {
                _activeStatusWriters.Remove( writer );
                if ( message != null )
                    _activeStatusWriters.Insert( 0, writer );
            }

            Core.UIManager.QueueUIJob( new UpdateStatusTextDelegate( UpdateStatusText ), new object[] { doEvents } );
        }

        internal void UpdateStatusText( bool doEvents )
        {
            if ( _statusBar.Panels.Count == 0 )   // do not crash when we are closing
                return;

            if ( _updatingStatusText )
            {
                _recursiveUpdateStatusText = true;
                return;
            }

            _updatingStatusText = true;

            do
            {
                string panelText;
                lock( _activeStatusWriters )
                {
                    if ( _activeStatusWriters.Count == 0 )
                    {
                        panelText = _defaultText;
                    }
                    else
                    {
                        StatusWriter statusWriter = (StatusWriter) _activeStatusWriters [0];
                        panelText = statusWriter.LastMessage;
                    }
                }
                _panel.Text = panelText;

                if ( doEvents )
                {
                    _recursiveUpdateStatusText = false;
                    _statusBar.Refresh();
                }
            } while( _recursiveUpdateStatusText );

            _updatingStatusText = false;
            _recursiveUpdateStatusText = false;
        }

        public bool NeedDoEvents
        {
            get { return _statusBar.InvokeRequired; }
        }
	}

    internal class StatusWriter: IStatusWriter
    {
        private StatusPaneManager _manager;
        private string _lastMessage;
        private object _owner;

        internal StatusWriter( StatusPaneManager manager, object owner )
        {
            _manager = manager;
            _owner = owner;
        }

        public void ShowStatus( string message )
        {
            ShowStatus( message, 0, _manager.NeedDoEvents );
        }

    	/// <summary><seealso cref="ClearStatus"/><seealso cref="LastMessage"/><seealso cref="IUIManager.GetStatusWriter"/>
    	/// Shows a status bar message in the appropriate status bar pane and optionally forces it to redraw immediately.
    	/// </summary>
    	public void ShowStatus( string message, bool repaint )
    	{
    		ShowStatus( message, 0, repaint );
    	}

    	public void ShowStatus( string message, int nShowForSeconds, bool doEvents )
        {
			// Save message
            if ( message == "" )
                _lastMessage = null;
            else
                _lastMessage = message;

			// Show
            _manager.UpdateStatus( this, _lastMessage, doEvents );

			// Queue erasure
			if(nShowForSeconds != 0)
				Core.UserInterfaceAP.QueueJobAt( DateTime.Now.AddSeconds(nShowForSeconds), "Clear Expired Status Message.", new MethodInvoker(ClearStatus));
        }

        public void ClearStatus()
        {
            _lastMessage = null;
            _manager.UpdateStatus( this, null, _manager.NeedDoEvents );
            _manager.RemoveStatusWriter( _owner );
        }

        public string LastMessage
        {
            get { return _lastMessage; }
        }

    	/// <summary><seealso cref="ClearStatus"/><seealso cref="LastMessage"/><seealso cref="IUIManager.GetStatusWriter"/>
    	/// Shows a status bar message in the appropriate status bar pane and automatically removes it after a given time span.
    	/// </summary>
    	public void ShowStatus( string message, int nSecondsToKeep )
    	{
    		ShowStatus(message, nSecondsToKeep, _manager.NeedDoEvents);
    	}
    }
}
