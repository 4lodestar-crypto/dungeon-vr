using DungeonVR.Shared;

namespace DungeonVR.Tests.EditMode.Fixtures
{
    /// <summary>
    /// Static factory methods for creating test grid states and game states.
    /// All grids use the same coordinate system: (x,z) where x is column, z is row.
    /// </summary>
    public static class TestGridBuilder
    {
        /// <summary>
        /// Adapter that wraps GridData to implement IGridQueryService for EditMode tests.
        /// </summary>
        public class GridQueryAdapter : DungeonVR.Shared.Interfaces.IGridQueryService
        {
            private readonly GridData _grid;
            public GridQueryAdapter(GridData grid) { _grid = grid; }
            public bool IsWalkable(int x, int z) => _grid.IsWalkable(x, z);
            public UnityEngine.Vector3 GetTileCenter(int x, int z)
                => new UnityEngine.Vector3(x * GameConstants.TILE_SIZE + GameConstants.TILE_SIZE * 0.5f, 0, z * GameConstants.TILE_SIZE + GameConstants.TILE_SIZE * 0.5f);
            public int Width => _grid.Width;
            public int Depth => _grid.Depth;
        }

        /// <summary>
        /// Creates a 5x5 grid with walls on the perimeter (row 0, row 4, col 0, col 4)
        /// and an open 3x3 interior: (1,1) through (3,3) are walkable.
        /// </summary>
        public static GridData CreateFiveByFiveGrid()
        {
            int width = 5;
            int depth = 5;
            bool[,] walls = new bool[width, depth];

            // Perimeter walls
            for (int x = 0; x < width; x++)
            {
                walls[x, 0] = true;           // z=0 (south edge)
                walls[x, depth - 1] = true;   // z=4 (north edge)
            }
            for (int z = 0; z < depth; z++)
            {
                walls[0, z] = true;           // x=0 (west edge)
                walls[width - 1, z] = true;   // x=4 (east edge)
            }

            return new GridData(width, depth, walls);
        }

        /// <summary>
        /// Creates a 3x3 grid with no walls — all tiles walkable.
        /// </summary>
        public static GridData CreateThreeByThreeGrid()
        {
            int size = 3;
            bool[,] walls = new bool[size, size];
            // All false — fully open
            return new GridData(size, size, walls);
        }

        /// <summary>
        /// Creates an irregular 5x5 grid for edge-case testing.
        /// Layout (W=wall, .=open):
        /// W W W W W
        /// W . . . W
        /// W . W . W
        /// W . . . W
        /// W W W W W
        /// </summary>
        public static GridData CreateIrregularGrid()
        {
            int width = 5;
            int depth = 5;
            bool[,] walls = new bool[width, depth];

            // Perimeter walls
            for (int x = 0; x < width; x++)
            {
                walls[x, 0] = true;
                walls[x, depth - 1] = true;
            }
            for (int z = 0; z < depth; z++)
            {
                walls[0, z] = true;
                walls[width - 1, z] = true;
            }

            // Interior wall at (2,2) — center of the open area
            walls[2, 2] = true;

            return new GridData(width, depth, walls);
        }

        /// <summary>
        /// Creates a default GameState with:
        /// - 5x5 perimeter-walled grid
        /// - Champion at grid position (2,2) facing South (FacingIndex=2)
        /// - CurrentTick = 0
        /// </summary>
        public static GameState CreateDefaultGameState()
        {
            return new GameState
            {
                CurrentTick = 0,
                Champion = new ChampionState(gridX: 2, gridZ: 2, facingIndex: 2), // South
                Grid = CreateFiveByFiveGrid()
            };
        }
    }
}
