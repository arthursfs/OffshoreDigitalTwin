using UnityEngine;

namespace CoDesignTurbine
{
    public struct AerodynamicLoads
    {
        public float powerCoefficient;
        public float thrustCoefficient;
        public float aerodynamicPower;
        public float aerodynamicTorqueNm;
        public float thrustNewtons;
    }

    public static class BladeElementAerodynamics
    {
        public static AerodynamicLoads Evaluate(
            WindTurbinePlantParameters plant,
            float windSpeed,
            float rotorSpeedRadPerSecond,
            float bladePitchDegrees)
        {
            BladeElementSettings settings = plant.bladeElement ?? new BladeElementSettings();
            int stations = Mathf.Max(4, settings.radialStations);
            int blades = Mathf.Max(1, settings.bladeCount);
            float radius = Mathf.Max(0.1f, plant.rotorRadius);
            float rootRadius = Mathf.Clamp(settings.rootCutoutRatio, 0.01f, 0.8f) * radius;
            float dr = (radius - rootRadius) / stations;
            float omega = Mathf.Max(0.01f, rotorSpeedRadPerSecond);
            float axialWind = Mathf.Max(0.01f, windSpeed);

            float torque = 0f;
            float thrust = 0f;

            for (int i = 0; i < stations; i++)
            {
                float stationRatio = (i + 0.5f) / stations;
                float r = rootRadius + (i + 0.5f) * dr;
                float bladeRatio = Mathf.InverseLerp(rootRadius, radius, r);
                float chord = Mathf.Lerp(settings.rootChordMeters, settings.tipChordMeters, bladeRatio);
                float twistDegrees = Mathf.Lerp(settings.rootTwistDegrees, settings.tipTwistDegrees, bladeRatio);

                float tangentialSpeed = omega * r;
                float relativeSpeed = Mathf.Sqrt(axialWind * axialWind + tangentialSpeed * tangentialSpeed);
                float inflowAngleRad = Mathf.Atan2(axialWind, tangentialSpeed);
                float angleOfAttackRad = inflowAngleRad - (twistDegrees + bladePitchDegrees - settings.zeroLiftAngleDegrees) * Mathf.Deg2Rad;

                float cl = Mathf.Clamp(
                    settings.liftSlopePerRadian * angleOfAttackRad,
                    -settings.maxLiftCoefficient,
                    settings.maxLiftCoefficient);
                float cd = settings.dragCoefficientZeroLift
                    + settings.dragCoefficientQuadratic * angleOfAttackRad * angleOfAttackRad;

                float dynamicPressure = 0.5f * plant.airDensity * relativeSpeed * relativeSpeed;
                float liftPerMeter = dynamicPressure * chord * cl;
                float dragPerMeter = dynamicPressure * chord * cd;

                float tangentialForcePerMeter = liftPerMeter * Mathf.Sin(inflowAngleRad)
                    - dragPerMeter * Mathf.Cos(inflowAngleRad);
                float normalForcePerMeter = liftPerMeter * Mathf.Cos(inflowAngleRad)
                    + dragPerMeter * Mathf.Sin(inflowAngleRad);

                torque += tangentialForcePerMeter * r * dr * blades;
                thrust += normalForcePerMeter * dr * blades;
            }

            torque = Mathf.Clamp(torque, 0f, plant.maxAerodynamicTorque);
            thrust = Mathf.Max(0f, thrust);

            float power = torque * omega;
            float denominator = 0.5f * plant.airDensity * plant.RotorArea * axialWind * axialWind * axialWind;
            float cp = denominator > 0.001f ? Mathf.Clamp(power / denominator, 0f, 0.593f) : 0f;
            float ctDenominator = 0.5f * plant.airDensity * plant.RotorArea * axialWind * axialWind;
            float ct = ctDenominator > 0.001f ? Mathf.Clamp(thrust / ctDenominator, 0f, 1.5f) : 0f;

            return new AerodynamicLoads
            {
                powerCoefficient = cp,
                thrustCoefficient = ct,
                aerodynamicPower = power,
                aerodynamicTorqueNm = torque,
                thrustNewtons = thrust
            };
        }

        public static AerodynamicLoads EvaluateLumpedCp(
            WindTurbinePlantParameters plant,
            float windSpeed,
            float rotorSpeedRadPerSecond,
            float bladePitchDegrees)
        {
            float omega = Mathf.Max(0.05f, rotorSpeedRadPerSecond);
            float tipSpeedRatio = omega * plant.rotorRadius / Mathf.Max(0.1f, windSpeed);
            float cp = PowerCoefficientModel.PowerCoefficient(tipSpeedRatio, bladePitchDegrees, plant);
            float ct = PowerCoefficientModel.ThrustCoefficient(tipSpeedRatio, bladePitchDegrees, plant);
            float power = 0.5f * plant.airDensity * plant.RotorArea * cp * windSpeed * windSpeed * windSpeed;
            float torque = Mathf.Min(power / omega, plant.maxAerodynamicTorque);
            float thrust = 0.5f * plant.airDensity * plant.RotorArea * ct * windSpeed * windSpeed;

            return new AerodynamicLoads
            {
                powerCoefficient = cp,
                thrustCoefficient = ct,
                aerodynamicPower = power,
                aerodynamicTorqueNm = torque,
                thrustNewtons = thrust
            };
        }
    }
}

