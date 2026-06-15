#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LevelMaker.Editor
{
    /// <summary>
    /// Batch-convert Unity mesh assets (.asset / .fbx / .obj) into prefabs.
    /// Scans a source folder, lets the user multi-select entries, and writes
    /// one prefab per mesh to an output folder. Prefabs are written under
    /// Assets/Resources/... so the runtime block library picks them up.
    ///
    /// Run from menu: Tools > Level Maker > Mesh -> Prefab Batch Converter
    /// </summary>
    public class MeshToPrefabBatchConverter : EditorWindow
    {
        // ---- settings (persisted via EditorPrefs) ----
        private const string PrefSourceFolder = "MeshToPrefab.SourceFolder";
        private const string PrefOutputFolder = "MeshToPrefab.OutputFolder";
        private const string PrefIncludeSubs  = "MeshToPrefab.IncludeSubfolders";
        private const string PrefAddCollider  = "MeshToPrefab.AddCollider";
        private const string PrefCenterPivot  = "MeshToPrefab.CenterPivot";
        private const string PrefOverwrite    = "MeshToPrefab.Overwrite";
        private const string PrefMirrorMats   = "MeshToPrefab.MirrorMaterials";
        private const string PrefSizeSuffix   = "MeshToPrefab.SizeSuffix";
        private const string PrefLastSearch   = "MeshToPrefab.LastSearch";

        private string _sourceFolder = "Assets/Ref/_Models";
        private string _outputFolder = "Assets/Resources/BlockPrefabs";
        private bool   _includeSubfolders = true;
        private bool   _addCollider = true;
        private bool   _centerPivot = true;
        private bool   _overwrite = true;
        private bool   _mirrorMaterials = true;
        private string _sizeSuffix = "_1x1x1";
        private string _search = "";

        // ---- UI state ----
        private Vector2 _scroll;
        private List<MeshEntry> _entries = new List<MeshEntry>();
        private HashSet<string> _selected = new HashSet<string>();

        private static readonly string[] MeshExtensions = { ".asset", ".fbx", ".obj", ".blend", ".mesh", ".dae" };

        [MenuItem("Tools/Level Maker/Mesh -> Prefab Batch Converter")]
        public static void Open()
        {
            var win = GetWindow<MeshToPrefabBatchConverter>("Mesh -> Prefab");
            win.minSize = new Vector2(560, 420);
            win.Show();
        }

        private void OnEnable()
        {
            _sourceFolder       = EditorPrefs.GetString(PrefSourceFolder, _sourceFolder);
            _outputFolder       = EditorPrefs.GetString(PrefOutputFolder, _outputFolder);
            _includeSubfolders  = EditorPrefs.GetBool(PrefIncludeSubs, _includeSubfolders);
            _addCollider        = EditorPrefs.GetBool(PrefAddCollider, _addCollider);
            _centerPivot        = EditorPrefs.GetBool(PrefCenterPivot, _centerPivot);
            _overwrite          = EditorPrefs.GetBool(PrefOverwrite, _overwrite);
            _mirrorMaterials    = EditorPrefs.GetBool(PrefMirrorMats, _mirrorMaterials);
            _sizeSuffix         = EditorPrefs.GetString(PrefSizeSuffix, _sizeSuffix);
            _search             = EditorPrefs.GetString(PrefLastSearch, _search);

            Rescan();
        }

        private void OnDisable()
        {
            EditorPrefs.SetString(PrefSourceFolder, _sourceFolder);
            EditorPrefs.SetString(PrefOutputFolder, _outputFolder);
            EditorPrefs.SetBool(PrefIncludeSubs, _includeSubfolders);
            EditorPrefs.SetBool(PrefAddCollider, _addCollider);
            EditorPrefs.SetBool(PrefCenterPivot, _centerPivot);
            EditorPrefs.SetBool(PrefOverwrite, _overwrite);
            EditorPrefs.SetBool(PrefMirrorMats, _mirrorMaterials);
            EditorPrefs.SetString(PrefSizeSuffix, _sizeSuffix);
            EditorPrefs.SetString(PrefLastSearch, _search);
        }

        // ============== UI ==============

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(2);
            DrawOptions();
            EditorGUILayout.Space(2);
            DrawList();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Rescan", EditorStyles.toolbarButton, GUILayout.Width(70)))
                {
                    Rescan();
                }
                GUILayout.FlexibleSpace();
                GUILayout.Label($"{_entries.Count(e => e.Visible)} visible | {_selected.Count} selected", EditorStyles.miniLabel);
            }
        }

        private void DrawOptions()
        {
            // Source / output folders
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Source", GUILayout.Width(50));
                string newSrc = EditorGUILayout.TextField(_sourceFolder);
                if (newSrc != _sourceFolder) { _sourceFolder = newSrc; }
                if (GUILayout.Button("...", GUILayout.Width(26)))
                {
                    string picked = EditorUtility.OpenFolderPanel("Pick source folder", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(picked))
                    {
                        _sourceFolder = "Assets" + picked.Substring(Application.dataPath.Length);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Output", GUILayout.Width(50));
                _outputFolder = EditorGUILayout.TextField(_outputFolder);
                if (GUILayout.Button("...", GUILayout.Width(26)))
                {
                    string picked = EditorUtility.OpenFolderPanel("Pick output folder (under Assets/)", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(picked))
                    {
                        if (picked.StartsWith(Application.dataPath))
                        {
                            _outputFolder = "Assets" + picked.Substring(Application.dataPath.Length);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Output folder",
                                "Output must be inside the project's Assets/ folder.\n\n" + picked, "OK");
                        }
                    }
                }
            }

            // Toggles
            using (new EditorGUILayout.HorizontalScope())
            {
                _includeSubfolders = GUILayout.Toggle(_includeSubfolders, "Include subfolders", GUILayout.Width(140));
                _addCollider       = GUILayout.Toggle(_addCollider, "Add MeshCollider", GUILayout.Width(140));
                _centerPivot       = GUILayout.Toggle(_centerPivot, "Center & reset pivot", GUILayout.Width(160));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                _overwrite         = GUILayout.Toggle(_overwrite, "Overwrite existing", GUILayout.Width(140));
                _mirrorMaterials   = GUILayout.Toggle(_mirrorMaterials, "Match materials by name", GUILayout.Width(200));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Name suffix", GUILayout.Width(80));
                _sizeSuffix = EditorGUILayout.TextField(_sizeSuffix, GUILayout.Width(100));
                GUILayout.Label("(empty = no suffix; e.g. \"_1x1x1\" to match library pattern)", EditorStyles.miniLabel);
            }

            // Search / filter
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Filter", GUILayout.Width(50));
                string newSearch = EditorGUILayout.TextField(_search);
                if (newSearch != _search) { _search = newSearch; ApplyFilter(); }
                if (GUILayout.Button("Clear", GUILayout.Width(50))) { _search = ""; ApplyFilter(); }
            }

            // Selection helpers + convert
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select All", GUILayout.Width(90)))
                {
                    foreach (var e in _entries) if (e.Visible) _selected.Add(e.AssetPath);
                }
                if (GUILayout.Button("Select None", GUILayout.Width(90)))
                {
                    _selected.Clear();
                }
                if (GUILayout.Button("Invert", GUILayout.Width(70)))
                {
                    var newSel = new HashSet<string>();
                    foreach (var e in _entries) if (e.Visible && !_selected.Contains(e.AssetPath)) newSel.Add(e.AssetPath);
                    _selected = newSel;
                }
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(_selected.Count == 0))
                {
                    if (GUILayout.Button($"Convert Selected ({_selected.Count})", GUILayout.Height(24)))
                    {
                        ConvertSelected();
                    }
                }
                if (GUILayout.Button("Convert All Visible", GUILayout.Height(24)))
                {
                    ConvertAllVisible();
                }
            }
        }

        private void DrawList()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            int shown = 0;
            for (int i = 0; i < _entries.Count; i++)
            {
                var e = _entries[i];
                if (!e.Visible) continue;
                shown++;

                using (new EditorGUILayout.HorizontalScope(i % 2 == 0 ? "CN Box" : "Box"))
                {
                    bool sel = _selected.Contains(e.AssetPath);
                    bool now = EditorGUILayout.Toggle(sel, GUILayout.Width(18));
                    if (now != sel)
                    {
                        if (now) _selected.Add(e.AssetPath);
                        else _selected.Remove(e.AssetPath);
                    }

                    EditorGUILayout.LabelField(e.DisplayName, GUILayout.MinWidth(180));
                    EditorGUILayout.LabelField(e.SubFolder, EditorStyles.miniLabel, GUILayout.Width(160));
                    EditorGUILayout.LabelField(e.TypeLabel, EditorStyles.miniLabel, GUILayout.Width(50));

                    if (GUILayout.Button("Convert", GUILayout.Width(70)))
                    {
                        _selected.Clear();
                        _selected.Add(e.AssetPath);
                        ConvertSelected();
                    }
                }
            }
            if (shown == 0)
            {
                EditorGUILayout.HelpBox("No meshes found. Pick a source folder and click Rescan.", MessageType.Info);
            }
            EditorGUILayout.EndScrollView();
        }

        // ============== scanning ==============

        private void Rescan()
        {
            _entries.Clear();
            _selected.Clear();
            if (string.IsNullOrEmpty(_sourceFolder) || !AssetDatabase.IsValidFolder(_sourceFolder))
            {
                Repaint();
                return;
            }

            string[] guids;
            if (_includeSubfolders)
            {
                guids = AssetDatabase.FindAssets("t:Mesh", new[] { _sourceFolder });
            }
            else
            {
                guids = AssetDatabase.FindAssets("t:Mesh", new[] { _sourceFolder });
                // filter out anything not directly in source folder
                guids = guids.Where(g =>
                {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    string dir = Path.GetDirectoryName(p)?.Replace('\\', '/');
                    return dir == _sourceFolder.Replace('\\', '/');
                }).ToArray();
            }

            // Also include FBX / OBJ / model files - FindAssets t:Mesh picks up .asset
            // and mesh nodes in models, but .fbx/obj also need scanning.
            var modelGuids = AssetDatabase.FindAssets("t:Model", new[] { _sourceFolder });
            var allGuids = new HashSet<string>(guids);
            foreach (var g in modelGuids) allGuids.Add(g);

            foreach (var g in allGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (string.IsNullOrEmpty(path)) continue;
                string ext = Path.GetExtension(path).ToLower();
                if (!MeshExtensions.Contains(ext) && ext != ".asset") continue;

                string sub = "";
                if (path.StartsWith(_sourceFolder + "/"))
                {
                    string rest = path.Substring(_sourceFolder.Length + 1);
                    int slash = rest.IndexOf('/');
                    if (slash > 0) sub = rest.Substring(0, slash);
                    else sub = "(root)";
                }
                else
                {
                    sub = "(other)";
                }

                _entries.Add(new MeshEntry
                {
                    AssetPath = path,
                    FileName = Path.GetFileNameWithoutExtension(path),
                    SubFolder = sub,
                    TypeLabel = ext.TrimStart('.'),
                    Visible = true
                });
            }

            _entries.Sort((a, b) => string.Compare(a.SubFolder + a.FileName, b.SubFolder + b.FileName, System.StringComparison.OrdinalIgnoreCase));
            ApplyFilter();
            Repaint();
        }

        private void ApplyFilter()
        {
            string s = (_search ?? "").Trim();
            for (int i = 0; i < _entries.Count; i++)
            {
                bool vis = s.Length == 0
                    || _entries[i].FileName.IndexOf(s, System.StringComparison.OrdinalIgnoreCase) >= 0
                    || _entries[i].SubFolder.IndexOf(s, System.StringComparison.OrdinalIgnoreCase) >= 0
                    || _entries[i].AssetPath.IndexOf(s, System.StringComparison.OrdinalIgnoreCase) >= 0;
                _entries[i].Visible = vis;
            }
        }

        // ============== conversion ==============

        private void ConvertAllVisible()
        {
            var list = _entries.Where(e => e.Visible).Select(e => e.AssetPath).ToList();
            Convert(list, "All Visible");
        }

        private void ConvertSelected()
        {
            Convert(_selected.ToList(), "Selected");
        }

        private void Convert(List<string> assetPaths, string label)
        {
            if (assetPaths == null || assetPaths.Count == 0) return;

            if (string.IsNullOrEmpty(_outputFolder) || !_outputFolder.StartsWith("Assets/"))
            {
                EditorUtility.DisplayDialog("Convert",
                    "Output folder must be inside the project's Assets/ folder (so prefabs can be loaded at runtime).", "OK");
                return;
            }

            EnsureFolder(_outputFolder);

            int ok = 0, skipped = 0, failed = 0;
            var failedEntries = new List<string>();

            try
            {
                for (int i = 0; i < assetPaths.Count; i++)
                {
                    string assetPath = assetPaths[i];
                    if (string.IsNullOrEmpty(assetPath)) continue;

                    string niceName = Path.GetFileNameWithoutExtension(assetPath);
                    string progress = $"{label} {i + 1}/{assetPaths.Count}: {niceName}";
                    if (EditorUtility.DisplayCancelableProgressBar("Converting meshes to prefabs", progress, (float)i / assetPaths.Count))
                    {
                        Debug.LogWarning($"[MeshToPrefab] Cancelled at {i}/{assetPaths.Count}");
                        break;
                    }

                    if (TryConvertOne(assetPath, out string reason))
                    {
                        ok++;
                    }
                    else
                    {
                        failed++;
                        failedEntries.Add($"{niceName}: {reason}");
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = $"Converted {label}: {ok} OK, {failed} failed.";
            if (failedEntries.Count > 0)
            {
                msg += "\n\nFailures:\n" + string.Join("\n", failedEntries.Take(10));
                if (failedEntries.Count > 10) msg += $"\n... and {failedEntries.Count - 10} more";
            }
            Debug.Log("[MeshToPrefab] " + msg.Replace("\n", " | "));
            EditorUtility.DisplayDialog("Mesh -> Prefab", msg, "OK");
        }

        /// <summary>
        /// Convert a single asset path. Returns true on success.
        /// Out-params: false with a reason string in `reason`.
        /// </summary>
        private bool TryConvertOne(string assetPath, out string reason)
        {
            reason = null;
            string ext = Path.GetExtension(assetPath).ToLower();

            // Output sub-folder under outputFolder: mirror the source sub-folder if possible
            string srcDir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            string outDir = _outputFolder;
            if (!string.IsNullOrEmpty(srcDir) && srcDir.StartsWith(_sourceFolder))
            {
                string rest = srcDir.Substring(_sourceFolder.Length).TrimStart('/');
                if (!string.IsNullOrEmpty(rest))
                {
                    outDir = _outputFolder + "/" + rest;
                    EnsureFolder(outDir);
                }
            }

            string baseName = Path.GetFileNameWithoutExtension(assetPath);
            string suffix = string.IsNullOrEmpty(_sizeSuffix) ? "" : _sizeSuffix;
            string prefabName = baseName + suffix + ".prefab";
            string prefabPath = (outDir + "/" + prefabName).Replace('\\', '/');

            if (File.Exists(prefabPath) && !_overwrite)
            {
                reason = "exists and overwrite is off";
                return false;
            }

            GameObject root = null;
            try
            {
                if (ext == ".asset")
                {
                    // Plain Unity Mesh asset
                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                    if (mesh == null) { reason = "could not load Mesh asset"; return false; }

                    root = new GameObject(baseName);
                    var mf = root.AddComponent<MeshFilter>();
                    mf.sharedMesh = mesh;
                    var mr = root.AddComponent<MeshRenderer>();

                    if (_mirrorMaterials) TryAttachMatchingMaterial(mr, baseName);
                    else mr.sharedMaterials = new Material[0];
                }
                else
                {
                    // Imported model (.fbx / .obj / .blend / .dae)
                    var modelRoot = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (modelRoot == null) { reason = "could not load model prefab"; return false; }

                    root = (GameObject)PrefabUtility.InstantiatePrefab(modelRoot);
                    root.name = baseName;

                    // Drop into world origin - the prefab's saved transform is what counts at runtime.
                    root.transform.localPosition = Vector3.zero;
                    root.transform.localRotation = Quaternion.identity;
                    root.transform.localScale = Vector3.one;
                }

                if (_centerPivot)
                {
                    // Snap to origin: position stays at the prefab's pivot, but we ensure rotation is identity
                    root.transform.position = Vector3.zero;
                    root.transform.rotation = Quaternion.identity;
                }

                if (_addCollider && root.GetComponentInChildren<MeshFilter>() != null && root.GetComponent<Collider>() == null)
                {
                    // Convex MeshCollider so the grid raycaster in LevelBuilder can hit it
                    var firstFilter = root.GetComponentInChildren<MeshFilter>();
                    if (firstFilter != null)
                    {
                        var col = root.AddComponent<MeshCollider>();
                        col.sharedMesh = firstFilter.sharedMesh;
                        col.convex = true;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                return true;
            }
            catch (System.Exception e)
            {
                reason = e.Message;
                return false;
            }
            finally
            {
                if (root != null) DestroyImmediate(root);
            }
        }

        // Best-effort material lookup. The Ref/_Materials/ tree has subfolders
        // (Car, Level_0, etc.) with materials whose name matches the mesh name.
        // We scan only the matching sub-folder to keep it fast.
        private void TryAttachMatchingMaterial(MeshRenderer mr, string meshName)
        {
            if (mr == null || string.IsNullOrEmpty(meshName)) return;

            // Look under each Materials sub-folder
            string materialsRoot = "Assets/Ref/_Materials";
            if (!AssetDatabase.IsValidFolder(materialsRoot)) return;

            foreach (var subGuid in AssetDatabase.FindAssets("", new[] { materialsRoot }))
            {
                string subPath = AssetDatabase.GUIDToAssetPath(subGuid);
                if (!AssetDatabase.IsValidFolder(subPath)) continue;

                string matPath = subPath + "/" + meshName + ".mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat != null)
                {
                    mr.sharedMaterials = new[] { mat };
                    return;
                }
            }
            // No matching material - leave default (pink) so the artist notices.
        }

        // Make sure every folder in the path exists
        private static void EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/');
            if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder)) return;

            string[] parts = folder.Split('/');
            string cur = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(cur, parts[i]);
                }
                cur = next;
            }
        }

        // ============== data ==============

        private class MeshEntry
        {
            public string AssetPath;
            public string FileName;
            public string SubFolder;
            public string TypeLabel;
            public string DisplayName => FileName;
            public bool   Visible = true;
        }
    }
}
#endif
