#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using LevelMaker;

namespace LevelMaker.Editor
{
    /// <summary>
    /// Editor utility to create a complete Canvas UI for LevelBuilder with all buttons pre-wired.
    /// Run from menu: Tools > Level Maker > Create Level Builder UI
    /// </summary>
    public class LevelBuilderUICreator
    {
        [MenuItem("Tools/Level Maker/Create Level Builder UI")]
        public static void CreateLevelBuilderUI()
        {
            // Find or create Canvas
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("LevelBuilderCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();

                // Add EventSystem if needed
                if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    GameObject eventObj = new GameObject("EventSystem");
                    eventObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            // Get or create LevelBuilderUI component
            LevelBuilderUI ui = Object.FindObjectOfType<LevelBuilderUI>();
            if (ui == null)
            {
                GameObject uiObj = new GameObject("LevelBuilderUI");
                ui = uiObj.AddComponent<LevelBuilderUI>();
                uiObj.transform.SetParent(canvas.transform, false);
            }

            // Get references
            LevelBuilder levelBuilder = Object.FindObjectOfType<LevelBuilder>();
            BlockLibrary blockLibrary = Object.FindObjectOfType<BlockLibrary>();

            // Build top mode buttons
            GameObject buttonsPanel = CreateTopButtonsPanel(canvas.transform, ui, levelBuilder);

            // Build library panel
            GameObject libPanel = CreateLibraryPanel(canvas.transform, ui, levelBuilder, blockLibrary);

            // Build debug panel (bottom-left)
            GameObject debugPanel = CreateDebugPanel(canvas.transform, ui);

            // Wire up references
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("levelBuilder").objectReferenceValue = levelBuilder;
            so.FindProperty("blockLibrary").objectReferenceValue = blockLibrary;
            so.ApplyModifiedProperties();

            // Mark scene dirty
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("[LevelBuilderUICreator] UI created! Check Canvas in scene.");
        }

        private static GameObject CreateTopButtonsPanel(Transform parent, LevelBuilderUI ui, LevelBuilder levelBuilder)
        {
            GameObject panel = CreatePanel("TopButtonsPanel", parent);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, -10);
            rt.sizeDelta = new Vector2(500, 50);

            HorizontalLayoutGroup hlg = panel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            // Create 3 buttons: Build, Erase, Select
            Button buildBtn = CreateButton("BuildButton", "BUILD (B)", panel.transform, new Color(0.3f, 0.7f, 0.3f));
            Button eraseBtn = CreateButton("EraseButton", "ERASE (E)", panel.transform, new Color(0.7f, 0.3f, 0.3f));
            Button selectBtn = CreateButton("SelectButton", "SELECT (V)", panel.transform, new Color(0.7f, 0.7f, 0.3f));

            // Wire button events
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("buildButton").objectReferenceValue = buildBtn;
            so.FindProperty("eraseButton").objectReferenceValue = eraseBtn;
            so.FindProperty("selectButton").objectReferenceValue = selectBtn;
            so.ApplyModifiedProperties();

            return panel;
        }

        private static GameObject CreateLibraryPanel(Transform parent, LevelBuilderUI ui, LevelBuilder levelBuilder, BlockLibrary blockLibrary)
        {
            GameObject panel = CreatePanel("LibraryPanel", parent);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-10, -70);
            rt.sizeDelta = new Vector2(-260, -130); // Right side, leave space for debug

            // Add a vertical layout
            VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(5, 5, 5, 5);
            vlg.spacing = 3;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            // Add a ScrollView for content
            GameObject scrollObj = new GameObject("LibraryScrollView");
            scrollObj.transform.SetParent(panel.transform, false);
            RectTransform scrollRT = scrollObj.AddComponent<RectTransform>();
            scrollRT.sizeDelta = new Vector2(0, 0);
            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0, 0, 0, 0.3f);
            ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = new Vector2(5, 5);
            vpRT.offsetMax = new Vector2(-5, -5);
            Image vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(0, 0, 0, 0.2f);
            viewport.AddComponent<Mask>().showMaskGraphic = false;

            // Content (this is where items go)
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRT = content.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup contentVLG = content.AddComponent<VerticalLayoutGroup>();
            contentVLG.padding = new RectOffset(3, 3, 3, 3);
            contentVLG.spacing = 2;
            contentVLG.childForceExpandWidth = true;
            contentVLG.childForceExpandHeight = false;
            contentVLG.childControlWidth = true;
            contentVLG.childControlHeight = true;
            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRT;
            scroll.content = contentRT;

            // Create library item prefab template
            GameObject itemTemplate = CreateLibraryItemTemplate();
            itemTemplate.transform.SetParent(panel.transform.parent); // Outside the panel so it won't show
            itemTemplate.SetActive(false);
            itemTemplate.name = "LibraryItemTemplate";

            // Header for library
            Text header = CreateText("LibraryHeader", "BLOCK LIBRARY", panel.transform, 16, Color.cyan, FontStyle.Bold);
            LayoutElement headerLE = header.gameObject.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 25;

            // Wire references
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("libraryPanel").objectReferenceValue = panel;
            so.FindProperty("libraryContent").objectReferenceValue = content.transform;
            so.FindProperty("libraryItemPrefab").objectReferenceValue = itemTemplate;
            so.ApplyModifiedProperties();

            return panel;
        }

        private static GameObject CreateLibraryItemTemplate()
        {
            GameObject item = new GameObject("LibraryItem");
            RectTransform rt = item.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 40);

            Image bg = item.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            HorizontalLayoutGroup hlg = item.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(5, 5, 5, 5);
            hlg.spacing = 5;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 40;
            le.minHeight = 40;

            // Name text
            Text nameText = CreateText("NameText", "BlockName", item.transform, 12, Color.white, FontStyle.Normal);
            LayoutElement nameLE = nameText.gameObject.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 1;

            // Use button
            Button useBtn = CreateButton("UseButton", "Use", item.transform, new Color(0.3f, 0.6f, 0.3f));
            LayoutElement btnLE = useBtn.gameObject.AddComponent<LayoutElement>();
            btnLE.preferredWidth = 50;

            return item;
        }

        private static GameObject CreateDebugPanel(Transform parent, LevelBuilderUI ui)
        {
            GameObject panel = CreatePanel("DebugPanel", parent);
            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(10, 10);
            rt.sizeDelta = new Vector2(280, 80);

            VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(5, 5, 5, 5);

            Text modeText = CreateText("ModeText", "Mode: Build | Items: 0", panel.transform, 14, Color.white, FontStyle.Bold);
            Text undoText = CreateText("UndoText", "Undo: 0", panel.transform, 12, Color.yellow, FontStyle.Normal);
            Text helpText = CreateText("HelpText",
                "1-5: Block type\n" +
                "B/E/V: Mode | L: Library\n" +
                "Ctrl+Z: Undo | Shift+RClick: Erase",
                panel.transform, 10, Color.gray, FontStyle.Normal);
            helpText.horizontalOverflow = HorizontalWrapMode.Wrap;

            // Wire references
            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("currentModeText").objectReferenceValue = modeText;
            so.FindProperty("debugText").objectReferenceValue = modeText;
            so.FindProperty("undoCountText").objectReferenceValue = undoText;
            so.ApplyModifiedProperties();

            return panel;
        }

        private static GameObject CreatePanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image img = panel.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.7f);
            return panel;
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

            // Label
            Text txt = CreateText("Text", label, btnObj.transform, 14, Color.white, FontStyle.Bold);
            txt.alignment = TextAnchor.MiddleCenter;
            RectTransform txtRT = txt.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;

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
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            return txt;
        }
    }
}
#endif
