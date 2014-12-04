using System;

namespace _4DMonoEngine.Core.Utils
{
    [Serializable]
    public class IntervalSelector<T> : IIntervalSelector<Interval<T>, int>
    {
        public int GetStart(Interval<T> item)
        {
            return item.Low;
        }

        public int GetEnd(Interval<T> item)
        {
            return item.Hi;
        }
    }
}