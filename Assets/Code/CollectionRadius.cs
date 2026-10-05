using UnityEngine;

public class CollectionRadius : MonoBehaviour
{
    // TODO: create method to change size of collection radius based on upgrades

    // BEGIN MODIFIED: Share the gated pickup logic with the stay callback.
    void OnTriggerEnter2D(Collider2D collectible) { TryCollect(collectible); }
    // END MODIFIED

    // BEGIN ADDED: Catch cheese that lands inside an existing pickup overlap.
    void OnTriggerStay2D(Collider2D collectible) { TryCollect(collectible); }
    // END ADDED

    // BEGIN MODIFIED: Falling cheese must finish its animation before pickup.
    void TryCollect(Collider2D collectible)
    {
        // Pick up the detected object if it is a collectible
        if (collectible.gameObject.tag == "Collectible")
        {
            if (!collectible.gameObject.activeSelf) return;
            CheeseDropMotion drop = collectible.GetComponent<CheeseDropMotion>();
            if (drop != null && !drop.IsSettled) return;
            // BEGIN ADDED: Reserve visual credit before Changed refreshes the HUD; real credit stays immediate.
            var progress = CheeseTownPhone.TownSession.Instance.Progress;
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
