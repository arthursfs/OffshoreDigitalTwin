using System.IO;
using UnityEngine;

namespace CoDesignTurbine.MOST
{
    public class MostFastComparisonHud : MonoBehaviour
    {
        public MostPlaybackDriver playback;
        public string comparisonCsvPath = "OpenFASTCases/VolturnUSUnity/most/outputs/sample_volturn_most_fast_comparison.csv";
        public string aeroModelLabel = "Aero: MOST aeroLoadsType=1 (BEM), Unity visual playback";
        public string windModelLabel = "Wind: TurbSim turbulent, match FAST with same mean wind and seed";
        public bool loadOnStart = true;
        public bool showHud = true;
        public Rect panel = new Rect(16f, 16f, 470f, 375f);

        private MostOutputSeries comparison;
        private GUIStyle labelStyle;

        private void Start()
        {
            if (playback == null)
            {
                playback = FindAnyObjectByType<MostPlaybackDriver>();
            }

            if (loadOnStart)
            {
                LoadComparisonCsv();
            }
        }

        public void LoadComparisonCsv()
        {
            string path = ResolveProjectPath(comparisonCsvPath);
            if (File.Exists(path))
            {
                comparison = MostCsvOutputReader.Read(path, "time_s");
            }
        }

        private void OnGUI()
        {
            if (!showHud)
            {
                return;
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    wordWrap = true
                };
                labelStyle.normal.textColor = Color.white;
            }

            float time = playback != null ? playback.PlaybackTimeSeconds : 0f;
            GUI.Box(panel, GUIContent.none);
            GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, panel.height - 16f));
            GUILayout.Label("VolturnUS MOST vs FAST comparison", labelStyle);
            GUILayout.Label("Paper pages 6-7 targets: pitch, power, blade-root axial force/moment", labelStyle);
            GUILayout.Label(aeroModelLabel, labelStyle);
            GUILayout.Label(windModelLabel, labelStyle);

            if (comparison != null)
            {
                GUILayout.Space(5f);
                GUILayout.Label("t = " + time.ToString("0.00") + " s", labelStyle);
                DrawPair("Pitch deg", "most_pitch_deg", "fast_pitch_deg", time);
                DrawPair("Power MW", "most_power_mw", "fast_power_mw", time);
                DrawPair("Root axial force kN", "most_root_axial_force_kn", "fast_root_axial_force_kn", time);
                DrawPair("Root axial moment MNm", "most_root_axial_moment_mnm", "fast_root_axial_moment_mnm", time);
            }
            else
            {
                GUILayout.Label("Comparison CSV not loaded.", labelStyle);
            }

            GUILayout.Space(5f);
            GUILayout.Label("Table 4 checkpoints:", labelStyle);
            GUILayout.Label("8 m/s pitch mean/std MOST 2.738/1.142, FAST 2.794/1.129", labelStyle);
            GUILayout.Label("8 m/s power mean/std MOST 7.383/2.905, FAST 7.726/2.876 MW", labelStyle);
            GUILayout.Label("14 m/s pitch mean/std MOST 2.112/1.060, FAST 1.942/1.046", labelStyle);
            GUILayout.Label("14 m/s power mean/std MOST 14.38/1.848, FAST 14.40/1.798 MW", labelStyle);
            GUILayout.EndArea();
        }

        private void DrawPair(string label, string mostChannel, string fastChannel, float time)
        {
            float most = comparison.SampleChannel(mostChannel, time, 0f);
            float fast = comparison.SampleChannel(fastChannel, time, 0f);
            float error = fast != 0f ? (most - fast) / Mathf.Abs(fast) * 100f : 0f;
            GUILayout.Label(label + ": MOST " + most.ToString("0.###") + " | FAST " + fast.ToString("0.###") + " | err " + error.ToString("0.0") + "%", labelStyle);
        }

        private static string ResolveProjectPath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            DirectoryInfo assetsDirectory = new DirectoryInfo(Application.dataPath);
            string projectRoot = assetsDirectory.Parent != null ? assetsDirectory.Parent.FullName : Application.dataPath;
            return Path.GetFullPath(Path.Combine(projectRoot, path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
