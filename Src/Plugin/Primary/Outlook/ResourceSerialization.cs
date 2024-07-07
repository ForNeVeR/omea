// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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
