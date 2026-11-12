using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class NPCWander : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float changeDirectionTime = 2f;

    private CharacterController controller;

    private Animator animator;
    private Vector3 moveDirection;
    private float timer;

   void Start()
{
    controller = GetComponent<CharacterController>();
    animator = GetComponentInChildren<Animator>();

    ChooseNewDirection();
}

    void Update()
    {
        timer -= Time.deltaTime;

        // Kalau waktunya habis, pilih arah baru
        if (timer <= 0)
        {
            ChooseNewDirection();
        }

        // Gerakkan NPC
        controller.Move(moveDirection * moveSpeed * Time.deltaTime);

        if (animator != null)
        {
            animator.SetFloat("MoveX", moveDirection.x);
            animator.SetFloat("MoveY", moveDirection.z);
        }
    }

    void ChooseNewDirection()
    {
        timer = changeDirectionTime;

        int randomDirection = Random.Range(0, 4);

        switch (randomDirection)
        {
            case 0:
                moveDirection = Vector3.forward;
                break;

            case 1:
                moveDirection = Vector3.back;
                break;

            case 2:
                moveDirection = Vector3.left;
                break;

            case 3:
                moveDirection = Vector3.right;
                break;
        }
    }
}