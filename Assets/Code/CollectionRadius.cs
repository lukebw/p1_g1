using UnityEngine;

public class CollectionRadius : MonoBehaviour
{
    // TODO: create method to change size of collection radius based on upgrades

    void OnTriggerEnter2D(Collider2D collectible)
    {
        // Pick up the detected object if it is a collectible
        if (collectible.gameObject.tag == "Collectible")
        {
            if (!collectible.gameObject.activeSelf) return;
            collectible.gameObject.SetActive(false);
            CheeseTownPhone.TownSession.Instance.Progress.CollectWorld(1);
            Destroy(collectible.gameObject);
        }
    }
}
