// =============================================================================
// CameraController.cs — Camera System for Veil of Uncertainty
// Supports top-down and isometric views with smooth following of the player.
// Allows the player to switch between camera perspectives.
// Corresponds to Implementation Step 1 (Scene Configuration).
// =============================================================================

using UnityEngine;

namespace VeilOfUncertainty
{
    /// <summary>
    /// Camera modes for the 3D viewport.
    /// </summary>
    public enum CameraMode
    {
        TopDown,
        Isometric
    }

    /// <summary>
    /// CameraController manages the main camera, providing top-down and
    /// isometric views with smooth player-following behavior.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private float followSpeed = 5f;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 20f;

        [Header("Top-Down Settings")]
        [SerializeField] private float topDownHeight = 15f;
        [SerializeField] private Vector3 topDownRotation = new Vector3(90f, 0f, 0f);

        [Header("Isometric Settings")]
        [SerializeField] private float isometricHeight = 12f;
        [SerializeField] private float isometricDistance = 8f;
        [SerializeField] private Vector3 isometricRotation = new Vector3(45f, 45f, 0f);

        private Vector3 targetPosition;
        private CameraMode currentMode = CameraMode.TopDown;
        private float currentZoom;

        private void Start()
        {
            if (mainCamera == null)
                mainCamera = Camera.main;

            currentZoom = topDownHeight;
            targetPosition = Vector3.zero;
            ApplyCameraMode();
        }

        private void LateUpdate()
        {
            // Smooth follow
            Vector3 desiredPos = GetDesiredPosition();
            mainCamera.transform.position = Vector3.Lerp(
                mainCamera.transform.position, desiredPos, followSpeed * Time.deltaTime);

            // Zoom with scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentZoom -= scroll * zoomSpeed;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            }

            // Toggle camera mode with [C]
            if (Input.GetKeyDown(KeyCode.C))
            {
                ToggleCameraMode();
            }
        }

        /// <summary>
        /// Updates the camera target to follow the player.
        /// </summary>
        public void FollowPlayer(Vector3 playerWorldPos)
        {
            targetPosition = playerWorldPos;
        }

        /// <summary>
        /// Toggles between top-down and isometric camera modes.
        /// </summary>
        public void ToggleCameraMode()
        {
            currentMode = currentMode == CameraMode.TopDown
                ? CameraMode.Isometric
                : CameraMode.TopDown;
            ApplyCameraMode();
        }

        private void ApplyCameraMode()
        {
            switch (currentMode)
            {
                case CameraMode.TopDown:
                    mainCamera.transform.rotation = Quaternion.Euler(topDownRotation);
                    currentZoom = topDownHeight;
                    break;
                case CameraMode.Isometric:
                    mainCamera.transform.rotation = Quaternion.Euler(isometricRotation);
                    currentZoom = isometricHeight;
                    break;
            }
        }

        private Vector3 GetDesiredPosition()
        {
            switch (currentMode)
            {
                case CameraMode.TopDown:
                    return targetPosition + Vector3.up * currentZoom;

                case CameraMode.Isometric:
                    Vector3 offset = Quaternion.Euler(isometricRotation) * (Vector3.back * currentZoom);
                    return targetPosition - offset;

                default:
                    return targetPosition + Vector3.up * currentZoom;
            }
        }
    }
}
