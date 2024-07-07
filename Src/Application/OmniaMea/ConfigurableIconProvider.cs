// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;
using System.Reflection;
using System.Xml;
using JetBrains.Omea.Base;
using System.Collections;
using System.IO;
using System.Drawing;

namespace JetBrains.Omea
{
	/**
     * IResourceIconProvider implementation based on an XML configuration
     * section.
     */

    internal class ConfigurableIconProvider: IResourceIconProvider, IOverlayIconProvider
	{
        private class ResourceIconCondition
        {
            private enum CondType { HasProp, PropValue, HasInLink };
            private CondType _type;
            private string _propName;
            private string _propValue;

            internal ResourceIconCondition( XmlNode node )
            {
                if ( node.Name == "prop" )
                {
                	_type = CondType.PropValue;
                }
                else if ( node.Name == "hasprop" )
                {
                	_type = CondType.HasProp;
                }
                else if ( node.Name == "hasinlink" )
                {
                	_type = CondType.HasInLink;
                }
                else
                {
                	throw new Exception( "Invalid icon condition " + node.Name );
                }

                _propName = XmlTools.GetRequiredAttribute( node, "name" );
                if ( _type == CondType.PropValue )
                {
                	_propValue = XmlTools.GetRequiredAttribute( node, "value" );
                }
            }

            internal bool MatchResource( IResource res )
            {
            	switch( _type )
            	{
            		case CondType.PropValue: return res.GetPropText( _propName ) == _propValue;
                    case CondType.HasProp:   return res.HasProp( _propName );
                    case CondType.HasInLink: return res.GetLinksTo( null, _propName ).Count > 0;
                    default: return false;
            	}
            }
        }

        private class ResourceIconInstance
        {
            internal string _name;
            internal bool _isDefault;
            internal ArrayList _conditions = new ArrayList();

            internal ResourceIconInstance( XmlNode node )
            {
                _name = XmlTools.GetRequiredAttribute( node, "name" );
                if ( XmlTools.GetIntAttribute( node, "default", 0 ) == 1 )
                {
                	_isDefault = true;
                }
                foreach( XmlNode condNode in node.ChildNodes )
                {
                	_conditions.Add( new ResourceIconCondition( condNode ) );
                }
            }

            internal bool MatchResource( IResource res )
            {
            	for( int i=0; i<_conditions.Count; i++ )
            	{
            		ResourceIconCondition cond = (ResourceIconCondition) _conditions [i];
                    if ( !cond.MatchResource( res ) )
                        return false;
            	}
                return true;
            }
        }

        private class ResourceTypeIconConfiguration
        {
            internal string _resType;
            private ArrayList _condIconInstances = new ArrayList();
            private ArrayList _overlayIconInstances = new ArrayList();
            private ResourceIconInstance _uncondIconInstance;
            private ResourceIconInstance _defaultIconInstance;

            internal ResourceTypeIconConfiguration( XmlNode node )
            {
                _resType = XmlTools.GetRequiredAttribute( node, "type" );
                foreach( XmlNode iconNode in node.SelectNodes( "icon" ) )
                {
                	ResourceIconInstance iconInstance = new ResourceIconInstance( iconNode );
                    AddIconInstance( iconInstance );
                }
                foreach( XmlNode overlayNode in node.SelectNodes( "overlay" ) )
                {
                    ResourceIconInstance iconInstance = new ResourceIconInstance( overlayNode );
                    _overlayIconInstances.Add( iconInstance );
                }
            }

            internal void AddIconInstance( ResourceIconInstance iconInstance )
            {
                if ( iconInstance._conditions.Count == 0 )
                {
                    if ( _uncondIconInstance != null )
                    {
                        throw new Exception( "There can be only one icon with no conditions for a resource type" );
                    }
                    _uncondIconInstance = iconInstance;
                }
                else
                {
                    _condIconInstances.Add( iconInstance );
                }
                if ( iconInstance._isDefault )
                {
                    if ( _defaultIconInstance != null )
                    {
                        throw new Exception( "There can be only one default icon for a resource type" );
                    }
                    _defaultIconInstance = iconInstance;
                }
            }

            internal string GetResourceIconName( IResource res )
            {
            	for( int i=0; i<_condIconInstances.Count; i++ )
            	{
                    ResourceIconInstance instance = (ResourceIconInstance) _condIconInstances [i];
                    if ( instance.MatchResource( res ) )
                    {
                        return instance._name;
                    }
            	}
                if ( _uncondIconInstance != null )
                {
                	return _uncondIconInstance._name;
                }
                return null;
            }

