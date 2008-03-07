/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Drawing;
using System.Collections;
using System.Windows.Forms;
using JetBrains.Omea.GUIControls;

namespace GUIControls.CustomViews
{
	/// <summary>
	/// Summary description for ShowIconsForm.
	/// </summary>
	public class ShowIconsForm : DialogBase
	{
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Panel  panelIcons;
        private Pen                         normal = new Pen( Color.Aqua, 1.0f ),
                                            inversed = new Pen( Color.White, 1.0f );

        private Hashtable           Icons;
        private ArrayList           validPaths = new ArrayList();
        private int                 IconsPerLine;
        private int                 ChosenIconNumber = -1;

        private const int           cStandardIconSize = 16;
        private const int           cInterIconSpace = 5;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        #region Ctor and Initialization
		public ShowIconsForm( Hashtable allIcons )
		{
			InitializeComponent();
            Icons = allIcons;

            InitializeValidIcons();
            ComputeFormSize();
		}

        private void  InitializeValidIcons()
        {
            foreach( string path in Icons.Keys )
            {
                Icon icon = (Icon) Icons[ path ];
                if( icon.Width == icon.Height && icon.Width == cStandardIconSize )
                    validPaths.Add( path );
            }
            validPaths.Sort();
        }

        private void  ComputeFormSize()
        {
            int sideVar = (int) Math.Sqrt( validPaths.Count );
            sideVar = 5 + sideVar * (cStandardIconSize + cInterIconSpace );
            if( sideVar > panelIcons.Width )
                this.Width += sideVar - panelIcons.Width;
            if( sideVar > panelIcons.Height )
                this.Height += sideVar - panelIcons.Height;
        }
        #endregion Ctor and Initialization

        public string IconName { get{ return (string) validPaths[ ChosenIconNumber ]; } }

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
            this.buttonOK = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.panelIcons = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // buttonOK
            // 
            this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonOK.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonOK.Location = new System.Drawing.Point(204, 240);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.TabIndex = 0;
            this.buttonOK.Text = "OK";
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.buttonCancel.Location = new System.Drawing.Point(292, 240);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.TabIndex = 0;
            this.buttonCancel.Text = "Cancel";
            // 
            // panelIcons
            // 
            this.panelIcons.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
                | System.Windows.Forms.AnchorStyles.Left) 
                | System.Windows.Forms.AnchorStyles.Right)));
            this.panelIcons.AutoScroll = true;
            this.panelIcons.BackColor = Color.White;
            this.panelIcons.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panelIcons.Location = new System.Drawing.Point(8, 8);
            this.panelIcons.Name = "panelIcons";
            this.panelIcons.Size = new System.Drawing.Size(364, 216);
            this.panelIcons.TabIndex = 1;
            this.panelIcons.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ShowIconsForm_MouseUp);
            this.panelIcons.DoubleClick += new EventHandler(panelIcons_DoubleClick);
            // 
            // ShowIconsForm
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 14);
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(380, 273);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Controls.Add(this.panelIcons);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.buttonCancel);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "ShowIconsForm";
            this.Text = "Available Icons";
            this.ResumeLayout(false);

            buttonOK.Enabled = false;
        }
		#endregion

		protected override void OnPaint( PaintEventArgs pe )
		{
            int x = cInterIconSpace, y = cInterIconSpace;
            IconsPerLine = validPaths.Count;
            for( int i = 0; i < validPaths.Count; i++ )
            {
                Icon icon = (Icon) Icons[ (string)validPaths[ i ] ];
                if( x + icon.Width >= panelIcons.Width )
                {
                    x = cInterIconSpace;
                    y += cStandardIconSize + cInterIconSpace;
                    if( IconsPerLine == validPaths.Count )
                        IconsPerLine = i;
                }

                Graphics.FromHwnd( panelIcons.Handle ).DrawIcon( icon, x, y );
                x += icon.Width + cInterIconSpace;
            }
        }

        private void ShowIconsForm_MouseUp(object sender, MouseEventArgs e)
        {
            int  posX = (e.X - cInterIconSpace) / (cInterIconSpace + cStandardIconSize),
                 posY = (e.Y - cInterIconSpace) / (cInterIconSpace + cStandardIconSize);
            int newIndex = posY * IconsPerLine + posX;
            if( newIndex < validPaths.Count )
            {
                if( ChosenIconNumber != -1 )
                    DrawRectangle( ChosenIconNumber );
                DrawRectangle( posX, posY, normal );
                ChosenIconNumber = newIndex;
            }

            buttonOK.Enabled = (ChosenIconNumber > -1) && (ChosenIconNumber < validPaths.Count);
        }

        private void  DrawRectangle( int index )
        {
            int posX = index % IconsPerLine,
                posY = index / IconsPerLine;
            DrawRectangle( posX, posY, inversed );
        }
        private void  DrawRectangle( int posX, int posY, Pen pen )
        {
            int leftCorner = cInterIconSpace + posX * (cInterIconSpace + cStandardIconSize),
                topCorner = cInterIconSpace + posY * (cInterIconSpace + cStandardIconSize);
            Graphics.FromHwnd( panelIcons.Handle ).DrawRectangle( pen, leftCorner - 1, topCorner - 1, cStandardIconSize + 2, cStandardIconSize + 2 );
        }

        private void panelIcons_DoubleClick(object sender, EventArgs e)
        {
            if( buttonOK.Enabled )
                DialogResult = DialogResult.OK;
        }
    }
}
