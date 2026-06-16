using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace CoDesignTurbine.Geometry
{
    public static class StlMeshLoader
    {
        public static Mesh Load(string path, bool convertZUpToUnity = true)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("STL path is empty.");
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("STL file was not found.", path);
            }

            byte[] bytes = File.ReadAllBytes(path);
            Mesh mesh = LooksBinary(bytes) ? LoadBinary(bytes, convertZUpToUnity) : LoadAscii(Encoding.ASCII.GetString(bytes), convertZUpToUnity);
            mesh.name = Path.GetFileNameWithoutExtension(path);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static bool LooksBinary(byte[] bytes)
        {
            if (bytes.Length < 84)
            {
                return false;
            }

            uint triangleCount = BitConverter.ToUInt32(bytes, 80);
            long expectedLength = 84L + triangleCount * 50L;
            return expectedLength == bytes.Length;
        }

        private static Mesh LoadBinary(byte[] bytes, bool convertZUpToUnity)
        {
            uint triangleCount = BitConverter.ToUInt32(bytes, 80);
            List<Vector3> vertices = new List<Vector3>((int)triangleCount * 3);
            List<int> triangles = new List<int>((int)triangleCount * 3);

            int offset = 84;
            for (int i = 0; i < triangleCount; i++)
            {
                offset += 12;
                for (int vertex = 0; vertex < 3; vertex++)
                {
                    float x = BitConverter.ToSingle(bytes, offset);
                    float y = BitConverter.ToSingle(bytes, offset + 4);
                    float z = BitConverter.ToSingle(bytes, offset + 8);
                    offset += 12;
                    vertices.Add(ConvertCoordinate(new Vector3(x, y, z), convertZUpToUnity));
                    triangles.Add(vertices.Count - 1);
                }

                offset += 2;
            }

            return BuildMesh(vertices, triangles);
        }

        private static Mesh LoadAscii(string text, bool convertZUpToUnity)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            string[] lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (!line.StartsWith("vertex ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string[] parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4)
                {
                    continue;
                }

                float x = Parse(parts[1]);
                float y = Parse(parts[2]);
                float z = Parse(parts[3]);
                vertices.Add(ConvertCoordinate(new Vector3(x, y, z), convertZUpToUnity));
                triangles.Add(vertices.Count - 1);
            }

            return BuildMesh(vertices, triangles);
        }

        private static Mesh BuildMesh(List<Vector3> vertices, List<int> triangles)
        {
            Mesh mesh = new Mesh();
            if (vertices.Count > 65000)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            return mesh;
        }

        private static Vector3 ConvertCoordinate(Vector3 source, bool convertZUpToUnity)
        {
            if (!convertZUpToUnity)
            {
                return source;
            }

            return new Vector3(source.y, source.z, source.x);
        }

        private static float Parse(string value)
        {
            value = value.Replace('D', 'E').Replace('d', 'e');
            return float.Parse(value, CultureInfo.InvariantCulture);
        }
    }
}

