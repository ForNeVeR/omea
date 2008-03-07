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
// $Log: UnixFileParser.cs,v $
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
using System.Text;
using Logger = EnterpriseDT.Util.Debug.Logger;
 
namespace EnterpriseDT.Net.Ftp
{
   
	/// <summary>  
	/// Represents a remote Unix file parser
	/// </summary>
	/// <author>       
	/// Bruce Blackshaw
	/// </author>
	/// <version>      
	/// $LastChangedRevision$
	/// </version>
	public class UnixFileParser:FTPFileParser
	{		
		/// <summary> Symbolic link symbol</summary>
		private const string SYMLINK_ARROW = "->";
		
		/// <summary> Indicates symbolic link</summary>
		private const char SYMLINK_CHAR = 'l';
		
		/// <summary> Indicates ordinary file</summary>
		private const char ORDINARY_FILE_CHAR = '-';
		
		/// <summary> Indicates directory</summary>
		private const char DIRECTORY_CHAR = 'd';
		
		/// <summary>Date format 1</summary>
		private const string format1a = "MMM'-'d'-'yyyy";
        
        /// <summary>Date format 1</summary>
		private const string format1b = "MMM'-'dd'-'yyyy";
        
        /// <summary>array of format 1 formats</summary> 
        private string[] format1 = {format1a,format1b};

		/// <summary>Date format 2</summary>
		private const string format2a = "MMM'-'d'-'yyyy'-'HH':'mm";
		
        /// <summary>Date format 2</summary>
		private const string format2b = "MMM'-'dd'-'yyyy'-'HH':'mm";	
        
        /// <summary>array of format 2 formats</summary>
        private string[] format2 = {format2a,format2b};
        
        /// <summary> Minimum number of expected fields</summary>
		private const int MIN_FIELD_COUNT = 8;
        
        /// <summary> Logging object</summary>
		private Logger log;
        
        /// <summary> Default constructor</summary>
		public UnixFileParser()
		{
            log = Logger.GetLogger(typeof(UnixFileParser));
        }
		
		/// <summary> 
		/// Parse server supplied string, e.g.:
		/// 
		/// lrwxrwxrwx   1 wuftpd   wuftpd         14 Jul 22  2002 MIRRORS -> README-MIRRORS
		/// -rw-r--r--   1 b173771  users         431 Mar 31 20:04 .htaccess
		/// 
		/// </summary>
		/// <param name="raw">  raw string to parse
		/// </param>
		public override FTPFile Parse(string raw)
		{		
			// test it is a valid line, e.g. "total 342522" is invalid
			char ch = raw[0];
			if (ch != ORDINARY_FILE_CHAR && ch != DIRECTORY_CHAR && ch != SYMLINK_CHAR)
				return null;
			
			string[] fields = Split(raw);
			
			if (fields.Length < MIN_FIELD_COUNT)
			{
				StringBuilder listing = new StringBuilder("Unexpected number of fields in listing '");
				listing.Append(raw).Append("' - expected minimum ").Append(MIN_FIELD_COUNT).
                        Append(" fields but found ").Append(fields.Length).Append(" fields");
				throw new FormatException(listing.ToString());
			}
			
			// field pos
			int index = 0;
			
			// first field is perms
			string permissions = fields[index++];
			ch = permissions[0];
			bool isDir = false;
			bool isLink = false;
			if (ch == DIRECTORY_CHAR)
				isDir = true;
			else if (ch == SYMLINK_CHAR)
				isLink = true;
			
			// some servers don't supply the link count
			int linkCount = 0;
			if (fields.Length > MIN_FIELD_COUNT)
			{
				try
				{
					linkCount = System.Int32.Parse(fields[index++]);
				}
				catch (FormatException)
				{
				}
			}
			
			// owner and group
			string owner = fields[index++];
			string group = fields[index++];
			
			// size
			long size = 0L;
			string sizeStr = fields[index++];
			try
			{
				size = Int64.Parse(sizeStr);
			}
			catch (FormatException)
			{
				throw new FormatException("Failed to parse size: " + sizeStr);
			}
			
			// next 3 are the date time
			int dateTimePos = index;
			System.DateTime lastModified;
			System.Text.StringBuilder stamp = new System.Text.StringBuilder(fields[index++]);
			stamp.Append('-').Append(fields[index++]).Append('-');
			
			string field = fields[index++];
			if (field.IndexOf((System.Char) ':') < 0)
			{
				stamp.Append(field); // year
				lastModified = DateTime.ParseExact(stamp.ToString(), format1, 
                    CultureInfo.CurrentCulture.DateTimeFormat, DateTimeStyles.None);
			}
			else
			{
				// add the year ourselves as not present
                int year = CultureInfo.CurrentCulture.Calendar.GetYear(DateTime.Now);
				stamp.Append(year).Append('-').Append(field);
				lastModified = DateTime.ParseExact(stamp.ToString(), format2, 
                    CultureInfo.CurrentCulture.DateTimeFormat, DateTimeStyles.None);
				
				// can't be in the future - must be the previous year
				if (lastModified > DateTime.Now)
				{
                    lastModified.AddYears(-1);
				}
			}
			
			// name of file or dir. Extract symlink if possible
			string name = null;
			string linkedname = null;
			
			// we've got to find the starting point of the name. We
			// do this by finding the pos of all the date/time fields, then
			// the name - to ensure we don't get tricked up by a userid the
			// same as the filename,for example
			int pos = 0;
			bool ok = true;
			for (int i = dateTimePos; i < dateTimePos + 3; i++)
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
                string remainder = raw.Substring(pos).Trim();
				if (!isLink)
					name = remainder;
				else
				{
					// symlink, try to extract it
					pos = remainder.IndexOf(SYMLINK_ARROW);
					if (pos <= 0)
					{
						// couldn't find symlink, give up & just assign as name
						name = remainder;
					}
					else
					{
						int len = SYMLINK_ARROW.Length;
						name = remainder.Substring(0, (pos) - (0)).Trim();
						if (pos + len < remainder.Length)
							linkedname = remainder.Substring(pos + len);
					}
				}
			}
			else
			{
				throw new FormatException("Failed to retrieve name: " + raw);
			}
			
			FTPFile file = new FTPFile(FTPFile.UNIX, raw, name, size, isDir, ref lastModified);
			file.Group = group;
			file.Owner = owner;
			file.Link = isLink;
			file.LinkCount = linkCount;
			file.LinkedName = linkedname;
			file.Permissions = permissions;
			return file;
		}
	}
}