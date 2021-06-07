using UnityEngine;

namespace ProceduralTerrain
{
    public class HeightMapGenerator : MonoBehaviour
    {
        #region Nested Types

        public readonly struct HeightMap
        {
            public readonly float[,] values;
            public readonly float minValue;
            public readonly float maxValue;

            public HeightMap(float[,] values, float minValue, float maxValue)
            {
                this.values = values;
                this.minValue = minValue;
                this.maxValue = maxValue;
            }
        }

        #endregion Nested Types

        /// <summary>
        /// Generates a height map from settings.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="settings"></param>
        /// <param name="sampleCenter"></param>
        /// <returns></returns>
        public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCenter)
        {
            float[,] values = Noise.GenerateNoiseMap(width, height, sampleCenter, settings.NoiseSettings);

            var threadSafeHeightCurve = new AnimationCurve(settings.HeightCurve.keys);

            float minValue = float.MaxValue;
            float maxValue = float.MinValue;

            for (int i = 0; i < width; ++i)
            {
                for (int j = 0; j < height; ++j)
                {
                    values[i, j] *= threadSafeHeightCurve.Evaluate(values[i, j]) * settings.HeightMultiplier;
                    minValue = Mathf.Min(minValue, values[i, j]);
                    maxValue = Mathf.Max(maxValue, values[i, j]);
                }
            }

            return new HeightMap(values, minValue, maxValue);
        }
    }
}
