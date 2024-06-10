using System.Collections.Concurrent;

namespace ProbeSampler.Core.Services.Processing.Core
{
    /// <summary>
    /// Очередь с ограниченым размером.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BoundedQueue<T>
    {
        private readonly ConcurrentQueue<T> queue = new ConcurrentQueue<T>();
        private readonly int maxSize;
        private readonly object syncObject = new object();

        public BoundedQueue(int maxSize)
        {
            this.maxSize = maxSize;
        }

        public void Enqueue(T item)
        {
            queue.Enqueue(item);

            lock (syncObject)
            {
                while (queue.Count > maxSize)
                {
                    queue.TryDequeue(out _);
                }
            }
        }

        public bool TryDequeue(out T result)
        {
            return queue.TryDequeue(out result);
        }

        public int Count => queue.Count;
    }
}
