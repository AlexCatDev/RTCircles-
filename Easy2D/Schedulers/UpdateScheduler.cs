using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Easy2D
{
    public class ScheduledUpdateTask : IEquatable<ScheduledUpdateTask>
    {
        public Action Action { get; private set; }
        public float Delay { get; private set; }

        public bool HasCompleted { get; private set; }

        public bool isCancelled { get; private set; }

        public ScheduledUpdateTask(Action action, float delay = 0)
        {
            Action = action;
            Delay = delay;
            HasCompleted = false;
        }

        public bool Equals(ScheduledUpdateTask other)
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
            UpdateScheduler.Cancel(this);
            isCancelled = true;
        }
    }

    /// <summary>
    /// Schedule actions to be run on the update thread
    /// </summary>
    public static class UpdateScheduler
    {
        private static readonly List<ScheduledUpdateTask> tasks = new List<ScheduledUpdateTask>();

        private static readonly object mutex = new object();

        /// <summary>
        /// Schedule a task to be run on the main thread, at the beginning of the next frame or when the time is up
        /// </summary>
        /// <param name="action"></param>
        /// <param name="allowDuplicates"></param>
        /// <param name="delay"></param>
        /// <returns>The task to be run or null if unsuccessfull</returns>
        public static ScheduledUpdateTask Run(ScheduledUpdateTask task, bool allowDuplicates = false)
        {
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

                tasks.Add(task);
                return task;
            }
        }

        public static ScheduledUpdateTask RunAsync(Action asyncTask, ScheduledUpdateTask onCompletion)
        {
            Task.Run(() => {
                asyncTask();
                if(onCompletion.isCancelled == false)
                    Run(onCompletion, true);
            });

            return onCompletion;
        }

        public static void Cancel(ScheduledUpdateTask task)
        {
            lock (mutex)
            {
                tasks.Remove(task);
            }
        }

        /// <summary>
        /// Called from the update thread
        /// </summary>
        /// <param name="delta"></param>
        public static void Update(float delta)
        {
            lock (mutex)
            {
                for (int i = tasks.Count - 1; i >= 0; i--)
                {
                    tasks[i].Update(delta);

                    if (tasks[i].HasCompleted)
                        tasks.RemoveAt(i);
                }
            }
        }
    }
}
