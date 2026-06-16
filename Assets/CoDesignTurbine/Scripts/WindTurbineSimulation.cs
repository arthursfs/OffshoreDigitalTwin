using UnityEngine;

namespace CoDesignTurbine
{
    public class WindTurbineSimulation : MonoBehaviour
    {
        public WindTurbinePlantParameters plant = new WindTurbinePlantParameters();
        public WindTurbineControllerSettings controller = new WindTurbineControllerSettings();
        public WindProfileSettings wind = new WindProfileSettings();

        [Header("Runtime")]
        [Min(0.01f)] public float simulationSpeed = 1f;
        [Range(1, 20)] public int substeps = 4;
        public bool paused;
        public bool resetOnAwake = true;

        public WindTurbineState State => state;
        public WindTurbineSample LastSample => lastSample;
        public float SimulationTimeSeconds => simulationTimeSeconds;

        private WindTurbineState state;
        private WindTurbineSample lastSample;
        private WindTurbineControllerMemory memory = new WindTurbineControllerMemory();
        private float simulationTimeSeconds;

        private void Awake()
        {
            if (resetOnAwake)
            {
                ResetSimulation();
            }
        }

        private void FixedUpdate()
        {
            if (paused)
            {
                return;
            }

            float scaledDt = Time.fixedDeltaTime * Mathf.Max(0.01f, simulationSpeed);
            int safeSubsteps = Mathf.Max(1, substeps);
            float dt = scaledDt / safeSubsteps;

            for (int i = 0; i < safeSubsteps; i++)
            {
                lastSample = ReducedOrderWindTurbineModel.Step(
                    ref state,
                    plant,
                    controller,
                    wind,
                    memory,
                    simulationTimeSeconds,
                    dt);
                simulationTimeSeconds += dt;
            }
        }

        public void ResetSimulation()
        {
            state = WindTurbineState.CreateInitial(plant);
            lastSample = default;
            memory.Reset();
            simulationTimeSeconds = 0f;
        }

        public CoDesignResult RunOffline(float durationSeconds, float dt)
        {
            return ReducedOrderWindTurbineModel.Simulate(
                plant.Clone(),
                controller.Clone(),
                wind.Clone(),
                durationSeconds,
                dt);
        }
    }
}

