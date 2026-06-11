using UnityEngine;
using DungeonVR.Shared;

namespace DungeonVR.Art
{
    /// <summary>
    /// Runtime prefab factory for Dungeon VR tile assets.
    /// Creates GameObject prefabs using Unity primitives with proper URP materials.
    /// All prefabs use center-bottom pivot, Y=0 floor level, and 3m x 3m footprint.
    /// </summary>
    public static class TileFactory
    {
        // ── Shared Constants ──────────────────────────────────────────────
        private const float TileSize = 3f;
        private const float WallHeight = 3f;

        // ── Cached Materials (static, not per-instance) ───────────────────
        private static Material _matStoneDark;
        private static Material _matStoneGray;
        private static Material _matDoorFrame;
        private static Material _matDoorPanel;
        private static Material _matGold;
        private static Material _matTrapRed;
        private static Material _matStairs;
        private static Material _matChampionBlue;

        // ── Material Helpers ──────────────────────────────────────────────

        private static Material GetOrCreateMaterial(ref Material cached, string name, Color color)
        {
            if (cached != null) return cached;

            // Try URP Lit first, fall back to Standard
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            cached = new Material(shader)
            {
                name = name,
                color = color
            };
            return cached;
        }

        // ── Primitive Helpers ─────────────────────────────────────────────

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Material material,
            Vector3 position, Vector3 scale, bool isStatic = false, bool hasCollider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = scale;
            go.hideFlags = HideFlags.DontSave;

            // Assign material
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = material;

            // Static flags
            if (isStatic)
                go.isStatic = true;

            // Collider management
            if (!hasCollider)
            {
                var collider = go.GetComponent<Collider>();
                if (collider != null)
                    Object.DestroyImmediate(collider);
            }

            return go;
        }

        // ── Public Factory Methods ────────────────────────────────────────

        /// <summary>
        /// Creates a Wall prefab: 3x3x3 cube, dark stone gray.
        /// </summary>
        public static GameObject CreateWallPrefab()
        {
            var mat = GetOrCreateMaterial(ref _matStoneDark, "Mat_Wall_DarkStoneGray", 
                ColorFromHex(0x555566));

            var root = new GameObject("Wall_Tile")
            {
                hideFlags = HideFlags.DontSave
            };
            root.isStatic = true;

            var wall = CreatePrimitive(PrimitiveType.Cube, "Wall_Mesh", mat,
                position: new Vector3(0, WallHeight / 2f, 0),
                scale: new Vector3(TileSize, WallHeight, TileSize),
                isStatic: true,
                hasCollider: true);

            wall.transform.SetParent(root.transform);

            // Ensure box collider
            var bc = wall.GetComponent<BoxCollider>();
            if (bc == null) wall.AddComponent<BoxCollider>();

            return root;
        }

        /// <summary>
        /// Creates a Floor prefab: 3x3 plane, stone gray, no collider.
        /// </summary>
        public static GameObject CreateFloorPrefab()
        {
            var mat = GetOrCreateMaterial(ref _matStoneGray, "Mat_Floor_StoneGray",
                ColorFromHex(0x888899));

            var root = new GameObject("Floor_Tile")
            {
                hideFlags = HideFlags.DontSave
            };
            root.isStatic = true;

            // Plane is 10x10 units at scale 1; scale to 3x3
            var floor = CreatePrimitive(PrimitiveType.Plane, "Floor_Mesh", mat,
                position: Vector3.zero,
                scale: new Vector3(TileSize / 10f, 1, TileSize / 10f),
                isStatic: true,
                hasCollider: false);

            floor.transform.SetParent(root.transform);

            return root;
        }

