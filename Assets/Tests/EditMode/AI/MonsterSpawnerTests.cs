using System.Collections.Generic;
using DungeonVR.AI.Data;
using DungeonVR.AI.Interfaces;
using DungeonVR.AI.Logic;
using DungeonVR.Shared.Data;
using NUnit.Framework;

namespace DungeonVR.Tests.EditMode.AI
{
    /// <summary>
    /// EditMode tests for MonsterSpawner: floor-filtered weighted random selection.
    /// Tests empty tables, floor filtering, weighted distribution,
    /// and seed determinism.
    /// </summary>
    [TestFixture]
    public class MonsterSpawnerTests
    {
        /// <summary>
        /// Creates a MonsterSpawnTable from entry arrays.
        /// </summary>
        private static MonsterSpawnTable CreateTable(params MonsterSpawnEntry[] entries)
        {
            MonsterSpawnTable table = UnityEngine.ScriptableObject.CreateInstance<MonsterSpawnTable>();
            table.entries = entries;
            table.seedOverride = 0;
            return table;
        }

        /// <summary>
        /// Creates a simple spawn entry.
        /// </summary>
        private static MonsterSpawnEntry CreateEntry(
            string id, float weight = 1f, int minFloor = 0, int maxFloor = 10)
        {
            return new MonsterSpawnEntry
            {
                spawnGroupId = "default",
                monsterDefinitionId = id,
                weight = weight,
                minFloor = minFloor,
                maxFloor = maxFloor,
            };
        }

        /// <summary>
        /// Creates a list of spawn points.
        /// </summary>
        private static List<TileCoord> CreateSpawnPoints(int count)
        {
            List<TileCoord> points = new List<TileCoord>(count);
            for (int i = 0; i < count; i++)
                points.Add(new TileCoord(i, 0));
            return points;
        }

        // ──────────────────────────────────────────────────────────────────
        // Basic Spawning
        // ──────────────────────────────────────────────────────────────────

        /// <summary>Single entry, one spawn point → one spawn request.</summary>
        [Test]
        public void SingleEntry_SingleSpawnPoint_ReturnsOneRequest()
        {
            MonsterSpawnTable table = CreateTable(CreateEntry("screamer"));
            MonsterSpawner spawner = new MonsterSpawner(table, CreateSpawnPoints(1), seed: 42);

            List<SpawnRequest> requests = spawner.EvaluateSpawns(floorDepth: 1, tickStamp: 0);

            Assert.AreEqual(1, requests.Count);
            Assert.AreEqual("screamer", requests[0].MonsterDefinitionId);
            Assert.AreEqual(new TileCoord(0, 0), requests[0].Position);
        }

        /// <summary>Multiple spawn points → one request per point.</summary>
        [Test]
        public void MultipleSpawnPoints_ReturnsOneRequestPerPoint()
        {
            MonsterSpawnTable table = CreateTable(CreateEntry("screamer"));
            MonsterSpawner spawner = new MonsterSpawner(table, CreateSpawnPoints(4), seed: 42);

            List<SpawnRequest> requests = spawner.EvaluateSpawns(floorDepth: 1, tickStamp: 0);

            Assert.AreEqual(4, requests.Count);
            for (int i = 0; i < 4; i++)
                Assert.AreEqual(new TileCoord(i, 0), requests[i].Position);
        }

        // ──────────────────────────────────────────────────────────────────
        // Empty Table / Empty Spawn Points
        // ──────────────────────────────────────────────────────────────────

        /// <summary>Empty spawn table returns zero requests.</summary>
        [Test]
        public void EmptySpawnTable_ReturnsZeroRequests()
        {
            MonsterSpawnTable table = CreateTable(); // No entries.
            MonsterSpawner spawner = new MonsterSpawner(table, CreateSpawnPoints(3), seed: 42);

            List<SpawnRequest> requests = spawner.EvaluateSpawns(floorDepth: 1, tickStamp: 0);

            Assert.AreEqual(0, requests.Count, "Empty table should produce no spawns.");
        }

        /// <summary>Null entries array returns zero requests.</summary>
        [Test]
        public void NullEntries_ReturnsZeroRequests()
        {
            MonsterSpawnTable table = UnityEngine.ScriptableObject.CreateInstance<MonsterSpawnTable>();
            table.entries = null;
            MonsterSpawner spawner = new MonsterSpawner(table, CreateSpawnPoints(2), seed: 42);

            List<SpawnRequest> requests = spawner.EvaluateSpawns(floorDepth: 1, tickStamp: 0);

            Assert.AreEqual(0, requests.Count);
        }

