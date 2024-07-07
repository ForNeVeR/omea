// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ContactsPlugin
{
	/**
     * Pane for selecting an email account that belongs to the current user.
     */

    public class EmailAccountSelector: GenericResourceSelectPane
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public EmailAccountSelector()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitializeComponent call

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
		}
		#endregion

        public override void SelectResources( string[] resTypes, IResourceList baseList, IResourceList selection )
        {
            IResourceList accounts = Core.ResourceStore.EmptyResourceList;
            if ( Core.ContactManager.MySelf.Resource != null )
            {
                accounts = Core.ContactManager.MySelf.Resource.GetLinksOfType( "EmailAccount", "EmailAcct" );
            }
            base.SelectResources( resTypes, accounts, selection );
        }
	}
}
