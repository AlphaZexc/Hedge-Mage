using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;

    [Header("Component Links")]
    public Transform playerLightTransform;  // Still assignable, but unused now

    private Vector2 movement;
    private Rigidbody2D rb;
    private PlayerHealth playerHealth;
    private Animator animator;
    private bool canMove = true;
    private Vector2 lastMoveDirection = Vector2.down;
    private bool isBookOpen = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerHealth = GetComponent<PlayerHealth>();
        animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogWarning("PlayerMovement: Animator component not found!");
        }
    }

    void Update()
    {
        HandleBookToggle();

        if (playerHealth != null && playerHealth.IsDead)
        {
            movement = Vector2.zero;

            if (animator != null)
            {
                animator.SetBool("IsDead", true);
                animator.SetFloat("MoveX", 0f);
                animator.SetFloat("MoveY", -1f);
                animator.SetFloat("Speed", 0f);
            }

            return;
        }

        if (animator != null)
        {
            animator.SetBool("IsDead", false);
        }

        if (!canMove)
        {
            movement = Vector2.zero;
            UpdateAnimatorMovement(Vector2.zero);
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Only allow one direction at a time (no diagonal movement)
        if (Mathf.Abs(horizontal) > 0.01f)
        {
            movement.x = horizontal;
            movement.y = 0f;
        }
        else if (Mathf.Abs(vertical) > 0.01f)
        {
            movement.x = 0f;
            movement.y = vertical;
        }
        else
        {
            movement = Vector2.zero;
        }

        if (movement != Vector2.zero)
        {
            lastMoveDirection = movement.normalized;
        }

        UpdateAnimatorMovement(movement);
    }

    void FixedUpdate()
    {
        if (!canMove || (playerHealth != null && playerHealth.IsDead))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.MovePosition(rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime);
    }

    void UpdateAnimatorMovement(Vector2 moveInput)
    {
        if (animator != null)
        {
            float speed = moveInput.sqrMagnitude;
            animator.SetFloat("Speed", speed);

            if (speed > 0.01f)
            {
                animator.SetFloat("MoveX", moveInput.x);
                animator.SetFloat("MoveY", moveInput.y);
                lastMoveDirection = moveInput.normalized;
            }
            else
            {
                // When idle, keep facing the last move direction
                animator.SetFloat("MoveX", lastMoveDirection.x);
                animator.SetFloat("MoveY", lastMoveDirection.y);
            }
        }
    }

    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;

        if (!enabled)
        {
            movement = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            UpdateAnimatorMovement(Vector2.zero);
        }
    }

    void HandleBookToggle()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleBookPopup();
        }
    }

    public void ToggleBookPopup()
    {
        isBookOpen = !isBookOpen;

        if (isBookOpen)
        {
            LevelPopupManager.Instance.ShowBookPopup();
        }
        else
        {
            LevelPopupManager.Instance.CloseBookPopup();
        }
    }

    public void ToggleBookPopupFromUI()
    {
        Debug.Log("Book button clicked!");
        ToggleBookPopup();
    }

    public Vector2 GetLastMoveDirection()
    {
        return lastMoveDirection;
    }
}