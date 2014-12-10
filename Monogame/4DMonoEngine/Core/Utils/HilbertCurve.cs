using System;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Utils
{
    public struct HilbertIndex
    {
        public readonly uint[] Transpose;
        public readonly int Bits;
        public readonly int Dimensions;
        public HilbertIndex(uint[] transpose, int bits)
        {
            Transpose = transpose;
            Bits = bits;
            Dimensions = transpose.Length;
        }
        public HilbertIndex(uint index, int dimensions, int bits) : this(index, new uint[dimensions], bits)
        { }

        public HilbertIndex(uint index, uint[] transpose, int bits)
        {
            Bits = bits;
            Dimensions = transpose.Length;
            Transpose = transpose;
            var len = Bits*Dimensions-1;
            var mask = ((2U << (bits - 1)) - 1);
            for(var i = 0; i < Dimensions; ++i)
            {
                uint row = 0;
                for (var j = 0; j < Bits; ++j)
                {
                    var source = j * Dimensions + (Dimensions - (i + 1));
                    row |= ((index >> source) & 1) << j;
                }
                Transpose[i] = row & mask;
            }
        }
    } 

    /// <summary>
    /// Convert between Hilbert index and N-dimensional points.
    /// 
    /// The Hilbert index is expressed as an array of transposed bits. 
    /// 
    /// Example: 5 bits for each of n=3 coordinates.
    /// 15-bit Hilbert integer = A B C D E F G H I J K L M N O is stored
    /// as its Transpose                        ^
    /// X[0] = A D G J M                    X[2]|  7
    /// X[1] = B E H K N        <------->       | /X[1]
    /// X[2] = C F I L O                   axes |/
    ///        high low                         0------> X[0]
    ///        
    /// NOTE: This algorithm is derived from work done by John Skilling and published in "Programming the Hilbert curve".
    /// (c) 2004 American Institute of Physics.
    /// 
    /// </summary>
    public static class HilbertCurve
    {
		public static uint[] HilbertAxes(uint index, int dimensions = 3, int bits = 5)
		{
			return HilbertAxes(new HilbertIndex(index, dimensions, bits));
		}

        public static uint[] HilbertAxes(HilbertIndex index)
        {
            var X = (uint[])index.Transpose.Clone();
			var bits = index.Bits;
            var n = index.Dimensions; // n: Number of dimensions
            uint N = 2U << (bits - 1), P, Q, t;
            int i;
            // Gray decode by H ^ (H/2)
            t = X[n - 1] >> 1;
            // Corrected error in Skilling's paper on the following line. The appendix had i >= 0 leading to negative array index.
            for (i = n - 1; i > 0; i--) 
            {
                X[i] ^= X[i - 1];
            }
            X[0] ^= t;
            // Undo excess work
            for (Q = 2; Q != N; Q <<= 1)
            {
                P = Q - 1;
                for (i = n - 1; i >= 0; i--)
                    if ((X[i] & Q) != 0U)
                        X[0] ^= P; // invert
                    else
                    {
                        t = (X[0] ^ X[i]) & P;
                        X[0] ^= t;
                        X[i] ^= t;
                    }
            } // exchange
            return X;
        }

        public static HilbertIndex HilbertIndexTransposed(uint[] hilbertAxes, int bits = 5)
        {
            var X = (uint[])hilbertAxes.Clone();
            var n = hilbertAxes.Length; // n: Number of dimensions
            uint M = 1U << (bits - 1), P, Q, t;
            int i;
            // Inverse undo
            for (Q = M; Q > 1; Q >>= 1)
            {
                P = Q - 1;
                for (i = 0; i < n; i++)
                    if ((X[i] & Q) != 0)
                        X[0] ^= P; // invert
                    else
                    {
                        t = (X[0] ^ X[i]) & P;
                        X[0] ^= t;
                        X[i] ^= t;
                    }
            } // exchange
            // Gray encode
            for (i = 1; i < n; i++)
                X[i] ^= X[i - 1];
            t = 0;
            for (Q = M; Q > 1; Q >>= 1)
                if ((X[n - 1] & Q)!=0)
                    t ^= Q - 1;
            for (i = 0; i < n; i++)
                X[i] ^= t;

            return new HilbertIndex(X, bits);
        }
    }
}