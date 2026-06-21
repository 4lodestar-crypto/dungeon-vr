using DungeonVR.Shared.Data;
using System.Collections.Generic;

namespace DungeonVR.AI.Interfaces
{
    /// <summary>
    /// Data for a single spawn point on a dungeon floor.
    /// </summary>
    public readonly struct SpawnPointData
    {
        /// <summary>Grid position of the spawn point.</summary>
        public readonly TileCoord Position;

        /// <summary>Initial facing direction index (0=N, 1=E, 2=S, 3=W).</summary>
        public readonly int FacingIndex;

        /// <summary>Floor number this spawn point belongs to.</summary>
        public readonly int FloorNumber;

        public SpawnPointData(TileCoord position, int facingIndex, int floorNumber)
        {
            Position = position;
            FacingIndex = facingIndex;
            FloorNumber = floorNumber;
        }
    }

    /// <summary>
    /// Provides spawn point data for a specific dungeon floor.
    /// Consumed by IMonsterSpawnHandler — NOT defined by AI layer.
    /// Implemented by the Level/Content system.
    /// </summary>
    public interface ISpawnPointProvider
    {
        /// <summary>
        /// Returns all valid spawn points for the given floor number.
        /// </summary>
        IEnumerable<SpawnPointData> GetSpawnPointsForFloor(int floorNumber);
    }
}
