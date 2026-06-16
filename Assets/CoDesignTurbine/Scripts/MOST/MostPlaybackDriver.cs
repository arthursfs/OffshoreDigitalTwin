using System.IO;
using UnityEngine;

namespace CoDesignTurbine.MOST
{
    public class MostPlaybackDriver : MonoBehaviour
    {
        [Header("CSV")]
        public string csvFilePath = "OpenFASTCases/HybridSparUnity/most/outputs/most_unity.csv";
        public MostChannelMap channelMap = new MostChannelMap();
        public bool loadOnStart;

        [Header("Playback")]
        public bool playOnLoad = true;
        public bool loop = true;
        [Min(0.01f)] public float playbackSpeed = 1f;

        [Header("Scene transforms")]
        public Transform platformTransform;
        public Transform rotorTransform;
        public Transform[] bladePitchPivots;

        [Header("Local axes")]
        public Vector3 rotorAxis = Vector3.forward;
        public Vector3 bladePitchAxis = Vector3.right;
        public bool integrateRotorSpeedForVisuals = true;

        [Header("Motion scaling")]
        [Min(0f)] public float platformTranslationScale = 1f;
        [Min(0f)] public float platformRotationScale = 1f;

        public MostOutputSeries Series { get; private set; }
        public MostPlaybackSample LastSample { get; private set; }
        public float PlaybackTimeSeconds { get; private set; }
        public bool IsPlaying { get; private set; }

        private Vector3 platformBasePosition;
        private Quaternion platformBaseRotation;
        private Quaternion rotorBaseRotation;
        private Quaternion[] bladeBaseRotations;
        private bool rotorAngleInitialized;
        private float integratedRotorAngleDegrees;

        private void Awake()
        {
            CaptureBaseTransforms();
        }

        private void CaptureBaseTransforms()
        {
            if (platformTransform != null)
            {
                platformBasePosition = platformTransform.localPosition;
                platformBaseRotation = platformTransform.localRotation;
            }
            else
            {
                platformBasePosition = Vector3.zero;
                platformBaseRotation = Quaternion.identity;
            }

            if (rotorTransform != null)
            {
                rotorBaseRotation = rotorTransform.localRotation;
            }
            else
            {
                rotorBaseRotation = Quaternion.identity;
            }

            if (bladePitchPivots != null)
            {
                bladeBaseRotations = new Quaternion[bladePitchPivots.Length];
                for (int i = 0; i < bladePitchPivots.Length; i++)
                {
                    bladeBaseRotations[i] = bladePitchPivots[i] != null ? bladePitchPivots[i].localRotation : Quaternion.identity;
                }
            }
            else
            {
                bladeBaseRotations = null;
            }
        }

        private void Start()
        {
            if (loadOnStart)
            {
                LoadCsv();
            }
        }

        private void Update()
        {
            if (!IsPlaying || Series == null || Series.SampleCount == 0)
            {
                return;
            }

            float deltaSeconds = Time.deltaTime * playbackSpeed;
            PlaybackTimeSeconds += deltaSeconds;
            if (PlaybackTimeSeconds > Series.DurationSeconds)
            {
                PlaybackTimeSeconds = loop ? 0f : Series.DurationSeconds;
                IsPlaying = loop;
            }

            ApplySample(Series.Sample(channelMap, PlaybackTimeSeconds), deltaSeconds);
        }

        public void LoadCsv()
        {
            string path = ResolveProjectPath(csvFilePath);
            SetSeries(MostCsvOutputReader.Read(path, channelMap.timeSeconds));
        }

        public void SetSeries(MostOutputSeries series)
        {
            CaptureBaseTransforms();
            Series = series;
            PlaybackTimeSeconds = 0f;
            IsPlaying = playOnLoad;
            rotorAngleInitialized = false;
            integratedRotorAngleDegrees = 0f;

            if (Series != null && Series.SampleCount > 0)
            {
                ApplySample(Series.Sample(channelMap, 0f));
            }
        }

        private void ApplySample(MostPlaybackSample sample)
        {
            ApplySample(sample, 0f);
        }

        private void ApplySample(MostPlaybackSample sample, float deltaSeconds)
        {
            LastSample = sample;

            if (platformTransform != null)
            {
                Vector3 translation = new Vector3(sample.swayMeters, sample.heaveMeters, sample.surgeMeters);
                platformTransform.localPosition = platformBasePosition + translation * platformTranslationScale;
                platformTransform.localRotation = platformBaseRotation
                    * Quaternion.Euler(
                        sample.pitchDegrees * platformRotationScale,
                        sample.yawDegrees * platformRotationScale,
                        -sample.rollDegrees * platformRotationScale);
            }

            if (rotorTransform != null)
            {
                if (integrateRotorSpeedForVisuals)
                {
                    if (!rotorAngleInitialized)
                    {
                        integratedRotorAngleDegrees = float.IsNaN(sample.rotorAzimuthDegrees) ? 0f : sample.rotorAzimuthDegrees;
                        rotorAngleInitialized = true;
                    }
                    else
                    {
                        integratedRotorAngleDegrees += sample.rotorSpeedRpm * 6f * deltaSeconds;
                    }

                    rotorTransform.localRotation = rotorBaseRotation * Quaternion.AngleAxis(integratedRotorAngleDegrees, rotorAxis.normalized);
                }
                else if (float.IsNaN(sample.rotorAzimuthDegrees))
                {
                    rotorTransform.Rotate(rotorAxis.normalized, sample.rotorSpeedRpm * 6f * Time.deltaTime, Space.Self);
                }
                else
                {
                    rotorTransform.localRotation = rotorBaseRotation * Quaternion.AngleAxis(sample.rotorAzimuthDegrees, rotorAxis.normalized);
                }
            }

            if (bladePitchPivots == null || bladeBaseRotations == null)
            {
                return;
            }

            for (int i = 0; i < bladePitchPivots.Length; i++)
            {
                if (bladePitchPivots[i] != null)
                {
                    bladePitchPivots[i].localRotation = bladeBaseRotations[i] * Quaternion.AngleAxis(sample.bladePitchDegrees, bladePitchAxis.normalized);
                }
            }
        }

        private static string ResolveProjectPath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return Path.GetFullPath(path);
            }

            DirectoryInfo assetsDirectory = new DirectoryInfo(Application.dataPath);
            string projectRoot = assetsDirectory.Parent != null ? assetsDirectory.Parent.FullName : Application.dataPath;
            return Path.GetFullPath(Path.Combine(projectRoot, path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
