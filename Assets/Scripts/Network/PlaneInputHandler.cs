using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;

/// <summary>
/// Captures and submits player input to the Fusion network system.
/// </summary>
public class PlaneInputHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    /// <summary>
    /// Registers input callbacks when the component is enabled.
    /// </summary>
    void OnEnable()
    {
        var runner = GetComponent<NetworkRunner>();
        if (runner != null)
        {
            runner.AddCallbacks(this);
        }
    }

    /// <summary>
    /// Called every tick to capture and send player input.
    /// </summary>
    /// <param name="runner">The NetworkRunner managing the simulation.</param>
    /// <param name="input">The structure where input data will be stored.</param>
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {

        PlaneNetworkInput data = new PlaneNetworkInput
        {
            throttle = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f,
            pitchRoll = new Vector2(
        Input.GetKey(KeyCode.RightArrow) ? 1f : Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f,
        Input.GetKey(KeyCode.DownArrow) ? -1f : Input.GetKey(KeyCode.UpArrow) ? 1f : 0f
    ),
            yaw = Input.GetKey(KeyCode.A) ? -1f : Input.GetKey(KeyCode.D) ? 1f : 0f,
            fireCannon = Input.GetKey(KeyCode.Space),
            fireMissile = Input.GetKeyDown(KeyCode.M),

        };
        data.buttons.Set((int)PlaneButtons.ToggleHelp, Input.GetKey(KeyCode.H));
        input.Set(data);
    }

    // Optional callbacks required by INetworkRunnerCallbacks
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
}
