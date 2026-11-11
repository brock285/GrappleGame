using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool behind Tools, Grappler, Build Test Level. It builds a small
/// practice room with one of each obstacle, so we can try out a mechanic
/// without climbing the tower. It replaces the tower level if that's in the
/// scene, and running it again rebuilds the room.
/// </summary>
public static class TestLevelBuilder
{
    /// <summary>Builds the test room and sets up the scene. Runs from Tools, Grappler, Build Test Level.</summary>
    [MenuItem("Tools/Grappler/Build Test Level", priority = 21)]
    public static void Build()
    {
        if (!LevelKit.CheckLayers()) return;

        LevelKit.DestroyIfExists("Tower Level");
        Transform level = LevelKit.NewRoot("Test Level");

        LevelKit.Ground("Floor", new Vector2(0f, -3f), new Vector2(22f, 1f), level);
        LevelKit.Ground("Left Wall", new Vector2(-11f, 17f), new Vector2(1f, 41f), level, LevelKit.WallColor);
        LevelKit.Ground("Right Wall", new Vector2(11f, 17f), new Vector2(1f, 41f), level, LevelKit.WallColor);

        LevelKit.Ground("Ledge 1", new Vector2(-6f, 5f), new Vector2(5f, 0.5f), level);
        LevelKit.Ice("Ice Ledge", new Vector2(6f, 11f), new Vector2(5f, 0.5f), level);
        LevelKit.Ground("Ledge 3", new Vector2(-4f, 17f), new Vector2(4f, 0.5f), level);
        LevelKit.Ground("Top Ledge", new Vector2(0f, 29f), new Vector2(6f, 0.5f), level);

        LevelKit.GrapplePoint("Grapple Point 1", new Vector2(0f, 6f), new Vector2(3f, 1f), level);
        LevelKit.GrapplePoint("Grapple Point 2", new Vector2(-2f, 12f), new Vector2(2f, 1f), level);
        LevelKit.GrapplePoint("Grapple Point 3", new Vector2(4f, 18f), new Vector2(2f, 1f), level);
        LevelKit.GrapplePoint("Grapple Point 4", new Vector2(-3f, 24f), new Vector2(2f, 1f), level);
        LevelKit.GrapplePoint("Grapple Point 5", new Vector2(2f, 33f), new Vector2(4f, 1f), level);

        LevelKit.Spikes("Spikes", new Vector2(4f, -2.25f), 2f, level);
        LevelKit.TimedSpikes("Timed Spikes", new Vector2(-6f, 5.5f), 2f, 0f, level);
        LevelKit.MovingPlatform("Moving Platform", new Vector2(-5f, 21f), new Vector2(5f, 21f), 3f, 2f, level);
        LevelKit.Finish(new Vector2(0f, 30.5f), new Vector2(6f, 2.5f), level);

        if (LevelKit.SetUpScene(new Vector2(0f, -1.5f)))
            Debug.Log("Grappler: test level built. Press Ctrl+S to save the scene, then press Play.");
        Selection.activeTransform = level;
    }
}
