/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

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