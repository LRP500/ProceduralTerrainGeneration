using Sirenix.OdinInspector;
using UnityEngine;

namespace ProceduralTerrain
{
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    [CreateAssetMenu(menuName = "Texture Data")]
    public class TextureData : ScriptableObject
    {
        private static readonly int MinHeight = Shader.PropertyToID("minHeight");
        private static readonly int MaxHeight = Shader.PropertyToID("maxHeight");
        private static readonly int BaseColors = Shader.PropertyToID("baseColors");
        private static readonly int BaseStartHeights = Shader.PropertyToID("baseStartHeights");
        private static readonly int BaseColorCount = Shader.PropertyToID("baseColorCount");

        [SerializeField]
        private Color[] _baseColors;

        [Range(0, 1)]
        [SerializeField]
        private float[] _baseStartHeights;

        //private float _previousMinHeight;
        //private float _previousMaxHeight;

        public void ApplyToMaterial(Material material)
        {
            material.SetInt(BaseColorCount, _baseColors.Length);
            material.SetColorArray(BaseColors, _baseColors);
            material.SetFloatArray(BaseStartHeights, _baseStartHeights);

            //UpdateMeshHeights(material, _previousMinHeight, _previousMaxHeight);
        }

        public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
        {
            //_previousMinHeight = minHeight;
            //_previousMaxHeight = maxHeight;
            material.SetFloat(MinHeight, minHeight);
            material.SetFloat(MaxHeight, maxHeight);
        }
    }
}
