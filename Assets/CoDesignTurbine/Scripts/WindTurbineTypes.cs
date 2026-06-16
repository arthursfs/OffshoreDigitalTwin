using System;
using UnityEngine;

namespace CoDesignTurbine
{
    public enum ControllerMode
    {
        GreedyCpTracking,
        RatedPowerPi,
        StabilityLimited
    }

    public enum AerodynamicModel
    {
        LumpedCpCurve,
        BladeElementTheory
    }

    [Serializable]
    public class WindTurbinePlantParameters
    {
        [Header("Rotor and drivetrain")]
        [Min(0.1f)] public float rotorRadius = 45f;
        [Min(0.01f)] public float airDensity = 1.225f;
        [Min(0.01f)] public float rotorInertia = 3900000f;
        [Min(0f)] public float drivetrainDamping = 12000f;
        [Range(0.1f, 1f)] public float generatorEfficiency = 0.94f;
        [Min(1f)] public float ratedPower = 2000000f;
        [Min(0.1f)] public float ratedRotorSpeedRpm = 16f;
        [Min(1f)] public float maxGeneratorTorque = 1800000f;
        [Min(1f)] public float maxAerodynamicTorque = 3200000f;

        [Header("Tower or platform reduced mode")]
        [Min(0.1f)] public float hubHeight = 80f;
        [Min(1f)] public float foreAftInertia = 80000000f;
        [Min(0f)] public float foreAftDamping = 8500000f;
        [Min(0f)] public float foreAftStiffness = 65000000f;
        [Range(0.1f, 45f)] public float stabilityLimitDegrees = 8f;
        [Range(0.1f, 45f)] public float hardStopDegrees = 14f;

        [Header("Aerodynamics")]
        public AerodynamicModel aerodynamicModel = AerodynamicModel.BladeElementTheory;
        [Range(1f, 16f)] public float optimalTipSpeedRatio = 7.5f;
        [Range(0.01f, 0.59f)] public float optimalPowerCoefficient = 0.46f;
        [Range(0.01f, 1.5f)] public float nominalThrustCoefficient = 0.82f;
        public BladeElementSettings bladeElement = new BladeElementSettings();

        public float RotorArea => Mathf.PI * rotorRadius * rotorRadius;
        public float RatedRotorSpeedRadPerSec => ratedRotorSpeedRpm * Mathf.PI * 2f / 60f;

        public float Region2TorqueGain
        {
            get
            {
                float lambda = Mathf.Max(0.1f, optimalTipSpeedRatio);
                return 0.5f * airDensity * RotorArea * Mathf.Pow(rotorRadius, 3f) * optimalPowerCoefficient / Mathf.Pow(lambda, 3f);
            }
        }

        public WindTurbinePlantParameters Clone()
        {
            WindTurbinePlantParameters clone = (WindTurbinePlantParameters)MemberwiseClone();
            clone.bladeElement = bladeElement != null ? bladeElement.Clone() : new BladeElementSettings();
            return clone;
        }
    }

    [Serializable]
    public class BladeElementSettings
    {
        [Range(1, 8)] public int bladeCount = 3;
        [Range(4, 64)] public int radialStations = 24;
        [Range(0.01f, 0.4f)] public float rootCutoutRatio = 0.18f;

        [Header("Parametric blade geometry")]
        [Min(0.01f)] public float rootChordMeters = 4.5f;
        [Min(0.01f)] public float tipChordMeters = 1.2f;
        [Range(-30f, 45f)] public float rootTwistDegrees = 14f;
        [Range(-30f, 45f)] public float tipTwistDegrees = 0f;

        [Header("Simple polar")]
        [Min(0f)] public float liftSlopePerRadian = 6.0f;
        [Min(0.1f)] public float maxLiftCoefficient = 1.35f;
        [Min(0f)] public float dragCoefficientZeroLift = 0.012f;
        [Min(0f)] public float dragCoefficientQuadratic = 0.018f;
        [Range(-20f, 20f)] public float zeroLiftAngleDegrees = -2f;

        public BladeElementSettings Clone()
        {
            return (BladeElementSettings)MemberwiseClone();
        }
    }

    [Serializable]
    public class WindTurbineControllerSettings
    {
        public ControllerMode mode = ControllerMode.StabilityLimited;

