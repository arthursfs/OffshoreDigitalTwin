using UnityEngine;

namespace CoDesignTurbine.MOST
{
    public class MostTelemetryHud : MonoBehaviour
    {
        public MostPlaybackDriver playback;
        public bool showHud = true;
        public Rect panel = new Rect(16f, 16f, 330f, 220f);

        private GUIStyle labelStyle;

        private void Awake()
        {
            if (playback == null)
            {
                playback = FindAnyObjectByType<MostPlaybackDriver>();
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

            MostPlaybackSample s = playback.LastSample;
            GUI.Box(panel, GUIContent.none);

            GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, panel.height - 16f));
            GUILayout.Label("MOST / WEC-Sim playback", labelStyle);
            GUILayout.Label("time: " + playback.PlaybackTimeSeconds.ToString("0.00") + " s", labelStyle);
            GUILayout.Label("surge: " + s.surgeMeters.ToString("0.000") + " m", labelStyle);
            GUILayout.Label("heave: " + s.heaveMeters.ToString("0.000") + " m", labelStyle);
            GUILayout.Label("pitch: " + s.pitchDegrees.ToString("0.000") + " deg", labelStyle);
            GUILayout.Label("rotor: " + s.rotorSpeedRpm.ToString("0.00") + " rpm", labelStyle);
            GUILayout.Label("blade pitch: " + s.bladePitchDegrees.ToString("0.00") + " deg", labelStyle);
            GUILayout.Label("turbine power: " + s.turbinePowerMw.ToString("0.000") + " MW", labelStyle);
            GUILayout.Label("PTO power: " + s.ptoPowerMw.ToString("0.000") + " MW", labelStyle);
            GUILayout.EndArea();
        }
    }
}
