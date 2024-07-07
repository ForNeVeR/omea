// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using JetBrains.Omea.Diagnostics;
using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.OutlookPlugin
{
    public class PostManRepeatable : PostMan
    {
        public PostManRepeatable( )
        {
            Queue();
        }
        public override int GetHashCode()
        {
            return 26071972;
        }

        public override bool Equals( object obj )
        {
            return obj is PostManRepeatable;
        }

        private void Queue()
        {
            if ( OutlookSession.OutlookProcessor.ShuttingDown )
            {
                return;
            }
            OutlookSession.OutlookProcessor.CancelTimedJobs( this );
            double timeout = Settings.SendReceiveTimeout;
            OutlookSession.OutlookProcessor.QueueJobAt( DateTime.Now.AddMinutes( timeout ), this );
        }

        protected override void Execute()
        {
            if ( OutlookSession.OutlookProcessor.ShuttingDown )
            {
                return;
            }
            if ( OutlookSession.OutlookProcessor.ScheduleDeliver )
            {
                base.Execute();
            }
            Queue();
        }
    }

    public class PostMan : AbstractNamedJob
    {
        private IStatusWriter _statusWriter;

        public PostMan( )
        {
            _statusWriter = Core.UIManager.GetStatusWriter(  this, StatusPane.Network );
        }
        public override int GetHashCode()
        {
            return 0xBA0BAB;
        }

        public override bool Equals( object obj )
        {
            return obj is PostMan;
        }

        protected override void Execute()
        {
            if ( OutlookSession.OutlookProcessor.ShuttingDown )
            {
                return;
            }

            _statusWriter.ShowStatus( "Delivering mail..." );
            try
            {
                OutlookMailDeliver.DeliverNow();
            }
            catch ( Exception exception )
            {
                Tracer._TraceException( exception );
            }
            finally
            {
                _statusWriter.ClearStatus();
            }
        }

        public override string Name
        {
            get { return "Send/Receive mail"; }
        }
    }
}
