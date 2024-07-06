// SPDX-FileCopyrightText: 2003-2008 JetBrains s.r.o.
//
// SPDX-License-Identifier: GPL-2.0-only

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections;
using System.Windows.Forms;

using System35;

using JetBrains.Annotations;
using JetBrains.Interop.WinApi;
using JetBrains.Omea.Containers;
using JetBrains.Omea.OpenAPI;
using JetBrains.Omea.Base;
using JetBrains.Omea.Diagnostics;
using JetBrains.DataStructures;

namespace JetBrains.Omea.AsyncProcessing
{
    public delegate void AsyncExceptionHandler( Exception e );

    public class AsyncProcessorException : Exception
    {
        public AsyncProcessorException( Exception innerException )
            : base( "Exception raised during async processing", innerException )
        {
        }
    }

    /**
     * Omea asynchronous processor
     */
    public class AsyncProcessor : IAsyncProcessor
    {
        public AsyncProcessor()
            : this( null, true ) {}

        public AsyncProcessor( bool startProcessorThread )
            : this( null, startProcessorThread ) {}

        public AsyncProcessor( AsyncExceptionHandler exceptionHandler, bool startProcessorThread )
        {
            _empty = true;
            _processMessages = false;
            _isReenterable = true;
            _isThreadStarted = 0;
            _idlePeriod = _defaultIdlePeriod;
            _jobQueue = new PriorityQueue();
            _uniqueJobs = new HashSet( 50 );
            _idleJobQueue = new PriorityQueue();
            _uniqueIdleJobs = new HashSet( 10 );
            _timedJobs = new DateTimePriorityQueue();
            _timedJobCounts = new HashMap( 30 );
            _startedJobs = new HashMap( 30 );
            _startedReenteringJobs = new HashSet();
            _exceptionHandler = exceptionHandler;
            _awakeningEvent = new ManualResetEvent( false );
            _tracer = new Tracer( "AsyncProcessor" );
            _currentJob = null;
            _processorThread = new Thread( new ThreadStart( ProcessorThread ) );
            _processorThread.IsBackground = true;
            if( startProcessorThread )
            {
                StartThread();
            }
        }

        public bool Finished
        {
            get { return _finished; }
        }

        public string CurrentJobName
        {
            get
            {
                AbstractNamedJob namedJob = _currentJob as AbstractNamedJob;
                return ( namedJob == null ) ? string.Empty : namedJob.Name ;
            }
        }

        public int OutstandingJobs
        {
            get { return _jobQueue.Count; }
        }

        public bool ProcessMessages
        {
            get { return _processMessages; }
            set { _processMessages = value; }
        }

        public bool Reenterable
        {
            get { return _isReenterable; }
            set { _isReenterable = value; }
        }

        #region thread controlling functions

        public void StartThread()
        {
            if( Interlocked.Exchange( ref _isThreadStarted, 1 ) == 0 )
            {
                _processorThread.Start();
            }
        }

        public virtual void EmployCurrentThread()
        {
            if( Interlocked.Exchange( ref _isThreadStarted, 1 ) == 0 )
            {
                _processorThread = Thread.CurrentThread;
                ProcessorThread();
            }
        }

        public ThreadPriority ThreadPriority
        {
            get { return _processorThread.Priority; }
            set
            {
                if( !_finished && _processorThread.IsAlive )
                {
                    try
                    {
                        _processorThread.Priority = value;
                    }
                    catch( ThreadStateException ) {}
                }
            }
        }

        public string ThreadName
        {
            get { return _processorThread.Name; }
            set
            {
                if( _processorThread.Name == null )
                {
                    _processorThread.Name = value;
                }
            }
        }

        public Thread Thread
        {
            get { return _processorThread; }
        }

        public virtual bool IsOwnerThread
        {
            get { return _processorThread == Thread.CurrentThread; }
        }

        private delegate TimeSpan GetTimeSpanDelegate();

        public TimeSpan GetKernelTime()
        {
            if( !IsOwnerThread )
            {
                return (TimeSpan) RunUniqueJob( new GetTimeSpanDelegate( GetKernelTime ) );
            }
            else
            {
                WindowsAPI.FILETIME ct = new WindowsAPI.FILETIME();
                WindowsAPI.FILETIME et = new WindowsAPI.FILETIME();
                WindowsAPI.FILETIME kt = new WindowsAPI.FILETIME();
                WindowsAPI.FILETIME ut = new WindowsAPI.FILETIME();
                WindowsAPI.GetThreadTimes( WindowsAPI.GetCurrentThread(), ref ct, ref et, ref kt, ref ut );
                long ticks = kt.dwLowDateTime + ( ( (long) kt.dwHighDateTime ) << 32 );
                return new TimeSpan( ticks );
            }
        }

        public TimeSpan GetUserTime()
        {
            if( !IsOwnerThread )
            {
                return (TimeSpan) RunUniqueJob( new GetTimeSpanDelegate( GetUserTime ) );
            }
            else
            {
                WindowsAPI.FILETIME ct = new WindowsAPI.FILETIME();
                WindowsAPI.FILETIME et = new WindowsAPI.FILETIME();
                WindowsAPI.FILETIME kt = new WindowsAPI.FILETIME();
                WindowsAPI.FILETIME ut = new WindowsAPI.FILETIME();
                WindowsAPI.GetThreadTimes( WindowsAPI.GetCurrentThread(), ref ct, ref et, ref kt, ref ut );
                long ticks = ut.dwLowDateTime + ( ( (long) ut.dwHighDateTime ) << 32 );
                return new TimeSpan( ticks );
            }
        }

