/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Windows.Forms;
using JetBrains.DataStructures;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.GUIControls;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.RemoteControl;
using Microsoft.Win32;

namespace JetBrains.Omea
{
	public class ProtocolHandlerManager : ProtocolHandlersInResourceStore, IProtocolHandlerManager
	{
        private readonly HashMap _handlers = new HashMap();
        private readonly HashMap _makeDefaultHandler = new HashMap();
        
        private string _openurl = null;
        private readonly OnRemoteInvokeDelegate _onRemoteInvokeDelegate = null;
        private delegate int OnRemoteInvokeDelegate( string url );
        private const string METHOD_NAME = "Omea.ProtocolHandlerManager.OpenURL";

		public ProtocolHandlerManager()
		{
            _onRemoteInvokeDelegate = OnRemoteInvoke;
		}

        public void CheckProtocols( IWin32Window parent )
        {
            Core.UserInterfaceAP.QueueJob( new CheckProtocolsDelegate( CheckProtocolsImpl ), parent );
        }
        private delegate void CheckProtocolsDelegate( IWin32Window parent );

        public static HashMap GetProtocols( IResourceList protocolHandlers )
        {
            HashMap protocolSet = new HashMap( protocolHandlers.Count );
            foreach ( IResource protocol in protocolHandlers )
            {
                string friendlyName = protocol.GetPropText( _propFriendlyName );
                HashMap.Entry entry = protocolSet.GetEntry( friendlyName );
                if ( entry == null )
                {
                    ArrayList list = new ArrayList();
                    list.Add( protocol );
                    protocolSet.Add( friendlyName, list );
                }
                else
                {
                    ((ArrayList)entry.Value).Add( protocol );
                }
            }
            return protocolSet;
        }
        private void CheckProtocolsImpl( IWin32Window parent )
        {
            IResourceList protocolHandlers = GetProtocolsToCheck();
            HashMap protocolSet = GetProtocols( protocolHandlers );
            foreach ( HashMap.Entry entry in protocolSet )
            {
                ArrayList list = (ArrayList)entry.Value;
                foreach ( IResource protocol in list )
                {
                    string protocolName = protocol.GetStringProp( PROTOCOL );
                    if ( !ProtocolHandlersInRegistry.IsDefaultHandler( protocolName ) )
                    {
                        CheckProtocols( parent, list );
                        break;
                    }
                }
            }
        }

        private static void CheckProtocols( IWin32Window parent, ArrayList list )
        {
            string friendlyName = ((IResource)list[0]).GetPropText( _propFriendlyName );
            string message = Core.ProductFullName + " is not your default " + friendlyName + 
                ". Would you like to make it your default " + friendlyName + "?";
            string checkBoxText = "&Check if " + Core.ProductFullName + " is the default " + friendlyName + " on startup";
            MessageBoxWithCheckBox.Result result = 
                MessageBoxWithCheckBox.ShowYesNo( parent, message, Core.ProductFullName, checkBoxText, true );
            foreach ( IResource protocol in list )
            {
                string protocolName = protocol.GetStringProp( PROTOCOL );
                if ( !result.Checked )
                {
                    SetCheckNeeded( protocolName, false );
                }
                if ( result.IdPressedButton == (int)DialogResult.Yes )
                {
                    SetAsDefaultHandler( protocol, result.Checked );
                }
            }
        }

        private void InvokeMakeDefault( string protocol )
        {
            Guard.NullArgument( protocol, "protocol" );
            lock ( _makeDefaultHandler )
            {
                HashMap.Entry entry = _makeDefaultHandler.GetEntry( protocol );
                if ( entry != null )
                {
                    ((MethodInvoker)entry.Value).Invoke();
                }
            }
        }

