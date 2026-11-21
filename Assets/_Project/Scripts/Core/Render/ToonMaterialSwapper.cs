using UnityEngine;

namespace FlyJam
{
    public class ToonMaterialSwapper : MonoBehaviour
    {
        [System.Serializable]
        public class Target
        {
            public Transform root;
            [Tooltip("0.5 = Ground (Planet, Pond), 1 = Object")]
            public float layer = 1f;
        }

        [SerializeField] Shader toonShader;
        [SerializeField] Target[] targets;

        void Start()
        {
            if (toonShader != null) ToonMaterials.ToonShader = toonShader;
            if (targets == null) return;
            foreach (var t in targets)
                if (t != null) ToonMaterials.Apply(t.root, t.layer);
        }
    }
}
