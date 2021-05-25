using UnityEngine;

namespace ProceduralTerrain
{
    [RequireComponent(typeof(MapGenerator))]
    public class MapDisplay : MonoBehaviour
    {
        #region Nested Types

        private enum DrawMode
        {
            HeightMap,
            FalloffMap,
            Mesh
        }

        #endregion Nested Types

        [SerializeField]
        private Renderer _textureRenderer;

        [SerializeField]
        private MeshFilter _meshFilter;

        [SerializeField]
        private DrawMode _drawMode = DrawMode.Mesh;

        private MapGenerator _mapGenerator;

        private MapGenerator GetMapGenerator()
        {
            _mapGenerator ??= GetComponent<MapGenerator>();
            return _mapGenerator;
        }

        public void DrawMap(MapGenerator.MapData mapData, TerrainData terrainData)
        {
            if (_drawMode == DrawMode.HeightMap)
            {
                DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
            }
            else if (_drawMode == DrawMode.FalloffMap)
            {
                var fallOffMap = FalloffGenerator.GenerateFalloffMap(_mapGenerator.ChunkSize);
                DrawTexture(TextureGenerator.TextureFromHeightMap(fallOffMap));
            }
            else if (_drawMode == DrawMode.Mesh)
            {
                DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, terrainData));
            }
        }

        private void DrawMesh(MeshGenerator.MeshData meshData)
        {
            _meshFilter.sharedMesh = meshData.CreateMesh();
            _meshFilter.transform.localScale = Vector3.one * GetMapGenerator().TerrainData.WorldScale;
        }

        private void DrawTexture(Texture texture)
        {
            _textureRenderer.sharedMaterial.mainTexture = texture;
            _textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
        }
    }
}
