#ifdef _MSC_VER
#define STRICT
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <ole2.h>
#include <mlang.h>

static MIMECPINFO s_CPI;
static char s_Charset[64];
#endif

static char *s_DefaultCharset = "iso-8859-15";

char *
vwGetDefaultCharset()
{
#ifdef _MSC_VER
    HRESULT hr = 0;
    IMultiLanguage *iml;

    hr = CoInitialize(NULL);

    if( FAILED(hr) )
    {
        return s_DefaultCharset;
    }

    hr = CoCreateInstance(
        &CLSID_CMultiLanguage,
        NULL,
        CLSCTX_INPROC_SERVER | CLSCTX_INPROC_HANDLER | CLSCTX_LOCAL_SERVER,
        &IID_IMultiLanguage,
        (void**)&iml
    );
    if( FAILED(hr) )
    {
        return s_DefaultCharset; 
    }
    iml->lpVtbl->GetCodePageInfo( iml, GetACP(), &s_CPI );
    WideCharToMultiByte( CP_ACP, 0, s_CPI.wszWebCharset, MAX_MIMEFACE_NAME, s_Charset, 64, NULL, NULL );
    iml->lpVtbl->Release( iml );
    CoUninitialize();
    return &s_Charset[0];
#else
    return s_DefaultCharset; 
#endif
}