        // in milliseconds
        public int IdlePeriod
        {
            get { return _idlePeriod; }
            set
            {
                if( _idlePeriod <= 0 )
                {
                    throw new Exception( "Idle period cannot be less or equal to zero" );
                }
                _idlePeriod = value;
            }
        }

        public void WaitUntilFinished()
        {
            if( _isThreadStarted != 0 )
            {
                if( !Application.MessageLoop )
                {
                    _processorThread.Join();
                }
                else
                {
                    while( _processorThread.IsAlive && !_processorThread.Join( 100 ) )
                    {
                        Application.DoEvents();
                    }
                }
            }
        }

        #endregion

        #region asyncprocessor events

        public event EventHandler ThreadStarted;
        public event EventHandler ThreadFinished;
        public event EventHandler FillingEmptyQueue;
        public event EventHandler QueueGotEmpty;
        public event EventHandler JobStarting
        {
            add
            {
                JobStartingDelegate += value;
            }
            remove
            {
                JobStartingDelegate -= value;
            }
        }
        public event EventHandler JobFinished
        {
            add
            {
                JobFinishedDelegate += value;
            }
            remove
            {
                JobFinishedDelegate -= value;
            }
        }

        protected EventHandler JobStartingDelegate;
        protected EventHandler JobFinishedDelegate;

        public delegate void JobDelegate( object sender, AbstractJob job );

        public event JobDelegate JobQueued;

        #endregion

        #region IAsyncProcessor Members

        public bool QueueJob( AbstractJob job )
        {
            return PushJob( job );
        }

        public bool QueueJob( Delegate method, params object[] args )
        {
            return PushJob( new DelegateJob( method, args ) );
        }

        public bool QueueJob( string name, Delegate method, params object[] args )
        {
            return PushJob( new DelegateJob( name, method, args ) );
        }

        public void QueueJob( [NotNull] string name, [NotNull] Action action )
        {
        	PushJob(new ActionJob(name, action));
        }

        public bool QueueJob([NotNull] string name, [NotNull] object identity, [NotNull] Action action)
        {
        	return PushJob(new ActionJob(name, action, identity));
        }

        public bool QueueJob( JobPriority priority, AbstractJob job )
        {
            return PushJob( job, priority );
        }

        public bool QueueJob( JobPriority priority, Delegate method, params object[] args )
        {
            return PushJob( new DelegateJob( method, args ), priority );
        }

        public bool QueueJob( JobPriority priority, string name, Delegate method, params object[] args )
        {
            return PushJob( new DelegateJob( name, method, args ), priority );
        }

        public void QueueJob( JobPriority priority, [NotNull] string name, [NotNull] Action action )
        {
            PushJob( new ActionJob( name, action ), priority );
        }

        public bool QueueJob( JobPriority priority, [NotNull] string name, [NotNull] object identity, [NotNull] Action action)
        {
            return PushJob( new ActionJob( name, action, identity ), priority );
        }

    	private delegate void QueueTimedJobDelegate( DateTime when, AbstractJob job );

        public void QueueJobAt( DateTime when, AbstractJob job )
        {
            if( Thread.CurrentThread == _processorThread && _isThreadStarted != 0 )
            {
                QueueTimedJob( when, job );
            }
            else
            {
                if( when <= DateTime.Now )
                {
                    PushJob( job, JobPriority.Immediate );
                }
                else
                {
                    QueueJob( JobPriority.Immediate, "Queueing timed jobs",
                        new QueueTimedJobDelegate( QueueTimedJob ), when, job );
                }
            }
        }

        public void QueueJobAt( DateTime when, Delegate method, params object[] args )
        {
            QueueJobAt( when, new DelegateJob( method, args ) );
        }

        public void QueueJobAt( DateTime when, string name, Delegate method, params object[] args )
        {
            QueueJobAt( when, new DelegateJob( name, method, args ) );
        }

        public void QueueJobAt( DateTime when, string name, Action action )
        {
            QueueJobAt( when, new ActionJob( name, action ) );
        }

        protected delegate bool QueueIdleJobDelegate( JobPriority priority, AbstractJob job );

        public void QueueIdleJob( JobPriority priority, AbstractJob job )
        {
            /**
             * idle processing can be performed only under WinNT or later
             * under other platforms, just queue an job
             */
            if( _platform != PlatformID.Win32NT )
            {
                PushJob( job, priority );
            }
            else
            {
                if( Thread.CurrentThread == _processorThread && _isThreadStarted != 0 )
                {
                    QueueIdleJobImpl( priority, job );
                }
                else
                {
                    QueueJob( JobPriority.Immediate, "Queueing idle jobs",
                        new QueueIdleJobDelegate( QueueIdleJobImpl ), priority, job );
                }
            }
        }

        public void QueueIdleJob( AbstractJob job )
        {
            QueueIdleJob( JobPriority.Normal, job );
        }

        public void RunJob( AbstractJob job )
        {
            RunJob( job, true );
        }

        public object RunJob( Delegate method, params object[] args )
        {
            DelegateJob job = new DelegateJob( method, args );
            job = (DelegateJob) RunJob( job, true );
            return ( job != null ) ? job.ReturnValue : null;
        }

        public object RunJob( string name, Delegate method, params object[] args )
        {
            DelegateJob job = new DelegateJob( name, method, args );
            job = (DelegateJob) RunJob( job, true );
            return ( job != null ) ? job.ReturnValue : null;
        }