        /// <summary>No spawn points returns zero requests even with valid table.</summary>
        [Test]
        public void EmptySpawnPoints_ReturnsZeroRequests()
        {
            MonsterSpawnTable table = CreateTable(CreateEntry("screamer"));
            MonsterSpawner spawner = new MonsterSpawner(table, new List<TileCoord>(), seed: 42);

            List<SpawnRequest> requests = spawner.EvaluateSpawns(floorDepth: 1, tickStamp: 0);

            Assert.AreEqual(0, requests.Count, "No spawn points → no spawns.");
        }

        // ──────────────────────────────────────────────────────────────────
        // Floor Filtering
        // ──────────────────────────────────────────────────────────────────

        /// <summary>Entry outside floor range is excluded.</summary>
        [Test]
        public void FloorFiltering_ExcludesOutOfRangeEntries()
        {
            MonsterSpawnTable table = CreateTable(
                CreateEntry("screamer", 1f, minFloor: 3, maxFloor: 5) // Only floors 3-5.
            );
            MonsterSpawner spawner = new MonsterSpawner(table, CreateSpawnPoints(1), seed: 42);

            // Floor 1: out of range.
            List<SpawnRequest> requests1 = spawner.EvaluateSpawns(floorDepth: 1, tickStamp: 0);
            Assert.AreEqual(0, requests1.Count, "Floor 1 should be excluded.");

            // Floor 4: in range.
            List<SpawnRequest> requests4 = spawner.EvaluateSpawns(floorDepth: 4, tickStamp: 0);
            Assert.AreEqual(1, requests4.Count, "Floor 4 should be included.");
        }

        /// <summary>Multiple entries, some filtered by floor.</summary>
        [Test]
        public void FloorFiltering_MixedEntries_OnlyEligibleSelected()
        {
            MonsterSpawnTable table = CreateTable(
                CreateEntry("screamer", 1f, minFloor: 1, maxFloor: 3),
                CreateEntry("ghost", 1f, minFloor: 5, maxFloor: 10),
                CreateEntry("skeleton", 1f, minFloor: 1, maxFloor: 10)
            );
            MonsterSpawner spawner = new MonsterSpawner(table, CreateSpawnPoints(3), seed: 42);

            // Floor 2: only screamer (1-3) and skeleton (1-10) eligible.
            List<SpawnRequest> requests = spawner.EvaluateSpawns(floorDepth: 2, tickStamp: 0);
            Assert.AreEqual(3, requests.Count);

            foreach (SpawnRequest req in requests)
            {
                Assert.IsTrue(
                    req.MonsterDefinitionId == "screamer" || req.MonsterDefinitionId == "skeleton",
                    $"Request should be screamer or skeleton, got '{req.MonsterDefinitionId}'.");
                Assert.AreNotEqual("ghost", req.MonsterDefinitionId,
                    "Ghost should be filtered out at floor 2.");
            }
        }

        // ──────────────────────────────────────────────────────────────────
        // Weighted Selection (distributional test)
        // ──────────────────────────────────────────────────────────────────

        /// <summary>Higher weight entries are selected more often.</summary>
        [Test]
        public void WeightedSelection_FavorsHigherWeights()
        {
            // Screamer weight 9, Skeleton weight 1.
            // With enough spawns, Screamer should appear ~90% of the time.
            MonsterSpawnTable table = CreateTable(
                CreateEntry("screamer", 9f),
                CreateEntry("skeleton", 1f)
            );
            // Many spawn points to get statistical distribution.
            const int pointCount = 1000;
            MonsterSpawner spawner = new MonsterSpawner(table, CreateSpawnPoints(pointCount), seed: 42);

            List<SpawnRequest> requests = spawner.EvaluateSpawns(floorDepth: 1, tickStamp: 0);

            int screamerCount = 0;
            int skeletonCount = 0;
            foreach (SpawnRequest req in requests)
            {
                if (req.MonsterDefinitionId == "screamer") screamerCount++;
                else if (req.MonsterDefinitionId == "skeleton") skeletonCount++;
            }

            Assert.AreEqual(pointCount, screamerCount + skeletonCount,
                "All requests should be one of the two types.");

            // Expect roughly 90% screamer (with tolerance).
            float screamerRatio = (float)screamerCount / pointCount;
            Assert.Greater(screamerRatio, 0.8f,
                $"Screamer should be ~90% of spawns, got {screamerRatio:P1}.");
            Assert.Less(screamerRatio, 0.98f,
                $"Screamer ratio should not be 100%, got {screamerRatio:P1}.");
        }

