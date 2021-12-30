using System;
using System.Collections.Generic;
using System.Threading;

namespace Easy2D
{
    public class Scheduler
    {
        private static List<WeakReference<Scheduler>> allSchedulers = new List<WeakReference<Scheduler>>();
        public static IReadOnlyList<WeakReference<Scheduler>> AllSchedulers => allSchedulers.AsReadOnly();

        private List<Action> tasks = new List<Action>();

        public int PendingTaskCount => tasks.Count;
        public int AsyncWorkloadsRunning => asyncWorkloadsRunning;

        private readonly object mutex = new object();

        private readonly int mainThreadID;

        private volatile int asyncWorkloadsRunning;

        private bool IsMainThread => mainThreadID == Thread.CurrentThread.ManagedThreadId;

        private readonly WeakReference<Scheduler> weakReference;
        public Scheduler()
        {
            mainThreadID = Thread.CurrentThread.ManagedThreadId;

            weakReference = new WeakReference<Scheduler>(this);
            allSchedulers.Add(weakReference);
        }

        ~Scheduler()
        {
            allSchedulers.Remove(weakReference);
        }

        /// <summary>
        /// Schedule an action to be run
        /// </summary>
        /// <param name="task">The action to schedule</param>
        /// <returns><paramref name="task"/></returns>
        public Action Add(Action task)
        {
            lock (mutex)
            {
                tasks.Add(task);
            }
            return task;
        }

        /// <summary>
        /// Schedule an action to be run but only if it has not been already scheduled in this cycle
        /// </summary>
        /// <param name="task">The action to schedule</param>
        /// <returns><paramref name="task"/></returns>
        public Action AddOnce(Action task)
        {
            lock (mutex)
            {
                if (!tasks.Contains(task))
                    tasks.Add(task);
            }
            return task;
        }

        /// <summary>
        /// Complete work on another thread and schedule the result
        /// </summary>
        /// <param name="asyncTask">The action to be invoked on another thread</param>
        /// <param name="onCompletion">The action that gets scheduled</param>
        public void AddAsync(Action<CancellationTokenSource> asyncTask, Action onCompletion)
        {
            ThreadPool.QueueUserWorkItem((obj) => {
                CancellationTokenSource cancellationToken = new CancellationTokenSource();

                ++asyncWorkloadsRunning;
                asyncTask.Invoke(cancellationToken);
                --asyncWorkloadsRunning;

                if (!cancellationToken.IsCancellationRequested)
                    Add(onCompletion);
            });
        }

        /// <summary>
        /// Complete work on another thread and schedule the result
        /// </summary>
        /// <typeparam name="T">The type to send to the scheduled action</typeparam>
        /// <param name="asyncTask">The action to be invoked on another thread</param>
        /// <param name="onCompletion">The action that gets scheduled</param>
        public void AddAsync<T>(Func<CancellationTokenSource, T> asyncAction, Action<T> onCompletion)
        {
            ThreadPool.QueueUserWorkItem((obj) => {
                CancellationTokenSource cancellationToken = new CancellationTokenSource();

                ++asyncWorkloadsRunning;
                var userObject = asyncAction.Invoke(cancellationToken);
                --asyncWorkloadsRunning;

                if (!cancellationToken.IsCancellationRequested)
                {
                    Add(() => {
                        onCompletion(userObject);
                    });
                }
            });
        }

        /// <summary>
        /// Schedule an action to be run or execute immediately if it's on the same thread
        /// </summary>
        /// <param name="task">The action to schedule</param>
        /// <returns><paramref name="task"/></returns>
        public Action AddOrExecuteImmediately(Action task)
        {
            if (IsMainThread)
                task.Invoke();
            else
                Add(task);

            return task;
        }

        /// <summary>
        /// Remove a task that has been schedued
        /// </summary>
        /// <param name="task">The action to remove</param>
        /// <returns><paramref name="task"/></returns>
        public bool Remove(Action task) => tasks.Remove(task);

        /// <summary>
        /// Execute all scheduled tasks, must only be called from the thread in which this scheduler object was created
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void RunPendingTasks()
        {
            //if (!IsMainThread)
            //    throw new InvalidOperationException("This method can only be run in the thread it was created on");

            lock (mutex)
            {
                int count = tasks.Count;
                for (int i = 0; i < count; i++)
                {
                    tasks[i].Invoke();
                }

                tasks.RemoveRange(0, count);
            }
        }

        public override string ToString()
        {
            return $"Scheduled tasks: {tasks.Count} Async workloads: {asyncWorkloadsRunning}";
        }
    }
}
