using CheeseTownPhone;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public InputAction moveAction;
    public InputAction interactAction;
    public SpriteRenderer spriteRenderer;
    public ChopHitbox chopHitbox;
    public Vector2 speed = new Vector2(4, 4);
    public float CollectRange { get; private set; }
    TownProgress progress;

    void Start()
    {
        moveAction = InputSystem.actions?.FindAction("Move");
        interactAction = InputSystem.actions?.FindAction("Interact");
        moveAction?.Enable();
        interactAction?.Enable();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable()
    {
        progress = TownSession.Instance.Progress;
        progress.Changed += ApplyUpgrades;
        ApplyUpgrades();
    }

    void OnDisable()
    {
        if (progress != null) progress.Changed -= ApplyUpgrades;
    }

    void ApplyUpgrades()
    {
        speed = Vector2.one * progress.MoveSpeed;
        CollectRange = progress.CollectRange;
    }

    public Vector2 MovementDelta(Vector2 input, float deltaTime)
    {
        return Vector2.ClampMagnitude(input, 1) * speed * deltaTime;
    }

    public bool CanCollect(TreeHurtbox tree)
    {
        return tree != null && tree.DistanceFrom(transform.position) <= CollectRange + .05f;
    }

    public int CollectNearby()
    {
        foreach (var tree in FindObjectsByType<TreeHurtbox>(FindObjectsSortMode.None))
            if (CanCollect(tree)) return tree.Collect();
        return 0;
    }

    void Update()
    {
        // Pause world controls while any tablet or story screen is visible.
        var tablet = FindAnyObjectByType<CheeseTownDemo>();
        if (tablet != null && (tablet.PhoneOpen || tablet.EndingOpen)) return;

        // Read player directional input, then calculate and apply the change in position
        Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector2 positionDelta = MovementDelta(moveInput, Time.deltaTime);
        transform.position += new Vector3(positionDelta.x, positionDelta.y, 0);

        // Flip sprite horizontally based on movement direction
        if (spriteRenderer != null && moveInput.x < 0)
        {
            spriteRenderer.flipX = true;
        }
        else if (spriteRenderer != null && moveInput.x > 0)
        {
            spriteRenderer.flipX = false;
        }

        if ((interactAction != null && interactAction.triggered) || (Keyboard.current?.eKey.wasPressedThisFrame ?? false))
            CollectNearby();
    }
}
