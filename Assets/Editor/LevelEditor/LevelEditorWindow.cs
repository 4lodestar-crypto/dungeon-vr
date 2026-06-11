using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using DungeonVR.Shared.Data;
using DungeonVR.Shared.Enums;
using DungeonVR.Level.Logic;
using DungeonVR.Level.Interfaces;
using DungeonVR.Level.Data;

namespace DungeonVR.Editor.LevelEditor
{
    /// <summary>
    /// Grid-based level editor window for designing Dungeon VR levels.
    /// Features a 32x32 scrollable grid, tile paint brush, eraser,
    /// JSON import/export, and integrated validation.
    /// Designed to be usable by non-programmers.
    /// 
    /// Open via Window → Dungeon VR → Level Editor
    /// </summary>
    public class LevelEditorWindow : EditorWindow
    {
        #region Constants

        private const int DEFAULT_WIDTH = 32;
        private const int DEFAULT_DEPTH = 32;
        private const float GRID_CELL_SIZE = 18f;
        private const float GRID_SPACING = 1f;
        private const float GRID_STEP = GRID_CELL_SIZE + GRID_SPACING;
        private const float TOOLBAR_HEIGHT = 60f;
        private const float STATUS_BAR_HEIGHT = 24f;

        #endregion

        #region State

        private int _gridWidth = DEFAULT_WIDTH;
        private int _gridDepth = DEFAULT_DEPTH;
        private TileType[,] _grid;
        private WallFace[,] _wallFaces;

        private TileType _selectedTileType = TileType.Floor;
        private bool _isPainting = false;
        private bool _isErasing = false;
        private Vector2 _scrollPosition;
        private string _statusMessage = "Ready. Paint tiles on the grid.";
        private MessageType _statusType = MessageType.Info;
        private bool _showValidation = false;
        private string[] _validationErrors = new string[0];
        private bool _isSolvable = false;

        private string _currentFilePath = string.Empty;
        private string _levelName = "New Level";

        // Colour scheme for tile types (non-programmer friendly)
        private static readonly Dictionary<TileType, Color> TileColors = new Dictionary<TileType, Color>
        {
            { TileType.Floor,  new Color(0.5f,  0.5f,  0.5f)  },  // Gray
            { TileType.Wall,   new Color(0.25f, 0.15f, 0.05f) },  // Brown
            { TileType.Door,   new Color(0.8f,  0.6f,  0.2f)  },  // Gold/Brass
            { TileType.Trap,   new Color(0.9f,  0.2f,  0.2f)  },  // Red
            { TileType.Altar,  new Color(0.9f,  0.8f,  0.1f)  },  // Gold/Yellow
            { TileType.Spawn,  new Color(0.2f,  0.6f,  1.0f)  },  // Blue
            { TileType.Stairs, new Color(0.2f,  0.8f,  0.3f)  },  // Green
            { TileType.Empty,  new Color(0.1f,  0.1f,  0.1f)  },  // Near black
        };

        // Display names for the toolbar
        private static readonly Dictionary<TileType, string> TileDisplayNames = new Dictionary<TileType, string>
        {
            { TileType.Floor,  "Floor"  },
            { TileType.Wall,   "Wall"   },
            { TileType.Door,   "Door"   },
            { TileType.Trap,   "Trap"   },
            { TileType.Altar,  "Altar"  },
            { TileType.Spawn,  "Spawn"  },
            { TileType.Stairs, "Stairs" },
            { TileType.Empty,  "Empty"  },
        };

        // Tooltips for the toolbar buttons
        private static readonly Dictionary<TileType, string> TileTooltips = new Dictionary<TileType, string>
        {
            { TileType.Floor,  "Walkable ground tile. The default for empty areas." },
            { TileType.Wall,   "Impassable obstacle. Blocks movement." },
            { TileType.Door,   "Walkable when open, blocks when closed." },
            { TileType.Trap,   "Triggers an effect when stepped on." },
            { TileType.Altar,  "Interaction point (save, heal, etc.)." },
            { TileType.Spawn,  "Player spawn point. Exactly one per level." },
            { TileType.Stairs, "Exit to the next floor. At least one per level." },
            { TileType.Empty,  "Void tile — no geometry, out of bounds." },
        };

        #endregion

        #region Initialization

