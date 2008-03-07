/// <copyright company="JetBrains">
/// Copyright © 2003-2008 JetBrains s.r.o.
/// You may distribute under the terms of the GNU General Public License, as published by the Free Software Foundation, version 2 (see License.txt in the repository root folder).
/// </copyright>

using System;
using System.Collections;
using System.Threading;
using System.Windows.Forms;
using JetBrains.Omea.AsyncProcessing;
using JetBrains.Omea.Base;
using JetBrains.Omea.OpenAPI;
using NUnit.Framework;

namespace OmniaMeaBaseTests
{

    [TestFixture]
    public class AsyncProcessorTests
    {
        internal class SleepJob : AbstractJob
        {
            public SleepJob( int ticks )
                : base()
            {
                _ticks = ticks;
            }
            protected override void Execute()
            {
                ++_jobCount;
                if( _ticks > 0 )
                {
                    Thread.Sleep( _ticks );
                }
            }
            private int _ticks;
        }

        [Test]
        public void TestSleep()
        {
            AsyncProcessor processor = new AsyncProcessor();

            int start = Environment.TickCount;
            // wait for two seconds in the thread of processor
            using( processor )
            {
                processor.QueueJob( new SleepJob( 2000 ));
                Thread.Sleep( 200 );
            }

            if( Environment.TickCount - start < 2000 )
            {
                Console.WriteLine( "Environment.TickCount=" + Environment.TickCount + " start=" + start );
                throw new Exception( "TestSleep(): sleep work finished earlier than it should." );
            }
        }

        internal class ExceptionRaiser : AbstractJob
        {
            protected override void Execute()
            {
                throw new Exception( "This is rather stange but a job!" );
            }
        }

        [Test]
        public void TestExceptionDelegate()
        {
            AsyncProcessor processor = new AsyncProcessor();

            _exceptionHandled = false;
            using( processor )
            {
                processor.ExceptionHandler = new AsyncExceptionHandler( ExceptionHandler );
                processor.QueueJob( new ExceptionRaiser() );
                Thread.Sleep( 200 );
            }
            if( !_exceptionHandled )
            {
                throw new Exception( "TestExceptionDelegate(): Async exception handling is not working." );
            }
        }

        private void ExceptionHandler( Exception e )
        {
            //Console.WriteLine( e.ToString() );
            _exceptionHandled = true;
        }
        private bool _exceptionHandled;

        internal class MultipleStepJob : AbstractJob
        {
            public MultipleStepJob()
            {
                OnTimeout += new MethodInvoker( OnTimeoutHandler );
            }
            protected override void Execute()
            {
                if( ++counter == 5 )
                {
                    if( Timeout != System.Threading.Timeout.Infinite )
                    {
                        // emulate hang-up
                        ManualResetEvent resetEvent = new ManualResetEvent( false );
                        InvokeAfterWait( new MethodInvoker( Execute ), resetEvent );
                    }
                    return;
                }
                InvokeAfterWait( new MethodInvoker( Execute ), new Mutex() );
            }
            private void OnTimeoutHandler()
            {
                ++counter;
            }
            public int counter = 0;
        }
                                   
        [Test]
        public void TestMultipleStepJob()
        {
            MultipleStepJob job = new MultipleStepJob();
            AsyncProcessor processor = new AsyncProcessor();

            using( processor )
            {
                processor.QueueJob( job );
                Thread.Sleep( 200 );
            }

            if( job.counter != 5 )
            {
                throw new Exception( "TestMultipleStepJob(): Invalid multiple step async processing. " + job.counter );
            }
        }

        [Test]
        public void TestMultipleStepJobWithTimeout()
        {
            MultipleStepJob job = new MultipleStepJob();
            AsyncProcessor processor = new AsyncProcessor();

            using( processor )
            {
                job.Timeout = 100;
                processor.QueueJob( job );
                Thread.Sleep( 200 );
            }

            if( job.counter != 6 )
            {
                throw new Exception( "TestMultipleStepJobWithTimeout(): Invalid multiple step async processing. " + job.counter );
            }
        }

        internal class TimedJob : AbstractJob, ICancelable
        {
            protected override void Execute()
            {
                ++_jobCount;
                //Console.Out.WriteLine( _jobCount );
            }

            public void OnCancel()
            {
                --_jobCount;
                //Console.Out.WriteLine( _jobCount );
            }
        }

