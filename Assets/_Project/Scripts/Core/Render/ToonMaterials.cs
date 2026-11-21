using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    public static class ToonMaterials
    {
        public static Shader ToonShader;   // ToonMaterialSwapper / ToonifyOnSpawn이 인스펙터 참조로 넣는다 (빌드 포함 보장)

        static readonly Dictionary<(Material, float), Material> cache = new Dictionary<(Material, float), Material>();

        public static void Apply(Transform root, float layer)
        {
            if (root == null) return;
            if (ToonShader == null) ToonShader = Shader.Find("FlyJam/Toon");
            if (ToonShader == null) { Debug.LogError("[ToonMaterials] FlyJam/Toon 셰이더가 없습니다."); return; }

            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    var m = mats[i];
                    if (m == null || m.shader == null) continue;
                    string sn = m.shader.name;
                    if (sn == "FlyJam/Toon" || sn == "FlyJam/Halo" || sn == "FlyJam/Particle") continue;
                    mats[i] = Convert(m, layer, sn.Contains("Unlit"));
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        static Material Convert(Material src, float layer, bool emissive)
        {
            var key = (src, layer);
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var m = new Material(ToonShader) { name = src.name + "_Toon" };
            Color c = Color.white;
            if (src.HasProperty("_BaseColor")) c = src.GetColor("_BaseColor");
            else if (src.HasProperty("_Color")) c = src.GetColor("_Color");
            m.SetColor("_BaseColor", c);

            Texture t = null;
            if (src.HasProperty("_BaseMap")) t = src.GetTexture("_BaseMap");
            if (t == null && src.HasProperty("_MainTex")) t = src.GetTexture("_MainTex");
            if (t != null) m.SetTexture("_BaseMap", t);

            m.SetFloat("_Layer", layer);
            m.SetFloat("_Emissive", emissive ? 1f : 0f);
            cache[key] = m;
            return m;
        }
    }
}