        public bool RunJob([NotNull] string name, [NotNull] Action action )
        {
        	var job = new ActionJob(name, action);
        	return RunJob( job, true ) != null;
        }

        public void RunUniqueJob( AbstractJob job )
        {
            RunJob( job, false );
        }

        public object RunUniqueJob( Delegate method, params object[] args )
        {
            DelegateJob job = new DelegateJob( method, args );
            job = (DelegateJob) RunJob( job, false );
            return ( job != null ) ? job.ReturnValue : null;
        }

        public object RunUniqueJob( string name, Delegate method, params object[] args )
        {
            DelegateJob job = new DelegateJob( name, method, args );
            job = (DelegateJob) RunJob( job, false );
            return ( job != null ) ? job.ReturnValue : null;
        }

        public void CancelJobs( JobFilter filter )
        {
            _jobsLock.Enter();
            try
            {
                PriorityQueue filteredQueue = new PriorityQueue();
                while( _jobQueue.Count > 0 )
                {
                    PriorityQueue.QueueEntry E = _jobQueue.PopEntry();
                    AbstractJob job = (AbstractJob) E.Value;
                    if( job != null )
                    {
                        if( !filter( job ) )
                        {
                            filteredQueue.Push( E.Priority, E.Value );
                        }
                        else
                        {
                            _uniqueJobs.Remove( job );
                            ICancelable cjob = job as ICancelable;
                            if( cjob != null )
                            {
                                cjob.OnCancel();
                            }
                        }
                    }
                }
                _jobQueue = filteredQueue;
            }
            finally
            {
                _jobsLock.Exit();
            }
        }

        public void CancelJobs( Delegate method )
        {
            DelegateJobFilter filter = new DelegateJobFilter( method );
            CancelJobs( new JobFilter( filter.DoFilter ) );
        }

        public void CancelJobs( AbstractJob job )
        {
            EqualsJobFilter filter = new EqualsJobFilter( job );
            CancelJobs( new JobFilter( filter.DoFilter ) );
        }

        public void CancelJobs()
        {
            _jobsLock.Enter();
            try
            {
                while( _jobQueue.Count > 0 )
                {
                    PriorityQueue.QueueEntry E = _jobQueue.PopEntry();
                    AbstractJob job = (AbstractJob) E.Value;
                    if( job != null )
                    {
                        _uniqueJobs.Remove( job );
                        ICancelable cjob = job as ICancelable;
                        if( cjob != null )
                        {
                            cjob.OnCancel();
                        }
                    }
                }
            }
            finally
            {
                _jobsLock.Exit();
            }
        }

        protected delegate void CancelTimedJobsDelegate( JobFilter filter );

        public void CancelTimedJobs( JobFilter filter )
        {
            if( Thread.CurrentThread == _processorThread && _isThreadStarted != 0 )
            {
                CancelTimedJobsImpl( filter );
            }
            else
            {
                QueueJob( JobPriority.Immediate, "Cancelling timed jobs",
                    new CancelTimedJobsDelegate( CancelTimedJobsImpl ), filter );
            }
        }

        public void CancelTimedJobs( Delegate method )
        {
            DelegateJobFilter filter = new DelegateJobFilter( method );
            CancelTimedJobs( new JobFilter( filter.DoFilter ) );
        }

        public void CancelTimedJobs( AbstractJob job )
        {
            EqualsJobFilter filter = new EqualsJobFilter( job );
            CancelTimedJobs( new JobFilter( filter.DoFilter ) );
        }

        public void QueueEndOfWork()
        {
            QueueJob( JobPriority.Lowest, new MethodInvoker( EndWork ) );
        }

        public AsyncExceptionHandler ExceptionHandler
        {
            get { return _exceptionHandler; }
            set { _exceptionHandler = value; }
        }

        #endregion

        #region IDisposable Members

        public virtual void Dispose()
        {
            if( _isThreadStarted != 0 && _processorThread != Thread.CurrentThread )
            {
                ThreadPriority = ThreadPriority.Normal;
                _finished = true;
                _awakeningEvent.Set();
                if( ProcessMessages )
                {
					User32Dll.PostThreadMessageW( unchecked((uint)_processorThread.GetHashCode()), (uint)WindowsMessages.WM_QUIT, IntPtr.Zero, IntPtr.Zero );
                }
                WaitUntilFinished();
            }
            /**
             * force cancel jobs in order to stop all runnable jobs in the queue
             */
            CancelJobs();
            _awakeningEvent.Close();
        }

        #endregion

        #region implementation details

        protected bool PushJob( AbstractJob job )
        {
            return PushJob( job, JobPriority.Normal );
        }

        protected virtual bool PushJob( AbstractJob job, JobPriority priority )
        {
            if( _finished )
            {
                return false;
            }
            _jobsLock.Enter();
            try
            {
                if( _uniqueJobs.Contains( job ) )
                {
                    return false;
                }
                _jobQueue.Push( (int) priority, job );
                _uniqueJobs.Add( job );
            }
            finally
            {
                _jobsLock.Exit();
            }
            _awakeningEvent.Set();
            if( JobQueued != null )
            {
                JobQueued( this, job );
            }

            return true;
        }

        /**
         * decorator for runnable jobs
         */
        protected class RunnableJobDecorator: AbstractNamedJob, ICancelable
        {
            public RunnableJobDecorator( AsyncProcessor processor, AbstractJob internalJob )
            {
                _processor = processor;
                _internalJob = internalJob;
                _refCount = 0;
            }

