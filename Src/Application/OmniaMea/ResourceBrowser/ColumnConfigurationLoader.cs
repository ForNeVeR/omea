// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea
{
	/// <summary>
	/// Loads display column configuration from XML files.
	/// </summary>
	internal class ColumnConfigurationLoader
	{
        internal static void LoadXmlConfiguration( Assembly pluginAssembly, XmlNode node )
        {
            foreach( XmlNode childNode in node.SelectNodes( "columns" ) )
            {
                string resourceType = XmlTools.GetRequiredAttribute( childNode, "type" );
                LoadColumns( resourceType, childNode );
            }
        }

	    private static void LoadColumns( string resourceType, XmlNode node )
	    {
            int index = 0;
            foreach( XmlNode columnNode in node.SelectNodes( "column" ) )
            {
                LoadColumn( resourceType, index, columnNode );
                index++;
            }
	    }

	    private static void LoadColumn( string resourceType, int index, XmlNode node )
	    {
            int width = XmlTools.GetRequiredIntAttribute( node, "width" );
            XmlNodeList propNameNodes = node.SelectNodes( "prop" );

            ArrayList propNameList = new ArrayList();
            for( int i=0; i<propNameNodes.Count; i++ )
            {
                string propName = XmlTools.GetRequiredAttribute( propNameNodes [i], "name" );
                if ( XmlTools.GetIntAttribute( propNameNodes [i], "ifExist", 0 ) == 1 )
                {
                    string propRealName = propName;
                    if ( propRealName.StartsWith( "-" ) )
                    {
                        propRealName = propRealName.Substring( 1 );
                    }
                    if ( !Core.ResourceStore.PropTypes.Exist( propRealName ) )
                    {
                        continue;
                    }
                }
                propNameList.Add( propName );
            }
            string[] propNames = (string[]) propNameList.ToArray( typeof (string) );

	        ColumnDescriptorFlags flags = 0;
            if ( XmlTools.GetIntAttribute( node, "fixedSize", 0 ) == 1 )
            {
                flags |= ColumnDescriptorFlags.FixedSize;
            }
            if ( XmlTools.GetIntAttribute( node, "showIfNotEmpty", 0 ) == 1 )
            {
                flags |= ColumnDescriptorFlags.ShowIfNotEmpty;
            }
            if ( XmlTools.GetIntAttribute( node, "showIfDistinct", 0 ) == 1 )
            {
                flags |= ColumnDescriptorFlags.ShowIfDistinct;
            }
            if ( XmlTools.GetIntAttribute( node, "autoSize", 0 ) == 1 )
            {
                flags |= ColumnDescriptorFlags.AutoSize;
            }

            ColumnDescriptor descriptor = new ColumnDescriptor( propNames, width, flags );

            XmlNode comparerNode = node.SelectSingleNode( "comparer" );
            if ( comparerNode != null )
            {
                IResourceComparer comparer = LoadComparer( comparerNode );
                descriptor.CustomComparer = comparer;
                if ( comparer is IResourceGroupProvider )
                {
                    descriptor.GroupProvider = comparer as IResourceGroupProvider;
                }
            }

            XmlNode sortMenuTextNode = node.SelectSingleNode( "sortmenutext" );
            if ( sortMenuTextNode != null )
            {
                descriptor.SortMenuAscText = XmlTools.GetRequiredAttribute( sortMenuTextNode, "asc" );
                descriptor.SortMenuDescText = XmlTools.GetRequiredAttribute( sortMenuTextNode, "desc" );
            }

	        Core.DisplayColumnManager.RegisterDisplayColumn( resourceType, index, descriptor );

            XmlNode multiLineNode = node.SelectSingleNode( "multiline" );
            if ( multiLineNode != null )
            {
                LoadMultiLineColumn( resourceType, propNames, multiLineNode );
            }
	    }

	    private static void LoadMultiLineColumn( string resourceType, string[] propNames, XmlNode node )
	    {
	        int row = XmlTools.GetRequiredIntAttribute( node, "row" );
            int endRow = XmlTools.GetIntAttribute( node, "endRow", -1 );
            if ( endRow == -1 )
            {
                endRow = row;
            }
            int startX = XmlTools.GetRequiredIntAttribute( node, "startX" );
            int width = XmlTools.GetRequiredIntAttribute( node, "width" );

            MultiLineColumnFlags flags = 0;

            string anchor = XmlTools.GetRequiredAttribute( node, "anchor" );
            switch( anchor )
            {
                case "left":  flags |= MultiLineColumnFlags.AnchorLeft; break;
                case "right": flags |= MultiLineColumnFlags.AnchorRight; break;
                case "both":  flags |= MultiLineColumnFlags.AnchorLeft | MultiLineColumnFlags.AnchorRight; break;
                default:
                    throw new Exception( "Invalid column anchor '" + anchor + "'" );
            }

            if ( XmlTools.GetIntAttribute( node, "hideIfNoProp", 0 ) == 1 )
            {
                flags |= MultiLineColumnFlags.HideIfNoProp;
            }

	        Color color = SystemColors.ControlText;
            int r = XmlTools.GetIntAttribute( node, "r", -1 );
            if ( r != -1 )
            {
                int g = XmlTools.GetRequiredIntAttribute( node, "g" );
                int b = XmlTools.GetRequiredIntAttribute( node, "b" );
                color = Color.FromArgb( r, g, b );
            }

	        HorizontalAlignment textAlign = HorizontalAlignment.Left;
            if ( node.Attributes ["align"] != null )
            {
                switch( node.Attributes ["align"].Value )
                {
                    case "left":   textAlign = HorizontalAlignment.Left; break;
                    case "center": textAlign = HorizontalAlignment.Center; break;
                    case "right":  textAlign = HorizontalAlignment.Right; break;
                    default:
                        throw new Exception( "Invalid column alignment '" + anchor + "'" );
                }
            }

            DisplayColumnManager colManager = Core.DisplayColumnManager as DisplayColumnManager;
            Core.DisplayColumnManager.RegisterMultiLineColumn( resourceType,
                colManager.PropNamesToIDs( propNames, true ), row, endRow, startX, width, flags, color, textAlign );
	    }

	    private static IResourceComparer LoadComparer( XmlNode node )
	    {
            string assemblyName = XmlTools.GetRequiredAttribute( node, "assembly" );
            string className = XmlTools.GetRequiredAttribute( node, "class" );

            Assembly[] asmList = AppDomain.CurrentDomain.GetAssemblies();
            foreach( Assembly asm in asmList )
            {
                if ( asm.GetName().Name == assemblyName )
                {
                    Type comparerType = asm.GetType( className );
                    return (IResourceComparer) Activator.CreateInstance( comparerType );
                }
            }
            throw new ActionException( "Could not find action assembly " + assemblyName );
	    }
	}
}
