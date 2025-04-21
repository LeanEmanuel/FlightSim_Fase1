using Fusion;
using UnityEngine;

/// <summary>
/// Represents a homing missile with tracking, explosion, and damage logic.
/// It tracks a target and applies area damage on explosion.
/// </summary>
public class Missile : NetworkBehaviour
{
    [SerializeField] private float lifetime;
    [SerializeField] private float speed;
    [SerializeField] private float trackingAngle;
    [SerializeField] private float damage;
    [SerializeField] private float damageRadius;
    [SerializeField] private float turningGForce;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private GameObject explosionGraphic;

    private Plane owner;
    private Target target;
    private float timer;
    private bool exploded;
    private Vector3 lastPosition;

    private Rigidbody rb;

    // Exposes the Rigidbody reference.
    public Rigidbody Rigidbody => rb;

    /// <summary>
    /// Called when the missile is spawned. Initializes Rigidbody and disables graphics.
    /// </summary>
    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        lastPosition = transform.position;
        explosionGraphic.SetActive(false);
    }

    /// <summary>
    /// Launches the missile towards a target.
    /// </summary>
    /// <param name="owner">The Plane that fired the missile.</param>
    /// <param name="target">The target the missile should track.</param>
    public void Launch(Plane owner, Target target)
    {
        this.owner = owner;
        this.target = target;
        timer = lifetime;

        if (target != null)
            target.NotifyMissileLaunched(this, true);
    }

    /// <summary>
    /// Handles movement, collision detection, and tracking logic.
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || exploded) return;

        // asegurar de que el Rigidbody no sea null
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogError($"[Missile] Rigidbody ES NULL en {gameObject.name}. Abortando movimiento.");
                return;
            }
        }

        if (rb == null || transform == null)
        {
            Debug.LogWarning($"{gameObject.name}: componentes críticos faltan.");
            return;
        }

        //explode missile automatically after lifetime ends
        //timer is reused to keep missile graphics alive after explosion
        timer -= Runner.DeltaTime;
        if (timer <= 0f)
        {
            Explode();
            return;
        }

        CheckCollision();
        TrackTarget(Runner.DeltaTime);

        // Protección reforzada
        try
        {
            rb.linearVelocity = transform.forward * speed;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Missile] Error al asignar velocidad: {e.Message}");
            return;
        }
        lastPosition = transform.position;
    }

    //missile can travel very fast, collision may not be detected by physics system
    //use raycasts to check for collisions
    private void CheckCollision()
    {
        Vector3 currentPosition = transform.position;
        Vector3 movement = currentPosition - lastPosition;

        if (Physics.Raycast(lastPosition, movement.normalized, out RaycastHit hit, movement.magnitude, collisionMask))
        {
            if (hit.collider.GetComponent<Plane>() != owner)
            {
                transform.position = hit.point;
                Explode();
            }
        }

        lastPosition = currentPosition;
    }

    /// <summary>
    /// Rotates the missile to track the assigned target using physics-based calculations.
    /// </summary>
    /// <param name="dt">Delta time for this tick.</param>
    private void TrackTarget(float dt)
    {
        if (target == null) return;

        Vector3 interceptPoint = Utilities.FirstOrderIntercept(
            transform.position,
            Vector3.zero,
            speed,
            target.Position,
            target.Velocity
        );

        Vector3 targetDir = (interceptPoint - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, targetDir);

        //if angle to target is too large, explode
        if (angle > trackingAngle)
        {
            Explode();
            return;
        }

        //calculate turning rate from G Force and speed
        float maxTurnRate = (turningGForce * 9.81f) / speed; //radians / s
        Vector3 newDir = Vector3.RotateTowards(transform.forward, targetDir, maxTurnRate * dt, 0f);
        transform.rotation = Quaternion.LookRotation(newDir);
    }

    /// <summary>
    /// Handles the explosion of the missile and applies area-of-effect damage.
    /// </summary>
    private void Explode()
    {
        if (exploded) return;

        exploded = true;
        explosionGraphic.SetActive(true);
        rb.isKinematic = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, damageRadius, collisionMask);
        foreach (var hit in hits)
        {
            Plane plane = hit.GetComponent<Plane>();
            if (plane != null && plane != owner)
            {
                plane.ApplyDamage(damage);
            }
        }

        if (target != null)
        {
            target.NotifyMissileLaunched(this, false);
        }

        Runner.Despawn(Object);
    }
}
