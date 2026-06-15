using System.Collections.Generic;
using UnityEngine;

namespace LevelMaker
{
    /// <summary>
    /// A single block definition exposed to the level builder library.
    /// Providers return these for the library to render; the library
    /// then calls Instantiate() to get a usable GameObject.
    /// </summary>
    [System.Serializable]
    public class BlockDefinition
    {
        public string id;             // unique id within provider, e.g. "Ball_1x1x1"
        public string displayName;    // "Ball 1x1x1"
        public string category;       // "Basic", "Decoration", etc.
        public string[] tags;         // ["rounded", "soft"]
        public GameObject prefab;     // may be null - provider will instantiate on demand
        public PrimitiveType fallbackPrimitive = PrimitiveType.Cube;
        public Texture2D icon;        // optional thumbnail
    }

    /// <summary>
    /// Plugin interface for adding custom block sources to the level library.
    /// Implement this to load blocks from a remote API, a custom folder layout,
    /// or a procedural generator. Register the provider at startup:
    ///
    ///   BlockProviderRegistry.RegisterProvider(new MyCustomProvider());
    /// </summary>
    public interface IBlockProvider
    {
        /// <summary>
        /// Human-readable name shown in the library UI category tabs.
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Higher priority providers are queried first.
        /// Use this to let custom providers override defaults for matching ids.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Return all blocks this provider contributes. Called once at startup
        /// and whenever the library is refreshed.
        /// </summary>
        IEnumerable<BlockDefinition> GetBlocks();

        /// <summary>
        /// Instantiate a block by id. The returned GameObject will be cloned
        /// by the builder; providers don't need to clone themselves.
        /// </summary>
        GameObject Instantiate(string id);
    }

    /// <summary>
    /// Global registry of block providers. The library UI iterates this
    /// to populate itself. Default provider scans Resources/BlockPrefabs.
    /// </summary>
    public static class BlockProviderRegistry
    {
        private static readonly List<IBlockProvider> _providers = new List<IBlockProvider>();

        public static IReadOnlyList<IBlockProvider> Providers => _providers;

        public static void RegisterProvider(IBlockProvider provider)
        {
            if (provider == null) return;
            _providers.Add(provider);
            _providers.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            Debug.Log($"[BlockProviderRegistry] Registered '{provider.ProviderName}' (priority {provider.Priority}). Total: {_providers.Count}");
        }

        public static void UnregisterProvider(IBlockProvider provider)
        {
            if (provider == null) return;
            _providers.Remove(provider);
        }

        public static void Clear()
        {
            _providers.Clear();
        }

        /// <summary>
        /// Resolve a block id to a GameObject by trying providers in priority order.
        /// Returns null if no provider knows the id.
        /// </summary>
        public static GameObject InstantiateBlock(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var p in _providers)
            {
                var go = p.Instantiate(id);
                if (go != null) return go;
            }
            return null;
        }

        /// <summary>
        /// Look up a block definition by id. Useful for icons, tags, etc.
        /// </summary>
        public static BlockDefinition FindDefinition(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var p in _providers)
            {
                foreach (var def in p.GetBlocks())
                {
                    if (def.id == id) return def;
                }
            }
            return null;
        }
    }
}
