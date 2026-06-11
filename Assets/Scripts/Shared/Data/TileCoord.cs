namespace DungeonVR.Shared.Data
{
    /// <summary>
    /// Represents a tile coordinate in the dungeon grid.
    /// (0,0) is the origin tile at world (0,0,0).
    /// X is right, Z is forward, Y is up.
    /// </summary>
    [System.Serializable]
    public struct TileCoord
    {
        public int X;
        public int Z;

        public TileCoord(int x, int z)
        {
            X = x;
            Z = z;
        }

        public override bool Equals(object obj)
        {
            if (obj is TileCoord other)
                return X == other.X && Z == other.Z;
            return false;
        }

        public override int GetHashCode()
        {
            // Simple hash combining X and Z
            return (X << 16) ^ Z;
        }

        public static bool operator ==(TileCoord a, TileCoord b)
        {
            return a.X == b.X && a.Z == b.Z;
        }

        public static bool operator !=(TileCoord a, TileCoord b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return $"({X}, {Z})";
        }
    }
}
