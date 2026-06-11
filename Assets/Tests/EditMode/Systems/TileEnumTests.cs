using DungeonVR.Shared.Data;
using DungeonVR.Shared.Enums;
using NUnit.Framework;
using UnityEngine;

namespace DungeonVR.Tests.EditMode.Systems
{
    /// <summary>
    /// EditMode tests for shared enum and data type contracts.
    /// Verifies TileType, WallFace, and TileData structural invariants.
    /// Deterministic — no Random, timing, or frame-rate dependencies.
    /// </summary>
    [TestFixture]
    public class TileEnumTests
    {
        /// <summary>
        /// TileType must have exactly 8 entries: Floor, Wall, Door, Trap, Altar, Spawn, Stairs, Empty.
        /// This count check prevents accidentally adding new values without updating consumers
        /// (palette, validator, pathfinder, etc.).
        /// </summary>
        [Test]
        public void TileType_Values_EnumeratorHas8Entries()
        {
            // Arrange & Act
            int count = System.Enum.GetValues(typeof(TileType)).Length;

            // Assert
            Assert.AreEqual(8, count,
                "TileType must have exactly 8 entries (Floor, Wall, Door, Trap, Altar, Spawn, Stairs, Empty). " +
                "Adding a new value requires updating TilePalette, LevelValidator, pathfinding, and visual systems.");
        }

        /// <summary>
        /// WallFace.None must be integer 0 for bitmask operations to work correctly.
        /// </summary>
        [Test]
        public void WallFace_None_IsZero()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(0, (int)WallFace.None,
                "WallFace.None must be 0 so it serves as a valid 'no walls' default in bitmask operations.");
        }

        /// <summary>
        /// WallFace flags must combine correctly via bitwise OR.
        /// North|South == 0b0011 (decimal 3).
        /// All must have all 4 direction bits set.
        /// </summary>
        [Test]
        public void WallFace_Flags_CombineCorrectly()
        {
            // Arrange & Act
            int northSouth = (int)(WallFace.North | WallFace.South);
            int allFour = (int)WallFace.All;

            // Assert — individual values
            Assert.AreEqual(1, (int)WallFace.North, "North = 1 << 0 = 1");
            Assert.AreEqual(2, (int)WallFace.South, "South = 1 << 1 = 2");
            Assert.AreEqual(4, (int)WallFace.East, "East = 1 << 2 = 4");
            Assert.AreEqual(8, (int)WallFace.West, "West = 1 << 3 = 8");

            // Assert — combined
            Assert.AreEqual(3, northSouth, "North|South = 0b0011 = 3");

            // Assert — All has all 4 bits set (bits 0-3)
            Assert.AreEqual(15, allFour, "All = North|South|East|West = 1+2+4+8 = 15 (0b1111)");

            // Assert — All contains every direction
            Assert.AreEqual(WallFace.All, WallFace.North | WallFace.South | WallFace.East | WallFace.West,
                "WallFace.All must be the logical OR of all four direction flags.");
        }

        /// <summary>
        /// WallFace should be decorated with the [Flags] attribute.
        /// </summary>
        [Test]
        public void WallFace_HasFlagsAttribute()
        {
            // Arrange & Act
            bool hasFlags = System.Attribute.IsDefined(typeof(WallFace), typeof(System.FlagsAttribute));

            // Assert
            Assert.IsTrue(hasFlags, "WallFace must have the [Flags] attribute for bitmask operations.");
        }

        /// <summary>
        /// TileData(1, 1, Floor) must compute WorldCenter = (4.5, 0, 4.5).
        /// Formula: (X * TILE_SIZE + TILE_SIZE * 0.5f, FloorHeight, Z * TILE_SIZE + TILE_SIZE * 0.5f)
        /// GameConstants.TILE_SIZE = 3.0f
        /// </summary>
        [Test]
        public void TileData_WorldCenter_ReturnsCorrectPosition()
        {
            // Arrange
            var tile = new TileData(1, 1, TileType.Floor);

            // Act
            Vector3 center = tile.WorldCenter;

            // Assert
            // X: 1 * 3.0 + 1.5 = 4.5
            // Z: 1 * 3.0 + 1.5 = 4.5
            // Y: FloorHeight = 0
            Assert.AreEqual(4.5f, center.x, 1e-6f, "WorldCenter.x should be X * TILE_SIZE + half-tile");
            Assert.AreEqual(0f, center.y, 1e-6f, "WorldCenter.y should equal FloorHeight (default 0)");
            Assert.AreEqual(4.5f, center.z, 1e-6f, "WorldCenter.z should be Z * TILE_SIZE + half-tile");
        }

        /// <summary>
        /// TileData(0, 0, Floor) must compute WorldCenter = (1.5, 0, 1.5) at the origin tile.
        /// </summary>
        [Test]
        public void TileData_WorldCenter_OriginTileIsCorrect()
        {
            // Arrange
            var tile = new TileData(0, 0, TileType.Floor);

            // Act
            Vector3 center = tile.WorldCenter;

            // Assert
            Assert.AreEqual(1.5f, center.x, 1e-6f, "Origin tile WorldCenter.x should be half-tile");
            Assert.AreEqual(0f, center.y, 1e-6f, "Origin tile WorldCenter.y should be 0");
            Assert.AreEqual(1.5f, center.z, 1e-6f, "Origin tile WorldCenter.z should be half-tile");
        }

        /// <summary>
        /// TileData with a non-zero FloorHeight must reflect that in WorldCenter.y.
        /// </summary>
        [Test]
        public void TileData_WorldCenter_RespectsFloorHeight()
        {
            // Arrange
            var tile = new TileData(2, 3, TileType.Stairs, WallFace.None, floorHeight: 1.5f);

            // Act
            Vector3 center = tile.WorldCenter;

            // Assert
            // X: 2 * 3.0 + 1.5 = 7.5
            // Z: 3 * 3.0 + 1.5 = 10.5
            // Y: 1.5
            Assert.AreEqual(7.5f, center.x, 1e-6f);
            Assert.AreEqual(1.5f, center.y, 1e-6f, "WorldCenter.y should equal FloorHeight when non-zero");
            Assert.AreEqual(10.5f, center.z, 1e-6f);
        }

        /// <summary>
        /// TileData constructor should set Tags and Metadata to null by default.
        /// </summary>
        [Test]
        public void TileData_DefaultFields_AreNull()
        {
            // Arrange & Act
            var tile = new TileData(0, 0, TileType.Floor);

            // Assert
            Assert.IsNull(tile.Tags, "Tags should default to null");
            Assert.IsNull(tile.Metadata, "Metadata should default to null");
            Assert.AreEqual(WallFace.None, tile.WallFaces, "WallFaces should default to None");
            Assert.AreEqual(0f, tile.FloorHeight, 1e-6f, "FloorHeight should default to 0");
        }

        /// <summary>
        /// TileData ToString should produce the expected format.
        /// </summary>
        [Test]
        public void TileData_ToString_FormatMatchesExpected()
        {
            // Arrange
            var tile = new TileData(3, 7, TileType.Door, WallFace.North | WallFace.South);

            // Act
            string str = tile.ToString();

            // Assert
            Assert.AreEqual("Tile (3,7) Door [North, South]", str);
        }
    }
}
