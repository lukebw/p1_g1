using UnityEngine;
using System.Collections.Generic;

public class ChopHitbox : MonoBehaviour
{
    List<Collider2D> activeCollisions = new List<Collider2D>();

    public List<Collider2D> GetActiveCollisions()
    {
        return activeCollisions;
    }

    void OnTriggerEnter2D(Collider2D hurtbox)
    {
        if (!activeCollisions.Contains(hurtbox) && hurtbox.gameObject.tag == "Tree")
        {
            activeCollisions.Add(hurtbox);
        }
    }

    void OnTriggerExit2D(Collider2D hurtbox)
    {
        if (activeCollisions.Contains(hurtbox))
        {
            activeCollisions.Remove(hurtbox);
        }
    }
}
