/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Threading;
using System.Windows.Forms;

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
        public void InvokeAfterWait( MethodInvoker method, WaitHandle handle )
        {
            _method = method;
            _handle = handle;
        }
        /// <summary>
        /// Gets next method to be executed.
        /// </summary>
        public MethodInvoker NextMethod
        {
            get { return ( _method == null ) ? ( _method = new MethodInvoker( Execute ) ) : _method; }
        }
        /// <summary>
        /// Gets next handle which should be waited to continue job.
        /// </summary>
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

        private static WaitHandle   _nullHandle = new Mutex();
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
        /// Gets or sets the name of the job.
        /// </summary>
        /// <remarks>The name of the last executing job is displayed in the tooltip for
        /// the async processor status indicator in the status bar.</remarks>
        abstract public string Name { get; set; }
    }

    public abstract class SimpleJob: AbstractNamedJob
    {
        private string _name = "";
        
        public override string Name
        {
            get { return _name; }
            set { _name = value; }
        }
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
        bool QueueJob( AbstractJob job );
        
        /// <summary>
        /// Queues a delegate for asynchronous execution with normal priority.
        /// </summary>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <returns>True if the delegate was really queued, false if it was merged with an equal one.</returns>
        bool QueueJob( Delegate method, params object[] args );
        
        /// <summary>
        /// Queues a named delegate for asynchronous execution with normal priority.
        /// </summary>
        /// <param name="name">Name of operation.</param>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <remarks>Name of operation is reflected by corresponding indicator light.</remarks>
        /// <returns>True if the delegate was really queued, false if it was merged with an equal one.</returns>
        bool QueueJob( string name, Delegate method, params object[] args );

        /// <summary>
        /// Queues a job for asynchronous execution with specified priority.
        /// </summary>
        /// <param name="priority">The priority of job.</param>
        /// <param name="job">The job to be executed.</param>
        /// <returns>True if the job was really queued, false if it was merged with an equal one.</returns>
        bool QueueJob( JobPriority priority, AbstractJob job );
        
        /// <summary>
        /// Queues a delegate for asynchronous execution with specified priority.
        /// </summary>
        /// <param name="priority">The priority of delegate.</param>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <returns>True if the delegate was really queued, false if it was merged with an equal one.</returns>
        bool QueueJob( JobPriority priority, Delegate method, params object[] args );
        
        /// <summary>
        /// Queues a named delegate for asynchronous execution with specified priority.
        /// </summary>
        /// <param name="priority">The priority of delegate.</param>
        /// <param name="name">Name of operation.</param>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <remarks>Name of operation is reflected by corresponding indicator light.</remarks>
        /// <returns>True if the delegate was really queued, false if it was merged with an equal one.</returns>
        bool QueueJob( JobPriority priority, string name, Delegate method, params object[] args );

        /// <summary>
        /// Queues a job for execution at specified time.
        /// </summary>
        /// <param name="dateTime">The time when job should be executed.</param>
        /// <param name="job">The job to be executed.</param>
        /// <remarks>If time has passed, job is executed immediately.</remarks>
        void QueueJobAt( DateTime dateTime, AbstractJob job );
        
        /// <summary>
        /// Queues a delegate for execution at specified time.
        /// </summary>
        /// <param name="dateTime">The time when delegate should be executed.</param>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <remarks>If time has passed, job is executed immediately.</remarks>
        void QueueJobAt( DateTime dateTime, Delegate method, params object[] args );
        
        /// <summary>
        /// Queues a named delegate for execution at specified time.
        /// </summary>
        /// <param name="dateTime">The time when delegate should be executed.</param>
        /// <param name="name">Name of operation.</param>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <remarks>If time has passed, job is executed immediately. Name of operation is reflected by corresponding indicator light.</remarks>
        void QueueJobAt( DateTime dateTime, string name, Delegate method, params object[] args );

        /// <summary>
        /// Queues a job for execution with normal priority in idle mode.
        /// </summary>
        /// <param name="job">The job to be executed in idle mode.</param>
        /// <remarks><seealso cref="ICore.IsSystemIdle"/><seealso cref="ICore.IdlePeriod"/></remarks>
        void QueueIdleJob( AbstractJob job );

        /// <summary>
        /// Queues a job for execution with specified priority in idle mode.
        /// </summary>
        /// <param name="priority">The priority of idle job.</param>
        /// <param name="job">The job to be executed in idle mode.</param>
        /// <remarks><seealso cref="ICore.IsSystemIdle"/><seealso cref="ICore.IdlePeriod"/></remarks>
        void QueueIdleJob( JobPriority priority, AbstractJob job );

        /// <summary>
        /// Queues a job for synchronous execution and waits until it is finished.
        /// </summary>
        /// <param name="job">The job to be executed.</param>
        /// <remarks>Jobs to be run are queued with the immediate priority.
        /// On attempt to run two or more equal jobs simultaneously the AsyncProcessorException is thrown.</remarks>
        void RunJob( AbstractJob job );
        
        /// <summary>
        /// Queues a delegate for synchronous execution and waits until it is finished.
        /// </summary>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <remarks>Jobs to be run are queued with the immediate priority.
        /// On attempt to run two or more equal jobs simultaneously the AsyncProcessorException is thrown.</remarks>
        /// <returns>Actual value returned by the method.</returns>
        object RunJob( Delegate method, params object[] args );
        
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
        object RunJob( string name, Delegate method, params object[] args );

        /// <summary>
        /// Queues a job for synchronous execution and waits until it is finished.
        /// </summary>
        /// <param name="job">The job to be executed.</param>
        /// <remarks>Jobs to be run are queued with the immediate priority.
        /// Unlike IAsyncProcessor.RunJob, attempt to run a job equal to another one already queued
        /// is silently skipped.</remarks>
        void RunUniqueJob( AbstractJob job );
        
        /// <summary>
        /// Queues a delegate for synchronous execution and waits until it is finished.
        /// </summary>
        /// <param name="method">The delegate to be executed.</param>
        /// <param name="args">Actual parameters of method.</param>
        /// <remarks>Jobs to be run are queued with the immediate priority.
        /// Unlike IAsyncProcessor.RunJob, attempt to run a delegate equal to another one already queued
        /// is silently skipped.</remarks>
        /// <returns>Actual value returned by the method or null, if the delegate was skipped.</returns>
        object RunUniqueJob( Delegate method, params object[] args );
        
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
        object RunUniqueJob( string name, Delegate method, params object[] args );

        /// <summary>
        /// Cancels earlier queued jobs that haven't started and match the filter.
        /// </summary>
        /// <param name="filter">Filter for cancellation.</param>
        void CancelJobs( JobFilter filter );
        /// <summary>
        /// Cancels earlier queued delegates that haven't started and equal to specified method.
        /// </summary>
        /// <param name="method">Method to be cancelled.</param>
        void CancelJobs( Delegate method );
        /// <summary>
        /// Cancels earlier queued jobs that haven't started and equal to specified job.
        /// </summary>
        /// <param name="job">Job to be cancelled.</param>
        void CancelJobs( AbstractJob job );
        /// <summary>
        /// Cancels all queued jobs that haven't started.
        /// </summary>
        void CancelJobs();

        /// <summary>
        /// Cancels timed jobs that haven't started and match the filter.
        /// </summary>
        /// <param name="filter">Filter for cancellation.</param>
        void CancelTimedJobs( JobFilter filter );
        /// <summary>
        /// Cancels timed delegates that haven't started and equal to specified method.
        /// </summary>
        /// <param name="method">Method to be cancelled.</param>
        void CancelTimedJobs( Delegate method );
        /// <summary>
        /// Cancels timed jobs that haven't started and equal to specified job.
        /// </summary>
        /// <param name="job">Job to be cancelled.</param>
        void CancelTimedJobs( AbstractJob job );

        /// <summary>
        /// Returns true if current thread is owned by the AsyncProcessor.
        /// </summary>
        /// <since>2.0</since>
        bool IsOwnerThread { get; }

        /// <summary>
        /// Returns the name of currently executed job. Empty name means that no named job is executed.
        /// </summary>
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
