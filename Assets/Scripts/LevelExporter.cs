using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace LevelMaker
{
    /// <summary>
    /// Export and import level data for saving/loading level layouts
    /// </summary>
    public class LevelExporter : MonoBehaviour
    {
        [System.Serializable]
        public class BlockInfo
        {
            public string blockType;
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public Vector3Int gridPosition;
            public Vector3Int gridSize;
        }

        [System.Serializable]
        public class LevelData
        {
            public string levelName;
            public string creationDate;
            public List<BlockInfo> blocks = new List<BlockInfo>();
        }

        public static LevelData ExportLevel(GridManager gridManager, string levelName = "Level")
        {
            LevelData data = new LevelData
            {
                levelName = levelName,
                creationDate = System.DateTime.Now.ToString()
            };

            var allBlocks = gridManager.GetAllBlocks();
            foreach (var kvp in allBlocks)
            {
                GameObject obj = kvp.Key;
                BlockMetadata metadata = kvp.Value;
                
                BlockInfo info = new BlockInfo
                {
                    blockType = metadata.blockType,
                    position = obj.transform.position,
                    rotation = obj.transform.rotation,
                    scale = obj.transform.localScale,
                    gridPosition = metadata.gridPosition,
                    gridSize = metadata.gridSize
                };
                data.blocks.Add(info);
            }

            return data;
        }

        public static void SaveLevelToFile(LevelData data, string filePath)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(filePath, json);
            Debug.Log($"Level saved to: {filePath}");
        }

        public static LevelData LoadLevelFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                return JsonUtility.FromJson<LevelData>(json);
            }
            return null;
        }

        public static GameObject ImportLevel(LevelData data, GridManager gridManager)
        {
            GameObject levelContainer = new GameObject(data.levelName);
            levelContainer.transform.SetParent(gridManager.transform);

            foreach (var blockInfo in data.blocks)
            {
                GameObject block = CreateBlockFromInfo(blockInfo, gridManager);
                if (block != null)
                {
                    block.transform.SetParent(gridManager.transform);
                }
            }

            Debug.Log($"Level imported: {data.levelName} with {data.blocks.Count} blocks");
            return levelContainer;
        }

        private static GameObject CreateBlockFromInfo(BlockInfo info, GridManager gridManager)
        {
            PrimitiveType primitiveType = PrimitiveType.Cube;
            
            switch (info.blockType.ToLower())
            {
                case "cube": primitiveType = PrimitiveType.Cube; break;
                case "sphere": primitiveType = PrimitiveType.Sphere; break;
                case "cylinder": primitiveType = PrimitiveType.Cylinder; break;
                case "capsule": primitiveType = PrimitiveType.Capsule; break;
                case "plane": primitiveType = PrimitiveType.Plane; break;
            }

            GameObject obj = GameObject.CreatePrimitive(primitiveType);
            obj.name = info.blockType;
            obj.transform.position = info.position;
            obj.transform.rotation = info.rotation;
            obj.transform.localScale = info.scale;

            // Register with GridManager (metadata stored in GridManager)
            gridManager.OccupyCells(info.gridPosition, info.gridSize, obj, info.blockType);

            return obj;
        }
    }
}
