using System.IO;
using CoDesignTurbine.Geometry;
using UnityEngine;

namespace CoDesignTurbine.MOST
{
    public class MostHybridSparSceneBuilder : MonoBehaviour
    {
        [Header("MOST geometry folder")]
        public string geometryDirectory = "OpenFASTCases/HybridSparUnity/geometry/MOSTHybridSpar";
        public string bladeStl = "IEA15MW_Blade.STL";
        public string hubStl = "IEA15MW_Hub.STL";
        public string towerStl = "IEA15MW_Tower.STL";
        public string nacelleStl = "IEA15MW_nacelle.STL";

        [Header("Playback")]
        public string mostCsvPath = "OpenFASTCases/HybridSparUnity/most/outputs/sample_most_unity.csv";
        public bool addPlayback = true;
        public bool loadOnStart = true;

        [Header("Scene layout")]
        public float geometryScale = 1f;
        public float hubHeight = 90f;
        public float rotorRadius = 120f;
        public float sparDraft = 115f;
        public float sparRadius = 9f;
        public float sparTopAboveWater = 8f;
        public float toroidRadius = 24f;
        public float toroidTubeRadius = 3.5f;
        public float toroidCenterBelowWater = 5f;

        [Header("Materials")]
        public Color towerColor = new Color(0.86f, 0.88f, 0.9f);
        public Color nacelleColor = new Color(0.78f, 0.80f, 0.84f);
        public Color bladeColor = new Color(0.95f, 0.95f, 0.92f);
        public Color platformColor = new Color(0.72f, 0.74f, 0.78f);
        public Color waterColor = new Color(0.12f, 0.38f, 0.62f, 0.65f);

        [Header("Auto view")]
        public bool setupCameraAndLight = true;
        public bool createWaterPlane = true;

        private void Start()
        {
            DisableOpenFastPreviewComponents();
            Build();
        }

        [ContextMenu("Build MOST HybridSpar Scene")]
        public void Build()
        {
            ClearPreviousBuild();

            Transform platform = new GameObject("MOST_HybridSpar_Platform").transform;
            platform.SetParent(transform, false);

            Material towerMaterial = CreateMaterial("TowerMaterial", towerColor);
            Material nacelleMaterial = CreateMaterial("NacelleMaterial", nacelleColor);
            Material bladeMaterial = CreateMaterial("BladeMaterial", bladeColor);
            Material platformMaterial = CreateMaterial("PlatformMaterial", platformColor);

            CreateSpar(platform, platformMaterial);
            CreateToroid(platform, platformMaterial);
            if (createWaterPlane)
            {
                CreateWaterPlane();
            }

            Transform tower = CreateStlObject("IEA15MW_Tower", towerStl, platform, towerMaterial);
            tower.localPosition = Vector3.zero;
            tower.localScale = Vector3.one * geometryScale;

            Transform nacelle = CreateStlObject("IEA15MW_Nacelle", nacelleStl, platform, nacelleMaterial);
            nacelle.localPosition = new Vector3(0f, hubHeight, 0f);
            nacelle.localScale = Vector3.one * geometryScale;

            Transform rotorHub = CreateStlObject("IEA15MW_Hub", hubStl, nacelle, nacelleMaterial);
            rotorHub.localPosition = new Vector3(0f, 0f, 4f);
            rotorHub.localScale = Vector3.one * geometryScale;

            Transform[] bladePivots = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                Transform pivot = new GameObject("BladePitchPivot_" + (i + 1)).transform;
                pivot.SetParent(rotorHub, false);
                pivot.localRotation = Quaternion.AngleAxis(i * 120f, Vector3.forward);
                bladePivots[i] = pivot;

                CreateRotorBlade("IEA15MW_Blade_" + (i + 1), pivot, bladeMaterial);
            }

