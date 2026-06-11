using UnityEngine;
using DungeonVR.Shared;
using DungeonVR.Shared.Interfaces;

namespace DungeonVR.Level.Components
{
    /// <summary>
    /// V0 hardcoded 5x5 test grid service. Implements IGridQueryService directly.
    /// V0-EXCEPTION: hardcoded grid data; proper level pipeline in V1.
    /// </summary>
    public class TestGridService : MonoBehaviour, IGridQueryService
    {
        [SerializeField, Tooltip("Grid width in tiles.")]
        private int _width = 5;

        [SerializeField, Tooltip("Grid depth in tiles.")]
        private int _depth = 5;

        private bool[,] _walls;

        public int Width => _width;
        public int Depth => _depth;

        private void Awake()
        {
            BuildDefaultGrid();
        }

        /// <summary>
        /// Builds a 5x5 grid with walls on the perimeter and open tiles inside.
        /// </summary>
        private void BuildDefaultGrid()
        {
            _walls = new bool[_width, _depth];

            for (int x = 0; x < _width; x++)
            {
                for (int z = 0; z < _depth; z++)
                {
                    // true = wall (blocked), false = floor (walkable)
                    _walls[x, z] = x == 0 || x == _width - 1 || z == 0 || z == _depth - 1;
                }
            }
        }

        /// <summary>
        /// Returns true if the tile at (gridX, gridZ) is walkable (within bounds and not a wall).
        /// </summary>
        public bool IsWalkable(int gridX, int gridZ)
        {
            if (gridX < 0 || gridX >= _width || gridZ < 0 || gridZ >= _depth)
                return false;

            return !_walls[gridX, gridZ];
        }

        /// <summary>
        /// Returns the world-space center of tile (gridX, gridZ).
        /// Tiles are TILE_SIZE x TILE_SIZE, pivot at center-bottom, Y=0 is floor level.
        /// </summary>
        public Vector3 GetTileCenter(int gridX, int gridZ)
        {
            float halfTile = GameConstants.TILE_SIZE * 0.5f;
            float worldX = gridX * GameConstants.TILE_SIZE + halfTile;
            float worldZ = gridZ * GameConstants.TILE_SIZE + halfTile;
            return new Vector3(worldX, 0f, worldZ);
        }

        /// <summary>
        /// Returns a copy of the current grid layout as a GridData instance.
        /// V0-EXCEPTION: in-memory snapshot; serialized load in V1.
        /// </summary>
        public GridData GetGridData()
        {
            bool[,] wallsCopy = new bool[_width, _depth];
            for (int x = 0; x < _width; x++)
            {
                for (int z = 0; z < _depth; z++)
                {
                    wallsCopy[x, z] = _walls[x, z];
                }
            }

            return new GridData(_width, _depth, wallsCopy);
        }
    }
}