            public int  IncRef()
            {
                return Interlocked.Increment( ref _refCount );
            }

            public int DecRef()
            {
                return Interlocked.Decrement( ref _refCount );
            }

            protected override void Execute()
            {
                try
                {
                    _processor.ExecuteJob( _internalJob );
                    while( !_processor.Finished && _internalJob.NextWaitHandle != null )
                    {
                        _processor.DoJobs();
                    }
                }
                catch( Exception exc )
                {
                    if( !( exc is ThreadAbortException ) )
                    {
                        _exception = exc;
                    }
                }
                finally
                {
                    OnCancel();
                }
            }

            public override string Name
            {
                get
                {
                    string result = "Running job";
                    AbstractNamedJob internalNamedJob = _internalJob as AbstractNamedJob;
                    if ( internalNamedJob != null )
                    {
                        result += ": " + internalNamedJob.Name;
                    }
                    return result;
                }
            }

            public override int GetHashCode()
            {
                return _internalJob.GetHashCode();
            }
            public override bool Equals( object obj )
            {
                AbstractJob right = obj as AbstractJob;
                return ( right != null ) && right.Equals( _internalJob );
            }

            #region ICancelable Members

            public void OnCancel()
            {
                try
                {
                    _jobFinished.Set();
                }
                catch( Exception exc )
                {
                    _processor._tracer.TraceException( exc );
                }
            }

            #endregion

            public AbstractJob          _internalJob;
            public ManualResetEvent     _jobFinished;
            public Exception            _exception;
            protected AsyncProcessor    _processor;
            protected int               _refCount;
        }

        /**
         * returns the instance of job which was actually executed
         */
        protected AbstractJob RunJob( AbstractJob job, bool throwExceptionIfEqualJobsDetected )
        {
            if( _finished )
            {
                return null;
            }
            if( _isThreadStarted == 0 || Thread.CurrentThread == _processorThread )
            {
                ExecuteJob( job );
                return job;
            }

            /**
             * If Run operation is invoked from a thread of an AsyncProcessor, then try to
             * get AsyncProcessor from the pool
             */
            AsyncProcessor procOfCurrentThread = GetProcessorFromPool( Thread.CurrentThread );
            if( procOfCurrentThread != null )
            {
                if( !procOfCurrentThread.Reenterable || procOfCurrentThread.Finished )
                {
                    procOfCurrentThread = null;
                }
                else
                {
                    if( procOfCurrentThread._numberOfRunJobs == _maxWaitableHandles - 1 )
                    {
                        procOfCurrentThread = null;
                    }
                    else
                    {
                        procOfCurrentThread._numberOfRunJobs++;
                    }
                }
            }

            RunnableJobDecorator runnable;
            ManualResetEvent jobFinished;
            _jobsLock.Enter();
            try
            {
                /**
                 * necessary check to avoid race condition OM-7021
                 */
                if( _finished )
                {
                    return null;
                }
                runnable = new RunnableJobDecorator( this, job );
                RunnableJobDecorator startedRunnable = _uniqueJobs.GetKey( runnable ) as RunnableJobDecorator;
                if( startedRunnable != null )
                {
                    if( throwExceptionIfEqualJobsDetected )
                    {
                        throw new Exception( "Attempt to run equal jobs! job.ToString() = " + job.ToString() );
                    }
                    runnable = startedRunnable;
                    jobFinished = runnable._jobFinished;
                }
                else
                {
                    _uniqueJobs.Add( runnable );
                    _jobQueue.Push( (int) JobPriority.Immediate, runnable );
                    jobFinished = new ManualResetEvent( false );
                    runnable._jobFinished = jobFinished;
                    _awakeningEvent.Set();
                }
				runnable.IncRef();
            }
            finally
            {
                _jobsLock.Exit();
            }

            try
            {
                /**
                 * if a job is run from an asyncprocessor's thread, then do jobs for it until job finished
                 */
                if( procOfCurrentThread != null )
                {
                    WaitForSingleObjectJob waitJob = new WaitForSingleObjectJob( jobFinished );
                    procOfCurrentThread.QueueJob( JobPriority.Immediate, waitJob );
                    do
                    {
                        procOfCurrentThread.DoJobs();
                        /**
                         * if processor of current thread is finished we may not to
                         * wait until the WaitForSingleObjectJob finishes
                         */
                        if( procOfCurrentThread.Finished )
                        {
                            jobFinished.WaitOne();
                            break;
                        }
                    }
                    while( waitJob.NextWaitHandle != null );
                    procOfCurrentThread._numberOfRunJobs--;
                }
                else
                {
                    if( !Application.MessageLoop )
                    {
                        jobFinished.WaitOne();
                    }
                    else
                    {
                        IntPtr[] handles = new IntPtr[] { jobFinished.Handle };
                        MsgWaitForMultipleObjects( handles, 1, WindowsAPI.INFINITE );
                    }
                }
            }
            finally
            {
                if( runnable.DecRef() == 0 )
                {
                    jobFinished.Close();
                }
            }
            if( runnable._exception != null )
            {
                throw new AsyncProcessorException( runnable._exception );
            }
            return runnable._internalJob;
        }

        /**
         * maximum number of async operations capable to be processed simultaneously
         * restriction goes from win32
         */
        public const int _maxWaitableHandles = 64;

        /**
         * period to wait for switching into idle mode (in milliseconds )
         */
        public const int _defaultIdlePeriod = 300000; // 5 minutes

