using System;
using System.Collections.Generic;
using System.Text;

namespace Easy2D
{
    public class CircularQueue<T> where T : class
    {
        private List<T> items = new();

        private int itemIndex = 0;

        public int CurrentIndex => itemIndex;
        public int ItemCount => items.Count;

        public void Add(T item)
        {
            if(item == null)
                throw new ArgumentNullException("item");

            items.Add(item);

            if (CurrentItem == null)
                CurrentItem = item;
        }

        public void AdvanceItem()
        {
            ++itemIndex;

            if (itemIndex >= items.Count)
                itemIndex = 0;

            CurrentItem = items[itemIndex];
        }

        public T CurrentItem { get; private set; } = null;
    }
}
