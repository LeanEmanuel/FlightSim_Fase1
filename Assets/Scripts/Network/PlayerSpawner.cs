using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    public GameObject PlayerPrefab;

    public Transform[] spawnPointsTeamA;
    public Transform[] spawnPointsTeamB;

    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            //Elegir base A o B según el número del jugador (pares/impares)
            bool isTeamA = (player.RawEncoded % 2 == 0);

            Transform[] spawnPoints = isTeamA ? spawnPointsTeamA : spawnPointsTeamB;
            Transform chosenSpawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

            Runner.Spawn(PlayerPrefab, chosenSpawn.position, chosenSpawn.rotation, player);

        }
    }
}
