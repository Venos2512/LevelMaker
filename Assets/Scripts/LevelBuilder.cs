using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace LevelMaker
{
    /// <summary>
    /// Runtime level building system with 3 modes:
    /// B = Build (Paint), E = Erase, V = Select/Move
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        public enum BuildMode
        {
            Build,   // B - Paint new blocks
            Erase,   // E - Delete blocks
            Select   // V - Select and move blocks
        }

        [Header("Building Settings")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private float maxPlaceDistance = 200f;
        [SerializeField] private LayerMask placementMask = ~0;

        [Header("Block Types")]
        [SerializeField] private PrimitiveType currentBlockType = PrimitiveType.Cube;
        [SerializeField] private Vector3Int currentBlockSize = Vector3Int.one;
        [SerializeField] private GameObject currentPrefab; // Selected prefab from library (null = use primitive)
        [SerializeField] private string currentPrefabResourcesPath; // Resources path for re-loading

        [Header("Preview Settings")]
        [SerializeField] private Color validColor = new Color(0.3f, 1f, 0.3f, 1f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 1f);

        [Header("Current State")]
        [SerializeField] private int currentLayer = 0;
        [SerializeField] private BuildMode currentMode = BuildMode.Build;

        // Indicators
        private GameObject hoverIndicator;
        private GameObject selectionIndicator;
        private GameObject dragIndicator;

        // Gizmo for Select mode
        private GameObject gizmoX;
        private GameObject gizmoY;
        private GameObject gizmoZ;
        private GameObject gizmoCenter;

        // Hover state
        private Vector3 hoverPosition;
        private Vector3Int gridHoverPosition;
        private bool validPlacement;
        private bool isMouseOverGrid;
        private Vector3 previewSize;
        private GameObject hoveredBlock;

        // Select/Drag state
        // selectedBlocks is the multi-select source of truth. selectedBlock is
        // kept as a convenience alias for the single-block UI gizmo and stays
        // in sync with selectedBlocks.FirstOrDefault().
        private System.Collections.Generic.HashSet<GameObject> selectedBlocks = new System.Collections.Generic.HashSet<GameObject>();
        private GameObject selectedBlock; // first member of selectedBlocks, or null
        private BlockMetadata selectedMetadata;
        private bool isDragging;
        private Vector3 dragOffset;
        private BlockMetadata dragOriginalMetadata;
        private Vector3 dragStartMouse;   // position when mouse first went down
        private const float DRAG_THRESHOLD_PIXELS = 5f;

        // Modified state - any place/delete/move/rotate sets this; save clears it.
        public bool IsModified { get; private set; } = false;
        public System.Action OnModifiedChanged; // UI subscribes to this

        // F1 help toggle
        public bool ShowHelp { get; private set; } = false;
        public System.Action OnHelpToggled;

        // Undo system
        private System.Collections.Generic.Stack<IUndoAction> undoHistory = new System.Collections.Generic.Stack<IUndoAction>();
        private const int MAX_UNDO_HISTORY = 100;

        // Debug
        private bool notOccupied;
        private bool hasConnection;

        // Cached
        private Camera mainCamera;
        private Renderer hoverRenderer;

        private void Start()
        {
            mainCamera = Camera.main;

            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
                if (gridManager == null)
                {
                    GameObject gridObj = new GameObject("GridManager");
                    gridManager = gridObj.AddComponent<GridManager>();
                }
            }

            CreateHoverIndicator();
            CreateSelectionIndicator();
            CreateDragIndicator();
            CreateGizmo();

            // Auto-spawn BlockLibrary if not in scene
            if (FindObjectOfType<BlockLibrary>() == null)
            {
                GameObject libObj = new GameObject("BlockLibrary");
                BlockLibrary lib = libObj.AddComponent<BlockLibrary>();
                lib.SetLevelBuilder(this);
            }

            // Auto-create Canvas UI at runtime if not present (fallback)
            if (FindObjectOfType<LevelBuilderUI>() == null)
            {
                Debug.LogWarning("[LevelBuilder] No LevelBuilderUI found in scene. Creating basic fallback UI. For full UI, add a LevelBuilderUI component to a Canvas in the scene.");
                CreateRuntimeUI();
            }
        }

        /// <summary>
        /// Create a basic Canvas UI at runtime with mode buttons, library panel, and debug text.
        /// This is a fallback for when the Editor UI Creator hasn't been run.
        /// </summary>
        private void CreateRuntimeUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("LevelBuilderCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventObj = new GameObject("EventSystem");
                eventObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Add the LevelBuilderUI + Toast + Confirm components
            LevelBuilderUI ui = canvasObj.AddComponent<LevelBuilderUI>();
            canvasObj.AddComponent<ToastUI>();
            canvasObj.AddComponent<ConfirmDialog>();

            // Build UI elements at runtime
            BuildRuntimeUIElements(canvasObj.transform, ui);

            Debug.Log("[LevelBuilder] Runtime UI created (Phase 1 + 2 layout).");
        }

        private void BuildRuntimeUIElements(Transform canvasTransform, LevelBuilderUI ui)
        {
            // Minimal runtime fallback UI - just the top bar + a compact debug line.
            // For full features (library, level list, help, toast, confirm), run
            // Tools > Level Maker > Reset Level Builder UI in the Editor.
            GameObject topPanel = CreateRuntimePanel("TopButtonsPanel", canvasTransform);
            RectTransform topRT = topPanel.GetComponent<RectTransform>();
            topRT.anchorMin = new Vector2(0.5f, 1f);
            topRT.anchorMax = new Vector2(0.5f, 1f);
            topRT.pivot = new Vector2(0.5f, 1f);
            topRT.anchoredPosition = new Vector2(0, -10);
            topRT.sizeDelta = new Vector2(720, 40);
            HorizontalLayoutGroup topHLG = topPanel.AddComponent<HorizontalLayoutGroup>();
            topHLG.spacing = 5;
            topHLG.childForceExpandWidth = true;
            topHLG.childForceExpandHeight = true;

            Button buildBtn  = CreateRuntimeButton("BuildBtn", "BUILD (B)", topPanel.transform, new Color(0.30f, 0.70f, 0.35f));
            Button eraseBtn  = CreateRuntimeButton("EraseBtn", "ERASE (X)", topPanel.transform, new Color(0.70f, 0.30f, 0.30f));
            Button selectBtn = CreateRuntimeButton("SelectBtn", "SELECT (V)", topPanel.transform, new Color(0.70f, 0.70f, 0.30f));
            Button saveBtn   = CreateRuntimeButton("SaveBtn", "SAVE", topPanel.transform, new Color(0.30f, 0.60f, 0.80f));
            Button loadBtn   = CreateRuntimeButton("LoadBtn", "LOAD", topPanel.transform, new Color(0.50f, 0.40f, 0.80f));
            Button libBtn    = CreateRuntimeButton("LibraryToggleBtn", "LIB (L)", topPanel.transform, new Color(0.40f, 0.50f, 0.65f));
            Button helpBtn   = CreateRuntimeButton("HelpBtn", "HELP (F1)", topPanel.transform, new Color(0.55f, 0.55f, 0.55f));

            // Compact debug panel
            GameObject debugPanel = CreateRuntimePanel("DebugPanel", canvasTransform, false);
            RectTransform debugRT = debugPanel.GetComponent<RectTransform>();
            debugRT.anchorMin = new Vector2(0, 0);
            debugRT.anchorMax = new Vector2(0, 0);
            debugRT.pivot = new Vector2(0, 0);
            debugRT.anchoredPosition = new Vector2(10, 10);
            debugRT.sizeDelta = new Vector2(360, 28);
            HorizontalLayoutGroup debugHLG = debugPanel.AddComponent<HorizontalLayoutGroup>();
            debugHLG.padding = new RectOffset(8, 8, 0, 0);
            debugHLG.spacing = 6;
            debugHLG.childAlignment = TextAnchor.MiddleLeft;
            debugHLG.childForceExpandWidth = false;
            debugHLG.childForceExpandHeight = true;
            debugHLG.childControlWidth = true;
            debugHLG.childControlHeight = true;

            Text compact = CreateRuntimeText("DebugCompact", "Mode: Build | Blocks: 0 | Sel: 0", debugPanel.transform, 12, Color.white, FontStyle.Bold);
            compact.alignment = TextAnchor.MiddleLeft;
            LayoutElement cLE = compact.gameObject.AddComponent<LayoutElement>();
            cLE.flexibleWidth = 1;

            // Wire all references via reflection (matches new LevelBuilderUI fields)
            var type = typeof(LevelBuilderUI);
            var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
            type.GetField("levelBuilder", flags)?.SetValue(ui, this);
            type.GetField("blockLibrary", flags)?.SetValue(ui, FindObjectOfType<BlockLibrary>());
            type.GetField("toastUI", flags)?.SetValue(ui, ui.GetComponent<ToastUI>());
            type.GetField("confirmDialog", flags)?.SetValue(ui, ui.GetComponent<ConfirmDialog>());
            type.GetField("buildButton", flags)?.SetValue(ui, buildBtn);
            type.GetField("eraseButton", flags)?.SetValue(ui, eraseBtn);
            type.GetField("selectButton", flags)?.SetValue(ui, selectBtn);
            type.GetField("saveButton", flags)?.SetValue(ui, saveBtn);
            type.GetField("loadButton", flags)?.SetValue(ui, loadBtn);
            type.GetField("libraryToggleButton", flags)?.SetValue(ui, libBtn);
            type.GetField("helpButton", flags)?.SetValue(ui, helpBtn);
            type.GetField("debugPanel", flags)?.SetValue(ui, debugPanel);
            type.GetField("debugCompactText", flags)?.SetValue(ui, compact);
            type.GetField("currentLevelNameText", flags)?.SetValue(ui, compact); // reuse as fallback
            type.GetField("currentSelectionText", flags)?.SetValue(ui, compact); // fallback
            type.GetField("modeText", flags)?.SetValue(ui, compact); // fallback
            type.GetField("undoCountText", flags)?.SetValue(ui, compact); // fallback
        }

        private GameObject CreateRuntimeLevelListItemTemplate()
        {
            GameObject item = new GameObject("LevelListItemTemplate");
            RectTransform rt = item.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380, 40);
            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.25f, 0.4f, 0.9f);
            VerticalLayoutGroup vlg = item.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 4, 4);
            vlg.spacing = 0;
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            le.minHeight = 36;

            Text nameText = CreateRuntimeText("NameText", "LevelName", item.transform, 12, Color.white, FontStyle.Normal);
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameText.verticalOverflow = VerticalWrapMode.Truncate;
            return item;
        }

        private GameObject CreateRuntimePanel(string name, Transform parent, bool raycastTarget = true)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image img = panel.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.7f);
            img.raycastTarget = raycastTarget; // false = không chặn click xuống scene
            return panel;
        }

        private Button CreateRuntimeButton(string name, string label, Transform parent, Color bgColor)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 30);
            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            Text txt = CreateRuntimeText("Text", label, btnObj.transform, 12, Color.white, FontStyle.Bold);
            txt.alignment = TextAnchor.MiddleCenter;
            RectTransform txtRT = txt.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;
            return btn;
        }

        private Text CreateRuntimeText(string name, string content, Transform parent, int fontSize, Color color, FontStyle style)
        {
            GameObject txtObj = new GameObject(name);
            txtObj.transform.SetParent(parent, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = content;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.color = color;
            txt.fontStyle = style;
            txt.alignment = TextAnchor.MiddleLeft;
            return txt;
        }

        private GameObject CreateRuntimeLibraryItemTemplate()
        {
            GameObject item = new GameObject("LibraryItemTemplate");
            RectTransform rt = item.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 32);
            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            HorizontalLayoutGroup hlg = item.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(5, 5, 5, 5);
            hlg.spacing = 5;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 32;

            Text nameText = CreateRuntimeText("NameText", "BlockName", item.transform, 11, Color.white, FontStyle.Normal);
            LayoutElement nameLE = nameText.gameObject.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 1;

            Button useBtn = CreateRuntimeButton("UseButton", "Use", item.transform, new Color(0.3f, 0.6f, 0.3f));
            LayoutElement btnLE = useBtn.gameObject.AddComponent<LayoutElement>();
            btnLE.preferredWidth = 50;
            return item;
        }

        private void Update()
        {
            // F1: toggle help overlay
            if (Input.GetKeyDown(KeyCode.F1)) ToggleHelp();

            HandleModeSwitch();
            UpdateMouseHover();
            HandleInput();
            UpdateGizmo();
        }

        private void HandleModeSwitch()
        {
            // B / V: switch between Build and Select
            if (Input.GetKeyDown(KeyCode.B)) SetMode(BuildMode.Build);
            if (Input.GetKeyDown(KeyCode.V)) SetMode(BuildMode.Select);
            // X or Delete: jump to Erase mode (intuitive "I want to delete")
            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Delete)) SetMode(BuildMode.Erase);
            // Keep E as fallback mode switch when no block is selected (back-compat)
            if (Input.GetKeyDown(KeyCode.E) && selectedBlock == null) SetMode(BuildMode.Erase);
        }

        public void SetMode(BuildMode mode)
        {
            currentMode = mode;
            if (isDragging) CancelDrag();
            if (mode != BuildMode.Select && selectedBlock != null) Deselect();
            Debug.Log($"[LevelBuilder] Mode: {mode}");
        }

        public BuildMode GetMode() => currentMode;
        public string GetBlockTypeName() => currentBlockType.ToString();
        public PrimitiveType GetBlockType() => currentBlockType;

        // ===== INDICATORS =====

        private Material CreateTransparentMaterial(Color color, int renderQueue)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = renderQueue;
            return mat;
        }

        private void CreateHoverIndicator()
        {
            hoverIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hoverIndicator.name = "HoverIndicator";
            hoverIndicator.transform.localScale = Vector3.one;
            var c = hoverIndicator.GetComponent<Collider>();
            if (c != null) Destroy(c);
            hoverRenderer = hoverIndicator.GetComponent<Renderer>();
            if (hoverRenderer != null)
                hoverRenderer.sharedMaterial = CreateTransparentMaterial(new Color(0f, 1f, 0f, 0.3f), 3000);
            hoverIndicator.SetActive(false);
        }

        private void CreateSelectionIndicator()
        {
            selectionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            selectionIndicator.name = "SelectionIndicator";
            var c = selectionIndicator.GetComponent<Collider>();
            if (c != null) Destroy(c);
            var r = selectionIndicator.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = CreateTransparentMaterial(new Color(0f, 1f, 0f, 0.4f), 3001);
            selectionIndicator.SetActive(false);
        }

        private void CreateDragIndicator()
        {
            dragIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dragIndicator.name = "DragIndicator";
            var c = dragIndicator.GetComponent<Collider>();
            if (c != null) Destroy(c);
            var r = dragIndicator.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = CreateTransparentMaterial(new Color(1f, 0.8f, 0f, 0.5f), 3002);
            dragIndicator.SetActive(false);
        }

        private void CreateGizmo()
        {
            gizmoCenter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gizmoCenter.name = "GizmoCenter";
            var gc = gizmoCenter.GetComponent<Collider>();
            if (gc != null) Destroy(gc);
            gizmoCenter.transform.localScale = Vector3.one * 0.3f;
            var gcr = gizmoCenter.GetComponent<Renderer>();
            if (gcr != null) gcr.sharedMaterial = CreateTransparentMaterial(new Color(1f, 1f, 0f, 0.8f), 3003);
            gizmoCenter.SetActive(false);

            gizmoX = CreateAxisArrow(Vector3.right, new Color(1f, 0.2f, 0.2f, 0.9f), "GizmoX");
            gizmoY = CreateAxisArrow(Vector3.up, new Color(0.2f, 1f, 0.2f, 0.9f), "GizmoY");
            gizmoZ = CreateAxisArrow(Vector3.forward, new Color(0.2f, 0.4f, 1f, 0.9f), "GizmoZ");
        }

        private GameObject CreateAxisArrow(Vector3 direction, Color color, string name)
        {
            GameObject arrow = new GameObject(name);
            GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = "ArrowMesh";
            cyl.transform.SetParent(arrow.transform);
            cyl.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);
            if (direction == Vector3.right)
                cyl.transform.localRotation = Quaternion.Euler(0, 0, -90);
            else if (direction == Vector3.forward)
                cyl.transform.localRotation = Quaternion.Euler(90, 0, 0);

            var c = cyl.GetComponent<Collider>();
            if (c != null) Destroy(c);

            var r = cyl.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = CreateTransparentMaterial(color, 3003);

            arrow.SetActive(false);
            return arrow;
        }

        private void UpdateGizmo()
        {
            if (selectedBlock == null || currentMode != BuildMode.Select)
            {
                if (gizmoCenter != null) gizmoCenter.SetActive(false);
                if (gizmoX != null) gizmoX.SetActive(false);
                if (gizmoY != null) gizmoY.SetActive(false);
                if (gizmoZ != null) gizmoZ.SetActive(false);
                return;
            }

            Vector3 center = gridManager.GridToWorld(selectedMetadata.gridPosition);
            center.y += selectedMetadata.gridSize.y * 0.5f * gridManager.CellSize;

            gizmoCenter.transform.position = center;
            gizmoX.transform.position = center;
            gizmoY.transform.position = center;
            gizmoZ.transform.position = center;

            gizmoCenter.SetActive(true);
            gizmoX.SetActive(true);
            gizmoY.SetActive(true);
            gizmoZ.SetActive(true);
        }

        // ===== MOUSE HOVER =====

        private void UpdateMouseHover()
        {
            // Skip hover if pointer is over UI
            if (IsPointerOverUI())
            {
                isMouseOverGrid = false;
                hoveredBlock = null;
                hoverIndicator.SetActive(false);
                selectionIndicator.SetActive(false);
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            isMouseOverGrid = false;
            hoveredBlock = null;

            if (Physics.Raycast(ray, out RaycastHit hit, maxPlaceDistance, placementMask))
            {
                hoverPosition = hit.point;
                GameObject hitObject = hit.collider.gameObject;

                if (gridManager.TryGetBlockMetadata(hitObject, out BlockMetadata hitMetadata))
                {
                    hoveredBlock = hitObject;

                    if (currentMode == BuildMode.Build)
                    {
                        // Build mode: detect which face is hovered, place block on that face
                        gridHoverPosition = GetPlacementPositionFromFace(hit, hitMetadata);
                        hoverPosition = gridManager.GridToWorld(gridHoverPosition);
                    }
                    else
                    {
                        // Erase/Select mode: select the block
                        gridHoverPosition = hitMetadata.gridPosition;
                        hoverPosition = gridManager.GridToWorld(gridHoverPosition);
                    }
                }
                else
                {
                    Vector3 snapped = gridManager.SnapToGrid(hoverPosition);
                    gridHoverPosition = gridManager.WorldToGrid(snapped);
                    gridHoverPosition.y = currentLayer;
                    hoverPosition = gridManager.GridToWorld(gridHoverPosition);
                }

                isMouseOverGrid = true;

                notOccupied = !gridManager.AreCellsOccupied(gridHoverPosition, currentBlockSize);
                hasConnection = currentLayer == 0 || gridManager.HasAdjacentOccupiedCell(gridHoverPosition, currentBlockSize);
                validPlacement = notOccupied && hasConnection;
            }
            else
            {
                float targetY = currentLayer * gridManager.CellSize;
                Plane plane = new Plane(Vector3.up, new Vector3(0, targetY, 0));
                if (plane.Raycast(ray, out float distance))
                {
                    hoverPosition = ray.GetPoint(distance);
                    Vector3 snapped = gridManager.SnapToGrid(hoverPosition);
                    gridHoverPosition = gridManager.WorldToGrid(snapped);
                    gridHoverPosition.y = currentLayer;
                    hoverPosition = gridManager.GridToWorld(gridHoverPosition);
                    isMouseOverGrid = true;

                    notOccupied = !gridManager.AreCellsOccupied(gridHoverPosition, currentBlockSize);
                    hasConnection = currentLayer == 0 || gridManager.HasAdjacentOccupiedCell(gridHoverPosition, currentBlockSize);
                    validPlacement = notOccupied && hasConnection;
                }
            }

            UpdateVisualIndicators();
        }

        /// <summary>
        /// Calculate placement grid position based on which face of the block is hovered.
        /// Detects the dominant axis of the hit normal to determine face direction.
        /// </summary>
        private Vector3Int GetPlacementPositionFromFace(RaycastHit hit, BlockMetadata hitMetadata)
        {
            // Snap hit point to grid first
            Vector3 snappedHit = gridManager.SnapToGrid(hit.point);

            // Determine face direction by finding the dominant axis of the normal
            Vector3 normal = hit.normal;
            float absX = Mathf.Abs(normal.x);
            float absY = Mathf.Abs(normal.y);
            float absZ = Mathf.Abs(normal.z);

            Vector3Int basePos = hitMetadata.gridPosition;
            Vector3Int size = hitMetadata.gridSize;

            // Top face (+Y normal)
            if (absY > absX && absY > absZ && normal.y > 0.5f)
            {
                // Place on top
                return new Vector3Int(basePos.x, basePos.y + size.y, basePos.z);
            }
            // Bottom face (-Y normal)
            else if (absY > absX && absY > absZ && normal.y < -0.5f)
            {
                // Place under
                return new Vector3Int(basePos.x, basePos.y - 1, basePos.z);
            }
            // +X face (right)
            else if (absX > absY && absX > absZ && normal.x > 0.5f)
            {
                return new Vector3Int(basePos.x + size.x, basePos.y, basePos.z);
            }
            // -X face (left)
            else if (absX > absY && absX > absZ && normal.x < -0.5f)
            {
                return new Vector3Int(basePos.x - 1, basePos.y, basePos.z);
            }
            // +Z face (forward)
            else if (absZ > absX && absZ > absY && normal.z > 0.5f)
            {
                return new Vector3Int(basePos.x, basePos.y, basePos.z + size.z);
            }
            // -Z face (back)
            else if (absZ > absX && absZ > absY && normal.z < -0.5f)
            {
                return new Vector3Int(basePos.x, basePos.y, basePos.z - 1);
            }

            // Fallback: place on top
            return new Vector3Int(basePos.x, basePos.y + size.y, basePos.z);
        }

        private void UpdateVisualIndicators()
        {
            hoverIndicator.SetActive(false);
            selectionIndicator.SetActive(false);

            if (!isMouseOverGrid) return;

            switch (currentMode)
            {
                case BuildMode.Build:
                    // If shift is held, show selection frame (erase preview) instead of build preview
                    bool shiftHeldVis = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    if (shiftHeldVis)
                    {
                        ShowSelectionFrame();
                    }
                    else
                    {
                        ShowBuildPreview();
                    }
                    break;
                case BuildMode.Erase:
                    ShowSelectionFrame();
                    break;
                case BuildMode.Select:
                    if (selectedBlock == null) ShowSelectionFrame();
                    break;
            }
        }

        private void ShowBuildPreview()
        {
            // Build mode: ALWAYS show preview at empty position, never hover into blocks
            Vector3 worldScale = gridManager.GetWorldScale(currentBlockSize);
            previewSize = GetCorrectedScale(worldScale, currentBlockType);
            Color previewColor = validPlacement ? validColor : invalidColor;
            DrawWireframeBox(hoverPosition, previewSize, previewColor);

            hoverIndicator.SetActive(true);
            Vector3 indicatorPos = hoverPosition;
            indicatorPos.y += previewSize.y * 0.5f;
            hoverIndicator.transform.position = indicatorPos;
            hoverIndicator.transform.localScale = previewSize;

            if (hoverRenderer != null && hoverRenderer.sharedMaterial != null)
            {
                Color boxColor = validPlacement ? new Color(0f, 1f, 0f, 0.3f) : new Color(1f, 0f, 0f, 0.3f);
                hoverRenderer.sharedMaterial.SetColor("_BaseColor", boxColor);
                hoverRenderer.sharedMaterial.SetColor("_Color", boxColor);
                hoverRenderer.sharedMaterial.color = boxColor;
            }
        }

        private void ShowSelectionFrame()
        {
            if (hoveredBlock != null && gridManager.TryGetBlockMetadata(hoveredBlock, out BlockMetadata meta))
            {
                selectionIndicator.SetActive(true);
                Vector3 selPos = gridManager.GridToWorld(meta.gridPosition);
                selPos.y += meta.gridSize.y * 0.5f * gridManager.CellSize;
                selectionIndicator.transform.position = selPos;
                selectionIndicator.transform.localScale = new Vector3(
                    meta.gridSize.x * gridManager.CellSize,
                    meta.gridSize.y * gridManager.CellSize,
                    meta.gridSize.z * gridManager.CellSize
                );
                gridHoverPosition = meta.gridPosition;
            }
        }

        // ===== INPUT =====

        private bool IsPointerOverUI()
        {
            // Check if pointer is over an interactive UGUI element (button, etc)
            // Panel backgrounds with raycastTarget=false are ignored
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return false;

            // Check if mouse is over any UGUI element
            if (!es.IsPointerOverGameObject()) return false;

            // Get the UI element under pointer
            var pointerData = new UnityEngine.EventSystems.PointerEventData(es);
            pointerData.position = Input.mousePosition;
            var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            es.RaycastAll(pointerData, results);

            // Only return true if the topmost element is interactive (Selectable)
            foreach (var result in results)
            {
                var selectable = result.gameObject.GetComponent<UnityEngine.UI.Selectable>();
                if (selectable != null && selectable.IsInteractable())
                {
                    return true; // Over a button or other interactive UI
                }
            }
            return false; // Only over non-interactive UI (panels, backgrounds)
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetBlockType(PrimitiveType.Cube);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetBlockType(PrimitiveType.Sphere);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetBlockType(PrimitiveType.Cylinder);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetBlockType(PrimitiveType.Capsule);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SetBlockType(PrimitiveType.Plane);

            if (Input.GetKeyDown(KeyCode.LeftBracket)) currentLayer = Mathf.Max(0, currentLayer - 1);
            if (Input.GetKeyDown(KeyCode.RightBracket)) currentLayer++;

            if (Input.GetKeyDown(KeyCode.Escape) && isDragging) CancelDrag();

            // Undo (Ctrl+Z)
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                && Input.GetKeyDown(KeyCode.Z))
            {
                Undo();
            }

            // Q/E: rotate selected block by 90° around Y (Select mode only)
            HandleRotationInput();

            // Skip scene input if pointer is over UI
            if (IsPointerOverUI()) return;

            switch (currentMode)
            {
                case BuildMode.Build: HandleBuildInput(); break;
                case BuildMode.Erase: HandleEraseInput(); break;
                case BuildMode.Select: HandleSelectInput(); break;
            }
        }

        private Vector3Int lastPaintPosition; // Track last painted cell to avoid duplicates
        private bool isPainting;             // Mouse is being held in Build mode
        private float lastPaintTime;         // Time of last paint (for throttling)
        private const float PAINT_THROTTLE_INTERVAL = 0.2f; // Min seconds between paints when holding (5 paints/sec max)

        private void HandleBuildInput()
        {
            bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // Shift+RightClick in Build mode: erase hovered block (like Erase mode)
            if (shiftHeld && Input.GetMouseButtonDown(1) && hoveredBlock != null)
            {
                DeleteBlock(hoveredBlock);
                return;
            }

            // If shift is held but not right-click, don't allow building
            if (shiftHeld)
            {
                return;
            }

            // Mouse down: start painting
            if (Input.GetMouseButtonDown(0) && isMouseOverGrid)
            {
                isPainting = true;
                TryPaintAtHover();
            }
            // Mouse held: continue painting with throttle (skip if at same position or too soon)
            else if (Input.GetMouseButton(0) && isPainting && isMouseOverGrid)
            {
                if (gridHoverPosition != lastPaintPosition &&
                    Time.time - lastPaintTime >= PAINT_THROTTLE_INTERVAL)
                {
                    TryPaintAtHover();
                }
            }
            // Mouse up: stop painting
            if (Input.GetMouseButtonUp(0))
            {
                isPainting = false;
            }
        }

        private void TryPaintAtHover()
        {
            if (validPlacement)
            {
                PlaceBlock();
                lastPaintPosition = gridHoverPosition;
                lastPaintTime = Time.time;
            }
        }

        private void HandleEraseInput()
        {
            if (Input.GetMouseButtonDown(0) && isMouseOverGrid && hoveredBlock != null)
            {
                DeleteBlock(hoveredBlock);
            }
        }

        // Q: rotate -90° around Y, E: rotate +90° around Y. Skipped during a drag
        // so a held mouse doesn't fight the keyboard input.
        private void HandleRotationInput()
        {
            if (selectedBlock == null || isDragging) return;

            if (Input.GetKeyDown(KeyCode.Q))
            {
                RotateSelectedBlock(-90f);
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                RotateSelectedBlock(90f);
            }
        }

        /// <summary>
        /// Snap-rotate the selected block by `angle` degrees around the Y axis.
        /// Y rotation swaps X and Z in the grid footprint (e.g. 2x1x1 -> 1x1x2),
        /// so we re-occupy cells when the size changes. 1x1x1 blocks keep the same
        /// footprint and only their visual rotation changes.
        /// </summary>
        private void RotateSelectedBlock(float angle)
        {
            if (selectedBlock == null) return;

            Vector3Int oldGridPos = selectedMetadata.gridPosition;
            Vector3Int oldGridSize = selectedMetadata.gridSize;
            Quaternion oldRot = selectedBlock.transform.rotation;
            string blockType = selectedMetadata.blockType;

            // Snap rotation to the nearest 90° on Y - prevents float drift after
            // many presses (e.g. 0, 90, 180, 270, 0) and stays consistent with
            // grid snapping in the rest of the system.
            float newYAngle = Mathf.RoundToInt((oldRot.eulerAngles.y + angle) / 90f) * 90f;
            Quaternion newRot = Quaternion.Euler(0, newYAngle, 0);

            // 90° Y rotation swaps the X and Z extents in the grid.
            Vector3Int newGridSize = new Vector3Int(oldGridSize.z, oldGridSize.y, oldGridSize.x);

            selectedBlock.transform.rotation = newRot;

            if (newGridSize != oldGridSize)
            {
                // Drop the old footprint and reclaim cells with the new size.
                // The metadata stored on the block is overwritten by OccupyCells.
                gridManager.FreeCells(selectedBlock);
                gridManager.OccupyCells(oldGridPos, newGridSize, selectedBlock, blockType);
                selectedMetadata = new BlockMetadata(oldGridPos, newGridSize, blockType);
            }

            MarkModified();
            RecordUndo(new RotateBlockAction(
                gridManager, selectedBlock,
                oldRot, newRot,
                oldGridPos,
                oldGridSize, newGridSize,
                blockType));
        }

        private void HandleSelectInput()
        {
            if (isDragging)
            {
                HandleDragging();
                return;
            }

            // Mouse down: record start position for drag-threshold check
            if (Input.GetMouseButtonDown(0) && isMouseOverGrid)
            {
                dragStartMouse = Input.mousePosition;
                if (hoveredBlock != null)
                {
                    bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                    if (shift)
                    {
                        // Shift+click: toggle block in multi-select set
                        ToggleSelection(hoveredBlock);
                    }
                    else
                    {
                        if (!selectedBlocks.Contains(hoveredBlock))
                        {
                            SetSelected(hoveredBlock);
                        }
                        // If already selected, fall through to drag-attempt below
                    }
                }
            }

            // Mouse held: if we passed the drag threshold, start dragging
            if (Input.GetMouseButton(0) && selectedBlock != null && dragStartMouse != Vector3.zero)
            {
                if (Vector2.Distance(dragStartMouse, Input.mousePosition) > DRAG_THRESHOLD_PIXELS)
                {
                    TryStartDrag();
                    dragStartMouse = Vector3.zero;
                }
            }

            // Mouse up: clear drag-start state
            if (Input.GetMouseButtonUp(0))
            {
                dragStartMouse = Vector3.zero;
            }
        }

        private void SelectBlock(GameObject block)
        {
            selectedBlock = block;
            if (gridManager.TryGetBlockMetadata(block, out BlockMetadata meta))
            {
                selectedMetadata = meta;
            }
        }

        private void Deselect()
        {
            if (isDragging) CancelDrag();
            selectedBlocks.Clear();
            selectedBlock = null;
            selectedMetadata = default;
        }

        /// <summary>
        /// Replace the current selection with a single block. Use ToggleSelection
        /// to add/remove individual blocks for multi-select.
        /// </summary>
        public void SetSelected(GameObject block)
        {
            selectedBlocks.Clear();
            if (block != null)
            {
                selectedBlocks.Add(block);
                selectedBlock = block;
                if (gridManager.TryGetBlockMetadata(block, out BlockMetadata meta)) selectedMetadata = meta;
            }
            else
            {
                selectedBlock = null;
                selectedMetadata = default;
            }
        }

        /// <summary>
        /// Shift-click helper: add to or remove from the selection set.
        /// </summary>
        public void ToggleSelection(GameObject block)
        {
            if (block == null) return;
            if (selectedBlocks.Contains(block))
            {
                selectedBlocks.Remove(block);
            }
            else
            {
                selectedBlocks.Add(block);
            }
            // Keep selectedBlock aligned with the first member for legacy single-block gizmo code
            if (selectedBlocks.Count == 0)
            {
                selectedBlock = null;
                selectedMetadata = default;
            }
            else
            {
                selectedBlock = null;
                foreach (var b in selectedBlocks) { selectedBlock = b; break; }
                if (selectedBlock != null && gridManager.TryGetBlockMetadata(selectedBlock, out BlockMetadata meta))
                {
                    selectedMetadata = meta;
                }
            }
        }

        public System.Collections.Generic.IReadOnlyCollection<GameObject> GetSelection() => selectedBlocks;
        public int GetSelectionCount() => selectedBlocks.Count;

        /// <summary>
        /// Mark the level as modified. Call after any Place/Delete/Rotate/Move.
        /// Notifies OnModifiedChanged subscribers (the UI shows the * indicator).
        /// </summary>
        public void MarkModified()
        {
            if (IsModified) return;
            IsModified = true;
            OnModifiedChanged?.Invoke();
        }

        public void ClearModified()
        {
            if (!IsModified) return;
            IsModified = false;
            OnModifiedChanged?.Invoke();
        }

        public void ToggleHelp()
        {
            ShowHelp = !ShowHelp;
            OnHelpToggled?.Invoke();
        }

        /// <summary>
        /// Explicitly set the help panel state. Used by the close button to
        /// turn help off without toggling; only fires the event if the state
        /// actually changes so we don't churn UI for no-op clicks.
        /// </summary>
        public void SetHelp(bool visible)
        {
            if (ShowHelp == visible) return;
            ShowHelp = visible;
            OnHelpToggled?.Invoke();
        }

        private void TryStartDrag()
        {
            if (selectedBlock == null) return;

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, maxPlaceDistance, placementMask))
            {
                if (hit.collider.gameObject == selectedBlock)
                {
                    dragOffset = selectedBlock.transform.position - hit.point;
                    isDragging = true;
                    dragOriginalMetadata = selectedMetadata;

                    dragIndicator.SetActive(true);
                    UpdateDragIndicator();
                }
            }
        }

        private void HandleDragging()
        {
            if (selectedBlock == null)
            {
                CancelDrag();
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                FinishDrag();
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Plane dragPlane = new Plane(Vector3.up, selectedBlock.transform.position);

            if (dragPlane.Raycast(ray, out float distance))
            {
                Vector3 worldPoint = ray.GetPoint(distance) + dragOffset;
                Vector3 snapped = gridManager.SnapToGrid(worldPoint);
                Vector3Int newGridPos = gridManager.WorldToGrid(snapped);

                if (CanMoveTo(newGridPos, selectedMetadata.gridSize))
                {
                    gridManager.FreeCells(selectedBlock);
                    selectedBlock.transform.position = snapped + new Vector3(0, selectedMetadata.gridSize.y * 0.5f * gridManager.CellSize, 0);
                    gridManager.OccupyCells(newGridPos, selectedMetadata.gridSize, selectedBlock, selectedMetadata.blockType);
                    selectedMetadata.gridPosition = newGridPos;
                }

                UpdateDragIndicator();
            }
        }

        private bool CanMoveTo(Vector3Int gridPos, Vector3Int size)
        {
            return !gridManager.AreCellsOccupied(gridPos, size, selectedBlock);
        }

        private void FinishDrag()
        {
            isDragging = false;
            dragIndicator.SetActive(false);
            MarkModified();
        }

        private void CancelDrag()
        {
            if (selectedBlock != null && isDragging)
            {
                gridManager.FreeCells(selectedBlock);
                Vector3 origWorld = gridManager.GridToWorld(dragOriginalMetadata.gridPosition);
                origWorld.y += dragOriginalMetadata.gridSize.y * 0.5f * gridManager.CellSize;
                selectedBlock.transform.position = origWorld;
                gridManager.OccupyCells(dragOriginalMetadata.gridPosition, dragOriginalMetadata.gridSize, selectedBlock, dragOriginalMetadata.blockType);
                selectedMetadata = dragOriginalMetadata;
            }
            isDragging = false;
            dragIndicator.SetActive(false);
        }

        private void UpdateDragIndicator()
        {
            if (selectedBlock == null) return;
            dragIndicator.transform.position = selectedBlock.transform.position;
            dragIndicator.transform.localScale = new Vector3(
                selectedMetadata.gridSize.x * gridManager.CellSize,
                selectedMetadata.gridSize.y * gridManager.CellSize,
                selectedMetadata.gridSize.z * gridManager.CellSize
            );
        }

        // ===== BLOCK OPS =====

        public void SetBlockType(PrimitiveType type)
        {
            currentBlockType = type;
            // Don't clear prefab here - user might want to keep prefab selection
            // Only clear if explicitly requested
        }

        public void ClearPrefab()
        {
            currentPrefab = null;
            currentPrefabResourcesPath = null;
        }

        public void SetPrefab(GameObject prefab, string resourcesPath = null, PrimitiveType fallbackType = PrimitiveType.Cube)
        {
            currentPrefab = prefab;
            currentPrefabResourcesPath = resourcesPath;
            currentBlockType = fallbackType; // Used for metadata / preview sizing
            Debug.Log($"[LevelBuilder.SetPrefab] prefab={prefab?.name} (ID={prefab?.GetInstanceID()}), fallbackType={fallbackType}");
        }

        public GameObject GetCurrentPrefab() => currentPrefab;

        public void SetBlockSize(Vector3Int size)
        {
            currentBlockSize = new Vector3Int(
                Mathf.Max(1, size.x),
                Mathf.Max(1, size.y),
                Mathf.Max(1, size.z)
            );
        }

        private Vector3 GetCorrectedScale(Vector3 baseScale, PrimitiveType primitiveType)
        {
            Vector3 corrected = baseScale;
            switch (primitiveType)
            {
                case PrimitiveType.Plane:
                    corrected.x *= 0.1f;
                    corrected.z *= 0.1f;
                    break;
                case PrimitiveType.Cylinder:
                case PrimitiveType.Capsule:
                    corrected.y *= 0.5f;
                    break;
            }
            return corrected;
        }

        private void PlaceBlock()
        {
            GameObject obj;

            // If a prefab is selected from library, instantiate it
            if (currentPrefab != null)
            {
                Debug.Log($"[LevelBuilder.PlaceBlock] Using prefab: {currentPrefab.name} (ID={currentPrefab.GetInstanceID()})");
                obj = Instantiate(currentPrefab);
                obj.name = currentPrefab.name;
            }
            else
            {
                Debug.Log($"[LevelBuilder.PlaceBlock] Using primitive: {currentBlockType} (prefab is NULL!)");
                // Fallback: create primitive
                obj = GameObject.CreatePrimitive(currentBlockType);
                obj.name = $"{currentBlockType}_{currentBlockSize.x}x{currentBlockSize.y}x{currentBlockSize.z}";
            }

            Vector3 placementPosition;

            if (currentPrefab != null)
            {
                // For prefabs: keep the prefab's original localScale (don't override)
                // Position at bottom-aligned (offset by half the prefab's bounding box)
                placementPosition = hoverPosition + new Vector3(0, obj.transform.localScale.y * 0.5f, 0);
            }
            else
            {
                // For primitives: apply corrected scale (handle non-1x1x1 sizes)
                Vector3 worldScale = gridManager.GetWorldScale(currentBlockSize);
                Vector3 correctedScale = GetCorrectedScale(worldScale, currentBlockType);
                obj.transform.localScale = correctedScale;
                placementPosition = hoverPosition + new Vector3(0, correctedScale.y * 0.5f, 0);
            }

            obj.transform.position = placementPosition;
            obj.transform.rotation = Quaternion.identity;
            obj.transform.SetParent(gridManager.transform);

            // Use prefab name for metadata if prefab is set, else block type
            string blockTypeName = currentPrefab != null ? currentPrefab.name : currentBlockType.ToString();
            gridManager.OccupyCells(gridHoverPosition, currentBlockSize, obj, blockTypeName);

            // Record undo action
            RecordUndo(new PlaceBlockAction(gridManager, obj, gridHoverPosition, currentBlockSize));
            MarkModified();
        }

        private void ReplaceBlock(GameObject oldBlock)
        {
            if (!gridManager.TryGetBlockMetadata(oldBlock, out BlockMetadata oldMetadata)) return;

            gridManager.FreeCells(oldBlock);
            // Deactivate instead of destroy so we can restore
            oldBlock.SetActive(false);

            GameObject newBlock;
            if (currentPrefab != null)
            {
                newBlock = Instantiate(currentPrefab);
                newBlock.name = currentPrefab.name;
            }
            else
            {
                newBlock = GameObject.CreatePrimitive(currentBlockType);
                newBlock.name = $"{currentBlockType}_{currentBlockSize.x}x{currentBlockSize.y}x{currentBlockSize.z}";
            }

            Vector3 placementPosition;

            if (currentPrefab != null)
            {
                // For prefabs: keep original scale
                placementPosition = hoverPosition + new Vector3(0, newBlock.transform.localScale.y * 0.5f, 0);
            }
            else
            {
                // For primitives: apply corrected scale
                Vector3 worldScale = gridManager.GetWorldScale(currentBlockSize);
                Vector3 correctedScale = GetCorrectedScale(worldScale, currentBlockType);
                newBlock.transform.localScale = correctedScale;
                placementPosition = hoverPosition + new Vector3(0, correctedScale.y * 0.5f, 0);
            }

            newBlock.transform.position = placementPosition;
            newBlock.transform.rotation = Quaternion.identity;
            newBlock.transform.SetParent(gridManager.transform);

            string blockTypeName = currentPrefab != null ? currentPrefab.name : currentBlockType.ToString();
            gridManager.OccupyCells(gridHoverPosition, currentBlockSize, newBlock, blockTypeName);

            // Record undo: replace = delete old + place new
            RecordUndo(new ReplaceBlockAction(gridManager, oldBlock, newBlock, oldMetadata.gridPosition, currentBlockSize, blockTypeName));
            MarkModified();
        }

        private void DeleteBlock(GameObject block)
        {
            if (block != null && gridManager.IsPlacedBlock(block))
            {
                if (selectedBlock == block) Deselect();

                // Get metadata BEFORE freeing
                BlockMetadata meta;
                gridManager.TryGetBlockMetadata(block, out meta);
                Vector3Int pos = meta.gridPosition;
                Vector3Int size = meta.gridSize;
                string type = meta.blockType;

                gridManager.FreeCells(block);
                // Deactivate instead of destroy so we can restore
                block.SetActive(false);

                // Record undo action
                RecordUndo(new DeleteBlockAction(gridManager, block, pos, size, type));
                MarkModified();
            }
        }

        // ===== WIREFRAME =====

        private void DrawWireframeBox(Vector3 bottomCenter, Vector3 size, Color color)
        {
            Vector3 center = bottomCenter + new Vector3(0, size.y * 0.5f, 0);
            Vector3 halfSize = size * 0.5f;
            Vector3[] corners = new Vector3[8];
            corners[0] = center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            corners[1] = center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            corners[2] = center + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            corners[3] = center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            corners[4] = center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            corners[5] = center + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            corners[6] = center + new Vector3(halfSize.x, halfSize.y, halfSize.z);
            corners[7] = center + new Vector3(-halfSize.x, halfSize.y, halfSize.z);

            Debug.DrawLine(corners[0], corners[1], color);
            Debug.DrawLine(corners[1], corners[2], color);
            Debug.DrawLine(corners[2], corners[3], color);
            Debug.DrawLine(corners[3], corners[0], color);
            Debug.DrawLine(corners[4], corners[5], color);
            Debug.DrawLine(corners[5], corners[6], color);
            Debug.DrawLine(corners[6], corners[7], color);
            Debug.DrawLine(corners[7], corners[4], color);
            Debug.DrawLine(corners[0], corners[4], color);
            Debug.DrawLine(corners[1], corners[5], color);
            Debug.DrawLine(corners[2], corners[6], color);
            Debug.DrawLine(corners[3], corners[7], color);
        }

        // ===== UNDO SYSTEM =====

        private interface IUndoAction
        {
            void Undo();
        }

        private class PlaceBlockAction : IUndoAction
        {
            private GridManager gridManager;
            private GameObject block;
            private Vector3Int gridPosition;
            private Vector3Int gridSize;
            public PlaceBlockAction(GridManager gm, GameObject b, Vector3Int pos, Vector3Int size)
            {
                gridManager = gm;
                block = b;
                gridPosition = pos;
                gridSize = size;
            }
            public void Undo()
            {
                if (block != null)
                {
                    gridManager.FreeCells(block);
                    Object.Destroy(block);
                }
            }
        }

        private class DeleteBlockAction : IUndoAction
        {
            private GridManager gridManager;
            private GameObject block;
            private Vector3Int gridPosition;
            private Vector3Int gridSize;
            private string blockType;
            public DeleteBlockAction(GridManager gm, GameObject b, Vector3Int pos, Vector3Int size, string type)
            {
                gridManager = gm;
                block = b;
                gridPosition = pos;
                gridSize = size;
                blockType = type;
            }
            public void Undo()
            {
                if (block == null) return;
                block.SetActive(true);
                gridManager.OccupyCells(gridPosition, gridSize, block, blockType);
            }
        }

        private class ReplaceBlockAction : IUndoAction
        {
            private GridManager gridManager;
            private GameObject oldBlock;
            private GameObject newBlock;
            private Vector3Int gridPosition;
            private Vector3Int newSize;
            private string newBlockType;
            public ReplaceBlockAction(GridManager gm, GameObject old, GameObject newB, Vector3Int pos, Vector3Int size, string type)
            {
                gridManager = gm;
                oldBlock = old;
                newBlock = newB;
                gridPosition = pos;
                newSize = size;
                newBlockType = type;
            }
            public void Undo()
            {
                if (newBlock == null) return;
                gridManager.FreeCells(newBlock);
                Object.Destroy(newBlock);
                if (oldBlock != null)
                {
                    oldBlock.SetActive(true);
                    if (gridManager.TryGetBlockMetadata(oldBlock, out BlockMetadata meta))
                    {
                        gridManager.OccupyCells(meta.gridPosition, meta.gridSize, oldBlock, meta.blockType);
                    }
                }
            }
        }

        /// <summary>
        /// Undo entry for a Q/E rotation. Restores the previous transform and
        /// reclaims the original grid footprint when the rotation changed size.
        /// </summary>
        private class RotateBlockAction : IUndoAction
        {
            private GridManager gridManager;
            private GameObject block;
            private Quaternion oldRotation;
            private Quaternion newRotation;
            private Vector3Int gridPosition;
            private Vector3Int oldSize;
            private Vector3Int newSize;
            private string blockType;

            public RotateBlockAction(GridManager gm, GameObject b,
                Quaternion oldR, Quaternion newR,
                Vector3Int pos,
                Vector3Int oldS, Vector3Int newS,
                string type)
            {
                gridManager = gm;
                block = b;
                oldRotation = oldR;
                newRotation = newR;
                gridPosition = pos;
                oldSize = oldS;
                newSize = newS;
                blockType = type;
            }

            public void Undo()
            {
                if (block == null) return;
                block.transform.rotation = oldRotation;
                if (oldSize != newSize)
                {
                    gridManager.FreeCells(block);
                    gridManager.OccupyCells(gridPosition, oldSize, block, blockType);
                }
            }
        }

        private void RecordUndo(IUndoAction action)
        {
            undoHistory.Push(action);
            if (undoHistory.Count > MAX_UNDO_HISTORY)
            {
                // Convert to array, drop oldest, rebuild stack
                var arr = undoHistory.ToArray();
                undoHistory.Clear();
                for (int i = 1; i < arr.Length; i++) undoHistory.Push(arr[i]);
            }
        }

        private void Undo()
        {
            if (undoHistory.Count == 0)
            {
                Debug.Log("[LevelBuilder] Nothing to undo");
                return;
            }
            IUndoAction action = undoHistory.Pop();
            action.Undo();
            Debug.Log($"[LevelBuilder] Undo: {action.GetType().Name} (history: {undoHistory.Count})");
        }

        public void UndoLast() => Undo();
        public int GetUndoCount() => undoHistory.Count;

        // ===== UI (Canvas-based, no IMGUI) =====
        // All UI is handled by LevelBuilderUI component on Canvas
    }
}
