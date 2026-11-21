using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// Adds up vertical offsets for the model child ("Visual") from several features without them fighting:
    /// Level fever lifts it ("fever"), ponds sink it ("pond"). Put on the Fly root (the Mate variant inherits it).
    [DefaultExecutionOrder(100)]
    public class VisualOffset : MonoBehaviour
    {
        [Tooltip("The model child to move. Empty = child named 'Visual'")]
        [SerializeField] Transform target;

        readonly Dictionary<string, float> offsets = new Dictionary<string, float>();
        Vector3 basePos;
        bool ready;

        void Awake()
        {
            if (target == null) target = transform.Find("Visual");
            if (target != null)
            {
                basePos = target.localPosition;
                ready = true;
            }
        }

        /// Sets one named offset (local up, units). 0 removes its effect.
        public void Set(string key, float y)
        {
            if (!string.IsNullOrEmpty(key)) offsets[key] = y;
        }

        public float Get(string key) => offsets.TryGetValue(key, out float y) ? y : 0f;

        void LateUpdate()
        {
            if (!ready) return;
            float sum = 0f;
            foreach (var kv in offsets) sum += kv.Value;
            target.localPosition = basePos + Vector3.up * sum;
        }
    }
}
