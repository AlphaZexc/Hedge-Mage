using UnityEngine;
using System.Collections.Generic;

// Enemy that wanders, pathfinds, and performs straight-line charges
public class StraightChaser : BaseCreature
{
    // Prevents constant stuck checks
    private const float STUCK_CHECK_INTERVAL = 0.2f;
    private const float STUCK_VELOCITY_THRESHOLD = 0.05f;

    // High-level behavior states
    private enum CreatureState
    {
        Wandering,
        Chasing,
        Charging,
        Cooldown,
        Stunned
    }

    [Header("Movement")]
    public float wanderSpeed = 2f;
    public float wanderSmoothing = 0.45f;
    public float chaseSpeed = 4f;
    public float chargeSpeed = 7f;
    public float chargeSightRange = 8f;

    [Header("Pathing")]
    public float waypointProximity = 0.2f;
    public float pathRequestCooldown = 0.1f;

    [Header("Stun")]
    public float stunDuration = 1.0f;
    public float stunMinImpactSpeed = 9f;

    [Header("Cooldown")]
    public float windupDuration = 1.5f;
    public float cooldownAfterHit = 4f;

    [Header("Gizmos")]
    public bool showGizmos = true;
    public Color wanderPathColor = Color.cyan;
    public Color chasePathColor = Color.red;
    public float waypointGizmoRadius = 0.1f;

    private CreatureState state;
    private AStarGridManager grid;
    private Animator animator;
    private PlayerHealth playerHealth;

    private List<Vector3> path;
    private int waypointIndex;
    private Vector3 lastKnownPlayerPos;
    private float lastPathRequestTime;

    private float cooldownEndTime;
    private float windupTime;
    private float stunEndTime;
    private float stuckTimer;

    private Vector2 lastMoveDir;

    private Vector2 chargeDirection;

    protected override void Start()
    {
        base.Start();

        // Get animator (on child Visuals object)
        animator = GetComponentInChildren<Animator>();

        // Find grid manager
        grid = FindFirstObjectByType<AStarGridManager>();

        // Initialize facing direction
        lastMoveDir = Vector2.down;

        // Start wandering
        state = CreatureState.Wandering;
        StartWandering();
    }

    protected override void Update()
    {
        if (state == CreatureState.Stunned)
        {
            if (Time.time > stunEndTime)
                ExitStunState();
            return;
        }

        // If player missing or dead, wander
        if (player == null || (playerHealth != null && playerHealth.isDead))
        {
            EnterWanderState();
            return;
        }

        // End cooldown
        if (state == CreatureState.Cooldown && Time.time >= cooldownEndTime)
        {
            EnterWanderState();
        }

        // Decide behavior
        UpdateState();

        // Update animation
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        // Stop if stunned
        if (state == CreatureState.Stunned)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        
        // Move straight during charge
        if (state == CreatureState.Charging && windupTime > Time.time)
        {
            rb.linearVelocity = chargeDirection * chargeSpeed;
            lastMoveDir = chargeDirection; // Track charge direction for animation
            return;
        } else if (state == CreatureState.Charging && windupTime < Time.time)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Prevent stuck behavior
        HandleStuckDetection();
        // Follow A* path
        FollowPath();
    }

    private void UpdateState()
    {
        float distance = Vector2.Distance(transform.position, player.transform.position);

        switch (state)
        {
            // Look for player
            case CreatureState.Wandering:

                if (distance <= detectionRange)
                {
                    state = CreatureState.Chasing;
                    lastKnownPlayerPos = player.transform.position;
                    RequestPath(lastKnownPlayerPos);
                }
                break;

            // Pathfind toward player
            case CreatureState.Chasing:

                // If player visible, start charge
                if (HasLineOfSightToPlayer())
                {
                    Debug.Log("Charging");
                    EnterChargeState();
                    return;
                }

                lastKnownPlayerPos = player.transform.position;

                // Recalculate path occasionally
                if (Time.time - lastPathRequestTime >= pathRequestCooldown)
                    RequestPath(lastKnownPlayerPos);

                break;
        }
    }

    // Checks if raycast hits player
    private bool HasLineOfSightToPlayer()
    {
        Vector2 origin = transform.position;
        Vector2 dir = (player.transform.position - transform.position).normalized;

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, chargeSightRange);

