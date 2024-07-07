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
// $Log: FTPFile.cs,v $
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

namespace EnterpriseDT.Net.Ftp
{
	/// <summary>
	/// Represents a remote file (implementation)
	/// </summary>
	/// <author>
	/// Bruce Blackshaw
	/// </author>
	/// <version>
	/// $LastChangedRevision$
	/// </version>
	public class FTPFile
	{
		/// <summary>
		/// Get the type of file, eg UNIX
		/// </summary>
		/// <returns> the integer type of the file
		/// </returns>
		virtual public int Type
		{
			get
			{
				return type;
			}
		}

		/// <returns>
		/// Returns the name.
		/// </returns>
		virtual public string Name
		{
			get
			{
				return name;
			}

		}
		/// <returns>
		/// Returns the raw server string.
		/// </returns>
		virtual public string Raw
		{
			get
			{
				return raw;
			}

		}

        /// <returns>
        /// Returns or sets the number of links to the file
		/// </returns>
		virtual public int LinkCount
		{
			get
			{
				return linkCount;
			}

			set
			{
				this.linkCount = value;
			}
		}

        /// <returns>
        /// Is this file a link
		/// </returns>
		virtual public bool Link
		{
			get
			{
				return isLink;
			}

			set
			{
				this.isLink = value;
			}
		}


		/// <returns>
		/// Returns the linked name.
		/// </returns>
		virtual public string LinkedName
		{
			get
			{
				return linkedname;
			}
			set
			{
				this.linkedname = value;
			}
		}


		/// <returns>
		/// Gets or sets the group.
		/// </returns>
		virtual public string Group
		{
			get
			{
				return group;
			}
			set
			{
				this.group = value;
			}
		}

		/// <returns>
		/// Gets or sets the owner.
		/// </returns>
		virtual public string Owner
		{
			get
			{
				return owner;
			}
			set
			{
				this.owner = value;
			}
		}


        /// <returns>
		/// Gets or sets whether this is a directory
		/// </returns>
		virtual public bool Dir
		{
			get
			{
				return isDir;
			}
			set
			{
				this.isDir = value;
			}
		}

		/// <returns>
		/// Gets or sets the permissions.
		/// </returns>
		virtual public string Permissions
		{
			get
			{
				return permissions;
			}
			set
			{
				this.permissions = value;
			}
		}

		/// <returns>
		/// Gets last modified timestamp
		/// </returns>
		virtual public DateTime LastModified
		{
			get
			{
				return lastModified;
			}
		}


		/// <returns>
		/// Gets size of file
		/// </returns>
		virtual public long Size
		{
			get
			{
				return size;
			}
		}

		/// <summary> Unknown remote server type</summary>
		public const int UNKNOWN = - 1;

		/// <summary> Windows type</summary>
		public const int WINDOWS = 0;

		/// <summary> UNIX type</summary>
		public const int UNIX = 1;

		/// <summary>Date format</summary>
		private static readonly string format = "dd-MM-yyyy HH:mm";

		/// <summary> Type of file</summary>
		private int type;

		/// <summary> Is this file a symbolic link?</summary>
		protected internal bool isLink = false;

		/// <summary> Number of links to file</summary>
		protected internal int linkCount = 1;

		/// <summary> Permission bits string</summary>
		protected internal string permissions;

		/// <summary> Is this a directory?</summary>
		protected internal bool isDir = false;

		/// <summary> Size of file</summary>
		protected internal long size = 0L;

		/// <summary> File/dir name</summary>
		protected internal string name;

		/// <summary> Name of file this is linked to</summary>
		protected internal string linkedname;

		/// <summary> Owner if known</summary>
		protected internal string owner;

		/// <summary> Group if known</summary>
		protected internal string group;

		/// <summary> Last modified</summary>
		protected internal System.DateTime lastModified;

		/// <summary> Raw string</summary>
		protected internal string raw;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">
		/// type of file
		/// </param>
		/// <param name="raw">
		/// raw string returned from server
		/// </param>
		/// <param name="name">
		/// name of file
		/// </param>
		/// <param name="size">
		/// size of file
		/// </param>
		/// <param name="isDir">
		/// true if a directory
		/// </param>
		/// <param name="lastModified">
		/// last modified timestamp
		/// </param>
		internal FTPFile(int type, string raw, string name, long size,
                         bool isDir, ref DateTime lastModified)
		{
			this.type = type;
			this.raw = raw;
			this.name = name;
			this.size = size;
			this.isDir = isDir;
			this.lastModified = lastModified;
		}

		/// <returns>
		/// string representation
		/// </returns>
		public override string ToString()
		{
			StringBuilder buf = new StringBuilder(raw);
			buf.Append("[").Append("Name=").Append(name).Append(",").Append("Size=").
                Append(size).Append(",").Append("Permissions=").Append(permissions).
                Append(",").Append("Owner=").Append(owner).Append(",").
                Append("Group=").Append(group).Append(",").Append("Is link=").Append(isLink).
                Append(",").Append("Link count=").Append(linkCount).Append(",").
                Append("Is dir=").Append(isDir).Append(",").
                Append("Linked name=").Append(linkedname).Append(",").
                Append("Permissions=").Append(permissions).Append(",").
                Append("Last modified=").Append(lastModified.ToString(format, CultureInfo.CurrentCulture.DateTimeFormat)).
                Append("]");
			return buf.ToString();
		}
	}
}
