// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

const cContractID  = '@jetbrains.com/omeaconnector;1';
const cCID         = Components.ID('{50dfb942-b1e0-49f7-81e2-b2d0596e5efd}');
const cIID         = Components.interfaces.nsIOmeaConnector;
const cDescription = 'OmeaConnector Component. (c) JetBrains s.r.o.';

const cRegKey  = "Software\\JetBrains\\Omea"
const cPortVal = "ControlPortAsString";
const cSecVal  = "ControlProtection"
const cRunVal  = "ControlRun"

const cIThread = Components.interfaces.nsIThread;

const cNC_NS = 'http://home.netscape.com/NC-rdf#';

const cBookmarkProps = {
  URL  : cNC_NS + 'URL',
  Name : cNC_NS + 'Name'
}

const cBookmarksRoot = 'NC:BookmarksRoot';

const REQ_ONESHOT = 0x00; // Don't try to repeat at all
const REQ_REPEAT  = 0x01; // Store in queue on fail
const REQ_SINGLE  = 0x02; // Merge all on fail
const REQ_FIRST   = 0x40; // Should go first
const REQ_REPORT  = 0x80; // Report failure
const REQ_MASK    = 0x0F;

function OmeaConnector()
{
  this._init();
  return this;
}

OmeaConnector.prototype =
{
///////////////////////////////////////////////////////////////////////////////
// nsISupports
  QueryInterface: function (iid)
  {
    if( ! iid.equals( cIID )                                   &&
        ! iid.equals( Components.interfaces.nsITimerCallback ) &&
        ! iid.equals( Components.interfaces.nsIObserver )      &&
        ! iid.equals( Components.interfaces.nsIRDFObserver )   &&
        ! iid.equals( Components.interfaces.nsISupports )      )
      throw Components.results.NS_ERROR_NO_INTERFACE;
    return this;
  },

///////////////////////////////////////////////////////////////////////////////
// Names of properties (settings)
  PROP__BASE           : "omeaconnector.",
  PROP_QUEUE_MODE      : "queue.mode",
  PROP_QUEUE_TIMER_SET : "queue.timer.set",
  PROP_QUEUE_ASKONEXIT : "queue.askonexit",
  PROP_CLIPPING_MODE   : "clip.mode",

///////////////////////////////////////////////////////////////////////////////
// Attributes for nsIOmeaConnector

  _prop_queue_mode : cIID.QM_STOREONLY,
  get prop_queue_mode() { return this._prop_queue_mode; },
  set prop_queue_mode( val )
  {
    if( ! ( val instanceof Number || typeof( val ) == 'number' ) )
    {
      return;
    }
    if( val != cIID.QM_STOREONLY && val != cIID.QM_RUNOMEA )
    {
      return;
    }
    if( this._prop_queue_mode != val )
    {
      this._prop_queue_mode = val;
      this._getPropStore().setIntPref( this.PROP_QUEUE_MODE, val );
    }
  },

  _prop_queue_timer_set : true,
  get prop_queue_timer_set() { return this._prop_queue_timer_set; },
  set prop_queue_timer_set( val )
  {
    val = val ? true : false;
    if( this._prop_queue_timer_set != val )
    {
      this._prop_queue_timer_set = val;
      this._getPropStore().setBoolPref( this.PROP_QUEUE_TIMER_SET, val );
    }
  },

  _prop_queue_askonexit : true,
  get prop_queue_askonexit() { return this._prop_queue_askonexit; },
  set prop_queue_askonexit( val )
  {
    val = val ? true : false;
    if( this._prop_queue_askonexit != val )
    {
      this._prop_queue_askonexit = val;
      this._getPropStore().setBoolPref( this.PROP_QUEUE_ASKONEXIT, val );
    }
  },

  _prop_clipping_mode : cIID.CM_EDIT,
  get prop_clipping_mode() { return this._prop_clipping_mode; },
  set prop_clipping_mode( val )
  {
    if( ! ( val instanceof Number || typeof( val ) == 'number' ) )
    {
      return;
    }
    if( val != cIID.CM_EDIT && val != cIID.CM_SAVE )
    {
      return;
    }
    if( this._prop_clipping_mode != val )
    {
      this._prop_clipping_mode = val;
      this._getPropStore().setIntPref( this.PROP_CLIPPING_MODE, val );
    }
  },

  get omeaInstalled() { return this._omeaInstalled; },

  get firstRun()
  {
    var first = true;
    try
    {
      first = this._getPropStore().getBoolPref( this._PROP_FIRST_RUN );
    }
    catch( ex )
    {
    }
    this._getPropStore().setBoolPref( this._PROP_FIRST_RUN, false );
    return first;
  },

  _PROP_FIRST_RUN: 'first_run',
  _PROP_FOUND_METHODS: 'methods.lastseen',

///////////////////////////////////////////////////////////////////////////////
// Public methods of nsIOmeaConnector

  loadSettings: function()
  {
    var needSave = false;

    try
    {
      this._prop_queue_mode = this._getPropStore().getIntPref( this.PROP_QUEUE_MODE );
    }
    catch( ex )
    {
      needSave = true;
    }

    try
    {
      this._prop_queue_timer_set = this._getPropStore().getBoolPref( this.PROP_QUEUE_TIMER_SET );
    }
    catch( ex )
    {
      needSave = true;
    }

    try
    {
      this._prop_queue_askonexit = this._getPropStore().getBoolPref( this.PROP_QUEUE_ASKONEXIT );
    }
    catch( ex )
    {
      needSave = true;
    }

    try
    {
      this._prop_clipping_mode = this._getPropStore().getIntPref( this.PROP_CLIPPING_MODE );
    }
    catch( ex )
    {
      needSave = true;
    }


    // Hidden ones
    try
    {
      this._timerSecondsNorm = this._getPropStore().getIntPref( this._PROP_TIMER_SECONDS_NORM );
    }
    catch( ex )
    {
      needSave = true;
    }
    try
    {
      this._timerSecondsRun = this._getPropStore().getIntPref( this._PROP_TIMER_SECONDS_RUN );
    }
    catch( ex )
    {
      needSave = true;
    }

    if( needSave )
    {
      this.storeSettings();
    }
  },

  storeSettings: function()
  {
    try
    {
      this._getPropStore().setIntPref ( this.PROP_QUEUE_MODE,      this._prop_queue_mode );
      this._getPropStore().setBoolPref( this.PROP_QUEUE_TIMER_SET, this._prop_queue_timer_set );
      this._getPropStore().setBoolPref( this.PROP_QUEUE_ASKONEXIT, this._prop_queue_askonexit );
      this._getPropStore().setIntPref ( this.PROP_CLIPPING_MODE,   this._prop_clipping_mode );
      // Hidden one
      this._getPropStore().setIntPref ( this._PROP_TIMER_SECONDS_NORM, this._timerSecondsNorm );
      this._getPropStore().setIntPref ( this._PROP_TIMER_SECONDS_RUN,  this._timerSecondsRun );
    }
    catch( ex )
    {
      this._console( "Prefs saving exception: '", ex, "'\'n" );
    }
  },

  methodIsSupported: function( method )
  {
    if( null == this._supportedMethods )
    {
      // Not known yet :(
      return true;
    }
    return      null != this._supportedMethods[method] &&
           undefined != this._supportedMethods[method] &&
                        this._supportedMethods[method]   ;
  },

  subscribeRSS:   function( url )
  {
    if( ! this._omeaInstalled )
    {
      return;
    }
    this._requestRemoteAPI(
      new RemoteCall( "RSSPlugin.SubscribeToFeed.1", this._marshallRequest( 'url', url ), REQ_REPEAT | REQ_REPORT )
    );
  },
  isSubscribeRSSSupported : function() { return this.methodIsSupported( "RSSPlugin.SubscribeToFeed.1" ); },

  createClipping: function( silently, subject, text, url )
  {
    if( ! this._omeaInstalled )
    {
      return;
    }

    var method = "Omea.CreateClipping.1";
    var callback = null;
    if( silently )
    {
      var self = this;
      callback = function() { self._showAlert( 'ClippingCreated' ) };
      method = "Omea.CreateClippingSilent.1";
    }
    this._requestRemoteAPI(
      new RemoteCall( method,
        this._marshallRequest( 'subject', subject, 'text', text, 'sourceUrl', url ),
        REQ_REPEAT | REQ_REPORT, callback
      )
    );
  },
  isCreateClippingSupported : function() { return this.methodIsSupported( "Omea.CreateClipping.1" ); },

  annotateURL: function( url, title )
  {
    if( ! this._omeaInstalled )
    {
      return;
    }
    var type  = ( this.prop_queue_mode == cIID.QM_RUNOMEA ) ? REQ_REPEAT : REQ_ONESHOT;
    type |= REQ_REPORT;
    this._requestRemoteAPI(
      new RemoteCall( "Favorites.AnnotateWeblink.1", this._marshallRequest( 'url', url, 'title', title ), type )
    );
  },
  isAnnotateURLSupported : function() { return this.methodIsSupported( "Favorites.AnnotateWeblink.1" ); },

  importBookmarks: function( browser )
  {
    if( ! this._omeaInstalled )
    {
      return;
    }
    if( this._Bookmarks == null )
    {
      this._prepareBookmarksEngine();
      if( ! this._Bookmarks )
      {
        return;
      }
    }
    var self = this;
    this._requestRemoteAPI(
      new RemoteCall( "Favorites.ExportMozillaBookmarkChanges.1",
        this._marshallRequest( 'profilePath', this._getProfileName() ),
        REQ_SINGLE,
        function( obj ) { self._importBookmarksCallback( obj ) }
      )
    );
  },

  _importBookmarksCallback: function( changes )
  {
    // Process changes
    if( changes && 'length' in changes && changes.length > 0 )
    {
      this._console( "Process externally changed bookmarks" );
      this._importInProgress = true;
      for( var i = 0; i < changes.length; ++i )
      {
        this._importOneBookmarkChange( changes[ i ] );
      }
      this._importInProgress = false;
    }
    else
    {
      this._console( "No externally changed bookmarks" );
    }
  },

///////////////////////////////////////////////////////////////////////////////
// Internal data
  _requestBaseURL:     "http://127.0.0.1:%p/%k/xml/",
  _requestContentType: "application/x-www-form-urlencoded",

  _timerSecondsNorm:        60,
  _PROP_TIMER_SECONDS_NORM: 'queue.timer.seconds.norm',

  _timerSecondsRun:         10,
  _PROP_TIMER_SECONDS_RUN:  'queue.timer.seconds.run',

  _timerSecondsCurr:        60,
///////////////////////////////////////////////////////////////////////////////
// Internal methods

  _init: function()
  {
    this._queue = new JobQueue();

    this._propStore        = null;
    this._omeaInstalled    = true;
    this._supportedMethods = null;

    this._omeaStartInProgress = false;

    this._queueTimer       = null;
    this._bookmarksTimer   = null;

    this._observerService  = Components.classes["@mozilla.org/observer-service;1"].getService( Components.interfaces.nsIObserverService );
    this._observerService.addObserver( this, "quit-application-requested",  false );
    this._observerService.addObserver( this, "omeaconnector:changes-ready", false );

    var eventQSvc  = Components.classes["@mozilla.org/event-queue-service;1"].getService( Components.interfaces.nsIEventQueueService );
    this._UIQueue  = eventQSvc.getSpecialEventQueue( Components.interfaces.nsIEventQueueService.UI_THREAD_EVENT_QUEUE );
    this._ProxyMgr = Components.classes["@mozilla.org/xpcomproxy;1"].getService( Components.interfaces.nsIProxyObjectManager );

    this._readReg = function( val ) { return null; };

    // DeepPark AKA FireFox 1.1
    if( "@mozilla.org/windows-registry-key;1" in Components.classes )
    {
      try
      {
        var key = Components.classes["@mozilla.org/windows-registry-key;1"].createInstance( Components.interfaces.nsIWindowsRegKey );
        key.open( Components.interfaces.nsIWindowsRegKey.ROOT_KEY_CURRENT_USER, cRegKey, Components.interfaces.nsIWindowsRegKey.ACCESS_READ );
        this._readReg = function( val )
          {
            try
            {
              var type = key.getValueType( val );
              switch( type )
              {
                case Components.interfaces.nsIWindowsRegKey.TYPE_STRING:
                  return key.readStringValue( val );
                case Components.interfaces.nsIWindowsRegKey.TYPE_INT:
                  return key.readIntValue( val );
                default:
                  return null;
              }
            }
            catch( ex )
            {
              return;
            }
          };
      }
      catch(ex)
      {
        this._console( "Can not get registry key: '", ex, "'" );
      }
    }
    else
    {
      var hooks = null;
      // Mozilla 1.x
      if( "@mozilla.org/winhooks;1" in Components.classes )
      {
        try
        {
          hooks = Components.classes["@mozilla.org/winhooks;1"].getService(Components.interfaces.nsIWindowsRegistry);
        }
        catch(ex)
        {
          this._console( "Can not get winhooks: '", ex, "'" );
        }
      }
      // FireFox 1.0.x
      else if( "@mozilla.org/browser/shell-service;1" in Components.classes )
      {
        try
        {
          hooks = Components.classes["@mozilla.org/browser/shell-service;1"].getService(Components.interfaces.nsIWindowsShellService);
        }
        catch(ex)
        {
          this._console( "Can not get shell service: '", ex, "'" );
        }
      }
      else
      {
          this._console( "No registry access service, use default value for port" );
      }
      if( hooks != null && typeof( hooks.getRegistryEntry ) == "function" )
      {
        this._readReg = function( val ) { return hooks.getRegistryEntry( hooks.HKCU, cRegKey, val ); };
      }
      else
      {
        this._console( "No registry access function, use default value for port" );
      }
    }

    this._prepareBookmarksEngine();

    this.loadSettings();
    this._scheduleStart();
    this._pumpQueue();
  },

  _prepareBookmarksEngine: function()
  {
    // Get profile manager
    this._dirManager = Components.classes["@mozilla.org/file/directory_service;1"].getService(Components.interfaces.nsIProperties);

    // Bookmarks service
    this._BookmarksSvc = Components.classes[ "@mozilla.org/browser/bookmarks-service;1" ].getService( Components.interfaces.nsIBookmarksService );
    // Read bookmarks :)
    this._BookmarksSvc.readBookmarks();

    // Simple RDF
    this._RDF          = Components.classes[ "@mozilla.org/rdf/rdf-service;1" ].getService( Components.interfaces.nsIRDFService );
    // Bookmarks datasource
    this._Bookmarks    = Components.classes[ "@mozilla.org/rdf/datasource;1?name=bookmarks" ].getService( Components.interfaces.nsIRDFDataSource );
    // Container utils
    this._CUtils       = Components.classes[ "@mozilla.org/rdf/container-utils;1" ].getService( Components.interfaces.nsIRDFContainerUtils );
    // Pre-cashed properties
    this._BProps = {
      Root    : this._RDF.GetResource( cBookmarksRoot       ),
      URL     : this._RDF.GetResource( cBookmarkProps.URL   ),
      Name    : this._RDF.GetResource( cBookmarkProps.Name  ),
    };
    // ID map
    this._bookmarksCreated = new Array();
    // Lock
    this._importInProgress = false;

    this._Bookmarks.AddObserver( this );
  },

  _getProfileName: function()
  {
    var f = this._dirManager.get( "ProfD", Components.interfaces.nsIFile );
    return f.path;
  },

  _getString: function( name )
  {
    if( ! this._stringBundle ) {
      var svc = Components.classes[ "@mozilla.org/intl/stringbundle;1" ].createInstance( Components.interfaces.nsIStringBundleService );
      this._stringBundle = svc.createBundle( "chrome://omeaconnector/locale/omeaconnector.properties" );
    }
    return this._stringBundle.GetStringFromName( name );
  },

  _getPropStore: function()
  {
    if ( ! this._propStore )
    {
      this._propStore = Components.classes["@mozilla.org/preferences-service;1"].getService(Components.interfaces.nsIPrefService).getBranch( this.PROP__BASE );
    }
    return this._propStore;
  },

  _requestRemoteAPI: function( rpc )
  {
    this._queue.addJob( rpc );
    this._pumpQueue();
  },

  _pumpQueue: function()
  {
    var rpc = this._queue.getForProcess();
    if( rpc == null )
    {
      this._console( "Try to pump empty queue" );
      return;
    }
    this._console( "Pump ", rpc.method, " from queue (count: ", this._queue.count, ")" );
    // And get current security key
    var sk = this._readReg( cSecVal );
    // And get current port
    var port = this._readReg( cPortVal );
    if( ! port || ! sk ) {
      this._omeaInstalled = false;
      return;
    }

    this._console( "Port: ", port, ", Secure key: ", sk );

    // Create URL
    var baseURL   = this._requestBaseURL.replace( '%p' , port ).replace( '%k', sk );
    var reqURL    = baseURL + rpc.method;

    // We have call here
    var self = this;
    var req  = Components.classes["@mozilla.org/xmlextras/xmlhttprequest;1"].createInstance(Components.interfaces.nsIXMLHttpRequest);
    req.open( 'POST', reqURL, true );
    req.setRequestHeader( "Content-Type", this._requestContentType );
    req.onreadystatechange = function() { self._RequestCallback( req, rpc ) };
    try
    {
      req.send( rpc.params );
    }
    catch( ex )
    {
      this._console( "Async send failed: '", ex.message, "'\n" );
      this._processFailedRequest( rpc );
    }
  },

  _RequestCallback: function( req, rpc )
  {
    if( req.readyState != 4 ) // If not complete, drop this
    {
      return;
    }

    try
    {
      var ready = req.status == 200;
    }
    catch( ex )
    {
      this._console( "Async failure with '", rpc.method, "': '", ex.message, "'" );
      rpc.error( true );
      this._processFailedRequest( rpc );
      return;
    }

    if( req.status == 200 )
    {
      res = req.responseXML;
      this._console( "Success with '", rpc.method, "'" );
    }
    else if ( req.status == 403 )
    {
      this._console( "Too early for '", rpc.method, "'" );
      this._processFailedRequest( rpc );
      return;
    }
    else
    {
      this._console( "Problems with '", rpc.method, "': ", req.status, " ", req.statusText, "'" );
      // Problems with this method. Disable it.
      this._supportedMethods[ new String( rpc.method ) ] = false;
      if( rpc.report )
      {
        this._reportFailedRequest( false );
      }
      // Don't exit! We need to pump queue!
    }

    // Stop timer!
    if( this._queueTimer != null )
    {
      this._queueTimer.cancel();
      this._queueTimer = null;
    }

    try
    {
      if( res == null )
      {
        throw "Empty answer XML";
      }

      var node = res.firstChild;
      if( Components.interfaces.nsIDOMNode.ELEMENT_NODE != node.nodeType || 'result' != node.nodeName )
      {
        throw "Invalid answer structure: '" + node.nodeName + "' instead of 'result'";
      }
      if( ! node.hasAttributes( 'status' ) )
      {
        throw "Invalid answer structure: no status";
      }
      var status = node.getAttribute('status');
      if( status == 'exception' )
      {
        // <result status="exception"> <!-- node -->
        //   <struct name='exception'>
        //    <string name="message">MESSAGE</string>
        //    ...
        //   </struct>
        // </result>
        var message = node.firstChild.getElementsByTagName( 'string' )[0].firstChild;
        throw "Server exception: '" + message + "'";
      }
      // Find retval
      var found = false;
      for( var c = 0; c < node.childNodes.length; ++c )
      {
        if( Components.interfaces.nsIDOMNode.ELEMENT_NODE == node.childNodes[c].nodeType &&
            node.childNodes[c].hasAttribute( 'name' ) &&
            node.childNodes[c].getAttribute( 'name' ) == 'retval' )
        {
          try
          {
            var obj = this._demarshallObjectFromXML( node.childNodes[c] );
            // If obj is null replace with Object();
            if( null == obj )
            {
              obj = new Object();
            }
            // Call provided callback with result!
            try
            {
              rpc.success( obj );
            }
            catch( ex )
            {
              this._console( "Custom callback exception: '", ex, "'" );
            }
          }
          catch( ex )
          {
            throw "Invalid answer structure: '" + ex + "'";
          }
          found = true;
          break;
        }
      }
      if( ! found )
      {
        throw "Invalid answer structure: no 'retval'-named element";
      }
    }
    catch( ex )
    {
      this._console( "Problems with answer parsing: '", ex, "'" );
      // Really failed!
      if( rpc.report )
      {
        this._reportFailedRequest( false );
      }
    }
    // Ok, now we should continue job pumping
    this._omeaStartInProgress = false;
    this._queue.markAsSuccess( rpc );
    this._pumpQueue();
  },

  _processFailedRequest: function( rpc )
  {
    this._console( "Process failed request" );

    // Report all requests in queue
    var self = this;
    this._queue.reportAllRequests( function( store) { self._reportFailedRequest( store ); } );
    // Mark in queue
    this._queue.markAsFailed( rpc );

    // Add new call to begin of queue
    this._scheduleStart();

    // Process queue change
    if( this._queue.hasRunnable && this.prop_queue_mode == cIID.QM_RUNOMEA )
    {
      this._console( "Start queue timer for queue processing & run Omea" );
      this._runOmea();
      this._setTimer( true, this._timerSecondsRun ); // Force
    }
    else
    {
      if( ! this._queueTimer )
      {
        this._console( "Start queue timer for queue processing" );
        this._setTimer( false, this._timerSecondsNorm );
      }
    }
  },

  _marshallRequest: function()
  {
    var r = "";
    for(var i = 0; i < arguments.length; i += 2)
    {
      var obj = this._marshallObjectAsForm( arguments[i], arguments[i+1] );
      if( r.length && obj.length )
      {
        r += '&';
      }
      r += obj;
    }
    return r;
  },

  _marshallObjectAsForm: function( name, val )
  {
    if( val == null || val == undefined )
    {
      return "";
    }
    else if( val instanceof String || typeof( val ) == 'string' ||
             val instanceof Number || typeof( val ) == 'number' )
    {
      return name + '=' + encodeURIComponent( val );
    }
    else if( val instanceof Boolean || typeof( val ) == 'boolean')
    {
      return name + '=' + ( val ? 1 : 0 );
    }
    else if( val instanceof Object )
    {
      var res = "";
      for( var prop in val )
      {
        var obj = this._marshallObjectAsForm( name + "." + prop, val[prop] );
        if( res.length && obj.length)
        {
          res += "&";
        }
        res += obj;
      }
      return res;
    }
    return "";
  },

  _scheduleStart : function()
  {
    var self = this;
    var gam = new RemoteCall( 'System.ListAllMethods', '', REQ_SINGLE | REQ_FIRST,
      function( obj ) { self._getAllMethodsSuccess( obj ) },
      function()      { self._getAllMethodsError() }
    );
    this._queue.addJob( gam );
  },

  _getAllMethodsSuccess : function( arr )
  {
    var newList = new Object();
    // always present
    newList[ "System.ListAllMethods" ] = true;

    if( ! ( arr instanceof Array ) )
    {
      return;
    }
    for( var i = 0; i < arr.length; ++i )
    {
      newList[ new String( arr[i] ) ] = true;
    }
    // Store new variant
    this._supportedMethods = newList;
    // Save this variant
    var prop = "";
    for( var method in newList )
    {
      if( prop ) prop += ' ';
      prop += method;
    }
    try { this._getPropStore().setCharPref( this._PROP_FOUND_METHODS, prop ); } catch(ex) {}
  },

  _getAllMethodsError : function( arr )
  {
    this._console( "getAllMethods failed" );
    // Try to get persistent list;
    try
    {
      var newList = new Object();
      // always present
      newList[ "System.ListAllMethods" ] = true;

      var prop = this._getPropStore().getCharPref( this._PROP_FOUND_METHODS );
      if( ! prop )
      {
        return;
      }
      var arr = prop.split( " " );
      for( var i = 0; i < arr.length; ++i )
      {
        newList[ new String( arr[i] ) ] = true;
      }
      this._supportedMethods = newList;
    }
    catch( ex ) {}
  },

  _demarshallObjectFromXML: function( xmlObj )
  {
    if( Components.interfaces.nsIDOMNode.ELEMENT_NODE != xmlObj.nodeType )
    {
      throw "Unknown node type";
    }

    if( xmlObj.nodeName == 'string' )
    {
      var str = xmlObj.firstChild;
      if( Components.interfaces.nsIDOMNode.TEXT_NODE != str.nodeType )
      {
        throw "String data is not a text";
      }
      // Remove spaces
      return str.data.replace(/^\s+/,'').replace(/\s+$/,'');
    }
    else if( xmlObj.nodeName == 'int' )
    {
      var intg = xmlObj.firstChild;
      if( Components.interfaces.nsIDOMNode.TEXT_NODE != intg.nodeType )
      {
        throw "Number data is not a text";
      }
      return (new Number(intg.data)).valueOf();
    }
    else if( xmlObj.nodeName == 'bool' )
    {
      var bool = xmlObj.firstChild;
      if( Components.interfaces.nsIDOMNode.TEXT_NODE != bool.nodeType )
      {
        throw "Boolean data is not a text";
      }
      return bool.data != "0";
    }
    else if( xmlObj.nodeName == 'struct' )
    {
      var fields = xmlObj.childNodes;
      var res = new Object();
      for( var f = 0; f < fields.length; ++f )
      {
        if( Components.interfaces.nsIDOMNode.ELEMENT_NODE == fields[f].nodeType && fields[f].hasAttribute( "name" ) )
        {
          res[fields[f].getAttribute( "name" )] = this._demarshallObjectFromXML( fields[f] );
        }
      }
      return res;
    }
    else if( xmlObj.nodeName == 'array' )
    {
      var fields = xmlObj.childNodes;
      var res = new Array();
      for( var f = 0; f < fields.length; ++f )
      {
        if( Components.interfaces.nsIDOMNode.ELEMENT_NODE == fields[f].nodeType )
        {
          res.push( this._demarshallObjectFromXML( fields[f] ) );
        }
      }
      return res;
    }
    else if( xmlObj.nodeName == 'void' )
    {
      // It is OK: really, VOID
      return null;
    }
    throw "Unknown type: '" + xmlObj.nodeName + "'";
  },

  _pingOmea: function()
  {
    // timer fired
    this._queueTimer = null;
    try
    {
      this._pumpQueue();
    }
    catch(ex)
    {
      this._console( "Ping exception: '", ex, "', start queue timer." );
      this._setTimer( false, this._timerSecondsCurr );
      return false;
    }
    return true;
  },

  _runOmea: function()
  {
    if( this._omeaStartInProgress )
    {
      return;
    }
    this._omeaStartInProgress = true;
    // Get executable path
    var path = this._readReg( cRunVal );
    if( ! path )
    {
      this._console( "Can not load executable path." );
      return false;
    }
    // Convert to "file"
    var exe = Components.classes[ "@mozilla.org/file/local;1" ].createInstance( Components.interfaces.nsILocalFile );
    exe.initWithPath( path );
    if( ! exe.isExecutable() )
    {
      this._console( "Executable '", path, "' is not executable." );
      return false;
    }
    if( exe.isDirectory() )
    {
      this._console( "Executable '", path, "' is directory." );
      return false;
    }
    try
    {
      // Get runner
      var runner = Components.classes[ "@mozilla.org/process/util;1" ].createInstance( Components.interfaces.nsIProcess );
      runner.init( exe );
      // Start Omea sync
      this._runAsync( function() { runner.run( false, "", 0 ); } );
    }
    catch( ex )
    {
      this._console( "Spawn error: '", ex, "'." );
      return false;
    }
    return true;
  },

  _setTimer: function( force, time )
  {
    if( ! this.prop_queue_timer_set && ! force )
    {
      this._console( "Timer disabled." );
      return;
    }
    this._console( "Set queue timer for ", time, " seconds." );
    if( this._queueTimer )
    {
      this._queueTimer.cancel();
      this._queueTimer = null;
    }
    else
    {
      this._queueTimer = Components.classes[ "@mozilla.org/timer;1" ].createInstance( Components.interfaces.nsITimer );
    }
    this._timerSecondsCurr = time;
    this._queueTimer.initWithCallback( this, time * 1000, Components.interfaces.nsITimer.TYPE_ONE_SHOT );
  },

  _reportFailedRequest : function( stored )
  {
    if( stored )
    {
      if( this.prop_queue_mode != cIID.QM_RUNOMEA )
      {
        this._showAlert( 'RequestQueued' );
      }
      else
      {
        this._showAlert( 'OmeaStarted' );
      }
    }
    else
    {
      this._showAlert( 'RequestFailed' );
    }
  },

  _showAlert: function( alertName )
  {
    var svc = Components.classes["@mozilla.org/alerts-service;1"].getService( Components.interfaces.nsIAlertsService );
    if( svc )
    {
      svc.showAlertNotification( "chrome://omeaconnector/content/omea-logo-48.png",
                                 this._getString( alertName + 'Title' ),
                                 this._getString( alertName + 'Message' ),
                                 false, "", null );
    }
  },

///////////////////////////////////////////////////////////////////////////////
// BOOKMARKS
///////////////////////////////////////////////////////////////////////////////
  _importOneBookmarkChange : function( change )
  {
    // First of all, check command
    if( ! ( 'type' in change ) )
    {
      this._console( "Invalid bookmark change: no type" );
      return;
    }
    var save = false;
    switch( change.type )
    {
    // Created or changed
    case 0:
      // Created or changed?
      if( this._getBookmarkResource( change.rdfid, change.id ) )
      {
        this._console( "Bookmark changed" );
        save = this._importBookmarkChange( change );
      }
      else
      {
        this._console( "Bookmark Added" );
        save = this._importBookmarkAdd( change );
      }
      break;
    // Deleted
    case 1:
      this._console( "Bookmark Removed" );
      save = this._importBookmarkDel( change );
      break;
    // Moved to other folder
    case 2:
      this._console( "Bookmark Moved" );
      save = this._importBookmarkMove( change );
      break;
    default:
      this._console( "Invalid bookmark change: unknown type '", change.type, "'" );
      return;
    }
    if( save )
    {
      var rds = this._Bookmarks.QueryInterface(Components.interfaces.nsIRDFRemoteDataSource);
      if( rds )
      {
        try { rds.Flush(); } catch( ex ) { this._console( "Can not save bookmarks changes: '", ex, "'" ); }
      }
      else
      {
        this._console( "Can not save bookmarks changes" );
      }
    }
  },

  _importBookmarkChange: function( change )
  {
    var res = null;
    var save = false;
    // We have 'rdfid' of this resource. Simple.
    res = this._getBookmarkResource( change.rdfid, change.id );
    if( ! res )
    {
      this._console( "Can not get resource for change by id '", change.rdfid, "'" );
      return false;
    }
    // Is it URL or folder?
    if( 'url' in change && change.url )
    {
      // Bookmark changed
      save |= this._updateBookmarkProp( res, this._BProps.URL,  change.url );
      save |= this._updateBookmarkProp( res, this._BProps.Name, ( 'name' in change ) ? change.name : '' );
    }
    else
    {
      // Folder changed
      save |= this._updateBookmarkProp( res, this._BProps.Name, ( 'name' in change ) ? change.name : '' );
    }
    return save;
  },

  _importBookmarkAdd: function( change )
  {
    var res = null;
    var save = false;
    var parent = this._getBookmarkParent( change.parent, change.parent_id );
    if( ! parent )
    {
      this._console( "Can not get parent for new bookmark by id '", change.parent, "'/'", change.parent_id, "'" );
      return false;
    }

    // Ok, create new one
    if( 'url' in change && change.url )
    {
      try
      {
        res = this._BookmarksSvc.createBookmarkInContainer( change.name, change.url, null, null , null, parent, -1 );
      }
      catch( ex )
      {
        // Firefox? Not enough parameters?!
        res = this._BookmarksSvc.createBookmarkInContainer( change.name, change.url, null, null , null, null, parent, -1 );
      }

    }
    else
    {
      res = this._BookmarksSvc.createFolderInContainer( change.name, parent, -1 );
    }

    // Store ID for future (may be) use
    this._bookmarksCreated[ change.id ] = res.Value;
    // Send new ID
    this._sendBkmID( change.id, res.Value );
    return true;
  },

  _importBookmarkDel: function( change )
  {
    var res = null;

    var parent = this._getBookmarkParent( change.parent, change.parent_id );
    if( ! parent )
    {
      this._console( "Can not get parent for removed bookmark by id '", change.parent, "' / '", change.parent_id, "'" );
      return false;
    }

    res = this._getBookmarkResource( change.rdfid, change.id );
    if( ! res )
    {
      this._console( "Can not get resource for remove by id '", change.rdfid, "'/'", change.id, "'" );
      return false;
    }
    // Check, is it container (folder)?
    if( this._CUtils.IsSeq( this._Bookmarks, res ) )
    {
      this._RDFDeleteAllChilds( res );
    }
    // Cool
    var container = Components.classes[ "@mozilla.org/rdf/container;1" ].createInstance( Components.interfaces.nsIRDFContainer );
    container.Init( this._Bookmarks, parent );
    container.RemoveElement( res, true );
    return true;
  },

  _importBookmarkMove: function( change )
  {
    var res = null;

    var oldParent = this._getBookmarkParent( change.oldparent, change.oldparent_id );
    if( ! oldParent )
    {
      this._console( "Can not get old parent for new bookmark by id '", change.oldparent, "'/'", change.oldparent_id, "'" );
      return false;
    }

    var newParent = this._getBookmarkParent( change.parent, change.parent_id );
    if( ! oldParent )
    {
      this._console( "Can not get new parent for new bookmark by id '", change.parent, "'/'", change.parent_id, "'" );
      return false;
    }

    res = this._getBookmarkResource( change.rdfid, change.id );
    if( ! res )
    {
      this._console( "Can not get resource for move by id '", change.rdfid, "'" );
      return false;
    }

    // Ok, move!
    var container = Components.classes[ "@mozilla.org/rdf/container;1" ].createInstance( Components.interfaces.nsIRDFContainer );
    container.Init( this._Bookmarks, oldParent );
    container.RemoveElement( res, true );
    container.Init( this._Bookmarks, newParent );
    container.AppendElement( res );
    return true;
  },

  _updateBookmarkProp: function( res, prop, newVal )
  {
    var oldVal = null;
    try { oldVal = this._Bookmarks.GetTarget( res, prop, true ).QueryInterface( Components.interfaces.nsIRDFLiteral ); } catch(ex) {};
    if     ( oldVal && newVal && oldVal.Value != newVal )
    {
      var nvl = this._RDF.GetLiteral( newVal );
      this._Bookmarks.Change( res, prop, oldVal, nvl );
      return true;
    }
    else if( oldVal && ! newVal )
    {
      this._Bookmarks.Unassert( res, prop, oldVal );
      return true;
    }
    else if( ! oldVal && newVal )
    {
      var nvl = this._RDF.GetLiteral( newVal );
      this._Bookmarks.Assert( res, prop, nvl, true );
      return true;
    }
    return false;
  },

  _getBookmarkResource: function( rdfid, id )
  {
    var res = null;
    if( rdfid )
    {
      try { res = this._RDF.GetResource( rdfid ); } catch(ex) {}
      if( ! res )
      {
        this._console( "Can not get resource from store" );
        return null;
      }
    }
    else
    {
      if( ! ( id in this._bookmarksCreated ) )
      {
        this._console( "Resource is unknown to me" );
        return null;
      }
      try { res = this._RDF.GetResource( this._bookmarksCreated[ id ] ); } catch(ex) {}
      if( ! res )
      {
        this._console( "Can not get resource '", this._bookmarksCreated[ id ], "' from store" );
        return null;
      }
      this._sendBkmID( id, res.Value );
    }
    return res;
  },

  _getBookmarkParent: function( rdfid, id )
  {
    var parent = null;
    if( rdfid )
    {
      if( rdfid == 'root' )
      {
        parent = this._BProps.Root;
      }
      else
      {
        try { parent = this._RDF.GetResource( rdfid ); } catch(ex) {}
        if( ! parent )
        {
          this._console( "Can not get parent from store" );
          return null;
        }
      }
    }
    else
    {
      if( ! ( id in this._bookmarksCreated ) )
      {
        this._console( "Parent is unknown to me" );
        return null;
      }
      try { parent = this._RDF.GetResource( this._bookmarksCreated[ id ] ); } catch(ex) {}
      if( ! parent )
      {
        this._console( "Can not get parent '", this._bookmarksCreated[ id ], "' from store" );
        return null;
      }
      // Submit rdfid
      this._sendBkmID( id, parent.Value );
    }
    // check: is parent container?
    if( ! this._CUtils.IsSeq( this._Bookmarks, parent ) )
    {
      this._console( "Parent is not a sequence" );
      return null;
    }
    return parent;
  },

  _RDFDeleteAllChilds: function( res )
  {
    if( ! this._CUtils.IsSeq( this._Bookmarks, res ) )
    {
      return;
    }
    var container = Components.classes[ "@mozilla.org/rdf/container;1" ].createInstance( Components.interfaces.nsIRDFContainer );
    container.Init( this._Bookmarks, res );
    var children = container.GetElements();
    while( children.hasMoreElements() )
    {
      var child = children.getNext().QueryInterface( Components.interfaces.nsIRDFNode );
      if( this._CUtils.IsSeq( this._Bookmarks, child ) )
      {
        this._RDFDeleteAllChilds( child );
      }
      container.RemoveElement( child, false );
    }
  },

  _exportBookmarks: function( timed )
  {
    if( ! this._omeaInstalled || ! this.methodIsSupported( 'Favorites.RefreshMozillaBookmarks.1' ) )
    {
      return;
    }
    if( timed )
    {
      this._console( "Really export bookmarks" );
      if( this._bookmarksTimer )
      {
        this._console( "Really export bookmarks by timer" );
        this._bookmarksTimer.cancel();
        this._bookmarksTimer = null;
      }
      this._requestRemoteAPI(
        new RemoteCall( 'Favorites.RefreshMozillaBookmarks.1',
          this._marshallRequest( 'profilePath', this._getProfileName() ),
          REQ_ONESHOT )
      );
    }
    else
    {
      // Is timer already set
      if( this._bookmarksTimer )
      {
        this._console( "Reset export timer" );
        this._bookmarksTimer.cancel();
      }
      else
      {
        this._console( "Set export timer" );
        this._bookmarksTimer = Components.classes[ "@mozilla.org/timer;1" ].createInstance( Components.interfaces.nsITimer );
      }
      this._bookmarksTimer.initWithCallback( this, 2000, Components.interfaces.nsITimer.TYPE_ONE_SHOT );
    }
  },

  _sendBkmID: function( oid, rid )
  {
    this._requestRemoteAPI(
      new RemoteCall( 'Favorites.SetMozillaBookmarkId.1', this._marshallRequest( 'idres', oid, 'rdfid', rid ), REQ_REPEAT )
    );
  },

///////////////////////////////////////////////////////////////////////////////
// Other ifaces
///////////////////////////////////////////////////////////////////////////////

///////////////////////////////////////////////////////////////////////////////
// nsIRDFObserver
  onAssert: function( dataSource, source, property, target )
  {
    if( this._importInProgress )
    {
      return;
    }
    this._exportBookmarks( false );
  },

  onBeginUpdateBatch: function( dataSource )
  {
    if( this._importInProgress )
    {
      return;
    }
    this._exportBookmarks( false );
  },

  onChange: function( dataSource, source, property, oldTarget, newTarget )
  {
    if( this._importInProgress )
    {
      return;
    }
    this._exportBookmarks( false );
  },

  onEndUpdateBatch: function( dataSource )
  {
    if( this._importInProgress )
    {
      return;
    }
    this._exportBookmarks( false );
  },

  onMove: function( dataSource, oldSource, newSource, property, target)
  {
    if( this._importInProgress )
    {
      return;
    }
    this._exportBookmarks( false );
  },

  onUnassert: function( dataSource, source, property, target )
  {
    if( this._importInProgress )
    {
      return;
    }
    this._exportBookmarks( false );
  },

///////////////////////////////////////////////////////////////////////////////
// nsITimerCallback
  notify: function( timer )
  {
    if( timer == this._queueTimer )
    {
      this._pingOmea();
      return;
    }
    if( timer == this._bookmarksTimer )
    {
      this._exportBookmarks( true );
      return;
    }
    this._console( "Strange timer notify us" );
  },

///////////////////////////////////////////////////////////////////////////////
// nsIObserver
   observe: function( subject, topic, data )
   {
     if( topic == "quit-application-requested" )
     {
       var subj = null;
       if( subject )
       {
         // Ask for boolean interface, it will be not asked by default...
         subj = subject.QueryInterface( Components.interfaces.nsISupportsPRBool );
       }

       // Maybe, already canceled?
       if( subj && subj.data )
       {
         return;
       }
       // Does we have queue?
       if( this.prop_queue_askonexit && this._queue.hasReportable )
       {
         // Try to get subject as boolean

         // Notify user about queue
         var IPS = Components.interfaces.nsIPromptService;
         var ps = Components.classes["@mozilla.org/embedcomp/prompt-service;1"].getService( IPS );

         var ask  = { value: true };
         var type =    ( subj ? 'Confirm' : 'Warn' );
         var buttons = ( subj ?
                         ( IPS.BUTTON_TITLE_YES * IPS.BUTTON_POS_0 ) + ( IPS.BUTTON_TITLE_NO  * IPS.BUTTON_POS_1 ) :
                           IPS.BUTTON_TITLE_OK * IPS.BUTTON_POS_0
                       );
         var rv = ps.confirmEx( null,
                                this._getString( 'Exit' + type + 'Title' ),
                                this._getString( 'Exit' + type + 'Message' ),
                                buttons,
                                null, null, null,
                                this._getString( 'Exit' + type + 'Checkbox' ),
                                ask );
         if( subj )
         {
           subj.data = ( 1 == rv );
         }
         this.prop_queue_askonexit = ask.value;
       }
     }
   },

   _runAsync: function( func )
   {
     var runnable = this._normalizeRunnable( func );
     if( ! runnable )
     {
       return;
     }
     // Ok, create thread
     var thread = Components.classes["@mozilla.org/thread;1"].createInstance( cIThread );
     thread.init( runnable, 0, cIThread.PRIORITY_NORMAL, cIThread.SCOPE_GLOBAL, cIThread.STATE_UNJOINABLE );
   },

   _normalizeRunnable: function( func )
   {
     var runnable = null;
     // Normalize argument
     if( typeof( func ) != 'function' && typeof( func ) == 'object' && 'run' in func && typeof( func.run ) == 'function' )
     {
       runnable = func;
     }
     else if( typeof( func ) == 'function' )
     {
       runnable = {
          QueryInterface: function (iid)
          {
            if( ! iid.equals( Components.interfaces.nsIRunnable ) &&
                ! iid.equals( Components.interfaces.nsISupports ) )
              throw Components.results.NS_ERROR_NO_INTERFACE;
            return this;
          },
          // nsIRunnable
          run : func
       };
     }
     return runnable;
   },
///////////////////////////////////////////////////////////////////////////////
// Debug
   _log: function( str )
   {
     var file = Components.classes["@mozilla.org/file/directory_service;1"].getService(Components.interfaces.nsIProperties).get("ProfD", Components.interfaces.nsIFile);

     //put in forecastfox subdirectory
     file.append("nsOmeaConnector.log");
     if( ! file.exists() )
       file.create(file.NORMAL_FILE_TYPE, 0664);

     //make sure file is writeable
     if( ! file.isWritable() )
       file.permissions = 0644;

     var flags = 0x02 | 0x08 | 0x10;
     var stream = Components.classes["@mozilla.org/network/file-output-stream;1"].createInstance(Components.interfaces.nsIFileOutputStream);
     stream.init(file, flags, 0664, 0);
     str = '[' + ( new Date() ).toGMTString() + '] ' + str + '\n';
     stream.write(str, str.length);
     stream.close();
     return true;
   },

   _console: function( )
   {
     var time = new Date();
     var time_string = time.getHours() + ":" + time.getMinutes() + ":" + time.getSeconds();
     str = '[OmeaConnector ' + time_string + '] ';
     for( var i = 0; i < arguments.length; ++i )
     {
       if     ( arguments[i] == undefined )
       {
         str += "--UNDEF--";
       }
       else if( arguments[i] == null )
       {
         str += "--NULL--";
       }
       else
       {
         str += arguments[i].toString();
       }
     }
     str += "\n";
     dump( str );
     return true;
   }
}

