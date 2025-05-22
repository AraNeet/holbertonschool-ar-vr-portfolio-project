using UnityEngine;
using System.Collections;

/// <summary>
/// Handles the behavior of a mage's magical projectile.
/// Controls movement, collision detection, and damage application.
/// </summary>
public class MageProjectile : MonoBehaviour
{
    [Header("Projectile Properties")]
    [SerializeField] private float damage = 15f;             // Damage dealt on hit
    [SerializeField] private float speed = 15f;              // Movement speed
    [SerializeField] private float lifeTime = 5f;            // Maximum projectile lifetime
    [SerializeField] private float impactEffectDuration = 0.2f; // How long the impact effect plays before destroying
    [SerializeField] private float impactEffectLifetime = 2f;  // How long the impact effect stays visible

    [Header("Visuals")]
    [SerializeField] private Color projectileColor = new Color(0.3f, 0.5f, 1f); // Base color (blue)
    [SerializeField] private Color trailEndColor = new Color(0f, 0.8f, 1f);     // Trail end color (cyan)
    [SerializeField] private float lightIntensity = 1f;      // Intensity of the glow light
    [SerializeField] private float lightRange = 2f;          // Range of the glow light
    [SerializeField] private GameObject impactEffectPrefab;  // Custom impact effect prefab

    [Header("Trail Settings")]
    [SerializeField] private float trailTime = 0.3f;         // How long the trail persists
    [SerializeField] private float trailStartWidth = 0.2f;   // Width at the start of the trail
    [SerializeField] private float trailEndWidth = 0.05f;    // Width at the end of the trail
    [SerializeField] private Material trailMaterial;         // Custom trail material

    [Header("Collider")]
    [SerializeField] private float colliderRadius = 0.2f;    // Size of the collider

    // Non-serialized properties
    private Vector3 direction;
    private LayerMask targetLayers;

    // Component references
    private TrailRenderer trailRenderer;
    private ParticleSystem impactParticles;
    private Light projectileLight;
    private SphereCollider sphereCollider;
    private Renderer projectileRenderer;

    // Collision detection
    private bool hasHit = false;

    void Awake()
    {
        // Get or add components
        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer == null)
        {
            trailRenderer = gameObject.AddComponent<TrailRenderer>();
            SetupDefaultTrail(trailRenderer);
        }

        // Add a light component for glow effect
        projectileLight = GetComponent<Light>();
        if (projectileLight == null)
        {
            projectileLight = gameObject.AddComponent<Light>();
            SetupDefaultLight(projectileLight);
        }

        // Get the renderer component for disabling visibility on hit
        projectileRenderer = GetComponent<Renderer>();

