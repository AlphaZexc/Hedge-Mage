using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public class Mirelight : MonoBehaviour
{
    [Header("Flicker State Control")]
    [Tooltip("How long to stay in Flickering state before transitioning (seconds)")]
    public float flickerDuration = 5f;
    private float flickerTimerState = 0f;

    [Header("State Timers")]
    [Tooltip("How long to stay in Enlighten state before Flickering (seconds)")]
    public float enlightenDuration = 30f;
    [Tooltip("How long to pause in Idle, Armed, or Attacking before returning to Enlighten (seconds)")]
    public float postStatePause = 5f;

    private float enlightenTimer = 0f;
    private float postStateTimer = 0f;

    [Header("Attack Timing")]
    [Tooltip("How long the light aims at the player before pouncing (seconds)")]
    public float aimTime = 0.5f;
    // Flicker horror variables
    private float flickerSeed;
    private float flickerSpeed;
    private float blackoutTimer = 0f;
    private float blackoutDuration = 0f;
    public static List<Mirelight> AllMirelights = new List<Mirelight>();

    private enum State { Enlighten, Idle, Flickering, Armed, Attacking }
    private State currentState = State.Enlighten;

    [Header("Mirelight Settings")]
    public float attackRange = 4f;
    public float pounceSpeed = 20f;
    public int pounceDamage = 25;

    [Header("Component Links")]
    [Tooltip("Drag the child object that holds the Light 2D component here.")]
    public Transform lightConeTransform;
    [Tooltip("Optional: Set to the base of the Mirelight for accurate attack checks.")]
    public Transform attackPoint;

    private Animator animator;
    private Light2D lampLight; 
    private PlayerHealth playerHealth;
    private Transform playerTransform;
    private Vector3 originalPosition;
    private Quaternion originalLightRotation;

    // Flicker parameters (private, per-instance)
    private float flickerPhase;
    private float flickerIntensityMin;
    private float flickerIntensityMax;
    private float blackoutChance;
    private float flickerTimeOffset;
    private float flickerFrequency;
    private float flickerTimer;
    private float flickerPauseTimer;
    private float flickerBurstTimer;
    private float flickerPausedValue;

    [Header("Flicker Randomization Ranges")]
    [Range(0.5f, 5f)] public float flickerSpeedMin = 0.5f;
    [Range(0.5f, 5f)] public float flickerSpeedMax = 5.0f;
    [Range(0.05f, 0.5f)] public float flickerIntensityMinMin = 0.05f;
    [Range(0.05f, 0.5f)] public float flickerIntensityMinMax = 0.5f;
    [Range(0.7f, 2.0f)] public float flickerIntensityMaxMin = 0.7f;
    [Range(1.0f, 2.0f)] public float flickerIntensityMaxMax = 2.0f;
    [Range(0.001f, 0.05f)] public float blackoutChanceMin = 0.001f;
    [Range(0.001f, 0.05f)] public float blackoutChanceMax = 0.05f;
    [Range(0.5f, 3.0f)] public float flickerFrequencyMin = 0.5f;
    [Range(0.5f, 3.0f)] public float flickerFrequencyMax = 3.0f;

    [Header("Mirelight Behavior Control")]
    [Tooltip("How long to stay in Armed state before returning to Idle if no attack occurs (seconds)")]
    public float armedTimeout = 60f;
    [Tooltip("Max Mirelights that can attack per cycle (future use)")]
    public int maxAttackersPerCycle = 1;
    [Tooltip("Cooldown before a new attack cycle can start (future use)")]
    public float cycleCooldown = 10f;

    private float armedTimer = 0f;
    private static bool isAnyMirelightAttacking = false;

    public bool IsIdle => currentState == State.Idle;

    private void Awake()
    {
        // Randomize flicker for each Mirelight using Inspector ranges
        flickerSeed = Random.Range(0f, 1000f);
        flickerSpeed = Random.Range(flickerSpeedMin, flickerSpeedMax);
        flickerPhase = Random.Range(0f, Mathf.PI * 2f);
        flickerIntensityMin = Random.Range(flickerIntensityMinMin, flickerIntensityMinMax);
        flickerIntensityMax = Random.Range(flickerIntensityMaxMin, flickerIntensityMaxMax);
        blackoutChance = Random.Range(blackoutChanceMin, blackoutChanceMax);
        flickerTimeOffset = Random.Range(0f, 1000f);
        flickerFrequency = Random.Range(flickerFrequencyMin, flickerFrequencyMax);
        animator = GetComponent<Animator>();


        if (lightConeTransform != null)
        {
            lampLight = lightConeTransform.GetComponent<Light2D>(); 
        }
        originalPosition = transform.position;
        if (lightConeTransform != null)
        {
            originalLightRotation = lightConeTransform.rotation;
        }
        flickerTimer = Random.Range(0f, 1000f); // start at a random offset
        flickerPauseTimer = 0f;
        flickerBurstTimer = 0f;
        flickerPausedValue = 0f;
    }

    private void OnEnable() { AllMirelights.Add(this); }
    private void OnDisable() { AllMirelights.Remove(this); }

    private void Start()
    {
        flickerTimerState = 0f;
        enlightenTimer = 0f;
        postStateTimer = 0f;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
        // All lights ON at game start (Enlighten state)
        if (lampLight) {
            lampLight.enabled = true;
            lampLight.intensity = 1f;
        }
    }

    private void Update()
    {
        if (currentState == State.Enlighten)
        {
            // Enlighten: lights ON, no flicker
            if (lampLight && !lampLight.enabled) lampLight.enabled = true;
            if (lampLight) lampLight.intensity = 1f;
            enlightenTimer += Time.deltaTime;
            if (enlightenTimer >= enlightenDuration)
            {
                enlightenTimer = 0f;
                StartFlickering();
            }
        }
        else if (currentState == State.Flickering)
        {
            flickerTimer += Time.deltaTime * flickerSpeed * flickerFrequency;
            if (lampLight && !lampLight.enabled) lampLight.enabled = true;
            HandleFlicker();
            flickerTimerState += Time.deltaTime;
            if (flickerTimerState >= flickerDuration)
            {
                flickerTimerState = 0f;
                // Check for player in range to decide next state
                if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) < attackRange)
                {
                    // Go to Armed if player in range
                    currentState = State.Armed;
                    armedTimer = 0f;
                    if (lampLight && lampLight.enabled) lampLight.enabled = false;
                    if (animator) animator.SetBool("IsFlickering", false);
                    if (animator) animator.SetBool("IsArmed", true);
                }
                else
                {
                    // Otherwise go to Idle
                    StopFlickering();
                }
            }
        }
        else if (currentState == State.Armed)
        {
            // Lamp is OFF in Armed except when aiming (handled in AttackSequence)
            armedTimer += Time.deltaTime;
            if (lampLight && lampLight.enabled) lampLight.enabled = false;
            // Only allow one attacker at a time
            if (!isAnyMirelightAttacking)
            {
                CheckForPlayerInRange();
            }
            if (armedTimer >= armedTimeout)
            {
                currentState = State.Idle;
                if (lampLight && lampLight.enabled) lampLight.enabled = false;
                if (animator) animator.SetBool("IsArmed", false);
                postStateTimer = 0f;
            }
            else
            {
                postStateTimer += Time.deltaTime;
                if (postStateTimer >= postStatePause)
                {
                    postStateTimer = 0f;
                    SetEnlighten();
                }
            }
        }
        else if (currentState == State.Idle)
        {
            // Idle: lamp always OFF, no flicker
            if (lampLight && lampLight.enabled) lampLight.enabled = false;
            postStateTimer += Time.deltaTime;
            if (postStateTimer >= postStatePause)
            {
                postStateTimer = 0f;
                SetEnlighten();
            }
        }
    }

    // Horror flicker logic
    private void HandleFlicker()
    {
        if (lampLight == null) return;

        // Random blackout: occasionally go completely dark for a split second
        if (blackoutTimer > 0f)
        {
            blackoutTimer -= Time.deltaTime;
            lampLight.intensity = 0f;
            return;
        }
        else if (Random.value < blackoutChance * Time.deltaTime * 60f)
        {
            blackoutDuration = Random.Range(0.05f, 0.18f);
            blackoutTimer = blackoutDuration;
            lampLight.intensity = 0f;
            return;
        }

        // Random burst event: spike to max intensity
        if (flickerBurstTimer > 0f)
        {
            flickerBurstTimer -= Time.deltaTime;
            lampLight.intensity = flickerIntensityMax;
            return;
        }
        else if (Random.value < 0.01f * Time.deltaTime * 60f) // 1% chance per frame
        {
            flickerBurstTimer = Random.Range(0.05f, 0.15f);
            lampLight.intensity = flickerIntensityMax;
            return;
        }

        // Random pause event: freeze intensity
        if (flickerPauseTimer > 0f)
        {
            flickerPauseTimer -= Time.deltaTime;
            lampLight.intensity = flickerPausedValue;
            return;
        }
        else if (Random.value < 0.02f * Time.deltaTime * 60f) // 2% chance per frame
        {
            flickerPauseTimer = Random.Range(0.08f, 0.25f);
            flickerPausedValue = Random.Range(flickerIntensityMin, flickerIntensityMax);
            lampLight.intensity = flickerPausedValue;
            return;
        }

        // Fully random strobe flicker
        float intensity = Random.Range(flickerIntensityMin, flickerIntensityMax);
        lampLight.intensity = intensity;
    }

    private void CheckForPlayerInRange()
    {
        if (playerTransform == null || currentState == State.Attacking) return;
        if (Vector2.Distance(transform.position, playerTransform.position) < attackRange && !isAnyMirelightAttacking)
        {
            StartCoroutine(AttackSequence());
        }
    }

    private IEnumerator AttackSequence()
        // When Attacking ends, after the normal reset, start postStateTimer for Enlighten
    {
        // SAFETY: If in Flickering, stop it before attacking
        if (currentState == State.Flickering)
        {
            StopFlickering();
        }
        currentState = State.Attacking;
        isAnyMirelightAttacking = true;
        if (animator) animator.SetTrigger("Transform");

        // 1. Enable lampLight, aim/follow player for aimTime seconds (no flicker, always intensity 1)
        if (lampLight) {
            lampLight.enabled = true;
            lampLight.intensity = 1f;
            float aimElapsed = 0f;
            while (aimElapsed < aimTime)
            {
                aimElapsed += Time.deltaTime;
                // Force light to full intensity, no flicker
                lampLight.intensity = 1f;
                if (lightConeTransform && playerTransform)
                {
                    Vector3 dir = (playerTransform.position - lightConeTransform.position).normalized;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    // For z=180 default (down), subtract 90 so cone points toward player
                    angle -= 90f;
                    Quaternion targetRot = Quaternion.Euler(0, 0, angle);
                    lightConeTransform.rotation = Quaternion.Slerp(lightConeTransform.rotation, targetRot, 0.5f);
                }
                yield return null;
            }
        } else {
            float aimElapsed = 0f;
            while (aimElapsed < aimTime) {
                aimElapsed += Time.deltaTime;
                yield return null;
            }
        }
        // 2. Disable lampLight for the lunge/attack
        if (lampLight) lampLight.enabled = false;

        Vector3 startPosition = transform.position;
        Vector3 retreatPosition = startPosition;

        // Track and home in on the player during the lunge, but stop just short
        float stopDistance = 0.5f; // Distance to stop before reaching the player
        Vector3 playerPos = playerTransform.position;
        Vector3 dirToPlayer = (playerPos - startPosition).normalized;
        Vector3 lungeTarget = playerPos - dirToPlayer * stopDistance;
        float lungeDistance = Vector3.Distance(startPosition, lungeTarget);
        float travelTime = lungeDistance / pounceSpeed;
        float elapsedTime = 0f;
        while (elapsedTime < travelTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / travelTime;
            t = t * t * (3f - 2f * t); // Smooth step
            transform.position = Vector3.Lerp(startPosition, lungeTarget, t);
            yield return null;
        }
        transform.position = lungeTarget;

        // Use tag-based check for robust player damage
        Vector2 attackOrigin = attackPoint ? (Vector2)attackPoint.position : (Vector2)transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackOrigin, attackRange);
        foreach (var c in hits)
        {
            if (c.CompareTag("Player"))
            {
                PlayerHealth ph = c.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(this.pounceDamage);
                }
                else
                {
                }
                break; // Only damage the first player found
            }
        }

        yield return new WaitForSeconds(0.3f); // Brief pause after attack

        // Retreat to original position
        float retreatTime = Vector3.Distance(transform.position, retreatPosition) / (pounceSpeed * 0.7f);
        elapsedTime = 0f;
        while (elapsedTime < retreatTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / retreatTime;
            t = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(playerTransform.position, retreatPosition, t);
            yield return null;
        }
        transform.position = retreatPosition;

        // Reset Mirelight state
        yield return new WaitForSeconds(0.2f);
        currentState = State.Idle;
        armedTimer = 0f;
        isAnyMirelightAttacking = false;
        if (lampLight) lampLight.enabled = true;
        if (animator) animator.SetBool("IsArmed", false);
    }

    public void StartFlickering()
    {
        if (currentState == State.Idle || currentState == State.Enlighten)
        {
            currentState = State.Flickering;
            flickerTimerState = 0f;
            if (lampLight) lampLight.enabled = true;
            if (animator) animator.SetBool("IsFlickering", true);
        }
    }

    public void StopFlickering()
    {
        if (currentState == State.Flickering)
        {
            // Before changing to any other state, turn off the light
            if (lampLight && lampLight.enabled) lampLight.enabled = false;
            currentState = State.Idle;
            flickerTimerState = 0f;
            if (animator) animator.SetBool("IsFlickering", false);
            postStateTimer = 0f;
        }
    }

    // Helper to set Enlighten state
    private void SetEnlighten()
    {
        currentState = State.Enlighten;
        enlightenTimer = 0f;
        if (lampLight) {
            lampLight.enabled = true;
            lampLight.intensity = 1f;
        }
    }

    public void Arm()
    {
        // SAFETY: If this Mirelight is flickering, stop it before arming
        if (currentState == State.Flickering)
        {
            StopFlickering();
        }
        if (lampLight) {
            lampLight.intensity = 1f; // Safety: ensure no flicker value remains
            lampLight.enabled = false;
        }
        // Set all other Mirelights to Idle and stop their flickering
        foreach (var mire in AllMirelights)
        {
            if (mire != this)
            {
                if (mire.currentState == State.Flickering)
                {
                    mire.StopFlickering();
                }
                if (mire.currentState != State.Idle)
                {
                    mire.currentState = State.Idle;
                    if (mire.lampLight && mire.lampLight.enabled) mire.lampLight.enabled = false;
                    if (mire.animator) mire.animator.SetBool("IsFlickering", false);
                    if (mire.animator) mire.animator.SetBool("IsArmed", false);
                }
            }
        }
        currentState = State.Armed;
        armedTimer = 0f;
        if (lampLight && lampLight.enabled) lampLight.enabled = false;
        if (animator) animator.SetBool("IsFlickering", false);
        if (animator) animator.SetBool("IsArmed", true);
    }

    // --- GIZMOS ADDED BACK FOR DEBUGGING ---
    private void OnDrawGizmos()
    {
        // Set the Gizmo color based on the current state to help debug
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
        // Draw a wire sphere to show the attack range, colored by its current state
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}