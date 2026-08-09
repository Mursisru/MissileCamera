using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen COD AC-130 gunship HUD (EN).
    /// TV: UI FLIR CRT overlay (animated) + PostFx fisheye when ScanlinesEnabled.
    /// FUEL/THR in weapon table; RC FlirGaugeBars.Update kept.
    /// </summary>
    internal sealed class MissileCameraGunshipHud
    {
        private readonly RectTransform _root;
        private readonly GunshipTvOverlay _tv;
        private readonly GunshipTelemetry _telemetry;
        private readonly GunshipCrosshair _crosshair;
        private readonly GunshipRangeScale _range;
        private readonly GunshipWeaponStatus _weapons;
        private readonly GunshipNavFooter _nav;
        private readonly MissileCameraFlirGaugeBars _gaugeBars;
        private float _layoutW = -1f;
        private float _layoutH = -1f;

        private MissileCameraGunshipHud(
            RectTransform root,
            GunshipTvOverlay tv,
            GunshipTelemetry telemetry,
            GunshipCrosshair crosshair,
            GunshipRangeScale range,
            GunshipWeaponStatus weapons,
            GunshipNavFooter nav,
            MissileCameraFlirGaugeBars gaugeBars)
        {
            _root = root;
            _tv = tv;
            _telemetry = telemetry;
            _crosshair = crosshair;
            _range = range;
            _weapons = weapons;
            _nav = nav;
            _gaugeBars = gaugeBars;
        }

        internal RectTransform Root => _root;

        internal static MissileCameraGunshipHud Create(RectTransform parent)
        {
            var rootGo = new GameObject("MissileCameraGunshipHud", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();
            GunshipChrome.Stretch(root);

            GunshipTvOverlay tv = GunshipTvOverlay.Create(root);
            return new MissileCameraGunshipHud(
                root,
                tv,
                GunshipTelemetry.Create(root),
                GunshipCrosshair.Create(root),
                GunshipRangeScale.Create(root),
                GunshipWeaponStatus.Create(root),
                GunshipNavFooter.Create(root),
                MissileCameraFlirGaugeBars.Create(root));
        }

        internal void InvalidateLayout()
        {
            _layoutW = -1f;
            _layoutH = -1f;
        }

        internal void SetVisible(bool visible)
        {
            if (_root != null && _root.gameObject.activeSelf != visible)
                _root.gameObject.SetActive(visible);
        }

        internal void Shutdown()
        {
            try { _tv.Shutdown(); } catch { /* ignore */ }
            try { _telemetry.Shutdown(); } catch { /* ignore */ }

            try
            {
                if (_root != null)
                    Object.Destroy(_root.gameObject);
            }
            catch
            {
                // ignore destroyed
            }
        }

        internal void UpdateGaugeBarsOnly(MissileCameraHudSnapshot snapshot, MissileCameraPanelMetrics panel)
        {
            EnsureLayout(panel);
            _tv.Update();
            _gaugeBars.Update(snapshot, panel);
            HideEdgeGauges();
        }

        internal void Update(MissileCameraHudSnapshot snapshot, MissileCameraPanelMetrics panel)
        {
            EnsureLayout(panel);
            _tv.Update();
            _telemetry.Update(snapshot);
            _crosshair.Update(snapshot);
            _range.Update(snapshot);
            _weapons.Update(snapshot);
            _nav.Update(snapshot);
            _gaugeBars.Update(snapshot, panel);
            HideEdgeGauges();
        }

        private void HideEdgeGauges()
        {
            try
            {
                Transform? fuel = _root.Find("FlirFuelGauge");
                Transform? thr = _root.Find("FlirThrottleGauge");
                if (fuel != null && fuel.gameObject.activeSelf)
                    fuel.gameObject.SetActive(false);
                if (thr != null && thr.gameObject.activeSelf)
                    thr.gameObject.SetActive(false);
            }
            catch { /* ignore */ }
        }

        private void EnsureLayout(MissileCameraPanelMetrics panel)
        {
            if (Mathf.Approximately(panel.Width, _layoutW)
                && Mathf.Approximately(panel.Height, _layoutH))
                return;

            _layoutW = panel.Width;
            _layoutH = panel.Height;
            _telemetry.Place(panel);
            _crosshair.Place(panel);
            _range.Place(panel);
            _weapons.Place(panel);
            _nav.Place(panel);
        }
    }
}