        [Test]
        public void TestTimedJobs()
        {
            AsyncProcessor processor = new AsyncProcessor();
            _jobCount = 0;
            using( processor )
            {
                processor.QueueJobAt( DateTime.Now.AddSeconds( -1 ), new TimedJob() );
                processor.QueueJobAt( DateTime.Now.AddSeconds( 1 ), new TimedJob() );
                processor.QueueJobAt( DateTime.Now.AddSeconds( 2 ), new TimedJob() );
                processor.QueueJobAt( DateTime.Now.AddSeconds( 2 ), new TimedJob() );
                processor.QueueJobAt( DateTime.Now.AddSeconds( 2 ), new TimedJob() );
                processor.QueueJobAt( DateTime.Now.AddSeconds( 3 ), new TimedJob() );
                processor.QueueJobAt( DateTime.Now.AddSeconds( 4 ), new TimedJob() );
                Thread.Sleep( 200 );
                if( _jobCount != 1 )
                    throw new Exception( "First timed job failed. _jobCount = " + _jobCount );
                Thread.Sleep( 1000 );
                if( _jobCount != 2 )
                    throw new Exception( "Second timed job failed. _jobCount = " + _jobCount );
                Thread.Sleep( 1000 );
                if( _jobCount != 5 )
                    throw new Exception( "Third-to-fifth  timed jobs failed. _jobCount = " + _jobCount );
                Thread.Sleep( 1000 );
                if( _jobCount != 6 )
                    throw new Exception( "Sixth timed job failed. _jobCount = " + _jobCount );
                Thread.Sleep( 1000 );
            }
            if( _jobCount != 7 )
                throw new Exception( "Seventh timed job failed. _jobCount = " + _jobCount );
        }

        internal class TimedMergedJob : AbstractJob
        {
            protected override void Execute()
            {
                ++_jobCount;
                //Console.Out.WriteLine( _jobCount );
            }
            public override bool Equals(object obj)
            {
                return obj is TimedMergedJob;
            }

            public override int GetHashCode()
            {
                return 31415926;
            }
        }

        [Test]
        public void TestTimedMergedJob()
        {
            AsyncProcessor processor = new AsyncProcessor();
            _jobCount = 0;
            using( processor )
            {
                processor.QueueJobAt( DateTime.Now.AddSeconds( 1 ), new TimedMergedJob() );
                processor.QueueJobAt( DateTime.Now.AddSeconds( 2 ), new TimedMergedJob() );
                processor.QueueJobAt( DateTime.Now.AddSeconds( 3 ), new TimedMergedJob() );
                processor.QueueJobAt( DateTime.Now.AddSeconds( 4 ), new TimedMergedJob() );
                Thread.Sleep( 4200 );
            }
            if( _jobCount != 1 )
            {
                throw new Exception( "TestTimedMergedJob(): Timed merged job failed. _jobCount = " + _jobCount );
            }
        }

        private static int _jobCount;

        [Test]
        public void TestMultipleAsyncProcessors()
        {
            const int count = 300000;
            AsyncProcessor lockProcessor1 = new AsyncProcessor( false );
            AsyncProcessor lockProcessor2 = new AsyncProcessor( false );
            AsyncProcessor lockProcessor3 = new AsyncProcessor( false );
            AsyncProcessor lockProcessor4 = new AsyncProcessor( false );
            ArrayList lockJobs = new ArrayList( count );
            for( int i = 0; i < count; ++i )
            {
                lockJobs.Add( new LockJob() );
            }

            for( int i = 0; i < count; ++i )
            {
                lockProcessor1.QueueJob( (AbstractJob) lockJobs[ i ] );
                lockProcessor2.QueueJob( (AbstractJob) lockJobs[ i ] );
                lockProcessor3.QueueJob( (AbstractJob) lockJobs[ i ] );
                lockProcessor4.QueueJob( (AbstractJob) lockJobs[ i ] );
            }
            lockProcessor1.QueueEndOfWork();
            lockProcessor2.QueueEndOfWork();
            lockProcessor3.QueueEndOfWork();
            lockProcessor4.QueueEndOfWork();
            lockProcessor1.StartThread();
            lockProcessor2.StartThread();
            lockProcessor3.StartThread();
            lockProcessor4.StartThread();
            lockProcessor1.WaitUntilFinished();
            lockProcessor2.WaitUntilFinished();
            lockProcessor3.WaitUntilFinished();
            lockProcessor4.WaitUntilFinished();
        }
        private static SpinWaitLock _lock = new SpinWaitLock();
        private static void WorkUnit()
        {
            for( int i = 0; i < 10;  )
                i = i + 1;                
        }
        internal class LockJob : AbstractJob
        {
            protected override void Execute()
            {
                /*lock( _lock )
                {
                    WorkUnit();
                }*/
                _lock.Enter();
                try
                {
                    WorkUnit();
                }
                finally
                {
                    _lock.Exit();
                }
            }
        }

        [Test]
        public void TestAbortAsyncProThread()
        {
            AsyncProcessor processor = new AsyncProcessor();
            _jobCount = 0;
            using( processor )
            {
                processor.QueueJobAt( DateTime.Now.AddSeconds( -1 ), new TimedJob() );
                processor.QueueJobAt( DateTime.Now.AddSeconds( 2 ), new TimedJob() );
                Thread.Sleep( 500 );
                processor.Thread.Abort();
                // after thread was aborted async processor should restart its own thread
                // and remain able to process units of work
                processor.QueueJob( new TimedJob() );
                Thread.Sleep( 2000 );
            }
            if( _jobCount != 3 )
                throw new Exception( "TestAbortAsyncProThread() failed. _jobCount = " + _jobCount );
        }

