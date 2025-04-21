using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles visual animations for the plane including control surfaces,
/// afterburners, airbrakes, flaps, and missile visuals.
/// </summary>
public class PlaneAnimation : MonoBehaviour
{
    [SerializeField]
    List<GameObject> afterburnerGraphics;
    [SerializeField]
    float afterburnerThreshold;
    [SerializeField]
    float afterburnerMinSize;
    [SerializeField]
    float afterburnerMaxSize;
    [SerializeField]
    float maxAileronDeflection;
    [SerializeField]
    float maxElevatorDeflection;
    [SerializeField]
    float maxRudderDeflection;
    [SerializeField]
    float airbrakeDeflection;
    [SerializeField]
    float flapsDeflection;
    [SerializeField]
    float deflectionSpeed;
    [SerializeField]
    Transform rightAileron;
    [SerializeField]
    Transform leftAileron;
    [SerializeField]
    List<Transform> elevators;
    [SerializeField]
    List<Transform> rudders;
    [SerializeField]
    Transform airbrake;
    [SerializeField]
    List<Transform> flaps;
    [SerializeField]
    List<GameObject> missileGraphics;

    Plane plane;
    List<Transform> afterburnersTransforms;
    Dictionary<Transform, Quaternion> neutralPoses;
    Vector3 deflection;
    float airbrakePosition;
    float flapsPosition;

    /// <summary>
    /// Initializes references and stores neutral rotation poses for all control surfaces.
    /// </summary>
    void Start()
    {
        plane = GetComponent<Plane>();
        afterburnersTransforms = new List<Transform>();
        neutralPoses = new Dictionary<Transform, Quaternion>();

        foreach (var go in afterburnerGraphics)
        {
            afterburnersTransforms.Add(go.GetComponent<Transform>());
        }

        AddNeutralPose(leftAileron);
        AddNeutralPose(rightAileron);

        foreach (var t in elevators)
        {
            AddNeutralPose(t);
        }

        foreach (var t in rudders)
        {
            AddNeutralPose(t);
        }

        AddNeutralPose(airbrake);

        foreach (var t in flaps)
        {
            AddNeutralPose(t);
        }
    }

    /// <summary>
    /// Shows or hides the missile mesh graphic for a given hardpoint index.
    /// </summary>
    /// <param name="index">Missile index (hardpoint number).</param>
    /// <param name="visible">Whether the missile is visible or not.</param>
    public void ShowMissileGraphic(int index, bool visible)
    {
        missileGraphics[index].SetActive(visible);
    }

    /// <summary>
    /// Stores the original local rotation of a transform for reference.
    /// </summary>
    /// <param name="transform">The control surface transform.</param>
    void AddNeutralPose(Transform transform)
    {
        neutralPoses.Add(transform, transform.localRotation);
    }

    /// <summary>
    /// Calculates the new local rotation based on the neutral pose and a desired offset.
    /// </summary>
    /// <param name="transform">Target transform.</param>
    /// <param name="offset">Rotation offset.</param>
    /// <returns>The final rotation to apply.</returns>
    Quaternion CalculatePose(Transform transform, Quaternion offset)
    {
        return neutralPoses[transform] * offset;
    }

    /// <summary>
    /// Updates visual appearance and scale of the afterburners based on current throttle.
    /// </summary>
    void UpdateAfterburners()
    {
        float throttle = plane.Throttle;
        float afterburnerT = Mathf.Clamp01(Mathf.InverseLerp(afterburnerThreshold, 1, throttle));
        float size = Mathf.Lerp(afterburnerMinSize, afterburnerMaxSize, afterburnerT);

        if (throttle >= afterburnerThreshold)
        {
            for (int i = 0; i < afterburnerGraphics.Count; i++)
            {
                afterburnerGraphics[i].SetActive(true);
                afterburnersTransforms[i].localScale = new Vector3(size, size, size);
            }
        }
        else
        {
            for (int i = 0; i < afterburnerGraphics.Count; i++)
            {
                afterburnerGraphics[i].SetActive(false);
            }
        }
    }

    /// <summary>
    /// Updates deflection of control surfaces based on player input.
    /// </summary>
    /// <param name="dt">Delta time.</param>
    void UpdateControlSurfaces(float dt)
    {
        var input = plane.EffectiveInput;

        deflection.x = Utilities.MoveTo(deflection.x, input.x, deflectionSpeed, dt, -1, 1);
        deflection.y = Utilities.MoveTo(deflection.y, input.y, deflectionSpeed, dt, -1, 1);
        deflection.z = Utilities.MoveTo(deflection.z, input.z, deflectionSpeed, dt, -1, 1);

        rightAileron.localRotation = CalculatePose(rightAileron, Quaternion.Euler(deflection.z * maxAileronDeflection, 0, 0));
        leftAileron.localRotation = CalculatePose(leftAileron, Quaternion.Euler(-deflection.z * maxAileronDeflection, 0, 0));

        foreach (var t in elevators)
        {
            t.localRotation = CalculatePose(t, Quaternion.Euler(deflection.x * maxElevatorDeflection, 0, 0));
        }

        foreach (var t in rudders)
        {
            t.localRotation = CalculatePose(t, Quaternion.Euler(0, -deflection.y * maxRudderDeflection, 0));
        }
    }

    /// <summary>
    /// Updates visual rotation of airbrake surface based on deployment state.
    /// </summary>
    /// <param name="dt">Delta time.</param>
    void UpdateAirbrakes(float dt)
    {
        var target = plane.AirbrakeDeployed ? 1 : 0;

        airbrakePosition = Utilities.MoveTo(airbrakePosition, target, deflectionSpeed, dt);

        airbrake.localRotation = CalculatePose(airbrake, Quaternion.Euler(-airbrakePosition * airbrakeDeflection, 0, 0));
    }

    /// <summary>
    /// Updates flap rotation based on deployment state.
    /// </summary>
    /// <param name="dt">Delta time.</param>
    void UpdateFlaps(float dt)
    {
        var target = plane.FlapsDeployed ? 1 : 0;

        flapsPosition = Utilities.MoveTo(flapsPosition, target, deflectionSpeed, dt);

        foreach (var t in flaps)
        {
            t.localRotation = CalculatePose(t, Quaternion.Euler(flapsPosition * flapsDeflection, 0, 0));
        }
    }

    /// <summary>
    /// Updates all visual elements at the end of the frame.
    /// </summary>
    void LateUpdate()
    {
        float dt = Time.deltaTime;

        UpdateAfterburners();
        UpdateControlSurfaces(dt);
        UpdateAirbrakes(dt);
        UpdateFlaps(dt);
    }
}
