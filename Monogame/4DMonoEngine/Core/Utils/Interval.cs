using System;
using System.Diagnostics;

namespace _4DMonoEngine.Core.Utils
{
    [Serializable]
    public class Interval<T>
    {
        private Interval() { }

        public Interval(int low, int hi, T data = default(T))
        {
            Debug.Assert(low <= hi, "lo higher than hi");
            Low = low;
            Hi = hi;
            MutableData = data;
        }

        public int Low { get; private set; }
        public int Hi { get; private set; }
        public T MutableData { get; set; }

        public override string ToString()
        {
            return string.Format("[Low={0}, Hi={1}, Data={2}]", Low, Hi, MutableData);
        }
    }
}