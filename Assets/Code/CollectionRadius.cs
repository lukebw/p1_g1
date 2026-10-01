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
            collectible.gameObject.SetActive(false);
            CheeseTownPhone.TownSession.Instance.Progress.CollectWorld(1);
            Destroy(collectible.gameObject);
        }
    }
    // END MODIFIED
}
