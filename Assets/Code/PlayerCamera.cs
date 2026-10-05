using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform playerTransform;

    public float cameraOffsetZ;

    // BEGIN ADDED: Keep the camera inside the baked ground, including at corners.
    public CheeseTown.World.WorldBoundary worldBoundary;
    Camera view;
    // END ADDED

    void Start()
    {
        view = GetComponent<Camera>();
        cameraOffsetZ = playerTransform.position.z - 10;
        FollowPlayer();
    }

    void LateUpdate()
    {
        FollowPlayer();
    }

    // BEGIN ADDED: Following after movement avoids a one-frame camera lag.
    void FollowPlayer()
    {
        if (playerTransform == null) return;
        Vector3 desired = new Vector3(playerTransform.position.x, playerTransform.position.y, cameraOffsetZ);
        transform.position = worldBoundary != null && worldBoundary.isActiveAndEnabled && view != null && view.orthographic
            ? worldBoundary.ConstrainCamera(desired, view.orthographicSize, view.aspect) : desired;
    }
    // END ADDED
}
