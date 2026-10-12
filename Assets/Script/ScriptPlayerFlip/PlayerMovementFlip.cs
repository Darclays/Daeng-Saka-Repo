using UnityEngine;

public class PlayerMovementFlip : MonoBehaviour
{
    public float moveSpeed = 5f;

    private CharacterController controller;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private Vector3 movement;

    void Start()
    {
        // Ambil Character Controller dari Player
        controller = GetComponent<CharacterController>();

        // Ambil Sprite Renderer dari child Sprite
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Ambil Animator dari child Sprite
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Input kanan dan kiri
        float horizontal = Input.GetAxisRaw("Horizontal");

        // Input atas dan bawah
        float vertical = Input.GetAxisRaw("Vertical");

        // Movement 3D
        movement = new Vector3(horizontal, 0f, vertical);

        // Supaya diagonal tidak lebih cepat
        if (movement.magnitude > 1f)
        {
            movement.Normalize();
        }

        // Gerakkan Player
        controller.Move(movement * moveSpeed * Time.deltaTime);


        // =========================
        // ANIMASI
        // =========================

        // Kalau tidak bergerak
        if (horizontal == 0 && vertical == 0)
        {
            animator.Play("PlayerIdle");
        }

        // Kalau bergerak kiri / kanan
        else if (horizontal != 0)
        {
            animator.Play("WalkAD");
        }

        else if (vertical > 0)
        {

        // W = jalan ke atas, tampilkan punggung
        animator.Play("WalkUp");

        }

        else if (vertical < 0)
        {

        // S = jalan ke bawah, tampilkan depan
        animator.Play("WalkDown");
        
        }   

        // =========================
        // FLIP
        // =========================

        if (horizontal < 0)
        {
            // Menghadap kiri
            spriteRenderer.flipX = true;
        }
        else if (horizontal > 0)
        {
            // Menghadap kanan
            spriteRenderer.flipX = false;
        }
    }
}