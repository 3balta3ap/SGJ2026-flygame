using UnityEngine;

namespace FlyJam
{
    /// SphereWalker의 State와 CurrentSpeed만 읽어서 파리를 코드로 움직인다.
    /// 나중에 Blender 모델 + Animator로 바꿀 때는 이 컴포넌트만 교체하면 된다.
    public class FlyVisual : MonoBehaviour
    {
        [SerializeField] SphereWalker walker;
        /// 겉모습이 뒤돌아 있을 때 다리 위상을 거꾸로 (뒤돌아보기 기능이 설정한다)
        public bool ReverseGait { get; set; }
        [SerializeField] Transform bodyRoot;
        [Tooltip("순서: L1, R1, L2, R2, L3, R3 (1 = 앞다리)")]
        [SerializeField] Transform[] legs = new Transform[6];
        [SerializeField] Transform wingL, wingR;
        [SerializeField] Transform antennaL, antennaR;

        [Header("Walk")]
        [SerializeField] float stepsPerUnit = 1.6f;   // 이동 1m당 걸음 사이클 수
        [SerializeField] float legSwingAngle = 25f;
        [SerializeField] float legLiftAngle = 15f;
        [SerializeField] float bodyBob = 0.015f;
        [SerializeField] bool invertSwing = false;     // 다리가 거꾸로 움직이면 체크
        [SerializeField] bool invertLift = false;

        [Header("Idle")]
        [SerializeField] Vector2 groomInterval = new Vector2(2.5f, 5f);
        [SerializeField] float groomDuration = 1.2f;
        [SerializeField] Vector2 twitchInterval = new Vector2(0.4f, 1.2f);
        [SerializeField] float twitchAngle = 15f;

        [Header("Celebrate")]
        [SerializeField] float wingSpreadAngle = 60f;
        [SerializeField] float wingFlapAngle = 50f;
        [SerializeField] float wingFlapFrequency = 22f;

        [SerializeField] float blend = 12f;            // 포즈 복귀 속도

        Quaternion[] legRest;
        Quaternion wingLRest, wingRRest, antLRest, antRRest;
        Quaternion antLTarget, antRTarget;
        Vector3 bodyRest;
        float phase, groomTimer, groomT = -1f, twitchTimer;

        // 다리 i가 파리 왼쪽인지 (짝수 인덱스 = L)
        static bool IsLeft(int i) => i % 2 == 0;
        // 삼각 보행: A조 = L1, R2, L3 (0, 3, 4) / B조 = R1, L2, R3
        static bool IsTripodA(int i) => i == 0 || i == 3 || i == 4;

        void Awake()
        {
            legRest = new Quaternion[legs.Length];
            for (int i = 0; i < legs.Length; i++) legRest[i] = legs[i].localRotation;
            wingLRest = wingL.localRotation;
            wingRRest = wingR.localRotation;
            antLRest = antLTarget = antennaL.localRotation;
            antRRest = antRTarget = antennaR.localRotation;
            bodyRest = bodyRoot.localPosition;
            groomTimer = Random.Range(groomInterval.x, groomInterval.y);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            switch (walker.State)
            {
                case FlyState.Walking: Walk(dt); break;
                case FlyState.Stopped: Idle(dt); break;
                case FlyState.Dodging: Celebrate(dt); break;
                case FlyState.Celebrating: Celebrate(dt); break;
                default: ReturnToRest(dt); break;
            }
        }

        float K(float dt) => 1f - Mathf.Exp(-blend * dt);

        void Walk(float dt)
        {
            groomT = -1f;
            float s = ReverseGait ? -walker.SignedSpeed : walker.SignedSpeed;
            phase += s * stepsPerUnit * dt * Mathf.PI * 2f;
            float sSign = invertSwing ? -1f : 1f;
            float lSign = invertLift ? -1f : 1f;

            for (int i = 0; i < legs.Length; i++)
            {
                float p = phase + (IsTripodA(i) ? 0f : Mathf.PI);
                float side = IsLeft(i) ? 1f : -1f;
                float swing = Mathf.Sin(p) * legSwingAngle * side * sSign;
                float lift = Mathf.Max(0f, Mathf.Cos(p)) * legLiftAngle * -side * lSign;
                legs[i].localRotation = legRest[i] * Quaternion.Euler(0f, swing, lift);
            }

            bodyRoot.localPosition = bodyRest + Vector3.up * Mathf.Abs(Mathf.Sin(phase * 2f)) * bodyBob;
            FoldWings(dt);
            Antennae(dt);
        }

