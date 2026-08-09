using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace MissileCamera
{
    /// <summary>Top-left callsign block — large COD-style type.</summary>
    internal sealed class GunshipTelemetry
    {
        private static readonly StringBuilder Sb = new StringBuilder(96);

        private readonly Image _signal;
        private readonly Text _callsign;
        private readonly Text _status;
        private readonly Text _coords;
        private readonly Text _flight;
        private string _lastCall = "";
        private string _lastStatus = "";
        private string _lastCoords = "";
        private string _lastFlight = "";

        private GunshipTelemetry(Image signal, Text callsign, Text status, Text coords, Text flight)
        {
            _signal = signal;
            _callsign = callsign;
            _status = status;
            _coords = coords;
            _flight = flight;
        }

        internal static GunshipTelemetry Create(RectTransform parent)
        {
            Image signal = GunshipChrome.CreateImage(parent, "GunshipSignal", GunshipChrome.White);
            Text callsign = GunshipChrome.CreateText(parent, "GunshipCallsign", TextAnchor.UpperLeft, GunshipChrome.FontCallsign);
            Text status = GunshipChrome.CreateText(parent, "GunshipStatus", TextAnchor.UpperLeft, GunshipChrome.FontStatus);
            Text coords = GunshipChrome.CreateText(parent, "GunshipCoords", TextAnchor.UpperLeft, GunshipChrome.FontBody);
            Text flight = GunshipChrome.CreateText(parent, "GunshipFlight", TextAnchor.UpperLeft, GunshipChrome.FontBody);
            status.color = GunshipChrome.WhiteDim;
            status.fontStyle = FontStyle.Normal;
            coords.color = GunshipChrome.White;
            coords.fontStyle = FontStyle.Normal;
            flight.color = GunshipChrome.White;
            flight.fontStyle = FontStyle.Normal;
            return new GunshipTelemetry(signal, callsign, status, coords, flight);
        }

        internal void Place(MissileCameraPanelMetrics panel)
        {
            float pad = Mathf.Max(panel.HorizontalInset, 24f) + 36f;
            float top = -Mathf.Max(panel.VerticalInset, 18f) - 16f;
            GunshipChrome.Place(_signal.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(pad, top - 6f), new Vector2(22f, 16f));
            GunshipChrome.Place(_callsign.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(pad + 30f, top), new Vector2(560f, 42f));
            GunshipChrome.Place(_status.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(pad + 30f, top - 40f), new Vector2(320f, 22f));
            GunshipChrome.Place(_coords.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(pad, top - 72f), new Vector2(480f, 48f));
            GunshipChrome.Place(_flight.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(pad, top - 128f), new Vector2(520f, 48f));
        }

        internal void Update(MissileCameraHudSnapshot snapshot)
        {
            string call = string.IsNullOrEmpty(snapshot.MissileName) ? "MSL-1" : snapshot.MissileName.ToUpperInvariant();
            if (call != _lastCall)
            {
                _lastCall = call;
                _callsign.text = call;
            }

            string status = snapshot.HasFeed ? "ON STATION" : "NO SIGNAL";
            if (status != _lastStatus)
            {
                _lastStatus = status;
                _status.text = status;
                _signal.color = snapshot.HasFeed ? GunshipChrome.White : GunshipChrome.WhiteSoft;
            }

            string coords = FormatCoords(snapshot);
            if (coords != _lastCoords)
            {
                _lastCoords = coords;
                _coords.text = coords;
            }

            string flight = FormatFlight(snapshot);
            if (flight != _lastFlight)
            {
                _lastFlight = flight;
                _flight.text = flight;
            }
        }

        private static string FormatCoords(MissileCameraHudSnapshot snapshot)
        {
            GlobalPosition gp = default;
            try
            {
                Missile? m = MissileCameraFeedController.TryGetFollowedMissile();
                if (m != null)
                    gp = m.transform.GlobalPosition();
            }
            catch { /* ignore */ }

            Sb.Length = 0;
            AppendDms(Sb, gp.x, northSouth: true);
            Sb.Append('\n');
            AppendDms(Sb, gp.z, northSouth: false);
            return Sb.ToString();
        }

        private static void AppendDms(StringBuilder sb, float meters, bool northSouth)
        {
            float degAbs = Mathf.Abs(meters) * 0.00001f;
            int d = (int)degAbs;
            float rem = (degAbs - d) * 60f;
            int m = (int)rem;
            float s = (rem - m) * 60f;
            char hemi = northSouth
                ? (meters >= 0f ? 'N' : 'S')
                : (meters >= 0f ? 'E' : 'W');
            sb.Append(d.ToString("000", CultureInfo.InvariantCulture))
                .Append('°').Append(' ')
                .Append(m.ToString("00", CultureInfo.InvariantCulture))
                .Append('\'').Append(' ')
                .Append(s.ToString("00.000", CultureInfo.InvariantCulture))
                .Append('"').Append(' ').Append(hemi);
        }

        private static string FormatFlight(MissileCameraHudSnapshot snapshot)
        {
            Sb.Length = 0;
            Sb.Append("SPD  ").Append(StripPrefix(snapshot.SpeedText, "S:"));
            Sb.Append("   HDG  ").Append(snapshot.MissileHeadingDeg.ToString("000", CultureInfo.InvariantCulture)).Append(" T");
            Sb.Append("\nALT  ").Append(StripPrefix(snapshot.AltitudeText, "A:"));
            return Sb.ToString();
        }

        private static string StripPrefix(string text, string prefix)
        {
            if (string.IsNullOrEmpty(text))
                return "---";
            if (text.StartsWith(prefix))
                return text.Substring(prefix.Length);
            return text;
        }
    }
}
