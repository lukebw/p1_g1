using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CursorController : MonoBehaviour
{
    public InputActionMap cursorActionMap;
    public ChopHitbox chopHitbox;

    InputAction pointAction;
    InputAction clickAction;

    float cursorZ = 1f;

    void Start()
    {
        cursorActionMap = InputSystem.actions.FindActionMap("Gameplay_Cursor");
        cursorActionMap.Enable();

        pointAction = cursorActionMap.FindAction("Point");
        clickAction = cursorActionMap.FindAction("Click");

        SyncCursorToMouse();
    }

    void Update()
    {
        SyncCursorToMouse();

        // Handle mouse click events
        clickAction.started += ctx =>
        {
            transform.rotation = Quaternion.Euler(0, 0, 45);

            List<Collider2D> activeCollisions = chopHitbox.GetActiveCollisions();
            foreach (Collider2D collider in activeCollisions)
            {
                collider.gameObject.GetComponent<TreeHurtbox>()?.RegisterHit(1);
            }
        };
        clickAction.performed += ctx =>
        {
            // Debug.Log("Click action performed");
        };
        clickAction.canceled += ctx =>
        {
            // Reset rotation when mouse button is released
            transform.rotation = Quaternion.Euler(0, 0, 0);
        };
    }

    void SyncCursorToMouse()
    {
        Vector2 mousePosition = pointAction.ReadValue<Vector2>();
        transform.position = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cursorZ));
    }
}
