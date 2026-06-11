using System.Collections.Generic;
using UnityEngine;
using DungeonVR.Shared;
using DungeonVR.Shared.Data;
using DungeonVR.Shared.Enums;
using DungeonVR.Shared.Interfaces;

namespace DungeonVR.Level.Logic
{
    /// <summary>
    /// Runtime grid query service. Implements IGridQueryService.
    /// Built from TileData during level loading.
    /// Zero-allocation in hot paths (IsWalkable, GetTileCenter).
    /// </summary>
    public class GridService : MonoBehaviour, IGridQueryService
    {
        private int _width;
        private int _depth;
        private TileType[,] _tileTypes;
        private bool[,] _blocked; // true = impassable (Wall, Empty)
        private List<TileCoord> _spawnPoints;

        /// <summary>
        /// Grid width in tiles.
        /// </summary>
        public int Width => _width;

        /// <summary>
        /// Grid depth in tiles.
        /// </summary>
        public int Depth => _depth;

        /// <summary>
        /// All registered spawn points on the grid.
        /// </summary>
        public IReadOnlyList<TileCoord> SpawnPoints => _spawnPoints;

        private void Awake()
        {
            // Ensure arrays are initialized to empty defaults
            if (_tileTypes == null)
            {
                _tileTypes = new TileType[0, 0];
                _blocked = new bool[0, 0];
            }

            if (_spawnPoints == null)
            {
                _spawnPoints = new List<TileCoord>(4);
            }
        }

        /// <summary>
        /// Register the grid from a complete TileData array.
        /// Called by LevelLoader after instantiation.
        /// </summary>
        public void RegisterFromTiles(TileData[] tiles, int width, int depth)
        {
            _width = width;
            _depth = depth;
            _tileTypes = new TileType[width, depth];
            _blocked = new bool[width, depth];

            if (_spawnPoints == null)
            {
                _spawnPoints = new List<TileCoord>(4);
            }
            else
            {
                _spawnPoints.Clear();
            }

            // Initialize all tiles as Floor by default
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    _tileTypes[x, z] = TileType.Floor;
                    _blocked[x, z] = false;
                }
            }

            // Process tile data
            foreach (TileData tile in tiles)
            {
                if (tile.X < 0 || tile.X >= width || tile.Z < 0 || tile.Z >= depth)
                {
                    Debug.LogWarning($"[GridService] Tile ({tile.X},{tile.Z}) is out of bounds ({width}x{depth}). Skipping.");
                    continue;
                }

                _tileTypes[tile.X, tile.Z] = tile.Type;

                // Determine if tile blocks movement
                _blocked[tile.X, tile.Z] = tile.Type == TileType.Wall || tile.Type == TileType.Empty
                    || (tile.Type == TileType.Door); // Doors block by default in V1 (no open/close yet)

                // Track spawn points
                if (tile.Type == TileType.Spawn)
                {
                    _spawnPoints.Add(new TileCoord(tile.X, tile.Z));
                }
            }
        }

        /// <summary>
        /// Returns true if the tile at (gridX, gridZ) is walkable.
        /// Bounds check, wall check, and edge-of-grid check.
        /// Zero-allocation — single int comparison path.
        /// </summary>
        public bool IsWalkable(int gridX, int gridZ)
        {
            // Bounds check
            if (gridX < 0 || gridX >= _width || gridZ < 0 || gridZ >= _depth)
                return false;

            // Blocked check (wall, empty, or closed door)
            return !_blocked[gridX, gridZ];
        }

        /// <summary>
        /// Returns the tile type at (gridX, gridZ).
        /// Returns Empty if out of bounds.
        /// </summary>
        public TileType GetTileType(int gridX, int gridZ)
        {
            if (gridX < 0 || gridX >= _width || gridZ < 0 || gridZ >= _depth)
                return TileType.Empty;

            return _tileTypes[gridX, gridZ];
        }

        /// <summary>
        /// Returns the world-space center of tile (gridX, gridZ).
        /// Tiles are TILE_SIZE x TILE_SIZE, pivot at center-bottom, Y=0 is floor level.
        /// Zero-allocation — pure arithmetic.
        /// </summary>
        public Vector3 GetTileCenter(int gridX, int gridZ)
        {
            float halfTile = GameConstants.TILE_SIZE * 0.5f;
            float worldX = gridX * GameConstants.TILE_SIZE + halfTile;
            float worldZ = gridZ * GameConstants.TILE_SIZE + halfTile;
            return new Vector3(worldX, 0f, worldZ);
        }

        /// <summary>
        /// Returns the first spawn point on the grid, or (0,0) if none registered.
        /// </summary>
        public TileCoord GetPrimarySpawnPoint()
        {
            if (_spawnPoints != null && _spawnPoints.Count > 0)
                return _spawnPoints[0];

            return new TileCoord(0, 0);
        }

        /// <summary>
        /// Returns all registered spawn points.
        /// </summary>
        public List<TileCoord> GetSpawnPoints()
        {
            return _spawnPoints ?? new List<TileCoord>(0);
        }

        /// <summary>
        /// Returns a copy of the current grid as TileData[].
        /// Used by editor tools and save/export operations.
        /// </summary>
        public TileData[] ExportTileData()
        {
            TileData[] result = new TileData[_width * _depth];
            int index = 0;

            for (int x = 0; x < _width; x++)
            {
                for (int z = 0; z < _depth; z++)
                {
                    result[index++] = new TileData(x, z, _tileTypes[x, z]);
                }
            }

            return result;
        }
    }
}
