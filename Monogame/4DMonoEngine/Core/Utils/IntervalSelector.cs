using System;
using ProtoBuf;

namespace _4DMonoEngine.Core.Utils
{
    [ProtoContract]
    public class IntervalSelector<TData, TIndex> : IIntervalSelector<Interval<TData, TIndex>, TIndex> 
        where TIndex : IComparable<TIndex>
    {
        public TIndex GetStart(Interval<TData, TIndex> item)
        {
            return item.Low;
        }

        public TIndex GetEnd(Interval<TData, TIndex> item)
        {
            return item.Hi;
        }
    }
}