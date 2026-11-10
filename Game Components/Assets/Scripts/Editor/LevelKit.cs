using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Shared building blocks for the level builders: blocks, spikes, ice, moving
/// platforms, hint text, and the scene setup every level needs (player,
/// GameManager, aim marker, camera, background).
/// </summary>
public static class LevelKit
{
    private const string ArtFolder = "Assets/Art";
    private const string SquarePath = ArtFolder + "/WhiteSquare.png";
    private const string NoFrictionPath = ArtFolder + "/NoFriction.physicsMaterial2D";

    public static readonly Color GroundColor = new Color(0.55f, 0.58f, 0.68f);
    public static readonly Color WallColor = new Color(0.42f, 0.45f, 0.56f);
    public static readonly Color GrappleColor = new Color(1f, 0.62f, 0.2f);
    public static readonly Color HazardColor = new Color(1f, 0.3f, 0.3f);
    public static readonly Color TimedHazardColor = new Color(1f, 0.45f, 0.2f);
    public static readonly Color IceColor = new Color(0.7f, 0.92f, 1f);
    public static readonly Color FinishColor = new Color(0.4f, 1f, 0.55f, 0.55f);
    public static readonly Color MovingColor = new Color(0.45f, 0.75f, 1f);
    private static readonly Color FarTowerColor = new Color(0.1f, 0.13f, 0.28f);
    private static readonly Color NearTowerColor = new Color(0.16f, 0.2f, 0.36f);

    public static int GroundLayer => LayerMask.NameToLayer("Ground");
    public static int GrappleLayer => LayerMask.NameToLayer("Grappleable");

    /// <summary>Shows a dialog and returns false if the project layers are missing.</summary>
    public static bool CheckLayers()
    {
        if (GroundLayer >= 0 && GrappleLayer >= 0) return true;
        EditorUtility.DisplayDialog("Grappler",
            "Run Tools > Grappler > Set Up Project first to add the Ground and Grappleable layers.", "OK");
        return false;
    }

    // ----------------------------------------------------------------------
    // Level pieces
    // ----------------------------------------------------------------------

    /// <summary>Creates an empty root object for a level, replacing any old one with the same name.</summary>
    public static Transform NewRoot(string name)
    {
        DestroyIfExists(name);
        var root = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(root, "Build Level");
        return root.transform;
    }

    public static Transform Group(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        return go.transform;
    }

    /// <summary>A solid block on the Ground layer.</summary>
    public static GameObject Ground(string name, Vector2 position, Vector2 size, Transform parent, Color? color = null) =>
        Block(name, position, size, ArtGenerator.Block, color ?? GroundColor, GroundLayer, parent);

    /// <summary>An orange block the grappling hook can attach to.</summary>
    public static GameObject GrapplePoint(string name, Vector2 position, Vector2 size, Transform parent) =>
        Block(name, position, size, ArtGenerator.GrapplePoint, GrappleColor, GrappleLayer, parent);

    /// <summary>A slippery ice ledge.</summary>
    public static GameObject Ice(string name, Vector2 position, Vector2 size, Transform parent)
    {
        GameObject go = Block(name, position, size, ArtGenerator.Block, IceColor, GroundLayer, parent);
        go.AddComponent<SlipperySurface>();
        return go;
    }

    /// <summary>A strip of spikes. Position is the center of the strip; it's 0.5 units tall.</summary>
    public static GameObject Spikes(string name, Vector2 position, float width, Transform parent)
    {
        GameObject go = Block(name, position, new Vector2(width, 0.5f), ArtGenerator.Spikes, HazardColor, GroundLayer, parent);
        var box = go.GetComponent<BoxCollider2D>();
        box.size = new Vector2(width - 0.1f, 0.4f); // A little forgiving at the tips.
        box.offset = new Vector2(0f, -0.05f);
        go.AddComponent<Hazard>();
        return go;
    }

    /// <summary>Spikes that pop up and retract on a cycle. Offset staggers neighbors.</summary>
    public static GameObject TimedSpikes(string name, Vector2 position, float width, float phaseOffset, Transform parent)
    {
        GameObject go = Spikes(name, position, width, parent);
        go.GetComponent<SpriteRenderer>().color = TimedHazardColor;
        go.GetComponent<BoxCollider2D>().isTrigger = true;
        go.layer = 0; // Don't block the hook or count as ground.
        SetFloat(go.AddComponent<global::TimedSpikes>(), "phaseOffset", phaseOffset);
        return go;
    }

