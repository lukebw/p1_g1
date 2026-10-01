// BEGIN ADDED: Separate ground travel from visual height for top-down fruit drops.
using System.Collections;
using UnityEngine;

public sealed class CheeseDropMotion : MonoBehaviour
{
    // BEGIN ADDED: Expose timing; placed cheese stays collectible until a drop begins.
    [SerializeField] Transform visual;
    [SerializeField, Min(0.05f)] float fallDuration = 0.75f;
    [SerializeField, Min(0.01f)] float bounceDuration = 0.22f;
    [SerializeField, Min(0f)] float bounceHeight = 0.22f;
    public bool IsSettled { get; private set; } = true;
    Vector3 restPosition;
    Quaternion restRotation;
    Rigidbody2D body;
    bool wasSimulated;
    Collider2D[] colliders;
    bool[] colliderStates;
    Coroutine drop;
    // END ADDED

    // BEGIN ADDED: Cache once, including drops requested before Awake has run.
    void Awake() { CacheReferences(); }

    void CacheReferences()
    {
        if (colliders != null) return;
        if (visual == null)
        {
            SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>();
            if (renderer != null && renderer.transform != transform) visual = renderer.transform;
        }
        if (visual != null)
        {
            restPosition = visual.localPosition;
            restRotation = visual.localRotation;
        }
        body = GetComponent<Rigidbody2D>();
        if (body != null) wasSimulated = body.simulated;
        colliders = GetComponents<Collider2D>();
        colliderStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++) colliderStates[i] = colliders[i].enabled;
    }
    // END ADDED

    // BEGIN ADDED: Activate templates and suspend physics to prevent early pickup.
    public void BeginDrop(Vector3 groundStart, Vector3 canopyOrigin, Vector3 landing)
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        CacheReferences();
        if (drop != null) StopCoroutine(drop);
        IsSettled = false;
        if (body != null) body.simulated = false;
        foreach (Collider2D collider in colliders) collider.enabled = false;
        if (visual != null)
        {
            visual.localPosition = restPosition;
            visual.localRotation = restRotation;
        }
        drop = StartCoroutine(Fall(groundStart, canopyOrigin, landing));
    }
    // END ADDED

    // BEGIN ADDED: Quadratic descent accelerates falling; a smaller arc supplies one bounce.
    IEnumerator Fall(Vector3 groundStart, Vector3 canopyOrigin, Vector3 landing)
    {
        SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>();
        transform.position = groundStart;
        float centerHeight = renderer != null ? renderer.bounds.center.y - groundStart.y : 0f;
        float height = Mathf.Max(0f, canopyOrigin.y - groundStart.y - centerHeight);
        Vector3 start = new Vector3(canopyOrigin.x, groundStart.y, groundStart.z);
        float duration = Mathf.Max(0.05f, fallDuration) * Random.Range(0.9f, 1.1f);
        float spin = Random.Range(-18f, 18f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            SetPose(Vector3.Lerp(start, landing, t), height * (1f - t * t), Mathf.Sin(t * Mathf.PI) * spin);
            elapsed += Time.deltaTime;
            yield return null;
        }
        float bounceTime = Mathf.Max(0.01f, bounceDuration);
        elapsed = 0f;
        while (elapsed < bounceTime)
        {
            float t = elapsed / bounceTime;
            SetPose(landing, 4f * Mathf.Max(0f, bounceHeight) * t * (1f - t), 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetPose(landing, 0f, 0f);
        IsSettled = true;
        RestorePhysics();
        drop = null;
    }

    void SetPose(Vector3 ground, float height, float angle)
    {
        transform.position = ground;
        if (visual == null)
        {
            transform.position += Vector3.up * height;
            return;
        }
        visual.localPosition = restPosition + transform.InverseTransformVector(Vector3.up * height);
        visual.localRotation = restRotation * Quaternion.Euler(0, 0, angle);
    }
    // END ADDED

    // BEGIN ADDED: Restore physics and pose if pooling or scene changes interrupt a drop.
    void RestorePhysics()
    {
        if (colliders == null) return;
        if (body != null)
        {
            // ADDED: Sync the landing pose so physics cannot restore a stale position.
            body.position = transform.position;
            body.rotation = transform.eulerAngles.z;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = wasSimulated;
        }
        for (int i = 0; i < colliders.Length; i++)
            if (colliders[i] != null) colliders[i].enabled = colliderStates[i];
    }

    void OnDisable()
    {
        if (drop != null) StopCoroutine(drop);
        drop = null;
        IsSettled = true;
        if (visual != null)
        {
            visual.localPosition = restPosition;
            visual.localRotation = restRotation;
        }
        RestorePhysics();
    }
    // END ADDED
}
// END ADDED
