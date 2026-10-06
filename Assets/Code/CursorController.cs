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
        if (tablet != null && tablet.BlocksWorldInput) return;
        // END CHANGED
        transform.rotation = Quaternion.Euler(0, 0, 45);
        if (chopHitbox == null) return;
        weaponAnimator.SetTrigger(onChopActionParam); // Trigger weapon swing animation
        HashSet<Tree> hitTrees = new HashSet<Tree>();
        foreach (Collider2D collider in chopHitbox.GetActiveCollisions())
        {
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy) continue;
            TreeHurtbox hurtbox = collider.GetComponent<TreeHurtbox>();
            if (hurtbox == null || !hurtbox.enabled) continue;
            Tree tree = hurtbox.tree != null ? hurtbox.tree : hurtbox.GetComponentInParent<Tree>();
            if (tree != null) {
                hitTrees.Add(tree);
                hurtbox.RegisterHit(1);
            }
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