        [MenuItem("Window/Dungeon VR/Level Editor", priority = 1000)]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelEditorWindow>();
            window.titleContent = new GUIContent("Dungeon VR Level Editor", "Design and edit dungeon levels");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        private void OnEnable()
        {
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            _grid = new TileType[_gridWidth, _gridDepth];
            _wallFaces = new WallFace[_gridWidth, _gridDepth];

            for (int x = 0; x < _gridWidth; x++)
            {
                for (int z = 0; z < _gridDepth; z++)
                {
                    _grid[x, z] = TileType.Floor;
                    _wallFaces[x, z] = WallFace.None;
                }
            }

            // Create a default perimeter
            for (int x = 0; x < _gridWidth; x++)
            {
                _grid[x, 0] = TileType.Wall;
                _grid[x, _gridDepth - 1] = TileType.Wall;
            }
            for (int z = 0; z < _gridDepth; z++)
            {
                _grid[0, z] = TileType.Wall;
                _grid[_gridWidth - 1, z] = TileType.Wall;
            }

            // Place a default spawn and stairs
            _grid[1, 1] = TileType.Spawn;
            _grid[_gridWidth - 2, _gridDepth - 2] = TileType.Stairs;
        }

        #endregion

        #region GUI

        private void OnGUI()
        {
            DrawToolbar();
            DrawGrid();
            DrawStatusBar();
        }

        /// <summary>
        /// Top toolbar: tile type selection, paint/erase mode, load/save buttons.
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Height(TOOLBAR_HEIGHT));

            // Row 1: Tile palette
            EditorGUILayout.LabelField("Tile Palette — Select a tile type to paint:", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            foreach (TileType type in Enum.GetValues(typeof(TileType)))
            {
                string displayName = TileDisplayNames.GetValueOrDefault(type, type.ToString());
                string tooltip = TileTooltips.GetValueOrDefault(type, "");
                Color color = TileColors.GetValueOrDefault(type, Color.white);

                GUI.color = color;
                bool isSelected = (_selectedTileType == type);
                GUIStyle buttonStyle = isSelected ? GUI.skin.button : new GUIStyle(GUI.skin.button);

                if (isSelected)
                {
                    // Highlight selected button
                    buttonStyle.normal.textColor = Color.white;
                    buttonStyle.fontStyle = FontStyle.Bold;
                }

                GUIContent content = new GUIContent(displayName, tooltip);

                if (GUILayout.Button(content, buttonStyle, GUILayout.Width(70), GUILayout.Height(28)))
                {
                    _selectedTileType = type;
                    _isPainting = false;
                    _isErasing = false;
                    _statusMessage = $"Selected: {displayName}. Click tiles to paint.";
                    _statusType = MessageType.Info;
                }
            }
            GUI.color = Color.white;

            // Paint/Erase toggles
            GUI.color = _isPainting ? Color.green : Color.gray;
            if (GUILayout.Button(new GUIContent("Paint Brush", "Click and drag to paint tiles"), GUILayout.Width(100), GUILayout.Height(28)))
            {
                _isPainting = !_isPainting;
                _isErasing = false;
                _statusMessage = _isPainting ? "Paint mode ON. Click and drag on the grid." : "Paint mode OFF.";
                _statusType = MessageType.Info;
            }

            GUI.color = _isErasing ? Color.red : Color.gray;
            if (GUILayout.Button(new GUIContent("Eraser", "Click and drag to erase (set to Floor)"), GUILayout.Width(100), GUILayout.Height(28)))
            {
                _isErasing = !_isErasing;
                _isPainting = false;
                _statusMessage = _isErasing ? "Erase mode ON. Click and drag to remove tiles." : "Erase mode OFF.";
                _statusType = MessageType.Info;
            }
            GUI.color = Color.white;

            // Level name
            EditorGUILayout.LabelField("Level Name:", GUILayout.Width(80));
            _levelName = EditorGUILayout.TextField(_levelName, GUILayout.Width(180));

            EditorGUILayout.EndHorizontal();

            // Row 2: Actions
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(new GUIContent("New Level", "Reset the grid to a blank 32x32 level"), GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("New Level", "Reset to a blank level? Unsaved changes will be lost.", "Yes", "Cancel"))
                {
                    InitializeGrid();
                    _statusMessage = "New level created.";
                    _statusType = MessageType.Info;
                }
            }

