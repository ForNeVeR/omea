/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#pragma once

#include "typefactory.h"

class PropTagArray : public RCObject
{
private:
    LPSPropTagArray _propTags;
public:
    PropTagArray( LPSPropTagArray propTags );
    virtual ~PropTagArray();
    int GetCount() const;
    int GetTag( int index ) const;
};

class MAPIProp : public RCObject
{
protected:
    LPMAPIPROP _mapiProp;
public:
    MAPIProp( LPMAPIPROP mapiProp );
    virtual ~MAPIProp();

    StringStreamSPtr openStreamPropertyToWrite( int tag ) const;
    void writeStringStreamProp( int tag, LPSTR propValue, int size ) const;
    StringStreamSPtr openStreamProperty( int tag ) const;
    CharBufferSPtr openStringProperty( int tag ) const;
    CharBufferSPtr openStringProperty( int tag, int sizeToRead ) const;

    void setSimpleProp( LPSPropValue lpPropValue ) const;
    void setStringProp( int tag, LPSTR lpStr ) const;
    void setLongProp( int tag, int value ) const;
    void setBoolProp( int tag, BOOL value ) const;
    void setDateTimeProp( int tag, ULONGLONG value ) const;
    void setStringArray( int tag, LPSTR* lppsz, int count ) const;

    ESPropValueSPtr getSingleProp( int tag ) const;
    PropTagArraySPtr getPropList() const;

    void deleteSimpleProp( int tag ) const;

    int getIDsFromNames( LPGUID lpGUID, LPWSTR name, int propType ) const;
    int getIDsFromNames( LPGUID lpGUID, int lID, int propType ) const;

    HRESULT SaveChanges( int flags ) const;
    void CopyTo( LPCIID lpInterface, LPVOID lpDestObj ) const;

private:
    void setFILETIME( _FILETIME* ft, ULONGLONG value ) const;
};
