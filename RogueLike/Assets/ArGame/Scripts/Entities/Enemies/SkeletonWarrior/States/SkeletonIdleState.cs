using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ArGame.Entities.Enemies.SkeletonWarrior
{
    public class SkeletonIdleState : State
    {
        private NavMeshAgent navAgent;
        private Animator animator;
        private float idleTimer;
        private float idleDuration;
        private SkeletonWarriorController controller;
        private Transform[] patrolPoints;
        private LayerMask playerLayer;
        private Transform playerTransform;
        private Transform sightArea;
        private float sightRange;
        private float sightAngle;
        private LayerMask obstacleLayer;
        private Transform detectionArea;
        private float detectionRadius;
        private SkeletonWarriorStateMachine skeletonStateMachine;
        private float attackRange;

        public SkeletonIdleState(StateMachine stateMachine, GameObject owner,
                                NavMeshAgent navAgent, Animator animator,
                                SkeletonWarriorController controller)
            : base(stateMachine, owner)
        {
            this.navAgent = navAgent;
            this.animator = animator;
            this.controller = controller;
            this.skeletonStateMachine = stateMachine as SkeletonWarriorStateMachine;

            // Get references from controller
            this.idleDuration = controller.idleDuration;
            this.patrolPoints = controller.patrolPoints;
            this.playerLayer = controller.playerLayer;
            this.playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            this.sightArea = controller.sightArea;
            this.sightRange = controller.sightRange;
            this.sightAngle = controller.sightAngle;
            this.obstacleLayer = controller.obstacleLayer;
            this.detectionArea = controller.detectionArea;
            this.detectionRadius = controller.detectionRadius;
            this.attackRange = controller.attackRange;
        }

        public override void Enter()
        {
            // Stop movement
            if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = true;
                navAgent.ResetPath();
            }

            // Set animation
            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsRunning", false);
                animator.SetBool("IsIdle", true);

                // Force the transition with a trigger
                animator.SetTrigger("StartIdle");
            }

            // Reset idle timer
            idleTimer = 0f;
        }

        public override void Exit()
        {
            // Clean up animation parameters when exiting state
            if (animator != null)
            {
                animator.SetBool("IsIdle", false);
                animator.ResetTrigger("StartIdle");
            }
        }

        public override void Update()
        {
            // Check if player is close enough to attack directly
            if (playerTransform != null)
            {
                float distanceToPlayer = Vector3.Distance(owner.transform.position, playerTransform.position);
                if (distanceToPlayer <= attackRange && CheckPlayerVisible())
                {
                    // Close enough to attack directly
                    skeletonStateMachine.ChangeToState(new SkeletonAttackState(
                        stateMachine, owner, navAgent, animator, controller));
                    return;
                }
            }

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
                Vector3 lastKnownPosition = controller.lastKnownPlayerPosition;
                skeletonStateMachine.ChangeToState(new SkeletonPatrolState(
                    stateMachine, owner, navAgent, animator, controller));
                return;
            }

            // Update idle timer
            idleTimer += Time.deltaTime;

            // Switch to roaming after idle duration
            if (idleTimer >= idleDuration)
            {
                // Random chance to patrol or roam
                if (Random.value > 0.5f && patrolPoints != null && patrolPoints.Length > 0)
                {
                    int randomIndex = Random.Range(0, patrolPoints.Length);
                    if (patrolPoints[randomIndex] != null)
                    {
                        skeletonStateMachine.ChangeToState(new SkeletonPatrolState(
                            stateMachine, owner, navAgent, animator, controller));
                        return;
                    }
                }

                skeletonStateMachine.ChangeToState(new SkeletonRoamState(
                    stateMachine, owner, navAgent, animator, controller));
                return;
            }
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