            internal string GetDefaultIconName()
            {
            	if ( _defaultIconInstance != null )
            	{
            		return _defaultIconInstance._name;
            	}
                if ( _uncondIconInstance != null )
                {
                	return _uncondIconInstance._name;
                }
                return null;
            }

            internal string[] GetOverlayIconNames( IResource res )
            {
                ArrayList result = null;
                for( int i=0; i<_overlayIconInstances.Count; i++ )
                {
                    ResourceIconInstance instance = (ResourceIconInstance) _overlayIconInstances [i];
                    if ( instance.MatchResource( res ) )
                    {
                        if ( result == null )
                        {
                            result = new ArrayList();
                        }
                        result.Add( instance._name );
                    }
                }
                if ( result != null )
                {
                    return (string[]) result.ToArray( typeof (string) );
                }
                return null;
            }
        }

        private Assembly _pluginAssembly;
        private string _defaultNamespace;
        private Hashtable _resourceTypeMap = new Hashtable();  // resource type (string) -> ResourceTypeIconConfiguration
        private Hashtable _iconMap = new Hashtable();          // icon name -> icon

        public ConfigurableIconProvider( Assembly pluginAssembly, XmlNode node )
		{
            _pluginAssembly = pluginAssembly;
            XmlAttribute attrNS = node.Attributes ["namespace"];
            _defaultNamespace = (attrNS == null) ? "" : attrNS.Value;

            foreach( XmlNode typeNode in node.SelectNodes( "icons" ) )
            {
                ResourceTypeIconConfiguration cfg = new ResourceTypeIconConfiguration( typeNode );
                _resourceTypeMap [cfg._resType] = cfg;
            }

            // shortcut for resource types which have only one icon
            foreach( XmlNode iconNode in node.SelectNodes( "icon" ) )
            {
                ResourceTypeIconConfiguration cfg = new ResourceTypeIconConfiguration( iconNode );
                ResourceIconInstance instance = new ResourceIconInstance( iconNode );
                cfg.AddIconInstance( instance );
                _resourceTypeMap [cfg._resType] = cfg;
            }
		}

        public Icon GetResourceIcon( IResource res )
        {
        	ResourceTypeIconConfiguration cfg = (ResourceTypeIconConfiguration) _resourceTypeMap [res.Type];
            if ( cfg == null )
                return null;

            string iconName = cfg.GetResourceIconName( res );
            return LoadIconIfNew( iconName );
        }

        public Icon GetDefaultIcon( string resType )
        {
            ResourceTypeIconConfiguration cfg = (ResourceTypeIconConfiguration) _resourceTypeMap [resType];
            if ( cfg == null )
                return null;

            string iconName = cfg.GetDefaultIconName();
            return LoadIconIfNew( iconName );
        }

        public Icon[] GetOverlayIcons( IResource res )
        {
            ResourceTypeIconConfiguration cfg = (ResourceTypeIconConfiguration) _resourceTypeMap [res.Type];
            if ( cfg == null )
                return null;

            string[] overlayIconNames = cfg.GetOverlayIconNames( res );
            if ( overlayIconNames == null || overlayIconNames.Length == 0 )
            {
                return null;
            }

            Icon[] overlayIcons = new Icon [overlayIconNames.Length];
            for( int i=0; i<overlayIconNames.Length; i++ )
            {
                overlayIcons [i] = LoadIconIfNew( overlayIconNames [i] );
            }
            return overlayIcons;
        }

        private Icon LoadIconIfNew( string iconName )
        {
            if ( iconName == null )
            {
                return null;
            }

            Icon icon = (Icon) _iconMap [iconName];
            if ( icon != null )
            {
                return icon;
            }

            string streamName = (_defaultNamespace != "") ? _defaultNamespace + "." + iconName : iconName;
            Stream iconStream = _pluginAssembly.GetManifestResourceStream( streamName );
            if ( iconStream == null )
            {
                throw new Exception( "Failed to load icon stream " + streamName );
            }
            icon = new Icon( iconStream );
            _iconMap [iconName] = icon;
            return icon;
        }

        public string[] ResourceTypes
        {
        	get
        	{
        		string[] result = new string[ _resourceTypeMap.Count ];
                int index = 0;
                foreach( DictionaryEntry de in _resourceTypeMap )
                {
                	result [index++] = (string) de.Key;
                }
                return result;
        	}
        }
	}
}
