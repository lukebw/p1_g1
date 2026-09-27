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
        Debug.Log("Tree took " + damage + " damage!");

        health -= damage;
        if (health <= 0)
        {
            // TODO: Spawn cheese to be collected upon death
            Debug.Log("You got some cheese!");
            Destroy(gameObject);
        }
    }
}
