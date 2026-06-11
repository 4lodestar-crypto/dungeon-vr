using UnityEngine;
using UnityEngine.Rendering;

namespace DungeonVR.Editor
{
    /// <summary>
    /// V0 placeholder prefab builder.
    /// [ExecuteInEditMode] — works both in-editor and at runtime for V0 simplicity.
    /// V1 will migrate to actual .prefab assets in Resources/.
    /// </summary>
    [ExecuteInEditMode]
    public static class PrefabBuilder
    {
        // ------------------------------------------------------------------
        //  Shared helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Try URP Lit first, fall back to Standard so this works in any render pipeline.
        /// </summary>
        private static Material CreateMaterial(Color color, string materialName)
        {
            string shaderName = GraphicsSettings.renderPipelineAsset != null
                ? "Universal Render Pipeline/Lit"
                : "Standard";

            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                // Last-resort fallback
                shader = Shader.Find("Standard");
            }

            Material mat = new Material(shader)
            {
                color = color,
                name = materialName
            };

            return mat;
        }

        private static void SetDefaultMaterial(GameObject go, Color color, string materialName)
        {
            Material mat = CreateMaterial(color, materialName);
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = mat;
            }
        }

        // ------------------------------------------------------------------
        //  Tile builders
        // ------------------------------------------------------------------

        /// <summary>
        /// Build a floor tile: Plane primitive, 3×3 (localScale), light gray.
        /// No collider (V0 — gameplay colliders added later).
        /// </summary>
        public static GameObject BuildFloorTile()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "Floor_Tile_Stone";

            // Plane is 10×10 units by default; scale to 3×3 world units.
            go.transform.localScale = new Vector3(3f, 1f, 3f);

            // Remove auto-box collider (floor shouldn't block movement in V0)
            Object.DestroyImmediate(go.GetComponent<Collider>());

            SetDefaultMaterial(go, new Color32(0xCC, 0xCC, 0xCC, 0xFF), "Mat_Floor_Tile_Stone");

            return go;
        }

        /// <summary>
        /// Build a wall tile: Cube primitive, 3×3×0.5, dark gray, auto BoxCollider.
        /// </summary>
        public static GameObject BuildWallTile()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Wall_Tile_Stone";

            go.transform.localScale = new Vector3(3f, 3f, 0.5f);

            // BoxCollider is already present from CreatePrimitive(Cube) — keep it.

            SetDefaultMaterial(go, new Color32(0x66, 0x66, 0x66, 0xFF), "Mat_Wall_Tile_Stone");

            return go;
        }

        /// <summary>
        /// Build a champion capsule: Capsule primitive, height 1.8, Y=0.9, blue.
        /// Auto CapsuleCollider is kept.
        /// </summary>
        public static GameObject BuildChampionCapsule()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Champion_Default";

            // Default capsule height is 2.0 (Y=1). Scale to target height.
            float targetHeight = 1.8f;
            go.transform.localScale = new Vector3(1f, targetHeight, 1f);
            go.transform.position = new Vector3(0f, targetHeight * 0.5f, 0f);

            // CapsuleCollider auto-sizes to mesh — fine for V0.

            SetDefaultMaterial(go, new Color32(0x44, 0x88, 0xFF, 0xFF), "Mat_Champion_Default");

            return go;
        }
    }
}
