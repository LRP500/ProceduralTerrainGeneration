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
            private readonly Vector3[] _outOfMeshVertices;
            private readonly int[] _outOfMeshTriangles;

            private int _triangleIndex;
            private int _outOfMeshTriangleIndex;

            private Vector3[] _bakedNormals;

            private readonly bool _useFlatShading;

            public MeshData(int vertCountPerLine, int skipIncrement, bool useFlatShading)
            {
                int meshEdgeVertCount = (vertCountPerLine - 2) * 4 - 4;
                int edgeConnectionVertCount = (skipIncrement - 1) * (vertCountPerLine - 5) / skipIncrement * 4;
                int mainVertCountPerLine = (vertCountPerLine - 5) / skipIncrement + 1;
                int mainVertCount = mainVertCountPerLine * vertCountPerLine;

                Vertices = new Vector3[meshEdgeVertCount + edgeConnectionVertCount + mainVertCount];
                UV = new Vector2[Vertices.Length];

                int meshEdgeTriangleCount = 8 * (vertCountPerLine - 4);
                int mainTriangleCount = (mainVertCountPerLine - 1) * (mainVertCountPerLine - 1) * 2;
                Triangles = new int[(meshEdgeTriangleCount + mainTriangleCount) * 3];

                _outOfMeshVertices = new Vector3[vertCountPerLine * 4 - 4];
                _outOfMeshTriangles = new int[24 * (vertCountPerLine - 2)];
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
                int borderTriangleCount = _outOfMeshTriangles.Length / 3;
                for (int i = 0; i < borderTriangleCount; ++i)
                {
                    int normalTriangleIndex = i * 3;
                    int vertexIndexA = _outOfMeshTriangles[normalTriangleIndex];
                    int vertexIndexB = _outOfMeshTriangles[normalTriangleIndex + 1];
                    int vertexIndexC = _outOfMeshTriangles[normalTriangleIndex + 2];
                    
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
                Vector3 pointA = a < 0 ? _outOfMeshVertices[-a - 1] : Vertices[a];
                Vector3 pointB = b < 0 ? _outOfMeshVertices[-b - 1] : Vertices[b];
                Vector3 pointC = c < 0 ? _outOfMeshVertices[-c - 1] : Vertices[c];
                Vector3 sideAB = pointB - pointA;
                Vector3 sideAC = pointC - pointA;
                return Vector3.Cross(sideAB, sideAC).normalized;
            }

            public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
            {
                // If border vertex
                if (vertexIndex < 0)
                {
                    _outOfMeshVertices[-vertexIndex - 1] = vertexPosition;
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
                    _outOfMeshTriangles[_outOfMeshTriangleIndex] = a;
                    _outOfMeshTriangles[_outOfMeshTriangleIndex + 1] = b;
                    _outOfMeshTriangles[_outOfMeshTriangleIndex + 2] = c;
                    _outOfMeshTriangleIndex += 3;
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
        /// Generates terrain mesh from height map.
        /// </summary>
        /// <param name="heightMap">The height map to generate mesh from.</param>
        /// <param name="settings"></param>
        /// <param name="lod"></param>
        /// <returns></returns>
        public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings settings, int lod)
        {
            int skipIncrement = lod == 0 ? 1 : lod * 2;
            int vertCountPerLine = settings.VertexCountPerLine;
            Vector2 topLeft = new Vector2(-1, 1) * settings.MeshWorldSize / 2f;
            int borderedSize = heightMap.GetLength(0);
            var meshData = new MeshData(vertCountPerLine, skipIncrement, settings.UseFlatShading);

            int[,] vertexIndicesMap = new int[borderedSize, borderedSize];
            int meshVertexIndex = 0;
            int outOfMeshVertexIndex = -1;

            for (int y = 0; y < borderedSize; ++y)
            {
                for (int x = 0; x < borderedSize; ++x)
                {
                    bool isOutOfMeshVertex = y == 0 || y == vertCountPerLine - 1 || x == 0 || x == vertCountPerLine - 1;
                    
                    if (isOutOfMeshVertex)
                    {
                        vertexIndicesMap[x, y] = outOfMeshVertexIndex;
                        outOfMeshVertexIndex--;
                    }
                    else if (!IsSkippedVertex(x, y, vertCountPerLine, skipIncrement))
                    {
                        vertexIndicesMap[x, y] = meshVertexIndex;
                        meshVertexIndex++;
                    }
                }
            }

            for (int y = 0; y < borderedSize; ++y)
            {
                for (int x = 0; x < borderedSize; ++x)
                {
                    if (!IsSkippedVertex(x, y, vertCountPerLine, skipIncrement))
                    {
                        bool isOutOfMeshVertex = y == 0 || y == vertCountPerLine - 1 || x == 0 || x == vertCountPerLine - 1;
                        bool isMeshEdgeVertex = y == 1 || y == vertCountPerLine - 2 || x == 1 || x == vertCountPerLine - 2 && !isOutOfMeshVertex;
                        bool isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
                        bool isEdgeConnectionVertex = (y == 2 || y == vertCountPerLine - 3 || x == 2 || x == vertCountPerLine - 3) && !isOutOfMeshVertex && !isMainVertex && !isMeshEdgeVertex;

                        int vertexIndex = vertexIndicesMap[x, y];
                        Vector2 percent = new Vector2(x - 1, y - 1) / (vertCountPerLine - 3);
                        Vector2 vertexPos2D = topLeft + new Vector2(percent.x, -percent.y) * settings.MeshWorldSize;
                        var height = heightMap[x, y];

                        // Resolve seams between mesh and edge triangles.
                        if (isEdgeConnectionVertex)
                        {
                            bool isVertical = x == 2 || x == vertCountPerLine - 3;
                            int dstToMainVertA = (isVertical ? y - 2 : x - 2) % skipIncrement;
                            int dstToMainVertB = skipIncrement - dstToMainVertA;

                            float dstPercentFromAToB = dstToMainVertA / (float) skipIncrement;
                            float heightMainVertA = heightMap[isVertical ? x : x - dstToMainVertA, isVertical ? y - dstToMainVertA : y];
                            float heightMainVertB = heightMap[isVertical ? x : x + dstToMainVertB, isVertical ? y + dstToMainVertB : y];

                            height = heightMainVertA * (1 - dstPercentFromAToB) + heightMainVertB * dstPercentFromAToB;
                        }

                        meshData.AddVertex(new Vector3(vertexPos2D.x, height, vertexPos2D.y), percent, vertexIndex);

                        bool createTriangle = x < vertCountPerLine - 1 && y < vertCountPerLine - 1 && (!isEdgeConnectionVertex || x != 2 && y != 2);

                        if (createTriangle)
                        {
                            int currentIncrement = isMainVertex && x != vertCountPerLine - 3 && y != vertCountPerLine - 3 ? skipIncrement : 1;

                            int a = vertexIndicesMap[x, y];
                            int b = vertexIndicesMap[x + currentIncrement, y];
                            int c = vertexIndicesMap[x, y + currentIncrement];
                            int d = vertexIndicesMap[x + currentIncrement, y + currentIncrement];

                            meshData.AddTriangle(a, d, c);
                            meshData.AddTriangle(d, a, b);
                        }
                    }
                }
            }

            meshData.CalculateShading();

            return meshData;
        }

        private static bool IsSkippedVertex(int x, int y, int vertexCountPerLine, int skipIncrement)
        {
            return x > 2 && x < vertexCountPerLine - 3 && y > 2 && y < vertexCountPerLine - 3 && ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
        }
    }
}