        protected virtual void ThreadFunction()
        {
            CallEventDelegatesSafe( ThreadStarted );

            if( _restartThread )
            {
                _finished = false;
            }
            _restartThread = false;

            /**
             * Add current thread to processor pool
             */
            AddToProcessorPool( Thread.CurrentThread, this );


            while( !_finished )
            {
                _numberOfRunJobs = _reenteringDoJobs = 0;
                try
                {
                    DoJobs();
                }
                catch( ThreadAbortException e )
                {
                    /**
                     * if ThreadAbortException caught then we try to start the new
                     * processor thread instead of this one, which is finished
                     */
                    _tracer.TraceException( e );
                    Thread.ResetAbort();
                    ThreadPriority priority = ThreadPriority;
                    string threadName = ThreadName;
                    _processorThread = new Thread( new ThreadStart( ProcessorThread ) );
                    _processorThread.IsBackground = true;
                    ThreadPriority = priority;
                    ThreadName = threadName;
                    _isThreadStarted = 0;
                    _restartThread = _finished = true;
                }
                catch( Exception e )
                {
                    _tracer.TraceException( e );
                    if( _exceptionHandler != null )
                    {
                        _exceptionHandler( e );
                    }
                }
            }

            /**
             * clean async processor pool from current thread
             */
            CleanProcessorPool( Thread.CurrentThread );

            CallEventDelegatesSafe( ThreadFinished );

            if( _restartThread )
            {
                StartThread();
            }
        }

        public virtual void DoJobs()
        {
            if( _finished )
            {
                return;
            }

            ++_reenteringDoJobs;

            try
            {
                AbstractJob     job;
                int             i;
                int             timeout;

                timeout = ProcessTimedJobs();
                // i - ticks until idle mode
                i = Timeout.Infinite;
                if( _jobQueue.Count == 0 && _idleJobQueue.Count > 0 )
                {
                    i = _idlePeriod - GetIdleDuration();
                    if( i < 0 )
                    {
                        i = 0;
                    }
                }
                if( i != Timeout.Infinite && ( timeout == Timeout.Infinite || timeout > i  ) )
                {
                    timeout = i;
                }

                i = WaitEvents( timeout );
                if( _finished )
                {
                    return;
                }

                if( i == WaitHandle.WaitTimeout )
                {
                    /**
                     * in idle mode execute only one job if any
                     */
                    if( _idleJobQueue.Count > 0 && GetIdleDuration() >= _idlePeriod )
                    {
                        job = (AbstractJob)_idleJobQueue.Pop();
                        _uniqueIdleJobs.Remove( job );
                        ExecuteJob( job );
                    }
                    return;
                }

                /**
                 * execute jobs if no timeout occured
                 */
                try
                {
                    /**
                     * if we get a signal that one of the earlier started async operations should be continued
                     */
                    if( i > 0 )
                    {
                        WaitHandle handle = _handles[ i ];
                        job = ( AbstractJob ) _startedJobs[ handle ];
                        _startedJobs.Remove( handle );
                        ExecuteJob( job );
                    }
                        /**
                         * else we get the awakening signal, so either queue is not empty or we are to finish thread
                         */
                    else
                    {
                        if( _empty && FillingEmptyQueue != null )
                        {
                            FillingEmptyQueue( this, EventArgs.Empty );
                        }
                        job = null;
                        int count;
                        _jobsLock.Enter();
                        try
                        {
                            count = _jobQueue.Count;
                            if( count > 0 )
                            {
                                job = (AbstractJob) _jobQueue.Pop();
                                _uniqueJobs.Remove( job );
                                --count;
                            }
                        }
                        finally
                        {
                            _jobsLock.Exit();
                        }
                        while( job != null )
                        {
                            ExecuteJob( job );
                            if( count == 0 || _finished || !_jobsLock.TryEnter() )
                            {
                                break;
                            }
                            try
                            {
                                job = null;
                                count = _jobQueue.Count;
                                if( count > 0 )
                                {
                                    job = (AbstractJob) _jobQueue.Pop();
                                    _uniqueJobs.Remove( job );
                                    --count;
                                }
                            }
                            finally
                            {
                                _jobsLock.Exit();
                            }
                        }
                    }
                }
                finally
                {
                    if( !_finished && _jobQueue.Count == 0 )
                    {
                        _jobsLock.Enter();
                        try
                        {
                            if( _empty = _jobQueue.Count == 0 )
                            {
                                _awakeningEvent.Reset();
                            }
                        }
                        finally
                        {
                            _jobsLock.Exit();
                        }
                        _empty = _empty && _reenteringDoJobs == 1 && _startedJobs.Count == 0;
                        if( _empty && QueueGotEmpty != null )
                        {
                            QueueGotEmpty( this, EventArgs.Empty );
                        }
                    }
                }
            }
            finally
            {
                --_reenteringDoJobs;
            }
        }

