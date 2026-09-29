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

    void OnEnable()
    {
        progress = TownSession.Instance.Progress;
        progress.Changed += ApplyUpgrades;
        ApplyUpgrades();
    }
    void OnDisable() { if (progress != null) progress.Changed -= ApplyUpgrades; }
    void ApplyUpgrades()
    {
        speed = Vector2.one * progress.MoveSpeed;
        CollectRange = progress.CollectRange;
    }
    void Start()
    {
        moveAction = InputSystem.actions?.FindAction("Move");
        interactAction = InputSystem.actions?.FindAction("Interact");
        moveAction?.Enable();
        interactAction?.Enable();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }
    public Vector2 MovementDelta(Vector2 input, float deltaTime)
    { return Vector2.ClampMagnitude(input, 1) * speed * deltaTime; }
    public bool CanCollect(TreeHurtbox tree)
    { return tree != null && tree.DistanceFrom(transform.position) <= CollectRange + .05f; }
    public int CollectNearby()
    {
        foreach (var tree in FindObjectsByType<TreeHurtbox>(FindObjectsSortMode.None))
            if (CanCollect(tree)) return tree.Collect();
        return 0;
    }
    void Update()
    {
        var tablet = FindAnyObjectByType<CheeseTownDemo>();
        if (tablet != null && tablet.PhoneOpen) return;
        Vector2 input = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector2 delta = MovementDelta(input, Time.deltaTime);
        transform.position += new Vector3(delta.x, delta.y, 0);
        if (spriteRenderer != null && input.x != 0) spriteRenderer.flipX = input.x < 0;
        if ((interactAction != null && interactAction.triggered) || (Keyboard.current?.eKey.wasPressedThisFrame ?? false))
            CollectNearby();
    }
}