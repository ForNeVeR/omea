/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
    public class LabelDisplayPane: IDisplayPane2
    {
        private readonly Label _lbl;

        public LabelDisplayPane( string text )
        {
            _lbl = new Label();
            _lbl.Text = text;
        }

        public Control GetControl()
        {
            return _lbl;
        }

        public void EndDisplayResource(IResource resource){}

        public void HighlightWords( WordPtr[] words ){}

        public void DisplayResource(IResource resource){}

    	public void DisplayResource( IResource resource, WordPtr[] wordsToHighlight ){}

        public void DisposePane()
        {
            _lbl.Dispose();
        }

        bool ICommandProcessor.CanExecuteCommand( string action )
        {
            return false;
        }

        void ICommandProcessor.ExecuteCommand( string action )
        {}

        public string GetSelectedText( ref TextFormat format )
        {
            return null;
        }

        public string GetSelectedPlainText()
        {
            return null;
        }
    }
}
