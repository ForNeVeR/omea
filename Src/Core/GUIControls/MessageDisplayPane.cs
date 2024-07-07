// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// Base implementation of a pane for displaying a message (with subject, date and
	/// an HTML or rich text body).
	/// </summary>
	public class MessageDisplayPane: UserControl, IDisplayPane2
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        private   JetRichTextBox    _editSubject;
        private   GradientBar       _gradientLine;
        protected Panel             _headerPane;
        protected AbstractWebBrowser _ieBrowser;
        protected JetRichTextBox    _editRtfBody;

        public MessageDisplayPane()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

            _ieBrowser = Core.WebBrowser;
			_ieBrowser.ContextProvider = Core.ResourceBrowser;
            _editRtfBody.ContextProvider = Core.ResourceBrowser;
            _editSubject.ContextProvider = new SubjectContextProvider( _editSubject );
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
			components = new System.ComponentModel.Container();

            this._headerPane = new System.Windows.Forms.Panel();
            this._gradientLine = new JetBrains.Omea.GUIControls.GradientBar();
            this._editSubject = new JetRichTextBox();
            this._editRtfBody = new JetRichTextBox();
            this._headerPane.SuspendLayout();
            this.SuspendLayout();
            //
            // _headerPane
            //
            this._headerPane.BackColor = System.Drawing.SystemColors.Window;
            this._headerPane.Controls.Add(this._gradientLine);
            this._headerPane.Controls.Add(this._editSubject);
            this._headerPane.Dock = System.Windows.Forms.DockStyle.Top;
            this._headerPane.Location = new System.Drawing.Point(0, 0);
            this._headerPane.Name = "_headerPane";
            this._headerPane.Size = new System.Drawing.Size(360, 24);
            this._headerPane.TabIndex = 11;
            //
            // _gradientLine
            //
            this._gradientLine.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._gradientLine.EndColor = System.Drawing.Color.White;
            this._gradientLine.GradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
            this._gradientLine.Location = new System.Drawing.Point(4, 22);
            this._gradientLine.Name = "_gradientLine";
            this._gradientLine.Size = new System.Drawing.Size(360, 1);
            this._gradientLine.StartColor = System.Drawing.SystemColors.ControlDark;
            this._gradientLine.TabIndex = 13;
            //
            // _editSubject
            //
            this._editSubject.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._editSubject.BackColor = SystemColors.Window;
            this._editSubject.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this._editSubject.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this._editSubject.ForeColor = System.Drawing.SystemColors.WindowText;
            this._editSubject.HideSelection = false;
            this._editSubject.Location = new System.Drawing.Point(4, 2);
            this._editSubject.Name = "_editSubject";
            this._editSubject.ReadOnly = true;
            this._editSubject.Size = new System.Drawing.Size(250, 16);
            this._editSubject.ScrollBars = RichTextBoxScrollBars.None;
            this._editSubject.TabIndex = 10;
            this._editSubject.Text = "";
            this._editSubject.ContentsResized += new ContentsResizedEventHandler( HandleSubjectContentsResized );
            this._editSubject.SizeChanged += new EventHandler( HandleSubjectSizeChanged );
            //
            // _mailBodyRTF
            //
            this._editRtfBody.BorderStyle = BorderStyle.None;
            this._editRtfBody.Dock = DockStyle.Fill;
            this._editRtfBody.Location = new System.Drawing.Point(8, 96);
            this._editRtfBody.Name = "_editRtfBody";
            this._editRtfBody.ReadOnly = true;
            this._editRtfBody.Size = new System.Drawing.Size(544, 176);
            this._editRtfBody.TabIndex = 2;
            this._editRtfBody.Text = "";
            this._editRtfBody.Visible = false;
            //
            // RSSItemView
            //
            this.BackColor = System.Drawing.SystemColors.Window;
            this.Controls.Add(this._editRtfBody);
            this.Controls.Add(this._headerPane);
            this.Name = "MessageDisplayPane";
            this.Size = new System.Drawing.Size(360, 150);
            this._headerPane.ResumeLayout(false);
            this.ResumeLayout(false);
        }

	    #endregion

        Control IDisplayPane.GetControl()
        {
            return this;
        }

        public void DisplayResource( IResource item )
        {
            DisplayResource( item, null );
        }

        public virtual void DisplayResource( IResource item, WordPtr[] wordsToHighlight )
        {}

        public virtual void EndDisplayResource( IResource res )
        {}

        public virtual void DisposePane()
        {
            if ( _ieBrowser != null && Controls.Contains( _ieBrowser ) )
            {
                Controls.Remove( _ieBrowser );
            }
            _ieBrowser.Visible = true;
            Dispose();
        }

        protected void ShowSubject( string text, WordPtr[] toHighlight )
        {
            _editSubject.Text = text;
            if ( toHighlight != null )
            {
                WordPtr[] restrictedWords = DocumentSection.RestrictResults( toHighlight, DocumentSection.SubjectSection );
                _editSubject.HighlightWords( restrictedWords );
            }
        }

        /// <summary>
        /// Method shows html-formatted text in the browser. Offsets for highlighting are
        /// restricted to the <see cref="DocumentSection.BodySection">BodySection</see> section.
        /// </summary>
        protected void ShowHtml( string html, WebSecurityContext securityContext, WordPtr[] toHighlight )
        {
            AttachWebBrowser();
            WordPtr[] restrictedWords = DocumentSection.RestrictResults( toHighlight, DocumentSection.BodySection );
            _ieBrowser.ShowHtml( html, securityContext, restrictedWords  ); // Use the updated offsets
        }

	    protected void AttachWebBrowser()
	    {
	        if ( _ieBrowser.Parent != this )
	        {
	            SuspendLayout();
	            Controls.Add( _ieBrowser );
	            Controls.SetChildIndex( _ieBrowser, 0 );
                _ieBrowser.TabIndex = 0;
	            _ieBrowser.Dock = DockStyle.Fill;
	            ResumeLayout();
	        }
	        _ieBrowser.Visible = true;
	        _editRtfBody.Visible = false;
	    }

	    protected void ShowRtf( string rtf, WordPtr[] wordsToHighlight )
        {
            _editRtfBody.Show();
            _ieBrowser.Hide();
            try
            {
                _editRtfBody.Rtf = rtf;
                if ( wordsToHighlight != null )
                {
                    _editRtfBody.HighlightWords( DocumentSection.RestrictResults(wordsToHighlight, DocumentSection.BodySection ));
                }
            }
            catch ( ArgumentException exc )
            {
                Core.ReportBackgroundException( exc );
                _editRtfBody.Text = "Cannot display this message. Error occured while reading RTF format.";
            }
        }

