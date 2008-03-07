////////////////////////////////////////////////////////////////////////////////////
//  File:   Designers.cs
//  Author: Sergei Pavlovsky
//
//  Copyright (c) 2004 by Sergei Pavlovsky (sergei_vp@hotmail.com, sergei_vp@ukr.net)
//
//	This file is provided "as is" with no expressed or implied warranty.
//	The author accepts no liability if it causes any damage whatsoever.
// 
//  This code is free and may be used in any way you desire. If the source code in 
//  this file is used in any commercial application then a simple email would be 
//	nice.
////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;


namespace SP.Windows
{
	/// <summary>
	/// HeaderDesigner class
	/// </summary>
	public class HeaderDesigner : ControlDesigner
	{
		/// <summary>
		/// Construction
		/// </summary>
		public HeaderDesigner()
		{
		}

		/// <summary>
		/// Overrides
		/// </summary>
		public override ICollection AssociatedComponents
		{
			get 
			{  
				Header header = base.Control as Header;
				if ( header != null )
				return header.Sections; 

				return base.AssociatedComponents; 
			}
		}

		protected override void PostFilterProperties(IDictionary Properties) 
		{
			Properties.Remove("BackgroundImage");
			Properties.Remove("BackColor");
			Properties.Remove("ForeColor");
			Properties.Remove("Cursor");
			Properties.Remove("Text");
			Properties.Remove("TabStop");
		}

		protected override void WndProc(ref Message message)
		{
			if ( message.Msg == NativeHeader.WM_NOTIFY + NativeHeader.OCM__BASE )
			{
				NativeWindowCommon.NMHDR nmhdr = 
					(NativeWindowCommon.NMHDR)message.GetLParam(typeof(NativeWindowCommon.NMHDR));

				if ( nmhdr.code == NativeHeader.HDN_ENDTRACK )
				{
					IComponentChangeService service 
						= (IComponentChangeService)GetService(typeof(IComponentChangeService));

					service.OnComponentChanged(this.Component, null, null, null);					
				}
			}

			base.WndProc(ref message);
		}

		protected override bool GetHitTest(Point point)
		{ 
			Header.HitTestArea fDirectEdit = Header.HitTestArea.OnDivider|
											 Header.HitTestArea.OnDividerOpen;

			Header header = (Header)this.Component;
			Point ptClient = this.Control.PointToClient(point);

			Header.HitTestInfo hti = header.HitTest(point);

			return (hti.fArea & fDirectEdit) != 0;
		}

	}
}
