using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    public class TerrainGenerator : MonoBehaviour
    {
        #region Constants

        /// <summary>
        /// The distance the viewer needs to move from before updating terrain chunks.
        /// </summary>
        private const float ViewerMoveDistanceThreshold = 25f;
        
        /// <summary>
        /// Square root of <see cref="ViewerMoveDistanceThreshold"/> for calculation optimization.
        /// </summary>
        private const float SqrViewerMoveDistanceThreshold = ViewerMoveDistanceThreshold * ViewerMoveDistanceThreshold;

        #endregion Constants

        #region Nested Types

        /// <summary>
        /// Editor settings for a single level of detail.
        /// </summary>
        [System.Serializable]
        public struct LODInfo
        {
            /// <summary>
            /// The level of detail.
            /// A higher number will reduce the amount of geometry.
            /// </summary>
            [Range(0, MeshSettings.SupportedLODCount)]
            public int level;

            /// <summary>
            /// The distance from viewer at which LOD is active.
            /// </summary>
            public float distanceThreshold;

            /// <summary>
            /// The square root of the visible distance threshold;
            /// </summary>
            public float SqrDistThreshold => distanceThreshold * distanceThreshold;
        }

        #endregion Nested Types

        [SerializeField]
        private MeshSettings _meshSettings;

        [SerializeField]
        private HeightMapSettings _heightMapSettings;

        [SerializeField]
        private TextureSettings _textureSettings;

        [SerializeField]
        private List<LODInfo> _detailLevels;

        [SerializeField]
        private Transform _viewer;
        
        [SerializeField]
        private Material _terrainMaterial;

        [SerializeField]
        [Range(0, MeshSettings.SupportedLODCount - 1)]
        private int _colliderLODIndex;

        #region Private Fields

        private Vector2 _lastViewerPosition;

        private float _meshWorldSize;
        private int _chunkVisibleInViewDistance;

        private readonly Dictionary<Vector2, TerrainChunk> _terrainChunks = new Dictionary<Vector2, TerrainChunk>();
        private readonly List<TerrainChunk> _visibleTerrainChunks = new List<TerrainChunk>();

        #endregion Private Fields

        private Vector2 ViewerPosition { get; set; }

        #region MonoBehaviour

        private void Start()
        {
            _textureSettings.ApplyToMaterial(_terrainMaterial);
            TextureSettings.UpdateMeshHeights(_terrainMaterial, _heightMapSettings.MinHeight,_heightMapSettings.MaxHeight);
            _meshWorldSize = _meshSettings.MeshWorldSize;
            float maxViewDistance = _detailLevels[_detailLevels.Count - 1].distanceThreshold;
            _chunkVisibleInViewDistance = Mathf.RoundToInt(maxViewDistance / _meshWorldSize);

            UpdateVisibleChunks();
        }

        private void Update()
        {
            Vector3 currentPosition = _viewer.position;
            ViewerPosition = new Vector2(currentPosition.x, currentPosition.z);

            // Update terrain chunk colliders
            if (ViewerPosition != _lastViewerPosition)
            {
                for (int i = 0, length = _visibleTerrainChunks.Count; i < length; ++i)
                {
                    _visibleTerrainChunks[i].UpdateCollisionMesh();
                }
            }

            // Update only if viewer has moved enough.
            if ((_lastViewerPosition - ViewerPosition).sqrMagnitude > SqrViewerMoveDistanceThreshold)
            {
                _lastViewerPosition = ViewerPosition;
                UpdateVisibleChunks();
            }
        }

        #endregion MonoBehaviour

        #region Private Methods

        private void UpdateVisibleChunks()
        {
            HashSet<Vector2> updatedChunkCoords = new HashSet<Vector2>();
            UpdatePreviouslyVisibleChunks(in updatedChunkCoords);

            int currentChunkCoordX = Mathf.RoundToInt(ViewerPosition.x / _meshWorldSize);
            int currentChunkCoordY = Mathf.RoundToInt(ViewerPosition.y / _meshWorldSize);

            for (int yOffset = -_chunkVisibleInViewDistance; yOffset <= _chunkVisibleInViewDistance; ++yOffset)
            {
                for (int xOffset = -_chunkVisibleInViewDistance; xOffset <= _chunkVisibleInViewDistance; ++xOffset)
                {
                    var chunkCoordinates = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                    if (!updatedChunkCoords.Contains(chunkCoordinates))
                    {
                        UpdateChunkAtPosition(chunkCoordinates);
                    }
                }
            }
        }

        private void UpdatePreviouslyVisibleChunks(in HashSet<Vector2> updatedChunkCoords)
        {
            // We iterate backward as UpdateTerrainChunk might remove elements from the visible terrain chunks list.
            for (int i = _visibleTerrainChunks.Count - 1; i >= 0; --i)
            {
                updatedChunkCoords.Add(_visibleTerrainChunks[i].Coordinates);
                _visibleTerrainChunks[i].UpdateTerrainChunk();
            }
        }

        private void UpdateChunkAtPosition(Vector2 coord)
        {
            if (_terrainChunks.ContainsKey(coord))
            {
                TerrainChunk chunk = _terrainChunks[coord];
                chunk.UpdateTerrainChunk();
            }
            else
            {
                var newChunk = new TerrainChunk(coord,
                    _heightMapSettings,
                    _meshSettings,
                    _detailLevels,
                    _colliderLODIndex,
                    transform, _viewer, _terrainMaterial);

                newChunk.SubscribeOnVisibilityChanged(OnTerrainChunkVisibilityChanged);
                newChunk.Load();

                _terrainChunks.Add(coord, newChunk);
            }
        }

        private void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
        {
            if (isVisible)
            {
                _visibleTerrainChunks.Add(chunk);
            }
            else
            {
                _visibleTerrainChunks.Remove(chunk);
            }
        }

        #endregion Private Methods
    }
}