/*
        protected void ShowPlainTextAsRtf( string text, WordPtr[] wordsToHighlight )
        {
            _editRtfBody.Show();
            _ieBrowser.Hide();
            _editRtfBody.Select();
            _editRtfBody.Clear();
            _editRtfBody.Text = text;
            _editRtfBody.Font = new Font("Courier New", 10F, FontStyle.Regular, GraphicsUnit.Point, 204);
            if ( wordsToHighlight != null )
            {
                _editRtfBody.HighlightWords( DocumentSection.RestrictResults(wordsToHighlight, DocumentSection.BodySection ));
            }
        }

*/
        public void HighlightWords( WordPtr[] wordsToHighlight )
        {
            Guard.NullArgument( wordsToHighlight, "wordsToHighlight" );
            _editSubject.HighlightWords( DocumentSection.RestrictResults(wordsToHighlight, DocumentSection.SubjectSection) );
            if ( _ieBrowser.Visible )
            {
                _ieBrowser.HighlightWords( DocumentSection.RestrictResults( wordsToHighlight, DocumentSection.BodySection ), 0 );
            }
            else if ( _editRtfBody.Visible )
            {
                _editRtfBody.HighlightWords( DocumentSection.RestrictResults(wordsToHighlight, DocumentSection.BodySection) );
            }
        }

        public virtual string GetSelectedText( ref TextFormat format )
        {
            if ( Core.WebBrowser.Visible )
            {
                format = TextFormat.Html;
                return Core.WebBrowser.SelectedHtml;
            }

            if ( _editRtfBody.Visible )
            {
                format = TextFormat.Rtf;
                return _editRtfBody.SelectedRtf;
            }
            return null;
        }

        public virtual string GetSelectedPlainText()
        {
            if ( Core.WebBrowser.Visible )
                return Core.WebBrowser.SelectedText;

            if ( _editRtfBody.Visible )
                return _editRtfBody.SelectedText;

            return null;
        }

        public virtual bool CanExecuteCommand( string action )
        {
            if ( _ieBrowser.Visible )
                return _ieBrowser.CanExecuteCommand( action );

            if ( _editRtfBody.Visible )
                return _editRtfBody.CanExecuteCommand( action );

            return false;
        }

        public virtual void ExecuteCommand( string action )
        {
            if ( _ieBrowser.Visible )
            {
                _ieBrowser.ExecuteCommand( action );
            }
            else if ( _editRtfBody.Visible )
            {
                _editRtfBody.ExecuteCommand( action );
            }
        }

        private void HandleSubjectContentsResized( object sender, ContentsResizedEventArgs e )
        {
            _editSubject.Height = e.NewRectangle.Height;
            _headerPane.Height = e.NewRectangle.Height + 8;
            _gradientLine.Top = e.NewRectangle.Height + 6;
        }

	    protected override void OnSizeChanged( EventArgs e )
	    {
	        base.OnSizeChanged( e );
            _editSubject.Width = Width - 50;
	    }

	    private void HandleSubjectSizeChanged( object sender, EventArgs e )
	    {
//	        Trace.WriteLine( "Subject size changed: new width " + _editSubject.Width );
	    }
	}

    internal class SubjectContextProvider : IContextProvider
    {
        private readonly JetRichTextBox _subjectCtrl;
        public SubjectContextProvider( JetRichTextBox ctrl )
        {
            _subjectCtrl = ctrl;
        }

        public IActionContext GetContext( ActionContextKind kind  )
        {
            ActionContext context = new ActionContext( kind, _subjectCtrl, Core.ResourceBrowser.SelectedResources );
            context.SetCommandProcessor( _subjectCtrl );
            context.SetListOwner( Core.ResourceBrowser.OwnerResource );
            return context;
        }
    }
}
