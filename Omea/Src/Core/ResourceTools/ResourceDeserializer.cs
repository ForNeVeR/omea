/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Xml;
using System.Collections;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Diagnostics;

namespace JetBrains.Omea.ResourceTools
{
    public class ResourceDeserializer
    {
        private XmlDocument _document = new XmlDocument();
        private XmlNode _root;
        private ArrayList _resources = new ArrayList();

        public ResourceDeserializer( string fileName )
        {
            _document.Load( fileName );
            XmlNode comment = _document.FirstChild;
            if ( comment.InnerText != "Resource transfer" )
            {
                throw new Exception( "Wrong format for transfer resources" );
            }
            _root = comment.NextSibling;
            if ( _root.Name != "OmniaMea-Resources" )
            {
                throw new Exception( "Wrong format for transfer resources" );
            }
        }

        public ArrayList GetSelectedResources()
        {
            XmlNodeList nodes = _root.ChildNodes;
            foreach ( XmlNode node in nodes )
            {
                if ( node.Name == "Resource" )
                {
                    AddResource( node );
                }
            }
            return _resources;
        }

        private void AddResource( XmlNode node )
        {
            ResourceUnpack resourceUnpack = new ResourceUnpack( node, null );
            if ( resourceUnpack.Valid )
            {
                _resources.Add( resourceUnpack );
            }
        }
    }
    public class ResourceUnpack
    {
        private IResource _resource;
        private XmlNode _xmlNode;
        private LinkUnpack _linkUnpack;
        private ArrayList _links = new ArrayList();
        private bool _valid = true;

        public ResourceUnpack( XmlNode node, LinkUnpack linkUnpack )
        {
            _linkUnpack = linkUnpack;
            _xmlNode = node;
            XmlAttribute attribute = (XmlAttribute)node.Attributes.GetNamedItem( "Type" );
            if ( attribute != null )
            {
                _valid = Core.ResourceStore.ResourceTypes.Exist( attribute.InnerText );
                if ( _valid )
                {
                    _resource = Core.ResourceStore.NewResourceTransient( attribute.InnerText );
                    XmlNode propertiesNode = node.FirstChild;
                    foreach( XmlNode propertyNode in propertiesNode.ChildNodes )
                    {
                        SetProperty( propertyNode );
                    }
                    XmlNode linksNode = propertiesNode.NextSibling;
                    foreach( XmlNode link in linksNode.ChildNodes )
                    {
                        AddLink( link );
                    }
                }
            }
        }
        public bool Valid { get { return _valid; } }
        public void AcceptReceiving()
        {
            if ( !_valid ) return;
            IResourceSerializer serializer = Core.PluginLoader.GetResourceSerializer( _resource.Type );
            IResource matchedResource = null;
            if ( serializer != null )
            {
                IResource parentResource = _resource;
                if ( _linkUnpack != null )
                {
                    parentResource = _linkUnpack.ParentResource;
                }
                matchedResource = serializer.AfterDeserialize( parentResource, _resource, _xmlNode );
            }

            if ( matchedResource == null || matchedResource.Id == _resource.Id )
            {
                _resource.EndUpdate();
            }
            if ( matchedResource != null )
            {
                _resource = matchedResource;
            }
            if ( _linkUnpack != null )
            {
                //  In the case of directed link, this is the proper linkage,
                //  in the case of indirected one the actual order is insignificant
                _resource.AddLink( _linkUnpack.InternalName, _linkUnpack.ParentResource );
            }
            Core.WorkspaceManager.AddToActiveWorkspace( _resource );
            if ( Core.TextIndexManager != null && Core.ResourceStore.ResourceTypes[_resource.Type].Flags != ResourceTypeFlags.NoIndex )
            {
                Core.TextIndexManager.QueryIndexing( _resource.Id );
            }
        }

