using UnityEngine;

namespace CoDesignTurbine
{
    public class WindTurbineTelemetryHud : MonoBehaviour
    {
        public WindTurbineSimulation simulation;
        public bool showHud = true;
        public Rect panel = new Rect(16f, 16f, 300f, 255f);

        private GUIStyle labelStyle;

        private void Awake()
        {
            if (simulation == null)
            {
                simulation = FindAnyObjectByType<WindTurbineSimulation>();
            }
        }

        private void OnGUI()
        {
            if (!showHud || simulation == null)
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

            WindTurbineSample s = simulation.LastSample;
            GUI.Box(panel, GUIContent.none);

            GUILayout.BeginArea(new Rect(panel.x + 12f, panel.y + 8f, panel.width - 24f, panel.height - 16f));
            GUILayout.Label($"time: {simulation.SimulationTimeSeconds,7:0.0} s", labelStyle);
            GUILayout.Label($"wind: {s.windSpeed,7:0.00} m/s", labelStyle);
            GUILayout.Label($"rotor: {s.rotorSpeedRpm,7:0.00} rpm", labelStyle);
            GUILayout.Label($"pitch: {s.bladePitchDegrees,7:0.00} deg", labelStyle);
            GUILayout.Label($"torque: {s.generatorTorqueNm / 1000f,7:0.0} kNm", labelStyle);
            GUILayout.Label($"power: {s.electricalPower / 1000f,7:0.0} kW", labelStyle);
            GUILayout.Label($"energy: {s.energyKWh,7:0.000} kWh", labelStyle);
            GUILayout.Label($"aero: {s.aerodynamicModelName}", labelStyle);
            GUILayout.Label($"Cp: {s.powerCoefficient,7:0.000}", labelStyle);
            GUILayout.Label($"tilt: {s.foreAftAngleDegrees,7:0.000} deg", labelStyle);
            GUILayout.Label($"margin: {s.stabilityMargin,7:0.000}", labelStyle);
            GUILayout.Label($"cost: {s.stabilityCost,7:0.00}", labelStyle);
            GUILayout.EndArea();
        }
    }
}
