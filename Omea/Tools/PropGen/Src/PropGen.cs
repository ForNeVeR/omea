﻿/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;

namespace JetBrains.Omea.Tools.PropGen
{
	class PropGen
	{
	    private XmlDocument _doc = new XmlDocument();
	    private StreamWriter _writer;
	    private string _indent;
	    private string _prefix;
	    private string _defaultDataType;
	    private bool _ownerPlugin;

	    /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main( string[] args )
        {
            if ( args.Length != 2 && args.Length != 3 )
            {
                Console.WriteLine( "Usage: PropGen [/interface] <XML schema file> <C# output file>" );
                return;
            }
            if ( args [0] == "/interface" )
            {
                new PropGen().GenerateInterface( args [1], args [2] );
            }
            else
            {
                new PropGen().Generate( args [0], args [1] );
            }
        }

        public void GenerateInterface( string xmlName, string csName )
        {
            _doc.Load( xmlName );
            _writer = new StreamWriter( csName );

			_writer.WriteLine("// DO NOT EDIT!!!");
			_writer.WriteLine("// This file was autogenerated by the PropGen utility from the {0} schema file on {1}.", xmlName, DateTime.Now.ToString("s"));
			_writer.WriteLine();

            //_writer.WriteLine( "using System;" );
			//_writer.WriteLine();

            XmlElement interfaceElement = (XmlElement) _doc.SelectSingleNode( "/props/interface" );
            string nameSpace = interfaceElement.GetAttribute( "namespace" );

            if ( nameSpace != "JetBrains.Omea.OpenAPI" )
            {
                _writer.WriteLine( "using JetBrains.Omea.OpenAPI;" );
            }
            _writer.WriteLine( "" );
            
            if ( nameSpace.Length > 0 )
            {
                _writer.WriteLine( "namespace " + nameSpace );
                _writer.WriteLine( "{" );
            }
            
            _indent = new string( ' ', 4 );
            GenerateXmlDocComment( interfaceElement );

            _writer.WriteLine( "{0}public interface {1}", _indent, interfaceElement.GetAttribute( "name" ) );
            _writer.WriteLine( "{0}{{", _indent );
            _indent = new string( ' ', 8 );
            GenerateInterfaceProperties();
            _indent = new string( ' ', 4 );
            _writer.WriteLine( "{0}}}", _indent );

            _writer.WriteLine( "}" );
            _writer.Close();

        }

        public void Generate( string xmlName, string csName )
        {
            _doc.Load( xmlName );
            _writer = new StreamWriter( csName );

			_writer.WriteLine("// DO NOT EDIT!!!");
			_writer.WriteLine("// This file was autogenerated by the PropGen utility from the {0} schema file on {1}.", xmlName, DateTime.Now.ToString("s"));
			_writer.WriteLine();

			//_writer.WriteLine( "using System;" );
            _writer.WriteLine( "using JetBrains.Omea.OpenAPI;" );
			_writer.WriteLine();

            string nameSpace = _doc.DocumentElement.GetAttribute( "namespace" );
            if ( nameSpace.Length > 0 )
            {
                _writer.WriteLine( "namespace " + nameSpace );
                _writer.WriteLine( "{" );
            }

            string className = _doc.DocumentElement.GetAttribute( "class" );
            if ( className.Length == 0 )
            {
                className = "Props";
            }
            string visibility = GetVisibility( _doc );

            _indent = new string( ' ', 4 );
            GenerateXmlDocComment( _doc.DocumentElement );

            string implements = "";
            XmlElement interfaceNode = (XmlElement) _doc.SelectSingleNode( "/props/interface" );
            if ( interfaceNode != null )
            {
                implements = ": " + interfaceNode.GetAttribute( "name" );
            }

            _writer.WriteLine( "    {0} class {1}{2}", visibility, className, implements );
            _writer.WriteLine( "    {" );

            _prefix = _doc.DocumentElement.GetAttribute( "prefix" );
            if ( _prefix.Length > 0 )
            {
                _prefix += ".";
            }
            _defaultDataType = _doc.DocumentElement.GetAttribute( "defaultDataType" );
            _ownerPlugin = _doc.DocumentElement.GetAttribute( "ownerPlugin" ) == "1";

            _indent = new string( ' ', 8 );
            GenerateClassNameConstants();
            GeneratePropFields();
            _writer.WriteLine( "" );
            GeneratePropProperties();
            _writer.WriteLine( "" );

            if ( _doc.DocumentElement.GetAttribute( "static" ) == "1" )
            {
                string registerArgs = "";
                if ( _ownerPlugin )
                {
                    registerArgs = " IPlugin ownerPlugin ";
                }
                _writer.WriteLine( "{0}{1} static void Register({2})", _indent, GetVisibility( _doc ), registerArgs );
                _writer.WriteLine( _indent + "{" );
                _indent = new string( ' ', 12 );
                _writer.WriteLine( "{0}IResourceStore store = Core.ResourceStore;", _indent );
            }
            else
            {
                string constructorVisibility = _doc.DocumentElement.GetAttribute( "constructor" );
                if ( constructorVisibility.Length == 0 )
                {
                    constructorVisibility = visibility;
                }
                _writer.WriteLine( "{0}{1} {2}( IResourceStore store )", 
                    _indent, constructorVisibility, className );
                _writer.WriteLine( _indent + "{" );
                _indent = new string( ' ', 12 );
            }
            

            GeneratePropTypeRegistration();
            _writer.WriteLine( "" );
            GenerateResourceTypeRegistration();
            GenerateUniqueRestrictions();
            GenerateLinkRestrictions();
            
            _indent = new string( ' ', 8 );

            _writer.WriteLine( _indent + "}" );

            _writer.WriteLine( "    }" );
            if ( _doc.DocumentElement.GetAttribute( "namespace" ).Length > 0 )
            {
                _writer.WriteLine( "}" );
            }

            _writer.Close();
        }

