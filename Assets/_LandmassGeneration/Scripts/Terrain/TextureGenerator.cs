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
        public static Texture2D TextureFromHeightMap(float[,] heightMap)
        {
            int width = heightMap.GetLength(0);
            int height = heightMap.GetLength(1);
            
            Color[] colorMap = new Color[width * height];

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    colorMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
                }
            }

            return TextureFromColorMap(colorMap, width, height);
        }
    }
}
