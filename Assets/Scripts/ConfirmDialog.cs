using System;
using UnityEngine;
using UnityEngine.UI;

namespace LevelMaker
{
    /// <summary>
    /// Runtime confirm dialog (Yes/No) that lives in the Canvas UI.
    /// Replaces EditorUtility.DisplayDialog for in-game flows so we can
    /// keep the same look in both Editor playmode and Standalone builds.
    ///
    /// Usage:
    ///   ConfirmDialog.Show(
    ///       title: "Load this level?",
    ///       message: "All current blocks will be replaced.",
    ///       onYes: () => { ... },
    ///       onNo:  () => { ... });
    /// </summary>
    public class ConfirmDialog : MonoBehaviour
    {
        [Header("References (auto-created if null)")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text messageText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        [SerializeField] private Text yesButtonText;
        [SerializeField] private Text noButtonText;
        [SerializeField] private Font font;

        public static ConfirmDialog Instance { get; private set; }

        private Action _onYes;
        private Action _onNo;
        private string _yesLabel = "Yes";
        private string _noLabel = "No";

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static void Show(string title, string message, Action onYes = null, Action onNo = null,
            string yesLabel = "Yes", string noLabel = "No")
        {
            if (Instance == null)
            {
                Debug.LogWarning("[ConfirmDialog] No instance - falling back to default behavior");
                onYes?.Invoke();
                return;
            }
            Instance.ShowInternal(title, message, onYes, onNo, yesLabel, noLabel);
        }

        private void ShowInternal(string title, string message, Action onYes, Action onNo,
            string yesLabel, string noLabel)
        {
            _onYes = onYes;
            _onNo = onNo;
            _yesLabel = yesLabel;
            _noLabel = noLabel;

            EnsureUI();

            if (titleText != null) titleText.text = title;
            if (messageText != null) messageText.text = message;
            if (yesButtonText != null) yesButtonText.text = yesLabel;
            if (noButtonText != null) noButtonText.text = noLabel;

            if (rootPanel != null) rootPanel.SetActive(true);
        }

        public void OnYesClicked()
        {
            var cb = _onYes;
            ClearAndHide();
            cb?.Invoke();
        }

        public void OnNoClicked()
        {
            var cb = _onNo;
            ClearAndHide();
            cb?.Invoke();
        }

        private void ClearAndHide()
        {
            _onYes = null;
            _onNo = null;
            if (rootPanel != null) rootPanel.SetActive(false);
        }

        /// <summary>
        /// Lazy-create the dialog UI under this GameObject. Safe to call multiple times.
        /// </summary>
        private void EnsureUI()
        {
            if (rootPanel != null && rootPanel.activeSelf) return;
            if (rootPanel != null && !rootPanel.activeSelf)
            {
                // Already built, just hidden
                return;
            }

            // Full-screen overlay panel
            rootPanel = new GameObject("ConfirmDialogPanel");
            rootPanel.transform.SetParent(transform, false);

            RectTransform rt = rootPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Dim background
            Image overlay = rootPanel.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.5f);
            overlay.raycastTarget = true;

            // Centered dialog box
            GameObject box = new GameObject("DialogBox");
            box.transform.SetParent(rootPanel.transform, false);
            RectTransform boxRT = box.AddComponent<RectTransform>();
            boxRT.anchorMin = new Vector2(0.5f, 0.5f);
            boxRT.anchorMax = new Vector2(0.5f, 0.5f);
            boxRT.pivot = new Vector2(0.5f, 0.5f);
            boxRT.anchoredPosition = Vector2.zero;
            boxRT.sizeDelta = new Vector2(420, 200);

            Image boxBg = box.AddComponent<Image>();
            boxBg.color = new Color(0.12f, 0.12f, 0.15f, 0.97f);
            boxBg.raycastTarget = true;

            VerticalLayoutGroup vlg = box.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 16, 16);
            vlg.spacing = 10;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            // Title
            titleText = CreateText("Title", "Confirm?", box.transform, 18, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            LayoutElement titleLE = titleText.gameObject.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 30;

            // Message
            messageText = CreateText("Message", "Are you sure?", box.transform, 13, new Color(0.85f, 0.85f, 0.85f), FontStyle.Normal, TextAnchor.UpperLeft);
            messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
            LayoutElement msgLE = messageText.gameObject.AddComponent<LayoutElement>();
            msgLE.preferredHeight = 70;
            msgLE.flexibleHeight = 1;

            // Buttons row
            GameObject btnRow = new GameObject("ButtonRow");
            btnRow.transform.SetParent(box.transform, false);
            HorizontalLayoutGroup hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            LayoutElement btnLE = btnRow.AddComponent<LayoutElement>();
            btnLE.preferredHeight = 40;

            // Yes button (green)
            yesButton = CreateButton("YesBtn", "Yes", btnRow.transform, new Color(0.3f, 0.65f, 0.35f), out yesButtonText);
            yesButton.onClick.AddListener(OnYesClicked);

            // No button (red)
            noButton = CreateButton("NoBtn", "No", btnRow.transform, new Color(0.65f, 0.3f, 0.3f), out noButtonText);
            noButton.onClick.AddListener(OnNoClicked);
        }

        private Text CreateText(string name, string content, Transform parent, int size, Color color, FontStyle style, TextAnchor align)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Text t = obj.AddComponent<Text>();
            t.text = content;
            t.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.alignment = align;
            return t;
        }

        private Button CreateButton(string name, string label, Transform parent, Color bgColor, out Text labelText)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(100, 36);

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            labelText = CreateText("Text", label, btnObj.transform, 14, Color.white, FontStyle.Bold, TextAnchor.MiddleCenter);
            RectTransform txtRT = labelText.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = Vector2.zero;
            txtRT.offsetMax = Vector2.zero;

            return btn;
        }
    }
}
