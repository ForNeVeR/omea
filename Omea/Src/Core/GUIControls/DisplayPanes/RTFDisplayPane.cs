// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
	public delegate void LoadTextDelegate( IResource res, RichTextBox richTextBox );

    /// <summary>
    /// Implementation of IDisplayPane based on a RichTextBox.
    /// </summary>
    public class RTFDisplayPane: IDisplayPane, IContextProvider
	{
		private LoadTextDelegate _richTextLoader;
        private JetRichTextBox _richTextBox;
        private IResource _resource;


        public RTFDisplayPane( LoadTextDelegate textLoader )
		{
            _richTextLoader = textLoader;

		    _richTextBox = new JetRichTextBox();
		    _richTextBox.Multiline = true;
		    _richTextBox.ReadOnly = true;
		    _richTextBox.AcceptsTab = true;
		    _richTextBox.HideSelection = false;
		    _richTextBox.ScrollBars = RichTextBoxScrollBars.Both;
		    _richTextBox.BackColor = SystemColors.Window;
            _richTextBox.BorderStyle = BorderStyle.None;
            _richTextBox.KeyDown += new KeyEventHandler( _richTextBox_KeyDown );
		}

        public event ResourceEventHandler DisplayResourceEnd;

	    public void DisplayResource( IResource resource )
	    {
            _resource = resource;
			_richTextBox.ContextProvider = this;
            _richTextLoader( resource, _richTextBox );
	    }

	    public void EndDisplayResource( IResource resource )
	    {
            if ( DisplayResourceEnd != null )
            {
                DisplayResourceEnd( this, new ResourceEventArgs( resource ) );
            }
	    }

	    public Control GetControl()
	    {
            return _richTextBox;
	    }

	    public void HighlightWords( WordPtr[] words )
	    {
            _richTextBox.HighlightWords( DocumentSection.RestrictResults(words, DocumentSection.BodySection) );
        }

	    public string GetSelectedText( ref TextFormat format )
	    {
            format = TextFormat.Rtf;
            return _richTextBox.SelectedRtf;
	    }

        public string GetSelectedPlainText()
        {
            return _richTextBox.SelectedText;
        }

        public void ExecuteCommand( string command )
	    {
            _richTextBox.ExecuteCommand( command );
	    }

	    public bool CanExecuteCommand( string command )
	    {
            return _richTextBox.CanExecuteCommand( command );
	    }

	    public void DisposePane()
	    {
            _richTextBox.KeyDown -= new KeyEventHandler( _richTextBox_KeyDown );
            _richTextBox.Dispose();
        }

        private void _richTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = Core.ActionManager.ExecuteKeyboardAction(
                new ActionContext( ActionContextKind.Keyboard, this, _resource.ToResourceList() ), e.KeyData );
		}
		#region IContextProvider Members

		public IActionContext GetContext(JetBrains.Omea.OpenAPI.ActionContextKind kind)
		{
			return new ActionContext(kind, this, (_resource != null ? _resource.ToResourceList() : null));
		}

		#endregion
	}
}
