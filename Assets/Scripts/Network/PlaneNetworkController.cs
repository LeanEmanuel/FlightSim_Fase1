using Fusion;
using UnityEngine;

[RequireComponent(typeof(Plane))]
public class PlaneNetworkController : NetworkBehaviour
{
    private Plane plane;
    [SerializeField] private GameObject hudPrefab;

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

            //Instanciar HUD SOLO para el jugador local
            if (hudPrefab != null && mainCam != null)
            {
                var hudInstance = Instantiate(hudPrefab);
                var hud = hudInstance.GetComponent<PlaneHUD>();
                if (hud != null)
                {
                    hud.SetPlane(plane);
                    hud.SetCamera(mainCam);
                }
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
