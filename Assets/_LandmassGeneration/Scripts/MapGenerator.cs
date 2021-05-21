using System.Collections.Generic;
using System.Threading;
using Sirenix.OdinInspector;
using UnityEngine;
using MeshData = ProceduralTerrain.MeshGenerator.MeshData;

namespace ProceduralTerrain
{
    [RequireComponent(typeof(MapDisplay))]
    public class MapGenerator : Singleton<MapGenerator>
    {
        #region Nested Types

        public readonly struct MapData
        {
            public readonly float[,] heightMap;
            public readonly Color[] colorMap;

            public MapData(float[,] heightMap, Color[] colorMap)
            {
                this.heightMap = heightMap;
                this.colorMap = colorMap;
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
        [OnValueChanged(nameof(OnSettingsChanged), true)]
        private MapGenerationSettings _settings;

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
        public static int ChunkSize => Instance._settings.UseFlatShading ? 95 : 239;

        private void Awake()
        {
            _falloffMap = FalloffGenerator.GenerateFalloffMap(ChunkSize);
        }

        private void Update()
        {
            ProcessMapDataInfoQueue();
            ProcessMeshDataInfoQueue();
        }

        private void DrawMap()
        {
            _falloffMap = FalloffGenerator.GenerateFalloffMap(ChunkSize);
            MapData mapData = GenerateMapData(Vector2.zero);
            _display = _display ? _display : GetComponent<MapDisplay>();
            _display.DrawMap(mapData, _settings);
        }

        private MapData GenerateMapData(Vector2 center)
        {
            float[,] heightMap = Noise.GenerateNoiseMap(_settings, center, _normalizeMode);
            Color[] colorMap = InitializeRegions(heightMap);
            return new MapData(heightMap, colorMap);
        }

        private Color[] InitializeRegions(float[,] noiseMap)
        {
            Color[] colorMap = new Color[ChunkSize * ChunkSize];

            for (int y = 0; y < ChunkSize; ++y)
            {
                for (int x = 0; x < ChunkSize; ++x)
                {
                    // Apply falloff
                    if (_settings.UseFalloff)
                    {
                        noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - _falloffMap[x, y]);
                    }

                    float currentHeight = noiseMap[x, y];
                    for (int i = 0, length = _settings.Regions.Count; i < length; i++)
                    {
                        TerrainType region = _settings.Regions[i];
                        if (currentHeight >= region.Height)
                        {
                            colorMap[y * ChunkSize + x] = region.Color;
                        }
                        else break;
                    }
                }
            }

            return colorMap;
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
            MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, _settings, lod);

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

        private void OnSettingsChanged()
        {
            if (_autoUpdate)
            {
                DrawMap();
            }
        }

        #endregion Editor
    }
}
