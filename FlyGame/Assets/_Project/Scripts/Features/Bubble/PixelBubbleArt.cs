using UnityEngine;

namespace FlyJam
{
    /// Speech bubble art made in code (no image files): a 12x12 rounded box with a 1-pixel dark outline (9-slice border 4)
    /// and a 7x4 tail. Point filtering keeps the pixels sharp.
    public static class PixelBubbleArt
    {
        static Sprite box, tail;

        public static Color Fill = new Color(0.98f, 0.97f, 0.92f, 1f);
        public static Color Line = new Color(0.16f, 0.15f, 0.2f, 1f);

        public static Sprite Box()
        {
            if (box != null) return box;
            string[] rows =
            {
                "  ########  ",
                " #........# ",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                " #........# ",
                "  ########  ",
            };
            box = Make(rows, "PixelBubbleBox", new Vector4(4, 4, 4, 4));
            return box;
        }

        public static Sprite Tail()
        {
            if (tail != null) return tail;
            string[] rows =
            {
                "#.....#",
                " #...# ",
                "  #.#  ",
                "   #   ",
            };
            tail = Make(rows, "PixelBubbleTail", Vector4.zero);
            return tail;
        }

        static Sprite Make(string[] rows, string name, Vector4 border)
        {
            int w = rows[0].Length, h = rows.Length;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = name
            };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    char c = rows[y][x];
                    tex.SetPixel(x, h - 1 - y, c == '#' ? Line : c == '.' ? Fill : Color.clear);
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
    }
}
