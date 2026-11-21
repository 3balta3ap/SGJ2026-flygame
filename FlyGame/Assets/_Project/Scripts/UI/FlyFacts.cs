using UnityEngine;

namespace FlyJam
{
    public static class FlyFacts
    {
        /// Picks one of "fact.1", "fact.2", ... from the text table. Add rows in the sheet to add facts.
        public static string Pick()
        {
            int count = 0;
            while (count < 99 && Loc.Has("fact." + (count + 1))) count++;

            string header = Loc.Get("fact.header");
            if (count == 0) return header;
            return header + "\n" + Loc.Get("fact." + Random.Range(1, count + 1));
        }
    }
}
