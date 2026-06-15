using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace LevelMaker
{
    /// <summary>
    /// UGUI-based UI for LevelBuilder.
    /// Owns: top bar (mode + save/load), block library (collapsible, search, grid view),
    /// debug panel (compact), saved-levels panel, toast + confirm dialog hookups.
    ///
    /// Attach to a Canvas in the scene with buttons pre-wired via inspector, or
    /// run Tools > Level Maker > Reset Level Builder UI to rebuild from scratch.
    /// </summary>
    public class LevelBuilderUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelBuilder levelBuilder;
        [SerializeField] private BlockLibrary blockLibrary;

        [Header("Top Bar")]
        [SerializeField] private Button buildButton;
        [SerializeField] private Button eraseButton;
        [SerializeField] private Button selectButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button libraryToggleButton;
        [SerializeField] private Button helpButton;
        [SerializeField] private Button settingsButton;

        [Header("Block Library Panel")]
        [SerializeField] private GameObject libraryPanel;
        [SerializeField] private InputField librarySearchInput;
        [SerializeField] private Transform libraryTabsRow;
        [SerializeField] private Transform libraryGridContent;
        [SerializeField] private GameObject libraryItemPrefab;
        [SerializeField] private GameObject libraryTabPrefab;
        [SerializeField] private Button libraryCloseButton;
        [SerializeField] private Text libraryHeaderText;

        [Header("Debug Panel (compact)")]
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private Text debugCompactText;
        [SerializeField] private Button debugExpandButton;
        [SerializeField] private GameObject debugExpandedGroup;
        [SerializeField] private Text modeText;
        [SerializeField] private Text undoCountText;
        [SerializeField] private Text currentLevelNameText;
        [SerializeField] private Text currentSelectionText;
        [SerializeField] private Text helpText;

        [Header("Saved Levels Panel")]
        [SerializeField] private GameObject levelListPanel;
        [SerializeField] private InputField levelListSearchInput;
        [SerializeField] private Transform levelListContent;
        [SerializeField] private GameObject levelListItemPrefab;
        [SerializeField] private Button closeLevelListButton;
        [SerializeField] private Text levelListHeaderText;

        [Header("Help Panel")]
        [SerializeField] private GameObject helpPanel;
        [SerializeField] private Text helpPanelText;
        [SerializeField] private Button helpCloseButton;

        [Header("Toast + Confirm (auto-found)")]
        [SerializeField] private ToastUI toastUI;
        [SerializeField] private ConfirmDialog confirmDialog;

        [Header("Import/Export Settings")]
        [SerializeField] private string defaultLevelPrefix = "Level";
        [SerializeField] private Color levelItemColor = new Color(0.2f, 0.25f, 0.4f, 0.9f);
        [SerializeField] private Color selectedItemColor = new Color(0.3f, 0.7f, 0.3f, 0.9f);
        [SerializeField] private Color normalItemColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        // ============== State ==============

        private GameObject selectedItemObj;
        private Image selectedItemBg;
        private string currentlySelectedItemName;
        private string currentCategory = "All";
        private bool debugExpanded = false;
        private string currentLevelPath; // path of last loaded/saved level (for save-in-place)

        // All discovered blocks (cached from providers)
        private List<BlockDefinition> allBlocks = new List<BlockDefinition>();
        private List<string> categories = new List<string>();
        private string currentCategoryFilter = "All";

        // Pagination for the library grid. Rendering 1158+ blocks at once
        // tanks scroll perf and makes the cell text unreadable, so we show
        // chunks and let the user "Show more" for the next batch.
        private const int LIBRARY_PAGE_SIZE = 60;
        private int libraryVisibleCount = LIBRARY_PAGE_SIZE;

        // ============== Lifecycle ==============

        private void Start()
        {
            if (buildButton != null) buildButton.interactable = false;
            if (eraseButton != null) eraseButton.interactable = false;
            if (selectButton != null) selectButton.interactable = false;
            if (saveButton != null) saveButton.interactable = false;
            if (loadButton != null) loadButton.interactable = false;
            if (libraryPanel != null) libraryPanel.SetActive(false);
            if (levelListPanel != null) levelListPanel.SetActive(false);
            if (helpPanel != null) helpPanel.SetActive(false);
            if (debugExpandedGroup != null) debugExpandedGroup.SetActive(false);

            StartCoroutine(WireUpNextFrame());
        }

        private System.Collections.IEnumerator WireUpNextFrame()
        {
            yield return null;

            if (levelBuilder == null) levelBuilder = FindObjectOfType<LevelBuilder>();
            if (blockLibrary == null) blockLibrary = FindObjectOfType<BlockLibrary>();
            if (toastUI == null) toastUI = FindObjectOfType<ToastUI>();
            if (confirmDialog == null) confirmDialog = FindObjectOfType<ConfirmDialog>();

            WireTopBar();
            WireDebugPanel();
            WireLibraryPanel();
            WireLevelListPanel();
            WireHelpPanel();

            if (levelBuilder != null)
            {
                levelBuilder.OnModifiedChanged += OnLevelBuilderModifiedChanged;
                levelBuilder.OnHelpToggled += OnLevelBuilderHelpToggled;
            }

            RefreshModifiedIndicator();
            UpdateHelpText();

            if (libraryContentOrGrid() != null && libraryItemPrefab != null)
            {
                BuildLibraryUI();
            }
        }

        private Transform libraryContentOrGrid() => libraryGridContent != null ? libraryGridContent : libraryPanel?.transform.Find("Content");

        private void OnDestroy()
        {
            if (levelBuilder != null)
            {
                levelBuilder.OnModifiedChanged -= OnLevelBuilderModifiedChanged;
                levelBuilder.OnHelpToggled -= OnLevelBuilderHelpToggled;
            }
        }

        // ============== Top bar wiring ==============

        private void WireTopBar()
        {
            if (buildButton != null)
            {
                buildButton.onClick.AddListener(() => levelBuilder?.SetMode(LevelBuilder.BuildMode.Build));
                buildButton.interactable = true;
            }
            if (eraseButton != null)
            {
                eraseButton.onClick.AddListener(() => levelBuilder?.SetMode(LevelBuilder.BuildMode.Erase));
                eraseButton.interactable = true;
            }
            if (selectButton != null)
            {
                selectButton.onClick.AddListener(() => levelBuilder?.SetMode(LevelBuilder.BuildMode.Select));
                selectButton.interactable = true;
            }
            if (saveButton != null)
            {
                saveButton.onClick.AddListener(OnSaveButtonClicked);
                saveButton.interactable = true;
            }
            if (loadButton != null)
            {
                loadButton.onClick.AddListener(OnLoadButtonClicked);
                loadButton.interactable = true;
            }
            if (libraryToggleButton != null)
            {
                libraryToggleButton.onClick.AddListener(ToggleLibraryPanel);
            }
            if (helpButton != null)
            {
                helpButton.onClick.AddListener(() => levelBuilder?.ToggleHelp());
            }
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }
        }

        private void ToggleLibraryPanel()
        {
            if (libraryPanel == null) return;
            bool willOpen = !libraryPanel.activeSelf;
            libraryPanel.SetActive(willOpen);
            if (willOpen) BuildLibraryUI();
        }

        private void OnSettingsClicked()
        {
            // Placeholder for future settings menu (grid size, theme, etc.)
            ToastUI.Info("Settings menu - coming soon");
        }

        // ============== Debug panel ==============

        private void WireDebugPanel()
        {
            if (debugExpandButton != null)
            {
                debugExpandButton.onClick.AddListener(ToggleDebugExpanded);
            }
        }

        private void ToggleDebugExpanded()
        {
            debugExpanded = !debugExpanded;
            if (debugExpandedGroup != null) debugExpandedGroup.SetActive(debugExpanded);
        }

        private void Update()
        {
            UpdateDebugCompactText();
            UpdateModeAndSelectionTexts();
        }

        private void UpdateDebugCompactText()
        {
            if (debugCompactText == null || levelBuilder == null) return;
            string mode = levelBuilder.GetMode().ToString();
            int blockCount = 0;
            if (levelBuilder != null)
            {
                var grid = FindObjectOfType<GridManager>();
                if (grid != null) blockCount = grid.GetAllBlocks().Count;
            }
            int selectionCount = levelBuilder.GetSelectionCount();
            string modMark = levelBuilder.IsModified ? "*" : "";
            debugCompactText.text = $"Mode: {mode} | Blocks: {blockCount} | Sel: {selectionCount} {modMark}";
        }

        private void UpdateModeAndSelectionTexts()
        {
            if (levelBuilder == null) return;
            if (modeText != null) modeText.text = $"Mode: {levelBuilder.GetMode()}";
            if (undoCountText != null) undoCountText.text = $"Undo: {levelBuilder.GetUndoCount()}";
            if (currentSelectionText != null) currentSelectionText.text = $"Selected: {levelBuilder.GetBlockTypeName()}";
        }

        private void RefreshModifiedIndicator()
        {
            // Asterisk on save button label
            if (saveButton != null)
            {
                var label = saveButton.GetComponentInChildren<Text>();
                if (label != null)
                {
                    string name = levelBuilder != null && levelBuilder.IsModified ? "SAVE *" : "SAVE";
                    label.text = name;
                }
            }
        }

        private void OnLevelBuilderModifiedChanged()
        {
            RefreshModifiedIndicator();
            if (levelBuilder != null && levelBuilder.IsModified && currentLevelNameText != null)
            {
                // Only update the level name if it isn't already showing the loaded name
            }
        }

        private void OnLevelBuilderHelpToggled()
        {
            if (helpPanel != null && levelBuilder != null) helpPanel.SetActive(levelBuilder.ShowHelp);
        }

        // ============== Block library ==============

        private void WireLibraryPanel()
        {
            if (librarySearchInput != null)
            {
                librarySearchInput.onValueChanged.AddListener(_ => BuildLibraryUI());
            }
            if (libraryCloseButton != null)
            {
                libraryCloseButton.onClick.AddListener(() => { if (libraryPanel != null) libraryPanel.SetActive(false); });
            }
        }

        /// <summary>
        /// Rebuild the library from registered providers, applying current search
        /// and category filter. Safe to call from a button event.
        /// </summary>
        public void BuildLibraryUI()
        {
            var grid = libraryGridContent;
            if (grid == null || libraryItemPrefab == null) return;

            // Discover blocks + categories on first build or after provider changes
            if (allBlocks.Count == 0)
            {
                RefreshFromProviders();
            }

            // Reset visible count when search/category changes so the user
            // doesn't end up with a half-rendered previous filter.
            string search = librarySearchInput != null ? librarySearchInput.text : "";
            // Detect a filter change by re-resolving each time; cheaper to just
            // reset to page-size whenever a new search/category query comes in.
            if (lastLibraryFilterKey != currentFilterKey(search))
            {
                lastLibraryFilterKey = currentFilterKey(search);
                libraryVisibleCount = LIBRARY_PAGE_SIZE;
            }

            // Apply search + category
            IEnumerable<BlockDefinition> filtered = allBlocks;
            if (!string.IsNullOrEmpty(currentCategoryFilter) && currentCategoryFilter != "All")
            {
                filtered = filtered.Where(b => b.category == currentCategoryFilter);
            }
            if (!string.IsNullOrEmpty(search))
            {
                string s = search.ToLower();
                filtered = filtered.Where(b =>
                    (b.displayName?.ToLower().Contains(s) ?? false) ||
                    (b.id?.ToLower().Contains(s) ?? false) ||
                    (b.category?.ToLower().Contains(s) ?? false));
            }

            var filteredList = filtered.ToList();
            int totalCount = filteredList.Count;

            if (libraryHeaderText != null)
            {
                int showing = Mathf.Min(libraryVisibleCount, totalCount);
                libraryHeaderText.text = totalCount == 0
                    ? "BLOCKS"
                    : (totalCount > showing
                        ? $"BLOCKS (showing {showing}/{totalCount})"
                        : $"BLOCKS ({totalCount})");
            }

            // Clear existing items
            foreach (Transform child in grid) Destroy(child.gameObject);

            // Render only the visible slice
            int toShow = Mathf.Min(libraryVisibleCount, totalCount);
            for (int i = 0; i < toShow; i++)
            {
                CreateLibraryItem(filteredList[i]);
            }

            // "Show more" button if there are more results
            if (toShow < totalCount)
            {
                CreateShowMoreButton(totalCount - toShow);
            }
        }

        private string lastLibraryFilterKey = null;
        private string currentFilterKey(string search)
        {
            return $"{currentCategoryFilter}|{search}";
        }

        private void CreateShowMoreButton(int remaining)
        {
            var grid = libraryGridContent;
            if (grid == null) return;

            // Reuse the item template but with text instead of a click handler.
            // Simpler: just create a dedicated button GameObject on the fly.
            GameObject btnObj = new GameObject("ShowMoreButton");
            btnObj.transform.SetParent(grid, false);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(0.20f, 0.35f, 0.55f, 0.9f);
            // Span the whole row of the grid by sizing via LayoutElement.
            LayoutElement le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 32;
            le.preferredWidth = 200; // ignored by GridLayoutGroup; visual fill via children

            // Text and Image are both Graphic components - can't coexist on
            // the same GameObject. Put Text on a child.
            GameObject txtObj = new GameObject("Text");
            txtObj.transform.SetParent(btnObj.transform, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = $"Show {Mathf.Min(LIBRARY_PAGE_SIZE, remaining)} more ({remaining} hidden)";
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 12;
            txt.color = Color.white;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false;

            RectTransform txtRT = txt.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero; txtRT.offsetMax = Vector2.zero;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() =>
            {
                libraryVisibleCount += LIBRARY_PAGE_SIZE;
                BuildLibraryUI();
            });
        }

        private void RefreshFromProviders()
        {
            allBlocks.Clear();
            categories.Clear();
            var seenCats = new HashSet<string>();
            foreach (var provider in BlockProviderRegistry.Providers)
            {
                foreach (var def in provider.GetBlocks())
                {
                    allBlocks.Add(def);
                    if (!string.IsNullOrEmpty(def.category) && seenCats.Add(def.category))
                    {
                        categories.Add(def.category);
                    }
                }
            }
            categories.Sort();
            categories.Insert(0, "All");

            // Rebuild tabs
            if (libraryTabsRow != null && libraryTabPrefab != null)
            {
                foreach (Transform child in libraryTabsRow) Destroy(child.gameObject);
                foreach (var cat in categories)
                {
                    CreateCategoryTab(cat);
                }
            }
        }

        private void CreateCategoryTab(string category)
        {
            GameObject tabObj = Instantiate(libraryTabPrefab, libraryTabsRow);
            tabObj.name = $"Tab_{category}";
            tabObj.SetActive(true);
            var text = tabObj.GetComponentInChildren<Text>();
            if (text != null) text.text = category;
            var btn = tabObj.GetComponent<Button>();
            if (btn == null) btn = tabObj.AddComponent<Button>();
            var bg = tabObj.GetComponent<Image>();
            if (bg != null) btn.targetGraphic = bg;
            // Highlight current category
            if (bg != null) bg.color = (category == currentCategoryFilter) ? selectedItemColor : normalItemColor;
            string captured = category;
            btn.onClick.AddListener(() => { currentCategoryFilter = captured; BuildLibraryUI(); });
        }

        private void CreateLibraryItem(BlockDefinition def)
        {
            GameObject itemObj = Instantiate(libraryItemPrefab, libraryGridContent);
            itemObj.name = $"Item_{def.id}";
            itemObj.SetActive(true);

            var text = itemObj.GetComponentInChildren<Text>();
            if (text != null)
            {
                // Truncate long names so they fit the 100x100 cell without
                // wrapping. Original id is preserved on the GameObject name
                // for the tooltip / click handler.
                text.text = TruncateForCell(def.displayName, 14);
            }
            // Hover tooltip showing the full untruncated name
            var tip = itemObj.GetComponent<HoverTooltip>();
            if (tip == null) tip = itemObj.AddComponent<HoverTooltip>();
            tip.fullText = $"{def.displayName}\nCategory: {def.category}";

            var btn = itemObj.GetComponent<Button>();
            if (btn == null) btn = itemObj.AddComponent<Button>();

            var bg = itemObj.GetComponent<Image>();
            if (bg != null) bg.color = levelItemColor;
            if (bg != null) btn.targetGraphic = bg;

            // Strip any inner "Use" buttons
            foreach (var childBtn in itemObj.GetComponentsInChildren<Button>(true))
            {
                if (childBtn.gameObject != itemObj)
                {
                    childBtn.onClick.RemoveAllListeners();
                    Destroy(childBtn.gameObject);
                }
            }

            string capturedId = def.id;
            GameObject capturedObj = itemObj;
            Image capturedBg = bg;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnLibraryItemClicked(def, capturedObj, capturedBg));
        }

        /// <summary>
        /// Truncate names longer than `maxLen` with an ellipsis. Keeps short
        /// names untouched. Used for grid items where wrap would break layout.
        /// </summary>
        private static string TruncateForCell(string s, int maxLen)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Length <= maxLen) return s;
            return s.Substring(0, maxLen - 1) + "…";
        }

        private void OnLibraryItemClicked(BlockDefinition def, GameObject itemObj, Image itemBg)
        {
            if (levelBuilder == null) return;
            if (currentlySelectedItemName == def.id) return; // toggle-off disabled

            if (selectedItemBg != null) selectedItemBg.color = levelItemColor;
            if (itemBg != null) itemBg.color = selectedItemColor;
            selectedItemObj = itemObj;
            selectedItemBg = itemBg;
            currentlySelectedItemName = def.id;

            // Apply to level builder
            if (def.prefab != null)
            {
                levelBuilder.SetPrefab(def.prefab, def.id, def.fallbackPrimitive);
            }
            else
            {
                levelBuilder.SetBlockType(def.fallbackPrimitive);
            }
        }

        // ============== Saved levels ==============

        private void WireLevelListPanel()
        {
            if (closeLevelListButton != null)
            {
                closeLevelListButton.onClick.AddListener(() => { if (levelListPanel != null) levelListPanel.SetActive(false); });
            }
            if (levelListSearchInput != null)
            {
                levelListSearchInput.onValueChanged.AddListener(_ => BuildLevelList());
            }
        }

        public void OnLoadButtonClicked()
        {
            if (levelListPanel == null) return;

            // Confirm if there are unsaved changes
            if (levelBuilder != null && levelBuilder.IsModified)
            {
                ConfirmDialog.Show(
                    "Discard changes?",
                    "You have unsaved changes. Loading another level will discard them.",
                    onYes: () => { OpenLevelListPanel(); },
                    onNo: null,
                    yesLabel: "Discard & Load",
                    noLabel: "Cancel");
            }
            else
            {
                OpenLevelListPanel();
            }
        }

        private void OpenLevelListPanel()
        {
            if (levelListPanel == null) return;
            levelListPanel.SetActive(!levelListPanel.activeSelf);
            if (levelListPanel.activeSelf) BuildLevelList();
        }

        public void OnSaveButtonClicked()
        {
            if (levelBuilder == null) return;
            var grid = FindObjectOfType<GridManager>();
            if (grid == null) { ToastUI.Error("No GridManager in scene"); return; }

            LevelExporter.EnsureLevelsFolder();
            string folder = LevelExporter.GetDefaultLevelsFolder();
            // Save-in-place when current level path is set
            if (!string.IsNullOrEmpty(currentLevelPath) && File.Exists(currentLevelPath))
            {
                SaveToPath(currentLevelPath, grid);
            }
            else
            {
                string ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string rand = System.Guid.NewGuid().ToString("N").Substring(0, 4);
                string levelName = $"{defaultLevelPrefix}_{ts}_{rand}";
                string filePath = Path.Combine(folder, levelName + ".json");
                SaveToPath(filePath, grid);
            }
        }

        private void SaveToPath(string filePath, GridManager grid)
        {
            try
            {
                string levelName = Path.GetFileNameWithoutExtension(filePath);
                var data = LevelExporter.ExportLevel(grid, levelName, LevelMetadataDefaults.DefaultAuthor);
                LevelExporter.SaveLevelToFile(data, filePath);
                currentLevelPath = filePath;
                levelBuilder?.ClearModified();
                ToastUI.Success($"Saved {Path.GetFileName(filePath)} ({data.blocks.Count} blocks)");
                if (currentLevelNameText != null) currentLevelNameText.text = $"Loaded: {levelName}";

                if (levelListPanel != null && levelListPanel.activeSelf) BuildLevelList();
            }
            catch (System.Exception e)
            {
                ToastUI.Error($"Save failed: {e.Message}");
            }
        }

        public void BuildLevelList()
        {
            if (levelListContent == null || levelListItemPrefab == null) return;

            foreach (Transform child in levelListContent) Destroy(child.gameObject);

            var levels = LevelExporter.ListSavedLevels();
            string search = levelListSearchInput != null ? levelListSearchInput.text : "";
            if (!string.IsNullOrEmpty(search))
            {
                string s = search.ToLower();
                levels = levels.Where(l => l.fileName.ToLower().Contains(s)).ToList();
            }

            if (levelListHeaderText != null) levelListHeaderText.text = $"SAVED LEVELS ({levels.Count})";

            if (levels.Count == 0)
            {
                GameObject placeholder = new GameObject("EmptyPlaceholder");
                placeholder.transform.SetParent(levelListContent, false);
                Text t = placeholder.AddComponent<Text>();
                t.text = "No saved levels yet.\nClick SAVE to save the current level.";
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize = 11;
                t.color = Color.gray;
                t.alignment = TextAnchor.MiddleCenter;
                t.horizontalOverflow = HorizontalWrapMode.Wrap;
                LayoutElement le = placeholder.AddComponent<LayoutElement>();
                le.preferredHeight = 50;
                return;
            }

            foreach (var level in levels) CreateLevelListItem(level);
        }

        private void CreateLevelListItem(LevelExporter.LevelFileInfo level)
        {
            GameObject itemObj = Instantiate(levelListItemPrefab, levelListContent);
            itemObj.name = $"LevelItem_{level.fileName}";
            itemObj.SetActive(true);

            var text = itemObj.GetComponentInChildren<Text>();
            if (text != null) text.text = $"{level.fileName}\n<size=9>({FormatSize(level.fileSize)} - {level.modifiedTime:MM/dd HH:mm})</size>";

            var bg = itemObj.GetComponent<Image>();
            if (bg != null) bg.color = levelItemColor;

            var btn = itemObj.GetComponent<Button>();
            if (btn == null) btn = itemObj.AddComponent<Button>();
            if (bg != null) btn.targetGraphic = bg;

            foreach (var childBtn in itemObj.GetComponentsInChildren<Button>(true))
            {
                if (childBtn.gameObject != itemObj)
                {
                    childBtn.onClick.RemoveAllListeners();
                    Destroy(childBtn.gameObject);
                }
            }

            string capturedPath = level.fullPath;
            string capturedName = level.fileName;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnLevelListItemClicked(capturedPath, capturedName));
        }

        private void OnLevelListItemClicked(string fullPath, string displayName)
        {
            var grid = FindObjectOfType<GridManager>();
            if (grid == null) { ToastUI.Error("No GridManager in scene"); return; }

            var data = LevelExporter.LoadLevelFromFile(fullPath);
            if (data == null) { ToastUI.Error($"Failed to load {displayName}"); return; }

            LevelExporter.ImportLevel(data, grid);
            currentLevelPath = fullPath;
            levelBuilder?.ClearModified();
            if (currentLevelNameText != null) currentLevelNameText.text = $"Loaded: {displayName}";
            ToastUI.Success($"Loaded {displayName} ({data.blocks.Count} blocks)");
            if (levelListPanel != null) levelListPanel.SetActive(false);
        }

        private string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            return $"{bytes / 1024f / 1024f:F2} MB";
        }

        // ============== Help panel ==============

        private void WireHelpPanel()
        {
            if (helpCloseButton != null)
            {
                helpCloseButton.onClick.AddListener(() => { levelBuilder?.SetHelp(false); });
            }
        }

        private void UpdateHelpText()
        {
            if (helpPanelText == null) return;
            helpPanelText.text =
                "═══ HORUS LEVEL MAKER ═══\n\n" +
                "MODES\n" +
                "  B  Build (paint blocks)\n" +
                "  V  Select / move\n" +
                "  X  or  Delete  Erase\n\n" +
                "BLOCK TYPES\n" +
                "  1-5  Primitive types (Cube, Sphere, etc.)\n" +
                "  L    Toggle library panel\n" +
                "  Click an item in the library to use it\n\n" +
                "SELECTED BLOCK\n" +
                "  Click drag  Move (with 5px threshold)\n" +
                "  Q / E       Rotate -90° / +90° (Y axis)\n" +
                "  Shift+Click Add/remove from selection\n\n" +
                "CAMERA\n" +
                "  Tab  Toggle Topdown / Free\n" +
                "  WASD Pan (Topdown) or Move (Free)\n" +
                "  Scroll  Zoom\n" +
                "  Middle mouse  Drag pan / rotate\n" +
                "  Shift  Faster\n\n" +
                "FILE\n" +
                "  SAVE / LOAD buttons (top right)\n" +
                "  Save format: JSON, includes metadata\n\n" +
                "OTHER\n" +
                "  Ctrl+Z  Undo\n" +
                "  Shift+RClick  Erase hovered block\n" +
                "  F1      Toggle this help\n" +
                "  Esc     Cancel drag / close panel\n";
        }

        // ============== Public API for buttons ==============

        public void OnBuildButtonClicked()  { levelBuilder?.SetMode(LevelBuilder.BuildMode.Build); }
        public void OnEraseButtonClicked()  { levelBuilder?.SetMode(LevelBuilder.BuildMode.Erase); }
        public void OnSelectButtonClicked() { levelBuilder?.SetMode(LevelBuilder.BuildMode.Select); }
        public void OnImportButtonClicked() { OnLoadButtonClicked(); }
    }
}
