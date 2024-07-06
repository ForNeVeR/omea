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
// $Log: FileAppender.cs,v $
// Revision 1.4  2004/11/06 11:15:24  bruceb
// namespace tidying up
//
// Revision 1.3  2004/10/29 09:42:30  bruceb
// removed /// from file headers
//
//
//

using System;
using System.IO;

namespace EnterpriseDT.Util.Debug
{
	/// <summary>
	/// Appends log statements to a file
	/// </summary>
	/// <author>       Bruce Blackshaw
	/// </author>
	/// <version>      $LastChangedRevision$
	/// </version>
	public class FileAppender : Appender
	{

		/// <summary> Destination</summary>
		private StreamWriter log;

		/// <summary> Constructor
		///
		/// </summary>
		/// <param name="file">     file to log to
		/// </param>
		/// <throws>  IOException </throws>
		public FileAppender(string file)
		{
			log = new StreamWriter(file, true);
		}

		/// <summary>
		/// Log a message
		/// </summary>
		/// <param name="msg"> message to log
		/// </param>
		public virtual void Log(string msg)
		{
			log.WriteLine(msg);
		}

		/// <summary>
		/// Log a stack trace
		/// </summary>
		/// <param name="t"> throwable object
		/// </param>
		public virtual void Log(Exception t)
		{
            log.WriteLine(t.StackTrace.ToString());
		}

		/// <summary>
		/// Close this appender
		/// </summary>
		public virtual void Close()
		{
			log.Flush();
			log.Close();
		}
	}
}