	    private void GenerateXmlDocComment( XmlElement element )
	    {
	        string summary = element.GetAttribute( "summary" );
	        if ( summary.Length > 0 )
	        {
                _writer.WriteLine( "" );
                _writer.WriteLine( "{0}/// <summary>", _indent );
                _writer.WriteLine( "{0}/// {1}", _indent, summary );
                _writer.WriteLine( "{0}/// </summary>", _indent );
	        }
            string since = element.GetAttribute( "since" );
            if ( since.Length > 0 )
            {
                _writer.WriteLine( "{0}/// <since>{1}</since>", _indent, since );
            }
	    }

	    private void GenerateClassNameConstants()
	    {
	        XmlNodeList resourceTypeNodes = _doc.SelectNodes( "/props/resourcetype" );
	        foreach( XmlElement node in resourceTypeNodes )
	        {
	            string typeName = node.GetAttribute( "name" );
                _writer.WriteLine( "{0}internal const string {1}Resource = \"{2}{1}\";",
                    _indent, typeName, _prefix );
            }
            if ( resourceTypeNodes.Count > 0 )
            {
                _writer.WriteLine( "" );
            }
	    }

        private void GeneratePropFields()
        {
            string prefix = GetStatic( _doc );

            foreach( XmlElement node in _doc.SelectNodes( "/props/prop" ) )
            {
                _writer.WriteLine( "{0}private {1}int _prop{2};", _indent, prefix, GetPropName( node ) );
            }
        }

        private void GenerateInterfaceProperties()
        {
            foreach( XmlElement node in _doc.SelectNodes( "/props/prop" ) )
            {
                GenerateXmlDocComment( node );
                _writer.WriteLine( "{0}int {1} {{ get; }}", _indent, GetPropName( node) );
            }
        }

        private void GeneratePropProperties()
        {
            string visibility = GetVisibility( _doc );
            string isStatic = GetStatic( _doc );
            foreach( XmlElement node in _doc.SelectNodes( "/props/prop" ) )
            {
                GenerateXmlDocComment( node );
                _writer.WriteLine( "{0}{1} {2}int {3} {{ get {{ return _prop{3}; }} }}", 
                    _indent, visibility, isStatic, GetPropName( node ) );
            }
        }

        private void GeneratePropTypeRegistration()
        {
            foreach( XmlElement node in _doc.SelectNodes( "/props/prop" ) )
            {
                _writer.Write( "{0}_prop{1} = store.PropTypes.Register( ", _indent, GetPropName( node ) );
                _writer.Write( "\"{0}{1}\", ", _prefix, node.GetAttribute( "name" ) );

                string dataType = node.GetAttribute( "dataType" );
                if ( dataType.Length == 0 )
                {
                    dataType = _defaultDataType;
                }
                _writer.Write( "PropDataType." + dataType );

                string flags = GetFlags( "PropTypeFlags", node );
                if ( flags != "" )
                {
                    _writer.WriteLine( "," );
                    _writer.Write( _indent + "    " +  flags );
                }
                _writer.WriteLine( " );" );

                if ( node.GetAttribute( "internal" ) != "1" )
                {
                    GenerateDisplayNameRegistration( node );
                }
            }
        }

        private void GenerateDisplayNameRegistration( XmlElement node )
        {
            string displayName = node.GetAttribute( "displayName" );
            if ( displayName == "" )
            {
                displayName = SplitCamelCase( node.GetAttribute( "name" ) );
            }
            if ( node.GetAttribute( "directedLink" ) != "1" )
            {
                if ( displayName != node.GetAttribute( "name" ) )
                {
                    _writer.WriteLine( "{0}store.PropTypes.RegisterDisplayName( _prop{1}, \"{2}\" );",
                        _indent, GetPropName( node ), displayName );
                }
            }
            else
            {
                string reverseDisplayName = node.GetAttribute( "reverseDisplayName" );
                if ( reverseDisplayName == "" )
                {
                    reverseDisplayName = SplitCamelCase( node.GetAttribute( "name" ) );
                }
                _writer.WriteLine( "{0}store.PropTypes.RegisterDisplayName( _prop{1}, \"{2}\", \"{3}\" );",
                    _indent, GetPropName( node ), displayName, reverseDisplayName );
            }
            _writer.WriteLine( "" );
        }

