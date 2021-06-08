using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

using HeightMap = ProceduralTerrain.HeightMapGenerator.HeightMap;

namespace ProceduralTerrain
{
    public class MapPreview : MonoBehaviour
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
        [OnValueChanged(nameof(OnHeightMapSettingsChanged), true)]
        private HeightMapSettings _heightMapSettings;

        [SerializeField]
        [OnValueChanged(nameof(OnMeshSettingsChanged), true)]
        private MeshSettings _meshSettings;

        [SerializeField]
        [OnValueChanged(nameof(OnTextureDataChanged), true)]
        private TextureSettings _textureSettings;

        [SerializeField]
        private Material _terrainMaterial;

        [SerializeField]
        private Renderer _textureRenderer;

        [SerializeField]
        private MeshFilter _meshFilter;

        [SerializeField]
        private DrawMode _drawMode = DrawMode.Mesh;

        [SerializeField]
        [LabelText("Editor Preview LOD")]
        [Range(0, MeshSettings.SupportedLODCount - 1)]
        private int _editorPreviewLOD = 1;

        public bool _autoUpdate = true;

        private void DrawMap(HeightMap heightMap, MeshSettings meshSettings, int editorPreviewLOD)
        {
            if (_drawMode == DrawMode.HeightMap)
            {
                DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
            }
            else if (_drawMode == DrawMode.FalloffMap)
            {
                var fallOffMap = FalloffGenerator.GenerateFalloffMap(meshSettings.VertexCountPerLine);
                DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(fallOffMap, 0, 1)));
            }
            else if (_drawMode == DrawMode.Mesh)
            {
                DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLOD));
            }
        }

        private void DrawMesh(MeshGenerator.MeshData meshData)
        {
            _meshFilter.sharedMesh = meshData.CreateMesh();
            _meshFilter.gameObject.SetActive(true);
            _textureRenderer.gameObject.SetActive(false);
        }

        private void DrawTexture(Texture texture)
        {
            _textureRenderer.sharedMaterial.mainTexture = texture;
            _textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10;
            _textureRenderer.gameObject.SetActive(true);
            _meshFilter.gameObject.SetActive(false);
        }

        private void DrawMapInEditor()
        {
            TextureSettings.UpdateMeshHeights(_terrainMaterial, _heightMapSettings.MinHeight, _heightMapSettings.MaxHeight);
            _textureSettings.ApplyToMaterial(_terrainMaterial);
            
            HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(
                _meshSettings.VertexCountPerLine,
                _meshSettings.VertexCountPerLine,
                _heightMapSettings,
                Vector2.zero);

            DrawMap(heightMap, _meshSettings, _editorPreviewLOD);
        }

        #region Editor

        [Button("Generate")]
        private void OnGenerateButtonClicked()
        {
            DrawMapInEditor();
        }

        private void OnHeightMapSettingsChanged()
        {
            UpdatePreview();
        }

        private void OnMeshSettingsChanged()
        {
            UpdatePreview();
        }

        private void OnTextureDataChanged()
        {
            _textureSettings.ApplyToMaterial(_terrainMaterial);
        }

        private void OnValidate()
        {
            // Delay call to avoid obsolete "SendMessage cannot be called during OnValidate" warning.
            EditorApplication.delayCall += UpdatePreview;
        }

        private void UpdatePreview()
        {
            if (_autoUpdate)
            {
                DrawMapInEditor();
            }
        }

        #endregion Editor
    }
}
