#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

namespace LevelMaker.Editor
{
    /// <summary>
    /// Editor utility to generate sample block prefabs in the BlockPrefabs folders.
    /// Run from menu: Tools > Level Maker > Generate Sample Prefabs
    /// </summary>
    public class BlockLibraryGenerator
    {
        [MenuItem("Tools/Level Maker/Generate Sample Prefabs")]
        public static void GenerateSamplePrefabs()
        {
            string basePath = "Assets/Resources/BlockPrefabs";

            // Create folders if they don't exist
            CreateFolderIfMissing(basePath);
            CreateFolderIfMissing(basePath + "/Basic");
            CreateFolderIfMissing(basePath + "/Decorations");
            CreateFolderIfMissing(basePath + "/Structures");

            // Basic folder
            CreatePrimitivePrefab(PrimitiveType.Cube, basePath + "/Basic", "Block_1x1x1", new Color(0.7f, 0.7f, 0.7f));
            CreatePrimitivePrefab(PrimitiveType.Sphere, basePath + "/Basic", "Ball_1x1x1", new Color(0.4f, 0.7f, 1f));
            CreatePrimitivePrefab(PrimitiveType.Cylinder, basePath + "/Basic", "Pillar_1x2x1", new Color(1f, 0.8f, 0.3f));
            CreatePrimitivePrefab(PrimitiveType.Capsule, basePath + "/Basic", "Capsule_1x1x1", new Color(0.5f, 1f, 0.5f));

            // Decorations folder
            CreatePrimitivePrefab(PrimitiveType.Plane, basePath + "/Decorations", "Floor_1x1x1", new Color(0.9f, 0.9f, 0.9f));
            CreatePrimitivePrefab(PrimitiveType.Cube, basePath + "/Decorations", "Window_1x1x1", new Color(0.6f, 0.85f, 1f));
            CreatePrimitivePrefab(PrimitiveType.Sphere, basePath + "/Decorations", "Lamp_1x1x1", new Color(1f, 0.95f, 0.5f));

            // Structures folder
            CreatePrimitivePrefab(PrimitiveType.Cube, basePath + "/Structures", "Wall_1x2x1", new Color(0.5f, 0.3f, 0.2f));
            CreatePrimitivePrefab(PrimitiveType.Cube, basePath + "/Structures", "Stairs_1x1x1", new Color(0.7f, 0.5f, 0.3f));
            CreatePrimitivePrefab(PrimitiveType.Cylinder, basePath + "/Structures", "Column_1x3x1", new Color(0.6f, 0.6f, 0.6f));

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BlockLibraryGenerator] Sample prefabs generated!");
        }

        private static void CreateFolderIfMissing(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                string leaf = Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    CreateFolderIfMissing(parent);
                }
                AssetDatabase.CreateFolder(parent, leaf);
            }
        }

        private static void CreatePrimitivePrefab(PrimitiveType type, string folder, string name, Color color)
        {
            string fullPath = folder + "/" + name + ".prefab";

            // Skip if already exists
            if (File.Exists(fullPath)) return;

            // Create primitive
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.position = Vector3.zero;

            // Apply color
            var renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                renderer.sharedMaterial.color = color;
            }

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(obj, fullPath);
            Object.DestroyImmediate(obj);

            Debug.Log($"[BlockLibraryGenerator] Created: {fullPath}");
        }
    }
}
#endif
