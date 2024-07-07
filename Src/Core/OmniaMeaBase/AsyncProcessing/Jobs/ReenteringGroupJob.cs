// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.AsyncProcessing
{
	/// <summary>
	/// Executes a group of jobs simultaneously.
	/// </summary>
    public abstract class ReenteringGroupJob : ReenteringJob
    {
        /**
         * provide next job, return null if no more jobs available
         */
        public abstract AbstractJob GetNextJob();

        /**
         * do smth before execute
         */
        public abstract void GroupStarting();

        /**
         * do smth after enumeration finished
         */
        public abstract void GroupFinished();

        protected override void Execute()
        {
            GroupStarting();
            try
            {
                AbstractJob[] currentJobs = new AbstractJob[ _numberOfSimultaneousJobs ];
                AbstractJob nextJob = GetNextJob();
                if( nextJob == null )
                {
                    return;
                }
                while( !Interrupted )
                {
                    /**
                     * get missing number of jobs and check there are jobs to continue
                     */
                    bool finished = true;
                    for( int i = 0; i < currentJobs.Length; ++i )
                    {
                        AbstractJob job = currentJobs[ i ];
                        if( job != null )
                        {
                            finished = false;
                        }
                        else if( ( job = nextJob ) != null )
                        {
                            Processor.QueueJob( ReenteringPriority, currentJobs[ i ] = job );
                            nextJob = GetNextJob();
                            finished = false;
                        }
                    }
                    if( finished )
                    {
                        break;
                    }
                    bool doJobs = true;
                    while( !Interrupted && doJobs )
                    {
                        DoJobs();
                        /**
                             * search for finished jobs
                             * if at least one job finished, exit DoJobs loop
                             */
                        for( int i = 0; i < currentJobs.Length; ++i )
                        {
                            AbstractJob job = currentJobs[ i ];
                            if( job != null && job.NextWaitHandle == null )
                            {
                                /**
                                     * job is finished
                                     */
                                currentJobs[ i ] = null;
                                doJobs = false;
                            }
                        }
                    }
                }
            }
            finally
            {
                if( NextWaitHandle == AsyncProcessor._nullHandle )
                {
                    GroupFinished();
                }
            }
        }

        public int SimultaneousJobs
        {
            get { return _numberOfSimultaneousJobs; }
            set { _numberOfSimultaneousJobs = value; }
        }

        protected ReenteringGroupJob()
        {
            _numberOfSimultaneousJobs = 1;
        }

        protected ReenteringGroupJob( int numberOfSimultaneousJobs )
        {
            _numberOfSimultaneousJobs = numberOfSimultaneousJobs;
        }

        private int     _numberOfSimultaneousJobs;
    }
}
