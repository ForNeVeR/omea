// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

// Make omeaconnector
var omeaconnector_Engine = Components.classes["@jetbrains.com/omeaconnector;1"].getService(Components.interfaces.nsIOmeaConnector);

function OmeaConnectorCmdSubscribeRSS() { return this; }
OmeaConnectorCmdSubscribeRSS.prototype =
{
  exec: function()
  {
    var url = document.getElementById( 'content' ).currentURI.spec;
    try
    {
      omeaconnector_Engine.subscribeRSS( url );
    }
    catch(ex)
    {
    }
  },

  isEnabled: function()
  {
    var browser = document.getElementById( 'content' );
    return this.isSupported() && ( browser.currentURI.schemeIs("http") || browser.currentURI.schemeIs("https") );
  },

  isSupported: function()
  {
    return omeaconnector_Engine.isSubscribeRSSSupported();
  }
}

function OmeaConnectorCmdCreateClipping( silent )
{
  this._silent = silent;
  return this;
}
OmeaConnectorCmdCreateClipping.prototype =
{
  _silent: true,
  _need:   false,

  _cutAllText: function( tree, leaveFrom, leaveTo, so, eo )
  {
    if( Node.TEXT_NODE == tree.nodeType && ! this._need )
    {
      return null;
    }

    // Special case: we enter with element, which is start element
    if( tree == leaveFrom && Node.TEXT_NODE != tree.nodeType )
    {
      // We need everything from this point
      this._need = true;
    }

    // This is element! Clone it & childs!
    var res = tree.cloneNode( false ); // Don't clone tree
    for( var c = 0; c < tree.childNodes.length; ++c )
    {
      var nc = null;
      if     ( tree.childNodes[c] == leaveFrom )
      {
        // It is proper node
        this._need = true;
        nc = leaveFrom.cloneNode( true );
        if( Node.TEXT_NODE == nc.nodeType )
        {
          if ( leaveFrom != leaveTo )
          {
            nc.data = nc.data.substring( so );
          }
          else
          {
            nc.data = nc.data.substring( so, eo );
          }
        }
      }
      if( tree.childNodes[c] == leaveTo )
      {
        this._need = false;
        if( nc == null )
        {
          nc = leaveTo.cloneNode( true );
          if( Node.TEXT_NODE == nc.nodeType )
          {
            nc.data = nc.data.substring( 0, eo );
          }
        }
      }
      // Ok, may be we should clone current node in non-special cases?
      if( ( Node.TEXT_NODE != tree.childNodes[c].nodeType || this._need ) && null == nc )
      {
        nc = this._cutAllText( tree.childNodes[c], leaveFrom, leaveTo, so, eo );
      }
      if( null != nc )
      {
        res.appendChild( nc );
      }
    }
    if( res.childNodes.length == 0 && ! this._need)
    {
      return null;
    }

    if( tree == leaveTo && Node.TEXT_NODE != tree.nodeType )
    {
      // We don't need anything anymore
      this._need = false;
    }
    return res;
  },

  exec: function()
  {
    var brws    = document.getElementById( 'content' );
    var fw      = document.commandDispatcher.focusedWindow;
    var url     = brws.currentURI.spec;
    var subject   = brws.contentTitle ? brws.contentTitle : brws.currentURI.spec;
    var text      = "";
    var selection = null;

    selection = brws.contentWindow.getSelection();
    if( null == selection || "" == selection.toString() )
    {
      return;
    }

    if( brws.contentDocument.contentType == "text/plain" )
    {
      text = selection.toString();
    }
    else
    {
      var range = selection.getRangeAt( 0 );

      var commonParent = range.commonAncestorContainer;
      if( Node.ELEMENT_NODE != commonParent.nodeType )
      {
        commonParent = commonParent.parentNode;
      }

      this._need = false;
      commonParent = this._cutAllText( commonParent, range.startContainer, range.endContainer, range.startOffset, range.endOffset );
      var serializer = new XMLSerializer();
      text = serializer.serializeToString( commonParent );
    }

    try
    {
      omeaconnector_Engine.createClipping( this._silent, subject, text, url );
    }
    catch(ex)
    {
    }
  },

  isEnabled: function()
  {
    var brws      = document.getElementById( 'content' );
    var selection = brws.contentWindow.getSelection();
    return this.isSupported() && ( null != selection && "" != selection.toString() );
  },

  isSupported: function()
  {
    return omeaconnector_Engine.isCreateClippingSupported();
  }
}

function OmeaConnectorCmdAnnotateURL() { return this; }
OmeaConnectorCmdAnnotateURL.prototype =
{
  exec: function()
  {
    var brws  = document.getElementById( 'content' );
    var url   = brws.currentURI.spec;
    var title = brws.contentTitle ? brws.contentTitle : brws.currentURI.spec;
    try
    {
      omeaconnector_Engine.annotateURL( url, title );
    }
    catch(ex)
    {
    }
  },

  isEnabled: function()
  {
    var browser = document.getElementById( 'content' );
    return this.isSupported() && ( browser.currentURI.schemeIs("http") || browser.currentURI.schemeIs("https") );
  },

  isSupported: function()
  {
    return omeaconnector_Engine.isAnnotateURLSupported();
  }
}
