using Sirenix.OdinInspector;
using UnityEngine;

namespace ProceduralTerrain
{
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    [CreateAssetMenu(menuName = "Noise Data")]
    public class NoiseData : ScriptableObject
    {
        #region Serialized Fields

        [SerializeField]
        private Vector2 _offset;

        [MinValue(0)]
        [SerializeField]
        private int _seed = 1;

        [MinValue(0.0001f)]
        [SerializeField]
        private float _noiseScale = 20f;

        [Range(1, 10)]
        [SerializeField]
        private int _octaves = 3;

        [Range(0, 1)]
        [SerializeField]
        private float _persistance = 0.5f;

        [MinValue(1)]
        [SerializeField]
        private float _lacunarity = 2;

        #endregion Serialized Fields

        #region Properties

        public Vector2 Offset => _offset;
        public int Seed => _seed;
        public float NoiseScale => _noiseScale;
        public int Octaves => _octaves;
        public float Persistance => _persistance;
        public float Lacunarity => _lacunarity;

        #endregion Properties
    }
}