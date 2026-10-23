using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]
public class Camera2DLock : MonoBehaviour
{
    [Header("Camera Settings")]
    public Vector3 fixedRotation = new Vector3(25f, 0f, 0f);
    public float cameraDistance = 25f;

    private CinemachinePositionComposer composer;

    void Awake()
    {
        composer = GetComponent<CinemachinePositionComposer>();
        if (composer != null)
            composer.CameraDistance = cameraDistance;

        transform.rotation = Quaternion.Euler(fixedRotation);
    }
}