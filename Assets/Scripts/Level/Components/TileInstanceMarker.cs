using UnityEngine;
using DungeonVR.Shared.Data;
using DungeonVR.Shared.Enums;

namespace DungeonVR.Level.Components
{
    /// <summary>
    /// Attached to each instantiated tile at runtime.
    /// Provides debug info and editor tool access to the underlying TileData.
    /// </summary>
    public class TileInstanceMarker : MonoBehaviour
    {
        [SerializeField]
        private int _gridX;

        [SerializeField]
        private int _gridZ;

        [SerializeField]
        private TileType _tileType;

        [SerializeField]
        private WallFace _wallFaces;

        /// <summary>Grid X coordinate.</summary>
        public int GridX => _gridX;

        /// <summary>Grid Z coordinate.</summary>
        public int GridZ => _gridZ;

        /// <summary>Tile classification.</summary>
        public TileType TileType => _tileType;

        /// <summary>Wall geometry mask.</summary>
        public WallFace WallFaces => _wallFaces;

        /// <summary>
        /// Initialize this marker from a TileData struct.
        /// Called during level loading.
        /// </summary>
        public void Initialize(TileData data)
        {
            _gridX = data.X;
            _gridZ = data.Z;
            _tileType = data.Type;
            _wallFaces = data.WallFaces;
        }

        private void OnDrawGizmos()
        {
            // Draw a small label at the tile center in the scene view
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.5f,
                $"({_gridX},{_gridZ}) {_tileType}"
            );
            #endif
        }
    }
}
