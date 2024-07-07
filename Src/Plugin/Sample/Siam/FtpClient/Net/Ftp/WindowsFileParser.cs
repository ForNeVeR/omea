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
// $Log: WindowsFileParser.cs,v $
// Revision 1.5  2004/11/06 11:10:02  bruceb
// tidied namespaces, changed IOException to SystemException
//
// Revision 1.4  2004/11/05 20:00:28  bruceb
// cleaned up namespaces
//
// Revision 1.3  2004/10/29 09:41:44  bruceb
// removed /// in file header
//
//
//

using System;
using System.Globalization;
using Logger = EnterpriseDT.Util.Debug.Logger;

namespace EnterpriseDT.Net.Ftp
{
	/// <summary>
	/// Represents a remote Windows file parser
	/// </summary>
	/// <author>       Bruce Blackshaw
	/// </author>
	/// <version>      $LastChangedRevision$
	/// </version>
	public class WindowsFileParser:FTPFileParser
	{
		/// <summary> Logging object</summary>
		private Logger log;

		/// <summary>Date format</summary>
		private static readonly string format = "MM'-'dd'-'yy hh':'mmtt";

		/// <summary> Directory field</summary>
		private const string DIR = "<DIR>";

        /// <summary>Splitter token</summary>
        private char[] sep = {' '};

		/// <summary> Number of expected fields</summary>
		private const int MIN_EXPECTED_FIELD_COUNT = 4;

		/// <summary> Default constructor</summary>
		public WindowsFileParser()
		{
            log = Logger.GetLogger(typeof(WindowsFileParser));
		}

		/// <summary> Parse server supplied string. Should be in
		/// form
		/// <![CDATA[
		/// 05-17-03  02:47PM                70776 ftp.jar
		/// 08-28-03  10:08PM       <DIR>          EDT SSLTest
		/// ]]>
		/// </summary>
		/// <param name="raw">
		/// raw string to parse
		/// </param>
		public override FTPFile Parse(string raw)
		{
			string[] fields = Split(raw);

			if (fields.Length < MIN_EXPECTED_FIELD_COUNT)
			{
				throw new FormatException("Unexpected number of fields: " + fields.Length);
			}

			// first two fields are date time
			DateTime lastModified = DateTime.ParseExact(fields[0] + " " + fields[1],
                format, CultureInfo.CurrentCulture.DateTimeFormat);

			// dir flag
			bool isDir = false;
			long size = 0L;
			if (fields[2].ToUpper().Equals(DIR.ToUpper()))
				isDir = true;
			else
			{
				try
				{
					size = Int64.Parse(fields[2]);
				}
				catch (FormatException)
				{
					throw new FormatException("Failed to parse size: " + fields[2]);
				}
			}

			// we've got to find the starting point of the name. We
			// do this by finding the pos of all the date/time fields, then
			// the name - to ensure we don't get tricked up by a date or dir the
			// same as the filename, for example
			int pos = 0;
			bool ok = true;
			for (int i = 0; i < 3; i++)
			{
				pos = raw.IndexOf(fields[i], pos);
				if (pos < 0)
				{
					ok = false;
					break;
				}
                else {
                    pos += fields[i].Length;
                }
			}
			if (ok)
			{
                string name = raw.Substring(pos).Trim();
				return new FTPFile(FTPFile.WINDOWS, raw, name, size, isDir, ref lastModified);
			}
			else
			{
				throw new FormatException("Failed to retrieve name: " + raw);
			}
		}
	}
}
