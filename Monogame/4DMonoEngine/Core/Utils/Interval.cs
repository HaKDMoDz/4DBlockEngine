using System;
using System.Diagnostics;

namespace _4DMonoEngine.Core.Utils
{
    [Serializable]
    public class Interval<TData, TIndex> where TIndex : IComparable<TIndex>
    {
        private Interval() { }

        public Interval(TIndex low, TIndex hi, TData data = default(TData))
        {
            Debug.Assert(low.CompareTo(hi) > 0, "lo higher than hi");
            Low = low;
            Hi = hi;
            MutableData = data;
        }

        public TIndex Low { get; private set; }
        public TIndex Hi { get; private set; }
        public TData MutableData { get; set; }

        public override string ToString()
        {
            return string.Format("[Low={0}, Hi={1}, Data={2}]", Low, Hi, MutableData);
        }
    }
}