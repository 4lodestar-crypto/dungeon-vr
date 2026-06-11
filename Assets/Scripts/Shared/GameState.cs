using UnityEngine;

namespace DungeonVR.Shared
{
    /// <summary>
    /// Root game state container. Owned by the server layer.
    /// V0-EXCEPTION: in-process storage; serialized/networked in V4+.
    /// </summary>
    [System.Serializable]
    public class GameState
    {
        public int CurrentTick { get; set; }
        public ChampionState Champion { get; set; }
        public GridData Grid { get; set; }

        public GameState()
        {
            CurrentTick = 0;
        }
    }

    /// <summary>
    /// Champion position and facing within the grid.
    /// Fields are public for serialization; modify only through MovementHandler.
    /// </summary>
    [System.Serializable]
    public class ChampionState
    {
        public int GridX;
        public int GridZ;
        public int FacingIndex; // 0=North, 1=East, 2=South, 3=West

        public Vector3 WorldPosition
            => new Vector3(GridX * GameConstants.TILE_SIZE, 0, GridZ * GameConstants.TILE_SIZE);

        public Vector3 ForwardDirection
        {
            get
            {
                return FacingIndex switch
                {
                    0 => Vector3.forward,  // North (+Z)
                    1 => Vector3.right,    // East  (+X)
                    2 => Vector3.back,     // South (-Z)
                    3 => Vector3.left,     // West  (-X)
                    _ => Vector3.forward
                };
            }
        }

        public ChampionState() { }

        public ChampionState(int gridX, int gridZ, int facingIndex)
        {
            GridX = gridX;
            GridZ = gridZ;
            FacingIndex = facingIndex;
        }

        public override string ToString()
            => $"Champion @ ({GridX},{GridZ}) facing {FacingIndex}";
    }

    /// <summary>
    /// Grid data for the test dungeon. Hardcoded for V0.
    /// V0-EXCEPTION: hardcoded grid; level data pipeline in V1.
    /// </summary>
    [System.Serializable]
    public class GridData
    {
        public int Width;
        public int Depth;
        public bool[,] Walls; // true = blocked

        public GridData() { }

        public GridData(int width, int depth, bool[,] walls)
        {
            Width = width;
            Depth = depth;
            Walls = walls;
        }

        public bool IsWalkable(int x, int z)
        {
            if (x < 0 || x >= Width || z < 0 || z >= Depth)
                return false;
            return !Walls[x, z];
        }
    }
}
