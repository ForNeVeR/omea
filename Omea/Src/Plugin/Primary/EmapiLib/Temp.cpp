/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

#include "MAPISession.h"
#define long __int32
#include "emapilib.h"
#include "temp.h"
#include "typeFactory.h"
#include "CharBuffer.h"
#include "StringStream.h"
#include "EMessage.h"
#include "MsgStore.h"
#include "StringConvertion.h"
using namespace System::Text;
using namespace System::Runtime::InteropServices;

ANSIStringSPtr Temp::GetANSIString( String* str )
{
    if ( str == NULL )
    {
        return ANSIStringSPtr( new ANSIString( NULL ) );
    }
    return ANSIStringSPtr( new ANSIString( GetLPSTR( str ) ));
}
UNIStringSPtr Temp::GetUNIString( String* str )
{
    if ( str == NULL )
    {
        return UNIStringSPtr( new UNIString( NULL ) );
    }
    return UNIStringSPtr( new UNIString( GetLPWSTR( str ) ));
}

LPSTR Temp::GetLPSTR( String* str )
{
    if ( str == NULL )
    {
        return NULL;
    }
    return static_cast<LPSTR>(static_cast<void*>(Marshal::StringToCoTaskMemAnsi(str)));
}
LPWSTR Temp::GetLPWSTR( String* str )
{
    if ( str == NULL )
    {
        return NULL;
    }
    return static_cast<LPWSTR>(static_cast<void*>(Marshal::StringToCoTaskMemUni(str)));
}

void Temp::SetANSIString( ANSIString* ansi, String* str )
{
    ansi->operator =( Temp::GetLPSTR( str ) );
}

void Temp::AddRecipients( const EMessageSPtr& msg, const MsgStoreSPtr& msgStore, ArrayList* recipients, int recType )
{
    if ( recipients != NULL )
    {
        for ( int i = 0; i < recipients->Count; i++ )
        {
            EMAPILib::RecipInfo* recipInfo = 
                (dynamic_cast<EMAPILib::RecipInfo*>( recipients->get_Item( i ) ));

            msgStore->AddRecipient( msg, 
                Temp::GetUNIString( recipInfo->get_DisplayName() )->GetChars(),
                Temp::GetUNIString( recipInfo->get_Email() )->GetChars(), 
                Temp::GetANSIString( recipInfo->get_DisplayName() )->GetChars(),
                Temp::GetANSIString( recipInfo->get_Email() )->GetChars(), 
                recType );
        }
    }
}

void Temp::AttachFiles( const EMessageSPtr& msg, ArrayList* attachments )
{
    if ( attachments != NULL )
    {
        for ( int i = 0; i < attachments->Count; i++ )
        {
            EMAPILib::AttachInfo* attachInfo = 
                (dynamic_cast<EMAPILib::AttachInfo*>( attachments->get_Item( i ) ));

            msg->AttachFile( 
                Temp::GetANSIString( attachInfo->get_Path() )->GetChars(), 
                Temp::GetANSIString( attachInfo->get_FileName() )->GetChars() );
        }
    }
}

EMAPILib::MessageBody* Temp::GetRawBodyAsRTF( const EMessageSPtr& msg )
{
    int cpid = msg->GetInternetCPID();

    StringBuilder* rtfBody = new StringBuilder();

    msg->RTFSyncBody();

    StringStreamSPtr streamComp = msg->openStreamProperty( (int)PR_RTF_COMPRESSED );
    if ( !streamComp.IsNull() )
    {
        StringStreamSPtr stream = streamComp->GetWrapCompressedRTFStream();

        if ( !stream.IsNull() )
        {
            bool bodyIsRead = !stream->Read();
            StringStream::Format format = stream->GetStreamFormat();
            if ( format == StringStream::Format::HTML )
            {
                CharBufferSPtr prHTML = msg->openStringProperty( (int)0x10130102 );//PR_HTML
                if ( !prHTML.IsNull() )
                {
                    int charCount = prHTML->Length();
                    unsigned char destination __gc[] = new unsigned char __gc[charCount];
                    Helper::MarshalCopy( (byte*)prHTML->GetRawChars(), destination, 0, charCount );
                    Encoding* enc = Encoding::GetEncoding( cpid );
                    rtfBody->Append( enc->GetString( destination ) );
                    return new EMAPILib::MessageBody( rtfBody->ToString(), EMAPILib::MailBodyFormat::HTML, msg->GetInternetCPID() );
                }
            }
            if ( !bodyIsRead )
            {
                stream->ReadToEnd();
            }

            if ( format == StringStream::Format::HTML )
            {
                int realcpid = stream->GetRealCodePage( );
                if ( realcpid != 0 && realcpid != 1 )
                {
                    cpid = realcpid;
                }

                CharBufferSPtr buffer = stream->DecodeRTF2HTML();
                unsigned char destination __gc[] = new unsigned char __gc[buffer->Length()];
                Helper::MarshalCopy( (byte*)buffer->GetRawChars(), destination, 0, buffer->Length() );
                Encoding* enc = Encoding::GetEncoding( cpid );
                String* result = enc->GetString( destination );
                rtfBody->Append( result );
                return new EMAPILib::MessageBody( rtfBody->ToString(), EMAPILib::MailBodyFormat::HTML, cpid );
            }

            CharBufferSPtr buffer = stream->GetBuffer();
            String* str = new String( buffer->GetRawChars(), 0, buffer->Length() );

            if ( format == StringStream::Format::PlainText )
            {
                return new EMAPILib::MessageBody( str, EMAPILib::MailBodyFormat::PlainTextInRTF, cpid );
            }
            if ( format == StringStream::Format::RTF )
            {
                return new EMAPILib::MessageBody( str, EMAPILib::MailBodyFormat::RTF, cpid );
            }
        }
    }
    {
        Encoding* enc = Encoding::GetEncoding( cpid );

        StringStreamSPtr bodyStream = msg->openStreamProperty( (int)0x10130102 );
        if ( !bodyStream.IsNull() )
        {
            bodyStream->ReadToEnd();

            CharBufferSPtr buffer = bodyStream->GetBuffer();
            int charCount = buffer->Length();
            unsigned char destination __gc[] = new unsigned char __gc[charCount];
            Helper::MarshalCopy( (byte*)buffer->GetRawChars(), destination, 0, charCount );
            return new EMAPILib::MessageBody( enc->GetString( destination ), EMAPILib::MailBodyFormat::HTML, cpid );
        }
        bodyStream = msg->openStreamProperty( (int)0x1013001E );
        if ( !bodyStream.IsNull() )
        {
            bodyStream->ReadToEnd();

            CharBufferSPtr buffer = bodyStream->GetBuffer();
            int charCount = buffer->Length();
            unsigned char destination __gc[] = new unsigned char __gc[charCount];
            Helper::MarshalCopy( (byte*)buffer->GetRawChars(), destination, 0, charCount );
            return new EMAPILib::MessageBody( enc->GetString( destination ), EMAPILib::MailBodyFormat::HTML, cpid );
        }
        bodyStream = msg->openStreamProperty( (int)PR_BODY );
        if ( !bodyStream.IsNull() )
        {
            bodyStream->ReadToEnd();
            CharBufferSPtr buffer = bodyStream->GetBuffer();
            return new EMAPILib::MessageBody( buffer->GetRawChars(), EMAPILib::MailBodyFormat::PlainText, cpid );
        }
    }

    return new EMAPILib::MessageBody( String::Empty, EMAPILib::MailBodyFormat::PlainText, cpid );
}
