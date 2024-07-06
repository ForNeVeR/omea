// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

#pragma unmanaged

#include "CharBuffer.h"
#include "StringStream.h"
#include "CharsStorage.h"
#include "guard.h"
#include "RCPtrDef.h"
#include <string>
#include <sstream>

template RCPtr<StringStream>;

StringStream::StringStream( LPSTREAM lpStream ) : _charsStorage( TypeFactory::CreateCharsStorage() )
{
    _lpStream = lpStream;
    _buff_size = BUFF_SIZE;
    //Debug::WriteLine( "StringStream::StringStream"  );
}
bool StringStream::IsHtml( const char* buf, unsigned int len )
{ // We look for the words "\fromhtml" somewhere in the file.
  // If the rtf encodes text rather than html, then instead
  // it will only find "\fromtext".
  for (const char *c = buf; c < buf + len; c++ )
  {
      if (strncmp(c,"\\from",5)==0)
      {
          return strncmp(c,"\\fromhtml",9)==0;
      }
  }
  return false;
}

bool StringStream::IsPlain( const char* buf, unsigned int len )
{ // We look for the words "\fromhtml" somewhere in the file.
  // If the rtf encodes text rather than html, then instead
  // it will only find "\fromtext".
  for (const char *c = buf; c < buf + len; c++ )
  {
      if (strncmp(c,"\\from",5)==0)
      {
          return strncmp(c,"\\fromtext",9)==0;
      }
  }
  return false;
}

StringStream::Format StringStream::GetStreamFormat()
{

    const char* buf = _charsStorage->GetBuffer( 0 );
    if ( IsHtml( buf, BUFF_SIZE ) )
    {
        return Format::HTML;
    }
    else if ( IsPlain( buf, BUFF_SIZE ) )
    {
        return Format::PlainText;
    }

    return Format::RTF;
}

StringStreamSPtr StringStream::GetWrapCompressedRTFStream( int flags )
{
    LPSTREAM lpStreamRTFSrc = NULL;
    // Get IStream pointer for uncompressed RTF, from which we will read
    HRESULT hr = WrapCompressedRTFStream( _lpStream, flags, &lpStreamRTFSrc );

    if ( hr == S_OK )
    {
        return TypeFactory::CreateStringStream( lpStreamRTFSrc );
    }
    return StringStreamSPtr( NULL );
}

void StringStream::ReadToEnd()
{
    while ( Read() );
}

bool StringStream::Read( int sizeToRead )
{
    _buff_size = sizeToRead;
    return Read();
}

bool StringStream::Read()
{
    ULONG red;
    CharBufferSPtr buf = TypeFactory::CreateCharBuffer( _buff_size );
    HRESULT hr = _lpStream->Read( buf->Get(), _buff_size, &red );
    if ( hr == S_OK && red > 0 )
    {
        buf->SetLength( red );
        _charsStorage->Add( buf );
        if ( red < BUFF_SIZE )
        {
            return false;
        }
        _buff_size += BUFF_SIZE;
        return true;
    }
    return false;
}
bool StringStream::Write( const void* pv, int cb )
{
    ULONG written = 0;
    HRESULT hr = _lpStream->Write( pv, cb, &written );
    if ( hr == S_OK )
    {
        return false;
    }
    return false;
}

void StringStream::Commit()
{
    HRESULT hr = _lpStream->Commit( STGC_OVERWRITE );
    hr = hr;
}

CharBufferSPtr StringStream::GetBuffer( )
{
    return _charsStorage->Concatenate();
}

int StringStream::GetRealCodePage( )
{
    return Guard::GetRealCodePage( _charsStorage );
}