        [Test]
        public void TestIdle()
        {
            Console.Out.WriteLine( "Starting TestIdle()" );
            AsyncProcessor processor = new AsyncProcessor( false );
            _jobCount = 0;
            using( processor )
            {
                processor.IdlePeriod = 500; 
                processor.StartThread();
                processor.QueueIdleJob( new TimedJob() );
                processor.QueueIdleJob( new TimedJob() );
                processor.QueueIdleJob( new TimedJob() );
                processor.QueueIdleJob( new TimedJob() );
                Thread.Sleep( 1500 );
            }
            if( _jobCount != 4 )
            {
                throw new Exception( "TestIdle() failed. _jobCount = " + _jobCount );
            }
        }

        internal class IdleRepeatedJob : AbstractJob
        {
            private AsyncProcessor _proc;

            internal IdleRepeatedJob( AsyncProcessor proc )
            {
                _proc = proc;
            }
            protected override void Execute()
            {
                if( ++_jobCount < 4 )
                {
                    _proc.QueueIdleJob( this );
                }
            }
        }

        [Test]
        public void TestIdleRepeated()
        {
            Console.Out.WriteLine( "Starting TestIdleRepetable()" );
            AsyncProcessor processor = new AsyncProcessor( false );
            _jobCount = 0;
            using( processor )
            {
                processor.IdlePeriod = 500; 
                processor.StartThread();
                processor.QueueIdleJob( new IdleRepeatedJob( processor) );
                Thread.Sleep( 1500 );
            }
            if( _jobCount != 4 )
            {
                throw new Exception( "TestIdle() failed. _jobCount = " + _jobCount );
            }
        }

        [Test]
        public void TestCancellation()
        {
            _jobCount = 0;
            AsyncProcessor processor = new AsyncProcessor();
            using( processor )
            {
                TimedJob job = new TimedJob();
                processor.QueueJobAt( DateTime.Now.AddSeconds( 4 ), job );
                processor.QueueJobAt( DateTime.Now.AddSeconds( 4.01 ), job );
                processor.CancelTimedJobs( job );
                processor.QueueEndOfWork();
                processor.WaitUntilFinished();
                if( _jobCount != -2 )
                {
                    throw new Exception( "TestCancellation() failed. _jobCount = " + _jobCount );
                }
            }
        }

        [Test]
        public void TestRunJob()
        {
            _jobCount = 0;
            AsyncProcessor processor = new AsyncProcessor();
            using( processor )
            {
                SleepJob job = new SleepJob( 400 );
                DateTime start = DateTime.Now;
                // sleep for 2 secodns in processor's thread
                processor.RunJob( job );
                processor.RunJob( job );
                if( start.AddSeconds( 0.79 ) >= DateTime.Now )
                {
                    throw new Exception( "TestRunJob() failed." );
                }
            }
        }

        [Test]
        public void TestNullHandleAfterFinish()
        {
            AsyncProcessor processor = new AsyncProcessor();
            using( processor )
            {
                SleepJob job = new SleepJob( 0 );
                processor.RunJob( job );
                if( job.NextWaitHandle != null )
                {
                    throw new Exception( "TestNullHandleAfterFinish() failed." );
                }
            }
        }

        internal class EnumeratorJob : ReenteringEnumeratorJob
        {
            public EnumeratorJob( int cycles )
                : this( cycles, 0 ) {}
            public EnumeratorJob( int cycles, int ticks )
            {
                _cycles = cycles;
                _ticks = ticks;
            }

            public override void EnumerationStarting()
            {
                _internalJob = new SleepJob( _ticks );
            }

            public override void EnumerationFinished()
            {
                ++_jobCount;
            }

            public override AbstractJob GetNextJob()
            {
                return ( _cycles-- > 0 ) ? _internalJob : null;
            }

            public override string Name
            {
                get { return ""; }
                set {}
            }

            private int _cycles;
            private int _ticks;
            private SleepJob _internalJob;
        }

        [Test]
        public void TestReenteringEnumerator()
        {
            _jobCount = 0;
            AsyncProcessor processor = new AsyncProcessor();
            using( processor )
            {
                const int cycles = 10;
                processor.QueueJob( new EnumeratorJob( cycles ) );
                Thread.Sleep( 200 );
                if( _jobCount != cycles + 1 )
                {
                    throw new Exception( "TestReenteringEnumerator() failed. _jobCount = " + _jobCount );
                }
            }
        }

        internal class GroupJob : ReenteringGroupJob
        {
            public GroupJob( int totaljobs, int simutaneousjobs )
                : base( simutaneousjobs )
            {
                _totaljobs = totaljobs;
                ReenteringPriority = JobPriority.AboveNormal;
            }

            public override AbstractJob GetNextJob()
            {
                return ( _totaljobs-- > 0 ) ? new SleepJob( 0 ) : null;
            }

            public override void GroupStarting()
            {
                _jobCount += 1;
            }

            public override void GroupFinished()
            {
                _jobCount += 2;
            }

            public override string Name
            {
                get { return ""; }
                set {}
            }

            private int         _totaljobs;
        }

