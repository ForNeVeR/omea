/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#pragma once

#include "typefactory.h"

class ESPropValue : public RCObject
{
private: 
    LPSPropValue _pProp;
    bool _isFreeNecessary;
    static int GetIDs( LPMAPIPROP lpProp, LPMAPINAMEID lpNmid, int propType );
public:
    ESPropValue( LPSPropValue pProp, bool isFreeNecessary = true );
    virtual ~ESPropValue();
    LPSTR GetLPSTR( int index = 0 );
    int GetLong( int index = 0 );
    bool GetBool( int index = 0 );
    int GetBinCB( int index = 0 );
    LPBYTE GetBinLPBYTE( int index = 0 );
    _FILETIME GetFILETIME( int index = 0 );
    SLPSTRArray GetMVszA( int index = 0 );
    SBinaryArray GetMVbin( int index = 0 );
    static void SetSimpleProp( LPMAPIPROP lpProp, LPSPropValue lpPropValue );
    static void DeleteSimpleProp( LPMAPIPROP lpProp, int tag );
    static ESPropValueSPtr GetSimpleProp( LPMAPIPROP lpProp, int tag );
    static int GetIDsFromNames( LPMAPIPROP lpProp, LPGUID lpGUID, LPWSTR name, int propType );
    static int GetIDsFromNames( LPMAPIPROP lpProp, LPGUID lpGUID, int lID, int propType );
};

