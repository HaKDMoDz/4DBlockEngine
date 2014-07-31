using System;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Utils.Noise
{
    public class SimplexNoise4D : SimplexCommon
    {
	    private static readonly double F4 = (Math.Sqrt(5.0)-1.0)/4.0;
	    private static readonly double G4 = (5.0-Math.Sqrt(5.0))/20.0;	

	    private static readonly int[][] Grad4=
	    {
	        new[] {0,1,1,1}, new[] {0,1,1,-1}, new[] {0,1,-1,1}, new[] {0,1,-1,-1},
	        new[] {0,-1,1,1}, new[] {0,-1,1,-1}, new[] {0,-1,-1,1}, new[] {0,-1,-1,-1},
	        new[] {1,0,1,1}, new[] {1,0,1,-1}, new[] {1,0,-1,1}, new[] {1,0,-1,-1},
	        new[] {-1,0,1,1}, new[] {-1,0,1,-1}, new[] {-1,0,-1,1}, new[] {-1,0,-1,-1},
	        new[] {1,1,0,1}, new[] {1,1,0,-1}, new[] {1,-1,0,1}, new[] {1,-1,0,-1},
	        new[] {-1,1,0,1}, new[] {-1,1,0,-1}, new[] {-1,-1,0,1}, new[] {-1,-1,0,-1},
	        new[] {1,1,1,0}, new[] {1,1,-1,0}, new[] {1,-1,1,0}, new[] {1,-1,-1,0},
	        new[] {-1,1,1,0}, new[] {-1,1,-1,0}, new[] {-1,-1,1,0}, new[] {-1,-1,-1,0}
	    };
        public SimplexNoise4D(ulong seed) :base(seed) {}

	    public float RidgedMultiFractal (float x, float y, float z, float w, float scale, float offset, float gain, int octaves)
	    {
		    float rValue = 0;
		    var position = new Vector4 (x / scale, y / scale, z / scale, w / scale);
		    float divisor = 0;
		    float weight = 1;
		    for (var i = 0; i < octaves; ++i) 
		    {
                var octaveScale = (float)Math.Pow(2, i);
			    divisor += (1.0f / octaveScale);
			    var signal = Perlin(position * octaveScale);
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

	    public float FractalBrownianMotion (float x, float y, float z, float w, float scale, float offset, int octaves)
	    {
		    float rValue = 0;
		    var position = new Vector4 (x / scale, y / scale, z / scale, w / scale);
		    float divisor = 0;
		    for (var i = 0; i < octaves; ++i) 
		    {
                var octaveScale = (float)Math.Pow(2, i);
			    divisor += (1.0f / octaveScale);
			    rValue += (1.0f / octaveScale) * (Perlin (position * octaveScale) + offset);
		    }
		
		    return rValue / divisor;
	    }

        private float Perlin(Vector4 pos)
	    {
		    return Perlin(pos.X, pos.Y, pos.Z, pos.W);
	    }

        private float Perlin(double pX, double pY, double pZ, double pW)
	    {
		    double n0, n1, n2, n3, n4; // Noise contributions from the five corners
		    // Skew the (x,y,z,w) space to determine which cell of 24 simplices we're in
		    var s = (pX + pY + pZ + pW) * F4; // Factor for 4D skewing
            var i = MathUtilities.FastFloor(pX + s);
            var j = MathUtilities.FastFloor(pY + s);
            var k = MathUtilities.FastFloor(pZ + s);
            var l = MathUtilities.FastFloor(pW + s);
		    var t = (i + j + k + l) * G4; // Factor for 4D unskewing
		    var X0 = i - t; // Unskew the cell origin back to (x,y,z,w) space
		    var Y0 = j - t;
		    var Z0 = k - t;
		    var W0 = l - t;
		    var x0 = pX - X0;  // The x,y,z,w distances from the cell origin
		    var y0 = pY - Y0;
		    var z0 = pZ - Z0;
		    var w0 = pW - W0;
		    // For the 4D case, the simplex is a 4D shape I won't even try to describe.
		    // To find out which of the 24 possible simplices we're in, we need to
		    // determine the magnitude ordering of x0, y0, z0 and w0.
		    // Six pair-wise comparisons are performed between each possible pair
		    // of the four coordinates, and the results are used to rank the numbers.
		    var rankx = 0;
		    var ranky = 0;
		    var rankz = 0;
		    var rankw = 0;
		    if(x0 > y0) rankx++; else ranky++;
		    if(x0 > z0) rankx++; else rankz++;
		    if(x0 > w0) rankx++; else rankw++;
		    if(y0 > z0) ranky++; else rankz++;
		    if(y0 > w0) ranky++; else rankw++;
		    if(z0 > w0) rankz++; else rankw++;
		    int i1, j1, k1, l1; // The integer offsets for the second simplex corner
		    int i2, j2, k2, l2; // The integer offsets for the third simplex corner
		    int i3, j3, k3, l3; // The integer offsets for the fourth simplex corner
		    // simplex[c] is a 4-vector with the numbers 0, 1, 2 and 3 in some order.
		    // Many values of c will never occur, since e.g. x>y>z>w makes x<z, y<w and x<w
		    // impossible. Only the 24 indices which have non-zero entries make any sense.
		    // We use a thresholding to set the coordinates in turn from the largest magnitude.
		    // Rank 3 denotes the largest coordinate.
		    i1 = rankx >= 3 ? 1 : 0;
		    j1 = ranky >= 3 ? 1 : 0;
		    k1 = rankz >= 3 ? 1 : 0;
		    l1 = rankw >= 3 ? 1 : 0;
		    // Rank 2 denotes the second largest coordinate.
		    i2 = rankx >= 2 ? 1 : 0;
		    j2 = ranky >= 2 ? 1 : 0;
		    k2 = rankz >= 2 ? 1 : 0;
		    l2 = rankw >= 2 ? 1 : 0;
		    // Rank 1 denotes the second smallest coordinate.
		    i3 = rankx >= 1 ? 1 : 0;
		    j3 = ranky >= 1 ? 1 : 0;
		    k3 = rankz >= 1 ? 1 : 0;
		    l3 = rankw >= 1 ? 1 : 0;
		    // The fifth corner has all coordinate offsets = 1, so no need to compute that.
		    var x1 = x0 - i1 + G4; // Offsets for second corner in (x,y,z,w) coords
		    var y1 = y0 - j1 + G4;
		    var z1 = z0 - k1 + G4;
		    var w1 = w0 - l1 + G4;
		    var x2 = x0 - i2 + 2.0*G4; // Offsets for third corner in (x,y,z,w) coords
		    var y2 = y0 - j2 + 2.0*G4;
		    var z2 = z0 - k2 + 2.0*G4;
		    var w2 = w0 - l2 + 2.0*G4;
		    var x3 = x0 - i3 + 3.0*G4; // Offsets for fourth corner in (x,y,z,w) coords
		    var y3 = y0 - j3 + 3.0*G4;
		    var z3 = z0 - k3 + 3.0*G4;
		    var w3 = w0 - l3 + 3.0*G4;
		    var x4 = x0 - 1.0 + 4.0*G4; // Offsets for last corner in (x,y,z,w) coords
		    var y4 = y0 - 1.0 + 4.0*G4;
		    var z4 = z0 - 1.0 + 4.0*G4;
		    var w4 = w0 - 1.0 + 4.0*G4;
		    // Work out the hashed gradient indices of the five simplex corners
		    var ii = i & 255;
		    var jj = j & 255;
		    var kk = k & 255;
		    var ll = l & 255;
		    var gi0 = Perm[ii+Perm[jj+Perm[kk+Perm[ll]]]] % 32;
		    var gi1 = Perm[ii+i1+Perm[jj+j1+Perm[kk+k1+Perm[ll+l1]]]] % 32;
		    var gi2 = Perm[ii+i2+Perm[jj+j2+Perm[kk+k2+Perm[ll+l2]]]] % 32;
		    var gi3 = Perm[ii+i3+Perm[jj+j3+Perm[kk+k3+Perm[ll+l3]]]] % 32;
		    var gi4 = Perm[ii+1+Perm[jj+1+Perm[kk+1+Perm[ll+1]]]] % 32;
		    // Calculate the contribution from the five corners
		    var t0 = 0.6 - x0*x0 - y0*y0 - z0*z0 - w0*w0;
		    if(t0<0) n0 = 0.0;
		    else {
			    t0 *= t0;
			    n0 = t0 * t0 * Dot(Grad4[gi0], x0, y0, z0, w0);
		    }
		    var t1 = 0.6 - x1*x1 - y1*y1 - z1*z1 - w1*w1;
		    if(t1<0) n1 = 0.0;
		    else {
			    t1 *= t1;
			    n1 = t1 * t1 * Dot(Grad4[gi1], x1, y1, z1, w1);
		    }
		    var t2 = 0.6 - x2*x2 - y2*y2 - z2*z2 - w2*w2;
		    if(t2<0) n2 = 0.0;
		    else {
			    t2 *= t2;
			    n2 = t2 * t2 * Dot(Grad4[gi2], x2, y2, z2, w2);
		    }
		    var t3 = 0.6 - x3*x3 - y3*y3 - z3*z3 - w3*w3;
		    if(t3<0) n3 = 0.0;
		    else {
			    t3 *= t3;
			    n3 = t3 * t3 * Dot(Grad4[gi3], x3, y3, z3, w3);
		    }
		    var t4 = 0.6 - x4*x4 - y4*y4 - z4*z4 - w4*w4;
		    if(t4<0) n4 = 0.0;
		    else {
			    t4 *= t4;
			    n4 = t4 * t4 * Dot(Grad4[gi4], x4, y4, z4, w4);
		    }
		    // Sum up and scale the result to cover the range [-1,1]
		    return (float)(27.0 * (n0 + n1 + n2 + n3 + n4));
        }
        
        private static double Dot(int[] g, double x, double y, double z, double w)
        {
            return g[0] * x + g[1] * y + g[2] * z + g[3] * w;
        }
    }
}