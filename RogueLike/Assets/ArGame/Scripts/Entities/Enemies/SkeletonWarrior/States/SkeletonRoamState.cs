using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ArGame.Entities.Enemies.SkeletonWarrior
{
    public class SkeletonRoamState : State
    {
        private NavMeshAgent navAgent;
        private Animator animator;
        private SkeletonWarriorController controller;
        private SkeletonWarriorStateMachine skeletonStateMachine;

        private Vector3 roamDestination;
        private float roamStartTime;
        private float maxRoamDuration = 10f;

        private Transform playerTransform;
        private Transform sightArea;
        private float sightRange;
        private float sightAngle;
        private LayerMask obstacleLayer;
        private Transform detectionArea;
        private float detectionRadius;
        private LayerMask playerLayer;
        private float roamMaxDistance;

        public SkeletonRoamState(StateMachine stateMachine, GameObject owner,
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
            this.roamMaxDistance = controller.roamMaxDistance;
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

            // Set roam parameters
            roamStartTime = Time.time;

            // Get a random destination to roam to
            roamDestination = GetRandomRoamDestination();

            // Start moving
            if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = false;
                navAgent.speed = controller.moveSpeed;
                navAgent.SetDestination(roamDestination);
            }

            Debug.Log($"Roaming to: {roamDestination}");
        }

        public override void Exit()
        {
            // Clean up animation parameters when exiting state
            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
                animator.ResetTrigger("StartWalk");
            }

            // Reset agent settings
            if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = true;
                navAgent.ResetPath();
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

            // Check if player is detected but not visible
            if (CheckPlayerDetected())
            {
                // Store position in controller for patrol state to use
                controller.lastKnownPlayerPosition = playerTransform.position;

                skeletonStateMachine.ChangeToState(new SkeletonPatrolState(
                    stateMachine, owner, navAgent, animator, controller));
                return;
            }

            // Check if reached destination or roaming for too long
            if (ReachedDestination() || Time.time - roamStartTime > maxRoamDuration)
            {
                // Switch to idle state
                skeletonStateMachine.ChangeToState(new SkeletonIdleState(
                    stateMachine, owner, navAgent, animator, controller));
                return;
            }

            // Check if path is invalid
            if (navAgent.pathStatus == NavMeshPathStatus.PathInvalid ||
                (navAgent.pathStatus == NavMeshPathStatus.PathComplete && navAgent.remainingDistance < 0.1f))
            {
                // Get a new destination
                roamDestination = GetRandomRoamDestination();
                navAgent.SetDestination(roamDestination);
            }
        }

        private Vector3 GetRandomRoamDestination()
        {
            NavMeshHit hit;
            Vector3 randomPosition = controller.transform.position;
            bool foundPosition = false;

            // Try to find a valid random point on NavMesh
            for (int i = 0; i < 30; i++) // Try 30 times then give up
            {
                Vector2 randomCircle = Random.insideUnitCircle * roamMaxDistance;
                Vector3 randomPoint = controller.originalPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

                // Sample closest point on NavMesh
                if (NavMesh.SamplePosition(randomPoint, out hit, roamMaxDistance, NavMesh.AllAreas))
                {
                    randomPosition = hit.position;
                    foundPosition = true;
                    break;
                }
            }

            if (!foundPosition)
            {
                Debug.LogWarning("Could not find a valid NavMesh position for roaming - using current position");
            }

            return randomPosition;
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