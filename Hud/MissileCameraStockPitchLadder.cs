using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>
    /// Fullscreen stock pitch ladder: one RawImage, stock UV/roll/FOV (scale = 50/fov).
    /// UI/Default + FLIR tint + black Outline (same as FLIR text). No hierarchy clone.
    /// </summary>
    internal sealed class MissileCameraStockPitchLadder
    {
        private const float StockFovReference = 50f;
        private const float PitchUvYOffset = 0.437f;
        private const float PitchUvHeight = 0.126f;
        private static readonly Color FlirOutlineColor = new Color(0f, 0f, 0f, 0.85f);
        private static readonly Vector2 PitchLadderOutlineDistance = new Vector2(0.45f, 0.45f);

        private static readonly FieldInfo? PitchCompassField =
            AccessToolsField(typeof(FlightHud), "pitchCompass");
        private static readonly FieldInfo? PitchCompassCenterField =
            AccessToolsField(typeof(FlightHud), "pitchCompassCenter");

        private static bool _sourceResolved;
        private static RawImage? _sourcePitchImage;
        private static RectTransform? _sourcePitchRect;
        private static RectTransform? _sourceRollRect;

        private GameObject? _host;
        private RectTransform? _rollRoot;
        private RawImage? _pitchImage;
        private Material? _pitchMaterial;
        private bool _bindFailed;
        private bool _loggedBindFail;
        private float _lastRollZ = float.NaN;
        private float _lastPitchDeg = float.NaN;
        private float _lastFov = float.NaN;
        private float _lastIntensity = -1f;
        private Color _lastTint = Color.clear;

        internal void EnsureBuilt(RectTransform parent)
        {
            if (parent == null)
                return;

            if (_host != null)
                return;

            ResolveSource();
            if (_sourcePitchImage == null || _sourcePitchImage.texture == null)
            {
                _bindFailed = true;
                if (!_loggedBindFail)
                {
                    _loggedBindFail = true;
                    MfdLog.Info("pitch ladder: FlightHud pitchCompass source missing");
                }

                return;
            }

            _host = new GameObject("MissileCameraStockPitchLadder", typeof(RectTransform));
            _host.transform.SetParent(parent, false);
            _host.transform.SetAsFirstSibling();

            RectTransform hostRt = _host.GetComponent<RectTransform>();
            StretchCenter(hostRt);

            var rollGo = new GameObject("PitchLadderRoll", typeof(RectTransform));
            rollGo.transform.SetParent(hostRt, false);
            _rollRoot = rollGo.GetComponent<RectTransform>();
            if (_sourceRollRect != null)
                CopyRectTransform(_sourceRollRect, _rollRoot);
            else
                CenterOnParent(_rollRoot);

            var pitchGo = new GameObject("PitchCompass", typeof(RectTransform), typeof(RawImage));
            pitchGo.transform.SetParent(_rollRoot, false);
            RectTransform pitchRt = pitchGo.GetComponent<RectTransform>();
            if (_sourcePitchRect != null)
                CopyRectTransform(_sourcePitchRect, pitchRt);
            else
                StretchCenter(pitchRt);

            _pitchImage = pitchGo.GetComponent<RawImage>();
            SetupPitchVisuals();
            ApplyTint(force: true);
            InvalidateMotion();
            MfdLog.Info("pitch ladder: clean single-layer ready");
        }

        internal void Update(Camera? feedCamera, Transform? missileBody, bool visible)
        {
            if (_bindFailed || _host == null)
                return;

            if (!visible || feedCamera == null || missileBody == null)
            {
                if (_host.activeSelf)
                    _host.SetActive(false);
                return;
            }

            MissileCameraFullscreenConfig.Refresh();
            if (!MissileCameraFullscreenConfig.PitchLadderEnabled)
            {
                if (_host.activeSelf)
                    _host.SetActive(false);
                return;
            }

            if (!_host.activeSelf)
                _host.SetActive(true);

            ApplyTint(force: false);

            float rollZ = feedCamera.transform.eulerAngles.z;
            float fov = Mathf.Max(feedCamera.fieldOfView, 0.1f);
            float pitchDeg = missileBody.eulerAngles.x;

            if (_rollRoot != null
                && (float.IsNaN(_lastRollZ)
                    || !Mathf.Approximately(_lastRollZ, rollZ)
                    || !Mathf.Approximately(_lastPitchDeg, pitchDeg)
                    || !Mathf.Approximately(_lastFov, fov)))
            {
                _lastRollZ = rollZ;
                _lastPitchDeg = pitchDeg;
                _lastFov = fov;

                _rollRoot.localEulerAngles = new Vector3(0f, 0f, -rollZ);

                float scale = StockFovReference / fov;
                if (_pitchImage != null)
                    _pitchImage.transform.localScale = Vector3.one * scale;

                if (_pitchImage != null)
                {
                    float uvY = -pitchDeg / 180f + PitchUvYOffset;
                    _pitchImage.uvRect = new Rect(1f, uvY, 1f, PitchUvHeight);
                }
            }
        }

        internal void SetVisible(bool visible)
        {
            if (_host == null)
                return;

            if (!visible)
            {
                _host.SetActive(false);
                InvalidateMotion();
            }
        }

        internal void Shutdown()
        {
            DestroyMaterial(ref _pitchMaterial);

            if (_host != null)
            {
                Object.Destroy(_host);
                _host = null;
            }

            _rollRoot = null;
            _pitchImage = null;
            _bindFailed = false;
            _loggedBindFail = false;
            InvalidateMotion();
        }

        internal static void ResetSourceCache()
        {
            _sourceResolved = false;
            _sourcePitchImage = null;
            _sourcePitchRect = null;
            _sourceRollRect = null;
        }

        private void SetupPitchVisuals()
        {
            if (_pitchImage == null || _sourcePitchImage == null)
                return;

            _pitchImage.raycastTarget = false;
            _pitchImage.maskable = false;
            _pitchImage.texture = _sourcePitchImage.texture;
            _pitchImage.uvRect = _sourcePitchImage.uvRect;

            DestroyMaterial(ref _pitchMaterial);
            _pitchMaterial = CreateUiMaterial(_sourcePitchImage.texture);
            _pitchImage.material = _pitchMaterial;

            Outline outline = _pitchImage.GetComponent<Outline>();
            if (outline == null)
                outline = _pitchImage.gameObject.AddComponent<Outline>();
            outline.effectColor = FlirOutlineColor;
            outline.effectDistance = PitchLadderOutlineDistance;
            outline.useGraphicAlpha = true;
        }

        private static Material CreateUiMaterial(Texture? texture)
        {
            Shader? shader = Shader.Find("UI/Default") ?? Shader.Find("Sprites/Default");
            var material = new Material(shader!);
            if (texture != null)
                material.mainTexture = texture;
            return material;
        }

        private void ApplyTint(bool force)
        {
            MissileCameraFullscreenConfig.Refresh();
            float intensity = MissileCameraFullscreenConfig.PitchLadderIntensity;
            Color tint = ResolveBrightFlirTint(intensity);

            if (!force && ColorsMatch(_lastTint, tint) && Mathf.Approximately(_lastIntensity, intensity))
                return;

            _lastTint = tint;
            _lastIntensity = intensity;

            if (_pitchImage == null)
                return;

            _pitchImage.color = tint;
            if (_pitchMaterial != null && _pitchMaterial.HasProperty("_Color"))
                _pitchMaterial.SetColor("_Color", tint);
        }

        private static Color ResolveBrightFlirTint(float intensity)
        {
            Color baseTint = ResolveBaseTint();
            float boost = Mathf.Clamp(intensity, 1f, 4f);
            // Keep FLIR hue; only lift alpha/readability via slight brightness, never wash to white.
            float lift = 1f + (boost - 1f) * 0.08f;
            return new Color(
                Mathf.Clamp01(baseTint.r * lift),
                Mathf.Clamp01(baseTint.g * lift),
                Mathf.Clamp01(baseTint.b * lift),
                1f);
        }

        private static Color ResolveBaseTint()
        {
            Color configured = MissileCameraFullscreenConfig.PitchLadderTint;
            Color flir = MissileCameraFlirHud.MarkerColor;
            return ColorsMatch(configured, MissileCameraFullscreenConfig.DefaultPitchLadderTintValue)
                ? flir
                : configured;
        }

        private void InvalidateMotion()
        {
            _lastRollZ = float.NaN;
            _lastPitchDeg = float.NaN;
            _lastFov = float.NaN;
            _lastIntensity = -1f;
            _lastTint = Color.clear;
        }

        private static void DestroyMaterial(ref Material? material)
        {
            if (material == null)
                return;

            Object.Destroy(material);
            material = null;
        }

        private static void ResolveSource()
        {
            if (_sourceResolved)
                return;

            _sourceResolved = true;

            try
            {
                FlightHud? flightHud = SceneSingleton<FlightHud>.i;
                if (flightHud == null)
                    return;

                if (PitchCompassField?.GetValue(flightHud) is RawImage pitchImage)
                {
                    _sourcePitchImage = pitchImage;
                    _sourcePitchRect = pitchImage.rectTransform;
                }

                if (PitchCompassCenterField?.GetValue(flightHud) is GameObject centerGo)
                    _sourceRollRect = centerGo.GetComponent<RectTransform>();
                else if (PitchCompassCenterField?.GetValue(flightHud) is Component centerComp)
                    _sourceRollRect = centerComp.GetComponent<RectTransform>();

                if (_sourceRollRect == null && _sourcePitchRect != null && _sourcePitchRect.parent != null)
                    _sourceRollRect = _sourcePitchRect.parent.GetComponent<RectTransform>();
            }
            catch
            {
                _sourcePitchImage = null;
                _sourcePitchRect = null;
                _sourceRollRect = null;
            }
        }

        private static void CopyRectTransform(RectTransform source, RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.anchoredPosition = source.anchoredPosition;
            target.sizeDelta = source.sizeDelta;
            target.localRotation = Quaternion.identity;
            target.localScale = Vector3.one;
        }

        private static void StretchCenter(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        private static void CenterOnParent(RectTransform rt)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        private static bool ColorsMatch(Color a, Color b) =>
            Mathf.Approximately(a.r, b.r)
            && Mathf.Approximately(a.g, b.g)
            && Mathf.Approximately(a.b, b.b)
            && Mathf.Approximately(a.a, b.a);

        private static FieldInfo? AccessToolsField(System.Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            return type.GetField(name, flags);
        }
    }
}
