using DungeonVR.Gameplay.Logic;
using NUnit.Framework;
using UnityEngine;

namespace DungeonVR.Tests.EditMode.Systems
{
    /// <summary>
    /// EditMode unit tests for MovementHandler coordinate conversion helpers
    /// (WorldToTile / TileToWorld) and the TILE_SIZE constant.
    /// </summary>
    public class MovementHandlerConversionTests
    {
        [Test]
        public void MovementHandler_TileSize_Is3Metres()
        {
            Assert.AreEqual(3.0f, MovementHandler.TILE_SIZE, 0.001f);
        }

        // ---------- TileToWorld ----------

        [Test]
        public void TileToWorld_Origin_ReturnsZero()
        {
            Vector3 world = MovementHandler.TileToWorld(Vector2Int.zero);
            Assert.AreEqual(Vector3.zero, world);
        }

        [Test]
        public void TileToWorld_OneOne_Returns3_0_3()
        {
            Vector3 world = MovementHandler.TileToWorld(new Vector2Int(1, 1));
            Assert.AreEqual(new Vector3(3f, 0f, 3f), world);
        }

        [Test]
        public void TileToWorld_NegativeTile_ReturnsNegativeWorld()
        {
            Vector3 world = MovementHandler.TileToWorld(new Vector2Int(-2, -3));
            Assert.AreEqual(new Vector3(-6f, 0f, -9f), world);
        }

        [Test]
        public void TileToWorld_NonZeroYIsZero()
        {
            // The z component of Vector3 stores tile.y, and y (height) is always 0
            Vector3 world = MovementHandler.TileToWorld(new Vector2Int(4, 5));
            Assert.AreEqual(0f, world.y, 0.001f, "Height/y should always be 0");
        }

        // ---------- WorldToTile ----------

        [Test]
        public void WorldToTile_Origin_ReturnsZero()
        {
            Assert.AreEqual(Vector2Int.zero, MovementHandler.WorldToTile(Vector3.zero));
        }

        [Test]
        public void WorldToTile_3_0_3_ReturnsOneOne()
        {
            Assert.AreEqual(new Vector2Int(1, 1), MovementHandler.WorldToTile(new Vector3(3f, 0f, 3f)));
        }

        [Test]
        public void WorldToTile_NegativeWorld_ReturnsNegativeTile()
        {
            Assert.AreEqual(new Vector2Int(-2, -3), MovementHandler.WorldToTile(new Vector3(-5.5f, 0f, -8.2f)));
        }

        [Test]
        public void WorldToTile_NearTileEdge_RoundsToNearest()
        {
            // 4.9 / 3 ≈ 1.633 → rounds to 2
            Assert.AreEqual(new Vector2Int(2, 2), MovementHandler.WorldToTile(new Vector3(4.9f, 2f, 4.9f)));
        }

        // ---------- Round-trip ----------

        [Test]
        public void WorldToTile_TileToWorld_RoundTripPreservesIntTile()
        {
            var original = new Vector2Int(3, -2);
            Vector3 world = MovementHandler.TileToWorld(original);
            Vector2Int roundTripped = MovementHandler.WorldToTile(world);
            Assert.AreEqual(original, roundTripped,
                "TileToWorld then WorldToTile should return the same tile for integer coordinates");
        }

        [Test]
        public void TileToWorld_WorldToTile_RoundTripPreservesTileCentre()
        {
            var originalTile = new Vector2Int(2, 3);
            Vector3 world = MovementHandler.TileToWorld(originalTile);
            Vector2Int result = MovementHandler.WorldToTile(world);
            Assert.AreEqual(originalTile, result,
                "Tile centre world position should map back to the same tile");
        }

        // ---------- Zero Y is ignored ----------

        [Test]
        public void WorldToTile_DifferentY_IgnoresHeight()
        {
            // Y (height) should be ignored — only x and z matter
            var withY = MovementHandler.WorldToTile(new Vector3(3f, 100f, 3f));
            var withoutY = MovementHandler.WorldToTile(new Vector3(3f, 0f, 3f));
            Assert.AreEqual(withoutY, withY, "WorldToTile should ignore the Y (height) component");
        }
    }
}