        public ArrayList Links { get { return _links; } }
        private void SetProperty( XmlNode propertyNode )
        {
            XmlAttribute typeAttribute = (XmlAttribute)propertyNode.Attributes.GetNamedItem( "Type" );
            string strType = typeAttribute.InnerText;
            object value = null;
            switch ( strType )
            {
                case "Bool":
                    value = Convert.ToBoolean( propertyNode.InnerText );
                    break;
                case "Date":
                    Tracer._Trace( "Try to parse " + propertyNode.InnerText + " for " + propertyNode.Name );
                    long ticks = Convert.ToInt64( propertyNode.InnerText );
                    DateTime dateTime = new DateTime( ticks );
                    if ( dateTime != DateTime.MinValue )
                    {
                        value = dateTime;
                    }
                    break;
                case "Double":
                    value = Convert.ToDouble( propertyNode.InnerText );
                    break;
                case "Int":
                    value = Convert.ToInt32( propertyNode.InnerText );
                    break;
                case "LongString":
                    value = propertyNode.InnerText;
                    break;
                case "String":
                    value = propertyNode.InnerText;
                    break;
                case "StringList":
                    //  Elements of the string list are stored as separate
                    //  nodes of name "StringField"
                    ArrayList fields = new ArrayList();
                    foreach ( XmlNode node in propertyNode.ChildNodes )
                    {
                        if ( node.Name == "StringField" )
                            fields.Add( node.InnerText );
                    }

                    IStringList val = _resource.GetStringListProp( propertyNode.Name );
                    foreach( string str in fields )
                        val.Add( str );
                    value = val;
                    break;
                default:
                    throw new ArgumentOutOfRangeException( "ResourceDeserialization -- Can not process" + 
                                                           " resource type [" + strType + "]" );
            }
            if ( value != null )
            {
                if ( Core.ResourceStore.PropTypes.Exist( propertyNode.Name ) )
                {
                    //  String list value is set impliit through initialization of
                    //  the property content elements.
                    if( strType != "StringList" )
                        _resource.SetProp( propertyNode.Name, value );
                }
            }
        }
        public IResource Resource { get { return _resource; } }
        private void AddLink( XmlNode node )
        {
            LinkUnpack linkUnpack = new LinkUnpack( node, this );
            if ( linkUnpack.Valid )
            {
                _links.Add( linkUnpack );
            }
        }
    }

    public class LinkUnpack
    {
        private string _displayName;
        private string _internalName;
        private ResourceUnpack _parentResourceUnpack;
        private ArrayList _resources = new ArrayList();
        private bool _directed = false;
        private bool _valid = true;
        public LinkUnpack( XmlNode linkNode, ResourceUnpack parentResourceUnpack )
        {
            _parentResourceUnpack = parentResourceUnpack;
            XmlAttribute typeAttribute = (XmlAttribute)linkNode.Attributes.GetNamedItem( "Type" );
            _displayName = typeAttribute.InnerText;
            XmlAttribute internalNameAttribute = (XmlAttribute)linkNode.Attributes.GetNamedItem( "InternalName" );
            _internalName = internalNameAttribute.InnerText;
            _valid = ( Core.ResourceStore.PropTypes.Exist( _internalName ) );
            if ( _valid )
            {
                XmlAttribute directedAttribute = (XmlAttribute)linkNode.Attributes.GetNamedItem( "Directed" );
                _directed = directedAttribute != null;
                foreach( XmlNode node in linkNode )
                {
                    AddResource( node );
                }
            }
        }
        public bool Valid { get { return _valid; } }
        public IResource ParentResource { get { return _parentResourceUnpack.Resource; } }
        public ArrayList Resources { get { return _resources; } }    
        public string InternalName { get { return _internalName; } }
        public string DisplayName { get { return _displayName; } }
        public bool Directed { get { return _directed; } }
        private ResourceUnpack AddResource( XmlNode node )
        {
            ResourceUnpack resourceUnpack = new ResourceUnpack( node, this );
            _resources.Add( resourceUnpack );
            return resourceUnpack;
        }
    }
}