        public static void SetAsDefaultHandler( IResource resProtocol, bool check )
        {
            ProtocolHandlerManager manager = Core.ProtocolHandlerManager as ProtocolHandlerManager;
            Guard.NullLocalVariable( manager, "manager" );

            string protocol = resProtocol.GetPropText( _propProtocol );
            ProtocolHandlersInRegistry.SetAsDefaultHandler( protocol, resProtocol.GetPropText( _propFriendlyName ) );
            SetCheckNeeded( protocol, check );
            manager.InvokeMakeDefault( protocol );
        }


        public int Registrations { get { return _handlers.Count; } }
        private class ProtocolHandler
        {
            private readonly string _protocol;
            private readonly string _friendlyName;
            private readonly ProtocolHandlerCallback _handler;

            public ProtocolHandler( string protocol, string friendlyName, ProtocolHandlerCallback handler )
            {
                _protocol = protocol;
                _friendlyName = friendlyName;
                _handler = handler;
            }
            public string Protocol { get { return _protocol; } }
            public string FriendlyName { get { return _friendlyName; } }
            public ProtocolHandlerCallback ProtocolHandlerCallback { get { return _handler; } }
            public void Invoke( string url )
            {
                _handler.Invoke( url );
            }
        }
        public int OnRemoteInvoke( string url )
        {
            try
            {
                Invoke( url );
            }
            catch ( Exception exception )
            {
                Tracer._TraceException( exception );
            }
            return 0;
        }

        public void SetOpenURL( string openurl )
        {
            Guard.NullArgument( openurl, "openurl" );
            _openurl = openurl;
        }
        public void AddRemoteCall()
        {
            Core.RemoteControllerManager.AddRemoteCall( METHOD_NAME, _onRemoteInvokeDelegate );
        }
        public void RemoteInvoke( int port, string secureKey )
        {
            if ( _openurl != null )
            {
                new RemoteControlClient( port, secureKey ).SendRequest( METHOD_NAME, new object[] { "url", _openurl });
            }
        }
        public void InvokeOpenUrl( )
        {
            Invoke( _openurl );
        }
        public bool IsOpenUrlRequested { get { return _openurl != null; } }

        public void Invoke( string url )
        {
            if ( url == null ) return;
            int pos = url.IndexOf( ':' );
            if ( pos != -1 )
            {
                string protocol = url.Substring( 0, pos );
                HashMap.Entry entry = null;
                lock ( _handlers )
                {
                    entry = _handlers.GetEntry( protocol );
                }
                if ( entry != null )
                {
                    ++pos;
                    string URL = url.Substring( pos, url.Length - pos );
                    ProtocolHandler handler = entry.Value as ProtocolHandler;
                    if ( handler != null )
                    {
                        try
                        {
                            handler.Invoke( URL );
                        }
                        catch ( Exception exception )
                        {
                            Core.ReportException( exception, ExceptionReportFlags.AttachLog );
                        }
                    }
                }
            }
        }

        private static void CheckParameters( string protocol, string friendlyName, ProtocolHandlerCallback handler )
        {
            Guard.NullArgument( protocol, "protocol" );
            Guard.NullArgument( friendlyName, "friendlyName" );
            Guard.NullArgument( handler, "handler" );
            if ( protocol.Length == 0 )
            {
                throw new ArgumentException( "Zero length for protocol name", "protocol" );
            }
            if ( protocol.IndexOf( ':' ) != -1 )
            {
                throw new ArgumentException( "Protocol name must not include ':' symbol", "protocol" );
            }
        }

