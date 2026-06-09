using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace LevelMaker.Editor
{
    public class LevelMakerWindow : EditorWindow
    {
        private enum PrimitiveType
        {
            Cube,
            Sphere,
            Cylinder,
            Capsule,
            Plane
        }

        private enum EditMode
        {
            None,
            Place,
            Resize,
            Boolean
        }

        private enum BooleanOperation
        {
            Union,
            Subtract,
            Intersect
        }

        private PrimitiveType selectedPrimitive = PrimitiveType.Cube;
        private EditMode currentMode = EditMode.Place;
        private BooleanOperation booleanOp = BooleanOperation.Union;
        
        private Vector3Int gridSize = Vector3Int.one;
        private Material previewMaterial;
        private GameObject previewObject;
        private Vector3 placementPosition;
        private Vector3Int gridPlacementPosition;
        private bool validPlacement;
        private string invalidReason = "";

        private GameObject selectedBlock;

        private List<GameObject> booleanSelection = new List<GameObject>();
        private Color placementColor = new Color(0.3f, 0.8f, 0.3f, 0.5f);
        private Color invalidColor = new Color(0.8f, 0.3f, 0.3f, 0.5f);

        private GridManager gridManager;
        private Transform parentTransform;
        private float cellSize = 1f;
        private int currentLayer = 0; // Current Y layer for placement

        [MenuItem("Tools/Level Maker")]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelMakerWindow>("Level Maker");
            window.minSize = new Vector2(300, 400);
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            CreatePreviewMaterial();
            FindOrCreateGridManager();
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            DestroyPreview();
            if (previewMaterial != null)
                DestroyImmediate(previewMaterial);
        }

        private void FindOrCreateGridManager()
        {
            gridManager = FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                // Auto-create grid manager
                GameObject gridObj = new GameObject("GridManager");
                gridManager = gridObj.AddComponent<GridManager>();
                
                // Set grid properties using reflection
                var type = typeof(GridManager);
                var cellSizeField = type.GetField("cellSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (cellSizeField != null)
                    cellSizeField.SetValue(gridManager, cellSize);
            }
        }

        private void CreatePreviewMaterial()
        {
            var shader = Shader.Find("Standard");
            previewMaterial = new Material(shader);
            previewMaterial.SetFloat("_Mode", 3);
            previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMaterial.SetInt("_ZWrite", 0);
            previewMaterial.DisableKeyword("_ALPHATEST_ON");
            previewMaterial.EnableKeyword("_ALPHABLEND_ON");
            previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            previewMaterial.renderQueue = 3000;
        }

        private void OnGUI()
        {
            GUILayout.Label("Level Maker Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Edit Mode Selection
            EditorGUILayout.LabelField("Edit Mode", EditorStyles.boldLabel);
            currentMode = (EditMode)GUILayout.SelectionGrid((int)currentMode, 
                new string[] { "None", "Place", "Resize", "Boolean" }, 2);
            EditorGUILayout.Space();

            if (currentMode == EditMode.Place)
            {
                DrawPlacementUI();
            }
            else if (currentMode == EditMode.Resize)
            {
                DrawResizeUI();
            }
            else if (currentMode == EditMode.Boolean)
            {
                DrawBooleanUI();
            }

            EditorGUILayout.Space();
            DrawUtilityUI();
        }

        private void DrawPlacementUI()
        {
            EditorGUILayout.LabelField("Placement Settings", EditorStyles.boldLabel);
            
            selectedPrimitive = (PrimitiveType)EditorGUILayout.EnumPopup("Primitive Type", selectedPrimitive);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Layer Control", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Layer -", GUILayout.Width(80)))
            {
                currentLayer--;
            }
            EditorGUILayout.LabelField($"Current Layer: {currentLayer}", EditorStyles.boldLabel);
            if (GUILayout.Button("Layer +", GUILayout.Width(80)))
            {
                currentLayer++;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
            parentTransform = (Transform)EditorGUILayout.ObjectField("Parent", parentTransform, typeof(Transform), true);
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Left Click: Place object\nRight Click: Delete object\nLayer +/-: Change height level\n\nBlocks above ground (Layer 0) must connect to adjacent blocks.\nInfinite grid - no boundaries!", MessageType.Info);
        }

        private void DrawResizeUI()
        {
            EditorGUILayout.LabelField("Resize/Move Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select an object to move it or change its grid size.", MessageType.Info);
            
            if (Selection.activeGameObject != null)
            {
                EditorGUILayout.ObjectField("Selected", Selection.activeGameObject, typeof(GameObject), true);
                
                BlockData blockData = Selection.activeGameObject.GetComponent<BlockData>();
                if (blockData != null && gridManager != null)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);
                    
                    EditorGUI.BeginChangeCheck();
                    Vector3Int newGridSize = EditorGUILayout.Vector3IntField("Size (cells)", blockData.gridSize);
                    newGridSize = new Vector3Int(
                        Mathf.Max(1, newGridSize.x),
                        Mathf.Max(1, newGridSize.y),
                        Mathf.Max(1, newGridSize.z)
                    );
                    
                    if (EditorGUI.EndChangeCheck())
                    {
                        // Free old cells
                        gridManager.FreeCells(Selection.activeGameObject);
                        
                        // Check if new size fits
                        if (!gridManager.AreCellsOccupied(blockData.gridPosition, newGridSize, Selection.activeGameObject))
                        {
                            Undo.RecordObject(blockData, "Change Grid Size");
                            Undo.RecordObject(Selection.activeGameObject.transform, "Change Grid Size");
                            
                            blockData.gridSize = newGridSize;
                            Vector3 worldScale = gridManager.GetWorldScale(newGridSize);
                            Selection.activeGameObject.transform.localScale = worldScale;
                            
                            // Occupy new cells
                            gridManager.OccupyCells(blockData.gridPosition, newGridSize, Selection.activeGameObject, blockData.blockType);
                        }
                        else
                        {
                            // Re-occupy old cells if resize failed
                            gridManager.OccupyCells(blockData.gridPosition, blockData.gridSize, Selection.activeGameObject, blockData.blockType);
                            EditorUtility.DisplayDialog("Cannot Resize", "The new size would overlap with existing objects.", "OK");
                        }
                    }
                }
            }
        }

        private void DrawBooleanUI()
        {
            EditorGUILayout.LabelField("Boolean Operations", EditorStyles.boldLabel);
            booleanOp = (BooleanOperation)EditorGUILayout.EnumPopup("Operation", booleanOp);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Selected Objects: {booleanSelection.Count}");
            
            if (GUILayout.Button("Add Selected to Boolean List"))
            {
                if (Selection.activeGameObject != null && !booleanSelection.Contains(Selection.activeGameObject))
                {
                    booleanSelection.Add(Selection.activeGameObject);
                }
            }

            if (GUILayout.Button("Clear Boolean List"))
            {
                booleanSelection.Clear();
            }

            GUI.enabled = booleanSelection.Count >= 2;
            if (GUILayout.Button("Perform Boolean Operation"))
            {
                PerformBooleanOperation();
            }
            GUI.enabled = true;

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Select 2 or more objects to perform boolean operations.", MessageType.Info);
        }

        private void DrawUtilityUI()
        {
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Create Level Container"))
            {
                CreateLevelContainer();
            }

            if (GUILayout.Button("Group Selected"))
            {
                GroupSelected();
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
            
            if (currentMode == EditMode.Place)
            {
                FindOrCreateGridManager();
                if (gridManager == null) return;
                
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                
                // Calculate Y position based on current layer
                float targetY = currentLayer * cellSize;
                
                // Raycast to find placement position on current layer or on existing objects
                bool hitSomething = false;
                Vector3 hitPoint = Vector3.zero;
                
                // First try raycast to existing objects
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    hitPoint = hit.point;
                    
                    // Snap to grid and move to next layer above hit object
                    Vector3 snappedPos = gridManager.SnapToGrid(hitPoint);
                    Vector3Int hitGridPos = gridManager.WorldToGrid(snappedPos);
                    
                    // Place on top of the hit object
                    gridPlacementPosition = new Vector3Int(hitGridPos.x, hitGridPos.y + 1, hitGridPos.z);
                    hitSomething = true;
                }
                
                // If didn't hit anything, use plane at current layer
                if (!hitSomething)
                {
                    Plane plane = new Plane(Vector3.up, new Vector3(0, targetY, 0));
                    if (plane.Raycast(ray, out float distance))
                    {
                        hitPoint = ray.GetPoint(distance);
                        Vector3 snappedPos = gridManager.SnapToGrid(hitPoint);
                        gridPlacementPosition = gridManager.WorldToGrid(snappedPos);
                        gridPlacementPosition.y = currentLayer; // Force to current layer
                        hitSomething = true;
                    }
                }
                
                if (hitSomething)
                {
                    placementPosition = gridManager.GridToWorld(gridPlacementPosition);
                    
                    // Check if placement is valid
                    bool notOccupied = !gridManager.AreCellsOccupied(gridPlacementPosition, gridSize);
                    
                    // For layers above 0, require adjacent connection
                    bool hasConnection = currentLayer == 0 || gridManager.HasAdjacentOccupiedCell(gridPlacementPosition, gridSize);
                    
                    validPlacement = notOccupied && hasConnection;
                    
                    // Set invalid reason
                    if (!notOccupied)
                    {
                        invalidReason = "Cell occupied";
                    }
                    else if (!hasConnection)
                    {
                        invalidReason = "No adjacent block";
                    }
                    else
                    {
                        invalidReason = "";
                    }
                    
                    UpdatePreview();
                }
                else
                {
                    DestroyPreview();
                }

                // Draw grid at current layer
                DrawCurrentLayerGrid();
                
                // Draw grid overlay info
                DrawGridOverlay();

                // Handle left click to place
                if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
                {
                    if (validPlacement)
                    {
                        PlaceObject();
                        e.Use();
                    }
                }

                // Handle right click to delete
                if (e.type == EventType.MouseDown && e.button == 1 && !e.alt)
                {
                    DeleteObjectAtMouse(ray);
                    e.Use();
                }

                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            }
            else if (currentMode == EditMode.Resize)
            {
                HandleResizeMode();
            }
        }
        
        private void DrawCurrentLayerGrid()
        {
            if (gridManager == null) return;
            
            float yPos = currentLayer * cellSize;
            int size = 20; // Visual grid size
            
            Handles.color = new Color(0.3f, 0.8f, 0.3f, 0.5f);
            
            // Draw grid lines at current layer
            for (int i = -size; i <= size; i++)
            {
                Vector3 start = new Vector3(-size * cellSize, yPos, i * cellSize);
                Vector3 end = new Vector3(size * cellSize, yPos, i * cellSize);
                Handles.DrawLine(start, end);
                
                start = new Vector3(i * cellSize, yPos, -size * cellSize);
                end = new Vector3(i * cellSize, yPos, size * cellSize);
                Handles.DrawLine(start, end);
            }
        }

        private void DrawGridOverlay()
        {
            if (gridManager == null) return;

            Handles.BeginGUI();
            GUILayout.BeginArea(new Rect(10, 10, 300, 120));
            
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f));
            style.normal.textColor = Color.white;
            style.padding = new RectOffset(10, 10, 10, 10);
            
            GUILayout.BeginVertical(style);
            GUILayout.Label($"Current Layer: {currentLayer} (Y={currentLayer * cellSize}m)", EditorStyles.whiteLargeLabel);
            GUILayout.Label($"Grid Position: {gridPlacementPosition}", EditorStyles.whiteLargeLabel);
            GUILayout.Label($"Grid Size: {gridSize.x}x{gridSize.y}x{gridSize.z}", EditorStyles.whiteLargeLabel);
            
            string statusText = validPlacement ? "✓ Valid" : $"✗ Invalid ({invalidReason})";
            GUILayout.Label($"Status: {statusText}", EditorStyles.whiteLargeLabel);
            
            GUILayout.Label($"Cell Size: {cellSize}m", EditorStyles.whiteLargeLabel);
            GUILayout.EndVertical();
            
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void UpdatePreview()
        {
            if (previewObject == null)
            {
                CreatePreview();
            }

            if (previewObject != null && gridManager != null)
            {
                // Set position to grid position
                previewObject.transform.position = placementPosition;
                previewObject.transform.rotation = Quaternion.identity;
                
                // Set scale based on grid size
                Vector3 worldScale = gridManager.GetWorldScale(gridSize);
                previewObject.transform.localScale = worldScale;
                
                var renderer = previewObject.GetComponent<Renderer>();
                if (renderer != null)
                {
                    previewMaterial.color = validPlacement ? placementColor : invalidColor;
                }
            }
        }

        private void CreatePreview()
        {
            DestroyPreview();
            
            previewObject = GameObject.CreatePrimitive(GetUnityPrimitiveType());
            previewObject.hideFlags = HideFlags.HideAndDontSave;
            
            var collider = previewObject.GetComponent<Collider>();
            if (collider != null)
                DestroyImmediate(collider);

            var renderer = previewObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = previewMaterial;
            }
        }

        private void DestroyPreview()
        {
            if (previewObject != null)
            {
                DestroyImmediate(previewObject);
                previewObject = null;
            }
        }

        private void PlaceObject()
        {
            if (gridManager == null) return;

            GameObject obj = GameObject.CreatePrimitive(GetUnityPrimitiveType());
            obj.name = $"{selectedPrimitive}_{gridSize.x}x{gridSize.y}x{gridSize.z}";
            obj.transform.position = placementPosition;
            obj.transform.rotation = Quaternion.identity;
            
            Vector3 worldScale = gridManager.GetWorldScale(gridSize);
            obj.transform.localScale = worldScale;

            if (parentTransform != null)
            {
                obj.transform.SetParent(parentTransform);
            }
            else if (gridManager.transform != parentTransform)
            {
                obj.transform.SetParent(gridManager.transform);
            }

            var blockData = obj.AddComponent<BlockData>();
            blockData.blockType = selectedPrimitive.ToString();
            blockData.originalSize = worldScale;
            blockData.gridPosition = gridPlacementPosition;
            blockData.gridSize = gridSize;

            // Register with grid manager
            gridManager.OccupyCells(gridPlacementPosition, gridSize, obj, blockData.blockType);

            Undo.RegisterCreatedObjectUndo(obj, "Place Block");
            Selection.activeGameObject = obj;
        }

        private void DeleteObjectAtMouse(Ray ray)
        {
            if (gridManager == null) return;

            // Raycast to find object to delete
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GameObject hitObject = hit.collider.gameObject;
                BlockData blockData = hitObject.GetComponent<BlockData>();
                
                if (blockData != null)
                {
                    // Free cells in grid
                    gridManager.FreeCells(hitObject);
                    
                    // Delete the object
                    Undo.DestroyObjectImmediate(hitObject);
                }
            }
        }

        private void HandleResizeMode()
        {
            if (Selection.activeGameObject != null && gridManager != null)
            {
                Transform t = Selection.activeGameObject.transform;
                BlockData blockData = t.GetComponent<BlockData>();
                
                if (blockData != null)
                {
                    // Draw current grid size info
                    Handles.BeginGUI();
                    GUILayout.BeginArea(new Rect(10, 10, 300, 100));
                    
                    GUIStyle style = new GUIStyle(GUI.skin.box);
                    style.normal.background = MakeTex(2, 2, new Color(0, 0, 0, 0.7f));
                    style.normal.textColor = Color.white;
                    style.padding = new RectOffset(10, 10, 10, 10);
                    
                    GUILayout.BeginVertical(style);
                    GUILayout.Label($"Grid Size: {blockData.gridSize}", EditorStyles.whiteLargeLabel);
                    GUILayout.Label($"Grid Position: {blockData.gridPosition}", EditorStyles.whiteLargeLabel);
                    GUILayout.Label("Use Inspector to change grid size", EditorStyles.whiteLargeLabel);
                    GUILayout.EndVertical();
                    
                    GUILayout.EndArea();
                    Handles.EndGUI();

                    // Draw grid bounds
                    Handles.color = Color.cyan;
                    Vector3 worldScale = gridManager.GetWorldScale(blockData.gridSize);
                    Handles.DrawWireCube(t.position, worldScale);
                    
                    // Draw position handle
                    EditorGUI.BeginChangeCheck();
                    Vector3 newPos = Handles.PositionHandle(t.position, t.rotation);
                    if (EditorGUI.EndChangeCheck())
                    {
                        // Free old cells
                        gridManager.FreeCells(t.gameObject);
                        
                        // Snap to grid
                        Vector3 snappedPos = gridManager.SnapToGrid(newPos);
                        Vector3Int newGridPos = gridManager.WorldToGrid(snappedPos);
                        
                        // Check if new position is valid
                        if (!gridManager.AreCellsOccupied(newGridPos, blockData.gridSize, t.gameObject))
                        {
                            Undo.RecordObject(t, "Move Block");
                            Undo.RecordObject(blockData, "Move Block");
                            
                            t.position = snappedPos;
                            blockData.gridPosition = newGridPos;
                            gridManager.OccupyCells(newGridPos, blockData.gridSize, t.gameObject, blockData.blockType);
                        }
                        else
                        {
                            // Re-occupy old cells if move failed
                            gridManager.OccupyCells(blockData.gridPosition, blockData.gridSize, t.gameObject, blockData.blockType);
                        }
                    }
                }
            }
        }

        private void PerformBooleanOperation()
        {
            if (booleanSelection.Count < 2) return;

            GameObject baseObj = booleanSelection[0];
            
            for (int i = 1; i < booleanSelection.Count; i++)
            {
                CSGOperations.PerformOperation(baseObj, booleanSelection[i], booleanOp.ToString());
            }

            booleanSelection.Clear();
            EditorUtility.DisplayDialog("Boolean Operation", "Boolean operation completed!\nNote: This is a simplified boolean operation.", "OK");
        }

        private UnityEngine.PrimitiveType GetUnityPrimitiveType()
        {
            switch (selectedPrimitive)
            {
                case PrimitiveType.Cube: return UnityEngine.PrimitiveType.Cube;
                case PrimitiveType.Sphere: return UnityEngine.PrimitiveType.Sphere;
                case PrimitiveType.Cylinder: return UnityEngine.PrimitiveType.Cylinder;
                case PrimitiveType.Capsule: return UnityEngine.PrimitiveType.Capsule;
                case PrimitiveType.Plane: return UnityEngine.PrimitiveType.Plane;
                default: return UnityEngine.PrimitiveType.Cube;
            }
        }

        private void CreateLevelContainer()
        {
            GameObject container = new GameObject("LevelContainer");
            Undo.RegisterCreatedObjectUndo(container, "Create Level Container");
            Selection.activeGameObject = container;
        }

        private void GroupSelected()
        {
            if (Selection.gameObjects.Length > 0)
            {
                GameObject group = new GameObject("Group");
                Undo.RegisterCreatedObjectUndo(group, "Group Objects");
                
                Vector3 center = Vector3.zero;
                foreach (var obj in Selection.gameObjects)
                {
                    center += obj.transform.position;
                }
                center /= Selection.gameObjects.Length;
                group.transform.position = center;

                foreach (var obj in Selection.gameObjects)
                {
                    Undo.SetTransformParent(obj.transform, group.transform, "Group Objects");
                }

                Selection.activeGameObject = group;
            }
        }
    }
}