            if (GUILayout.Button(new GUIContent("Load JSON...", "Load a level from a JSON file in the project"), GUILayout.Width(120)))
            {
                LoadLevelFromJSON();
            }

            if (GUILayout.Button(new GUIContent("Save JSON...", "Save the current level to a JSON file"), GUILayout.Width(120)))
            {
                SaveLevelToJSON();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(new GUIContent("Validate", "Run validation checks on this level"), GUILayout.Width(100)))
            {
                ValidateLevel();
            }

            EditorGUILayout.EndHorizontal();

            // Validation results
            if (_showValidation && _validationErrors.Length > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Validation Results:", EditorStyles.boldLabel);
                foreach (string error in _validationErrors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Scrollable 32x32 grid view with colored tiles.
        /// </summary>
        private void DrawGrid()
        {
            // Calculate the scrollable area (leaving room for toolbar and status bar)
            float availableHeight = position.height - TOOLBAR_HEIGHT - STATUS_BAR_HEIGHT - 30f;
            float gridContentWidth = _gridWidth * GRID_STEP + 20f;
            float gridContentHeight = _gridDepth * GRID_STEP + 20f;

            _scrollPosition = EditorGUILayout.BeginScrollView(
                _scrollPosition,
                GUILayout.Width(position.width - 5f),
                GUILayout.Height(availableHeight)
            );

            // Axis labels
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(20), GUILayout.Height(GRID_STEP));
            for (int x = 0; x < _gridWidth && x < 32; x++)
            {
                GUILayout.Label(x.ToString(), GUILayout.Width(GRID_CELL_SIZE), GUILayout.Height(GRID_STEP));
                GUILayout.Space(GRID_SPACING);
            }
            EditorGUILayout.EndHorizontal();

            // Grid rows
            for (int z = 0; z < _gridDepth && z < 32; z++)
            {
                EditorGUILayout.BeginHorizontal();

                // Row label
                GUILayout.Label(z.ToString(), GUILayout.Width(20), GUILayout.Height(GRID_CELL_SIZE));

                // Row tiles
                for (int x = 0; x < _gridWidth && x < 32; x++)
                {
                    TileType type = _grid[x, z];
                    Color color = TileColors.GetValueOrDefault(type, Color.gray);

                    GUI.color = color;

                    string cellLabel = GetTileShortLabel(type);

                    Rect cellRect = EditorGUILayout.BeginHorizontal(GUILayout.Width(GRID_CELL_SIZE), GUILayout.Height(GRID_CELL_SIZE));

                    // Check for click and drag events
                    Event evt = Event.current;
                    if (evt.type == EventType.MouseDown || evt.type == EventType.MouseDrag)
                    {
                        if (cellRect.Contains(evt.mousePosition))
                        {
                            if (_isErasing)
                            {
                                _grid[x, z] = TileType.Floor;
                                _wallFaces[x, z] = WallFace.None;
                                Repaint();
                            }
                            else if (_isPainting || evt.type == EventType.MouseDown)
                            {
                                _grid[x, z] = _selectedTileType;
                                Repaint();
                            }
                        }
                    }

                    // Draw cell label
                    GUILayout.Label(cellLabel, new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        fontSize = 9,
                        normal = { textColor = Color.white }
                    });

                    EditorGUILayout.EndHorizontal();
                    GUI.color = Color.white;

                    GUILayout.Space(GRID_SPACING);
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(GRID_SPACING);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Status bar at the bottom.
        /// </summary>
        private void DrawStatusBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox, GUILayout.Height(STATUS_BAR_HEIGHT));

            EditorGUILayout.LabelField(new GUIContent(_statusMessage, "Current status"), GUILayout.ExpandWidth(true));

            // Grid dimensions indicator
            int walkableCount = 0;
            int wallCount = 0;
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int z = 0; z < _gridDepth; z++)
                {
                    if (_grid[x, z] == TileType.Wall || _grid[x, z] == TileType.Empty)
                        wallCount++;
                    else
                        walkableCount++;
                }
            }

            EditorGUILayout.LabelField($"Grid: {_gridWidth}x{_gridDepth}  |  Walkable: {walkableCount}  |  Walls: {wallCount}", GUILayout.Width(300));

            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Tile Display Helpers

