using UnityEngine;
using System.Collections.Generic;

namespace LevelMaker
{
    /// <summary>
    /// Block metadata stored by GridManager
    /// </summary>
    [System.Serializable]
    public struct BlockMetadata
    {
        public Vector3Int gridPosition;
        public Vector3Int gridSize;
        public string blockType; // Primitive type or prefab name
        
        public BlockMetadata(Vector3Int gridPos, Vector3Int size, string type)
        {
            gridPosition = gridPos;
            gridSize = size;
            blockType = type;
        }
    }
    
    /// <summary>
    /// Manages the grid system for level layout
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private Vector3 gridOrigin = Vector3.zero;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private int gridVisualSize = 20; // Only for visualization
        
        [Header("Visual Settings")]
        [SerializeField] private bool showGrid = true;
        [SerializeField] private bool showOccupiedCells = false; // Disabled by default for performance
        [SerializeField] private Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        [SerializeField] private Color occupiedColor = new Color(1f, 0.5f, 0.5f, 0.3f);
        
        private Dictionary<Vector3Int, GameObject> occupiedCells = new Dictionary<Vector3Int, GameObject>();
        private Dictionary<GameObject, BlockMetadata> blockMetadata = new Dictionary<GameObject, BlockMetadata>();

        public Vector3 GridOrigin => gridOrigin;
        public float CellSize => cellSize;
        public bool ShowGrid => showGrid;

        private void Awake()
        {
            // Initialize grid data
        }

        /// <summary>
        /// Snap world position to nearest grid cell center
        /// </summary>
        public Vector3 SnapToGrid(Vector3 worldPosition)
        {
            Vector3 localPos = worldPosition - gridOrigin;
            
            int x = Mathf.RoundToInt(localPos.x / cellSize);
            int y = Mathf.RoundToInt(localPos.y / cellSize);
            int z = Mathf.RoundToInt(localPos.z / cellSize);
            
            return gridOrigin + new Vector3(x * cellSize, y * cellSize, z * cellSize);
        }

        /// <summary>
        /// Convert world position to grid coordinates
        /// </summary>
        public Vector3Int WorldToGrid(Vector3 worldPosition)
        {
            Vector3 localPos = worldPosition - gridOrigin;
            
            return new Vector3Int(
                Mathf.RoundToInt(localPos.x / cellSize),
                Mathf.RoundToInt(localPos.y / cellSize),
                Mathf.RoundToInt(localPos.z / cellSize)
            );
        }

        /// <summary>
        /// Convert grid coordinates to world position
        /// </summary>
        public Vector3 GridToWorld(Vector3Int gridPosition)
        {
            return gridOrigin + new Vector3(
                gridPosition.x * cellSize,
                gridPosition.y * cellSize,
                gridPosition.z * cellSize
            );
        }

        /// <summary>
        /// Check if grid position is within bounds (infinite grid - always true)
        /// </summary>
        public bool IsInBounds(Vector3Int gridPosition)
        {
            // Infinite grid - no bounds checking
            return true;
        }

        /// <summary>
        /// Check if cells are occupied by an object
        /// </summary>
        public bool AreCellsOccupied(Vector3Int gridPosition, Vector3Int size, GameObject ignoreObject = null)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        Vector3Int cellPos = gridPosition + new Vector3Int(x, y, z);
                        
                        if (!IsInBounds(cellPos))
                            return true;
                            
