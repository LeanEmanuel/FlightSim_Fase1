using Fusion;
using UnityEngine;

public class Bullet : NetworkBehaviour
{
    [SerializeField] private float speed = 100f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float lifeTime = 2f;
    [SerializeField] private LayerMask collisionMask;

    private float timer;
    private Plane owner;

    public void Fire(Plane owner)
    {
        this.owner = owner;
        timer = lifeTime;

        // Ignorar colisión con el propio avión
        Collider ownerCollider = owner.GetComponent<Collider>();
        Collider bulletCollider = GetComponent<Collider>();

        if (ownerCollider != null && bulletCollider != null)
        {
            Physics.IgnoreCollision(bulletCollider, ownerCollider);
            Debug.Log("🛡️ Ignorando colisión entre bala y avión que la disparó.");
        }

        Debug.Log($"🟢 Bala creada por: {owner.gameObject.name} con ID: {owner.GetInstanceID()}");
    }

    public override void FixedUpdateNetwork()
    {
        if (timer <= 0)
        {
            Runner.Despawn(Object);
            return;
        }

        float step = speed * Runner.DeltaTime;
        Vector3 direction = transform.forward;

        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, step, collisionMask))
        {
            Debug.Log($"Bala impactó en {hit.collider.gameObject.name}");
            Plane hitPlane = hit.collider.GetComponent<Plane>();

            if (hitPlane != null)
            {
                
                Debug.Log($"🎯 Impactó a: {hitPlane.gameObject.name} con ID: {hitPlane.GetInstanceID()}");

                if (hitPlane.Object.Id == owner.Object.Id)
                {
                    Debug.Log("⚠️ El avión impactado es el mismo que disparó (mismo ID de red).");
                }
                else
                {
                    Debug.Log("✅ Avión enemigo impactado.");
                }

                if (hitPlane.Object.Id != owner.Object.Id && HasStateAuthority)
                {
                    Debug.Log("✅ Enviando RPC de daño al enemigo...");
                    hitPlane.RPC_ApplyDamage(damage); // ✅ esto va al dueño real
                }
            }

            Runner.Despawn(Object);
            return;
        }

        transform.position += direction * step;
        timer -= Runner.DeltaTime;
    }
}

