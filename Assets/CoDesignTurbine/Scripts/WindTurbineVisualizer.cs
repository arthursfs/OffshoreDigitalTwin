using UnityEngine;

namespace CoDesignTurbine
{
    public class WindTurbineVisualizer : MonoBehaviour
    {
        public WindTurbineSimulation simulation;

        [Header("Transforms")]
        public Transform rotor;
        public Transform towerOrPlatformTop;
        public Transform[] bladePitchPivots;

        [Header("Local axes")]
        public Vector3 rotorAxis = Vector3.forward;
        public Vector3 foreAftAxis = Vector3.right;
        public Vector3 bladePitchAxis = Vector3.right;

        [Header("Display scale")]
        [Min(0f)] public float foreAftAngleVisualScale = 1f;

        private Quaternion towerBaseRotation;
        private Quaternion[] bladeBaseRotations;

        private void Awake()
        {
            if (simulation == null)
            {
                simulation = FindAnyObjectByType<WindTurbineSimulation>();
            }

            if (towerOrPlatformTop != null)
            {
                towerBaseRotation = towerOrPlatformTop.localRotation;
            }

            if (bladePitchPivots != null)
            {
                bladeBaseRotations = new Quaternion[bladePitchPivots.Length];
                for (int i = 0; i < bladePitchPivots.Length; i++)
                {
                    bladeBaseRotations[i] = bladePitchPivots[i] != null
                        ? bladePitchPivots[i].localRotation
                        : Quaternion.identity;
                }
            }
        }

        private void Update()
        {
            if (simulation == null)
            {
                return;
            }

            WindTurbineState state = simulation.State;
            WindTurbineSample sample = simulation.LastSample;

            if (rotor != null)
            {
                rotor.Rotate(rotorAxis.normalized, state.rotorSpeedRadPerSec * Mathf.Rad2Deg * Time.deltaTime, Space.Self);
            }

            if (towerOrPlatformTop != null)
            {
                float angle = sample.foreAftAngleDegrees * foreAftAngleVisualScale;
                towerOrPlatformTop.localRotation = towerBaseRotation * Quaternion.AngleAxis(angle, foreAftAxis.normalized);
            }

            if (bladePitchPivots == null || bladeBaseRotations == null)
            {
                return;
            }

            for (int i = 0; i < bladePitchPivots.Length; i++)
            {
                Transform pivot = bladePitchPivots[i];
                if (pivot == null)
                {
                    continue;
                }

                pivot.localRotation = bladeBaseRotations[i] * Quaternion.AngleAxis(state.bladePitchDegrees, bladePitchAxis.normalized);
            }
        }
    }
}
