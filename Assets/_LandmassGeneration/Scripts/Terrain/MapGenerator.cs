using System.Collections.Generic;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;
using MeshData = ProceduralTerrain.MeshGenerator.MeshData;

namespace ProceduralTerrain
{
    [RequireComponent(typeof(MapDisplay))]
    public class MapGenerator : MonoBehaviour
    {
        #region Nested Types

        private readonly struct MapThreadInfo<T>
        {
            public readonly System.Action<T> callback;
            public readonly T parameter;

            public MapThreadInfo(System.Action<T> callback, T parameter)
            {
                this.callback = callback;
                this.parameter = parameter;
            }
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
        private TextureData _textureData;

        [SerializeField]
        private Material _terrainMaterial;

        [SerializeField]
        [LabelText("Editor Preview LOD")]
        [Range(0, MeshSettings.SupportedLODCount - 1)]
        private int _editorPreviewLOD = 1;

        public bool _autoUpdate = true;

        private MapDisplay _display;

        private float[,] _falloffMap;

        private readonly Queue<MapThreadInfo<HeightMapGenerator.HeightMap>> _heightMapThreadInfoQueue = new Queue<MapThreadInfo<HeightMapGenerator.HeightMap>>();
        private readonly Queue<MapThreadInfo<MeshData>> _meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

        public MeshSettings MeshSettings => _meshSettings;

        private void Start()
        {
            _textureData.ApplyToMaterial(_terrainMaterial);
            _textureData.UpdateMeshHeights(_terrainMaterial, _heightMapSettings.MinHeight, _heightMapSettings.MaxHeight);
        }

        private void Update()
        {
            ProcessHeightMapInfoQueue();
            ProcessMeshDataInfoQueue();
        }

        private void DrawMap()
        {
            HeightMapGenerator.HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(
                _meshSettings.VertexCountPerLine,
                _meshSettings.VertexCountPerLine,
                _heightMapSettings,
                Vector2.zero);

            _display = _display ? _display : GetComponent<MapDisplay>();
            _textureData.UpdateMeshHeights(_terrainMaterial, _heightMapSettings.MinHeight, _heightMapSettings.MaxHeight);
            _textureData.ApplyToMaterial(_terrainMaterial);
            _display.DrawMap(heightMap, _meshSettings, _editorPreviewLOD);
        }

        private void ApplyFalloff(in float[,] heightMap, int chunkSize)
        {
            _falloffMap ??= FalloffGenerator.GenerateFalloffMap(_meshSettings.VertexCountPerLine);

            for (int y = 0, height = chunkSize; y < height; ++y)
            {
                for (int x = 0, width = chunkSize; x < width; ++x)
                {
                    heightMap[x, y] = Mathf.Clamp01(heightMap[x, y] - _falloffMap[x, y]);
                }
            }
        }

        #region Threading

        public void RequestHeightMap(Vector2 center, System.Action<HeightMapGenerator.HeightMap> callback)
        {
            void ThreadStart() => HeightMapThread(center, callback);
            new Thread(ThreadStart).Start();
        }

        private void HeightMapThread(Vector2 center, System.Action<HeightMapGenerator.HeightMap> callback)
        {
            HeightMapGenerator.HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(
                _meshSettings.VertexCountPerLine,
                _meshSettings.VertexCountPerLine,
                _heightMapSettings,
                center);

            lock (_heightMapThreadInfoQueue)
            {
                _heightMapThreadInfoQueue.Enqueue(new MapThreadInfo<HeightMapGenerator.HeightMap>(callback, heightMap));
            }
        }

        public void RequestMeshData(HeightMapGenerator.HeightMap heightMap, int lod, System.Action<MeshData> callback)
        {
            void ThreadStart() => MeshDataThread(heightMap, lod, callback);
            new Thread(ThreadStart).Start();
        }

        private void MeshDataThread(HeightMapGenerator.HeightMap heightMap, int lod, System.Action<MeshData> callback)
        {
            MeshData meshData = MeshGenerator.GenerateTerrainMesh(heightMap.values, _meshSettings, lod);

            lock (_meshDataThreadInfoQueue)
            {
                _meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
            }
        }

        private void ProcessHeightMapInfoQueue()
        {
            lock (_heightMapThreadInfoQueue)
            {
                if (_heightMapThreadInfoQueue.Count > 0)
                {
                    for (int i = 0, length = _heightMapThreadInfoQueue.Count; i < length; ++i)
                    {
                        var threadInfo = _heightMapThreadInfoQueue.Dequeue();
                        threadInfo.callback?.Invoke(threadInfo.parameter);
                    }
                }
            }
        }

        private void ProcessMeshDataInfoQueue()
        {
            lock (_meshDataThreadInfoQueue)
            {
                if (_meshDataThreadInfoQueue.Count > 0)
                {
                    for (int i = 0, length = _meshDataThreadInfoQueue.Count; i < length; ++i)
                    {
                        var threadInfo = _meshDataThreadInfoQueue.Dequeue();
                        threadInfo.callback?.Invoke(threadInfo.parameter);
                    }
                }
            }
        }

        #endregion Threading

        #region Editor

        [Button("Generate")]
        private void OnGenerateButtonClicked()
        {
            DrawMap();
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
            _textureData.ApplyToMaterial(_terrainMaterial);
        }

        private void OnValidate()
        {
           UpdatePreview();
        }

        private void UpdatePreview()
        {
            if (_autoUpdate)
            {
                DrawMap();
            }
        }

        #endregion Editor
    }
}