        // Add collider for hit detection
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider == null)
        {
            sphereCollider = gameObject.AddComponent<SphereCollider>();
            sphereCollider.radius = colliderRadius;
            sphereCollider.isTrigger = true;
        }

        // Add rigidbody to enable trigger events
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    /// <summary>
    /// Initialize the projectile with damage, direction, and speed
    /// </summary>
    public void Initialize(float damage, Vector3 direction, float speed, LayerMask targetLayers)
    {
        this.damage = damage;
        this.direction = direction;
        this.speed = speed;
        this.targetLayers = targetLayers;

        // Start life timer
        StartCoroutine(DestroyAfterLifetime());
    }

    void Update()
    {
        if (!hasHit)
        {
            // Move projectile in the set direction
            transform.position += direction * speed * Time.deltaTime;

            // Rotate to face movement direction
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // Check if we've already registered a hit
        if (hasHit) return;

        // Check if the object is in the target layers
        if (((1 << other.gameObject.layer) & targetLayers.value) != 0 || other.CompareTag("Enemy"))
        {
            // We hit a valid target
            HandleHit(other, true);
        }
        else if (other.gameObject.CompareTag("Obstacle") || other.gameObject.CompareTag("Environment"))
        {
            // We hit an obstacle or the environment
            HandleHit(other, false);
        }
    }

    /// <summary>
    /// Handle projectile hit on any object
    /// </summary>
    private void HandleHit(Collider other, bool isEnemy)
    {
        // Mark as hit to prevent further collisions
        hasHit = true;

        // Log hit information
        if (isEnemy)
        {
            Debug.Log($"[MageProjectile] Hit enemy: {other.gameObject.name}");

            // Apply damage to target if it has a Health component
            Health targetHealth = other.GetComponent<Health>();
            if (targetHealth == null && other.transform.parent != null)
            {
                // Try to get health from parent if this is a collider child
                targetHealth = other.transform.parent.GetComponent<Health>();
            }

            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage);
                Debug.Log($"[MageProjectile] Applied {damage} damage to {other.gameObject.name}");
            }
        }
        else
        {
            Debug.Log($"[MageProjectile] Hit environment: {other.gameObject.name}");
        }

        // Immediately disable the collider to prevent further collisions
        if (sphereCollider != null)
        {
            sphereCollider.enabled = false;
        }

        // Hide the projectile mesh immediately
        if (projectileRenderer != null)
        {
            projectileRenderer.enabled = false;
        }

        // Make all child renderers invisible too
        Renderer[] childRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in childRenderers)
        {
            renderer.enabled = false;
        }

        // Play impact effects
        PlayImpactEffects(other, isEnemy);

        // Destroy the projectile
        StartCoroutine(DestroyAfterImpact());
    }

    /// <summary>
    /// Play visual and sound effects on impact
    /// </summary>
    private void PlayImpactEffects(Collider hitObject, bool isEnemy)
    {
        // Get impact point (closest point on collider)
        Vector3 impactPoint = hitObject.ClosestPoint(transform.position);

        // Stop movement
        speed = 0f;

        // Disable trail renderer but keep its existing trail
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
        }

        // Disable the glow light
        if (projectileLight != null)
        {
            projectileLight.enabled = false;
        }

        // Determine the parent for the impact effect
        Transform effectParent = null;
        if (isEnemy)
        {
            // If it's an enemy, use it as the parent
            effectParent = hitObject.transform;

            // If the collider is a child object, try to find the enemy's actual transform
            if (hitObject.GetComponent<Health>() == null && hitObject.transform.parent != null)
            {
                if (hitObject.transform.parent.GetComponent<Health>() != null)
                {
                    effectParent = hitObject.transform.parent;
                }
            }
        }

        // Create impact particles
        GameObject impactEffect = null;

        if (impactEffectPrefab != null)
        {
            // Use the assigned impact effect prefab
            impactEffect = Instantiate(impactEffectPrefab, impactPoint, Quaternion.identity);
        }
        else
        {
            // Try to use a default particle effect if available from Resources
            GameObject defaultEffectPrefab = Resources.Load<GameObject>("Prefabs/MageProjectileImpact");
            if (defaultEffectPrefab != null)
            {
                impactEffect = Instantiate(defaultEffectPrefab, impactPoint, Quaternion.identity);
            }
            else
            {
                // Create a simple particle effect
                impactEffect = new GameObject("ImpactParticles");
                impactEffect.transform.position = impactPoint;
                ParticleSystem particles = impactEffect.AddComponent<ParticleSystem>();

                // Configure a simple burst particle effect
                var main = particles.main;
                main.startColor = projectileColor; // Use the projectile's color
                main.startSize = 0.2f;
                main.startLifetime = 0.5f;
                main.duration = 0.2f;

                var emission = particles.emission;
                var burst = new ParticleSystem.Burst(0f, 20);
                emission.SetBurst(0, burst);
            }
        }

        // Set the parent if we have a valid effect and parent
        if (impactEffect != null)
        {
            if (effectParent != null)
            {
                // Parent the effect to the enemy
                impactEffect.transform.SetParent(effectParent, true);

                // Keep the local position where it hit
                Vector3 localPos = effectParent.InverseTransformPoint(impactPoint);
                impactEffect.transform.localPosition = localPos;

                Debug.Log($"[MageProjectile] Attached impact effect to {effectParent.name}");
            }

            // Destroy the effect after the specified lifetime
            Destroy(impactEffect, impactEffectLifetime);
        }
    }

    /// <summary>
    /// Destroy the projectile after a short delay to allow effects to play
    /// </summary>
    private IEnumerator DestroyAfterImpact()
    {
        // Wait a short time for effects to play
        yield return new WaitForSeconds(impactEffectDuration);

        // Destroy this projectile
        Destroy(gameObject);
    }

    /// <summary>
    /// Destroy the projectile after its lifetime expires
    /// </summary>
    private IEnumerator DestroyAfterLifetime()
    {
        yield return new WaitForSeconds(lifeTime);

        // Only destroy if we haven't hit anything yet
        if (!hasHit)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Set up default trail renderer properties
    /// </summary>
    private void SetupDefaultTrail(TrailRenderer trail)
    {
        trail.startWidth = trailStartWidth;
        trail.endWidth = trailEndWidth;
        trail.time = trailTime;

        // Create a gradient based on the configured colors
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(projectileColor, 0.0f),
                new GradientColorKey(trailEndColor, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        trail.colorGradient = gradient;

        // Use the assigned material if available
        if (trailMaterial != null)
        {
            trail.material = trailMaterial;
        }
        else
        {
            // Try to use a default material from Resources
            Material defaultTrailMaterial = Resources.Load<Material>("Materials/MageTrail");
            if (defaultTrailMaterial != null)
            {
                trail.material = defaultTrailMaterial;
            }
        }
    }

    /// <summary>
    /// Set up default light properties
    /// </summary>
    private void SetupDefaultLight(Light light)
    {
        light.color = projectileColor;
        light.intensity = lightIntensity;
        light.range = lightRange;
    }
}