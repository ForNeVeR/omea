// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using JetBrains.Omea.OpenAPI;

namespace JetBrains.Omea.AsyncProcessing
{
	/// <summary>
	/// A base class for re-entering jobs.
	/// </summary>
    public abstract class ReenteringJob : AbstractNamedJob
    {
        protected AsyncProcessor Processor
        {
            get { return _processor; }
        }

        protected JobPriority ReenteringPriority
        {
            get { return _reenteringPriority; }
            set { _reenteringPriority = value; }
        }

        public bool Interrupted
        {
            get { return _finished || ( _processor != null && _processor.Finished ); }
            set { _finished = value; }
        }

        /**
         * can only be called in the Processor's thread
         */
        protected void DoJobs()
        {
            _processor.DoJobs();
        }

        internal AsyncProcessor _processor;
        private JobPriority     _reenteringPriority = JobPriority.Normal;
        private bool            _finished;
    }
}
