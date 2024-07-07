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
// $Log: Logger.cs,v $
// Revision 1.6  2004/11/13 18:20:52  bruceb
// clear appenders/loggers in shutdown
//
// Revision 1.5  2004/11/06 11:15:24  bruceb
// namespace tidying up
//
// Revision 1.4  2004/10/29 09:42:30  bruceb
// removed /// from file headers
//
//
//

using System;
using System.Globalization;
using System.Collections;
using System.Configuration;

namespace EnterpriseDT.Util.Debug
{
	/// <summary>
	/// Logger class that mimics log4net Logger class
	/// </summary>
	/// <author>       Bruce Blackshaw
	/// </author>
	/// <version>      $LastChangedRevision$
	/// </version>
	public class Logger
	{
		/// <summary>
		/// Set all loggers to this level
		/// </summary>
		public static Level CurrentLevel
		{
			set
			{
				globalLevel = value;
			}
            get
            {
                return globalLevel;
            }
		}

		/// <summary>
		/// Is debug logging enabled?
		/// </summary>
		/// <returns> true if enabled
		/// </returns>
		virtual public bool DebugEnabled
		{
			get
			{
				return IsEnabledFor(Level.DEBUG);
			}

		}
		/// <summary> Is info logging enabled for the supplied level?
		///
		/// </summary>
		/// <returns> true if enabled
		/// </returns>
		virtual public bool InfoEnabled
		{
			get
			{
				return IsEnabledFor(Level.INFO);
			}

		}

		/// <summary> Level of all loggers</summary>
		private static Level globalLevel;

		/// <summary>Date format</summary>
		private static readonly string format = "d MMM yyyy HH:mm:ss.fff";

		/// <summary> Hash of all loggers that exist</summary>
		private static Hashtable loggers = Hashtable.Synchronized(new Hashtable(10));

		/// <summary> Vector of all appenders</summary>
		private static ArrayList appenders = ArrayList.Synchronized(new ArrayList(2));

		/// <summary> Timestamp</summary>
		private DateTime ts;

		/// <summary> Class name for this logger</summary>
		private string clazz;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="clazz">
		/// class this logger is for
		/// </param>
		private Logger(string clazz)
		{
			this.clazz = clazz;
		}


		/// <summary> Get a logger for the supplied class
		///
		/// </summary>
		/// <param name="clazz">   full class name
		/// </param>
		/// <returns>  logger for class
		/// </returns>
		public static Logger GetLogger(System.Type clazz)
		{
			return GetLogger(clazz.FullName);
		}

		/// <summary>
		/// Get a logger for the supplied class
		/// </summary>
		/// <param name="clazz">   full class name
		/// </param>
		/// <returns>  logger for class
		/// </returns>
		public static Logger GetLogger(string clazz)
		{
			Logger logger = (Logger) loggers[clazz];
			if (logger == null)
			{
				logger = new Logger(clazz);
				loggers[clazz] = logger;
			}
			return logger;
		}

		/// <summary>
		/// Add an appender to our list
		/// </summary>
		/// <param name="newAppender">
		/// new appender to add
		/// </param>
		public static void AddAppender(Appender newAppender)
		{
			appenders.Add(newAppender);
		}

		/// <summary> Close and remove all appenders and loggers</summary>
		public static void Shutdown()
		{
			for (int i = 0; i < appenders.Count; i++)
			{
				Appender a = (Appender) appenders[i];
				a.Close();
			}
			loggers.Clear();
			appenders.Clear();
		}

		/// <summary> Log a message
		///
		/// </summary>
		/// <param name="level">    log level
		/// </param>
		/// <param name="message">  message to log
		/// </param>
		/// <param name="t">        throwable object
		/// </param>
		public virtual void Log(Level level, string message, Exception t)
		{
			if (globalLevel.IsGreaterOrEqual(level))
				OurLog(level, message, t);
		}

