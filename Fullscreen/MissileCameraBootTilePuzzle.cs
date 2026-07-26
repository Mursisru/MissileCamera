using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>Phase 0: shuffled feed tiles assemble into a full frame (~6 rows of squares).</summary>
    internal sealed class MissileCameraBootTilePuzzle
    {
        private const int Rows = 6;
        private const float StartScale = 0.62f;
        private static readonly Color FallbackTint = new Color(0.15f, 0.55f, 0.28f, 1f);
        private static readonly Color BorderTint = new Color(0.2f, 1f, 0.45f, 0.85f);

        private readonly GameObject _rootGo;
        private readonly RectTransform[] _tileRts;
        private readonly Vector2[] _startPos;
        private readonly Vector2[] _endPos;

        private MissileCameraBootTilePuzzle(
            GameObject rootGo,
            RectTransform[] tileRts,
            Vector2[] startPos,
            Vector2[] endPos)
        {
            _rootGo = rootGo;
            _tileRts = tileRts;
            _startPos = startPos;
            _endPos = endPos;
        }

        internal static MissileCameraBootTilePuzzle? Create(
            RectTransform parent,
            Texture? feedTexture,
            float screenW,
            float screenH)
        {
            if (parent == null || screenW < 16f || screenH < 16f)
                return null;

            float cell = screenH / Rows;
            int cols = Mathf.Max(1, Mathf.CeilToInt(screenW / cell));
            // Keep cells square; may slightly overflow width — OK for assemble look.
            int count = cols * Rows;

            var rootGo = new GameObject("BootTilePuzzle", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();
            Stretch(root);
            root.SetAsLastSibling();

            var tileRts = new RectTransform[count];
            var endPos = new Vector2[count];
            var startPos = new Vector2[count];
            var order = new int[count];
            for (int i = 0; i < count; i++)
                order[i] = i;
            Shuffle(order);

            float originX = -cols * cell * 0.5f + cell * 0.5f;
            float originY = Rows * cell * 0.5f - cell * 0.5f;

            for (int i = 0; i < count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                float u = col / (float)cols;
                float v = 1f - (row + 1) / (float)Rows;
                float uw = 1f / cols;
                float uh = 1f / Rows;

                var go = new GameObject("BootTile" + i, typeof(RectTransform));
                go.transform.SetParent(root, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(cell - 2f, cell - 2f);

                Vector2 correct = new Vector2(originX + col * cell, originY - row * cell);
                endPos[i] = correct;

                int from = order[i];
                int fromCol = from % cols;
                int fromRow = from / cols;
                startPos[i] = new Vector2(originX + fromCol * cell, originY - fromRow * cell);

                rt.anchoredPosition = startPos[i];
                rt.localScale = new Vector3(StartScale, StartScale, 1f);

                // Border so tiles read as squares even on dark feed.
                var borderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
                borderGo.transform.SetParent(rt, false);
                RectTransform borderRt = borderGo.GetComponent<RectTransform>();
                Stretch(borderRt);
                Image border = borderGo.GetComponent<Image>();
                border.color = BorderTint;
                border.raycastTarget = false;

                var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(RawImage));
                fillGo.transform.SetParent(rt, false);
                RectTransform fillRt = fillGo.GetComponent<RectTransform>();
                Stretch(fillRt);
                fillRt.offsetMin = new Vector2(2f, 2f);
                fillRt.offsetMax = new Vector2(-2f, -2f);
                RawImage img = fillGo.GetComponent<RawImage>();
                img.raycastTarget = false;
                if (feedTexture != null)
                {
                    img.texture = feedTexture;
                    img.uvRect = new Rect(u, v, uw, uh);
                    img.color = Color.white;
                }
                else
                {
                    img.texture = Texture2D.whiteTexture;
                    img.color = FallbackTint;
                }

                tileRts[i] = rt;
            }

            MfdLog.Info($"boot tiles created cols={cols} rows={Rows} cell={cell:F0} tex={(feedTexture != null ? "ok" : "fallback")}");
            return new MissileCameraBootTilePuzzle(rootGo, tileRts, startPos, endPos);
        }

        internal void Tick(float t)
        {
            t = Mathf.Clamp01(t);
            float e = EaseOutCubic(t);
            float scale = Mathf.Lerp(StartScale, 1f, e);
            Vector3 s = new Vector3(scale, scale, 1f);
            for (int i = 0; i < _tileRts.Length; i++)
            {
                RectTransform? rt = _tileRts[i];
                if (rt == null)
                    continue;
                rt.anchoredPosition = Vector2.Lerp(_startPos[i], _endPos[i], e);
                rt.localScale = s;
            }
        }

        internal void Destroy()
        {
            if (_rootGo != null)
                Object.Destroy(_rootGo);
        }

        private static void Shuffle(int[] a)
        {
            for (int i = a.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                int tmp = a[i];
                a[i] = a[j];
                a[j] = tmp;
            }
        }

        private static float EaseOutCubic(float x)
        {
            if (x <= 0f)
                return 0f;
            if (x >= 1f)
                return 1f;
            float u = 1f - x;
            return 1f - u * u * u;
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
