// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
