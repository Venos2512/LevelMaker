using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LevelMaker
{
    /// <summary>
    /// Lightweight toast notification system for the runtime Canvas UI.
    /// Auto-fading messages in the bottom-right corner with type-coded color.
    ///
    /// Usage:
    ///   ToastUI.Show("✓ Saved Level_20260615.json", ToastUI.Type.Success);
    ///   ToastUI.Show("Failed to load file", ToastUI.Type.Error, 5f);
    ///
    /// Toasts queue (max 3) so rapid events don't pile up endlessly.
    /// </summary>
    public class ToastUI : MonoBehaviour
    {
        public enum Type
        {
            Info,
            Success,
            Warning,
            Error
        }

        [Header("Settings")]
        [SerializeField] private float defaultDuration = 3f;
        [SerializeField] private int maxVisible = 3;
        [SerializeField] private float slideInDuration = 0.25f;
        [SerializeField] private float fadeOutDuration = 0.5f;

        [Header("Colors")]
        [SerializeField] private Color infoColor    = new Color(0.25f, 0.55f, 0.85f, 0.95f);
        [SerializeField] private Color successColor = new Color(0.30f, 0.75f, 0.40f, 0.95f);
        [SerializeField] private Color warningColor = new Color(0.95f, 0.70f, 0.20f, 0.95f);
        [SerializeField] private Color errorColor   = new Color(0.90f, 0.30f, 0.30f, 0.95f);

        [Header("References (optional)")]
        [SerializeField] private RectTransform container;
        [SerializeField] private Font font;

        // Queue + currently visible
        private readonly Queue<ToastData> _queue = new Queue<ToastData>();
        private readonly List<ToastInstance> _active = new List<ToastInstance>();

        private class ToastData
        {
            public string message;
            public Type type;
            public float duration;
        }

        private class ToastInstance
        {
            public GameObject go;
            public RectTransform rt;
            public Text text;
            public Image bg;
            public float remaining;
            public float totalDuration;
            public ToastData data;
        }

        public static ToastUI Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// Show a toast. If maxVisible is reached, oldest toast is dismissed to make room.
        /// </summary>
        public static void Show(string message, Type type = Type.Info, float duration = -1f)
        {
            if (Instance == null)
            {
                Debug.Log($"[ToastUI] No instance - logged: {message}");
                return;
            }
            if (duration <= 0f) duration = Instance.defaultDuration;
            Instance.EnqueueToast(new ToastData { message = message, type = type, duration = duration });
        }

        public static void Success(string msg, float dur = 3f) => Show(msg, Type.Success, dur);
        public static void Info(string msg, float dur = 3f)    => Show(msg, Type.Info, dur);
        public static void Warning(string msg, float dur = 4f) => Show(msg, Type.Warning, dur);
        public static void Error(string msg, float dur = 5f)   => Show(msg, Type.Error, dur);

        private void EnqueueToast(ToastData data)
        {
            // Cap visible
            while (_active.Count >= maxVisible)
            {
                DismissAt(0, immediate: true);
            }
            _queue.Enqueue(data);
            TrySpawnNext();
        }

        private void Update()
        {
            // Try to spawn queued toasts if we have room
            TrySpawnNext();

            // Update active toasts
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var t = _active[i];
                t.remaining -= Time.unscaledDeltaTime;
                float fadeAlpha = 1f;
                float fadeOutStart = t.totalDuration - fadeOutDuration;
                if (t.remaining < fadeOutDuration)
                {
                    fadeAlpha = Mathf.Clamp01(t.remaining / fadeOutDuration);
                }
                if (t.bg != null)
                {
                    var c = t.bg.color;
                    c.a = fadeAlpha * (c.a > 0.01f ? 1f : 1f);
                    t.bg.color = c;
                }
                if (t.text != null)
                {
                    var c = t.text.color;
                    c.a = fadeAlpha;
                    t.text.color = c;
                }
                if (t.remaining <= 0f)
                {
                    DismissAt(i, immediate: true);
                }
            }
        }

        private void TrySpawnNext()
        {
            while (_queue.Count > 0 && _active.Count < maxVisible)
            {
                var data = _queue.Dequeue();
                var inst = CreateToastInstance(data);
                _active.Add(inst);
            }
        }

        private ToastInstance CreateToastInstance(ToastData data)
        {
            if (container == null)
            {
                // Lazy-create a container anchored to bottom-right
                EnsureContainer();
            }

            GameObject go = new GameObject("Toast");
            go.transform.SetParent(container, false);

            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360, 36);

            Image bg = go.AddComponent<Image>();
            bg.color = GetColor(data.type);
            bg.raycastTarget = false;

            HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(12, 12, 4, 4);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 36;
            le.minHeight = 32;

            // Text and Image are both Graphic components - can't coexist on
            // the same GameObject. Put Text on a child.
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            Text text = textGo.AddComponent<Text>();
            text.text = data.message;
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 12;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            LayoutElement textLE = textGo.AddComponent<LayoutElement>();
            textLE.flexibleWidth = 1;

            // Slide-in: start slightly off-screen on the right
            rt.anchoredPosition = new Vector2(80, 0);

            return new ToastInstance
            {
                go = go,
                rt = rt,
                text = text,
                bg = bg,
                remaining = data.duration,
                totalDuration = data.duration,
                data = data
            };
        }

        private void EnsureContainer()
        {
            var containerGo = new GameObject("ToastContainer");
            containerGo.transform.SetParent(transform, false);
            container = containerGo.AddComponent<RectTransform>();
            container.anchorMin = new Vector2(1, 0);
            container.anchorMax = new Vector2(1, 0);
            container.pivot = new Vector2(1, 0);
            container.anchoredPosition = new Vector2(-16, 16);
            container.sizeDelta = new Vector2(380, 0);

            VerticalLayoutGroup vlg = containerGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.childAlignment = TextAnchor.LowerRight;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            ContentSizeFitter fitter = containerGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private Color GetColor(Type type)
        {
            switch (type)
            {
                case Type.Success: return successColor;
                case Type.Warning: return warningColor;
                case Type.Error:   return errorColor;
                default:           return infoColor;
            }
        }

        private void DismissAt(int index, bool immediate)
        {
            if (index < 0 || index >= _active.Count) return;
            var t = _active[index];
            if (t.go != null) Destroy(t.go);
            _active.RemoveAt(index);
        }
    }
}
