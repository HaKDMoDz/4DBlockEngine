using System;
using Microsoft.Xna.Framework;

namespace _4DMonoEngine.Core.Utils.Vector
{
    public struct Vector3Int
    {
        public int X;
        public int Y;
        public int Z;

        public Vector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3Int(Vector3 vector)
        {
            X = (int) vector.X;
            Y = (int) vector.Y;
            Z = (int) vector.Z;
        }

        public float DistanceSquared(ref Vector3Int otherVector)
        {
            var dX = otherVector.X - X;
            var dY = otherVector.X - X;
            var dZ = otherVector.X - X;
            return dX * dX + dY * dY + dZ * dZ;
        }

        public float DistanceSquared(ref Vector3 otherVector)
        {
            var dX = otherVector.X - X;
            var dY = otherVector.X - X;
            var dZ = otherVector.X - X;
            return dX * dX + dY * dY + dZ * dZ;
        }

        public float Distance(ref Vector3Int otherVector)
        {
            return (float)Math.Sqrt(DistanceSquared(ref otherVector));
        }

        public float Distance(ref Vector3 otherVector)
        {
            return (float)Math.Sqrt(DistanceSquared(ref otherVector));
        }


        public override bool Equals(object obj)
        {
            if (obj is Vector3Int) return Equals((Vector3Int) obj);
            return false;
        }

        public bool Equals(Vector3Int other)
        {
            return (((X == other.X) && (Y == other.Y)) && (Z == other.Z));
        }

        public static bool operator ==(Vector3Int value1, Vector3Int value2)
        {
            return (((value1.X == value2.X) && (value1.Y == value2.Y)) && (value1.Z == value2.Z));
        }

        public static bool operator !=(Vector3Int value1, Vector3Int value2)
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