        /**
         * function where an asyncprocessor spends all its time when it's idle
         * waiting events scheme depends on number of events and the
         * ProcessMessages property value. If ProcessMessages is set to true then
         * waiting function processes windows messages by means of Application.DoEvents()
         */
        protected int WaitEvents( int timeout )
        {
            /**
             * if we're to wait only for awakening event is set to signaled state
             * then use WaitOne() because its performance is much better than WaitAny()
             */
            int handlesCount = _startedJobs.Count + 1;
            if( handlesCount == 1 && !_processMessages )
            {
                return ( _awakeningEvent.WaitOne( timeout, false ) ) ? 0 : WaitHandle.WaitTimeout;
            }
            if( handlesCount >= _maxWaitableHandles )
            {
                handlesCount = _maxWaitableHandles;
                if( _processMessages )
                {
                    --handlesCount;
                }
            }
            _handles[ 0 ] = _awakeningEvent;
            IEnumerator mapEnumerator = _startedJobs.GetEnumerator();
            for( int i = 1; i < handlesCount; ++i )
            {
                mapEnumerator.MoveNext();
                _handles[ i ] = (WaitHandle)( (HashMap.Entry) mapEnumerator.Current ).Key;
            }

            int result = ( _processMessages ) ?
                MsgWaitForMultipleObjects( handlesCount, timeout ) :
                WaitForMultipleObjects( handlesCount, timeout );
            if( result < 0 )
            {
                RemoveInvalidHandles();
                result = WaitEvents( timeout );
            }
            return result;
        }

        /**
         * check for obsolete timed jobs if any, returns number of ticks till next job
         * if returned value is zero then at least one obsolete job was executed
         */
        protected int ProcessTimedJobs()
        {
            if( _timedJobs.Count == 0 )
            {
                return Timeout.Infinite;
            }
            DateTime nearest = _timedJobs.GetMinimumEntry().Priority;
            DateTime now = DateTime.Now;
            if( nearest > now )
            {
                return (int) (( nearest.Ticks - now.Ticks ) / 10000 );
            }
            do
            {
                AbstractJob job = (AbstractJob)_timedJobs.Pop();
                HashMap.Entry E = _timedJobCounts.GetEntry( job );
                if( E != null && ( (int) E.Value ) > 1 )
                {
                    E.Value = (int) E.Value - 1;
                }
                else
                {
                    if( E != null )
                    {
                        _timedJobCounts.Remove( job );
                    }
                    ExecuteJob( job );
                }
                if( _timedJobs.Count == 0 )
                {
                    break;
                }
                nearest = _timedJobs.GetMinimumEntry().Priority;
            } while( nearest <= DateTime.Now );
            return 0;
        }

        /**
         * execute all jobs here
         */
        protected virtual void ExecuteJob( AbstractJob job )
        {
            AbstractJob lastExecutedJob = _currentJob;
            _currentJob = job;
            if( JobStartingDelegate != null )
            {
                JobStartingDelegate( this, EventArgs.Empty );
            }
            try
            {
                ReenteringJob reenteringJob = job as ReenteringJob;
                if( reenteringJob != null )
                {
                    ReenteringJob sameJob = (ReenteringJob) _startedReenteringJobs.GetKey( reenteringJob );
                    if( sameJob == null )
                    {
                        reenteringJob._processor = this;
                        reenteringJob.Interrupted = false;
                        _startedReenteringJobs.Add( reenteringJob );
                    }
                    else
                    {
                        sameJob.Interrupted = true;
                        PushJob( reenteringJob, JobPriority.Lowest );
                        return;
                    }
                }
                MethodInvoker method = job.NextMethod;
                job.InvokeAfterWait( method, _nullHandle );
                method();
                WaitHandle handle = job.NextWaitHandle;
                if( handle == _nullHandle )
                {
                    /**
                     * setting null handle means that job is finished
                     */
                    job.InvokeAfterWait( method, handle = null );
                }
                if( handle != null )
                {
                    int timeout = job.Timeout;
                    if( timeout != Timeout.Infinite )
                    {
                        QueueTimedJob( DateTime.Now.AddMilliseconds( timeout ),
                            new DelegateJob( new ProcessTimeoutOfStartedJobDelegate(
                            ProcessTimeoutOfStartedJob ), new object[] { handle } ) );
                    }
                    _startedJobs.Add( handle, job );
                }
                else
                {
                    if( reenteringJob != null )
                    {
                        _startedReenteringJobs.Remove( job );
                    }
                }
            }
            catch( Exception exc )
            {
                exc = Utils.GetMostInnerException( exc );
                if( !( exc is ThreadAbortException ) )
                {
                    throw new AsyncProcessorException( exc );
                }
            }
            finally
            {
                if( JobFinishedDelegate != null )
                {
                    JobFinishedDelegate( this, EventArgs.Empty );
                }
                _currentJob = lastExecutedJob;
            }
        }

        private delegate void ProcessTimeoutOfStartedJobDelegate( WaitHandle handle );

        private void ProcessTimeoutOfStartedJob( WaitHandle handle )
        {
            HashMap.Entry entry = _startedJobs.GetEntry( handle );
            if( entry != null )
            {
                ( (AbstractJob) entry.Value ).FireTimeout();
                _startedJobs.Remove( handle );
            }
        }

        private void QueueTimedJob( DateTime when, AbstractJob job )
        {
            _timedJobs.Push( when, job );
            HashMap.Entry E = _timedJobCounts.GetEntry( job );
            if( E == null )
            {
                _timedJobCounts.Add( job, 1 );
            }
            else
            {
                E.Value = (int) E.Value + 1;
            }
        }

