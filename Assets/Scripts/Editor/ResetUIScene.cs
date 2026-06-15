#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

namespace LevelMaker.Editor
{
    /// <summary>
    /// Wipe the existing Canvas UI built by older versions of LevelBuilderUICreator
    /// and rebuild from scratch using the new layout (Phase 1 + 2 redesign).
    ///
    /// Idempotent: safe to run multiple times. Old LevelBuilderUI / TopButtonsPanel /
    /// LibraryPanel / LevelListPanel / DebugPanel are destroyed, then a fresh set is
    /// created with all the new fields wired.
    ///
    /// Run from menu: Tools > Level Maker > Reset Level Builder UI
    /// </summary>
    public static class ResetUIScene
    {
        private const string CanvasName = "LevelBuilderCanvas";
        private const string EventSystemName = "EventSystem";
        private const string UIName = "LevelBuilderUI";

        [MenuItem("Tools/Level Maker/Reset Level Builder UI")]
        public static void Run()
        {
            // Find or create the Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject(CanvasName);
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // EventSystem
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject(EventSystemName);
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Wipe old UI children of canvas
            WipeOldUI(canvas.transform);

            // Wipe old LevelBuilderUI component from any leftover GameObject
            foreach (var old in Object.FindObjectsOfType<LevelBuilderUI>())
            {
                Object.DestroyImmediate(old.gameObject);
            }

            // Build fresh
            GameObject uiObj = new GameObject(UIName);
            uiObj.transform.SetParent(canvas.transform, false);
            LevelBuilderUI ui = uiObj.AddComponent<LevelBuilderUI>();
            ToastUI toast = uiObj.AddComponent<ToastUI>();
            ConfirmDialog confirm = uiObj.AddComponent<ConfirmDialog>();

            LevelBuilder levelBuilder = Object.FindObjectOfType<LevelBuilder>();
            BlockLibrary blockLibrary = Object.FindObjectOfType<BlockLibrary>();

            // Build sections
            BuildTopBar(canvas.transform, ui, levelBuilder);
            BuildDebugPanel(canvas.transform, ui, levelBuilder);
            BuildLibraryPanel(canvas.transform, ui, levelBuilder);
            BuildSavedLevelsPanel(canvas.transform, ui);
            BuildHelpPanel(canvas.transform, ui);

            // Wire non-UI references
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("levelBuilder").objectReferenceValue = levelBuilder;
            so.FindProperty("blockLibrary").objectReferenceValue = blockLibrary;
            so.FindProperty("toastUI").objectReferenceValue = toast;
            so.FindProperty("confirmDialog").objectReferenceValue = confirm;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[ResetUIScene] Canvas UI rebuilt with new layout (Phase 1 + 2).");
        }

        private static void WipeOldUI(Transform canvas)
        {
            string[] toWipe = {
                "TopButtonsPanel", "DebugPanel", "LibraryPanel", "LevelListPanel",
                "HelpPanel", "ToastContainer", "ConfirmDialogPanel",
                "LibraryScrollView", "LevelScrollView"
            };
            var hash = new HashSet<string>(toWipe);
            for (int i = canvas.childCount - 1; i >= 0; i--)
            {
                var child = canvas.GetChild(i);
                if (hash.Contains(child.name)) Object.DestroyImmediate(child.gameObject);
            }
        }

        // ============== section builders ==============

        private static void BuildTopBar(Transform canvas, LevelBuilderUI ui, LevelBuilder lb)
        {
            GameObject panel = CreatePanel("TopButtonsPanel", canvas);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -10);
            rt.sizeDelta = new Vector2(960, 48);

            HorizontalLayoutGroup hlg = panel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            // Mode group
            Button buildBtn  = CreateButton("BuildButton",  "🔨 BUILD",  panel.transform, new Color(0.30f, 0.70f, 0.35f));
            Button eraseBtn  = CreateButton("EraseButton",  "🖌 ERASE",  panel.transform, new Color(0.70f, 0.30f, 0.30f));
            Button selectBtn = CreateButton("SelectButton", "👆 SELECT", panel.transform, new Color(0.70f, 0.70f, 0.30f));

            // Separator
            CreateSeparator(panel.transform);

