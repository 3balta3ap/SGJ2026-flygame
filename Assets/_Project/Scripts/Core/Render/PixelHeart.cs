using UnityEngine;

namespace FlyJam
{
    /// Procedurally generates a 7x6 pixel heart texture (no external asset required).
    public static class PixelHeart
    {
        static Texture2D cached;

        public static Texture2D Get()
        {
            if (cached != null) return cached;
            string[] rows =
            {
                " XX XX ",
                "XXXXXXX",
                "XXXXXXX",
                " XXXXX ",
                "  XXX  ",
                "   X   ",
            };
            int w = 7, h = rows.Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = "PixelHeart"
            };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, h - 1 - y, rows[y][x] == 'X' ? Color.white : Color.clear);
            tex.Apply();
            cached = tex;
            return tex;
        }
    }
}
