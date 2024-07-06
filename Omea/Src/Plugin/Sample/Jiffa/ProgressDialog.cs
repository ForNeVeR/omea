// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.Drawing;
using System.Windows.Forms;

namespace JetBrains.Omea.Jiffa
{
	public partial class ProgressDialog : Form
	{
		public ProgressDialog()
		{
			InitializeComponent();
		}

		///<summary>
		///Gets or sets the image that is displayed by <see cref="T:System.Windows.Forms.PictureBox"></see>.
		///</summary>
		///
		///<returns>
		///The <see cref="T:System.Drawing.Image"></see> to display.
		///</returns>
		///<filterpriority>1</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.EnvironmentPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" /><IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" /><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="UnmanagedCode, ControlEvidence" /><IPermission class="System.Diagnostics.PerformanceCounterPermission, System, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true" /></PermissionSet>
		public Image Image
		{
			get
			{
				return _image.Image;
			}
			set
			{
				_image.Image = value;
			}
		}

		/// <summary>
		/// Gets or sets the status text of the progress dialog.
		/// </summary>
		public string StatusText
		{
			get
			{
				return _labelStatus.Text;
			}
			set
			{
				_labelStatus.Text = value;
			}
		}
	}
}
