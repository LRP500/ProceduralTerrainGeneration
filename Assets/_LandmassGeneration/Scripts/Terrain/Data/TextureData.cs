using Sirenix.OdinInspector;
using UnityEngine;

namespace ProceduralTerrain
{
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    [CreateAssetMenu(menuName = "Texture Data")]
    public class TextureData : ScriptableObject
    {
        public void ApplyToMaterial(Material material)
        {
        }
    }
}
