using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool behind Tools, Grappler, Build Tower Level. It builds the full
/// tower described in our SRS. The player climbs about 135 units through five
/// stages, and each one gets harder and teaches one new thing.
///
/// Section 1 (y 0 to 20) is the tutorial, with wide ledges, big grapple points,
/// and hint text for the controls. Section 2 (y 20 to 46) has narrow ledges,
/// long falls (R08), and spikes waiting where a bad swing ends up. Section 3
/// (y 44 to 68) adds slippery ice and moving platforms. Section 4 (y 68 to 99)
/// has timed spikes and fewer grapple points. Section 5 (y 97 to 137) is the
/// last stretch and mixes everything from the earlier sections, with the
/// finish at the top.
///
/// There are no checkpoints, so a missed grab can drop the player a long way.
/// The level uses a snaking layout, though, so most falls land on an earlier
/// ledge instead of all the way at the bottom. Building the tower replaces the
/// test level if it's in the scene, and running it again rebuilds the tower.
/// </summary>
public static class TowerLevelBuilder
{
    // Each section has its own wall color, so players can tell which stage they're in.
    private static readonly Color Section2Color = new Color(0.62f, 0.56f, 0.64f);
    private static readonly Color Section3Color = new Color(0.5f, 0.62f, 0.72f);
    private static readonly Color Section4Color = new Color(0.66f, 0.52f, 0.52f);
    private static readonly Color Section5Color = new Color(0.6f, 0.6f, 0.46f);

    /// <summary>Builds the tower and sets up the scene. Runs from Tools, Grappler, Build Tower Level.</summary>
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
        BuildLastStretch(LevelKit.Group("5 - Last Stretch", level));

        // The F1 to F5 testing shortcut for jumping to each section (only in the Editor and development builds).
        var teleporter = level.gameObject.AddComponent<SectionTeleporter>();
        var so = new SerializedObject(teleporter);
        SerializedProperty starts = so.FindProperty("sectionStarts");
        Vector2[] points =
        {
            new Vector2(0f, -1.5f), new Vector2(-7f, 19.4f), new Vector2(-2f, 45.4f),
            new Vector2(-7f, 66.4f), new Vector2(1f, 98.4f),
        };
        starts.arraySize = points.Length;
        for (int i = 0; i < points.Length; i++) starts.GetArrayElementAtIndex(i).vector2Value = points[i];
        so.ApplyModifiedProperties();