        void Idle(float dt)
        {
            bodyRoot.localPosition = Vector3.Lerp(bodyRoot.localPosition, bodyRest, K(dt));
            FoldWings(dt);
            Antennae(dt);

            // 그루밍: 가끔 앞다리 두 개로 얼굴을 닦는다
            if (groomT < 0f)
            {
                groomTimer -= dt;
                if (groomTimer <= 0f) groomT = 0f;
            }

            for (int i = 0; i < legs.Length; i++)
            {
                Quaternion target = legRest[i];
                bool frontLeg = i < 2;
                if (frontLeg && groomT >= 0f)
                {
                    float side = IsLeft(i) ? 1f : -1f;
                    float env = Mathf.Sin(groomT * Mathf.PI);                 // 0→1→0
                    float rub = Mathf.Sin(groomT * Mathf.PI * 8f) * 12f * env; // 비비기
                    float yaw = (70f * env + rub) * side * (invertSwing ? -1f : 1f);
                    float roll = 40f * env * -side * (invertLift ? -1f : 1f);
                    target = legRest[i] * Quaternion.Euler(0f, yaw, roll);
                    legs[i].localRotation = target;
                }
                else
                {
                    legs[i].localRotation = Quaternion.Slerp(legs[i].localRotation, target, K(dt));
                }
            }

            if (groomT >= 0f)
            {
                groomT += dt / groomDuration;
                if (groomT >= 1f)
                {
                    groomT = -1f;
                    groomTimer = Random.Range(groomInterval.x, groomInterval.y);
                }
            }
        }

        void Celebrate(float dt)
        {
            float t = Time.time * wingFlapFrequency * Mathf.PI * 2f;
            float flap = Mathf.Sin(t) * wingFlapAngle;
            wingL.localRotation = wingLRest * Quaternion.Euler(0f, -wingSpreadAngle, flap);
            wingR.localRotation = wingRRest * Quaternion.Euler(0f, wingSpreadAngle, -flap);

            bodyRoot.localPosition = bodyRest + Vector3.up * (0.03f * Mathf.Sin(Time.time * 8f));
            for (int i = 0; i < legs.Length; i++)
                legs[i].localRotation = Quaternion.Slerp(legs[i].localRotation, legRest[i], K(dt));
            Antennae(dt);
        }

        void ReturnToRest(float dt)
        {
            float k = K(dt);
            for (int i = 0; i < legs.Length; i++)
                legs[i].localRotation = Quaternion.Slerp(legs[i].localRotation, legRest[i], k);
            FoldWings(dt);
            bodyRoot.localPosition = Vector3.Lerp(bodyRoot.localPosition, bodyRest, k);
        }

        void FoldWings(float dt)
        {
            float k = K(dt);
            wingL.localRotation = Quaternion.Slerp(wingL.localRotation, wingLRest, k);
            wingR.localRotation = Quaternion.Slerp(wingR.localRotation, wingRRest, k);
        }

        void Antennae(float dt)
        {
            twitchTimer -= dt;
            if (twitchTimer <= 0f)
            {
                twitchTimer = Random.Range(twitchInterval.x, twitchInterval.y);
                antLTarget = antLRest * Quaternion.Euler(Random.Range(-twitchAngle, twitchAngle), Random.Range(-twitchAngle, twitchAngle), 0f);
                antRTarget = antRRest * Quaternion.Euler(Random.Range(-twitchAngle, twitchAngle), Random.Range(-twitchAngle, twitchAngle), 0f);
            }
            float k = 1f - Mathf.Exp(-20f * dt);
            antennaL.localRotation = Quaternion.Slerp(antennaL.localRotation, antLTarget, k);
            antennaR.localRotation = Quaternion.Slerp(antennaR.localRotation, antRTarget, k);
        }
    }
}
