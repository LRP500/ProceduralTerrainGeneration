using System.Collections.Generic;
using UnityEngine;

using LODInfo = ProceduralTerrain.TerrainGenerator.LODInfo;

namespace ProceduralTerrain 
{
    /// <summary>
    /// A single chunk of terrain at runtime.
    /// </summary>
    public class TerrainChunk
    {
        #region Constants

        /// <summary>
        /// The distance from viewer from which chunk colliders are updated.
        /// </summary>
        private const float ColliderGenerationDistanceThreshold = 5;

        public event System.Action<TerrainChunk, bool> OnVisibilityChanged;

        #endregion Constants

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

        private readonly HeightMapSettings _heightMapSettings;
        private readonly MeshSettings _meshSettings;
        private readonly Transform _viewer;

        private readonly float _maxViewDistance;

        #endregion Private Fields

        private bool IsVisible() => _gameObject.activeSelf;
        private Vector2 ViewerPosition => _viewer.position;
        
        public Vector2 Coordinates { get; }

        #region Public Methods

        public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, List<LODInfo> detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material)
        {
            Coordinates = coord;
            _detailLevels = detailLevels.ToArray();
            _colliderLODIndex = colliderLODIndex;
            _heightMapSettings = heightMapSettings;
            _meshSettings = meshSettings;
            _viewer = viewer;
           
            _sampleCenter = coord * meshSettings.MeshWorldSize / meshSettings.Scale;
            Vector2 position = coord * meshSettings.MeshWorldSize;
            _bounds = new Bounds(position, Vector2.one * meshSettings.MeshWorldSize);

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

            _maxViewDistance = detailLevels[detailLevels.Count - 1].distanceThreshold;
        }

        public void Load()
        {
            ThreadedDataService.RequestData(() => HeightMapGenerator.GenerateHeightMap(
                    _meshSettings.VertexCountPerLine,
                    _meshSettings.VertexCountPerLine, 
                    _heightMapSettings, 
                    _sampleCenter), 
                OnHeightMapReceived);
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
                    SetVisible(visible);
                    OnVisibilityChanged?.Invoke(this, visible);
                }
            }
        }

        private void SetVisible(bool visible)
        {
            _gameObject.SetActive(visible);
        }

        public void SubscribeOnVisibilityChanged(System.Action<TerrainChunk, bool> callback)
        {
            OnVisibilityChanged += callback;
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
                    lodMesh.RequestMesh(_heightMap, _meshSettings);
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
                    lodMesh.RequestMesh(_heightMap, _meshSettings);
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

        private void OnHeightMapReceived(object heightMap)
        {
            _heightMap = (HeightMapGenerator.HeightMap) heightMap;
            _heightMapReceived = true;

            UpdateTerrainChunk();
        }

        #endregion Private Methods
    }

    /// <summary>
    /// A terrain chunk's mesh for a single level of detail.
    /// </summary>
    public class LODMesh
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

        public void RequestMesh(HeightMapGenerator.HeightMap heightMap, MeshSettings meshSettings)
        {
            HasRequestedMesh = true;
            ThreadedDataService.RequestData(
                () => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, _lod),
                OnMeshDataReceived);
        }

        public void AddCallback(System.Action callback)
        {
            _updateCallback += callback;
        }

        private void OnMeshDataReceived(object meshData)
        {
            Mesh = ((MeshGenerator.MeshData) meshData).CreateMesh();
            HasMesh = true;

            _updateCallback?.Invoke();
        }
    }
}
