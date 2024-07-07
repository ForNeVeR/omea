// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.InstantMessaging.ICQ
{
    internal class ICQAccountSerializer : IResourceSerializer
    {
        public void AfterSerialize( IResource parentResource, IResource res, System.Xml.XmlNode node )
        {
        }

        public IResource AfterDeserialize( IResource parentResource, IResource res, System.Xml.XmlNode node )
        {
            int propUIN = ICQPlugin._propUIN;
            IResource account = Core.ResourceStore.FindUniqueResource(
                ICQPlugin._icqAccountResName, propUIN, res.GetIntProp( propUIN ) );
            return ( account != null ) ? account : res;
        }
        public SerializationMode GetSerializationMode( IResource res, string propertyType )
        {
            return SerializationMode.Default;
        }
    }
}
