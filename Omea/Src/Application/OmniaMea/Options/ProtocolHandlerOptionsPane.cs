// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Collections;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
	/// Summary description for ProtocolHandlerOptionsPane.
	/// </summary>
	public class ProtocolHandlerOptionsPane : AbstractOptionsPane
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        public static AbstractOptionsPane Creator()
        {
            return new ProtocolHandlerOptionsPane();
        }

		public ProtocolHandlerOptionsPane()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            //
            // ProtocolHandlerOptionsPane
            //
            this.Name = "ProtocolHandlerOptionsPane";
            this.Size = new System.Drawing.Size(524, 320);
            this.Load += new System.EventHandler(this.OnLoad);

        }
		#endregion

        private class ProtocolCtrl
        {
            private System.Windows.Forms.Label _friendlyName = new System.Windows.Forms.Label();
            private System.Windows.Forms.Label _defaultText = new System.Windows.Forms.Label();
            private System.Windows.Forms.Button _makeDefault = new System.Windows.Forms.Button();
            private ArrayList _handlers;

            public ProtocolCtrl( ProtocolHandlerOptionsPane pane, ArrayList handlers, int index )
            {
                _handlers = handlers;

                IResource handler = (IResource)_handlers[0];

                string friendlyName = handler.GetPropText( ProtocolHandlersInResourceStore._propFriendlyName ) + ": ";
                _friendlyName.Text = friendlyName.Substring( 0, 1 ).ToUpper() + friendlyName.Remove( 0, 1 );

                bool isDefaultHandler = true;
                foreach ( IResource protocol in handlers )
                {
                    if ( !ProtocolHandlersInRegistry.IsDefaultHandler( protocol.GetPropText( ProtocolHandlersInResourceStore._propProtocol ) ) )
                    {
                        isDefaultHandler = false;
                        break;
                    }
                }
                if ( isDefaultHandler )
                {
                    _defaultText.Text = "Default";
                    _makeDefault.Visible = false;
                }
                else
                {
                    _defaultText.Text = "Not Default";
                }

                int coordY = index * 25 + 12;
                //
                // _friendlyName
                //
                _friendlyName.FlatStyle = System.Windows.Forms.FlatStyle.System;
                _friendlyName.Location = new System.Drawing.Point(8, coordY );
                _friendlyName.Size = new System.Drawing.Size( 200, 20 );
                _friendlyName.TabIndex = 0;
                //
                // _defaultText
                //
                _defaultText.FlatStyle = System.Windows.Forms.FlatStyle.System;
                _defaultText.Location = new System.Drawing.Point(216, coordY );
                _defaultText.Size = new System.Drawing.Size(92, 20 );
                _defaultText.TabIndex = 2;
                //
                // _makeDefault
                //
                _makeDefault.FlatStyle = System.Windows.Forms.FlatStyle.System;
                _makeDefault.Location = new System.Drawing.Point(328, coordY - 4 );
                _makeDefault.Size = new System.Drawing.Size(88, 23 );
                _makeDefault.TabIndex = 1;
                _makeDefault.Text = "Make Default";
                _makeDefault.Click += new System.EventHandler(this.OnClick);
                //
                // ProtocolHandlerOptionsPane
                //
                pane.Controls.Add( _friendlyName );
                pane.Controls.Add( _defaultText );
                if ( handler.GetPropText( ProtocolHandlersInResourceStore._propProtocol ).ToLower() != "http" )
                {
                    pane.Controls.Add( _makeDefault );
                }
            }

            private void OnClick(object sender, System.EventArgs e)
            {
                IResource handler = (IResource)_handlers[0];
                string friendlyName = handler.GetPropText( ProtocolHandlersInResourceStore._propFriendlyName );
                string message = "Would you like to check on Omea startup that it is the default '" + friendlyName + "' ?";
                DialogResult result =
                    MessageBox.Show( message, Core.ProductFullName, MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question, MessageBoxDefaultButton.Button3 );
                if ( result == DialogResult.Cancel )
                {
                    return;
                }

                foreach ( IResource protocol in _handlers )
                {
                    ProtocolHandlerManager.SetAsDefaultHandler( protocol, result == DialogResult.Yes );
                }
                string protocolName = handler.GetPropText( ProtocolHandlersInResourceStore._propProtocol );
                if ( ProtocolHandlersInRegistry.IsDefaultHandler( protocolName ) )
                {
                    _defaultText.Text = "Default";
                    _makeDefault.Visible = false;
                }
                else
                {
                    _defaultText.Text = "Not Default";
                    _makeDefault.Visible = true;
                }
            }
        }
        private void OnLoad(object sender, System.EventArgs e)
        {
            IResourceList handlers = ProtocolHandlersInResourceStore.GetProtocolHandlersList();
            HashMap protocolSet = ProtocolHandlerManager.GetProtocols( handlers );
            int count = 0;
            foreach ( HashMap.Entry entry in protocolSet )
            {
                new ProtocolCtrl( this, entry.Value as ArrayList, count++ );
            }
        }

	    public override string GetHelpKeyword()
	    {
	        return "/reference/default_application.htm";
	    }
	}
}
