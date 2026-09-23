using UnityEngine;

public class ChopHitbox : MonoBehaviour
{
    public Transform playerTransform;
    public SpriteRenderer playerSprite;

    public float offsetX = 0.5f;

    void Update()
    {
        // Extend the hitbox slightly in front of the player depending on the direction they are facing
        transform.position = playerTransform.position + new Vector3(playerSprite.flipX ? -offsetX : offsetX, 0, 0);
    }
}
