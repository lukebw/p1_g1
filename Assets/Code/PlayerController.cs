using CheeseTownPhone;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public InputAction moveAction;
    public InputAction interactAction;
    public Animator playerAnimator;
    public SpriteRenderer playerSpriteRenderer;
    public Animator weaponAnimator;
    public SpriteRenderer weaponSpriteRenderer;
    public ChopHitbox chopHitbox;

    public Vector2 speed = new Vector2(4, 4);
    string isMovingParam = "isMoving";
    string isFastParam = "isFast";
    string isFacingLeftParam = "isFacingLeft";

    public float CollectRange { get; private set; }
    TownProgress progress;
    // BEGIN ADDED: Transform movement must explicitly respect the map's invisible walls.
    public CheeseTown.World.WorldBoundary worldBoundary;
    public void MoveWithinWorld(Vector2 delta)
    {
        Vector3 desired = transform.position + new Vector3(delta.x, delta.y, 0);
        transform.position = worldBoundary != null && worldBoundary.isActiveAndEnabled
            ? worldBoundary.ConstrainPosition(desired) : desired;
    }
    // END ADDED

    void Start()
    {
        moveAction = InputSystem.actions?.FindAction("Move");
        interactAction = InputSystem.actions?.FindAction("Interact");
        moveAction?.Enable();
        interactAction?.Enable();
        if (playerSpriteRenderer == null) playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // BEGIN CHANGED: A reloaded player must keep the purchased movement animation tier.
        if (playerAnimator == null) playerAnimator = GetComponentInChildren<Animator>();
        ApplyUpgrades();
        // END CHANGED
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
        // BEGIN CHANGED: Also reset when settings remove an upgrade; tolerate missing animator bindings.
        if (playerAnimator != null) playerAnimator.SetBool(isFastParam, progress.MoveSpeed >= 8);
        // END CHANGED
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
        // BEGIN CHANGED: Keep movement blocked until the closing tablet has left the screen.
        if (tablet != null && tablet.BlocksWorldInput) return;
        // END CHANGED

        // Read player directional input, then calculate and apply the change in position
        Vector2 moveInput = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        Vector2 positionDelta = MovementDelta(moveInput, Time.deltaTime);
        MoveWithinWorld(positionDelta);

        // Configure animation parameters based on movement
        if (playerAnimator != null) playerAnimator.SetBool(isMovingParam, moveInput != Vector2.zero);

        // Flip sprite horizontally based on movement direction
        if (playerSpriteRenderer != null && moveInput.x < 0)
        {
            playerSpriteRenderer.flipX = true;
            weaponAnimator.SetBool(isFacingLeftParam, true);
        }
        else if (playerSpriteRenderer != null && moveInput.x > 0)
        {
            playerSpriteRenderer.flipX = false;
            weaponAnimator.SetBool(isFacingLeftParam, false);
        }

        if ((interactAction != null && interactAction.triggered) || (Keyboard.current?.eKey.wasPressedThisFrame ?? false))
            CollectNearby();
    }
}
