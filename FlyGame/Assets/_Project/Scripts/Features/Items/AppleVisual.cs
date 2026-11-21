using UnityEngine;

namespace FlyJam
{
    /// Floats the apple model up and down and spins it slowly. Put on the Apple prefab root; 'visual' = the model child.
    public class AppleVisual : MonoBehaviour
    {
        [SerializeField] Transform visual;

        ItemBalance balance;
        Vector3 basePos;
        float phase;

        public void Init(ItemBalance b) => balance = b;

        void Awake()
        {
            if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
            if (visual != null) basePos = visual.localPosition;
            phase = Random.Range(0f, 10f);
        }

        void Update()
        {
            if (visual == null) return;
            float bob = balance != null ? balance.BobHeight : 0.12f;
            float speed = balance != null ? balance.BobSpeed : 2.5f;
            float spin = balance != null ? balance.SpinSpeed : 90f;
            visual.localPosition = basePos + Vector3.up * (Mathf.Sin((Time.time + phase) * speed) * bob);
            visual.Rotate(Vector3.up, spin * Time.deltaTime, Space.Self);
        }
    }
}