        /// <summary>
        /// Returns a short label for a tile type (1-2 characters for grid display).
        /// </summary>
        private static string GetTileShortLabel(TileType type)
        {
            return type switch
            {
                TileType.Floor => "·",
                TileType.Wall => "█",
                TileType.Door => "D",
                TileType.Trap => "T",
                TileType.Altar => "A",
                TileType.Spawn => "S",
                TileType.Stairs => "E",
                TileType.Empty => "·",
                _ => "?"
            };
        }

        #endregion

        #region Validation

        /// <summary>
        /// Run validation and solvability checks on the current grid.
        /// </summary>
        private void ValidateLevel()
        {
            TileData[] tileData = ExportToTileData();
            ITilePalette palette = LoadTilePalette();

            var validator = new LevelValidator();

            _showValidation = true;
            bool isValid = validator.Validate(tileData, _gridWidth, _gridDepth, palette, out _validationErrors);

            if (isValid)
            {
                _isSolvable = validator.IsSolvable(tileData, _gridWidth, _gridDepth);

                _statusMessage = $"Validation PASSED. Level is {( _isSolvable ? "solvable" : "NOT solvable" )}.";
                _statusType = _isSolvable ? MessageType.Info : MessageType.Warning;

                if (!_isSolvable)
                {
                    Array.Resize(ref _validationErrors, _validationErrors.Length + 1);
                    _validationErrors[_validationErrors.Length - 1] = "Stairs (exit) cannot be reached from the Spawn point!";
                }
            }
            else
            {
                _statusMessage = $"Validation FAILED — {_validationErrors.Length} issue(s) found.";
                _statusType = MessageType.Error;
            }

            Repaint();
        }

        /// <summary>
        /// Try to find the Tile Palette asset in the project.
        /// </summary>
        private static ITilePalette LoadTilePalette()
        {
            string[] guids = AssetDatabase.FindAssets("t:TilePalette");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<TilePalette>(path);
            }

