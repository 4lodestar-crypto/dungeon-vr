using DungeonVR.Shared.Enums;
using UnityEngine;

namespace DungeonVR.Shared.Data
{
    /// <summary>
    /// Describes a single tile in the dungeon grid.
    /// V1 target for JSON deserialization from level data files.
    /// Schema version 1.
    /// </summary>
    [System.Serializable]
    public struct TileData
    {
        /// <summary>Grid column index (3m pitch).</summary>
        public int X;

        /// <summary>Grid row index (3m pitch).</summary>
        public int Z;

        /// <summary>Tile classification.</summary>
        public TileType Type;

        /// <summary>Which edges have wall geometry (interior walls, door frames).</summary>
        public WallFace WallFaces;

        /// <summary>Y-offset for elevation changes (stairs, raised platforms — V2+).</summary>
        public float FloorHeight;

        /// <summary>Arbitrary tags for gameplay systems (e.g. "locked", "hidden", "secret").</summary>
        public string[] Tags;

        /// <summary>JSON blob for extensible per-tile data (e.g. spawn config, item ID, trigger params).</summary>
        public string Metadata;

        public TileData(int x, int z, TileType type, WallFace wallFaces = WallFace.None, float floorHeight = 0f)
        {
            X = x;
            Z = z;
            Type = type;
            WallFaces = wallFaces;
            FloorHeight = floorHeight;
            Tags = null;
            Metadata = null;
        }

        /// <summary>World-space center of this tile.</summary>
        public Vector3 WorldCenter => new Vector3(
            X * GameConstants.TILE_SIZE + GameConstants.TILE_SIZE * 0.5f,
            FloorHeight,
            Z * GameConstants.TILE_SIZE + GameConstants.TILE_SIZE * 0.5f
        );

        public override string ToString()
            => $"Tile ({X},{Z}) {Type} [{WallFaces}]";
    }
}
