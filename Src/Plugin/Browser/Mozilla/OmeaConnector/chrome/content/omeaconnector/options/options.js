// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

function omeaconnector_options_load()
{
  var engine = Components.classes["@jetbrains.com/omeaconnector;1"].getService(Components.interfaces.nsIOmeaConnector);

  var t;

  t = document.getElementById( 'omeaconnector-options-timerset' );
  t.checked = engine.prop_queue_timer_set;

  t = document.getElementById( 'omeaconnector-options-askonexit' );
  t.checked = engine.prop_queue_askonexit;

  t = document.getElementById( 'omeaconnector-options-pending-radiogroup' );
  switch( engine.prop_queue_mode )
  {
  default:
  case Components.interfaces.nsIOmeaConnector.QM_STOREONLY:
    t.selectedIndex = 0;
    break;
  case Components.interfaces.nsIOmeaConnector.QM_RUNOMEA:
    t.selectedIndex = 1;
    break;
  }
  omeaconnector_options_methodChanged( t.value );
}

function omeaconnector_options_apply()
{
  var engine = Components.classes["@jetbrains.com/omeaconnector;1"].getService(Components.interfaces.nsIOmeaConnector);
  var t;

  t = document.getElementById( 'omeaconnector-options-timerset' );
  engine.prop_queue_timer_set = t.checked;

  t = document.getElementById( 'omeaconnector-options-askonexit' );
  engine.prop_queue_askonexit = t.checked;

  t = document.getElementById( 'omeaconnector-options-pending-radiogroup' );
  switch( t.value )
  {
  case 'store':
    engine.prop_queue_mode = Components.interfaces.nsIOmeaConnector.QM_STOREONLY;
    break;
  case 'run':
    engine.prop_queue_mode = Components.interfaces.nsIOmeaConnector.QM_RUNOMEA;
    break;
  default:
    break;
  }
}

function omeaconnector_options_enableControl( id, state )
{
  var c = document.getElementById( id );
  if( ! c )
  {
    return;
  }
  if( state )
  {
    c.removeAttribute( 'disabled' );
  }
  else
  {
    c.setAttribute( 'disabled', true );
  }
}

function omeaconnector_options_methodChanged( method )
{
  var t;
  omeaconnector_options_enableControl( 'omeaconnector-options-timerset',  false );
  omeaconnector_options_enableControl( 'omeaconnector-options-askonexit', false );
  switch( method )
  {
  case 'store':
    omeaconnector_options_enableControl( 'omeaconnector-options-timerset',  true );
    omeaconnector_options_enableControl( 'omeaconnector-options-askonexit', true );
    break;
  case 'run':
    break;
  default:
    break;
  }
}
