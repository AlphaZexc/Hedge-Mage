using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public GameObject interactionPrompt;
    public float interactionDistance = 0.5f;
    public float interactionRadius = 0.3f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Prompt Settings")]
    public Vector3 promptOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Layer Filtering")]
    public LayerMask interactionLayer;

    private PlayerMovement playerMovement;

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();

        if (interactionPrompt != null)
            interactionPrompt.SetActive(false);
    }

    private void Update()
    {
        if (playerMovement == null)
            return;

        Vector2 facingDirection = playerMovement.GetLastMoveDirection();
        if (facingDirection == Vector2.zero)
            facingDirection = Vector2.down;

        bool foundInteractable = false;

        RaycastHit2D hit = Physics2D.CircleCast(
            transform.position,
            interactionRadius,
            facingDirection,
            interactionDistance,
            interactionLayer
        );

        if (hit.collider != null)
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject.CompareTag("Fountain"))
            {
                if (WordProgressManager.Instance.AllLettersCollected)
                {
                    foundInteractable = true;

                    if (Input.GetKeyDown(interactKey))
                        CompleteLevelAtFountain(hitObject);
                }
            }
            else if (hitObject.CompareTag("Gate"))
            {
                Gate gate = hitObject.GetComponent<Gate>();

                if (gate != null && !gate.IsOpen && !gate.isLocked)
                {
                    foundInteractable = true;

                    if (Input.GetKeyDown(interactKey))
                        gate.Interact();
                }
            }
        }

        HandlePrompt(foundInteractable);
    }

    private void HandlePrompt(bool shouldShow)
    {
        if (interactionPrompt == null)
            return;

        interactionPrompt.SetActive(shouldShow);

        if (shouldShow)
        {
            interactionPrompt.transform.position = transform.position + promptOffset;
        }
    }

    private void CompleteLevelAtFountain(GameObject fountainObject)
    {
        float finalTime = PlayerHealth.Instance.GetElapsedLevelTime();
        LevelPopupManager.Instance.ShowLevelCompletePopup(finalTime);
    }

    // ---------------- GIZMO ----------------
    private void OnDrawGizmosSelected()
    {
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement == null)
            return;

        Vector2 direction = movement.GetLastMoveDirection();
        if (direction == Vector2.zero)
            direction = Vector2.down;

        Vector3 origin = transform.position;
        Vector3 end = origin + (Vector3)(direction.normalized * interactionDistance);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(end, interactionRadius);
    }
}