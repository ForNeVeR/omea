// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collections;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Favorites
{
	internal class ImportBookmarksOptionsPane : AbstractOptionsPane
	{
		private System.ComponentModel.Container components = null;
        private static ArrayList _creators = new ArrayList();
        private ArrayList _panes = new ArrayList();

		private ImportBookmarksOptionsPane()
		{
			InitializeComponent();
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

        public static AbstractOptionsPane StartupWizardPaneCreator()
        {
            return new ImportBookmarksOptionsPane();
        }

        public static void AddPane( OptionsPaneCreator paneCreator )
        {
            _creators.Add( paneCreator );
        }

        public override void ShowPane()
        {
            int y = 0;
            BookmarksOptionsPane lastPane = null;

            foreach( OptionsPaneCreator creator in _creators )
            {
                BookmarksOptionsPane pane = (BookmarksOptionsPane) creator();
                pane.IsStartupPane = IsStartupPane;
                pane.ShowPane();
                pane.Top = y;
                y += pane.OccupiedHeight;
                pane.Height = pane.OccupiedHeight;
                pane.Width = Width;
                Controls.Add( pane );
                _panes.Add( pane );
                pane.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
                lastPane = pane;
            }

            if( lastPane != null )
            {
                lastPane.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom;
                lastPane.Height = Height - ( y - lastPane.Height );
            }

            DialogBase.AdjustContolProperties( this );
        }

        public override void OK()
        {
            foreach( BookmarksOptionsPane pane in _panes )
            {
                pane.OK();
            }
        }

        public override void Cancel()
        {
            foreach( BookmarksOptionsPane pane in _panes )
            {
                pane.Cancel();
            }
        }

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            //
            // StartupWizardPane
            //
            this.Name = "StartupWizardPane";
            this.Size = new System.Drawing.Size(304, 160);

        }
		#endregion
	}

    internal abstract class BookmarksOptionsPane : AbstractOptionsPane
    {
        public abstract int OccupiedHeight { get; }
    }
}
