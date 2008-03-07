/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Xml;

namespace JetBrains.Omea.RSSPlugin
{
	/// <summary>
	/// Summary description for LooseNSXmlParser.
	/// </summary>
	internal class LooseNSManager : XmlNamespaceManager
	{
        public LooseNSManager() : this( null, new Hashtable() )
        {
        }
        
        public LooseNSManager( XmlNameTable table ) : this( table, null )
        {
        }

        internal LooseNSManager( XmlNameTable table, Hashtable anst ) : base( table )
        {
            if( anst != null )
            {
                foreach( string k in anst.Keys )
                {
                    AddNamespace( k, anst[ k ] as string );
                }
            }
        }

        public override bool HasNamespace( string prefix )
        {
            if( base.HasNamespace( prefix ) )
            {
                return true;
            }
            // Check for special namespaces
            if( base.LookupNamespace( prefix ) != null )
            {
                // don't add because it is found
                return false;
            }
            AddNamespace( prefix, "uri:" + new Random().Next( 1000, 9999 ).ToString() );
            return true;
        }

        public override string LookupNamespace( string prefix )
        {
            HasNamespace( prefix );
            return base.LookupNamespace( prefix );
        }
    }
}
