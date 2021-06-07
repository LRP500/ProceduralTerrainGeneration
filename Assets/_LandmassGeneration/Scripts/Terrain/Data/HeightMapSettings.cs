using Sirenix.OdinInspector;
using UnityEngine;

namespace ProceduralTerrain
{
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    [CreateAssetMenu(menuName = "Procedural Terrain/Height Map Settings")]
    public class HeightMapSettings : ScriptableObject
    {
        #region Serialized Fields

        [SerializeField]
        private Noise.NoiseSettings _noiseSettings;

        [MinValue(1)]
        [SerializeField]
        private float _heightMultiplier = 1;

        [SerializeField]
        private AnimationCurve _heightCurve;

        [SerializeField]
        private bool _useFalloff;
        
        #endregion Serialized Fields

        #region Properties

        public Noise.NoiseSettings NoiseSettings => _noiseSettings;
        public float HeightMultiplier => _heightMultiplier;
        public AnimationCurve HeightCurve => _heightCurve;
        public bool UseFalloff => _useFalloff;

        public float MinHeight => HeightMultiplier * HeightCurve.Evaluate(0);
        public float MaxHeight => HeightMultiplier * HeightCurve.Evaluate(1);

        #endregion Properties
    }
}