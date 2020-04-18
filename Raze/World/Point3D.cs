
using Microsoft.Xna.Framework;

namespace Raze.World
{
    public struct Point3D
    {
        public static Point3D Zero { get { return zero; } }
        private static readonly Point3D zero = new Point3D(0, 0, 0); // Not actually sure if this boosts performance in any way. Probably not.

        public int X, Y, Z;

        public Point3D(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            switch (obj)
            {
                case Point3D point:
                    return this == point;

                case Vector3 vector:
                    return this == vector;

                default:
                    return false;
            }
        }

        public override int GetHashCode()
        {
            // ReSharper disable NonReadonlyMemberInGetHashCode
            return (X * 397) ^ (Y * 131) ^ Z;
            // ReSharper restore NonReadonlyMemberInGetHashCode
        }

        public static bool operator ==(Point3D self, Point3D other)
        {
            return self.X == other.X && self.Y == other.Y && self.Z == other.Z;
        }

        public static bool operator !=(Point3D self, Point3D other)
        {
            return !(self == other);
        }

        public static Point3D operator +(Point3D self, Point3D other)
        {
            return new Point3D(self.X + other.X, self.Y + other.Y, self.Z + other.Z);
        }

        public static Point3D operator -(Point3D self, Point3D other)
        {
            return new Point3D(self.X - other.X, self.Y - other.Y, self.Z - other.Z);
        }

        public static implicit operator Vector3(Point3D point)
        {
            return new Vector3(point.X, point.Y, point.Z);
        }
    }
}