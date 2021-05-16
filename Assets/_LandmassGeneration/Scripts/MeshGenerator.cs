using UnityEngine;

namespace ProceduralTerrain
{
    /// <summary>
    /// Credits to Sebastian Lague :
    /// https://www.youtube.com/watch?v=4RpVBYW1r5M&list=PLFt_AvWsXl0eBW2EiBtl_sxmDtSgZBxB3&index=5
    /// </summary>
    public static class MeshGenerator
    {
        #region Nested Types

        public class MeshData
        {
            private Vector3[] Vertices { get; }
            private Vector2[] UV { get; }
            private int[] Triangles { get; }
            private int VertexIndex { get; set; }

            public MeshData(int meshWidth, int meshHeight)
            {
                Vertices = new Vector3[meshWidth * meshHeight];
                UV = new Vector2[meshWidth * meshHeight];
                Triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
            }

            public Mesh CreateMesh()
            {
                var mesh = new Mesh()
                {
                    vertices = Vertices,
                    triangles = Triangles,
                    uv = UV
                };

                mesh.RecalculateNormals();
                return mesh;
            }

            public void AddTriangle(int a, int b, int c)
            {
                Triangles[VertexIndex] = a;
                Triangles[VertexIndex  + 1] = b;
                Triangles[VertexIndex  + 2] = c;
                VertexIndex += 3;
            }

            public void SetVertex(Vector3 vertex, int index)
            {
                Vertices[index] = vertex;
            }

            public void SetUV(Vector2 uv, int index)
            {
                UV[index] = uv;
            }
        }

        #endregion Nested Types

        public static MeshData GenerateTerrainMesh(float[,] heightMap, MapGenerationSettings settings)
        {
            int height = heightMap.GetLength(0);
            int width = heightMap.GetLength(1);
            float topLeftX = (width - 1) / -2f;
            float topLeftZ = (height - 1) / 2f;

            int increment = settings.LevelOfDetails == 0 ? 1 : settings.LevelOfDetails * 2;
            int verticesPerLine = (width - 1) / increment + 1;
            var meshData = new MeshData(verticesPerLine, verticesPerLine);
            int vertexIndex = 0;

            for (int y = 0; y < height; y += increment)
            {
                for (int x = 0; x < width; x += increment)
                {
                    // Add current vertex and UV to mesh data
                    var posY = settings.HeightCurve.Evaluate(heightMap[x, y]) * settings.HeightMultiplier;
                    meshData.SetVertex(new Vector3(topLeftX + x, posY, topLeftZ - y), vertexIndex);
                    meshData.SetUV(new Vector2(x / (float)width, y / (float)height), vertexIndex);

                    // Add both triangles forming quad with top left corner on current vertex
                    if (x < width - 1 && y < height - 1)
                    {
                        meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                        meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                    }

                    vertexIndex++;
                }
            }

            return meshData;
        }
    }
}
