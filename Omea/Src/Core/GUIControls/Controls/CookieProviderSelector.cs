// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.HttpTools;

namespace JetBrains.Omea.GUIControls
{
	public class CookieProviderSelector : System.Windows.Forms.UserControl
	{
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox _cookieProvidersBox;
		private System.ComponentModel.Container components = null;

        private class CookieProviderItem
        {
            public const string _noneSelection = "None";

            private ICookieProvider _provider;

            public CookieProviderItem( ICookieProvider provider )
            {
                _provider = provider;
            }

            public override string ToString()
            {
                return ( _provider == null ) ? _noneSelection : _provider.Name;
            }
        }

		public CookieProviderSelector()
		{
			InitializeComponent();
		}

	    public void Populate( Type userClass )
	    {
	        string providerName = CookiesManager.GetUserCookieProviderName( userClass );
	        _cookieProvidersBox.SuspendLayout();
            _cookieProvidersBox.Items.Clear();
            CookieProviderItem item;
            int index;
	        foreach( ICookieProvider provider in CookiesManager.GetAllProviders() )
	        {
                item = new CookieProviderItem( provider );
	            index = _cookieProvidersBox.Items.Add( item );
	            if( item.ToString() == providerName )
	            {
	                _cookieProvidersBox.SelectedIndex = index;
	            }
	        }
	        item = new CookieProviderItem( null );
	        index = _cookieProvidersBox.Items.Add( item );
            if( item.ToString() == providerName )
            {
                _cookieProvidersBox.SelectedIndex = index;
            }
	        _cookieProvidersBox.ResumeLayout();
	    }

	    public string SelectedProfileName
        {
            get
            {
                object item = _cookieProvidersBox.SelectedItem;
                return ( item == null ) ? CookieProviderItem._noneSelection : ( (CookieProviderItem) item ).ToString();
            }
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
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.label1 = new System.Windows.Forms.Label();
            this._cookieProvidersBox = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.Location = new System.Drawing.Point(0, 2);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(108, 22);
            this.label1.TabIndex = 0;
            this.label1.Text = "Use &cookies from";
            //
            // _cookieProvidersBox
            //
            this._cookieProvidersBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._cookieProvidersBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cookieProvidersBox.Location = new System.Drawing.Point(108, 0);
            this._cookieProvidersBox.Name = "_cookieProvidersBox";
            this._cookieProvidersBox.Size = new System.Drawing.Size(252, 21);
            this._cookieProvidersBox.TabIndex = 1;
            //
            // CookieProviderSelector
            //
            this.Controls.Add(this._cookieProvidersBox);
            this.Controls.Add(this.label1);
            this.Name = "CookieProviderSelector";
            this.Size = new System.Drawing.Size(360, 24);
            this.ResumeLayout(false);

        }
		#endregion
	}
}
