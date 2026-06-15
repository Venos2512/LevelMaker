using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace LevelMaker
{
    /// <summary>
    /// Export and import level data for saving/loading level layouts
    /// </summary>
    public class LevelExporter : MonoBehaviour
    {
        [System.Serializable]
        public class BlockInfo
        {
            public string blockType;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public Vector3Int gridPosition;
            public Vector3Int gridSize;
        }

        /// <summary>
        /// Bump this when changing LevelData's serialized layout.
        /// Older saves are routed through Migrate() to stay forward-compatible.
        /// </summary>
        public const int CurrentSchemaVersion = 2;

        [System.Serializable]
        public class LevelData
        {
            public int schemaVersion = CurrentSchemaVersion;
            public string levelName;
            public string creationDate;
            public string lastModified;
            public string author;
            public string description;
            public List<string> tags = new List<string>();
            public string thumbnailPath; // relative to the level file (e.g. "thumb.png")
            public List<BlockInfo> blocks = new List<BlockInfo>();
        }

        /// <summary>
        /// Information about a saved level file on disk.
        /// Used by the runtime UI to populate the level list panel.
        /// </summary>
        [System.Serializable]
        public class LevelFileInfo
        {
            public string fileName;       // name without extension
            public string fullPath;       // absolute path
            public string displayName;    // friendly name (may include date suffix)
            public long fileSize;         // bytes
            public System.DateTime modifiedTime;
        }

        // Folder used at runtime for save/import. Persistent so it works in both
        // Editor playmode and standalone builds (Assets/ is read-only in builds).
        public static string GetDefaultLevelsFolder()
        {
            return Path.Combine(Application.persistentDataPath, "Levels");
        }

        /// <summary>
        /// Create the default levels folder if it doesn't exist.
        /// </summary>
        public static void EnsureLevelsFolder()
        {
            string folder = GetDefaultLevelsFolder();
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        /// <summary>
        /// Scan the given folder (or the default one) for *.json level files.
        /// Sorted by modification time, newest first.
        /// </summary>
        public static List<LevelFileInfo> ListSavedLevels(string folderPath = null)
        {
            var result = new List<LevelFileInfo>();
            string folder = string.IsNullOrEmpty(folderPath) ? GetDefaultLevelsFolder() : folderPath;
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return result;

            foreach (var file in Directory.GetFiles(folder, "*.json"))
            {
                try
                {
                    var fi = new FileInfo(file);
                    result.Add(new LevelFileInfo
                    {
                        fileName = Path.GetFileNameWithoutExtension(file),
                        fullPath = file,
                        displayName = Path.GetFileNameWithoutExtension(file),
                        fileSize = fi.Length,
                        modifiedTime = fi.LastWriteTime
                    });
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[LevelExporter] Skipping unreadable file '{file}': {e.Message}");
                }
            }

            result.Sort((a, b) => b.modifiedTime.CompareTo(a.modifiedTime));
            return result;
        }

        public static LevelData ExportLevel(GridManager gridManager, string levelName = "Level",
            string author = null, string description = null, List<string> tags = null)
        {
            LevelData data = new LevelData
            {
                schemaVersion = CurrentSchemaVersion,
                levelName = levelName,
                creationDate = System.DateTime.Now.ToString("o"),
                lastModified = System.DateTime.Now.ToString("o"),
                author = string.IsNullOrEmpty(author) ? "" : author,
                description = string.IsNullOrEmpty(description) ? "" : description,
                tags = tags ?? new List<string>()
            };

            var allBlocks = gridManager.GetAllBlocks();
            foreach (var kvp in allBlocks)
            {
                GameObject obj = kvp.Key;
                BlockMetadata metadata = kvp.Value;

                BlockInfo info = new BlockInfo
                {
                    blockType = metadata.blockType,
                    position = obj.transform.position,
                    rotation = obj.transform.rotation,
                    scale = obj.transform.localScale,
                    gridPosition = metadata.gridPosition,
                    gridSize = metadata.gridSize
                };
                data.blocks.Add(info);
            }

            return data;
        }

        public static void SaveLevelToFile(LevelData data, string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"[LevelExporter] Level saved to: {filePath}");
        }

        public static LevelData LoadLevelFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return null;
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"[LevelExporter] Level file not found: {filePath}");
                return null;
            }
            try
            {
                string json = File.ReadAllText(filePath);
                var data = JsonUtility.FromJson<LevelData>(json);
                if (data == null) return null;
                return Migrate(data);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LevelExporter] Failed to load level '{filePath}': {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Bring older schemas up to the current version. Each step handles one
        /// bump. New fields are filled with safe defaults so the rest of the
        /// code can rely on them being non-null.
        /// </summary>
        public static LevelData Migrate(LevelData data)
        {
            if (data == null) return null;
            if (data.schemaVersion < 1) data.schemaVersion = 1;
            if (data.schemaVersion < 2)
            {
                // v1 -> v2: added author/description/tags/lastModified/thumbnailPath
                if (data.author == null) data.author = "";
                if (data.description == null) data.description = "";
                if (data.tags == null) data.tags = new List<string>();
                if (string.IsNullOrEmpty(data.lastModified)) data.lastModified = data.creationDate;
                if (data.thumbnailPath == null) data.thumbnailPath = "";
                data.schemaVersion = 2;
            }
            // Always keep these non-null even if loaded from a file with missing fields
            if (data.author == null) data.author = "";
            if (data.description == null) data.description = "";
            if (data.tags == null) data.tags = new List<string>();
            if (data.thumbnailPath == null) data.thumbnailPath = "";
            if (data.blocks == null) data.blocks = new List<BlockInfo>();
            return data;
        }

        /// <summary>
        /// Remove every block currently tracked by the grid manager.
        /// Used before importing so the new level replaces the old one cleanly.
        /// </summary>
        public static void ClearLevel(GridManager gridManager)
        {
            if (gridManager == null) return;

            var blocks = gridManager.GetAllBlocks();
            foreach (var kvp in blocks)
            {
                GameObject obj = kvp.Key;
                if (obj == null) continue;

                gridManager.FreeCells(obj);
                if (Application.isPlaying) Object.Destroy(obj);
                else Object.DestroyImmediate(obj);
            }
            gridManager.ClearOccupiedCells();
        }

        /// <summary>
        /// Clear the current level, then re-create all blocks from the saved data.
        /// Returns the container GameObject holding the imported blocks, or null on failure.
        /// </summary>
        public static GameObject ImportLevel(LevelData data, GridManager gridManager)
        {
            if (data == null)
            {
                Debug.LogWarning("[LevelExporter] ImportLevel called with null data");
                return null;
            }
            if (gridManager == null)
            {
                Debug.LogWarning("[LevelExporter] ImportLevel called with null GridManager");
                return null;
            }

            // Replace any existing layout
            ClearLevel(gridManager);

            GameObject levelContainer = new GameObject($"Imported_{data.levelName}");
            levelContainer.transform.SetParent(gridManager.transform);

            int imported = 0;
            int failed = 0;
            foreach (var blockInfo in data.blocks)
            {
                GameObject block = CreateBlockFromInfo(blockInfo, gridManager);
                if (block != null)
                {
                    block.transform.SetParent(gridManager.transform);
                    imported++;
                }
                else
                {
                    failed++;
                }
            }

            Debug.Log($"[LevelExporter] Level imported: '{data.levelName}' ({imported} blocks, {failed} failed)");
            return levelContainer;
        }

        private static GameObject CreateBlockFromInfo(BlockInfo info, GridManager gridManager)
        {
            // Try to load as a prefab first - covers anything stored as a custom block
            // (e.g. "Ball_1x1x1", "Pillar_1x2x1") saved from the block library.
            GameObject prefab = TryLoadPrefabByName(info.blockType);
            GameObject obj;

            if (prefab != null)
            {
                // For prefabs, instantiate and only apply position/rotation.
                // The prefab carries its own localScale; the saved scale is ignored
                // so a future prefab edit doesn't get flattened by a stale import.
                obj = Object.Instantiate(prefab);
                obj.name = info.blockType;
                obj.transform.position = info.position;
                obj.transform.rotation = info.rotation;
            }
            else
            {
                // Fallback: create a primitive using the saved type and apply the
                // saved transform verbatim. This keeps legacy exports loadable.
                PrimitiveType primitiveType = ParsePrimitiveType(info.blockType);
                obj = GameObject.CreatePrimitive(primitiveType);
                obj.name = string.IsNullOrEmpty(info.blockType) ? "Cube" : info.blockType;
                obj.transform.position = info.position;
                obj.transform.rotation = info.rotation;
                obj.transform.localScale = info.scale;
            }

            gridManager.OccupyCells(info.gridPosition, info.gridSize, obj, info.blockType);
            return obj;
        }

        private static PrimitiveType ParsePrimitiveType(string blockType)
        {
            if (string.IsNullOrEmpty(blockType)) return PrimitiveType.Cube;
            switch (blockType.ToLower())
            {
                case "cube": return PrimitiveType.Cube;
                case "sphere": return PrimitiveType.Sphere;
                case "cylinder": return PrimitiveType.Cylinder;
                case "capsule": return PrimitiveType.Capsule;
                case "plane": return PrimitiveType.Plane;
                default: return PrimitiveType.Cube;
            }
        }

        /// <summary>
        /// Look up a prefab by display name across all Resources/BlockPrefabs subfolders.
        /// Returns null if no match - caller falls back to primitive creation.
        /// </summary>
        private static GameObject TryLoadPrefabByName(string blockType)
        {
            if (string.IsNullOrEmpty(blockType)) return null;
            if (IsPrimitiveName(blockType)) return null; // primitives aren't in Resources

            // Scan the whole BlockPrefabs tree once per import - cheap (a few prefabs)
            var allPrefabs = Resources.LoadAll<GameObject>("BlockPrefabs");
            foreach (var p in allPrefabs)
            {
                if (p != null && p.name == blockType) return p;
            }
            return null;
        }

        private static bool IsPrimitiveName(string blockType)
        {
            switch ((blockType ?? "").ToLower())
            {
                case "cube":
                case "sphere":
                case "cylinder":
                case "capsule":
                case "plane":
                    return true;
                default:
                    return false;
            }
        }

        // ============== metadata helpers ==============

        /// <summary>
        /// Validate that a LevelData has the minimum required fields populated.
        /// Used by the level list to flag broken saves.
        /// </summary>
        public static bool IsValid(LevelData data)
        {
            if (data == null) return false;
            if (string.IsNullOrEmpty(data.levelName)) return false;
            if (data.blocks == null) return false;
            return true;
        }

        /// <summary>
        /// Format a metadata block for display in tooltips / list rows.
        /// </summary>
        public static string FormatMetadataSummary(LevelData data)
        {
            if (data == null) return "(empty)";
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(data.author)) parts.Add($"by {data.author}");
            if (!string.IsNullOrEmpty(data.description)) parts.Add(data.description);
            if (data.tags != null && data.tags.Count > 0) parts.Add("[" + string.Join(", ", data.tags) + "]");
            if (parts.Count == 0) return $"{data.blocks.Count} blocks";
            return string.Join(" - ", parts);
        }
    }
}
