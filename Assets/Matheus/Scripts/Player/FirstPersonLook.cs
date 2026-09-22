using UnityEngine;
using UnityEngine.InputSystem;

namespace COMP602
{
    // Mouse look for a first-person character. Yaw rotates this object so that
    // "forward" follows the camera; pitch rotates the camera alone, clamped.
    // Also owns the cursor lock, since capturing the cursor is what makes
    // looking around possible. Other scripts read InputCaptured instead of
    // checking the cursor themselves.
    // Menus do not touch the cursor or Time.timeScale either: they call
    // PushUiModal/PopUiModal, so one owner decides when the game is frozen and
    // who holds the pointer, and two menus open at once cannot fight over it.
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

        // how many interactive panels currently want the cursor. A count rather
        // than a flag, so an inventory opened on top of a pause menu still
        // leaves the cursor free when only one of the two closes.
        static int uiModalCount;

        // true while a panel owns the cursor, so a click belongs to that panel
        // and must not hand the cursor back to the camera
        public static bool UiHasCursor => uiModalCount > 0;

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

        // call when an interactive panel opens, it hands the cursor to the UI
        // and freezes the game
        public static void PushUiModal()
        {
            uiModalCount++;

            if (uiModalCount == 1)
            {
                Time.timeScale = 0f;
                SetCursorLocked(false);
            }
        }

        // call when a panel closes, the game resumes and the camera takes the
        // cursor back only once the last panel is gone
        public static void PopUiModal()
        {
            if (uiModalCount == 0)
                return;

            uiModalCount--;

            if (uiModalCount == 0)
            {
                Time.timeScale = 1f;
                SetCursorLocked(true);
            }
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

            // a reloaded scene starts with no panel open, so drop any count left
            // over from the previous one
            uiModalCount = 0;

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
            // a panel owns the cursor while it is open. without this, the click
            // the player aims at that panel is read as "click to resume" below,
            // which recaptures the cursor and pins the pointer to screen centre
            if (UiHasCursor)
                return;

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
