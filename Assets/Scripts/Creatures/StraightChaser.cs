using UnityEngine;
using System.Collections.Generic;

public class StraightChaser : BaseCreature
{
    private const float STUCK_CHECK_INTERVAL = 0.35f;
    private const float STUCK_VELOCITY_THRESHOLD = 0.12f;
    private const float CHARGE_WALL_RAY_DISTANCE = 0.55f;
    private const int NULL_PATH_FALLBACK_RETRIES = 3;

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
    public float pathRequestCooldown = 0.4f;
    [SerializeField] private float basePlayerProximity = 30f;
    [SerializeField] private float letterProximityReductionValue = 3f; // Larger value means more aggressive scaling as player collects letters

    [Header("Charging")]
    public float stunDuration = 1.0f;
    public float stunMinImpactSpeed = 5f;
    public float windupDuration = 1.5f;
    public float cooldownAfterHit = 4f;
    public float maxChargeDuration = 2.5f;
    [SerializeField] private LayerMask chargeObstacleMask;

    [Header("Detection")]
    public LayerMask lineOfSightMask;

    private float lastDamageTime;
    [Header("Contact Damage")]
    [SerializeField] private float contactDamageCooldown = 1f;

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
    private Vector2Int lastPlayerGridCell;
    private bool hasPlayerGridCell;
    private int nullPathRetries;
    private Vector2 lastContactNormal;

    private float cooldownEndTime;
    private float stunEndTime;
    private float chargeEndTime;

    private float stuckTimer;
    private float minotaurPlayerProximity;

    private Vector2 lastMoveDir;
    private Vector2 chargeDirection;

    private bool hasLineOfSight;
    private Vector2 lastLOSOrigin;
    private Vector2 lastLOSDirection;


    protected override void Start()
    {
        base.Start();

        animator = GetComponentInChildren<Animator>();
        grid = FindFirstObjectByType<AStarGridManager>();

        if (chargeObstacleMask.value == 0)
            chargeObstacleMask = LayerMask.GetMask("Obstacles");

        lastMoveDir = Vector2.down;
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Interactable"));
        Physics2D.IgnoreLayerCollision(gameObject.layer, LayerMask.NameToLayer("Creature"));

        ChangeState(CreatureState.Wandering);
    }

