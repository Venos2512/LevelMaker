using UnityEngine;
using UnityEditor;

namespace LevelMaker.Editor
{
    /// <summary>
    /// Custom inspector for GridManager
    /// </summary>
    [CustomEditor(typeof(GridManager))]
    public class GridManagerInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GridManager gridManager = (GridManager)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Clear All Occupied Cells"))
            {
                if (EditorUtility.DisplayDialog("Clear Grid", 
                    "Clear all occupied cell data? (Objects won't be deleted)", 
                    "Yes", "Cancel"))
                {
                    gridManager.ClearOccupiedCells();
                    EditorUtility.DisplayDialog("Done", "Grid data cleared.", "OK");
                }
            }

            if (GUILayout.Button("Rebuild Grid from Scene"))
            {
                RebuildGrid(gridManager);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "Grid Origin: Starting point of the grid\n" +
                "Cell Size: Size of each grid cell in world units\n" +
                "Grid Visual Size: How many cells to show (visualization only)\n" +
                "Show Grid: Display grid lines in Scene view\n" +
                "Infinite Grid: No boundaries, place anywhere!",
                MessageType.Info
            );
        }

        private void RebuildGrid(GridManager gridManager)
        {
            gridManager.ClearOccupiedCells();
            
            BlockData[] allBlocks = FindObjectsOfType<BlockData>();
            int rebuilt = 0;
            
            foreach (var block in allBlocks)
            {
                if (block.gridSize != Vector3Int.zero)
                {
                    Vector3Int gridPos = gridManager.WorldToGrid(block.transform.position);
                    block.gridPosition = gridPos;
                    
                    if (!gridManager.AreCellsOccupied(gridPos, block.gridSize))
                    {
                        gridManager.OccupyCells(gridPos, block.gridSize, block.gameObject, block.blockType);
                        rebuilt++;
                    }
                }
            }
            
            EditorUtility.DisplayDialog("Rebuild Complete", 
                $"Rebuilt grid data for {rebuilt} blocks.", 
                "OK");
        }

        private void OnSceneGUI()
        {
            GridManager gridManager = (GridManager)target;
            
            // Draw grid origin handle
            Handles.color = Color.yellow;
            Handles.Label(gridManager.GridOrigin, "Grid Origin");
            
            EditorGUI.BeginChangeCheck();
            Vector3 newOrigin = Handles.PositionHandle(gridManager.GridOrigin, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(gridManager, "Move Grid Origin");
                // Update via reflection
                var field = typeof(GridManager).GetField("gridOrigin", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                    field.SetValue(gridManager, newOrigin);
            }
        }
    }
}
