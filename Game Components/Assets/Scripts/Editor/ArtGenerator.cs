using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool that draws all of the game's art in code, pixel by pixel, and
/// saves it as sprites in Assets/Art. That way the project doesn't depend on
/// image files from anywhere else. Most of the pictures are gray, and each
/// object's SpriteRenderer tints them to the right color, so one block
/// picture works for the ground, the walls, and the ice. The player and the
/// background city are drawn in full color instead.
///
/// The level builders call this on their own. To redraw everything, use
/// Tools, Grappler, Regenerate Art.
/// </summary>
public static class ArtGenerator
{
    private const string Folder = "Assets/Art";

    // Each sprite is drawn the first time it's asked for, then loaded from Assets/Art after that.

    public static Sprite Block => GetOrCreate("Block", 16, 16, 16, new Vector4(4, 4, 4, 4), DrawBlock);
    public static Sprite Spikes => GetOrCreate("Spikes", 16, 8, 16, Vector4.zero, DrawSpikes, repeat: true);
    public static Sprite GrapplePoint => GetOrCreate("GrapplePoint", 16, 16, 16, new Vector4(5, 5, 5, 5), DrawGrapplePoint);
    public static Sprite Player => GetOrCreate("Player", 16, 32, 16, Vector4.zero, DrawPlayer);
    public static Sprite Sky => GetOrCreate("Sky", 4, 64, 4, Vector4.zero, DrawSky, smooth: true);
    // The three layers of city buildings in the background, from farthest to closest.
    // Each one is taller than the last, so the closer layers keep showing
    // buildings as the player climbs higher up the tower.
    public static Sprite CityFar => GetOrCreate("CityFar", 256, 256, 8, Vector4.zero,
        (px, w, h) => DrawCity(px, w, h, FarCity), repeat: true);
    public static Sprite CityMid => GetOrCreate("CityMid", 256, 384, 8, Vector4.zero,
        (px, w, h) => DrawCity(px, w, h, MidCity), repeat: true);
    public static Sprite CityNear => GetOrCreate("CityNear", 256, 576, 8, Vector4.zero,
        (px, w, h) => DrawCity(px, w, h, NearCity), repeat: true);
    public static Sprite Marker => GetOrCreate("AimMarker", 16, 16, 20, Vector4.zero, DrawMarker);
    public static Sprite Checker => GetOrCreate("Checker", 8, 8, 8, Vector4.zero, DrawChecker, repeat: true);

    /// <summary>
    /// Deletes the saved art and draws it all again. Runs from Tools, Grappler,
    /// Regenerate Art. Rebuild the level afterward to use the new art.
    /// </summary>
    [MenuItem("Tools/Grappler/Regenerate Art", priority = 50)]
    public static void Regenerate()
    {
        foreach (string name in new[] { "Block", "Spikes", "GrapplePoint", "Player", "Sky", "Towers", "CityFar", "CityMid", "CityNear", "AimMarker", "Checker" })
            AssetDatabase.DeleteAsset($"{Folder}/{name}.png");

        // Reading each property makes it draw and save that sprite again.
        _ = Block; _ = Spikes; _ = GrapplePoint; _ = Player; _ = Sky; _ = CityFar; _ = CityMid; _ = CityNear; _ = Marker; _ = Checker;
        Debug.Log("Grappler: art regenerated in Assets/Art. Run Build Test Level to apply it.");
    }

    // Drawing. Each method fills in a grid of pixels, starting at the bottom left.

    private delegate void Painter(Color[] px, int w, int h);

