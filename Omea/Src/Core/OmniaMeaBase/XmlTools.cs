/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Xml;

namespace JetBrains.Omea.Base
{
    /**
     * Helper functions for working with XML.
     */
	
    public class XmlTools
	{
        private XmlTools() {}

        public static string GetRequiredAttribute( XmlNode node, string attrName )
        {
            XmlAttribute attr = node.Attributes [attrName];
            if ( attr == null )
                throw new XmlToolsException( "<" + node.Name + "> '" + attrName + "' attribute not specified" );            

            return attr.Value;
        }

        public static string GetOptionalAttribute( XmlNode node, string attrName )
        {
            XmlAttribute attr = node.Attributes [attrName];
            return (attr == null)? null : attr.Value;
        }

        public static int GetIntAttribute( XmlNode node, string attrName, int defaultValue )
        {
            XmlAttribute attr = node.Attributes [attrName];
            if ( attr == null )
                return defaultValue;

            return Int32.Parse( attr.Value );
        }

        public static int GetRequiredIntAttribute( XmlNode node, string attrName )
        {
            XmlAttribute attr = node.Attributes [attrName];
            if ( attr == null )
                throw new XmlToolsException( "<" + node.Name + "> '" + attrName + "' attribute not specified" );            

            return Int32.Parse( attr.Value );
        }
	}

    public class XmlToolsException: Exception
    {
        public XmlToolsException( string msg ): base( msg ) {}
    }
}
