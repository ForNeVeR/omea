// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma once

#include "helpers.h"
#include "typefactory.h"
#using <mscorlib.dll>
using namespace System;
using namespace System::Collections;

class MAPISession;
class ANSIString;

class Temp
{
public:
    static LPSTR GetLPSTR( String* str );
    static LPWSTR GetLPWSTR( String* str );
    static ANSIStringSPtr GetANSIString( String* str );
    static void SetANSIString( ANSIString* ansi, String* str );
    static UNIStringSPtr GetUNIString( String* str );
    static EMAPILib::MessageBody* GetRawBodyAsRTF( const EMessageSPtr& msg );
    static void AttachFiles( const EMessageSPtr& msg, ArrayList* attachments );
    static void AddRecipients( const EMessageSPtr& msg, const MsgStoreSPtr& msgStore, ArrayList* recipients, int recType );
};