function JobQueue()
{
  this._head = null;
  this._tail = null;
  return this;
}

JobQueue.prototype =
{
  _head: null,
  _tail: null,
  _count: 0,

  get count() { return this._count; },

  get hasReportable()
  {
    var e = this._head;
    while( e != null )
    {
      if( e.data.report )
      {
        return true;
      }
      e = e.next;
    }
    return false;
  },

  get hasRunnable()
  {
    var e = this._head;
    while( e != null )
    {
      if( e.data.needRun )
      {
        return true;
      }
      e = e.next;
    }
    return false;
  },

  addJob: function( c )
  {
    switch( c.type )
    {
    case REQ_ONESHOT:
    case REQ_REPEAT:
      if( c.first )
      {
        this._add2h( c );
      }
      else
      {
        this._add2t( c );
      }
      break;
    case REQ_SINGLE:
      if( c.first )
      {
        this._add2h_u( c );
      }
      else
      {
        this._add2t_u( c );
      }
      break;
    default:
      break;
    }
  },

  getForProcess: function()
  {
    var e = this._head;
    // Skip all in progress
    while( e != null && e.prog )
    {
      e = e.next;
    }
    if( e == null )
    {
      return null;
    }
    if( e.type == REQ_ONESHOT )
    {
      this._delete( e );
    }
    else
    {
      e.prog = true;
    }
    return e.data;
  },

  markAsSuccess: function( c )
  {
    var e = this._find( c, this._head );
    while( e != null && ! e.prog )
    {
      e = this._find( c, e.next );
    }
    if( e != null )
    {
      this._delete( e );
    }
  },

  markAsFailed: function( c )
  {
    var e = this._find( c, this._head );
    while( e != null && ! e.prog )
    {
      e = this._find( c, e.next );
    }
    if( e != null )
    {
      e.prog = false;
    }
  },

  reportAllRequests: function( reporter )
  {
    var e = this._head;
    // Skip all in progress
    while( e != null )
    {
      if( e.data.report )
      {
        reporter( e.data.type != REQ_ONESHOT );
        e.data.report = false;
      }
      e = e.next;
    }
  },

  _add2t: function( c )
  {
    var e = {
      next: null,
      prog: false,
      data: c
    };
    if( this._tail == null )
    {
      this._tail = this._head = e;
    }
    else
    {
      this._tail.next = e;
      this._tail = e;
    }
    ++this._count;
  },

  _add2t_u: function( c )
  {
    var e = this._head;
    while( e != null )
    {
      if( e.data.method == c.method )
      {
        e.data = c;
        return;
      }
      e = e.next;
    }
    this._add2t( c );
  },

  _add2h: function( c )
  {
    var e = {
      next: null,
      prog: false,
      data: c
    };
    if( this._tail == null )
    {
      this._tail = this._head = e;
    }
    else
    {
      e.next = this._head;
      this._head = e;
    }
    ++this._count;
  },

  _add2h_u: function( c )
  {
    if( this._head != null && this._head.data.method == c.method )
    {
      this._head.data = c;
      return;
    }
    this._add2h( c );
  },

  _find: function( c, e )
  {
    if( e == undefined || e == null )
    {
      e = this._head;
    }
    while( e != null )
    {
      if( e.data == c || ( c.type != REQ_REPEAT && c.method == e.data.method ) )
      {
        break;
      }
      e = e.next;
    }
    return e;
  },

  _delete: function( e )
  {
    if( e == this._head && e != null )
    {
      this._head = e.next;
      if( this._head == null )
      {
        this._tail = null;
      }
      --this._count;
    }
    else
    {
      var p = this._head;
      while( p != null && p.next != e )
      {
        p = p.next;
      }
      if( p != null )
      {
        p.next = p.next.next;
        if( e == this._tail )
        {
          this._tail = p;
        }
        --this._count;
      }
    }
  }
}

