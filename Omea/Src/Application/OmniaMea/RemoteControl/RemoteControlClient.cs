// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Web;
using System.Xml;

namespace JetBrains.Omea.RemoteControl
{
    public class RemoteControlClient
    {
        public class RemoteException : Exception
        {
            private string _message;
            private int _code;

            internal int code
            {
                get { return _code; }
            }

            internal string message
            {
                get { return _message; }
            }

            internal RemoteException( int aCode, string aMessage )
            {
                _message = aMessage;
                _code = aCode;
            }
        }

        private static Type _typeString = null;
        private static Type _typeInt = null;
        private static Type _typeBool = null;

        private const string _protocolURI = "http://127.0.0.1:{0}/{1}/xml/";
        private const string _contentType = "application/x-www-form-urlencoded";
        private int _port;
        private string _protectionKey = null;

        public RemoteControlClient( int port, string protectionKey )
        {
            _typeString = Type.GetType( "System.String" );
            _typeInt = Type.GetType( "System.Int32" );
            _typeBool = Type.GetType( "System.Boolean" );

            _port = port;
            _protectionKey = protectionKey;
        }

        public object SendRequest( string command, params object[] parameters )
        {
            object result = null;
            HttpWebRequest req = null;
            HttpWebResponse rsp = null;
            string content = "";

            // Marshal parameters
            if ( parameters.Length % 2 != 0 )
            {
                throw new ArgumentException( "Odd number of parameters is not allowed" );
            }
            for ( int p = 0; p < parameters.Length; p += 2 )
            {
                string name;
                try
                {
                    name = (string) parameters[p];
                }
                catch
                {
                    throw new ArgumentException( "Invalid parameter name at position " + p.ToString() );
                }
                string val = MarshalObject( name, parameters[p + 1] );
                if ( content.Length != 0 && val.Length != 0 )
                {
                    content += "&";
                }
                content += val;
            }
            byte[] reqb = Encoding.UTF8.GetBytes( content );
            // Ready
            try
            {
                req = WebRequest.Create( String.Format( _protocolURI, _port, _protectionKey ) + command ) as HttpWebRequest;
                req.Method = "POST";
                req.ContentType = _contentType;
                req.ContentLength = content.Length;
                req.SendChunked = false;
                Stream reqs = req.GetRequestStream();
                reqs.Write( reqb, 0, reqb.Length );
                reqs.Close();
                rsp = req.GetResponse() as HttpWebResponse;
            }
            catch (Exception ex)
            {
                throw new Exception( "HTTP request failed: " + ex.Message );
            }
            // Ok, response are here
            try
            {
                if ( rsp.StatusCode != HttpStatusCode.OK )
                    throw new RemoteException( (int) rsp.StatusCode, rsp.StatusDescription );
            }
            catch (WebException ex)
            {
                throw new RemoteException( 500, ex.Message );
            }
            // Ok, response is here
            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load( new StreamReader( rsp.GetResponseStream(), Encoding.UTF8 ) );
            }
            catch
            {
                throw new RemoteException( 500, "Can not parse XML" );
            }
            // Ok, answer is loaded!
            // Check root element
            XmlElement elem = null;
            foreach ( XmlNode n in xmlDoc.ChildNodes )
            {
                if ( n.NodeType == XmlNodeType.Element )
                {
                    if ( n.LocalName != "result" )
                    {
                        throw new RemoteException( 500, "Invalid root element in answer XML" );
                    }
                    elem = n as XmlElement;
                    if ( ! elem.HasAttribute( "status" ) )
                    {
                        throw new RemoteException( 500, "Root element has not 'status' in answer XML" );
                    }
                    break;
                }

            }
            if ( null == elem )
                throw new RemoteException( 500, "Can not find root element in answer XML" );
            // find retval object in any case :)
            XmlElement retval = null;
            foreach ( XmlNode n in elem.ChildNodes )
            {
                if ( n.NodeType == XmlNodeType.Element )
                {
                    retval = n as XmlElement;
                    if ( ! retval.HasAttribute( "name" ) || retval.GetAttribute( "name" ) != "retval" )
                    {
                        throw new RemoteException( 500, "Can not find retval in answer XML." );
                    }
                    break;
                }
            }
            string objname;
            result = DemarshalObject( retval, out objname );
            // Ok, check status
            if ( elem.GetAttribute( "status" ) == "ok" )
            {
                return result;
            }
            else if ( elem.GetAttribute( "status" ) == "exception" )
            {
                Hashtable e = result as Hashtable;
                throw new RemoteException( (int) e ["code"], e ["message"] as string );
            }
            throw new RemoteException( 500, "Unknown 'status' in answer XML: '" + elem.GetAttribute( "status" ) + "'" );
        }

