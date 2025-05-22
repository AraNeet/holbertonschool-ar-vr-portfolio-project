using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ArGame.Entities.Enemies.SkeletonWarrior
{
    public class SkeletonPatrolState : State
    {
        private NavMeshAgent navAgent;
        private Animator animator;
        private SkeletonWarriorController controller;
        private SkeletonWarriorStateMachine skeletonStateMachine;

        private Vector3 patrolDestination;
        private bool reachedDestination = false;
        private float searchStartTime;
        private float searchDuration = 5f;

        private Transform playerTransform;
        private Transform sightArea;
        private float sightRange;
        private float sightAngle;
        private LayerMask obstacleLayer;
        private Transform detectionArea;
        private float detectionRadius;
        private LayerMask playerLayer;
        private NavMeshPath path;

        public SkeletonPatrolState(StateMachine stateMachine, GameObject owner,
                                NavMeshAgent navAgent, Animator animator,
                                SkeletonWarriorController controller)
            : base(stateMachine, owner)
        {
            this.navAgent = navAgent;
            this.animator = animator;
            this.controller = controller;
            this.skeletonStateMachine = stateMachine as SkeletonWarriorStateMachine;

            // Get references from controller
            this.playerLayer = controller.playerLayer;
            this.playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            this.sightArea = controller.sightArea;
            this.sightRange = controller.sightRange;
            this.sightAngle = controller.sightAngle;
            this.obstacleLayer = controller.obstacleLayer;
            this.detectionArea = controller.detectionArea;
            this.detectionRadius = controller.detectionRadius;

            // Determine patrol destination
            if (controller.lastKnownPlayerPosition != Vector3.zero)
            {
                // If player was spotted, go to last known position
                this.patrolDestination = controller.lastKnownPlayerPosition;
            }
            else if (controller.patrolPoints != null && controller.patrolPoints.Length > 0)
            {
                // Pick a random patrol point
                int randomIndex = Random.Range(0, controller.patrolPoints.Length);
                if (controller.patrolPoints[randomIndex] != null)
                {
                    this.patrolDestination = controller.patrolPoints[randomIndex].position;
                }
                else
                {
                    // Fallback to original position
                    this.patrolDestination = controller.originalPosition;
                }
            }
            else
            {
                // No patrol points, go back to original position
                this.patrolDestination = controller.originalPosition;
            }
        }

        public override void Enter()
        {
            // Set animation
            if (animator != null)
            {
                animator.SetBool("IsWalking", true);
                animator.SetBool("IsRunning", false);
                animator.SetBool("IsIdle", false);

                // Force the transition with a trigger
                animator.SetTrigger("StartWalk");
            }

            // Start moving
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                if (!navAgent.isOnNavMesh)
                {
                    Debug.LogError($"[{owner.name}] NavMeshAgent is not on NavMesh!");

                    // Try to place on NavMesh
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(owner.transform.position, out hit, 5f, NavMesh.AllAreas))
                    {
                        owner.transform.position = hit.position;
                    }
                    else
                    {
                        // Can't find NavMesh nearby, go back to idle
                        skeletonStateMachine.ChangeToState(new SkeletonIdleState(
                            stateMachine, owner, navAgent, animator, controller));
                        return;
                    }
                }

                // Configure agent
                navAgent.isStopped = false;
                navAgent.updatePosition = true;
                navAgent.updateRotation = true;
                navAgent.ResetPath();
                navAgent.speed = controller.moveSpeed;

                // Check if destination is on NavMesh
                NavMeshHit destinationHit;
                if (!NavMesh.SamplePosition(patrolDestination, out destinationHit, 2.0f, NavMesh.AllAreas))
                {
                    // Try to find a valid point nearby
                    if (NavMesh.SamplePosition(patrolDestination, out destinationHit, 10.0f, NavMesh.AllAreas))
                    {
                        patrolDestination = destinationHit.position;
                    }
                    else
                    {
                        // No valid nearby point, go back to idle
                        skeletonStateMachine.ChangeToState(new SkeletonIdleState(
                            stateMachine, owner, navAgent, animator, controller));
                        return;
                    }
                }

                // Calculate path and set destination
                path = new NavMeshPath();
                navAgent.CalculatePath(patrolDestination, path);

                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    navAgent.SetDestination(patrolDestination);
                    navAgent.isStopped = false;
                }
                else if (path.status == NavMeshPathStatus.PathPartial)
                {
                    navAgent.SetDestination(patrolDestination);
                }
                else
                {
                    Debug.LogError($"[{owner.name}] No path found to {patrolDestination}. Returning to idle.");
                    skeletonStateMachine.ChangeToState(new SkeletonIdleState(
                        stateMachine, owner, navAgent, animator, controller));
                    return;
                }
            }
            else
            {
                Debug.LogError($"[{owner.name}] NavMeshAgent is null or not active!");
                skeletonStateMachine.ChangeToState(new SkeletonIdleState(
                    stateMachine, owner, navAgent, animator, controller));
                return;
            }

            reachedDestination = false;
        }

        public override void Exit()
        {
            // Clean up animation parameters when exiting state
            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
                animator.ResetTrigger("StartWalk");
            }
        }

        public override void Update()
        {
            // Check if player is visible
            if (CheckPlayerVisible())
            {
                skeletonStateMachine.ChangeToState(new SkeletonChaseState(
                    stateMachine, owner, navAgent, animator, controller));
                return;
            }

            // Check for invalid path
            if (navAgent != null && navAgent.isActiveAndEnabled && !reachedDestination)
            {
                if (float.IsInfinity(navAgent.remainingDistance) || navAgent.pathStatus == NavMeshPathStatus.PathInvalid)
                {
                    Debug.LogError($"[{owner.name}] Path is invalid or unreachable. Returning to idle.");
                    skeletonStateMachine.ChangeToState(new SkeletonIdleState(
                        stateMachine, owner, navAgent, animator, controller));
                    return;
                }
            }

            // Check if reached destination
            if (!reachedDestination && ReachedDestination())
            {
                // Start searching
                reachedDestination = true;
                searchStartTime = Time.time;

                // Stop movement and set idle animation
                if (navAgent != null)
                {
                    navAgent.isStopped = true;
                    navAgent.ResetPath();
                }

                // Set idle animation for searching
                if (animator != null)
                {
                    animator.SetBool("IsWalking", false);
                    animator.SetBool("IsIdle", true);
                }

                Debug.Log("Reached patrol destination, searching area...");
            }

            // If searching
            if (reachedDestination)
            {
                // Look around (rotate while searching)
                owner.transform.Rotate(0, 90 * Time.deltaTime, 0);

                // Check if search time completed
                if (Time.time - searchStartTime >= searchDuration)
                {
                    Debug.Log("Search completed, going back to idle");

                    // Switch to idle state
                    skeletonStateMachine.ChangeToState(new SkeletonIdleState(
                        stateMachine, owner, navAgent, animator, controller));
                    return;
                }
            }

            // Check if player is detected during patrol
            if (!reachedDestination && CheckPlayerDetected())
            {
                // Update the patrol destination to the new player position
                if (playerTransform != null)
                {
                    patrolDestination = playerTransform.position;
                    controller.lastKnownPlayerPosition = patrolDestination;

                    // Update destination
                    if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
                    {
                        navAgent.SetDestination(patrolDestination);
                    }
                }
            }
        }

        private bool ReachedDestination()
        {
            if (navAgent == null || !navAgent.isActiveAndEnabled || !navAgent.isOnNavMesh)
                return true;

            return !navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance;
        }

        private bool CheckPlayerVisible()
        {
            if (sightArea == null || playerTransform == null)
                return false;

            Vector3 directionToPlayer = playerTransform.position - sightArea.position;
            float angle = Vector3.Angle(sightArea.forward, directionToPlayer);

            if (angle < sightAngle * 0.5f && directionToPlayer.magnitude < sightRange)
            {
                // Cast ray to check for obstacles
                Ray ray = new Ray(sightArea.position, directionToPlayer.normalized);
                if (!Physics.Raycast(ray, directionToPlayer.magnitude, obstacleLayer))
                {
                    // Player is visible
                    controller.lastKnownPlayerPosition = playerTransform.position;
                    return true;
                }
            }

            return false;
        }

        private bool CheckPlayerDetected()
        {
            if (detectionArea == null || playerTransform == null)
                return false;

            Collider[] colliders = Physics.OverlapSphere(detectionArea.position, detectionRadius, playerLayer);
            if (colliders.Length > 0)
            {
                controller.lastKnownPlayerPosition = playerTransform.position;
                return true;
            }

            return false;
        }
    }
}