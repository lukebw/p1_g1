using CheeseTownPhone;
using UnityEngine;

public class TreeHurtbox : MonoBehaviour
{
    public Tree tree;

    TownProgress progress;
    Transform treeRoot;
    Vector3 originalScale;
    Collider2D hurtbox;

    void Start()
    {
        tree = GetComponentInParent<Tree>();
    }

    void Awake()
    {
        treeRoot = transform.parent != null && transform.parent.CompareTag("Tree") ? transform.parent : transform;
        originalScale = treeRoot.localScale;
        // BEGIN CHANGED: The actual tree prefab uses a 2D collider, including its growth scale.
        hurtbox = GetComponent<Collider2D>();
        tree = GetComponentInParent<Tree>();
        // END CHANGED
    }

    void OnEnable()
    {
        progress = TownSession.Instance.Progress;
        progress.Changed += ApplyUpgrades;
        ApplyUpgrades();
    }

    void OnDisable()
    {
        if (progress != null) progress.Changed -= ApplyUpgrades;
    }

    void ApplyUpgrades()
    {
        Vector3 scale = originalScale * progress.WildTreeScale;
        // BEGIN CHANGED: Currency refreshes must not dirty every tree's collider transform.
        if (treeRoot.localScale != scale) treeRoot.localScale = scale;
        // END CHANGED
    }

    public float DistanceFrom(Vector3 point)
    {
        // BEGIN ADDED: Stumps must not block searches for nearby collectible trees.
        if (tree != null && tree.IsChopped) return float.PositiveInfinity;
        // END ADDED
        Vector3 nearest = hurtbox != null ? (Vector3)hurtbox.ClosestPoint(point) : transform.position;
        return Vector2.Distance(point, nearest);
    }

    public int Collect()
    {
        // BEGIN ADDED: Batch-mode town stock is collected on its page; wild trees only supply chopped drops.
        if (progress.UsesBatchProduction) return 0;
        // END ADDED
        // BEGIN ADDED: Chopped trees cannot pay the separate manual collection action.
        if (tree != null && tree.IsChopped) return 0;
        // END ADDED
        return progress.Collect();
    }

    public void RegisterHit(int damage)
    {
        // BEGIN MODIFIED: Ignore stale hitbox references after conversion to a stump.
        if (tree == null) tree = GetComponentInParent<Tree>();
        if (enabled && tree != null) tree.TakeDamage(damage);
        // END MODIFIED
    }
}
