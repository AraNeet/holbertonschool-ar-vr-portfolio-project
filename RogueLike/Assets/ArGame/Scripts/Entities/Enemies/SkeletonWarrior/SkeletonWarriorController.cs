using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using ArGame.Entities.Enemies.SkeletonWarrior;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Health))]
public class SkeletonWarriorController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float turnSpeed = 120f;

    [Header("Behavior Parameters")]
    public float idleDuration = 3f;
    public float maxLosePlayerTime = 5f;
    public float roamMaxDistance = 20f;
    public Vector3 originalPosition;
    public Vector3 lastKnownPlayerPosition;

    [Header("Patrol Settings")]
    public Transform[] patrolPoints;

    [Header("Detection Settings")]
    public Transform sightArea;
    public float sightRange = 10f;
    public float sightAngle = 80f;
    public Transform detectionArea;
    public float detectionRadius = 5f;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

    [Header("Combat Settings")]
    public float attackRange = 2f;
    public float attackDamage = 10f;
    public float attackCooldown = 1.5f;

    // Component references
    private NavMeshAgent navAgent;
    private Animator animator;
    private Health health;
    private SkeletonWarriorStateMachine stateMachine;

    private void Awake()
    {
        // Get component references
        navAgent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();

        // Check for null components and log errors
        if (navAgent == null)
        {
            Debug.LogError($"[{gameObject.name}] NavMeshAgent component is missing!");
            enabled = false;
            return;
        }

        if (animator == null)
        {
            Debug.LogError($"[{gameObject.name}] Animator component is missing!");
            enabled = false;
            return;
        }

        if (health == null)
        {
            Debug.LogError($"[{gameObject.name}] Health component is missing!");
            enabled = false;
            return;
        }

        // Store the original position for roaming behavior
        originalPosition = transform.position;

        // Configure the NavMeshAgent
        navAgent.speed = moveSpeed;
        navAgent.angularSpeed = turnSpeed;
        navAgent.stoppingDistance = 1f;
        navAgent.autoBraking = false;
        navAgent.updatePosition = true;
        navAgent.updateRotation = true;

        // Disable any CharacterController that might exist as it conflicts with NavMeshAgent
        CharacterController charController = GetComponent<CharacterController>();
        if (charController != null)
        {
            charController.enabled = false;
        }

        // Check for Rigidbody conflicts
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.isKinematic = true;
        }

        // Configure animator
        if (animator != null)
        {
            animator.applyRootMotion = false;
        }

        // Ensure agent is on a NavMesh
        if (!navAgent.isOnNavMesh)
        {
            Debug.LogError($"[{gameObject.name}] NavMeshAgent is not on a NavMesh! Check object position or NavMesh baking.");
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
            {
                transform.position = hit.position;
                Debug.Log($"[{gameObject.name}] Repositioned to nearest NavMesh at {hit.position}");
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] Could not find a nearby NavMesh position. Enemy will not move.");
                enabled = false;
                return;
            }
        }

        // Create the state machine
        stateMachine = GetComponent<SkeletonWarriorStateMachine>();
        if (stateMachine == null)
        {
            stateMachine = gameObject.AddComponent<SkeletonWarriorStateMachine>();
        }

        // Set up health events
        if (health != null)
        {
            health.OnDeath.AddListener(HandleDeath);
            health.OnDamaged.AddListener(HandleDamage);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Health component is null. Death and damage handling won't work.");
        }

        // Ensure enemy is on the Enemy layer and has the Enemy tag
        gameObject.tag = "Enemy";
        gameObject.layer = LayerMask.NameToLayer("Enemy");
    }

    private void Start()
    {
        // Validate patrol points
        ValidatePatrolPoints();

        // Initialize the state machine with idle state
        if (stateMachine != null && navAgent != null && animator != null)
        {
            stateMachine.Initialize(new SkeletonIdleState(
                stateMachine, gameObject, navAgent, animator, this));
        }
        else
        {
            Debug.LogError($"[{gameObject.name}] Cannot initialize state machine. Missing required components.");
            enabled = false;
        }
    }

    // Validate that patrol points are on the NavMesh
    private void ValidatePatrolPoints()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogWarning($"[{gameObject.name}] No patrol points assigned!");
            return;
        }

        List<Transform> validPoints = new List<Transform>();

        foreach (Transform point in patrolPoints)
        {
            if (point == null) continue;

            // Check if point is on NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(point.position, out hit, 2.0f, NavMesh.AllAreas))
            {
                // If position is not exactly on NavMesh, move it there
                if (Vector3.Distance(hit.position, point.position) > 0.1f)
                {
                    point.position = hit.position;
                }
                validPoints.Add(point);
            }
            else
            {
                Debug.LogError($"[{gameObject.name}] Patrol point at {point.position} is too far from NavMesh and will be ignored!");
            }
        }

        if (validPoints.Count == 0)
        {
            Debug.LogError($"[{gameObject.name}] No valid patrol points on NavMesh! Agent will not be able to patrol.");
        }
        else if (validPoints.Count < patrolPoints.Length)
        {
            // Update patrol points array with only valid points
            patrolPoints = validPoints.ToArray();
        }
    }

    private void OnValidate()
    {
        // Auto-assign detection areas if not set manually
        if (sightArea == null)
        {
            Transform sightTransform = transform.Find("SightArea");
            if (sightTransform != null)
                sightArea = sightTransform;
            else
                sightArea = transform;
        }

        if (detectionArea == null)
        {
            Transform detectionTransform = transform.Find("DetectionArea");
            if (detectionTransform != null)
                detectionArea = detectionTransform;
            else
                detectionArea = transform;
        }

        // Default layers if not set
        if (playerLayer == 0)
            playerLayer = LayerMask.GetMask("Player");

        if (obstacleLayer == 0)
            obstacleLayer = LayerMask.GetMask("Obstacle", "Default");
    }

    private void HandleDamage()
    {
        // Only change to hit state if we're not already in hit or death state
        if (!(stateMachine.CurrentStateName.Contains("Hit") || stateMachine.CurrentStateName.Contains("Death")))
        {
            // Transition to hit state
            stateMachine.ChangeToState(new SkeletonHitState(
                stateMachine, gameObject, navAgent, animator, this));
        }
    }

    private void HandleDeath()
    {
        // Don't handle death if we're already in death state
        if (!stateMachine.CurrentStateName.Contains("Death"))
        {
            // Transition to death state
            stateMachine.ChangeToState(new SkeletonDeathState(
                stateMachine, gameObject, navAgent, animator, this));
        }
    }

    // Visual debugging for editor only
    private void OnDrawGizmosSelected()
    {
        // Draw sight cone
        if (sightArea != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 sightPosition = sightArea.position;

            // Draw sight radius
            DrawWireArc(sightPosition, sightArea.forward, sightAngle, sightRange);
        }

        // Draw detection radius
        if (detectionArea != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(detectionArea.position, detectionRadius);
        }

        // Draw attack range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw patrol points
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            foreach (Transform point in patrolPoints)
            {
                if (point != null)
                {
                    Gizmos.DrawSphere(point.position, 0.5f);
                }
            }
        }
    }

    private void DrawWireArc(Vector3 position, Vector3 dir, float angle, float radius)
    {
        float angleRad = angle * Mathf.Deg2Rad;
        Vector3 forwardDir = dir.normalized * radius;

        Vector3 right = Vector3.Cross(dir, Vector3.up).normalized * radius;

        int segments = 20;
        Vector3 previous = position + Quaternion.AngleAxis(-angle / 2, Vector3.up) * forwardDir;

        for (int i = 0; i <= segments; i++)
        {
            float segmentAngle = (-angle / 2) + ((float)i / segments) * angle;
            Vector3 current = position + Quaternion.AngleAxis(segmentAngle, Vector3.up) * forwardDir;

            Gizmos.DrawLine(previous, current);
            if (i == 0 || i == segments)
                Gizmos.DrawLine(position, current);

            previous = current;
        }
    }
}
