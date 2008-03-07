/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

namespace JetBrains.Omea.Containers
{

    public  class   Pair
    {
        public  Pair( object x, object y )
        {
            oFirst = x;
            oSecond = y;
        }

        public  object  First
        {
            get{ return oFirst; }
            set{ oFirst = value; }
        }
        public  object  Second
        {
            get{ return oSecond; }
            set{ oSecond = value; }
        }

        //-------------------------------------------------------------------------
        protected   object  oFirst;
        protected   object  oSecond;
    }

}