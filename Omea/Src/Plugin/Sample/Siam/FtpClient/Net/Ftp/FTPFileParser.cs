// edtFTPnet
// 
// Copyright (C) 2004 Enterprise Distributed Technologies Ltd
// 
// www.enterprisedt.com
// 
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
// 
// Bug fixes, suggestions and comments should posted on 
// http://www.enterprisedt.com/forums/index.php
// 
// Change Log:
// 
// $Log: FTPFileParser.cs,v $
// Revision 1.5  2004/11/05 20:00:28  bruceb
// cleaned up namespaces
//
// Revision 1.4  2004/10/29 09:41:44  bruceb
// removed /// in file header
//
//
//

using System;
using System.Text;
using System.Collections;
    
namespace EnterpriseDT.Net.Ftp
{
	/// <summary>  
	/// Root class of all file parsers
	/// </summary>
	/// <author>       
	/// Bruce Blackshaw
	/// </author>
	/// <version>      
	/// $LastChangedRevision$
	/// </version>
	abstract public class FTPFileParser
	{
		
		/// <summary> Maximum number of fields in raw string</summary>
		private const int MAX_FIELDS = 20;
		
		/// <summary> Parse server supplied string
		/// 
		/// </summary>
		/// <param name="raw">  raw string to parse
		/// </param>
		abstract public FTPFile Parse(string raw);
                
        /// <summary>
        /// Splits string consisting of fields separated by
        /// whitespace into an array of strings
        /// </summary>
        /// <param name="str">
        /// string to split
        /// </param>   
        protected string[] Split(string str) {
            ArrayList fields = new ArrayList();
            StringBuilder field = new StringBuilder();
            for (int i = 0; i < str.Length; i++) {
                char ch = str[i];
                if (!Char.IsWhiteSpace(ch))
                    field.Append(ch);
                else {
                    if (field.Length > 0) {
                        fields.Add(field.ToString());
                        field.Length = 0;
                    }
                }
            }
            // pick up last field
            if (field.Length > 0) {
                fields.Add(field.ToString());
            }
            string[] result = (string[])fields.ToArray(typeof(string));
            return result;
        }
	}
}