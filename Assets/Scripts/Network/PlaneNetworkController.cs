using Fusion;
using UnityEngine;

/// <summary>
/// Controls player input, HUD management, and dynamic target assignment for the aircraft.
/// Handles both client-side control and authority-based logic for multiplayer synchronization.
/// </summary>
[RequireComponent(typeof(Plane))]
public class PlaneNetworkController : NetworkBehaviour
{
    private Plane plane;
    private PlaneHUD hud;
    private NetworkButtons previousButtons;
    private float retryAssignTimer = 0;
    private bool targetAssigned = false;

    [SerializeField] private GameObject hudPrefab;

    // Stores the state of pressed buttons from the previous tick.
    // Used for detecting button toggles.
    [Networked] private NetworkButtons PreviousButtons { get; set; }

    /// <summary>
    /// Called when the object is spawned on the network.
    /// Sets up the HUD and assigns an initial target if the player has authority.
    /// </summary>
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

            //Instantiate HUD only for the local player
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

    /// <summary>
    /// Finds the nearest valid enemy target and assigns it to the plane.
    /// Skips dead or self-assigned planes.
    /// </summary>
    /// <returns>True if a valid target was assigned; otherwise, false.</returns>
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

    /// <summary>
    /// Called every network tick. Handles input, HUD updates, and target reassignment logic.
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if (plane == null) return; // protección contra null

        if (targetAssigned && (plane.Target == null || plane.Target.Plane == null || plane.Target.Plane.Dead))
        {
            Debug.LogWarning($"⚠️ Invalid target. Reassigning...");
            targetAssigned = false;
            plane.SetTarget(null);
        }

        // Try to reassign target every second if needed
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
                    retryAssignTimer = 1f; // 1 segundo
                }
            }
        }

        // Process input if this client has authority
        if (HasInputAuthority && GetInput<PlaneNetworkInput>(out var input))
        {
            plane.SetThrottleInput(input.throttle);
            plane.SetControlInput(new Vector3(input.pitchRoll.y, input.yaw, -input.pitchRoll.x));
            plane.SetCannonInput(input.fireCannon);

            if (input.fireMissile)
                plane.TryFireMissile();

            // Evaluate the button only if this tick was pressed
            var pressed = input.buttons.GetPressed(PreviousButtons);
            if (pressed.IsSet((int)PlaneButtons.ToggleHelp))
            {
                hud?.ToggleHelpDialogs();
            }

            // Save the current state as previous
            PreviousButtons = input.buttons;

        }
    }
}
