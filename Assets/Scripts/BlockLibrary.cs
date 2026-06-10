using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LevelMaker
{
    /// <summary>
    /// Library panel showing block prefabs organized by folder tabs.
    /// Loads from Assets/Resources/BlockPrefabs/&lt;FolderName&gt;/&lt;PrefabName&gt;.prefab
    /// </summary>
    public class BlockLibrary : MonoBehaviour
    {
        [SerializeField] private LevelBuilder levelBuilder;
        [SerializeField] private string resourcesRoot = "BlockPrefabs";
        [SerializeField] private string assetsPath = "Assets/Resources/BlockPrefabs";

        // Loaded data
        private List<string> folderNames = new List<string>(); // Tab names
        private Dictionary<string, List<LibraryItem>> folderItems = new Dictionary<string, List<LibraryItem>>();
        private int currentTab = 0;
        private Vector2 scrollPos;

        // UI
        private Rect panelRect = new Rect(0, 0, 220, 400);
        private bool isVisible = true;

        public class LibraryItem
        {
            public string name;
            public string folderPath;     // Resources path, e.g. "BlockPrefabs/Basic/Cube"
            public string assetsPath;     // Assets path for icon loading
            public GameObject previewPrefab;
            public Texture2D icon;
            public PrimitiveType fallbackPrimitive = PrimitiveType.Cube;
        }

        private void Awake()
        {
            // Auto-find LevelBuilder if not assigned
            if (levelBuilder == null) levelBuilder = FindObjectOfType<LevelBuilder>();
        }

        private void Start()
        {
            if (levelBuilder == null) levelBuilder = FindObjectOfType<LevelBuilder>();
            LoadLibrary();
            PositionPanel();
        }

        public void SetLevelBuilder(LevelBuilder builder)
        {
            levelBuilder = builder;
        }

        private void PositionPanel()
        {
            // Position to the right side, below the mode buttons
            panelRect.x = Screen.width - panelRect.width - 10;
            panelRect.y = 50;
        }

        private void Update()
        {
            // No-op: UI handled by Canvas
        }

        public void LoadLibrary()
        {
            folderNames.Clear();
            folderItems.Clear();

            string fullResourcesPath = Path.Combine(Application.dataPath, "Resources", resourcesRoot);

            if (!Directory.Exists(fullResourcesPath))
            {
                Debug.LogWarning($"[BlockLibrary] Resources folder not found: {fullResourcesPath}");
                return;
            }

            // Get subfolders (these become tabs)
            string[] subDirs = Directory.GetDirectories(fullResourcesPath);
            foreach (var dir in subDirs)
            {
                string folderName = Path.GetFileName(dir);
                folderNames.Add(folderName);

                var items = new List<LibraryItem>();
                string[] prefabFiles = Directory.GetFiles(dir, "*.prefab");

                foreach (var prefabFile in prefabFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(prefabFile);
                    // Resources path: BlockPrefabs/Basic/Cube_1x1x1 (no .prefab extension)
                    string resourcesPath = $"{resourcesRoot}/{folderName}/{fileName}";

                    // Try to load from Resources
                    GameObject prefab = Resources.Load<GameObject>(resourcesPath);

                    LibraryItem item = new LibraryItem
                    {
                        name = fileName,
                        folderPath = resourcesPath,
                        assetsPath = "Assets/Resources/" + resourcesPath + ".prefab",
                        previewPrefab = prefab,
                        icon = TryLoadIcon(prefab)
                    };

                    // Try to detect primitive type from prefab name
                    item.fallbackPrimitive = GuessPrimitiveFromName(fileName);

                    items.Add(item);
                }

                folderItems[folderName] = items;
            }

            // Also scan root BlockPrefabs/ folder (for prefabs directly in root, not in subfolders)
            string[] rootPrefabs = Directory.GetFiles(fullResourcesPath, "*.prefab");
            if (rootPrefabs.Length > 0)
            {
                folderNames.Insert(0, "All");
                var allItems = new List<LibraryItem>();
                foreach (var folder in folderItems)
                {
                    allItems.AddRange(folder.Value);
                }
                folderItems["All"] = allItems;
            }

            Debug.Log($"[BlockLibrary] Loaded {folderNames.Count} tabs, {GetTotalItemCount()} items");
        }

        private int GetTotalItemCount()
        {
            int total = 0;
            foreach (var kvp in folderItems) total += kvp.Value.Count;
            return total;
        }

        public int GetItemCount() => GetTotalItemCount();
        public List<string> GetFolderNames() => folderNames;

        private PrimitiveType GuessPrimitiveFromName(string name)
        {
            string lower = name.ToLower();
            if (lower.Contains("sphere")) return PrimitiveType.Sphere;
            if (lower.Contains("cylinder") || lower.Contains("cyl")) return PrimitiveType.Cylinder;
            if (lower.Contains("capsule")) return PrimitiveType.Capsule;
            if (lower.Contains("plane") || lower.Contains("floor")) return PrimitiveType.Plane;
            if (lower.Contains("cube") || lower.Contains("block") || lower.Contains("wall")) return PrimitiveType.Cube;
            return PrimitiveType.Cube;
        }

        private Texture2D TryLoadIcon(GameObject prefab)
        {
            // Try to get a preview texture from prefab
            if (prefab == null) return null;

#if UNITY_EDITOR
            // In editor, try to get asset preview
            var preview = UnityEditor.AssetPreview.GetAssetPreview(prefab);
            if (preview != null) return preview;
#endif

            return null;
        }

        // UI is handled by LevelBuilderUI (Canvas-based). No IMGUI.
        private void OnGUI() { }

        private Color GetColorForPrimitive(PrimitiveType type)
        {
            switch (type)
            {
                case PrimitiveType.Cube: return new Color(0.7f, 0.7f, 0.7f);
                case PrimitiveType.Sphere: return new Color(0.4f, 0.7f, 1f);
                case PrimitiveType.Cylinder: return new Color(1f, 0.8f, 0.3f);
                case PrimitiveType.Capsule: return new Color(0.5f, 1f, 0.5f);
                case PrimitiveType.Plane: return new Color(0.9f, 0.9f, 0.9f);
                default: return Color.gray;
            }
        }

        private void SelectLibraryItem(LibraryItem item)
        {
            if (levelBuilder == null) return;

            // Use the fallback primitive type to set the current block
            levelBuilder.SetBlockType(item.fallbackPrimitive);

            Debug.Log($"[BlockLibrary] Selected: {item.name} (Type: {item.fallbackPrimitive})");
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;
        }

        public void Refresh()
        {
            LoadLibrary();
        }
    }
}
