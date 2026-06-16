using System.IO;
using CoDesignTurbine.Geometry;
using UnityEngine;

namespace CoDesignTurbine.MOST
{
    public class MostVolturnUsSceneBuilder : MonoBehaviour
    {
        [Header("MOST geometry folder")]
        public string geometryDirectory = "OpenFASTCases/VolturnUSUnity/geometry/MOSTVolturnUS";
        public string bladeStl = "IEA15MW_Blade.STL";
        public string hubStl = "IEA15MW_Hub.STL";
        public string towerStl = "IEA15MW_Tower.STL";
        public string nacelleStl = "IEA15MW_nacelle.STL";

        [Header("Playback and comparison")]
        public string mostCsvPath = "OpenFASTCases/VolturnUSUnity/most/outputs/sample_volturn_most_unity.csv";
        public string comparisonCsvPath = "OpenFASTCases/VolturnUSUnity/most/outputs/sample_volturn_most_fast_comparison.csv";
        public string aeroModelLabel = "Aero: MOST aeroLoadsType=1 (BEM), Unity visual playback";
        public string windModelLabel = "Wind: TurbSim turbulent, compare against FAST with same wind file/seed";
        public bool addPlayback = true;
        public bool loadOnStart = true;
        public bool showComparisonHud = false;

        [Header("Scene layout")]
        public bool useProceduralTurbineVisuals = true;
        public float geometryScale = 1f;
        public float hubHeight = 150f;
        public float towerRadius = 4f;
        public float rotorRadius = 117f;
        public float hubRadius = 3.5f;
        public float columnRadius = 6.5f;
        public float columnHeight = 42f;
        public float columnSpacing = 72f;
        public float pontoonRadius = 3.0f;

        [Header("Materials")]
        public Color towerColor = new Color(0.86f, 0.88f, 0.9f);
        public Color nacelleColor = new Color(0.78f, 0.80f, 0.84f);
        public Color bladeColor = new Color(0.95f, 0.95f, 0.92f);
        public Color platformColor = new Color(0.70f, 0.73f, 0.78f);
        public Color waterColor = new Color(0.12f, 0.38f, 0.62f, 0.65f);

        [Header("Auto view")]
        public bool setupCameraAndLight = true;
        public bool createWaterPlane = true;

        private void Start()
        {
            DisableOtherPreviewComponents();
            Build();
        }

        [ContextMenu("Build MOST VolturnUS Scene")]
        public void Build()
        {
            ClearPreviousBuild();

            Transform platform = new GameObject("MOST_VolturnUS_Platform").transform;
            platform.SetParent(transform, false);

            Material towerMaterial = CreateMaterial("VolturnTowerMaterial", towerColor);
            Material nacelleMaterial = CreateMaterial("VolturnNacelleMaterial", nacelleColor);
            Material bladeMaterial = CreateMaterial("VolturnBladeMaterial", bladeColor);
            Material platformMaterial = CreateMaterial("VolturnPlatformMaterial", platformColor);

            BuildSemiSubmersible(platform, platformMaterial);
            if (createWaterPlane)
            {
                CreateWaterPlane();
            }

            Transform rotorHub;
            Transform[] bladePivots;
            if (useProceduralTurbineVisuals)
            {
                BuildProceduralTurbine(platform, towerMaterial, nacelleMaterial, bladeMaterial, out rotorHub, out bladePivots);
            }
            else
            {
                BuildStlTurbine(platform, towerMaterial, nacelleMaterial, bladeMaterial, out rotorHub, out bladePivots);
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

                MostFastComparisonHud comparisonHud = gameObject.GetComponent<MostFastComparisonHud>();
                if (comparisonHud == null)
                {
                    comparisonHud = gameObject.AddComponent<MostFastComparisonHud>();
                }

                comparisonHud.playback = playback;
                comparisonHud.comparisonCsvPath = comparisonCsvPath;
                comparisonHud.aeroModelLabel = aeroModelLabel;
                comparisonHud.windModelLabel = windModelLabel;
                comparisonHud.loadOnStart = loadOnStart;
                comparisonHud.showHud = showComparisonHud;
                if (Application.isPlaying && loadOnStart)
                {
                    comparisonHud.LoadComparisonCsv();
                }
            }

            if (setupCameraAndLight)
            {
                SetupCameraAndLight();
            }
        }

