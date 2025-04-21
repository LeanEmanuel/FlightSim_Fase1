using Fusion;
using UnityEngine;

/// <summary>
/// Struct that defines the input schema for the player's aircraft.
/// This data is sent each network tick from the local player to the network.
/// </summary>
public struct PlaneNetworkInput : INetworkInput
{

    // Throttle control: forward (1), idle (0), reverse (-1).
    public float throttle;


    // X = roll, Y = pitch. Represents airplane maneuver input.
    public Vector2 pitchRoll;

 
    // Yaw control (left/right).
    public float yaw;

    // Whether the cannon is being fired.
    public NetworkBool fireCannon;


    // Whether a missile launch has been triggered.
    public NetworkBool fireMissile;


    // Optional input buttons (toggles like HUD help).
    public NetworkButtons buttons;
}

/// <summary>
/// Enum representing mapped button actions for aircraft controls.
/// </summary>
public enum PlaneButtons
{
    // Toggle in-game help overlays or HUD info.
    ToggleHelp = 0
}