            if (addPlayback)
            {
                MostPlaybackDriver playback = gameObject.GetComponent<MostPlaybackDriver>();
                if (playback == null)
                {
                    playback = gameObject.AddComponent<MostPlaybackDriver>();
                }

                playback.csvFilePath = mostCsvPath;
                playback.platformTransform = platform;
                playback.rotorTransform = rotorHub;
                playback.bladePitchPivots = bladePivots;
                playback.bladePitchAxis = Vector3.up;
                playback.integrateRotorSpeedForVisuals = true;
                playback.loadOnStart = loadOnStart;
                playback.playOnLoad = true;
                playback.loop = true;
                if (Application.isPlaying && loadOnStart)
                {
                    playback.LoadCsv();
                }

                if (gameObject.GetComponent<MostTelemetryHud>() == null)
                {
                    gameObject.AddComponent<MostTelemetryHud>();
                }
            }

            if (setupCameraAndLight)
            {
                SetupCameraAndLight();
            }
        }

        private void ClearPreviousBuild()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private void CreateWaterPlane()
        {
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "WaterPlane";
            water.transform.SetParent(transform, false);
            water.transform.localPosition = Vector3.zero;
            water.transform.localScale = new Vector3(80f, 1f, 80f);

            Material material = CreateMaterial("WaterMaterial", waterColor);
            water.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void CreateSpar(Transform parent, Material material)
        {
            GameObject spar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spar.name = "Procedural_Spar15MW";
            spar.transform.SetParent(parent, false);
            spar.transform.localScale = new Vector3(sparRadius * 2f, sparDraft * 0.5f, sparRadius * 2f);
            spar.transform.localPosition = new Vector3(0f, -sparDraft * 0.5f + sparTopAboveWater, 0f);
            spar.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void CreateToroid(Transform parent, Material material)
        {
            GameObject toroid = new GameObject("Procedural_Toroid");
            toroid.transform.SetParent(parent, false);
            toroid.transform.localPosition = new Vector3(0f, -toroidCenterBelowWater, 0f);
            MeshFilter filter = toroid.AddComponent<MeshFilter>();
            filter.sharedMesh = ProceduralOffshoreGeometry.CreateTorus(toroidRadius, toroidTubeRadius, 48, 12);
            MeshRenderer renderer = toroid.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }

        private Transform CreateStlObject(string objectName, string fileName, Transform parent, Material material)
        {
            GameObject meshObject = new GameObject(objectName);
            meshObject.transform.SetParent(parent, false);
            MeshFilter filter = meshObject.AddComponent<MeshFilter>();
            filter.sharedMesh = StlMeshLoader.Load(Path.Combine(ResolveProjectPath(geometryDirectory), fileName), true);
            MeshRenderer renderer = meshObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return meshObject.transform;
        }

        private Transform CreateRotorBlade(string objectName, Transform parent, Material material)
        {
            Transform blade = CreateStlObject(objectName, bladeStl, parent, material);
            MeshFilter filter = blade.GetComponent<MeshFilter>();
            float rootOffset = 0f;
            if (filter != null && filter.sharedMesh != null)
            {
                rootOffset = -filter.sharedMesh.bounds.min.y;
            }

            blade.localRotation = Quaternion.identity;
            blade.localPosition = new Vector3(0f, rootOffset, 0f);
            blade.localScale = Vector3.one * geometryScale;
            return blade;
        }

        private static Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            Material material = new Material(shader);
            material.name = name;
            material.color = color;
            if (color.a < 0.99f)
            {
                ConfigureTransparentMaterial(material, color);
            }
            return material;
        }

        private static void ConfigureTransparentMaterial(Material material, Color color)
        {
            material.color = color;

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_AlphaClip", 0f);
                material.renderQueue = 3000;
            }

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3f);
            }

            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        private void SetupCameraAndLight()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.position = new Vector3(210f, 95f, -260f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 25f, 0f) - camera.transform.position, Vector3.up);
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 5000f;

            if (FindAnyObjectByType<Light>() == null)
            {
                GameObject lightObject = new GameObject("Directional Light");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            }
        }

        private static void DisableOpenFastPreviewComponents()
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
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
