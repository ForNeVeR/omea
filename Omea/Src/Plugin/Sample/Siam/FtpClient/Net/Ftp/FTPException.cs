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
// $Log: FTPException.cs,v $
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
    
	/// <summary>  FTP specific exceptions
	/// 
	/// </summary>
	/// <author>      Bruce Blackshaw
	/// </author>
	/// <version>     $LastChangedRevision$
	/// 
	/// </version>
	public class FTPException:ApplicationException
	{
		/// <summary>   Get the reply code if it exists
		/// 
		/// </summary>
		/// <returns>  reply if it exists, -1 otherwise
		/// </returns>
		virtual public int ReplyCode
		{
			get
			{
				return replyCode;
			}
			
		}
				
		/// <summary>  Integer reply code</summary>
		private int replyCode = - 1;
		
		/// <summary>   Constructor. Delegates to super.
		/// 
		/// </summary>
		/// <param name="msg">  Message that the user will be
		/// able to retrieve
		/// </param>
		public FTPException(string msg):base(msg)
		{
		}
		
		/// <summary>  Constructor. Permits setting of reply code
		/// 
		/// </summary>
		/// <param name="msg">       message that the user will be
		/// able to retrieve
		/// </param>
		/// <param name="replyCode"> string form of reply code
		/// </param>
		public FTPException(string msg, string replyCode):base(msg)
		{
			
			// extract reply code if possible
			try
			{
				this.replyCode = System.Int32.Parse(replyCode);
			}
			catch (FormatException)
			{
				this.replyCode = - 1;
			}
		}
		
		/// <summary>  Constructor. Permits setting of reply code
		/// 
		/// </summary>
		/// <param name="reply">    reply object
		/// </param>
		public FTPException(FTPReply reply):base(reply.ReplyText)
		{
			
			// extract reply code if possible
			try
			{
				this.replyCode = System.Int32.Parse(reply.ReplyCode);
			}
			catch (FormatException)
			{
				this.replyCode = - 1;
			}
		}
	}
}