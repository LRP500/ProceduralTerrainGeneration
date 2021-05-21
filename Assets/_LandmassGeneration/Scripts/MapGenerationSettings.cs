using System.Collections.Generic;
using ProceduralTerrain.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace ProceduralTerrain
{
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    [CreateAssetMenu(menuName = "Map Generation Settings")]
    public class MapGenerationSettings : ScriptableObject
    {
        #region Serialized Fields

        [SerializeField]
        private Vector2 _offset;

        [MinValue(1)]
        [SerializeField]
        private float _heightMultiplier = 1;

        [SerializeField]
        private AnimationCurve _heightCurve;

        [SerializeField]
        private bool _useFalloff;

        [SerializeField]
        private bool _useFlatShading;

        [Range(0, 6)]
        [SerializeField]
        [LabelText("LOD Preview")]
        private int _lodPreview = 1;

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

        [SerializeField]
        [ListDrawerSettings(CustomAddFunction = nameof(AddRegion), CustomRemoveElementFunction = nameof(RemoveRegion))]
        private List<TerrainType> _regions;

        #endregion Serialized Fields

        #region Properties

        public Vector2 Offset => _offset;
        public float HeightMultiplier => _heightMultiplier;
        public AnimationCurve HeightCurve => _heightCurve;
        public bool UseFalloff => _useFalloff;
        public bool UseFlatShading => _useFlatShading;
        public int LODPreview => _lodPreview;
        public int Seed => _seed;
        public float NoiseScale => _noiseScale;
        public int Octaves => _octaves;
        public float Persistance => _persistance;
        public float Lacunarity => _lacunarity;
        public List<TerrainType> Regions => _regions;

        #endregion Properties

        #region Editor

        private void AddRegion()
        {
            TerrainType region = this.CreateSubAsset(ref _regions);
            region.SetDestroyCallback(RemoveRegion);
        }

        private void RemoveRegion(TerrainType region)
        {
            _regions.Remove(region);
            this.DestroySubAsset(region);
        }

        #endregion Editor
    }
}