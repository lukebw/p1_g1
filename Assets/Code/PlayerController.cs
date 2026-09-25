using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Components assigned in Unity Editor
    public InputAction moveAction;
    public InputAction interactAction;
    public SpriteRenderer spriteRenderer;
    public ChopHitbox chopHitbox;

    public Vector2 speed = new Vector2(15, 15);

    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        moveAction.Enable();

        interactAction = InputSystem.actions.FindAction("Interact");
        interactAction.Enable();
    }

    void Update()
    {
        // Read player directional input, then calculate and apply the change in position
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector2 positionDelta = moveInput * speed * Time.deltaTime;
        transform.position += new Vector3(positionDelta.x, positionDelta.y, 0);

        // Flip sprite horizontally based on movement direction
        if (moveInput.x < 0)
        {
            spriteRenderer.flipX = true;
        }
        else if (moveInput.x > 0)
        {
            spriteRenderer.flipX = false;
        }

        if (interactAction.triggered)
        {
            List<Collider> activeCollisions = chopHitbox.GetActiveCollisions();

            foreach (Collider collider in activeCollisions)
            {
                collider.gameObject.GetComponent<TreeHurtbox>()?.RegisterHit(1);
            }
        }
    }
}
