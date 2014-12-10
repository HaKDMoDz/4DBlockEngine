using System.Collections.Concurrent;

namespace _4DMonoEngine.Core.Utils
{
    /// <summary>
    /// A dictionary objects that accepts 3 keys for indexing.
    /// </summary>
    public class SparseArray3D<T> : ConcurrentDictionary<long, T>
    {
        private readonly long m_rowSize;
        private readonly long m_columnSize;
        public SparseArray3D(long rowSize, long columnSize) : base()
        {
            m_rowSize = rowSize;
            m_columnSize = rowSize * columnSize;
        }
        public T this[int x, int y, int z]
        {
            get
            {
                T @out = default(T);
                TryGetValue(x + y * m_rowSize + z * m_columnSize, out @out);
                return @out;
            }
            set 
            {
                this[x + y * m_rowSize + z * m_columnSize] = value; 
            }
        }

        public bool ContainsKey(int x, int y, int z)
        {
            return ContainsKey(x + y * m_rowSize + z * m_columnSize);
        }

        public T Remove(int x, int y, int z)
        {
            T removed;
            TryRemove(x + y * m_rowSize + z * m_columnSize, out removed);
            return removed;
        }
    }
}