        [Test]
        public void TestReenteringGroup()
        {
            _jobCount = 0;
            AsyncProcessor processor = new AsyncProcessor();
            using( processor )
            {
                const int total = 10;
                processor.QueueJob( new GroupJob( total,  1 ) );
                Thread.Sleep( 200 );
                if( _jobCount != total + 3 )
                {
                    throw new Exception( "GroupJob( 10, 1 ) failed. _jobCount = " + _jobCount );
                }
                _jobCount = 0;
                processor.QueueJob( new GroupJob( total,  5 ) );
                Thread.Sleep( 200 );
                if( _jobCount != total + 3 )
                {
                    throw new Exception( "GroupJob( 10, 5 ) failed. _jobCount = " + _jobCount );
                }
                _jobCount = 0;
                processor.QueueJob( new GroupJob( total,  10 ) );
                Thread.Sleep( 200 );
                if( _jobCount != total + 3 )
                {
                    throw new Exception( "GroupJob( 10, 10 ) failed. _jobCount = " + _jobCount );
                }
                _jobCount = 0;
                processor.QueueJob( new GroupJob( total,  15 ) );
                Thread.Sleep( 200 );
                if( _jobCount != total + 3 )
                {
                    throw new Exception( "GroupJob( 10, 15 ) failed. _jobCount = " + _jobCount );
                }
            }
        }

        [Test]
        public void TestReenteringJobMixedWithOrdinaryJobs()
        {
            _jobCount = 0;
            AsyncProcessor processor = new AsyncProcessor( false );
            using( processor )
            {
                const int total = 10;
                processor.QueueJob( JobPriority.AboveNormal, new SleepJob( 0 ) );
                processor.QueueJob( JobPriority.AboveNormal, new SleepJob( 0 ) );
                processor.QueueJob( JobPriority.AboveNormal, new SleepJob( 0 ) );
                processor.QueueJob( JobPriority.AboveNormal, new SleepJob( 0 ) );
                processor.QueueJob( new GroupJob( total,  total ) );
                processor.QueueJob( new SleepJob( 300 ) );
                processor.QueueJob( new SleepJob( 0 ) );
                processor.QueueJob( new SleepJob( 0 ) );
                processor.QueueJob( JobPriority.Immediate, new SleepJob( 0 ) );
                processor.StartThread();
                Thread.Sleep( 200 );
                if( _jobCount != total + 7 )
                {
                    throw new Exception( "TestReenteringJobMixedWithOrdinaryJobs() failed (first check). _jobCount = " + _jobCount );
                }
                Thread.Sleep( 150 );
                if( _jobCount != total + 11 )
                {
                    throw new Exception( "TestReenteringJobMixedWithOrdinaryJobs() failed (second check). _jobCount = " + _jobCount );
                }
            }
        }

        [Test]
        public void TestMergingReenteringJobs()
        {
            _jobCount = 0;
            AsyncProcessor processor = new AsyncProcessor();
            using( processor )
            {
                const int total = 5;
                EnumeratorJob job = new EnumeratorJob( total, 100 );
                processor.QueueJob( job );
                Thread.Sleep( 250 );
                processor.QueueJob( job );
                Thread.Sleep( 550 );
                if( _jobCount != total + 2 )
                {
                    throw new Exception( "TestMergingReenteringJobs() failed. _jobCount = " + _jobCount );
                }
            }
        }

        [Test]
        public void TestUnconditionalFinishOnDispose()
        {
            _jobCount = 0;
            AsyncProcessor processor = new AsyncProcessor();
            processor.QueueJob( new SleepJob( 500 ) );
            processor.QueueJob( new ExceptionRaiser() );
            using( processor )
            {
                Thread.Sleep( 200 );
            }
            // only sleep job should be executed, otherwise an exception will be raised
            if( _jobCount != 1 )
            {
                throw new Exception( "UnconditionalFinishOnDispose() failed. _jobCount = " + _jobCount );
            }
        }

        [Test]
        public void TestRunReenteringEnumerator()
        {
            _jobCount = 0;
            AsyncProcessor processor = new AsyncProcessor();
            using( processor )
            {
                const int total = 5;
                EnumeratorJob job = new EnumeratorJob( total, 100 );
                DateTime start = DateTime.Now;
                processor.RunJob( job );
                if( start.AddMilliseconds( total * 99 ) > DateTime.Now )
                {
                    throw new Exception( "TestRunReenteringEnumerator() finished too early. _jobCount = " + _jobCount );
                }
                if( _jobCount != total + 1 )
                {
                    throw new Exception( "TestRunReenteringEnumerator() failed. _jobCount = " + _jobCount );
                }
            }
        }

