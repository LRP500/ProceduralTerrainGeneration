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
            private Vector3[] Vertices { get; set; }
            private Vector2[] UV { get; set; }
            private int[] Triangles { get; }

            /// Borders are used to calculate normals so chunks can blend correctly
            private readonly Vector3[] _borderVertices;
            private readonly int[] _borderTriangles;

            private int _triangleIndex;
            private int _borderTriangleIndex;

            private Vector3[] _bakedNormals;

            private bool _useFlatShading;

            public MeshData(int verticesPerLine, bool useFlatShading)
            {
                Vertices = new Vector3[verticesPerLine * verticesPerLine];
                UV = new Vector2[verticesPerLine * verticesPerLine];
                Triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

                _borderVertices = new Vector3[verticesPerLine * 4 + 4];
                _borderTriangles = new int[24 * verticesPerLine];
                _useFlatShading = useFlatShading;
            }

            public Mesh CreateMesh()
            {
                var mesh = new Mesh
                {
                    vertices = Vertices,
                    triangles = Triangles,
                    uv = UV
                };

                if (_useFlatShading)
                {
                    mesh.RecalculateNormals();
                }
                else
                {
                    mesh.normals = _bakedNormals;
                }

                return mesh;
            }

            private Vector3[] CalculateNormals()
            {
                Vector3[] vertexNormals = new Vector3[Vertices.Length];
                
                // Mesh triangles
                int triangleCount = Triangles.Length / 3;
                for (int i = 0; i < triangleCount; ++i)
                {
                    int normalTriangleIndex = i * 3;
                    int vertexIndexA = Triangles[normalTriangleIndex];
                    int vertexIndexB = Triangles[normalTriangleIndex + 1];
                    int vertexIndexC = Triangles[normalTriangleIndex + 2];
                    
                    Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
                    vertexNormals[vertexIndexA] += triangleNormal;
                    vertexNormals[vertexIndexB] += triangleNormal;
                    vertexNormals[vertexIndexC] += triangleNormal;
                }

                // Border triangles
                int borderTriangleCount = _borderTriangles.Length / 3;
                for (int i = 0; i < borderTriangleCount; ++i)
                {
                    int normalTriangleIndex = i * 3;
                    int vertexIndexA = _borderTriangles[normalTriangleIndex];
                    int vertexIndexB = _borderTriangles[normalTriangleIndex + 1];
                    int vertexIndexC = _borderTriangles[normalTriangleIndex + 2];
                    
                    Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);

                    if (vertexIndexA >= 0)
                    {
                        vertexNormals[vertexIndexA] += triangleNormal;
                    }

                    if (vertexIndexB >= 0)
                    {
                        vertexNormals[vertexIndexB] += triangleNormal;
                    }

                    if (vertexIndexC >= 0)
                    {
                        vertexNormals[vertexIndexC] += triangleNormal;
                    }
                }

                for (int i = 0, length = vertexNormals.Length; i < length; ++i)
                {
                    vertexNormals[i].Normalize();
                }

                return vertexNormals;
            }

            public void CalculateShading()
            {
                if (_useFlatShading)
                {
                    FlatShading();
                }
                else
                {
                    BakeNormals();
                }
            }

            private void BakeNormals()
            {
                _bakedNormals = CalculateNormals();
            }

            public void FlatShading()
            {
                Vector3[] flatShadedVertices = new Vector3[Triangles.Length];
                Vector2[] flatShadedUV = new Vector2[Triangles.Length];

                for (int i = 0, length = Triangles.Length; i < length; ++i)
                {
                    flatShadedVertices[i] = Vertices[Triangles[i]];
                    flatShadedUV[i] = UV[Triangles[i]];
                    Triangles[i] = i;
                }

                Vertices = flatShadedVertices;
                UV = flatShadedUV;
            }

            private Vector3 SurfaceNormalFromIndices(int a, int b, int c)
            {
                Vector3 pointA = a < 0 ? _borderVertices[-a - 1] : Vertices[a];
                Vector3 pointB = b < 0 ? _borderVertices[-b - 1] : Vertices[b];
                Vector3 pointC = c < 0 ? _borderVertices[-c - 1] : Vertices[c];
                Vector3 sideAB = pointB - pointA;
                Vector3 sideAC = pointC - pointA;
                return Vector3.Cross(sideAB, sideAC).normalized;
            }

            public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
            {
                // If border vertex
                if (vertexIndex < 0)
                {
                    _borderVertices[-vertexIndex - 1] = vertexPosition;
                }
                else
                {
                    Vertices[vertexIndex] = vertexPosition;
                    UV[vertexIndex] = uv;
                }
            }

            public void AddTriangle(int a, int b, int c)
            {
                if (a < 0 || b < 0 || c < 0)
                {
                    _borderTriangles[_borderTriangleIndex] = a;
                    _borderTriangles[_borderTriangleIndex + 1] = b;
                    _borderTriangles[_borderTriangleIndex + 2] = c;
                    _borderTriangleIndex += 3;
                }
                else
                {
                    Triangles[_triangleIndex] = a;
                    Triangles[_triangleIndex + 1] = b;
                    Triangles[_triangleIndex + 2] = c;
                    _triangleIndex += 3;
                }
            }
        }

        #endregion Nested Types

        /// <summary>
        /// Generates terrain mesh with specified level of detail.
        /// </summary>
        /// <param name="heightMap">The generated height map.</param>
        /// <param name="settings">The map generation settings.</param>
        /// <param name="lod">The level of detail.</param>
        /// <returns>The generated mesh data.</returns>
        public static MeshData GenerateTerrainMesh(float[,] heightMap, MapGenerationSettings settings, int lod)
        {
            // Important: we create a new local instance of animation curve as AnimationCurve is not thread safe.
            var heightCurve = new AnimationCurve(settings.HeightCurve.keys);

            int increment = lod == 0 ? 1 : lod * 2; // Mesh simplification increment (LOD)
            int borderedSize = heightMap.GetLength(0);
            int meshSize = borderedSize - 2 * increment; // Use mesh simplification increment to compensate for border size
            int meshSizeUnsimplified = borderedSize - 2; // Mesh size independant from LOD
            float topLeftX = (meshSizeUnsimplified - 1) / -2f;
            float topLeftZ = (meshSizeUnsimplified - 1) / 2f;

            int verticesPerLine = (meshSize - 1) / increment + 1;
            var meshData = new MeshData(verticesPerLine, settings.UseFlatShading);

            int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
            int meshVertexIndex = 0;
            int borderVertexIndex = -1;

            for (int y = 0; y < borderedSize; y += increment)
            {
                for (int x = 0; x < borderedSize; x += increment)
                {
                    bool isBorderVertex = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;

                    if (isBorderVertex)
                    {
                        vertexIndicesMap[x, y] = borderVertexIndex;
                        borderVertexIndex--;
                    }
                    else
                    {
                        vertexIndicesMap[x, y] = meshVertexIndex;
                        meshVertexIndex++;
                    }
                }
            }

            for (int y = 0; y < borderedSize; y += increment)
            {
                for (int x = 0; x < borderedSize; x += increment)
                {
                    int vertexIndex = vertexIndicesMap[x, y];
                    var height = heightCurve.Evaluate(heightMap[x, y]) * settings.HeightMultiplier;
                    var percent = new Vector2((x - increment) / (float) meshSize, (y - increment) / (float) meshSize); 
                    var vertexPosition = new Vector3(topLeftX + percent.x * meshSizeUnsimplified, height, topLeftZ - percent.y * meshSizeUnsimplified);
                    
                    meshData.AddVertex(vertexPosition, percent, vertexIndex);

                    if (x < borderedSize - 1 && y < borderedSize - 1)
                    {
                        int a = vertexIndicesMap[x, y];
                        int b = vertexIndicesMap[x + increment, y];
                        int c = vertexIndicesMap[x, y + increment];
                        int d = vertexIndicesMap[x + increment, y + increment];

                        meshData.AddTriangle(a, d, c);
                        meshData.AddTriangle(d, a, b);
                    }
                }
            }

            meshData.CalculateShading();

            return meshData;
        }

        /// <summary>
        /// Generates terrain mesh with preview settings.
        /// </summary>
        /// <param name="heightMap">The generated height map.</param>
        /// <param name="settings">The map generation settings.</param>
        /// <returns>The generated mesh data.</returns>
        public static MeshData GenerateTerrainMesh(float[,] heightMap, MapGenerationSettings settings)
        {
            return GenerateTerrainMesh(heightMap, settings, settings.LODPreview);
        }
    }
}
