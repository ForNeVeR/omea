// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.GUIControls
{
    /// <summary>
    /// A toolbar which contais buttons and other controls applicable to formatting the content of rich editors, such as HTML or RTF editors.
    /// The toolbar is bound to the control it rules over by giving it the proper command processor after creation.
    /// </summary>
    public class RichEditToolbarFull : RichEditToolbar
    {
        public RichEditToolbarFull()
        {
            AddActions();
        }

        private void  AddActions()
        {
            string sGroup;

            sGroup = "Font";
            _actionmanager.RegisterAction( new CommandProcessorAction( "IncFontSize" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.IncFont" ), "", "Increase Font Size", null, null );
            _actionmanager.RegisterAction( new CommandProcessorAction( "DecFontSize" ), sGroup, ListAnchor.Last, LoadImage( "RichEdit.DecFont" ), "", "Decrease Font Size", null, null );
        }
    }
}
