using UnityEngine;
using UnityEditor;

namespace LevelMaker.Editor
{
    /// <summary>
    /// Simplified CSG (Constructive Solid Geometry) operations
    /// For full CSG support, consider using ProBuilder or other mesh boolean libraries
    /// </summary>
    public static class CSGOperations
    {
        public static void PerformOperation(GameObject objA, GameObject objB, string operation)
        {
            if (objA == null || objB == null) return;

            MeshFilter meshFilterA = objA.GetComponent<MeshFilter>();
            MeshFilter meshFilterB = objB.GetComponent<MeshFilter>();

            if (meshFilterA == null || meshFilterB == null)
            {
                Debug.LogWarning("CSG Operations require MeshFilter components on both objects.");
                return;
            }

            switch (operation.ToLower())
            {
                case "union":
                    PerformUnion(objA, objB);
                    break;
                case "subtract":
                    PerformSubtract(objA, objB);
                    break;
                case "intersect":
                    PerformIntersect(objA, objB);
                    break;
            }
        }

        private static void PerformUnion(GameObject objA, GameObject objB)
        {
            // Simplified union: Create a parent group and keep both objects
            GameObject unionGroup = new GameObject($"Union_{objA.name}_{objB.name}");
            Undo.RegisterCreatedObjectUndo(unionGroup, "Boolean Union");

            Vector3 center = (objA.transform.position + objB.transform.position) / 2;
            unionGroup.transform.position = center;

            Undo.SetTransformParent(objA.transform, unionGroup.transform, "Boolean Union");
            Undo.SetTransformParent(objB.transform, unionGroup.transform, "Boolean Union");

            var blockData = unionGroup.AddComponent<BlockData>();
            blockData.isResultOfBoolean = true;
            blockData.booleanOperation = "Union";
            blockData.blockType = "BooleanUnion";

            Selection.activeGameObject = unionGroup;
            Debug.Log($"Union created: {unionGroup.name}");
        }

        private static void PerformSubtract(GameObject objA, GameObject objB)
        {
            // Simplified subtraction: Mark the subtracted object and change its appearance
            Renderer rendererB = objB.GetComponent<Renderer>();
            if (rendererB != null)
            {
                Material subMat = new Material(Shader.Find("Standard"));
                subMat.color = new Color(1f, 0.5f, 0.5f, 0.5f);
                subMat.SetFloat("_Mode", 3);
                subMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                subMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                subMat.SetInt("_ZWrite", 0);
                subMat.EnableKeyword("_ALPHABLEND_ON");
                subMat.renderQueue = 3000;
                
                Undo.RecordObject(rendererB, "Boolean Subtract");
                rendererB.sharedMaterial = subMat;
            }

            // Create a group to represent the subtraction
            GameObject subtractGroup = new GameObject($"Subtract_{objA.name}_minus_{objB.name}");
            Undo.RegisterCreatedObjectUndo(subtractGroup, "Boolean Subtract");

            Vector3 center = objA.transform.position;
            subtractGroup.transform.position = center;

            Undo.SetTransformParent(objA.transform, subtractGroup.transform, "Boolean Subtract");
            Undo.SetTransformParent(objB.transform, subtractGroup.transform, "Boolean Subtract");

            var blockData = subtractGroup.AddComponent<BlockData>();
            blockData.isResultOfBoolean = true;
            blockData.booleanOperation = "Subtract";
            blockData.blockType = "BooleanSubtract";

            objB.name = $"[SUBTRACTED]_{objB.name}";

            Selection.activeGameObject = subtractGroup;
            Debug.Log($"Subtraction created: {subtractGroup.name}");
        }

        private static void PerformIntersect(GameObject objA, GameObject objB)
        {
            // Simplified intersection: Calculate overlap bounds and create a visual representation
            Bounds boundsA = GetWorldBounds(objA);
            Bounds boundsB = GetWorldBounds(objB);

            if (!boundsA.Intersects(boundsB))
            {
                Debug.LogWarning("Objects do not intersect!");
                return;
            }

            // Calculate intersection bounds
            Vector3 min = Vector3.Max(boundsA.min, boundsB.min);
            Vector3 max = Vector3.Min(boundsA.max, boundsB.max);
            Vector3 intersectCenter = (min + max) / 2;
            Vector3 intersectSize = max - min;

            GameObject intersectObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            intersectObj.name = $"Intersect_{objA.name}_{objB.name}";
            intersectObj.transform.position = intersectCenter;
            intersectObj.transform.localScale = intersectSize;

            var blockData = intersectObj.AddComponent<BlockData>();
            blockData.isResultOfBoolean = true;
            blockData.booleanOperation = "Intersect";
            blockData.blockType = "BooleanIntersect";

            Renderer renderer = intersectObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material intMat = new Material(Shader.Find("Standard"));
                intMat.color = new Color(0.5f, 0.5f, 1f, 0.7f);
                renderer.sharedMaterial = intMat;
            }

            Undo.RegisterCreatedObjectUndo(intersectObj, "Boolean Intersect");
            
            // Hide the original objects
            Undo.RecordObject(objA, "Boolean Intersect");
            Undo.RecordObject(objB, "Boolean Intersect");
            objA.SetActive(false);
            objB.SetActive(false);

            Selection.activeGameObject = intersectObj;
            Debug.Log($"Intersection created: {intersectObj.name}");
        }

        private static Bounds GetWorldBounds(GameObject obj)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                return renderer.bounds;
            }

            // Fallback: calculate bounds from transform
            return new Bounds(obj.transform.position, obj.transform.localScale);
        }

        /// <summary>
        /// Helper method to combine meshes (basic implementation)
        /// </summary>
        public static Mesh CombineMeshes(GameObject[] objects)
        {
            CombineInstance[] combine = new CombineInstance[objects.Length];
            
            for (int i = 0; i < objects.Length; i++)
            {
                MeshFilter meshFilter = objects[i].GetComponent<MeshFilter>();
                if (meshFilter != null)
                {
                    combine[i].mesh = meshFilter.sharedMesh;
                    combine[i].transform = objects[i].transform.localToWorldMatrix;
                }
            }

            Mesh combinedMesh = new Mesh();
            combinedMesh.CombineMeshes(combine);
            return combinedMesh;
        }
    }
}
