// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Threading;
using System.Windows.Forms;

using System35;

using JetBrains.Annotations;

namespace JetBrains.Omea.OpenAPI
{
    // -- Interfaces for Omnia Mea async processing ------------------------------------

    /// <summary>
    /// Specifies the priority of executing a job.
    /// </summary>
    public enum JobPriority
    {
        Lowest,
        BelowNormal,
        Normal,
        AboveNormal,
        Immediate
    }

    /// <summary>
    /// Represents a method which determines if the specified job should be cancelled.
    /// </summary>
    public delegate bool JobFilter( AbstractJob job );   // returns true - cancel, returns false - do not cancel

    /// <summary>
    /// Defines a base class for all asynchronous jobs.
    /// </summary>
    public abstract class AbstractJob
    {
        /// <summary>
        /// Override this method in order to perform an one-step job or to do
        /// initialization work for a many-steps job.
        /// </summary>
        protected abstract void Execute();

        /// <summary>
        /// Sets the next portion of work to be executed.
        /// </summary>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="handle">The waitable handle which is used to signal that an
        /// AsyncProcesor should start execution of the next method.</param>
        /// <remarks>By default, the overriden Execute method is called by AsyncProcessor
        /// unconditionally, and only it. It is possible to continue job with execution of
        /// the method, which is invoked when the handle is signaled. Setting handle to null
        /// (this is not necessary) means that the job should not be continued.</remarks>
        public void InvokeAfterWait([NotNull] MethodInvoker method, [NotNull] WaitHandle handle )
        {
            _method = method;
            _handle = handle;
        }
        /// <summary>
        /// Gets next method to be executed.
        /// </summary>
        [NotNull]
        public MethodInvoker NextMethod
        {
            get { return _method ?? ( _method = Execute ); }
        }
        /// <summary>
        /// Gets next handle which should be waited to continue job.
        /// </summary>
        [CanBeNull]
        public WaitHandle NextWaitHandle
        {
            get { return _handle; }
        }
        /// <summary>
        /// Gets or sets timeout in milliseconds during which job is waited to continue.
        /// </summary>
        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = value; }
        }
        /// <summary>
        /// Timeout handler.
        /// </summary>
        public event MethodInvoker OnTimeout;

        public void FireTimeout()
        {
            _handle = null;
            if( OnTimeout != null )
            {
                OnTimeout();
            }
        }

        private static readonly WaitHandle   _nullHandle = new Mutex();
        private MethodInvoker       _method;
        private WaitHandle          _handle = _nullHandle;
        private int                 _timeout = System.Threading.Timeout.Infinite;
    }

    /// <summary>
    /// Defines a base class for asynchronous jobs which have a name.
    /// </summary>
    public abstract class AbstractNamedJob : AbstractJob
    {
        /// <summary>
        /// Gets the name of the job.
        /// </summary>
        /// <remarks>The name of the last executing job is displayed in the tooltip for
        /// the async processor status indicator in the status bar.</remarks>
        [NotNull]
        abstract public string Name { get; }
    }

	/// <summary>
    /// Manages asynchronous execution of jobs in a thread.
    /// </summary>
    public interface IAsyncProcessor : IDisposable
    {
        /// <summary>
        /// Queues a job for asynchronous execution with normal priority.
        /// </summary>
        /// <param name="job">The job to be executed.</param>
        /// <returns>True if the job was really queued, false if it was merged with an equal one.</returns>
        bool QueueJob([NotNull] AbstractJob job );

        /// <summary>
        /// Queues a delegate for asynchronous execution with normal priority.
        /// </summary>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <returns>True if the delegate was really queued, false if it was merged with an equal one.</returns>
        [Obsolete("An overload that takes the job name should be used.")]
        bool QueueJob([NotNull] Delegate method, params object[] args );

        /// <summary>
        /// Queues a named delegate for asynchronous execution with normal priority.
        /// </summary>
        /// <param name="name">Name of operation.</param>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <remarks>Name of operation is reflected by corresponding indicator light.</remarks>
        /// <returns>True if the delegate was really queued, false if it was merged with an equal one.</returns>
        bool QueueJob([NotNull] string name, [NotNull] Delegate method, params object[] args );

        /// <summary>
        /// Queues a named delegate for asynchronous execution with normal priority.
        /// These jobs are never merged.
        /// </summary>
        /// <param name="name">Name of operation.</param>
        /// <param name="action">The delegate to be executed. Arguments should be passed via a closure.</param>
        /// <remarks>Name of operation is reflected by corresponding indicator light.</remarks>
        void QueueJob( [NotNull] string name, [NotNull] Action action );

        /// <summary>
        /// Queues a named delegate for asynchronous execution with normal priority.
        /// </summary>
        /// <param name="name">Name of operation.</param>
        /// <param name="identity">An optional identity. Jobs with equal non-<c>Null</c> identity will be merged together.</param>
        /// <param name="action">The delegate to be executed. Arguments should be passed via a closure.</param>
        /// <remarks>Name of operation is reflected by corresponding indicator light.</remarks>
        /// <returns><c>True</c> if the delegate was really queued, <c>False</c> if it was merged with an equal one.</returns>
        bool QueueJob([NotNull] string name, [NotNull] object identity, [NotNull] Action action);

        /// <summary>
        /// Queues a job for asynchronous execution with specified priority.
        /// </summary>
        /// <param name="priority">The priority of job.</param>
        /// <param name="job">The job to be executed.</param>
        /// <returns>True if the job was really queued, false if it was merged with an equal one.</returns>
        bool QueueJob( JobPriority priority, [NotNull] AbstractJob job );

        /// <summary>
        /// Queues a delegate for asynchronous execution with specified priority.
        /// </summary>
        /// <param name="priority">The priority of this job.</param>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <returns>True if the delegate was really queued, false if it was merged with an equal one.</returns>
        [Obsolete("An overload that takes the job name should be used.")]
        bool QueueJob( JobPriority priority, [NotNull] Delegate method, params object[] args );

        /// <summary>
        /// Queues a named delegate for asynchronous execution with specified priority.
        /// </summary>
        /// <param name="priority">The priority of this job.</param>
        /// <param name="name">Name of operation.</param>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <remarks>Name of operation is reflected by corresponding indicator light.</remarks>
        /// <returns>True if the delegate was really queued, false if it was merged with an equal one.</returns>
        bool QueueJob( JobPriority priority, [NotNull] string name, [NotNull] Delegate method, params object[] args );

        /// <summary>
        /// Queues a named delegate for asynchronous execution with specified priority.
        /// These jobs are never merged.
        /// </summary>
        /// <param name="priority">The priority of this job.</param>
        /// <param name="name">Name of operation.</param>
        /// <param name="action">The delegate to be executed. Arguments should be passed via a closure.</param>
        /// <remarks>Name of operation is reflected by corresponding indicator light.</remarks>
        void QueueJob( JobPriority priority, [NotNull] string name, [NotNull] Action action );

        /// <summary>
        /// Queues a named delegate for asynchronous execution with specified priority.
        /// </summary>
        /// <param name="priority">The priority of this job.</param>
        /// <param name="name">Name of operation.</param>
        /// <param name="identity">An optional identity. Jobs with equal non-<c>Null</c> identity will be merged together.</param>
        /// <param name="action">The delegate to be executed. Arguments should be passed via a closure.</param>
        /// <remarks>Name of operation is reflected by corresponding indicator light.</remarks>
        /// <returns><c>True</c> if the delegate was really queued, <c>False</c> if it was merged with an equal one.</returns>
        bool QueueJob( JobPriority priority, [NotNull] string name, [NotNull] object identity, [NotNull] Action action);

        /// <summary>
        /// Queues a job for execution at specified time.
        /// </summary>
        /// <param name="dateTime">The time when job should be executed.</param>
        /// <param name="job">The job to be executed.</param>
        /// <remarks>If time has passed, job is executed immediately.</remarks>
        void QueueJobAt( DateTime dateTime, [NotNull] AbstractJob job );

        /// <summary>
        /// Queues a delegate for execution at specified time.
        /// </summary>
        /// <param name="dateTime">The time when delegate should be executed.</param>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <remarks>If time has passed, job is executed immediately.</remarks>
        [Obsolete("An overload that takes the job name should be used.")]
        void QueueJobAt( DateTime dateTime, [NotNull] Delegate method, params object[] args );

        /// <summary>
        /// Queues a named delegate for execution at specified time.
        /// </summary>
        /// <param name="dateTime">The time when delegate should be executed.</param>
        /// <param name="name">Name of operation.</param>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <remarks>If time has passed, job is executed immediately. Name of operation is reflected by corresponding indicator light.</remarks>
        void QueueJobAt( DateTime dateTime, [NotNull] string name, [NotNull] Delegate method, params object[] args );

        /// <summary>
        /// Queues a named delegate for execution at specified time.
        /// </summary>
        /// <param name="dateTime">The time when delegate should be executed.</param>
        /// <param name="name">Name of operation.</param>
        /// <param name="action">The delegate to be executed. Arguments should be passed via a closure.</param>
        /// <remarks>If time has passed, job is executed immediately. Name of operation is reflected by corresponding indicator light.</remarks>
        void QueueJobAt( DateTime dateTime, [NotNull] string name, [NotNull] Action action );

        /// <summary>
        /// Queues a job for execution with normal priority in idle mode.
        /// </summary>
        /// <param name="job">The job to be executed in idle mode.</param>
        /// <remarks><seealso cref="ICore.IsSystemIdle"/><seealso cref="ICore.IdlePeriod"/></remarks>
        void QueueIdleJob([NotNull] AbstractJob job );

        /// <summary>
        /// Queues a job for execution with specified priority in idle mode.
        /// </summary>
        /// <param name="priority">The priority of idle job.</param>
        /// <param name="job">The job to be executed in idle mode.</param>
        /// <remarks><seealso cref="ICore.IsSystemIdle"/><seealso cref="ICore.IdlePeriod"/></remarks>
        void QueueIdleJob( JobPriority priority, [NotNull] AbstractJob job );

        /// <summary>
        /// Queues a job for synchronous execution and waits until it is finished.
        /// </summary>
        /// <param name="job">The job to be executed.</param>
        /// <remarks>Jobs to be run are queued with the immediate priority.
        /// On attempt to run two or more equal jobs simultaneously the AsyncProcessorException is thrown.</remarks>
        void RunJob([NotNull] AbstractJob job );

        /// <summary>
        /// Queues a delegate for synchronous execution and waits until it is finished.
        /// </summary>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <remarks>Jobs to be run are queued with the immediate priority.
        /// On attempt to run two or more equal jobs simultaneously the AsyncProcessorException is thrown.</remarks>
        /// <returns>Actual value returned by the method.</returns>
        [Obsolete("An overload that takes the job name should be used.")]
        object RunJob([NotNull] Delegate method, params object[] args );

        /// <summary>
        /// Queues a named delegate for synchronous execution and waits until it is finished.
        /// </summary>
        /// <param name="name">Name of operation.</param>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <remarks>Jobs to be run are queued with the immediate priority.
        /// On attempt to run two or more equal jobs simultaneously the AsyncProcessorException is thrown.
        /// Name of operation is reflected by corresponding indicator light.</remarks>
        /// <returns>Actual value returned by the method.</returns>
        object RunJob([NotNull] string name, [NotNull] Delegate method, params object[] args );

        /// <summary>
        /// Queues a named delegate for synchronous execution and waits until it is finished.
        /// </summary>
        /// <param name="name">Name of operation.</param>
        /// <param name="action">The delegate to be executed. Arguments and a return value should be passed via a closure.</param>
        /// <remarks>Jobs to be run are queued with the immediate priority.
        /// On attempt to run two or more equal jobs simultaneously the <c>AsyncProcessorException</c> is thrown.
        /// Name of operation is reflected by corresponding indicator light.</remarks>
        /// <returns>Whether the execution succeeded.</returns>
        bool RunJob( [NotNull] string name, [NotNull] Action action );

        /// <summary>
        /// Queues a job for synchronous execution and waits until it is finished.
        /// </summary>
        /// <param name="job">The job to be executed.</param>
        /// <remarks>Jobs to be run are queued with the immediate priority.
        /// Unlike IAsyncProcessor.RunJob, attempt to run a job equal to another one already queued
        /// is silently skipped.</remarks>
        void RunUniqueJob([NotNull] AbstractJob job );

        /// <summary>
        /// Queues a delegate for synchronous execution and waits until it is finished.
        /// </summary>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <remarks>Jobs to be run are queued with the immediate priority.
        /// Unlike IAsyncProcessor.RunJob, attempt to run a delegate equal to another one already queued
        /// is silently skipped.</remarks>
        /// <returns>Actual value returned by the method or null, if the delegate was skipped.</returns>
        [Obsolete("An overload that takes the job name should be used.")]
        object RunUniqueJob([NotNull] Delegate method, params object[] args );

        /// <summary>
        /// Queues a named delegate for synchronous execution and waits until it is finished.
        /// </summary>
        /// <param name="name">Name of operation.</param>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <remarks>Jobs to be run are queued with the immediate priority.
        /// Unlike IAsyncProcessor.RunJob, attempt to run a delegate equal to another one already queued
        /// is silently skipped. Name of operation is reflected by corresponding indicator light.</remarks>
        /// <returns>Actual value returned by the method or null, if the delegate was skipped.</returns>
        object RunUniqueJob([NotNull] string name, [NotNull] Delegate method, params object[] args );

        /// <summary>
        /// Cancels earlier queued jobs that haven't started and match the filter.
        /// </summary>
        /// <param name="filter">Filter for cancellation.</param>
        void CancelJobs([NotNull] JobFilter filter );
        /// <summary>
        /// Cancels earlier queued delegates that haven't started and equal to specified method.
        /// </summary>
        /// <param name="method">Method to be cancelled.</param>
        void CancelJobs([NotNull] Delegate method );
        /// <summary>
        /// Cancels earlier queued jobs that haven't started and equal to specified job.
        /// </summary>
        /// <param name="job">Job to be cancelled.</param>
        void CancelJobs([NotNull] AbstractJob job );
        /// <summary>
        /// Cancels all queued jobs that haven't started.
        /// </summary>
        void CancelJobs();

        /// <summary>
        /// Cancels timed jobs that haven't started and match the filter.
        /// </summary>
        /// <param name="filter">Filter for cancellation.</param>
        void CancelTimedJobs([NotNull] JobFilter filter );
        /// <summary>
        /// Cancels timed delegates that haven't started and equal to specified method.
        /// </summary>
        /// <param name="method">Method to be cancelled.</param>
        void CancelTimedJobs([NotNull] Delegate method );
        /// <summary>
        /// Cancels timed jobs that haven't started and equal to specified job.
        /// </summary>
        /// <param name="job">Job to be cancelled.</param>
        void CancelTimedJobs([NotNull] AbstractJob job );

        /// <summary>
        /// Returns true if current thread is owned by the AsyncProcessor.
        /// </summary>
        /// <since>2.0</since>
        bool IsOwnerThread { get; }

        /// <summary>
        /// Returns the name of currently executed job. Empty name means that no named job is executed.
        /// </summary>
        [NotNull]
        string CurrentJobName { get; }

        /// <summary>
        /// Event handler invoked before starting a job.
        /// </summary>
        event EventHandler JobStarting;
        /// <summary>
        /// Event handler invoked after a job finished.
        /// </summary>
        event EventHandler JobFinished;
        /// <summary>
        /// Event handler invoked when AsyncProcessor's main queue of jobs becomes empty.
        /// </summary>
        /// <remarks>Main queue of jobs doesn't contain timed or idle jobs.</remarks>
        event EventHandler QueueGotEmpty;
    }
}