            // File group
            Button saveBtn = CreateButton("SaveButton", "💾 SAVE",  panel.transform, new Color(0.30f, 0.60f, 0.80f));
            Button loadBtn = CreateButton("LoadButton", "📂 LOAD",  panel.transform, new Color(0.50f, 0.40f, 0.80f));

            // Separator
            CreateSeparator(panel.transform);

            // Tools group
            Button libBtn  = CreateButton("LibraryToggleButton", "📦 LIB",    panel.transform, new Color(0.40f, 0.50f, 0.65f));
            Button helpBtn = CreateButton("HelpButton",           "❓ HELP",  panel.transform, new Color(0.55f, 0.55f, 0.55f));
            Button setBtn  = CreateButton("SettingsButton",       "⚙",       panel.transform, new Color(0.45f, 0.45f, 0.45f));

            var so = new SerializedObject(ui);
            so.FindProperty("buildButton").objectReferenceValue = buildBtn;
            so.FindProperty("eraseButton").objectReferenceValue = eraseBtn;
            so.FindProperty("selectButton").objectReferenceValue = selectBtn;
            so.FindProperty("saveButton").objectReferenceValue = saveBtn;
            so.FindProperty("loadButton").objectReferenceValue = loadBtn;
            so.FindProperty("libraryToggleButton").objectReferenceValue = libBtn;
            so.FindProperty("helpButton").objectReferenceValue = helpBtn;
            so.FindProperty("settingsButton").objectReferenceValue = setBtn;
            so.ApplyModifiedProperties();
        }

        private static void BuildDebugPanel(Transform canvas, LevelBuilderUI ui, LevelBuilder lb)
        {
            GameObject panel = CreatePanel("DebugPanel", canvas);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(10, 10);
            rt.sizeDelta = new Vector2(420, 32);

            HorizontalLayoutGroup hlg = panel.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 0, 0);
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            // Compact one-line text
            Text compact = CreateText("DebugCompact", "Mode: Build | Blocks: 0 | Sel: 0", panel.transform, 12, Color.white, FontStyle.Bold);
            compact.alignment = TextAnchor.MiddleLeft;
            LayoutElement cLE = compact.gameObject.AddComponent<LayoutElement>();
            cLE.flexibleWidth = 1;

            // Expand button
            Button expandBtn = CreateButton("DebugExpand", "▼", panel.transform, new Color(0.30f, 0.30f, 0.30f));
            LayoutElement eLE = expandBtn.gameObject.AddComponent<LayoutElement>();
            eLE.preferredWidth = 28;

            // Expanded group (hidden by default)
            GameObject expanded = new GameObject("DebugExpandedGroup");
            expanded.transform.SetParent(panel.transform, false);
            // We'll attach it OUTSIDE the HLG via a vertical layout. Simpler: put it as a sibling below.
            RectTransform exRT = expanded.AddComponent<RectTransform>();
            exRT.anchorMin = new Vector2(0, 0);
            exRT.anchorMax = new Vector2(0, 0);
            exRT.pivot = new Vector2(0, 0);
            exRT.anchoredPosition = new Vector2(0, 36);
            exRT.sizeDelta = new Vector2(420, 110);
            VerticalLayoutGroup exVLG = expanded.AddComponent<VerticalLayoutGroup>();
            exVLG.padding = new RectOffset(8, 8, 6, 6);
            exVLG.spacing = 2;
            exVLG.childForceExpandWidth = true;
            exVLG.childForceExpandHeight = false;
            exVLG.childControlWidth = true;
            exVLG.childControlHeight = true;
            Image exBg = expanded.AddComponent<Image>();
            exBg.color = new Color(0, 0, 0, 0.5f);

            Text modeText = CreateText("ModeText", "Mode: Build", expanded.transform, 12, Color.white, FontStyle.Bold);
            Text undoText = CreateText("UndoText", "Undo: 0", expanded.transform, 11, Color.yellow, FontStyle.Normal);
            Text lvlNameText = CreateText("LevelNameText", "No level loaded", expanded.transform, 11, new Color(0.6f, 0.9f, 1f), FontStyle.Normal);
            lvlNameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            LayoutElement lnlLE = lvlNameText.gameObject.AddComponent<LayoutElement>();
            lnlLE.preferredHeight = 28;
            Text selText = CreateText("SelectionText", "Selected: Cube", expanded.transform, 11, Color.cyan, FontStyle.Normal);
            Text helpText = CreateText("HelpText", "Press F1 for full keyboard help", expanded.transform, 10, Color.gray, FontStyle.Italic);
            expanded.SetActive(false);

