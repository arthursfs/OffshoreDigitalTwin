using System;
using System.Collections.Generic;
using UnityEngine;

namespace CoDesignTurbine.MOST
{
    public class MostOutputSeries
    {
        public string sourcePath;
        public string[] channelNames;
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
            if (!string.IsNullOrWhiteSpace(name) && values != null)
            {
                channels[name.Trim()] = values;
            }
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

        public float SampleForwardAngleChannel(string channelName, float time, float fallback)
        {
            float[] values;
            if (!TryGetChannel(channelName, out values) || values.Length == 0 || timeSeconds == null || timeSeconds.Length == 0)
            {
                return fallback;
            }

            if (time <= timeSeconds[0])
            {
                return Mathf.Repeat(values[0], 360f);
            }

            int last = Mathf.Min(timeSeconds.Length, values.Length) - 1;
            if (time >= timeSeconds[last])
            {
                return Mathf.Repeat(values[last], 360f);
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

            float a = values[lo];
            float b = values[hi];
            if (float.IsNaN(a) || float.IsNaN(b))
            {
                return fallback;
            }

            while (b < a)
            {
                b += 360f;
            }

            float span = Mathf.Max(0.000001f, timeSeconds[hi] - timeSeconds[lo]);
            float alpha = Mathf.Clamp01((time - timeSeconds[lo]) / span);
            return Mathf.Repeat(Mathf.Lerp(a, b, alpha), 360f);
        }

        public MostPlaybackSample Sample(MostChannelMap map, float time)
        {
            if (map == null)
            {
                map = new MostChannelMap();
            }

            return new MostPlaybackSample
            {
                timeSeconds = time,
                surgeMeters = SampleChannel(map.surgeMeters, time, 0f),
                swayMeters = SampleChannel(map.swayMeters, time, 0f),
                heaveMeters = SampleChannel(map.heaveMeters, time, 0f),
                rollDegrees = SampleChannel(map.rollDegrees, time, 0f),
                pitchDegrees = SampleChannel(map.pitchDegrees, time, 0f),
                yawDegrees = SampleChannel(map.yawDegrees, time, 0f),
                rotorSpeedRpm = SampleChannel(map.rotorSpeedRpm, time, 0f),
                rotorAzimuthDegrees = SampleForwardAngleChannel(map.rotorAzimuthDegrees, time, float.NaN),
                turbinePowerMw = SampleChannel(map.turbinePowerMw, time, 0f),
                generatorTorqueNm = SampleChannel(map.generatorTorqueNm, time, 0f),
                bladePitchDegrees = SampleChannel(map.bladePitchDegrees, time, 0f),
                windSpeedMetersPerSecond = SampleChannel(map.windSpeedMetersPerSecond, time, 0f),
                waveElevationMeters = SampleChannel(map.waveElevationMeters, time, 0f),
                ptoPowerMw = SampleChannel(map.ptoPowerMw, time, 0f)
            };
        }
    }

    [Serializable]
    public class MostChannelMap
    {
        public string timeSeconds = "time_s";
        public string surgeMeters = "surge_m";
        public string swayMeters = "sway_m";
        public string heaveMeters = "heave_m";
        public string rollDegrees = "roll_deg";
        public string pitchDegrees = "pitch_deg";
        public string yawDegrees = "yaw_deg";
        public string rotorSpeedRpm = "rotor_speed_rpm";
        public string rotorAzimuthDegrees = "rotor_azimuth_deg";
        public string turbinePowerMw = "turbine_power_mw";
        public string generatorTorqueNm = "gen_torque_nm";
        public string bladePitchDegrees = "blade_pitch_deg";
        public string windSpeedMetersPerSecond = "wind_speed_mps";
        public string waveElevationMeters = "wave_elevation_m";
        public string ptoPowerMw = "pto_power_mw";
    }

    [Serializable]
    public struct MostPlaybackSample
    {
        public float timeSeconds;
        public float surgeMeters;
        public float swayMeters;
        public float heaveMeters;
        public float rollDegrees;
        public float pitchDegrees;
        public float yawDegrees;
        public float rotorSpeedRpm;
        public float rotorAzimuthDegrees;
        public float turbinePowerMw;
        public float generatorTorqueNm;
        public float bladePitchDegrees;
        public float windSpeedMetersPerSecond;
        public float waveElevationMeters;
        public float ptoPowerMw;
    }
}