	    /// <summary>
	    /// Registers new URL protocol handler.
	    /// </summary>
	    /// <param name="protocol">URL protocol.</param>
	    /// <param name="friendlyName">Friendly name of URL protocol.</param>
	    /// <param name="handler">Delegate that uses as protocol handler for processing requested urls.</param>
	    public void RegisterProtocolHandler( string protocol, string friendlyName, ProtocolHandlerCallback handler )
	    {
            CheckParameters( protocol, friendlyName, handler );
            lock ( _handlers )
	        {
                _handlers[ protocol ] = new ProtocolHandler( protocol, friendlyName, handler );
                SaveProtocolSettings( protocol, friendlyName, Default.NoChanges );
	        }
	    }
        /// <summary>
        /// Registers new URL protocol handler.
        /// </summary>
        /// <param name="protocol">URL protocol.</param>
        /// <param name="friendlyName">Friendly name of URL protocol.</param>
        /// <param name="handler">Delegate that uses as protocol handler for processing requested urls.</param>
        /// <param name="makeDefaultHandler">Delegate that is invoked when protocol is set as default.</param>
        public void RegisterProtocolHandler( string protocol, string friendlyName, ProtocolHandlerCallback handler, MethodInvoker makeDefaultHandler )
        {
            RegisterProtocolHandler( protocol, friendlyName, handler );
            Guard.NullArgument( makeDefaultHandler, "makeDefaultHandler" );
            lock ( _makeDefaultHandler )
            {
                _makeDefaultHandler[ protocol ] = makeDefaultHandler;
            }
        }

	}
    internal class ProtocolHandlersInRegistry
    {
        private const string URLProtocol = "URL Protocol";
        private const string SHELL_COMMAND = "\\shell\\open\\command";
        public static bool IsDefaultHandler( string protocol )
        {
            Guard.NullArgument( protocol, "protocol" );
            string protocolKey = "Software\\Classes\\" + protocol;
            string urlProtocol = RegUtil.GetValue( Registry.CurrentUser, protocolKey, URLProtocol ) as string;
            if ( urlProtocol != null )
            {
                string command = RegUtil.GetValue( Registry.CurrentUser, protocolKey + SHELL_COMMAND, "" ) as string;
                if ( command != null )
                {
                    return command.ToLower().IndexOf( Application.ExecutablePath.ToLower() ) != -1;
                }
            }
            return false;
        }
        public static void SetAsDefaultHandler( string protocol, string friendlyName )
        {
            Guard.NullArgument( protocol, "protocol" );
            Guard.NullArgument( friendlyName, "friendlyName" );
            string protocolKey = "Software\\Classes\\" + protocol;
            if ( !RegUtil.CreateSubKey( Registry.CurrentUser, protocolKey ) ) return;
            RegUtil.SetValue( Registry.CurrentUser, protocolKey, "", "URL:" + friendlyName );
            RegUtil.SetValue( Registry.CurrentUser, protocolKey, URLProtocol, "" );
            if ( RegUtil.CreateSubKey( Registry.CurrentUser, protocolKey + SHELL_COMMAND ) )
            {
                RegUtil.SetValue( Registry.CurrentUser, protocolKey + SHELL_COMMAND, "", 
                    "\"" + Application.ExecutablePath + "\"" + " -openurl %1" );
            }
            if ( RegUtil.CreateSubKey( Registry.CurrentUser, protocolKey + "\\DefaultIcon" ) )
            {
                RegUtil.SetValue( Registry.CurrentUser, protocolKey + "\\DefaultIcon", "", 
                    "\"" + Application.ExecutablePath + "\"" );
            }
        }
    }
    public class ProtocolHandlersInResourceStore
    {
        private static bool _registered = false;
        public const string PROTOCOL_HANDLER = "ProtocolHandler";
        public const string FNAME = "ProtocolFriendlyName";
        public const string PROTOCOL = "Protocol";
        public const string DEFAULT = "DefaultProtocol";
        public const string CHECK = "Check";
        public static int _propProtocol;
        public static int _propFriendlyName;
        public static int _propDefault;
        public static int _propCheck;

        public ProtocolHandlersInResourceStore()
        {
            _registered = false;
        }
        private static void CheckRegistration()
        {
            if ( !_registered )
            {
                throw new ApplicationException( "ProtocolHandlersInResourceStore: Resoures were not registered" );
            }
        }
        public IResourceList GetProtocolsToCheck( )
        {
            return Core.ResourceStore.FindResources( PROTOCOL_HANDLER, _propCheck, true );
        }

