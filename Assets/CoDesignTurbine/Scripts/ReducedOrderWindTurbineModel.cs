using UnityEngine;

namespace CoDesignTurbine
{
    public static class ReducedOrderWindTurbineModel
    {
        public static WindTurbineSample Step(
            ref WindTurbineState state,
            WindTurbinePlantParameters plant,
            WindTurbineControllerSettings controller,
            WindProfileSettings wind,
            WindTurbineControllerMemory memory,
            float timeSeconds,
            float dt)
        {
            dt = Mathf.Max(0.0001f, dt);
            if (memory == null)
            {
                memory = new WindTurbineControllerMemory();
            }

            float windSpeed = ProceduralWindProfile.Evaluate(wind, timeSeconds, plant.hubHeight);
            WindTurbineControl target = ComputeControl(state, plant, controller, memory, windSpeed, dt);

            state.generatorTorqueNm = Mathf.MoveTowards(
                state.generatorTorqueNm,
                target.targetGeneratorTorqueNm,
                Mathf.Max(1f, controller.maxTorqueRateNmPerSecond) * dt);

            state.bladePitchDegrees = Mathf.MoveTowards(
                state.bladePitchDegrees,
                target.targetBladePitchDegrees,
                Mathf.Max(0.1f, controller.maxPitchRateDegreesPerSecond) * dt);

            float omega = Mathf.Max(0.05f, state.rotorSpeedRadPerSec);
            float tipSpeedRatio = omega * plant.rotorRadius / Mathf.Max(0.1f, windSpeed);
            AerodynamicLoads loads = plant.aerodynamicModel == AerodynamicModel.BladeElementTheory
                ? BladeElementAerodynamics.Evaluate(plant, windSpeed, omega, state.bladePitchDegrees)
                : BladeElementAerodynamics.EvaluateLumpedCp(plant, windSpeed, omega, state.bladePitchDegrees);

            float aerodynamicPower = loads.aerodynamicPower;
            float aerodynamicTorque = loads.aerodynamicTorqueNm;
            float thrust = loads.thrustNewtons;
            float electricalPower = Mathf.Max(0f, state.generatorTorqueNm * omega * plant.generatorEfficiency);
            electricalPower = Mathf.Min(electricalPower, plant.ratedPower * 1.15f);

            float rotorAcceleration = (aerodynamicTorque - state.generatorTorqueNm - plant.drivetrainDamping * omega)
                / Mathf.Max(1f, plant.rotorInertia);
            state.rotorSpeedRadPerSec = Mathf.Max(0f, state.rotorSpeedRadPerSec + rotorAcceleration * dt);
            state.rotorAzimuthRad = Mathf.Repeat(state.rotorAzimuthRad + state.rotorSpeedRadPerSec * dt, Mathf.PI * 2f);

            float overturningMoment = thrust * plant.hubHeight;
            float foreAftAcceleration = (overturningMoment
                - plant.foreAftDamping * state.foreAftRateRadPerSec
                - plant.foreAftStiffness * state.foreAftAngleRad)
                / Mathf.Max(1f, plant.foreAftInertia);

            state.foreAftRateRadPerSec += foreAftAcceleration * dt;
            state.foreAftAngleRad += state.foreAftRateRadPerSec * dt;

            float hardStopRad = plant.hardStopDegrees * Mathf.Deg2Rad;
            if (Mathf.Abs(state.foreAftAngleRad) > hardStopRad)
            {
                state.foreAftAngleRad = Mathf.Sign(state.foreAftAngleRad) * hardStopRad;
                state.foreAftRateRadPerSec *= -0.15f;
            }

            float ratedOmega = plant.RatedRotorSpeedRadPerSec;
            float overspeedRatio = Mathf.Max(0f, state.rotorSpeedRadPerSec / Mathf.Max(0.1f, ratedOmega) - 1f);
            if (overspeedRatio > 0f)
            {
                state.overspeedSeconds += dt;
            }

            float tiltRatio = Mathf.Abs(state.foreAftAngleRad) / Mathf.Max(0.001f, plant.stabilityLimitDegrees * Mathf.Deg2Rad);
            float rateCost = Mathf.Abs(state.foreAftRateRadPerSec * Mathf.Rad2Deg) * 0.02f;
            state.stabilityCost += (tiltRatio * tiltRatio + rateCost * rateCost + overspeedRatio * overspeedRatio) * dt;
            state.energyJoules += electricalPower * dt;

            return new WindTurbineSample
            {
                timeSeconds = timeSeconds,
                windSpeed = windSpeed,
                rotorSpeedRpm = state.rotorSpeedRadPerSec * 60f / (Mathf.PI * 2f),
                tipSpeedRatio = tipSpeedRatio,
                bladePitchDegrees = state.bladePitchDegrees,
                generatorTorqueNm = state.generatorTorqueNm,
                powerCoefficient = loads.powerCoefficient,
                thrustCoefficient = loads.thrustCoefficient,
                aerodynamicPower = aerodynamicPower,
                electricalPower = electricalPower,
                aerodynamicTorqueNm = aerodynamicTorque,
                thrustNewtons = thrust,
                aerodynamicModelName = plant.aerodynamicModel.ToString(),
                foreAftAngleDegrees = state.foreAftAngleRad * Mathf.Rad2Deg,
                foreAftRateDegreesPerSecond = state.foreAftRateRadPerSec * Mathf.Rad2Deg,
                stabilityMargin = 1f - tiltRatio,
                energyKWh = state.energyJoules / 3600000f,
                stabilityCost = state.stabilityCost,
                overspeedSeconds = state.overspeedSeconds
            };
        }

