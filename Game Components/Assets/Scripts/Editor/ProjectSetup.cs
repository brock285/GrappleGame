using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor tool behind Tools, Grappler, Set Up Project. It's the first thing to
/// run on a new copy of the project. It adds the Ground and Grappleable layers
/// if they're missing. Then, if the open scene doesn't have a player, it builds
/// one with every component it needs, the GroundCheck and Rope child objects,
/// and all of the fields filled in. Running it again doesn't add anything twice.
///
/// It also adds Tools, Grappler, Clear Best Times, which deletes the saved
/// times for testing.
/// </summary>
public static class ProjectSetup
{
    private const string RopeMaterialPath = "Assets/Art/Rope.mat";

    /// <summary>Adds the layers and builds the player. Runs from Tools, Grappler, Set Up Project.</summary>
    [MenuItem("Tools/Grappler/Set Up Project", priority = 0)]
    public static void Run()
    {
        bool addedGround = AddLayer("Ground");
        bool addedGrapple = AddLayer("Grappleable");

        int groundLayer = LayerMask.NameToLayer("Ground");
        int grappleLayer = LayerMask.NameToLayer("Grappleable");
        if (groundLayer < 0 || grappleLayer < 0)
        {
            EditorUtility.DisplayDialog("Grappler",
                "Couldn't add the layers because every user layer is taken. Free one in Edit > Project Settings > Tags and Layers.", "OK");
            return;
        }

        string playerResult;
        if (GameObject.Find("Player") != null)
        {
            playerResult = "A Player already exists, so it was left as is.";
        }
        else
        {
            BuildPlayer(groundLayer, grappleLayer);
            playerResult = "Built a Player at the origin.";
        }

        Debug.Log($"Grappler: layers {(addedGround || addedGrapple ? "added" : "already set up")}. {playerResult} " +
                  "Next, run Tools > Grappler > Build Tower Level.");
    }

    /// <summary>Deletes every saved time after asking first. Only for testing in the Editor.</summary>
    [MenuItem("Tools/Grappler/Clear Best Times", priority = 51)]
    public static void ClearBestTimes()
    {
        if (!EditorUtility.DisplayDialog("Grappler", "Delete all saved best times on this computer?", "Delete", "Cancel"))
            return;
        BestTimeManager.ClearTimes();
        Debug.Log("Grappler: best times cleared.");
    }

    /// <summary>Adds a user layer with this name. Returns true if it was added.</summary>
    private static bool AddLayer(string layerName)
    {
        if (LayerMask.NameToLayer(layerName) >= 0) return false;

        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        // Unity uses layers 0 to 5 itself, so our layers start at 6.
        for (int i = 6; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return true;
            }
        }
        return false;
    }

    // Creates the player object with a Rigidbody2D, collider, PlayerController,
    // GrapplingHook, and the GroundCheck and Rope children, all linked together.
    private static void BuildPlayer(int groundLayer, int grappleLayer)
    {
        var player = new GameObject("Player");
        Undo.RegisterCreatedObjectUndo(player, "Build Player");

        var rb = player.AddComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate; // Smooths out movement between physics steps so the camera doesn't jitter.
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var capsule = player.AddComponent<CapsuleCollider2D>();
        capsule.size = new Vector2(1f, 2f);

        // GrapplingHook needs a DistanceJoint2D, so Unity adds one along with it.
        PlayerController controller = player.AddComponent<PlayerController>();
        GrapplingHook grapple = player.AddComponent<GrapplingHook>();

        var groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform, false);
        groundCheck.transform.localPosition = new Vector3(0f, -1f, 0f);

        var ropeObject = new GameObject("Rope");
        ropeObject.transform.SetParent(player.transform, false);
        var rope = ropeObject.AddComponent<LineRenderer>();
        rope.positionCount = 2;
        rope.useWorldSpace = true;
        rope.widthMultiplier = 0.05f;
        rope.startColor = rope.endColor = new Color(0.9f, 0.85f, 0.7f);
        rope.sharedMaterial = GetOrCreateRopeMaterial();
        rope.sortingOrder = 9;
        rope.enabled = false;

        SetRef(controller, "groundCheck", groundCheck.transform);
        SetLayerMask(controller, "groundLayer", 1 << groundLayer);
        SetRef(grapple, "rope", rope);
        SetLayerMask(grapple, "grappleableLayers", 1 << grappleLayer);
        SetLayerMask(grapple, "blockingLayers", 1 << groundLayer);

        // Character art, squash and stretch, and rope style.
        LevelKit.StylePlayer(player); // Adds the character art on a child named Body.

        EditorSceneManager.MarkSceneDirty(player.scene);
        Selection.activeGameObject = player;
    }

    // Loads the rope's material from Assets/Art, or makes it if it's missing.
    private static Material GetOrCreateRopeMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(RopeMaterialPath);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        Directory.CreateDirectory(Path.GetDirectoryName(RopeMaterialPath));
        var mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, RopeMaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    // Sets a private [SerializeField] field so it saves with the scene.
    private static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    // Sets a private LayerMask field so it saves with the scene.
    private static void SetLayerMask(Object target, string field, int mask)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).intValue = mask;
        so.ApplyModifiedProperties();
    }
}
