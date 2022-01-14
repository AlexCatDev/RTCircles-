using System;
using System.Collections.Generic;

namespace RTCircles
{
    public class Option<T>
    {
        private static Dictionary<string, object> names = new Dictionary<string, object>();
        private static List<WeakReference<Option<T>>> allOptions = new List<WeakReference<Option<T>>>();

        public static IReadOnlyList<WeakReference<Option<T>>> AllOptions => allOptions.AsReadOnly();

        public string Name => name;

        private WeakReference<Option<T>> weakRef;

        private string name;

        private T defaultValue;

        private bool initialized = false;

        //value cache
        private T value;
        public T Value
        {
            get
            {
                if (!initialized)
                {
                    value = Settings.GetValue<T>(name, out bool exists, defaultValue);
                    setter?.Invoke(value);
                    initialized = true;
                }

                return value;
            }
            set
            {
                setter?.Invoke(value);

                Settings.SetValue<T>(value, name);
                this.value = value;
                initialized = true;
            }
        }

        private Action<T> setter;

        /// <summary>
        /// Create a proxy around an already existing property
        /// </summary>
        /// <param name="name">The known name of the option</param>
        /// <param name="defaultValue">If the option does not exist, what is the default value?</param>
        public static Option<T> CreateProxy(string name, Action<T> setter, T defaultValue = default(T))
        {
            var proxy = new Option<T>(name, defaultValue, true);
            proxy.setter = setter;

            T cock_and_balls_cum = proxy.Value;

            return proxy;
        }
        /// <summary>
        /// A class for managing options
        /// </summary>
        /// <param name="name">The known name of the option</param>
        /// <param name="defaultValue">If the option does not exist, what is the default value?</param>
        /// <param name="lazyInitialization">If false the value is initialized immediately when the constructor is called, otherwise it's initialized the first call to Value</param>
        public Option(string name, T defaultValue = default(T), bool lazyInitialization = false)
        {
            if(!names.TryAdd(name, null)) 
                throw new ArgumentException($"A option with the name {name} already exists.");

            weakRef = new WeakReference<Option<T>>(this);
            allOptions.Add(weakRef);

            this.name = name;
            this.defaultValue = defaultValue;

            if (!lazyInitialization)
            {
                //just fetch the value immediately
                T cock_and_balls_cum = Value;
            }
        }

        ~Option()
        {
            names.Remove(name);
            allOptions.Remove(weakRef);
        }
    }
}
