using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoDesignTurbine.OpenFAST
{
    public class OpenFastOutputSeries
    {
        public string sourcePath;
        public string[] channelNames;
        public string[] units;
        public float[] timeSeconds;

        private readonly Dictionary<string, float[]> channels = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);

        public int SampleCount
        {
            get { return timeSeconds == null ? 0 : timeSeconds.Length; }
        }

        public float DurationSeconds
        {
            get
            {
                if (timeSeconds == null || timeSeconds.Length == 0)
                {
                    return 0f;
                }

                return timeSeconds[timeSeconds.Length - 1];
            }
        }

        public void AddChannel(string name, float[] values)
        {
            if (string.IsNullOrWhiteSpace(name) || values == null)
            {
                return;
            }

            channels[name.Trim()] = values;
        }

        public bool HasChannel(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && channels.ContainsKey(name.Trim());
        }

        public bool TryGetChannel(string name, out float[] values)
        {
            values = null;
            return !string.IsNullOrWhiteSpace(name) && channels.TryGetValue(name.Trim(), out values);
        }

        public float SampleChannel(string channelName, float time, float fallback)
        {
            float[] values;
            if (!TryGetChannel(channelName, out values) || values.Length == 0 || timeSeconds == null || timeSeconds.Length == 0)
            {
                return fallback;
            }

            if (time <= timeSeconds[0])
            {
                return values[0];
            }

            int last = Mathf.Min(timeSeconds.Length, values.Length) - 1;
            if (time >= timeSeconds[last])
            {
                return values[last];
            }

            int lo = 0;
            int hi = last;
            while (hi - lo > 1)
            {
                int mid = (lo + hi) / 2;
                if (timeSeconds[mid] <= time)
                {
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            float span = Mathf.Max(0.000001f, timeSeconds[hi] - timeSeconds[lo]);
            float alpha = Mathf.Clamp01((time - timeSeconds[lo]) / span);
            return Mathf.Lerp(values[lo], values[hi], alpha);
        }

        public OpenFastPlaybackSample Sample(OpenFastChannelMap map, float time)
        {
            if (map == null)
            {
                map = new OpenFastChannelMap();
            }

            return new OpenFastPlaybackSample
            {
                timeSeconds = time,
                windSpeed = SampleChannel(map.windSpeed, time, 0f),
                rotorSpeedRpm = SampleChannel(map.rotorSpeedRpm, time, 0f),
                rotorAzimuthDegrees = SampleChannel(map.rotorAzimuthDegrees, time, float.NaN),
                generatorPowerKw = SampleChannel(map.generatorPowerKw, time, 0f),
                generatorTorque = SampleChannel(map.generatorTorque, time, 0f),
                bladePitch1Degrees = SampleChannel(map.bladePitch1Degrees, time, 0f),
                bladePitch2Degrees = SampleChannel(map.bladePitch2Degrees, time, 0f),
                bladePitch3Degrees = SampleChannel(map.bladePitch3Degrees, time, 0f),
                platformSurgeMeters = SampleChannel(map.platformSurgeMeters, time, 0f),
                platformSwayMeters = SampleChannel(map.platformSwayMeters, time, 0f),
                platformHeaveMeters = SampleChannel(map.platformHeaveMeters, time, 0f),
                platformRollDegrees = SampleChannel(map.platformRollDegrees, time, 0f),
                platformPitchDegrees = SampleChannel(map.platformPitchDegrees, time, 0f),
                platformYawDegrees = SampleChannel(map.platformYawDegrees, time, 0f),
                towerTopForeAftMeters = SampleChannel(map.towerTopForeAftMeters, time, 0f),
                towerTopSideSideMeters = SampleChannel(map.towerTopSideSideMeters, time, 0f),
                towerBaseMy = SampleChannel(map.towerBaseMy, time, 0f)
            };
        }
    }

    [Serializable]
    public struct OpenFastPlaybackSample
    {
        public float timeSeconds;
        public float windSpeed;
        public float rotorSpeedRpm;
        public float rotorAzimuthDegrees;
        public float generatorPowerKw;
        public float generatorTorque;
        public float bladePitch1Degrees;
        public float bladePitch2Degrees;
        public float bladePitch3Degrees;
        public float platformSurgeMeters;
        public float platformSwayMeters;
        public float platformHeaveMeters;
        public float platformRollDegrees;
        public float platformPitchDegrees;
        public float platformYawDegrees;
        public float towerTopForeAftMeters;
        public float towerTopSideSideMeters;
        public float towerBaseMy;
    }
}

