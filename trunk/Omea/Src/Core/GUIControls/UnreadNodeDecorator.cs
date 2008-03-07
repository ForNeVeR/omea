/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Diagnostics;
using System.Drawing;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;
using JetBrains.UI.RichText;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// The decorator which draws bold blue unread counters next to tree nodes.
    /// </summary>
    public class UnreadNodeDecorator: IResourceNodeDecorator
    {
        private string      _ownerName = "";
        private UnreadState _unreadState;
        private TextStyle   _unreadTextStyle = new TextStyle( FontStyle.Regular, Color.Blue, SystemColors.Window );
        private bool        _traceUnreadCounters;

        public static string Key = "UnreadCount";

        public event ResourceEventHandler DecorationChanged;

        public UnreadNodeDecorator()
        {
            UnreadState = (Core.UnreadManager as UnreadManager).CurrentUnreadState;
            _traceUnreadCounters = Core.SettingStore.ReadBool( "UnreadCounters", "TraceUnreadCounters", false );
        }

        /**
         * State from which the values of unread counters are taken.
         */
        
        public UnreadState UnreadState
        {
            get { return _unreadState; }
            set 
            { 
                if ( _unreadState != value )
                {
                    if ( _unreadState != null )
                    {
                        _unreadState.UnreadCountChanged -= OnUnreadCountChanged;
                    }
                    _unreadState = value; 
                    if ( _unreadState != null )
                    {
                        _unreadState.UnreadCountChanged += OnUnreadCountChanged;
                    }
                }
            }
        }

        /**
         * Name of the tab owning the tree pane (for debug purposes).
         */

        internal string OwnerName
        {
            get { return _ownerName; }
            set { _ownerName = value; }
        }

        public string DecorationKey
        {
            get { return Key; }
        }

        public bool DecorateNode( IResource res, RichText nodeText )
        {
            int unreadCount = ( _unreadState == null )
                ? Core.UnreadManager.GetUnreadCount( res )
                : _unreadState.GetUnreadCount( res );

            if ( _unreadState != null && _traceUnreadCounters )
            {
                Trace.WriteLine( "Decorating node " + res + " with count " + unreadCount + " from " + _unreadState );
            }
            if ( unreadCount != 0 )
            {
                nodeText.Append( " " );
                nodeText.SetStyle( FontStyle.Bold, 0, res.DisplayName.Length );
                nodeText.Append( "(" + unreadCount + ")", _unreadTextStyle );
            }
            return true;
        }

        /**
         * When the unread count for a resource changes in the unread state, updates
         * the rich text for the respective tree node.
         */
        
        private void OnUnreadCountChanged( object sender, ResourceEventArgs e )
        {
            if ( DecorationChanged != null )
            {
                DecorationChanged( this, new ResourceEventArgs( e.Resource ) );
            }
        }
    }
}
