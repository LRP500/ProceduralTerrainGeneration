using System.Collections.Generic;
using UnityEngine;

namespace ProceduralTerrain
{
    public class TerrainSteaming : MonoBehaviour
    {
        #region Nested Types

        private class TerrainChunk
        {
            #region Private Fields

            private readonly GameObject _gameObject;
            private Vector2 _position;
            private Bounds _bounds;
            
            #endregion Private Fields

            #region Public Methods

            public TerrainChunk(Vector2 coord, int size, Transform parent)
            {
                _position = coord * size;
                _bounds = new Bounds(_position, Vector2.one * size);
                var worldPosition = new Vector3(_position.x, 0, _position.y);

                _gameObject = GameObject.CreatePrimitive(PrimitiveType.Plane);
                _gameObject.transform.SetParent(parent);
                _gameObject.transform.position = worldPosition;
                _gameObject.transform.localScale = Vector3.one * size / 10f;

                SetVisible(false);
            }

            /// <summary>
            /// Enables or disables mesh based on distance from viewer.
            /// </summary>
            public void Update()
            {
                float distanceFromNearestEdge = Mathf.Sqrt(_bounds.SqrDistance(ViewerPosition));
                bool visible = distanceFromNearestEdge <= MaxViewDistance;
                SetVisible(visible);
            }

            public void SetVisible(bool visible)
            {
                _gameObject.SetActive(visible);
            }

            public bool IsVisible()
            {
                return _gameObject.activeSelf;
            }

            #endregion Public Methods
        }
        
        #endregion Nested Types

        #region Constants

        private const float MaxViewDistance = 450;

        #endregion Constants

        [SerializeField]
        private Transform _viewer;

        #region Private Fields

        private int _chunkSize;
        private int _chunkVisibleInViewDistance;

        private readonly Dictionary<Vector2, TerrainChunk> _terrainChunks = new Dictionary<Vector2, TerrainChunk>();
        private readonly List<TerrainChunk> _visibleTerrainChunks = new List<TerrainChunk>();

        #endregion Private Fields

        private static Vector2 ViewerPosition { get; set; }

        #region MonoBehaviour

        private void Start()
        {
            _chunkSize = MapGenerationSettings.ChunkSize - 1;
            _chunkVisibleInViewDistance = Mathf.RoundToInt(MaxViewDistance / _chunkSize);
        }

        private void Update()
        {
            Vector3 currentPosition = _viewer.position;
            ViewerPosition = new Vector2(currentPosition.x, currentPosition.z);
            UpdateVisibleChunks();
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
                        
                chunk.Update();
                        
                if (chunk.IsVisible())
                {
                    _visibleTerrainChunks.Add(chunk);
                }
            }
            else
            {
                _terrainChunks.Add(coord, new TerrainChunk(coord, _chunkSize, transform));
            }
        }

        private void ResetVisibleChunks()
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
