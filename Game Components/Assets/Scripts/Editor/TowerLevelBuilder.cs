using UnityEditor;
using UnityEngine;

/// <summary>
/// Adds "Tools > Grappler > Build Tower Level": the full Grappler tower from
/// the SRS. The player climbs about 100 units through four sections that get
/// harder as they go up:
///   1. Tutorial (y 0-20)        - wide ledges, big grapple points, control hints
///   2. Gaps (y 20-46)           - narrow ledges and long falls (R08)
///   3. Ice and movers (y 44-68) - slippery ice and moving platforms
///   4. Timing (y 68-101)        - timed spikes, sparse grapple points, the finish
/// There are no checkpoints: a missed grab can drop the player a long way (R06, R07).
/// It replaces the test level if one is in the scene. Run it again to rebuild.
/// </summary>
public static class TowerLevelBuilder
{
    private static readonly Color Section2Color = new Color(0.62f, 0.56f, 0.64f);
    private static readonly Color Section3Color = new Color(0.5f, 0.62f, 0.72f);
    private static readonly Color Section4Color = new Color(0.66f, 0.52f, 0.52f);

    [MenuItem("Tools/Grappler/Build Tower Level", priority = 20)]
    public static void Build()
    {
        if (!LevelKit.CheckLayers()) return;

        LevelKit.DestroyIfExists("Test Level");
        Transform level = LevelKit.NewRoot("Tower Level");

        BuildShell(LevelKit.Group("Walls", level));
        BuildTutorial(LevelKit.Group("1 - Tutorial", level));
        BuildGaps(LevelKit.Group("2 - Gaps", level));
        BuildIceAndMovers(LevelKit.Group("3 - Ice and Movers", level));
        BuildTiming(LevelKit.Group("4 - Timing", level));

        // Testing shortcut: F1-F4 jump to each section (Editor and development builds only).
        var teleporter = level.gameObject.AddComponent<SectionTeleporter>();
        var so = new SerializedObject(teleporter);
        SerializedProperty starts = so.FindProperty("sectionStarts");
        Vector2[] points = { new Vector2(0f, -1.5f), new Vector2(-7f, 19.4f), new Vector2(-2f, 45.4f), new Vector2(-7f, 66.4f) };
        starts.arraySize = points.Length;
        for (int i = 0; i < points.Length; i++) starts.GetArrayElementAtIndex(i).vector2Value = points[i];
        so.ApplyModifiedProperties();

        if (LevelKit.SetUpScene(new Vector2(0f, -1.5f)))
            Debug.Log("Grappler: tower level built. Press Ctrl+S to save the scene, then press Play. " +
                      "While testing, F1-F4 jump to each section.");
        Selection.activeTransform = level;
    }

    // Layout rule: the player can't jump, so every ledge has a grapple point above it
    // within reach (range 12, most under 10), and each next ledge sits below a grapple
    // point that's reachable from the previous ledge or from hanging on the last point.

    private static void BuildShell(Transform g)
    {
        LevelKit.Ground("Floor", new Vector2(0f, -3f), new Vector2(26f, 1f), g);
        LevelKit.Ground("Left Wall", new Vector2(-12.5f, 53f), new Vector2(1f, 114f), g, LevelKit.WallColor);
        LevelKit.Ground("Right Wall", new Vector2(12.5f, 53f), new Vector2(1f, 114f), g, LevelKit.WallColor);
        LevelKit.Ground("Ceiling", new Vector2(0f, 110.5f), new Vector2(26f, 1f), g, LevelKit.WallColor);
    }

    // Section 1: learn to walk, grapple, swing, and avoid spikes.
    private static void BuildTutorial(Transform g)
    {
        LevelKit.Hint("A / D to move", new Vector2(0f, 0.3f), g);
        LevelKit.Hint("Hold LEFT MOUSE on orange blocks to grapple", new Vector2(0f, 2f), g);
        LevelKit.Hint("While hooked: A / D to swing, W to reel in, S to let out", new Vector2(0f, 3.7f), g, 2.6f);

        LevelKit.Spikes("Floor Spikes", new Vector2(7f, -2.25f), 3f, g);
        LevelKit.Hint("Red spikes knock you back", new Vector2(7.5f, -1f), g, 2f);

        LevelKit.GrapplePoint("Grapple 1", new Vector2(0f, 8f), new Vector2(3f, 1f), g);
        LevelKit.Ground("Ledge 1L", new Vector2(-7f, 5f), new Vector2(5f, 0.5f), g);
        LevelKit.Ground("Ledge 1R", new Vector2(7f, 7f), new Vector2(5f, 0.5f), g);
        LevelKit.GrapplePoint("Grapple 2", new Vector2(-2f, 14f), new Vector2(2f, 1f), g);
        LevelKit.Ground("Ledge 2", new Vector2(6f, 12f), new Vector2(4f, 0.5f), g);
        LevelKit.GrapplePoint("Grapple 3", new Vector2(1f, 20f), new Vector2(2f, 1f), g);
        LevelKit.Ground("Ledge 3", new Vector2(-7f, 18f), new Vector2(5f, 0.5f), g);
    }

