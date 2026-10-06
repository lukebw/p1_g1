using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CursorController : MonoBehaviour
{
    public InputActionMap cursorActionMap;
    public ChopHitbox chopHitbox;
    public Animator weaponAnimator;

    InputAction pointAction;
    InputAction clickAction;

    float cursorZ = 1f;
    string onChopActionParam = "onChopAction";

    // BEGIN MODIFIED: Subscribe once per enable so clicks do not accumulate each frame.
    void OnEnable()
    {
        cursorActionMap = InputSystem.actions?.FindActionMap("Gameplay_Cursor");
        if (cursorActionMap == null) return;

        pointAction = cursorActionMap.FindAction("Point");
        clickAction = cursorActionMap.FindAction("Click");
        if (clickAction != null)
        {
            clickAction.started += OnChopStarted;
            clickAction.canceled += OnChopCanceled;
        }
        cursorActionMap.Enable();
        SyncCursorToMouse();
    }
    // END MODIFIED

    // BEGIN MODIFIED: Update only follows the mouse; callbacks own click handling.
    void Update()
    {
        SyncCursorToMouse();
    }
    // END MODIFIED

    // BEGIN ADDED: Pair subscriptions and hit each live tree at most once per click.
    void OnDisable()
    {
        if (clickAction != null)
        {
            clickAction.started -= OnChopStarted;
            clickAction.canceled -= OnChopCanceled;
        }
        transform.rotation = Quaternion.identity;
    }

    void OnChopStarted(InputAction.CallbackContext context)
    {
        var tablet = FindAnyObjectByType<CheeseTownPhone.CheeseTownDemo>();
        // BEGIN CHANGED: Prevent world chops through a tablet that is still closing.
        if (tablet != null && (tablet.BlocksWorldInput || tablet.IsPointerOverInteractionGuide(pointAction.ReadValue<Vector2>()))) return;
        // END CHANGED
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger(onChopActionParam); // Trigger weapon swing animation
        }
    }

    void OnChopCanceled(InputAction.CallbackContext context)
    {
        transform.rotation = Quaternion.identity;
    }
    // END ADDED

    // BEGIN MODIFIED: Missing input or camera should not break scene loading.
    void SyncCursorToMouse()
    {
        if (pointAction == null || Camera.main == null) return;
        Vector2 mousePosition = pointAction.ReadValue<Vector2>();
        transform.position = Camera.main.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cursorZ));
    }
    // END MODIFIED
}
