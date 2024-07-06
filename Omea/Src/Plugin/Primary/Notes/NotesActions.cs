// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.IO;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Notes
{
    /**
     * displays posting form & posts an article to selected newsgroups
     */
    public class NewNoteAction: SimpleAction
    {
        public override void Execute( IActionContext context )
        {
            ResourceProxy proxy = ResourceProxy.BeginNewResource( "Note" );
            proxy.SetProp( Core.Props.Subject, "New note" );
            proxy.EndUpdate();

            NoteEditor editor = new NoteEditor();
            Core.UIManager.OpenResourceEditWindow( editor, proxy.Resource, true );
        }
    }

    public class OpenNoteAction: ActionOnSingleResource
    {
        public override void Execute( IActionContext context )
        {
            IResource note = context.SelectedResources[ 0 ];
            Core.UIManager.OpenResourceEditWindow( new NoteEditor(), note, false );
        }
    }

    /**
     * base class for the Reply2Sender and the ForwardArticle actions
     */
    public abstract class ReplyForwardAction : IAction
    {
        protected IEmailService GetEmailService()
        {
            return (IEmailService) Core.PluginLoader.GetPluginService( typeof( IEmailService ) );
        }

        public virtual void Execute( IActionContext context )
        {}

        public virtual void Update( IActionContext context, ref ActionPresentation presentation )
        {
			presentation.Visible = (context.SelectedResources.Count == 1) &&
                                   (GetEmailService() != null) &&
                                    context.SelectedResources[ 0 ].HasProp( Core.Props.LongBody );
        }
    }

    /**
     * Forwards an article
     */
    public class SendNoteAction : ReplyForwardAction
    {
        public override void Execute( IActionContext context )
        {
            base.Execute( context );

            IResource   note = context.SelectedResources[ 0 ];
            string      subject = note.GetPropText( Core.Props.Subject );
            string      body = note.GetPropText( Core.Props.LongBody );
            GetEmailService().CreateEmail( "Fw: " + subject, body, EmailBodyFormat.Html,
                                           (EmailRecipient[]) null, null, true );
        }
    }

    /**
     * action for saving articles as files
     */
    public class SaveNoteAction : ActionOnResource
    {
        public override void Execute( IActionContext context )
        {
            IResourceList notes = context.SelectedResources;

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.RestoreDirectory = true;
            dlg.Filter = "html files (*.html)|*.html|All files (*.*)|*.*";

            if( notes.Count == 1 )
            {
                string name = notes[ 0 ].GetPropText( Core.Props.Subject );
                IOTools.MakeValidFileName( ref name );
                dlg.FileName = name;
            }
            if( dlg.ShowDialog() == DialogResult.OK )
            {
                try
                {
                    Stream stream = dlg.OpenFile();
                    if( stream != null )
                    {
                        using( StreamWriter writer = new StreamWriter( stream ) )
                        {
                            writer.WriteLine( "<html><body>" );
                            foreach( IResource note in notes )
                            {
                                string subject = note.GetPropText( Core.Props.Subject );
                                string body    = note.GetPropText( Core.Props.LongBody);
                                if( subject.Length > 0 )
                                {
                                    writer.WriteLine( "<h1>" + subject + "</h1><p>" );
                                }
                                writer.WriteLine( body );
                                writer.WriteLine( "<br><hr>" );
                            }
                            writer.WriteLine( "</body></html>" );
                        }
                    }
                }
                catch {}
            }
        }
    }
}
