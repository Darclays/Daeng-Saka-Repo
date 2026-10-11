using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 7f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private Animator anim;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        
        if (rb != null)
        {
            // Kunci Rotasi biar karakter gak guling-guling pas nabrak objek
            rb.constraints = RigidbodyConstraints.FreezeRotationX | 
                             RigidbodyConstraints.FreezeRotationY | 
                             RigidbodyConstraints.FreezeRotationZ;
        }
    }

    void Update()
    {
        if (Keyboard.current != null)
        {
            float moveX = 0f;
            float moveZ = 0f;

            // Input Kanan (D ATAU Panah Kanan)
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX = 1f;
            // Input Kiri (A ATAU Panah Kiri)
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX = -1f;

            // Input Maju (W ATAU Panah Atas)
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) moveZ = 1f;
            // Input Mundur (S ATAU Panah Bawah)
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) moveZ = -1f;
            
            // Simpan input X (kanan/kiri) dan Y (di sini dipakai untuk maju/mundur sumbu Z)
            moveInput = new Vector2(moveX, moveZ);
        }

        // --- LOGIKA ANIMASI RUN ---
        if (anim != null)
        {
            // Kalau salah satu tombol pergerakan lagi dipencet
            if (moveInput.x != 0f || moveInput.y != 0f)
            {
                anim.SetBool("isRunning", true); // Jalankan animasi lari
            }
            else
            {
                anim.SetBool("isRunning", false); // Balik ke Idle
            }
        }
    }

    void FixedUpdate()
    {
        // Jalankan karakter di sumbu X (kanan/kiri) DAN sumbu Z (maju/mundur)
        rb.linearVelocity = new Vector3(moveInput.x * moveSpeed, rb.linearVelocity.y, moveInput.y * moveSpeed);
    }
}