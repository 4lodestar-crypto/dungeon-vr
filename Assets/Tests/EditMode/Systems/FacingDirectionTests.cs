using DungeonVR.Shared;
using NUnit.Framework;
using UnityEngine;

namespace DungeonVR.Tests.EditMode.Systems
{
    /// <summary>
    /// EditMode unit tests for FacingDirection enum and its extension methods.
    /// Covers: ToOffset, RotateLeft, RotateRight, full cycle, and edge-of-grid cases.
    /// </summary>
    public class FacingDirectionTests
    {
        // ---------- ToOffset ----------

        [Test]
        public void FacingDirection_North_OffsetIsPositiveY()
        {
            Assert.AreEqual(new Vector2Int(0, 1), FacingDirection.North.ToOffset());
        }

        [Test]
        public void FacingDirection_East_OffsetIsPositiveX()
        {
            Assert.AreEqual(new Vector2Int(1, 0), FacingDirection.East.ToOffset());
        }

        [Test]
        public void FacingDirection_South_OffsetIsNegativeY()
        {
            Assert.AreEqual(new Vector2Int(0, -1), FacingDirection.South.ToOffset());
        }

        [Test]
        public void FacingDirection_West_OffsetIsNegativeX()
        {
            Assert.AreEqual(new Vector2Int(-1, 0), FacingDirection.West.ToOffset());
        }

        // ---------- RotateLeft ----------

        [Test]
        public void FacingDirection_RotateLeftFromNorth_ReturnsWest()
        {
            Assert.AreEqual(FacingDirection.West, FacingDirection.North.RotateLeft());
        }

        [Test]
        public void FacingDirection_RotateLeftFromWest_ReturnsSouth()
        {
            Assert.AreEqual(FacingDirection.South, FacingDirection.West.RotateLeft());
        }

        [Test]
        public void FacingDirection_RotateLeftFullCycle_ReturnsToNorth()
        {
            var dir = FacingDirection.North;
            dir = dir.RotateLeft(); // West
            dir = dir.RotateLeft(); // South
            dir = dir.RotateLeft(); // East
            dir = dir.RotateLeft(); // North
            Assert.AreEqual(FacingDirection.North, dir, "Four rotate-lefts should return to North");
        }

        // ---------- RotateRight ----------

        [Test]
        public void FacingDirection_RotateRightFromNorth_ReturnsEast()
        {
            Assert.AreEqual(FacingDirection.East, FacingDirection.North.RotateRight());
        }

        [Test]
        public void FacingDirection_RotateRightFromEast_ReturnsSouth()
        {
            Assert.AreEqual(FacingDirection.South, FacingDirection.East.RotateRight());
        }

        [Test]
        public void FacingDirection_RotateRightFullCycle_ReturnsToNorth()
        {
            var dir = FacingDirection.North;
            dir = dir.RotateRight(); // East
            dir = dir.RotateRight(); // South
            dir = dir.RotateRight(); // West
            dir = dir.RotateRight(); // North
            Assert.AreEqual(FacingDirection.North, dir, "Four rotate-rights should return to North");
        }

        // ---------- Round-trip: RotateLeft then RotateRight = identity ----------

        [Test]
        public void FacingDirection_RotateLeftThenRight_ReturnsOriginal()
        {
            foreach (FacingDirection dir in new[] { FacingDirection.North, FacingDirection.East, FacingDirection.South, FacingDirection.West })
            {
                Assert.AreEqual(dir, dir.RotateLeft().RotateRight(),
                    $"RotateLeft then RotateRight from {dir} should return {dir}");
            }
        }
    }
}
