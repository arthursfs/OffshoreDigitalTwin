using System.IO;
using UnityEngine;

namespace CoDesignTurbine.MOST
{
    public static class MostHybridSparAutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateSceneBuilderIfMissing()
        {
            DisableOpenFastPreviewComponents();

            if (Object.FindAnyObjectByType<MostHybridSparSceneBuilder>() != null)
            {
                return;
            }

            if (Object.FindAnyObjectByType<MostVolturnUsSceneBuilder>() != null)
            {
                return;
            }

            if (!Directory.Exists(ResolveProjectPath("OpenFASTCases/HybridSparUnity/geometry/MOSTHybridSpar")))
            {
                return;
            }

            GameObject root = new GameObject("MOST_HybridSpar");
            MostHybridSparSceneBuilder builder = root.AddComponent<MostHybridSparSceneBuilder>();
            builder.loadOnStart = true;
            builder.setupCameraAndLight = true;
            builder.createWaterPlane = true;
        }

        private static void DisableOpenFastPreviewComponents()
        {
            MonoBehaviour[] behaviours = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;
                if (typeName == "OpenFastTelemetryHud" || typeName == "OpenFastPlaybackDriver")
                {
                    behaviour.enabled = false;
                    behaviour.gameObject.SetActive(false);
                }
            }
        }

        private static string ResolveProjectPath(string path)
        {
            DirectoryInfo assetsDirectory = new DirectoryInfo(Application.dataPath);
            string projectRoot = assetsDirectory.Parent != null ? assetsDirectory.Parent.FullName : Application.dataPath;
            return Path.GetFullPath(Path.Combine(projectRoot, path.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
