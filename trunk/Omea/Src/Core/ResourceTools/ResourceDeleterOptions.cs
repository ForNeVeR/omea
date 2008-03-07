/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

namespace JetBrains.Omea.ResourceTools
{
    public class ResourceDeleterOptions
    {
        public static bool GetDeleteAlwaysPermanently( string type )
        {
            return ObjectStore.ReadBool( "DeleteAction", type + ".always-permanently", false );
        }
        public static void SetDeleteAlwaysPermanently( string type, bool value )
        {
            ObjectStore.WriteBool( "DeleteAction", type + ".always-permanently", value );
        }
        public static bool GetConfirmDeleteToRecycleBin( string type )
        {
            return ObjectStore.ReadBool( "DeleteAction", type + ".confirm-to-recyclebin", false );
        }
        public static void SetConfirmDeleteToRecycleBin( string type, bool value )
        {
            ObjectStore.WriteBool( "DeleteAction", type + ".confirm-to-recyclebin", value );
        }
        public static bool GetConfirmDeletePermanently( string type )
        {
            return ObjectStore.ReadBool( "DeleteAction", type + ".confirm-permanently", true );
        }
        public static void SetConfirmDeletePermanently( string type, bool value )
        {
            ObjectStore.WriteBool( "DeleteAction", type + ".confirm-permanently", value );
        }
    }
}