using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Independent fullscreen feed surface — never reparents MFD panel, always landscape stretch.
    /// </summary>
    internal static class MissileCameraFullscreenFeedHost
    {
        private const float LandscapeContentRotationZ = 0f;

        private static readonly MissileCameraHudOverlay HudOverlay = new MissileCameraHudOverlay();

        private static RectTransform? _panelRt;
        private static RectTransform? _viewRt;
        private static RawImage? _feedImage;

        internal static RectTransform? PanelRt => _panelRt;
        internal static RectTransform? ViewRt => _viewRt ?? _panelRt;
        internal static RawImage? FeedImage => _feedImage;
        internal static MissileCameraHudOverlay Hud => HudOverlay;

        internal static void EnsureBuilt(RectTransform fullscreenRoot)
        {
            if (fullscreenRoot == null)
                return;

            if (_panelRt != null && _panelRt.parent == fullscreenRoot)
            {
                LockLandscapeLayout();
                return;
            }

            Destroy();

            var panelGo = new GameObject("MissileCameraFullscreenPanel", typeof(RectTransform));
            panelGo.transform.SetParent(fullscreenRoot, false);
            _panelRt = panelGo.GetComponent<RectTransform>();
            Stretch(_panelRt);
            _panelRt.localScale = Vector3.one;
            _panelRt.localRotation = Quaternion.identity;

            _viewRt = _panelRt;

            var feedGo = new GameObject("MissileCameraFeed", typeof(RectTransform), typeof(RawImage));
            feedGo.transform.SetParent(_viewRt, false);
            feedGo.transform.SetAsFirstSibling();
            RectTransform feedRt = feedGo.GetComponent<RectTransform>();
            Stretch(feedRt);
            feedRt.localRotation = Quaternion.identity;

            _feedImage = feedGo.GetComponent<RawImage>();
            _feedImage.raycastTarget = false;
            _feedImage.color = Color.white;

            LockLandscapeLayout();
        }

        private static void LockLandscapeLayout()
        {
            if (_panelRt == null)
                return;

            _panelRt.localRotation = Quaternion.identity;
            MissileCameraFeedLayout.ApplyContentRotation(_panelRt, LandscapeContentRotationZ);
            HudOverlay.EnsureBuilt(_panelRt, screenUi: null, contentRotationZOverride: LandscapeContentRotationZ);
            _viewRt = MissileCameraFeedLayout.ResolveProjectionRect(_panelRt) ?? _panelRt;

            Transform? feed = _viewRt.Find("MissileCameraFeed");
            if (feed != null && feed.TryGetComponent(out RawImage feedImage))
                _feedImage = feedImage;

            HudOverlay.InvalidateDynamicSchedule();
        }

        internal static void Show()
        {
            if (_panelRt != null)
                _panelRt.gameObject.SetActive(true);
        }

        internal static void Hide()
        {
            if (_panelRt != null)
                _panelRt.gameObject.SetActive(false);
        }

        internal static void ResetForMissionUnload()
        {
            Destroy();
        }

        private static void Destroy()
        {
            HudOverlay.Destroy();
            _feedImage = null;
            _viewRt = null;

            if (_panelRt != null)
            {
                Object.Destroy(_panelRt.gameObject);
                _panelRt = null;
            }
        }

        private static void Stretch(RectTransform rt)
        {
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
