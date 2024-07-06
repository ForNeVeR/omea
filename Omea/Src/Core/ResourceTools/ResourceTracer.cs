// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceTools
{
	public class ResourceTracer
	{
        private static ResourceTracer _resourceTracer = new ResourceTracer( "ResourceTracer" );
        private Tracer _tracer = null;
		public ResourceTracer( string category )
		{
            _tracer = new Tracer( category );
		}
        public static void _Trace( IResource resource )
        {
            _Trace( resource, false );
        }
        public static void _Trace( IResource resource, bool traceLinks )
        {
            _resourceTracer.Trace( resource, traceLinks );
        }

        public void Trace( IResource resource )
        {
            Trace( resource, false );
        }

        public void Trace( IResource resource, bool traceLinks )
        {
            _tracer.Trace( "______________________________________" );
            _tracer.Trace( "DisplayName: " + resource.DisplayName );
            _tracer.Trace( "Type: " + resource.Type );
            _tracer.Trace( "ID: " + resource.Id );
            IPropertyCollection properties = resource.Properties;
            foreach ( IResourceProperty property in properties )
            {
                if ( property.DataType == PropDataType.Link )
                {
                    if ( traceLinks )
                    {
                        bool directed = ICore.Instance.ResourceStore.PropTypes[property.PropId].HasFlag( PropTypeFlags.DirectedLink );

                        IResourceList resources = null;
                        string linkType = string.Empty;

                        if ( !directed )
                        {
                            resources = resource.GetLinksOfType( null, property.Name );
                        }
                        else
                        {
                            if ( property.PropId < 0 )
                            {
                                resources = resource.GetLinksTo( null, property.Name );
                                linkType = "To: ";
                            }
                            else
                            {
                                resources = resource.GetLinksFrom( null, property.Name );
                                linkType = "From: ";
                            }
                        }
                        foreach ( IResource linkedResource in resources )
                        {
                            _tracer.Trace( linkType + property.Name );
                            Trace( linkedResource, false );
                        }
                    }
                }
                else
                {
                    string value = string.Empty;
                    if ( property.Value != null )
                    {
                        value = property.Value.ToString();
                    }
                    _tracer.Trace( property.Name+ " = " + value );
                }
            }
            _tracer.Trace( "______________________________________" );
        }
	}
}
