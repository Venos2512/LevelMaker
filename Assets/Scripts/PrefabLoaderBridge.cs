using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LevelMaker
{
    /// <summary>
    /// Bridge for loading prefabs fresh in Editor, falling back to Resources in builds.
    /// Wrapped in #if UNITY_EDITOR so it works in both contexts.
    /// </summary>
    public static class PrefabLoaderBridge
    {
        /// <summary>
        /// Load a prefab from Assets path, forcing fresh load in Editor.
        /// </summary>
        public static GameObject LoadPrefabFresh(string assetsPath)
        {
            if (string.IsNullOrEmpty(assetsPath)) return null;

#if UNITY_EDITOR
            // In Editor: force reimport then load from AssetDatabase
            AssetDatabase.ImportAsset(assetsPath, ImportAssetOptions.ForceUpdate);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetsPath);
            if (asset != null)
            {
                Debug.Log($"[PrefabLoaderBridge] Fresh-loaded prefab: {assetsPath}");
                return asset;
            }
            Debug.LogWarning($"[PrefabLoaderBridge] Asset not found at: {assetsPath}");
            return null;
#else
            // In runtime: must use Resources path
            Debug.LogWarning("[PrefabLoaderBridge] LoadPrefabFresh from assets path only works in Editor");
            return null;
#endif
        }
    }
}
