using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>FLIR text effects during boot: char calibration, hex line drum, value drum.</summary>
    internal sealed class MissileCameraBootTextFx
    {
        private const int MaxSlots = 8;
        private const int BufCap = 96;
        private const string Hex = "0123456789ABCDEF";

        private struct CalSlot
        {
            internal int TextIndex;
            internal int CharIndex;
            internal float PhaseT;
            internal byte Stage; // 0 fill/noise, 1 random, 2 done wait
            internal float StageDur;
            internal bool Active;
        }

        private Text[] _texts = System.Array.Empty<Text>();
        private string[] _targets = System.Array.Empty<string>();
        private char[][] _work = System.Array.Empty<char[]>();
        private readonly CalSlot[] _slots = new CalSlot[MaxSlots];
        private readonly StringBuilder _sb = new StringBuilder(BufCap);
        private readonly char[] _lineBuf = new char[BufCap];
        private CanvasGroup? _flickerGroup;
        private float _nextValueTime;
        private float _valueInterval = 1f / 20f;
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

            _texts = found;
            _targets = new string[_texts.Length];
            _work = new char[_texts.Length][];
            for (int i = 0; i < _texts.Length; i++)
            {
                string t = _texts[i] != null ? _texts[i].text ?? string.Empty : string.Empty;
                _targets[i] = t;
                _work[i] = new char[Mathf.Max(BufCap, t.Length + 8)];
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
                    _texts[i].text = _targets[i] ?? string.Empty;
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
                    // restore char then free slot
                    RestoreChar(_slots[s].TextIndex, _slots[s].CharIndex);
                    _slots[s].Active = false;
                }
            }

            // Keep non-active chars at target (in case drum wasn't running).
            FlushWorkToTexts();
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

                int len = Mathf.Clamp((_targets[i] ?? string.Empty).Length, 8, BufCap - 1);
                for (int c = 0; c < len; c++)
                {
                    if ((c & 3) == 3)
                        _lineBuf[c] = ' ';
                    else if ((c & 7) == 0)
                        _lineBuf[c] = '0';
                    else if ((c & 7) == 1)
                        _lineBuf[c] = 'x';
                    else
                        _lineBuf[c] = Hex[Random.Range(0, 16)];
                }

                // short hash tail
                int hashStart = Mathf.Max(0, len - 8);
                for (int c = hashStart; c < len; c++)
                    _lineBuf[c] = Hex[Random.Range(0, 16)];

                _sb.Length = 0;
                _sb.Append(_lineBuf, 0, len);
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

                string target = _targets[i] ?? string.Empty;
                if (LooksNumericHeavy(target))
                    text.text = BuildChaoticTelemetryLine(target);
                else
                    text.text = target;
            }
        }

        private void StartSlot(ref CalSlot slot)
        {
            if (_texts.Length == 0)
                return;

            int ti = Random.Range(0, _texts.Length);
            string target = _targets[ti] ?? string.Empty;
            if (target.Length == 0)
                return;

            int ci = Random.Range(0, target.Length);
            if (char.IsWhiteSpace(target[ci]))
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

        private void FlushWorkToTexts()
        {
            // no-op: active slots write themselves; inactive keep last restore
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

            return digits >= 2 || s.IndexOf("SPD", System.StringComparison.Ordinal) >= 0
                || s.IndexOf("ALT", System.StringComparison.Ordinal) >= 0
                || s.IndexOf("MACH", System.StringComparison.Ordinal) >= 0
                || s.IndexOf("MAG", System.StringComparison.Ordinal) >= 0
                || s.IndexOf("RNG", System.StringComparison.Ordinal) >= 0
                || s.IndexOf("G ", System.StringComparison.Ordinal) >= 0;
        }

        private string BuildChaoticTelemetryLine(string template)
        {
            float spd = Random.Range(120f, 980f);
            float alt = Random.Range(50f, 12000f);
            float mach = Random.Range(0.2f, 2.8f);
            float g = Random.Range(0.2f, 9.5f);
            float rng = Random.Range(200f, 45000f);
            float mag = Random.Range(1f, 48f);

            if (template.IndexOf("MACH", System.StringComparison.Ordinal) >= 0)
            {
                return "MACH " + mach.ToString("F2", CultureInfo.InvariantCulture)
                    + "  FUEL " + Random.Range(5, 99).ToString(CultureInfo.InvariantCulture) + "%";
            }

            if (template.IndexOf("MAG", System.StringComparison.Ordinal) >= 0)
            {
                return "MAG x" + mag.ToString("F1", CultureInfo.InvariantCulture)
                    + "\nFOV " + Random.Range(2, 90).ToString(CultureInfo.InvariantCulture) + "°";
            }

            if (template.IndexOf("SPD", System.StringComparison.Ordinal) >= 0
                || template.IndexOf("ALT", System.StringComparison.Ordinal) >= 0)
            {
                return "SPD " + spd.ToString("F0", CultureInfo.InvariantCulture)
                    + "  HDG " + Random.Range(0, 359).ToString(CultureInfo.InvariantCulture) + "°T"
                    + "\nALT " + alt.ToString("F0", CultureInfo.InvariantCulture)
                    + "  G " + g.ToString("F1", CultureInfo.InvariantCulture);
            }

            if (template.IndexOf("SLT", System.StringComparison.Ordinal) >= 0
                || template.IndexOf("RNG", System.StringComparison.Ordinal) >= 0
                || template.IndexOf("LRF", System.StringComparison.Ordinal) >= 0)
            {
                return "SLT " + rng.ToString("F0", CultureInfo.InvariantCulture)
                    + "  CLOS " + Random.Range(50, 900).ToString(CultureInfo.InvariantCulture)
                    + "\nTTI " + Random.Range(0.2f, 40f).ToString("F1", CultureInfo.InvariantCulture) + "s"
                    + "  LRF " + rng.ToString("F0", CultureInfo.InvariantCulture) + "m";
            }

            return "V " + spd.ToString("F0", CultureInfo.InvariantCulture)
                + " A " + alt.ToString("F0", CultureInfo.InvariantCulture)
                + " M " + mach.ToString("F2", CultureInfo.InvariantCulture);
        }
    }
}