    protected override void Update()
    {
        if (player == null || (playerHealth != null && playerHealth.isDead))
        {
            ChangeState(CreatureState.Wandering);
            return;
        }

        // Minotaur gets more aggressive as player collects letters
        minotaurPlayerProximity = Mathf.Max(5,
            basePlayerProximity - (PlayerInventory.Instance.collectedLetters.Count / letterProximityReductionValue) * WordProgressManager.Instance.targetWord.Length);

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
            if (CheckChargeWallAhead())
                return;

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
                nullPathRetries = 0;
                StartWandering();
                break;

            case CreatureState.Chasing:
                lastMoveDir = Vector2.zero;
                lastKnownPlayerPos = player.transform.position;
                nullPathRetries = 0;
                hasPlayerGridCell = false;
                RequestPath(lastKnownPlayerPos);
                break;

            case CreatureState.Charging:
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

                if (ShouldRepath())
                    RequestPath(lastKnownPlayerPos);

                break;
        }
    }

    private bool ShouldRepath()
    {
        if (Time.time - lastPathRequestTime >= pathRequestCooldown)
            return true;

        if (grid == null || player == null)
            return false;

        Node playerNode = grid.NodeFromWorldPoint(player.transform.position);
        if (playerNode == null)
            return false;

        Vector2Int cell = new Vector2Int(playerNode.gridX, playerNode.gridY);
        if (!hasPlayerGridCell || cell != lastPlayerGridCell)
        {
            hasPlayerGridCell = true;
            lastPlayerGridCell = cell;
            return true;
        }

        return false;
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

    private bool HasLineOfSightToPlayer()
    {
        if (player == null)
            return false;

        Vector2 origin = transform.position;
        Vector2 toPlayer = player.transform.position - transform.position;

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

    private bool CheckChargeWallAhead()
    {
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            chargeDirection,
            CHARGE_WALL_RAY_DISTANCE,
            chargeObstacleMask
        );

        if (!hit || hit.collider == null)
            return false;

        if (hit.collider.CompareTag("Player"))
            return false;

        lastContactNormal = hit.normal;
        EndChargeFromWallImpact(chargeSpeed);
        return true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (playerHealth == null || playerHealth.isDead) return;

            TryDealContactDamage();

            if (state == CreatureState.Charging)
                ChangeState(CreatureState.Cooldown);
            return;
        }

        if (state != CreatureState.Charging)
        {
            if (collision.contactCount > 0)
                lastContactNormal = collision.GetContact(0).normal;
            return;
        }

        // Non-player collision while charging: stun or soft cooldown
        if (collision.contactCount > 0)
            lastContactNormal = collision.GetContact(0).normal;

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < 0.01f)
            impactSpeed = rb.linearVelocity.magnitude;

        EndChargeFromWallImpact(impactSpeed);
    }

    private void EndChargeFromWallImpact(float impactSpeed)
    {
        if (impactSpeed >= stunMinImpactSpeed)
            ChangeState(CreatureState.Stunned);
        else
            ChangeState(CreatureState.Cooldown);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.contactCount > 0)
            lastContactNormal = collision.GetContact(0).normal;

        if (!collision.gameObject.CompareTag("Player")) return;
        if (playerHealth == null || playerHealth.isDead) return;

        TryDealContactDamage();
    }

    private void TryDealContactDamage()
    {
        if (Time.time - lastDamageTime < contactDamageCooldown) return;

        lastDamageTime = Time.time;
        playerHealth.TakeDamage(damage);
    }

    private void FollowPath()
    {
        if (state != CreatureState.Chasing && state != CreatureState.Wandering)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (path == null || waypointIndex >= path.Count)
        {
            rb.linearVelocity = Vector2.zero;

            if (state == CreatureState.Chasing)
                HandleNullPathWhileChasing();
            else if (state == CreatureState.Wandering)
                StartWandering();

            return;
        }

        nullPathRetries = 0;

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

    private void HandleNullPathWhileChasing()
    {
        nullPathRetries++;

        if (nullPathRetries >= NULL_PATH_FALLBACK_RETRIES)
        {
            nullPathRetries = 0;
            ChangeState(CreatureState.Wandering);
            return;
        }

        // Try an alternate nearby target before giving up
        if (grid != null)
        {
            Node nearPlayer = grid.NodeFromWorldPoint(lastKnownPlayerPos);
            if (nearPlayer != null)
            {
                Node alt = grid.FindClosestWalkableNodePublic(nearPlayer);
                if (alt != null)
                {
                    RequestPath(alt.worldPosition);
                    if (path != null && path.Count > 0)
                        return;
                }
            }
        }

        RequestPath(lastKnownPlayerPos);
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

        Vector2 escapeDir = Vector2.zero;

        if (path != null && waypointIndex < path.Count)
        {
            Vector2 toBlocked = (path[waypointIndex] - transform.position);
            if (toBlocked.sqrMagnitude > 0.0001f)
                escapeDir = -toBlocked.normalized;
        }

        if (escapeDir.sqrMagnitude < 0.01f && lastContactNormal.sqrMagnitude > 0.01f)
            escapeDir = lastContactNormal.normalized;

        if (escapeDir.sqrMagnitude < 0.01f && lastMoveDir.sqrMagnitude > 0.01f)
            escapeDir = -lastMoveDir.normalized;

        if (escapeDir.sqrMagnitude > 0.01f)
            rb.position += escapeDir * 0.15f;

        // Skip the blocked waypoint so we don't immediately drive back into it
        if (path != null && waypointIndex < path.Count - 1)
            waypointIndex++;
        else if (path != null && waypointIndex >= path.Count - 1)
            path = null;

        // Optionally mark the *blocked neighbor* (toward the old waypoint), never the occupied cell
        if (grid != null && escapeDir.sqrMagnitude > 0.01f)
        {
            Vector3 blockedProbe = transform.position - (Vector3)(escapeDir * 0.6f);
            Node blockedNeighbor = grid.NodeFromWorldPoint(blockedProbe);
            Node currentNode = grid.NodeFromWorldPoint(transform.position);
            if (blockedNeighbor != null && currentNode != null && blockedNeighbor != currentNode)
                grid.SetNodeTemporarilyUnwalkable(blockedNeighbor.worldPosition, 0.5f);
        }

        if (state == CreatureState.Chasing)
            RequestPath(lastKnownPlayerPos);
        else if (state == CreatureState.Wandering)
            StartWandering();
    }

    private void RequestPath(Vector3 target)
    {
        lastPathRequestTime = Time.time;

        if (grid == null)
        {
            path = null;
            waypointIndex = 0;
            return;
        }

        Vector3 reference = transform.position;
        bool hadPrevious = path != null && path.Count > 0;
        if (hadPrevious)
        {
            int refIndex = Mathf.Clamp(waypointIndex, 0, path.Count - 1);
            reference = path[refIndex];
        }

        List<Vector3> newPath = grid.FindPath(transform.position, target);
        path = newPath;

        if (path == null || path.Count == 0)
        {
            waypointIndex = 0;
            return;
        }

        // Preserve progress: steer toward closest waypoint on the new path
        int closest = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < path.Count; i++)
        {
            float d = (path[i] - reference).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                closest = i;
            }
        }

        waypointIndex = closest;

        // If we're already on/near that waypoint, advance so we keep moving
        if ((path[waypointIndex] - transform.position).sqrMagnitude <= waypointProximity * waypointProximity
            && waypointIndex < path.Count - 1)
        {
            waypointIndex++;
        }
    }

    private void StartWandering()
    {
        Node node = null;
        int maxAttempts = 50;

        for (int i = 0; i < maxAttempts; i++)
        {
            Node candidate = grid.GetRandomWalkableNode();
            if (candidate == null) continue;
            if (Vector2.Distance(candidate.worldPosition, player.transform.position) <= minotaurPlayerProximity)
            {
                node = candidate;
                break;
            }
        }

        // Fall back to any walkable node if nothing found near the player
        if (node == null)
            node = grid.GetRandomWalkableNode();

        if (node != null)
            RequestPath(node.worldPosition);
    }

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

        Gizmos.color = state == CreatureState.Chasing ? chasePathColor : wanderPathColor;

        for (int i = 0; i < path.Count; i++)
        {
            Gizmos.DrawSphere(path[i], waypointGizmoRadius);

            if (i + 1 < path.Count)
                Gizmos.DrawLine(path[i], path[i + 1]);
        }

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
