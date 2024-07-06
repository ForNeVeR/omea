// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.GUIControls
{
	/// <summary>
	/// A container for a set of colors which can be loaded from an XML file,
	/// which also provides pen and brush caching.
	/// </summary>
	public class ColorScheme
	{
		private abstract class SchemeElement: IDisposable
		{
            protected Brush _brush;
            protected Pen _pen;

            public abstract Color GetColor();

            public virtual Color GetStartColor()
            {
                return GetColor();
            }

            public virtual Color GetEndColor()
            {
                return GetColor();
            }

		    public abstract Brush GetBrush( Rectangle rc );
            public abstract Pen GetPen();

            public virtual void Dispose()
            {
                if ( _brush != null )
                {
                    _brush.Dispose();
                    _brush = null;
                }

                if ( _pen != null )
                {
                    _pen.Dispose();
                    _pen = null;
                }
            }
		}

        private class SolidColorElement: SchemeElement
        {
            private Color _color;

            public SolidColorElement( Color color )
            {
                _color = color;
            }

            public override Color GetColor()
            {
                return _color;
            }

            public override Brush GetBrush( Rectangle rc )
            {
                if ( _brush == null )
                {
                    _brush = new SolidBrush( _color );
                }
                return _brush;
            }

            public override Pen GetPen()
            {
                if ( _pen == null )
                {
                    _pen = new Pen( _color );
                }
                return _pen;
            }
        }

        private class GradientElement: SchemeElement
        {
            private Color _startColor;
            private Color _endColor;
            private LinearGradientMode _mode;
            private float[] _blendPositions;
            private float[] _blendFactors;

            private Rectangle _brushRect;

            public GradientElement( Color startColor, Color endColor, LinearGradientMode mode,
                float[] blendPositions, float[] blendFactors )
            {
                _startColor = startColor;
                _endColor = endColor;
                _mode = mode;
                _blendPositions = blendPositions;
                _blendFactors = blendFactors;
            }

            public override Color GetColor()
            {
                return _startColor;
            }

            public override Color GetStartColor()
            {
                return _startColor;
            }

            public override Color GetEndColor()
            {
                return _endColor;
            }

            public override Brush GetBrush( Rectangle rc )
            {
                if ( _brush != null )
                {
                    if ( _mode == LinearGradientMode.Vertical &&
                        rc.Top == _brushRect.Top && rc.Bottom == _brushRect.Bottom )
                    {
                        return _brush;
                    }
                    if ( _mode == LinearGradientMode.Horizontal &&
                        rc.Left == _brushRect.Left && rc.Right == _brushRect.Right )
                    {
                        return _brush;
                    }
                    _brush.Dispose();
                }

                if ( rc.Width == 0 || rc.Height == 0 )
                {
                    _brush = new SolidBrush( _startColor );
                }
                else
                {
                    _brush = new LinearGradientBrush( rc, _startColor, _endColor, _mode );
                    if ( _blendPositions != null )
                    {
                        Blend blend = new Blend();
                        blend.Positions = _blendPositions;
                        blend.Factors = _blendFactors;
                        (_brush as LinearGradientBrush).Blend = blend;
                    }
                }
                _brushRect = rc;

                return _brush;
            }

            public override Pen GetPen()
            {
                if ( _pen == null )
                {
                    _pen = new Pen( _startColor );
                }
                return _pen;
            }
        }

        private Assembly _resourceAssembly;
        private string _iconPrefix;
        private ColorDepth _colorDepth;
        private Hashtable _schemeElements = new Hashtable();   // string -> SchemeElement

        public ColorScheme( Assembly resourceAssembly, string iconPrefix, ColorDepth colorDepth )
		{
            _resourceAssembly = resourceAssembly;
            _iconPrefix = iconPrefix;
            _colorDepth = colorDepth;
		}

        public void Load( Stream stream )
        {
            XmlDocument doc = new XmlDocument();
            doc.Load( stream );
            Load( doc );
        }

        public void Load( XmlDocument doc )
        {
            XmlNode rootNode = doc.SelectSingleNode( "/colorscheme" );
            if ( rootNode == null )
                throw new Exception( "Omea color scheme not found in file" );
            foreach( XmlNode node in rootNode.ChildNodes )
            {
                LoadSchemeNode( node, "" );
            }
        }

	    private void LoadSchemeNode( XmlNode node, string prefix )
	    {
            string nodeName = prefix + node.Name;
            foreach( XmlNode childNode in node.ChildNodes )
            {
                if ( childNode.Name == "color" )
                {
                    LoadColorNode( nodeName, childNode );
                }
                else if ( childNode.Name == "gradient" )
                {
                    LoadGradientNode( nodeName, childNode );
                }
                else if ( childNode.Name == "imagelist" )
                {
                    LoadImageListNode( nodeName, childNode );
                }
                else
                {
                    LoadSchemeNode( childNode, nodeName + "." );
                }
            }
	    }

        private void LoadColorNode( string key, XmlNode node )
        {
            _schemeElements [key] = new SolidColorElement( LoadColorFromNode( node ) );
        }

        private Color LoadColorFromNode( XmlNode node )
        {
            XmlAttribute attrRef = node.Attributes ["ref"];
            if ( attrRef != null )
            {
                string xpath = attrRef.Value;
                XmlNode refNode = node.OwnerDocument.SelectSingleNode( xpath );
                if ( refNode == null )
                {
                    throw new Exception( "Invalid node reference " + xpath );
                }
                return LoadColorFromNode( refNode );
            }

            Color baseColor;
            XmlAttribute attrName = node.Attributes ["name"];
            if ( attrName != null )
            {
                baseColor = Color.FromName( attrName.Value );
            }
            else
            {
                int r = XmlTools.GetRequiredIntAttribute( node, "r" );
                int g = XmlTools.GetRequiredIntAttribute( node, "g" );
                int b = XmlTools.GetRequiredIntAttribute( node, "b" );
                baseColor = Color.FromArgb( r, g, b );
            }

            XmlAttribute attrMult = node.Attributes ["mult"];
            if ( attrMult != null )
            {
                float mult = (float) Double.Parse( attrMult.Value, CultureInfo.InvariantCulture );
                return GdiPlusTools.GetColorMult( baseColor, mult );
            }
            return baseColor;
        }

        private void LoadGradientNode( string key, XmlNode node )
        {
            LinearGradientMode mode;
            XmlAttribute attrMode = node.Attributes ["mode"];
            switch( attrMode.Value )
            {
                case "vertical":          mode = LinearGradientMode.Vertical; break;
                case "horizontal":        mode = LinearGradientMode.Horizontal; break;
                case "forward-diagonal":  mode = LinearGradientMode.ForwardDiagonal; break;
                case "backward-diagonal": mode = LinearGradientMode.BackwardDiagonal; break;
                default:
                    throw new Exception( "Invalid or unspecified <gradient> mode" );
            }

            XmlNode gradStartNode = node.SelectSingleNode( "startcolor" );
            if ( gradStartNode == null )
                throw new Exception( "Gradient <startcolor> not specified" );
            Color startColor = LoadColorFromNode( gradStartNode );

            XmlNode gradEndNode = node.SelectSingleNode( "endcolor" );
            if ( gradEndNode == null )
                throw new Exception( "Gradient <endcolor> not specified" );
            Color endColor = LoadColorFromNode( gradEndNode );

            float[] blendPositions = null;
            float[] blendFactors = null;

            XmlNodeList blendNodes = node.SelectNodes( "blend" );
            if( blendNodes.Count > 0 )
            {
                blendPositions = new float [blendNodes.Count+2];
                blendFactors = new float [blendNodes.Count+2];

                blendPositions [0] = 0.0f;
                blendFactors [0] = 0.0f;

                blendPositions [blendNodes.Count+1] = 1.0f;
                blendFactors [blendNodes.Count+1] = 1.0f;

                for( int i=0; i<blendNodes.Count; i++ )
                {
                    XmlNode blendNode = blendNodes [i];
                    XmlAttribute attrPosition = blendNode.Attributes ["position"];
                    if ( attrPosition == null )
                        throw new Exception( "<blend> position not specified" );

                    XmlAttribute attrFactor = blendNode.Attributes ["factor"];
                    if ( attrFactor == null )
                        throw new Exception( "<blend> factor not specified" );

                    blendPositions [i+1] = (float) Double.Parse( attrPosition.Value, CultureInfo.InvariantCulture );
                    blendFactors [i+1] = (float) Double.Parse( attrFactor.Value, CultureInfo.InvariantCulture );
                }
            }

            _schemeElements [key] = new GradientElement( startColor, endColor, mode,
                blendPositions, blendFactors );
        }

        private void LoadImageListNode( string key, XmlNode node )
        {
            ImageList imageList = new ImageList();
            imageList.ImageSize = new Size( XmlTools.GetRequiredIntAttribute( node, "width" ),
                XmlTools.GetRequiredIntAttribute( node, "height" ) );
            imageList.ColorDepth = _colorDepth;

            foreach( XmlNode childNode in node.ChildNodes )
            {
                if ( childNode.Name == "icon" )
                {
                    string name = XmlTools.GetRequiredAttribute( childNode, "name" );
                    Stream stream = _resourceAssembly.GetManifestResourceStream( _iconPrefix + name );
                    if ( stream == null )
                    {
                        throw new Exception( "Invalid icon name " + name );
                    }
                    imageList.Images.Add( new Icon( stream ) );
                }
            }

            _schemeElements [key] = imageList;
        }

	    public Color GetColor( string elementId )
        {
            return GetSchemeElement( elementId ).GetColor();
        }

        public Color GetStartColor( string elementId )
        {
            return GetSchemeElement( elementId ).GetStartColor();
        }

        public Color GetEndColor( string elementId )
        {
            return GetSchemeElement( elementId ).GetEndColor();
        }

        public Brush GetBrush( string elementId, Rectangle rc )
        {
            return GetSchemeElement( elementId ).GetBrush( rc );
        }

        public Pen GetPen( string elementId )
        {
            return GetSchemeElement( elementId ).GetPen();
        }

        public ImageList GetImageList( string elementId )
        {
            return (ImageList) _schemeElements [elementId];
        }

        private SchemeElement GetSchemeElement( string elementId )
        {
            object anElement = _schemeElements [elementId];
            if ( anElement == null )
            {
                throw new ArgumentException( "Element " + elementId + " not found in scheme", "elementId" );
            }
            if ( !(anElement is SchemeElement ) )
            {
                throw new ArgumentException( "Element " + elementId + " is not a color element", "elementId" );
            }
            return (SchemeElement) anElement;
        }

        public static Color GetColor( ColorScheme scheme, string elementId, Color defaultColor )
        {
            if( scheme != null )
            {
                return scheme.GetColor( elementId );
            }
            return defaultColor;
        }

        public static Color GetStartColor( ColorScheme scheme, string elementId, Color defaultColor )
        {
            if( scheme != null )
            {
                return scheme.GetStartColor( elementId );
            }
            return defaultColor;
        }

        public static Color GetEndColor( ColorScheme scheme, string elementId, Color defaultColor )
        {
            if( scheme != null )
            {
                return scheme.GetEndColor( elementId );
            }
            return defaultColor;
        }

        public static Brush GetBrush( ColorScheme scheme, string elementId, Rectangle rc, Brush defaultBrush )
        {
            if ( scheme != null )
            {
                return scheme.GetBrush( elementId, rc );
            }
            return defaultBrush;
        }

        public static Pen GetPen( ColorScheme scheme, string elementId, Pen defaultPen )
        {
            if ( scheme != null )
            {
                return scheme.GetPen( elementId );
            }
            return defaultPen;
        }

        public static void DrawRectangle( Graphics g, ColorScheme scheme, string elementId, Rectangle rc,
            Pen defaultPen )
        {
            g.DrawRectangle( GetPen( scheme, elementId, defaultPen ), rc );
        }

        public static void FillRectangle( Graphics g, ColorScheme scheme, string elementId, Rectangle rc,
            Brush defaultBrush )
        {
            g.FillRectangle( GetBrush( scheme, elementId, rc, defaultBrush ), rc );
        }
	}

    public interface IColorSchemeable
    {
        ColorScheme ColorScheme { get; set; }
    }

}
