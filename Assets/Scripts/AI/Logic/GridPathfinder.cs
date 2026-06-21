using System;
using System.Collections.Generic;
using DungeonVR.Shared.Data;
using DungeonVR.Shared.Interfaces;

namespace DungeonVR.AI.Logic
{
    /// <summary>
    /// A* pathfinder operating on the dungeon tile grid.
    /// Uses Octile distance heuristic for 8-directional movement with diagonal
    /// corner-cutting prevention. Walkability is determined by
    /// IGridQueryService.IsWalkable — no Physics queries.
    ///
    /// Cache: paths are cached for PathCacheTtlTicks server ticks.
    /// Zero-allocation on cache hits; allocates only on cache misses.
    /// </summary>
    public static class GridPathfinder
    {
        /// <summary>Number of ticks a cached path remains valid before recomputation.</summary>
        public const int PathCacheTtlTicks = 30;

        // ── Cache ──────────────────────────────────────────────────────────

        private struct CacheKey : IEquatable<CacheKey>
        {
            public readonly TileCoord Start;
            public readonly TileCoord Goal;
            public CacheKey(TileCoord start, TileCoord goal) { Start = start; Goal = goal; }
            public bool Equals(CacheKey other) => Start == other.Start && Goal == other.Goal;
            public override bool Equals(object obj) => obj is CacheKey ck && Equals(ck);
            public override int GetHashCode() => (Start.GetHashCode() * 397) ^ Goal.GetHashCode();
        }

        private struct CacheEntry
        {
            public List<TileCoord> Path;
            public int CachedAtTick;
        }

        private static readonly Dictionary<CacheKey, CacheEntry> _cache =
            new Dictionary<CacheKey, CacheEntry>();

        private static int _currentTick;

        /// <summary>Advance the internal tick counter (call once per server tick).</summary>
        public static void AdvanceTick() { _currentTick++; }

        /// <summary>Clear all cached paths (call when grid changes).</summary>
        public static void InvalidateCache() { _cache.Clear(); }

        // ── 8-directional neighbour deltas ─────────────────────────────────

        private static readonly (int dx, int dz)[] _neighborDeltas = new (int, int)[]
        {
            ( 0,  1), // N
            ( 1,  1), // NE
            ( 1,  0), // E
            ( 1, -1), // SE
            ( 0, -1), // S
            (-1, -1), // SW
            (-1,  0), // W
            (-1,  1), // NW
        };

        // ── Public API ─────────────────────────────────────────────────────

        /// <summary>
        /// Find a shortest path from start to goal on the dungeon grid.
        /// </summary>
        /// <returns>
        /// List of waypoints from start (exclusive) to goal (inclusive).
        /// Empty list if no path exists, start/goal out of bounds, or blocked.
        /// </returns>
        public static List<TileCoord> FindPath(TileCoord start, TileCoord goal, IGridQueryService grid)
        {
            // ── Guards ─────────────────────────────────────────────────────
            if (grid == null) return new List<TileCoord>(0);
            if (start == goal) return new List<TileCoord>(0);

            if (!InBounds(start, grid) || !InBounds(goal, grid))
                return new List<TileCoord>(0);

            if (!grid.IsWalkable(start.X, start.Z) || !grid.IsWalkable(goal.X, goal.Z))
                return new List<TileCoord>(0);

            // ── Cache lookup (no allocation on hit) ─────────────────────────
            CacheKey key = new CacheKey(start, goal);
            if (_cache.TryGetValue(key, out CacheEntry cached))
            {
                if (_currentTick - cached.CachedAtTick < PathCacheTtlTicks)
                    return cached.Path;
            }

            // ── A* search ──────────────────────────────────────────────────
            List<TileCoord> path = AStarSearch(start, goal, grid);

            // ── Store in cache ─────────────────────────────────────────────
            _cache[key] = new CacheEntry { Path = path, CachedAtTick = _currentTick };

            return path;
        }

        // ── A* Implementation ──────────────────────────────────────────────

