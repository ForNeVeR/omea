// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Xml;
using System.IO;
using JetBrains.Omea.OpenAPI;
using System.Collections;
using JetBrains.Omea.Diagnostics;

namespace JetBrains.Omea.ResourceTools
{
	public class ResourceSerializer
	{
        private ArrayList _resources = new ArrayList();
        public const string ResourceTransferFileName = "resourcetransfer.xml";

        public ResourceNode AddResource( IResource resource )
        {
            ResourceNode resourceNode = new ResourceNode( resource );
            _resources.Add( resourceNode );
            return resourceNode;
        }
        public long GenerateXML( string fileName )
        {
            XmlDocument document = new XmlDocument();
            XmlNode comment = document.CreateComment("Resource transfer");
            document.AppendChild( comment );
            XmlNode root = document.CreateElement( null, "OmniaMea-Resources", null );
            document.AppendChild( root );
            foreach ( ResourceNode resourceNode in _resources )
            {
                resourceNode.GenerateXML( document, root );
            }
            document.Save( fileName );
            long fileSize = 0;
            try
            {
                fileSize = new FileInfo( fileName ).Length;
            }
            catch ( Exception exception )
            {
                Tracer._TraceException( exception );
            }
            return fileSize;
        }
	}

    public abstract class SerializableNode
    {
        private bool _acceptSending = true;
        public bool AcceptSending
        {
            get { return _acceptSending; }
            set { _acceptSending = value; }
        }
    }
    public class ResourceNode : SerializableNode
    {
        private IResource _parentResource;
        private IResource _resource;
        private ArrayList _links = new ArrayList();
        private ArrayList _properties = new ArrayList();

        public ResourceNode( IResource resource )
        {
            _resource = resource;
            _parentResource = _resource;
        }
        public ResourceNode( IResource parentResource, IResource resource )
        {
            _resource = resource;
            _parentResource = parentResource;
        }

        public IResource Resource { get { return _resource; } }
        public PropertyNode AddProperty( IResourceProperty property )
        {
            PropertyNode propertyNode = new PropertyNode( property );
            _properties.Add( propertyNode );
            return propertyNode;
        }
        public LinkNode AddLink( string displayName, string name, bool directed )
        {
            LinkNode linkNode = new LinkNode( _resource, displayName, name, directed );
            _links.Add( linkNode );
            return linkNode;
        }
        public void GenerateXML( XmlDocument document, XmlNode parent )
        {
            if ( !AcceptSending ) return;
            XmlNode resNode = document.CreateElement( "Resource" );
            LoadProperies( document, resNode );
            LoadLinks( document, resNode );

            IResourceSerializer serializer = ICore.Instance.PluginLoader.GetResourceSerializer( _resource.Type );
            if ( serializer != null )
            {
                serializer.AfterSerialize( _parentResource, _resource, resNode );
            }

            parent.AppendChild( resNode );
        }
        private void LoadLinks( XmlDocument document, XmlNode resNode )
        {
            XmlNode linksNode = document.CreateElement( "Links" );
            resNode.AppendChild( linksNode );
            foreach ( LinkNode linkNode in _links )
            {
                linkNode.GenerateXML( document, linksNode );
            }
        }
        private void LoadProperies( XmlDocument document, XmlNode resNode )
        {
            XmlAttribute attribute = document.CreateAttribute( "Type" );
            attribute.InnerText = _resource.Type;
            resNode.Attributes.Append( attribute );

            XmlNode propertiesNode = document.CreateElement( "Properties" );
            resNode.AppendChild( propertiesNode );
            foreach ( PropertyNode propertyNode in _properties )
            {
                propertyNode.GenerateXML( document, propertiesNode );
            }
        }
    }
    public class PropertyNode : SerializableNode
    {
        private IResourceProperty _property;
        public PropertyNode( IResourceProperty property )
        {
            _property = property;
        }
        public void GenerateXML( XmlDocument document, XmlNode parent )
        {
            if( AcceptSending )
            {
                XmlNode      propertyNode = document.CreateElement( _property.Name );

                string value = string.Empty;
                if ( _property.Value != null )
                {
                    if( _property.DataType == PropDataType.Date )
                    {
                        value = ((DateTime)_property.Value).Ticks.ToString();
                    }
                    else
                    if ( _property.DataType == PropDataType.StringList )
                    {
                        foreach( string str in ((IStringList)_property.Value) )
                        {
                            XmlNode stringNode = document.CreateElement( "StringField" );
                            stringNode.InnerText = str;
                            propertyNode.AppendChild( stringNode );
                        }
                    }
                    else
                        value = _property.Value.ToString();
                }

                XmlAttribute typeAttribute = document.CreateAttribute( "Type" );
                typeAttribute.InnerText = _property.DataType.ToString();
                propertyNode.Attributes.Append( typeAttribute );
                if ( _property.DataType != PropDataType.StringList )
                    propertyNode.InnerText = value;
                parent.AppendChild( propertyNode );
            }
        }
    }

    public class LinkNode : SerializableNode
    {
        private string _displayName;
        private string _internalName;
        private ArrayList _resources = new ArrayList();
        private bool _directed = false;
        private IResource _parentResource;

        public LinkNode( IResource parentResource, string displayName, string internalName, bool directed )
        {
            _displayName = displayName;
            _internalName = internalName;
            _directed = directed;
            _parentResource = parentResource;
        }

        public ResourceNode AddResource( IResource resource )
        {
            ResourceNode resourceNode = new ResourceNode( _parentResource, resource );
            _resources.Add( resourceNode );
            return resourceNode;
        }
        public void GenerateXML( XmlDocument document, XmlNode parent )
        {
            if ( !AcceptSending ) return;
            if ( _resources.Count == 0 ) return;

            XmlNode linkNode = document.CreateElement( "Link" );
            XmlAttribute typeAttribute = document.CreateAttribute( "Type" );
            typeAttribute.InnerText = _displayName;
            linkNode.Attributes.Append( typeAttribute );
            XmlAttribute internalNameAttribute = document.CreateAttribute( "InternalName" );
            internalNameAttribute.InnerText = _internalName;
            if ( _directed )
            {
                XmlAttribute directedAttribute = document.CreateAttribute( "Directed" );
                linkNode.Attributes.Append( directedAttribute );
            }
            linkNode.Attributes.Append( internalNameAttribute );
            parent.AppendChild( linkNode );
            foreach ( ResourceNode resourceNode in _resources )
            {
                resourceNode.GenerateXML( document, linkNode );
            }
        }
    }
}
