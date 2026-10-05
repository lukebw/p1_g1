using UnityEngine;
// BEGIN ADDED: Coroutines time feedback without moving the physics root.
using System.Collections;
// END ADDED

public class Tree : MonoBehaviour
{
    public GameObject cheesePrefab;

    public int health = 5;

    // BEGIN ADDED: Expose art and tuning so effects can be adjusted in Inspector.
    [Header("Tree visuals")]
    [SerializeField] SpriteRenderer treeVisual;
    [SerializeField] Sprite stumpSprite = null;

    [Header("Hit feedback")]
    [SerializeField, Min(0.01f)] float shakeDuration = 0.2f;
    [SerializeField, Range(0f, 15f)] float shakeAngle = 4f;
    [SerializeField, Min(0)] int minLeafCount = 3;
    [SerializeField, Min(0)] int maxLeafCount = 6;

    [Header("Cheese drops")]
    [SerializeField, Min(0)] int minCheeseCount = 2;
    [SerializeField, Min(0)] int maxCheeseCount = 4;
    [SerializeField, Range(0f, 1f)] float canopyHeight = 0.76f;
    [SerializeField, Range(0f, 0.5f)] float spawnWidth = 0.1f;
    [SerializeField, Min(0f)] float minSpreadRadius = 0.8f;
    [SerializeField, Min(0f)] float maxSpreadRadius = 2.6f;

    public bool IsChopped { get; private set; }
    Vector3 restPosition;
    Quaternion restRotation;
    Coroutine shake;
    ParticleSystem leaves;
    Material leafMaterial;
    // END ADDED

    // BEGIN ADDED: Cache the sprite pose; root sorting and collisions stay fixed.
    void Awake()
    {
        if (treeVisual == null) treeVisual = GetComponentInChildren<SpriteRenderer>();
        if (treeVisual == null) return;
        restPosition = treeVisual.transform.localPosition;
        restRotation = treeVisual.transform.localRotation;
    }

    void OnValidate()
    {
        minCheeseCount = Mathf.Clamp(minCheeseCount, 0, int.MaxValue - 1);
        maxCheeseCount = Mathf.Clamp(maxCheeseCount, minCheeseCount, int.MaxValue - 1);
        minLeafCount = Mathf.Clamp(minLeafCount, 0, 128);
        maxLeafCount = Mathf.Clamp(maxLeafCount, minLeafCount, 128);
        maxSpreadRadius = Mathf.Max(minSpreadRadius, maxSpreadRadius);
        shakeDuration = Mathf.Max(0.01f, shakeDuration);
    }
    // END ADDED

    // BEGIN MODIFIED: Each hit gives feedback; the chopped guard prevents extra payouts.
    public void TakeDamage(int damage)
    {
        if (IsChopped || damage <= 0) return;

        EmitLeaves();
        ResetShake();
        if (treeVisual != null) shake = StartCoroutine(ShakeVisual());
        health -= damage;
        if (health <= 0)
        {
            IsChopped = true;
            int minimum = Mathf.Clamp(minCheeseCount, 0, int.MaxValue - 1);
            int maximum = Mathf.Clamp(maxCheeseCount, minimum, int.MaxValue - 1);
            SpawnCheese(Random.Range(minimum, maximum + 1));
            StartCoroutine(LeaveStump());
        }
    }
    // END MODIFIED

    // BEGIN MODIFIED: Start drops in the canopy and choose landing points on the ground.
    void SpawnCheese(int amount)
    {
        if (cheesePrefab == null || treeVisual == null) return;
        Bounds bounds = treeVisual.bounds;
        Vector3 ground = treeVisual.transform.TransformPoint(
            new Vector3(treeVisual.sprite.bounds.center.x, treeVisual.sprite.bounds.min.y, 0));
        float minimum = Mathf.Max(0f, minSpreadRadius);
        float maximum = Mathf.Max(minimum, maxSpreadRadius);
        for (int i = 0; i < amount; i++)
        {
            Vector3 origin = new Vector3(
                bounds.center.x + Random.Range(-spawnWidth, spawnWidth) * bounds.size.x,
                Mathf.Lerp(bounds.min.y, bounds.max.y, canopyHeight), ground.z);
            float angle = (i + Random.Range(0.1f, 0.9f)) * Mathf.PI * 2f / amount;
            float radius = Mathf.Sqrt(Random.Range(minimum * minimum, maximum * maximum));
            Vector3 landing = ground + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            GameObject cheese = Instantiate(cheesePrefab, ground, Quaternion.identity);
            CheeseDropMotion motion = cheese.GetComponent<CheeseDropMotion>();
            if (motion == null) motion = cheese.AddComponent<CheeseDropMotion>();
            motion.BeginDrop(ground, origin, landing);
        }
    }
    // END MODIFIED

