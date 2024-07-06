// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Windows.Forms;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.Net
{
    public delegate void LineDelegate( string line );

    public class AsciiTcpConnection : AsyncTcpClient
    {
        public AsciiTcpConnection( string server, int port )
            : base( server, port )
        {
            _closeDelegate = new MethodInvoker( Close );
            _startDelegate = new StartUnitDelegate( StartUnit );
            _startNextDelegate = new MethodInvoker( StartNextUnit );
            IdleConnectionTimeout = 300;
            _unitQueue = new PriorityQueue();
            CloseIdleConnectionAfterTimeout();
        }

        private delegate void StartUnitDelegate( int priority, AsciiProtocolUnit unit );

        public virtual void StartUnit( int priority, AsciiProtocolUnit unit )
        {
            if( !Core.NetworkAP.IsOwnerThread )
            {
                Core.NetworkAP.QueueJob( JobPriority.Immediate, _startDelegate, priority, unit );
            }
            else
            {
                _unitQueue.Push( priority, unit );
                if( !IsBusy )
                {
                    StartNextUnitAsync();
                }
            }
        }

        public bool IsBusy
        {
            get { return _currentUnit != null; }
        }

        /**
         * idle timeout in seconds
         * connection automatically disconnects from server after it's been idle for this time
         */
        public int IdleConnectionTimeout
        {
            get { return _idleTimeout; }
            set
            {
                Core.NetworkAP.CancelTimedJobs( _closeDelegate );
                _idleTimeout = value;
                CloseIdleConnectionAfterTimeout();
            }
        }

        public virtual void Close()
        {
            if( LastSocketException == null )
            {
                Disconnect();
            }
            AbandonStartedUnits();
        }

        #region protocol events

        public event MethodInvoker  ResolveFailed;
        public event MethodInvoker  ConnectFailed;
        public event MethodInvoker  AfterConnect;
        public event MethodInvoker  BeforeDisconnect;
        public event MethodInvoker  AfterSend;
        public event LineDelegate   LineReceived;
        public event MethodInvoker  AfterReceive;
        public event MethodInvoker  OperationFailed;

        #endregion

        #region overriden callbacks

        protected override void OnResolveFailed()
        {
            if( ResolveFailed != null )
            {
                ResolveFailed();
            }
            Close();
        }

        protected override void OnConnectFailed()
        {
            if( ConnectFailed != null )
            {
                ConnectFailed();
            }
            Close();
        }

        protected override void OnAfterConnect()
        {
            if( AfterConnect != null )
            {
                AfterConnect();
            }
        }

        protected override void OnBeforeDisconnect()
        {
            if( BeforeDisconnect != null )
            {
                BeforeDisconnect();
            }
        }

        protected override void OnAfterSend()
        {
            if( AfterSend != null )
            {
                AfterSend();
            }
        }

        protected override void OnLineReceived( string line )
        {
            if( LineReceived != null )
            {
                LineReceived( line );
            }
        }

        protected override void OnAfterReceive()
        {
            if( AfterReceive != null )
            {
                AfterReceive();
            }
        }

        protected override void OnOperationFailed()
        {
            if( OperationFailed != null )
            {
                OperationFailed();
            }
            Close();
        }

        #endregion

        #region implementation details

        protected void AbandonStartedUnits()
        {
            if( IsBusy )
            {
                AsciiProtocolUnit.FireFinished( _currentUnit );
            }
            while( _unitQueue.Count > 0 )
            {
                AsciiProtocolUnit.FireFinished( (AsciiProtocolUnit) _unitQueue.Pop() );
            }
        }

        private void StartNextUnit()
        {
            if( !Core.NetworkAP.IsOwnerThread )
            {
                StartNextUnitAsync();
            }
            else
            {
                if( _unitQueue.Count > 0 )
                {
                    AsciiProtocolUnit unit = (AsciiProtocolUnit) _unitQueue.Pop();
                    unit.Finished += new AsciiProtocolUnitDelegate( unit_Finished );
                    _currentUnit = unit;
                    if( LastSocketException == null )
                    {
                        unit.Start( this );
                    }
                    else
                    {
                        Close();
                    }
                }
            }
        }

        private void unit_Finished( AsciiProtocolUnit unit )
        {
            _currentUnit = null;
            CloseIdleConnectionAfterTimeout();
            StartNextUnitAsync();
        }

        private void StartNextUnitAsync()
        {
            Core.NetworkAP.QueueJob( JobPriority.AboveNormal, _startNextDelegate );
        }

        /**
         * invoke closing connection after a timeout in seconds
         */
        private void CloseIdleConnectionAfterTimeout()
        {
            Core.NetworkAP.QueueJobAt(
                DateTime.Now.AddSeconds( _idleTimeout ),
                "Closing connection to " + Server + ":" + Port, _closeDelegate );
        }

        private MethodInvoker       _closeDelegate;
        private StartUnitDelegate   _startDelegate;
        private MethodInvoker       _startNextDelegate;
        private int                 _idleTimeout;
        private PriorityQueue       _unitQueue;
        private AsciiProtocolUnit   _currentUnit;

        #endregion
    }
}
