using UnityEngine;

namespace FlyJam
{
    public static class FlyFacts
    {
        static readonly string[] facts =
        {
            "Fruit flies dodge obstacles hundreds of times faster than today's robots.",
            "Motion-blind fruit flies can't see things while they move. When an object stops, it seems to pop into view.",
            "Even without sight, a fruit fly stops when something is in front of it. Its antennae seem to feel the air flow.",
            "Blind fruit flies may also notice shadows with light receptors. (Research in progress)",
            "A male fruit fly sings a courtship song to the female - sometimes hundreds of times.",
        };

        public static string Pick() => "FLY FACT\n" + facts[Random.Range(0, facts.Length)];
    }
}
