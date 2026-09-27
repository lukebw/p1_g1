using UnityEngine;
using UnityEngine.InputSystem;

public class CursorController : MonoBehaviour
{
    public InputActionMap cursorActionMap;

    InputAction pointAction;
    InputAction clickAction;

    float cursorZ = 1f;

    void Start()
    {
        cursorActionMap = InputSystem.actions.FindActionMap("UI");
        cursorActionMap.Enable();

        pointAction = cursorActionMap.FindAction("Point");
        clickAction = cursorActionMap.FindAction("Click");

        SyncCursorToMouse();
    }

    void Update()
    {
        SyncCursorToMouse();
    }

    void SyncCursorToMouse()
    {
        Vector2 mousePosition = pointAction.ReadValue<Vector2>();
        transform.position = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cursorZ));
    }
}