        /// <summary>All-zero weights defaults to equal chance (fallback).</summary>
        [Test]
        public void AllZeroWeights_NoCrash_ReturnsSpawns()
        {
            MonsterSpawnTable table = CreateTable(
                CreateEntry("screamer", 0f),
                CreateEntry("skeleton", 0f)
            );
            MonsterSpawner spawner = new MonsterSpawner(table, CreateSpawnPoints(1), seed: 42);

            // All weights zero → totalWeight=0 → no eligible entries → empty result.
            List<SpawnRequest> requests = spawner.EvaluateSpawns(floorDepth: 1, tickStamp: 0);
            Assert.AreEqual(0, requests.Count,
                "Zero-weight entries should produce no spawns.");
        }

        /// <summary>Negative-weight entries are excluded.</summary>
        [Test]
        public void NegativeWeight_Excluded()
        {
            MonsterSpawnTable table = CreateTable(
                CreateEntry("screamer", -1f),
                CreateEntry("skeleton", 5f)
            );
            MonsterSpawner spawner = new MonsterSpawner(table, CreateSpawnPoints(5), seed: 42);

            List<SpawnRequest> requests = spawner.EvaluateSpawns(floorDepth: 1, tickStamp: 0);

            foreach (SpawnRequest req in requests)
            {
                Assert.AreEqual("skeleton", req.MonsterDefinitionId,
                    "Negative-weight entries should never be selected.");
            }
        }

        // ──────────────────────────────────────────────────────────────────
        // Determinism
        // ──────────────────────────────────────────────────────────────────

        /// <summary>Same seed produces identical spawn results.</summary>
        [Test]
        public void Determinism_SameSeed_SameResults()
        {
            MonsterSpawnTable table = CreateTable(
                CreateEntry("screamer", 3f),
                CreateEntry("ghost", 2f),
                CreateEntry("skeleton", 1f)
            );
            const int seed = 12345;
            List<TileCoord> points = CreateSpawnPoints(20);

            MonsterSpawner spawner1 = new MonsterSpawner(table, points, seed);
            MonsterSpawner spawner2 = new MonsterSpawner(table, points, seed);

            List<SpawnRequest> result1 = spawner1.EvaluateSpawns(floorDepth: 1, tickStamp: 0);
            List<SpawnRequest> result2 = spawner2.EvaluateSpawns(floorDepth: 1, tickStamp: 0);

            Assert.AreEqual(result1.Count, result2.Count);
            for (int i = 0; i < result1.Count; i++)
            {
                Assert.AreEqual(result1[i].MonsterDefinitionId, result2[i].MonsterDefinitionId,
                    $"Entry {i}: definition IDs should match.");
                Assert.AreEqual(result1[i].Position, result2[i].Position,
                    $"Entry {i}: positions should match.");
                Assert.AreEqual(result1[i].FacingIndex, result2[i].FacingIndex,
                    $"Entry {i}: facing indices should match.");
            }
        }

        /// <summary>Different seeds produce different results (statistically).</summary>
        [Test]
        public void DifferentSeeds_ProduceDifferentResults()
        {
            MonsterSpawnTable table = CreateTable(
                CreateEntry("screamer", 5f),
                CreateEntry("skeleton", 5f)
            );
            List<TileCoord> points = CreateSpawnPoints(50);

            MonsterSpawner spawnerA = new MonsterSpawner(table, points, seed: 1);
            MonsterSpawner spawnerB = new MonsterSpawner(table, points, seed: 999);

            List<SpawnRequest> resultA = spawnerA.EvaluateSpawns(floorDepth: 1, tickStamp: 0);
            List<SpawnRequest> resultB = spawnerB.EvaluateSpawns(floorDepth: 1, tickStamp: 0);

            // They should differ in at least one position (with 50 spawns, very high probability).
            bool differs = false;
            for (int i = 0; i < resultA.Count; i++)
            {
                if (resultA[i].MonsterDefinitionId != resultB[i].MonsterDefinitionId ||
                    resultA[i].FacingIndex != resultB[i].FacingIndex)
                {
                    differs = true;
                    break;
                }
            }
            Assert.IsTrue(differs, "Different seeds should produce different spawn patterns.");
        }
    }
}
