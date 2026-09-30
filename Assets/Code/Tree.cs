using UnityEngine;

public class Tree : MonoBehaviour
{
    public GameObject cheesePrefab;

    public int health = 5;

    public void TakeDamage(int damage)
    {
        // TODO: Add shake animation and/or particle effects for visual feedback
        Debug.Log("Tree took " + damage + " damage!");

        health -= damage;
        if (health <= 0)
        {
            // TODO: Add death or fade out animation
            SpawnCheese(3);
            Destroy(gameObject);
        }
    }

    void SpawnCheese(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            // Spawn the cheese at a random distance near the center of the tree
            Vector3 spawnOffset = new Vector3(Random.Range(-3f, 3f), Random.Range(-3f, 3f), 0);
            Instantiate(cheesePrefab, transform.position + spawnOffset, Quaternion.identity);
        }
    }
}
