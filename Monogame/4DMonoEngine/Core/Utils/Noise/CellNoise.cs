using System;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Utils.Noise
{
    public class CellNoise
    {
        private static readonly int[] PoissonCount = new int[]  
	    {4, 3, 1, 1, 1, 2, 4, 2, 2, 2, 5, 1, 0, 2, 1, 2, 2, 0, 4, 3, 2, 1, 2, 1, 3, 2, 2, 4, 2, 2, 5, 1, 2, 3,
		    2, 2, 2, 2, 2, 3, 2, 4, 2, 5, 3, 2, 2, 2, 5, 3, 3, 5, 2, 1, 3, 3, 4, 4, 2, 3, 0, 4, 2, 2, 2, 1, 3, 2,
		    2, 2, 3, 3, 3, 1, 2, 0, 2, 1, 1, 2, 2, 2, 2, 5, 3, 2, 3, 2, 3, 2, 2, 1, 0, 2, 1, 1, 2, 1, 2, 2, 1, 3,
		    4, 2, 2, 2, 5, 4, 2, 4, 2, 2, 5, 4, 3, 2, 2, 5, 4, 3, 3, 3, 5, 2, 2, 2, 2, 2, 3, 1, 1, 4, 2, 1, 3, 3,
		    4, 3, 2, 4, 3, 3, 3, 4, 5, 1, 4, 2, 4, 3, 1, 2, 3, 5, 3, 2, 1, 3, 1, 3, 3, 3, 2, 3, 1, 5, 5, 4, 2, 2,
		    4, 1, 3, 4, 1, 5, 3, 3, 5, 3, 4, 3, 2, 2, 1, 1, 1, 1, 1, 2, 4, 5, 4, 5, 4, 2, 1, 5, 1, 1, 2, 3, 3, 3,
		    2, 5, 2, 3, 3, 2, 0, 2, 1, 1, 4, 2, 1, 3, 2, 1, 2, 2, 3, 2, 5, 5, 3, 4, 5, 5, 2, 4, 4, 5, 3, 2, 2, 2,
		    1, 4, 2, 3, 3, 4, 2, 5, 4, 2, 4, 2, 2, 2, 4, 5, 3, 2};

        private const float FeaturePointMap = 1.0f/4294967296.0f;
        private readonly ulong m_seed;
	    public CellNoise(ulong seed)
        {
		    m_seed = seed;
        }

        public float VoroniFbm(float x, float y, float z, float scale, float offset, float octaves)
        {
            float rValue = 0;
            var px = x / scale;
            var py = y / scale;
            var pz = z / scale;
            float divisor = 0;
            for (var i = 0; i < octaves; ++i)
            {
                var octaveScale = (float)Math.Pow(2, i);
                divisor += (1.0f / octaveScale);
                rValue += (1.0f / octaveScale) * (VoroniBlend(px * octaveScale, py * octaveScale, pz * octaveScale) + offset);
            }
            return rValue / divisor;
        }

        public float VoroniBlend(float px, float py, float pz)
        {
            var basisFunctions = new float[2];
            var ids = new uint[2];
            var deltas = new Vector3[2];
            Worley(new Vector3(px, py, pz), 2, basisFunctions, deltas, ids);
            return basisFunctions[1] - basisFunctions[0];
        }

        public struct VoroniData
        {
            public float Blend;
            public uint Id;
            public Vector3 Delta;
        }

        public VoroniData Voroni(float px, float py, float pz, float scale)
        {
            return Voroni(px / scale, py / scale, pz / scale);
        }

        public VoroniData Voroni(float px, float py, float pz)
        {
            var basisFunctions = new float[2];
            var ids = new uint[2];
            var deltas = new Vector3[2];
            Worley(new Vector3(px, py, pz), 2, basisFunctions, deltas, ids);
            var data = new VoroniData();
            data.Blend = basisFunctions[1] - basisFunctions[0];
            data.Id = ids[0];
            data.Delta = deltas[0];
            return data;
        }

        private void Worley(Vector3 at, int maxOrder, float[] basisFunctions, Vector3[] deltas, uint[] ID)
        {
            int intAtX = MathUtilities.FastFloor(at.X);
            int intAtY = MathUtilities.FastFloor(at.Y);
            int intAtZ = MathUtilities.FastFloor(at.Z);

            for (var i = 0; i < maxOrder; ++i)
            {
                basisFunctions[i] = float.MaxValue;
                deltas[i] = new Vector3();
            }

            AddSamples(intAtX, intAtY, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);

            var x2 = at.X - intAtX;
            var y2 = at.Y - intAtY;
            var z2 = at.Z - intAtZ;
            x2 = x2 < 0 ? 1 - x2 : x2;
            y2 = y2 < 0 ? 1 - y2 : y2;
            z2 = z2 < 0 ? 1 - z2 : z2;
            var mx2 = (1.0f - x2) * (1.0f - x2);
            var my2 = (1.0f - y2) * (1.0f - y2);
            var mz2 = (1.0f - z2) * (1.0f - z2);
            x2 *= x2;
            y2 *= y2;
            z2 *= z2;

            //6 face cubes
            if (x2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX - 1, intAtY, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (y2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX, intAtY - 1, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (z2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX, intAtY, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (mx2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX + 1, intAtY, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (my2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX, intAtY + 1, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (mz2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX, intAtY, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }

            //12 edge cubes
            if (x2 + y2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX - 1, intAtY - 1, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (x2 + z2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX - 1, intAtY, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (y2 + z2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX, intAtY - 1, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (mx2 + my2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX + 1, intAtY + 1, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (mx2 + mz2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX + 1, intAtY, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (my2 + mz2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX, intAtY + 1, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (x2 + my2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX - 1, intAtY + 1, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (x2 + mz2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX - 1, intAtY, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (y2 + mz2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX, intAtY - 1, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (mx2 + y2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX + 1, intAtY - 1, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (mx2 + z2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX + 1, intAtY, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (my2 + z2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX, intAtY + 1, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }

            //8 corner cubes
            if (x2 + y2 + z2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX - 1, intAtY - 1, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (x2 + y2 + mz2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX - 1, intAtY - 1, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (x2 + my2 + z2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX - 1, intAtY + 1, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (x2 + my2 + mz2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX - 1, intAtY + 1, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (mx2 + y2 + z2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX + 1, intAtY - 1, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (mx2 + y2 + mz2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX + 1, intAtY - 1, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (mx2 + my2 + z2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX + 1, intAtY + 1, intAtZ - 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }
            if (mx2 + my2 + mz2 < basisFunctions[maxOrder - 1])
            {
                AddSamples(intAtX + 1, intAtY + 1, intAtZ + 1, maxOrder, ref at, deltas, basisFunctions, ID);
            }

            for (var i = 0; i < maxOrder; ++i)
            {
                basisFunctions[i] = (float)Math.Sqrt(basisFunctions[i]);
            }
        }

        private void AddSamples(int x, int y, int z, int maxOrder, ref Vector3 at, Vector3[] deltas, float[] basisFunctions, uint[] ID)
        {
            var seed = (uint)(702395077 * x + 915488749 * y + 2120969693 * z);
            var count = PoissonCount[seed >> 24];
            seed = 142024253 * (seed + (uint)m_seed) + 586950981;

            for (var j = 0; j < count; ++j)
            {
                var thisId = seed;
                seed = 142024253 * seed + 586950981;
                var fx = (seed + 0.5f) * FeaturePointMap;
                seed = 142024253 * seed + 586950981;
                var fy = (seed + 0.5f) * FeaturePointMap;
                seed = 142024253 * seed + 586950981;
                var fz = (seed + 0.5f) * FeaturePointMap;

                var dx = x + fx - at.X;
                var dy = y + fy - at.Y;
                var dz = z + fz - at.Z;

                var d2 = dx * dx + dy * dy + dz * dz;

                if (d2 < basisFunctions[maxOrder - 1])
                {
                    var index = maxOrder;
                    while (index > 0 && d2 < basisFunctions[index - 1]) --index;
                    for (var i = maxOrder - 1; i > index; --i)
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
    }
}
