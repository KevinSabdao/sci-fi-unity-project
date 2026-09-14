using UnityEngine;
using UnityEngine.InputSystem;

namespace COMP602
{
    // Walking, gravity and ground detection for a first-person character.
    // Standalone by design: references no other script, so it runs on its own.
    // Other features extend it by pushing rather than being pulled.
    // This is the only script that may drive the CharacterController.
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonMovement : MonoBehaviour
    {
        [Header("Ground Movement")]
        [SerializeField] float walkSpeed = 6f;

        // how quickly the character reaches its target speed
        [SerializeField] float groundAcceleration = 15f;

        [Header("Air Movement")]
        [SerializeField] float gravity = 20f;

        // how much the player can steer while airborne
        [SerializeField] float airAcceleration = 25f;

        [Header("External Forces")]
        // how quickly pushes and knockbacks fade
        [SerializeField] float impulseDamping = 5f;

        [Header("Ground Detection")]
        [SerializeField] LayerMask groundLayers = ~0;

        // multiplier on walk speed, left at 1 unless something else sets it
        public float SpeedMultiplier { get; set; } = 1f;

        // true while the capsule is resting on climbable ground
        public bool IsGrounded { get; private set; }

        // current world-space velocity, in metres per second
        public Vector3 Velocity { get; private set; }

        // true while moving faster than the base walk speed
        public bool IsSprinting { get; private set; }

        // horizontal speed, in metres per second
        public float HorizontalSpeed => new Vector2(Velocity.x, Velocity.z).magnitude;

        const float GroundProbeDistance = 0.05f;
        const float AirborneGroundProbeDistance = 0.07f;

        // ground detection is suppressed briefly after jumping, or the capsule
        // finds the floor on the same frame and cancels the jump
        const float GroundingSuppressionAfterJump = 0.2f;

        CharacterController controller;
        InputAction moveAction;

        Vector3 groundNormal = Vector3.up;
        float lastJumpTime = float.NegativeInfinity;
        float pendingJumpSpeed;

        // knockback and other outside forces, kept apart from input-driven
        // velocity so the speed clamp does not swallow them
        Vector3 externalVelocity;

        // asks for an upward impulse this frame, honoured only when grounded
        public void RequestJump(float jumpSpeed) => pendingJumpSpeed = jumpSpeed;

        // adds a velocity impulse that fades over the next fraction of a second
        public void AddImpulse(Vector3 impulse) => externalVelocity += impulse;

        void Awake()
        {
            controller = GetComponent<CharacterController>();

            // prevents the capsule sticking when wedged against two colliders
            controller.enableOverlapRecovery = true;
        }

        void Start()
        {
            moveAction = InputSystem.actions?.FindAction("Player/Move");

            if (moveAction == null)
            {
                Debug.LogError($"{nameof(FirstPersonMovement)}: could not find Player/Move. " +
                               "Check Project Settings > Input System Package.", this);
                enabled = false;
            }
        }

        void Update()
        {
            UpdateGroundedState();
            ApplyMovement();

            // dropped whether or not it was used, a request made in mid-air
            // must not fire the moment the character lands
            pendingJumpSpeed = 0f;
        }

        // --- Ground -----------------------------------------------------------

        // Sweeps the capsule downwards to find the floor. Preferred over
        // CharacterController.isGrounded, which flickers on slopes and steps.
        // Also caches the ground normal so movement can follow ramps.
        void UpdateGroundedState()
        {
            // both probes clear the skin width, the gap the CharacterController
            // keeps between the capsule and any surface. A shorter probe never
            // reaches the floor and the character reads as permanently
            // airborne. The airborne probe stays shorter so the capsule does
            // not snap to ledges it merely passes.
            float probeDistance = controller.skinWidth + (IsGrounded
                ? GroundProbeDistance
                : AirborneGroundProbeDistance);

            IsGrounded = false;
            groundNormal = Vector3.up;

            if (Time.time < lastJumpTime + GroundingSuppressionAfterJump)
                return;

            bool hitSomething = Physics.CapsuleCast(
                CapsuleBottom, CapsuleTop, controller.radius,
                Vector3.down, out RaycastHit hit, probeDistance,
                groundLayers, QueryTriggerInteraction.Ignore);

            if (!hitSomething || !IsClimbable(hit.normal))
                return;

            IsGrounded = true;
            groundNormal = hit.normal;

            // snap down so the capsule does not hover on descents
            if (hit.distance > controller.skinWidth)
                controller.Move(Vector3.down * hit.distance);
        }

        bool IsClimbable(Vector3 surfaceNormal) =>
            Vector3.Dot(surfaceNormal, transform.up) > 0f &&
            Vector3.Angle(transform.up, surfaceNormal) <= controller.slopeLimit;

        Vector3 CapsuleBottom => transform.position + transform.up * controller.radius;

        Vector3 CapsuleTop => transform.position + transform.up * (controller.height - controller.radius);

        // --- Movement ---------------------------------------------------------

        void ApplyMovement()
        {
            Vector2 input = moveAction.ReadValue<Vector2>();

            // clamping stops diagonal input from beating the intended max speed
            Vector3 localDirection = Vector3.ClampMagnitude(new Vector3(input.x, 0f, input.y), 1f);
            Vector3 worldDirection = transform.TransformVector(localDirection);

            bool moving = localDirection.sqrMagnitude > 0f;
            IsSprinting = moving && SpeedMultiplier > 1f;

            float targetSpeed = walkSpeed * SpeedMultiplier;

            Vector3 velocity = IsGrounded
                ? GroundVelocity(worldDirection, targetSpeed)
                : AirVelocity(worldDirection, targetSpeed);

            controller.Move((velocity + externalVelocity) * Time.deltaTime);

            // decays after the move, so the first frame delivers the full push
            externalVelocity = Vector3.Lerp(externalVelocity, Vector3.zero,
                                            impulseDamping * Time.deltaTime);

            Velocity = RemoveVelocityIntoObstacles(velocity);
        }

        Vector3 GroundVelocity(Vector3 worldDirection, float targetSpeed)
        {
            // following the slope prevents launching off downhill ramps
            Vector3 target = ProjectOntoGroundPlane(worldDirection) * targetSpeed;
            Vector3 velocity = Vector3.Lerp(Velocity, target, groundAcceleration * Time.deltaTime);

            if (pendingJumpSpeed > 0f)
            {
                velocity = new Vector3(velocity.x, pendingJumpSpeed, velocity.z);
                lastJumpTime = Time.time;
                IsGrounded = false;
                groundNormal = Vector3.up;
            }

            return velocity;
        }

        Vector3 AirVelocity(Vector3 worldDirection, float maxHorizontalSpeed)
        {
            Vector3 velocity = Velocity + worldDirection * (airAcceleration * Time.deltaTime);

            Vector3 horizontal = Vector3.ClampMagnitude(
                Vector3.ProjectOnPlane(velocity, Vector3.up), maxHorizontalSpeed);

            velocity = horizontal + Vector3.up * velocity.y;
            return velocity + Vector3.down * (gravity * Time.deltaTime);
        }

        // cancels velocity pointing into a surface, so the player slides along
        // walls instead of sticking to them
        Vector3 RemoveVelocityIntoObstacles(Vector3 velocity)
        {
            bool blocked = Physics.CapsuleCast(
                CapsuleBottom, CapsuleTop, controller.radius,
                velocity.normalized, out RaycastHit hit,
                velocity.magnitude * Time.deltaTime, ~0, QueryTriggerInteraction.Ignore);

            return blocked ? Vector3.ProjectOnPlane(velocity, hit.normal) : velocity;
        }

        // reprojects a horizontal direction so it runs parallel to the ground
        Vector3 ProjectOntoGroundPlane(Vector3 direction)
        {
            if (direction.sqrMagnitude < Mathf.Epsilon)
                return Vector3.zero;

            Vector3 right = Vector3.Cross(direction.normalized, transform.up);
            return Vector3.Cross(groundNormal, right).normalized * direction.magnitude;
        }
    }
}