            var so = new SerializedObject(ui);
            so.FindProperty("debugPanel").objectReferenceValue = panel;
            so.FindProperty("debugCompactText").objectReferenceValue = compact;
            so.FindProperty("debugExpandButton").objectReferenceValue = expandBtn;
            so.FindProperty("debugExpandedGroup").objectReferenceValue = expanded;
            so.FindProperty("modeText").objectReferenceValue = modeText;
            so.FindProperty("undoCountText").objectReferenceValue = undoText;
            so.FindProperty("currentLevelNameText").objectReferenceValue = lvlNameText;
            so.FindProperty("currentSelectionText").objectReferenceValue = selText;
            so.FindProperty("helpText").objectReferenceValue = helpText;
            so.ApplyModifiedProperties();
        }

        private static void BuildLibraryPanel(Transform canvas, LevelBuilderUI ui, LevelBuilder lb)
        {
            GameObject panel = CreatePanel("LibraryPanel", canvas);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-10, -70);
            // Wider panel so the 100x100 grid cells fit comfortably (3 cells/row
            // at 100 + 2 spacings + 8px padding each side = 332).
            rt.sizeDelta = new Vector2(360, -130);
            Image img = panel.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0.75f);

            VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 6;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            // Header row
            GameObject hdr = new GameObject("HeaderRow");
            hdr.transform.SetParent(panel.transform, false);
            HorizontalLayoutGroup hlg = hdr.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            // Disable force-expand so the X button keeps its 28px width and the
            // text absorbs the remaining space via flexibleWidth.
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            LayoutElement hdrLE = hdr.AddComponent<LayoutElement>();
            hdrLE.preferredHeight = 28;

            Text hdrText = CreateText("LibraryHeader", "BLOCKS", hdr.transform, 14, Color.cyan, FontStyle.Bold);
            hdrText.alignment = TextAnchor.MiddleLeft;
            LayoutElement hdrTxtLE = hdrText.gameObject.AddComponent<LayoutElement>();
            hdrTxtLE.flexibleWidth = 1;
            hdrTxtLE.preferredHeight = 28;
            Button closeBtn = CreateButton("LibraryCloseBtn", "X", hdr.transform, new Color(0.7f, 0.3f, 0.3f));
            LayoutElement cBLE = closeBtn.gameObject.AddComponent<LayoutElement>();
            cBLE.preferredWidth = 28;
            cBLE.preferredHeight = 28;
            cBLE.flexibleWidth = 0;

            // Search input
            InputField search = CreateInputField("LibrarySearchInput", "Search blocks...", panel.transform);
            LayoutElement sLE = search.gameObject.AddComponent<LayoutElement>();
            sLE.preferredHeight = 28;

            // Tabs row
            GameObject tabsRow = new GameObject("LibraryTabsRow");
            tabsRow.transform.SetParent(panel.transform, false);
            HorizontalLayoutGroup tHlg = tabsRow.AddComponent<HorizontalLayoutGroup>();
            tHlg.spacing = 2;
            tHlg.childForceExpandWidth = true;
            tHlg.childForceExpandHeight = true;
            tHlg.childControlWidth = true;
            tHlg.childControlHeight = true;
            LayoutElement tabsLE = tabsRow.AddComponent<LayoutElement>();
            tabsLE.preferredHeight = 24;

            // Scroll grid
            GameObject scroll = new GameObject("LibraryScrollView");
            scroll.transform.SetParent(panel.transform, false);
            LayoutElement sgrLE = scroll.AddComponent<LayoutElement>();
            sgrLE.flexibleHeight = 1;
            Image scrollBg = scroll.AddComponent<Image>();
            scrollBg.color = new Color(0, 0, 0, 0.3f);
            ScrollRect sr = scroll.AddComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            RectTransform srRT = scroll.GetComponent<RectTransform>();
            srRT.sizeDelta = new Vector2(0, 0);

            // Viewport
            GameObject vp = new GameObject("Viewport");
            vp.transform.SetParent(scroll.transform, false);
            RectTransform vpRT = vp.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = new Vector2(4, 4); vpRT.offsetMax = new Vector2(-4, -4);
            Image vpImg = vp.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0.2f);
            Mask mask = vp.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Content with GridLayoutGroup
            GameObject content = new GameObject("Content");
            content.transform.SetParent(vp.transform, false);
            RectTransform cRT = content.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1);
            cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1f);
            cRT.anchoredPosition = Vector2.zero;
            cRT.sizeDelta = new Vector2(0, 0);
            GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
            // Cell sized to fit 3-per-row in the 360px panel
            // (3*108 + 2*4 + 2*4 = 340, leaves 20px margin).
            grid.cellSize = new Vector2(108, 108);
            grid.spacing = new Vector2(4, 4);
            grid.padding = new RectOffset(4, 4, 4, 4);
            grid.childAlignment = TextAnchor.UpperLeft;
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = vpRT;
            sr.content = cRT;

            // Item prefab (template)
            GameObject itemTpl = CreateLibraryItemTemplate();
            itemTpl.transform.SetParent(canvas);
            itemTpl.SetActive(false);

            // Tab prefab (template)
            GameObject tabTpl = CreateLibraryTabTemplate();
            tabTpl.transform.SetParent(canvas);
            tabTpl.SetActive(false);

            panel.SetActive(false);

            var so = new SerializedObject(ui);
            so.FindProperty("libraryPanel").objectReferenceValue = panel;
            so.FindProperty("librarySearchInput").objectReferenceValue = search;
            so.FindProperty("libraryTabsRow").objectReferenceValue = tabsRow.transform;
            so.FindProperty("libraryGridContent").objectReferenceValue = cRT;
            so.FindProperty("libraryItemPrefab").objectReferenceValue = itemTpl;
            so.FindProperty("libraryTabPrefab").objectReferenceValue = tabTpl;
            so.FindProperty("libraryCloseButton").objectReferenceValue = closeBtn;
            so.FindProperty("libraryHeaderText").objectReferenceValue = hdrText;
            so.ApplyModifiedProperties();
        }

        private static void BuildSavedLevelsPanel(Transform canvas, LevelBuilderUI ui)
        {
            GameObject panel = CreatePanel("LevelListPanel", canvas);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(420, 420);

            VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 6;
            vlg.childForceExpandWidth = true;
            vlg.childControlWidth = true;

            // Header
            GameObject hdr = new GameObject("HeaderRow");
            hdr.transform.SetParent(panel.transform, false);
            HorizontalLayoutGroup hlg = hdr.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            LayoutElement hdrLE = hdr.AddComponent<LayoutElement>();
            hdrLE.preferredHeight = 28;

            Text hdrText = CreateText("LvlHeader", "SAVED LEVELS (0)", hdr.transform, 14, Color.cyan, FontStyle.Bold);
            hdrText.alignment = TextAnchor.MiddleLeft;
            LayoutElement htLE = hdrText.gameObject.AddComponent<LayoutElement>();
            htLE.flexibleWidth = 1;
            htLE.preferredHeight = 28;
            Button closeBtn = CreateButton("CloseLvlBtn", "X", hdr.transform, new Color(0.7f, 0.3f, 0.3f));
            LayoutElement cBLE = closeBtn.gameObject.AddComponent<LayoutElement>();
            cBLE.preferredWidth = 28;
            cBLE.preferredHeight = 28;
            cBLE.flexibleWidth = 0;

            InputField search = CreateInputField("LevelListSearchInput", "Search levels...", panel.transform);
            LayoutElement sLE = search.gameObject.AddComponent<LayoutElement>();
            sLE.preferredHeight = 28;

            GameObject scroll = new GameObject("LevelScrollView");
            scroll.transform.SetParent(panel.transform, false);
            LayoutElement sgrLE = scroll.AddComponent<LayoutElement>();
            sgrLE.flexibleHeight = 1;
            Image scrollBg = scroll.AddComponent<Image>();
            scrollBg.color = new Color(0, 0, 0, 0.3f);
            ScrollRect sr = scroll.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            RectTransform srRT = scroll.GetComponent<RectTransform>();
            srRT.sizeDelta = new Vector2(0, 0);

            GameObject vp = new GameObject("Viewport");
            vp.transform.SetParent(scroll.transform, false);
            RectTransform vpRT = vp.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = new Vector2(4, 4); vpRT.offsetMax = new Vector2(-4, -4);
            Image vpImg = vp.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0.2f);
            Mask m = vp.AddComponent<Mask>();
            m.showMaskGraphic = false;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(vp.transform, false);
            RectTransform cRT = content.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1);
            cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1f);
            cRT.anchoredPosition = Vector2.zero;
            cRT.sizeDelta = new Vector2(0, 0);
            VerticalLayoutGroup cVLG = content.AddComponent<VerticalLayoutGroup>();
            cVLG.padding = new RectOffset(3, 3, 3, 3);
            cVLG.spacing = 2;
            cVLG.childForceExpandWidth = true;
            cVLG.childControlWidth = true;
            ContentSizeFitter cFitter = content.AddComponent<ContentSizeFitter>();
            cFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = vpRT;
            sr.content = cRT;

            GameObject itemTpl = CreateLevelListItemTemplate();
            itemTpl.transform.SetParent(canvas);
            itemTpl.SetActive(false);

            panel.SetActive(false);

            var so = new SerializedObject(ui);
            so.FindProperty("levelListPanel").objectReferenceValue = panel;
            so.FindProperty("levelListSearchInput").objectReferenceValue = search;
            so.FindProperty("levelListContent").objectReferenceValue = cRT;
            so.FindProperty("levelListItemPrefab").objectReferenceValue = itemTpl;
            so.FindProperty("closeLevelListButton").objectReferenceValue = closeBtn;
            so.FindProperty("levelListHeaderText").objectReferenceValue = hdrText;
            so.ApplyModifiedProperties();
        }

        private static void BuildHelpPanel(Transform canvas, LevelBuilderUI ui)
        {
            GameObject panel = CreatePanel("HelpPanel", canvas);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(560, 560);

            VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 16, 16);
            vlg.spacing = 8;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            Text title = CreateText("HelpTitle", "═══ HORUS LEVEL MAKER ═══", panel.transform, 18, Color.cyan, FontStyle.Bold);
            title.alignment = TextAnchor.MiddleCenter;
            LayoutElement tLE = title.gameObject.AddComponent<LayoutElement>();
            tLE.preferredHeight = 30;

            Text body = CreateText("HelpBody",
                "MODES\n  B Build | V Select | X or Delete Erase\n\n" +
                "BLOCK TYPES\n  1-5 Primitive | L Toggle library | Click an item to use it\n\n" +
                "SELECTED BLOCK\n  Click drag Move (5px threshold)\n  Q/E Rotate -90°/+90° (Y)\n  Shift+Click Add/remove from selection\n\n" +
                "CAMERA (Tab to toggle)\n  WASD Pan/Move | Scroll Zoom | Middle mouse Drag\n  Shift Faster\n\n" +
                "FILE\n  SAVE/LOAD buttons (top right)\n\n" +
                "OTHER\n  Ctrl+Z Undo | Shift+RClick Erase | F1 Help | Esc Cancel",
                panel.transform, 13, Color.white, FontStyle.Normal);
            body.alignment = TextAnchor.UpperLeft;
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            LayoutElement bLE = body.gameObject.AddComponent<LayoutElement>();
            bLE.flexibleHeight = 1;

            Button closeBtn = CreateButton("HelpClose", "CLOSE", panel.transform, new Color(0.6f, 0.3f, 0.3f));
            LayoutElement cBLE = closeBtn.gameObject.AddComponent<LayoutElement>();
            cBLE.preferredHeight = 36;

            panel.SetActive(false);

            var so = new SerializedObject(ui);
            so.FindProperty("helpPanel").objectReferenceValue = panel;
            so.FindProperty("helpPanelText").objectReferenceValue = body;
            so.FindProperty("helpCloseButton").objectReferenceValue = closeBtn;
            so.ApplyModifiedProperties();
        }

        // ============== shared UI builders ==============

        private static GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image img = panel.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.7f);
            return panel;
        }

        private static void CreateSeparator(Transform parent)
        {
            GameObject sep = new GameObject("Separator");
            sep.transform.SetParent(parent, false);
            Image img = sep.AddComponent<Image>();
            img.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
            LayoutElement le = sep.AddComponent<LayoutElement>();
            le.preferredWidth = 2;
        }

        private static Button CreateButton(string name, string label, Transform parent, Color bgColor)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 30);

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            Text txt = CreateText("Text", label, btnObj.transform, 12, Color.white, FontStyle.Bold);
            txt.alignment = TextAnchor.MiddleCenter;
            RectTransform txtRT = txt.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero; txtRT.offsetMax = Vector2.zero;
            return btn;
        }

        private static Text CreateText(string name, string content, Transform parent, int fontSize, Color color, FontStyle style)
        {
            GameObject txtObj = new GameObject(name);
            txtObj.transform.SetParent(parent, false);
            Text txt = txtObj.AddComponent<Text>();
            txt.text = content;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.color = color;
            txt.fontStyle = style;
            txt.alignment = TextAnchor.MiddleLeft;
            return txt;
        }

        private static InputField CreateInputField(string name, string placeholder, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.10f, 0.15f, 0.95f);
            InputField input = go.AddComponent<InputField>();
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 28);

            // TextArea
            GameObject ta = new GameObject("Text");
            ta.transform.SetParent(go.transform, false);
            Text textComp = ta.AddComponent<Text>();
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = 12;
            textComp.color = Color.white;
            textComp.supportRichText = false;
            RectTransform taRT = ta.GetComponent<RectTransform>();
            taRT.anchorMin = Vector2.zero; taRT.anchorMax = Vector2.one;
            taRT.offsetMin = new Vector2(6, 2); taRT.offsetMax = new Vector2(-6, -2);

            // Placeholder
            GameObject ph = new GameObject("Placeholder");
            ph.transform.SetParent(go.transform, false);
            Text phComp = ph.AddComponent<Text>();
            phComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            phComp.fontSize = 12;
            phComp.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
            phComp.fontStyle = FontStyle.Italic;
            phComp.text = placeholder;
            RectTransform phRT = ph.GetComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(6, 2); phRT.offsetMax = new Vector2(-6, -2);

            input.textComponent = textComp;
            input.placeholder = phComp;
            input.targetGraphic = bg;
            input.lineType = InputField.LineType.SingleLine;
            return input;
        }

        private static GameObject CreateLibraryItemTemplate()
        {
            GameObject item = new GameObject("LibraryItemTemplate");
            RectTransform rt = item.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 80);
            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.25f, 0.4f, 0.9f);
            VerticalLayoutGroup vlg = item.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.spacing = 2;
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredWidth = 80; le.preferredHeight = 80;
            Button btn = item.AddComponent<Button>();
            btn.targetGraphic = bg;

            Text nameText = CreateText("NameText", "Block", item.transform, 10, Color.white, FontStyle.Bold);
            nameText.alignment = TextAnchor.LowerCenter;
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            return item;
        }

        private static GameObject CreateLibraryTabTemplate()
        {
            GameObject tab = new GameObject("LibraryTabTemplate");
            RectTransform rt = tab.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(40, 22);
            Image bg = tab.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            Text txt = CreateText("TabText", "Tab", tab.transform, 11, Color.white, FontStyle.Bold);
            txt.alignment = TextAnchor.MiddleCenter;
            RectTransform txtRT = txt.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero; txtRT.offsetMax = Vector2.zero;
            return tab;
        }

        private static GameObject CreateLevelListItemTemplate()
        {
            GameObject item = new GameObject("LevelListItemTemplate");
            RectTransform rt = item.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380, 40);
            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.25f, 0.4f, 0.9f);
            VerticalLayoutGroup vlg = item.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 4, 4);
            vlg.spacing = 0;
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = true;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            le.minHeight = 36;
            Text nameText = CreateText("NameText", "LevelName", item.transform, 12, Color.white, FontStyle.Normal);
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameText.verticalOverflow = VerticalWrapMode.Truncate;
            return item;
        }
    }
}
#endif