        [Test]
        public void TestRunReenteringGroup()
        {
            AsyncProcessor processor = new AsyncProcessor();
            using( processor )
            {
                const int total = 5;
                _jobCount = 0;
                GroupJob job = new GroupJob( total, 1 );
                processor.RunJob( job );
                if( _jobCount != total + 3 )
                {
                    throw new Exception( "TestRunReenteringGroup(): GroupJob( total, 1 ) failed. _jobCount = " + _jobCount );
                }
                _jobCount = 0;
                job = new GroupJob( total, 3 );
                processor.RunJob( job );
                if( _jobCount != total + 3 )
                {
                    throw new Exception( "TestRunReenteringGroup(): GroupJob( total, 3 ) failed. _jobCount = " + _jobCount );
                }
                _jobCount = 0;
                job = new GroupJob( total, 5 );
                processor.RunJob( job );
                if( _jobCount != total + 3 )
                {
                    throw new Exception( "TestRunReenteringGroup(): GroupJob( total, 5 ) failed. _jobCount = " + _jobCount );
                }
                _jobCount = 0;
                job = new GroupJob( total, 11 );
                processor.RunJob( job );
                if( _jobCount != total + 3 )
                {
                    throw new Exception( "TestRunReenteringGroup(): GroupJob( total, 11 ) failed. _jobCount = " + _jobCount );
                }
            }
        }

        [Test]
        public void TestRunMultipleStepJob()
        {
            MultipleStepJob job = new MultipleStepJob();
            AsyncProcessor processor = new AsyncProcessor();

            using( processor )
            {
                processor.RunJob( job );
            }

            if( job.counter != 5 )
            {
                throw new Exception( "TestRunMultipleStepJob(): multiple step async processing. " + job.counter );
            }
        }

        internal class NonBlockingRunJob : AbstractJob
        {
            private AsyncProcessor _pairProc;

            internal NonBlockingRunJob( AsyncProcessor pairProc )
            {
                _pairProc = pairProc;
            }
            protected override void Execute()
            {
                _pairProc.RunJob( new SleepJob( 1000 ) );
            }

        }
        
        [Test]
        public void TestNonBlockingRun()
        {
            _jobCount = 0;
            AsyncProcessor proc1 = new AsyncProcessor();
            AsyncProcessor proc2 = new AsyncProcessor();
            using( proc1 )
            {
                using( proc2 )
                {
                    proc1.QueueJob( JobPriority.Immediate, new NonBlockingRunJob( proc2 ) );
                    proc1.RunJob( new SleepJob( 0 ) );
                    Thread.Sleep( 100 );
                    if( _jobCount != 2 )
                    {
                        throw new Exception( "TestNonBlockingRun() failed. _jobCount = " + _jobCount  );
                    }
                }
            }
        }

        internal class RunJobInProcessor: AbstractJob
        {
            private AsyncProcessor _proc;
            private AbstractJob _job;
            public RunJobInProcessor( AsyncProcessor proc, AbstractJob job )
            {
                _proc = proc;
                _job = job;
            }
            protected override void Execute()
            {
                _proc.RunJob( _job );
            }
        }
        internal class RecurringRunJob: AbstractJob
        {
            private AsyncProcessor _proc1;
            private AsyncProcessor _proc2;
            public RecurringRunJob( AsyncProcessor proc1, AsyncProcessor proc2 )
            {
                _proc1 = proc1;
                _proc2 = proc2;
            }
            protected override void Execute()
            {
                if( ++_jobCount < 5 )
                {
                    _proc2.RunJob( new RunJobInProcessor( _proc1, this ) );
                }
            }
        }

        [Test]
        public void TestRecurringRunEqualJobs()
        {
            _jobCount = 0;
            AsyncProcessor proc1 = new AsyncProcessor();
            AsyncProcessor proc2 = new AsyncProcessor();
            RecurringRunJob job = new RecurringRunJob( proc1, proc2 );
            using( proc2 )
            {
                using( proc1 )
                {
                    proc1.RunJob( job );
                }
            }
            if( _jobCount != 5 )
            {
                throw new Exception( "TestRecurringRunEqualJobs() failed. _jobCount = " + _jobCount  );
            }
        }

        [Test]
        public void TestEmployCurrentThread()
        {
            _jobCount = 0;
            AsyncProcessor proc = new AsyncProcessor( false );
            using( proc )
            {
                proc.QueueJob( new SleepJob( 0 ) );
                proc.QueueJob( new SleepJob( 0 ) );
                proc.QueueJob( new SleepJob( 0 ) );
                proc.QueueEndOfWork();
                proc.EmployCurrentThread();
            }
            if( _jobCount != 3 )
            {
                throw new Exception( "TestEmployCurrentThread() failed. _jobCount = " + _jobCount  );
            }
        }

        internal class CrossRunJob: AbstractJob
        {
            private AsyncProcessor _proc1;
            private AsyncProcessor _proc2;
            internal static int _maxJobs = 0;
            public CrossRunJob( AsyncProcessor proc1, AsyncProcessor proc2 )
            {
                _proc1 = proc1;
                _proc2 = proc2;
            }
            protected override void Execute()
            {
                if( _jobCount < _maxJobs )
                {
                    ++_jobCount;
                    _proc1.RunJob( new CrossRunJob( _proc2, _proc1 ) );
                }
            }
        }

