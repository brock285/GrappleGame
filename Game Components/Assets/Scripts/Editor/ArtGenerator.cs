using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws Grappler Game's placeholder art in code and saves it as sprites in
/// Assets/Art, so the project needs no image files. Most textures are
/// grayscale and get their color from the SpriteRenderer tint.
/// The level builder calls this; run "Tools > Grappler > Regenerate Art" to redraw.
/// </summary>
public static class ArtGenerator
{
    private const string Folder = "Assets/Art";

    public static Sprite Block => GetOrCreate("Block", 16, 16, 16, new Vector4(4, 4, 4, 4), DrawBlock);
    public static Sprite Spikes => GetOrCreate("Spikes", 16, 8, 16, Vector4.zero, DrawSpikes, repeat: true);
    public static Sprite GrapplePoint => GetOrCreate("GrapplePoint", 16, 16, 16, new Vector4(5, 5, 5, 5), DrawGrapplePoint);
    public static Sprite Player => GetOrCreate("Player", 16, 32, 16, Vector4.zero, DrawPlayer);
    public static Sprite Sky => GetOrCreate("Sky", 4, 64, 4, Vector4.zero, DrawSky, smooth: true);
    public static Sprite Towers => GetOrCreate("Towers", 64, 64, 8, Vector4.zero, DrawTowers, repeat: true);
    public static Sprite Marker => GetOrCreate("AimMarker", 16, 16, 20, Vector4.zero, DrawMarker);
    public static Sprite Checker => GetOrCreate("Checker", 8, 8, 8, Vector4.zero, DrawChecker, repeat: true);

    [MenuItem("Tools/Grappler/Regenerate Art", priority = 50)]
    public static void Regenerate()
    {
        foreach (string name in new[] { "Block", "Spikes", "GrapplePoint", "Player", "Sky", "Towers", "AimMarker", "Checker" })
            AssetDatabase.DeleteAsset($"{Folder}/{name}.png");

        // Touch each property to redraw it.
        _ = Block; _ = Spikes; _ = GrapplePoint; _ = Player; _ = Sky; _ = Towers; _ = Marker; _ = Checker;
        Debug.Log("Grappler: art regenerated in Assets/Art. Run Build Test Level to apply it.");
    }

    // ----------------------------------------------------------------------
    // Drawing
    // ----------------------------------------------------------------------

    private delegate void Painter(Color[] px, int w, int h);

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
            if (d < 3.2f && d > 1.8f) v = 0.45f; // Ring in the middle: a hook point
            px[y * w + x] = new Color(v, v, v, 1f);
        }
    }

    private static void DrawPlayer(Color[] px, int w, int h)
    {
        // Full color (not tinted): a small round climber with a scarf, facing right.
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

    private static void DrawTowers(Color[] px, int w, int h)
    {
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

        // Blocky silhouettes of distant towers, 8 px per column group.
        for (int col = 0; col < w / 8; col++)
        {
            int top = 20 + (int)(Hash(col, 7) * 40f);
            int x0 = col * 8 + (int)(Hash(col, 3) * 2f);
            int x1 = x0 + 5 + (int)(Hash(col, 5) * 2f);
            for (int y = 0; y < top; y++)
            for (int x = x0; x < Mathf.Min(x1, w); x++)
            {
                bool window = (x - x0) % 3 == 1 && y % 6 == 3 && Hash(x, y) > 0.55f;
                px[y * w + x] = window ? new Color(1f, 1f, 1f, 1f) : new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }
    }

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

    // ----------------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------------

    private static bool InsideRounded(int x, int y, int left, int bottom, int width, int height, int radius)
    {
        if (x < left || y < bottom || x >= left + width || y >= bottom + height) return false;
        int cx = Mathf.Clamp(x, left + radius, left + width - 1 - radius);
        int cy = Mathf.Clamp(y, bottom + radius, bottom + height - 1 - radius);
        return (x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius;
    }

    // Repeatable pseudo-random value in [0, 1) for a pixel, so art looks the same every time.
    private static float Hash(int x, int y)
    {
        uint n = (uint)(x * 374761393 + y * 668265263);
        n = (n ^ (n >> 13)) * 1274126177u;
        return ((n ^ (n >> 16)) & 0xFFFF) / 65536f;
    }

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
