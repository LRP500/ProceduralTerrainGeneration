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

        public readonly struct MapData
        {
            public readonly float[,] heightMap;

            public MapData(float[,] heightMap)
            {
                this.heightMap = heightMap;
            }
        }

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
        [OnValueChanged(nameof(OnNoiseDataChanged), true)]
        private NoiseData _noiseData;

        [SerializeField]
        [OnValueChanged(nameof(OnTerrainDataChanged), true)]
        private TerrainData _terrainData;

        [SerializeField]
        [OnValueChanged(nameof(OnTextureDataChanged), true)]
        private TextureData _textureData;

        [SerializeField]
        private Material _terrainMaterial;

        [SerializeField]
        private Noise.NormalizeMode _normalizeMode;

        public bool _autoUpdate = true;

        private MapDisplay _display;

        private float[,] _falloffMap;

        private readonly Queue<MapThreadInfo<MapData>> _mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
        private readonly Queue<MapThreadInfo<MeshData>> _meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

        // Chunk size must be dividable by all possible LOD values
        // (e.g. 240 is dividable by all numbers up to twelve)
        // (- 2 to compensate for borders used to calculate seamless normals)
        // With flat shading we need 3x the triangles so we use a smaller chunk size.
        public int ChunkSize => _terrainData.UseFlatShading ? 95 : 239;

        public NoiseData NoiseData => _noiseData;
        public TerrainData TerrainData => _terrainData;
        public TextureData TextureData => _textureData;

        private void Awake()
        {
            _textureData.ApplyToMaterial(_terrainMaterial);
            _textureData.UpdateMeshHeights(_terrainMaterial, _terrainData.MinHeight, _terrainData.MaxHeight);
        }

        private void Update()
        {
            ProcessMapDataInfoQueue();
            ProcessMeshDataInfoQueue();
        }

        private void DrawMap()
        {
            MapData mapData = GenerateMapData(Vector2.zero);
            _display = _display ? _display : GetComponent<MapDisplay>();
            _textureData.UpdateMeshHeights(_terrainMaterial, _terrainData.MinHeight, _terrainData.MaxHeight);
            _textureData.ApplyToMaterial(_terrainMaterial);
            _display.DrawMap(mapData, _terrainData);
        }

        private MapData GenerateMapData(Vector2 center)
        {
            // We add 2 to chunk size to compensate for the borders used to calculate seamless normals
            float[,] heightMap = Noise.GenerateNoiseMap(_noiseData, center, ChunkSize + 2, _normalizeMode);

            if (_terrainData.UseFalloff)
            {
               ApplyFalloff(heightMap, ChunkSize + 2);
            }

            return new MapData(heightMap);
        }

        private void ApplyFalloff(in float[,] heightMap, int chunkSize)
        {
            _falloffMap ??= FalloffGenerator.GenerateFalloffMap(ChunkSize + 2);

            for (int y = 0, height = chunkSize; y < height; ++y)
            {
                for (int x = 0, width = chunkSize; x < width; ++x)
                {
                    heightMap[x, y] = Mathf.Clamp01(heightMap[x, y] - _falloffMap[x, y]);
                }
            }
        }

        #region Threading

        public void RequestMapData(Vector2 center, System.Action<MapData> callback)
        {
            void ThreadStart() => MapDataThread(center, callback);
            new Thread(ThreadStart).Start();
        }

        private void MapDataThread(Vector2 center, System.Action<MapData> callback)
        {
            MapData mapData = GenerateMapData(center);

            lock (_mapDataThreadInfoQueue)
            {
                _mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
            }
        }

        public void RequestMeshData(MapData mapData, int lod, System.Action<MeshData> callback)
        {
            void ThreadStart() => MeshDataThread(mapData, lod, callback);
            new Thread(ThreadStart).Start();
        }

        private void MeshDataThread(MapData mapData, int lod, System.Action<MeshData> callback)
        {
            MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, _terrainData, lod);

            lock (_meshDataThreadInfoQueue)
            {
                _meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
            }
        }

        private void ProcessMapDataInfoQueue()
        {
            lock (_mapDataThreadInfoQueue)
            {
                if (_mapDataThreadInfoQueue.Count > 0)
                {
                    for (int i = 0, length = _mapDataThreadInfoQueue.Count; i < length; ++i)
                    {
                        var threadInfo = _mapDataThreadInfoQueue.Dequeue();
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

        private void OnNoiseDataChanged()
        {
            if (_autoUpdate)
            {
                DrawMap();
            }
        }

        private void OnTerrainDataChanged()
        {
            if (_autoUpdate)
            {
                DrawMap();
            }
        }

        private void OnTextureDataChanged()
        {
            _textureData.ApplyToMaterial(_terrainMaterial);
        }

        #endregion Editor
    }
}
