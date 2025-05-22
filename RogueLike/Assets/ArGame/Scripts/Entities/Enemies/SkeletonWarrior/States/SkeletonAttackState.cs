using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace ArGame.Entities.Enemies.SkeletonWarrior
{
    /// <summary>
    /// State for when the skeleton warrior is attacking the player.
    /// </summary>
    public class SkeletonAttackState : State
    {
        private NavMeshAgent navAgent;
        private Animator animator;
        private SkeletonWarriorController controller;
        private Transform player;
        private Health playerHealth;
        private EnemyAttack enemyAttack;

        private float attackTimer;
        private float lastAttackTime;
        private bool hasDealtDamage = false;

        public SkeletonAttackState(
            StateMachine stateMachine,
            GameObject owner,
            NavMeshAgent navAgent,
            Animator animator,
            SkeletonWarriorController controller) : base(stateMachine, owner)
        {
            this.navAgent = navAgent;
            this.animator = animator;
            this.controller = controller;

            // Find the player
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
                playerHealth = playerObject.GetComponent<Health>();
            }

            // Get or add EnemyAttack component
            enemyAttack = owner.GetComponent<EnemyAttack>();
            if (enemyAttack == null)
            {
                enemyAttack = owner.AddComponent<EnemyAttack>();
            }
            
            // Always sync attack properties from controller to ensure consistency
            enemyAttack.attackDamage = controller.attackDamage;
            enemyAttack.attackCooldown = controller.attackCooldown;
            enemyAttack.attackRange = controller.attackRange;
            
            // Set hitbox to activate immediately with the animation
            enemyAttack.hitboxDelay = 0f;
            
            // Duration to match how long the attack animation shows the swing
            enemyAttack.hitboxDuration = controller.attackCooldown * 0.6f;
        }

        public override void Enter()
        {
            // Stop the navigation agent
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;

            // Face the player
            if (player != null)
            {
                Vector3 lookDirection = player.position - owner.transform.position;
                lookDirection.y = 0; // Keep on the horizontal plane
                if (lookDirection != Vector3.zero)
                {
                    Quaternion rotation = Quaternion.LookRotation(lookDirection);
                    owner.transform.rotation = rotation;
                }
            }

            // Start attack animation
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsAttacking", true);

            // Reset attack state
            hasDealtDamage = false;
            attackTimer = 0f;
            lastAttackTime = Time.time;
            
            // Double-check that attack range and timing match controller settings
            if (enemyAttack != null)
            {
                enemyAttack.attackRange = controller.attackRange;
                enemyAttack.attackCooldown = controller.attackCooldown;
                
                // Make sure hitbox activates immediately with the animation
                enemyAttack.hitboxDelay = 0f;
                
                // Set hitbox duration to match the main portion of the attack animation
                enemyAttack.hitboxDuration = controller.attackCooldown * 0.6f;
            }
        }

        public override void Exit()
        {
            // Reset animation parameters
            animator.SetBool("IsAttacking", false);
            // Cancel any ongoing attack
            if (enemyAttack != null)
            {
                enemyAttack.CancelAttack();
            }

            // Re-enable navigation
            navAgent.isStopped = false;
        }

        public override void Update()
        {
            if (player == null)
            {
                ChangeToIdleState();
                return;
            }

            // Track attack timing
            attackTimer += Time.deltaTime;
            
            // Ensure we're facing the player during attack
            Vector3 lookDirection = player.position - owner.transform.position;
            lookDirection.y = 0;
            if (lookDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                owner.transform.rotation = Quaternion.Slerp(
                    owner.transform.rotation, 
                    targetRotation, 
                    Time.deltaTime * controller.turnSpeed / 2f); // Half turn speed during attack
            }

            // If at the start of attack animation and haven't dealt damage yet, apply damage
            // Note: We're now starting the attack immediately, so use a small threshold
            float attackStartPoint = controller.attackCooldown * 0.1f; // Just 10% into the animation
            if (attackTimer >= attackStartPoint && !hasDealtDamage)
            {
                // Try to deal damage using the EnemyAttack component
                if (enemyAttack != null && player != null)
                {
                    float distanceToPlayer = Vector3.Distance(owner.transform.position, player.position);
                    if (distanceToPlayer <= controller.attackRange)
                    {
                        enemyAttack.TryAttack(player.gameObject);
                        hasDealtDamage = true;
                    }
                }
            }

            // Check if attack is finished
            if (attackTimer >= controller.attackCooldown)
            {
                float distanceToPlayer = Vector3.Distance(owner.transform.position, player.position);

                // If player is still in range, attack again
                if (distanceToPlayer <= controller.attackRange)
                {
                    // Reset attack state
                    hasDealtDamage = false;
                    attackTimer = 0f;
                    lastAttackTime = Time.time;

                    // Start new attack animation
                    animator.SetBool("IsAttacking", false);
                    animator.SetBool("IsAttacking", true);
                }
                // If player is out of attack range but still in detection range, chase
                else if (distanceToPlayer <= controller.detectionRadius)
                {
                    ChangeToChaseState();
                }
                // If player is out of detection range, go back to idle
                else
                {
                    ChangeToIdleState();
                }
            }

            // If player dies, go back to idle
            if (playerHealth != null && playerHealth.IsDead)
            {
                ChangeToIdleState();
            }
        }

        private void ChangeToIdleState()
        {
            SkeletonWarriorStateMachine skeletonStateMachine = stateMachine as SkeletonWarriorStateMachine;
            if (skeletonStateMachine != null)
            {
                skeletonStateMachine.ChangeToState(new SkeletonIdleState(
                    stateMachine, owner, navAgent, animator, controller));
            }
        }

        private void ChangeToChaseState()
        {
            SkeletonWarriorStateMachine skeletonStateMachine = stateMachine as SkeletonWarriorStateMachine;
            if (skeletonStateMachine != null)
            {
                skeletonStateMachine.ChangeToState(new SkeletonChaseState(
                    stateMachine, owner, navAgent, animator, controller));
            }
        }
    }
}