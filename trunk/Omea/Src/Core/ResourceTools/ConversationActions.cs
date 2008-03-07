/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using System.Windows.Forms;
using System.IO;
using System.Text;

namespace JetBrains.Omea.Conversations
{
    /**
     * Saves an IM conversation as an HTML file to the disk.
     */

//    public class SaveConversationAction: ActionOnSingleResource
    public class SaveConversationAction: IAction
    {
        private IMConversationsManager _convManager;
        private int _propDisplayName;

        public SaveConversationAction( IMConversationsManager convManager, int propDisplayName )
        {
            if ( convManager == null )
                throw new ArgumentNullException( "convManager" );

            _convManager = convManager;
            _propDisplayName = propDisplayName;
        }

        public void Update( IActionContext context, ref ActionPresentation presentation )
        {
        }

        public void Execute( IActionContext context )
        {
            using( SaveFileDialog dlg = new SaveFileDialog() )
            {
                dlg.Filter = "HTML files|*.html|All files|*.*";
                dlg.Title = "Save IM Conversation";
                if ( dlg.ShowDialog( ICore.Instance.MainWindow ) != DialogResult.OK )
                {
                    return;
                }

                string html = _convManager.ToHtmlString( context.SelectedResources, _propDisplayName );
                try
                {
                    TextWriter writer = new StreamWriter( dlg.FileName, false, Encoding.UTF8 );
                    writer.Write( html );
                    writer.Close();
                }
                catch( Exception e )
                {
                    MessageBox.Show( ICore.Instance.MainWindow,
                        "Error saving conversation: " + e.Message, "Save IM Conversation" );
                }
            }
        }
    }

    /**
     * Sends a conversation by email.
     */

    public class EmailConversationAction: ActionOnResource
    {
        private IMConversationsManager _convManager;
        private int _propDisplayName;
        private bool _haveEmailService;
        private IEmailService _emailService;

        public EmailConversationAction( IMConversationsManager convManager, int propDisplayName )
        {
            Guard.NullArgument( convManager, "convManager" );
            _convManager = convManager;
            _propDisplayName = propDisplayName;
        }

        public override void Execute( IActionContext context )
        {
            InitializeEmailService();
            if ( _emailService != null )
            {
                foreach( IResource selItem in context.SelectedResources.ValidResources )
                {
                    if( !selItem.IsDeleted )
                    {
                        string html = _convManager.ToHtmlString( selItem, _propDisplayName );
                        _emailService.CreateEmail( selItem.GetPropText( "Subject" ), html,
                            EmailBodyFormat.Html, Core.ResourceStore.EmptyResourceList, new string[] {}, false );
                    }
                }
            }
        }

        public override void Update( IActionContext context, ref ActionPresentation presentation )
        {
            InitializeEmailService();
            base.Update( context, ref presentation );
            presentation.Enabled = ( _emailService != null );
        }

        private void InitializeEmailService()
        {
            if ( !_haveEmailService )
            {
                _emailService = (IEmailService) ICore.Instance.PluginLoader.GetPluginService( typeof(IEmailService) );
                _haveEmailService = true;
            }
        }
    }
}