        private void GenerateResourceTypeRegistration()
        {
            XmlNodeList resourceTypeNodes = _doc.SelectNodes( "/props/resourcetype" );
            foreach( XmlElement node in resourceTypeNodes )
            {
                string name = node.GetAttribute( "name" );
                string displayName = node.GetAttribute( "displayName" );
                if ( displayName.Length == 0 && node.GetAttribute( "internal" ) != "1" )
                {
                    displayName = SplitCamelCase( name );
                }
                string displayNameTemplate = node.GetAttribute( "dnTemplate" );
                if ( displayNameTemplate.StartsWith( "Core." ) )
                {
                    displayNameTemplate = "Core.ResourceStore.PropTypes [Core.Props." + 
                        displayNameTemplate.Substring( 5 ) + "].Name";
                }
                else
                {
                    displayNameTemplate = "\"" + displayNameTemplate + "\"";
                }
                _writer.Write( "{0}store.ResourceTypes.Register( {1}Resource, \"{2}\", {3}",
                    _indent, name, displayName, displayNameTemplate );
                
                string flags = GetFlags( "ResourceTypeFlags", node );
                if ( flags != "" )
                {
                    _writer.WriteLine( "," );
                    _writer.Write( _indent + "    " + flags );
                }
                else if ( _ownerPlugin )
                {
                    _writer.Write( "ResourceTypeFlags.Normal" );
                }

                if ( _ownerPlugin )
                {
                    _writer.Write( ", ownerPlugin" );                    
                }
                _writer.WriteLine( " );" );
            }
            if ( resourceTypeNodes.Count > 0 )
            {
                _writer.WriteLine( "" );
            }
        }

        private void GenerateLinkRestrictions()
        {
            foreach( XmlElement node in _doc.SelectNodes( "/props/prop/linkRestriction" ) )
            {
                string fromType = GetClassName( node, "fromtype" );
                string propName = GetPropName( (XmlElement) node.ParentNode );
                string toType = GetClassName( node, "totype" );
                if ( toType.Length == 0 )
                {
                    toType = null;
                }
                string minCount = node.GetAttribute( "mincount" );
                if ( minCount.Length == 0 )
                {
                    minCount = "0";
                }
                string maxCount = node.GetAttribute( "maxcount" );
                if ( maxCount.Length == 0 )
                {
                    maxCount = "Int32.MaxValue";
                }

                _writer.WriteLine( "{0}store.RegisterLinkRestriction( {1}, _prop{2}, {3}, {4}, {5} );",
                    _indent, fromType, propName, toType, minCount, maxCount );
            }
        }

        private void GenerateUniqueRestrictions()
        {
            foreach( XmlElement node in _doc.SelectNodes( "/props/prop/unique" ) )
            {
                string propName = GetPropName( (XmlElement) node.ParentNode );
                string className = GetClassName( node, "resourcetype" );
                _writer.WriteLine( "{0}store.RegisterUniqueRestriction( {1}, _prop{2} );",
                    _indent, className, propName );
            }
        }

	    private string GetClassName( XmlElement node, string attrName )
	    {
	        string className = node.GetAttribute( attrName );
	        if ( _doc.SelectSingleNode( "/props/resourcetype[@name='" + className + "']" ) != null )
	        {
	            className = className + "Resource";
	        }
	        else
	        {
	            className = "\"" + className + "\"";
	        }
	        return className;
	    }

	    private static string GetVisibility( XmlDocument doc )
        {
            string visibility = doc.DocumentElement.GetAttribute( "visibility" );
            if ( visibility.Length == 0 )
            {
                visibility = "public";
            }
            return visibility;
        }

        private static string GetStatic( XmlDocument doc )
        {
            if ( doc.DocumentElement.GetAttribute( "static" ) == "1" )
            {
                return "static ";
            }
            return "";
        }

        private static string GetPropName( XmlElement node )
        {
            string propName = node.GetAttribute( "propName" );
            if ( propName.Length == 0 )
            {
                propName = node.GetAttribute( "name" );
            }
            return propName;
        }

	    private static string SplitCamelCase( string s )
	    {
            StringBuilder result = new StringBuilder( s );
            for( int i=result.Length-1; i > 0; i-- )
            {
                if ( Char.IsUpper( result [i] ) )
                {
                    result.Insert( i, ' ' );
                }
            }
            return result.ToString();
	    }

	    private string GetFlags( string prefix, XmlElement node )
	    {
	        ArrayList flags = new ArrayList();
            foreach( XmlAttribute attr in node.Attributes )
            {
                if ( attr.Value == "1" )
                {
                    string attrName = attr.Name;
                    flags.Add( prefix + "." + Char.ToUpper( attrName [0] ) + attrName.Substring( 1 ) );
                }
            }
            string[] flagsArray = (string[]) flags.ToArray( typeof (string) );
            return String.Join( " | ", flagsArray );
	    }
	}
}
