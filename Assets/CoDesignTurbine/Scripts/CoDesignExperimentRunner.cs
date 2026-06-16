using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace CoDesignTurbine
{
    public class CoDesignExperimentRunner : MonoBehaviour
    {
        public WindTurbineSimulation sourceSimulation;

        [Header("Sweep")]
        public CoDesignSweepRange rotorRadiusMeters = new CoDesignSweepRange { min = 35f, max = 55f, steps = 5 };
        public CoDesignSweepRange rotorInertiaScale = new CoDesignSweepRange { min = 0.75f, max = 1.35f, steps = 4 };
        public CoDesignSweepRange stabilityPitchGain = new CoDesignSweepRange { min = 2f, max = 14f, steps = 5 };
        public CoDesignSweepRange pitchKp = new CoDesignSweepRange { min = 8f, max = 20f, steps = 4 };

        [Header("Evaluation")]
        [Min(1f)] public float durationSeconds = 300f;
        [Min(0.005f)] public float timeStepSeconds = 0.05f;
        [Min(0f)] public float stabilityCostWeight = 0.08f;
        [Min(0f)] public float tiltViolationWeight = 25f;

        [Header("Output")]
        public bool runOnStart;
        public KeyCode runKey = KeyCode.F9;
        public string fileName = "co_design_sweep.csv";

        public IReadOnlyList<CoDesignResult> Results => results;

        private readonly List<CoDesignResult> results = new List<CoDesignResult>();

        private void Awake()
        {
            if (sourceSimulation == null)
            {
                sourceSimulation = FindAnyObjectByType<WindTurbineSimulation>();
            }
        }

        private void Start()
        {
            if (runOnStart)
            {
                RunSweep();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(runKey))
            {
                RunSweep();
            }
        }

        public void RunSweep()
        {
            if (sourceSimulation == null)
            {
                Debug.LogWarning("No WindTurbineSimulation assigned for co-design sweep.");
                return;
            }

            results.Clear();
            WindTurbinePlantParameters basePlant = sourceSimulation.plant.Clone();
            WindTurbineControllerSettings baseController = sourceSimulation.controller.Clone();
            WindProfileSettings baseWind = sourceSimulation.wind.Clone();
            float baseInertia = Mathf.Max(1f, basePlant.rotorInertia);

            for (int r = 0; r < rotorRadiusMeters.steps; r++)
            {
                for (int i = 0; i < rotorInertiaScale.steps; i++)
                {
                    for (int g = 0; g < stabilityPitchGain.steps; g++)
                    {
                        for (int k = 0; k < pitchKp.steps; k++)
                        {
                            WindTurbinePlantParameters plant = basePlant.Clone();
                            WindTurbineControllerSettings controller = baseController.Clone();

                            plant.rotorRadius = rotorRadiusMeters.ValueAt(r);
                            plant.rotorInertia = baseInertia * rotorInertiaScale.ValueAt(i);
                            controller.stabilityPitchGain = stabilityPitchGain.ValueAt(g);
                            controller.pitchKp = pitchKp.ValueAt(k);
                            controller.mode = ControllerMode.StabilityLimited;

                            CoDesignResult result = ReducedOrderWindTurbineModel.Simulate(
                                plant,
                                controller,
                                baseWind,
                                durationSeconds,
                                timeStepSeconds);

                            result.rotorRadius = plant.rotorRadius;
                            result.inertiaScale = rotorInertiaScale.ValueAt(i);
                            result.stabilityPitchGain = controller.stabilityPitchGain;
                            result.pitchKp = controller.pitchKp;
                            result.designLabel = $"R{plant.rotorRadius:0.0}_I{result.inertiaScale:0.00}_G{result.stabilityPitchGain:0.0}_K{result.pitchKp:0.0}";
                            result.score = Score(result, plant);

                            results.Add(result);
                        }
                    }
                }
            }

            results.Sort((a, b) => b.score.CompareTo(a.score));
            WriteCsv();

            if (results.Count > 0)
            {
                CoDesignResult best = results[0];
                Debug.Log($"Best co-design candidate: {best.designLabel}, score={best.score:0.000}, energy={best.energyKWh:0.000} kWh, max tilt={best.maxAbsTiltDegrees:0.000} deg");
            }
        }

        private float Score(CoDesignResult result, WindTurbinePlantParameters plant)
        {
            float tiltExcess = Mathf.Max(0f, result.maxAbsTiltDegrees - plant.stabilityLimitDegrees);
            return result.energyKWh
                - stabilityCostWeight * result.stabilityCost
                - tiltViolationWeight * tiltExcess * tiltExcess;
        }

        private void WriteCsv()
        {
            StringBuilder csv = new StringBuilder();
            CultureInfo culture = CultureInfo.InvariantCulture;
            csv.AppendLine("rank,label,score,energy_kwh,avg_power_kw,stability_cost,max_tilt_deg,max_rpm,rotor_radius_m,inertia_scale,stability_pitch_gain,pitch_kp");

            for (int index = 0; index < results.Count; index++)
            {
                CoDesignResult r = results[index];
                csv.AppendFormat(
                    culture,
                    "{0},{1},{2:F6},{3:F6},{4:F6},{5:F6},{6:F6},{7:F6},{8:F4},{9:F4},{10:F4},{11:F4}\n",
                    index + 1,
                    r.designLabel,
                    r.score,
                    r.energyKWh,
                    r.averagePowerKw,
                    r.stabilityCost,
                    r.maxAbsTiltDegrees,
                    r.maxRotorSpeedRpm,
                    r.rotorRadius,
                    r.inertiaScale,
                    r.stabilityPitchGain,
                    r.pitchKp);
            }

            string path = Path.Combine(Application.persistentDataPath, fileName);
            File.WriteAllText(path, csv.ToString());
            Debug.Log($"Co-design sweep written to {path}");
        }
    }
}
