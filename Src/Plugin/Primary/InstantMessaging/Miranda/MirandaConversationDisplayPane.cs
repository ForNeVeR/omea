// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Conversations;

namespace JetBrains.Omea.InstantMessaging.Miranda
{
    internal class MirandaConversationDisplayPane: IDisplayPane, IDisplayPane2
    {
        private IResourceList _currentConvList;
        private IMConversationsManager _convManager;
        private int _propNickName;

    	private WordPtr[] _wordsToHighlight = null;

    	public MirandaConversationDisplayPane( IMConversationsManager convManager, int propNickName )
        {
            _convManager = convManager;
            _propNickName = propNickName;
        }

        public void DisplayResource( IResource resource )
        {
			DisplayResource( resource, null );
        }

        /**
         * Shows the text of the currently displayed conversation in IEBrowser.
         */

        private void DisplayCurrentConversation()
        {
            if ( _currentConvList != null )
            {
                string htmlString = _convManager.ToHtmlString( _currentConvList[0], _propNickName );
                Core.WebBrowser.ShowHtml( htmlString, WebSecurityContext.Restricted, DocumentSection.RestrictResults(_wordsToHighlight, DocumentSection.BodySection) );
            }
        }

        public Control GetControl()
        {
            return Core.WebBrowser;
        }

        public void EndDisplayResource( IResource resource )
        {
            DisposeCurrentConversation();
        }

        public void HighlightWords( WordPtr[] words )
        {
			_wordsToHighlight = words;
            Core.WebBrowser.HighlightWords( words, 0 );
        }

        bool ICommandProcessor.CanExecuteCommand( string action )
        {
            return Core.WebBrowser.CanExecuteCommand( action );
        }

        void ICommandProcessor.ExecuteCommand( string action )
        {
            Core.WebBrowser.ExecuteCommand( action );
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

        public void DisposePane()
        {
        }

        /**
         * Disposes the live resource list used for tracking the changes of the
         * currently displayed conversation.
         */

        private void DisposeCurrentConversation()
        {
            if ( _currentConvList != null )
            {
                _currentConvList.Dispose();
                _currentConvList = null;
            }
			_wordsToHighlight = null;
        }

        private void OnCurrentConversationChanged( object sender, ResourcePropIndexEventArgs e )
        {
            Core.UIManager.QueueUIJob( new MethodInvoker( DisplayCurrentConversation ) );
		}
		#region IDisplayPane2 Members

		public void DisplayResource(IResource resource, WordPtr[] wordsToHighlight)
		{
			DisposeCurrentConversation();

			_currentConvList = resource.ToResourceListLive();
			_currentConvList.ResourceChanged += new ResourcePropIndexEventHandler(OnCurrentConversationChanged);
			_wordsToHighlight = wordsToHighlight;

			DisplayCurrentConversation( );
		}

		#endregion

		#region ICommandProcessor Members

		public void ExecuteCommand(string command)
		{
			// TODO:  Add MirandaConversationDisplayPane.ExecuteCommand implementation
		}

		public bool CanExecuteCommand(string command)
		{
			// TODO:  Add MirandaConversationDisplayPane.CanExecuteCommand implementation
			return false;
		}

		#endregion
	}
}
