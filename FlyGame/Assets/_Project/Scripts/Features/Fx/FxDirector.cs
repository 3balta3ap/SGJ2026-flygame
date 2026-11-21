using UnityEngine;

namespace FlyJam
{
    /// One-shot particle effects for gameplay events. Listens to GameEvents only.
    public class FxDirector : MonoBehaviour
    {
        [SerializeField] Planet planet;
        [SerializeField] Transform player;
        [SerializeField] ParticleSystem dodgeTrail, hitBurst, heartBurst, matePop, perfectBurst;
        [Tooltip("Item pickup burst (apple, banana). Empty = uses perfectBurst")]
        [SerializeField] ParticleSystem appleBurst;

        void Awake()
        {
            var ctx = GameContext.Current;
            if (ctx != null)
            {
                if (planet == null) planet = ctx.Planet;
                if (player == null && ctx.Player != null) player = ctx.Player.transform;
            }
            if (heartBurst == null) return;
            var mat = heartBurst.GetComponent<ParticleSystemRenderer>().material;
            var tex = PixelHeart.Get();
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
        }

        void OnEnable()
        {
            GameEvents.PlayerDodged += OnDodged;
            GameEvents.PlayerHit += OnHit;
            GameEvents.CourtshipFinished += OnCourtship;
            GameEvents.MateAppeared += OnMateAppeared;
            GameEvents.PerfectDodge += OnPerfect;
            GameEvents.ItemEaten += OnItem;
        }

        void OnDisable()
        {
            GameEvents.PlayerDodged -= OnDodged;
            GameEvents.PlayerHit -= OnHit;
            GameEvents.CourtshipFinished -= OnCourtship;
            GameEvents.MateAppeared -= OnMateAppeared;
            GameEvents.PerfectDodge -= OnPerfect;
            GameEvents.ItemEaten -= OnItem;
        }

        void OnDodged(Vector3 pos, Vector3 dir)
        {
            Vector3 up = planet.UpAt(pos);
            PlayAt(dodgeTrail, pos + up * 0.3f, Quaternion.LookRotation(-dir, up));
        }

        void OnHit() => PlayUp(hitBurst, player.position, 0.3f);

        void OnCourtship(bool ok, Vector3 pos)
        {
            if (ok) PlayUp(heartBurst, pos, 0.6f);
        }

        void OnMateAppeared(Vector3 pos) => PlayUp(matePop, pos, 0.3f);

        void OnPerfect() => PlayUp(perfectBurst, player.position, 0.4f);

        void OnItem(string id, Vector3 pos) => PlayUp(appleBurst != null ? appleBurst : perfectBurst, pos, 0.4f);

        void PlayUp(ParticleSystem ps, Vector3 pos, float lift)
        {
            Vector3 up = planet.UpAt(pos);
            PlayAt(ps, pos + up * lift, Quaternion.FromToRotation(Vector3.up, up));
        }

        static void PlayAt(ParticleSystem ps, Vector3 pos, Quaternion rot)
        {
            if (ps == null) return;
            ps.transform.SetPositionAndRotation(pos, rot);
            ps.Play(true);
        }
    }
}
