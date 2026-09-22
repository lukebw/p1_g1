using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Components assigned in Unity Editor
    public InputAction moveAction;
    public SpriteRenderer spriteRenderer;

    public Vector2 speed = new Vector2(15, 15);
    
    void Start() {
        moveAction = InputSystem.actions.FindAction("Move");
        moveAction.Enable();
    }

    void Update() {
        // Read player directional input, then calculate and apply the change in position
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Debug.Log("Move Input: " + moveInput.ToString());
        Vector2 positionDelta = moveInput * speed * Time.deltaTime;
        transform.position += new Vector3(positionDelta.x, positionDelta.y, 0);

        // Flip sprite horizontally based on movement direction
        if (moveInput.x < 0) {
            spriteRenderer.flipX = true;
        } else if (moveInput.x > 0) {
            spriteRenderer.flipX = false;
        }
    }
}
