using UnityEngine;

public abstract class BaseCreature : MonoBehaviour
{
    [Header("Detection & Movement")]
    public float detectionRange = 5f;
    public float moveSpeed = 3f;

    [Header("Combat")]
    [SerializeField] protected int damage = 25;

    [Header("Lifetime")]
    public float maxLifeDuration = -1f;

    protected GameObject player;
    protected Rigidbody2D rb;
    protected Vector2 movement;
    private float spawnTime;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError($"{name} requires a Rigidbody2D.", this);
        }

        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning($"{name} could not find Player.", this);
        }

        spawnTime = Time.time;
    }

    protected virtual void Update()
    {
        if (maxLifeDuration > 0f && Time.time - spawnTime > maxLifeDuration)
        {
            gameObject.SetActive(false);
            return;
        }

        if (player == null)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            return;
        }

        float distance = Vector2.Distance(transform.position, player.transform.position);

        if (distance <= detectionRange)
        {
            movement = (player.transform.position - transform.position).normalized;
        }
        else
        {
            Wander();
        }

        ApplyMovement();
    }

    protected virtual void Wander()
    {
        if (Random.value < 0.01f)
        {
            movement = Random.insideUnitCircle.normalized;
        }
    }

    protected virtual void ApplyMovement()
    {
        if (rb != null)
        {
            rb.linearVelocity = movement * moveSpeed;
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (player == null || collision.gameObject != player) return;

        PlayerHealth health = collision.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }
    }
}