        private void CancelTimedJobsImpl( JobFilter filter )
        {
            _timedJobCounts.Clear();
            DateTimePriorityQueue filteredQueue = new DateTimePriorityQueue();
            while( _timedJobs.Count > 0 )
            {
                DateTimePriorityQueue.QueueEntry E = _timedJobs.PopEntry();
                AbstractJob job = (AbstractJob) E.Value;
                if( filter( job ) )
                {
                    ICancelable cjob = job as ICancelable;
                    if( cjob != null )
                    {
                        cjob.OnCancel();
                    }
                }
                else
                {
                    DateTime when = E.Priority;
                    filteredQueue.Push( when, job );
                    HashMap.Entry Entry = _timedJobCounts.GetEntry( job );
                    if( Entry == null )
                    {
                        _timedJobCounts.Add( job, 1 );
                    }
                    else
                    {
                        Entry.Value = (int) Entry.Value + 1;
                    }
                }
            }
            _timedJobs = filteredQueue;
        }

        private bool QueueIdleJobImpl( JobPriority priority, AbstractJob job )
        {
            if( _uniqueIdleJobs.Contains( job ) )
            {
                return false;
            }
            _idleJobQueue.Push( (int) priority, job );
            _uniqueIdleJobs.Add( job );
            return true;
        }

        /**
         * gets idle duration at the moment
         */
        private static int GetIdleDuration()
        {
            if( _platform == PlatformID.Win32NT )
            {
                LASTINPUTINFO lii = new LASTINPUTINFO();
                lii.cbSize = 8;
                WindowsAPI.GetLastInputInfo( ref lii );
                return (int)( Kernel32Dll.GetTickCount() - lii.dwTime );
            }
            else
            {
                if( Cursor.Position != _lastCursorPos )
                {
                    _lastCursorPos = Cursor.Position;
                    _firstNoUserActivityTick = Kernel32Dll.GetTickCount();
                    return 0;
                }
                return (int)( Kernel32Dll.GetTickCount() - _firstNoUserActivityTick );
            }
        }

        /**
         * Class-scope wait functions wait for specified number of handles
         * stored in the _handles array
         */

        private const uint ERROR_INVALID_HANDLE = 6;

        /// <summary>
        /// /// Waits for multiple objects in the _handles array.
        /// </summary>
        /// <param name="handlesCount">Number of of waitable handles.</param>
        /// <param name="timeout">Value of timeout.</param>
        /// <returns>WaitHandle.WaitTimeout -- timeout, less than zero -- one of the handles is invalid,
        /// otherwise -- index of signaled handle.</returns>
        protected int WaitForMultipleObjects( int handlesCount, int timeout )
        {
            for( int i = 0; i < handlesCount; ++i )
            {
                _winHandles[ i ] = _handles[ i ].Handle;
            }
            uint waitResult = InteropWinApi.WaitForMultipleObjects(
                (uint) handlesCount, _winHandles, false, (uint) timeout );
            if( waitResult == WindowsAPI.WAIT_TIMEOUT )
            {
                return WaitHandle.WaitTimeout;
            }
            waitResult -= WindowsAPI.WAIT_OBJECT_0;
            if( waitResult < handlesCount )
            {
                return (int) waitResult;
            }
            int error = Marshal.GetLastWin32Error();
            if( error != ERROR_INVALID_HANDLE )
            {
                throw new Exception( "WaitForMultipleObjects failed. Error code: " + error );
            }
            return -1;
        }

        protected int MsgWaitForMultipleObjects( int handlesCount, int timeout )
        {
            for( int i = 0; i < handlesCount; ++i )
            {
                _winHandles[ i ] = _handles[ i ].Handle;
            }
            return MsgWaitForMultipleObjects( _winHandles, handlesCount, (uint) timeout );
        }

        /// <summary>
        /// Waits for multiple objects processing windows messages if any.
        /// </summary>
        /// <param name="handles">Array of handles.</param>
        /// <param name="handlesCount">Number of handles.</param>
        /// <param name="timeout">Timeout to wait.</param>
        /// <returns>WaitHandle.WaitTimeout -- timeout, less than zero -- one of the handles is invalid,
        /// otherwise -- index of signaled handle.</returns>
        protected static int MsgWaitForMultipleObjects( IntPtr[] handles, int handlesCount, uint timeout )
        {
            for( ; ; )
            {
                uint startWait = Kernel32Dll.GetTickCount();
                uint waitResult = InteropWinApi.MsgWaitForMultipleObjectsEx(
                    (uint) handlesCount, handles, timeout, InteropWinApi.QS_ALLINPUT, InteropWinApi.MWMO_INPUTAVAILABLE );
                if( waitResult == WindowsAPI.WAIT_FAILED )
                {
                    int error = Marshal.GetLastWin32Error();
                    if( error != ERROR_INVALID_HANDLE )
                    {
                        throw new Exception( "MsgWaitForMultipleObjects failed. Error code: " + error );
                    }
                    return -1;
                }
                if( waitResult == WindowsAPI.WAIT_TIMEOUT )
                {
                    return WaitHandle.WaitTimeout;
                }
                waitResult -= WindowsAPI.WAIT_OBJECT_0;
                if( waitResult < handlesCount )
                {
                    return (int) waitResult;
                }
                Application.DoEvents();
                uint spentTicks = Kernel32Dll.GetTickCount() - startWait;
                timeout = ( spentTicks >= timeout ) ? 0 : timeout - spentTicks;
            }
        }

        private void RemoveInvalidHandles()
        {
            IntPtr[] oneHandle = new IntPtr[ 1 ];
            do
            {
                object handle2Remove = null;
                foreach( HashMap.Entry e in _startedJobs )
                {
                    oneHandle[ 0 ] = ( (WaitHandle) e.Key ).Handle;
                    if( MsgWaitForMultipleObjects( oneHandle, 1, 0 ) < 0 )
                    {
                        // the handle is invalid!
                        handle2Remove = e.Key;
                        break;
                    }
                }
                if( handle2Remove != null )
                {
                    _startedJobs.Remove( handle2Remove );
                    continue;
                }
            } while( false );
        }