        private void BuildSemiSubmersible(Transform parent, Material material)
        {
            Vector3 centerColumn = new Vector3(0f, -columnHeight * 0.5f + 8f, 0f);
            Vector3[] columns =
            {
                new Vector3(0f, -columnHeight * 0.5f + 6f, columnSpacing * 0.55f),
                new Vector3(-columnSpacing * 0.48f, -columnHeight * 0.5f + 6f, -columnSpacing * 0.28f),
                new Vector3(columnSpacing * 0.48f, -columnHeight * 0.5f + 6f, -columnSpacing * 0.28f)
            };

            GameObject central = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            central.name = "VolturnUS_CentralColumn";
            central.transform.SetParent(parent, false);
            central.transform.localPosition = centerColumn;
            central.transform.localScale = new Vector3(columnRadius * 2.2f, columnHeight * 0.5f, columnRadius * 2.2f);
            central.GetComponent<Renderer>().sharedMaterial = material;

            for (int i = 0; i < columns.Length; i++)
            {
                GameObject column = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                column.name = "VolturnUS_Column_" + (i + 1);
                column.transform.SetParent(parent, false);
                column.transform.localPosition = columns[i];
                column.transform.localScale = new Vector3(columnRadius * 2f, columnHeight * 0.5f, columnRadius * 2f);
                column.GetComponent<Renderer>().sharedMaterial = material;
            }

            CreatePontoon(parent, columns[0], columns[1], material, "VolturnUS_Pontoon_1");
            CreatePontoon(parent, columns[1], columns[2], material, "VolturnUS_Pontoon_2");
            CreatePontoon(parent, columns[2], columns[0], material, "VolturnUS_Pontoon_3");
            CreatePontoon(parent, centerColumn, columns[0], material, "VolturnUS_RadialPontoon_1");
            CreatePontoon(parent, centerColumn, columns[1], material, "VolturnUS_RadialPontoon_2");
            CreatePontoon(parent, centerColumn, columns[2], material, "VolturnUS_RadialPontoon_3");
        }

        private void CreatePontoon(Transform parent, Vector3 a, Vector3 b, Material material, string name)
        {
            Vector3 midpoint = (a + b) * 0.5f;
            Vector3 direction = b - a;
            GameObject pontoon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pontoon.name = name;
            pontoon.transform.SetParent(parent, false);
            pontoon.transform.localPosition = new Vector3(midpoint.x, -columnHeight + 4f, midpoint.z);
            pontoon.transform.localScale = new Vector3(pontoonRadius * 2f, direction.magnitude * 0.5f, pontoonRadius * 2f);
            pontoon.transform.localRotation = Quaternion.FromToRotation(Vector3.up, new Vector3(direction.x, 0f, direction.z).normalized);
            pontoon.GetComponent<Renderer>().sharedMaterial = material;
        }

        private void CreateWaterPlane()
        {
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "WaterPlane";
            water.transform.SetParent(transform, false);
            water.transform.localScale = new Vector3(120f, 1f, 120f);
            water.GetComponent<Renderer>().sharedMaterial = CreateMaterial("VolturnWaterMaterial", waterColor);
        }

        private void BuildProceduralTurbine(
            Transform platform,
            Material towerMaterial,
            Material nacelleMaterial,
            Material bladeMaterial,
            out Transform rotorHub,
            out Transform[] bladePivots)
        {
            float towerBase = 8f;
            float towerHeight = Mathf.Max(10f, hubHeight - towerBase);

            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tower.name = "IEA15MW_Procedural_Tower";
            tower.transform.SetParent(platform, false);
            tower.transform.localPosition = new Vector3(0f, towerBase + towerHeight * 0.5f, 0f);
            tower.transform.localScale = new Vector3(towerRadius * 2f, towerHeight * 0.5f, towerRadius * 2f);
            tower.GetComponent<Renderer>().sharedMaterial = towerMaterial;

            GameObject nacelle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nacelle.name = "IEA15MW_Procedural_Nacelle";
            nacelle.transform.SetParent(platform, false);
            nacelle.transform.localPosition = new Vector3(0f, hubHeight, -4f);
            nacelle.transform.localScale = new Vector3(9f, 7f, 20f);
            nacelle.GetComponent<Renderer>().sharedMaterial = nacelleMaterial;

            GameObject rotor = new GameObject("IEA15MW_Procedural_Rotor");
            rotor.transform.SetParent(platform, false);
            rotor.transform.localPosition = new Vector3(0f, hubHeight, 8f);
            rotorHub = rotor.transform;

            GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hub.name = "IEA15MW_Procedural_Hub";
            hub.transform.SetParent(rotorHub, false);
            hub.transform.localScale = Vector3.one * (hubRadius * 2f);
            hub.GetComponent<Renderer>().sharedMaterial = nacelleMaterial;

            GameObject spinner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spinner.name = "IEA15MW_Procedural_Spinner";
            spinner.transform.SetParent(rotorHub, false);
            spinner.transform.localRotation = Quaternion.FromToRotation(Vector3.up, Vector3.forward);
            spinner.transform.localPosition = new Vector3(0f, 0f, 1.2f);
            spinner.transform.localScale = new Vector3(hubRadius * 1.5f, 2.2f, hubRadius * 1.5f);
            spinner.GetComponent<Renderer>().sharedMaterial = nacelleMaterial;

            bladePivots = new Transform[3];
            for (int i = 0; i < bladePivots.Length; i++)
            {
                Transform pivot = new GameObject("BladePitchPivot_" + (i + 1)).transform;
                pivot.SetParent(rotorHub, false);
                pivot.localRotation = Quaternion.AngleAxis(i * 120f, Vector3.forward);
                bladePivots[i] = pivot;

                GameObject blade = new GameObject("IEA15MW_Procedural_Blade_" + (i + 1));
                blade.transform.SetParent(pivot, false);
                MeshFilter filter = blade.AddComponent<MeshFilter>();
                filter.sharedMesh = CreateProceduralBladeMesh(rotorRadius, hubRadius + 1.5f);
                MeshRenderer renderer = blade.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = bladeMaterial;
            }
        }

