using Sirenix.OdinInspector;
using UnityEngine;

namespace ProceduralTerrain
{
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    [CreateAssetMenu(menuName = "Terrain Data")]
    public class TerrainData : ScriptableObject
    {
        #region Serialized Fields

        [MinValue(1)]
        [SerializeField]
        private float _heightMultiplier = 1;

        [SerializeField]
        private AnimationCurve _heightCurve;

        [SerializeField]
        private float _worldScale = 1f;

        [SerializeField]
        private bool _useFalloff;

        [SerializeField]
        private bool _useFlatShading;

        [Range(0, 6)]
        [SerializeField]
        [LabelText("LOD Preview")]
        private int _lodPreview = 1;

        #endregion Serialized Fields

        #region Properties

        public float HeightMultiplier => _heightMultiplier;
        public AnimationCurve HeightCurve => _heightCurve;
        public float WorldScale => _worldScale;
        public bool UseFalloff => _useFalloff;
        public bool UseFlatShading => _useFlatShading;
        public int LODPreview => _lodPreview;

        #endregion Properties
    }
}