using Fusion;
using UnityEngine;

public class StateAuthorityRecovery : NetworkBehaviour
{
    [SerializeField] private NetworkObject playerPlanePrefab; // 👈 Asigna el prefab desde el editor

    private float checkInterval = 3f;
    private float timer;

    public override void FixedUpdateNetwork()
    {
        if (Object.HasInputAuthority && !Object.HasStateAuthority)
        {
            timer += Runner.DeltaTime;

            if (timer >= checkInterval)
            {
                Debug.LogWarning($"[🛑] {gameObject.name} perdió StateAuthority pero tiene InputAuthority.");

                if (Runner.IsServer) // solo el servidor puede hacer esto
                {
                    Debug.Log("🔁 Server va a forzar un respawn del objeto...");

                    var inputAuthority = Object.InputAuthority;

                    Runner.Despawn(Object);

                    var newPlane = Runner.Spawn(playerPlanePrefab, transform.position, transform.rotation, inputAuthority);

                    Debug.Log($"✅ Nuevo avión instanciado con autoridad restaurada: {newPlane.name}");
                }

                timer = 0;
            }
        }
        else
        {
            timer = 0;
        }
    }
}
