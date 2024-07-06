// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace JetBrains.Omea.Base
{
    public struct SpinWaitLock
    {
        static SpinWaitLock()
        {
            CalcApprovedProcessorCount();
        }

        public static int ApprovedProcessorCount
        {
            get { return _processorCount; }
        }

        public bool TryEnter()
        {
            return TryEnter( Thread.CurrentThread.GetHashCode() );
        }

        public void Enter()
        {
            int currentThreadId = Thread.CurrentThread.GetHashCode();
            if( !TryEnter( currentThreadId ) )
            {
                if( _processorCount == 1 )
                {
                    do
                    {
                        Sleep();
                    }
                    while( !TryEnter( currentThreadId ) );
                }
                else
                {
                    int iterations = 16;
                    do
                    {
                        if( iterations > 256 )
                        {
                            Sleep();
                            iterations = 16;
                        }
                        else
                        {
                            Thread.SpinWait( iterations );
                            iterations <<= 1;
                        }
                    }
                    while( !TryEnter( currentThreadId ) );
                }
            }
        }

        public void Exit()
        {
            if( Interlocked.Decrement( ref _lockCount ) == 0 )
            {
                Interlocked.Exchange( ref _ownerThreadId, 0 );
            }
        }

        private bool TryEnter( int currentThreadId )
        {
            int oldThreadId = Interlocked.CompareExchange( ref _ownerThreadId, currentThreadId, 0 );
            bool result = oldThreadId == 0 || oldThreadId == currentThreadId;
            if( result )
            {
                Interlocked.Increment( ref _lockCount );
            }
            return result;
        }

        private static void CalcApprovedProcessorCount()
        {
            int procCount = 0;
            try
            {
                int am = Process.GetCurrentProcess().ProcessorAffinity.ToInt32();
                while( am != 0 )
                {
                    ++procCount;
                    am &= ( am - 1 );
                }
            }
            catch( Win32Exception )
            {
                // Access is denied, or whatever else
            }
            finally
            {
                if( procCount == 0 )
                {
                    procCount = 1;
                }
                _processorCount = procCount;
            }
        }

        private static void Sleep()
        {
            // The following code is not MT-safe, but that's actually doesn't matter, because we are just
            // trying to calc number of processors "rarely" in order to decrease its affect on performance.
            if( ( ++_sleepCount & 0xfff ) == 0 )
            {
                CalcApprovedProcessorCount();
            }
            SwitchToThread();
        }

        [DllImport("kernel32", ExactSpelling=true)]
        private static extern void SwitchToThread();

        private int         _ownerThreadId;
        private int         _lockCount;
        private static int  _processorCount = 1;
        private static int  _sleepCount = 0;
    }
}
