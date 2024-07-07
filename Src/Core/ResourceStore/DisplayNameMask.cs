// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Collections;
using System.Text;
using JetBrains.Omea.Base;

namespace JetBrains.Omea.ResourceStore
{
    /// <summary>
    /// The mask for calculating the display name of a resource.
    /// </summary>
    internal class DisplayNameMask
	{
        // TODO: optimize (perform complete parsing when the mask is first constructed)
        private string _mask;
		private string[] _alternatives;
        private BitArray _properties;

        public DisplayNameMask( string mask, bool validate )
		{
            _mask = mask;
            if ( mask == null || mask == "" )
            {
                _alternatives = null;
                _properties = null;
            }
            else
            {
                _alternatives = mask.Split( '|' );
                _properties = new BitArray( 64 );
                for( int i=0; i<_alternatives.Length; i++ )
                {
                    CalcDisplayNameFromMask( null, _alternatives [i], _properties, validate );
                }
            }
		}

        public string GetValue( Resource res )
        {
            if ( _alternatives == null )
            {
                return "";
            }
            for( int i=0; i<_alternatives.Length; i++ )
            {
                string result = CalcDisplayNameFromMask( res, _alternatives [i], null, false );
                if ( result != null )
                    return result;
            }
            return "";
        }

        /**
         * Returns true if the display name mask depends on the specified property.
         */

        public bool DependsOnProperty( int propID )
        {
            if ( _properties == null || propID >= _properties.Length )
                return false;

            return _properties [propID];
        }

        /**
         * Calculates the display name from a single mask variant, or stores the
         * properties used by the mask in a bit array.
         */

        private string CalcDisplayNameFromMask( Resource res, string mask, BitArray properties, bool validate )
        {
            int pos = 0;
            bool wasSpace = false;
            bool foundProps = false;
            StringBuilder result = StringBuilderPool.Alloc();
            try
            {
                while ( pos < mask.Length )
                {
                    if ( Char.IsLetterOrDigit( mask, pos ) )
                    {
                        int propStartPos = pos;
                        pos++;
                        while( pos < mask.Length && Char.IsLetterOrDigit( mask, pos ) )
                        {
                            pos++;
                        }
                        string propName = mask.Substring( propStartPos, pos-propStartPos );
                        if ( CalcProperty( propName, res, result, wasSpace, properties, validate ) )
                        {
                            foundProps = true;
                        }

                        wasSpace = false;
                    }
                    else if ( mask [pos] == '{' )
                    {
                        pos++;
                        int propStartPos = pos;
                        while( pos < mask.Length && mask [pos] != '}'  )
                        {
                            pos++;
                        }
                        string propName = mask.Substring( propStartPos, pos-propStartPos );
                        if ( mask [pos] == '}' )
                        {
                            pos++;
                        }

                        if ( CalcProperty( propName, res, result, wasSpace, properties, validate ) )
                        {
                            foundProps = true;
                        }

                        wasSpace = false;
                    }
                    else if ( mask [pos] == ' ' )
                    {
                        wasSpace = true;
                        pos++;
                    }
                    else
                    {
                        if ( result.Length > 0 )
                        {
                            if ( wasSpace )
                                result.Append( " " );
                            result.Append( mask [pos] );
                        }
                        pos++;
                        wasSpace = false;
                    }
                }
                if ( !foundProps )
                    return null;

                return result.ToString();
            }
            finally
            {
                StringBuilderPool.Dispose( result );
            }
        }

        private static bool CalcProperty( string propName, Resource res,
            StringBuilder result, bool wasSpace, BitArray properties, bool validate )
        {
            bool foundProps = false;
            if ( !validate && !MyPalStorage.Storage.PropTypes.Exist( propName ) )
            {
                return false;
            }
            int propID = MyPalStorage.Storage.GetPropId( propName );

            if ( res != null && res.HasProp( propID ) )
            {
                foundProps = true;
                string propValue = res.GetPropText( propID );
                if ( propValue.Length > 0 )
                {
                    if ( result.Length > 0 && wasSpace )
                        result.Append( " " );
                    result.Append( propValue );
                }
            }
            if ( properties != null )
            {
                if ( properties.Length < propID+1 )
                {
                    properties.Length = propID+1;
                }
                properties [propID] = true;
            }
            return foundProps;
        }

        public override string ToString()
        {
            return _mask;
        }
	}
}