        private void BuildStlTurbine(
            Transform platform,
            Material towerMaterial,
            Material nacelleMaterial,
            Material bladeMaterial,
            out Transform rotorHub,
            out Transform[] bladePivots)
        {
            Transform tower = CreateStlObject("IEA15MW_Tower", towerStl, platform, towerMaterial);
            tower.localPosition = Vector3.zero;
            tower.localScale = Vector3.one * geometryScale;

            Transform nacelle = CreateStlObject("IEA15MW_Nacelle", nacelleStl, platform, nacelleMaterial);
            nacelle.localPosition = new Vector3(0f, hubHeight, 0f);
            nacelle.localScale = Vector3.one * geometryScale;

            rotorHub = CreateStlObject("IEA15MW_Hub", hubStl, nacelle, nacelleMaterial);
            rotorHub.localPosition = new Vector3(0f, 0f, 4f);
            rotorHub.localScale = Vector3.one * geometryScale;

            bladePivots = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                Transform pivot = new GameObject("BladePitchPivot_" + (i + 1)).transform;
                pivot.SetParent(rotorHub, false);
                pivot.localRotation = Quaternion.AngleAxis(i * 120f, Vector3.forward);
                bladePivots[i] = pivot;

                CreateRotorBlade("IEA15MW_Blade_" + (i + 1), pivot, bladeMaterial);
            }
        }

        private static Mesh CreateProceduralBladeMesh(float radius, float rootRadius)
        {
            float length = Mathf.Max(10f, radius - rootRadius);
            float rootWidth = 5.2f;
            float midWidth = 3.0f;
            float tipWidth = 0.55f;
            float thickness = 0.22f;

            Vector3[] vertices =
            {
                new Vector3(-rootWidth * 0.5f, rootRadius, -thickness),
                new Vector3(rootWidth * 0.5f, rootRadius, -thickness),
                new Vector3(-midWidth * 0.5f, rootRadius + length * 0.45f, -thickness * 0.7f),
                new Vector3(midWidth * 0.5f, rootRadius + length * 0.45f, -thickness * 0.7f),
                new Vector3(-tipWidth * 0.5f, radius, 0f),
                new Vector3(tipWidth * 0.5f, radius, 0f),
                new Vector3(-rootWidth * 0.5f, rootRadius, thickness),
                new Vector3(rootWidth * 0.5f, rootRadius, thickness),
                new Vector3(-midWidth * 0.5f, rootRadius + length * 0.45f, thickness * 0.7f),
                new Vector3(midWidth * 0.5f, rootRadius + length * 0.45f, thickness * 0.7f),
                new Vector3(-tipWidth * 0.5f, radius, 0f),
                new Vector3(tipWidth * 0.5f, radius, 0f)
            };

            int[] triangles =
            {
                0, 2, 1, 1, 2, 3,
                2, 4, 3, 3, 4, 5,
                6, 7, 8, 7, 9, 8,
                8, 9, 10, 9, 11, 10,
                0, 6, 2, 2, 6, 8,
                1, 3, 7, 3, 9, 7,
                4, 10, 5, 5, 10, 11,
                0, 1, 6, 1, 7, 6
            };

            Mesh mesh = new Mesh();
            mesh.name = "Procedural_IEA15MW_Blade";
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
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

            camera.transform.position = new Vector3(240f, 125f, -310f);
            camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 35f, 0f) - camera.transform.position, Vector3.up);
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
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (color.a < 0.99f)
            {
                if (material.HasProperty("_Surface"))
                {
                    material.SetFloat("_Surface", 1f);
                    material.SetFloat("_Blend", 0f);
                    material.SetFloat("_AlphaClip", 0f);
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

            return material;
        }

        private static void DisableOtherPreviewComponents()
        {
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour.GetType() == typeof(MostVolturnUsSceneBuilder))
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;
                if (typeName == "OpenFastTelemetryHud"
                    || typeName == "OpenFastPlaybackDriver"
                    || typeName == "MostHybridSparSceneBuilder")
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
