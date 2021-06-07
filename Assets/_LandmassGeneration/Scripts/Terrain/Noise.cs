using Sirenix.OdinInspector;
using UnityEngine;
using Random = System.Random;

namespace ProceduralTerrain
{
    public class Noise : MonoBehaviour
    {
        #region Nested Types

        [System.Serializable]
        public class NoiseSettings
        {
            [SerializeField]
            private Vector2 _offset;

            [MinValue(0)]
            [SerializeField]
            private int _seed = 1;

            [SerializeField]
            [MinValue(0.01f)]
            private float _scale = 50f;

            [Range(1, 10)]
            [SerializeField]
            private int _octaves = 6;

            [Range(0, 1)]
            [SerializeField]
            private float _persistance = 0.5f;

            [MinValue(1)]
            [SerializeField]
            private float _lacunarity = 2;

            [SerializeField]
            private Noise.NormalizeMode _normalizeMode;

            public Vector2 Offset => _offset;
            public int Seed => _seed;
            public float Scale => _scale;
            public int Octaves => _octaves;
            public float Persistance => _persistance;
            public float Lacunarity => _lacunarity;
            public NormalizeMode NormalizeMode => _normalizeMode;
        }

        #endregion Nested Types

        /// <summary>
        /// Has min and max height for each terrain chunk will vary slightly,
        /// normalizing chunks of a large terrain based on each chunk's local min and max will result in visible seams.
        /// We use an estimated min and max when working with chunked terrains.
        /// Estimated global normalization might result in maximum heights being slighty cut-off. You can fix
        /// that by moving height curve maximum slightly above 1 in the generation settings.
        /// </summary>
        public enum NormalizeMode
        {
            /// <summary>
            /// Preferred way to normalize map height when generating a single chunk.
            /// </summary>
            Local,
            
            /// <summary>
            /// Resolves seams when working with chunked terrain streaming.
            /// </summary>
            Global
        }

        /// <summary>
        /// Generates and returns a noise map from settings.
        /// </summary>
        /// <param name="mapWidth"></param>
        /// <param name="mapHeight"></param>
        /// <param name="sampleCenter">The world position to generate from.</param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, Vector2 sampleCenter, NoiseSettings settings)
        {
            float[,] noiseMap = new float[mapWidth, mapHeight];

            float halfWidth = mapWidth / 2f;
            float halfHeight = mapHeight / 2f;
            float minLocalHeight = float.MaxValue;
            float maxLocalheight = float.MinValue;
            
            var prng = new Random(settings.Seed);
            var octaveOffsets = GenerateOctaveOffsets(settings, sampleCenter, prng, out float maxPossibleHeight);

            for (int y = 0; y < mapHeight; ++y)
            {
                for (int x = 0; x < mapWidth; ++x)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < settings.Octaves; i++)
                    {
                        // Note : octave offsets must be affected by scale and frequency to maintain consistency in noise
                        // shape no matter the sample position.
                        float sampleX = (x - halfWidth + octaveOffsets[i].x) / settings.Scale * frequency;
                        float sampleY = (y - halfHeight + octaveOffsets[i].y) / settings.Scale * frequency;
                        
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;
                        
                        amplitude *= settings.Persistance;
                        frequency *= settings.Lacunarity;
                    }

                    minLocalHeight = Mathf.Min(minLocalHeight, noiseHeight);
                    maxLocalheight = Mathf.Max(maxLocalheight, noiseHeight);

                    noiseMap[x, y] = noiseHeight;

                    if (settings.NormalizeMode == NormalizeMode.Global)
                    {
                        noiseMap[x, y] = NormalizeEstimate(noiseHeight, maxPossibleHeight);
                    }
                }
            }

            if (settings.NormalizeMode == NormalizeMode.Local)
            {
                Normalize(ref noiseMap, minLocalHeight, maxLocalheight);
            }

            return noiseMap;
        }

        /// <summary>
        /// Normalize height value based on estimated max possible height.
        /// </summary>
        /// <param name="value">The height value to normalize.</param>
        /// <param name="maxPossibleHeight">The maximum possible height.</param>
        /// <returns>The normalized height value.</returns>
        private static float NormalizeEstimate(float value, float maxPossibleHeight)
        {
            // Normalize using an estimated maximum height
            const float factor = 2f; 
            float normalizedHeight = (value + 1) / (2f * maxPossibleHeight / factor);
            return Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
        }

        /// <summary>
        /// Normalizes local map inside min/max range.
        /// </summary>
        /// <param name="noiseMap"></param>
        /// <param name="minLocalHeight"></param>
        /// <param name="maxLocalHeight"></param>
        private static void Normalize(ref float[,] noiseMap, float minLocalHeight, float maxLocalHeight)
        {
            var height = noiseMap.GetLength(0);
            var width = noiseMap.GetLength(1);
            
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalHeight, maxLocalHeight, noiseMap[x, y]);
                }
            }
        }

        /// <summary>
        /// Samples octaves from random points on map.
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="sampleCenter"></param>
        /// <param name="rand">The pseudo-random number generator.</param>
        /// <param name="maxPossibleHeight">The maximum possible noise height value.</param>
        /// <returns></returns>
        private static Vector2[] GenerateOctaveOffsets(NoiseSettings settings, Vector2 sampleCenter, Random rand, out float maxPossibleHeight)
        {
            float amplitude = 1;
            maxPossibleHeight = 0;         
            var octaveOffsets = new Vector2[settings.Octaves];

            for (int i = 0, length = octaveOffsets.Length; i < length; ++i)
            {
                float offsetX = rand.Next(-100000, 100000) + settings.Offset.x + sampleCenter.x;
                float offsetY = rand.Next(-100000, 100000) - settings.Offset.y - sampleCenter.y;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
                maxPossibleHeight += amplitude;
                amplitude *= settings.Persistance;
            }

            return octaveOffsets;
        }
    }
}
