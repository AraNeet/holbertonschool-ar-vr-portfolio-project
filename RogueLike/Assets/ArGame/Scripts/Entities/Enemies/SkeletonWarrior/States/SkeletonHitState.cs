using UnityEngine;
using UnityEngine.AI;

namespace ArGame.Entities.Enemies.SkeletonWarrior
{
    /// <summary>
    /// State for when the skeleton warrior takes damage.
    /// </summary>
    public class SkeletonHitState : State
    {
        private NavMeshAgent navAgent;
        private Animator animator;
        private SkeletonWarriorController controller;
        private Health health;

        private float hitStaggerTime = 0.7f;
        private float timer;
        private Transform player;

        public SkeletonHitState(
            StateMachine stateMachine,
            GameObject owner,
            NavMeshAgent navAgent,
            Animator animator,
            SkeletonWarriorController controller) : base(stateMachine, owner)
        {
            this.navAgent = navAgent;
            this.animator = animator;
            this.controller = controller;

            // Get health component
            health = owner.GetComponent<Health>();

            // Find player
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        public override void Enter()
        {
            // Stop the navigation agent
            navAgent.isStopped = true;
            navAgent.velocity = Vector3.zero;

            // Play hit animation
            animator.SetBool("IsMoving", false);
            animator.SetBool("IsAttacking", false);
            animator.SetTrigger("Hit");

            // Cancel any ongoing attack
            EnemyAttack enemyAttack = owner.GetComponent<EnemyAttack>();
            if (enemyAttack != null)
            {
                enemyAttack.CancelAttack();
            }

            // Reset timer
            timer = 0f;

            // Make the skeleton face the player if possible
            if (player != null)
            {
                Vector3 lookDirection = player.position - owner.transform.position;
                lookDirection.y = 0; // Keep on horizontal plane
                if (lookDirection != Vector3.zero)
                {
                    Quaternion rotation = Quaternion.LookRotation(lookDirection);
                    owner.transform.rotation = rotation;
                }
            }

            // Add a small backwards force to simulate impact
            if (player != null)
            {
                Vector3 knockbackDirection = (owner.transform.position - player.position).normalized;
                knockbackDirection.y = 0; // Keep on horizontal plane

                // Apply knockback (without physics to avoid NavMesh issues)
                Vector3 knockbackPosition = owner.transform.position + knockbackDirection * 0.5f;

                // Check if knockback position is on NavMesh
                NavMeshHit hit;
                if (NavMesh.SamplePosition(knockbackPosition, out hit, 1.0f, NavMesh.AllAreas))
                {
                    owner.transform.position = hit.position;
                }
            }
        }

        public override void Exit()
        {
            // Clear hit animation state
            animator.ResetTrigger("Hit");

            // Re-enable navigation
            navAgent.isStopped = false;
        }

        public override void Update()
        {
            timer += Time.deltaTime;

            // Check if hit state should end
            if (timer >= hitStaggerTime)
            {
                // If health is depleted, go to death state
                if (health != null && health.IsDead)
                {
                    ChangeToDeathState();
                    return;
                }

                // Otherwise, if player is in attack range, attack
                if (player != null)
                {
                    float distanceToPlayer = Vector3.Distance(owner.transform.position, player.position);

                    if (distanceToPlayer <= controller.attackRange)
                    {
                        ChangeToAttackState();
                        return;
                    }
                    // If player is in detection range, chase
                    else if (distanceToPlayer <= controller.detectionRadius)
                    {
                        ChangeToChaseState();
                        return;
                    }
                }

                // Default to idle if player not in range
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

        private void ChangeToAttackState()
        {
            SkeletonWarriorStateMachine skeletonStateMachine = stateMachine as SkeletonWarriorStateMachine;
            if (skeletonStateMachine != null)
            {
                skeletonStateMachine.ChangeToState(new SkeletonAttackState(
                    stateMachine, owner, navAgent, animator, controller));
            }
        }

        private void ChangeToDeathState()
        {
            SkeletonWarriorStateMachine skeletonStateMachine = stateMachine as SkeletonWarriorStateMachine;
            if (skeletonStateMachine != null)
            {
                skeletonStateMachine.ChangeToState(new SkeletonDeathState(
                    stateMachine, owner, navAgent, animator, controller));
            }
        }
    }
}