using UnityEngine;
using UnityEditor;
using System.IO;

namespace LevelMaker
{
    /// <summary>
    /// UI Manager for Level Builder - handles saving to prefab
    /// </summary>
    public class LevelBuilderUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelBuilder levelBuilder;
        [SerializeField] private GridManager gridManager;
        
        [Header("Save Settings")]
        [SerializeField] private string prefabSavePath = "Assets/Prefabs/Levels/";
        [SerializeField] private string defaultLevelName = "NewLevel";
        
        private string currentLevelName;
        private bool showSaveDialog = false;

        private void Start()
        {
            if (levelBuilder == null)
                levelBuilder = FindObjectOfType<LevelBuilder>();
                
            if (gridManager == null)
                gridManager = FindObjectOfType<GridManager>();
                
            currentLevelName = defaultLevelName;
            
            // Ensure save directory exists
            if (!Directory.Exists(prefabSavePath))
            {
                Directory.CreateDirectory(prefabSavePath);
            }
        }

        private void Update()
        {
            // Press P to open save dialog
            if (Input.GetKeyDown(KeyCode.P))
            {
                showSaveDialog = !showSaveDialog;
            }
            
            // Quick save with Ctrl+S
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
            {
                SaveLevel(currentLevelName);
            }
        }

        private void DrawSaveDialog()
        {
            int dialogWidth = 400;
            int dialogHeight = 250;
            int dialogX = Screen.width / 2 - dialogWidth / 2;
            int dialogY = Screen.height / 2 - dialogHeight / 2;
            
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.95f));
            
            GUI.Box(new Rect(dialogX, dialogY, dialogWidth, dialogHeight), "", boxStyle);
            
            GUILayout.BeginArea(new Rect(dialogX + 20, dialogY + 20, dialogWidth - 40, dialogHeight - 40));
            
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 20;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = Color.white;
            GUILayout.Label("Save Level as Prefab", titleStyle);
            
            GUILayout.Space(20);
            
            GUILayout.Label("Level Name:", GUI.skin.label);
            currentLevelName = GUILayout.TextField(currentLevelName, 50);
            
            GUILayout.Space(10);
            
            GUILayout.Label($"Save Path: {prefabSavePath}", GUI.skin.label);
            
            GUILayout.Space(20);
            
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Save", GUILayout.Height(40)))
            {
                SaveLevel(currentLevelName);
                showSaveDialog = false;
            }
            
            if (GUILayout.Button("Cancel", GUILayout.Height(40)))
            {
                showSaveDialog = false;
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.Space(20);
            
            GUIStyle helpStyle = new GUIStyle(GUI.skin.label);
            helpStyle.fontSize = 10;
            helpStyle.normal.textColor = Color.gray;
            GUILayout.Label("This will save all blocks under GridManager as a prefab.", helpStyle);
            
            GUILayout.EndArea();
        }

        public void SaveLevel(string levelName)
        {
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found! Cannot save level.");
                return;
            }
            
            if (string.IsNullOrEmpty(levelName))
            {
                levelName = defaultLevelName;
            }
            
            // Sanitize filename
            levelName = SanitizeFileName(levelName);
            
#if UNITY_EDITOR
            // Ensure directory exists
            if (!Directory.Exists(prefabSavePath))
            {
                Directory.CreateDirectory(prefabSavePath);
            }
            
            string prefabPath = Path.Combine(prefabSavePath, levelName + ".prefab");
            
            // Create a temporary parent to hold all blocks
            GameObject levelContainer = new GameObject(levelName);
            
            // Move all blocks to the container
            Transform gridTransform = gridManager.transform;
            int childCount = gridTransform.childCount;
            Transform[] children = new Transform[childCount];
            
            for (int i = 0; i < childCount; i++)
            {
                children[i] = gridTransform.GetChild(i);
            }
            
            foreach (Transform child in children)
            {
                // Only move objects that are placed blocks (check GridManager metadata)
                if (gridManager.IsPlacedBlock(child.gameObject))
                {
                    child.SetParent(levelContainer.transform);
                }
            }
            
            // Save as prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(levelContainer, prefabPath);
            
            if (prefab != null)
            {
                Debug.Log($"Level saved successfully to: {prefabPath}");
                
                // Move blocks back to GridManager
                childCount = levelContainer.transform.childCount;
                children = new Transform[childCount];
                
                for (int i = 0; i < childCount; i++)
                {
                    children[i] = levelContainer.transform.GetChild(i);
                }
                
                foreach (Transform child in children)
                {
                    child.SetParent(gridTransform);
                }
                
                // Destroy temporary container
                DestroyImmediate(levelContainer);
                
                // Show success message
                ShowSuccessMessage($"Level '{levelName}' saved!");
            }
            else
            {
                Debug.LogError("Failed to save prefab!");
                DestroyImmediate(levelContainer);
            }
#else
            Debug.LogWarning("Saving prefabs is only available in the Editor!");
#endif
        }

        private string SanitizeFileName(string fileName)
        {
            char[] invalids = Path.GetInvalidFileNameChars();
            foreach (char c in invalids)
            {
                fileName = fileName.Replace(c, '_');
            }
            return fileName;
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

        private float successMessageTime = 0f;
        private string successMessageText = "";

        private void ShowSuccessMessage(string message)
        {
            successMessageText = message;
            successMessageTime = Time.time + 3f;
        }

        private void OnGUI()
        {
            // Save dialog
            if (showSaveDialog)
            {
                DrawSaveDialog();
            }
            else
            {
                // Quick instructions in corner
                GUIStyle style = new GUIStyle(GUI.skin.box);
                style.fontSize = 12;
                style.alignment = TextAnchor.UpperRight;
                style.normal.textColor = Color.white;
                
                string quickHelp = 
                    "P: Save Menu\n" +
                    "Ctrl+S: Quick Save";
                
                GUI.Box(new Rect(Screen.width - 160, 10, 150, 60), quickHelp, style);
            }
            
            // Success message
            if (Time.time < successMessageTime)
            {
                GUIStyle successStyle = new GUIStyle(GUI.skin.box);
                successStyle.fontSize = 16;
                successStyle.alignment = TextAnchor.MiddleCenter;
                successStyle.normal.textColor = Color.green;
                successStyle.fontStyle = FontStyle.Bold;
                
                GUI.Box(new Rect(Screen.width / 2 - 150, 80, 300, 50), successMessageText, successStyle);
            }
        }
    }
}