        public static CoDesignResult Simulate(
            WindTurbinePlantParameters plant,
            WindTurbineControllerSettings controller,
            WindProfileSettings wind,
            float durationSeconds,
            float dt)
        {
            WindTurbineState state = WindTurbineState.CreateInitial(plant);
            WindTurbineControllerMemory memory = new WindTurbineControllerMemory();
            WindTurbineSample sample = default;
            float maxAbsTilt = 0f;
            float maxRotorSpeed = 0f;
            float powerIntegral = 0f;
            int steps = Mathf.Max(1, Mathf.CeilToInt(durationSeconds / Mathf.Max(0.001f, dt)));

            for (int i = 0; i < steps; i++)
            {
                float time = i * dt;
                sample = Step(ref state, plant, controller, wind, memory, time, dt);
                maxAbsTilt = Mathf.Max(maxAbsTilt, Mathf.Abs(sample.foreAftAngleDegrees));
                maxRotorSpeed = Mathf.Max(maxRotorSpeed, sample.rotorSpeedRpm);
                powerIntegral += sample.electricalPower * dt;
            }

            return new CoDesignResult
            {
                energyKWh = sample.energyKWh,
                stabilityCost = state.stabilityCost,
                maxAbsTiltDegrees = maxAbsTilt,
                maxRotorSpeedRpm = maxRotorSpeed,
                averagePowerKw = powerIntegral / Mathf.Max(0.001f, durationSeconds) / 1000f
            };
        }

        private static WindTurbineControl ComputeControl(
            WindTurbineState state,
            WindTurbinePlantParameters plant,
            WindTurbineControllerSettings controller,
            WindTurbineControllerMemory memory,
            float windSpeed,
            float dt)
        {
            float omega = Mathf.Max(0.05f, state.rotorSpeedRadPerSec);
            float ratedOmega = Mathf.Max(0.1f, plant.RatedRotorSpeedRadPerSec);
            float targetTorque = Mathf.Clamp(plant.Region2TorqueGain * omega * omega, 0f, plant.maxGeneratorTorque);
            float targetPitch = controller.minPitchDegrees;

            bool outsideWindWindow = windSpeed < controller.cutInWindSpeed || windSpeed > controller.cutOutWindSpeed;
            if (outsideWindWindow)
            {
                if (windSpeed > controller.cutOutWindSpeed && controller.featherAboveCutOut)
                {
                    targetPitch = controller.maxPitchDegrees;
                }

                return new WindTurbineControl
                {
                    targetGeneratorTorqueNm = 0f,
                    targetBladePitchDegrees = targetPitch
                };
            }

            if (controller.mode != ControllerMode.GreedyCpTracking)
            {
                float estimatedPower = targetTorque * omega;
                bool aboveRated = omega > ratedOmega || estimatedPower > plant.ratedPower;
                if (aboveRated)
                {
                    targetTorque = Mathf.Clamp(plant.ratedPower / omega, 0f, plant.maxGeneratorTorque);
                    float speedError = omega - ratedOmega;
                    memory.pitchIntegral = Mathf.Clamp(
                        memory.pitchIntegral + speedError * dt,
                        -controller.integralLimit,
                        controller.integralLimit);
                    targetPitch = controller.minPitchDegrees
                        + controller.pitchKp * speedError
                        + controller.pitchKi * memory.pitchIntegral;
                }
                else
                {
                    memory.pitchIntegral = Mathf.MoveTowards(memory.pitchIntegral, 0f, dt);
                }
            }

            if (controller.mode == ControllerMode.StabilityLimited)
            {
                float stabilityLimitRad = Mathf.Max(0.001f, plant.stabilityLimitDegrees * Mathf.Deg2Rad);
                float tiltRatio = Mathf.Abs(state.foreAftAngleRad) / stabilityLimitRad;
                float startRatio = Mathf.Clamp01(controller.stabilityFeedbackStartRatio);

                if (tiltRatio > startRatio)
                {
                    float feedbackRatio = Mathf.InverseLerp(startRatio, 1f, tiltRatio);
                    targetPitch += controller.stabilityPitchGain * feedbackRatio;
                    targetPitch += controller.tiltRatePitchGain * Mathf.Abs(state.foreAftRateRadPerSec * Mathf.Rad2Deg);
                    targetTorque *= Mathf.Clamp01(1f - controller.stabilityTorqueDerateGain * feedbackRatio);
                }
            }

            return new WindTurbineControl
            {
                targetGeneratorTorqueNm = Mathf.Clamp(targetTorque, 0f, plant.maxGeneratorTorque),
                targetBladePitchDegrees = Mathf.Clamp(targetPitch, controller.minPitchDegrees, controller.maxPitchDegrees)
            };
        }
    }
}
