using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;

    [Header("Footsteps")]
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float footstepInterval = 0.4f;
    [SerializeField][Range(0f, 1f)] private float footstepVolume = 0.7f;

    public float moveSpeed = 5f;
    private PlayerHealth playerHealth => PlayerHealth.Instance;
    private Vector2 movement;
    private Vector2 lastMoveDirection = Vector2.down;
    private bool canMove = true;

    private Coroutine footstepCoroutine;
    private bool wasMoving = false;

    void Update()
    {
        if (playerHealth != null && playerHealth.isDead)
        {
            movement = Vector2.zero;
            StopFootsteps();
            if (animator != null)
            {
                animator.SetFloat("MoveX", 0f);
                animator.SetFloat("MoveY", -1f);
                animator.SetFloat("Speed", 0f);
            }
            return;
        }

        if (!canMove)
        {
            movement = Vector2.zero;
            StopFootsteps();
            UpdateAnimatorMovement(Vector2.zero);
            return;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Prioritize horizontal or vertical — no diagonal movement
        if (horizontal != 0)
            movement = new Vector2(horizontal, 0f);
        else
            movement = new Vector2(0f, vertical);

        if (movement != Vector2.zero)
            lastMoveDirection = movement.normalized;

        // Start or stop footsteps based on whether the player is moving
        bool isMoving = movement != Vector2.zero;
        if (isMoving && !wasMoving)
            StartFootsteps();
        else if (!isMoving && wasMoving)
            StopFootsteps();

        wasMoving = isMoving;
        UpdateAnimatorMovement(movement);
    }

    void FixedUpdate()
    {
        if (!canMove || playerHealth.isDead)
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
                animator.SetFloat("MoveX", lastMoveDirection.x);
                animator.SetFloat("MoveY", lastMoveDirection.y);
            }
        }
    }

    private void StartFootsteps()
    {
        if (footstepAudioSource == null || footstepClips == null || footstepClips.Length == 0)
            return;

        if (footstepCoroutine != null)
            StopCoroutine(footstepCoroutine);

        footstepCoroutine = StartCoroutine(FootstepLoop());
    }

    private void StopFootsteps()
    {
        if (footstepCoroutine != null)
        {
            StopCoroutine(footstepCoroutine);
            footstepCoroutine = null;
        }
    }

    private IEnumerator FootstepLoop()
    {
        while (true)
        {
            PlayFootstepSound();
            yield return new WaitForSeconds(footstepInterval);
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepAudioSource == null || footstepClips.Length == 0) return;

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        footstepAudioSource.PlayOneShot(clip, footstepVolume);
    }

    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;
        if (!enabled)
        {
            movement = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            StopFootsteps();
        }
    }

    public Vector2 GetLastMoveDirection()
    {
        return lastMoveDirection;
    }
}