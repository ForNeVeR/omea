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
// $Log: Level.cs,v $
// Revision 1.4  2004/10/29 09:42:30  bruceb
// removed /// from file headers
//
//
//
namespace EnterpriseDT.Util.Debug
{   
	/// <summary>  
	/// Simple debug level class. Uses the same interface (but
	/// not implementation) as log4net, so that the debug
	/// classes could be easily replaced by log4net 
	/// </summary>
	/// <author>       Bruce Blackshaw
	/// </author>
	/// <version>      $LastChangedRevision$
	/// </version>
	public class Level
	{
		internal const int OFF_INT = -1;
		
		private const string OFF_STR = "OFF";
		
		internal const int FATAL_INT = 0;
		
		private const string FATAL_STR = "FATAL";
		
		internal const int ERROR_INT = 1;
		
		private const string ERROR_STR = "ERROR";
		
		internal const int WARN_INT = 2;
		
		private const string WARN_STR = "WARN";
		
		internal const int INFO_INT = 3;
		
		private const string INFO_STR = "INFO";
		
		internal const int DEBUG_INT = 4;
		
		private const string DEBUG_STR = "DEBUG";
		
		internal const int ALL_INT = 10;
		
		private const string ALL_STR = "ALL";
		
		internal const int LEVEL_COUNT = 5;
				
		/// <summary> Off level</summary>
		public static Level OFF = new Level(OFF_INT, OFF_STR);
		
		/// <summary> Fatal level</summary>
		public static Level FATAL = new Level(FATAL_INT, FATAL_STR);
		
		/// <summary> OFF level</summary>
		public static Level ERROR = new Level(ERROR_INT, ERROR_STR);
		
		/// <summary> Warn level</summary>
		public static Level WARN = new Level(WARN_INT, WARN_STR);
		
		/// <summary> Info level</summary>
		public static Level INFO = new Level(INFO_INT, INFO_STR);
		
		/// <summary> Debug level</summary>
		public static Level DEBUG = new Level(DEBUG_INT, DEBUG_STR);
		
		/// <summary> All level</summary>
		public static Level ALL = new Level(ALL_INT, ALL_STR);
		
		/// <summary> The level's integer value</summary>
		private int level = OFF_INT;
		
		/// <summary> The level's string representation</summary>
		private string levelStr;
		
		/// <summary> 
		/// Private constructor so no-one outside the class can
		/// create any more instances
		/// </summary>
		/// <param name="level">    level to set this instance at
		/// </param>
		/// <param name="levelStr">   string representation
		/// </param>
		private Level(int level, string levelStr)
		{
			this.level = level;
			this.levelStr = levelStr;
		}
		
		/// <summary> 
		/// Get integer log level
		/// </summary>
		/// <returns> log level
		/// </returns>
		internal int GetLevel()
		{
			return level;
		}
		
		/// <summary> Is this level greater or equal to the supplied level
		/// 
		/// </summary>
		/// <param name="l">     level to test against
		/// </param>
		/// <returns>  true if greater or equal to, false if less than
		/// </returns>
		internal bool IsGreaterOrEqual(Level l)
		{
			if (this.level >= l.level)
				return true;
			return false;
		}
		
		/// <summary> Get level from supplied string
		/// 
		/// </summary>
		/// <param name="level">level as a string
		/// </param>
		/// <returns> level object or null if not found
		/// </returns>
		internal static Level GetLevel(string level)
		{
			if (OFF.ToString().ToUpper().Equals(level.ToUpper()))
				return OFF;
			if (FATAL.ToString().ToUpper().Equals(level.ToUpper()))
				return FATAL;
			if (ERROR.ToString().ToUpper().Equals(level.ToUpper()))
				return ERROR;
			if (WARN.ToString().ToUpper().Equals(level.ToUpper()))
				return WARN;
			if (INFO.ToString().ToUpper().Equals(level.ToUpper()))
				return INFO;
			if (DEBUG.ToString().ToUpper().Equals(level.ToUpper()))
				return DEBUG;
			if (ALL.ToString().ToUpper().Equals(level.ToUpper()))
				return ALL;
			return null;
		}
		
		/// <summary> String representation
		/// 
		/// </summary>
		/// <returns> string
		/// </returns>
		public override string ToString()
		{
			return levelStr;
		}
	}
}
