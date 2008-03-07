/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Windows.Forms;

namespace JetBrains.JetListViewLibrary
{
    public interface IControlMethodInvoker
    {
        void BeginInvoke( Delegate method, params object[] args );
        bool InvokeRequired { get; }
    }

	public class ControlMethodInvoker: IControlMethodInvoker
	{
        private Control _control;

	    public ControlMethodInvoker( Control control )
	    {
	        _control = control;
	    }

	    public void BeginInvoke( Delegate method, params object[] args )
	    {
	        _control.BeginInvoke( method, args );
	    }

	    public bool InvokeRequired
	    {
	        get { return _control.InvokeRequired; }
	    }
	}
}
