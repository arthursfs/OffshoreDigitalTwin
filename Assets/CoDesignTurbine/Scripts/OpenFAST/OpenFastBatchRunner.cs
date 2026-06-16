using System;
using UnityEngine;

namespace CoDesignTurbine.OpenFAST
{
    public class OpenFastBatchRunner : MonoBehaviour
    {
        public OpenFastCaseConfig caseConfig;
        public bool runOnStart;
        public KeyCode runKey = KeyCode.F8;
        public bool loadOutputAfterRun = true;
        public OpenFastPlaybackDriver playbackTarget;

        public OpenFastRunResult LastRunResult { get; private set; }

        private void Start()
        {
            if (runOnStart)
            {
                RunCase();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(runKey))
            {
                RunCase();
            }
        }

        public void RunCase()
        {
            if (caseConfig == null)
            {
                Debug.LogWarning("No OpenFAST case config assigned.");
                return;
            }

            try
            {
                LastRunResult = OpenFastRunUtility.Run(caseConfig, caseConfig.runLabel, null, loadOutputAfterRun);
                if (LastRunResult.outputSeries != null && playbackTarget != null)
                {
                    playbackTarget.SetSeries(LastRunResult.outputSeries, caseConfig.channelMap);
                }

                if (!string.IsNullOrWhiteSpace(LastRunResult.standardError))
                {
                    Debug.LogWarning(LastRunResult.standardError);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("OpenFAST run failed: " + ex.Message);
            }
        }
    }
}

