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

        Vector2 mousePosition = pointAction.ReadValue<Vector2>();
        // Input.mousePosition;
        // Vector3 mousePosition = Input.mousePosition;
        // mousePosition.z = cursorZ; // Set the Z position to the desired value
        transform.position = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cursorZ));
        // transform.position = new Vector3(mousePosition.x, mousePosition.y, cursorZ);
    }

    void Update()
    {
        Vector2 mousePosition = pointAction.ReadValue<Vector2>();
        // transform.position = new Vector3(mousePosition.x, mousePosition.y, cursorZ);
        transform.position = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cursorZ));
    }
}
