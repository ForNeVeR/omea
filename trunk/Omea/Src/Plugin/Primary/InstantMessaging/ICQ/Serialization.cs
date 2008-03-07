/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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