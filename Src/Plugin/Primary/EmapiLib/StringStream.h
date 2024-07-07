// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once

#include "typefactory.h"

class StringStream : public RCObject
{
public:
    enum Format
    {
        PlainText,
        RTF,
        HTML,
    };

private:
    LPSTREAM _lpStream;
    static const int BUFF_SIZE = 1024;
    int _buff_size;
    CharsStorageSPtr _charsStorage;
public:
    StringStream( LPSTREAM lpStream );
    StringStreamSPtr GetWrapCompressedRTFStream( int flags = 0 );
    Format GetStreamFormat();
    CharBufferSPtr Html2Rtf( char* buffer );
    bool Read();
    bool Read( int sizeToRead );
    void ReadToEnd();
    int GetRealCodePage( );
    CharBufferSPtr GetBuffer( );
    CharBufferSPtr DecodeRTF2HTML();
    bool Write( const void* pv, int cb );
    void Commit();
    virtual ~StringStream();
private:
    void DecodeRTF2HTMLInternal( char *buf, unsigned int *len );
    bool IsPlain( const char* buf, unsigned int len );
    bool IsHtml( const char* buf, unsigned int len );
};
