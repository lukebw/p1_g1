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
            Debug.Log("You got 5 cheese!");
            Destroy(gameObject);
        }
    }
}
