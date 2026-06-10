using UnityEngine;
using DungeonVR.Gameplay.Components;
using DungeonVR.Gameplay.Level;
using DungeonVR.Server;

namespace DungeonVR.Level
{
    /// <summary>
    /// MonoBehaviour that generates a dungeon layout via RandomRoomGenerator,
    /// loads it into GridData, and spawns placeholder primitive cubes for walls/floors.
    /// V0-EXCEPTION: placeholder visuals using primitive cubes — replace with
    /// proper prefabs / tilemap rendering in V1.
    /// </summary>
    public class DungeonGridSpawner : MonoBehaviour
    {
        [Header("Generation")]
        [SerializeField] private int _seed = 42;

        [Header("Grid Data")]
        [SerializeField] private GridData _gridData;

        [Header("Visuals")]
        [SerializeField] private Material _floorMaterial;
        [SerializeField] private Material _wallMaterial;

        [Header("Tick System")]
        [SerializeField] private GameTick _gameTick;

        /// <summary>Tile pitch constant matching MovementHandler.TILE_SIZE.</summary>
        private const float TILE_SIZE = 3.0f;

        /// <summary>World-space Y for the top of a floor tile (below the walkable plane).</summary>
        private const float FLOOR_Y = -1.5f;

        /// <summary>Wall cube half-extent — cubes are 3.0³.</summary>
        private const float WALL_SIZE = 3.0f;

        /// <summary>Floor cube dimensions: flat slab.</summary>
        private static readonly Vector3 FLOOR_SCALE = new Vector3(3.0f, 0.3f, 3.0f);

        /// <summary>Cached start position from the generated dungeon, applied in Start().</summary>
        private Vector2Int _startPosition;

        private void Awake()
        {
            // --- 1. Create and run the generator ---
            RandomRoomGenerator generator = new RandomRoomGenerator(
                minRoomSize: 5,
                maxRoomSize: 10,
                corridorWidth: 1,
                roomCount: 4,
                minGridWidth: 15,
                minGridHeight: 15
            );

            DungeonData data = generator.Generate(_seed);
            Debug.Log($"[DungeonGridSpawner] Generated dungeon {data.Width}x{data.Height} with start at ({data.StartPosition.x},{data.StartPosition.y})");
            _startPosition = data.StartPosition;

            // --- 2. Load data into GridData ---
            if (_gridData != null)
            {
                _gridData.LoadFromDungeonData(data);
            }
            else
            {
                Debug.LogWarning("[DungeonGridSpawner] _gridData is null — dungeon data not loaded into GridData.");
                return;
            }

            // --- 3. Spawn visual tiles ---
            Transform dungeonRoot = SpawnTiles(data);

            Debug.Log($"[DungeonGridSpawner] Spawn complete — {dungeonRoot.childCount} tiles under '{dungeonRoot.name}'.");
        }

        /// <summary>
        /// Unity Start: applies the dungeon's start position to GameTick.
        /// Runs after all Awake() calls are complete, guaranteeing GameTick.Champion is initialised.
        /// </summary>
        private void Start()
        {
            if (_gameTick != null)
            {
                _gameTick.SetStartPosition(_startPosition);
            }
        }

        /// <summary>
        /// Spawn primitive-cube tiles for every cell in the dungeon grid.
        /// Wall tiles get tall cubes with colliders; floor tiles get flat slabs without colliders.
        /// All tiles are parented under a "Dungeon" GameObject for hierarchy cleanliness.
        /// </summary>
        /// <returns>The root Transform of the spawned dungeon.</returns>
        private Transform SpawnTiles(DungeonData data)
        {
            GameObject dungeonGO = new GameObject("Dungeon");
            dungeonGO.transform.SetParent(transform, worldPositionStays: false);

            int wallCount = 0;
            int floorCount = 0;

            for (int x = 0; x < data.Width; x++)
            {
                for (int y = 0; y < data.Height; y++)
                {
                    Vector3 worldPos = new Vector3(
                        x * TILE_SIZE,
                        data.Walls[x, y] ? 0f : FLOOR_Y,
                        y * TILE_SIZE
                    );

                    if (data.Walls[x, y])
                    {
                        // --- Wall tile: tall cube with collider ---
                        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        wall.name = $"Wall_{x}_{y}";
                        wall.transform.SetParent(dungeonGO.transform, worldPositionStays: false);
                        wall.transform.localPosition = worldPos;
                        wall.transform.localScale = Vector3.one * WALL_SIZE;
                        wall.tag = "Wall";

                        // CreatePrimitive already adds a BoxCollider — keep it.

                        if (_wallMaterial != null)
                        {
                            wall.GetComponent<Renderer>().material = _wallMaterial;
                        }

                        wallCount++;
                    }
                    else
                    {
                        // --- Floor tile: flat slab without collider ---
                        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        floor.name = $"Floor_{x}_{y}";
                        floor.transform.SetParent(dungeonGO.transform, worldPositionStays: false);
                        floor.transform.localPosition = worldPos;
                        floor.transform.localScale = FLOOR_SCALE;

                        // Remove collider so the player walks through floor tiles
                        DestroyImmediate(floor.GetComponent<Collider>());

                        if (_floorMaterial != null)
                        {
                            floor.GetComponent<Renderer>().material = _floorMaterial;
                        }

                        floorCount++;
                    }
                }
            }

            Debug.Log($"[DungeonGridSpawner] Spawned {wallCount} walls, {floorCount} floor tiles.");
            return dungeonGO.transform;
        }
    }
}