		/// <summary>
		/// Log a message to our logging system
		/// </summary>
		/// <param name="level">    log level
		/// </param>
		/// <param name="message">  message to log
		/// </param>
		/// <param name="t">        throwable object
		/// </param>
		private void OurLog(Level level, string message, Exception t)
		{
            ts = DateTime.Now;
			string stamp = ts.ToString(format, CultureInfo.CurrentCulture.DateTimeFormat);
			System.Text.StringBuilder buf = new System.Text.StringBuilder(level.ToString());
			buf.Append(" [").Append(clazz).Append("] ").Append(stamp).Append(" : ").Append(message);
			if (appenders.Count == 0)
			{
				// by default to stdout
				System.Console.Out.WriteLine(buf.ToString());
				if (t != null)
				{
                    System.Console.Out.WriteLine(t.StackTrace);
				}
			}
			else
			{
				for (int i = 0; i < appenders.Count; i++)
				{
					Appender a = (Appender) appenders[i];
					a.Log(buf.ToString());
					if (t != null)
					{
						a.Log(t);
					}
				}
			}
		}

		/// <summary> Log an info level message
		///
		/// </summary>
		/// <param name="message">  message to log
		/// </param>
		public virtual void Info(string message)
		{
			Log(Level.INFO, message, null);
		}

		/// <summary> Log an info level message
		///
		/// </summary>
		/// <param name="message">  message to log
		/// </param>
		/// <param name="t">        throwable object
		/// </param>
		public virtual void Info(string message, Exception t)
		{
			Log(Level.INFO, message, t);
		}

		/// <summary> Log a warning level message
		///
		/// </summary>
		/// <param name="message">  message to log
		/// </param>
		public virtual void  Warn(string message)
		{
			Log(Level.WARN, message, null);
		}

		/// <summary> Log a warning level message
		///
		/// </summary>
		/// <param name="message">  message to log
		/// </param>
		/// <param name="t">        throwable object
		/// </param>
		public virtual void  Warn(string message, Exception t)
		{
			Log(Level.WARN, message, t);
		}

		/// <summary> Log an error level message
		///
		/// </summary>
		/// <param name="message">  message to log
		/// </param>
		public virtual void Error(string message)
		{
			Log(Level.ERROR, message, null);
		}

		/// <summary> Log an error level message
		///
		/// </summary>
		/// <param name="message">  message to log
		/// </param>
		/// <param name="t">        throwable object
		/// </param>
		public virtual void Error(string message, Exception t)
		{
			Log(Level.ERROR, message, t);
		}

		/// <summary> Log a fatal level message
		///
		/// </summary>
		/// <param name="message">  message to log
		/// </param>
		public virtual void Fatal(string message)
		{
			Log(Level.FATAL, message, null);
		}

		/// <summary> Log a fatal level message
		///
		/// </summary>
		/// <param name="message">  message to log
		/// </param>
		/// <param name="t">        throwable object
		/// </param>
		public virtual void Fatal(string message, Exception t)
		{
			Log(Level.FATAL, message, t);
		}

		/// <summary> Log a debug level message
		///
		/// </summary>
		/// <param name="message">  message to log
		/// </param>
		public virtual void Debug(string message)
		{
			Log(Level.DEBUG, message, null);
		}


		/// <summary> Log a debug level message
		///
		/// </summary>
		/// <param name="message">  message to log
		/// </param>
		/// <param name="t">        throwable object
		/// </param>
		public virtual void Debug(string message, Exception t)
		{
			Log(Level.DEBUG, message, t);
		}

		/// <summary> Is logging enabled for the supplied level?
		///
		/// </summary>
		/// <param name="level">  level to test for
		/// </param>
		/// <returns> true   if enabled
		/// </returns>
		public virtual bool IsEnabledFor(Level level)
		{
			if (globalLevel.IsGreaterOrEqual(level))
				return true;
			return false;
		}

		/// <summary> Determine the logging level</summary>
		static Logger()
		{
			{
				string level = ConfigurationSettings.AppSettings["edtftp.log.level"];
                if (level != null)
				    globalLevel = Level.GetLevel(level);
                else {
                    globalLevel = Level.OFF;
                    System.Console.Out.WriteLine("WARNING: 'edtftp.log.level' not found - logging switched off");
                }
			}
		}
	}
}