    // Section 2: narrow ledges with wide gaps between them.
    private static void BuildGaps(Transform g)
    {
        LevelKit.Hint("Miss a gap and you fall a long way", new Vector2(-5f, 22f), g, 2.4f);

        LevelKit.GrapplePoint("Grapple 4", new Vector2(-3f, 26f), new Vector2(1.5f, 1f), g);
        LevelKit.Ground("Narrow 1", new Vector2(1f, 23f), new Vector2(1.5f, 0.5f), g, Section2Color);
        LevelKit.GrapplePoint("Grapple 5", new Vector2(3f, 31f), new Vector2(1.5f, 1f), g);
        LevelKit.Ground("Narrow 2", new Vector2(7f, 28f), new Vector2(1.2f, 0.5f), g, Section2Color);
        LevelKit.GrapplePoint("Grapple 6", new Vector2(-2f, 36f), new Vector2(1.5f, 1f), g);
        LevelKit.Ground("Narrow 3", new Vector2(-7f, 33f), new Vector2(1.2f, 0.5f), g, Section2Color);
        LevelKit.GrapplePoint("Grapple 7", new Vector2(5f, 41f), new Vector2(1.5f, 1f), g);
        LevelKit.Ground("Narrow 4", new Vector2(9.5f, 38f), new Vector2(1.5f, 0.5f), g, Section2Color);
        LevelKit.GrapplePoint("Grapple 8", new Vector2(3f, 46f), new Vector2(1.5f, 1f), g);
    }

    // Section 3: ice that slides you toward spikes, and platforms that move.
    private static void BuildIceAndMovers(Transform g)
    {
        LevelKit.Ice("Ice 1", new Vector2(-4f, 44f), new Vector2(7f, 0.5f), g);
        LevelKit.Spikes("Ice 1 Spikes", new Vector2(-6.75f, 44.5f), 1.5f, g);
        LevelKit.Hint("Ice is slippery", new Vector2(-3f, 46.8f), g, 2.6f);

        LevelKit.GrapplePoint("Grapple 9", new Vector2(2f, 51f), new Vector2(1.5f, 1f), g);
        LevelKit.MovingPlatform("Mover 1", new Vector2(-8f, 49f), new Vector2(-1f, 49f), 3f, 2f, g);
        LevelKit.GrapplePoint("Grapple 10", new Vector2(-4f, 56f), new Vector2(1.5f, 1f), g);
        LevelKit.MovingPlatform("Mover 2", new Vector2(8.5f, 51f), new Vector2(8.5f, 58f), 3f, 1.5f, g);

        LevelKit.GrapplePoint("Grapple 11", new Vector2(6f, 64f), new Vector2(1.5f, 1f), g);
        LevelKit.Ice("Ice 2", new Vector2(4f, 61f), new Vector2(4f, 0.5f), g);
        LevelKit.Spikes("Ice 2 Spikes", new Vector2(2.5f, 61.5f), 1f, g);
        LevelKit.GrapplePoint("Grapple 12", new Vector2(-3f, 68f), new Vector2(1.5f, 1f), g);
        LevelKit.Ground("Rest Ledge", new Vector2(-7f, 65f), new Vector2(4f, 0.5f), g, Section3Color);
    }

    // Section 4: spikes that pop up on a timer, sparse grapple points, and the finish.
    private static void BuildTiming(Transform g)
    {
        LevelKit.Hint("Orange spikes pop up. Watch the timing.", new Vector2(-4f, 67.6f), g, 2.4f);

        LevelKit.GrapplePoint("Grapple 13", new Vector2(0f, 72f), new Vector2(1.5f, 1f), g);
        LevelKit.Ground("Timed Ledge 1", new Vector2(1f, 69f), new Vector2(7f, 0.5f), g, Section4Color);
        LevelKit.TimedSpikes("Timed Spikes 1", new Vector2(1f, 69.5f), 3f, 0f, g);
        LevelKit.GrapplePoint("Grapple 14", new Vector2(6f, 76f), new Vector2(1.5f, 1f), g);
        LevelKit.GrapplePoint("Grapple 15", new Vector2(-1f, 81f), new Vector2(1.2f, 1f), g);
        LevelKit.MovingPlatform("Mover 3", new Vector2(-8f, 79f), new Vector2(8f, 79f), 2.5f, 3f, g);

        LevelKit.GrapplePoint("Grapple 16", new Vector2(5f, 86f), new Vector2(1.2f, 1f), g);
        LevelKit.Ground("Timed Ledge 2", new Vector2(8f, 83f), new Vector2(4f, 0.5f), g, Section4Color);
        LevelKit.TimedSpikes("Timed Spikes 2", new Vector2(8f, 83.5f), 2f, 1.2f, g);
        LevelKit.GrapplePoint("Grapple 17", new Vector2(0f, 90f), new Vector2(1.2f, 1f), g);
        LevelKit.GrapplePoint("Grapple 18", new Vector2(-6f, 94f), new Vector2(1.2f, 1f), g);

        LevelKit.GrapplePoint("Grapple 19", new Vector2(-2f, 101f), new Vector2(2f, 1f), g);
        LevelKit.Ground("Top Ledge", new Vector2(2f, 97f), new Vector2(6f, 0.5f), g, Section4Color);
        LevelKit.Finish(new Vector2(2f, 98.5f), new Vector2(6f, 2.5f), g);
        LevelKit.Hint("Top of the tower", new Vector2(3f, 104f), g);
    }
}
