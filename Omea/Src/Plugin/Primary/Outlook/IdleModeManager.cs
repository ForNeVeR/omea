/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using JetBrains.Interop.WinApi;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OutlookPlugin;

namespace JetBrains.Omea.OutlookPlugin
{

    internal class IdleModeManager
    {
        private LASTINPUTINFO _LASTINPUTINFO = new LASTINPUTINFO();
        private bool _idle = false;
        private bool _interrupted = false;

        private Tracer _tracer = new Tracer( "IdleMode" );

        public bool CompletedIdle
        {
            get
            {
                return Idle && !Interrupted;
            }
        }
        public bool Idle { get { return _idle; } }
        public bool CheckIdleAndSyncComplete()
        {
            return ( Settings.IdleModeManager.Idle && OutlookSession.OutlookProcessor.IsSyncComplete() );
        }
        public bool CheckInterruptIdle()
        {
            if ( Interrupted ) return true;
            if ( Idle && GetIdleTicks() <= OutlookSession.OutlookProcessor.IdlePeriod )
            {
                Interrupt();
                return true;
            }
            return false;
        }
        public bool Interrupted
        {
            get
            {
                return _interrupted || OutlookSession.OutlookProcessor.ShuttingDown;
            }
        }
        public void DropInterrupted()
        {
            _interrupted = false;
        }
        public void SetIdleFlag()
        {
            _interrupted = false;
            _LASTINPUTINFO.cbSize = 8;
            _idle = ( GetIdleTicks() >= OutlookSession.OutlookProcessor.IdlePeriod );
        }
        private int GetIdleTicks()
        {
            WindowsAPI.GetLastInputInfo( ref _LASTINPUTINFO );
            int idleTicks = (int) ( Kernel32Dll.GetTickCount() - _LASTINPUTINFO.dwTime );
            return idleTicks;
        }
        private void Interrupt()
        {
            _tracer.Trace( "Interrupt Idle" );
            _interrupted = true;
        }
    }
}