    // A beveled block with light top and left edges and dark bottom and right edges.
    private static void DrawBlock(Color[] px, int w, int h)
    {
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float v = 0.78f + (Hash(x, y) - 0.5f) * 0.08f; // Slightly speckled face
            if (y == h - 1 || x == 0) v = 1f;              // Lit top and left edges
            else if (y == h - 2 || x == 1) v = 0.93f;
            if (y == 0 || x == w - 1) v = 0.42f;           // Shaded bottom and right edges
            else if (y == 1 || x == w - 2) v = 0.6f;
            px[y * w + x] = new Color(v, v, v, 1f);
        }
    }

    // A row of two triangle spikes on a gray base. It repeats sideways for wider strips.
    private static void DrawSpikes(Color[] px, int w, int h)
    {
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int local = x % 8;
            float height = 8f * (1f - Mathf.Abs(local + 0.5f - 4f) / 4f); // Triangle 8 px wide
            Color c = Color.clear;
            if (y == 0) c = new Color(0.55f, 0.55f, 0.55f, 1f);           // Base strip
            else if (y < height) c = local < 4 ? Color.white : new Color(0.72f, 0.72f, 0.72f, 1f);
            px[y * w + x] = c;
        }
    }

    // A block with a ring in the middle, so grapple points look different from plain ground.
    private static void DrawGrapplePoint(Color[] px, int w, int h)
    {
        Vector2 center = new Vector2(w / 2f - 0.5f, h / 2f - 0.5f);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float v = 0.95f;
            bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1;
            if (edge) v = 0.35f;
            else if (x == 1 || y == h - 2) v = 1f;
            else if (x == w - 2 || y == 1) v = 0.7f;

            float d = Vector2.Distance(new Vector2(x, y), center);
            if (d < 3.2f && d > 1.8f) v = 0.45f; // A ring in the middle that looks like a hook point
            px[y * w + x] = new Color(v, v, v, 1f);
        }
    }

    // The player character.
    private static void DrawPlayer(Color[] px, int w, int h)
    {
        // This one is full color and isn't tinted. It's a small round climber with a scarf, facing right.
        Color body = new Color(0.55f, 0.78f, 1f);
        Color shade = new Color(0.4f, 0.6f, 0.9f);
        Color outline = new Color(0.08f, 0.1f, 0.22f);
        Color scarf = new Color(1f, 0.55f, 0.15f);
        Color eye = Color.white;
        Color pupil = new Color(0.05f, 0.05f, 0.12f);

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            Color c = Color.clear;
            if (InsideRounded(x, y, 1, 0, 14, 30, 4))
            {
                bool isEdge = !InsideRounded(x - 1, y, 1, 0, 14, 30, 4) || !InsideRounded(x + 1, y, 1, 0, 14, 30, 4) ||
                              !InsideRounded(x, y - 1, 1, 0, 14, 30, 4) || !InsideRounded(x, y + 1, 1, 0, 14, 30, 4);
                c = isEdge ? outline : (x < 5 ? shade : body);

                if (!isEdge && (y == 14 || y == 15)) c = scarf;                 // Scarf
                if (!isEdge && y >= 2 && y <= 3 && (x <= 6 || x >= 9)) c = outline; // Feet
                if (y >= 20 && y <= 24 && (x == 8 || x == 9 || x == 11 || x == 12)) c = eye;
                if (y >= 21 && y <= 23 && (x == 9 || x == 12)) c = pupil;         // Looking right
            }
            px[y * w + x] = c;
        }
        // Scarf tail behind the body.
        for (int y = 12; y <= 15; y++) px[y * w + 0] = scarf;
    }

    // A vertical gradient from dark blue at the top to lighter blue at the bottom.
    private static void DrawSky(Color[] px, int w, int h)
    {
        Color bottom = new Color(0.36f, 0.56f, 0.74f);
        Color top = new Color(0.05f, 0.07f, 0.18f);
        for (int y = 0; y < h; y++)
        {
            Color c = Color.Lerp(bottom, top, Mathf.SmoothStep(0f, 1f, y / (h - 1f)));
            for (int x = 0; x < w; x++) px[y * w + x] = c;
        }
    }
    // Settings for one layer of city buildings.
    private struct CityStyle
    {
        public int Seed;              // Changes which buildings get drawn, so each layer looks different.
        public Color Body;            // The building color.
        public Color LitWindow;       // A window with the lights on.
        public Color DarkWindow;      // A window with the lights off.
        public float MinHeight;       // The shortest building as a fraction of the picture's height.
        public int MaxWidth;          // The widest a building can be, in pixels.
        public int MaxGap;            // The widest gap between buildings, in pixels.
        public float LitChance;       // The lowest share of windows that are lit (each building adds up to 12% more).
        public int WindowW, WindowH;  // Window size in pixels.
        public int StepX, StepY;      // Distance from one window to the next, in pixels.
    }

    // Far buildings are lighter and bluer, like they're fading into the sky,
    // and they have tiny windows. Closer layers are darker with bigger windows.
    // The closest layer has fewer lit windows and wider gaps, so it doesn't
    // make the level pieces in front of it hard to see.
    private static readonly CityStyle FarCity = new CityStyle
    {
        Seed = 11, Body = new Color(0.2f, 0.26f, 0.42f), LitWindow = new Color(0.42f, 0.44f, 0.46f),
        DarkWindow = new Color(0.16f, 0.2f, 0.34f), MinHeight = 0.35f, MaxWidth = 26, MaxGap = 3,
        LitChance = 0.06f, WindowW = 1, WindowH = 1, StepX = 3, StepY = 4,
    };

    private static readonly CityStyle MidCity = new CityStyle
    {
        Seed = 23, Body = new Color(0.14f, 0.18f, 0.32f), LitWindow = new Color(0.55f, 0.5f, 0.36f),
        DarkWindow = new Color(0.1f, 0.13f, 0.24f), MinHeight = 0.4f, MaxWidth = 30, MaxGap = 4,
        LitChance = 0.06f, WindowW = 2, WindowH = 2, StepX = 4, StepY = 5,
    };

    private static readonly CityStyle NearCity = new CityStyle
    {
        Seed = 37, Body = new Color(0.08f, 0.1f, 0.2f), LitWindow = new Color(0.62f, 0.52f, 0.32f),
        DarkWindow = new Color(0.06f, 0.08f, 0.15f), MinHeight = 0.3f, MaxWidth = 30, MaxGap = 9,
        LitChance = 0.03f, WindowW = 2, WindowH = 3, StepX = 5, StepY = 6,
    };

    // Draws a row of skyscrapers from left to right. Each building gets its own
    // width, height, and shade, a grid of windows, and sometimes a narrower top
    // section, an antenna with a red light, or a box on the roof. No building
    // crosses the left or right edge, so the picture repeats sideways without a seam.
    private static void DrawCity(Color[] px, int w, int h, CityStyle style)
    {
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        int x = 1;
        for (int b = 0; ; b++)
        {
            int s = style.Seed;
            int width = 10 + (int)(Hash(b, s) * (style.MaxWidth - 10));
            if (x + width >= w - 1) break;

            int height = (int)(h * (style.MinHeight + (1f - style.MinHeight) * Hash(b, s + 1)));
            float shade = 0.85f + 0.3f * Hash(b, s + 2);
            Color body = new Color(Mathf.Min(1f, style.Body.r * shade), Mathf.Min(1f, style.Body.g * shade),
                                   Mathf.Min(1f, style.Body.b * shade), 1f);
            float litChance = style.LitChance + 0.12f * Hash(b, s + 3);

            // About half the buildings get narrower for their top 15%, like a setback.
            bool setback = Hash(b, s + 4) > 0.5f;
            int setbackStart = (int)(height * 0.85f);
            int inset = Mathf.Max(2, width / 5);

            for (int y = 0; y < height; y++)
            {
                int x0 = x, x1 = x + width;
                if (setback && y >= setbackStart) { x0 += inset; x1 -= inset; }

                for (int xx = x0; xx < x1; xx++)
                {
                    Color c = body;
                    if (xx == x0) c = Color.Lerp(body, Color.white, 0.12f);   // Lit left edge
                    if (xx == x1 - 1) c = Color.Lerp(body, Color.black, 0.25f); // Shaded right edge

                    // Windows in a grid, leaving a border around the edges and the roof.
                    int lx = xx - x0 - 2, ly = y - 3;
                    bool inWindowArea = lx >= 0 && lx < x1 - x0 - 3 && ly >= 0 && y < height - 3;
                    if (inWindowArea && lx % style.StepX < style.WindowW && ly % style.StepY < style.WindowH)
                    {
                        bool lit = Hash(xx / style.StepX + b * 31, y / style.StepY + s) < litChance;
                        c = lit ? style.LitWindow : style.DarkWindow;
                    }
                    px[y * w + xx] = c;
                }
            }

            // Roof details: an antenna with a red light, or a small box like a water tank.
            int center = x + width / 2;
            float roof = Hash(b, s + 5);
            if (roof > 0.55f)
            {
                int antenna = 4 + (int)(Hash(b, s + 6) * 10f);
                for (int y = height; y < Mathf.Min(h, height + antenna); y++) px[y * w + center] = body;
                if (height + antenna < h) px[(height + antenna) * w + center] = new Color(0.9f, 0.25f, 0.2f, 1f);
            }
            else if (roof > 0.3f && width > 12)
            {
                Color box = Color.Lerp(body, Color.black, 0.15f);
                for (int y = height; y < Mathf.Min(h, height + 3); y++)
                for (int xx = center - 2; xx < center + 2; xx++)
                    px[y * w + xx] = box;
            }

            x += width + 1 + (int)(Hash(b, s + 7) * style.MaxGap);
        }
    }

    // The aim marker, a broken ring with tick marks.
    private static void DrawMarker(Color[] px, int w, int h)
    {
        Vector2 c = new Vector2(w / 2f - 0.5f, h / 2f - 0.5f);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), c);
            bool ring = d > 5f && d < 6.6f;
            bool tick = (Mathf.Abs(x - c.x) < 1f && (y < 3 || y > h - 4)) ||
                        (Mathf.Abs(y - c.y) < 1f && (x < 3 || x > w - 4));
            bool gap = Mathf.Abs(x - c.x) < 1f || Mathf.Abs(y - c.y) < 1f; // Gaps in the ring at the ticks
            px[y * w + x] = (ring && !gap) || tick ? Color.white : Color.clear;
        }
    }

    // A checkerboard for the finish zone.
    private static void DrawChecker(Color[] px, int w, int h)
    {
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            bool light = ((x / 4) + (y / 4)) % 2 == 0;
            float v = light ? 1f : 0.3f;
            px[y * w + x] = new Color(v, v, v, 1f);
        }
    }

    // Helpers

    // True if the pixel is inside a rectangle with rounded corners.
    private static bool InsideRounded(int x, int y, int left, int bottom, int width, int height, int radius)
    {
        if (x < left || y < bottom || x >= left + width || y >= bottom + height) return false;
        int cx = Mathf.Clamp(x, left + radius, left + width - 1 - radius);
        int cy = Mathf.Clamp(y, bottom + radius, bottom + height - 1 - radius);
        return (x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius;
    }

    // Gives a random-looking number from 0 to 1 for each pixel. It's the same
    // every time for the same pixel, so the art looks the same every time it's drawn.
    private static float Hash(int x, int y)
    {
        uint n = (uint)(x * 374761393 + y * 668265263);
        n = (n ^ (n >> 13)) * 1274126177u;
        return ((n ^ (n >> 16)) & 0xFFFF) / 65536f;
    }

    // Loads the sprite from Assets/Art. If it's missing, this draws it, saves it
    // there, and sets the import settings so the pixels stay sharp.
    private static Sprite GetOrCreate(string name, int w, int h, float ppu, Vector4 border, Painter paint,
                                      bool repeat = false, bool smooth = false)
    {
        string path = $"{Folder}/{name}.png";
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        Directory.CreateDirectory(Folder);
        var px = new Color[w * h];
        paint(px, w, h);

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels(px);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = smooth ? FilterMode.Bilinear : FilterMode.Point;
        importer.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.spriteBorder = border;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; // Needed for tiled and sliced drawing.
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
