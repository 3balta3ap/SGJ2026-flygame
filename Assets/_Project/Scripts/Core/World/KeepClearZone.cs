using System.Collections.Generic;
using UnityEngine;

namespace FlyJam
{
    /// 이 오브젝트 주변 radius 안에는 마을 오브젝트를 생성하지 않는다 (플레이어·짝 시작점 등).
    /// 마을 생성기가 플레이어나 짝을 직접 몰라도 되게 하는 표식.
    [ExecuteAlways]
    [DefaultExecutionOrder(-500)]
    public class KeepClearZone : MonoBehaviour
    {
        [SerializeField] float radius = 3f;

        static readonly List<KeepClearZone> all = new List<KeepClearZone>();
        public static IReadOnlyList<KeepClearZone> All => all;
        public float Radius => radius;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        void OnEnable()
        {
            if (!all.Contains(this)) all.Add(this);
        }

        void OnDisable() => all.Remove(this);
    }
}
