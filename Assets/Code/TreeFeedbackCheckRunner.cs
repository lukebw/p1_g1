// BEGIN ADDED: Integration checks catch broken prefab wiring and pickup timing.
#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class TreeFeedbackCheckRunner : MonoBehaviour
{
    // BEGIN ADDED: Keep failures visible and test in an empty scene, away from gameplay.
    public Action<Exception> Completed;
    string runtimeError;
    InputSettings originalInputSettings;
    InputSettings testInputSettings;
    void Start()
    {
        // BEGIN ADDED: Hidden batch Game Views must accept synthetic mouse input.
        originalInputSettings = InputSystem.settings;
        testInputSettings = Instantiate(originalInputSettings);
        testInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        testInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings = testInputSettings;
        // END ADDED
        Application.logMessageReceived += CaptureError;
        Time.captureFramerate = 60;
        StartCoroutine(Guard());
    }
    void CaptureError(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception) runtimeError = message + "\n" + stack;
    }
    void OnDestroy()
    {
        Application.logMessageReceived -= CaptureError;
        if (originalInputSettings != null) InputSystem.settings = originalInputSettings;
        if (testInputSettings != null) Destroy(testInputSettings);
    }
    IEnumerator Guard()
    {
        IEnumerator steps = Steps();
        while (true)
        {
            object current;
            try
            {
                if (runtimeError != null) throw new Exception(runtimeError);
                if (!steps.MoveNext()) break;
                current = steps.Current;
            }
            catch (Exception error) { Completed?.Invoke(error); yield break; }
            yield return current;
        }
        Completed?.Invoke(null);
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void SetInt(Tree tree, string name, int value)
    {
        SerializedObject settings = new SerializedObject(tree);
        settings.FindProperty(name).intValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Click(Mouse mouse, bool pressed)
    {
        InputSystem.QueueStateEvent(mouse, new MouseState { buttons = (ushort)(pressed ? 1 : 0) });
        InputSystem.Update();
    }

    // BEGIN ADDED: Render real Unity frames so art alignment can be inspected.
    static void Capture(Camera camera, string name)
    {
        RenderTexture target = new RenderTexture(768, 768, 24);
        Texture2D pixels = new Texture2D(768, 768, TextureFormat.RGBA32, false);
        RenderTexture previous = RenderTexture.active;
        try
        {
            target.Create();
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 768, 768), 0, 0);
            pixels.Apply();
            Directory.CreateDirectory("Logs");
            File.WriteAllBytes("Logs/TreeFeedback-" + name + ".png", pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            target.Release();
            Destroy(target);
            Destroy(pixels);
        }
    }
    // END ADDED
    // END ADDED

    // BEGIN ADDED: Exercise the actual prefab, input callbacks, and 2D trigger pickup.
    IEnumerator Steps()
    {
        GameObject cameraObject = new GameObject("Check camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.orthographicSize = 7;
        camera.aspect = 1;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.6f, 0.8f, 0.25f);
        camera.transform.position = new Vector3(0, 2, -10);
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        new GameObject("Check daylight").AddComponent<Light2D>().lightType = Light2D.LightType.Global;
        SpriteRenderer background = new GameObject("Check grass").AddComponent<SpriteRenderer>();
        background.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/MAP_03.png").OfType<Sprite>().First();
        background.transform.localScale = Vector3.one * 4;
        background.sortingOrder = -100;
        GameObject treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Tree.prefab");
        GameObject cheesePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Cheese.prefab");
        Check(treePrefab != null && cheesePrefab != null, "Prefab import failed.");
        Check(cheesePrefab.GetComponent<CheeseDropMotion>() != null, "Cheese motion is not wired.");
        Tree tree = Instantiate(treePrefab, Vector3.zero, Quaternion.identity).GetComponent<Tree>();
        yield return null;
        TreeHurtbox hurtbox = tree.GetComponentInChildren<TreeHurtbox>();
        BoxCollider2D collision = hurtbox.GetComponent<BoxCollider2D>();
        SpriteRenderer visual = tree.transform.Find("TreeSprite").GetComponent<SpriteRenderer>();
        Vector3 rootPosition = tree.transform.position;
        Vector3 collisionPosition = collision.transform.position;
        Vector3 rest = visual.transform.localPosition;
        float originalCollisionHeight = collision.bounds.size.y;
        Vector3 ground = visual.transform.TransformPoint(new Vector3(0, visual.sprite.bounds.min.y, 0));
        int originalLayer = tree.GetComponent<SortingGroup>().sortingLayerID;
        int originalOrder = tree.GetComponent<SortingGroup>().sortingOrder;
        SetInt(tree, "minCheeseCount", 3);
        SetInt(tree, "maxCheeseCount", 3);

        GameObject cursorObject = new GameObject("Check cursor");
        ChopHitbox hitbox = cursorObject.AddComponent<ChopHitbox>();
        CursorController cursor = cursorObject.AddComponent<CursorController>();
        cursor.chopHitbox = hitbox;
        Check(cursor.cursorActionMap != null && cursor.cursorActionMap.enabled, "Cursor input map is missing or disabled.");
        Mouse mouse = InputSystem.AddDevice<Mouse>();
        yield return new WaitForSeconds(0.5f);
        int before = tree.health;
        Click(mouse, true);
        Check(tree.health == before - 1, "Click damage was " + (before - tree.health) + "; expected one hit.");
        Click(mouse, false);
        bool sawShake = false;
        for (int frame = 0; frame < 5; frame++)
        {
            yield return null;
            sawShake |= Vector3.Distance(visual.transform.localPosition, rest) > 0.001f;
        }
        Check(sawShake, "The visual did not shake.");
        Check(tree.transform.position == rootPosition && collision.transform.position == collisionPosition,
            "Shake moved the sorting or physics root.");
        Check(tree.GetComponentInChildren<ParticleSystem>().particleCount > 0, "No leaves were emitted.");
        Capture(camera, "Hit");
        yield return new WaitForSeconds(0.25f);
        Check(Vector3.Distance(visual.transform.localPosition, rest) < 0.0001f, "Shake did not reset.");
        cursor.enabled = false;
        cursor.enabled = true;
        before = tree.health;
        Click(mouse, true);
        Check(tree.health == before - 1, "Re-enabling duplicated click callbacks.");
        Click(mouse, false);
        InputSystem.RemoveDevice(mouse);
        Destroy(cursorObject);
        yield return new WaitForSeconds(0.25f);

        tree.TakeDamage(100);
        yield return null;
        CheeseDropMotion[] drops = FindObjectsByType<CheeseDropMotion>();
        Check(tree.IsChopped && drops.Length == 3, "Death state " + tree.IsChopped + ", drop count " + drops.Length + "; expected 3.");
        Check(drops.All(d => !d.IsSettled && !d.GetComponent<Rigidbody2D>().simulated), "Airborne cheese can collide.");
        float startY = drops[0].GetComponentInChildren<SpriteRenderer>().bounds.center.y;
        tree.TakeDamage(100);
        Check(FindObjectsByType<CheeseDropMotion>().Length == 3, "A dead tree paid twice.");
        yield return new WaitForSeconds(0.25f);
        Check(visual.sprite.name == "Cheese_tree_chopped_0", "Stump sprite did not load.");
        Check(visual.sprite.rect.width == 122 && visual.sprite.rect.height == 58, "Stump crop includes the empty canopy.");
        Check(collision.enabled && collision.bounds.size.y < originalCollisionHeight / 4f, "Stump collision was not resized.");
        Check(!hurtbox.enabled && float.IsPositiveInfinity(hurtbox.DistanceFrom(rootPosition)) && hurtbox.Collect() == 0,
            "The stump still accepts interaction.");
        Check(tree.transform.position == rootPosition && tree.GetComponent<SortingGroup>().sortingLayerID == originalLayer
            && tree.GetComponent<SortingGroup>().sortingOrder == originalOrder, "Stump changed occlusion settings.");
        Check(drops[0].GetComponentInChildren<SpriteRenderer>().bounds.center.y < startY, "Cheese did not descend.");
        Capture(camera, "Drop");
        yield return new WaitForSeconds(1.2f);
        Check(drops.All(d => d.IsSettled && d.GetComponent<Rigidbody2D>().simulated), "Cheese never became collectible.");
        Check(drops.All(d => Vector2.Distance(d.transform.position, ground) <= 3.4f),
            "Cheese landed outside the spread area: " + string.Join(", ", drops.Select(d => d.transform.position.ToString())) + "; ground " + ground);
        Capture(camera, "Landed");
        foreach (CheeseDropMotion item in drops) Destroy(item.gameObject);
        Destroy(tree.gameObject);
        yield return null;

        bool sawMinimum = false, sawMaximum = false;
        UnityEngine.Random.InitState(5110);
        for (int i = 0; i < 24; i++)
        {
            Tree sample = Instantiate(treePrefab, Vector3.zero, Quaternion.identity).GetComponent<Tree>();
            yield return null;
            sample.TakeDamage(100);
            yield return null;
            CheeseDropMotion[] spawned = FindObjectsByType<CheeseDropMotion>();
            Check(spawned.Length >= 2 && spawned.Length <= 4, "Random count left the inclusive range.");
            sawMinimum |= spawned.Length == 2;
            sawMaximum |= spawned.Length == 4;
            foreach (CheeseDropMotion item in spawned) Destroy(item.gameObject);
            Destroy(sample.gameObject);
            yield return null;
        }
        Check(sawMinimum && sawMaximum, "Random count never reached both endpoints.");

        GameObject pickup = new GameObject("Check pickup");
        pickup.layer = 0;
        pickup.transform.position = new Vector3(30, 0, 0);
        pickup.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        CircleCollider2D pickupArea = pickup.AddComponent<CircleCollider2D>();
        pickupArea.isTrigger = true;
        pickupArea.includeLayers = 1 << 8;
        pickup.AddComponent<CollectionRadius>();
        GameObject fruit = Instantiate(cheesePrefab, pickup.transform.position, Quaternion.identity);
        CheeseDropMotion falling = fruit.GetComponent<CheeseDropMotion>();
        int wallet = CheeseTownPhone.TownSession.Instance.Progress.Cheeses;
        falling.BeginDrop(pickup.transform.position, pickup.transform.position + Vector3.up * 4f, pickup.transform.position);
        yield return new WaitForSeconds(0.2f);
        Check(fruit != null && CheeseTownPhone.TownSession.Instance.Progress.Cheeses == wallet, "Cheese was picked up in flight.");
        yield return new WaitForSeconds(1.3f);
        Check(fruit == null && CheeseTownPhone.TownSession.Instance.Progress.Cheeses == wallet + CheeseTownPhone.TownSession.Instance.Progress.UnitPrice,
            "Overlap pickup failed: fruit remains " + (fruit != null) + ", wallet delta " +
            (CheeseTownPhone.TownSession.Instance.Progress.Cheeses - wallet) +
            ", unit price " + CheeseTownPhone.TownSession.Instance.Progress.UnitPrice +
            (fruit != null ? ", settled " + falling.IsSettled + ", position " + fruit.transform.position +
            ", tag " + fruit.tag + ", collider bounds " + fruit.GetComponent<Collider2D>().bounds +
            ", body type " + fruit.GetComponent<Rigidbody2D>().bodyType +
            ", touching " + fruit.GetComponent<Collider2D>().IsTouching(pickupArea) : ""));
        Destroy(pickup);
    }
    // END ADDED
}
#endif
// END ADDED
