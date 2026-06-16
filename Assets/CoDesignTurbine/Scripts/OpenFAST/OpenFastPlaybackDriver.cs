using UnityEngine;

namespace CoDesignTurbine.OpenFAST
{
    public class OpenFastPlaybackDriver : MonoBehaviour
    {
        public OpenFastCaseConfig caseConfig;

        [Header("Output loading")]
        public string outputFilePath;
        public bool loadOnStart;

        [Header("Playback")]
        public bool playOnLoad = true;
        public bool loop = true;
        [Min(0.01f)] public float playbackSpeed = 1f;

        [Header("Scene transforms")]
        public Transform platformTransform;
        public Transform towerTopTransform;
        public Transform rotorTransform;
        public Transform[] bladePitchPivots;

        [Header("Local axes")]
        public Vector3 rotorAxis = Vector3.forward;
        public Vector3 bladePitchAxis = Vector3.right;

        [Header("Motion scaling")]
        [Min(0f)] public float platformTranslationScale = 1f;
        [Min(0f)] public float towerTopDisplacementScale = 1f;

        public OpenFastOutputSeries Series { get; private set; }
        public OpenFastPlaybackSample LastSample { get; private set; }
        public float PlaybackTimeSeconds { get; private set; }
        public bool IsPlaying { get; private set; }

        private OpenFastChannelMap channelMap;
        private Quaternion platformBaseRotation;
        private Vector3 platformBasePosition;
        private Vector3 towerTopBasePosition;
        private Quaternion rotorBaseRotation;
        private Quaternion[] bladeBaseRotations;

        private void Awake()
        {
            channelMap = caseConfig != null ? caseConfig.channelMap : new OpenFastChannelMap();

            if (platformTransform != null)
            {
                platformBaseRotation = platformTransform.localRotation;
                platformBasePosition = platformTransform.localPosition;
            }

            if (towerTopTransform != null)
            {
                towerTopBasePosition = towerTopTransform.localPosition;
            }

            if (rotorTransform != null)
            {
                rotorBaseRotation = rotorTransform.localRotation;
            }

            if (bladePitchPivots != null)
            {
                bladeBaseRotations = new Quaternion[bladePitchPivots.Length];
                for (int i = 0; i < bladePitchPivots.Length; i++)
                {
                    bladeBaseRotations[i] = bladePitchPivots[i] != null ? bladePitchPivots[i].localRotation : Quaternion.identity;
                }
            }
        }

        private void Start()
        {
            if (loadOnStart)
            {
                LoadConfiguredOutput();
            }
        }

        private void Update()
        {
            if (!IsPlaying || Series == null || Series.SampleCount == 0)
            {
                return;
            }

            PlaybackTimeSeconds += Time.deltaTime * playbackSpeed;
            if (PlaybackTimeSeconds > Series.DurationSeconds)
            {
                PlaybackTimeSeconds = loop ? 0f : Series.DurationSeconds;
                IsPlaying = loop;
            }

            ApplySample(Series.Sample(channelMap, PlaybackTimeSeconds));
        }

        public void LoadConfiguredOutput()
        {
            string path = outputFilePath;
            if (string.IsNullOrWhiteSpace(path) && caseConfig != null)
            {
                path = caseConfig.ResolveOutputPath(caseConfig.ResolveCaseRoot());
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                Debug.LogWarning("No OpenFAST output file path configured.");
                return;
            }

            SetSeries(OpenFastAsciiOutputReader.Read(OpenFastCaseConfig.ResolvePath(path, OpenFastCaseConfig.ProjectRoot)), channelMap);
        }

        public void SetSeries(OpenFastOutputSeries series, OpenFastChannelMap map)
        {
            Series = series;
            channelMap = map != null ? map : new OpenFastChannelMap();
            PlaybackTimeSeconds = 0f;
            IsPlaying = playOnLoad;

            if (Series != null && Series.SampleCount > 0)
            {
                ApplySample(Series.Sample(channelMap, 0f));
            }
        }

        private void ApplySample(OpenFastPlaybackSample sample)
        {
            LastSample = sample;

            if (platformTransform != null)
            {
                Vector3 translation = new Vector3(sample.platformSurgeMeters, sample.platformHeaveMeters, sample.platformSwayMeters);
                platformTransform.localPosition = platformBasePosition + translation * platformTranslationScale;
                platformTransform.localRotation = platformBaseRotation
                    * Quaternion.Euler(sample.platformPitchDegrees, sample.platformYawDegrees, -sample.platformRollDegrees);
            }

            if (towerTopTransform != null)
            {
                Vector3 towerDisplacement = new Vector3(sample.towerTopSideSideMeters, 0f, sample.towerTopForeAftMeters);
                towerTopTransform.localPosition = towerTopBasePosition + towerDisplacement * towerTopDisplacementScale;
            }

            if (rotorTransform != null)
            {
                if (float.IsNaN(sample.rotorAzimuthDegrees))
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
                Transform pivot = bladePitchPivots[i];
                if (pivot == null)
                {
                    continue;
                }

                float pitch = sample.bladePitch1Degrees;
                if (i == 1)
                {
                    pitch = sample.bladePitch2Degrees;
                }
                else if (i == 2)
                {
                    pitch = sample.bladePitch3Degrees;
                }

                pivot.localRotation = bladeBaseRotations[i] * Quaternion.AngleAxis(pitch, bladePitchAxis.normalized);
            }
        }
    }
}
