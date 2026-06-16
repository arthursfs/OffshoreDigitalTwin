using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace CoDesignTurbine
{
    public class CsvTelemetryLogger : MonoBehaviour
    {
        public WindTurbineSimulation simulation;
        public bool logOnPlay;
        [Min(0.01f)] public float samplePeriodSeconds = 0.25f;
        public string fileName = "wind_turbine_telemetry.csv";

        private float nextSampleTime;
        private StringBuilder buffer;

        private void Awake()
        {
            if (simulation == null)
            {
                simulation = FindAnyObjectByType<WindTurbineSimulation>();
            }
        }

        private void OnEnable()
        {
            buffer = new StringBuilder();
            buffer.AppendLine("time_s,wind_mps,rotor_rpm,pitch_deg,torque_nm,power_w,energy_kwh,cp,ct,thrust_n,tilt_deg,tilt_rate_deg_s,stability_margin,stability_cost,overspeed_s");
            nextSampleTime = 0f;
        }

        private void Update()
        {
            if (!logOnPlay || simulation == null)
            {
                return;
            }

            float time = simulation.SimulationTimeSeconds;
            if (time < nextSampleTime)
            {
                return;
            }

            AppendSample(simulation.LastSample);
            nextSampleTime = time + samplePeriodSeconds;
        }

        private void OnDisable()
        {
            if (buffer == null || buffer.Length == 0)
            {
                return;
            }

            string path = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllText(path, buffer.ToString());
            Debug.Log($"Telemetry written to {path}");
        }

        public void AppendSample(WindTurbineSample s)
        {
            if (buffer == null)
            {
                buffer = new StringBuilder();
            }
            CultureInfo culture = CultureInfo.InvariantCulture;

            buffer.AppendFormat(
                culture,
                "{0:F3},{1:F4},{2:F4},{3:F4},{4:F4},{5:F4},{6:F6},{7:F5},{8:F5},{9:F4},{10:F5},{11:F5},{12:F5},{13:F5},{14:F5}\n",
                s.timeSeconds,
                s.windSpeed,
                s.rotorSpeedRpm,
                s.bladePitchDegrees,
                s.generatorTorqueNm,
                s.electricalPower,
                s.energyKWh,
                s.powerCoefficient,
                s.thrustCoefficient,
                s.thrustNewtons,
                s.foreAftAngleDegrees,
                s.foreAftRateDegreesPerSecond,
                s.stabilityMargin,
                s.stabilityCost,
                s.overspeedSeconds);
        }
    }
}
