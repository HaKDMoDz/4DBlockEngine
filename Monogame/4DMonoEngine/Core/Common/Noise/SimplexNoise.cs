﻿using System;
using Microsoft.Xna.Framework;
namespace _4DMonoEngine.Core.Common.Noise
{
    public class SimplexNoise
    {
	    private static readonly double F4 = (Math.Sqrt(5.0)-1.0)/4.0;
	    private static readonly double G4 = (5.0-Math.Sqrt(5.0))/20.0;	

        private static readonly int[][] grad3 = new int[][] {
		    new[] {1,1,0}, new[] {-1,1,0}, new[] {1,-1,0}, new[] {-1,-1,0},
		    new[] {1,0,1}, new[] {-1,0,1}, new[] {1,0,-1}, new[] {-1,0,-1},
		    new[] {0,1,1}, new[] {0,-1,1}, new[] {0,1,-1}, new[] {0,-1,-1}
	    };
	
	    private static readonly int[][] grad4= new int[][]{
		    new[] {0,1,1,1}, new[] {0,1,1,-1}, new[] {0,1,-1,1}, new[] {0,1,-1,-1},
		    new[] {0,-1,1,1}, new[] {0,-1,1,-1}, new[] {0,-1,-1,1}, new[] {0,-1,-1,-1},
		    new[] {1,0,1,1}, new[] {1,0,1,-1}, new[] {1,0,-1,1}, new[] {1,0,-1,-1},
		    new[] {-1,0,1,1}, new[] {-1,0,1,-1}, new[] {-1,0,-1,1}, new[] {-1,0,-1,-1},
		    new[] {1,1,0,1}, new[] {1,1,0,-1}, new[] {1,-1,0,1}, new[] {1,-1,0,-1},
		    new[] {-1,1,0,1}, new[] {-1,1,0,-1}, new[] {-1,-1,0,1}, new[] {-1,-1,0,-1},
		    new[] {1,1,1,0}, new[] {1,1,-1,0}, new[] {1,-1,1,0}, new[] {1,-1,-1,0},
		    new[] {-1,1,1,0}, new[] {-1,1,-1,0}, new[] {-1,-1,1,0}, new[] {-1,-1,-1,0}
	    };
	
	private static int[] p = {151,160,137,91,90,15,
		131,13,201,95,96,53,194,233,7,225,140,36,103,30,69,142,8,99,37,240,21,10,23,
		190, 6,148,247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,57,177,33,
		88,237,149,56,87,174,20,125,136,171,168, 68,175,74,165,71,134,139,48,27,166,
		77,146,158,231,83,111,229,122,60,211,133,230,220,105,92,41,55,46,245,40,244,
		102,143,54, 65,25,63,161, 1,216,80,73,209,76,132,187,208, 89,18,169,200,196,
		135,130,116,188,159,86,164,100,109,198,173,186, 3,64,52,217,226,250,124,123,
		5,202,38,147,118,126,255,82,85,212,207,206,59,227,47,16,58,17,182,189,28,42,
		223,183,170,213,119,248,152, 2,44,154,163, 70,221,153,101,155,167, 43,172,9,
		129,22,39,253, 19,98,108,110,79,113,224,232,178,185, 112,104,218,246,97,228,
		251,34,242,193,238,210,144,12,191,179,162,241, 81,51,145,235,249,14,239,107,
		49,192,214, 31,181,199,106,157,184, 84,204,176,115,121,50,45,127, 4,150,254,
		138,236,205,93,222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180};
        
	    private ulong m_seed;
	    private ulong m_lcgCurrSeed;
	    private readonly int[] m_perm;
        public SimplexNoise(ulong seed)
        {
		    m_seed = seed;
		    m_lcgCurrSeed = 6364136223846793005 * seed + 1442695040888963407;
		    var permTemp = new int[256];
            for (uint i = 0; i < 256; i++)
		    {
			    permTemp[i] = p[i];
		    }
		    for(var i = 255; i > 0; --i)
		    {
			    var j = RandInt(i);
			    var permTempJ = permTemp[j];
			    permTemp[j] = permTemp[i];
			    permTemp[i] = permTempJ;
		    }
		    m_perm = new int[512];
		    for (uint i = 0; i < 512; i++)
		    {
			    m_perm[i] = permTemp[i & 255];
		    }

        }

	    private int RandInt(int max)
	    {
		    return (int)(max * Rand());
	    }

        private float Rand()
	    {
		    m_lcgCurrSeed = 6364136223846793005 * m_lcgCurrSeed + 1442695040888963407;
		    return (float)(m_lcgCurrSeed / (double)ulong.MaxValue);
	    }