        if (LevelKit.SetUpScene(new Vector2(0f, -1.5f)))
            Debug.Log("Grappler: tower level built. Press Ctrl+S to save the scene, then press Play. " +
                      "While testing, F1 to F5 jump to each section.");
        Selection.activeTransform = level;
    }

    // Layout rule: the player can't jump, so every ledge has a grapple point above
    // it within the hook's range of 12 (most are under 10). Each next ledge sits
    // below a grapple point the player can reach from the last ledge or while
    // hanging from the last point. We checked every distance, and we also
    // checked that no wall or ledge blocks the line from one point to the next
    // and that every floating spike strip is at least 1.5 units away from that line.

    // The floor at the bottom and the outer walls that run up the whole tower.
    private static void BuildShell(Transform g)
    {
        LevelKit.Ground("Floor", new Vector2(0f, -3f), new Vector2(26f, 1f), g);
        LevelKit.Ground("Left Wall", new Vector2(-12.5f, 71.5f), new Vector2(1f, 151f), g, LevelKit.WallColor);
        LevelKit.Ground("Right Wall", new Vector2(12.5f, 71.5f), new Vector2(1f, 151f), g, LevelKit.WallColor);
        LevelKit.Ground("Ceiling", new Vector2(0f, 147.5f), new Vector2(26f, 1f), g, LevelKit.WallColor);
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

    // Section 2: narrow ledges with wide gaps between them. Spikes float off
    // to the sides, so swinging too far or falling short gets punished.
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

        LevelKit.Spikes("Gap Spikes 1", new Vector2(6f, 25f), 2f, g);
        LevelKit.Spikes("Gap Spikes 2", new Vector2(-8f, 30.5f), 2f, g);
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

        // Spikes on the first ice ledge that pop up, so the player can't just slide to a stop anywhere.
        LevelKit.TimedSpikes("Ice 1 Timed Spikes", new Vector2(-2f, 44.5f), 1.5f, 0.6f, g);

        // Spikes hanging over the left end of the first moving platform's path.
        LevelKit.Spikes("Mover 1 Spikes", new Vector2(-6.5f, 53f), 2f, g);
    }

    // Section 4: spikes that pop up on a timer and fewer grapple points.
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

        LevelKit.Spikes("Float Spikes 4", new Vector2(-7f, 85f), 2f, g);

        // This used to be the top. Now it's a rest ledge before the last section,
        // with timed spikes on its right end.
        LevelKit.GrapplePoint("Grapple 19", new Vector2(-2f, 101f), new Vector2(2f, 1f), g);
        LevelKit.Ground("Rest Ledge 2", new Vector2(2f, 97f), new Vector2(6f, 0.5f), g, Section4Color);
        LevelKit.TimedSpikes("Timed Spikes 3", new Vector2(4f, 97.5f), 1.5f, 0.3f, g);
    }

    // Section 5: the last stretch. It mixes ice, a moving platform, narrow
    // ledges, timed spikes, and floating spikes, then the finish at the top.
    private static void BuildLastStretch(Transform g)
    {
        LevelKit.Hint("The last stretch mixes everything", new Vector2(-6f, 99.5f), g, 2.4f);

        LevelKit.GrapplePoint("Grapple 20", new Vector2(6f, 106f), new Vector2(1.5f, 1f), g);
        LevelKit.Ice("Ice 3", new Vector2(8f, 103f), new Vector2(4f, 0.5f), g);
        LevelKit.Spikes("Ice 3 Spikes", new Vector2(9.5f, 103.5f), 1f, g);

        LevelKit.GrapplePoint("Grapple 21", new Vector2(-1f, 111f), new Vector2(1.2f, 1f), g);
        LevelKit.MovingPlatform("Mover 4", new Vector2(-8f, 106f), new Vector2(-8f, 113f), 2.5f, 2f, g);
        LevelKit.Spikes("Float Spikes 5", new Vector2(7f, 112.5f), 2f, g);

        LevelKit.GrapplePoint("Grapple 22", new Vector2(5f, 116f), new Vector2(1.2f, 1f), g);
        LevelKit.Ground("Narrow 5", new Vector2(9f, 114f), new Vector2(1.2f, 0.5f), g, Section5Color);

        LevelKit.GrapplePoint("Grapple 23", new Vector2(-3f, 121f), new Vector2(1.2f, 1f), g);
        LevelKit.Ground("Timed Ledge 3", new Vector2(-6f, 118f), new Vector2(5f, 0.5f), g, Section5Color);
        LevelKit.TimedSpikes("Timed Spikes 4", new Vector2(-6f, 118.5f), 3f, 0.4f, g);

        LevelKit.GrapplePoint("Grapple 24", new Vector2(3f, 126f), new Vector2(1.2f, 1f), g);
        LevelKit.Spikes("Float Spikes 6", new Vector2(-6.5f, 125f), 2f, g);
        LevelKit.MovingPlatform("Mover 5", new Vector2(5f, 128f), new Vector2(10f, 128f), 2.5f, 2.5f, g);

        LevelKit.GrapplePoint("Grapple 25", new Vector2(-4f, 131f), new Vector2(1.2f, 1f), g);
        LevelKit.GrapplePoint("Grapple 26", new Vector2(-2f, 137f), new Vector2(2f, 1f), g);
        LevelKit.Ground("Top Ledge", new Vector2(2f, 133f), new Vector2(6f, 0.5f), g, Section5Color);
        LevelKit.Finish(new Vector2(2f, 134.5f), new Vector2(6f, 2.5f), g);
        LevelKit.Hint("Top of the tower", new Vector2(3f, 140f), g);
    }
}
