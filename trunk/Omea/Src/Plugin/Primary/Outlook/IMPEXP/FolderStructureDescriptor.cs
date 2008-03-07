/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class FolderStructureDescriptor : AbstractNamedJob
    {
        private FolderDescriptor _parentFolder;
        private FolderDescriptor _folder;

        //parentFolder can be null
        public FolderStructureDescriptor( FolderDescriptor parentFolder, FolderDescriptor folder )
        {
            Guard.NullArgument( folder, "folder" );

            _parentFolder = parentFolder;
            _folder = folder;
        }

        protected override void Execute()
        {
            Folder.AddSubFolder( _parentFolder, _folder );
            UpdateContactFolder( _folder );
        }
        public static void UpdateContactFolder( FolderDescriptor folder )
        {
            if ( folder.ContainerClass == FolderType.Contact )
            {
                IResource resAB = Core.ResourceStore.FindUniqueResource( "AddressBook", PROP.EntryID, folder.FolderIDs.EntryId );
                if ( resAB != null )
                {
                    OutlookAddressBook.SetName( resAB, OutlookAddressBook.GetProposedName( folder.Name, folder.FolderIDs.EntryId ) );
                }
            }
        }

        public override string Name
        {
            get { return "Add subfolder: "; }
            set { }
        }
    }
}