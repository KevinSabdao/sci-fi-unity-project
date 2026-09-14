using UnityEngine;
using UnityEngine.InputSystem;

namespace COMP602
{
    // Sprint and jump input for a first-person character. Jumping belongs to
    // User Story 3, owned by another team member, so it lives here instead of
    // inside FirstPersonMovement.
    //
    // The dependency runs one way only: this script knows about movement, and
    // movement knows nothing about this script. Remove the component and
    // walking is unaffected.
    //
    // Runs before movement so the speed multiplier and any jump request land
    // in the same frame they were pressed.
    [DefaultExecutionOrder(-50)]
    [RequireComponent(typeof(FirstPersonMovement))]
    public class FirstPersonSprintJump : MonoBehaviour
    {
        [Header("Sprint")]
        [SerializeField] float sprintMultiplier = 1.8f;

        [Header("Jump")]
        [SerializeField] float jumpSpeed = 8f;

        // true while the sprint input is held, moving or not
        public bool SprintHeld { get; private set; }

        FirstPersonMovement movement;
        InputAction jumpAction;
        InputAction sprintAction;

        void Awake() => movement = GetComponent<FirstPersonMovement>();

        void Start()
        {
            jumpAction = InputSystem.actions?.FindAction("Player/Jump");
            sprintAction = InputSystem.actions?.FindAction("Player/Sprint");

            if (jumpAction == null || sprintAction == null)
            {
                Debug.LogError($"{nameof(FirstPersonSprintJump)}: could not find Player/Jump and " +
                               "Player/Sprint. Check Project Settings > Input System Package.", this);
                enabled = false;
            }
        }

        void OnDisable()
        {
            // handing the multiplier back stops an endless sprint
            SprintHeld = false;

            if (movement != null)
                movement.SpeedMultiplier = 1f;
        }

        void Update()
        {
            SprintHeld = sprintAction.IsPressed();
            movement.SpeedMultiplier = SprintHeld ? sprintMultiplier : 1f;

            if (jumpAction.WasPressedThisFrame())
                movement.RequestJump(jumpSpeed);
        }
    }
}
