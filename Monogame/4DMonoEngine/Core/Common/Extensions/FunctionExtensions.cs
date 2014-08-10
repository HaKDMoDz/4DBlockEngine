using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _4DMonoEngine.Core.Common.Extensions
{
    public static class FunctionExtensions
    {
        public static Func<TA, Func<TB, TR>> Curry<TA, TB, TR>(this Func<TA, TB, TR> f)
        {
            return a => b => f(a, b);
        }

        public static Func<TA, Func<TB, TC, TR>> Curry<TA, TB, TC, TR>(this Func<TA, TB, TC, TR> f)
        {
            return a => (b, c) => f(a, b, c);
        }

        public static Func<TA, Func<TB, TC, TD, TR>> Curry<TA, TB, TC, TD, TR>(this Func<TA, TB, TC, TD, TR> f)
        {
            return a => (b, c, d) => f(a, b, c, d);
        }

        public static Func<TB, TR> Partial<TA, TB, TR>(this Func<TA, TB, TR> f, TA a)
        {
            var curried = f.Curry();
            return curried(a);
        }

        public static Func<TB, TC, TR> Partial<TA, TB, TC, TR>(this Func<TA, TB, TC, TR> f, TA a)
        {
            var curried = f.Curry();
            return curried(a);
        }

        public static Func<TB, TC, TD, TR> Partial<TA, TB, TC, TD, TR>(this Func<TA, TB, TC, TD, TR> f, TA a)
        {
            var curried = f.Curry();
            return curried(a);
        }

        public static Comparison<TA> ToCompareFunc<TA>(this Func<TA, TA, int> f)
        {
            return (a, b) => f(a, b);
        }

        public static Comparison<TB> MakePivotCompare<TA, TB>(this Func<TA, TB, TB, int> f, TA pivot)
        {
            return f.Partial(pivot).ToCompareFunc();
        }


    }
}
