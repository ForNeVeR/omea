// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Diagnostics;
using System.IO;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.FileTypes;


namespace JetBrains.Omea.ResourceTools
{
    public delegate void BeforeDeserializationDelegate( IResource baseRes, IResource linked, int propId );
    public class ResourceBinarySerialization
    {
        /**
         * User should manually close returned stream
         */
        public static Stream Serialize( IResource resource )
        {
            JetMemoryStream result = new JetMemoryStream();
            BinaryWriter writer = new BinaryWriter( result );
            writer.Write( resource.Type );
            writer.Write( resource.Properties.Count );
            foreach( IResourceProperty prop in resource.Properties )
            {
                writer.Write( (int) prop.DataType );
                int propId = prop.PropId;
                writer.Write( propId );
                switch( prop.DataType )
                {
                    case PropDataType.Link:
                    {
                        IResourceList links;
                        if( Core.ResourceStore.PropTypes[ propId ].HasFlag( PropTypeFlags.DirectedLink ) )
                        {
                            links = ( propId < 0 ) ? resource.GetLinksTo( null, -propId ) : resource.GetLinksFrom( null, propId );
                        }
                        else
                        {
                            links = resource.GetLinksOfType( null, propId );
                        }
                        writer.Write( links.Count );
                        foreach( IResource linked in links )
                        {
                            writer.Write( linked.Id );
                        }
                        break;
                    }
                    case PropDataType.String:
                    case PropDataType.LongString:
                    {
                        writer.Write( resource.GetPropText( propId ) );
                        break;
                    }
                    case PropDataType.StringList:
                    {
                        IStringList strList = resource.GetStringListProp( propId );
                        int count = strList.Count;
                        writer.Write( count );
                        for( int i = 0; i < count; ++i )
                        {
                            writer.Write( strList[ i ] );
                        }
                        break;
                    }
                    case PropDataType.Int:
                    {
                        writer.Write( resource.GetIntProp( propId ) );
                        break;
                    }
                    case PropDataType.Date:
                    {
                        writer.Write( resource.GetDateProp( propId ).Ticks );
                        break;
                    }
                    case PropDataType.Bool:
                    {
                        /**
                         * if a resource has bool prop, then it's equal to 'true'
                         * there is no need always to write 'true' to stream :-)
                         */
                        break;
                    }
                    case PropDataType.Double:
                    {
                        writer.Write( resource.GetDoubleProp( propId ) );
                        break;
                    }
                    case PropDataType.Blob:
                    {
                        Stream stream = resource.GetBlobProp( propId );
                        writer.Write( (int) stream.Length );
                        FileResourceManager.CopyStream( stream, result );
                        break;
                    }
                    default:
                    {
                        throw new Exception( "Serialized resource has properties of unknown type" );
                    }
                }
            }
            return result;
        }

        public static IResource Deserialize( Stream stream )
        {
            return Deserialize( stream, null );
        }
        public static IResource Deserialize( Stream stream, BeforeDeserializationDelegate beforeCheck )
        {
            if( stream.CanSeek )
            {
                stream.Position = 0;
            }
            BinaryReader reader = new BinaryReader( stream );
            using( reader )
            {
                string resType = reader.ReadString();
                IResource result = Core.ResourceStore.BeginNewResource( resType );
                try
                {
                    int propCount = reader.ReadInt32();
                    for( int i = 0; i < propCount; ++i )
                    {
                        PropDataType propType = (PropDataType) reader.ReadInt32();
                        int propId = reader.ReadInt32();
                        switch( propType )
                        {
                            case PropDataType.Link:
                            {
                                int count = reader.ReadInt32();
                                for( int j = 0; j < count; ++j )
                                {
                                    LinkResource( reader, result, propId, beforeCheck );
                                }
                                break;
                            }
                            case PropDataType.String:
                            case PropDataType.LongString:
                            {
                                result.SetProp( propId, reader.ReadString() );
                                break;
                            }
                            case PropDataType.StringList:
                            {
                                int count = reader.ReadInt32();
                                IStringList strLst = result.GetStringListProp( propId );
                                using( strLst )
                                {
                                    for( int j = 0; j < count; ++j )
                                    {
                                        strLst.Add( reader.ReadString() );
                                    }
                                }
                                break;
                            }
                            case PropDataType.Int:
                            {
                                result.SetProp( propId, reader.ReadInt32() );
                                break;
                            }
                            case PropDataType.Date:
                            {
                                result.SetProp( propId, new DateTime( reader.ReadInt64() ) );
                                break;
                            }
                            case PropDataType.Bool:
                            {
                                result.SetProp( propId, true );
                                break;
                            }
                            case PropDataType.Double:
                            {
                                result.SetProp( propId, reader.ReadDouble() );
                                break;
                            }
                            case PropDataType.Blob:
                            {
                                int length = reader.ReadInt32();
                                byte[] buffer = new byte[ length ];
                                reader.Read( buffer, 0, length );
                                result.SetProp( propId, new JetMemoryStream( buffer, true ) );
                                break;
                            }
                        }
                    }
                }
                finally
                {
                    result.EndUpdate();
                }
                return result;
            }
        }

        private static void LinkResource( BinaryReader reader, IResource result, int linkId,
            BeforeDeserializationDelegate beforeCheck )
        {
            int linkedResId = reader.ReadInt32();
            IResource linked = Core.ResourceStore.TryLoadResource( linkedResId );

            if ( linked != null )
            {
                linked.BeginUpdate();
                //  Caller may need to perform special actions, e.g. to
                //  keep the consistency of link restrictions
                if( beforeCheck != null )
                    beforeCheck( result, linked, linkId );

                if( Math.Abs( linkId ) == Core.ContactManager.Props.LinkBaseContact )
                    Trace.WriteLine( "Deserializer -- adding link between " + result.DisplayName + "/" + result.Id + " and " + linked.DisplayName + "/" + linked.Id );
                if( linkId < 0 )
                {
                    linked.AddLink( -linkId, result );
                }
                else
                {
                    result.AddLink( linkId, linked );
                }
                linked.EndUpdate();
            }
        }
    }
}
