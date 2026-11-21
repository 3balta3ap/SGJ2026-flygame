using System;
using UnityEngine;

namespace FlyJam
{
    public static partial class GameEvents
    {
        public static event Action<string, Vector3> ItemEaten;   // item id ("apple", "banana"), where it was

        public static void RaiseItemEaten(string id, Vector3 p) => ItemEaten?.Invoke(id, p);
    }
}