        private static List<TileCoord> AStarSearch(TileCoord start, TileCoord goal, IGridQueryService grid)
        {
            int width = grid.Width;
            int depth = grid.Depth;

            // Flat-index helpers.
            int startIdx = Index(start.X, start.Z, width);
            int goalIdx = Index(goal.X, goal.Z, width);

            // Closed set: flat bool array (no allocations beyond initial).
            bool[] closed = new bool[width * depth];

            // Open set: dictionary mapping flat index → g score.
            // Parent links stored in separate dict so they survive node removal.
            Dictionary<int, float> gScores = new Dictionary<int, float>();
            Dictionary<int, int> parents = new Dictionary<int, int>(); // childIdx → parentIdx

            float hStart = OctileDistance(start, goal);
            gScores[startIdx] = 0f;
            parents[startIdx] = -1; // Sentinel: no parent.

            // Pre-allocated neighbour buffer (zero per-iteration allocations).
            (int nx, int nz, float cost)[] neighbors = new (int, int, float)[8];

            while (gScores.Count > 0)
            {
                // Find open node with lowest F = g + h.
                int currentIdx = -1;
                float bestF = float.MaxValue;
                foreach (var kvp in gScores)
                {
                    int idx = kvp.Key;
                    float g = kvp.Value;
                    int ctx = idx % width;
                    int ctz = idx / width;
                    float h = OctileDistance(new TileCoord(ctx, ctz), goal);
                    float f = g + h;
                    if (f < bestF)
                    {
                        bestF = f;
                        currentIdx = idx;
                    }
                }

                float currentG = gScores[currentIdx];
                gScores.Remove(currentIdx);

                // Goal reached — reconstruct path.
                if (currentIdx == goalIdx)
                    return ReconstructPath(parents, currentIdx, width);

                closed[currentIdx] = true;

                int cx = currentIdx % width;
                int cz = currentIdx / width;

                // Expand neighbours.
                int nCount = GetWalkableNeighbors(cx, cz, grid, width, depth, neighbors);
                for (int n = 0; n < nCount; n++)
                {
                    int nIdx = Index(neighbors[n].nx, neighbors[n].nz, width);
                    if (closed[nIdx]) continue;

                    float tentativeG = currentG + neighbors[n].cost;

                    if (gScores.TryGetValue(nIdx, out float existingG))
                    {
                        if (tentativeG < existingG)
                        {
                            gScores[nIdx] = tentativeG;
                            parents[nIdx] = currentIdx;
                        }
                    }
                    else
                    {
                        gScores[nIdx] = tentativeG;
                        parents[nIdx] = currentIdx;
                    }
                }
            }

            // No path.
            return new List<TileCoord>(0);
        }

        /// <summary>
        /// Walk parent links backwards from goalIdx to start, then reverse.
        /// Result: list of tiles from start (exclusive) to goal (inclusive).
        /// </summary>
        private static List<TileCoord> ReconstructPath(Dictionary<int, int> parents, int goalIdx, int width)
        {
            List<TileCoord> reversed = new List<TileCoord>();
            int cur = goalIdx;

            while (cur >= 0)
            {
                int x = cur % width;
                int z = cur / width;
                reversed.Add(new TileCoord(x, z));

                if (!parents.TryGetValue(cur, out int parent))
                    break;
                cur = parent;
            }

            // Remove start tile (last in reversed list).
            if (reversed.Count > 0)
                reversed.RemoveAt(reversed.Count - 1);

            reversed.Reverse();
            return reversed;
        }

        // ── Neighbour expansion ────────────────────────────────────────────

        /// <summary>
        /// Fill pre-allocated array with walkable neighbours and their costs.
        /// Returns the number of entries written. O(1): 8 checks, each O(1).
        /// Diagonal moves are only allowed when both cardinal neighbours are
        /// walkable (prevents corner-cutting through walls).
        /// </summary>
        private static int GetWalkableNeighbors(int x, int z, IGridQueryService grid,
            int width, int depth, (int nx, int nz, float cost)[] outNeighbors)
        {
            int count = 0;
            for (int i = 0; i < _neighborDeltas.Length; i++)
            {
                int nx = x + _neighborDeltas[i].dx;
                int nz = z + _neighborDeltas[i].dz;

                if (nx < 0 || nx >= width || nz < 0 || nz >= depth)
                    continue;

                if (!grid.IsWalkable(nx, nz))
                    continue;

                bool diag = (_neighborDeltas[i].dx != 0 && _neighborDeltas[i].dz != 0);
                if (diag)
                {
                    // Block diagonal if either cardinal is unwalkable.
                    if (!grid.IsWalkable(x + _neighborDeltas[i].dx, z) ||
                        !grid.IsWalkable(x, z + _neighborDeltas[i].dz))
                        continue;
                }

                outNeighbors[count] = (nx, nz, diag ? 1.414213562f : 1.0f);
                count++;
            }
            return count;
        }

        // ── Heuristic ──────────────────────────────────────────────────────

        /// <summary>
        /// Octile distance: admissible for 8-directional grid movement.
        /// Considers both cardinal (cost 1) and diagonal (cost √2) movement.
        /// </summary>
        private static float OctileDistance(TileCoord a, TileCoord b)
        {
            int dx = Math.Abs(a.X - b.X);
            int dz = Math.Abs(a.Z - b.Z);
            const float sqrt2minus1 = 0.414213562f;
            return dx < dz
                ? sqrt2minus1 * dx + dz
                : sqrt2minus1 * dz + dx;
        }

        // ── Utilities ──────────────────────────────────────────────────────

        private static bool InBounds(TileCoord c, IGridQueryService grid) =>
            c.X >= 0 && c.X < grid.Width && c.Z >= 0 && c.Z < grid.Depth;

        private static int Index(int x, int z, int width) => z * width + x;
    }
}
