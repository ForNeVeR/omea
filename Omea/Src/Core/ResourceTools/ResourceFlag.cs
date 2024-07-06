// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.OpenAPI;
using System.Windows.Forms;

namespace JetBrains.Omea.ResourceTools
{
	/**
     * Resource for managing a flag.
     */

    public class ResourceFlag
	{
        private static bool _typesRegistered = false;
        public static int _propFlagId;
        private static int _propIconAssembly;
        private static int _propIconName;
        public static int PropFlag;
        public static int PropNextStateFlag;
        private static ResourceFlag _defaultFlag;

        public static void RegisterTypes()
        {
            IResourceStore store = ICore.Instance.ResourceStore;
            store.ResourceTypes.Register( "Flag", "Name", ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            _propFlagId = store.PropTypes.Register( "FlagId", PropDataType.String, PropTypeFlags.Internal );
            store.RegisterUniqueRestriction( "Flag", _propFlagId );
            _propIconAssembly = store.PropTypes.Register( "IconAssembly", PropDataType.String, PropTypeFlags.Internal );
            _propIconName = store.PropTypes.Register( "IconName", PropDataType.String, PropTypeFlags.Internal );
            PropFlag = ResourceTypeHelper.UpdatePropTypeRegistration( "Flag", PropDataType.Link,
                PropTypeFlags.DirectedLink );
            store.PropTypes.RegisterDisplayName( PropFlag, "Flag", "Flagged" );
            PropNextStateFlag = store.PropTypes.Register( "NextStateFlag", PropDataType.Link,
                PropTypeFlags.Internal | PropTypeFlags.DirectedLink );
            _typesRegistered = true;
        }

        public static ResourceFlag DefaultFlag
        {
            get
            {
                if( _defaultFlag == null )
                    throw new ApplicationException( "DefaultFlag is requested before initialization" );
                return _defaultFlag;
            }
            set { _defaultFlag = value; }
        }

        private IResource _resource;

        public ResourceFlag( string flagID, string name, string iconAssembly, string iconName )
        {
            if ( !_typesRegistered )
            {
                Core.ResourceAP.RunJob( new MethodInvoker( RegisterTypes ) );
            }

            _resource = Core.ResourceStore.FindUniqueResource( "Flag", "FlagId", flagID );
            if ( _resource == null )
            {
                ResourceProxy proxy = ResourceProxy.BeginNewResource( "Flag" );
                proxy.SetProp( Core.Props.Name, name );
                proxy.SetProp( _propFlagId, flagID );
                proxy.SetProp( _propIconAssembly, iconAssembly );
                proxy.SetProp( _propIconName, iconName );
                proxy.EndUpdate();

                _resource = proxy.Resource;
            }
            else
            {
                ResourceProxy proxy = new ResourceProxy( _resource );
                proxy.SetProp( _propIconAssembly, iconAssembly );
                proxy.SetProp( _propIconName, iconName );
            }
        }

        public ResourceFlag( string flagID )
        {
            _resource = Core.ResourceStore.FindUniqueResource( "Flag", "FlagId", flagID );
            if ( _resource == null )
            {
                throw new Exception( "Flag " + flagID + "not found" );
            }
        }

        public ResourceFlag( IResource res )
        {
            _resource = res;
        }

        public string FlagId { get { return _resource.GetStringProp( _propFlagId ); } }

        public IResource Resource
        {
            get { return _resource; }
        }

        public ResourceFlag NextStateFlag
        {
            get
            {
                IResource flag = _resource.GetLinkProp( PropNextStateFlag );
                if ( flag == null )
                    return null;
                return new ResourceFlag( flag );
            }
            set
            {
                new ResourceProxy( _resource ).SetProp( PropNextStateFlag, value._resource );
            }
        }

        static public void Clear( IResource resource )
        {
            resource.DeleteLinks( "Flag" );
        }

        static public bool IsFlagSet( IResource resource )
        {
            return resource.HasProp( PropFlag );
        }
        static public ResourceFlag GetResourceFlag( IResource resource )
        {
            IResource resFlag = resource.GetLinkProp( PropFlag );
            if ( resFlag != null )
            {
                return new ResourceFlag( resFlag );
            }
            return null;
        }

        public static void ToggleFlag( IResource res )
        {
            if ( !res.HasProp( PropFlag ) )
            {
                if ( DefaultFlag != null )
                {
                    DefaultFlag.SetOnResource( res );
                }
            }
            else
            {
                ResourceFlag flag = new ResourceFlag( res.GetLinkProp( PropFlag ) );
                ResourceFlag nextStateFlag = flag.NextStateFlag;
                if ( nextStateFlag != null )
                {
                    nextStateFlag.SetOnResource( res );
                }
                else
                {
                    new ResourceProxy( res ).DeleteLinks( PropFlag );
                }
            }
        }

        public void SetOnResource( IResource res )
        {
            new ResourceProxy( res ).SetProp( "Flag", _resource );
        }
	}
}