        [Test]
        public void TestCrossRunJob()
        {
            _jobCount = 0;
            CrossRunJob._maxJobs = AsyncProcessor._maxWaitableHandles * 2 - 2;
            AsyncProcessor proc1 = new AsyncProcessor();
            AsyncProcessor proc2 = new AsyncProcessor();
            CrossRunJob job = new CrossRunJob( proc1, proc2 );
            using( proc2 )
            {
                using( proc1 )
                {
                    proc2.RunJob( job );
                }
            }
            if( _jobCount != CrossRunJob._maxJobs )
            {
                throw new Exception( "TestCrossRunJob() failed. _jobCount = " + _jobCount  );
            }
        }

        [Test]
        public void TestCrossRunJobsWithFinalSingleBlockingRun()
        {
            _jobCount = 0;
            CrossRunJob._maxJobs = AsyncProcessor._maxWaitableHandles * 2 - 1;
            AsyncProcessor proc1 = new AsyncProcessor();
            AsyncProcessor proc2 = new AsyncProcessor();
            CrossRunJob job = new CrossRunJob( proc1, proc2 );
            using( proc1 )
            {
                using( proc2 )
                {
                    proc2.QueueJob( job );
                    Thread.Sleep( 1000 );
                }
            }
        }

        [Test]
        public void TestRunJobDuringFinishingAsyncProcessor()
        {
            _jobCount = 0;
            AsyncProcessor proc1 = new AsyncProcessor();
            AsyncProcessor proc2 = new AsyncProcessor();
            using( proc2 )
            {
                using( proc1 )
                {
                    proc2.QueueJob( JobPriority.Immediate, new SleepJob( 200 ) );
                    Thread.Sleep( 50 );
                    proc1.QueueJob( new RunJobInProcessor( proc2, new SleepJob( 200 ) ) );
                }
            }
            // test just will not hang-up
        }

        [Test]
        public void TestSimultaneousRunJobAndDispose()
        {
            _jobCount = 0;
            AsyncProcessor proc1 = new AsyncProcessor();
            AsyncProcessor proc2 = new AsyncProcessor();
            using( proc2 )
            {
                using( proc1 )
                {
                    for( int i = 0; i < 20; ++i )
                    {
                        proc1.QueueJob( JobPriority.Immediate, new SleepJob( 20 ) );
                    }
                    Thread.Sleep( 50 );
                    proc2.QueueJob( new RunJobInProcessor( proc1, new SleepJob( 100 ) ) );
                }
            }
            // test just will not hang-up
        }

        internal class JobRunner : AbstractJob
        {
            private AbstractJob _job;
            private AsyncProcessor _proc;
            public JobRunner( AbstractJob job, AsyncProcessor proc )
            {
                _job = job;
                _proc = proc;
            }
            protected override void Execute()
            {
                _proc.RunJob( _job );
            }
        }

        [Test][ExpectedException( typeof(Exception) )]
        public void TestRunNonUniqieJobs()
        {
            SleepJob job = new SleepJob( 200 );
            AsyncProcessor proc = new AsyncProcessor();
            AsyncProcessor proc1 = new AsyncProcessor();
            using( proc )
            {
                using( proc1 )
                {
                    proc.QueueJob( JobPriority.Immediate, job );
                    Thread.Sleep( 50 );
                    proc1.QueueJob( new JobRunner( job, proc ) );
                    Thread.Sleep( 50 );
                    proc.RunJob( job ); // this should throw the exception
                }
            }
        }

        [Test]
        public void TestRunUniqieJobs()
        {
            SleepJob job = new SleepJob( 200 );
            AsyncProcessor proc = new AsyncProcessor();
            AsyncProcessor proc1 = new AsyncProcessor();
            using( proc )
            {
                using( proc1 )
                {
                    proc.QueueJob( JobPriority.Immediate, job );
                    Thread.Sleep( 50 );
                    proc1.QueueJob( new JobRunner( job, proc ) );
                    Thread.Sleep( 50 );
                    proc.RunUniqueJob( job ); // this shouldn't throw an exception
                }
            }
        }

        private delegate int DelayedIntDelegate();
        private static int count;
        private static int count1;
        private const int e = 271828;
        private static int DelayedInt()
        {
            count1++;
            Thread.Sleep( 500 );
            return e;
        }
        private static DelayedIntDelegate _delayedIntDelegate = new DelayedIntDelegate( DelayedInt );
        private delegate void GetDelayedIntDelegate( AsyncProcessor proc, int dummy );
        private static void GetDelayedInt( AsyncProcessor proc, int dummy )
        {
            object result = proc.RunUniqueJob( _delayedIntDelegate );
            if( result == null )
            {
                throw new Exception( "RunUniqueJob() returned null!" );
            }
            if( (int) result != e )
            {
                throw new Exception( "RunUniqueJob() returned invalid value!" );
            }
            ++count;
        }

        [Test]
        public void TestRunUniqueJobReturns()
        {
            count1 = count = 0;
            using( AsyncProcessor proc = new AsyncProcessor() )
            {
                using( AsyncProcessor proc1 = new AsyncProcessor() )
                {
                    for( int i = 0; i < 20; ++i )
                    {
                        bool queued = proc.QueueJob( new GetDelayedIntDelegate( GetDelayedInt ), proc1, i );
                        if( !queued )
                        {
                            throw new Exception( "QueueJob() didn't actually queue the job" );
                        }
                    }
                    Thread.Sleep( 1100 );
                }
            }
            if( count != 20 || count1 != 2 )
            {
                throw new Exception( "RunUniqueJob() wasn't executed specified number of times, count = " +
                    count + ", count1 = " + count1 );
            }
        }

