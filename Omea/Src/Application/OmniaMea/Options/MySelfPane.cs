// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.ComponentModel;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	public class MySelfPane : AbstractOptionsPane
	{
        private Label label1;
        private Label label2;
        private TextBox _firstName;
        private TextBox _lastName;
        private Label label5;
        private GroupBox groupBox1;
        private EmailBlock _emailBlock;
        private CheckBox _checkShowOrigNames;
		private Container components = null;

		private MySelfPane()
		{
			InitializeComponent();
		}

        public static AbstractOptionsPane MySelfPaneCreator()
        {
            return new MySelfPane();
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
            this.label2 = new System.Windows.Forms.Label();
            this._firstName = new System.Windows.Forms.TextBox();
            this._lastName = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this._emailBlock = new JetBrains.Omea.GUIControls.EmailBlock();
            this._checkShowOrigNames = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            //
            // label1
            //
            this.label1.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label1.Location = new System.Drawing.Point(0, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(104, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "&First Name:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _firstName
            //
            this._firstName.Location = new System.Drawing.Point(112, 4);
            this._firstName.Name = "_firstName";
            this._firstName.Size = new System.Drawing.Size(152, 20);
            this._firstName.TabIndex = 1;
            this._firstName.Text = "";
            //
            // label2
            //
            this.label2.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label2.Location = new System.Drawing.Point(0, 36);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 20);
            this.label2.TabIndex = 2;
            this.label2.Text = "&Last Name:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // _lastName
            //
            this._lastName.Location = new System.Drawing.Point(112, 32);
            this._lastName.Name = "_lastName";
            this._lastName.Size = new System.Drawing.Size(152, 20);
            this._lastName.TabIndex = 3;
            this._lastName.Text = "";
            //
            // label5
            //
            this.label5.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.label5.Location = new System.Drawing.Point(0, 64);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(176, 16);
            this.label5.TabIndex = 13;
            this.label5.Text = "&E-mail addresses:";
            //
            // groupBox1
            //
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Location = new System.Drawing.Point(180, 64);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(268, 8);
            this.groupBox1.TabIndex = 15;
            this.groupBox1.TabStop = false;
            //
            // _emailBlock
            //
            this._emailBlock.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                | System.Windows.Forms.AnchorStyles.Left)
                | System.Windows.Forms.AnchorStyles.Right)));
            this._emailBlock.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this._emailBlock.Location = new System.Drawing.Point(0, 80);
            this._emailBlock.Name = "_emailBlock";
            this._emailBlock.Size = new System.Drawing.Size(448, 272);
            this._emailBlock.TabIndex = 16;
            //
            // _checkShowOrigNames
            //
            this._checkShowOrigNames.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this._checkShowOrigNames.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this._checkShowOrigNames.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this._checkShowOrigNames.Location = new System.Drawing.Point(8, 360);
            this._checkShowOrigNames.Name = "_checkShowOrigNames";
            this._checkShowOrigNames.Size = new System.Drawing.Size(450, 22);
            this._checkShowOrigNames.TabIndex = 17;
            this._checkShowOrigNames.Text = "Show name used by &sender in received messages addressed to me";
            //
            // MySelfPane
            //
            this.Controls.Add(this._checkShowOrigNames);
            this.Controls.Add(this._emailBlock);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this._lastName);
            this.Controls.Add(this._firstName);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Name = "MySelfPane";
            this.Size = new System.Drawing.Size(448, 392);
            this.ResumeLayout(false);

        }
		#endregion

        public override void ShowPane()
        {
            IContact me = Core.ContactManager.MySelf;
            _firstName.Text = me.FirstName;
            _lastName.Text = me.LastName;

            if( IsStartupPane )
                _emailBlock.SetStartupMode();
            _emailBlock.EditResource( me.Resource );
            _checkShowOrigNames.Checked = me.Resource.HasProp( Core.ContactManager.Props.ShowOriginalNames );
        }

        public override void OK()
        {
            if( !Core.ResourceStore.PropTypes.Exist( "FirstName" ) )
            {
                _emailBlock.Save();
            }
            else
            {
                Core.ResourceAP.RunUniqueJob(
                    new GetUpdatedMyselfDelegate( GetUpdatedMyself ), _firstName.Text, _lastName.Text, _checkShowOrigNames.Checked );
                //  Difference in two methods of Save is explaned by the fact
                //  that Core.ContactManager.MySelf has side-effects (creation
                //  of a resource). Thus the resource is passed originally to
                //  the Email block may become invalidated. Use explicit
                //  resource for saving.
                _emailBlock.Save( Core.ContactManager.MySelf.Resource );
            }
        }

	    private delegate void GetUpdatedMyselfDelegate( string firstName, string lastName, bool showOrigNames );

	    private void GetUpdatedMyself( string firstName, string lastName, bool showOrigNames )
	    {
	        IContact myself = Core.ContactManager.MySelf;
	        myself.FirstName = firstName;
	        myself.LastName = lastName;
	        myself.Resource.SetProp( Core.ContactManager.Props.ShowOriginalNames, showOrigNames );
	    }

	    public override string GetHelpKeyword()
	    {
	        return "/reference/user_info.html";
	    }
	}
}
