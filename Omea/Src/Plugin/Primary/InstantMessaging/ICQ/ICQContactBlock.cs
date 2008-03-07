/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Drawing;
using System.Text;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.GUIControls;

namespace JetBrains.Omea.InstantMessaging.ICQ
{
    internal class ICQContactBlock : AbstractContactViewBlock
    {
        private System.Windows.Forms.Label _lblICQ;
        private JetTextBox _uinsList;
    
        private ICQContactBlock()
        {
            InitializeComponent();
        }

        public static AbstractContactViewBlock CreateBlock()
        {
            return new ICQContactBlock();
        }

        public override void EditResource( IResource contact )
        {
            SuspendLayout();
            try
            {
                IResourceList uinList = contact.GetLinksOfType( ICQPlugin._icqAccountResName, ICQPlugin._propICQAcct );
                if( uinList.Count == 0 )
                {
                    _uinsList.ForeColor = SystemColors.GrayText;
                    _uinsList.Enabled = false;
                    _uinsList.Text = "Not Specified";
                }
                else
                {
                    _uinsList.ForeColor = SystemColors.ControlText;
                    _uinsList.Enabled = true;
                    StringBuilder uinBuilder = StringBuilderPool.Alloc();
                    try 
                    {
                        foreach( IResource uin in uinList )
                        {
                            uinBuilder.Append( uin.ToString() ).Append( ';' );
                        }
                        _uinsList.Text = uinBuilder.ToString( 0, uinBuilder.Length - 2 );
                    }
                    finally
                    {
                        StringBuilderPool.Dispose( uinBuilder );
                    }
                }
                Height = _uinsList.PreferredHeight;
                _uinsList.BackColor = BackColor;
            }
            finally
            {
                ResumeLayout();
            }
        }

        public override bool OwnsProperty( int propId )
        {
            return propId == ICQPlugin._propICQAcct;
        }

        private void InitializeComponent()
        {
            _lblICQ = new System.Windows.Forms.Label();
            _uinsList = new JetTextBox();
            SuspendLayout();
            // 
            // _lblICQ
            // 
            _lblICQ.FlatStyle = System.Windows.Forms.FlatStyle.System;
            _lblICQ.Location = new Point(4, 0);
            _lblICQ.Name = "_lblICQ";
            _lblICQ.Size = new Size(68, 16);
            _lblICQ.TabIndex = 2;
            _lblICQ.Text = "ICQ:";
            // 
            // _uinsList
            // 
            _uinsList.Anchor = (((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            _uinsList.BackColor = SystemColors.Control;
            _uinsList.BorderStyle = System.Windows.Forms.BorderStyle.None;
            _uinsList.ContextProvider = null;
            _uinsList.EmptyText = null;
            _uinsList.Location = new Point(88, 0);
            _uinsList.Multiline = false;
            _uinsList.Name = "_uinsList";
            _uinsList.ReadOnly = true;
            _uinsList.Size = new Size(120, 24);
            _uinsList.TabIndex = 3;
            _uinsList.Text = "";
            // 
            // ICQContactBlock
            // 
            Controls.Add(_uinsList);
            Controls.Add(_lblICQ);
            Name = "ICQContactBlock";
            Size = new Size(208, 30);
            ResumeLayout(false);
        }

        public override string  HtmlContent( IResource contact )
        {
            StringBuilder result = new StringBuilder( "\t<tr><td>ICQ:</td>" );
            IResourceList uinList = contact.GetLinksOfType(ICQPlugin._icqAccountResName, ICQPlugin._propICQAcct);
            if( uinList.Count > 0 )
            {
                result.Append( "<td>" );
                foreach( IResource res in uinList )
                    result.Append( res + "<br/>" );
                result.Append( "</td>" );
            }
            else
                result.Append( ContactViewStandardTags.NotSpecifiedHtmlText );
            result.Append("</tr>");
            return result.ToString();
        }
    }
}