function RemoteCall( method, params, type, success, error )
{
  if( type == undefined ||  type == null )
  {
    type = REQ_REPEAT;
  }
  this.method   = method;
  this.params   = params;
  this.type     = type & REQ_MASK;
  this.report   = ( type & REQ_REPORT ) == REQ_REPORT;
  this.first    = ( type & REQ_FIRST  ) == REQ_FIRST;
  this.needRun  = this.report;

  if( success != undefined && success != null )
  {
    this.success = success;
  }
  if( error != undefined && error != null )
  {
    this.error = error;
  }
  return this;
}

RemoteCall.prototype =
{
  success: function( data ) {},
  error: function( stored ) {}
}

var gModule = {

   registerSelf: function( compMgr, fileSpec, location, type )
   {
     compMgr = compMgr.QueryInterface( Components.interfaces.nsIComponentRegistrar );
     compMgr.registerFactoryLocation( cCID,
                                      cDescription,
                                      cContractID,
                                      fileSpec,
                                      location,
                                      type );
   },

  getClassObject: function( compMgr, cid, iid )
  {
    if ( ! cid.equals( cCID ) )
        throw Components.results.NS_ERROR_NO_INTERFACE;

    if ( ! iid.equals( Components.interfaces.nsIFactory ) )
        throw Components.results.NS_ERROR_NOT_IMPLEMENTED;

    return this.Factory;
  },

  canUnload: function( compMgr ) { return true; },

  Factory :
  {
    _instance: null,

    createInstance: function( outer, iid )
    {
      if( outer != null )
          throw Components.results.NS_ERROR_NO_AGGREGATION;
      if( ! iid.equals( cIID )                                   &&
          ! iid.equals( Components.interfaces.nsITimerCallback ) &&
          ! iid.equals( Components.interfaces.nsIObserver )      &&
          ! iid.equals( Components.interfaces.nsIRDFObserver )   &&
          ! iid.equals( Components.interfaces.nsISupports )      )
        throw Components.results.NS_ERROR_INVALID_ARG;
      if( ! this._instance )
      {
        this._instance = new OmeaConnector();
      }
      return this._instance;
    }
  },
}
function NSGetModule(compMgr, fileSpec) { return gModule; }
dump( "[OmeaConnector] Module loaded\n" );
