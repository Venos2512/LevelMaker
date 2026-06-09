using UnityEngine;
using UnityEditor;

namespace LevelMaker.Editor
{
    /// <summary>
    /// Custom inspector for BlockData component with quick edit tools
    /// </summary>
    [CustomEditor(typeof(BlockData))]
    public class LevelMakerInspector : UnityEditor.Editor
    {
        private GridManager gridManager;

        private void OnEnable()
        {
            gridManager = FindObjectOfType<GridManager>();
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            BlockData blockData = (BlockData)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Information", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Grid Position: {blockData.gridPosition}");
            EditorGUILayout.LabelField($"Grid Size: {blockData.gridSize.x}x{blockData.gridSize.y}x{blockData.gridSize.z}");
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Reset Size"))
            {
                Undo.RecordObject(blockData.transform, "Reset Size");
                blockData.transform.localScale = blockData.originalSize;
            }

            if (GUILayout.Button("Duplicate"))
            {
                DuplicateBlock(blockData);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Delete from Grid"))
            {
                DeleteBlock(blockData);
            }

            if (GUILayout.Button("Convert to Mesh"))
            {
                ConvertToMesh(blockData.gameObject);
            }

            // Grid Size Quick Presets
            if (gridManager != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Grid Size Presets", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("1x1x1")) SetGridSize(blockData, new Vector3Int(1, 1, 1));
                if (GUILayout.Button("2x1x1")) SetGridSize(blockData, new Vector3Int(2, 1, 1));
                if (GUILayout.Button("1x2x1")) SetGridSize(blockData, new Vector3Int(1, 2, 1));
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("2x2x2")) SetGridSize(blockData, new Vector3Int(2, 2, 2));
                if (GUILayout.Button("3x1x1")) SetGridSize(blockData, new Vector3Int(3, 1, 1));
                if (GUILayout.Button("1x3x1")) SetGridSize(blockData, new Vector3Int(1, 3, 1));
                EditorGUILayout.EndHorizontal();
            }
        }

        private void SetGridSize(BlockData blockData, Vector3Int newSize)
        {
            if (gridManager == null)
            {
                EditorUtility.DisplayDialog("No Grid Manager", "Grid Manager not found in scene.", "OK");
                return;
            }

            // Free old cells
            gridManager.FreeCells(blockData.gameObject);
            
            // Check if new size fits
            if (!gridManager.AreCellsOccupied(blockData.gridPosition, newSize, blockData.gameObject))
            {
                Undo.RecordObject(blockData, "Set Grid Size");
                Undo.RecordObject(blockData.transform, "Set Grid Size");
                
                blockData.gridSize = newSize;
                Vector3 worldScale = gridManager.GetWorldScale(newSize);
                blockData.transform.localScale = worldScale;
                
                // Occupy new cells
                gridManager.OccupyCells(blockData.gridPosition, newSize, blockData.gameObject, blockData.blockType);
            }
            else
            {
                // Re-occupy old cells if resize failed
                gridManager.OccupyCells(blockData.gridPosition, blockData.gridSize, blockData.gameObject, blockData.blockType);
                EditorUtility.DisplayDialog("Cannot Resize", "The new size would overlap with existing objects.", "OK");
            }
        }

        private void DuplicateBlock(BlockData blockData)
        {
            if (gridManager == null) return;

            // Try to find empty space near the original
            Vector3Int[] offsets = new Vector3Int[]
            {
                Vector3Int.right,
                Vector3Int.left,
                Vector3Int.forward,
                Vector3Int.back,
                Vector3Int.up
            };

            foreach (var offset in offsets)
            {
                Vector3Int newGridPos = blockData.gridPosition + offset;
                
                if (!gridManager.AreCellsOccupied(newGridPos, blockData.gridSize))
                {
                    GameObject duplicate = Instantiate(blockData.gameObject, blockData.transform.parent);
                    duplicate.transform.position = gridManager.GridToWorld(newGridPos);
                    
                    BlockData newBlockData = duplicate.GetComponent<BlockData>();
                    if (newBlockData != null)
                    {
                        newBlockData.gridPosition = newGridPos;
                        gridManager.OccupyCells(newGridPos, newBlockData.gridSize, duplicate, newBlockData.blockType);
                    }
                    
                    Undo.RegisterCreatedObjectUndo(duplicate, "Duplicate Block");
                    Selection.activeGameObject = duplicate;
                    return;
                }
            }

            EditorUtility.DisplayDialog("Cannot Duplicate", "No empty space found near the original block.", "OK");
        }

        private void DeleteBlock(BlockData blockData)
        {
            if (EditorUtility.DisplayDialog("Delete Block", "Delete this block from the grid?", "Yes", "Cancel"))
            {
                if (gridManager != null)
                {
                    gridManager.FreeCells(blockData.gameObject);
                }
                
                Undo.DestroyObjectImmediate(blockData.gameObject);
            }
        }

        private void ConvertToMesh(GameObject obj)
        {
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Save Mesh",
                    obj.name + ".asset",
                    "asset",
                    "Save mesh as asset"
                );

                if (!string.IsNullOrEmpty(path))
                {
                    Mesh meshCopy = Instantiate(meshFilter.sharedMesh);
                    AssetDatabase.CreateAsset(meshCopy, path);
                    AssetDatabase.SaveAssets();
                    
                    EditorUtility.DisplayDialog("Success", $"Mesh saved to {path}", "OK");
                }
            }
        }
    }
}
