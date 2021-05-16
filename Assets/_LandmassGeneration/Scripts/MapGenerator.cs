using Sirenix.OdinInspector;
using UnityEngine;

namespace ProceduralTerrain
{
    [RequireComponent(typeof(MapDisplay))]
    public class MapGenerator : MonoBehaviour
    {
        #region Nested Types

        public struct MapData
        {
            public float[,] heightMap;
            public Color[] colorMap;

            public MapData(float[,] heightMap, Color[] colorMap)
            {
                this.heightMap = heightMap;
                this.colorMap = colorMap;
            }
        }

        #endregion Nested Types

        [SerializeField]
        [OnValueChanged(nameof(OnSettingsChanged), true)]
        private MapGenerationSettings _settings;

        public bool _autoUpdate = true;

        private MapDisplay _display;

        public void DrawMap()
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
