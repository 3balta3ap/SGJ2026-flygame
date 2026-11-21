using UnityEngine;

namespace FlyJam
{
    /// direction 방향의 행성 표면에 오브젝트를 세운다. 에디터에서도 바로 반영된다.
    [ExecuteAlways]
    public class SurfacePlacer : MonoBehaviour
    {
        [SerializeField] Planet planet;
        [SerializeField] Vector3 direction = Vector3.up;
        [SerializeField] float offset = 0f;   // 오브젝트 반지름만큼 띄우기

        void Start() => Place();

        void Update()
        {
            if (!Application.isPlaying) Place();
        }

        public void Place()
        {
            if (planet == null || direction.sqrMagnitude < 1e-6f) return;
            Vector3 d = direction.normalized;
            transform.position = planet.SurfacePoint(d, offset);
            transform.rotation = Quaternion.FromToRotation(transform.up, d) * transform.rotation;
        }
    }
}