        /// <summary>
        /// Creates a Door prefab: frame + door panel, pivot at bottom edge.
        /// </summary>
        public static GameObject CreateDoorPrefab()
        {
            var frameMat = GetOrCreateMaterial(ref _matDoorFrame, "Mat_Door_Frame",
                ColorFromHex(0x666677));
            var panelMat = GetOrCreateMaterial(ref _matDoorPanel, "Mat_Door_Panel",
                ColorFromHex(0x8B7355));

            var root = new GameObject("Door_Tile")
            {
                hideFlags = HideFlags.DontSave
            };
            root.isStatic = true;

            const float frameThick = 0.15f;
            const float doorWidth = 1.2f;
            const float doorHeight = 2.4f;

            // Left frame pillar
            var leftPillar = CreatePrimitive(PrimitiveType.Cube, "Door_Frame_Left", frameMat,
                position: new Vector3(-TileSize / 2f + frameThick / 2f, doorHeight / 2f, 0),
                scale: new Vector3(frameThick, doorHeight, frameThick),
                isStatic: true, hasCollider: true);
            leftPillar.transform.SetParent(root.transform);

            // Right frame pillar
            var rightPillar = CreatePrimitive(PrimitiveType.Cube, "Door_Frame_Right", frameMat,
                position: new Vector3(TileSize / 2f - frameThick / 2f, doorHeight / 2f, 0),
                scale: new Vector3(frameThick, doorHeight, frameThick),
                isStatic: true, hasCollider: true);
            rightPillar.transform.SetParent(root.transform);

            // Top frame beam
            var topBeam = CreatePrimitive(PrimitiveType.Cube, "Door_Frame_Top", frameMat,
                position: new Vector3(0, doorHeight - frameThick / 2f, 0),
                scale: new Vector3(TileSize, frameThick, frameThick),
                isStatic: true, hasCollider: true);
            topBeam.transform.SetParent(root.transform);

            // Door panel (slightly recessed)
            var panel = CreatePrimitive(PrimitiveType.Cube, "Door_Panel", panelMat,
                position: new Vector3(0, doorHeight / 2f - 0.1f, 0.05f),
                scale: new Vector3(doorWidth - 0.1f, doorHeight - 0.1f, 0.08f),
                isStatic: true, hasCollider: false);
            panel.transform.SetParent(root.transform);

            return root;
        }

        /// <summary>
        /// Creates an Altar prefab: stepped cube structure with gold material.
        /// </summary>
        public static GameObject CreateAltarPrefab()
        {
            var mat = GetOrCreateMaterial(ref _matGold, "Mat_Altar_Gold",
                ColorFromHex(0xDAA520));

            var root = new GameObject("Altar_Tile")
            {
                hideFlags = HideFlags.DontSave
            };
            root.isStatic = true;

            // Base step (wider, shorter)
            var baseStep = CreatePrimitive(PrimitiveType.Cube, "Altar_Base", mat,
                position: new Vector3(0, 0.25f, 0),
                scale: new Vector3(2.4f, 0.5f, 2.4f),
                isStatic: true, hasCollider: true);
            baseStep.transform.SetParent(root.transform);

            // Middle step
            var midStep = CreatePrimitive(PrimitiveType.Cube, "Altar_Mid", mat,
                position: new Vector3(0, 0.75f, 0),
                scale: new Vector3(1.8f, 0.5f, 1.8f),
                isStatic: true, hasCollider: true);
            midStep.transform.SetParent(root.transform);

            // Top platform
            var top = CreatePrimitive(PrimitiveType.Cube, "Altar_Top", mat,
                position: new Vector3(0, 1.25f, 0),
                scale: new Vector3(1.2f, 0.5f, 1.2f),
                isStatic: true, hasCollider: true);
            top.transform.SetParent(root.transform);

            // Emissive glow orb on top
            var glowMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
            if (glowMat != null)
            {
                glowMat.name = "Mat_Altar_Glow";
                glowMat.color = ColorFromHex(0xFFD700);
                glowMat.hideFlags = HideFlags.DontSave;

                var glow = CreatePrimitive(PrimitiveType.Sphere, "Altar_Glow", glowMat,
                    position: new Vector3(0, 1.65f, 0),
                    scale: new Vector3(0.4f, 0.4f, 0.4f),
                    isStatic: true, hasCollider: false);
                glow.transform.SetParent(root.transform);
            }

            return root;
        }

