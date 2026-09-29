using UnityEngine;

public class Tree : MonoBehaviour
{
    public int health = 5;

    void Start()
    {

    }

    void Update()
    {

    }

    public void TakeDamage(int damage)
    {
        // TODO: Add shake animation and/or particle effects for visual feedback
        Debug.Log("Tree took " + damage + " damage!");

        health -= damage;
        if (health <= 0)
        {
            // TODO: Spawn cheese to be collected upon death
            // TODO: Add death or fade out animation
            Debug.Log("You got some cheese!");
            Destroy(gameObject);
        }
    }
}
