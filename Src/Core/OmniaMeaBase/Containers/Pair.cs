// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
