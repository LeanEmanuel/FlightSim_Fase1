using Fusion;
using UnityEngine;

[RequireComponent(typeof(Plane))]
public class PlaneNetworkController : NetworkBehaviour
{
    private Plane plane;

    public override void Spawned()
    {
        plane = GetComponent<Plane>();

        if (HasStateAuthority)
        {
            var mainCam = Camera.main;
            var planeCam = mainCam.GetComponent<PlaneCamera>();
            if (planeCam != null && plane != null)
            {
                planeCam.SetPlane(plane);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (plane == null) return; // protección contra null

        if (HasInputAuthority && GetInput<PlaneNetworkInput>(out var input))
        {
            plane.SetThrottleInput(input.throttle);
            plane.SetControlInput(new Vector3(input.pitchRoll.y, input.yaw, -input.pitchRoll.x));
            plane.SetCannonInput(input.fireCannon);
            if (input.fireMissile)
                plane.TryFireMissile();
        }
    }
}
