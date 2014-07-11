using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Structs.Vector
{
    public struct Vector3Byte
    {
        public byte X;
        public byte Y;
        public byte Z;

        public Vector3Byte(byte x, byte y, byte z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3Byte(Vector3 vector)
        {
            X = (byte)vector.X;
            Y = (byte)vector.Y;
            Z = (byte)vector.Z;
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector3Byte) return Equals((Vector3Byte)obj);
            else return false;
        }

        public bool Equals(Vector3Byte other)
        {
            return (((X == other.X) && (Y == other.Y)) && (Z == other.Z));
        }

        public static bool operator ==(Vector3Byte value1, Vector3Byte value2)
        {
            return (((value1.X == value2.X) && (value1.Y == value2.Y)) && (value1.Z == value2.Z));
        }

        public static bool operator !=(Vector3Byte value1, Vector3Byte value2)
        {
            if ((value1.X == value2.X) && (value1.Y == value2.Y)) return value1.Z != value2.Z;
            return true;
        }

        public override int GetHashCode()
        {
            return ((X.GetHashCode() + Y.GetHashCode()) + Z.GetHashCode());
        }

        public override string ToString()
        {
            return string.Format("{{X:{0} Y:{1} Z:{2}}}", X, Y, Z);
        }

        public Vector3 AsVector3()
        {
            return new Vector3(X, Y, Z);
        }
    }
}