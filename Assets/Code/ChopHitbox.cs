using UnityEngine;

public class ChopHitbox : MonoBehaviour
{
    public TreeHitAudio treeHitAudio;

    void Awake()
    {
        // Reuse the authored component; support hitboxes created without editor setup.
        treeHitAudio = GetComponent<TreeHitAudio>();
        if (treeHitAudio == null) treeHitAudio = gameObject.AddComponent<TreeHitAudio>();
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
