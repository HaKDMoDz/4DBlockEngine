

using System;

namespace _4DMonoEngine.Core.Common.Vector
{
    public struct Vector4Byte
    {
        public byte X;
        public byte Y;
        public byte Z;
        public byte W;

        public Vector4Byte(byte x, byte y, byte z, byte w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public Vector4Byte(byte x, byte y, byte z)
        {
            X = x;
            Y = y;
            Z = z;
            W = 1;
        }

        public Vector4Byte(int x, int y, int z)
        {
            X = (byte) x;
            Y = (byte) y;
            Z = (byte) z;
            W = 1;
        }

        public Vector4Byte(byte value)
        {
            X = Y = Z = value;
            W = 1;
        }

        #region Operators

        public static bool operator ==(Vector4Byte left, Vector4Byte right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector4Byte left, Vector4Byte right)
        {
            return !left.Equals(right);
        }

        public static Vector4Byte operator +(Vector4Byte a, Vector4Byte b)
        {
            Vector4Byte result;

            result.X = (byte) (a.X + b.X);
            result.Y = (byte) (a.Y + b.Y);
            result.Z = (byte) (a.Z + b.Z);
            result.W = 1;

            return result;
        }

        public static Vector4Byte operator -(Vector4Byte a, Vector4Byte b)
        {
            Vector4Byte result;

            result.X = (byte) (a.X - b.X);
            result.Y = (byte) (a.Y - b.Y);
            result.Z = (byte) (a.Z - b.Z);
            result.W = 1;

            return result;
        }

        public static Vector4Byte operator *(Vector4Byte a, Vector4Byte b)
        {
            Vector4Byte result;

            result.X = (byte) (a.X*b.X);
            result.Y = (byte) (a.Y*b.Y);
            result.Z = (byte) (a.Z*b.Z);
            result.W = 1;

            return result;
        }

        public static Vector4Byte operator /(Vector4Byte a, Vector4Byte b)
        {
            Vector4Byte result;

            result.X = (byte) (a.X/b.X);
            result.Y = (byte) (a.Y/b.Y);
            result.Z = (byte) (a.Z/b.Z);
            result.W = 1;

            return result;
        }

        #endregion

        public bool Equals(Vector4Byte other)
        {
            return (((X == other.X) && (Y == other.Y)) &&
                    (Z == other.Z) && (W == other.W));
        }

        public override bool Equals(object obj)
        {
            bool flag = false;

            if (obj is Vector4Byte)
            {
                flag = Equals((Vector4Byte) obj);
            }

            return flag;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return X.GetHashCode() + Y.GetHashCode() +
                       Z.GetHashCode() + W.GetHashCode();
            }
        }

        public override string ToString()
        {
            return String.Format("{{X:{0} Y:{1} Z:{2} W:{3}}}", X, Y, Z, W);
        }
    }
}