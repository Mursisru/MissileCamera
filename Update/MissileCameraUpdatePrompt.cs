using MissileCamera.Config;
using UnityEngine;

namespace MissileCamera
{
    /// <summary>
    /// One-shot EN update prompt (IMGUI). Shown after GitHub latest &gt; AppVersion; never on offline.
    /// </summary>
    internal sealed class MissileCameraUpdatePrompt : MonoBehaviour
    {
        private const float MinSecondsInGame = 2.5f;

        private static bool _offeredThisSession;
        private static MissileCameraUpdatePrompt? _instance;

        private bool _visible;
        private bool _dontShowAgain;
        private Rect _window = new Rect(0f, 0f, 420f, 168f);
        private GUIStyle? _boxStyle;
        private GUIStyle? _titleStyle;
        private GUIStyle? _bodyStyle;

        internal static void EnsureOn(GameObject host)
        {
            if (host == null || _instance != null)
                return;

            _instance = host.GetComponent<MissileCameraUpdatePrompt>();
            if (_instance == null)
                _instance = host.AddComponent<MissileCameraUpdatePrompt>();
        }

        private void Update()
        {
            if (_offeredThisSession || _visible)
                return;
            if (!MissileCameraUpdateChecker.IsCompleted || !MissileCameraUpdateChecker.IsOutdated)
                return;
            if (!MissileCameraBepInConfig.IsBound
                || !MissileCameraBepInConfig.CheckForUpdates.Value
                || MissileCameraBepInConfig.UpdatePromptDontShowAgain.Value)
                return;
            if (Time.unscaledTime < MinSecondsInGame)
                return;

            _offeredThisSession = true;
            _visible = true;
            _dontShowAgain = false;
            _window.x = (Screen.width - _window.width) * 0.5f;
            _window.y = (Screen.height - _window.height) * 0.28f;
        }

        private void OnGUI()
        {
            if (!_visible)
                return;

            EnsureStyles();
            _window = GUI.ModalWindow(
                0x4D435550, // MCUP
                _window,
                DrawWindow,
                "Missile Camera — Update Available",
                _boxStyle);
        }

        private void DrawWindow(int id)
        {
            string latest = MissileCameraUpdateChecker.LatestTag;
            if (string.IsNullOrEmpty(latest))
                latest = "newer";

            GUILayout.Space(6f);
            GUILayout.Label(
                "A newer full release is available on GitHub.\n"
                + "Installed: " + AppVersion.DisplayVersion
                + "    Latest: " + latest,
                _bodyStyle);

            GUILayout.Space(10f);
            _dontShowAgain = GUILayout.Toggle(_dontShowAgain, " Don't show again");

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Open download page", GUILayout.Height(28f)))
            {
                OpenReleasePage();
                Dismiss(saveDontShow: _dontShowAgain);
            }

            if (GUILayout.Button("Later", GUILayout.Width(90f), GUILayout.Height(28f)))
                Dismiss(saveDontShow: _dontShowAgain);

            GUILayout.EndHorizontal();
            GUI.DragWindow(new Rect(0f, 0f, 10000f, 22f));
        }

        private void OpenReleasePage()
        {
            string url = MissileCameraUpdateChecker.ReleaseUrl;
            if (string.IsNullOrEmpty(url))
                url = "https://github.com/Mursisru/MissileCamera/releases/latest";
            try
            {
                Application.OpenURL(url);
            }
            catch
            {
                // ignore
            }
        }

        private void Dismiss(bool saveDontShow)
        {
            _visible = false;
            if (saveDontShow && MissileCameraBepInConfig.IsBound)
            {
                try
                {
                    MissileCameraBepInConfig.UpdatePromptDontShowAgain.Value = true;
                }
                catch
                {
                    // ignore
                }
            }
        }

        private void EnsureStyles()
        {
            if (_boxStyle != null)
                return;

            _boxStyle = new GUIStyle(GUI.skin.window);
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14
            };
            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                wordWrap = true,
                fontSize = 13
            };
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
