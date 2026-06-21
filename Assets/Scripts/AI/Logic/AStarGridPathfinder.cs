using DungeonVR.AI.Interfaces;
using DungeonVR.Shared.Data;
using System.Collections.Generic;

namespace DungeonVR.AI.Logic
{
    /// <summary>
    /// Internal node used by A* pathfinding. Pooled to avoid GC allocations.
    /// </summary>
    internal struct PathNode
    {
        public int X;
        public int Z;
        public int GCost;  // cost from start to this node
        public int HCost;  // heuristic cost to target
        public int FCost => GCost + HCost;
        public int ParentIndex; // index into node pool, -1 = none
        public bool IsClosed;
        public bool IsOpen;

        public void Reset()
        {
            X = 0;
            Z = 0;
            GCost = 0;
            HCost = 0;
            ParentIndex = -1;
            IsClosed = false;
            IsOpen = false;
        }
    }

    /// <summary>
    /// A* pathfinder on a 4-directional tile grid.
    /// Features: node pooling, path caching, budget-aware interruption.
    /// Zero allocations in TryFindPath after initialisation (uses pooled arrays).
    /// </summary>
    public class AStarGridPathfinder : IGridPathfinder
    {
        private const int MaxPoolSize = 4096;
        private const int MaxOpenSetSize = 1024;

        private readonly DungeonVR.Shared.Interfaces.IGridQueryService _gridQuery;

        // Node pool — pre-allocated, reused across all pathfinding calls
        private readonly PathNode[] _nodePool;
        private readonly int[] _openSet;      // min-heap of node indices
        private int _openSetCount;

        // 2D lookup: nodeIndex[x, z] maps grid coords to pool index (-1 = not allocated)
        private readonly int[,] _nodeIndexMap;
        private readonly int _gridWidth;
        private readonly int _gridDepth;

        // Pre-allocated reversal buffer for path reconstruction (zero allocation)
        private readonly TileCoord[] _reversalBuffer;

        // Path cache
        private TileCoord _cachedStart;
        private TileCoord _cachedTarget;
        private List<TileCoord> _cachedPath;
        private bool _cacheValid;

        // Cardinal direction offsets: N, E, S, W
        private static readonly int[] DirX = { 0, 1, 0, -1 };
        private static readonly int[] DirZ = { 1, 0, -1, 0 };

        /// <summary>
        /// Creates the pathfinder bound to a specific grid query service.
        /// </summary>
        public AStarGridPathfinder(DungeonVR.Shared.Interfaces.IGridQueryService gridQuery)
        {
            _gridQuery = gridQuery;
            _gridWidth = gridQuery.Width;
            _gridDepth = gridQuery.Depth;

            _nodePool = new PathNode[MaxPoolSize];
            _openSet = new int[MaxOpenSetSize];
            _nodeIndexMap = new int[_gridWidth, _gridDepth];
            _reversalBuffer = new TileCoord[MaxPoolSize];

            // Initialise node index map to -1
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int z = 0; z < _gridDepth; z++)
                {
                    _nodeIndexMap[x, z] = -1;
                }
            }

