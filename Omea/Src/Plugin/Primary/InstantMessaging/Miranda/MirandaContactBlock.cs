// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.InstantMessaging.Miranda
{
	/**
     * Block for viewing Miranda account information in the contact view.
     */

    internal class MirandaContactBlock : AbstractContactViewBlock
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        private Font _normalFont = new Font( "Tahoma", 8 );
        private Font _boldFont = new Font( "Tahoma", 8, FontStyle.Bold );
        private ControlPool _typeLabelPool;
        private ControlPool _valueLabelPool;

		public MirandaContactBlock()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

		    _typeLabelPool = new ControlPool( this, new ControlPoolCreateDelegate( CreateTypeLabel ) );
            _valueLabelPool = new ControlPool( this, new ControlPoolCreateDelegate( CreateValueLabel ) );
        }

	    internal static AbstractContactViewBlock CreateBlock()
        {
            return new MirandaContactBlock();
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
            this.SuspendLayout();
            this.Name = "MirandaContactBlock";
            this.ResumeLayout(false);

        }
		#endregion

        public override void EditResource( IResource res )
	    {
            IResourceList accounts = res.GetLinksOfType( null, Props.MirandaAcct );
            accounts.Sort( new SortSettings( ResourceProps.Type, true ) );
            int curY = 4;

            _typeLabelPool.MoveControlsToPool();
            _valueLabelPool.MoveControlsToPool();

            foreach( IResource acct in accounts )
            {
                string acctType;
                switch( acct.Type )
                {
                    case ResourceTypes.MirandaICQAccount:    acctType = "ICQ";    break;
                    case ResourceTypes.MirandaAIMAccount:    acctType = "AIM";    break;
                    case ResourceTypes.MirandaJabberAccount: acctType = "Jabber"; break;
                    case ResourceTypes.MirandaYahooAccount:  acctType = "Yahoo";  break;
                    default: acctType = "Other"; break;
                }

                Label lblType = (Label) _typeLabelPool.GetControl();
                lblType.Text = acctType + ":";
                lblType.Location = new Point( 4, curY );

                JetTextBox lblValue = (JetTextBox) _valueLabelPool.GetControl();
                if ( acct.Type == ResourceTypes.MirandaICQAccount )
                {
                    lblValue.Text = acct.GetPropText( "UIN" );
                }
                else
                {
                    lblValue.Text = acct.DisplayName;
                }

                lblValue.Location = new Point( 88, curY );
                lblValue.Width = Width - 96;

                curY += 20;
            }

            _typeLabelPool.RemovePooledControls();
            _valueLabelPool.RemovePooledControls();

            Height = curY + 4;
	    }

        public override bool OwnsProperty( int propId )
        {
            return propId == Props.MirandaAcct;
        }

        private Control CreateTypeLabel()
        {
            Label lblType = new Label();
            lblType.FlatStyle = FlatStyle.System;
            lblType.AutoSize = true;
            lblType.Font = _normalFont;
            return lblType;
        }

        private Control CreateValueLabel()
        {
            JetTextBox edtValue = new JetTextBox();
            edtValue.Font = _boldFont;
            edtValue.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            edtValue.ReadOnly = true;
            edtValue.BackColor = BackColor;
            edtValue.BorderStyle = BorderStyle.None;
            return edtValue;
        }

        public override string  HtmlContent( IResource contact )
        {
            string  result = string.Empty;
            IResourceList accounts = contact.GetLinksOfType( null, Props.MirandaAcct );
            accounts.Sort( new SortSettings( ResourceProps.Type, true ) );

            foreach( IResource acct in accounts )
            {
                string acctType;
                switch( acct.Type )
                {
                    case ResourceTypes.MirandaICQAccount:    acctType = "ICQ";    break;
                    case ResourceTypes.MirandaAIMAccount:    acctType = "AIM";    break;
                    case ResourceTypes.MirandaJabberAccount: acctType = "Jabber"; break;
                    case ResourceTypes.MirandaYahooAccount:  acctType = "Yahoo";  break;
                    default: acctType = "Other"; break;
                }

                result += "\t<tr><td>" + acctType + "</td><td>";
                if ( acct.Type == ResourceTypes.MirandaICQAccount )
                {
                    result += acct.GetPropText( "UIN" ) + "</td>";
                }
                else
                {
                    result += acct.DisplayName + "</td>";
                }
                result += "</tr>";
            }
            return result;
        }
    }
}
