using UnityEngine;

namespace GamecommMentor.DaengSaka
{
    [DefaultExecutionOrder(200)]
    [DisallowMultipleComponent]
    public sealed class GmSpriteBillboard : MonoBehaviour
    {
        [SerializeField] private Camera viewCamera;

        public void SetCamera(Camera cameraToFace) => viewCamera = cameraToFace;

        private void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (viewCamera != null) transform.rotation = viewCamera.transform.rotation;
        }
    }
}