                        if (occupiedCells.ContainsKey(cellPos))
                        {
                            if (ignoreObject == null || occupiedCells[cellPos] != ignoreObject)
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Occupy cells with an object and store metadata
        /// </summary>
        public void OccupyCells(Vector3Int gridPosition, Vector3Int size, GameObject obj, string blockType)
        {
            // Store metadata
            blockMetadata[obj] = new BlockMetadata(gridPosition, size, blockType);
            
            // Occupy cells
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        Vector3Int cellPos = gridPosition + new Vector3Int(x, y, z);
                        if (IsInBounds(cellPos))
                        {
                            occupiedCells[cellPos] = obj;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Free cells occupied by an object and remove metadata
        /// </summary>
        public void FreeCells(GameObject obj)
        {
            // Remove metadata
            blockMetadata.Remove(obj);
            
            // Free cells
            List<Vector3Int> toRemove = new List<Vector3Int>();
            
            foreach (var kvp in occupiedCells)
            {
                if (kvp.Value == obj)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var pos in toRemove)
            {
                occupiedCells.Remove(pos);
            }
        }

        /// <summary>
        /// Clear all occupied cells and metadata
        /// </summary>
        public void ClearOccupiedCells()
        {
            occupiedCells.Clear();
            blockMetadata.Clear();
        }

        /// <summary>
        /// Get size in grid units from world scale
        /// </summary>
        public Vector3Int GetGridSize(Vector3 worldScale)
        {
            return new Vector3Int(
                Mathf.Max(1, Mathf.RoundToInt(worldScale.x / cellSize)),
                Mathf.Max(1, Mathf.RoundToInt(worldScale.y / cellSize)),
                Mathf.Max(1, Mathf.RoundToInt(worldScale.z / cellSize))
            );
        }

        /// <summary>
        /// Get world scale from grid size
        /// </summary>
        public Vector3 GetWorldScale(Vector3Int gridSize)
        {
            return new Vector3(
                gridSize.x * cellSize,
                gridSize.y * cellSize,
                gridSize.z * cellSize
            );
        }

        /// <summary>
        /// Get GameObject at specific grid position
        /// </summary>
        public GameObject GetObjectAtPosition(Vector3Int gridPosition)
        {
            if (occupiedCells.TryGetValue(gridPosition, out GameObject obj))
            {
                return obj;
            }
            return null;
        }

        /// <summary>
        /// Get block metadata for a GameObject
        /// </summary>
        public bool TryGetBlockMetadata(GameObject obj, out BlockMetadata metadata)
        {
            return blockMetadata.TryGetValue(obj, out metadata);
        }

        /// <summary>
        /// Check if GameObject is a placed block (has metadata)
        /// </summary>
        public bool IsPlacedBlock(GameObject obj)
        {
            return blockMetadata.ContainsKey(obj);
        }

        /// <summary>
        /// Get all placed blocks
        /// </summary>
        public Dictionary<GameObject, BlockMetadata> GetAllBlocks()
        {
            return new Dictionary<GameObject, BlockMetadata>(blockMetadata);
        }

        /// <summary>
        /// Check if there are adjacent occupied cells (non-diagonal, 6 directions)
        /// Used to ensure new blocks connect to existing ones
        /// </summary>
        public bool HasAdjacentOccupiedCell(Vector3Int gridPosition, Vector3Int size)
        {
            // Check 6 adjacent directions (no diagonals): +X, -X, +Y, -Y, +Z, -Z
            Vector3Int[] adjacentOffsets = new Vector3Int[]
            {
                new Vector3Int(1, 0, 0),   // Right
                new Vector3Int(-1, 0, 0),  // Left
                new Vector3Int(0, 1, 0),   // Up
                new Vector3Int(0, -1, 0),  // Down
                new Vector3Int(0, 0, 1),   // Forward
                new Vector3Int(0, 0, -1)   // Back
            };

            // Check all cells that this object will occupy
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        Vector3Int cellPos = gridPosition + new Vector3Int(x, y, z);
                        
                        // Check each of the 6 adjacent directions for this cell
                        foreach (var offset in adjacentOffsets)
                        {
                            Vector3Int adjacentPos = cellPos + offset;
                            
                            // If adjacent cell is occupied, we have a connection
                            if (occupiedCells.ContainsKey(adjacentPos))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            
            return false;
        }

        private void OnDrawGizmos()
        {
            if (!showGrid) return;

            // Draw grid lines - infinite but only show around origin
            Gizmos.color = gridColor;
            
            int halfSize = gridVisualSize / 2;
            
            // Draw horizontal grid at Y = 0 (main ground plane)
            for (int z = -halfSize; z <= halfSize; z++)
            {
                Vector3 start = gridOrigin + new Vector3(-halfSize * cellSize, 0, z * cellSize);
                Vector3 end = gridOrigin + new Vector3(halfSize * cellSize, 0, z * cellSize);
                Gizmos.DrawLine(start, end);
            }
            
            for (int x = -halfSize; x <= halfSize; x++)
            {
                Vector3 start = gridOrigin + new Vector3(x * cellSize, 0, -halfSize * cellSize);
                Vector3 end = gridOrigin + new Vector3(x * cellSize, 0, halfSize * cellSize);
                Gizmos.DrawLine(start, end);
            }

            // Draw occupied cells (disabled by default for performance)
            if (showOccupiedCells)
            {
                Gizmos.color = occupiedColor;
                foreach (var kvp in occupiedCells)
                {
                    Vector3 worldPos = GridToWorld(kvp.Key);
                    Gizmos.DrawWireCube(worldPos, Vector3.one * cellSize * 0.9f);
                }
            }
        }
    }
}
