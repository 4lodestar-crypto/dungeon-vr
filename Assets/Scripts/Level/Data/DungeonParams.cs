using UnityEngine;

namespace DungeonVR.Level.Data
{
    /// <summary>
    /// Configuration parameters for procedural dungeon generation.
    /// Can be used as a ScriptableObject asset (via CreateAssetMenu)
    /// or constructed in code via ScriptableObject.CreateInstance.
    /// </summary>
    [CreateAssetMenu(menuName = "DungeonVR/Dungeon Params")]
    public class DungeonParams : ScriptableObject
    {
        [Header("Grid Dimensions")]
        [SerializeField, Tooltip("Random seed for reproducible generation.")]
        private int _seed = 42;

        [SerializeField, Tooltip("Grid width in tiles.")]
        private int _width = 32;

        [SerializeField, Tooltip("Grid depth (length) in tiles.")]
        private int _depth = 32;

        [Header("Room Generation")]
        [SerializeField, Tooltip("Minimum number of rooms to generate.")]
        private int _minRoomCount = 3;

        [SerializeField, Tooltip("Maximum number of rooms to generate.")]
        private int _maxRoomCount = 8;

        [SerializeField, Tooltip("Minimum room width/height in tiles.")]
        private int _minRoomSize = 4;

        [SerializeField, Tooltip("Maximum room width/height in tiles.")]
        private int _maxRoomSize = 8;

        [Header("Corridors")]
        [SerializeField, Tooltip("Width of corridors in tiles (1 = single tile).")]
        private int _corridorWidth = 1;

        [Header("Walls")]
        [SerializeField, Tooltip("If true, place wall tiles around each room perimeter.")]
        private bool _placeWallsAroundRooms = true;

        /// <summary>Random seed for reproducible generation.</summary>
        public int Seed
        {
            get => _seed;
            set => _seed = value;
        }

        /// <summary>Grid width in tiles.</summary>
        public int Width
        {
            get => _width;
            set => _width = value;
        }

        /// <summary>Grid depth (length) in tiles.</summary>
        public int Depth
        {
            get => _depth;
            set => _depth = value;
        }

        /// <summary>Minimum number of rooms to generate.</summary>
        public int MinRoomCount
        {
            get => _minRoomCount;
            set => _minRoomCount = value;
        }

        /// <summary>Maximum number of rooms to generate.</summary>
        public int MaxRoomCount
        {
            get => _maxRoomCount;
            set => _maxRoomCount = value;
        }

        /// <summary>Minimum room width/height in tiles.</summary>
        public int MinRoomSize
        {
            get => _minRoomSize;
            set => _minRoomSize = value;
        }

        /// <summary>Maximum room width/height in tiles.</summary>
        public int MaxRoomSize
        {
            get => _maxRoomSize;
            set => _maxRoomSize = value;
        }

        /// <summary>Width of corridors in tiles.</summary>
        public int CorridorWidth
        {
            get => _corridorWidth;
            set => _corridorWidth = value;
        }

        /// <summary>If true, place wall tiles around each room perimeter.</summary>
        public bool PlaceWallsAroundRooms
        {
            get => _placeWallsAroundRooms;
            set => _placeWallsAroundRooms = value;
        }

        /// <summary>
        /// Clamps all parameter values to sensible ranges.
        /// Returns self for chaining.
        /// </summary>
        public DungeonParams Clamp()
        {
            _width = Mathf.Max(10, _width);
            _depth = Mathf.Max(10, _depth);
            _minRoomCount = Mathf.Max(1, _minRoomCount);
            _maxRoomCount = Mathf.Max(_minRoomCount, _maxRoomCount);
            _minRoomSize = Mathf.Max(3, _minRoomSize);
            _maxRoomSize = Mathf.Max(_minRoomSize, _maxRoomSize);
            _maxRoomSize = Mathf.Min(_maxRoomSize, _width / 2, _depth / 2);
            _corridorWidth = Mathf.Max(1, _corridorWidth);
            return this;
        }

        /// <summary>
        /// Create a default DungeonParams instance for code-based usage.
        /// Equivalent to CreateInstance&lt;DungeonParams&gt;() with field defaults.
        /// </summary>
        public static DungeonParams CreateDefault()
        {
            return CreateInstance<DungeonParams>();
        }

        private void OnValidate()
        {
            Clamp();
        }
    }
}
