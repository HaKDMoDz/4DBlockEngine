namespace _4DMonoEngine.Core.Utils.Vector
{
    public struct Vector2Int
    {
        public int X;
        public int Z;

        public Vector2Int(int x, int z)
        {
            X = x;
            Z = z;
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector2Int) return Equals((Vector2Int) obj);
            return false;
        }

        public bool Equals(Vector2Int other)
        {
            return ((X == other.X) && (Z == other.Z));
        }

        public static bool operator ==(Vector2Int value1, Vector2Int value2)
        {
            return ((value1.X == value2.X) && (value1.Z == value2.Z));
        }

        public static bool operator !=(Vector2Int value1, Vector2Int value2)
        {
            if (value1.X == value2.X) return value1.Z != value2.Z;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("{{X:{0} Z:{1}}}", X, Z);
        }
    }
}