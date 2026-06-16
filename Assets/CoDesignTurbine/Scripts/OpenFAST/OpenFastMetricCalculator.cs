using System;
using UnityEngine;

namespace CoDesignTurbine.OpenFAST
{
    [Serializable]
    public class OpenFastScoreWeights
    {
        public float stabilityPenalty = 5f;
        public float towerDisplacementPenalty = 2f;
        public float overspeedPenalty = 0.5f;
        public float ratedRotorSpeedRpm = 12.1f;
    }

    public class OpenFastCoDesignResult
    {
        public string label;
        public string runRoot;
        public int exitCode;
        public float energyKWh;
        public float averagePowerKw;
        public float maxAbsPlatformPitchDegrees;
        public float maxAbsTowerTopForeAftMeters;
        public float maxRotorSpeedRpm;
        public float score;
    }

    public static class OpenFastMetricCalculator
    {
        public static OpenFastCoDesignResult Calculate(
            OpenFastOutputSeries series,
            OpenFastChannelMap map,
            OpenFastScoreWeights weights)
        {
            if (series == null)
            {
                throw new ArgumentNullException("series");
            }

            if (map == null)
            {
                map = new OpenFastChannelMap();
            }

            if (weights == null)
            {
                weights = new OpenFastScoreWeights();
            }

            float energyKWh = IntegratePowerKWh(series, map.generatorPowerKw);
            float duration = Mathf.Max(0.001f, series.DurationSeconds);
            float maxPitch = MaxAbs(series, map.platformPitchDegrees);
            float maxTower = MaxAbs(series, map.towerTopForeAftMeters);
            float maxRotor = MaxAbs(series, map.rotorSpeedRpm);
            float overspeed = Mathf.Max(0f, maxRotor - weights.ratedRotorSpeedRpm);

            float score = energyKWh
                - weights.stabilityPenalty * maxPitch * maxPitch
                - weights.towerDisplacementPenalty * maxTower * maxTower
                - weights.overspeedPenalty * overspeed * overspeed;

            return new OpenFastCoDesignResult
            {
                energyKWh = energyKWh,
                averagePowerKw = energyKWh * 3600f / duration,
                maxAbsPlatformPitchDegrees = maxPitch,
                maxAbsTowerTopForeAftMeters = maxTower,
                maxRotorSpeedRpm = maxRotor,
                score = score
            };
        }

        private static float IntegratePowerKWh(OpenFastOutputSeries series, string powerChannel)
        {
            float[] power;
            if (!series.TryGetChannel(powerChannel, out power) || power.Length < 2 || series.timeSeconds == null)
            {
                return 0f;
            }

            int count = Mathf.Min(power.Length, series.timeSeconds.Length);
            float kWS = 0f;
            for (int i = 1; i < count; i++)
            {
                float dt = Mathf.Max(0f, series.timeSeconds[i] - series.timeSeconds[i - 1]);
                kWS += 0.5f * (power[i] + power[i - 1]) * dt;
            }

            return kWS / 3600f;
        }

        private static float MaxAbs(OpenFastOutputSeries series, string channel)
        {
            float[] values;
            if (!series.TryGetChannel(channel, out values) || values.Length == 0)
            {
                return 0f;
            }

            float max = 0f;
            for (int i = 0; i < values.Length; i++)
            {
                max = Mathf.Max(max, Mathf.Abs(values[i]));
            }

            return max;
        }
    }
}

