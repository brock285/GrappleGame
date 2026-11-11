using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor helpers that both level builders share. It has methods that make
/// each kind of level piece (ground, grapple points, ice, spikes, timed
/// spikes, moving platforms, the finish, and hint text). Its SetUpScene
/// method adds the rest of what every level needs, which is the player at the
/// spawn point, the GameManager and timer, the aim marker, the camera, and
/// the background. Keeping this in one place means a new level only has to
/// describe its layout (NFR7).
/// </summary>
public static class LevelKit
{
    private const string ArtFolder = "Assets/Art";
    private const string SquarePath = ArtFolder + "/WhiteSquare.png";
    private const string NoFrictionPath = ArtFolder + "/NoFriction.physicsMaterial2D";

    // Colors for each kind of piece, so players can tell them apart at a glance.
    public static readonly Color GroundColor = new Color(0.55f, 0.58f, 0.68f);
    public static readonly Color WallColor = new Color(0.42f, 0.45f, 0.56f);
    public static readonly Color GrappleColor = new Color(1f, 0.62f, 0.2f);
    public static readonly Color HazardColor = new Color(1f, 0.3f, 0.3f);
    public static readonly Color TimedHazardColor = new Color(1f, 0.45f, 0.2f);
    public static readonly Color IceColor = new Color(0.7f, 0.92f, 1f);
    public static readonly Color FinishColor = new Color(0.4f, 1f, 0.55f, 0.55f);
    public static readonly Color MovingColor = new Color(0.45f, 0.75f, 1f);

    // Layer numbers, looked up by name. They're -1 if Set Up Project hasn't been run yet.
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

    // Level pieces. Positions are the center of the piece, in world units.

    /// <summary>Creates an empty root object for a level, replacing any old one with the same name.</summary>
    public static Transform NewRoot(string name)
    {
        DestroyIfExists(name);
        var root = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(root, "Build Level");
        return root.transform;
    }

    /// <summary>Creates an empty child object to group related pieces, like one section of the tower.</summary>
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

    /// <summary>A strip of spikes that are always up. The position is the center of the strip, and it's 0.5 units tall.</summary>
    public static GameObject Spikes(string name, Vector2 position, float width, Transform parent)
    {
        GameObject go = Block(name, position, new Vector2(width, 0.5f), ArtGenerator.Spikes, HazardColor, GroundLayer, parent);
        var box = go.GetComponent<BoxCollider2D>();
        box.size = new Vector2(width - 0.1f, 0.4f); // A little smaller than the picture, so brushing the tips doesn't count.
        box.offset = new Vector2(0f, -0.05f);
        go.AddComponent<Hazard>();
        return go;
    }

    /// <summary>Spikes that pop up and go down on a cycle. The phase offset makes spikes next to each other take turns.</summary>
    public static GameObject TimedSpikes(string name, Vector2 position, float width, float phaseOffset, Transform parent)
    {
        GameObject go = Spikes(name, position, width, parent);
        go.GetComponent<SpriteRenderer>().color = TimedHazardColor;
        go.GetComponent<BoxCollider2D>().isTrigger = true;
        go.layer = 0; // The Default layer, so the spikes don't block the hook or count as ground.
        SetFloat(go.AddComponent<global::TimedSpikes>(), "phaseOffset", phaseOffset);
        return go;
    }

    /// <summary>A platform that moves back and forth between two points.</summary>
    public static GameObject MovingPlatform(string name, Vector2 from, Vector2 to, float width, float speed, Transform parent)
    {
        GameObject go = Block(name, from, new Vector2(width, 0.5f), ArtGenerator.Block, MovingColor, GroundLayer, parent);
        Transform a = Marker(name + " Point A", from, parent);
        Transform b = Marker(name + " Point B", to, parent);
        var mover = go.AddComponent<global::MovingPlatform>(); // This also adds the Rigidbody2D it needs.
        SetRef(mover, "pointA", a);
        SetRef(mover, "pointB", b);
        SetFloat(mover, "speed", speed);
        return go;
    }

    /// <summary>The checkered finish zone at the top (UC10). The position is the center of the zone.</summary>
    public static GameObject Finish(Vector2 position, Vector2 size, Transform parent)
    {
        GameObject go = Block("Finish Line", position, size, ArtGenerator.Checker, FinishColor, 0, parent);
        go.GetComponent<BoxCollider2D>().isTrigger = true;
        go.AddComponent<FinishLine>();
        return go;
    }