        public IResource GetProtocolResource( string protocol )
        {
            CheckRegistration();
            return Core.ResourceStore.FindUniqueResource( PROTOCOL_HANDLER, _propProtocol, protocol );
        }
        public static IResourceList GetProtocolHandlersList()
        {
            CheckRegistration();
            return Core.ResourceStore.GetAllResources( PROTOCOL_HANDLER );
        }
        public IResourceList ProtocolHandlersList
        {
            get
            {
                return GetProtocolHandlersList();
            }
        }
        public void RegisterResources()
        {
            _registered = true;
            IResourceStore RS = Core.ResourceStore;
            _propProtocol = RS.PropTypes.Register( PROTOCOL, PropDataType.String, PropTypeFlags.Internal );
            _propFriendlyName = RS.PropTypes.Register( FNAME, PropDataType.String, PropTypeFlags.Internal );
            _propDefault = RS.PropTypes.Register( DEFAULT, PropDataType.Bool, PropTypeFlags.Internal );
            _propCheck = RS.PropTypes.Register( CHECK, PropDataType.Bool, PropTypeFlags.Internal );
            RS.ResourceTypes.Register( PROTOCOL_HANDLER, PROTOCOL_HANDLER, FNAME + " | " + PROTOCOL, ResourceTypeFlags.Internal | ResourceTypeFlags.NoIndex );
            RS.RegisterUniqueRestriction( PROTOCOL_HANDLER, _propProtocol );
        }
        public enum Default
        {
            Yes,
            No,
            NoChanges
        }
        public static void SetCheckNeeded( string protocol, bool check )
        {
            IResource resProtocol = Core.ResourceStore.FindUniqueResource( PROTOCOL_HANDLER, _propProtocol, protocol );
            if ( resProtocol != null )
            {
                ResourceProxy proxy = new ResourceProxy( resProtocol );
                proxy.BeginUpdate();
                proxy.AsyncPriority = JobPriority.Immediate;
                proxy.SetProp( _propCheck, check );
                proxy.EndUpdate();
            }
        }
        public bool IsCheckNeeded( string protocol )
        {
            IResource resProtocol = Core.ResourceStore.FindUniqueResource( PROTOCOL_HANDLER, _propProtocol, protocol );
            if ( resProtocol != null )
            {
                return resProtocol.HasProp( _propCheck );
            }
            return false;
        }
        public void SaveProtocolSettings( IResource resource, string friendlyName, bool defaultProtocol )
        {
            CheckRegistration();
            ResourceProxy proxy = new ResourceProxy( resource );
            proxy.BeginUpdate();
            SaveProtocolSettings( proxy, friendlyName, defaultProtocol ? Default.Yes : Default.No );
        }

        private static void SaveProtocolSettings( ResourceProxy proxy, string friendlyName, Default defProtocol )
        {
            CheckRegistration();
            proxy.AsyncPriority = JobPriority.Immediate;
            proxy.SetProp( _propFriendlyName, friendlyName );
            if ( defProtocol != Default.NoChanges )
            {
                proxy.SetProp( _propDefault, defProtocol == Default.Yes );
            }
            proxy.EndUpdate();
            if ( proxy.Resource.HasProp( _propDefault ) )
            {
               ProtocolHandlersInRegistry.SetAsDefaultHandler( proxy.Resource.GetPropText( _propProtocol ), friendlyName );
            }
        }

        protected static void SaveProtocolSettings( string protocol, string friendlyName, Default defProtocol )
        {
            CheckRegistration();
            IResource resProtocol = Core.ResourceStore.FindUniqueResource( PROTOCOL_HANDLER, _propProtocol, protocol );
            ResourceProxy proxy = null;
            if ( resProtocol == null )
            {
                proxy = ResourceProxy.BeginNewResource( PROTOCOL_HANDLER );
                proxy.SetProp( _propCheck, true );
            }
            else
            {
                proxy = new ResourceProxy( resProtocol );
                proxy.BeginUpdate();
            }
            proxy.SetProp( _propProtocol, protocol );
            SaveProtocolSettings( proxy, friendlyName, defProtocol );
        }
    }
}
