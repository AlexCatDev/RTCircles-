using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Easy2D
{
    /// <summary>
    /// Schedule tasks to be run, schedule them on any thread, schedule them under water
    /// Then just call RunPendingTasks(); on the thread of your choosing or whatever ezpz
    /// </summary>
    public class Scheduler
    {
        private static volatile int totalPendingWorkloads;
        private static volatile int totalAsyncWorkloads;

        public static int TotalPendingWorkloads => totalPendingWorkloads;
        public static int TotalAsyncWorkloads => totalAsyncWorkloads;

        public string Name { get; set; } = Guid.NewGuid().ToString();

        public int PendingTaskCount => tasks.Count;

        /// <summary>
        /// Is the calling thread the owner of this scheduler?
        /// </summary>
        public bool IsMainThread => mainThreadID == Thread.CurrentThread.ManagedThreadId;

        /// <summary>
        /// Default: True
        /// </summary>
        public bool AllowCrossThreadRun { get; set; } = true;

        private Queue<Action> tasks = new Queue<Action>();

        private int mainThreadID;

        private readonly object mutex = new object();

        public Scheduler() => TransferThreadOwnership();

        public void TransferThreadOwnership() => mainThreadID = Thread.CurrentThread.ManagedThreadId;

        /// <summary>
        /// Execute scheduled tasks, if AllowCrossThreadRun is false this will throw an exception if the calling thread does not own this scheduler.
        /// </summary>
        /// <param name="maxCount">The max amount of tasks that can run now, 0 and below are treated as ALL</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void RunPendingTasks(int maxCount = 0)
        {
            if (!AllowCrossThreadRun && !IsMainThread)
                throw new InvalidOperationException("This method can only be run in the thread it was created on");

            lock (mutex)
            {
                int count = tasks.Count;

                if (count == 0)
                    return;

                if(maxCount > 0)
                    count = Math.Min(maxCount, count);

                for (int i = 0; i < count; i++)
                    tasks.Dequeue().Invoke();

                totalPendingWorkloads -= count;
            }
        }

        public void Enqueue(Action task, bool tryExecuteNow = false)
        {
            if(tryExecuteNow && IsMainThread)
            {
                task();
                return;
            }

            lock (mutex)
            {
                tasks.Enqueue(task);
                totalPendingWorkloads++;
            }
        }

        public void EnqueueDelayed(Action task, int delay)
        {
            EnqueueAsync(() => { return (true, 0); }, (i) => { task(); }, delay);
        }

        public delegate (bool ShouldContinue, T Result) AsyncAction<T>();

        public void EnqueueAsync<T>(AsyncAction<T> asyncAction, Action<T> onCompletion, int delay = 0)
        {
            ThreadPool.QueueUserWorkItem((obj) =>
            {
                totalAsyncWorkloads++;

                try
                {
                    if (delay > 0)
                        Thread.Sleep(delay);

                    var result = asyncAction();

                    if (result.ShouldContinue)
                        Enqueue(() => { onCompletion(result.Result); });

                    totalAsyncWorkloads--;
                }
                catch (Exception ex)
                {
                    totalAsyncWorkloads--;
                    OnAsyncException?.Invoke(ex);
                }
            });
        }

        public event Action<Exception> OnAsyncException;
        
        public override string ToString()
        {
            return $"[{Name}] Scheduled tasks: {PendingTaskCount}";
        }
    }
}
