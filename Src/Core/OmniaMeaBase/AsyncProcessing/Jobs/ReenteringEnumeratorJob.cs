// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.AsyncProcessing
{
	/// <summary>
	/// Enumerates and executes jobs successively.
	/// </summary>
    public abstract class ReenteringEnumeratorJob : ReenteringJob
    {
        /**
         * provide next job, return null if no more jobs available
         */
        public abstract AbstractJob GetNextJob();

        /**
         * do smth before enumeration started
         */
        public abstract void EnumerationStarting();

        /**
         * do smth after enumeration finished
         */
        public abstract void EnumerationFinished();

        public bool ExecuteInIdle
        {
            get { return _executeInIdle; }
            set { _executeInIdle = value; }
        }

        protected override void Execute()
        {
            EnumerationStarting();
            AbstractJob job;
            try
            {
                while( !Interrupted )
                {
                    if( _executeInIdle && !Core.IsSystemIdle )
                    {
                        Interrupted = true;
                        break;
                    }
                    if( ( job = GetNextJob() ) == null )
                    {
                        break;
                    }
                    // if do jobs only if job was merged
                    if( Processor.QueueJob( ReenteringPriority, job ) )
                    {
                        do
                        {
                            DoJobs();
                        }
                        while( !Interrupted && job.NextWaitHandle != null );
                        /**
                         * if job.NextMethod == null then job is finished!
                         */
                    }
                }
            }
            finally
            {
                if( NextWaitHandle == AsyncProcessor._nullHandle )
                {
                    EnumerationFinished();
                }
            }
        }

        protected bool      _executeInIdle;
    }
}