    /// <summary>Hint text that floats in the level, like the control tips in the first section (NFR1).</summary>
    public static void Hint(string text, Vector2 position, Transform parent, float size = 3f)
    {
        var go = new GameObject("Hint: " + text);
        go.transform.SetParent(parent);
        go.transform.position = position;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size * 3f; // Text in the world is about 0.1 units tall per point.
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 1f, 1f, 0.85f);
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.rectTransform.sizeDelta = new Vector2(24f, 3f);
        tmp.sortingOrder = 5;
    }

    /// <summary>Creates a tiled sprite with a box collider. Most of the other piece methods use this.</summary>
    public static GameObject Block(string name, Vector2 position, Vector2 size, Sprite sprite, Color color, int layer, Transform parent)
    {
        var go = new GameObject(name);
        go.layer = layer;
        go.transform.SetParent(parent);
        go.transform.position = position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled; // Keeps the edges the same and repeats the middle, so big blocks don't look stretched.
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;
        sr.color = color;

        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
        return go;
    }

    /// <summary>Creates an empty object at a position, used for things like moving platform end points.</summary>
    public static Transform Marker(string name, Vector2 position, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        return go.transform;
    }

    // Scene setup that every level needs

    /// <summary>
    /// Moves the player to the spawn point and sets up the GameManager, the aim
    /// marker, the camera, and the background. Returns false if the scene has
    /// no player, in which case Set Up Project needs to run first.
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
            collider.sharedMaterial = GetOrCreateNoFriction(); // Without this, pushing into a wall would make the player stick to it.
        }
        StylePlayer(player);

        // The GameManager and its timer. If one already exists, it gets reused so
        // the UI's links to it don't break.
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

        // The aim marker that shows where the hook will land.
        var aim = new GameObject("Aim Marker");
        Undo.RegisterCreatedObjectUndo(aim, "Build Level");
        var aimRenderer = aim.AddComponent<SpriteRenderer>();
        aimRenderer.sprite = ArtGenerator.Marker;
        aimRenderer.color = new Color(1f, 0.95f, 0.6f);
        aimRenderer.sortingOrder = 40;
        aimRenderer.enabled = false;
        SetRef(aim.AddComponent<AimIndicator>(), "grapple", player.GetComponent<GrapplingHook>());

        // The camera follows the player.
        Camera cam = Camera.main;
        if (cam != null)
        {
            Undo.RecordObject(cam, "Set Camera");
            cam.orthographicSize = 8f;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.18f);
            Undo.RecordObject(cam.transform, "Move Camera");
            cam.transform.position = new Vector3(spawn.x, spawn.y + 2f, -10f);
            // This doesn't use ?? on purpose. A destroyed Unity object can look like
            // null to Unity but not to ??, so ?? could hand back a broken object.
            CameraFollow follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = Undo.AddComponent<CameraFollow>(cam.gameObject);
            SetRef(follow, "target", player.transform);
        }

        BuildBackground(spawn);
        EditorSceneManager.MarkSceneDirty(player.scene);
        return true;
    }

    /// <summary>
    /// Puts the character art on a child object named Body, adds PlayerVisuals,
    /// and sets how the rope looks. The art goes on a child so squash and
    /// stretch don't change the collider. Running it again doesn't add
    /// anything twice.
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
            rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smooths out movement between physics steps so the camera doesn't jitter.
        }
    }

    // Builds the sky and the two tower layers behind the level.
    private static void BuildBackground(Vector2 spawn)
    {
        var root = new GameObject("Background");
        Undo.RegisterCreatedObjectUndo(root, "Build Level");

        // The sky moves almost all the way with the camera, so it looks very far away.
        var sky = new GameObject("Sky");
        sky.transform.SetParent(root.transform);
        sky.transform.position = new Vector3(spawn.x, spawn.y + 11.5f, 0f);
        sky.transform.localScale = new Vector3(60f, 2.2f, 1f); // The sprite is 1 by 16 units before scaling.
        var skyRenderer = sky.AddComponent<SpriteRenderer>();
        skyRenderer.sprite = ArtGenerator.Sky;
        skyRenderer.sortingOrder = -100;
        SetFloat(sky.AddComponent<Parallax>(), "followFactor", 0.95f);

        // Three layers of city buildings. Closer layers move less with the
        // camera, so they look closer and scroll by faster as the player climbs.
        // Each layer is tall enough that its tallest buildings reach the top of
        // the screen around the finish, so the city stays in view the whole climb.
        // The bottoms start below the screen so no gap shows at the street.
        float street = spawn.y - 7f;
        CityLayer(root.transform, "Far Buildings", ArtGenerator.CityFar, spawn.x, street, -90, 0.88f);
        CityLayer(root.transform, "Mid Buildings", ArtGenerator.CityMid, spawn.x, street, -85, 0.75f);
        CityLayer(root.transform, "Near Buildings", ArtGenerator.CityNear, spawn.x, street, -80, 0.55f);
    }

    // One layer of buildings, repeated sideways far past the tower walls. Its
    // bottom sits at the street height, and a higher follow value makes the
    // layer look farther away.
    private static void CityLayer(Transform parent, string name, Sprite sprite, float x, float street, int order, float follow)
    {
        float height = sprite.rect.height / sprite.pixelsPerUnit;
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, street + height / 2f, 0f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = new Vector2(160f, height);
        sr.sortingOrder = order;
        SetFloat(go.AddComponent<Parallax>(), "followFactor", follow);
    }

    // Utilities

    /// <summary>
    /// Sets a private [SerializeField] field on a component, the same as
    /// dragging something into that slot in the Inspector, so it saves with
    /// the scene.
    /// </summary>
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

    /// <summary>Sets a private number field on a component so it saves with the scene.</summary>
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

    /// <summary>Deletes the object with this name from the scene, if there is one.</summary>
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
