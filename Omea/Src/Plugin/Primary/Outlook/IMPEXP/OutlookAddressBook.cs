/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.ResourceTools;

namespace JetBrains.Omea.OutlookPlugin
{
    internal class OutlookAddressBook
    {
        private string _name;
        private bool _imported;
        private string _entryID;
        private string _storeID;
        private bool _exportable = true;
        public OutlookAddressBook( string name, string entryID, bool exportable )
        {
            _entryID = entryID;
            _imported = false;
            _name = name;
            _exportable = exportable;
        }
        public OutlookAddressBook( string name, PairIDs folderIDs, bool exportable )
        {
            _imported = true;
            _name = name;
            _entryID = folderIDs.EntryId;
            _storeID = folderIDs.StoreId;
            _exportable = exportable;
        }
        public string Name { get { return _name; } }
        public bool Imported { get { return _imported; } }
        public static void SetName( IResource resAB, string name )
        {
            Guard.NullArgument( resAB, "resAB" );
            resAB.SetProp( "DeepName", name );
            resAB.SetProp( "Name", name + " (Outlook)" );
        }
        public static void SetName( string entryID, string name )
        {
            IResource resAB = Core.ResourceStore.FindUniqueResource( "AddressBook", PROP.EntryID, entryID );
            if ( resAB != null )
            {
                SetName( resAB, name );
            }
        }

        public static string GetProposedName( string name, string entryID )
        {
            Guard.NullArgument( name, "name" );
            Guard.NullArgument( entryID, "entryID" );
            if ( name.Length == 0 )
            {
                name = "<noname>";
            }
            string proposedName = name;

            IResource resAB = Core.ResourceStore.FindUniqueResource( "AddressBook", PROP.EntryID, entryID );
            if ( resAB != null )
            {
                string existingName = resAB.GetStringProp( "DeepName" );
                if ( existingName == proposedName )
                {
                    return proposedName;
                }
                if ( existingName.StartsWith( name ) )
                {
                    string suffixStr = existingName.Substring( name.Length );
                    bool digitsOnly = true;
                    for( int i=0; i<suffixStr.Length; i++ )
                    {
                        if ( !Char.IsDigit( suffixStr [i] ) )
                        {
                            digitsOnly = false;                            
                        }
                    }

                    if ( digitsOnly )
                        return existingName;
                }
            }

            int suffix = 1;
            while ( AddressBook.Exists( proposedName ) )
            {
                proposedName = name + suffix++;
            }
            return proposedName;
        }
        public void RunAB()
        {
            Core.ResourceAP.QueueJob( JobPriority.Immediate, new AddressBookDelegate( AB ) );
        }

        private delegate AddressBook AddressBookDelegate();

        public AddressBook AB()
        {
            IResource resAB = Core.ResourceStore.FindUniqueResource( "AddressBook", PROP.EntryID, _entryID );
            if ( resAB != null )
            {
                _name = resAB.GetStringProp( "DeepName" );
            }
            else
            {
                _name = GetProposedName( _name, _entryID );
            }
            AddressBook AB = new AddressBook( _name, STR.Email );
            AB.IsExportable = _exportable;
            AB.Resource.SetProp( PROP.EntryID, _entryID );
            SetName( _entryID, _name );
            if ( _imported )
            {
                AB.Resource.SetProp( PROP.Imported, 1 );
                AB.Resource.SetProp( PROP.StoreID, _storeID );
            }

            //  Upgrade information about Outlook address book - set its
            //  ContentType property so that it could be filtered out when
            //  this plugin is switched off.
            AB.Resource.SetProp( "ContentType", STR.Email );

            return AB;
        }
    }
    public class OutlookAddressBookReName : AbstractNamedJob
    {
        private string _entryID;
        private string _abName;
        public OutlookAddressBookReName( string entryID, string abName )
        {
            _entryID = entryID;
            _abName = abName;
        }
        protected override void Execute()
        {
            OutlookAddressBook.SetName( _entryID, _abName );
        }

        public override string Name
        {
            get { return "Rename address book"; }
            set { }
        }
    }

    internal class OutlookABDescriptor : AbstractNamedJob
    {
        private string _name; 
        private string _oldName; 
        private string _entryID; 
        public OutlookABDescriptor( string name, string entryID )
        {
            _oldName = name;
            _name = name;
            if ( _name == null )
            {
                _name = "<Noname>";
            }
            _entryID = entryID;
        }
        protected override void Execute()
        {
            IResource resource = 
                Core.ResourceStore.FindUniqueResource( STR.OutlookABDescriptor, PROP.EntryID, _entryID );
            if ( resource == null )
            {
                resource = Core.ResourceStore.BeginNewResource( STR.OutlookABDescriptor );
                resource.SetProp( PROP.EntryID, _entryID );
                resource.SetProp( Core.Props.Name, _name );
                resource.EndUpdate();
            }
            else
            {
                resource.SetProp( Core.Props.Name, _name );
                _name = OutlookAddressBook.GetProposedName( _name, _entryID );
                OutlookAddressBook.SetName( _entryID, _name );
            }
        }

        public override string Name
        {
            get { return "Creation address book: " + _oldName + " to: " + _name; }
            set { }
        }
    }
}