        private void CallEventDelegatesSafe( EventHandler handler )
        {
            if( handler != null )
            {
                try
                {
                    handler( this, EventArgs.Empty );
                }
                catch( Exception e )
                {
                    _tracer.TraceException( e );
                    if( _exceptionHandler != null )
                    {
                        _exceptionHandler( e );
                    }
                }
            }
        }

        /**
         * Working with pool of AsyncProcesors
         */
        protected static void AddToProcessorPool( Thread thread, AsyncProcessor processor )
        {
            lock( _processorPool )
            {
                if( _processorPool.Contains( thread ) )
                {
                    throw new InvalidOperationException(
                        "An async processor is already registered for thread (id=" + thread.GetHashCode() + ")" );
                }
                _processorPool[ thread ] = processor;
            }
        }

        protected static void CleanProcessorPool( Thread thread )
        {
            lock( _processorPool )
            {
                _processorPool.Remove( thread );
            }
        }

        protected static AsyncProcessor GetProcessorFromPool( Thread thread )
        {
            lock( _processorPool )
            {
                return (AsyncProcessor) _processorPool[ thread ];
            }
        }

        public static AsyncProcessor[] GetAllPooledProcessors()
        {
            ArrayList procs = new ArrayList();
            lock( _processorPool )
            {
                foreach( HashMap.Entry e in _processorPool )
                {
                    procs.Add( e.Value );
                }
            }
            return (AsyncProcessor[]) procs.ToArray( typeof( AsyncProcessor ) );
        }

        private void EndWork()
        {
            _finished = true;
        }

        /**
         * ProcessorThread is instance function used for thread creation in contructor
         * To change behaviour of AsyncProcessor, override ThreadFunction()
         */
        private void ProcessorThread()
        {
            ThreadFunction();
        }

        protected bool                      _finished;
        protected bool                      _restartThread;
        protected bool                      _empty;
        protected bool                      _processMessages;
        protected bool                      _isReenterable;
        protected int                       _isThreadStarted;
        protected int                       _idlePeriod;
        protected int                       _numberOfRunJobs;
        protected int                       _reenteringDoJobs;
        protected PriorityQueue             _jobQueue;
        protected HashSet                   _uniqueJobs;
        protected PriorityQueue             _idleJobQueue;
        protected HashSet                   _uniqueIdleJobs;
        protected DateTimePriorityQueue     _timedJobs;
        protected HashMap                   _timedJobCounts;
        protected HashMap                   _startedJobs;
        protected HashSet                   _startedReenteringJobs;
        protected AsyncExceptionHandler     _exceptionHandler;
        protected ManualResetEvent          _awakeningEvent;
        protected Thread                    _processorThread;
        protected Tracer                    _tracer;
        protected EventHandler              _threadStarted;
        protected EventHandler              _threadFinished;
        protected EventHandler              _fillingEmptyQueue;
        protected EventHandler              _queueGotEmpty;
        protected AbstractJob               _currentJob;

        private SpinWaitLock                _jobsLock = new SpinWaitLock();
        private WaitHandle[]                _handles = new WaitHandle[ _maxWaitableHandles ];
        private IntPtr[]                    _winHandles = new IntPtr[ _maxWaitableHandles ];
        private static HashMap              _processorPool = new HashMap();
        private static PlatformID           _platform = Environment.OSVersion.Platform;
        private static uint                 _firstNoUserActivityTick = Kernel32Dll.GetTickCount();
        private static Point                _lastCursorPos = Cursor.Position;
        internal static WaitHandle          _nullHandle = new Mutex();

        #endregion

    	/// <summary>
    	/// AsyncProcessor Private Interop.
    	/// </summary>
    	internal static class InteropWinApi
    	{
    		[DllImport("user32", CharSet = CharSet.Auto, SetLastError = true)]
    		public static extern uint MsgWaitForMultipleObjectsEx(uint nCount, IntPtr[] pHandles, uint dwMilliseconds, uint dwWakeMask, uint dwFlags);

    		[DllImport("kernel32", CharSet = CharSet.Auto, SetLastError = true)]
    		public static extern uint WaitForMultipleObjects(uint nCount, IntPtr[] pHandles, bool fWaitAll, uint dwMilliseconds);

    		private const uint QS_KEY = 0x1;

    		private const uint QS_MOUSEMOVE = 0x2;

    		private const uint QS_MOUSEBUTTON = 0x4;

    		private const uint QS_POSTMESSAGE = 0x8;

    		private const uint QS_TIMER = 0x10;

    		private const uint QS_PAINT = 0x20;

    		private const uint QS_SENDMESSAGE = 0x40;

    		private const uint QS_HOTKEY = 0x80;

    		private const uint QS_RAWINPUT = 0x400;

    		internal const uint QS_ALLINPUT = (((QS_MOUSEMOVE | QS_MOUSEBUTTON) | QS_KEY | QS_RAWINPUT) | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY | QS_SENDMESSAGE);

    		internal const uint MWMO_INPUTAVAILABLE = 4;
    	}
    }

    internal class EqualsJobFilter
    {
        private AbstractJob _job;

        internal EqualsJobFilter( AbstractJob job )
        {
            _job = job;
        }

        public bool DoFilter( AbstractJob job )
        {
            return _job.Equals( job );
        }
    }
}