CharBufferSPtr StringStream::DecodeRTF2HTML()
{
    CharBufferSPtr charsBuffer = _charsStorage->Concatenate();
    unsigned int length = charsBuffer->Length();
    DecodeRTF2HTMLInternal( (char*)charsBuffer->GetRawChars(), &length );
    charsBuffer->SetLength( length );
    return charsBuffer;
}
bool Html2Rtf1( const std::string& sHtml, int iCodePage, std::string& sRtf )
 {
   int i, p, j;
   std::string strTag, strExtra;
   char ch;
   const char *pCmp;
   bool bInTag=false;

   sRtf=sHtml;
   if(iCodePage==CP_UTF8) iCodePage=0;
   for(i=(int)sRtf.length()-1; i>=0; --i)
   {
     switch(ch=sRtf[i])
     {
     case '>':
       {
         bInTag=true;
         break;
       }
     case '<':
       {
         bInTag=false;
         break;
       }
     case '\t':
       {
         sRtf.erase(i, 1);
         sRtf.insert(i, "\\tab");
         break;
       }
     case '{':
     case '}':
     case '\\':
       {
         sRtf.insert(i, "\\");
         break;
       }
     case '\r':
       {
         if(
           i<(int)sRtf.length()-1 &&
           sRtf[i+1]=='\n'
           )
           sRtf.insert(i, " ");
         break;
       }
     }
     if( ch & 0x80 )
     {
       //sprintf(sz, "\\'%02X", ch);
       //sRtf.erase(i, 1);
       //sRtf.insert(i, sz );
     }
   }

   for(i=(int)sRtf.length()-2; i>=0; --i)
   {
     if(sRtf[i]=='<')
     {
       p=0;
       for(j=i+1; j<(int)sRtf.length(); ++j)
       {
         if(sRtf[j]=='>')
         {
           p=j;
           break;
         }
       }
       if(!p)
       {
         sRtf=sHtml;
         return false;
       }
       strTag=std::string(sRtf.begin()+i, sRtf.begin()+p+1);
       pCmp=strTag.c_str()+1;
       if(
         !strncmp(pCmp, "P>",  2) ||
         !strncmp(pCmp, "P ",  2) ||
         !strncmp(pCmp, "BR>", 3) ||
         !strncmp(pCmp, "BR ", 3)
         )   strExtra="\\par ";
       else  strExtra="";

       if(strTag[1]=='/') strTag="{\\*\\htmltag8 "+strExtra+strTag+"}";
       else               strTag="{\\*\\htmltag0 "+strExtra+strTag+"}";
       sRtf.erase(i, p-i+1);
       sRtf.insert(i, strTag.c_str());
     }
   }

   sRtf = std::string( "{\\rtf1\\ansi\\ansicpg" ) + "1"
     + "\\fromhtml1\\deff0{\\fonttbl\r\n"
     + "{\\f0\\fswiss\\fcharset"
     + "1"
     + " Arial;}\r\n"
     + "{\\f1\\fmodern\\fcharset"
     + "1"
     + " Courier New;}\r\n"
     + "{\\f2\\fnil\\fcharset"
     + "1"
     + " Symbol;}\r\n"
     + "{\\f3\\fmodern\\fcharset"
     + "1"
     + " Courier New;}}\r\n"
     + "\\uc1\\pard\\plain\\deftab360 \\f0\\fs24\r\n"
     + sRtf
     + "}";
   return true;
 }
CharBufferSPtr StringStream::Html2Rtf(  char* buffer )
{
    std::string strHTML( buffer );
    std::string strRTF;
    Html2Rtf1( strHTML, 1251, strRTF );
    CharBufferSPtr charBuffer = TypeFactory::CreateCharBuffer( (int)strRTF.length() + 1 );
    charBuffer->strcopy( strRTF.c_str() );
    return charBuffer;
}

