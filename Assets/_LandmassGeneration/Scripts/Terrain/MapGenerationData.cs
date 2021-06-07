using Sirenix.OdinInspector;
using UnityEngine;

namespace ProceduralTerrain
{
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    [CreateAssetMenu(menuName = "Map Generation Data")]
    public class MapGenerationData : ScriptableObject
    {
        #region Serialized Fields

        [SerializeField]
        private HeightMapSettings _heightMapSettings;

        [SerializeField]
        private MeshSettings _meshSettings;

        #endregion Serialized Fields

        #region Properties

        public HeightMapSettings HeightMapSettings => _heightMapSettings;
        public MeshSettings MeshSettings => _meshSettings;

        #endregion Properties
    }
}