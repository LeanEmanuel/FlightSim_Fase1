using Fusion;
using UnityEngine;

[RequireComponent(typeof(Plane))]
public class PlaneNetworkController : NetworkBehaviour
{
    private Plane plane;
    private PlaneHUD hud;
    private NetworkButtons previousButtons;
    [SerializeField] private GameObject hudPrefab;
    [Networked] private NetworkButtons PreviousButtons { get; set; }

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
                this.hud = hudInstance.GetComponent<PlaneHUD>();
                if (this.hud != null)
                {
                    this.hud.SetPlane(plane);
                    this.hud.SetCamera(mainCam);
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

            //Aquí evaluamos el botón solo si fue presionado este tick
            var pressed = input.buttons.GetPressed(PreviousButtons);
            if (pressed.IsSet((int)PlaneButtons.ToggleHelp))
            {
                hud?.ToggleHelpDialogs();
            }

            //Guardamos el estado actual como anterior
            PreviousButtons = input.buttons;

        }
    }
}
