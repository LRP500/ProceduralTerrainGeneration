using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProceduralTerrain.Utils
{
#if UNITY_EDITOR

    public static class ScriptableObjectUtils
    {
        /// <summary>
        /// Creates subasset of type and returns it.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static TType CreateSubAsset<TType>(this ScriptableObject parent)
            where TType : ScriptableObject
        {
            var child = ScriptableObject.CreateInstance<TType>();
            child.name = typeof(TType).Name;
            child.hideFlags = parent.hideFlags;

            string path = AssetDatabase.GetAssetPath(parent);
            AssetDatabase.AddObjectToAsset(child, path);
            AssetDatabase.SaveAssets();

            Undo.RegisterCreatedObjectUndo(child, "Add sub asset to ScriptableObjet");

            return child;
        }

        /// <summary>
        /// Creates subasset of type and adds it to the list.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="parent"></param>
        /// <param name="list"></param>
        /// <returns></returns>
        public static TType CreateSubAsset<TType>(this ScriptableObject parent, ref List<TType> list)
            where TType : ScriptableObject
        {
            var child = parent.CreateSubAsset<TType>();
            list.Add(child);
            return child;
        }

        /// <summary>
        /// Destroys subasset from parent's path.
        /// </summary>
        /// <typeparam name="TType"></typeparam>
        /// <param name="parent"></param>
        /// <param name="child"></param>
        public static void DestroySubAsset<TType>(this ScriptableObject parent, TType child)
            where TType : ScriptableObject
        {
            Undo.RecordObject(child, string.Empty);
            Object.DestroyImmediate(child, true);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

#endif
}
