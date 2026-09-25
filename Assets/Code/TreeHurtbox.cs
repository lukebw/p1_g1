using UnityEngine;

public class TreeHurtbox : MonoBehaviour
{
    public Tree tree;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tree = GetComponentInParent<Tree>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void RegisterHit(int damage)
    {
        tree.TakeDamage(damage);
    }
}
