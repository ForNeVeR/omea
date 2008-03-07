/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace OmniaMea.RemoteControl
{
	internal interface IOutputFormatter
	{
		void startResult();
		void startException( int code, string message );
		byte[] finishOutput();

		void addValue(string name, object val);

		string getContentType();
	}

	internal delegate IOutputFormatter CreateFormatter();

	internal class XmlOutputFormatter : IOutputFormatter
	{
		private static Type _typeString = Type.GetType( "System.String" );
		private static Type _typeInt    = Type.GetType( "System.Int32" );
		private static Type _typeBool   = Type.GetType( "System.Boolean" );
		private static Type _typeVoid   = Type.GetType( "System.Void" );

		private XmlDocument _output = null;
		private XmlElement  _root = null;

		public static IOutputFormatter CreateFormatter()
		{
			return new XmlOutputFormatter();
		}

		private void startDocument(string status)
		{
			_output = new XmlDocument();
			_root = _output.CreateElement( "result", "" );
			_root.SetAttribute( "status", status );
			_output.AppendChild( _root  );
		}

		private void addValue( XmlElement root,  string name, object obj )
		{
			XmlElement xval;
			
			if( null == obj || _typeVoid == obj.GetType() )
			{
				xval = _output.CreateElement( "void", "" );
			}
			else
			{
				Type objectType = obj.GetType();
				if( _typeString == objectType )
				{
					xval = _output.CreateElement( "string", "" );
					xval.AppendChild( _output.CreateTextNode( obj as string ) );
				}
				else if( _typeInt == objectType )
				{
					xval = _output.CreateElement( "int", "" );
					xval.AppendChild( _output.CreateTextNode( ((int)obj).ToString() ) );
				}
				else if( _typeBool == objectType )
				{
					xval = _output.CreateElement( "bool", "" );
					xval.AppendChild( _output.CreateTextNode( ( ((bool)obj) ? "1" : "0" ) ) );
				}
				else if( objectType.IsArray )
				{
					xval = _output.CreateElement( "array", "" );
					Array array = obj as Array;
					foreach(object o in array)
					{
						addValue( xval, null, o );
					}
				}
				else if( objectType.IsValueType && !objectType.IsPrimitive )
				{
					MemberInfo[] fields = objectType.GetMembers();
					xval = _output.CreateElement( "struct", "" );
					foreach ( MemberInfo mi in fields )
					{
						if( mi.MemberType != MemberTypes.Field )
							continue;
						FieldInfo field = mi as FieldInfo;
						addValue( xval, field.Name,  field.GetValue( obj ) );
					}
				}
				else
				{
					throw new Exception( "Invalid result type" );
				}
			}
			if( name != null && name.Length > 0 )
			{
				xval.SetAttribute( "name", name );
			}
			root.AppendChild( xval );
		}

		public void startResult()
		{
			startDocument( "ok" );
		}

		public void startException( int code, string message )
		{
			startDocument( "exception" );
			XmlElement xval = _output.CreateElement( "struct", "" );
			xval.SetAttribute( "name", "exception" );
			addValue( xval, "code", code );
			addValue( xval, "message", message );
			_root.AppendChild( xval );
		}

		public byte[] finishOutput()
		{
			MemoryStream str = new MemoryStream();
			_output.Save( str );
			_output = null;
			_root = null;
			str.Capacity = (int)str.Length;
			return str.GetBuffer();
		}

		public void addValue( string name, object val )
		{
			addValue( _root, name, val );
		}

		public string getContentType() { return "text/xml"; }
	}
}