        internal class NamedJob : AbstractNamedJob
        {
            public override string Name
            {
                get { return "The Name"; }
                set {}
            }
            protected override void Execute()
            {
                Thread.Sleep( 200 );
            }
        }

        [Test]
        public void TestNamedJob()
        {
            AsyncProcessor proc = new AsyncProcessor();
            using( proc )
            {
                proc.QueueJob( new NamedJob() );
                Thread.Sleep( 100 );
                if( proc.CurrentJobName != new NamedJob().Name )
                {
                    throw new Exception( "TestNamedJob() failed." );
                }
            }
        }

        internal class RunNamedJob: AbstractJob
        {
            private AsyncProcessor _proc;
            public RunNamedJob( AsyncProcessor proc )
            {
                _proc = proc;
            }
            protected override void Execute()
            {
                _proc.RunJob( new NamedJob() );
            }
        }

        [Test]
        public void TestNamedRunJob()
        {
            AsyncProcessor proc1 = new AsyncProcessor();
            AsyncProcessor proc2 = new AsyncProcessor();
            using( proc1 )
            {
                using( proc2 )
                {
                    proc1.QueueJob( new RunNamedJob( proc2 ) );
                    Thread.Sleep( 100 );
                    if( proc2.CurrentJobName != new NamedJob().Name )
                    {
                        throw new Exception( "TestNamedRunJob() failed." );
                    }
                }
            }
        }

        [Test]
        public void TestAsyncProcessorEvents()
        {
            _jobCount = 0;
            AsyncProcessor proc = new AsyncProcessor( false );
            using( proc )
            {
                proc.ThreadStarted += new EventHandler( proc_ThreadStarted );
                proc.ThreadStarted += new EventHandler( proc_ThreadStarted1 );
                proc.ThreadStarted += new EventHandler( proc_ThreadStarted2 );
                proc.ThreadFinished += new EventHandler( proc_ThreadStarted );
                proc.ThreadFinished += new EventHandler( proc_ThreadStarted1 );
                proc.FillingEmptyQueue += new EventHandler( proc_FillingEmptyQueue );
                proc.QueueGotEmpty += new EventHandler( proc_QueueGotEmpty );
                proc.QueueGotEmpty += new EventHandler( proc_QueueGotEmpty1 );
                proc.JobStarting += new EventHandler( proc_JobStarting );
                proc.JobFinished += new EventHandler( proc_JobFinished );
                proc.JobQueued += new JetBrains.Omea.AsyncProcessing.AsyncProcessor.JobDelegate( proc_JobQueued );
                proc.QueueJob( new SleepJob( 0 ) );
                proc.QueueJob( new SleepJob( 0 ) );
                proc.QueueJob( new SleepJob( 0 ) );
                proc.QueueJob( new SleepJob( 0 ) );
                proc.QueueEndOfWork();
                proc.EmployCurrentThread();
            }
            if( _jobCount != 0 )
            {
                throw new Exception( "TestAsyncProcessorEvents() failed. _jobCount = " + _jobCount  );
            }
        }

        [Test]
        public void TestQueueGotEmpty()
        {
            _jobCount = 0;
            AsyncProcessor proc = new AsyncProcessor();
            using( proc )
            {
                proc.QueueGotEmpty += new EventHandler( proc_QueueGotEmpty );
                proc.ThreadFinished += new EventHandler( proc_ThreadStarted1 );
                proc.QueueJob( new SleepJob( 0 ) );
                proc.QueueJob( new EnumeratorJob( 3, 200 ) );
                Thread.Sleep( 300 );
                if( _jobCount != 3 )
                {
                    throw new Exception( "TestQueueGotEmpty() failed (first check). _jobCount = " + _jobCount  );
                }
                Thread.Sleep( 200 );
                if( _jobCount != 4 )
                {
                    throw new Exception( "TestQueueGotEmpty() failed (second check). _jobCount = " + _jobCount  );
                }
                Thread.Sleep( 300 );
                if( _jobCount != 8 )
                {
                    throw new Exception( "TestQueueGotEmpty() failed (third check). _jobCount = " + _jobCount  );
                }
            }
            if( _jobCount != 4 )
            {
                throw new Exception( "TestQueueGotEmpty() failed (final check). _jobCount = " + _jobCount  );
            }
        }

