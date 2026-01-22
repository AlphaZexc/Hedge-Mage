using UnityEngine;
// using UnityEngine.AI; // NavMeshAgent isn't used here, can likely remove

public abstract class BaseCreature : MonoBehaviour
{
    [Header("Detection & Movement")]
    public float detectionRange = 5f;
    public float moveSpeed = 2f;

    [Header("Combat Settings")]
    [SerializeField] protected int damage = 25;

    public int Damage => damage; // Optional public getter if you need to read it elsewhere



    [Header("Lifetime")]
    public float maxLifeDuration = -1f; // -1 means live forever

    protected GameObject player;
    protected Rigidbody2D rb;
    protected Vector2 movement; // Current movement direction
    protected float spawnTime;

    protected virtual void Start()
    {
        // Cache Rigidbody2D component
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("BaseCreature requires a Rigidbody2D component.", this);
        }

        // Find Player - Ensure your player GameObject has the "Player" tag!
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("BaseCreature could not find GameObject with tag 'Player'.", this);
        }

        spawnTime = Time.time;
    }

    protected virtual void Update()
    {
        // Handle lifetime expiration
        if (maxLifeDuration > 0 && Time.time - spawnTime > maxLifeDuration)
        {
            // Consider using Destroy(gameObject) or an object pooling system
            gameObject.SetActive(false);
            return;
        }

        // If player doesn't exist, do nothing further
        if (player == null)
        {
             // Optionally stop moving if player disappears
             if (rb != null) rb.linearVelocity = Vector2.zero;
             return;
        }

        // --- Base Movement Logic ---
        float distance = Vector2.Distance(transform.position, player.transform.position);
        if (distance < detectionRange)
        {
            // Move towards player
            movement = (player.transform.position - transform.position).normalized;
        }
        else
        {
            // Simple random wandering when player is out of range
            Wander();
        }

        // Apply movement using Rigidbody velocity
        // Note: Derived classes (like StraightChaser) might override this in their Update
        ApplyMovement();
    }

    protected virtual void Wander()
    {
         // Change direction occasionally for random wandering
        if (Random.Range(0f, 100f) < 1f) // Adjust frequency as needed
        {
           movement = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        }
        // If no change, keep current direction (or set movement to zero if you want it to stop)
        // movement = Vector2.zero; // Uncomment if you want idle behaviour instead of wander
    }


    protected virtual void ApplyMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = movement * moveSpeed;
        }
    }




    // --- ADDED: TRIGGER DETECTION FOR DAMAGE ---
    // This method is called by Unity's 2D Physics engine
    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        // Check if the object we collided with is the Player
        if (player != null && collision.gameObject == player) // More robust check than tag if player reference exists
        // Or use CompareTag if player reference might be unreliable: if (collision.CompareTag("Player"))
        {
            Debug.Log($"{gameObject.name} collided with Player.", this);

            // Get the PlayerHealth component from the player
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();

            // If the player has the health script, deal damage
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage); // Use the damage value from this creature

                // Optional: Add reaction logic here (e.g., knockback, destroy self)
                // Destroy(gameObject); // Example: Creature dies on impact
            }
            else
            {
                Debug.LogWarning($"Collided with Player object, but it doesn't have a PlayerHealth component.", collision.gameObject);
            }
        }
    }

    
    public virtual void OnPlayerCollision(GameObject player)
    {
        Debug.Log($"{this.name} hit player for {damage} damage.");
        player.GetComponent<PlayerHealth>()?.TakeDamage(damage);
    }
}