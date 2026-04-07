using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Fireball : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private float hitAnimationDuration = 0.4f;

    private Vector2 moveDirection;
    private Animator anim;
    private Rigidbody2D rb;
    private Light2D fireLight;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        fireLight = GetComponentInChildren<Light2D>();
    }

    public void Initialize(Vector2 direction)
    {
        moveDirection = direction.normalized;
        SetDirectionAnimation(moveDirection);
        Destroy(gameObject, lifetime);
    }

    private void SetDirectionAnimation(Vector2 dir)
    {
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            anim.SetInteger("Direction", dir.x >= 0 ? 0 : 1);
        else
            anim.SetInteger("Direction", dir.y >= 0 ? 2 : 3);
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveDirection * speed;
    }

    private void Update()
    {
        if (fireLight != null)
            fireLight.intensity = 1f + Mathf.Sin(Time.time * 20f) * 0.15f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall") || other.CompareTag("Enemy"))
            StartCoroutine(PlayHitAndDestroy());
    }

    private IEnumerator PlayHitAndDestroy()
    {
        rb.linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;

        if (fireLight != null)
            fireLight.intensity = 0.3f;

        anim.Play("Hit");
        yield return new WaitForSeconds(hitAnimationDuration);

        Destroy(gameObject);
    }
}