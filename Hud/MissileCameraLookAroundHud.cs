using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>Small center cross while FS look-around (RMB) is active.</summary>
    internal static class MissileCameraLookAroundHud
    {
        private const float ArmPx = 7f;
        private const float ThickPx = 1.8f;

        private static GameObject? _root;
        private static bool _visible;

        internal static void SetVisible(bool visible)
        {
            _visible = visible;
            if (!visible)
            {
                if (_root != null)
                    _root.SetActive(false);
                return;
            }

            EnsureUi();
            if (_root != null)
                _root.SetActive(true);
        }

        internal static void DestroyUi()
        {
            if (_root != null)
            {
                Object.Destroy(_root);
                _root = null;
            }

            _visible = false;
        }

        private static void EnsureUi()
        {
            if (_root != null)
            {
                ReparentIfNeeded();
                return;
            }

            RectTransform? parent = MissileCameraFullscreenFeedHost.ViewRt
                ?? MissileCameraFullscreenFeedHost.PanelRt;
            if (parent == null)
                return;

            _root = new GameObject("MissileCamera.LookCenterMark");
            _root.hideFlags = HideFlags.HideAndDontSave;
            _root.transform.SetParent(parent, false);

            var rt = _root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(ArmPx * 2f, ArmPx * 2f);
            rt.localScale = Vector3.one;

            AddArm(_root.transform, "H", new Vector2(ArmPx, ThickPx));
            AddArm(_root.transform, "V", new Vector2(ThickPx, ArmPx));
            _root.SetActive(_visible);
            _root.transform.SetAsLastSibling();
        }

        private static void ReparentIfNeeded()
        {
            RectTransform? parent = MissileCameraFullscreenFeedHost.ViewRt
                ?? MissileCameraFullscreenFeedHost.PanelRt;
            if (parent == null || _root == null)
                return;
            if (_root.transform.parent != parent)
            {
                _root.transform.SetParent(parent, false);
                _root.transform.SetAsLastSibling();
            }
        }

        private static void AddArm(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.78f);
            img.raycastTarget = false;
        }
    }
}
