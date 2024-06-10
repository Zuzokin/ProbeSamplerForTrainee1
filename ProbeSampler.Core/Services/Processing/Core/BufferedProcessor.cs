namespace ProbeSampler.Core.Services.Processing.Core
{
    public class BufferedProcessor<T>
    {
        private readonly List<T> buffer = new List<T>();
        private readonly int bufferSize;
        private readonly Action<IEnumerable<T>> onBufferFull;

        public BufferedProcessor(int bufferSize, Action<IEnumerable<T>> onBufferFull)
        {
            this.bufferSize = bufferSize;
            this.onBufferFull = onBufferFull;
        }

        public void Add(T item)
        {
            buffer.Add(item);
            if (buffer.Count >= bufferSize)
            {
                onBufferFull(buffer);
                buffer.Clear();
            }
        }

        public void Flush()
        {
            if (buffer.Count > 0)
            {
                onBufferFull(buffer);
                buffer.Clear();
            }
        }
    }
}
