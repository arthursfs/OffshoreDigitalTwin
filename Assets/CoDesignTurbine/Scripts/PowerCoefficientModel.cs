using UnityEngine;

namespace CoDesignTurbine
{
    public static class PowerCoefficientModel
    {
        public static float PowerCoefficient(float tipSpeedRatio, float pitchDegrees, WindTurbinePlantParameters plant)
        {
            float lambda = Mathf.Max(0.01f, tipSpeedRatio);
            float beta = Mathf.Max(0f, pitchDegrees);

            float inverseLambdaI = 1f / (lambda + 0.08f * beta) - 0.035f / (Mathf.Pow(beta, 3f) + 1f);
            float lambdaI = inverseLambdaI > 0.001f ? 1f / inverseLambdaI : 1000f;

            float empiricalCp = 0.22f * (116f / lambdaI - 0.4f * beta - 5f) * Mathf.Exp(-12.5f / lambdaI);
            float shapedCp = plant.optimalPowerCoefficient
                * Mathf.Exp(-Mathf.Pow((lambda - plant.optimalTipSpeedRatio) / 3.5f, 2f))
                * Mathf.Exp(-0.055f * beta);

            float cp = Mathf.Lerp(empiricalCp, shapedCp, 0.25f);
            return Mathf.Clamp(cp, 0f, 0.593f);
        }

        public static float ThrustCoefficient(float tipSpeedRatio, float pitchDegrees, WindTurbinePlantParameters plant)
        {
            float lambdaError = Mathf.Abs(tipSpeedRatio - plant.optimalTipSpeedRatio) / Mathf.Max(0.1f, plant.optimalTipSpeedRatio);
            float pitchLoss = Mathf.InverseLerp(0f, 30f, Mathf.Max(0f, pitchDegrees)) * 0.45f;
            float speedLoss = Mathf.Clamp01(lambdaError) * 0.25f;
            float ct = plant.nominalThrustCoefficient * (1f - pitchLoss - speedLoss);
            return Mathf.Clamp(ct, 0.05f, 1.2f);
        }
    }
}

