using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>Center stacked diagnostic lines + final SUCCESSFUL badge.</summary>
    internal sealed class MissileCameraBootDiagnostics
    {
        private static readonly Color FlirGreen = new Color(0.2f, 1f, 0.45f, 1f);
        private const int MaxLines = 8;
        private const float LineHeight = 22f;

        private readonly GameObject _rootGo;
        private readonly RectTransform _root;
        private readonly CanvasGroup _stackGroup;
        private readonly Text[] _lines;
        private readonly string[] _prefixes;
        private int _count;
        private GameObject? _badgeGo;

        private MissileCameraBootDiagnostics(
            GameObject rootGo,
            RectTransform root,
            CanvasGroup stackGroup,
            Text[] lines,
            string[] prefixes)
        {
            _rootGo = rootGo;
            _root = root;
            _stackGroup = stackGroup;
            _lines = lines;
            _prefixes = prefixes;
        }

        internal static MissileCameraBootDiagnostics Create(RectTransform parent)
        {
            var rootGo = new GameObject("BootDiagnostics", typeof(RectTransform), typeof(CanvasGroup));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchoredPosition = Vector2.zero;
            root.sizeDelta = new Vector2(720f, MaxLines * LineHeight + 80f);
            root.SetAsLastSibling();

            CanvasGroup stackGroup = rootGo.GetComponent<CanvasGroup>();
            stackGroup.blocksRaycasts = false;
            stackGroup.interactable = false;
            stackGroup.alpha = 1f;

            var lines = new Text[MaxLines];
            var prefixes = new string[MaxLines];
            for (int i = 0; i < MaxLines; i++)
            {
                lines[i] = CreateLabel(root, "Diag" + i);
                lines[i].gameObject.SetActive(false);
                prefixes[i] = string.Empty;
            }

            return new MissileCameraBootDiagnostics(rootGo, root, stackGroup, lines, prefixes);
        }

        internal void BeginStage(string title)
        {
            if (_count >= MaxLines)
                return;

            int i = _count++;
            _prefixes[i] = title;
            Text line = _lines[i];
            line.gameObject.SetActive(true);
            line.text = title + "...";
            Relayout();
        }

        internal void CompleteCurrentStage()
        {
            if (_count <= 0)
                return;

            int i = _count - 1;
            _lines[i].text = _prefixes[i] + "... SUCCESSFUL";
        }

        internal void DimStackAndShowBadge()
        {
            _stackGroup.alpha = 0.5f;
            if (_badgeGo != null)
                return;

            _badgeGo = new GameObject("BootSuccessfulBadge", typeof(RectTransform), typeof(Image));
            _badgeGo.transform.SetParent(_root, false);
            RectTransform rt = _badgeGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(280f, 48f);
            Image bg = _badgeGo.GetComponent<Image>();
            bg.color = new Color(0.02f, 0.08f, 0.04f, 0.92f);
            bg.raycastTarget = false;

            Text label = CreateLabel(rt, "BadgeText");
            RectTransform lrt = label.rectTransform;
            Stretch(lrt);
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 20;
            label.text = "SUCCESSFUL";
            label.color = FlirGreen;
        }

        internal void Destroy()
        {
            if (_rootGo != null)
                Object.Destroy(_rootGo);
        }

        private void Relayout()
        {
            float totalH = _count * LineHeight;
            float y0 = totalH * 0.5f - LineHeight * 0.5f;
            for (int i = 0; i < _count; i++)
            {
                RectTransform rt = _lines[i].rectTransform;
                rt.anchoredPosition = new Vector2(0f, y0 - i * LineHeight);
            }
        }

        private static Text CreateLabel(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(700f, LineHeight);
            Text text = go.GetComponent<Text>();
            text.font = HudFontHelper.GetFont();
            text.fontSize = 15;
            text.color = FlirGreen;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }
    }
}