        private object DemarshalObject( XmlElement e, out string name )
        {
            if ( e.HasAttribute( "name" ) )
            {
                name = e.GetAttribute( "name" );
            }
            else
            {
                name = "";
            }

            if ( e.LocalName == "string" )
            {
                return e.InnerText;
            }
            else if ( e.LocalName == "int" )
            {
                int r = 0;
                try
                {
                    r = Int32.Parse( e.InnerText );
                }
                catch
                {
                    throw new RemoteException( 500, "Can not parse integer in answer XML" );
                }
                return r;
            }
            else if ( e.LocalName == "bool" )
            {
                bool b = e.InnerText != "0";
                return b;
            }
            else if ( e.LocalName == "array" )
            {
                ArrayList a = new ArrayList();
                foreach ( XmlNode n in e.ChildNodes )
                {
                    if ( n.NodeType == XmlNodeType.Element )
                    {
                        string nm;
                        a.Add( DemarshalObject( n as XmlElement, out nm ) );
                    }
                }
                return a;
            }
            else if ( e.LocalName == "struct" )
            {
                Hashtable h = new Hashtable();
                foreach ( XmlNode n in e.ChildNodes )
                {
                    if ( n.NodeType == XmlNodeType.Element )
                    {
                        string nm = "";
                        object val = DemarshalObject( n as XmlElement, out nm );
                        if ( nm.Length == 0 )
                        {
                            throw new RemoteException( 500, "Nameless field in structure" );
                        }
                        h.Add( nm, val );
                    }
                }
                return h;
            }
            else if ( e.LocalName == "void" )
            {
                return new object();
            }
            throw new RemoteException( 500, "Unknown type '" + e.LocalName + "' in answer XML" );
        }

        private string MarshalObject( string name, object o )
        {
            if ( null == o )
            {
                throw new ArgumentException( "Null parameter '" + name + "'" );
            }
            Type ot = o.GetType();
            if ( _typeString == ot )
            {
                return name + "=" + HttpUtility.UrlEncode( o as string );
            }
            else if ( _typeInt == ot )
            {
                return name + "=" + ((int) o).ToString();
            }
            else if ( _typeBool == ot )
            {
                return name + "=" + (((bool) o) ? "1" : "0");
            }
            else if ( ot.IsArray )
            {
                throw new ArgumentException( "Array parameter '" + name + "' is not supported" );
            }
            else if ( ot.IsValueType && !ot.IsPrimitive )
            {
                MemberInfo[] fields = ot.GetMembers();
                string retval = "";
                foreach ( MemberInfo mi in fields )
                {
                    if ( mi.MemberType != MemberTypes.Field )
                        continue;
                    FieldInfo field = mi as FieldInfo;
                    string r = MarshalObject( name + "." + field.Name, field.GetValue( o ) );
                    if ( retval.Length != 0 && r.Length != 0 )
                    {
                        retval += "&";
                    }
                    retval += r;
                }
                return retval;
            }
            else
            {
                throw new ArgumentException( "Unsupported parameter '" + name + "': " + ot.ToString() );
            }
        }
    }
}
