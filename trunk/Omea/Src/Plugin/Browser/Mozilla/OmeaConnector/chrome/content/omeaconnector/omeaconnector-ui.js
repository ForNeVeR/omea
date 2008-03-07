/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

function omeaconnector_initToolbar()
{
  const cAllOurButtons = [ 'subscribeRSS' , 'annotateURL', 'createClipping' ];
  var toolbar = document.getElementById( 'jetbrains-toolbar' );
  if( toolbar && toolbar.insertItem )
  {
    dump( "[OmeaConnector] Init toolbar '" + toolbar + "'\n" );
    for( var i = 0; i < cAllOurButtons.length; ++i )
    {
      var id = 'omeaconnector-' + cAllOurButtons[i] + '-toolbar';
      if( toolbar.currentSet.indexOf( id ) < 0 )
      {
        toolbar.insertItem( id );
      }
    }
    toolbar.setAttribute( "currentset", toolbar.currentSet );
    toolbar.setAttribute( "defaultset", toolbar.currentSet );

    var tb = document.getElementById("navigator-toolbox");
    if( tb && tb.ownerDocument )
    {
      tb.ownerDocument.persist( toolbar.getAttribute( 'id' ), 'currentset' );
    }
  }
}

function omeaconnector_switchCreateClipping( mode, run )
{
  // Set new mode
  var b = document.getElementById( 'omeaconnector-createClipping-toolbar' );
  var name = 'createAnd' + mode + 'Clipping';
  var cmd  = 'omeaconnector_' + name;

  b.setAttribute( 'omeaconnector-mode', mode );
  b.setAttribute( 'label',       b.getAttribute( 'label'       + mode ) );
  b.setAttribute( 'tooltiptext', b.getAttribute( 'tooltiptext' + mode ) );
  b.setAttribute( 'command',  cmd );
  b.setAttribute( 'observes', cmd );
  // And we need to store new mode
  switch( mode )
  {
  case 'Save':
    omeaconnector_Engine.prop_clipping_mode = Components.interfaces.nsIOmeaConnector.CM_SAVE;
    break;
  case 'Edit':
    omeaconnector_Engine.prop_clipping_mode = Components.interfaces.nsIOmeaConnector.CM_EDIT;
    break;
  default:
    dump("[OmeaConnector] Unknown clip mode: '" + mode + "'\n" );
    break;
  }
  // Do command !
  if( run )
  {
    omeaconnector_doCommand( cmd );
  }
}

function omeaconnector_subscribeRSSLink( evt )
{
  omeaconnector_Engine.subscribeRSS( gContextMenu.linkURL() );
}

function omeaconnector_setupContextMenu( evt )
{
  // We alter only contentAreaContextMenu
  if( evt.target.id != "contentAreaContextMenu" )
  {
    return;
  }

  var needSeparator = false;

  // Show/hide creation of clipping
  // Hide both
  gContextMenu.showItem( 'omeaconnector-contextmenu-createAndEditClipping', false );
  gContextMenu.showItem( 'omeaconnector-contextmenu-createAndSaveClipping', false );

  // And show what we need, if we need
  var b = document.getElementById( 'omeaconnector-createClipping-toolbar' );
  var id = 'omeaconnector-contextmenu-createAnd' + b.getAttribute( 'omeaconnector-mode' ) + 'Clipping';
  gContextMenu.showItem( id, gContextMenu.isTextSelected );
  needSeparator |= gContextMenu.isTextSelected;

  /////
  // Simple commands
  var disp = window.document.commandDispatcher;
  var ctrl = disp.getControllerForCommand( 'omeaconnector_subscribeRSS' );
  var haveCmd;
  
  // Show/hide RSS subscription
  haveCmd = ctrl.isCommandEnabled( 'omeaconnector_subscribeRSS' );
  gContextMenu.showItem( 'omeaconnector-contextmenu-subscribeRSS',     haveCmd );
  gContextMenu.showItem( 'omeaconnector-contextmenu-subscribeRSSLink', haveCmd && gContextMenu.onLink );
  needSeparator |= haveCmd;
  // Show/hide Annotation
  haveCmd = ctrl.isCommandEnabled( 'omeaconnector_annotateURL' );
  gContextMenu.showItem( 'omeaconnector-contextmenu-annotateURL', haveCmd );
  needSeparator |= haveCmd;
  // Show/hide separator
  gContextMenu.showItem( 'omeaconnector-contextmenu-separator', needSeparator );
}

function omeaconnector_focused( event )
{
  if( event.eventPhase == Event.BUBBLING_PHASE )
  {
    omeaconnector_Engine.importBookmarks();
  }
}

function omeaconnector_initUI()
{
  window.document.addEventListener( "popupshown", function( event ) { omeaconnector_setupContextMenu( event ); }, true );

  if( omeaconnector_Engine.firstRun )
  {
    omeaconnector_initToolbar();
  }
  switch( omeaconnector_Engine.prop_clipping_mode )
  {
  case Components.interfaces.nsIOmeaConnector.CM_SAVE:
    omeaconnector_switchCreateClipping( 'Save', false );
    break;
  case Components.interfaces.nsIOmeaConnector.CM_EDIT:
    omeaconnector_switchCreateClipping( 'Edit', false );
    break;
  default:
    dump( "[OmeaConnector] Unknown clip mode: " + omeaconnector_Engine.prop_clipping_mode + "\n" );
    break;
  }
  omeaconnector_Engine.importBookmarks();
  window.addEventListener( "focus", function( event ) { omeaconnector_focused( event); }, false );
}
window.addEventListener( "load", function( event ) { omeaconnector_initUI(); }, false );
