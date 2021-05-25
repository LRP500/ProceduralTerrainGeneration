using UnityEngine;

namespace ProceduralTerrain
{
    public static class FalloffGenerator
    {
        public static float[,] GenerateFalloffMap(int size)
        {
            float[,] map = new float[size, size];

            for (int i = 0; i < size; ++i)
            {
                for (int j = 0; j < size; ++j)
                {
                    float x = i / (float) size * 2 - 1;
                    float y = j / (float) size * 2 - 1;

                    float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                    map[i, j] = Evaluate(value);
                }
            }

            return map;
        }

        private static float Evaluate(float value)
        {
            const float a = 3;
            const float b = 2.2f;
            float pow = Mathf.Pow(value, a); 
            return pow / (pow + Mathf.Pow(b - b * value, a));
        }
    }
}
