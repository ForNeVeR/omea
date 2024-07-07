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
// $Log: FTPReply.cs,v $
// Revision 1.4  2004/11/05 20:00:28  bruceb
// cleaned up namespaces
//
// Revision 1.3  2004/10/29 09:41:44  bruceb
// removed /// in file header
//
//
//

using System;

namespace EnterpriseDT.Net.Ftp
{
	/// <summary>  Encapsulates the FTP server reply
	///
	/// </summary>
	/// <author>       Bruce Blackshaw
	/// </author>
	/// <version>      $LastChangedRevision$
	/// </version>
	public class FTPReply
	{
		/// <summary>  Getter for reply code
		///
		/// </summary>
		/// <returns> server's reply code
		/// </returns>
		virtual public string ReplyCode
		{
			get
			{
				return replyCode;
			}

		}
		/// <summary>  Getter for reply text
		///
		/// </summary>
		/// <returns> server's reply text
		/// </returns>
		virtual public string ReplyText
		{
			get
			{
				return replyText;
			}

		}
		/// <summary> Getter for reply data lines
		///
		/// </summary>
		/// <returns> array of data lines returned (if any). Null
		/// if no data lines
		/// </returns>
		virtual public string[] ReplyData
		{
			get
			{
				return data;
			}

		}

		/// <summary>  Reply code</summary>
		private string replyCode;

		/// <summary>  Reply text</summary>
		private string replyText;

		/// <summary> Lines of data returned, e.g. FEAT</summary>
		private string[] data;

		/// <summary>  Constructor. Only to be constructed
		/// by this package, hence package access
		///
		/// </summary>
		/// <param name="replyCode"> the server's reply code
		/// </param>
		/// <param name="replyText"> the server's reply text
		/// </param>
		internal FTPReply(string replyCode, string replyText)
		{
			this.replyCode = replyCode;
			this.replyText = replyText;
		}


		/// <summary>  Constructor. Only to be constructed
		/// by this package, hence package access
		///
		/// </summary>
		/// <param name="replyCode"> the server's reply code
		/// </param>
		/// <param name="replyText"> the server's full reply text
		/// </param>
		/// <param name="data">      data lines contained in reply text
		/// </param>
		internal FTPReply(string replyCode, string replyText, string[] data)
		{
			this.replyCode = replyCode;
			this.replyText = replyText;
			this.data = data;
		}


		/// <summary>  Constructor. Only to be constructed
		/// by this package, hence package access
		///
		/// </summary>
		/// <param name="rawReply"> the server's raw reply
		/// </param>
		internal FTPReply(string rawReply)
		{
			// all reply codes are 3 chars long
			rawReply = rawReply.Trim();
			replyCode = rawReply.Substring(0, (3) - (0));
			if (rawReply.Length > 3)
				replyText = rawReply.Substring(4);
			else
				replyText = "";
		}
	}
}
