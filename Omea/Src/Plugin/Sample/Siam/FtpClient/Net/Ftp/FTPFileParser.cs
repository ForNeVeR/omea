// edtFTPnet
/*
SPDX-FileCopyrightText: 2004 Enterprise Distributed Technologies Ltd

SPDX-License-Identifier: LGPL-2.1-or-later
*/
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
