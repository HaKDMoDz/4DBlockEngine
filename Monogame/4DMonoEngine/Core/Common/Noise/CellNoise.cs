using System;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Common.Noise
{
    public class CellNoise
    {
        private static int[] poissonCount = new int[]  
	    {4, 3, 1, 1, 1, 2, 4, 2, 2, 2, 5, 1, 0, 2, 1, 2, 2, 0, 4, 3, 2, 1, 2, 1, 3, 2, 2, 4, 2, 2, 5, 1, 2, 3,
		    2, 2, 2, 2, 2, 3, 2, 4, 2, 5, 3, 2, 2, 2, 5, 3, 3, 5, 2, 1, 3, 3, 4, 4, 2, 3, 0, 4, 2, 2, 2, 1, 3, 2,
		    2, 2, 3, 3, 3, 1, 2, 0, 2, 1, 1, 2, 2, 2, 2, 5, 3, 2, 3, 2, 3, 2, 2, 1, 0, 2, 1, 1, 2, 1, 2, 2, 1, 3,
		    4, 2, 2, 2, 5, 4, 2, 4, 2, 2, 5, 4, 3, 2, 2, 5, 4, 3, 3, 3, 5, 2, 2, 2, 2, 2, 3, 1, 1, 4, 2, 1, 3, 3,
		    4, 3, 2, 4, 3, 3, 3, 4, 5, 1, 4, 2, 4, 3, 1, 2, 3, 5, 3, 2, 1, 3, 1, 3, 3, 3, 2, 3, 1, 5, 5, 4, 2, 2,
		    4, 1, 3, 4, 1, 5, 3, 3, 5, 3, 4, 3, 2, 2, 1, 1, 1, 1, 1, 2, 4, 5, 4, 5, 4, 2, 1, 5, 1, 1, 2, 3, 3, 3,
		    2, 5, 2, 3, 3, 2, 0, 2, 1, 1, 4, 2, 1, 3, 2, 1, 2, 2, 3, 2, 5, 5, 3, 4, 5, 5, 2, 4, 4, 5, 3, 2, 2, 2,
		    1, 4, 2, 3, 3, 4, 2, 5, 4, 2, 4, 2, 2, 2, 4, 5, 3, 2};
        private static float featurePointMap = 1.0f / 4294967296.0f;
        private ulong m_seed;
	    private ulong lcgCurrSeed;
	    private int[] perm;
	    public CellNoise(ulong seed)
        {
		    m_seed = seed;
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
                float octaveScale = (float)Math.Pow(2, i);
                divisor += (1.0f / octaveScale);
                rValue += (1.0f / octaveScale) * (VoroniBlend(px * octaveScale, py * octaveScale, pz * octaveScale) + offset);
            }
            return rValue / divisor;
        }

        public float VoroniBlend(float px, float py, float pz)
        {
            float[] basisFunctions = new float[2];
            uint[] ids = new uint[2];
            Vector3[] deltas = new Vector3[2];
            Worley(new Vector3(px, py, pz), 2, basisFunctions, deltas, ids);
            return basisFunctions[1] - basisFunctions[0];
        }

        public struct VoroniData
        {
            public float blend;
            public uint id;
            public Vector3 delta;
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
            Worley(new Vector3(px, py, pz), 2, basisFunctions, deltas, ids);
            VoroniData data = new VoroniData();
            data.blend = basisFunctions[1] - basisFunctions[0];
            data.id = ids[0];
            data.delta = deltas[0];
            return data;
        }

        private void Worley(Vector3 at, int maxOrder, float[] basisFunctions, Vector3[] deltas, uint[] ID)
        {
            int intAtX = MathUtilities.FastFloor(at.X);
            int intAtY = MathUtilities.FastFloor(at.Y);
            int intAtZ = MathUtilities.FastFloor(at.Z);

            for (int i = 0; i < maxOrder; ++i)
            {
                basisFunctions[i] = float.MaxValue;
                deltas[i] = new Vector3();
            }

            AddSamples(intAtX, intAtY, intAtZ, maxOrder, ref at, deltas, basisFunctions, ID);

            float x2 = at.X - intAtX;
            float y2 = at.Y - intAtY;
            float z2 = at.Z - intAtZ;
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

            for (int i = 0; i < maxOrder; ++i)
            {
                basisFunctions[i] = (float)Math.Sqrt(basisFunctions[i]);
            }
        }

        private void AddSamples(int x, int y, int z, int maxOrder, ref Vector3 at, Vector3[] deltas, float[] basisFunctions, uint[] ID)
        {
            uint seed = (uint)(702395077 * x + 915488749 * y + 2120969693 * z);
            int count = poissonCount[seed >> 24];
            seed = 142024253 * (seed + (uint)m_seed) + 586950981;

            for (int j = 0; j < count; ++j)
            {
                uint thisId = seed;
                seed = 142024253 * seed + 586950981;
                float fx = (seed + 0.5f) * featurePointMap;
                seed = 142024253 * seed + 586950981;
                float fy = (seed + 0.5f) * featurePointMap;
                seed = 142024253 * seed + 586950981;
                float fz = (seed + 0.5f) * featurePointMap;

                float dx = x + fx - at.X;
                float dy = y + fy - at.Y;
                float dz = z + fz - at.Z;

                float d2 = dx * dx + dy * dy + dz * dz;

                if (d2 < basisFunctions[maxOrder - 1])
                {
                    int index = maxOrder;
                    while (index > 0 && d2 < basisFunctions[index - 1]) --index;
                    for (int i = maxOrder - 1; i > index; --i)
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
