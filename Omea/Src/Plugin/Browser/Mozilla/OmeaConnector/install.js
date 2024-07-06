// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

const author              = "@AUTHOR@";
const displayName         = "@DISPLAYNAME@";
const name                = "@NAME@";
const jarName             = name + ".jar";
const existsInApplication = File.exists( getFolder( getFolder("chrome"), jarName ) );
const version             = "@VERSION@";

var jarFolder   = getFolder( "Current User", "chrome" );
var xpcomFolder = getFolder( "components" );
var contentFlag = CONTENT;
var localeFlag  = LOCALE;
var skinFlag    = SKIN;

const existsInProfile = File.exists( getFolder( jarFolder, jarName ) );

if( existsInApplication ||
    ( ! existsInProfile &&
      ! confirm( "Do you want to install the " + displayName + " extension into your profile folder?\n(Cancel will install into the application folder)")
    )
  )
{
  jarFolder    = getFolder( "chrome" );
  contentFlag |= DELAYED_CHROME;
  localeFlag  |= DELAYED_CHROME;
  skinFlag    |= DELAYED_CHROME;
}
else
{
  contentFlag |= PROFILE_CHROME;
  localeFlag  |= PROFILE_CHROME;
  skinFlag    |= PROFILE_CHROME;
}

var error = null;
try
{
  error = initInstall( displayName, name, version );
  if( SUCCESS != error ) throw 0;

  error = setPackageFolder( jarFolder );
  if( SUCCESS != error ) throw 0;
  error = addFile( name, version, "chrome/" + jarName, jarFolder, null );
  if( SUCCESS != error ) throw 0;

  error = setPackageFolder( xpcomFolder );
  if( SUCCESS != error ) throw 0;
  error = addFile( name, version, "components/nsIOmeaConnector.xpt", xpcomFolder, null );
  if( SUCCESS != error ) throw 0;
  error = addFile( name, version, "components/nsOmeaConnector.js",   xpcomFolder, null );
  if( SUCCESS != error ) throw 0;
}
catch( ex )
{
}

// If adding the JAR file succeeded
if( SUCCESS == error )
{
  jarFolder = getFolder( jarFolder, jarName );

  registerChrome(contentFlag, jarFolder, "content/"      + name + "/");
  registerChrome(localeFlag,  jarFolder, "locale/en-US/" + name + "/");
  registerChrome(skinFlag,    jarFolder, "skin/classic/" + name + "/");

  error = performInstall();

  if( SUCCESS != error && 999 != error && -239 != error )
  {
    displayError( error );
    cancelInstall( error );
  }
  else
  {
    alert( "The installation of the " + displayName + " extension succeeded." );
  }
}
else
{
    displayError( error );
    cancelInstall( error );
}

// Displays the error message to the user
function displayError( error )
{
  if( -215 == error )
  {
    alert( "The installation of the " + displayName + " extension failed.\nOne of the files being overwritten is read-only." );
  }
  else if( -235 == error )
  {
    alert( "The installation of the " + displayName + " extension failed.\nThere is insufficient disk space." );
  }
  else
  {
    alert( "The installation of the " + displayName + " extension failed.\nThe error code is: " + error );
  }
}