    /// <summary>A platform that moves back and forth between two points.</summary>
    public static GameObject MovingPlatform(string name, Vector2 from, Vector2 to, float width, float speed, Transform parent)
    {
        GameObject go = Block(name, from, new Vector2(width, 0.5f), ArtGenerator.Block, MovingColor, GroundLayer, parent);
        Transform a = Marker(name + " Point A", from, parent);
        Transform b = Marker(name + " Point B", to, parent);
        var mover = go.AddComponent<global::MovingPlatform>(); // Adds a Rigidbody2D.
        SetRef(mover, "pointA", a);
        SetRef(mover, "pointB", b);
        SetFloat(mover, "speed", speed);
        return go;
    }

    /// <summary>The checkered finish zone. Position is the center of the zone.</summary>
    public static GameObject Finish(Vector2 position, Vector2 size, Transform parent)
    {
        GameObject go = Block("Finish Line", position, size, ArtGenerator.Checker, FinishColor, 0, parent);
        go.GetComponent<BoxCollider2D>().isTrigger = true;
        go.AddComponent<FinishLine>();
        return go;
    }

    /// <summary>Floating hint text in the level.</summary>
    public static void Hint(string text, Vector2 position, Transform parent, float size = 3f)
    {
        var go = new GameObject("Hint: " + text);
        go.transform.SetParent(parent);
        go.transform.position = position;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size * 3f; // World-space text: about 0.1 units tall per point.
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 1f, 1f, 0.85f);
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.rectTransform.sizeDelta = new Vector2(24f, 3f);
        tmp.sortingOrder = 5;
    }

    public static GameObject Block(string name, Vector2 position, Vector2 size, Sprite sprite, Color color, int layer, Transform parent)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent);
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled; // Keeps the bevel edges and repeats the middle.
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;
        sr.color = color;

        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        return go;
    }

    public static Transform Marker(string name, Vector2 position, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        return go.transform;
    }

    // ----------------------------------------------------------------------
    // Scene setup shared by every level
    // ----------------------------------------------------------------------

    /// <summary>
    /// Puts the player at the spawn point and sets up the GameManager, aim marker,
    /// camera, and background. Returns false if there's no Player.
    /// </summary>
    public static bool SetUpScene(Vector2 spawn)
    {
        DestroyIfExists("Background");
        DestroyIfExists("Aim Marker");

        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Grappler", "No object named \"Player\" was found. Run Tools > Grappler > Set Up Project first.", "OK");
            return false;
        }

        Undo.RecordObject(player.transform, "Move Player");
        player.transform.position = new Vector3(spawn.x, spawn.y, 0f);

        var collider = player.GetComponent<Collider2D>();
        if (collider != null)
        {
            Undo.RecordObject(collider, "Set Player Material");
            collider.sharedMaterial = GetOrCreateNoFriction(); // Stops the player from sticking to walls.
        }
        StylePlayer(player);

        // GameManager with a timer. Reuse an existing one so the UI's links to it stay valid.
        GameManager manager = Object.FindAnyObjectByType<GameManager>();
        if (manager == null)
        {
            var managerObject = new GameObject("GameManager");
            Undo.RegisterCreatedObjectUndo(managerObject, "Build Level");
            manager = managerObject.AddComponent<GameManager>();
        }
        CompletionTimer timer = manager.GetComponent<CompletionTimer>();
        if (timer == null) timer = Undo.AddComponent<CompletionTimer>(manager.gameObject);
        SetRef(manager, "timer", timer);
        SetRef(manager, "player", player.GetComponent<PlayerController>());
        SetRef(manager, "grapple", player.GetComponent<GrapplingHook>());

        // Aim marker that shows where the hook will land.
        var aim = new GameObject("Aim Marker");
        Undo.RegisterCreatedObjectUndo(aim, "Build Level");
        var aimRenderer = aim.AddComponent<SpriteRenderer>();
        aimRenderer.sprite = ArtGenerator.Marker;
        aimRenderer.color = new Color(1f, 0.95f, 0.6f);
        aimRenderer.sortingOrder = 40;
        aimRenderer.enabled = false;
        SetRef(aim.AddComponent<AimIndicator>(), "grapple", player.GetComponent<GrapplingHook>());

        // Camera.
        Camera cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Set Camera");
            cam.orthographicSize = 8f;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.18f);
            Undo.RecordObject(cam.transform, "Move Camera");
            cam.transform.position = new Vector3(spawn.x, spawn.y + 2f, -10f);
            // Avoid ?? here: Unity objects can be "fake null", which ?? doesn't detect.
            CameraFollow follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = Undo.AddComponent<CameraFollow>(cam.gameObject);
            SetRef(follow, "target", player.transform);
        }

        BuildBackground(spawn);
        EditorSceneManager.MarkSceneDirty(player.scene);
        return true;
    }

    /// <summary>
    /// Gives the player the generated character art on a child "Body" (so
    /// squash and stretch don't resize the collider), adds PlayerVisuals, and
    /// styles the rope. Safe to run more than once.
    /// </summary>
    public static void StylePlayer(GameObject player)
    {
        var rootRenderer = player.GetComponent<SpriteRenderer>();
        if (rootRenderer != null) Undo.DestroyObjectImmediate(rootRenderer);

        Transform body = player.transform.Find("Body");
        if (body == null)
        {
            var bodyObject = new GameObject("Body");
            Undo.RegisterCreatedObjectUndo(bodyObject, "Style Player");
            bodyObject.transform.SetParent(player.transform, false);
            body = bodyObject.transform;
        }

        var sr = body.GetComponent<SpriteRenderer>();
        if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(body.gameObject);
        sr.sprite = ArtGenerator.Player;
        sr.drawMode = SpriteDrawMode.Simple;
        sr.color = Color.white;
        sr.sortingOrder = 10;

        var visuals = player.GetComponent<PlayerVisuals>();
        if (visuals == null) visuals = Undo.AddComponent<PlayerVisuals>(player);
        SetRef(visuals, "body", body);
        SetRef(visuals, "bodyRenderer", sr);

        var rope = player.GetComponentInChildren<LineRenderer>(true);
        if (rope != null)
        {
            Undo.RecordObject(rope, "Style Rope");
            rope.widthMultiplier = 0.07f;
            rope.startColor = new Color(1f, 0.92f, 0.75f);
            rope.endColor = new Color(1f, 0.7f, 0.35f);
            rope.numCapVertices = 2;
            rope.sortingOrder = 9;
        }

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Undo.RecordObject(rb, "Smooth Player");
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smoother with the following camera.
        }
    }

    private static void BuildBackground(Vector2 spawn)
    {
        var root = new GameObject("Background");
        Undo.RegisterCreatedObjectUndo(root, "Build Level");

        // Sky gradient that moves almost fully with the camera.
        var sky = new GameObject("Sky");
        sky.transform.SetParent(root.transform);
        sky.transform.position = new Vector3(spawn.x, spawn.y + 11.5f, 0f);
        sky.transform.localScale = new Vector3(60f, 2.2f, 1f); // The sprite is 1 x 16 units.
        var skyRenderer = sky.AddComponent<SpriteRenderer>();
        skyRenderer.sprite = ArtGenerator.Sky;
        skyRenderer.sortingOrder = -100;
        SetFloat(sky.AddComponent<Parallax>(), "followFactor", 0.95f);

        // Two layers of distant towers. They sink out of view as the player climbs above the city.
        TowerLayer(root.transform, "Far Towers", new Vector2(spawn.x, spawn.y - 0.5f), FarTowerColor, -90, 0.85f);
        TowerLayer(root.transform, "Near Towers", new Vector2(spawn.x, spawn.y - 2.5f), NearTowerColor, -80, 0.65f);
    }

    private static void TowerLayer(Transform parent, string name, Vector2 position, Color color, int order, float follow)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ArtGenerator.Towers;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(160f, 8f);
        sr.color = color;
        sr.sortingOrder = order;
        SetFloat(go.AddComponent<Parallax>(), "followFactor", follow);
    }

    // ----------------------------------------------------------------------
    // Utilities
    // ----------------------------------------------------------------------

    /// <summary>Sets a private [SerializeField] reference so it saves with the scene.</summary>
    public static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogWarning($"Grappler: field \"{field}\" not found on {target.GetType().Name}.");
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    public static void SetFloat(Object target, string field, float value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogWarning($"Grappler: field \"{field}\" not found on {target.GetType().Name}.");
            return;
        }
        prop.floatValue = value;
        so.ApplyModifiedProperties();
    }

    public static void DestroyIfExists(string name)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) Undo.DestroyObjectImmediate(existing);
    }

    public static PhysicsMaterial2D GetOrCreateNoFriction()
    {
        var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);
        if (existing != null) return existing;

        Directory.CreateDirectory(ArtFolder);
        var mat = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(mat, NoFrictionPath);
        AssetDatabase.SaveAssets();
        return mat;
    }
}
