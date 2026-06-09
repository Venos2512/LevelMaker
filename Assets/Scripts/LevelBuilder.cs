using UnityEngine;

namespace LevelMaker
{
    /// <summary>
    /// Runtime level building system - top-down placement with mouse
    /// Controls: Left Click to place, Right Click to delete, Shift+Click to replace
    /// </summary>
    public class LevelBuilder : MonoBehaviour
    {
        [Header("Building Settings")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private float maxPlaceDistance = 200f;
        [SerializeField] private LayerMask placementMask = ~0;
        
        [Header("Block Types")]
        [SerializeField] private PrimitiveType currentBlockType = PrimitiveType.Cube;
        [SerializeField] private Vector3Int currentBlockSize = Vector3Int.one;
        
        [Header("Preview Settings")]
        [SerializeField] private Color validColor = new Color(0.3f, 1f, 0.3f, 1f); // Green wireframe
        [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f, 1f); // Red wireframe
        [SerializeField] private Color hoverColor = new Color(0.5f, 0.9f, 1f, 0.3f);
        
        [Header("Current Layer")]
        [SerializeField] private int currentLayer = 0;
        
        private GameObject hoverIndicator;
        private Vector3 hoverPosition;
        private Vector3Int gridHoverPosition;
        private bool validPlacement;
        private bool isMouseOverGrid;
        private Vector3 previewSize; // Store size for wireframe drawing
        
        // Debug info
        private bool notOccupied;
        private bool hasConnection;
        
        // Cached components for performance
        private Camera mainCamera;
        private Renderer hoverRenderer;

        private void Start()
        {
            // Cache main camera
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
        }

        private void Update()
        {
            UpdateMouseHover();
            HandleInput();
        }

        /// <summary>
        /// Draw a wireframe box for preview with bottom at specified position
        /// </summary>
        private void DrawWireframeBox(Vector3 bottomCenter, Vector3 size, Color color)
        {
            // Offset to center since box is drawn from center
            Vector3 center = bottomCenter + new Vector3(0, size.y * 0.5f, 0);
            
            // Calculate half extents
            Vector3 halfSize = size * 0.5f;
            
            // Define 8 corners of the box
            Vector3[] corners = new Vector3[8];
            corners[0] = center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
            corners[1] = center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
            corners[2] = center + new Vector3(halfSize.x, -halfSize.y, halfSize.z);
            corners[3] = center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
            corners[4] = center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
            corners[5] = center + new Vector3(halfSize.x, halfSize.y, -halfSize.z);
            corners[6] = center + new Vector3(halfSize.x, halfSize.y, halfSize.z);
            corners[7] = center + new Vector3(-halfSize.x, halfSize.y, halfSize.z);
            
            // Draw bottom face
            Debug.DrawLine(corners[0], corners[1], color);
            Debug.DrawLine(corners[1], corners[2], color);
            Debug.DrawLine(corners[2], corners[3], color);
            Debug.DrawLine(corners[3], corners[0], color);
            
            // Draw top face
            Debug.DrawLine(corners[4], corners[5], color);
            Debug.DrawLine(corners[5], corners[6], color);
            Debug.DrawLine(corners[6], corners[7], color);
            Debug.DrawLine(corners[7], corners[4], color);
            
            // Draw vertical edges
            Debug.DrawLine(corners[0], corners[4], color);
            Debug.DrawLine(corners[1], corners[5], color);
            Debug.DrawLine(corners[2], corners[6], color);
            Debug.DrawLine(corners[3], corners[7], color);
            
            // Draw thicker bottom edges for better visibility
            Color bottomColor = new Color(color.r, color.g, color.b, 1f); // Full opacity for bottom
            Debug.DrawLine(corners[0], corners[1], bottomColor);
            Debug.DrawLine(corners[1], corners[2], bottomColor);
            Debug.DrawLine(corners[2], corners[3], bottomColor);
            Debug.DrawLine(corners[3], corners[0], bottomColor);
        }

        private void CreateHoverIndicator()
        {
            hoverIndicator = GameObject.CreatePrimitive(PrimitiveType.Plane);
            hoverIndicator.name = "HoverIndicator";
            // Plane default size is 10x10, so scale by 0.1 to make it 1x1
            hoverIndicator.transform.localScale = Vector3.one * gridManager.CellSize * 0.1f;
            
            var collider = hoverIndicator.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
            
            // Create transparent material for hover plane
            var mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            mat.color = hoverColor;
            
            hoverRenderer = hoverIndicator.GetComponent<Renderer>();
            if (hoverRenderer != null)
            {
                hoverRenderer.sharedMaterial = mat;
            }
            
            hoverIndicator.SetActive(false);
        }

        private void UpdateMouseHover()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            isMouseOverGrid = false;
            
            // Raycast to find hover position
            if (Physics.Raycast(ray, out RaycastHit hit, maxPlaceDistance, placementMask))
            {
                hoverPosition = hit.point;
                GameObject hitObject = hit.collider.gameObject;
                
                // Check if we're hovering over an existing block
                if (gridManager.TryGetBlockMetadata(hitObject, out BlockMetadata hitMetadata))
                {
                    // Hovering over existing block - place on top of it
                    gridHoverPosition = hitMetadata.gridPosition;
                    gridHoverPosition.y += hitMetadata.gridSize.y; // Place on top
                    hoverPosition = gridManager.GridToWorld(gridHoverPosition);
                }
                else
                {
                    // Hovering over ground or other object - snap to grid at current layer
                    Vector3 snappedPos = gridManager.SnapToGrid(hoverPosition);
                    gridHoverPosition = gridManager.WorldToGrid(snappedPos);
                    gridHoverPosition.y = currentLayer;
                    hoverPosition = gridManager.GridToWorld(gridHoverPosition);
                }
                
                isMouseOverGrid = true;
                
                // Check if placement is valid
                notOccupied = !gridManager.AreCellsOccupied(gridHoverPosition, currentBlockSize);
                hasConnection = currentLayer == 0 || gridManager.HasAdjacentOccupiedCell(gridHoverPosition, currentBlockSize);
                validPlacement = notOccupied && hasConnection;
            }
            else
            {
                // Raycast to plane at current layer if nothing was hit
                float targetY = currentLayer * gridManager.CellSize;
                Plane plane = new Plane(Vector3.up, new Vector3(0, targetY, 0));
                
                if (plane.Raycast(ray, out float distance))
                {
                    hoverPosition = ray.GetPoint(distance);
                    Vector3 snappedPos = gridManager.SnapToGrid(hoverPosition);
                    gridHoverPosition = gridManager.WorldToGrid(snappedPos);
                    gridHoverPosition.y = currentLayer;
                    hoverPosition = gridManager.GridToWorld(gridHoverPosition);
                    
                    isMouseOverGrid = true;
                    
                    notOccupied = !gridManager.AreCellsOccupied(gridHoverPosition, currentBlockSize);
                    hasConnection = currentLayer == 0 || gridManager.HasAdjacentOccupiedCell(gridHoverPosition, currentBlockSize);
                    validPlacement = notOccupied && hasConnection;
                }
            }
            
            // Update preview and hover indicator
            if (isMouseOverGrid)
            {
                // Calculate bottom position (grid position is at bottom of cell)
                Vector3 bottomPosition = hoverPosition;
                
                // Draw wireframe preview (from bottom)
                Vector3 worldScale = gridManager.GetWorldScale(currentBlockSize);
                previewSize = GetCorrectedScale(worldScale, currentBlockType);
                Color previewColor = validPlacement ? validColor : invalidColor;
                DrawWireframeBox(bottomPosition, previewSize, previewColor);
                
                // Update hover indicator (ground plane at bottom, sized to match block)
                hoverIndicator.SetActive(true);
                Vector3 indicatorPos = bottomPosition;
                indicatorPos.y += 0.01f; // Slightly above ground to prevent z-fighting
                hoverIndicator.transform.position = indicatorPos;
                
                // Scale hover plane to match block footprint (Plane is 10x10, so scale by 0.1 * size)
                float scaleX = previewSize.x * 0.1f;
                float scaleZ = previewSize.z * 0.1f;
                hoverIndicator.transform.localScale = new Vector3(scaleX, 1f, scaleZ);
            }
            else
            {
                hoverIndicator.SetActive(false);
            }
        }