        if (hit.collider == null)
            return false;

        return hit.collider.CompareTag("Player");
    }

    // Starts straight-line charge
    private void EnterChargeState()
    {
        state = CreatureState.Charging;

        windupTime = Time.time + windupDuration;
        // Lock direction ONCE
        chargeDirection = (player.transform.position - transform.position).normalized;
    }


    // Moves toward current waypoint
    private void FollowPath()
    {
        if (path == null || waypointIndex >= path.Count)
        {
            rb.linearVelocity = Vector2.zero;

            if (state != CreatureState.Chasing)
                StartWandering();

            return;
        }

        Vector2 toWaypoint = path[waypointIndex] - transform.position;
        float distance = toWaypoint.magnitude;
        Vector2 desiredDir = toWaypoint.normalized;

        Vector2 moveDir;
        float speed;

        // Fast movement while chasing
        if (state == CreatureState.Chasing)
        {
            moveDir = desiredDir;
            speed = chaseSpeed;
        }
        // Smooth movement while wandering
        else
        {
            moveDir = Vector2.Lerp(lastMoveDir, desiredDir, wanderSmoothing).normalized;
            speed = wanderSpeed;
        }

        rb.linearVelocity = moveDir * speed;
        lastMoveDir = moveDir;

        // Advance waypoint
        if (distance <= waypointProximity)
            waypointIndex++;
    }

    // Repath if not moving
    private void HandleStuckDetection()
    {
        if (rb.linearVelocity.magnitude > STUCK_VELOCITY_THRESHOLD)
        {
            stuckTimer = 0f;
            return;
        }

        stuckTimer += Time.fixedDeltaTime;

        if (stuckTimer < STUCK_CHECK_INTERVAL)
            return;

        stuckTimer = 0f;
        RequestPath(lastKnownPlayerPos);
    }

    // Ask grid for new path
    private void RequestPath(Vector3 target)
    {
        lastPathRequestTime = Time.time;
        path = grid.FindPath(transform.position, target);
        waypointIndex = 0;
    }

    // Pick random tile
    private void StartWandering()
    {
        Node node = grid.GetRandomWalkableNode();

        if (node != null)
            RequestPath(node.worldPosition);
    }

    // Switch back to wander
    private void EnterWanderState()
    {
        state = CreatureState.Wandering;
        StartWandering();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Wall slam causes stun
        if (state == CreatureState.Charging && collision.gameObject.CompareTag("Wall") && rb.linearVelocity.magnitude >= stunMinImpactSpeed)
        {
            EnterStunState();
            return;
        }

        // Damage player
        if (collision.gameObject.CompareTag("Player"))
        {
            if (playerHealth == null)
                playerHealth = collision.gameObject.GetComponent<PlayerHealth>();

            if (playerHealth != null && !playerHealth.isDead)
            {
                playerHealth.TakeDamage(damage);
                state = CreatureState.Cooldown;
                cooldownEndTime =
                    Time.time + cooldownAfterHit;
            }
        }
    }

    private void EnterStunState()
    {
        state = CreatureState.Stunned;
        stunEndTime = Time.time + stunDuration;

        rb.linearVelocity = Vector2.zero;
        path = null;
    }


    private void ExitStunState()
    {
        state = CreatureState.Wandering;
        StartWandering();
    }


    // Animation
    private void UpdateAnimator()
    {
        if (!animator) return;

        // Use lastMoveDir for direction (so animation doesn't reset to default when stopped)
        // Use actual velocity for speed
        if (lastMoveDir.sqrMagnitude > 0.01f)
        {
            animator.SetFloat("Horizontal", lastMoveDir.x);
            animator.SetFloat("Vertical", lastMoveDir.y);
        }
        
        animator.SetFloat("Speed", rb.linearVelocity.sqrMagnitude);
    }

    // Debug drawing
    private void OnDrawGizmos()
    {
        if (!showGizmos || path == null) return;

        Gizmos.color =
            state == CreatureState.Chasing
            ? chasePathColor
            : wanderPathColor;

        for (int i = 0; i < path.Count; i++)
        {
            Gizmos.DrawSphere(path[i], waypointGizmoRadius);

            if (i + 1 < path.Count)
                Gizmos.DrawLine(path[i], path[i + 1]);
        }
    }
}
