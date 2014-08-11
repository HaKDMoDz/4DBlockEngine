using System;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Utils.Noise
{
    public class SimplexNoise2D : SimplexCommon
    {
        private static readonly int[][] Grad2 =
        {
            new[] {1,1}, new[] {-1,1}, new[] {1,-1}, new[] {-1,-1},
            new[] {1,0}, new[] {-1,0}, new[] {1,0}, new[] {-1,0},
            new[] {0,1}, new[] {0,-1}, new[] {0,1}, new[] {0,-1}
        };
      private static double F2 = 0.5*(Math.Sqrt(3.0)-1.0);
      private static double G2 = (3.0-Math.Sqrt(3.0))/6.0;

        public SimplexNoise2D(ulong seed) : base(seed) { }

	
	    public float RidgedMultiFractal (float x, float y, float scale, float offset, float gain, int octaves)
	    {
		    float rValue = 0;
		    var position = new Vector2 (x / scale, y / scale);
		    float divisor = 0;
		    float weight = 1;
		    for (var i = 0; i < octaves; ++i) 
		    {
			    var octaveScale = (float)Math.Pow(2, i);
			    divisor += (1.0f / octaveScale);
			    var signal = Perlin (position * octaveScale);
			    signal = offset - (signal < 0 ? -signal : signal);
			    signal *= signal;
			    signal *= weight;
			    weight = signal * gain;
			    if (weight > 1.0f) { weight = 1.0f; }
			    if (weight < 0.0f) { weight = 0.0f; }
			    rValue += (signal * (1.0f / octaveScale));
		    }
		
		    return rValue / divisor;
	    }
	
	    public float FractalBrownianMotion (float x, float y, float scale, float offset, int octaves)
	    {
		    float rValue = 0;
		    var position = new Vector2 (x / scale, y / scale);
		    float divisor = 0;
		    for (var i = 0; i < octaves; ++i) 
		    {
                var octaveScale = (float)Math.Pow(2, i);
			    divisor += (1.0f / octaveScale);
			    rValue += (1.0f / octaveScale) * (Perlin (position * octaveScale) + offset);
		    }
		    return rValue  / divisor;
	    }

        public float Perlin(Vector2 pos)
	    {
		    return Perlin(pos.X, pos.Y);
	    }

        public float Perlin(double pX, double pY)
        {
            double n0, n1, n2; // Noise contributions from the three corners
            // Skew the input space to determine which simplex cell we're in
            var s = (pX+pY)*F2; // Hairy factor for 2D
            var i = MathUtilities.FastFloor(pX+s);
            var j = MathUtilities.FastFloor(pY + s);
            var t = (i+j)*G2;
            var X0 = i-t; // Unskew the cell origin back to (x,y) space
            var Y0 = j-t;
            var x0 = pX-X0; // The x,y distances from the cell origin
            var y0 = pY-Y0;
            // For the 2D case, the simplex shape is an equilateral triangle.
            // Determine which simplex we are in.
            int i1, j1; // Offsets for second (middle) corner of simplex in (i,j) coords
            if(x0>y0) {i1=1; j1=0;} // lower triangle, XY order: (0,0)->(1,0)->(1,1)
            else {i1=0; j1=1;}      // upper triangle, YX order: (0,0)->(0,1)->(1,1)
            // A step of (1,0) in (i,j) means a step of (1-c,-c) in (x,y), and
            // a step of (0,1) in (i,j) means a step of (-c,1-c) in (x,y), where
            // c = (3-sqrt(3))/6
            var x1 = x0 - i1 + G2; // Offsets for middle corner in (x,y) unskewed coords
            var y1 = y0 - j1 + G2;
            var x2 = x0 - 1.0 + 2.0 * G2; // Offsets for last corner in (x,y) unskewed coords
            var y2 = y0 - 1.0 + 2.0 * G2;
            // Work out the hashed gradient indices of the three simplex corners
            var ii = i & 255;
            var jj = j & 255;
            var gi0 = Perm[ii + Perm[jj]] % 12;
            var gi1 = Perm[ii + i1 + Perm[jj + j1]] % 12;
            var gi2 = Perm[ii + 1 + Perm[jj + 1]] % 12;
            // Calculate the contribution from the three corners
            var t0 = 0.5 - x0*x0-y0*y0;
            if (t0 < 0)
            {
                n0 = 0.0;
            }
            else 
            {
                t0 *= t0;
                n0 = t0 * t0 * Dot(Grad2[gi0], x0, y0);  // (x,y) of grad3 used for 2D gradient
            }
            var t1 = 0.5 - x1*x1-y1*y1;
            if (t1 < 0)
            {
                n1 = 0.0;
            }
            else {
                t1 *= t1;
                n1 = t1 * t1 * Dot(Grad2[gi1], x1, y1);
            }
            var t2 = 0.5 - x2*x2-y2*y2;
            if (t2 < 0)
            {
                n2 = 0.0;
            }
            else 
            {
                t2 *= t2;
                n2 = t2 * t2 * Dot(Grad2[gi2], x2, y2);
            }
            // Add contributions from each corner to get the final noise value.
            // The result is scaled to return values in the interval [-1,1].
            return (float)(70.0 * (n0 + n1 + n2));
        }


        private static double Dot(int[] g, double x, double y)
        {
            return g[0] * x + g[1] * y;
        }
    }
}