using System;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Utils
{
    public delegate int MappingFunction(int x, int y, int z);
    public delegate int MappingFunctionVector3(ref Vector3 position);
    public delegate float GetHeight(float x, float z);

    public static class MathUtilities
    {
        private static float[] s_biasTable = new float[101];      	
	    public static float Bias(float value, float bias)
	    {
            if (s_biasTable == null)
            {
                for (int i = 0; i < 101; ++i)
                {
                    s_biasTable[i] = (float)(Math.Log(i * 0.01) / Math.Log(0.5));
                } 
            }
		    return (float)Math.Pow(value, s_biasTable[(int)(bias * 100)]);
	    }

	    public static float Gain(float value, float gain)
	    {
		    float ret;
		    if(value < 0.5f)
		    {
			    ret = Bias(2 * value, 1 - gain) * 0.5f;
		    }
		    else
		    {
			    ret = 1 - Bias(2 - 2 * value, 1 - gain) * 0.5f;
		    }
		    return ret;
	    }

        public static int FastAbs(int i)
        {
            return (i >= 0) ? i : -i;
        }

        public static int FastFloor(float f)
        {
            return f > 0 ? (int)f : (int)f - 1;
        }

        public static int FastFloor(double d)
        {
            return d > 0 ? (int)d : (int)d - 1;
        }

        public static int FastRound(float f)
        {
            int i = (int)f;
            float delta = f - i;
            int round;
            if(delta < 0.5f)
            {
                round = i;
            }
            else if(delta > 0.5f)
            {
                round = i + 1;
            }
            else
            {
                round = i % 2 == 0 ? i : i + 1;
            }
            return round;
        }

        public static int Modulo(int x, int m)
        {
            return ((x % m) + m) % m;
        }

        public static float DistanceFromPointToLineSegment(Vector2 point, Vector2 anchor, Vector2 end)
        {
            var d = end - anchor;
            var length = d.Length();
            if (Math.Abs(length) < 0.0001)
            {
                return (point - anchor).Length();
            }
            d.Normalize();
            var intersect = Vector2.Dot((point - anchor), d);
            if (intersect < 0)
            {
                return (point - anchor).Length();
            }
            return intersect > length ? (point - end).Length() : (point - (anchor + d * intersect)).Length();
        }

        public static float DistanceFromPointToLineSegment(Vector3 point, Vector3 anchor, Vector3 end)
        {
            var d = end - anchor;
            var length = d.Length();
            if (Math.Abs(length) < 0.0001)
            {
                return (point - anchor).Length();
            }
            d.Normalize();
            var intersect = Vector3.Dot((point - anchor), d);
            if (intersect < 0)
            {
                return (point - anchor).Length();
            }
            return intersect > length ? (point - end).Length() : (point - (anchor + d * intersect)).Length();
        }
    }
}
