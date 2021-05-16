using UnityEngine;

namespace ProceduralTerrain
{
    public class Noise : MonoBehaviour
    {
        /// <summary>
        /// Generates and returns a noise map from settings.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static float[,] GenerateNoiseMap(MapGenerationSettings settings)
        {
            float[,] noiseMap = new float[MapGenerationSettings.ChunkSize, MapGenerationSettings.ChunkSize];
            float scale = settings.NoiseScale == 0 ? 0.0001f : settings.NoiseScale;
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            const float halfWidth = MapGenerationSettings.ChunkSize / 2f; // Allows centering noise origin
            const float halfHeight = MapGenerationSettings.ChunkSize / 2f;
            var prng = new System.Random(settings.Seed);
            var octaveOffsets = GenerateOctaveOffsets(settings.Octaves, settings.Offset, prng);

            for (int y = 0; y < MapGenerationSettings.ChunkSize; ++y)
            {
                for (int x = 0; x < MapGenerationSettings.ChunkSize; ++x)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < settings.Octaves; i++)
                    {
                        float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
                        float sampleY = (y - halfHeight) / scale  * frequency  + octaveOffsets[i].y;
                        
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;
                        
                        // Update amplitude and frequency
                        amplitude *= settings.Persistance;
                        frequency *= settings.Lacunarity;
                    }

                    minHeight = Mathf.Min(minHeight, noiseHeight);
                    maxHeight = Mathf.Max(maxHeight, noiseHeight);

                    noiseMap[x, y] = noiseHeight;
                }
            }

            NormalizeMap(ref noiseMap, minHeight, maxHeight);

            return noiseMap;
        }

        /// <summary>
        /// Normalizes map inside min/max range.
        /// </summary>
        /// <param name="noiseMap"></param>
        /// <param name="minHeight"></param>
        /// <param name="maxHeight"></param>
        private static void NormalizeMap(ref float[,] noiseMap, float minHeight, float maxHeight)
        {
            var height = noiseMap.GetLength(0);
            var width = noiseMap.GetLength(1);
            
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    noiseMap[x, y] = Mathf.InverseLerp(minHeight, maxHeight, noiseMap[x, y]);
                }
            }
        }

        /// <summary>
        /// Samples octaves from random points on map.
        /// </summary>
        /// <param name="octaves"></param>
        /// <param name="offset"></param>
        /// <param name="prng"></param>
        /// <returns></returns>
        private static Vector2[] GenerateOctaveOffsets(int octaves, Vector2 offset, System.Random prng)
        {
            var octaveOffsets = new Vector2[octaves];

            for (int i = 0; i < octaves; ++i)
            {
                float offsetX = prng.Next(-100000, 100000) + offset.x;
                float offsetY = prng.Next(-100000, 100000) + offset.y;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
            }

            return octaveOffsets;
        }
    }
}
