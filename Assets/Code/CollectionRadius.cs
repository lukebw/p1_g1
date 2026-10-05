using UnityEngine;

public class CollectionRadius : MonoBehaviour
{
    // BEGIN ADDED: Keep the authored touch radius, then apply purchased range in world units.
    CircleCollider2D pickupCircle;
    float originalRadius;
    CheeseTownPhone.TownProgress progress;
    void Awake()
    {
        pickupCircle = GetComponent<CircleCollider2D>();
        if (pickupCircle != null) originalRadius = pickupCircle.radius;
    }
    void OnEnable()
    {
        progress = CheeseTownPhone.TownSession.Instance.Progress;
        progress.Changed += ApplyUpgrades;
        ApplyUpgrades();
    }
    void OnDisable() { if (progress != null) progress.Changed -= ApplyUpgrades; }
    void ApplyUpgrades()
    {
        if (pickupCircle == null) return;
        float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), .0001f);
        float radius = Mathf.Max(originalRadius, progress.CollectRange / scale);
        // Wallet updates share this event; avoid rebuilding the collider for every collected piece.
        if (!Mathf.Approximately(pickupCircle.radius, radius)) pickupCircle.radius = radius;
    }
    // END ADDED

    // BEGIN MODIFIED: Share the gated pickup logic with the stay callback.
    void OnTriggerEnter2D(Collider2D collectible) { TryCollect(collectible); }
    // END MODIFIED

    // BEGIN ADDED: Catch cheese that lands inside an existing pickup overlap.
    void OnTriggerStay2D(Collider2D collectible) { TryCollect(collectible); }
    // END ADDED

    // BEGIN MODIFIED: Falling cheese must finish its animation before pickup.
    void TryCollect(Collider2D collectible)
    {
        if (!isActiveAndEnabled || progress == null || progress.GameEnded) return;
        // Pick up the detected object if it is a collectible
        if (collectible.gameObject.tag == "Collectible")
        {
            if (!collectible.gameObject.activeSelf) return;
            CheeseDropMotion drop = collectible.GetComponent<CheeseDropMotion>();
            if (drop != null && !drop.IsSettled) return;
            // BEGIN ADDED: Reserve visual credit before Changed refreshes the HUD; real credit stays immediate.
            var visual = collectible.GetComponentInChildren<SpriteRenderer>();
            var feedback = FindAnyObjectByType<CheeseTownPhone.CheesePickupFeedback>();
            int credit = progress.GameEnded ? 0 : Mathf.Min(progress.UnitPrice, 1000000000 - progress.Cheeses);
            feedback?.BeginPickup(visual != null ? visual.bounds.center : collectible.transform.position,
                visual != null ? visual.sprite : null, credit);
            collectible.gameObject.SetActive(false);
            progress.CollectWorld(1);
            // END ADDED
            Destroy(collectible.gameObject);
        }
    }
    // END MODIFIED
}
