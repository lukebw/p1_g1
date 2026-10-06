using UnityEngine;
using System.Collections.Generic;

public class ChopHitbox : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D hurtbox)
    {
        if (hurtbox != null && hurtbox.gameObject.tag == "Tree" &&hurtbox.enabled && hurtbox.gameObject.activeInHierarchy)
        {
            TreeHurtbox treeHurtbox = hurtbox.GetComponent<TreeHurtbox>();
            if (treeHurtbox != null && treeHurtbox.enabled)
            {
                treeHurtbox.RegisterHit(1);
            }
        }
    }
}
