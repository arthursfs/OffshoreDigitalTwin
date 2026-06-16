using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace CoDesignTurbine.OpenFAST
{
    public class OpenFastCoDesignRunner : MonoBehaviour
    {
        public OpenFastCaseConfig caseConfig;
        public bool runOnStart;
        public KeyCode runKey = KeyCode.F10;

        [Header("Parameter sweeps")]
        public OpenFastParameterSweep[] sweeps;

        [Header("Scoring")]
        public OpenFastScoreWeights scoreWeights = new OpenFastScoreWeights();

        [Header("Output")]
        public string resultCsvFileName = "openfast_codesign_results.csv";
        public bool loadOutputForBestRun = true;
        public OpenFastPlaybackDriver playbackTarget;

        public List<OpenFastCoDesignResult> Results { get; private set; }

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
            if (caseConfig == null)
            {
                Debug.LogWarning("No OpenFAST case config assigned.");
                return;
            }

            Results = new List<OpenFastCoDesignResult>();
            OpenFastCaseConfig sweepConfig = Instantiate(caseConfig);
            sweepConfig.copyCaseBeforeRun = true;

            List<int[]> combinations = BuildIndexCombinations(sweeps);
            if (combinations.Count == 0)
            {
                combinations.Add(new int[0]);
            }

            for (int i = 0; i < combinations.Count; i++)
            {
                List<OpenFastParameterEdit> edits = OpenFastParameterPatcher.BuildEdits(sweeps, combinations[i]);
                string label = OpenFastRunUtility.SanitizeFileName(OpenFastParameterPatcher.DescribeEdits(edits));

                try
                {
                    OpenFastRunResult run = OpenFastRunUtility.Run(sweepConfig, label, edits, true);
                    OpenFastCoDesignResult metrics = new OpenFastCoDesignResult
                    {
                        label = label,
                        runRoot = run.runRoot,
                        exitCode = run.exitCode
                    };

                    if (run.outputSeries != null)
                    {
                        OpenFastCoDesignResult calculated = OpenFastMetricCalculator.Calculate(run.outputSeries, sweepConfig.channelMap, scoreWeights);
                        metrics.energyKWh = calculated.energyKWh;
                        metrics.averagePowerKw = calculated.averagePowerKw;
                        metrics.maxAbsPlatformPitchDegrees = calculated.maxAbsPlatformPitchDegrees;
                        metrics.maxAbsTowerTopForeAftMeters = calculated.maxAbsTowerTopForeAftMeters;
                        metrics.maxRotorSpeedRpm = calculated.maxRotorSpeedRpm;
                        metrics.score = calculated.score;
                    }

                    Results.Add(metrics);
                }
                catch (Exception ex)
                {
                    Results.Add(new OpenFastCoDesignResult
                    {
                        label = label,
                        exitCode = -1,
                        score = float.NegativeInfinity
                    });
                    Debug.LogError("OpenFAST co-design run failed for " + label + ": " + ex.Message);
                }
            }

            Results.Sort((a, b) => b.score.CompareTo(a.score));
            WriteCsv();

            if (Results.Count > 0)
            {
                OpenFastCoDesignResult best = Results[0];
                Debug.Log("Best OpenFAST co-design candidate: " + best.label + ", score=" + best.score.ToString("0.000") + ", energy=" + best.energyKWh.ToString("0.000") + " kWh.");

                if (loadOutputForBestRun && playbackTarget != null && !string.IsNullOrWhiteSpace(best.runRoot))
                {
                    string output = sweepConfig.ResolveOutputPath(best.runRoot);
                    playbackTarget.outputFilePath = output;
                    playbackTarget.SetSeries(OpenFastAsciiOutputReader.Read(output), sweepConfig.channelMap);
                }
            }
        }

        private static List<int[]> BuildIndexCombinations(OpenFastParameterSweep[] sweepDefinitions)
        {
            List<int[]> results = new List<int[]>();
            if (sweepDefinitions == null || sweepDefinitions.Length == 0)
            {
                return results;
            }

            int[] current = new int[sweepDefinitions.Length];
            AddCombinationRecursive(sweepDefinitions, 0, current, results);
            return results;
        }

        private static void AddCombinationRecursive(OpenFastParameterSweep[] sweepDefinitions, int depth, int[] current, List<int[]> results)
        {
            if (depth >= sweepDefinitions.Length)
            {
                int[] copy = new int[current.Length];
                Array.Copy(current, copy, current.Length);
                results.Add(copy);
                return;
            }

            OpenFastParameterSweep sweep = sweepDefinitions[depth];
            int count = sweep != null && sweep.values != null && sweep.values.Length > 0 ? sweep.values.Length : 1;
            for (int i = 0; i < count; i++)
            {
                current[depth] = i;
                AddCombinationRecursive(sweepDefinitions, depth + 1, current, results);
            }
        }

        private void WriteCsv()
        {
            if (Results == null)
            {
                return;
            }

            StringBuilder csv = new StringBuilder();
            CultureInfo culture = CultureInfo.InvariantCulture;
            csv.AppendLine("rank,label,exit_code,score,energy_kwh,average_power_kw,max_platform_pitch_deg,max_tower_fa_m,max_rotor_rpm,run_root");

            for (int i = 0; i < Results.Count; i++)
            {
                OpenFastCoDesignResult r = Results[i];
                csv.AppendFormat(
                    culture,
                    "{0},{1},{2},{3:F6},{4:F6},{5:F6},{6:F6},{7:F6},{8:F6},{9}\n",
                    i + 1,
                    EscapeCsv(r.label),
                    r.exitCode,
                    r.score,
                    r.energyKWh,
                    r.averagePowerKw,
                    r.maxAbsPlatformPitchDegrees,
                    r.maxAbsTowerTopForeAftMeters,
                    r.maxRotorSpeedRpm,
                    EscapeCsv(r.runRoot));
            }

            string path = Path.Combine(Application.persistentDataPath, resultCsvFileName);
            File.WriteAllText(path, csv.ToString());
            Debug.Log("OpenFAST co-design results written to " + path);
        }

        private static string EscapeCsv(string value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value.IndexOf(',') >= 0 || value.IndexOf('"') >= 0)
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }
    }
}
