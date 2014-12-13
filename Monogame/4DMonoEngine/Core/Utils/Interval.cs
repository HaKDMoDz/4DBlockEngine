using System;
using System.Diagnostics;
using ProtoBuf;

namespace _4DMonoEngine.Core.Utils
{
    [ProtoContract]
    public class Interval<TData, TIndex> where TIndex : IComparable<TIndex>
    {
        private Interval() { }

        public Interval(TIndex low, TIndex hi, TData data = default(TData))
        {
            Debug.Assert(low.CompareTo(hi) <= 0,  string.Format("[Low={0} >  Hi={1}", low, hi));
            Low = low;
            Hi = hi;
            MutableData = data;
        }

        [ProtoMember(1)]
        public TIndex Low { get; private set; }
        [ProtoMember(2)]
        public TIndex Hi { get; private set; }
        [ProtoMember(3)]
        public TData MutableData { get; set; }

        public override string ToString()
        {
            return string.Format("[Low={0}, Hi={1}, Data={2}]", Low, Hi, MutableData);
        }
    }
}