using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public class Mirelight : MonoBehaviour
{
    public static List<Mirelight> AllMirelights = new List<Mirelight>();

    private enum State { Idle, Flickering, Armed, Attacking }
    private State currentState = State.Idle;

    [Header("Attack Settings")]
    public float attackRange = 4f;
    public float pounceSpeed = 10f;
    public int pounceDamage = 25;
    public float armingTime = 3f;

    [Header("References")]
    public Transform lightConeTransform;
    public Transform attackPoint;

    [Header("Flicker Settings")]
    [SerializeField] private float flickerIntensityMin = 0.2f;
    [SerializeField] private float flickerIntensityMax = 1.4f;
    [SerializeField] private float flickerSpeedMin = 15f;
    [SerializeField] private float flickerSpeedMax = 35f;

    private float flickerSpeed;
    private float flickerSeed;
    private bool hasDealtDamage;

    private Animator animator;
    private Light2D lampLight;
    private Transform player;
    private PlayerHealth playerHealth;

    private Vector3 originalPosition;
    private Quaternion originalLightRotation;

    public bool IsIdle => currentState == State.Idle;

    #region UNITY

    private void Awake()
    {
        AllMirelights.Add(this);

        animator = GetComponentInChildren<Animator>();

        if (lightConeTransform)
            lampLight = lightConeTransform.GetComponent<Light2D>();

        flickerSeed = Random.Range(0f, 1000f);
        flickerSpeed = Random.Range(flickerSpeedMin, flickerSpeedMax);

        originalPosition = transform.position;

        if (lightConeTransform)
            originalLightRotation = lightConeTransform.rotation;
    }

    private void OnDestroy()
    {
        AllMirelights.Remove(this);
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p)
        {
            player = p.transform;
            playerHealth = p.GetComponent<PlayerHealth>();
        }

        SetIdleImmediate();
    }

    private void Update()
    {
        if (currentState == State.Flickering)
            UpdateFlicker();

        if (currentState == State.Armed)
            AimAtPlayer();
    }

    #endregion

    #region FLICKER LOGIC

    public void StartFlicker()
    {
        if (currentState != State.Idle)
            return;

        currentState = State.Flickering;

        if (lampLight)
            lampLight.enabled = true;

        if (animator)
        {
            animator.SetTrigger("Flicker");
            animator.SetBool("IsFlickering", true);
        }
    }

    public void ResolvePostFlicker()
    {
        if (currentState != State.Flickering)
            return;

        if (animator)
            animator.SetBool("IsFlickering", false);

        if (PlayerInRange())
            Arm();
        else
            GoIdle();
    }

    private void UpdateFlicker()
    {
        if (!lampLight) return;

        float noise = Mathf.PerlinNoise(
            flickerSeed,
            Time.time * flickerSpeed
        );

        float intensity = Mathf.Lerp(
            flickerIntensityMin,
            flickerIntensityMax,
            noise
        );

        lampLight.intensity = intensity;
    }

    #endregion

    #region STATE TRANSITIONS

    private void Arm()
    {
        if (!PlayerInRange())
        {
            GoIdle();
            return;
        }

        currentState = State.Armed;

        if (lampLight)
        {
            lampLight.enabled = true;
            lampLight.intensity = 1f;
        }

        // Wait before transforming
        StartCoroutine(ArmingDelay());
    }

    private IEnumerator ArmingDelay()
    {
        yield return new WaitForSeconds(armingTime); // arming duration

        StartCoroutine(AttackSequence());
    }

    private void GoIdle()
    {
        currentState = State.Idle;

        if (lampLight)
        {
            lampLight.enabled = true; 
            lampLight.intensity = 1f;
        }

        animator.ResetTrigger("Transform");
        animator.ResetTrigger("Pounce");
        animator.ResetTrigger("Reset"); 

        transform.position = originalPosition;
        hasDealtDamage = false;

        if (lightConeTransform)
            lightConeTransform.rotation = originalLightRotation;
    }

    private void SetIdleImmediate()
    {
        currentState = State.Idle;

        if (lampLight)
        {
            lampLight.intensity = 1f;
            lampLight.enabled = true;
        }
    }

    #endregion

    #region ATTACK

    private IEnumerator AttackSequence()
    {
        currentState = State.Attacking;

        animator.SetTrigger("Transform");

        yield return new WaitForSeconds(1.5f); // length of transform anim

        animator.SetTrigger("Pounce");

        yield return new WaitForSeconds(0.3f);

        yield return StartCoroutine(Pounce());

        yield return new WaitForSeconds(1f);

        GoIdle();
    }

    private IEnumerator Pounce()
    {
        float maxChaseTime = 1.5f; // prevents infinite chase
        float timer = 0f;

        // FORWARD POUNCE
        while (timer < maxChaseTime && hasDealtDamage == false)
        {
            if (!player) break;

            timer += Time.deltaTime;

            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * pounceSpeed * Time.deltaTime;

            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        animator.SetTrigger("Reset");

        // RETURN TO ORIGINAL POSITION
        while (Vector3.Distance(transform.position, originalPosition) > 0.05f)
        {
            Vector3 direction = (originalPosition - transform.position).normalized;
            transform.position += direction * pounceSpeed * Time.deltaTime;

            yield return null;
        }

        transform.position = originalPosition;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentState != State.Attacking) return;
        if (hasDealtDamage) return;

        if (collision.collider.CompareTag("Player"))
        {
            PlayerHealth player = collision.gameObject.GetComponent<PlayerHealth>();
            if (player != null)
            {
                player.TakeDamage(pounceDamage);
                hasDealtDamage = true;
                Debug.Log("Mirelight dealt " + pounceDamage + " damage.");
            }
            else Debug.LogWarning("PlayerHealth not found!");
        }
    }

    private void AimAtPlayer()
    {
        if (!lightConeTransform || !player) return;

        Vector3 dir = (player.position - lightConeTransform.position).normalized;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;

        Quaternion targetRot = Quaternion.Euler(0, 0, targetAngle);

        lightConeTransform.rotation = Quaternion.Lerp(
            lightConeTransform.rotation,
            targetRot,
            10f * Time.deltaTime
        );
    }

    private bool PlayerInRange()
    {
        if (!player) return false;
        return Vector2.Distance(transform.position, player.position) <= attackRange;
    }

    #endregion

    #region GIZMOS

    private void OnDrawGizmos()
    {
        switch (currentState)
        {
            case State.Idle:
                Gizmos.color = Color.gray;
                break;
            case State.Flickering:
                Gizmos.color = Color.yellow;
                break;
            case State.Armed:
                Gizmos.color = Color.red;
                break;
            case State.Attacking:
                Gizmos.color = Color.magenta;
                break;
        }

        Gizmos.DrawWireSphere(transform.position, attackRange);

#if UNITY_EDITOR
        if ((currentState == State.Armed || currentState == State.Attacking) && player != null)
            Gizmos.DrawLine(transform.position, player.position);
#endif
    }

    #endregion
}