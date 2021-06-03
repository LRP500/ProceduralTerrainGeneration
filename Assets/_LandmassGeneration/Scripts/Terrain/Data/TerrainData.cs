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

        #endregion Serialized Fields

        #region Properties

        public float HeightMultiplier => _heightMultiplier;
        public AnimationCurve HeightCurve => _heightCurve;
        public float WorldScale => _worldScale;
        public bool UseFalloff => _useFalloff;
        public bool UseFlatShading => _useFlatShading;

        public float MinHeight => WorldScale * HeightMultiplier * HeightCurve.Evaluate(0);
        public float MaxHeight => WorldScale * HeightMultiplier * HeightCurve.Evaluate(1);

        #endregion Properties
    }
}