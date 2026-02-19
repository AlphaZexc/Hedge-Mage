using UnityEngine;
using System.Collections.Generic;

public class StraightChaser : BaseCreature
{
    private const float STUCK_CHECK_INTERVAL = 0.2f;
    private const float STUCK_VELOCITY_THRESHOLD = 0.05f;

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

    [Header("Charging")]
    public float stunDuration = 1.0f;
    public float stunMinImpactSpeed = 9f;
    public float windupDuration = 1.5f;
    public float cooldownAfterHit = 4f;
    public float maxChargeDuration = 2.5f;

    [Header("Detection")]
    public LayerMask lineOfSightMask;

    [Header("Gizmos")]
    public bool showGizmos = true;
    public Color wanderPathColor = Color.cyan;
    public Color chasePathColor = Color.red;
    public float waypointGizmoRadius = 0.1f;

    private CreatureState state;

    private AStarGridManager grid;
    private Animator animator;
    private PlayerHealth playerHealth => PlayerHealth.Instance;

    private List<Vector3> path;
    private int waypointIndex;
    private Vector3 lastKnownPlayerPos;
    private float lastPathRequestTime;

    private float cooldownEndTime;
    private float stunEndTime;
    private float chargeEndTime;

    private float stuckTimer;

    private Vector2 lastMoveDir;
    private Vector2 chargeDirection;

    // Line of sight visuals
    private bool hasLineOfSight;
    private Vector2 lastLOSOrigin;
    private Vector2 lastLOSDirection;


    protected override void Start()
    {
        base.Start();

        animator = GetComponentInChildren<Animator>();
        grid = FindFirstObjectByType<AStarGridManager>();

        lastMoveDir = Vector2.down;

        ChangeState(CreatureState.Wandering);
    }

    protected override void Update()
    {
        if (player == null || (playerHealth != null && playerHealth.isDead))
        {
            ChangeState(CreatureState.Wandering);
            return;
        }

        HandleTimers();
        UpdateStateLogic();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (state == CreatureState.Stunned)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (state == CreatureState.Charging)
        {
            rb.linearVelocity = chargeDirection * chargeSpeed;
            lastMoveDir = chargeDirection;
            return;
        }

        HandleStuckDetection();
        FollowPath();
    }

    private void ChangeState(CreatureState newState)
    {
        state = newState;

        switch (state)
        {
            case CreatureState.Wandering:
                StartWandering();
                break;

            case CreatureState.Chasing:
                lastKnownPlayerPos = player.transform.position;
                RequestPath(lastKnownPlayerPos);
                break;

            case CreatureState.Charging:
                // Find out which direction player is in
                Vector2 toPlayer = player.transform.position - transform.position;

                if (Mathf.Abs(toPlayer.x) > Mathf.Abs(toPlayer.y))
                    chargeDirection = new Vector2(Mathf.Sign(toPlayer.x), 0f);
                else 
                    chargeDirection = new Vector2(0f, Mathf.Sign(toPlayer.y));

                chargeEndTime = Time.time + maxChargeDuration;
                break;

            case CreatureState.Cooldown:
                cooldownEndTime = Time.time + cooldownAfterHit;
                rb.linearVelocity = Vector2.zero;
                break;

            case CreatureState.Stunned:
                stunEndTime = Time.time + stunDuration;
                rb.linearVelocity = Vector2.zero;
                path = null;
                break;
        }
    }

    private void UpdateStateLogic()
    {
        float distance = Vector2.Distance(transform.position, player.transform.position);

        switch (state)
        {
            case CreatureState.Wandering:
                if (distance <= detectionRange)
                    ChangeState(CreatureState.Chasing);
                break;

            case CreatureState.Chasing:

                if (HasLineOfSightToPlayer())
                {
                    ChangeState(CreatureState.Charging);
                    return;
                }

                lastKnownPlayerPos = player.transform.position;

                if (Time.time - lastPathRequestTime >= pathRequestCooldown)
                    RequestPath(lastKnownPlayerPos);

                break;
        }
    }

    private void HandleTimers()
    {
        if (state == CreatureState.Stunned && Time.time >= stunEndTime)
            ChangeState(CreatureState.Wandering);

        if (state == CreatureState.Cooldown && Time.time >= cooldownEndTime)
            ChangeState(CreatureState.Wandering);

        if (state == CreatureState.Charging && Time.time >= chargeEndTime)
            ChangeState(CreatureState.Cooldown);
    }

