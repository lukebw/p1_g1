using UnityEngine;
using System.Collections.Generic;

public class ChopHitbox : MonoBehaviour
{
    public UnityEngine.Events.UnityEvent onTreeHit = new UnityEngine.Events.UnityEvent();

    void OnTriggerEnter2D(Collider2D hurtbox)
    {
        if (hurtbox != null && hurtbox.gameObject.tag == "Tree" &&hurtbox.enabled && hurtbox.gameObject.activeInHierarchy)
        {
            TreeHurtbox treeHurtbox = hurtbox.GetComponent<TreeHurtbox>();
            if (treeHurtbox != null && treeHurtbox.enabled)
            {
                onTreeHit.Invoke();
                treeHurtbox.RegisterHit(1);
            }
        }
    }
}
