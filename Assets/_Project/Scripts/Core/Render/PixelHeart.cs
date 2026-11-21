using UnityEngine;

namespace FlyJam
{
    /// 7×6 픽셀 하트 텍스처를 코드로 만든다 (외부 에셋 불필요).
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
