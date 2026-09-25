using UnityEngine;
using System.Collections.Generic;

public class ChopHitbox : MonoBehaviour
{
    public Transform playerTransform;
    public SpriteRenderer playerSprite;

    public List<Collider> activeCollisions = new List<Collider>();

    public float offsetX = 0.75f;

    void Update()
    {
        // Extend the hitbox slightly in front of the player depending on the direction they are facing
        transform.position = playerTransform.position + new Vector3(playerSprite.flipX ? -offsetX : offsetX, 0, 0);
    }

    public List<Collider> GetActiveCollisions()
    {
        return activeCollisions;
    }

    void OnTriggerEnter(Collider hurtbox)
    {
        if (!activeCollisions.Contains(hurtbox) && hurtbox.gameObject.tag == "Tree")
        {
            activeCollisions.Add(hurtbox);
            Debug.Log("Number of active collisions: " + activeCollisions.Count);
        }
    }

    void OnTriggerExit(Collider hurtbox)
    {
        if (activeCollisions.Contains(hurtbox) && hurtbox.gameObject.tag == "Tree")
        {
            activeCollisions.Remove(hurtbox);
            Debug.Log("Number of active collisions: " + activeCollisions.Count);
        }
    }
}
