﻿/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

/////////////////////////////////////////////////////////
// Parses the DTD files from w3c to extract the character entity reference definitions from there and store them as XML format, so that the resulting file could be added to the resources and used by HtmlEntityReader class as an entites definition source.

import System;
import System.Xml;
import System.Text.RegularExpressions;
import System.Text;
import System.IO;

try
{
	var	xml = new XmlDocument();
	
	xml.AppendChild(xml.CreateComment("\n\tList of HTML Character Entities, as it resides in the OmniaMeaBase resources.\n\tAutogenerated from DTDs, DTDs are retrieved from the w3c website (see http://www.w3.org/TR/html4/sgml/entities.html).\n\tUse ExtractEntities.js to convert DTDs into this file.\n\n\t(H) Serge, 2005\n"));
	
	var	xmlEntities : XmlElement = xml.CreateElement("Entites");
	xml.AppendChild(xmlEntities);
	
	ParseDtdSource("Entities.ISO 8859-1 (Latin-1) characters.dtd", xmlEntities);
	ParseDtdSource("Entities.markup-significant and internationalization characters.dtd", xmlEntities);
	ParseDtdSource("Entities.symbols, mathematical symbols, and Greek letters.dtd", xmlEntities);	

	print("Saving");	
	xml.Save("HtmlEntities.xml");
	print("Done OK");
}
catch(ex : Exception)
{
	print(ex);
}

function ParseDtdSource(filename : System.String, root : XmlElement)
{
	var	reader : StreamReader = new StreamReader(filename);
	
	var	sLine : System.String;
	var	regex : Regex = new Regex("\<\!ENTITY +(?<name>[^ %]+) +CDATA +\"&#(?<value>[^;]+);\"");
	var	match : Match;
	var	xmlEntity : XmlElement;
	while((sLine = reader.ReadLine()) != null)
	{
		match = regex.Match(sLine);
		if(!match.Success)
			continue;

		root.AppendChild(xmlEntity = root.OwnerDocument.CreateElement("Entity"));
		xmlEntity.SetAttribute("Name", match.Groups["name"]);
		xmlEntity.SetAttribute("Value", match.Groups["value"]);
	}
}
