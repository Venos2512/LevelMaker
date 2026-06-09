using UnityEngine;
using UnityEditor;
using System.IO;

namespace LevelMaker.Editor
{
    /// <summary>
    /// Editor window for exporting and importing levels
    /// </summary>
    public class LevelExporterWindow : EditorWindow
    {
        private Transform levelContainer;
        private string levelName = "MyLevel";
        private string exportPath = "Assets/Levels/";

        [MenuItem("Tools/Level Exporter")]
        public static void ShowWindow()
        {
            GetWindow<LevelExporterWindow>("Level Exporter");
        }

        private void OnGUI()
        {
            GUILayout.Label("Level Export/Import", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Export Section
            EditorGUILayout.LabelField("Export Level", EditorStyles.boldLabel);
            levelContainer = (Transform)EditorGUILayout.ObjectField("Level Container", levelContainer, typeof(Transform), true);
            levelName = EditorGUILayout.TextField("Level Name", levelName);
            exportPath = EditorGUILayout.TextField("Export Path", exportPath);

            if (GUILayout.Button("Export Level"))
            {
                ExportLevel();
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            // Import Section
            EditorGUILayout.LabelField("Import Level", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Import Level from File"))
            {
                ImportLevel();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Export your level layout to a JSON file for backup or reuse.\nImport to load a saved level layout.", MessageType.Info);
        }

        private void ExportLevel()
        {
            if (levelContainer == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a level container to export.", "OK");
                return;
            }

            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            string fileName = $"{levelName}_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
            string fullPath = Path.Combine(exportPath, fileName);

            GridManager gridManager = levelContainer.GetComponent<GridManager>();
            if (gridManager == null)
            {
                gridManager = FindObjectOfType<GridManager>();
            }
            
            if (gridManager == null)
            {
                EditorUtility.DisplayDialog("Error", "GridManager not found in scene.", "OK");
                return;
            }

            LevelExporter.LevelData data = LevelExporter.ExportLevel(gridManager, levelName);
            LevelExporter.SaveLevelToFile(data, fullPath);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success", $"Level exported to:\n{fullPath}", "OK");
        }

        private void ImportLevel()
        {
            string path = EditorUtility.OpenFilePanel("Import Level", exportPath, "json");
            
            if (!string.IsNullOrEmpty(path))
            {
                LevelExporter.LevelData data = LevelExporter.LoadLevelFromFile(path);
                
                if (data != null)
                {
                    GridManager gridManager = FindObjectOfType<GridManager>();
                    if (gridManager == null)
                    {
                        EditorUtility.DisplayDialog("Error", "GridManager not found in scene.", "OK");
                        return;
                    }
                    
                    GameObject imported = LevelExporter.ImportLevel(data, gridManager);
                    Selection.activeGameObject = imported;
                    Undo.RegisterCreatedObjectUndo(imported, "Import Level");
                    
                    EditorUtility.DisplayDialog("Success", $"Level '{data.levelName}' imported with {data.blocks.Count} blocks.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Failed to load level data.", "OK");
                }
            }
        }
    }
}
