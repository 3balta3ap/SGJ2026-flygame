using UnityEngine;

namespace FlyJam
{
    public class ToonifyOnSpawn : MonoBehaviour
    {
        [SerializeField] Shader toonShader;
        [SerializeField] float layer = 1f;

        void Awake()
        {
            if (toonShader != null) ToonMaterials.ToonShader = toonShader;
            ToonMaterials.Apply(transform, layer);
        }
    }
}
