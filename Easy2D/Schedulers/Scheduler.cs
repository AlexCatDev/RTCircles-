using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Easy2D
{
    public class ScheduledTask : IEquatable<ScheduledTask>
    {
        public Action Action { get; private set; }
        public float Delay { get; private set; }

        public bool HasCompleted { get; internal set; }
        public bool IsCancelled { get; internal set; }

        public ScheduledTask(Action action, float delay = 0)
        {
            Action = action;
            Delay = delay;
            HasCompleted = false;
        }

        public bool Equals(ScheduledTask other)
        {
            //I'm really not sure about doing duplicate detection by this it's really sketchy but it seems to work the way i want it to work somehow
            //C# magic =)
            return Action.GetHashCode() == other.Action.GetHashCode();
        }

        private float timer;
        internal void Update(float delta)
        {
            timer += delta;
            if (timer >= Delay)
            {
                timer = 0;
                Action?.Invoke();
                HasCompleted = true;
            }
        }

        public void Cancel()
        {
            IsCancelled = true;
        }
    }

    /// <summary>
    /// Schedule tasks to run
    /// </summary>
    public class Scheduler
    {
        private readonly List<ScheduledTask> tasks = new List<ScheduledTask>();

        private readonly object mutex = new object();

        private int creationThreadID;
        private bool IsSameThread => System.Threading.Thread.CurrentThread.ManagedThreadId == creationThreadID;

        public bool ExecuteImmediatelyIfSameThread { get; private set; }

        public Scheduler()
        {
            creationThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        /// Schedule a task to be run on the main thread, at the beginning of the next frame or when the time is up
        /// </summary>
        /// <param name="action"></param>
        /// <param name="allowDuplicates"></param>
        /// <param name="delay"></param>
        /// <returns>The task to be run or null if unsuccessfull</returns>
        public ScheduledTask Run(ScheduledTask task, bool allowDuplicates = false)
        {
            if(ExecuteImmediatelyIfSameThread && IsSameThread)
            {
                task.Action.Invoke();
                return task;
            }

            lock (mutex)
            {
                if (allowDuplicates == false)
                {
                    if (tasks.Contains(task))
                    {
                        Utils.Log($"A duplicate of a task already in the Scheduler tried to be added", LogLevel.Error);
                        return null;
                    }
                }

                task.HasCompleted = false;
                task.IsCancelled = false;
                tasks.Add(task);
                return task;
            }
        }

        public ScheduledTask RunAsync(Action asyncTask, ScheduledTask onCompletion)
        {
            Task.Run(() =>
            {
                asyncTask();

                if (onCompletion.IsCancelled == false)
                    Run(onCompletion, true);
            });

            return onCompletion;
        }

        /// <summary>
        /// Called from the graphics thread
        /// </summary>
        /// <param name="delta"></param>
        public void Update(float delta)
        {
            lock (mutex)
            {
                for (int i = tasks.Count - 1; i >= 0; i--)
                {
                    tasks[i].Update(delta);

                    if (tasks[i].HasCompleted || tasks[i].IsCancelled)
                        tasks.RemoveAt(i);
                }
            }
        }
    }
}
