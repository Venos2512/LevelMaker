#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace LevelMaker.Editor
{
    /// <summary>
    /// Editor-only utility for loading prefabs fresh (bypassing Resources cache).
    /// Used by runtime scripts via reflection to avoid hard dependency on UnityEditor.
    /// </summary>
    public static class PrefabLoader
    {
        /// <summary>
        /// Load a prefab from Assets path, bypassing Resources cache.
        /// Returns null if not found.
        /// </summary>
        public static GameObject LoadPrefabFromAssetsPath(string assetsPath)
        {
            if (string.IsNullOrEmpty(assetsPath)) return null;

            // Force AssetDatabase to reimport (in case the file changed on disk)
            AssetDatabase.ImportAsset(assetsPath, ImportAssetOptions.ForceUpdate);

            return AssetDatabase.LoadAssetAtPath<GameObject>(assetsPath);
        }
    }
}
#endif
