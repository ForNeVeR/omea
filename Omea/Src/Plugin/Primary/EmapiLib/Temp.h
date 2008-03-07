/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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
