using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BlogGhost
{
    public class ProducerConsumerQueue<T>
    {
        private Queue<T> mQueue;
        private int MaxCount =  10;

        public ProducerConsumerQueue()
        {
            mQueue = new Queue<T>();
        }

        public ProducerConsumerQueue(int maxCount)
        {
            mQueue = new Queue<T>();
            MaxCount = maxCount;
        }

        public T Buffer
        {

            get {
                T item;
                lock (mQueue)
                {
                    int currentCount = mQueue.Count;
                    while (currentCount == 0)
                    {
                        Monitor.Wait(mQueue);
                    }

                    item = mQueue.Dequeue();
                    Monitor.PulseAll(mQueue);
                }
                return item;
            }
            set {
                lock (mQueue) {

                    int currentCount = mQueue.Count;
                    while (currentCount == this.MaxCount)
                    {
                        Monitor.Wait(mQueue);
                    }
                    mQueue.Enqueue(value);
                    Monitor.PulseAll(mQueue);
                }
            }
        }
    }
}
