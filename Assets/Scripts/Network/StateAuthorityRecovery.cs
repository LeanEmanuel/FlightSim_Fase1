using Fusion;
using UnityEngine;

/// <summary>
/// Detects loss of StateAuthority and recovers it by respawning the player's aircraft with proper authority.
/// This prevents cases where a plane is still visible and controllable locally but no longer receives network updates.
/// </summary>
public class StateAuthorityRecovery : NetworkBehaviour
{

    // Prefab reference used for respawning the player's aircraft.
    // This should be assigned in the Unity Inspector.
    [SerializeField] private NetworkObject playerPlanePrefab;

    private float checkInterval = 3f;
    private float timer;

    /// <summary>
    /// Called each network tick. Checks for authority loss and initiates recovery.
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if (Object.HasInputAuthority && !Object.HasStateAuthority)
        {
            timer += Runner.DeltaTime;

            if (timer >= checkInterval)
            {
                Debug.LogWarning($"[🛑] {gameObject.name} lost StateAuthority but has InputAuthority.");

                if (Runner.IsServer) // solo el servidor puede hacer esto
                {
                    Debug.Log("🔁 Server is forcing object respawn to restore authority.");

                    var inputAuthority = Object.InputAuthority;

                    Runner.Despawn(Object);

                    var newPlane = Runner.Spawn(playerPlanePrefab, transform.position, transform.rotation, inputAuthority);

                    Debug.Log($"✅ Respawned plane with restored StateAuthority: {newPlane.name}");
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
