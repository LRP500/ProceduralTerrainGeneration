using UnityEngine;

namespace ProceduralTerrain.Utils
{
    /// <summary>
    /// Simple component to hide a gameObject when entering play mode.
    /// </summary>
    public class HideOnPlay : MonoBehaviour
    {
        private void Awake()
        {
            gameObject.SetActive(false);
        }
    }
}