        [Header("Actuator limits")]
        [Range(0f, 10f)] public float minPitchDegrees = 0f;
        [Range(5f, 90f)] public float maxPitchDegrees = 35f;
        [Min(0.1f)] public float maxPitchRateDegreesPerSecond = 8f;
        [Min(1f)] public float maxTorqueRateNmPerSecond = 180000f;

        [Header("Rated speed pitch PI")]
        [Min(0f)] public float pitchKp = 14f;
        [Min(0f)] public float pitchKi = 4f;
        [Min(0f)] public float integralLimit = 1.5f;

        [Header("Stability feedback")]
        [Range(0f, 1f)] public float stabilityFeedbackStartRatio = 0.65f;
        [Min(0f)] public float stabilityPitchGain = 9f;
        [Min(0f)] public float tiltRatePitchGain = 0.35f;
        [Range(0f, 1f)] public float stabilityTorqueDerateGain = 0.6f;

        [Header("Operating limits")]
        [Min(0f)] public float cutInWindSpeed = 3f;
        [Min(1f)] public float cutOutWindSpeed = 25f;
        public bool featherAboveCutOut = true;

        public WindTurbineControllerSettings Clone()
        {
            return (WindTurbineControllerSettings)MemberwiseClone();
        }
    }

    [Serializable]
    public class WindProfileSettings
    {
        [Min(0f)] public float meanWindSpeed = 10f;
        [Range(0f, 1f)] public float turbulenceIntensity = 0.12f;
        [Min(0f)] public float turbulenceFrequency = 0.15f;
        [Min(0f)] public float gustAmplitude = 4f;
        [Min(0f)] public float gustStartTime = 30f;
        [Min(0.1f)] public float gustDuration = 12f;
        [Min(1f)] public float referenceHeight = 80f;
        [Range(0f, 1f)] public float shearExponent = 0.14f;
        public int seed = 42;

        public WindProfileSettings Clone()
        {
            return (WindProfileSettings)MemberwiseClone();
        }
    }

    [Serializable]
    public struct WindTurbineState
    {
        public float rotorSpeedRadPerSec;
        public float rotorAzimuthRad;
        public float foreAftAngleRad;
        public float foreAftRateRadPerSec;
        public float bladePitchDegrees;
        public float generatorTorqueNm;
        public float energyJoules;
        public float stabilityCost;
        public float overspeedSeconds;

        public static WindTurbineState CreateInitial(WindTurbinePlantParameters plant)
        {
            return new WindTurbineState
            {
                rotorSpeedRadPerSec = Mathf.Max(0.2f, 0.35f * plant.RatedRotorSpeedRadPerSec),
                bladePitchDegrees = 0f
            };
        }
    }

    public struct WindTurbineControl
    {
        public float targetGeneratorTorqueNm;
        public float targetBladePitchDegrees;
    }

    public class WindTurbineControllerMemory
    {
        public float pitchIntegral;

        public void Reset()
        {
            pitchIntegral = 0f;
        }
    }

    public struct WindTurbineSample
    {
        public float timeSeconds;
        public float windSpeed;
        public float rotorSpeedRpm;
        public float tipSpeedRatio;
        public float bladePitchDegrees;
        public float generatorTorqueNm;
        public float powerCoefficient;
        public float thrustCoefficient;
        public float aerodynamicPower;
        public float electricalPower;
        public float aerodynamicTorqueNm;
        public float thrustNewtons;
        public string aerodynamicModelName;
        public float foreAftAngleDegrees;
        public float foreAftRateDegreesPerSecond;
        public float stabilityMargin;
        public float energyKWh;
        public float stabilityCost;
        public float overspeedSeconds;
    }

    [Serializable]
    public class CoDesignSweepRange
    {
        public float min = 0f;
        public float max = 1f;
        [Min(1)] public int steps = 3;

        public float ValueAt(int index)
        {
            if (steps <= 1)
            {
                return min;
            }

            float t = Mathf.Clamp01(index / (float)(steps - 1));
            return Mathf.Lerp(min, max, t);
        }
    }

    public class CoDesignResult
    {
        public string designLabel;
        public float rotorRadius;
        public float inertiaScale;
        public float stabilityPitchGain;
        public float pitchKp;
        public float energyKWh;
        public float stabilityCost;
        public float maxAbsTiltDegrees;
        public float maxRotorSpeedRpm;
        public float averagePowerKw;
        public float score;
    }
}