        private void proc_ThreadStarted( object sender, EventArgs e )
        {
            _jobCount += 5;
        }
        private void proc_ThreadStarted1( object sender, EventArgs e )
        {
            _jobCount -= 4;
        }
        private void proc_ThreadStarted2( object sender, EventArgs e )
        {
            _jobCount += 2;
        }
        private void proc_FillingEmptyQueue( object sender, EventArgs e )
        {
            _jobCount += 2;
        }
        private void proc_QueueGotEmpty( object sender, EventArgs e )
        {
            _jobCount += 3;
        }
        private void proc_QueueGotEmpty1( object sender, EventArgs e )
        {
            _jobCount -= 2;
        }
        private void proc_JobStarting( object sender, EventArgs e )
        {
            _jobCount -=2;
        }
        private void proc_JobFinished( object sender, EventArgs e )
        {
            _jobCount++;
        }
        private void proc_JobQueued( object sender, AbstractJob job )
        {
            _jobCount--;
        }

        class InvalidHandleJob : AbstractJob
        {
            protected override void Execute()
            {
                WaitHandle handle = new ManualResetEvent( false );
                InvokeAfterWait( new MethodInvoker( Execute ), handle );
                handle.SafeWaitHandle.Close();
            }
        }
        [Test]
        public void TestInvalidHandle()
        {
            _exceptionHandled = false;
            AsyncProcessor proc = new AsyncProcessor();
            using( proc )
            {
                proc.ExceptionHandler = new AsyncExceptionHandler( ExceptionHandler );
                proc.ProcessMessages = true;
                proc.QueueJob( new InvalidHandleJob() );
                Thread.Sleep( 500 );
            }
            if( _exceptionHandled )
            {
                throw new Exception( "There was an invalid handle exception!" );
            }
        }

        internal class StrangeJobException : Exception
        {
            public StrangeJobException( string message ) : base ( message ) {}
        }
        internal class ExceptionJob : AbstractJob
        {
            protected override void Execute()
            {
                Thread.Sleep( 200 );
                throw new StrangeJobException( "Such a strange job!" );
            }
            public override int GetHashCode()
            {
                return 0;
            }
            public override bool Equals(object obj)
            {
                return obj is ExceptionJob;
            }
        }
        internal class ExceptionJobRunner : AbstractJob
        {
            AsyncProcessor _proc;
            internal ExceptionJobRunner( AsyncProcessor proc )
            {
                _proc = proc;
            }
            protected override void Execute()
            {
                _proc.RunUniqueJob( new ExceptionJob() );
            }
        }
        [Test]
        public void TestRunWithException()
        {
            _exceptionHandled = false;
            AsyncProcessor proc = new AsyncProcessor();
            using( proc )
            {
                proc.ExceptionHandler = new AsyncExceptionHandler( ExceptionHandler );
                proc.QueueJob( new SleepJob( 200 ) );
                AsyncProcessor proc1 = new AsyncProcessor();
                AsyncProcessor proc2 = new AsyncProcessor();
                AsyncProcessor proc3 = new AsyncProcessor();
                AsyncProcessor proc4 = new AsyncProcessor();
                AsyncProcessor proc5 = new AsyncProcessor();
                proc1.QueueJob( new ExceptionJobRunner( proc ) );
                proc2.QueueJob( new ExceptionJobRunner( proc ) );
                proc3.QueueJob( new ExceptionJobRunner( proc ) );
                proc4.QueueJob( new ExceptionJobRunner( proc ) );
                proc5.QueueJob( new ExceptionJobRunner( proc ) );
                try
                {
                    proc.RunUniqueJob( new ExceptionJob() );
                }
                catch( Exception e )
                {
                    e = Utils.GetMostInnerException( e );
                    if( !( e is StrangeJobException ) )
                    {
                        throw new Exception( e.Message );
                    }
                }
                proc1.Dispose();
                proc2.Dispose();
                proc3.Dispose();
                proc4.Dispose();
                proc5.Dispose();
            }
        }

        class SpinWaitLockJob : AbstractJob
        {
            SpinWaitLock _lock;

            public SpinWaitLockJob( SpinWaitLock theLock )
            {
                _lock = theLock;
            }

            protected override void Execute()
            {
                _lock.Enter();
                Thread.Sleep( 0 );
                _lock.Enter();
                Thread.Sleep( 0 );
                _lock.Enter();
                Thread.Sleep( 0 );
                _lock.Enter();
                Thread.Sleep( 0 );
                _lock.Exit();
                Thread.Sleep( 0 );
                _lock.Exit();
                Thread.Sleep( 0 );
                _lock.Exit();
                Thread.Sleep( 0 );
                _lock.Exit();
            }
        }

        [Test]
        public void TestSpinWaitLock()
        {
            SpinWaitLock aLock = new SpinWaitLock();
            AsyncProcessor proc1 = new AsyncProcessor( false );
            AsyncProcessor proc2 = new AsyncProcessor( false );
            using( proc1 )
            {
                using( proc2 )
                {
                    for( int i = 0; i < 100000; ++i )
                    {
                        proc1.QueueJob( new SpinWaitLockJob( aLock ) );
                        proc2.QueueJob( new SpinWaitLockJob( aLock ) );
                    }
                    proc1.StartThread();
                    proc2.StartThread();
                    proc1.QueueEndOfWork();
                    proc2.QueueEndOfWork();
                    proc1.WaitUntilFinished();
                    proc2.WaitUntilFinished();
                }
            }
        }
    }
}