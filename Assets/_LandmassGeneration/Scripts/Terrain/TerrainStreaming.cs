using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    [RequireComponent(typeof(MapGenerator))]
    public class TerrainStreaming : MonoBehaviour
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

        /// <summary>
        /// The distance from viewer from which chunk colliders are updated.
        /// </summary>
        private const float ColliderGenerationDistanceThreshold = 5;

        #endregion Constants

        #region Nested Types

        /// <summary>
        /// A single chunk of terrain at runtime.
        /// </summary>
        private class TerrainChunk
        {
            #region Private Fields

            private readonly GameObject _gameObject;
            private readonly Vector2 _sampleCenter;
            private Bounds _bounds;

            private readonly MeshRenderer _meshRenderer;
            private readonly MeshFilter _meshFilter;
            private readonly MeshCollider _meshCollider;

            private readonly LODInfo[] _detailLevels;
            private readonly LODMesh[] _lodMeshes;
            private readonly int _colliderLODIndex;

            private HeightMapGenerator.HeightMap _heightMap;
            private bool _heightMapReceived;
            private bool _hasSetCollider;
            private int _previousLODIndex = -1;

            #endregion Private Fields

            private bool IsVisible() => _gameObject.activeSelf;
            public Vector2 Coordinates { get; }

            #region Public Methods

            public TerrainChunk(Vector2 coord, float worldSize, List<LODInfo> detailLevels, int colliderLODIndex, Transform parent, Material material)
            {
                Coordinates = coord;
                _detailLevels = detailLevels.ToArray();
                _colliderLODIndex = colliderLODIndex;
               
                _sampleCenter = coord * worldSize / _mapGenerator.MeshSettings.Scale;
                Vector2 position = coord * worldSize;
                _bounds = new Bounds(position, Vector2.one * worldSize);

                _gameObject = new GameObject("Terrain Chunk");
                _gameObject.transform.position = new Vector3(position.x, 0, position.y);
                _gameObject.transform.SetParent(parent);
                
                _meshFilter = _gameObject.AddComponent<MeshFilter>();
                _meshCollider = _gameObject.AddComponent<MeshCollider>();
                _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
                _meshRenderer.material = material;
                
                SetVisible(false);

                // Initialize LOD meshes
                _lodMeshes = new LODMesh[detailLevels.Count];
                for (int i = 0, length = detailLevels.Count; i < length; ++i)
                {
                    _lodMeshes[i] = new LODMesh(detailLevels[i].level);
                    _lodMeshes[i].AddCallback(UpdateTerrainChunk);
                    
                    if (i == _colliderLODIndex)
                    {
                        _lodMeshes[i].AddCallback(UpdateCollisionMesh);
                    }
                }
                
                // Request map data
                _mapGenerator.RequestHeightMap(_sampleCenter, OnHeightMapReceived);
            }

            /// <summary>
            /// Updates terrain chunk based on distance from viewer.
            /// </summary>
            public void UpdateTerrainChunk()
            {
                if (_heightMapReceived)
                {
                    float viewerDistanceFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(ViewerPosition));
                    bool wasVisible = IsVisible();
                    bool visible = viewerDistanceFromNearestEdge <= _maxViewDistance;

                    if (visible)
                    {
                        SetMeshFromLODIndex(GetLODIndexFromViewerDistance(viewerDistanceFromNearestEdge));
                    }

                    if (wasVisible != visible)
                    {
                        if (visible)
                        {
                            _visibleTerrainChunks.Add(this);
                        }
                        else
                        {
                            _visibleTerrainChunks.Remove(this);
                        }
                    }

                    SetVisible(visible);
                }
            }

            private void SetVisible(bool visible)
            {
                _gameObject.SetActive(visible);
            }

            #endregion Public Methods

            #region Private Methods

            /// <summary>
            /// Returns the lod index based on distance from viewer.
            /// </summary>
            /// <param name="viewerDistance">The distance from viewer.</param>
            /// <returns>The LOD index.</returns>
            private int GetLODIndexFromViewerDistance(float viewerDistance)
            {
                int lodIndex = 0;

                for (int i = 0, length = _detailLevels.Length - 1; i < length; ++i)
                {
                    if (viewerDistance > _detailLevels[i].distanceThreshold)
                    {
                        lodIndex = i + 1;
                    }
                    else break;
                }

                return lodIndex;
            }

            private void SetMeshFromLODIndex(int lodIndex)
            {
                // Terrain chunk mesh
                if (lodIndex != _previousLODIndex)
                {
                    LODMesh lodMesh = _lodMeshes[lodIndex];

                    if (lodMesh.HasMesh)
                    {
                        _previousLODIndex = lodIndex;
                        _meshFilter.mesh = lodMesh.Mesh;
                    }
                    else if (!lodMesh.HasRequestedMesh)
                    {
                        lodMesh.RequestMesh(_heightMap);
                    }
                }
            }

            public void UpdateCollisionMesh()
            {
                if (_hasSetCollider) return;

                float sqrDstFromViewerToEdge = _bounds.SqrDistance(ViewerPosition);
                LODMesh lodMesh = _lodMeshes[_colliderLODIndex];

                if (sqrDstFromViewerToEdge < _detailLevels[_colliderLODIndex].SqrDistThreshold)
                {
                    if (!lodMesh.HasRequestedMesh)
                    {
                        lodMesh.RequestMesh(_heightMap);
                    }
                }

                if (sqrDstFromViewerToEdge < ColliderGenerationDistanceThreshold * ColliderGenerationDistanceThreshold)
                {
                    if (lodMesh.HasMesh)
                    {
                        _meshCollider.sharedMesh = lodMesh.Mesh;
                        _hasSetCollider = true;
                    }
                }
            }

            private void OnHeightMapReceived(HeightMapGenerator.HeightMap heightMap)
            {
                _heightMap = heightMap;
                _heightMapReceived = true;

                UpdateTerrainChunk();
            }

            #endregion Private Methods
        }

        /// <summary>
        /// A terrain chunk's mesh for a single level of detail.
        /// </summary>
        private class LODMesh
        {
            public Mesh Mesh { get; private set; }
            public bool HasRequestedMesh { get; private set; }
            public bool HasMesh { get; private set; }
            
            private readonly int _lod;
            private event System.Action _updateCallback;

            public LODMesh(int lod)
            {
                _lod = lod;
            }

            public void RequestMesh(HeightMapGenerator.HeightMap heightMap)
            {
                HasRequestedMesh = true;
                _mapGenerator.RequestMeshData(heightMap, _lod, OnMeshDataReceived);
            }

            public void AddCallback(System.Action callback)
            {
                _updateCallback += callback;
            }

            private void OnMeshDataReceived(MeshGenerator.MeshData meshData)
            {
                Mesh = meshData.CreateMesh();
                HasMesh = true;

                _updateCallback?.Invoke();
            }
        }

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
        private List<LODInfo> _detailLevels;

        [SerializeField]
        [Range(0, MeshSettings.SupportedLODCount - 1)]
        private int _colliderLODIndex;

        [SerializeField]
        private Transform _viewer;

        [SerializeField]
        private Material _mapMaterial;

        #region Private Fields

        private static float _maxViewDistance = 450;

        private Vector2 _lastViewerPosition;

        private float _meshWorldSize;
        private int _chunkVisibleInViewDistance;

        private readonly Dictionary<Vector2, TerrainChunk> _terrainChunks = new Dictionary<Vector2, TerrainChunk>();
        private static readonly List<TerrainChunk> _visibleTerrainChunks = new List<TerrainChunk>();

        private static MapGenerator _mapGenerator;

        #endregion Private Fields

        private static Vector2 ViewerPosition { get; set; }

        #region MonoBehaviour

        private void Start()
        {
            _mapGenerator = GetComponent<MapGenerator>();
            _meshWorldSize = _mapGenerator.MeshSettings.MeshWorldSize;
            _maxViewDistance = _detailLevels[_detailLevels.Count - 1].distanceThreshold;
            _chunkVisibleInViewDistance = Mathf.RoundToInt(_maxViewDistance / _meshWorldSize);

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

        private static void UpdatePreviouslyVisibleChunks(in HashSet<Vector2> updatedChunkCoords)
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
                _terrainChunks.Add(coord, new TerrainChunk(coord, _meshWorldSize, _detailLevels, _colliderLODIndex, transform, _mapMaterial));
            }
        }

        #endregion Private Methods
    }
}