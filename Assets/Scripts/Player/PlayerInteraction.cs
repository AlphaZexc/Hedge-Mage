using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("The UI element to show when the player can interact.")]
    public GameObject interactionPrompt;
    [Tooltip("How far in front of the player to check for interactable objects.")]
    public float interactionDistance = 1.5f;
    [Tooltip("The key the player must press to interact.")]
    public KeyCode interactKey = KeyCode.E;

    private PlayerMovement playerMovement; // Reference to get the player's facing direction

    private void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }
    }

    private void Update()
    {
        // Hide the prompt by default each frame
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(false);
        }

        // Get the last direction the player was moving
        Vector2 facingDirection = playerMovement.GetLastMoveDirection();

        // Perform a CircleCast to see what's in front of the player
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, 0.5f, facingDirection, interactionDistance);

        // Check if we hit something
        if (hit.collider != null)
        {
            // Check if the thing we hit is the Fountain
            if (hit.collider.CompareTag("Fountain"))
            {
                // If it is the fountain, check if the word is complete
                if (WordProgressManager.Instance.AllLettersCollected)
                {
                    // If all conditions are met, show the prompt!
                    if (interactionPrompt != null)
                    {
                        interactionPrompt.SetActive(true);
                    }

                    // And check if the player presses the interact key
                    if (Input.GetKeyDown(interactKey))
                    {
                        CompleteLevelAtFountain(hit.collider.gameObject);
                    }
                }
            }
        }
    }
    
    private void CompleteLevelAtFountain(GameObject fountainObject)
    {
        Debug.Log("Player interacted with Fountain! Level Complete.");
        
        // You could tell the fountain to play an animation here if you wanted
        // fountainObject.GetComponent<Animator>().SetTrigger("Activate");

        float finalTime = PlayerHealth.Instance.GetElapsedLevelTime();
        LevelPopupManager.Instance.ShowLevelCompletePopup(finalTime);
    }
}