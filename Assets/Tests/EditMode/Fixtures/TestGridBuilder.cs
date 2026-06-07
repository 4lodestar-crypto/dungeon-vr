using DungeonVR.Gameplay;
using DungeonVR.Shared;
using NUnit.Framework;
using UnityEngine;

namespace DungeonVR.Tests.EditMode.Fixtures
{
    /// <summary>
    /// Shared test fixture for building test grids used by both MovementHandlerTests and integration tests.
    /// Provides static methods to create bool[,] wall arrays and champion states.
    ///
    /// V0-EXCEPTION: once ChampionState and GameTick are in Server namespace (V1),
    /// the champion/tick helper methods below should be updated accordingly.
    /// </summary>
    public static class TestGridBuilder
    {
        /// <summary>Returns a 5x5 bool[,] where edges are walls (true) and interior is walkable (false).</summary>
        public static bool[,] Create5x5PerimeterWalled()
        {
            var grid = new bool[5, 5];
            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 5; y++)
                    grid[x, y] = x == 0 || x == 4 || y == 0 || y == 4;
            return grid;
        }

        /// <summary>Returns a 3x3 bool[,] with no walls (all false).</summary>
        public static bool[,] Create3x3Empty()
        {
            return new bool[3, 3];
        }

        /// <summary>Returns a 5x5 bool[,] with no walls (all false).</summary>
        public static bool[,] Create5x5Empty()
        {
            return new bool[5, 5];
        }

        /// <summary>Returns a ChampionState at position (2,2) facing FacingDirection.North.</summary>
        public static ChampionState CreateDefaultChampion()
        {
            return new ChampionState(new Vector2Int(2, 2), FacingDirection.North);
        }

        /// <summary>Returns a ChampionState at the specified position and facing.</summary>
        public static ChampionState CreateChampionAt(Vector2Int position, FacingDirection facing)
        {
            return new ChampionState(position, facing);
        }
    }
}
