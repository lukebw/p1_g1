using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public Transform playerTransform;

    public float cameraOffsetZ;

    void Start()
    {
        transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, cameraOffsetZ);
        cameraOffsetZ = playerTransform.position.z - 10;
    }

    void Update()
    {
        // Update camera position to match player position
        transform.position = new Vector3(playerTransform.position.x, playerTransform.position.y, cameraOffsetZ);

        // TODO: Add camera bounds to stay within level boundaries
        // TODO: Add camera smoothing to make the camera movement less abrupt (i.e. player not always directly centered)
    }
}
