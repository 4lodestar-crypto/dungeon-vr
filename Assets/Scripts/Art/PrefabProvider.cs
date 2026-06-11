using UnityEngine;

namespace DungeonVR.Art
{
    /// <summary>
    /// Runtime prefab provider for V0 placeholder tiles.
    /// Builds primitives at runtime — no external assets needed.
    /// 
    /// V0-EXCEPTION: runtime primitive building; proper prefabs in V1.
    /// Caches built prefabs in static fields so we don't reconstruct every frame.
    /// </summary>
    public class PrefabProvider : MonoBehaviour
    {
        private static GameObject _cachedFloorPrefab;
        private static GameObject _cachedWallPrefab;
        private static GameObject _cachedChampionPrefab;

        // Material colors for placeholders
        private static readonly Color FloorColor = new Color32(0xCC, 0xCC, 0xCC, 0xFF);
        private static readonly Color WallColor = new Color32(0x66, 0x66, 0x66, 0xFF);
        private static readonly Color ChampionColor = new Color32(0x44, 0x88, 0xFF, 0xFF);

        // Inline primitive builders (no Editor assembly dependency)
        private static Material CreateMaterial(Color color)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                                  ?? Shader.Find("Standard"));
            mat.color = color;
            mat.name = $"Mat_Placeholder_{color}";
            return mat;
        }

        private static GameObject BuildFloorTile()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "Floor_Tile_Stone";
            go.transform.localScale = new Vector3(3f, 1f, 3f);
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.material = CreateMaterial(FloorColor);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject BuildWallTile()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Wall_Tile_Stone";
            go.transform.localScale = new Vector3(3f, 3f, 0.5f);
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.material = CreateMaterial(WallColor);
            return go;
        }

        private static GameObject BuildChampionCapsule()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Champion_Default";
            go.transform.position = new Vector3(0f, 0.9f, 0f);
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.material = CreateMaterial(ChampionColor);
            return go;
        }

        public static GameObject GetFloorPrefab()
        {
            if (_cachedFloorPrefab != null) return _cachedFloorPrefab;
            _cachedFloorPrefab = Resources.Load<GameObject>("Prefabs/Floor_Tile_Stone");
            if (_cachedFloorPrefab == null)
            {
                _cachedFloorPrefab = BuildFloorTile();
                if (_cachedFloorPrefab != null) _cachedFloorPrefab.hideFlags = HideFlags.DontSave;
            }
            return _cachedFloorPrefab;
        }

        public static GameObject GetWallPrefab()
        {
            if (_cachedWallPrefab != null) return _cachedWallPrefab;
            _cachedWallPrefab = Resources.Load<GameObject>("Prefabs/Wall_Tile_Stone");
            if (_cachedWallPrefab == null)
            {
                _cachedWallPrefab = BuildWallTile();
                if (_cachedWallPrefab != null) _cachedWallPrefab.hideFlags = HideFlags.DontSave;
            }
            return _cachedWallPrefab;
        }

        public static GameObject GetChampionPrefab()
        {
            if (_cachedChampionPrefab != null) return _cachedChampionPrefab;
            _cachedChampionPrefab = Resources.Load<GameObject>("Prefabs/Champion_Default");
            if (_cachedChampionPrefab == null)
            {
                _cachedChampionPrefab = BuildChampionCapsule();
                if (_cachedChampionPrefab != null) _cachedChampionPrefab.hideFlags = HideFlags.DontSave;
            }
            return _cachedChampionPrefab;
        }

        private void OnDestroy()
        {
            foreach (var cached in new[] { _cachedFloorPrefab, _cachedWallPrefab, _cachedChampionPrefab })
            {
                if (cached != null && cached.hideFlags == HideFlags.DontSave)
                    DestroyImmediate(cached);
            }
            _cachedFloorPrefab = _cachedWallPrefab = _cachedChampionPrefab = null;
        }
    }
}
