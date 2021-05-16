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
            public readonly Color[] colorMap;

            public MapData(float[,] heightMap, Color[] colorMap)
            {
                this.heightMap = heightMap;
                this.colorMap = colorMap;
            }
        }

        public readonly struct MapThreadInfo<T>
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

        public bool _autoUpdate = true;

        private MapDisplay _display;

        private readonly Queue<MapThreadInfo<MapData>> _mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
        private readonly Queue<MapThreadInfo<MeshGenerator.MeshData>> _meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshGenerator.MeshData>>();

        private void Update()
        {
            ProcessMapDataInfoQueue();
            ProcessMeshDataInfoQueue();
        }

        private void DrawMap()
        {
            MapData mapData = GenerateMapData();
            _display = _display ? _display : GetComponent<MapDisplay>();
            _display.DrawMap(mapData, _settings);
        }

        private MapData GenerateMapData()
        {
            float[,] heightMap = Noise.GenerateNoiseMap(_settings);
            Color[] colorMap = InitializeRegions(heightMap);
            return new MapData(heightMap, colorMap);
        }

        private Color[] InitializeRegions(float[,] noiseMap)
        {
            Color[] colorMap = new Color[MapGenerationSettings.ChunkSize * MapGenerationSettings.ChunkSize];

            for (int y = 0; y < MapGenerationSettings.ChunkSize; ++y)
            {
                for (int x = 0; x < MapGenerationSettings.ChunkSize; ++x)
                {
                    float currentHeight = noiseMap[x, y];
                    for (int i = 0, length = _settings.Regions.Count; i < length; i++)
                    {
                        TerrainType region = _settings.Regions[i];
                        if (currentHeight <= region.Height)
                        {
                            colorMap[y * MapGenerationSettings.ChunkSize + x] = region.Color;
                            break;
                        }
                    }
                }
            }

            return colorMap;
        }

        #region Threading

        public void RequestMapData(System.Action<MapData> callback)
        {
            void ThreadStart() => MapDataThread(callback);
            new Thread(ThreadStart).Start();
        }

        private void MapDataThread(System.Action<MapData> callback)
        {
            MapData mapData = GenerateMapData();

            lock (_mapDataThreadInfoQueue)
            {
                _mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
            }
        }

        public void RequestMeshData(MapData mapData, System.Action<MeshData> callback)
        {
            void ThreadStart() => MeshDataThread(mapData, callback);
            new Thread(ThreadStart).Start();
        }

        private void MeshDataThread(MapData mapData, System.Action<MeshData> callback)
        {
            MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, _settings);

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