	    public float RidgedMultiFractal4D (float x, float y, float z, float w, float scale, float offset, float gain, int octaves)
	    {
		    float rValue = 0;
		    var position = new Vector4 (x / scale, y / scale, z / scale, w / scale);
		    float divisor = 0;
		    float weight = 1;
		    for (var i = 0; i < octaves; ++i) 
		    {
                var octaveScale = (float)Math.Pow(2, i);
			    divisor += (1.0f / octaveScale);
			    var signal = Perlin4D (position * octaveScale);
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
	
	    public float RidgedMultiFractal3D (float x, float y, float z, float scale, float offset, float gain, int octaves)
	    {
		    float rValue = 0;
		    var position = new Vector3 (x / scale, y / scale, z / scale);
		    float divisor = 0;
		    float weight = 1;
		    for (var i = 0; i < octaves; ++i) 
		    {
			    var octaveScale = (float)Math.Pow(2, i);
			    divisor += (1.0f / octaveScale);
			    var signal = Perlin3D (position * octaveScale);
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


	    public float Perlin4Dfbm (float x, float y, float z, float w, float scale, float offset, int octaves)
	    {
		    float rValue = 0;
		    var position = new Vector4 (x / scale, y / scale, z / scale, w / scale);
		    float divisor = 0;
		    for (var i = 0; i < octaves; ++i) 
		    {
                var octaveScale = (float)Math.Pow(2, i);
			    divisor += (1.0f / octaveScale);
			    rValue += (1.0f / octaveScale) * (Perlin4D (position * octaveScale) + offset);
		    }
		
		    return rValue / divisor;
	    }
	
	    public float Perlin3Dfmb (float x, float y, float z, float scale, float offset, int octaves)
	    {
		    float rValue = 0;
		    var position = new Vector3 (x / scale, y / scale, z / scale);
		    float divisor = 0;
		    for (var i = 0; i < octaves; ++i) 
		    {
                var octaveScale = (float)Math.Pow(2, i);
			    divisor += (1.0f / octaveScale);
			    rValue += (1.0f / octaveScale) * (Perlin3D (position * octaveScale) + offset);
		    }
		    return rValue  / divisor;
	    }

        private float Perlin4D(Vector4 pos)
	    {
		    return Perlin4D(pos.X, pos.Y, pos.Z, pos.W);
	    }

        private float Perlin3D(Vector3 pos)
	    {
		    return Perlin3D(pos.X, pos.Y, pos.Z);
	    }

        private float Perlin3D(double pX, double pY, double pZ) {
		    double n0, n1, n2, n3; // Noise contributions from the four corners
		    // Skew the input space to determine which simplex cell we're in
		    var F3 = 1.0/3.0; // Very nice and simple skew factor for 3D
		    var s = (pX+pY+pZ)*F3;
            int i = MathUtilities.FastFloor(pX + s);
            int j = MathUtilities.FastFloor(pY + s);
            int k = MathUtilities.FastFloor(pZ + s);
		    var G3 = 1.0/6.0; // Very nice and simple unskew factor, too
		    var t = (i+j+k)*G3; 
		    var X0 = i-t; // Unskew the cell origin back to (x,y,z) space
		    var Y0 = j-t;
		    var Z0 = k-t;
		    var x0 = pX-X0; // The x,y,z distances from the cell origin
		    var y0 = pY-Y0;
		    var z0 = pZ-Z0;
		    // For the 3D case, the simplex shape is a slightly irregular tetrahedron.
		    // Determine which simplex we are in.
		    int i1, j1, k1; // Offsets for second corner of simplex in (i,j,k) coords
		    int i2, j2, k2; // Offsets for third corner of simplex in (i,j,k) coords
		    if (x0 >= y0)
		    {
			    if (y0 >= z0)
			    {
				    i1 = 1;
				    j1 = 0;
				    k1 = 0;
				    i2 = 1;
				    j2 = 1;
				    k2 = 0;
			    } // X Y Z order
			    else if (x0 >= z0)
			    {
				    i1 = 1;
				    j1 = 0;
				    k1 = 0;
				    i2 = 1;
				    j2 = 0;
				    k2 = 1;
			    } // X Z Y order
			    else
			    {
				    i1 = 0;
				    j1 = 0;
				    k1 = 1;
				    i2 = 1;
				    j2 = 0;
				    k2 = 1;
			    } // Z X Y order
		    }
		    else
		    { // x0<y0
			    if (y0 < z0)
			    {
				    i1 = 0;
				    j1 = 0;
				    k1 = 1;
				    i2 = 0;
				    j2 = 1;
				    k2 = 1;
			    } // Z Y X order
			    else if (x0 < z0)
			    {
				    i1 = 0;
				    j1 = 1;
				    k1 = 0;
				    i2 = 0;
				    j2 = 1;
				    k2 = 1;
			    } // Y Z X order
			    else
			    {
				    i1 = 0;
				    j1 = 1;
				    k1 = 0;
				    i2 = 1;
				    j2 = 1;
				    k2 = 0;
			    } // Y X Z order
		    }
		    // A step of (1,0,0) in (i,j,k) means a step of (1-c,-c,-c) in (x,y,z),
		    // a step of (0,1,0) in (i,j,k) means a step of (-c,1-c,-c) in (x,y,z), and
		    // a step of (0,0,1) in (i,j,k) means a step of (-c,-c,1-c) in (x,y,z), where
		    // c = 1/6.
		    var x1 = x0 - i1 + G3; // Offsets for second corner in (x,y,z) coords
		    var y1 = y0 - j1 + G3;
		    var z1 = z0 - k1 + G3;
		    var x2 = x0 - i2 + 2.0*G3; // Offsets for third corner in (x,y,z) coords
		    var y2 = y0 - j2 + 2.0*G3;
		    var z2 = z0 - k2 + 2.0*G3;
		    var x3 = x0 - 1.0 + 3.0*G3; // Offsets for last corner in (x,y,z) coords
		    var y3 = y0 - 1.0 + 3.0*G3;
		    var z3 = z0 - 1.0 + 3.0*G3;
		    // Work out the hashed gradient indices of the four simplex corners
		    var ii = i & 255;
		    var jj = j & 255;
		    var kk = k & 255;
		    var gi0 = m_perm[ii+m_perm[jj+m_perm[kk]]] % 12;
		    var gi1 = m_perm[ii+i1+m_perm[jj+j1+m_perm[kk+k1]]] % 12;
		    var gi2 = m_perm[ii+i2+m_perm[jj+j2+m_perm[kk+k2]]] % 12;
		    var gi3 = m_perm[ii+1+m_perm[jj+1+m_perm[kk+1]]] % 12;
		    // Calculate the contribution from the four corners
		    var t0 = 0.5 - x0*x0 - y0*y0 - z0*z0;
		    if(t0<0) n0 = 0.0;
		    else {
			    t0 *= t0;
			    n0 = t0 * t0 * dot(grad3[gi0], x0, y0, z0);
		    }
		    var t1 = 0.5 - x1*x1 - y1*y1 - z1*z1;
		    if(t1<0) n1 = 0.0;
		    else {
			    t1 *= t1;
			    n1 = t1 * t1 * dot(grad3[gi1], x1, y1, z1);
		    }
		    var t2 = 0.5 - x2*x2 - y2*y2 - z2*z2;
		    if(t2<0) n2 = 0.0;
		    else {
			    t2 *= t2;
			    n2 = t2 * t2 * dot(grad3[gi2], x2, y2, z2);
		    }
		    var t3 = 0.5 - x3*x3 - y3*y3 - z3*z3;
		    if(t3<0) n3 = 0.0;
		    else {
			    t3 *= t3;
			    n3 = t3 * t3 * dot(grad3[gi3], x3, y3, z3);
		    }
		    // Add contributions from each corner to get the final noise value.
		    // The result is scaled to stay just inside [-1,1]
		    return (float)(32.0*(n0 + n1 + n2 + n3));
	    }

        private float Perlin4D(double pX, double pY, double pZ, double pW)
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
		    var gi0 = m_perm[ii+m_perm[jj+m_perm[kk+m_perm[ll]]]] % 32;
		    var gi1 = m_perm[ii+i1+m_perm[jj+j1+m_perm[kk+k1+m_perm[ll+l1]]]] % 32;
		    var gi2 = m_perm[ii+i2+m_perm[jj+j2+m_perm[kk+k2+m_perm[ll+l2]]]] % 32;
		    var gi3 = m_perm[ii+i3+m_perm[jj+j3+m_perm[kk+k3+m_perm[ll+l3]]]] % 32;
		    var gi4 = m_perm[ii+1+m_perm[jj+1+m_perm[kk+1+m_perm[ll+1]]]] % 32;
		    // Calculate the contribution from the five corners
		    var t0 = 0.6 - x0*x0 - y0*y0 - z0*z0 - w0*w0;
		    if(t0<0) n0 = 0.0;
		    else {
			    t0 *= t0;
			    n0 = t0 * t0 * dot(grad4[gi0], x0, y0, z0, w0);
		    }
		    var t1 = 0.6 - x1*x1 - y1*y1 - z1*z1 - w1*w1;
		    if(t1<0) n1 = 0.0;
		    else {
			    t1 *= t1;
			    n1 = t1 * t1 * dot(grad4[gi1], x1, y1, z1, w1);
		    }
		    var t2 = 0.6 - x2*x2 - y2*y2 - z2*z2 - w2*w2;
		    if(t2<0) n2 = 0.0;
		    else {
			    t2 *= t2;
			    n2 = t2 * t2 * dot(grad4[gi2], x2, y2, z2, w2);
		    }
		    var t3 = 0.6 - x3*x3 - y3*y3 - z3*z3 - w3*w3;
		    if(t3<0) n3 = 0.0;
		    else {
			    t3 *= t3;
			    n3 = t3 * t3 * dot(grad4[gi3], x3, y3, z3, w3);
		    }
		    var t4 = 0.6 - x4*x4 - y4*y4 - z4*z4 - w4*w4;
		    if(t4<0) n4 = 0.0;
		    else {
			    t4 *= t4;
			    n4 = t4 * t4 * dot(grad4[gi4], x4, y4, z4, w4);
		    }
		    // Sum up and scale the result to cover the range [-1,1]
		    return (float)(27.0 * (n0 + n1 + n2 + n3 + n4));
        }
        
        private static double dot(int[] g, double x, double y, double z, double w)
        {
            return g[0] * x + g[1] * y + g[2] * z + g[3] * w;
        }

        private static double dot(int[] g, double x, double y, double z)
        {
            return g[0] * x + g[1] * y + g[2] * z;
        }
    }
}