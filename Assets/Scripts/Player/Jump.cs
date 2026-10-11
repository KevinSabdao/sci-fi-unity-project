using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerJump : MonoBehaviour
{
    [SerializeField] float jumpSpeed = 8f;
    [SerializeField] float gravity = 20f;

    CharacterController controller;
    float verticalVelocity;

    void Awake() => controller = GetComponent<CharacterController>();

    void Update()
    {
        if (controller.isGrounded)
        {
            verticalVelocity = -1f;

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
                verticalVelocity = jumpSpeed;
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }
}
