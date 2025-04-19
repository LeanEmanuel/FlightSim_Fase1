using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    public GameObject PlayerPrefab;

    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            // Spawneamos el avión con autoridad
            Runner.Spawn(PlayerPrefab, new Vector3(0, 2, 0), Quaternion.identity, player);

        }
    }
}
