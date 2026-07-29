using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>Phase 0: shuffled feed tiles recursively subdivide (x4 per step) and keep swapping.</summary>
    internal sealed class MissileCameraBootTilePuzzle
    {
        private const int BaseRows = 4;
        private const int SubdivideSteps = 2;
        private const float StartScale = 1f;
        private static readonly Color FallbackTint = new Color(0.15f, 0.55f, 0.28f, 1f);

        private readonly GameObject _rootGo;
        private readonly Texture? _feedTexture;
        private readonly float _screenH;
        private readonly int _baseCols;
        private readonly int _baseRows;
        private RectTransform[] _tileRts;
        private Vector2[] _endPos;
        private int[] _order;
        private int _currentLevel;

        private MissileCameraBootTilePuzzle(
            GameObject rootGo,
            Texture? feedTexture,
            float screenH,
            int baseCols,
            int baseRows,
            RectTransform[] tileRts,
            Vector2[] endPos,
            int[] order,
            int currentLevel)
        {
            _rootGo = rootGo;
            _feedTexture = feedTexture;
            _screenH = screenH;
            _baseCols = baseCols;
            _baseRows = baseRows;
            _tileRts = tileRts;
            _endPos = endPos;
            _order = order;
            _currentLevel = currentLevel;
        }

        internal static MissileCameraBootTilePuzzle? Create(
            RectTransform parent,
            Texture? feedTexture,
            float screenW,
            float screenH)
        {
            if (parent == null || screenW < 16f || screenH < 16f)
                return null;

            float cell = screenH / BaseRows;
            int cols = Mathf.Max(1, Mathf.CeilToInt(screenW / cell));
            int rows = BaseRows;

            var rootGo = new GameObject("BootTilePuzzle", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            RectTransform root = rootGo.GetComponent<RectTransform>();
            Stretch(root);
            root.SetAsLastSibling();

            RectTransform[] tileRts = System.Array.Empty<RectTransform>();
            Vector2[] endPos = System.Array.Empty<Vector2>();
            int[] order = System.Array.Empty<int>();
            BuildTiles(
                root,
                feedTexture,
                screenH,
                cols,
                rows,
                0,
                ref tileRts,
                ref endPos,
                ref order);

            MfdLog.Info($"boot tiles created cols={cols} rows={rows} tex={(feedTexture != null ? "ok" : "fallback")}");
            return new MissileCameraBootTilePuzzle(
                rootGo,
                feedTexture,
                screenH,
                cols,
                rows,
                tileRts,
                endPos,
                order,
                0);
        }

        internal void Tick(float t)
        {
            t = Mathf.Clamp01(t);
            int desiredLevel = Mathf.Clamp(Mathf.FloorToInt(t * (SubdivideSteps + 1)), 0, SubdivideSteps);
            if (desiredLevel != _currentLevel)
            {
                Rebuild(desiredLevel);
            }

            if (t >= 1f)
            {
                Vector3 doneScale = Vector3.one;
                for (int i = 0; i < _tileRts.Length; i++)
                {
                    RectTransform? rt = _tileRts[i];
                    if (rt == null)
                        continue;
                    rt.anchoredPosition = _endPos[i];
                    rt.localScale = doneScale;
                }

                return;
            }

            // Instant random swaps between tile positions (no interpolation) on each subdivision level.
            int swaps = Mathf.Clamp(_order.Length / 10, 8, 96);
            for (int k = 0; k < swaps; k++)
            {
                int a = Random.Range(0, _order.Length);
                int b = Random.Range(0, _order.Length);
                if (a == b)
                    continue;
                int tmp = _order[a];
                _order[a] = _order[b];
                _order[b] = tmp;
            }

            Vector3 s = Vector3.one;
            for (int i = 0; i < _tileRts.Length; i++)
            {
                RectTransform? rt = _tileRts[i];
                if (rt == null)
                    continue;
                rt.anchoredPosition = _endPos[_order[i]];
                rt.localScale = s;
            }
        }

        private void Rebuild(int level)
        {
            if (_rootGo == null)
                return;

            RectTransform root = _rootGo.GetComponent<RectTransform>();
            BuildTiles(
                root,
                _feedTexture,
                _screenH,
                _baseCols,
                _baseRows,
                level,
                ref _tileRts,
                ref _endPos,
                ref _order);
            _currentLevel = level;
        }

        private static void BuildTiles(
            RectTransform root,
            Texture? feedTexture,
            float screenH,
            int baseCols,
            int baseRows,
            int level,
            ref RectTransform[] tileRts,
            ref Vector2[] endPos,
            ref int[] order)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(root.GetChild(i).gameObject);
            }

            int rows = baseRows << level;
            int cols = baseCols << level;
            int count = cols * rows;
            float cell = screenH / rows;
            float originX = -cols * cell * 0.5f + cell * 0.5f;
            float originY = rows * cell * 0.5f - cell * 0.5f;

            tileRts = new RectTransform[count];
            endPos = new Vector2[count];
            order = new int[count];
            for (int i = 0; i < count; i++)
                order[i] = i;
            Shuffle(order);

            for (int i = 0; i < count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                float u = col / (float)cols;
                float v = 1f - (row + 1) / (float)rows;
                float uw = 1f / cols;
                float uh = 1f / rows;
                // Slight UV inset removes bilinear bleed seams that look like borders.
                float epsU = uw * 0.02f;
                float epsV = uh * 0.02f;

                var go = new GameObject("BootTile" + i, typeof(RectTransform));
                go.transform.SetParent(root, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(cell, cell);

                Vector2 correct = new Vector2(originX + col * cell, originY - row * cell);
                endPos[i] = correct;

                int from = order[i];
                int fromCol = from % cols;
                int fromRow = from / cols;
                rt.anchoredPosition = new Vector2(originX + fromCol * cell, originY - fromRow * cell);
                rt.localScale = new Vector3(StartScale, StartScale, 1f);

                var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(RawImage));
                fillGo.transform.SetParent(rt, false);
                RectTransform fillRt = fillGo.GetComponent<RectTransform>();
                Stretch(fillRt);
                RawImage img = fillGo.GetComponent<RawImage>();
                img.raycastTarget = false;
                if (feedTexture != null)
                {
                    img.texture = feedTexture;
                    img.uvRect = new Rect(
                        u + epsU,
                        v + epsV,
                        Mathf.Max(0f, uw - epsU * 2f),
                        Mathf.Max(0f, uh - epsV * 2f));
                    img.color = Color.white;
                }
                else
                {
                    img.texture = Texture2D.whiteTexture;
                    img.color = FallbackTint;
                }

                tileRts[i] = rt;
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
