using System;

namespace _4DMonoEngine.Core.Utils
{
    public struct Interval : IComparable<Interval>
    {
        public readonly ushort Min;
        public readonly ushort Max;

        public Interval(ushort point)
        {
            Min = point;
            Max = point;
        }

        public Interval(ushort min, ushort max)
        {
            Min = min;
            Max = max;
        }

        public int CompareTo(Interval other)
        {
            if (other.Min > Max)
            {
                return -1;
            }
            if (other.Max < Min)
            {
                return 1;
            }
            return 0;
        }
    }
}