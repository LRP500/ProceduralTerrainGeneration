using UnityEngine;

namespace ProceduralTerrain
{
    public class Noise : MonoBehaviour
    {
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
        /// <param name="noiseData">The generation settings.</param>
        /// <param name="center">The world position to generate from.</param>
        /// <param name="chunkSize">The vertex count of a single terrain chunk.</param>
        /// <param name="normalizeMode">The height normalization mode.</param>
        /// <returns></returns>
        public static float[,] GenerateNoiseMap(NoiseData noiseData, Vector2 center, int chunkSize, NormalizeMode normalizeMode)
        {
            // Allows centering noise origin
            float halfWidth = chunkSize / 2f;
            float halfHeight = chunkSize / 2f;

            float[,] noiseMap = new float[chunkSize, chunkSize];
            float scale = noiseData.NoiseScale == 0 ? 0.0001f : noiseData.NoiseScale;
            
            var prng = new System.Random(noiseData.Seed);
            var octaveOffsets = GenerateOctaveOffsets(noiseData, center, prng, out float maxPossibleHeight);

            float minLocalHeight = float.MaxValue;
            float maxLocalheight = float.MinValue;

            for (int y = 0; y < chunkSize; ++y)
            {
                for (int x = 0; x < chunkSize; ++x)
                {
                    float amplitude = 1;
                    float frequency = 1;
                    float noiseHeight = 0;

                    for (int i = 0; i < noiseData.Octaves; i++)
                    {
                        // Note : octave offsets must be affected by scale and frequency to maintain consistency in noise
                        // shape no matter the sample position.
                        float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                        float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale  * frequency;
                        
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                        noiseHeight += perlinValue * amplitude;
                        
                        amplitude *= noiseData.Persistance;
                        frequency *= noiseData.Lacunarity;
                    }

                    minLocalHeight = Mathf.Min(minLocalHeight, noiseHeight);
                    maxLocalheight = Mathf.Max(maxLocalheight, noiseHeight);
                    
                    noiseMap[x, y] = noiseHeight;
                }
            }

            NormalizeMap(ref noiseMap, minLocalHeight, maxLocalheight, maxPossibleHeight, normalizeMode);

            return noiseMap;
        }

        /// <summary>
        /// Normalizes map inside min/max range.
        /// </summary>
        /// <param name="noiseMap"></param>
        /// <param name="minLocalHeight"></param>
        /// <param name="maxLocalHeight"></param>
        /// <param name="maxPossibleHeight">The maximum estimated height.</param>
        /// <param name="normalizeMode">The height normalization method.</param>
        private static void NormalizeMap(ref float[,] noiseMap,
            float minLocalHeight,
            float maxLocalHeight,
            float maxPossibleHeight,
            NormalizeMode normalizeMode)
        {
            var height = noiseMap.GetLength(0);
            var width = noiseMap.GetLength(1);
            
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    if (normalizeMode == NormalizeMode.Local)
                    {
                        // Normalize using calculated min and max heights
                        noiseMap[x, y] = Mathf.InverseLerp(minLocalHeight, maxLocalHeight, noiseMap[x, y]);
                    }
                    else if (normalizeMode == NormalizeMode.Global)
                    {
                        // Normalize using an estimated maximum height
                        const float factor = 2f;
                        float normalizedHeight = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight / factor);

                        // We clamp the normalized height to make sure we do not end up with negative values
                        noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
                    }
                }
            }
        }

        /// <summary>
        /// Samples octaves from random points on map.
        /// </summary>
        /// <param name="noiseData">The map generation settings.</param>
        /// <param name="center">The noise origin position.</param>
        /// <param name="rand">The pseudo-random number generator.</param>
        /// <param name="maxPossibleHeight">The maximum possible noise height value.</param>
        /// <returns></returns>
        private static Vector2[] GenerateOctaveOffsets(NoiseData noiseData, Vector2 center, System.Random rand, out float maxPossibleHeight)
        {
            float amplitude = 1;
            maxPossibleHeight = 0;         
            Vector2 offset = noiseData.Offset + center;
            var octaveOffsets = new Vector2[noiseData.Octaves];

            for (int i = 0, length = octaveOffsets.Length; i < length; ++i)
            {
                float offsetX = rand.Next(-100000, 100000) + offset.x;
                float offsetY = rand.Next(-100000, 100000) - offset.y;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);
                maxPossibleHeight += amplitude;
                amplitude *= noiseData.Persistance;
            }

            return octaveOffsets;
        }
    }
}