            _cachedPath = new List<TileCoord>(64);
        }

        /// <summary>
        /// Attempts to find a path from start to target.
        /// Uses path caching: if the same start/target is requested and cache is valid,
        /// returns the cached path immediately.
        /// </summary>
        public bool TryFindPath(TileCoord start, TileCoord target, int maxSteps, out List<TileCoord> path)
        {
            // Cache hit
            if (_cacheValid && _cachedStart == start && _cachedTarget == target)
            {
                path = _cachedPath;
                return _cachedPath != null && _cachedPath.Count > 0;
            }

            // Validate bounds
            if (!IsInBounds(start) || !IsInBounds(target))
            {
                path = null;
                return false;
            }

            // Start or target is blocked
            if (!_gridQuery.IsWalkable(start.X, start.Z) || !_gridQuery.IsWalkable(target.X, target.Z))
            {
                path = null;
                return false;
            }

            // Already at target
            if (start == target)
            {
                _cachedPath.Clear();
                _cachedStart = start;
                _cachedTarget = target;
                _cacheValid = true;
                path = _cachedPath;
                return true;
            }

            // Run A*
            bool found = RunAStar(start, target, maxSteps);

            if (found)
            {
                ReconstructPath(start, target);
            }
            else
            {
                _cachedPath.Clear();
            }

            _cachedStart = start;
            _cachedTarget = target;
            _cacheValid = true;
            path = _cachedPath;
            return found;
        }

        public float GetHeuristicCost(TileCoord from, TileCoord to)
        {
            int dx = from.X - to.X;
            int dz = from.Z - to.Z;
            if (dx < 0) dx = -dx;
            if (dz < 0) dz = -dz;
            return dx + dz;
        }

        public void InvalidateCache(TileCoord tile)
        {
            // If the tile is part of the cached path, invalidate
            if (_cacheValid && _cachedPath != null)
            {
                for (int i = 0; i < _cachedPath.Count; i++)
                {
                    if (_cachedPath[i] == tile)
                    {
                        _cacheValid = false;
                        return;
                    }
                }
            }

            // If the tile is the cached start or target, invalidate
            if (_cacheValid && (_cachedStart == tile || _cachedTarget == tile))
            {
                _cacheValid = false;
            }
        }

        /// <summary>
        /// Core A* implementation using pooled nodes.
        /// Zero allocations after construction — reuses pre-allocated arrays.
        /// </summary>
        private bool RunAStar(TileCoord start, TileCoord target, int maxSteps)
        {
            // Reset open set
            _openSetCount = 0;

            // Allocate start node from pool
            int startIndex = AllocateNode(start.X, start.Z);
            _nodePool[startIndex].GCost = 0;
            _nodePool[startIndex].HCost = ManhattanDistance(start, target);
            _nodePool[startIndex].ParentIndex = -1;

            AddToOpenSet(startIndex);

            int nodesExplored = 0;

            while (_openSetCount > 0 && nodesExplored < maxSteps)
            {
                int currentIndex = RemoveCheapestFromOpenSet();

                if (_nodePool[currentIndex].X == target.X && _nodePool[currentIndex].Z == target.Z)
                {
                    // Target reached — parent chain stored in node pool
                    return true;
                }

                _nodePool[currentIndex].IsClosed = true;
                nodesExplored++;

                // Explore 4 cardinal neighbours
                for (int d = 0; d < 4; d++)
                {
                    int nx = _nodePool[currentIndex].X + DirX[d];
                    int nz = _nodePool[currentIndex].Z + DirZ[d];

                    if (!IsInBounds(nx, nz))
                        continue;

                    if (!_gridQuery.IsWalkable(nx, nz))
                        continue;

                    int neighbourIndex = GetOrAllocateNode(nx, nz);

                    if (_nodePool[neighbourIndex].IsClosed)
                        continue;

                    int tentativeG = _nodePool[currentIndex].GCost + 1; // cost of 1 per step

                    if (!_nodePool[neighbourIndex].IsOpen || tentativeG < _nodePool[neighbourIndex].GCost)
                    {
                        _nodePool[neighbourIndex].GCost = tentativeG;
                        _nodePool[neighbourIndex].HCost = ManhattanDistanceInline(nx, nz, target.X, target.Z);
                        _nodePool[neighbourIndex].ParentIndex = currentIndex;

                        if (!_nodePool[neighbourIndex].IsOpen)
                        {
                            AddToOpenSet(neighbourIndex);
                        }
                    }
                }
            }

            return false;
        }

        private void ReconstructPath(TileCoord start, TileCoord target)
        {
            _cachedPath.Clear();

            // Find target node in pool
            int targetIndex = _nodeIndexMap[target.X, target.Z];
            if (targetIndex < 0 || _nodePool[targetIndex].ParentIndex < 0)
            {
                // Target was never reached or is start
                return;
            }

            // Walk parent chain backwards, collect tiles
            // Use pre-allocated reversal buffer to avoid GC allocations
            int count = 0;

            int current = targetIndex;
            while (current >= 0 && _nodePool[current].ParentIndex >= 0 && count < MaxPoolSize)
            {
                _reversalBuffer[count++] = new TileCoord(_nodePool[current].X, _nodePool[current].Z);
                current = _nodePool[current].ParentIndex;
            }

            // Reverse into _cachedPath (excluding start, which is always last in reverse)
            for (int i = count - 1; i >= 0; i--)
            {
                _cachedPath.Add(_reversalBuffer[i]);
            }

            // Clean up pool allocations
            CleanupPool();
        }

        // ─── Node Pool Management ──────────────────────────────────────

        private int AllocateNode(int x, int z)
        {
            // Find a free slot
            for (int i = 0; i < MaxPoolSize; i++)
            {
                if (_nodePool[i].ParentIndex == -1 && !_nodePool[i].IsOpen && !_nodePool[i].IsClosed)
                {
                    _nodePool[i].X = x;
                    _nodePool[i].Z = z;
                    _nodePool[i].GCost = 0;
                    _nodePool[i].HCost = 0;
                    _nodePool[i].ParentIndex = -1;
                    _nodePool[i].IsClosed = false;
                    _nodePool[i].IsOpen = false;
                    _nodeIndexMap[x, z] = i;
                    return i;
                }
            }
            // Pool exhausted — fallback (shouldn't happen in practice)
            return -1;
        }

        private int GetOrAllocateNode(int x, int z)
        {
            int index = _nodeIndexMap[x, z];
            if (index >= 0)
                return index;
            return AllocateNode(x, z);
        }

        private void CleanupPool()
        {
            // Reset all nodes that were used
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int z = 0; z < _gridDepth; z++)
                {
                    int index = _nodeIndexMap[x, z];
                    if (index >= 0)
                    {
                        _nodePool[index].Reset();
                        _nodeIndexMap[x, z] = -1;
                    }
                }
            }
        }

        // ─── Open Set (Min-Heap) ───────────────────────────────────────

        private void AddToOpenSet(int nodeIndex)
        {
            if (_openSetCount >= MaxOpenSetSize)
                return;

            _openSet[_openSetCount] = nodeIndex;
            _nodePool[nodeIndex].IsOpen = true;
            HeapifyUp(_openSetCount);
            _openSetCount++;
        }

        private int RemoveCheapestFromOpenSet()
        {
            int result = _openSet[0];
            _nodePool[result].IsOpen = false;

            _openSetCount--;
            if (_openSetCount > 0)
            {
                _openSet[0] = _openSet[_openSetCount];
                HeapifyDown(0);
            }
            return result;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_nodePool[_openSet[index]].FCost >= _nodePool[_openSet[parent]].FCost)
                    break;

                int temp = _openSet[index];
                _openSet[index] = _openSet[parent];
                _openSet[parent] = temp;
                index = parent;
            }
        }

        private void HeapifyDown(int index)
        {
            while (true)
            {
                int left = 2 * index + 1;
                int right = 2 * index + 2;
                int smallest = index;

                if (left < _openSetCount &&
                    _nodePool[_openSet[left]].FCost < _nodePool[_openSet[smallest]].FCost)
                    smallest = left;

                if (right < _openSetCount &&
                    _nodePool[_openSet[right]].FCost < _nodePool[_openSet[smallest]].FCost)
                    smallest = right;

                if (smallest == index)
                    break;

                int temp = _openSet[index];
                _openSet[index] = _openSet[smallest];
                _openSet[smallest] = temp;
                index = smallest;
            }
        }

        // ─── Helpers ────────────────────────────────────────────────────

        private bool IsInBounds(int x, int z)
        {
            return x >= 0 && x < _gridWidth && z >= 0 && z < _gridDepth;
        }

        private bool IsInBounds(TileCoord coord)
        {
            return IsInBounds(coord.X, coord.Z);
        }

        private static int ManhattanDistance(TileCoord a, TileCoord b)
        {
            int dx = a.X - b.X;
            int dz = a.Z - b.Z;
            if (dx < 0) dx = -dx;
            if (dz < 0) dz = -dz;
            return dx + dz;
        }

        /// <summary>
        /// Inlined Manhattan distance — avoids TileCoord allocation in hot A* loop.
        /// </summary>
        private static int ManhattanDistanceInline(int ax, int az, int bx, int bz)
        {
            int dx = ax - bx;
            int dz = az - bz;
            if (dx < 0) dx = -dx;
            if (dz < 0) dz = -dz;
            return dx + dz;
        }
    }
}
