using UnityEngine;
using DungeonVR.Server;
using DungeonVR.Shared;
using DungeonVR.Shared.Interfaces;
using DungeonVR.Gameplay.Logic;

namespace DungeonVR.Gameplay.Components
{
    /// <summary>
    /// MonoBehaviour that creates the 5x5 test grid at runtime using Unity primitives.
    /// Sets up the GameState, GameServer, MovementHandler, GameLoopController and InputQueueBridge.
    /// </summary>
    public class GridBuilder : MonoBehaviour, IGridQueryService
    {
        [Header("Grid Dimensions")]
        [SerializeField] private int _gridWidth = 5;
        [SerializeField] private int _gridDepth = 5;

        [Header("Prefab References (null = use primitives)")]
        [SerializeField] private Material _wallMaterial;
        [SerializeField] private Material _floorMaterial;
        [SerializeField] private Material _championMaterial;

        private GameServer _gameServer;
        private GameLoopController _gameLoop;
        private InputQueueBridge _inputBridge;
        private GridData _gridData;
        private ChampionState _championState;

        // IGridQueryService implementation
        public int Width => _gridData.Width;
        public int Depth => _gridData.Depth;

        private void Awake()
        {
            BuildGrid();
            BuildChampion();
            BuildSystems();
        }

        /// <summary>
        /// Creates the 5x5 grid: walls around the perimeter, open floor inside,
        /// and builds the GridData with wall flags.
        /// </summary>
        private void BuildGrid()
        {
            bool[,] walls = new bool[_gridWidth, _gridDepth];

            // Outer walls: set perimeter tiles as walls
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int z = 0; z < _gridDepth; z++)
                {
                    if (x == 0 || x == _gridWidth - 1 || z == 0 || z == _gridDepth - 1)
                    {
                        walls[x, z] = true;
                    }
                    else
                    {
                        walls[x, z] = false;
                    }
                }
            }

            _gridData = new GridData(_gridWidth, _gridDepth, walls);

            // Create a parent object for grid visuals
            GameObject gridRoot = new GameObject("Grid_Root");
            gridRoot.transform.SetParent(transform);

            // Build walls (cubes) at perimeter tiles
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int z = 0; z < _gridDepth; z++)
                {
                    if (!walls[x, z])
                    {
                        // Interior tile: place a floor plane
                        CreateFloorTile(x, z, gridRoot.transform);
                    }
                    else
                    {
                        // Wall tile: place a wall cube
                        CreateWall(x, z, gridRoot.transform);
                    }
                }
            }
        }

        /// <summary>
        /// Creates a floor plane at the given grid position.
        /// </summary>
        private void CreateFloorTile(int gridX, int gridZ, Transform parent)
        {
            Vector3 position = GetTileCenter(gridX, gridZ);
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = $"Floor_{gridX}_{gridZ}";
            floor.transform.SetParent(parent);
            floor.transform.position = position;
            floor.transform.localScale = Vector3.one * (GameConstants.TILE_SIZE / 10f); // Plane is 10x10 units default

            if (_floorMaterial != null)
            {
                floor.GetComponent<Renderer>().material = _floorMaterial;
            }
        }

        /// <summary>
        /// Creates a wall cube at the given grid position.
        /// </summary>
        private void CreateWall(int gridX, int gridZ, Transform parent)
        {
            Vector3 position = GetTileCenter(gridX, gridZ);
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = $"Wall_{gridX}_{gridZ}";
            wall.transform.SetParent(parent);
            wall.transform.position = position;
            wall.transform.localScale = new Vector3(
                GameConstants.TILE_SIZE,
                GameConstants.TILE_SIZE,
                GameConstants.TILE_SIZE
            );

            if (_wallMaterial != null)
            {
                wall.GetComponent<Renderer>().material = _wallMaterial;
            }
        }

        /// <summary>
        /// Creates the champion capsule at the starting position (2,2) facing South (index 2).
        /// </summary>
        private void BuildChampion()
        {
            int startX = 2;
            int startZ = 2;
            int startFacing = 2; // South

            _championState = new ChampionState(startX, startZ, startFacing);

            Vector3 position = _championState.WorldPosition;
            position.y = GameConstants.EYE_HEIGHT;

            GameObject champion = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            champion.name = "Champion";
            champion.transform.SetParent(transform);
            champion.transform.position = position;
            champion.transform.localScale = Vector3.one * 0.5f;

            if (_championMaterial != null)
            {
                champion.GetComponent<Renderer>().material = _championMaterial;
            }
        }

        /// <summary>
        /// Creates the GameServer, MovementHandler, and wires up GameLoopController + InputQueueBridge.
        /// </summary>
        private void BuildSystems()
        {
            // Create the movement handler with this GridBuilder as the IGridQueryService
            var movementHandler = new MovementHandler(this);

            // Create initial GameState
            var initialState = new GameState
            {
                CurrentTick = 0,
                Champion = _championState,
                Grid = _gridData
            };

            // Create the GameServer
            _gameServer = new GameServer(initialState, movementHandler);

            // Find or create GameLoopController
            _gameLoop = GetComponent<GameLoopController>();
            if (_gameLoop == null)
            {
                _gameLoop = gameObject.AddComponent<GameLoopController>();
            }
            _gameLoop.Initialize(_gameServer);

            // Find or create InputQueueBridge
            _inputBridge = GetComponent<InputQueueBridge>();
            if (_inputBridge == null)
            {
                _inputBridge = gameObject.AddComponent<InputQueueBridge>();
            }
            _inputBridge.Initialize(_gameServer);
        }

        // Public accessor for PlayerCameraController and other components to access GameServer
        public Server.GameServer GameServer => _gameServer;

        // --- IGridQueryService Implementation ---

        public bool IsWalkable(int gridX, int gridZ)
        {
            return _gridData.IsWalkable(gridX, gridZ);
        }

        public Vector3 GetTileCenter(int gridX, int gridZ)
        {
            float x = gridX * GameConstants.TILE_SIZE;
            float z = gridZ * GameConstants.TILE_SIZE;
            return new Vector3(x, 0f, z);
        }
    }
}
