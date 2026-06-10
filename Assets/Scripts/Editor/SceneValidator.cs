#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using LevelMaker;

namespace LevelMaker.Editor
{
    /// <summary>
    /// Validates scene setup and adds missing components.
    /// Run from menu: Tools > Level Maker > Validate Scene
    /// </summary>
    public class SceneValidator
    {
        [MenuItem("Tools/Level Maker/Validate Scene")]
        public static void ValidateScene()
        {
            int issues = 0;

            // Check for LevelBuilder
            LevelBuilder levelBuilder = Object.FindObjectOfType<LevelBuilder>();
            if (levelBuilder == null)
            {
                Debug.LogError("[SceneValidator] ❌ MISSING: LevelBuilder component! Adding now...");
                GameObject lbObj = new GameObject("LevelBuilder");
                levelBuilder = lbObj.AddComponent<LevelBuilder>();
                issues++;
            }
            else
            {
                Debug.Log("[SceneValidator] ✓ LevelBuilder found");
            }

            // Check for GridManager
            GridManager gridManager = Object.FindObjectOfType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogWarning("[SceneValidator] ⚠ GridManager not found, LevelBuilder will create it at runtime");
            }
            else
            {
                Debug.Log("[SceneValidator] ✓ GridManager found");
            }

            // Check for Camera
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogError("[SceneValidator] ❌ MISSING: Main Camera!");
                issues++;
            }
            else
            {
                // Check if camera has LevelBuilderCamera
                if (cam.GetComponent<LevelBuilderCamera>() == null)
                {
                    Debug.LogWarning("[SceneValidator] ⚠ Camera missing LevelBuilderCamera component");
                }
                else
                {
                    Debug.Log("[SceneValidator] ✓ Camera has LevelBuilderCamera");
                }
            }

            // Check for EventSystem
            var eventSystem = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                Debug.LogError("[SceneValidator] ❌ MISSING: EventSystem! Adding now...");
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                issues++;
            }
            else
            {
                Debug.Log("[SceneValidator] ✓ EventSystem found");
            }

            // Check for Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[SceneValidator] ⚠ No Canvas found, will be created at runtime");
            }
            else
            {
                Debug.Log("[SceneValidator] ✓ Canvas found");
                // Check if LevelBuilderUI is on it
                if (canvas.GetComponent<LevelBuilderUI>() == null)
                {
                    Debug.LogWarning("[SceneValidator] ⚠ Canvas missing LevelBuilderUI");
                }
            }

            // Mark scene dirty if we made changes
            if (issues > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log($"[SceneValidator] Fixed {issues} issues. Save the scene!");
            }
            else
            {
                Debug.Log("[SceneValidator] ✓ Scene is valid!");
            }
        }

        [MenuItem("Tools/Level Maker/Quick Setup Scene")]
        public static void QuickSetupScene()
        {
            // Create or find a LevelBuilder GameObject
            LevelBuilder levelBuilder = Object.FindObjectOfType<LevelBuilder>();
            if (levelBuilder == null)
            {
                GameObject lbObj = new GameObject("LevelBuilder");
                lbObj.AddComponent<LevelBuilder>();
                Debug.Log("[SceneValidator] Created LevelBuilder");
            }

            // Make sure camera exists
            Camera cam = Camera.main;
            if (cam != null && cam.GetComponent<LevelBuilderCamera>() == null)
            {
                cam.gameObject.AddComponent<LevelBuilderCamera>();
                Debug.Log("[SceneValidator] Added LevelBuilderCamera to Main Camera");
            }

            // Make sure EventSystem exists
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Debug.Log("[SceneValidator] Created EventSystem");
            }

            // Make sure GridManager exists
            if (Object.FindObjectOfType<GridManager>() == null)
            {
                GameObject gmObj = new GameObject("GridManager");
                gmObj.AddComponent<GridManager>();
                Debug.Log("[SceneValidator] Created GridManager");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[SceneValidator] Quick setup complete! Save scene and Play.");
        }
    }
}
#endif
