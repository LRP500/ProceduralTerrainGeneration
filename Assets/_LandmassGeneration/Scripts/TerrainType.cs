using Sirenix.OdinInspector;
using UnityEngine;

namespace ProceduralTerrain
{
    [CreateAssetMenu]
    [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Hidden)]
    public class TerrainType : ScriptableObject
    {
        [SerializeField]
        private string _name;

        [SerializeField]
        private float _height;

        [SerializeField]
        private Color _color;

        public string Name => _name;
        public float Height => _height;
        public Color Color => _color;

        private System.Action<TerrainType> DestroyCallback;

        public void RemoveRegion()
        {
            DestroyCallback?.Invoke(this);
        }

        public void SetDestroyCallback(System.Action<TerrainType> callback)
        {
            DestroyCallback = callback;
        }
    }
}