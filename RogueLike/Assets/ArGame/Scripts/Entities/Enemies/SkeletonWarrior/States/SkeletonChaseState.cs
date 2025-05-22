using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace ArGame.Entities.Enemies.SkeletonWarrior
{
    public class SkeletonChaseState : State
    {
        private NavMeshAgent navAgent;
        private Animator animator;
        private SkeletonWarriorController controller;
        private SkeletonWarriorStateMachine skeletonStateMachine;
        private Transform playerTransform;
        private float chaseSpeed;
        private float losePlayerTimer;
        private float maxLosePlayerTime;
        private LayerMask obstacleLayer;
        private Transform sightArea;
        private float sightRange;
        private float sightAngle;
        private float attackRange;

        public SkeletonChaseState(StateMachine stateMachine, GameObject owner,
                                 NavMeshAgent navAgent, Animator animator,
                                 SkeletonWarriorController controller)
            : base(stateMachine, owner)
        {
            this.navAgent = navAgent;
            this.animator = animator;
            this.controller = controller;
            this.skeletonStateMachine = stateMachine as SkeletonWarriorStateMachine;

            // Get references from controller
            this.chaseSpeed = controller.chaseSpeed;
            this.maxLosePlayerTime = controller.maxLosePlayerTime;
            this.obstacleLayer = controller.obstacleLayer;
            this.sightArea = controller.sightArea;
            this.sightRange = controller.sightRange;
            this.sightAngle = controller.sightAngle;
            this.attackRange = controller.attackRange;

            // Find player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                this.playerTransform = player.transform;
            }
        }

        public override void Enter()
        {
            // Configure NavMeshAgent for chasing
            if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = false;
                navAgent.speed = chaseSpeed;
            }

            // Set animation
            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsRunning", true);
                animator.SetBool("IsIdle", false);

                // Force the transition with a trigger
                animator.SetTrigger("StartRun");
            }

            // Reset timer
            losePlayerTimer = 0f;
        }

        public override void Update()
        {
            if (playerTransform == null)
            {
                SwitchToRoamState();
                return;
            }

            // Check if player is visible
            bool isPlayerVisible = CheckPlayerVisible();

            // Update player's last known position if visible
            if (isPlayerVisible)
            {
                controller.lastKnownPlayerPosition = playerTransform.position;
                losePlayerTimer = 0f;
            }
            else
            {
                // Increment timer if player not visible
                losePlayerTimer += Time.deltaTime;
                if (losePlayerTimer >= maxLosePlayerTime)
                {
                    // Lost player for too long, switch to patrol to last known position
                    skeletonStateMachine.ChangeToState(new SkeletonPatrolState(
                        stateMachine, owner, navAgent, animator, controller));
                    return;
                }
            }

            // Set destination to player's position
            if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                navAgent.SetDestination(playerTransform.position);

                // Check if in attack range
                float distanceToPlayer = Vector3.Distance(owner.transform.position, playerTransform.position);
                if (distanceToPlayer <= attackRange)
                {
                    // Switch to attack state
                    SwitchToAttackState();
                    return;
                }
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
                    return true;
                }
            }

            return false;
        }

        private void SwitchToRoamState()
        {
            skeletonStateMachine.ChangeToState(new SkeletonRoamState(
                stateMachine, owner, navAgent, animator, controller));
        }

        private void SwitchToAttackState()
        {
            skeletonStateMachine.ChangeToState(new SkeletonAttackState(
                stateMachine, owner, navAgent, animator, controller));
        }

        public override void Exit()
        {
            // Reset animation
            if (animator != null)
            {
                animator.SetBool("IsRunning", false);
                animator.ResetTrigger("StartRun");
            }

            // Reset agent settings
            if (navAgent != null && navAgent.isActiveAndEnabled && navAgent.isOnNavMesh)
            {
                navAgent.isStopped = true;
                navAgent.ResetPath();
            }
        }
    }
}