using CheeseTownPhone;
using UnityEngine;

public class TreeHurtbox : MonoBehaviour
{
    public Tree tree;

    TownProgress progress;
    Transform treeRoot;
    Vector3 originalScale;
    Collider hurtbox;

    void Start()
    {
        tree = GetComponentInParent<Tree>();
    }

    void Awake()
    {
        treeRoot = transform.parent != null && transform.parent.CompareTag("Tree") ? transform.parent : transform;
        originalScale = treeRoot.localScale;
        hurtbox = GetComponent<Collider>();
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
        treeRoot.localScale = originalScale * progress.TreeScale;
    }

    public float DistanceFrom(Vector3 point)
    {
        Vector3 nearest = hurtbox != null ? hurtbox.ClosestPoint(point) : transform.position;
        return Vector2.Distance(point, nearest);
    }

    public int Collect()
    {
        return progress.Collect();
    }

    public void RegisterHit(int damage)
    {
        tree.TakeDamage(damage);
    }
}
