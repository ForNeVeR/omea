/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

/*
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Contacts;

namespace JetBrains.Omea.OutlookPlugin
{
    public class EmailAccountSerializer : IResourceSerializer
    {
        #region IResourceSerializer Members

        public void AfterSerialize( IResource parentResource, IResource res, System.Xml.XmlNode node )
        {
        }

        public IResource AfterDeserialize( IResource parentResource, IResource res, System.Xml.XmlNode node )
        {
            if ( parentResource.Type != "Contact" && res.Type != REGISTRY._strEmailAccount ) return res;
            IResourceList resourceList = parentResource.GetLinksOfType( REGISTRY._strEmailAccount, ContactHelper._propEmailAcct );
            foreach ( IResource resource in resourceList )
            {
                if ( resource.GetStringProp( "EmailAddress" ) == res.GetStringProp( "EmailAddress" ) )
                {
                    return resource;
                }
            }
            return res;
        }

        public SerializationMode GetSerializationMode( IResource res, string propertyType )
        {
            return SerializationMode.Default;
        }
        #endregion
    }
}
*/