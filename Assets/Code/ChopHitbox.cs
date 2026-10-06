using UnityEngine;

public class ChopHitbox : MonoBehaviour
{
    public TreeHitAudio treeHitAudio;

    void Awake()
    {
        treeHitAudio = this.gameObject.AddComponent<TreeHitAudio>();
    }

    void OnTriggerEnter2D(Collider2D hurtbox)
    {
        if (hurtbox != null && hurtbox.gameObject.tag == "Tree" &&hurtbox.enabled && hurtbox.gameObject.activeInHierarchy)
        {
            TreeHurtbox treeHurtbox = hurtbox.GetComponent<TreeHurtbox>();
            if (treeHurtbox != null && treeHurtbox.enabled)
            {
                treeHitAudio?.Play();
                treeHurtbox.RegisterHit(1);
            }
        }
    }
}
