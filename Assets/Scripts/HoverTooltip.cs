using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LevelMaker
{
    /// <summary>
    /// Cheap, single-instance tooltip for library items. Set <see cref="Text"/>
    /// on hover, clear on exit. No allocation per hover - the label is created
    /// once at startup and moved/parented lazily.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class HoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [TextArea] public string fullText;
        public float showDelay = 0.3f;
        public Vector2 offset = new Vector2(0, 28);

        private static HoverTooltip _instance;
        private static GameObject _labelGo;
        private static Text _labelText;
        private static RectTransform _labelRT;
        private static Canvas _labelCanvas;

        private float _hoverTime;
        private bool _isHovering;
        private bool _isShowing;

        private void Awake()
        {
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                if (_labelGo != null) Destroy(_labelGo);
                _labelGo = null;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovering = true;
            _hoverTime = Time.unscaledTime;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
            Hide();
        }

        private void Update()
        {
            if (!_isHovering) return;
            if (!_isShowing && Time.unscaledTime - _hoverTime >= showDelay)
            {
                Show();
            }
            if (_isShowing && _labelRT != null)
            {
                // Track the mouse position so the tooltip follows the cursor.
                Vector2 pos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)_labelRT.parent, Input.mousePosition, null, out pos);
                _labelRT.anchoredPosition = pos + offset;
            }
        }

        private void Show()
        {
            if (string.IsNullOrEmpty(fullText)) return;
            EnsureLabel();
            if (_labelText != null) _labelText.text = fullText;
            if (_labelGo != null) _labelGo.SetActive(true);
            _isShowing = true;
        }

        private void Hide()
        {
            _isShowing = false;
            if (_labelGo != null) _labelGo.SetActive(false);
        }

        private static void EnsureLabel()
        {
            if (_labelGo != null) return;
            // Try to find a Canvas to parent under - prefer the topmost
            _labelCanvas = Object.FindObjectOfType<Canvas>();
            if (_labelCanvas == null) return;

            _labelGo = new GameObject("HoverTooltip");
            _labelGo.transform.SetParent(_labelCanvas.transform, false);

            // Make sure the tooltip draws on top of everything
            _labelGo.transform.SetAsLastSibling();

            _labelRT = _labelGo.AddComponent<RectTransform>();
            _labelRT.anchorMin = Vector2.zero; _labelRT.anchorMax = Vector2.zero;
            _labelRT.pivot = new Vector2(0, 0);
            _labelRT.sizeDelta = new Vector2(360, 0);

            HorizontalLayoutGroup hlg = _labelGo.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 4, 4);
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;

            Image bg = _labelGo.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);
            bg.raycastTarget = false;

            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(_labelGo.transform, false);
            _labelText = textGo.AddComponent<Text>();
            _labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _labelText.fontSize = 11;
            _labelText.color = Color.white;
            _labelText.alignment = TextAnchor.MiddleLeft;
            _labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _labelText.verticalOverflow = VerticalWrapMode.Overflow;
            _labelText.raycastTarget = false;

            ContentSizeFitter fitter = _labelGo.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _labelGo.SetActive(false);
        }
    }
}
