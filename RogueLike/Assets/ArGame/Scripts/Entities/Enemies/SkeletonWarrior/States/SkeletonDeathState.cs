using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace ArGame.Entities.Enemies.SkeletonWarrior
{
    /// <summary>
    /// State for when the skeleton warrior is killed.
    /// </summary>
    public class SkeletonDeathState : State
    {
        private NavMeshAgent navAgent;
        private Animator animator;
        private SkeletonWarriorController controller;
        private Health health;

        private float deathAnimationDuration = 3f;
        private float timer;
        private bool hasStartedDeath = false;
        private bool hasTriggeredDestroy = false;

        public SkeletonDeathState(
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
        }

        public override void Enter()
        {
            // Stop the navigation agent
            if (navAgent != null && navAgent.isActiveAndEnabled)
            {
                navAgent.isStopped = true;
                navAgent.velocity = Vector3.zero;
                navAgent.enabled = false; // Disable NavMeshAgent to avoid conflicts
            }

            // Disable any physics to prevent glitches
            Collider[] colliders = owner.GetComponents<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            // Play death animation
            if (animator != null)
            {
                animator.SetBool("IsMoving", false);
                animator.SetBool("IsAttacking", false);
                animator.SetTrigger("Death");

                // Ensure other parameters are reset
                animator.ResetTrigger("Hit");
            }

            // Reset timer
            timer = 0f;
            hasStartedDeath = true;
            hasTriggeredDestroy = false;

            // Disable any enemy attack component
            EnemyAttack enemyAttack = owner.GetComponent<EnemyAttack>();
            if (enemyAttack != null)
            {
                enemyAttack.enabled = false;
            }

            // Start destroy sequence as coroutine
            owner.GetComponent<MonoBehaviour>().StartCoroutine(DestroyAfterDelay());
        }

        public override void Exit()
        {
            // This state should never exit naturally
            // The GameObject will be destroyed
        }

        public override void Update()
        {
            if (!hasStartedDeath) return;

            timer += Time.deltaTime;

            // The skeleton stays in this state until it's destroyed
            // No transitions out of death state
        }

        private IEnumerator DestroyAfterDelay()
        {
            // Wait for death animation to complete
            yield return new WaitForSeconds(deathAnimationDuration);

            if (!hasTriggeredDestroy)
            {
                hasTriggeredDestroy = true;

                // Optional: fade out the model or play dissolve effect
                // FadeOutModel();

                // Wait a bit more for any fade effect
                yield return new WaitForSeconds(1f);

                // Destroy the GameObject
                GameObject.Destroy(owner);
            }
        }

        // Optional: Method to gradually fade out the model using materials
        private void FadeOutModel()
        {
            // Get all renderers
            Renderer[] renderers = owner.GetComponentsInChildren<Renderer>();

            // Start fade out coroutine
            owner.GetComponent<MonoBehaviour>().StartCoroutine(FadeOutCoroutine(renderers));
        }

        private IEnumerator FadeOutCoroutine(Renderer[] renderers)
        {
            float duration = 1.0f;
            float elapsed = 0.0f;

            // Store the original materials if needed to restore
            Material[] originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    // Create a material instance to modify without affecting other enemies
                    originalMaterials[i] = renderers[i].material;
                    renderers[i].material = new Material(originalMaterials[i]);
                }
            }

            // Fade out all renderers
            while (elapsed < duration)
            {
                float normalizedTime = elapsed / duration;

                foreach (Renderer renderer in renderers)
                {
                    if (renderer != null)
                    {
                        Color color = renderer.material.color;
                        color.a = 1.0f - normalizedTime;
                        renderer.material.color = color;
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // Ensure full transparency at the end
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    Color color = renderer.material.color;
                    color.a = 0.0f;
                    renderer.material.color = color;
                }
            }
        }
    }
}