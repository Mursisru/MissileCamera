using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>FLIR text effects during boot: char calibration, hex line drum, value drum.</summary>
    internal sealed class MissileCameraBootTextFx
    {
        private const int MaxSlots = 8;
        private const int BufCap = 192;
        private const string Hex = "0123456789ABCDEF";

        private struct CalSlot
        {
            internal int TextIndex;
            internal int CharIndex;
            internal float PhaseT;
            internal byte Stage;
            internal float StageDur;
            internal bool Active;
        }

        private Text[] _texts = System.Array.Empty<Text>();
        private string[] _targets = System.Array.Empty<string>();
        private char[][] _work = System.Array.Empty<char[]>();
        private bool[] _skipIndices = System.Array.Empty<bool>();
        private readonly CalSlot[] _slots = new CalSlot[MaxSlots];
        private readonly StringBuilder _sb = new StringBuilder(BufCap);
        private CanvasGroup? _flickerGroup;
        private float _nextValueTime;
        private float _valueInterval = 1f / 18f;
        private bool _bound;

        internal void Bind(RectTransform? flirRoot)
        {
            _bound = false;
            _texts = System.Array.Empty<Text>();
            _targets = System.Array.Empty<string>();
            _work = System.Array.Empty<char[]>();
            _flickerGroup = null;
            if (flirRoot == null)
                return;

            _flickerGroup = flirRoot.GetComponent<CanvasGroup>();
            if (_flickerGroup == null)
                _flickerGroup = flirRoot.gameObject.AddComponent<CanvasGroup>();

            Text[] found = flirRoot.GetComponentsInChildren<Text>(true);
            if (found == null || found.Length == 0)
                return;

            // Exclude FUEL/THR gauge labels + percent readouts — drums must not touch them.
            int keep = 0;
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != null
                    && !IsUnderFuelThrottleGauge(found[i].transform)
                    && !IsUnderGunshipHud(found[i].transform))
                    keep++;
            }

            if (keep <= 0)
                return;

            _texts = new Text[keep];
            _targets = new string[keep];
            _work = new char[keep][];
            _skipIndices = new bool[keep];
            int w = 0;
            for (int i = 0; i < found.Length; i++)
            {
                Text? text = found[i];
                if (text == null
                    || IsUnderFuelThrottleGauge(text.transform)
                    || IsUnderGunshipHud(text.transform))
                    continue;

                _texts[w] = text;
                string t = text.text ?? string.Empty;
                _targets[w] = t;
                _work[w] = new char[Mathf.Max(BufCap, t.Length + 8)];
                _skipIndices[w] = IsUnderFuelThrottleGauge(text.transform) || LooksPercent(t);
                w++;
            }

            for (int s = 0; s < MaxSlots; s++)
                _slots[s].Active = false;

            _bound = true;
        }

        internal void RecaptureTargets()
        {
            if (!_bound)
                return;
            for (int i = 0; i < _texts.Length; i++)
            {
                if (_texts[i] == null)
                    continue;
                string t = _texts[i].text ?? string.Empty;
                _targets[i] = t;
            }
        }

        internal void SetFlickerAlpha(float alpha)
        {
            if (_flickerGroup != null)
                _flickerGroup.alpha = Mathf.Clamp01(alpha);
        }

        internal void RestoreTargets()
        {
            if (!_bound)
                return;
            for (int i = 0; i < _texts.Length; i++)
            {
                if (_texts[i] != null)
                {
                    if (_skipIndices != null && i < _skipIndices.Length && _skipIndices[i])
                        continue;
                    _texts[i].text = _targets[i] ?? string.Empty;
                }
            }

            if (_flickerGroup != null)
                _flickerGroup.alpha = 1f;
        }

        internal void TickCharCalibration(float dt)
        {
            if (!_bound || dt <= 0f)
                return;

            for (int s = 0; s < MaxSlots; s++)
            {
                if (!_slots[s].Active)
                {
                    if (Random.value < 0.35f)
                        StartSlot(ref _slots[s]);
                    continue;
                }

                _slots[s].PhaseT += dt;
                if (_slots[s].PhaseT < _slots[s].StageDur)
                {
                    ApplySlotVisual(ref _slots[s]);
                    continue;
                }

                _slots[s].PhaseT = 0f;
                if (_slots[s].Stage == 0)
                {
                    _slots[s].Stage = 1;
                    _slots[s].StageDur = Random.Range(0.04f, 0.08f);
                    ApplySlotVisual(ref _slots[s]);
                }
                else if (_slots[s].Stage == 1)
                {
                    RestoreChar(_slots[s].TextIndex, _slots[s].CharIndex);
                    _slots[s].Active = false;
                }
            }
        }

        internal void TickHexLineDrum()
        {
            if (!_bound)
                return;

            for (int i = 0; i < _texts.Length; i++)
            {
                Text? text = _texts[i];
                if (text == null)
                    continue;

                if (_skipIndices != null && i < _skipIndices.Length && _skipIndices[i])
                {
                    // Keep gauge values live (drums must not overwrite them).
                    _targets[i] = text.text ?? string.Empty;
                    continue;
                }

                string target = _targets[i] ?? string.Empty;
                if (target.Length == 0)
                {
                    text.text = string.Empty;
                    continue;
                }

                if (ShouldPreserveStableTelemetry(target))
                {
                    // Keep stable telemetry exactly (no scrambling).
                    text.text = target;
                    continue;
                }

                // Preserve newlines / whitespace layout; scramble glyph cells only.
                EnsureWork(i, target);
                char[] buf = _work[i];
                target.CopyTo(0, buf, 0, target.Length);
                for (int c = 0; c < target.Length; c++)
                {
                    char ch = target[c];
                    if (ch == '\n' || ch == '\r' || ch == ' ' || ch == '\t')
                        continue;
                    if ((c & 3) == 3)
                        buf[c] = ' ';
                    else if ((c & 7) == 0)
                        buf[c] = '0';
                    else if ((c & 7) == 1)
                        buf[c] = 'x';
                    else
                        buf[c] = Hex[Random.Range(0, 16)];
                }

                _sb.Length = 0;
                _sb.Append(buf, 0, target.Length);
                text.text = _sb.ToString();
            }
        }

        internal void TickValueDrum(float unscaledTime)
        {
            if (!_bound)
                return;
            if (unscaledTime < _nextValueTime)
                return;
            _nextValueTime = unscaledTime + _valueInterval;

            for (int i = 0; i < _texts.Length; i++)
            {
                Text? text = _texts[i];
                if (text == null)
                    continue;

                if (_skipIndices != null && i < _skipIndices.Length && _skipIndices[i])
                {
                    // Keep gauge values live (drums must not overwrite them).
                    _targets[i] = text.text ?? string.Empty;
                    continue;
                }

                string target = _targets[i] ?? string.Empty;
                if (ShouldPreserveStableTelemetry(target))
                {
                    // Keep stable telemetry exactly (no scrambling).
                    text.text = target;
                    continue;
                }
                if (LooksNumericHeavy(target))
                    text.text = ScrambleNumericKeepLayout(target);
                else
                    text.text = target;
            }
        }

        private void StartSlot(ref CalSlot slot)
        {
            if (_texts.Length == 0)
                return;

            int ti = Random.Range(0, _texts.Length);
            if (_skipIndices != null && _skipIndices.Length == _texts.Length)
            {
                int attempts = 0;
                while (ti < _skipIndices.Length && _skipIndices[ti] && attempts < _texts.Length)
                {
                    ti = Random.Range(0, _texts.Length);
                    attempts++;
                }
                if (ti < _skipIndices.Length && _skipIndices[ti])
                    return;
            }
            string target = _targets[ti] ?? string.Empty;
            if (target.Length == 0)
                return;

            int ci = Random.Range(0, target.Length);
            if (char.IsWhiteSpace(target[ci]) || target[ci] == '\n' || target[ci] == '\r')
                return;

            slot.Active = true;
            slot.TextIndex = ti;
            slot.CharIndex = ci;
            slot.Stage = 0;
            slot.PhaseT = 0f;
            slot.StageDur = Random.Range(0.05f, 0.11f);
            EnsureWork(ti, target);
            ApplySlotVisual(ref slot);
        }

        private void EnsureWork(int textIndex, string target)
        {
            char[] buf = _work[textIndex];
            if (buf.Length < target.Length)
                buf = _work[textIndex] = new char[target.Length + 8];
            target.CopyTo(0, buf, 0, target.Length);
        }

        private void ApplySlotVisual(ref CalSlot slot)
        {
            string target = _targets[slot.TextIndex] ?? string.Empty;
            if (slot.CharIndex < 0 || slot.CharIndex >= target.Length)
                return;

            EnsureWork(slot.TextIndex, target);
            char[] buf = _work[slot.TextIndex];
            target.CopyTo(0, buf, 0, target.Length);

            if (slot.Stage == 0)
            {
                if (Random.value < 0.5f)
                    buf[slot.CharIndex] = '█';
                else
                    buf[slot.CharIndex] = PerlinGlyph(slot.TextIndex, slot.CharIndex, Time.unscaledTime);
            }
            else
            {
                buf[slot.CharIndex] = (char)Random.Range(33, 127);
            }

            _sb.Length = 0;
            _sb.Append(buf, 0, target.Length);
            if (_texts[slot.TextIndex] != null)
                _texts[slot.TextIndex].text = _sb.ToString();
        }

        private void RestoreChar(int textIndex, int charIndex)
        {
            string target = _targets[textIndex] ?? string.Empty;
            if (_texts[textIndex] == null || charIndex < 0 || charIndex >= target.Length)
                return;
            EnsureWork(textIndex, target);
            target.CopyTo(0, _work[textIndex], 0, target.Length);
            _sb.Length = 0;
            _sb.Append(_work[textIndex], 0, target.Length);
            _texts[textIndex].text = _sb.ToString();
        }

        private static char PerlinGlyph(int a, int b, float t)
        {
            float n = Mathf.PerlinNoise(a * 0.37f + t * 3.1f, b * 0.53f + t * 2.7f);
            int code = 33 + (int)(n * 93f);
            if (code > 126)
                code = 126;
            return (char)code;
        }

        private static bool LooksNumericHeavy(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            int digits = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (char.IsDigit(s[i]))
                    digits++;
            }

            return digits >= 2
                || s.IndexOf("SPD", System.StringComparison.Ordinal) >= 0
                || s.IndexOf("ALT", System.StringComparison.Ordinal) >= 0
                || s.IndexOf("MACH", System.StringComparison.Ordinal) >= 0
                || s.IndexOf("MAG", System.StringComparison.Ordinal) >= 0
                || s.IndexOf("RNG", System.StringComparison.Ordinal) >= 0
                || s.IndexOf("PIT", System.StringComparison.Ordinal) >= 0
                || s.IndexOf("HDG", System.StringComparison.Ordinal) >= 0
                || s.IndexOf("TTI", System.StringComparison.Ordinal) >= 0
                || s.IndexOf("G ", System.StringComparison.Ordinal) >= 0;
        }

        private static bool LooksPercent(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;

            s = s.Trim();
            if (s.Length < 2 || !s.EndsWith("%", System.StringComparison.Ordinal))
                return false;

            bool hasDigit = false;
            // Allow: spaces (already trimmed), digits, one dot, +/-, then '%'.
            for (int i = 0; i < s.Length - 1; i++)
            {
                char ch = s[i];
                if (char.IsDigit(ch))
                {
                    hasDigit = true;
                    continue;
                }

                if (ch == '.' || ch == '+' || ch == '-')
                    continue;

                return false;
            }

            return hasDigit;
        }

        private static bool ShouldPreserveStableTelemetry(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;

            return s.IndexOf("FUEL", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("THR", System.StringComparison.OrdinalIgnoreCase) >= 0
                || s.IndexOf("THROTTLE", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsUnderFuelThrottleGauge(Transform node)
        {
            Transform? t = node;
            while (t != null)
            {
                string n = t.name;
                if (n.StartsWith("FlirFuelGauge", System.StringComparison.Ordinal)
                    || n.StartsWith("FlirThrottleGauge", System.StringComparison.Ordinal))
                    return true;
                t = t.parent;
            }

            return false;
        }

        /// <summary>Gunship English labels stay intact during boot — only feed tiles animate.</summary>
        private static bool IsUnderGunshipHud(Transform node)
        {
            Transform? t = node;
            while (t != null)
            {
                if (t.name == "MissileCameraGunshipHud")
                    return true;
                t = t.parent;
            }

            return false;
        }

        /// <summary>
        /// Keep newlines and labels; only scramble digit / decimal runs so multiline panels stay multiline.
        /// </summary>
        private string ScrambleNumericKeepLayout(string template)
        {
            EnsureScratch(template.Length);
            template.CopyTo(0, _scratch, 0, template.Length);

            int i = 0;
            while (i < template.Length)
            {
                char ch = template[i];
                if (ch == '\n' || ch == '\r')
                {
                    i++;
                    continue;
                }

                if (char.IsDigit(ch) || ch == '.' || ch == '+' || ch == '-')
                {
                    int start = i;
                    bool hasDot = ch == '.';
                    i++;
                    while (i < template.Length)
                    {
                        char n = template[i];
                        if (char.IsDigit(n))
                        {
                            i++;
                            continue;
                        }

                        if (n == '.' && !hasDot)
                        {
                            hasDot = true;
                            i++;
                            continue;
                        }

                        break;
                    }

                    int len = i - start;
                    if (len <= 0)
                        continue;

                    // Leading sign alone — leave it.
                    if (len == 1 && (template[start] == '+' || template[start] == '-'))
                        continue;

                    for (int c = start; c < i; c++)
                    {
                        char t = template[c];
                        if (t == '.' || t == '+' || t == '-')
                            _scratch[c] = t;
                        else
                            _scratch[c] = (char)('0' + Random.Range(0, 10));
                    }

                    continue;
                }

                i++;
            }

            _sb.Length = 0;
            _sb.Append(_scratch, 0, template.Length);
            return _sb.ToString();
        }

        private char[] _scratch = new char[BufCap];

        private void EnsureScratch(int len)
        {
            if (_scratch.Length < len)
                _scratch = new char[len + 16];
        }
    }
}
