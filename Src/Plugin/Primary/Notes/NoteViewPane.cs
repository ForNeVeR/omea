// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Notes
{
	internal class NotePreviewPane: MessageDisplayPane, IContextProvider
	{
		private System.ComponentModel.Container components = null;

        private IResource  _displayedNote;

		/// <summary>
		/// The Web Security Context that displays the Note preview by default, in the restricted environment.
		/// </summary>
		private readonly WebSecurityContext _ctxRestricted;

		public NotePreviewPane()
		{
			InitializeComponent();

			// Initialize the security context
			_ctxRestricted = WebSecurityContext.Restricted;
            _ctxRestricted.WorkOffline = false;	// Enable downloading of the referenced content
            _ctxRestricted.ShowPictures = true;
		}

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
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// ArticlePreviewPane
			//
			this.Name = "ArticlePreviewPane";
			this.Size = new System.Drawing.Size(608, 280);
			this.ResumeLayout(false);

		}
		#endregion

		public override void DisplayResource( IResource note, WordPtr[] toHighlight )
		{
			_ieBrowser.Visible = true;
            _displayedNote = note;
            _ieBrowser.ContextProvider = this;

			// Set the subject, highlight if needed
			ShowSubject( note.GetPropText( Core.Props.Subject ), toHighlight );

            string formattedText = note.GetPropText( Core.Props.LongBody );
            formattedText = Core.MessageFormatter.GetFormattedHtmlBody( note, formattedText, ref toHighlight );
            ShowHtml( formattedText, _ctxRestricted, toHighlight );
		}

		public override void EndDisplayResource( IResource res )
		{
			_ieBrowser.Visible = true;
		}

		public override void DisposePane() {}

        #region IContextProvider Members
        public IActionContext GetContext( ActionContextKind kind )
        {
            return new ActionContext( kind, null, (_displayedNote == null) ? null : _displayedNote.ToResourceList() );
        }
        #endregion IContextProvider Members
    }
}
