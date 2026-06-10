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
                    OnLibraryItemClicked(info.itemName, info.itemObj, info.itemBg);
                }
            }
        }

        private struct FirstItemInfo
        {
            public string itemName;
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
            // Disable the "Use" child button if exists - whole item is clickable
            Button useButton = itemObj.GetComponentInChildren<Button>();
            if (useButton != null && useButton.gameObject != itemObj)
            {
                useButton.gameObject.SetActive(false);
            }

            // Wire click handler
            string capturedName = itemName;
            Image capturedBg = itemBg;
            itemButton.onClick.AddListener(() => OnLibraryItemClicked(capturedName, itemObj, capturedBg));

            // Track first item for auto-select default
            if (!firstItemInfo.HasValue)
            {
                firstItemInfo = new FirstItemInfo
                {
                    itemName = itemName,
                    itemObj = itemObj,
                    itemBg = itemBg
                };
            }
        }

        private void OnLibraryItemClicked(string itemName, GameObject itemObj, Image itemBg)
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

            // Select new
            PrimitiveType type = GuessPrimitiveFromName(itemName);
            levelBuilder.SetBlockType(type);

            // Highlight new selection
            if (itemBg != null) itemBg.color = selectedItemColor;
            selectedItemObj = itemObj;
            selectedItemBg = itemBg;
            currentlySelectedItemName = itemName;

            Debug.Log($"[LevelBuilderUI] Selected: {itemName} (Type: {type})");
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

        // Public API for buttons
        public void OnBuildButtonClicked() { if (levelBuilder != null) levelBuilder.SetMode(LevelBuilder.BuildMode.Build); }
        public void OnEraseButtonClicked() { if (levelBuilder != null) levelBuilder.SetMode(LevelBuilder.BuildMode.Erase); }
        public void OnSelectButtonClicked() { if (levelBuilder != null) levelBuilder.SetMode(LevelBuilder.BuildMode.Select); }
    }
}
