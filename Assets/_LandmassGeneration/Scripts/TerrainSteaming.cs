using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEngine;

namespace ProceduralTerrain
{
    [RequireComponent(typeof(MapGenerator))]
    public class TerrainSteaming : MonoBehaviour
    {
        #region Constants

        /// <summary>
        /// The amount of distance the viewer needs to move from before updating terrain chunks.
        /// </summary>
        private const float ViewerMoveDistanceThreshold = 25f;
        
        /// <summary>
        /// Square root of <see cref="ViewerMoveDistanceThreshold"/> for calculation optimization.
        /// </summary>
        private const float SqrViewerMoveDistanceThreshold = ViewerMoveDistanceThreshold * ViewerMoveDistanceThreshold;

        /// <summary>
        /// Allows to scale generated terrain (to match player size for example)
        /// </summary>
        private const float Scale = 1f;

        #endregion Constants

        #region Nested Types

        /// <summary>
        /// A single chunk of terrain at runtime.
        /// </summary>
        private class TerrainChunk
        {
            #region Private Fields

            private readonly GameObject _gameObject;
            private readonly Vector2 _position;
            private Bounds _bounds;

            private readonly MeshRenderer _meshRenderer;
            private readonly MeshFilter _meshFilter;
            private readonly MeshCollider _meshCollider;

            private readonly LODInfo[] _detailLevels;
            private readonly LODMesh[] _lodMeshes;
            private readonly LODMesh _collisionMesh;
            
            private MapGenerator.MapData _mapData;
            private bool _mapDataReceived;
            private int _previousLODIndex = -1;

            #endregion Private Fields

            #region Public Methods

            public TerrainChunk(Vector2 coord, int size, List<LODInfo> detailLevels, Transform parent, Material material)
            {
                _detailLevels = detailLevels.ToArray();
                _position = coord * size;
                _bounds = new Bounds(_position, Vector2.one * size);
                var worldPosition = new Vector3(_position.x, 0, _position.y);

                // Initialize visual state
                _gameObject = new GameObject("Terrain Chunk");
                _gameObject.transform.SetParent(parent);
                _gameObject.transform.position = worldPosition * Scale;
                _gameObject.transform.localScale = Vector3.one * Scale;
                
                _meshFilter = _gameObject.AddComponent<MeshFilter>();
                _meshCollider = _gameObject.AddComponent<MeshCollider>();
                _meshRenderer = _gameObject.AddComponent<MeshRenderer>();
                _meshRenderer.material = material;
                
                SetVisible(false);

                // Initialize LOD meshes
                _lodMeshes = new LODMesh[detailLevels.Count];
                for (int i = 0, length = detailLevels.Count; i < length; ++i)
                {
                    _lodMeshes[i] = new LODMesh(detailLevels[i].level, UpdateTerrainChunk);

                    if (detailLevels[i].useForCollider)
                    {
                        _collisionMesh = _lodMeshes[i];
                    }
                }
                
                // Request map data
                _mapGenerator.RequestMapData(_position, OnMapDataReceived);
            }

            /// <summary>
            /// Updates terrain chunk based on distance from viewer.
            /// </summary>
            public void UpdateTerrainChunk()
            {
                if (_mapDataReceived)
                {
                    float viewerDistanceFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(ViewerPosition));
                    bool visible = viewerDistanceFromNearestEdge <= _maxViewDistance;

                    if (visible)
                    {
                        SetMeshFromLODIndex(GetLODIndexFromViewerDistance(viewerDistanceFromNearestEdge));
                        _visibleTerrainChunks.Add(this);
                    }

                    SetVisible(visible);
                }
            }

            public void SetVisible(bool visible)
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
                        lodMesh.RequestMesh(_mapData);
                    }
                }

                // Collider mesh
                if (lodIndex == 0)
                {
                   UpdateCollisionMesh();
                }
            }

            private void UpdateCollisionMesh()
            {
                if (_collisionMesh.HasMesh)
                {
                    _meshCollider.sharedMesh = _collisionMesh.Mesh;
                }
                else if (!_collisionMesh.HasRequestedMesh) 
                {
                    _collisionMesh.RequestMesh(_mapData);
                }
            }

            private void OnMapDataReceived(MapGenerator.MapData mapData)
            {
                _mapData = mapData;
                _mapDataReceived = true;

                Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap,
                    MapGenerator.ChunkSize,
                    MapGenerator.ChunkSize);

                _meshRenderer.material.mainTexture = texture;

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
            private readonly System.Action _updateCallback;

            public LODMesh(int lod, System.Action updateCallback)
            {
                _lod = lod;
                _updateCallback = updateCallback;
            }

            public void RequestMesh(MapGenerator.MapData mapData)
            {
                HasRequestedMesh = true;
                _mapGenerator.RequestMeshData(mapData, _lod, OnMeshDataReceived);
            }

            private void OnMeshDataReceived(MeshGenerator.MeshData meshData)
            {
                Mesh = meshData.CreateMesh();
                HasMesh = true;

                _updateCallback.Invoke();
            }
        }

        /// <summary>
        /// Editor settings for a single level of detail.
        /// </summary>
        [System.Serializable]
        public struct LODInfo
        {
            /// <summary>
            /// A higher number will reduce the amount of geometry.
            /// </summary>
            public int level;

            public float distanceThreshold;
            public bool useForCollider;
        }

        #endregion Nested Types

        [SerializeField]
        private List<LODInfo> _detailLevels;

        [SerializeField]
        private Transform _viewer;

        [SerializeField]
        private Material _mapMaterial;

        #region Private Fields

        private static float _maxViewDistance = 450;

        private Vector2 _lastViewerPosition;

        private int _chunkSize;
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
            _chunkSize = MapGenerator.ChunkSize - 1;
            _maxViewDistance = _detailLevels[_detailLevels.Count - 1].distanceThreshold;
            _chunkVisibleInViewDistance = Mathf.RoundToInt(_maxViewDistance / _chunkSize);

            UpdateVisibleChunks();
        }

        private void Update()
        {
            Vector3 currentPosition = _viewer.position;
            ViewerPosition = new Vector2(currentPosition.x, currentPosition.z) / Scale;

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
            ResetVisibleChunks();

            int currentChunkCoordX = Mathf.RoundToInt(ViewerPosition.x / _chunkSize);
            int currentChunkCoordY = Mathf.RoundToInt(ViewerPosition.y / _chunkSize);

            for (int yOffset = -_chunkVisibleInViewDistance; yOffset <= _chunkVisibleInViewDistance; ++yOffset)
            {
                for (int xOffset = -_chunkVisibleInViewDistance; xOffset <= _chunkVisibleInViewDistance; ++xOffset)
                {
                    UpdateChunkAtPosition(new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset));
                }
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
                _terrainChunks.Add(coord, new TerrainChunk(coord, _chunkSize, _detailLevels, transform, _mapMaterial));
            }
        }

        private static void ResetVisibleChunks()
        {
            for (int i = 0, length = _visibleTerrainChunks.Count; i < length; ++i)
            {
                _visibleTerrainChunks[i].SetVisible(false);
            }

            _visibleTerrainChunks.Clear();
        }

        #endregion Private Methods
    }
}
