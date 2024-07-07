#ifdef _MSC_VER
#define STRICT
#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#else
#include <sys/time.h>
#include <sys/resource.h>
#endif

wvSetLowPriority()
{
#ifdef _MSC_VER
    SetPriorityClass( GetCurrentProcess(), BELOW_NORMAL_PRIORITY_CLASS );
    SetThreadPriority( GetCurrentThread(), THREAD_PRIORITY_BELOW_NORMAL );
#else
    setpriority( PRIO_PROCESS, getpid(), 10 );
#endif
}