void StringStream::DecodeRTF2HTMLInternal( char *buf, unsigned int *len )
{ // c -- pointer to where we're reading from
  // d -- pointer to where we're writing to. Invariant: d<c
  // max -- how far we can read from (i.e. to the end of the original rtf)
  // ignore_tag -- stores 'N': after \mhtmlN, we will ignore the subsequent \htmlN.
  char *c=buf, *max=buf+*len, *d=buf; int ignore_tag=-1;
  // First, we skip forwards to the first \htmltag.
  while (c<max && strncmp(c,"{\\*\\htmltag",11)!=0) c++;
  //
  // Now work through the document. Our plan is as follows:
  // * Ignore { and }. These are part of RTF markup.
  // * Ignore \htmlrtf...\htmlrtf0. This is how RTF keeps its equivalent markup separate from the html.
  // * Ignore \r and \n. The real carriage returns are stored in \par tags.
  // * Ignore \pntext{..} and \liN and \fi-N. These are RTF junk.
  // * Convert \par and \tab into \r\n and \t
  // * Convert \'XX into the ascii character indicated by the hex number XX
  // * Convert \{ and \} into { and }. This is how RTF escapes its curly braces.
  // * When we get \*\mhtmltagN, keep the tag, but ignore the subsequent \*\htmltagN
  // * When we get \*\htmltagN, keep the tag as long as it isn't subsequent to a \*\mhtmltagN
  // * All other text should be kept as it is.
  while (c<max)
  { if (*c=='{') c++;
    else if (*c=='}') c++;
    else if (strncmp(c,"\\*\\htmltag",10)==0)
    { c+=10; int tag=0; while (*c>='0' && *c<='9') {tag=tag*10+*c-'0'; c++;}
      if (*c==' ') c++;
      if (tag==ignore_tag) {while (c<max && *c!='}') c++; if (*c=='}') c++;}
      ignore_tag=-1;
    }
    else if (strncmp(c,"\\*\\mhtmltag",11)==0)
    { c+=11; int tag=0; while (*c>='0' && *c<='9') {tag=tag*10+*c-'0'; c++;}
      if (*c==' ') c++;
      ignore_tag=tag;
    }
    else if (strncmp(c,"\\par",4)==0) {strcpy(d,"\r\n"); d+=2; c+=4; if (*c==' ') c++;}
    else if (strncmp(c,"\\tab",4)==0) {strcpy(d,"   "); d+=3; c+=4; if (*c==' ') c++;}
    else if (strncmp(c,"\\line",5)==0) {strcpy(d,"\r\n"); d+=2; c+=5; if (*c==' ') c++;}
    else if (strncmp(c,"\\li",3)==0)
    { c+=3; while (*c>='0' && *c<='9') c++; if (*c==' ') c++;
    }
    else if (strncmp(c,"\\fi-",4)==0)
    { c+=4; while (*c>='0' && *c<='9') c++; if (*c==' ') c++;
    }
    else if (strncmp(c,"\\'",2)==0)
    { unsigned int hi=c[2], lo=c[3];
      if (hi>='0' && hi<='9') hi-='0'; else if (hi>='A' && hi<='Z') hi-='A'-10; else if (hi>='a' && hi<='z') hi-='a'-10;
      if (lo>='0' && lo<='9') lo-='0'; else if (lo>='A' && lo<='Z') lo-='A'-10; else if (lo>='a' && lo<='z') lo-='a'-10;
      *((unsigned char*)d) = (unsigned char)(hi*16+lo);
      c+=4; d++;
    }
    else if (strncmp(c,"\\pntext",7)==0) {c+=7; while (c<max && *c!='}') c++;}
    else if (strncmp(c,"\\htmlrtf",8)==0)
    { c++; while (c<max && strncmp(c,"\\htmlrtf0",9)!=0) c++;
      if (c<max) c+=9; if (*c==' ') c++;
    }
    else if (*c=='\r' || *c=='\n') c++;
    else if (strncmp(c,"\\{",2)==0) {*d='{'; d++; c+=2;}
    else if (strncmp(c,"\\}",2)==0) {*d='}'; d++; c+=2;}
    else {*d=*c; c++; d++;}
  }
  *d=0; d++;
  *len = d-buf;
}


StringStream::~StringStream()
{
    try
    {
        UlRelease( _lpStream );
    }
    catch(...){}
}
