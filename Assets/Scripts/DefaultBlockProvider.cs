using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LevelMaker
{
    /// <summary>
    /// Scans Resources/BlockPrefabs/ (including subfolders) and exposes them
    /// as block definitions. Subfolder names become categories.
    /// Auto-registered at startup via [RuntimeInitializeOnLoadMethod].
    /// </summary>
    public class DefaultBlockProvider : IBlockProvider
    {
        public string ProviderName => "Default";
        public int Priority => 0;

        private List<BlockDefinition> _cache;
        private string _resourcesRoot = "BlockPrefabs";
        private string _assetsPath = "Assets/Resources/BlockPrefabs";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoRegister()
        {
            if (BlockProviderRegistry.Providers.Count == 0)
            {
                BlockProviderRegistry.RegisterProvider(new DefaultBlockProvider());
            }
        }

        public IEnumerable<BlockDefinition> GetBlocks()
        {
            if (_cache != null) return _cache;
            _cache = new List<BlockDefinition>();
            ScanFolder(_assetsPath, "Root", _cache);
            return _cache;
        }

        public GameObject Instantiate(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            // Fast path: load by name across all BlockPrefabs subfolders
            var allPrefabs = Resources.LoadAll<GameObject>(_resourcesRoot);
            foreach (var p in allPrefabs)
            {
                if (p != null && p.name == id) return p;
            }
            return null;
        }

        private void ScanFolder(string folder, string category, List<BlockDefinition> output)
        {
            if (!Directory.Exists(folder)) return;

            // Prefabs directly in this folder
            foreach (var prefabFile in Directory.GetFiles(folder, "*.prefab"))
            {
                string fileName = Path.GetFileNameWithoutExtension(prefabFile);
                string resourcesPath = $"{_resourcesRoot}/{(category == "Root" ? "" : category + "/")}{fileName}";
                var prefab = Resources.Load<GameObject>(resourcesPath);
                output.Add(new BlockDefinition
                {
                    id = fileName,
                    displayName = fileName,
                    category = category,
                    fallbackPrimitive = GuessPrimitive(fileName),
                    prefab = prefab
                });
            }

            // Recurse into subfolders
            foreach (var subDir in Directory.GetDirectories(folder))
            {
                string subName = Path.GetFileName(subDir);
                ScanFolder(subDir, subName, output);
            }
        }

        private PrimitiveType GuessPrimitive(string name)
        {
            string lower = name.ToLower();
            if (lower.Contains("sphere") || lower.Contains("ball")) return PrimitiveType.Sphere;
            if (lower.Contains("cylinder") || lower.Contains("pillar") || lower.Contains("column")) return PrimitiveType.Cylinder;
            if (lower.Contains("capsule")) return PrimitiveType.Capsule;
            if (lower.Contains("plane") || lower.Contains("floor")) return PrimitiveType.Plane;
            return PrimitiveType.Cube;
        }
    }
}
