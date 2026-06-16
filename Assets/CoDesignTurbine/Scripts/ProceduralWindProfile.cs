using UnityEngine;

namespace CoDesignTurbine
{
    public static class ProceduralWindProfile
    {
        public static float Evaluate(WindProfileSettings settings, float timeSeconds, float hubHeight)
        {
            float shear = Mathf.Pow(Mathf.Max(0.1f, hubHeight) / Mathf.Max(0.1f, settings.referenceHeight), settings.shearExponent);
            float mean = settings.meanWindSpeed * shear;

            float noiseA = Mathf.PerlinNoise(settings.seed * 0.013f, timeSeconds * settings.turbulenceFrequency);
            float noiseB = Mathf.PerlinNoise(settings.seed * 0.017f + 37.1f, timeSeconds * settings.turbulenceFrequency * 0.31f);
            float turbulence = ((noiseA - 0.5f) * 0.75f + (noiseB - 0.5f) * 0.25f) * 2f;
            turbulence *= mean * settings.turbulenceIntensity;

            float gust = 0f;
            float gustAge = timeSeconds - settings.gustStartTime;
            if (gustAge >= 0f && gustAge <= settings.gustDuration)
            {
                float phase = gustAge / Mathf.Max(0.1f, settings.gustDuration);
                gust = settings.gustAmplitude * Mathf.Sin(Mathf.PI * phase);
            }

            return Mathf.Max(0f, mean + turbulence + gust);
        }
    }
}

