using UnityEngine;

namespace ProceduralTerrain
{
    public class MapDisplay : MonoBehaviour
    {
        #region Nested Types

        private enum DrawMode
        {
            HeightMap,
            ColorMap,
            FalloffMap,
            Mesh
        }

        #endregion Nested Types

        [SerializeField]
        private Renderer _textureRenderer;

        [SerializeField]
        private MeshFilter _meshFilter;

        [SerializeField]
        private MeshRenderer _meshRenderer;

        [SerializeField]
        private DrawMode _drawMode = DrawMode.ColorMap;

        public void DrawMap(MapGenerator.MapData data, MapGenerationSettings settings)
        {
            Texture2D coloredTexture = TextureGenerator.TextureFromColorMap(data.colorMap,
                MapGenerationSettings.ChunkSize,
                MapGenerationSettings.ChunkSize);

            if (_drawMode == DrawMode.HeightMap)
            {
                DrawTexture(TextureGenerator.TextureFromHeightMap(data.heightMap));
            }
            else if (_drawMode == DrawMode.ColorMap)
            {
                DrawTexture(coloredTexture);
            }
            else if (_drawMode == DrawMode.FalloffMap)
            {
                var fallOffMap = FalloffGenerator.GenerateFalloffMap(MapGenerationSettings.ChunkSize);
                DrawTexture(TextureGenerator.TextureFromHeightMap(fallOffMap));
            }
            else if (_drawMode == DrawMode.Mesh)
            {
                DrawMesh(MeshGenerator.GenerateTerrainMesh(data.heightMap, settings), coloredTexture);
            }
        }

        private void DrawMesh(MeshGenerator.MeshData meshData, Texture texture)
        {
            _meshFilter.sharedMesh = meshData.CreateMesh();
            _meshRenderer.sharedMaterial.mainTexture = texture;
        }

        private void DrawTexture(Texture texture)
        {
            _textureRenderer.sharedMaterial.mainTexture = texture;
            _textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
        }
    }
}
