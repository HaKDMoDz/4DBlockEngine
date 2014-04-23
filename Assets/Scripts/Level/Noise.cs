using UnityEngine;
using System.Collections;

//TAKEN FROM : http://webstaff.itn.liu.se/~stegu/simplexnoise/simplexnoise.pdf

public class Noise
{
	// To remove the need for index wrapping, double the permutation table length
	private ulong m_seed;
	private ulong lcgCurrSeed;
	private int[] perm;
	public Noise(ulong seed)
    {
		m_seed = seed;
		lcgCurrSeed = 6364136223846793005 * seed + 1442695040888963407;
		int[] permTemp = new int[256];
        for (uint i = 0; i < 256; i++)
		{
			permTemp[i] = p[i];
		}
		for(int i = 255; i > 0; --i)
		{
			int j = randInt(i);
			int permTempJ = permTemp[j];
			permTemp[j] = permTemp[i];
			permTemp[i] = permTempJ;
		}
		perm = new int[512];
		for (uint i = 0; i < 512; i++)
		{
			perm[i] = permTemp[i & 255];
		}

    }

	private int randInt(int max)
	{
		return (int)(max * rand());
	}

	public float rand()
	{
		lcgCurrSeed = 6364136223846793005 * lcgCurrSeed + 1442695040888963407;
		return (float)(lcgCurrSeed / (double)ulong.MaxValue);
	}

