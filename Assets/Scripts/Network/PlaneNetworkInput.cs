
using Fusion;
using UnityEngine;

public struct PlaneNetworkInput : INetworkInput
{
    public float throttle;
    public Vector2 pitchRoll;
    public float yaw;
    public NetworkBool fireCannon;
    public NetworkBool fireMissile;
    public NetworkButtons buttons;
}
public enum PlaneButtons
{
    ToggleHelp = 0
}
