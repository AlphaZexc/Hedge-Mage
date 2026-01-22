
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StraightChaser : BaseCreature
{
    // Stuck detection
    private float stuckTimer = 0f;
    private Vector3 lastStuckCheckPosition;
    private const float stuckCheckInterval = 0.2f;
    private const float stuckVelocityThreshold = 0.05f;
    // Preloaded decor positions for avoidance
    private List<Vector3> decorPositions = new List<Vector3>();
    // Track last known player position for persistent chase
    private Vector3 lastKnownPlayerPosition;
    [Header("Movement Tuning")]
    [Tooltip("Smoothing factor for wandering (higher = more curve)")]
    public float wanderSmoothing = 0.45f;
    [Tooltip("Smoothing factor for chasing (lower = more direct; 0 = no smoothing)")]
    public float chaseSmoothing = 0.0f;
    [Tooltip("Dot product threshold for straight move tracking (lower = less sensitive)")]
    public float straightMoveDotThreshold = 0.90f;
    private enum CreatureState { Wandering, Chasing, Cooldown }
    private CreatureState currentState;

    private AStarGridManager gridManager;
    private List<Vector3> path;
    private int currentWaypointIndex;
    private float pathRequestCooldown = 0.5f;
    private float lastPathRequestTime;

    [Header("Core Pathfinding")]
    public float waypointProximity = 0.2f;
    public float wanderRadius = 8f;

    [Header("Combat Escalation & Attack")]
    public float attackRange = 2.0f;
    [Tooltip("Speed while wandering (slow, heavy minotaur movement)")]
    public float wanderSpeed = 2.5f;
    [Tooltip("Speed while charging/attacking the player (raging bull)")]
    public float chargeSpeed = 13f;
    [Space(5)]
    public int tilesForTier2Boost = 2;
    public int tilesForTier3Boost = 4;
    [Space(5)]
    public float speedTier1 = 4f; // Used for legacy tier logic only
    public float damageTier1 = 10f;
    [Space(5)]
    public float speedTier2 = 6f;
    public float damageTier2 = 15f;
    [Space(5)]
    public float speedTier3 = 9f;
    public float damageTier3 = 20f;
    [Space(5)]
    public float cooldownAfterHit = 1.5f;

    [Header("Gizmos")]
    public bool showGizmos = true;
    public Color wanderPathColor = Color.cyan;
    public Color chasePathColor = Color.red;
    public Color attackRangeColor = Color.yellow;
    public float waypointGizmoRadius = 0.1f;

    private Animator animator;
    private PlayerHealth playerHealth;
    private float currentSpeed;
    private float currentDamage;
    private Vector2 lastMoveDirection;
    private int consecutiveStraightMoves = 0;
    private float cooldownEndTime;
    

    protected override void Start()
    {
        base.Start();
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        gridManager = FindFirstObjectByType<AStarGridManager>();
        if (gridManager == null)
        {
            Debug.LogError("AStarGridManager not found!");
            enabled = false;
            return;
        }

        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }

        // Pre-load decor positions
        GameObject[] decorObjects = GameObject.FindGameObjectsWithTag("Decor");
        decorPositions.Clear();
        foreach (var obj in decorObjects)
        {
            decorPositions.Add(obj.transform.position);
        }

        currentState = CreatureState.Wandering;
        StartWandering();
    }

    protected override void Update()
    {
        if (player == null || (playerHealth != null && playerHealth.IsDead))
        {
            if (currentState != CreatureState.Wandering)
            {
                currentState = CreatureState.Wandering;
                path = null;
                ResetChargeState();
            }
        }

        if (currentState == CreatureState.Cooldown && Time.time >= cooldownEndTime)
        {
            currentState = CreatureState.Wandering;
        }

        UpdateState();
        UpdateSpeedAndDamageTier();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        // Stuck detection logic
        if (currentState == CreatureState.Chasing || currentState == CreatureState.Wandering)
        {
            if (rb.linearVelocity.magnitude < stuckVelocityThreshold)
            {
                stuckTimer += Time.fixedDeltaTime;
                if (stuckTimer > stuckCheckInterval)
                {
                    // Emergency escape: recalc path to last known player position
                    RequestPathTo(lastKnownPlayerPosition);
                    // If path is null, pick a random walkable node far from decor
                    if (path == null || path.Count == 0)
                    {
                        if (gridManager != null)
                        {
                            List<Node> walkableNodes = gridManager.GetAllWalkableNodes();
                            Node bestNode = null;
                            float maxDist = 0f;
                            foreach (var node in walkableNodes)
                            {
                                float minDecorDist = float.MaxValue;
                                foreach (var decorPos in decorPositions)
                                {
                                    float d = Vector3.Distance(node.worldPosition, decorPos);
                                    if (d < minDecorDist) minDecorDist = d;
                                }
                                if (minDecorDist > maxDist)
                                {
                                    maxDist = minDecorDist;
                                    bestNode = node;
                                }
                            }
                            if (bestNode != null)
                            {
                                RequestPathTo(bestNode.worldPosition);
                            }
                        }
                    }
                    // Apply strong random nudge
                    Vector2 randomNudge = Random.insideUnitCircle.normalized * 800f;
                    rb.AddForce(randomNudge, ForceMode2D.Impulse);
                    stuckTimer = 0f;
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }
        FollowPath();
        // Reset stuck frame counter if not colliding
        // ...existing code...
    }
    
    private void UpdateSpeedAndDamageTier()
    {
        if (currentState != CreatureState.Chasing)
        {
            currentSpeed = speedTier1;
            currentDamage = damageTier1;
            return;
        }

        if (consecutiveStraightMoves >= tilesForTier3Boost)
        {
            currentSpeed = speedTier3;
            currentDamage = damageTier3;
        }
        else if (consecutiveStraightMoves >= tilesForTier2Boost)
        {
            currentSpeed = speedTier2;
            currentDamage = damageTier2;
        }
        else
        {
            currentSpeed = speedTier1;
            currentDamage = damageTier1;
        }
    }

    private void StartCharge()
    {
        // Removed charge lock logic
    }

    private void ExecuteCharge()
    {
        // Removed charge lock logic
    }

    private void ResetChargeState()
    {
        // Removed charge lock logic
    }

    private void FollowPath()
    {
        if (path == null || currentWaypointIndex >= path.Count)
        {
            rb.linearVelocity = Vector2.zero;
            if (currentState == CreatureState.Wandering || currentState == CreatureState.Cooldown)
            {
                StartWandering();
            }
            return;
        }
        
        Vector3 targetWaypoint = path[currentWaypointIndex];
        Vector2 toWaypoint = (targetWaypoint - transform.position);
        float distance = toWaypoint.magnitude;
        Vector2 desiredDirection = toWaypoint.normalized;
        Vector2 currentVelocity = rb.linearVelocity;

        // Minotaur-like dynamics: slow, heavy wander; raging charge when chasing
        // Use explicit inspector fields for speeds
        float wanderMoveSpeed = wanderSpeed;
        float chargeMoveSpeed = chargeSpeed;
        Vector2 moveDirection;
        if (currentState == CreatureState.Chasing)
        {
            moveDirection = desiredDirection;
            currentSpeed = chargeMoveSpeed;
            rb.linearVelocity = moveDirection * currentSpeed;
        }
        else
        {
            float smoothing = wanderSmoothing;
            moveDirection = Vector2.Lerp(lastMoveDirection, desiredDirection, smoothing).normalized;
            rb.linearVelocity = moveDirection * wanderMoveSpeed;
        }

        // Track straight moves for tier logic
        if (Vector2.Dot(desiredDirection, lastMoveDirection) < straightMoveDotThreshold)
        {
            consecutiveStraightMoves = 1;
        }
        lastMoveDirection = moveDirection;

        // ...existing code...

        // Advance waypoint if close enough
        if (distance < waypointProximity)
        {
            currentWaypointIndex++;
            if (currentState == CreatureState.Chasing)
            {
                consecutiveStraightMoves++;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // No longer used for damage logic
        // Damage logic moved to OnCollisionStay2D

    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (playerHealth == null)
            {
                playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            }
            if (playerHealth != null && !playerHealth.IsDead)
            {
                playerHealth.TakeDamage(Mathf.RoundToInt(currentDamage));
            }
        }
        else if (collision.gameObject.CompareTag("Decor") || (collision.gameObject.CompareTag("Enemy") && collision.gameObject != this.gameObject))
        {
            // Mark decor as unwalkable in grid
            if (gridManager != null && collision.gameObject.CompareTag("Decor"))
            {
                gridManager.SetNodeTemporarilyUnwalkable(collision.transform.position, 2.0f); // Longer block duration for decor
            }
            // Strong nudge
            Vector2 away = (transform.position - collision.transform.position).normalized;
            rb.AddForce(away * 500f, ForceMode2D.Impulse);

            // Immediately recalc path to last known player position
            lastPathRequestTime = Time.time;
            RequestPathTo(lastKnownPlayerPosition);
            // ...existing code...
        }
    }
    
    private void EnterCooldownState()
    {
        currentState = CreatureState.Cooldown;
        cooldownEndTime = Time.time + cooldownAfterHit;
        ResetChargeState();
        consecutiveStraightMoves = 0;
        StartWandering();
    }
    
    #region Unchanged Logic
    private void UpdateState()
    {
        if (currentState == CreatureState.Cooldown) return;
        float distanceToPlayer = (player != null) ? Vector2.Distance(transform.position, player.transform.position) : Mathf.Infinity;
        switch (currentState)
        {
            case CreatureState.Wandering:
                if (distanceToPlayer < detectionRange)
                {
                    currentState = CreatureState.Chasing;
                    ResetChargeState();
                    consecutiveStraightMoves = 0;
                    lastKnownPlayerPosition = player.transform.position;
                    RequestPathTo(lastKnownPlayerPosition);
                }
                break;
            case CreatureState.Chasing:
                if (distanceToPlayer < detectionRange)
                {
                    // Update last known position and keep chasing
                    lastKnownPlayerPosition = player.transform.position;
                    if (Time.time > lastPathRequestTime + pathRequestCooldown)
                    {
                        lastPathRequestTime = Time.time;
                        RequestPathTo(lastKnownPlayerPosition);
                    }
                }
                else
                {
                    // Player lost: continue to last known position
                    if (path == null || currentWaypointIndex >= path.Count)
                    {
                        // Arrived at last known position, resume wandering
                        currentState = CreatureState.Wandering;
                        path = null;
                        ResetChargeState();
                        consecutiveStraightMoves = 0;
                    }
                    else if (Time.time > lastPathRequestTime + pathRequestCooldown)
                    {
                        lastPathRequestTime = Time.time;
                        RequestPathTo(lastKnownPlayerPosition);
                    }
                }
                break;
        }
    }
    private void RequestPathTo(Vector3 targetPosition)
    {
        List<Vector3> newPath = gridManager.FindPath(transform.position, targetPosition);
        if (newPath != null && newPath.Count > 0)
        {
            path = newPath;
            currentWaypointIndex = 0;
        }
        else
        {
            path = null;
        }
    }
    
   // --- REPLACE THE EXISTING StartWandering METHOD IN StraightChaser.cs ---
    private void StartWandering()
    {
        if (gridManager == null) return;

        // Ask the grid manager for a guaranteed valid spot.
        Node randomNode = gridManager.GetRandomWalkableNode();
        
        if (randomNode != null)
        {
            // If we found a valid spot, request a path to it.
            RequestPathTo(randomNode.worldPosition);
        }
        else
        {
            // Fallback in case no walkable nodes exist at all.
            Debug.LogWarning("Could not find a random walkable node to wander to.");
        }
    }
    
    private void UpdateAnimator()
    {
        if (animator == null) return;
        Vector2 velocity = rb.linearVelocity;
        animator.SetFloat("Horizontal", velocity.x);
        animator.SetFloat("Vertical", velocity.y);
        animator.SetFloat("Speed", velocity.sqrMagnitude);
    }
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = attackRangeColor;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        // Removed charge lock gizmo

        if (path == null) return;

        Gizmos.color = currentState switch
        {
            CreatureState.Wandering => wanderPathColor,
            CreatureState.Chasing => chasePathColor,
            CreatureState.Cooldown => Color.blue,
            _ => Color.gray
        };

        for (int i = 0; i < path.Count; i++)
        {
            Gizmos.DrawSphere(path[i], waypointGizmoRadius);
            if (i < path.Count - 1)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }
        }
    }
    #endregion
}
