using UnityEngine;
using UnityEngine.InputSystem;

namespace COMP602
{
    // Mouse look for a first-person character. Yaw rotates this object so that
    // "forward" follows the camera; pitch rotates the camera alone, clamped.
    // Also owns the cursor lock, since capturing the cursor is what makes
    // looking around possible. Other scripts read InputCaptured instead of
    // checking the cursor themselves.
    // Runs first so movement, later in the same frame, reads a rotated transform.
    [DefaultExecutionOrder(-100)]
    public class FirstPersonLook : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] Camera playerCamera;

        [Header("Look")]
        // degrees rotated per unit of look input, pointer scaling is internal
        [SerializeField] float lookSensitivity = 200f;

        [SerializeField] bool invertVerticalLook;

        // true while the cursor is captured and gameplay input should be read,
        // reads global state so it survives this component being disabled
        public static bool InputCaptured => Cursor.lockState == CursorLockMode.Locked;

        const float MaxPitchDegrees = 89f;

        // turns raw pointer delta, measured in pixels, into a usable scale,
        // which keeps this working with an unconfigured action asset
        const float LookInputScale = 0.001f;

        InputAction lookAction;
        float cameraPitch;

        // reads the current camera rotation back into the internal pitch, call
        // it after something else has moved the view or the next frame snaps
        public void SyncFromTransform()
        {
            if (playerCamera == null)
                return;

            float pitch = playerCamera.transform.localEulerAngles.x;

            // euler angles come back in 0..360, past 180 is negative
            if (pitch > 180f)
                pitch -= 360f;

            cameraPitch = Mathf.Clamp(pitch, -MaxPitchDegrees, MaxPitchDegrees);
        }

        void Start()
        {
            // the child camera keeps the prefab working if the reference is lost
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();

            if (playerCamera == null)
            {
                Debug.LogError($"{nameof(FirstPersonLook)}: no camera found. " +
                               "Add a Camera as a child of this object.", this);
                enabled = false;
                return;
            }

            lookAction = InputSystem.actions?.FindAction("Player/Look");

            if (lookAction == null)
            {
                Debug.LogError($"{nameof(FirstPersonLook)}: could not find Player/Look. " +
                               "Check Project Settings > Input System Package.", this);
                enabled = false;
                return;
            }

            SetCursorLocked(true);
        }

        void Update()
        {
            UpdateCursorLock();

            if (!InputCaptured)
                return;

            Vector2 rawLook = lookAction.ReadValue<Vector2>();

            // Y is negated, a positive euler X looks downwards in Unity
            Vector2 lookDelta = new Vector2(rawLook.x, -rawLook.y) *
                                (lookSensitivity * LookInputScale);

            transform.Rotate(0f, lookDelta.x, 0f, Space.Self);

            cameraPitch += invertVerticalLook ? -lookDelta.y : lookDelta.y;
            cameraPitch = Mathf.Clamp(cameraPitch, -MaxPitchDegrees, MaxPitchDegrees);
            playerCamera.transform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
        }

        void UpdateCursorLock()
        {
            bool escapePressed = Keyboard.current?.escapeKey.wasPressedThisFrame ?? false;
            if (escapePressed)
                SetCursorLocked(false);

            bool clickedToResume = !InputCaptured &&
                                   (Mouse.current?.leftButton.wasPressedThisFrame ?? false);
            if (clickedToResume)
                SetCursorLocked(true);
        }

        static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
