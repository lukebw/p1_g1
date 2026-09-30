using UnityEngine;

public class CollectionRadius : MonoBehaviour
{
    // TODO: create method to change size of collection radius based on upgrades

    void OnTriggerEnter2D(Collider2D collectible)
    {
        // Pick up the detected object if it is a collectible
        if (collectible.gameObject.tag == "Collectible")
        {
            Debug.Log("You collected 1 " + collectible.gameObject.name + "!"); // TODO: add to actual inventory
            Destroy(collectible.gameObject);
        }
    }
}