        private void HandleInput()
        {
            // Left click to place
            if (Input.GetMouseButtonDown(0) && isMouseOverGrid)
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    // Shift+Click to replace
                    GameObject existingBlock = gridManager.GetObjectAtPosition(gridHoverPosition);
                    if (existingBlock != null)
                    {
                        ReplaceBlock(existingBlock);
                    }
                    else if (validPlacement)
                    {
                        PlaceBlock();
                    }
                }
                else if (validPlacement)
                {
                    PlaceBlock();
                }
            }
            
            // Right click to delete
            if (Input.GetMouseButtonDown(1) && isMouseOverGrid)
            {
                DeleteBlockAtPosition(gridHoverPosition);
            }
            
            // Number keys to change block type
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetBlockType(PrimitiveType.Cube);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetBlockType(PrimitiveType.Sphere);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetBlockType(PrimitiveType.Cylinder);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetBlockType(PrimitiveType.Capsule);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SetBlockType(PrimitiveType.Plane);
            
            // [ ] to change layer
            if (Input.GetKeyDown(KeyCode.LeftBracket))
            {
                currentLayer = Mathf.Max(0, currentLayer - 1);
            }
            if (Input.GetKeyDown(KeyCode.RightBracket))
            {
                currentLayer++;
            }
        }

        public void SetBlockType(PrimitiveType type)
        {
            currentBlockType = type;
            // Preview is drawn as wireframe in UpdateMouseHover, no need to recreate object
        }

        public void SetBlockSize(Vector3Int size)
        {
            currentBlockSize = new Vector3Int(
                Mathf.Max(1, size.x),
                Mathf.Max(1, size.y),
                Mathf.Max(1, size.z)
            );
        }

        /// <summary>
        /// Get corrected scale for primitive types with non-standard default sizes
        /// </summary>
        private Vector3 GetCorrectedScale(Vector3 baseScale, PrimitiveType primitiveType)
        {
            Vector3 corrected = baseScale;
            
            switch (primitiveType)
            {
                case PrimitiveType.Plane:
                    // Plane is 10x10 by default, not 1x1
                    corrected.x *= 0.1f;
                    corrected.z *= 0.1f;
                    break;
                    
                case PrimitiveType.Cylinder:
                case PrimitiveType.Capsule:
                    // Cylinder and Capsule are height 2 by default, not 1
                    corrected.y *= 0.5f;
                    break;
            }
            
            return corrected;
        }

        private void PlaceBlock()
        {
            GameObject obj = GameObject.CreatePrimitive(currentBlockType);
            obj.name = $"{currentBlockType}_{currentBlockSize.x}x{currentBlockSize.y}x{currentBlockSize.z}";
            
            // Calculate placement position (primitives have center pivot, so offset up by half height)
            Vector3 worldScale = gridManager.GetWorldScale(currentBlockSize);
            Vector3 correctedScale = GetCorrectedScale(worldScale, currentBlockType);
            Vector3 placementPosition = hoverPosition + new Vector3(0, correctedScale.y * 0.5f, 0);
            
            obj.transform.position = placementPosition;
            obj.transform.rotation = Quaternion.identity;
            obj.transform.localScale = correctedScale;
            obj.transform.SetParent(gridManager.transform);

            // Register with grid manager (metadata stored in GridManager)
            gridManager.OccupyCells(gridHoverPosition, currentBlockSize, obj, currentBlockType.ToString());
        }

        private void ReplaceBlock(GameObject oldBlock)
        {
            // Get old block metadata from GridManager
            if (!gridManager.TryGetBlockMetadata(oldBlock, out BlockMetadata oldMetadata))
                return;
            
            gridManager.FreeCells(oldBlock);
            
            GameObject newBlock = GameObject.CreatePrimitive(currentBlockType);
            newBlock.name = $"{currentBlockType}_{currentBlockSize.x}x{currentBlockSize.y}x{currentBlockSize.z}";
            
            // Calculate placement position (offset up by half height)
            Vector3 worldScale = gridManager.GetWorldScale(currentBlockSize);
            Vector3 correctedScale = GetCorrectedScale(worldScale, currentBlockType);
            Vector3 placementPosition = hoverPosition + new Vector3(0, correctedScale.y * 0.5f, 0);
            
            newBlock.transform.position = placementPosition;
            newBlock.transform.rotation = Quaternion.identity;
            newBlock.transform.localScale = correctedScale;
            newBlock.transform.SetParent(gridManager.transform);

            // Register with grid manager (metadata stored in GridManager)
            gridManager.OccupyCells(gridHoverPosition, currentBlockSize, newBlock, currentBlockType.ToString());
            
            Destroy(oldBlock);
        }

        private void DeleteBlockAtPosition(Vector3Int gridPosition)
        {
            GameObject block = gridManager.GetObjectAtPosition(gridPosition);
            if (block != null && gridManager.IsPlacedBlock(block))
            {
                gridManager.FreeCells(block);
                Destroy(block);
            }
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 14;
            style.alignment = TextAnchor.UpperLeft;
            style.normal.textColor = Color.white;
            
            string instructions = 
                $"=== LEVEL BUILDER (Top-Down) ===\n" +
                $"Block: {currentBlockType} (1-5 to change)\n" +
                $"Grid Size: {currentBlockSize.x}x{currentBlockSize.y}x{currentBlockSize.z}\n" +
                $"World Size: {previewSize.x:F1}x{previewSize.y:F1}x{previewSize.z:F1}\n" +
                $"Layer: {currentLayer} ([ ] to change)\n" +
                $"Grid Pos: {gridHoverPosition}\n" +
                $"World Pos (bottom): {hoverPosition:F1}\n" +
                $"\n" +
                $"DEBUG:\n" +
                $"Not Occupied: {(notOccupied ? "✓" : "✗")}\n" +
                $"Has Connection: {(hasConnection ? "✓" : "✗")}\n" +
                $"Valid: {(validPlacement ? "✓ CAN PLACE" : "✗ CANNOT PLACE")}\n" +
                $"\n" +
                $"Controls:\n" +
                $"Left Click: Place block\n" +
                $"Shift + Click: Replace\n" +
                $"Right Click: Delete\n" +
                $"1-5: Block type\n" +
                $"[ ]: Layer ±\n" +
                $"V: Toggle camera mode";
            
            GUI.Box(new Rect(10, 10, 380, 380), instructions, style);
        }
    }
}
