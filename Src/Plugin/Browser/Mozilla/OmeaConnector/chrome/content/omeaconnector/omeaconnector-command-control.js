// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

function OmeaConnectorCommandController() { return this; }

OmeaConnectorCommandController.prototype =
{
  _commands:
  {
    omeaconnector_subscribeRSS          : new OmeaConnectorCmdSubscribeRSS(),
    omeaconnector_createAndEditClipping : new OmeaConnectorCmdCreateClipping( false ),
    omeaconnector_createAndSaveClipping : new OmeaConnectorCmdCreateClipping( true ),
    omeaconnector_annotateURL           : new OmeaConnectorCmdAnnotateURL(),
    '' : undefined
  },

  supportsCommand: function( cmd )
  {
    return ( cmd in this._commands );
  },

  doCommand: function( cmd )
  {
    if( ! this.supportsCommand( cmd ) )
      return false;
    this._commands[ cmd ].exec();
  },

  isCommandEnabled: function( cmd )
  {
    if( ! this.supportsCommand( cmd ) )
      return true;
    return omeaconnector_Engine.omeaInstalled && this._commands[ cmd ].isSupported() && this._commands[ cmd ].isEnabled();
  },

  isCommandSupported: function( cmd )
  {
    if( ! this.supportsCommand( cmd ) )
      return true;
    return this._commands[ cmd ].isSupported();
  },

  onEvent: function( event )
  {
  }

}

var omeaconnector_Controller =  new OmeaConnectorCommandController();
window.controllers.appendController( omeaconnector_Controller );

function omeaconnector_doCommand( cmd )
{
  var disp = window.document.commandDispatcher;
  var ctrl = disp.getControllerForCommand( cmd );
  ctrl.doCommand( cmd );
}

function omeaconnector_updateCommand( cmd )
{
  var disp = window.document.commandDispatcher;
  var ctrl = disp.getControllerForCommand( cmd );
  var target = document.getElementById( cmd );

  // We called for some strange command?!
  if( ! target || ! ctrl )
  {
    return;
  }

  if( omeaconnector_Controller.isCommandSupported( cmd ) )
  {
    target.removeAttribute( 'hidden' );
    if( ctrl.isCommandEnabled( cmd ) )
    {
      target.removeAttribute( 'disabled' );
    }
    else
    {
      target.setAttribute( 'disabled', 'true' );
    }
  }
  else
  {
    target.setAttribute( 'hidden', 'true' );
  }
}

function omeaconnector_updateAll()
{
  omeaconnector_updateCommand( 'omeaconnector_subscribeRSS'          );
  omeaconnector_updateCommand( 'omeaconnector_createAndEditClipping' );
  omeaconnector_updateCommand( 'omeaconnector_createAndSaveClipping' );
  omeaconnector_updateCommand( 'omeaconnector_annotateURL'           );
  return true;
}
