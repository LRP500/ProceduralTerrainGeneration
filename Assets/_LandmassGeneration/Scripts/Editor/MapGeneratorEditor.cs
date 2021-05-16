using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace ProceduralTerrain
{
    [CustomEditor(typeof(MapGenerator))]
    public class MapGeneratorEditor : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
    }
}
