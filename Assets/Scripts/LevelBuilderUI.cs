using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace LevelMaker
{
    /// <summary>
    /// UGUI-based UI for LevelBuilder: mode buttons, debug panel, library panel
    /// Attach to a Canvas in the scene with buttons pre-wired via inspector
    /// </summary>
    public class LevelBuilderUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelBuilder levelBuilder;
        [SerializeField] private BlockLibrary blockLibrary;

        [Header("UI References")]
        [SerializeField] private Button buildButton;
        [SerializeField] private Button eraseButton;
        [SerializeField] private Button selectButton;
        [SerializeField] private Text currentModeText;
        [SerializeField] private Text debugText;
        [SerializeField] private Text currentSelectionText;
        [SerializeField] private GameObject libraryPanel;
        [SerializeField] private Transform libraryContent;
        [SerializeField] private GameObject libraryItemPrefab;
        [SerializeField] private Text undoCountText;
        [SerializeField] private Toggle libraryToggle;

        [Header("Settings")]
        [SerializeField] private string resourcesRoot = "BlockPrefabs";
        [SerializeField] private Color selectedItemColor = new Color(0.3f, 0.7f, 0.3f, 0.9f);
        [SerializeField] private Color normalItemColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // Currently selected library item (for visual highlight)
        private GameObject selectedItemObj;
        private Image selectedItemBg;
        private string currentlySelectedItemName;

        private void Start()
        {
            // Disable buttons until wiring is complete
            if (buildButton != null) buildButton.interactable = false;
            if (eraseButton != null) eraseButton.interactable = false;
            if (selectButton != null) selectButton.interactable = false;

            // Defer wiring to next frame to ensure all references are set (especially when auto-created at runtime)
            StartCoroutine(WireUpNextFrame());
        }

        private System.Collections.IEnumerator WireUpNextFrame()
        {
            yield return null; // Wait one frame

            if (levelBuilder == null) levelBuilder = FindObjectOfType<LevelBuilder>();
            if (blockLibrary == null) blockLibrary = FindObjectOfType<BlockLibrary>();

            // Wire up button events with null checks
            if (buildButton != null)
            {
                buildButton.onClick.AddListener(() => {
                    if (levelBuilder != null) levelBuilder.SetMode(LevelBuilder.BuildMode.Build);
                });
                buildButton.interactable = true;
            }
            if (eraseButton != null)
            {
                eraseButton.onClick.AddListener(() => {
                    if (levelBuilder != null) levelBuilder.SetMode(LevelBuilder.BuildMode.Erase);
                });
                eraseButton.interactable = true;
            }
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(() => {
                    if (levelBuilder != null) levelBuilder.SetMode(LevelBuilder.BuildMode.Select);
                });
                selectButton.interactable = true;
            }

            // Auto-build library if libraryContent is set
            if (libraryContent != null && libraryItemPrefab != null)
            {
                BuildLibraryUI();
            }
        }

        private void Update()
        {
            UpdateDebug();
        }

        private void UpdateDebug()
        {
            if (debugText != null && levelBuilder != null)
            {
                var mode = levelBuilder.GetMode();
                int itemCount = blockLibrary != null ? blockLibrary.GetItemCount() : 0;
                debugText.text = $"Mode: {mode} | Library items: {itemCount}";
            }

            if (undoCountText != null && levelBuilder != null)
            {
                undoCountText.text = $"Undo: {levelBuilder.GetUndoCount()}";
            }

            if (currentSelectionText != null && levelBuilder != null)
            {
                currentSelectionText.text = $"Selected: {levelBuilder.GetBlockTypeName()}";
            }
        }

        /// <summary>
        /// Auto-generate library UI from Resources/BlockPrefabs/
        /// </summary>
        public void BuildLibraryUI()
        {
            if (libraryContent == null || libraryItemPrefab == null) return;

            // Clear existing
            foreach (Transform child in libraryContent)
            {
                Destroy(child.gameObject);
            }

            // Reset selection state
            selectedItemObj = null;
            selectedItemBg = null;
            currentlySelectedItemName = null;
            firstItemInfo = null;

            string fullPath = Path.Combine(Application.dataPath, "Resources", resourcesRoot);
            if (!Directory.Exists(fullPath)) return;

            // Scan subfolders
            foreach (var dir in Directory.GetDirectories(fullPath))
            {
                string folderName = Path.GetFileName(dir);
                CreateFolderSection(folderName, dir);
            }

            // Also add root prefabs
            string[] rootPrefabs = Directory.GetFiles(fullPath, "*.prefab");
            if (rootPrefabs.Length > 0)
            {
                CreateFolderSection("Root", fullPath);
            }

            // Auto-select first item as default (always have 1 selected)
            if (firstItemInfo.HasValue)
            {
                var info = firstItemInfo.Value;
                if (info.itemObj != null)
                {
                    OnLibraryItemClicked(info.itemName, info.prefabPath, info.itemObj, info.itemBg);
                }
            }
        }

        private struct FirstItemInfo
        {
            public string itemName;
            public string prefabPath;
            public GameObject itemObj;
            public Image itemBg;
        }
        private FirstItemInfo? firstItemInfo;

        private void CreateFolderSection(string folderName, string folderPath)
        {
            // Create header
            GameObject headerObj = new GameObject($"Header_{folderName}");
            headerObj.transform.SetParent(libraryContent, false);
            Text headerText = headerObj.AddComponent<Text>();
            headerText.text = $"[{folderName}]";
            headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headerText.fontSize = 14;
            headerText.color = Color.cyan;
            headerText.fontStyle = FontStyle.Bold;
            headerObj.AddComponent<LayoutElement>().preferredHeight = 22;

            // Create items
            foreach (var prefabFile in Directory.GetFiles(folderPath, "*.prefab"))
            {
                string fileName = Path.GetFileNameWithoutExtension(prefabFile);
                CreateLibraryItem(fileName, prefabFile);
            }
        }

        private void CreateLibraryItem(string itemName, string prefabPath)
        {
            GameObject itemObj = Instantiate(libraryItemPrefab, libraryContent);
            itemObj.name = $"Item_{itemName}";
            itemObj.SetActive(true);

            Text nameText = itemObj.GetComponentInChildren<Text>();
            if (nameText != null) nameText.text = itemName;

            // Make the whole item clickable (not just the "Use" button)
            Image itemBg = itemObj.GetComponent<Image>();
            Button itemButton = itemObj.GetComponent<Button>();
            if (itemButton == null)
            {
                itemButton = itemObj.AddComponent<Button>();
                itemButton.targetGraphic = itemBg;
            }
            // Remove ALL child buttons (e.g. "Use" button from template) to prevent double-click
            Button[] allButtons = itemObj.GetComponentsInChildren<Button>(true);
            foreach (var btn in allButtons)
            {
                if (btn.gameObject != itemObj)
                {
                    btn.onClick.RemoveAllListeners(); // Remove any old listeners
                    Destroy(btn.gameObject); // Destroy the child button entirely
                }
            }

            // Wire click handler - pass prefabPath for loading
            string capturedName = itemName;
            string capturedPath = prefabPath;
            Image capturedBg = itemBg;
            itemButton.onClick.AddListener(() => OnLibraryItemClicked(capturedName, capturedPath, itemObj, capturedBg));

            // Track first item for auto-select default
            if (!firstItemInfo.HasValue)
            {
                firstItemInfo = new FirstItemInfo
                {
                    itemName = itemName,
                    prefabPath = prefabPath,
                    itemObj = itemObj,
                    itemBg = itemBg
                };
            }
        }

        private void OnLibraryItemClicked(string itemName, string prefabPath, GameObject itemObj, Image itemBg)
        {
            if (levelBuilder == null)
            {
                Debug.LogWarning("[LevelBuilderUI] levelBuilder is null!");
                return;
            }

            // Always have 1 item selected - clicking same item does nothing (no toggle-off)
            if (currentlySelectedItemName == itemName)
            {
                return;
            }

            // Deselect previous
            if (selectedItemBg != null)
            {
                selectedItemBg.color = normalItemColor;
            }

            // Try to load the prefab from Resources (path like "BlockPrefabs/Basic/Ball_1x1x1")
            GameObject prefab = null;
            string resourcesPath = null;
            if (!string.IsNullOrEmpty(prefabPath))
            {
                prefab = LoadPrefabFresh(prefabPath, out resourcesPath);
            }

            // Set on LevelBuilder - either prefab or fallback primitive
            if (prefab != null)
            {
                PrimitiveType fallbackType = GuessPrimitiveFromName(itemName);
                levelBuilder.SetPrefab(prefab, resourcesPath, fallbackType);
                Debug.Log($"[LevelBuilderUI] Selected prefab: {itemName} (path: {prefabPath})");
            }
            else
            {
                // Fallback: use primitive type guessed from name
                PrimitiveType type = GuessPrimitiveFromName(itemName);
                levelBuilder.SetBlockType(type);
                Debug.Log($"[LevelBuilderUI] Selected primitive: {itemName} (Type: {type}) - prefab not found");
            }

            // Highlight new selection
            if (itemBg != null) itemBg.color = selectedItemColor;
            selectedItemObj = itemObj;
            selectedItemBg = itemBg;
            currentlySelectedItemName = itemName;
        }

        private PrimitiveType GuessPrimitiveFromName(string name)
        {
            string lower = name.ToLower();
            if (lower.Contains("sphere") || lower.Contains("ball") || lower.Contains("lamp")) return PrimitiveType.Sphere;
            if (lower.Contains("cylinder") || lower.Contains("pillar") || lower.Contains("column")) return PrimitiveType.Cylinder;
            if (lower.Contains("capsule")) return PrimitiveType.Capsule;
            if (lower.Contains("plane") || lower.Contains("floor")) return PrimitiveType.Plane;
            return PrimitiveType.Cube;
        }

        /// <summary>
        /// Load prefab from Assets path, forcing fresh load in Editor (bypassing Resources cache).
        /// Returns the loaded GameObject prefab and the corresponding Resources path.
        /// </summary>
        private GameObject LoadPrefabFresh(string inputPath, out string resourcesPath)
        {
            resourcesPath = null;
            if (string.IsNullOrEmpty(inputPath)) return null;

            // Convert to Assets-relative path
            // Input might be: "E:\Horus\LevelMaker\Assets\Resources\BlockPrefabs\Basic\Ball_1x1x1.prefab"
            // or: "Assets/Resources/BlockPrefabs/Basic/Ball_1x1x1.prefab"
            string assetsPath = inputPath.Replace('\\', '/');
            const string projectRoot = "Assets/";
            int idx = assetsPath.IndexOf(projectRoot);
            if (idx >= 0)
            {
                assetsPath = assetsPath.Substring(idx);
            }
            else
            {
                Debug.LogWarning($"[LevelBuilderUI] Path does not contain 'Assets/': {inputPath}");
                return null;
            }

            // Convert Assets path to Resources path
            // "Assets/Resources/BlockPrefabs/Basic/Ball_1x1x1.prefab" -> "BlockPrefabs/Basic/Ball_1x1x1"
            resourcesPath = assetsPath;
            const string prefix = "Assets/Resources/";
            if (resourcesPath.StartsWith(prefix))
            {
                resourcesPath = resourcesPath.Substring(prefix.Length);
            }
            if (resourcesPath.EndsWith(".prefab"))
            {
                resourcesPath = resourcesPath.Substring(0, resourcesPath.Length - 7);
            }

            Debug.Log($"[LevelBuilderUI.LoadPrefabFresh] assetsPath='{assetsPath}', resourcesPath='{resourcesPath}'");

            // Use bridge for fresh load in Editor (bypasses Resources cache)
            GameObject fresh = PrefabLoaderBridge.LoadPrefabFresh(assetsPath);
            if (fresh != null) return fresh;

            // Fallback: load from Resources (may use cached version)
            return Resources.Load<GameObject>(resourcesPath);
        }

        // Public API for buttons
        public void OnBuildButtonClicked() { if (levelBuilder != null) levelBuilder.SetMode(LevelBuilder.BuildMode.Build); }
        public void OnEraseButtonClicked() { if (levelBuilder != null) levelBuilder.SetMode(LevelBuilder.BuildMode.Erase); }
        public void OnSelectButtonClicked() { if (levelBuilder != null) levelBuilder.SetMode(LevelBuilder.BuildMode.Select); }
    }
}