            Debug.LogWarning("[LevelEditor] No TilePalette asset found in the project. Skipping palette validation.");
            return null;
        }

        #endregion

        #region Load / Save

        /// <summary>
        /// Load a level from a JSON file in the project.
        /// </summary>
        private void LoadLevelFromJSON()
        {
            string path = EditorUtility.OpenFilePanel(
                "Load Dungeon VR Level",
                Application.dataPath + "/Data/Levels",
                "json"
            );

            if (string.IsNullOrEmpty(path))
                return;

            if (!File.Exists(path))
            {
                _statusMessage = $"File not found: {path}";
                _statusType = MessageType.Error;
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var levelWrapper = JsonUtility.FromJson<LevelDataWrapper>(json);

                if (levelWrapper == null)
                {
                    _statusMessage = "Failed to parse JSON file.";
                    _statusType = MessageType.Error;
                    return;
                }

                if (levelWrapper.schemaVersion != 1)
                {
                    _statusMessage = $"Unsupported schema version: {levelWrapper.schemaVersion}. Expected version 1.";
                    _statusType = MessageType.Error;
                    return;
                }

                _gridWidth = levelWrapper.width;
                _gridDepth = levelWrapper.depth;
                _levelName = string.IsNullOrEmpty(levelWrapper.levelName) ? "Loaded Level" : levelWrapper.levelName;
                _currentFilePath = path;

                // Initialize grid with defaults
                _grid = new TileType[_gridWidth, _gridDepth];
                _wallFaces = new WallFace[_gridWidth, _gridDepth];

                TileType defaultType = TileType.Floor;
                if (levelWrapper.defaultTile != null)
                {
                    TryParseTileType(levelWrapper.defaultTile.type, out defaultType);
                }

                // Fill with defaults
                for (int x = 0; x < _gridWidth; x++)
                {
                    for (int z = 0; z < _gridDepth; z++)
                    {
                        _grid[x, z] = defaultType;
                        _wallFaces[x, z] = WallFace.None;
                    }
                }

                // Apply explicit tiles
                if (levelWrapper.tiles != null)
                {
                    foreach (var tile in levelWrapper.tiles)
                    {
                        if (tile.x >= 0 && tile.x < _gridWidth && tile.z >= 0 && tile.z < _gridDepth)
                        {
                            TryParseTileType(tile.type, out TileType parsedType);
                            _grid[tile.x, tile.z] = parsedType;

                            if (!string.IsNullOrEmpty(tile.wallFaces))
                            {
                                TryParseWallFace(tile.wallFaces, out WallFace parsedFaces);
                                _wallFaces[tile.x, tile.z] = parsedFaces;
                            }
                        }
                    }
                }

                _statusMessage = $"Loaded: {Path.GetFileName(path)} ({_gridWidth}x{_gridDepth})";
                _statusType = MessageType.Info;
                _showValidation = false;
            }
            catch (Exception ex)
            {
                _statusMessage = $"Error loading file: {ex.Message}";
                _statusType = MessageType.Error;
            }

            Repaint();
        }

        /// <summary>
        /// Save the current level to a JSON file.
        /// </summary>
        private void SaveLevelToJSON()
        {
            string path = EditorUtility.SaveFilePanel(
                "Save Dungeon VR Level",
                Application.dataPath + "/Data/Levels",
                $"{_levelName.Replace(" ", "_")}.json",
                "json"
            );

            if (string.IsNullOrEmpty(path))
                return;

            var wrapper = new LevelDataWrapper
            {
                schemaVersion = 1,
                levelName = _levelName,
                width = _gridWidth,
                depth = _gridDepth,
                defaultTile = new DefaultTileJson { type = "floor" },
                tiles = new List<TileDataJson>()
            };

            // Only include non-floor tiles to keep the file compact
            for (int x = 0; x < _gridWidth; x++)
            {
                for (int z = 0; z < _gridDepth; z++)
                {
                    TileType type = _grid[x, z];
                    WallFace faces = _wallFaces[x, z];

                    // Skip default floor tiles
                    if (type == TileType.Floor && faces == WallFace.None)
                        continue;

                    var tileJson = new TileDataJson
                    {
                        x = x,
                        z = z,
                        type = type.ToString().ToLowerInvariant()
                    };

                    if (faces != WallFace.None)
                    {
                        tileJson.wallFaces = faces.ToString().ToLowerInvariant();
                    }

                    wrapper.tiles.Add(tileJson);
                }
            }

            try
            {
                string json = JsonUtility.ToJson(wrapper, prettyPrint: true);
                File.WriteAllText(path, json);

                // Refresh the asset database so the new file appears
                AssetDatabase.Refresh();

                _currentFilePath = path;
                _statusMessage = $"Saved: {Path.GetFileName(path)} ({wrapper.tiles.Count} non-default tiles)";
                _statusType = MessageType.Info;
            }
            catch (Exception ex)
            {
                _statusMessage = $"Error saving file: {ex.Message}";
                _statusType = MessageType.Error;
            }

            Repaint();
        }

        /// <summary>
        /// Export the current grid to a TileData array for validation.
        /// </summary>
        private TileData[] ExportToTileData()
        {
            TileData[] result = new TileData[_gridWidth * _gridDepth];
            int index = 0;

            for (int x = 0; x < _gridWidth; x++)
            {
                for (int z = 0; z < _gridDepth; z++)
                {
                    result[index++] = new TileData(x, z, _grid[x, z], _wallFaces[x, z]);
                }
            }

            return result;
        }

        #endregion

        #region JSON Serialization Classes

        [Serializable]
        private class LevelDataWrapper
        {
            public int schemaVersion = 1;
            public string levelName;
            public int width;
            public int depth;
            public DefaultTileJson defaultTile;
            public List<TileDataJson> tiles;
        }

        [Serializable]
        private class TileDataJson
        {
            public int x;
            public int z;
            public string type = "floor";
            public string wallFaces;
        }

        [Serializable]
        private class DefaultTileJson
        {
            public string type = "floor";
        }

        #endregion

        #region Parsing Utilities

        private static bool TryParseTileType(string typeStr, out TileType result)
        {
            if (string.IsNullOrEmpty(typeStr))
            {
                result = TileType.Floor;
                return false;
            }

            string normalized = char.ToUpperInvariant(typeStr[0]) + typeStr.Substring(1).ToLowerInvariant();
            return Enum.TryParse(normalized, out result);
        }

        private static bool TryParseWallFace(string faceStr, out WallFace result)
        {
            result = WallFace.None;
            if (string.IsNullOrEmpty(faceStr))
                return false;

            if (faceStr.Trim().ToLowerInvariant() == "all")
            {
                result = WallFace.All;
                return true;
            }

            string normalized = char.ToUpperInvariant(faceStr[0]) + faceStr.Substring(1).ToLowerInvariant();
            return Enum.TryParse(normalized, out result);
        }

        #endregion
    }
}