    // Charge with line of sight
    private bool HasLineOfSightToPlayer()
    {
        if (player == null)
            return false;

        Vector2 origin = transform.position;
        Vector2 toPlayer = player.transform.position - transform.position;

        // Cardinal direction only
        if (Mathf.Abs(toPlayer.x) > Mathf.Abs(toPlayer.y))
            toPlayer = new Vector2(Mathf.Sign(toPlayer.x), 0f);
        else
            toPlayer = new Vector2(0f, Mathf.Sign(toPlayer.y));

        Vector2 dir = toPlayer.normalized;

        lastLOSOrigin = origin;
        lastLOSDirection = dir;

        RaycastHit2D hit = Physics2D.Raycast(
            origin,
            dir,
            chargeSightRange,
            lineOfSightMask
        );

        if (!hit)
        {
            hasLineOfSight = false;
            return false;
        }

        hasLineOfSight = hit.collider.CompareTag("Player");
        return hasLineOfSight;
    }



    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player"))
            return;

        if (playerHealth == null || playerHealth.isDead)
            return;

        playerHealth.TakeDamage(damage);

        // ALWAYS enter cooldown after hitting player
        ChangeState(CreatureState.Cooldown);
    }


    // Movement
    private void FollowPath()
    {
        if (path == null || waypointIndex >= path.Count)
        {
            rb.linearVelocity = Vector2.zero;

            if (state == CreatureState.Chasing)
                RequestPath(lastKnownPlayerPos);
            else if (state == CreatureState.Wandering)
                StartWandering();

            return;
        }

        Vector2 toWaypoint = path[waypointIndex] - transform.position;
        float distance = toWaypoint.magnitude;
        Vector2 desiredDir = toWaypoint.normalized;

        Vector2 moveDir;
        float speed;

        if (state == CreatureState.Chasing)
        {
            moveDir = desiredDir;
            speed = chaseSpeed;
        }
        else
        {
            moveDir = Vector2.Lerp(lastMoveDir, desiredDir, wanderSmoothing).normalized;
            speed = wanderSpeed;
        }

        rb.linearVelocity = moveDir * speed;
        lastMoveDir = moveDir;

        if (distance <= waypointProximity)
            waypointIndex++;
    }

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

        Node currentNode = grid.NodeFromWorldPoint(transform.position);

        if (currentNode != null)
        {
            // Temporarily block this tile so A* avoids it
            grid.SetNodeTemporarilyUnwalkable(currentNode.worldPosition, 0.5f);
        }

        // Small physical nudge toward target to escape corners
        if (state == CreatureState.Chasing && path != null && waypointIndex < path.Count)
        {
            Vector2 escapeDir = (path[waypointIndex] - transform.position).normalized;
            rb.position += escapeDir * 0.1f; // small positional correction
        }

        // Force fresh path
        if (state == CreatureState.Chasing)
            RequestPath(lastKnownPlayerPos);
        else if (state == CreatureState.Wandering)
            StartWandering();
    }


    private void RequestPath(Vector3 target)
    {
        lastPathRequestTime = Time.time;
        path = grid.FindPath(transform.position, target);
        waypointIndex = 0;
    }

    private void StartWandering()
    {
        Node node = grid.GetRandomWalkableNode();
        if (node != null)
            RequestPath(node.worldPosition);
    }

    // Animation
    private void UpdateAnimator()
    {
        if (!animator) return;

        if (lastMoveDir.sqrMagnitude > 0.01f)
        {
            animator.SetFloat("Horizontal", lastMoveDir.x);
            animator.SetFloat("Vertical", lastMoveDir.y);
        }

        animator.SetFloat("Speed", rb.linearVelocity.sqrMagnitude);
    }

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

        // Draw line of sight ray
        if (Application.isPlaying && lastLOSDirection != Vector2.zero)
        {
            Gizmos.color = hasLineOfSight ? Color.green : Color.red;
            Gizmos.DrawLine(
                lastLOSOrigin,
                lastLOSOrigin + lastLOSDirection * chargeSightRange
            );
        }

    }
}
