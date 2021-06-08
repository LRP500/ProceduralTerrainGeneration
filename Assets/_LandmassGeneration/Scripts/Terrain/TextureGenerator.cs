using UnityEngine;

namespace ProceduralTerrain
{
    public static class TextureGenerator
    {
        /// <summary>
        /// Generates a texture from a color map and returns it.
        /// </summary>
        /// <param name="colorMap"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height)
        {
            var texture = new Texture2D(width, height)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            texture.SetPixels(colorMap);
            texture.Apply();
            return texture;
        }

        /// <summary>
        /// Generates a texture from a height map and returns it.
        /// </summary>
        /// <param name="heightMap"></param>
        /// <returns></returns>
        public static Texture2D TextureFromHeightMap(HeightMapGenerator.HeightMap heightMap)
        {
            int width = heightMap.values.GetLength(0);
            int height = heightMap.values.GetLength(1);
            
            Color[] colorMap = new Color[width * height];

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    float t = Mathf.InverseLerp(heightMap.minValue, heightMap.maxValue, heightMap.values[x, y]);
                    colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, t);
                }
            }

            return TextureFromColorMap(colorMap, width, height);
        }
    }
}
