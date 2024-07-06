// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.ResourceTools
{
	/**
     * A list of contacts or contact categories with specific synchronization
     * settings.
     */

    public class AddressBook
	{
        private static bool _typesRegistered;
        private static int _propInAddressBook;
        private static int _propDeepName;
        private static IResource _addressBookRoot;

        private string      _name;
        private string      _ownerType;
        private IResource   _abResource;

        /**
         * Creates or finds an existing address book with the specified name.
         */

        public AddressBook( string name ) : this ( name, null ) {}
        public AddressBook( string name, string ownerType )
		{
            Initialize();

            _name = name;
            _ownerType = ownerType;
            _abResource = FindUniqueAB(  name );
            if ( _abResource == null )
            {
                Core.ResourceAP.RunJob( "Creating the address book", new MethodInvoker( CreateAddressBook ) );
            }
            else
            //  Provide the upgrade between Resource store versions.
            if( _abResource.GetStringProp( "ContentType" ) == null && ownerType != null )
            {
                new ResourceProxy( _abResource ).SetProp( "ContentType", ownerType );
            }
		}

        public static IResource FindUniqueAB( string name )
        {
            IResource ret = null;
            IResourceList ABs = Core.ResourceStore.FindResources( "AddressBook", _propDeepName, name );
            if ( ABs.Count > 0 )
            {
                ret = ABs[0];
                if ( ABs.Count > 1 )
                {
                    for ( int i = ABs.Count - 1; i > 0; i-- )
                    {
                        MergeABs( ret, ABs[i] );
                    }
                }
            }
            return ret;
        }

        public static void MergeABs( IResource dest, IResource src )
        {
            ResourceProxy prxDest = new ResourceProxy( dest );
            IResourceList contacts = src.GetLinksOfType( "Contact", _propInAddressBook );
            foreach ( IResource contact in contacts )
            {
                prxDest.AddLink( _propInAddressBook, contact );
            }
            ResourceProxy prxSrc = new ResourceProxy( src );
            prxSrc.Delete();
        }

        /**
         * Creates an address book from the specified resource.
         */

        public AddressBook( IResource abResource )
        {
            Initialize();
            _abResource = abResource;
            _name = _abResource.GetStringProp( "Name" );
            _ownerType = (string)_abResource.GetProp( "ContentType" );
        }

        public static IResource AddressBookRoot
        {
            get { return _addressBookRoot; }
        }

        public static int PropInAddressBook
        {
            get { return _propInAddressBook; }
        }

        public static void Initialize()
        {
            if ( !_typesRegistered )
            {
                Core.ResourceAP.RunUniqueJob( new MethodInvoker( RegisterTypes ) );
                AddressBookUIHandler handler = new AddressBookUIHandler();
                Core.PluginLoader.RegisterResourceUIHandler( "AddressBook", handler );
                Core.PluginLoader.RegisterResourceRenameHandler( "AddressBook", handler );
                Core.PluginLoader.RegisterResourceDragDropHandler( "AddressBook", handler );
            }
        }
        public static void Initialize( bool force )
        {
            if ( force )
            {
                _typesRegistered = false;
            }
            Initialize();
        }

        /**
         * Registers the resource and property types used by address books,
         * and creates the address book root.
         */

        private static void RegisterTypes()
        {
            IResourceStore store = Core.ResourceStore;
            IPropTypeCollection props = Core.ResourceStore.PropTypes;

            store.ResourceTypes.Register( "AddressBook", "Address Book", "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            _propInAddressBook = props.Register( "InAddressBook", PropDataType.Link, PropTypeFlags.DirectedLink );
            _propDeepName = props.Register( "DeepName", PropDataType.String );
            props.Register( "IsNonExportable", PropDataType.Bool, PropTypeFlags.Internal );
            props.RegisterDisplayName( _propInAddressBook, "In Address Book", "Contains" );

            _addressBookRoot = Core.ResourceTreeManager.GetRootForType( "AddressBook" );
            Core.ResourceTreeManager.SetResourceNodeSort( _addressBookRoot, "Name" );

            if ( store.ResourceTypes.Exist( "AddressBookRoot" ) )
            {
                IResourceList abRootsOld = store.GetAllResources( "AddressBookRoot" );
                if ( abRootsOld.Count > 0 )
                {
                    IResource abRootOld = abRootsOld [0];
                    foreach( IResource res in abRootOld.GetLinksTo( null, "Parent") )
                    {
                        res.SetProp( "Parent", _addressBookRoot );
                    }
                    abRootOld.Delete();
                }
            }

            _typesRegistered = true;
        }

        /**
         * Creates the resource for the current address book and links it to the
         * address book root.
         */

        private void CreateAddressBook()
        {
            _abResource = Core.ResourceStore.BeginNewResource( "AddressBook" );
            _abResource.SetProp( "Name", _name );
            _abResource.SetProp( "DeepName", _name );
            _abResource.SetProp( "ContentType", _ownerType );
            _abResource.AddLink( "Parent", _addressBookRoot );
            _abResource.EndUpdate();
        }

        /**
         * Adds a contact to the address book.
         */

        public void AddContact( IResource res )
        {
            #region Preconditions
            if( res == null )
                throw new ArgumentNullException( "AddressBook - Contact can not be NULL." );

            if( res.Type != "Contact" )
                throw new ArgumentException( "AddressBook - Contact resource has illegal type [" + res.Type + "]" );
            #endregion Preconditions

            new ResourceProxy( res ).AddLink( _propInAddressBook, _abResource );
        }

        /**
         * Removes a contact from the address book.
         */

        public void RemoveContact( IResource res )
        {
            #region Preconditions
            if( res == null )
                throw new ArgumentNullException( "AddressBook - Contact can not be NULL." );

            if( res.Type != "Contact" )
                throw new ArgumentException( "AddressBook - Contact resource has illegal type [" + res.Type + "]" );

            if( !res.HasLink( _propInAddressBook, _abResource ) )
                throw new ArgumentException( "AddressBook - Contact [" + res.DisplayName +
                                             "] does not exist in the Address Book [" + _abResource.DisplayName + "]" );
            #endregion Preconditions

            new ResourceProxy( res ).DeleteLink( _propInAddressBook, _abResource );
        }

        public IResource Resource
        {
            get { return _abResource; }
        }

        public bool IsExportable
        {
            get
            {
                return !_abResource.HasProp( "IsNonExportable" );
            }
            set
            {
                bool hasProp = _abResource.HasProp( "IsNonExportable" );
                if( value && hasProp )
                {
                    new ResourceProxy( _abResource ).DeleteProp( "IsNonExportable" );
                }
                else if ( !value && !hasProp )
                {
                    new ResourceProxy( _abResource ).SetProp( "IsNonExportable", true );
                }
            }
        }

        public static bool Exists( string name )
        {
            Initialize();
            return FindUniqueAB( name ) != null;
        }
    }

    /**
     * UI operations handler for the AddressBook class.
     */

    internal class AddressBookUIHandler: IResourceUIHandler, IResourceRenameHandler, IResourceDragDropHandler
    {
        private IResource _lastAB;
        private IResourceList _lastABList;
        private delegate void DropResourcesDelegate( IResource targetRes, IResourceList droppedResources );

        internal AddressBookUIHandler()
        {
        }

        public bool CanRenameResource(IResource res, ref string editText)
        {
            return true;
        }

        public bool ResourceRenamed( IResource res, string newName )
        {
            if ( newName == "" )
            {
                MessageBox.Show( Core.MainWindow, "Please specify a name for the address book" );
                return false;
            }

            IResource ab = Core.ResourceStore.FindUniqueResource( "AddressBook", "Name", newName );
            if ( ab != null )
            {
                MessageBox.Show( Core.MainWindow, "An address book called '" + newName + "' already exists" );
                return false;
            }

            new ResourceProxy( res ).SetPropAsync( "Name", newName );
            return true;
        }

        public void ResourceNodeSelected( IResource res )
        {
            if ( res != _lastAB )
            {
                _lastAB = res;
                _lastABList = _lastAB.GetLinksToLive( null, "InAddressBook" );

                _lastABList.Sort( new SortSettings( ResourceProps.DisplayName, true ) );
            }

            ResourceListDisplayOptions options = new ResourceListDisplayOptions();
            options.DefaultGroupItems = false;
            options.CaptionTemplate = "Address Book: %OWNER%";
            Core.ResourceBrowser.DisplayResourceList( res, _lastABList, options );
        }

        public bool CanRenameResource(IResource res)
        {
            throw new NotImplementedException();
        }

        #region IResourceDragDropHandler Members
        public void Drop(IResource targetResource, IDataObject data, System.Windows.Forms.DragDropEffects allowedEffect, int keyState)
        {
            IResourceList dragResources = data.GetData( typeof(IResourceList) ) as IResourceList;
            if ( dragResources != null )
            {
                ResourcesDropped( targetResource, dragResources );
            }
        }

        public System.Windows.Forms.DragDropEffects DragOver(IResource targetResource, IDataObject data, System.Windows.Forms.DragDropEffects allowedEffect, int keyState)
        {
            IResourceList dragResources = data.GetData( typeof(IResourceList) ) as IResourceList;
            if ( dragResources != null && CanDropResources( targetResource, dragResources ) )
            {
                return DragDropEffects.Link;
            }
            return DragDropEffects.None;
        }

        public void AddResourceDragData(IResourceList dragResources, IDataObject dataObject)
        {
        }

        public bool CanDropResources( IResource targetResource, IResourceList dragResources )
        {
            bool  allContacts = dragResources.AllResourcesOfType( "Contact" ),
                  allCategs = dragResources.AllResourcesOfType( "Category" );
            return allCategs || (allContacts && !targetResource.HasProp( "IsNonExportable" ) );
        }

        public void ResourcesDropped( IResource targetResource, IResourceList droppedResources )
        {
            Core.ResourceAP.QueueJob( JobPriority.Immediate,
                new DropResourcesDelegate( DoDropResources ), targetResource, droppedResources );
        }

        private void DoDropResources( IResource targetResource, IResourceList droppedResources )
        {
            foreach( IResource res in droppedResources )
            {
                res.AddLink( "InAddressBook", targetResource );
            }
        }
        #endregion
    }
}
