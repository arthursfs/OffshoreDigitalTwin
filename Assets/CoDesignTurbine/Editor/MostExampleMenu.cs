#if UNITY_EDITOR
using CoDesignTurbine.MOST;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CoDesignTurbine.EditorTools
{
    public static class MostExampleMenu
    {
        [MenuItem("Tools/CoDesign Turbine/Create MOST VolturnUS Example")]
        public static void CreateVolturnUsExample()
        {
            DisableCompetingPreviewObjects();

            GameObject root = GameObject.Find("MOST_VolturnUS");
            if (root == null)
            {
                root = new GameObject("MOST_VolturnUS");
                Undo.RegisterCreatedObjectUndo(root, "Create MOST VolturnUS Example");
            }
            else
            {
                root.SetActive(true);
                Undo.RecordObject(root, "Configure MOST VolturnUS Example");
            }

            MostVolturnUsSceneBuilder builder = root.GetComponent<MostVolturnUsSceneBuilder>();
            if (builder == null)
            {
                builder = root.AddComponent<MostVolturnUsSceneBuilder>();
            }

            builder.loadOnStart = true;
            builder.addPlayback = true;
            builder.useProceduralTurbineVisuals = true;
            builder.showComparisonHud = false;
            builder.setupCameraAndLight = true;
            builder.createWaterPlane = true;
            builder.Build();

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
        }

        private static void DisableCompetingPreviewObjects()
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
                if (typeName == "MostHybridSparSceneBuilder"
                    || typeName == "OpenFastTelemetryHud"
                    || typeName == "OpenFastPlaybackDriver")
                {
                    behaviour.enabled = false;
                    behaviour.gameObject.SetActive(false);
                }
            }
        }
    }
}
#endif