        /// <summary>
        /// Creates a Trap Trigger prefab: floor tile with red-tinted trigger zone.
        /// </summary>
        public static GameObject CreateTrapTriggerPrefab()
        {
            var mat = GetOrCreateMaterial(ref _matTrapRed, "Mat_Trap_Red",
                ColorFromHex(0xCC3333));

            var root = new GameObject("Trap_Trigger_Tile")
            {
                hideFlags = HideFlags.DontSave
            };
            root.isStatic = true;

            // Visual floor tile
            var tile = CreatePrimitive(PrimitiveType.Cube, "Trap_Visual", mat,
                position: new Vector3(0, 0.05f, 0),
                scale: new Vector3(TileSize, 0.1f, TileSize),
                isStatic: true, hasCollider: false);
            tile.transform.SetParent(root.transform);

            // Trigger zone (invisible, isTrigger collider)
            var triggerGo = new GameObject("Trap_Trigger")
            {
                hideFlags = HideFlags.DontSave
            };
            triggerGo.transform.SetParent(root.transform);
            triggerGo.transform.localPosition = new Vector3(0, 0.15f, 0);

            var bc = triggerGo.AddComponent<BoxCollider>();
            bc.isTrigger = true;
            bc.size = new Vector3(TileSize - 0.2f, 0.2f, TileSize - 0.2f);

            return root;
        }

        /// <summary>
        /// Creates Stairs prefab: 3 stepped cubes going up, each 1m high.
        /// </summary>
        public static GameObject CreateStairsPrefab()
        {
            var mat = GetOrCreateMaterial(ref _matStairs, "Mat_Stairs_StoneGray",
                ColorFromHex(0x777788));

            var root = new GameObject("Stairs_Tile")
            {
                hideFlags = HideFlags.DontSave
            };
            root.isStatic = true;

            const float stepHeight = 1f;
            const float stepDepth = 1f;

            for (int i = 0; i < 3; i++)
            {
                float yPos = i * stepHeight + stepHeight / 2f;
                float zPos = -TileSize / 2f + (i + 0.5f) * stepDepth;
                float width = TileSize - (i * 0.2f); // Slightly narrower each step

                var step = CreatePrimitive(PrimitiveType.Cube, $"Stairs_Step_{i}", mat,
                    position: new Vector3(0, yPos, zPos),
                    scale: new Vector3(width, stepHeight, stepDepth),
                    isStatic: true, hasCollider: true);
                step.transform.SetParent(root.transform);
            }

            return root;
        }

        /// <summary>
        /// Creates a Champion prefab: capsule with blue material, 1.8m tall.
        /// </summary>
        public static GameObject CreateChampionPrefab()
        {
            var mat = GetOrCreateMaterial(ref _matChampionBlue, "Mat_Champion_Blue",
                ColorFromHex(0x4488FF));

            var root = new GameObject("Champion")
            {
                hideFlags = HideFlags.DontSave
            };

            const float height = 1.8f;
            const float radius = 0.4f;

            var capsule = CreatePrimitive(PrimitiveType.Capsule, "Champion_Mesh", mat,
                position: new Vector3(0, height / 2f, 0),
                scale: new Vector3(radius * 2f, height, radius * 2f),
                isStatic: false,
                hasCollider: true);

            capsule.transform.SetParent(root.transform);

            // Ensure capsule collider
            var cc = capsule.GetComponent<CapsuleCollider>();
            if (cc != null)
            {
                cc.height = 1f;
                cc.radius = 0.5f;
                cc.direction = 1; // Y-axis
            }

            return root;
        }

        // ── Utility ──────────────────────────────────────────────────────

        private static Color ColorFromHex(uint hex)
        {
            float r = ((hex >> 16) & 0xFF) / 255f;
            float g = ((hex >> 8) & 0xFF) / 255f;
            float b = (hex & 0xFF) / 255f;
            return new Color(r, g, b);
        }
    }
}
