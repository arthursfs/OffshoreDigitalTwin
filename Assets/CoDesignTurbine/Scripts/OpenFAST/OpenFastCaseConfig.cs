using System;
using System.IO;
using UnityEngine;

namespace CoDesignTurbine.OpenFAST
{
    [CreateAssetMenu(menuName = "CoDesign Turbine/OpenFAST Case Config")]
    public class OpenFastCaseConfig : ScriptableObject
    {
        [Header("Executable")]
        [Tooltip("Full path to openfast.exe, or just openfast if it is on PATH.")]
        public string openFastExecutablePath = "openfast";

        [Header("Case paths")]
        [Tooltip("Absolute path, or path relative to the Unity project root.")]
        public string caseRootDirectory = "OpenFASTCases/HybridSparUnity";

        [Tooltip("Main .fst file relative to the case root.")]
        public string fstFileRelativePath = "openfast/main.fst";

        [Tooltip("ASCII .out file relative to the case root or run root. Configure OpenFAST to write text output, not only .outb.")]
        public string outputFileRelativePath = "openfast/main.out";

        [Header("Run workspace")]
        public bool copyCaseBeforeRun = true;
        public string generatedRunsDirectory = "OpenFASTRuns";
        public string runLabel = "run_latest";
        public bool timestampRunFolders = true;

        [Header("OpenFAST output channels")]
        public OpenFastChannelMap channelMap = new OpenFastChannelMap();

        public string ResolveCaseRoot()
        {
            return ResolvePath(caseRootDirectory, ProjectRoot);
        }

        public string ResolveExecutable()
        {
            if (string.IsNullOrWhiteSpace(openFastExecutablePath))
            {
                return "openfast";
            }

            string expanded = Environment.ExpandEnvironmentVariables(openFastExecutablePath.Trim());
            bool looksLikePath = expanded.IndexOfAny(new[] { '\\', '/' }) >= 0 || Path.IsPathRooted(expanded);
            return looksLikePath ? ResolvePath(expanded, ProjectRoot) : expanded;
        }

        public string ResolveGeneratedRunsRoot()
        {
            return ResolvePath(generatedRunsDirectory, Application.persistentDataPath);
        }

        public string ResolveFstPath(string root)
        {
            return Path.GetFullPath(Path.Combine(root, NormalizeRelativePath(fstFileRelativePath)));
        }

        public string ResolveOutputPath(string root)
        {
            return Path.GetFullPath(Path.Combine(root, NormalizeRelativePath(outputFileRelativePath)));
        }

        public static string ProjectRoot
        {
            get
            {
                string dataPath = Application.dataPath;
                DirectoryInfo assetsDirectory = new DirectoryInfo(dataPath);
                return assetsDirectory.Parent != null ? assetsDirectory.Parent.FullName : dataPath;
            }
        }

        public static string ResolvePath(string path, string relativeRoot)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string expanded = Environment.ExpandEnvironmentVariables(path.Trim());
            if (Path.IsPathRooted(expanded))
            {
                return Path.GetFullPath(expanded);
            }

            return Path.GetFullPath(Path.Combine(relativeRoot, NormalizeRelativePath(expanded)));
        }

        public static string NormalizeRelativePath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }
    }

    [Serializable]
    public class OpenFastChannelMap
    {
        public string timeSeconds = "Time";
        public string windSpeed = "Wind1VelX";
        public string rotorSpeedRpm = "RotSpeed";
        public string rotorAzimuthDegrees = "Azimuth";
        public string generatorPowerKw = "GenPwr";
        public string generatorTorque = "GenTq";
        public string bladePitch1Degrees = "BldPitch1";
        public string bladePitch2Degrees = "BldPitch2";
        public string bladePitch3Degrees = "BldPitch3";
        public string platformSurgeMeters = "PtfmSurge";
        public string platformSwayMeters = "PtfmSway";
        public string platformHeaveMeters = "PtfmHeave";
        public string platformRollDegrees = "PtfmRoll";
        public string platformPitchDegrees = "PtfmPitch";
        public string platformYawDegrees = "PtfmYaw";
        public string towerTopForeAftMeters = "TTDspFA";
        public string towerTopSideSideMeters = "TTDspSS";
        public string towerBaseMy = "TwrBsMyt";
    }
}
