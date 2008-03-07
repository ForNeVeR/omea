/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System.Diagnostics;

namespace JetBrains.Omea.OutlookPlugin
{
	/// <summary>
	/// OutlookKiller serves to kill fat dirty asses that called Outlooks.
	/// </summary>
	internal class OutlookKiller
	{
        public static void KillFatAsses()
        {
            Process[] victims = null;
            try
            {
                victims = Process.GetProcessesByName( "outlook" );
            }
            catch{}
            if ( victims != null )
            {
                foreach( Process victim in victims )
                {
                    try
                    {
                        victim.Kill();
                    }
                    catch{}
                }
            }
        }
	}
}
