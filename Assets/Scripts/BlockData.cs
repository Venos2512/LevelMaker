using UnityEngine;

namespace LevelMaker
{
    /// <summary>
    /// Legacy BlockData component - kept for backward compatibility with Editor scripts
    /// NEW SYSTEM: Metadata is now stored in GridManager.blockMetadata dictionary
    /// This component is NO LONGER ADDED to runtime blocks
    /// </summary>
    [System.Obsolete("BlockData is deprecated. Use GridManager.TryGetBlockMetadata() instead.")]
    public class BlockData : MonoBehaviour
    {
        [Header("Grid Data (Legacy - Not Used in Runtime)")]
        public Vector3Int gridPosition;
        public Vector3Int gridSize = Vector3Int.one;
        
        [Header("Block Info")]
        public string blockType = "Cube";
        public Vector3 originalSize = Vector3.one;
        
        [Header("Boolean Operation Data (Legacy)")]
        public bool isResultOfBoolean = false;
        public string booleanOperation = "";
        
        // Add warning when component is added
        private void Reset()
        {
            Debug.LogWarning($"[BlockData] This component is DEPRECATED. Runtime blocks use GridManager.blockMetadata instead. " +
                           $"Remove this component from {gameObject.name}.");
        }
    }
}
