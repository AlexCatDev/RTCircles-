using Easy2D;
using System.Collections.Generic;

namespace RTCircles
{
    public static class ObjectPool<T> where T : new()
    {
        private static Stack<T> pool = new Stack<T>();

        public static int TotalCreated { get; private set; }

        public static int UnusedObjectsCount => pool.Count;

        public static T Take()
        {
            if(pool.TryPop(out T result))
                return result;

            T newObj = new T();
            TotalCreated++;

#if DEBUG
            //Utils.Log($"{typeof(T).Name} : {TotalCreated}", LogLevel.Info);
#endif

            return newObj;
        }

        public static void ClearUnusedObjects() => pool.Clear();

        public static void Return(T t) 
        {
            if (t == null)
                return;

            pool.Push(t);
        }
    }
}