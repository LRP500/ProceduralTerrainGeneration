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
        private NoiseData _noiseData;

        [SerializeField]
        private TerrainData _terrainData;

        #endregion Serialized Fields

        #region Properties

        public NoiseData NoiseData => _noiseData;
        public TerrainData TerrainData => _terrainData;

        #endregion Properties
    }
}