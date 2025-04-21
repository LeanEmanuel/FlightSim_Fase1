using Fusion;
using UnityEngine;

/// <summary>
/// Responsible for spawning player aircraft when they join the game session.
/// Spawns aircraft at a random spawn point depending on their assigned team.
/// </summary>
public class PlayerSpawner : SimulationBehaviour, IPlayerJoined

{    // The prefab of the aircraft controlled by each player.
    public GameObject PlayerPrefab;

    // Spawn points available for Team A.
    public Transform[] spawnPointsTeamA;

    // Spawn points available for Team B.
    public Transform[] spawnPointsTeamB;

    /// <summary>
    /// Called automatically by Fusion when a player joins the session.
    /// Assigns a spawn point and instantiates the player aircraft.
    /// </summary>
    /// <param name="player">The player reference joining the session.</param>
    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            // Determine team based on player ID (even = Team A, odd = Team B)
            bool isTeamA = (player.RawEncoded % 2 == 0);

            Transform[] spawnPoints = isTeamA ? spawnPointsTeamA : spawnPointsTeamB;
            Transform chosenSpawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

            Runner.Spawn(PlayerPrefab, chosenSpawn.position, chosenSpawn.rotation, player);

        }
    }
}
