// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

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

ANSIStringSPtr Temp::GetANSIString( String ^str )
{
    if ( str == nullptr )
    {
        return ANSIStringSPtr( new ANSIString( NULL ) );
    }
    return ANSIStringSPtr( new ANSIString( GetLPSTR( str ) ));
}
UNIStringSPtr Temp::GetUNIString( String ^str )
{
    if ( str == nullptr )
    {
        return UNIStringSPtr( new UNIString( NULL ) );
    }
    return UNIStringSPtr( new UNIString( GetLPWSTR( str ) ));
}

LPSTR Temp::GetLPSTR( String ^str )
{
    if ( str == nullptr )
    {
        return NULL;
    }
    return static_cast<LPSTR>(static_cast<void*>(Marshal::StringToCoTaskMemAnsi(str)));
}
LPWSTR Temp::GetLPWSTR( String ^str )
{
    if ( str == nullptr )
    {
        return NULL;
    }
    return static_cast<LPWSTR>(static_cast<void*>(Marshal::StringToCoTaskMemUni(str)));
}

void Temp::SetANSIString( ANSIString* ansi, String ^str )
{
    ansi->operator =( Temp::GetLPSTR( str ) );
}

void Temp::AddRecipients( const EMessageSPtr& msg, const MsgStoreSPtr& msgStore, ArrayList ^recipients, int recType )
{
    if ( recipients != nullptr )
    {
        for ( int i = 0; i < recipients->Count; i++ )
        {
            EMAPILib::RecipInfo ^recipInfo =
                (dynamic_cast<EMAPILib::RecipInfo^>( recipients[i] ));

            msgStore->AddRecipient( msg,
                Temp::GetUNIString( recipInfo->DisplayName )->GetChars(),
                Temp::GetUNIString( recipInfo->Email )->GetChars(),
                Temp::GetANSIString( recipInfo->DisplayName )->GetChars(),
                Temp::GetANSIString( recipInfo->Email )->GetChars(),
                recType );
        }
    }
}

void Temp::AttachFiles( const EMessageSPtr& msg, ArrayList ^attachments )
{
    if ( attachments != nullptr )
    {
        for ( int i = 0; i < attachments->Count; i++ )
        {
            EMAPILib::AttachInfo ^attachInfo =
                (dynamic_cast<EMAPILib::AttachInfo^>( attachments[i] ));

            msg->AttachFile(
                Temp::GetANSIString( attachInfo->Path )->GetChars(),
                Temp::GetANSIString( attachInfo->FileName )->GetChars() );
        }
    }
}

EMAPILib::MessageBody ^Temp::GetRawBodyAsRTF( const EMessageSPtr& msg )
{
    int cpid = msg->GetInternetCPID();

    StringBuilder ^rtfBody = gcnew StringBuilder();

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
                    array<unsigned char> ^destination = gcnew array<unsigned char>(charCount);
                    Helper::MarshalCopy( (byte*)prHTML->GetRawChars(), destination, 0, charCount );
                    Encoding ^enc = Encoding::GetEncoding( cpid );
                    rtfBody->Append( enc->GetString( destination ) );
                    return gcnew EMAPILib::MessageBody( rtfBody->ToString(), EMAPILib::MailBodyFormat::HTML, msg->GetInternetCPID() );
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
                array<unsigned char> ^destination = gcnew array<unsigned char>(buffer->Length());
                Helper::MarshalCopy( (byte*)buffer->GetRawChars(), destination, 0, buffer->Length() );
                Encoding ^enc = Encoding::GetEncoding( cpid );
                String ^result = enc->GetString( destination );
                rtfBody->Append( result );
                return gcnew EMAPILib::MessageBody( rtfBody->ToString(), EMAPILib::MailBodyFormat::HTML, cpid );
            }

            CharBufferSPtr buffer = stream->GetBuffer();
            String ^str = gcnew String( buffer->GetRawChars(), 0, buffer->Length() );

            if ( format == StringStream::Format::PlainText )
            {
                return gcnew EMAPILib::MessageBody( str, EMAPILib::MailBodyFormat::PlainTextInRTF, cpid );
            }
            if ( format == StringStream::Format::RTF )
            {
                return gcnew EMAPILib::MessageBody( str, EMAPILib::MailBodyFormat::RTF, cpid );
            }
        }
    }
    {
        Encoding ^enc = Encoding::GetEncoding( cpid );

        StringStreamSPtr bodyStream = msg->openStreamProperty( (int)0x10130102 );
        if ( !bodyStream.IsNull() )
        {
            bodyStream->ReadToEnd();

            CharBufferSPtr buffer = bodyStream->GetBuffer();
            int charCount = buffer->Length();
            array<unsigned char> ^destination = gcnew array<unsigned char>(charCount);
            Helper::MarshalCopy( (byte*)buffer->GetRawChars(), destination, 0, charCount );
            return gcnew EMAPILib::MessageBody( enc->GetString( destination ), EMAPILib::MailBodyFormat::HTML, cpid );
        }
        bodyStream = msg->openStreamProperty( (int)0x1013001E );
        if ( !bodyStream.IsNull() )
        {
            bodyStream->ReadToEnd();

            CharBufferSPtr buffer = bodyStream->GetBuffer();
            int charCount = buffer->Length();
            array<unsigned char> ^destination = gcnew array<unsigned char>(charCount);
            Helper::MarshalCopy( (byte*)buffer->GetRawChars(), destination, 0, charCount );
            return gcnew EMAPILib::MessageBody( enc->GetString( destination ), EMAPILib::MailBodyFormat::HTML, cpid );
        }
        bodyStream = msg->openStreamProperty( (int)PR_BODY );
        if ( !bodyStream.IsNull() )
        {
            bodyStream->ReadToEnd();
            CharBufferSPtr buffer = bodyStream->GetBuffer();
            return gcnew EMAPILib::MessageBody(gcnew String(buffer->GetRawChars()), EMAPILib::MailBodyFormat::PlainText, cpid );
        }
    }

    return gcnew EMAPILib::MessageBody( String::Empty, EMAPILib::MailBodyFormat::PlainText, cpid );
}
