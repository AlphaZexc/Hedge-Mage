using UnityEngine;

public class Fireball : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;

    private Vector2 moveDirection;
    private Animator anim;
    private Rigidbody2D rb;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void Initialize(Vector2 direction)
    {
        moveDirection = direction;
        Destroy(gameObject, lifetime);
    }

    private void FixedUpdate()
    {
        // Ensure velocity stays consistent (optional but safe)
        rb.linearVelocity = moveDirection * speed;
    }

    private void Update()
    {
        anim.SetFloat("VelocityX", rb.linearVelocity.x);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall") || other.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}