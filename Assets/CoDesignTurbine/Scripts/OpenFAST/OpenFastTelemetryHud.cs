using UnityEngine;

namespace CoDesignTurbine.OpenFAST
{
    public class OpenFastTelemetryHud : MonoBehaviour
    {
        public OpenFastPlaybackDriver playback;
        public bool showHud = true;
        public Rect panel = new Rect(16f, 285f, 330f, 220f);

        private GUIStyle labelStyle;

        private void Awake()
        {
            if (playback == null)
            {
                playback = FindAnyObjectByType<OpenFastPlaybackDriver>();
            }
        }

        private void OnGUI()
        {
            if (!showHud || playback == null)
            {
                return;
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14
                };
                labelStyle.normal.textColor = Color.white;
            }

            OpenFastPlaybackSample s = playback.LastSample;
            GUI.Box(panel, GUIContent.none);

            GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, panel.height - 16f));
            GUILayout.Label("OpenFAST playback", labelStyle);
            GUILayout.Label("time: " + playback.PlaybackTimeSeconds.ToString("0.00") + " s", labelStyle);
            GUILayout.Label("wind: " + s.windSpeed.ToString("0.00") + " m/s", labelStyle);
            GUILayout.Label("rotor: " + s.rotorSpeedRpm.ToString("0.00") + " rpm", labelStyle);
            GUILayout.Label("power: " + s.generatorPowerKw.ToString("0.00") + " kW", labelStyle);
            GUILayout.Label("torque: " + s.generatorTorque.ToString("0.00"), labelStyle);
            GUILayout.Label("pitch: " + s.bladePitch1Degrees.ToString("0.00") + " deg", labelStyle);
            GUILayout.Label("platform pitch: " + s.platformPitchDegrees.ToString("0.000") + " deg", labelStyle);
            GUILayout.Label("tower FA: " + s.towerTopForeAftMeters.ToString("0.000") + " m", labelStyle);
            GUILayout.EndArea();
        }
    }
}