    // BEGIN ADDED: Rock around the sprite base, then restore the exact original pose.
    IEnumerator ShakeVisual()
    {
        Transform visual = treeVisual.transform;
        Vector3 pivot = restPosition + restRotation * Vector3.Scale(
            new Vector3(0, treeVisual.sprite.bounds.min.y, 0), visual.localScale);
        float duration = Mathf.Max(0.01f, shakeDuration);
        float elapsed = 0;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float angle = Mathf.Sin(t * Mathf.PI * 6f) * shakeAngle * (1f - t);
            Quaternion offset = Quaternion.Euler(0, 0, angle);
            visual.localRotation = offset * restRotation;
            visual.localPosition = pivot + offset * (restPosition - pivot);
            elapsed += Time.deltaTime;
            yield return null;
        }
        visual.localPosition = restPosition;
        visual.localRotation = restRotation;
        shake = null;
    }

    void ResetShake()
    {
        if (shake != null) StopCoroutine(shake);
        shake = null;
        if (treeVisual == null) return;
        treeVisual.transform.localPosition = restPosition;
        treeVisual.transform.localRotation = restRotation;
    }
    // END ADDED

    // BEGIN ADDED: Swap art in place and shrink collision to the stump's footprint.
    IEnumerator LeaveStump()
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, shakeDuration));
        ResetShake();
        if (treeVisual == null || stumpSprite == null)
        {
            Debug.LogWarning("Assign the tree visual and stump sprite on the Tree prefab.", this);
            yield break;
        }

        Sprite standingSprite = treeVisual.sprite;
        Vector2 standingAnchor = standingSprite.rect.position + standingSprite.pivot;
        Vector2 stumpAnchor = stumpSprite.rect.position + stumpSprite.pivot;
        Vector2 offset = (stumpAnchor - standingAnchor) / standingSprite.pixelsPerUnit;
        treeVisual.transform.localPosition = restPosition + restRotation * Vector3.Scale(
            new Vector3(offset.x, offset.y, 0), treeVisual.transform.localScale);
        treeVisual.sprite = stumpSprite;
        restPosition = treeVisual.transform.localPosition;

        TreeHurtbox hurtbox = GetComponentInChildren<TreeHurtbox>();
        if (hurtbox == null) yield break;
        hurtbox.enabled = false;
        hurtbox.gameObject.tag = "Untagged";
        BoxCollider2D stumpCollider = hurtbox.GetComponent<BoxCollider2D>();
        if (stumpCollider == null) yield break;
        Transform collision = stumpCollider.transform;
        collision.localPosition = treeVisual.transform.localPosition;
        collision.localRotation = restRotation;
        collision.localScale = treeVisual.transform.localScale;
        Bounds stumpBounds = stumpSprite.bounds;
        stumpCollider.size = new Vector2(stumpBounds.size.x * 0.6f, stumpBounds.size.y * 0.25f);
        stumpCollider.offset = new Vector2(stumpBounds.center.x,
            stumpBounds.min.y + stumpCollider.size.y * 0.5f);
    }
    // END ADDED

    // BEGIN ADDED: A small world-space burst supplies falling leaves without new textures.
    void EmitLeaves()
    {
        if (treeVisual == null || maxLeafCount <= 0) return;
        if (leaves == null)
        {
            GameObject emitter = new GameObject("Hit leaves");
            emitter.transform.SetParent(transform, false);
            leaves = emitter.AddComponent<ParticleSystem>();
            leaves.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = leaves.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.65f, 1.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.13f);
            main.startSpeed = 0f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.18f;
            main.maxParticles = 64;
            var emission = leaves.emission;
            emission.enabled = false;
            var shape = leaves.shape;
            shape.enabled = false;
            var color = leaves.colorOverLifetime;
            color.enabled = true;
            Gradient fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.65f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;
            var spin = leaves.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-3f, 3f);
            ParticleSystemRenderer renderer = leaves.GetComponent<ParticleSystemRenderer>();
            leafMaterial = new Material(treeVisual.sharedMaterial);
            leafMaterial.mainTexture = Texture2D.whiteTexture;
            renderer.sharedMaterial = leafMaterial;
            renderer.sortingLayerID = treeVisual.sortingLayerID;
            renderer.sortingOrder = treeVisual.sortingOrder + 1;
        }

        Bounds bounds = treeVisual.bounds;
        leaves.Play();
        int minimum = Mathf.Clamp(minLeafCount, 0, 128);
        int maximum = Mathf.Clamp(maxLeafCount, minimum, 128);
        int count = Random.Range(minimum, maximum + 1);
        for (int i = 0; i < count; i++)
        {
            var particle = new ParticleSystem.EmitParams
            {
                position = new Vector3(bounds.center.x + Random.Range(-0.3f, 0.3f) * bounds.size.x,
                    Mathf.Lerp(bounds.min.y, bounds.max.y, Random.Range(0.62f, 0.88f)), transform.position.z),
                velocity = new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-0.6f, -0.2f), 0),
                startColor = Color.Lerp(new Color(0.25f, 0.48f, 0.12f), new Color(0.55f, 0.72f, 0.22f), Random.value)
            };
            leaves.Emit(particle, 1);
        }
    }

    void OnDisable() { ResetShake(); }
    void OnDestroy() { if (leafMaterial != null) Destroy(leafMaterial); }
    // END ADDED
}
