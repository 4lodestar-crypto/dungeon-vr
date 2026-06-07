using DungeonVR.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace DungeonVR.Tests.EditMode.Systems
{
    /// <summary>
    /// EditMode unit tests for ChampionState pure data class.
    /// Tests: constructor, property access, Clone() deep copy.
    /// </summary>
    public class ChampionStateTests
    {
        [Test]
        public void ChampionState_Constructor_SetsPositionAndFacing()
        {
            var state = new ChampionState(new Vector2Int(3, 1), FacingDirection.East);
            Assert.AreEqual(new Vector2Int(3, 1), state.GridPosition);
            Assert.AreEqual(FacingDirection.East, state.FacingDirection);
        }

        [Test]
        public void ChampionState_DefaultFacing_IsCorrect()
        {
            var state = new ChampionState(new Vector2Int(0, 0), FacingDirection.North);
            Assert.AreEqual(FacingDirection.North, state.FacingDirection);
        }

        [Test]
        public void ChampionState_Clone_ReturnsEqualCopy()
        {
            var original = new ChampionState(new Vector2Int(1, 2), FacingDirection.South);
            var clone = original.Clone();

            Assert.AreEqual(original.GridPosition, clone.GridPosition);
            Assert.AreEqual(original.FacingDirection, clone.FacingDirection);
        }

        [Test]
        public void ChampionState_Clone_IsDeepCopy()
        {
            var original = new ChampionState(new Vector2Int(0, 0), FacingDirection.North);
            var clone = original.Clone();

            // Mutate original — clone should not be affected
            original.GridPosition = new Vector2Int(4, 4);
            original.FacingDirection = FacingDirection.East;

            Assert.AreEqual(new Vector2Int(0, 0), clone.GridPosition,
                "Clone position should be independent of original");
            Assert.AreEqual(FacingDirection.North, clone.FacingDirection,
                "Clone facing should be independent of original");
        }

        [Test]
        public void ChampionState_NegativePosition_Supported()
        {
            var state = new ChampionState(new Vector2Int(-3, -1), FacingDirection.West);
            Assert.AreEqual(new Vector2Int(-3, -1), state.GridPosition);
        }
    }
}
