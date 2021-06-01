using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ProceduralTerrain
{
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    [CreateAssetMenu(menuName = "Texture Data")]
    public class TextureData : ScriptableObject
    {
        #region Constants

        private static readonly int MinHeight = Shader.PropertyToID("minHeight");
        private static readonly int MaxHeight = Shader.PropertyToID("maxHeight");
        private static readonly int BaseColors = Shader.PropertyToID("baseColors");
        private static readonly int BaseStartHeights = Shader.PropertyToID("baseStartHeights");
        private static readonly int LayerCount = Shader.PropertyToID("layerCount");
        private static readonly int BaseBlends = Shader.PropertyToID("baseBlends");
        private static readonly int BaseColorStrength = Shader.PropertyToID("baseColorStrength");
        private static readonly int BaseTextureScales = Shader.PropertyToID("baseTextureScales");
        private static readonly int BaseTextures = Shader.PropertyToID("baseTextures");

        private const int TextureSize = 512;
        private const TextureFormat BaseTextureFormat = TextureFormat.RGB565;

        #endregion Constants

        #region Nested Types

        [System.Serializable]
        public class Layer
        {
            [SerializeField]
            private Texture2D _texture;
            
            [SerializeField]
            private Color _tint;
            
            [Range(0, 1)]
            [SerializeField]
            private float _tintStrength;
            
            [Range(0, 1)]
            [SerializeField]
            private float _startHeight;
            
            [Range(0, 1)]
            [SerializeField]
            private float _blendStrength;
            
            [SerializeField]
            private float _textureScale;

            public Texture2D Texture => _texture;
            public Color Tint => _tint;
            public float TintStrength => _tintStrength;
            public float StartHeight => _startHeight;
            public float BlendStrength => _blendStrength;
            public float TextureScale => _textureScale;
        }

        #endregion Nested Types

        [SerializeField]
        private Layer[] _layers;


        public void ApplyToMaterial(Material material)
        {
            material.SetInt(LayerCount, _layers.Length);
            material.SetColorArray(BaseColors, _layers.Select(x => x.Tint).ToArray());
            material.SetFloatArray(BaseColorStrength, _layers.Select(x => x.TintStrength).ToArray());
            material.SetFloatArray(BaseStartHeights, _layers.Select(x => x.StartHeight).ToArray());
            material.SetFloatArray(BaseBlends, _layers.Select(x => x.BlendStrength).ToArray());
            material.SetFloatArray(BaseTextureScales, _layers.Select(x => x.TextureScale).ToArray());
            
            Texture2DArray textureArray = GenerateTextureArray(_layers.Select(x => x.Texture).ToArray());
            material.SetTexture(BaseTextures, textureArray);
        }

        private static Texture2DArray GenerateTextureArray(Texture2D[] textures)
        {
            var textureArray = new Texture2DArray(TextureSize, TextureSize, textures.Length, BaseTextureFormat, true);
            
            for (int i = 0; i < textures.Length; ++i)
            {
                textureArray.SetPixels(textures[i].GetPixels(), i);
            }

            textureArray.Apply();
            return textureArray;
        }

        public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
        {
            material.SetFloat(MinHeight, minHeight);
            material.SetFloat(MaxHeight, maxHeight);
        }
    }
}
