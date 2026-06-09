using UnityEngine;

namespace DungeonVR.Gameplay.Components
{
    /// <summary>
    /// MonoBehaviour that holds the grid's wall data at runtime.
    /// V0: hardcoded 5x5 grid with perimeter walls. V1: loaded from Level/Content data.
    /// V0-EXCEPTION: refactor through Level/Content schema in V1.
    /// </summary>
    public class GridData : MonoBehaviour
    {
        [Header("Grid Dimensions")]
        [SerializeField] private int _width = 5;
        [SerializeField] private int _height = 5;

        /// <summary>
        /// true = blocked (wall), false = walkable (floor).
        /// Indexed as [x, y] where x = column (0..Width-1), y = row (0..Height-1).
        /// </summary>
        private bool[,] _walls;

        public int Width => _width;
        public int Height => _height;

        /// <summary>
        /// Get the wall array. Returns null before Awake.
        /// </summary>
        public bool[,] Walls => _walls;

        private void Awake()
        {
            GeneratePerimeterWalledGrid();
        }

        /// <summary>
        /// Load grid data from a procedurally generated dungeon layout.
        /// Deep-copies the Walls array so the caller can discard its original.
        /// </summary>
        public void LoadFromDungeonData(DungeonVR.Gameplay.Level.DungeonData data)
        {
            _width = data.Width;
            _height = data.Height;

            _walls = new bool[_width, _height];
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _walls[x, y] = data.Walls[x, y];
                }
            }

            Debug.Log($"GridData loaded dungeon: {_width}x{_height}");
        }

        /// <summary>
        /// Generates a grid with walls on all four edges and walkable interior.
        /// </summary>
        public void GeneratePerimeterWalledGrid()
        {
            _walls = new bool[_width, _height];
            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    // Edges are walls
                    _walls[x, y] = x == 0 || x == _width - 1 || y == 0 || y == _height - 1;
                }
            }
        }

        /// <summary>
        /// Set a specific tile as wall or walkable. Used by test fixtures.
        /// </summary>
        public void SetTile(int x, int y, bool isWall)
        {
            if (x >= 0 && x < _width && y >= 0 && y < _height)
            {
                _walls[x, y] = isWall;
            }
        }

        /// <summary>
        /// Check if a tile coordinate is inside the grid bounds.
        /// </summary>
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        /// <summary>
        /// Check if a tile is walkable (in bounds and not a wall).
        /// </summary>
        public bool IsWalkable(int x, int y)
        {
            return IsInBounds(x, y) && !_walls[x, y];
        }
    }
}
