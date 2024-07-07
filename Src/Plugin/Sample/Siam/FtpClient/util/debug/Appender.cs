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
// $Log: Appender.cs,v $
// Revision 1.4  2004/11/06 11:15:24  bruceb
// namespace tidying up
//
// Revision 1.3  2004/10/29 09:42:30  bruceb
// removed /// from file headers
//
//

using System;

namespace EnterpriseDT.Util.Debug
{
	/// <summary>  Interface for classes that output log
	/// statements
	///
	/// </summary>
	/// <author>       Bruce Blackshaw
	/// </author>
	/// <version>      $LastChangedRevision$
	/// </version>
	public interface Appender
	{
		/// <summary> Close this appender</summary>
		void Close();

		/// <summary> Log a message
		///
		/// </summary>
		/// <param name="msg"> message to log
		/// </param>
		void Log(string msg);

		/// <summary>
		/// Log a stack trace
		/// </summary>
		/// <param name="t"> throwable object
		/// </param>
		void Log(Exception t);
	}
}
