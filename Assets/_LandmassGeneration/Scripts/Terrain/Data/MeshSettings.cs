using Sirenix.OdinInspector;
using UnityEngine;

namespace ProceduralTerrain
{
    [InlineEditor(InlineEditorObjectFieldModes.Boxed)]
    [CreateAssetMenu(menuName = "Procedural Terrain/Mesh Settings")]
    public class MeshSettings : ScriptableObject
    {
        #region Constants

        /// <summary>
        /// The number of vertices making the LOD stitching border.
        /// </summary>
        private const int BorderSize = 5;

        /// <summary>
        /// The maximum number of levels of detail currently supported.
        /// </summary>
        public const int SupportedLODCount = 5;

        /// <summary>
        /// The number of supported chunk sizes.
        /// </summary>
        private const int SupportedChunkSizeCount = 9;
        
        /// <summary>
        /// The number of supported chunk sizes in flat shaded mode.
        /// </summary>
        private const int SupportedFlatShadedChunkSizeCount = 3;

        /// <summary>
        /// The supported chunk sizes.
        /// </summary>
        private static readonly int[] SupportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };
        
        #endregion Constants

        #region Serialized Fields

        [SerializeField]
        private float _scale = 1f;

        [SerializeField]
        private bool _useFlatShading;

        /// <summary>
        /// Use smaller chunk sizes for better performances.
        /// </summary>
        [SerializeField]
        [Range(0, SupportedChunkSizeCount - 1)]
        private int _chunkSizeIndex;

        /// <summary>
        /// Same as <see cref="_chunkSizeIndex"/> but for flat shaded mode.
        /// </summary>
        [SerializeField]
        [Range(0, SupportedFlatShadedChunkSizeCount- 1)]
        private int _flatShadedChunkSizeIndex;

        #endregion Serialized Fields

        #region Properties

        public float Scale => _scale;
        public bool UseFlatShading => _useFlatShading;

        /// <summary>
        /// The number of vertices per line of mesh rendered at LOD max.
        /// Includes 2 extra vertices for borders used to calculate seamless normals.
        /// </summary>
        public int VertexCountPerLine => SupportedChunkSizes[UseFlatShading ? _flatShadedChunkSizeIndex : _chunkSizeIndex] + BorderSize;

        /// <summary>
        /// The mesh size in world space.
        /// Includes borders for seamless normals calculation (<see cref="VertexCountPerLine"/>).
        /// </summary>
        public float MeshWorldSize => (VertexCountPerLine - 3) * _scale;

        #endregion Properties
    }
}