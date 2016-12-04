using System;
using System.Collections;
using System.Threading;
using System.Security.Permissions;
using System.Collections.Generic;

namespace CS422
{
	public class SimpleLockThreadPool
	{	
		struct WorkItem
		{
			internal WaitCallback work;
			internal object obj;
			internal ExecutionContext executionContext;

			internal WorkItem(WaitCallback work, object obj)
			{
				this.work = work;
				this.obj = obj;
				this.executionContext = null;
			}

			internal void Invoke()
			{
				// Run normally (delegate invoke) or under context, as appropriate.
				if (executionContext == null)
					work(obj);
				else
					ExecutionContext.Run(executionContext, ContextInvoke, null);
			}

			private void ContextInvoke(object obj)
			{
				work(this.obj);
			}
		}

		// Variables

		private int concurrencyLevel = 0;
		private bool flowExecutionContext;
		private Queue<WorkItem> workItemQueue;
		private Thread[] threads;
		private int threadsWaiting;
		private bool shutdown;

		// Constructors
		public SimpleLockThreadPool() :
						this(Environment.ProcessorCount, true) 
		{
		
		}

		public SimpleLockThreadPool(int concurrencyLevel) :
						this(concurrencyLevel, true) 
		{
		
		}

		public SimpleLockThreadPool(bool flowExecutionContext) :
				this(Environment.ProcessorCount, flowExecutionContext) 
		{ }

		public SimpleLockThreadPool(int concurrencyLevel, bool flowExecutionContext)
		{
			if (concurrencyLevel <= 0)
				throw new ArgumentOutOfRangeException("concurrencyLevel <= 0");

			this.concurrencyLevel = concurrencyLevel;
			this.flowExecutionContext = flowExecutionContext;
			workItemQueue = new Queue<WorkItem> ();
			// If suppressing flow, we need to demand permissions.
			if (!flowExecutionContext)
				new SecurityPermission(SecurityPermissionFlag.Infrastructure).Demand();
		}

		// Methods
		public void QueueUserWorkItem(WaitCallback work)
		{
			QueueUserWorkItem(work, null);
		}

		public void QueueUserWorkItem(WaitCallback work, object obj)
		{
			WorkItem wi = new WorkItem(work, obj);

			// If execution context flowing is on, capture the caller's context.
			if (flowExecutionContext)
				wi.executionContext = ExecutionContext.Capture();

			// Make sure the pool is started (threads created, etc).
			EnsureStarted();

			// Now insert the work item into the queue, possibly waking a thread.
			lock (workItemQueue) {
				workItemQueue.Enqueue(wi);
				if (threadsWaiting > 0)
					Monitor.Pulse(workItemQueue);
			}
		}
		// Ensures that threads have begun executing.
		private void EnsureStarted()
		{
			if (threads == null) {
				lock (workItemQueue) {
					if (threads == null) {
						threads = new Thread[concurrencyLevel];
						for (int i = 0; i < threads.Length; i++) {
							threads[i] = new Thread(DispatchLoop);
							threads[i].Start();
						}
					}
				}
			}
		}

		// Each thread runs the dispatch loop.
		private void DispatchLoop()
		{
			while (true) {
				WorkItem wi = default(WorkItem);
				lock (workItemQueue) {
					// If shutdown was requested, exit the thread.
					if (shutdown)
						return;

					// Find a new work item to execute.
					while (workItemQueue.Count == 0) {
						threadsWaiting++;
						try { Monitor.Wait(workItemQueue); }
						finally { threadsWaiting--; }

						// If we were signaled due to shutdown, exit the thread.
						if (shutdown)
							return;
					}

					// We found a work item! Grab it ...
					wi = workItemQueue.Dequeue();
				}

				// ...and Invoke it. Note: exceptions will go unhandled (and crash).
				wi.Invoke();
			}
		}

		// Disposing will signal shutdown, and then wait for all threads to finish.
		public void Dispose()
		{
			shutdown = true;
			lock (workItemQueue) {
				Monitor.PulseAll(workItemQueue);
			}

			for (int i = 0; i < threads.Length; i++)
				threads[i].Join();
		}

	}
}

