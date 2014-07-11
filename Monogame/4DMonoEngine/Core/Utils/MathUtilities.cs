using System;

namespace _4DMonoEngine.Core.Utils
{
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
            return x < 0 ? x % m + m : x % m;
        }
    }
}
