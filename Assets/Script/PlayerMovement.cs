using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 7f;
    public float jumpForce = 8f;
    public float gravity = 20f;

    [Header("Visuals")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
        // untuk Cek apakah di nempe di tanah
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // Ambil Input (New Input System)
        float x = 0, z = 0;
        if (Keyboard.current != null)
        {
            // Gerak Horizontal & Vertikal (X & Z)
            if (Keyboard.current.wKey.isPressed) z = 1;
            else if (Keyboard.current.sKey.isPressed) z = -1;
            if (Keyboard.current.aKey.isPressed) x = -1;
            else if (Keyboard.current.dKey.isPressed) x = 1;

            // Input Lompat
            if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpForce * 2f * gravity);
            }
        }

        // Eksekusi Gerakan berjalannya
        Vector3 moveDirection = new Vector3(x, 0, z).normalized;
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        // menerapkan Gravitasi
        velocity.y -= gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Update Animasi & Flip
        bool walking = moveDirection.magnitude > 0;
        UpdateVisuals(x, z, walking);
    }

    void UpdateVisuals(float horizontalInput, float verticalInput, bool isWalking)
{
    if (animator != null)
    {
        animator.SetBool("Run", isWalking);
        animator.SetBool("Jump", !isGrounded);

        animator.SetFloat("MoveX", horizontalInput);
        animator.SetFloat("MoveY", verticalInput);
    }

}
}