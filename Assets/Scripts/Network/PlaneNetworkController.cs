using Fusion;
using UnityEngine;

[RequireComponent(typeof(Plane))]
public class PlaneNetworkController : NetworkBehaviour
{
    private Plane plane;
    private PlaneHUD hud;
    private NetworkButtons previousButtons;
    private float retryAssignTimer = 0;
    private bool targetAssigned = false;

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

            AssignTarget();
        }
    }

    public bool AssignTarget()
    {
        var allTargets = FindObjectsOfType<Target>();
        float closestDistance = float.MaxValue;
        Target closestTarget = null;

        foreach (var t in allTargets)
        {
            if (t.Plane == null || t.Plane == plane || t.Plane.Dead)
                continue;

            float dist = Vector3.Distance(plane.transform.position, t.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestTarget = t;
            }
        }

        if (closestTarget != null)
        {
            plane.SetTarget(closestTarget);
            Debug.Log($"{plane.name} ha asignado como target más cercano a {closestTarget.Name} (distancia: {closestDistance:F1})");
            return true;
        }

        return false;
    }

    public override void FixedUpdateNetwork()
    {
        if (plane == null) return; // protección contra null
                                   
        if (targetAssigned && (plane.Target == null || plane.Target.Plane == null || plane.Target.Plane.Dead))
        {
            Debug.LogWarning($"⚠️ Target inválido. Reasignando...");
            targetAssigned = false;
            plane.SetTarget(null);
        }

        // Reintentar asignación si no hay target válido
        if (!targetAssigned)
        {
            retryAssignTimer -= Runner.DeltaTime;
            if (retryAssignTimer <= 0f)
            {
                if (AssignTarget())
                {
                    targetAssigned = true;
                }
                else
                {
                    retryAssignTimer = 1f; // volver a intentar en 1 segundo
                }
            }
        }

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