	public float RidgedMultiFractal4D (float x, float y, float z, float w, float scale, float offset, float gain, int octaves)
	{
		float rValue = 0;
		Vector4 position = new Vector4 (x / scale, y / scale, z / scale, w / scale);
		float divisor = 0;
		float weight = 1;
		for (int i = 0; i < octaves; ++i) 
		{
			float octaveScale = Mathf.Pow(2, i);
			divisor += (1.0f / octaveScale);
			float signal = Perlin4D (position * octaveScale);
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
		Vector3 position = new Vector4 (x / scale, y / scale, z / scale);
		float divisor = 0;
		float weight = 1;
		for (int i = 0; i < octaves; ++i) 
		{
			float octaveScale = Mathf.Pow(2, i);
			divisor += (1.0f / octaveScale);
			float signal = Perlin3D (position * octaveScale);
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


	public float Perlin4DFBM (float x, float y, float z, float w, float scale, float offset, int octaves)
	{
		float rValue = 0;
		Vector4 position = new Vector4 (x / scale, y / scale, z / scale, w / scale);
		float divisor = 0;
		for (int i = 0; i < octaves; ++i) 
		{
			float octaveScale = Mathf.Pow(2, i);
			divisor += (1.0f / octaveScale);
			rValue += (1.0f / octaveScale) * (Perlin4D (position * octaveScale) + offset);
		}
		
		return rValue / divisor;
	}
	
	public float Perlin3DFMB (float x, float y, float z, float scale, float offset, int octaves)
	{
		float rValue = 0;
		Vector3 position = new Vector3 (x / scale, y / scale, z / scale);
		float divisor = 0;
		for (int i = 0; i < octaves; ++i) 
		{
			float octaveScale = Mathf.Pow(2, i);
			divisor += (1.0f / octaveScale);
			rValue += (1.0f / octaveScale) * (Perlin3D (position * octaveScale) + offset);
		}
		return rValue  / divisor;
	}
  
	public float Perlin4D(Vector4 pos)
	{
		return Perlin4D(pos.x, pos.y, pos.z, pos.w);
	}

	public float Perlin3D(Vector3 pos)
	{
		return Perlin3D(pos.x, pos.y, pos.z);
	}

	public float Perlin3D(double pX, double pY, double pZ) {
		double n0, n1, n2, n3; // Noise contributions from the four corners
		// Skew the input space to determine which simplex cell we're in
		double F3 = 1.0/3.0; // Very nice and simple skew factor for 3D
		double s = (pX+pY+pZ)*F3;
		int i = fastfloor(pX+s);
		int j = fastfloor(pY+s);
		int k = fastfloor(pZ+s);
		double G3 = 1.0/6.0; // Very nice and simple unskew factor, too
		double t = (i+j+k)*G3; 
		double X0 = i-t; // Unskew the cell origin back to (x,y,z) space
		double Y0 = j-t;
		double Z0 = k-t;
		double x0 = pX-X0; // The x,y,z distances from the cell origin
		double y0 = pY-Y0;
		double z0 = pZ-Z0;
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
		double x1 = x0 - i1 + G3; // Offsets for second corner in (x,y,z) coords
		double y1 = y0 - j1 + G3;
		double z1 = z0 - k1 + G3;
		double x2 = x0 - i2 + 2.0*G3; // Offsets for third corner in (x,y,z) coords
		double y2 = y0 - j2 + 2.0*G3;
		double z2 = z0 - k2 + 2.0*G3;
		double x3 = x0 - 1.0 + 3.0*G3; // Offsets for last corner in (x,y,z) coords
		double y3 = y0 - 1.0 + 3.0*G3;
		double z3 = z0 - 1.0 + 3.0*G3;
		// Work out the hashed gradient indices of the four simplex corners
		int ii = i & 255;
		int jj = j & 255;
		int kk = k & 255;
		int gi0 = perm[ii+perm[jj+perm[kk]]] % 12;
		int gi1 = perm[ii+i1+perm[jj+j1+perm[kk+k1]]] % 12;
		int gi2 = perm[ii+i2+perm[jj+j2+perm[kk+k2]]] % 12;
		int gi3 = perm[ii+1+perm[jj+1+perm[kk+1]]] % 12;
		// Calculate the contribution from the four corners
		double t0 = 0.5 - x0*x0 - y0*y0 - z0*z0;
		if(t0<0) n0 = 0.0;
		else {
			t0 *= t0;
			n0 = t0 * t0 * dot(grad3[gi0], x0, y0, z0);
		}
		double t1 = 0.5 - x1*x1 - y1*y1 - z1*z1;
		if(t1<0) n1 = 0.0;
		else {
			t1 *= t1;
			n1 = t1 * t1 * dot(grad3[gi1], x1, y1, z1);
		}
		double t2 = 0.5 - x2*x2 - y2*y2 - z2*z2;
		if(t2<0) n2 = 0.0;
		else {
			t2 *= t2;
			n2 = t2 * t2 * dot(grad3[gi2], x2, y2, z2);
		}
		double t3 = 0.5 - x3*x3 - y3*y3 - z3*z3;
		if(t3<0) n3 = 0.0;
		else {
			t3 *= t3;
			n3 = t3 * t3 * dot(grad3[gi3], x3, y3, z3);
		}
		// Add contributions from each corner to get the final noise value.
		// The result is scaled to stay just inside [-1,1]
		return (float)(32.0*(n0 + n1 + n2 + n3));
	}

    public float Perlin4D(double pX, double pY, double pZ, double pW)
	{
		double n0, n1, n2, n3, n4; // Noise contributions from the five corners
		// Skew the (x,y,z,w) space to determine which cell of 24 simplices we're in
		double s = (pX + pY + pZ + pW) * F4; // Factor for 4D skewing
		int i = fastfloor(pX + s);
		int j = fastfloor(pY + s);
		int k = fastfloor(pZ + s);
		int l = fastfloor(pW + s);
		double t = (i + j + k + l) * G4; // Factor for 4D unskewing
		double X0 = i - t; // Unskew the cell origin back to (x,y,z,w) space
		double Y0 = j - t;
		double Z0 = k - t;
		double W0 = l - t;
		double x0 = pX - X0;  // The x,y,z,w distances from the cell origin
		double y0 = pY - Y0;
		double z0 = pZ - Z0;
		double w0 = pW - W0;
		// For the 4D case, the simplex is a 4D shape I won't even try to describe.
		// To find out which of the 24 possible simplices we're in, we need to
		// determine the magnitude ordering of x0, y0, z0 and w0.
		// Six pair-wise comparisons are performed between each possible pair
		// of the four coordinates, and the results are used to rank the numbers.
		int rankx = 0;
		int ranky = 0;
		int rankz = 0;
		int rankw = 0;
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
		double x1 = x0 - i1 + G4; // Offsets for second corner in (x,y,z,w) coords
		double y1 = y0 - j1 + G4;
		double z1 = z0 - k1 + G4;
		double w1 = w0 - l1 + G4;
		double x2 = x0 - i2 + 2.0*G4; // Offsets for third corner in (x,y,z,w) coords
		double y2 = y0 - j2 + 2.0*G4;
		double z2 = z0 - k2 + 2.0*G4;
		double w2 = w0 - l2 + 2.0*G4;
		double x3 = x0 - i3 + 3.0*G4; // Offsets for fourth corner in (x,y,z,w) coords
		double y3 = y0 - j3 + 3.0*G4;
		double z3 = z0 - k3 + 3.0*G4;
		double w3 = w0 - l3 + 3.0*G4;
		double x4 = x0 - 1.0 + 4.0*G4; // Offsets for last corner in (x,y,z,w) coords
		double y4 = y0 - 1.0 + 4.0*G4;
		double z4 = z0 - 1.0 + 4.0*G4;
		double w4 = w0 - 1.0 + 4.0*G4;
		// Work out the hashed gradient indices of the five simplex corners
		int ii = i & 255;
		int jj = j & 255;
		int kk = k & 255;
		int ll = l & 255;
		int gi0 = perm[ii+perm[jj+perm[kk+perm[ll]]]] % 32;
		int gi1 = perm[ii+i1+perm[jj+j1+perm[kk+k1+perm[ll+l1]]]] % 32;
		int gi2 = perm[ii+i2+perm[jj+j2+perm[kk+k2+perm[ll+l2]]]] % 32;
		int gi3 = perm[ii+i3+perm[jj+j3+perm[kk+k3+perm[ll+l3]]]] % 32;
		int gi4 = perm[ii+1+perm[jj+1+perm[kk+1+perm[ll+1]]]] % 32;
		// Calculate the contribution from the five corners
		double t0 = 0.6 - x0*x0 - y0*y0 - z0*z0 - w0*w0;
		if(t0<0) n0 = 0.0;
		else {
			t0 *= t0;
			n0 = t0 * t0 * dot(grad4[gi0], x0, y0, z0, w0);
		}
		double t1 = 0.6 - x1*x1 - y1*y1 - z1*z1 - w1*w1;
		if(t1<0) n1 = 0.0;
		else {
			t1 *= t1;
			n1 = t1 * t1 * dot(grad4[gi1], x1, y1, z1, w1);
		}
		double t2 = 0.6 - x2*x2 - y2*y2 - z2*z2 - w2*w2;
		if(t2<0) n2 = 0.0;
		else {
			t2 *= t2;
			n2 = t2 * t2 * dot(grad4[gi2], x2, y2, z2, w2);
		}
		double t3 = 0.6 - x3*x3 - y3*y3 - z3*z3 - w3*w3;
		if(t3<0) n3 = 0.0;
		else {
			t3 *= t3;
			n3 = t3 * t3 * dot(grad4[gi3], x3, y3, z3, w3);
		}
		double t4 = 0.6 - x4*x4 - y4*y4 - z4*z4 - w4*w4;
		if(t4<0) n4 = 0.0;
		else {
			t4 *= t4;
			n4 = t4 * t4 * dot(grad4[gi4], x4, y4, z4, w4);
		}
		// Sum up and scale the result to cover the range [-1,1]
		return (float)(27.0 * (n0 + n1 + n2 + n3 + n4));
    }

	public struct VoroniData
	{
		public float blend;
		public uint id;
		public Vector3 delta;
	}

	public float VoroniFBM(float x, float y, float z, float scale, float offset, float octaves)
	{
		float rValue = 0;
		float px = x / scale;
		float py = y / scale;
		float pz = z / scale;
		float divisor = 0;
		for (int i = 0; i < octaves; ++i) 
		{
			float octaveScale = Mathf.Pow(2, i);
			divisor += (1.0f / octaveScale);
			rValue += (1.0f / octaveScale) * (VoroniBlend (px * octaveScale, py * octaveScale, pz * octaveScale) + offset);
		}
		return rValue  / divisor;
	}

	public float VoroniBlend(float px, float py, float pz)
	{
		float[] basisFunctions = new float[2];
		uint[] ids = new uint[2];
		Vector3[] deltas = new Vector3[2];
		Worley (new Vector3 (px, py, pz), 2, basisFunctions, deltas, ids);
		return basisFunctions [1] - basisFunctions [0];
	}

	public VoroniData Voroni(float px, float py, float pz, float scale)
	{
		return Voroni(px / scale, py / scale, pz / scale);
	}

	public VoroniData Voroni(float px, float py, float pz)
	{
		float[] basisFunctions = new float[2];
		uint[] ids = new uint[2];
		Vector3[] deltas = new Vector3[2];
		Worley (new Vector3 (px, py, pz), 2, basisFunctions, deltas, ids);
		VoroniData data = new VoroniData ();
		data.blend = basisFunctions [1] - basisFunctions [0];
		data.id = ids [0];
		data.delta = deltas [0];
		return data;
	}

	private void Worley(Vector3 at, int maxOrder, float[] basisFunctions, Vector3[] deltas, uint[] ID)
	{
		int intAtX = Mathf.FloorToInt(at.x);
		int intAtY = Mathf.FloorToInt(at.y);
		int intAtZ = Mathf.FloorToInt(at.z);
		
		for(int i = 0; i < maxOrder; ++i)
		{
			basisFunctions[i] = float.MaxValue;	
			deltas[i] = new Vector3();
		}
		
		AddSamples(intAtX, intAtY, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);
		
		float x2 = at.x - intAtX;
		float y2 = at.y - intAtY;
		float z2 = at.z - intAtZ;
		x2 = x2 < 0 ? 1 - x2 : x2;
		y2 = y2 < 0 ? 1 - y2 : y2;
		z2 = z2 < 0 ? 1 - z2 : z2;
		float mx2 = (1.0f - x2) * (1.0f - x2);
		float my2 = (1.0f - y2) * (1.0f - y2);
		float mz2 = (1.0f - z2) * (1.0f - z2);	
		x2 *= x2;
		y2 *= y2;
		z2 *= z2;
		
		//6 face cubes
		if(x2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX - 1, intAtY, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(y2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX, intAtY - 1, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(z2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX, intAtY, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(mx2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX + 1, intAtY, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(my2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX, intAtY + 1, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(mz2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX, intAtY, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		
		//12 edge cubes
		if(x2 + y2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX - 1, intAtY - 1, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(x2 + z2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX - 1, intAtY, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(y2 + z2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX, intAtY - 1, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
		}
		if(mx2 + my2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX + 1, intAtY + 1, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(mx2 + mz2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX + 1, intAtY, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(my2 + mz2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX, intAtY + 1, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
		}
		if(x2 + my2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX - 1, intAtY + 1, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(x2 + mz2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX - 1, intAtY, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(y2 + mz2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX, intAtY - 1, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
		}
		if(mx2 + y2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX + 1, intAtY - 1, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(mx2 + z2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX + 1, intAtY, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(my2 + z2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX, intAtY + 1, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
		}
		
		//8 corner cubes
		if(x2 + y2 + z2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX - 1, intAtY - 1, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(x2 + y2 + mz2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX - 1, intAtY - 1, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(x2 + my2 + z2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX -1, intAtY + 1, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
		}
		if(x2 +my2 + mz2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX - 1, intAtY + 1, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(mx2 + y2 + z2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX + 1, intAtY - 1, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(mx2 + y2 + mz2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX + 1, intAtY - 1, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(mx2 + my2 + z2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX + 1, intAtY + 1, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		if(mx2 + my2 + mz2 < basisFunctions[maxOrder - 1])
		{
			AddSamples(intAtX + 1, intAtY + 1, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
		} 
		
		for(int i = 0; i < maxOrder; ++i)
		{
			basisFunctions[i] = Mathf.Sqrt(basisFunctions[i]);
		}	
	}
	
	private void AddSamples(int x, int y, int z, int maxOrder, ref Vector3 at, Vector3[] deltas, float[] basisFunctions, uint[] ID)
	{
		uint seed = (uint)(702395077 * x + 915488749 * y + 2120969693 * z);
		int count = poissonCount[seed >> 24];
		seed = 142024253 * (seed + (uint)m_seed) + 586950981;
		
		for(int j = 0; j < count; ++j)
		{
			uint thisId = seed;
			seed = 142024253 * seed + 586950981;
			float fx = (seed + 0.5f) * featurePointMap;
			seed = 142024253 * seed + 586950981;
			float fy = (seed + 0.5f) * featurePointMap;
			seed = 142024253 * seed + 586950981;
			float fz = (seed + 0.5f) * featurePointMap;
			
			float dx = x + fx - at.x;
			float dy = y + fy - at.y;
			float dz = z + fz - at.z;
			
			float d2 = dx * dx + dy * dy + dz * dz;
			
			if(d2 < basisFunctions[maxOrder - 1])
			{
				int index = maxOrder;
				while(index > 0 && d2 < basisFunctions[index - 1]) --index;
				for(int i = maxOrder - 1; i > index; --i)
				{
					basisFunctions[i] = basisFunctions[i - 1];
					deltas[i] = deltas[i - 1];
					ID[i] = ID[i - 1];
				}
				basisFunctions[index] = d2;
				deltas[index] = new Vector3(dx, dy, dz);
				ID[index] = thisId;
			}
		}
	}

	private static int fastfloor(double x)
	{
		return x > 0 ? (int)x : (int)x - 1;
	}
	
	private static double dot(int[] g, double x, double y, double z, double w)
	{
		return g[0] * x + g[1] * y + g[2] * z + g[3] * w;
	}
	
	private static double dot(int[] g, double x, double y, double z)
	{
		return g[0] * x + g[1] * y + g[2] * z;
	}

	
	private static int[][] grad3 = new int[][] {
		new int[] {1,1,0}, new int[] {-1,1,0}, new int[] {1,-1,0}, new int[] {-1,-1,0},
		new int[] {1,0,1}, new int[] {-1,0,1}, new int[] {1,0,-1}, new int[] {-1,0,-1},
		new int[] {0,1,1}, new int[] {0,-1,1}, new int[] {0,1,-1}, new int[] {0,-1,-1}
	};
	
	private static int[][] grad4= new int[][]{
		new int[] {0,1,1,1}, new int[] {0,1,1,-1}, new int[] {0,1,-1,1}, new int[] {0,1,-1,-1},
		new int[] {0,-1,1,1}, new int[] {0,-1,1,-1}, new int[] {0,-1,-1,1}, new int[] {0,-1,-1,-1},
		new int[] {1,0,1,1}, new int[] {1,0,1,-1}, new int[] {1,0,-1,1}, new int[] {1,0,-1,-1},
		new int[] {-1,0,1,1}, new int[] {-1,0,1,-1}, new int[] {-1,0,-1,1}, new int[] {-1,0,-1,-1},
		new int[] {1,1,0,1}, new int[] {1,1,0,-1}, new int[] {1,-1,0,1}, new int[] {1,-1,0,-1},
		new int[] {-1,1,0,1}, new int[] {-1,1,0,-1}, new int[] {-1,-1,0,1}, new int[] {-1,-1,0,-1},
		new int[] {1,1,1,0}, new int[] {1,1,-1,0}, new int[] {1,-1,1,0}, new int[] {1,-1,-1,0},
		new int[] {-1,1,1,0}, new int[] {-1,1,-1,0}, new int[] {-1,-1,1,0}, new int[] {-1,-1,-1,0}
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
	
	private static int[] poissonCount = new int[]  
	{4, 3, 1, 1, 1, 2, 4, 2, 2, 2, 5, 1, 0, 2, 1, 2, 2, 0, 4, 3, 2, 1, 2, 1, 3, 2, 2, 4, 2, 2, 5, 1, 2, 3,
		2, 2, 2, 2, 2, 3, 2, 4, 2, 5, 3, 2, 2, 2, 5, 3, 3, 5, 2, 1, 3, 3, 4, 4, 2, 3, 0, 4, 2, 2, 2, 1, 3, 2,
		2, 2, 3, 3, 3, 1, 2, 0, 2, 1, 1, 2, 2, 2, 2, 5, 3, 2, 3, 2, 3, 2, 2, 1, 0, 2, 1, 1, 2, 1, 2, 2, 1, 3,
		4, 2, 2, 2, 5, 4, 2, 4, 2, 2, 5, 4, 3, 2, 2, 5, 4, 3, 3, 3, 5, 2, 2, 2, 2, 2, 3, 1, 1, 4, 2, 1, 3, 3,
		4, 3, 2, 4, 3, 3, 3, 4, 5, 1, 4, 2, 4, 3, 1, 2, 3, 5, 3, 2, 1, 3, 1, 3, 3, 3, 2, 3, 1, 5, 5, 4, 2, 2,
		4, 1, 3, 4, 1, 5, 3, 3, 5, 3, 4, 3, 2, 2, 1, 1, 1, 1, 1, 2, 4, 5, 4, 5, 4, 2, 1, 5, 1, 1, 2, 3, 3, 3,
		2, 5, 2, 3, 3, 2, 0, 2, 1, 1, 4, 2, 1, 3, 2, 1, 2, 2, 3, 2, 5, 5, 3, 4, 5, 5, 2, 4, 4, 5, 3, 2, 2, 2,
		1, 4, 2, 3, 3, 4, 2, 5, 4, 2, 4, 2, 2, 2, 4, 5, 3, 2};

	private static double F4 = (Mathf.Sqrt(5.0f)-1.0)/4.0;
	private static double G4 = (5.0-Mathf.Sqrt(5.0f))/20.0;	
	private static float featurePointMap = 1.0f / 4294967296.0f;
}