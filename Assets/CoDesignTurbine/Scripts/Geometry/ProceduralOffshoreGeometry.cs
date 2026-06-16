using UnityEngine;

namespace CoDesignTurbine.Geometry
{
    public static class ProceduralOffshoreGeometry
    {
        public static Mesh CreateTorus(float majorRadius, float minorRadius, int majorSegments, int minorSegments)
        {
            majorSegments = Mathf.Max(8, majorSegments);
            minorSegments = Mathf.Max(6, minorSegments);

            Vector3[] vertices = new Vector3[majorSegments * minorSegments];
            int[] triangles = new int[majorSegments * minorSegments * 6];

            for (int i = 0; i < majorSegments; i++)
            {
                float u = i / (float)majorSegments * Mathf.PI * 2f;
                Vector3 center = new Vector3(Mathf.Cos(u) * majorRadius, 0f, Mathf.Sin(u) * majorRadius);

                for (int j = 0; j < minorSegments; j++)
                {
                    float v = j / (float)minorSegments * Mathf.PI * 2f;
                    Vector3 radial = new Vector3(Mathf.Cos(u), 0f, Mathf.Sin(u));
                    Vector3 point = center + radial * (Mathf.Cos(v) * minorRadius) + Vector3.up * (Mathf.Sin(v) * minorRadius);
                    vertices[i * minorSegments + j] = point;
                }
            }

            int index = 0;
            for (int i = 0; i < majorSegments; i++)
            {
                int nextI = (i + 1) % majorSegments;
                for (int j = 0; j < minorSegments; j++)
                {
                    int nextJ = (j + 1) % minorSegments;
                    int a = i * minorSegments + j;
                    int b = nextI * minorSegments + j;
                    int c = nextI * minorSegments + nextJ;
                    int d = i * minorSegments + nextJ;

                    triangles[index++] = a;
                    triangles[index++] = b;
                    triangles[index++] = c;
                    triangles[index++] = a;
                    triangles[index++] = c;
                    triangles[index++] = d;
                }
            }

            Mesh mesh = new Mesh
            {
                name = "ProceduralToroid"
            };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}

