using UnityEngine;

namespace FlyJam
{
    /// Places an object on the planet surface along 'direction'. Updates live in Edit Mode as well.
    [ExecuteAlways]
    public class SurfacePlacer : MonoBehaviour
    {
        [SerializeField] Planet planet;
        [SerializeField] Vector3 direction = Vector3.up;
        [SerializeField] float offset = 0f;   // Vertical offset above the surface (e.g., object radius)

        void Start() => Place();

        void Update()
        {
            if (!Application.isPlaying) Place();
        }

        public void Place()
        {
            if (planet == null && GameContext.Current != null) planet = GameContext.Current.Planet;
            if (planet == null || direction.sqrMagnitude < 1e-6f) return;
            Vector3 d = direction.normalized;
            transform.position = planet.SurfacePoint(d, offset);
            transform.rotation = Quaternion.FromToRotation(transform.up, d) * transform.rotation;
        }
    }
}
