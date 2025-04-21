using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

/// <summary>
/// Controls the behavior and physics of the player-controlled aircraft,
/// including flight mechanics, input handling, weapons, damage system,
/// and network synchronization using Photon Fusion.
/// </summary>
public class Plane : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public float Health { get; private set; }

    [Networked] public float MaxHealth { get; private set; }
    [SerializeField]
    private GameObject playerPlanePrefab;
    [SerializeField]
    float maxThrust;
    [SerializeField]
    float throttleSpeed;
    [SerializeField]
    float gLimit;
    [SerializeField]
    float gLimitPitch;

    [Header("Lift")]
    [SerializeField]
    float liftPower;
    [SerializeField]
    AnimationCurve liftAOACurve;
    [SerializeField]
    float inducedDrag;
    [SerializeField]
    AnimationCurve inducedDragCurve;
    [SerializeField]
    float rudderPower;
    [SerializeField]
    AnimationCurve rudderAOACurve;
    [SerializeField]
    AnimationCurve rudderInducedDragCurve;
    [SerializeField]
    float flapsLiftPower;
    [SerializeField]
    float flapsAOABias;
    [SerializeField]
    float flapsDrag;
    [SerializeField]
    float flapsRetractSpeed;

    [Header("Steering")]
    [SerializeField]
    Vector3 turnSpeed;
    [SerializeField]
    Vector3 turnAcceleration;
    [SerializeField]
    AnimationCurve steeringCurve;

    [Header("Drag")]
    [SerializeField]
    AnimationCurve dragForward;
    [SerializeField]
    AnimationCurve dragBack;
    [SerializeField]
    AnimationCurve dragLeft;
    [SerializeField]
    AnimationCurve dragRight;
    [SerializeField]
    AnimationCurve dragTop;
    [SerializeField]
    AnimationCurve dragBottom;
    [SerializeField]
    Vector3 angularDrag;
    [SerializeField]
    float airbrakeDrag;

    [Header("Misc")]
    [SerializeField]
    List<Collider> landingGear;
    [SerializeField]
    PhysicsMaterial landingGearBrakesMaterial;
    [SerializeField]
    List<GameObject> graphics;
    [SerializeField]
    GameObject damageEffect;
    [SerializeField]
    GameObject deathEffect;
    [SerializeField]
    bool flapsDeployed;
    [SerializeField]
    float initialSpeed;

    [Header("Weapons")]
    [SerializeField]
    List<Transform> hardpoints;
    [SerializeField]
    float missileReloadTime;
    [SerializeField]
    float missileDebounceTime;
    [SerializeField]
    GameObject missilePrefab;
    [SerializeField]
    Target target;
    [SerializeField]
    float lockRange;
    [SerializeField]
    float lockSpeed;
    [SerializeField]
    float lockAngle;
    [SerializeField]
    [Tooltip("Firing rate in Rounds Per Minute")]
    float cannonFireRate;
    [SerializeField]
    float cannonDebounceTime;
    [SerializeField]
    float cannonSpread;
    [SerializeField]
    Transform cannonSpawnPoint;
    [SerializeField]
    GameObject bulletPrefab;

    new PlaneAnimation animation;

    float throttleInput;
    Vector3 controlInput;

    Vector3 lastVelocity;
    PhysicsMaterial landingGearDefaultMaterial;

    int missileIndex;
    List<float> missileReloadTimers;
    float missileDebounceTimer;
    Vector3 missileLockDirection;

    bool cannonFiring;
    float cannonDebounceTimer;
    float cannonFiringTimer;
    private bool previousMissileLocked;



    public bool Dead { get; private set; }

    public Rigidbody Rigidbody { get; private set; }
    public float Throttle { get; private set; }
    public Vector3 EffectiveInput { get; private set; }
    public Vector3 Velocity { get; private set; }
    public Vector3 LocalVelocity { get; private set; }
    public Vector3 LocalGForce { get; private set; }
    public Vector3 LocalAngularVelocity { get; private set; }
    public float AngleOfAttack { get; private set; }
    public float AngleOfAttackYaw { get; private set; }
    public bool AirbrakeDeployed { get; private set; }

    public bool FlapsDeployed
    {
        get
        {
            return flapsDeployed;
        }
        private set
        {
            flapsDeployed = value;

            foreach (var lg in landingGear)
            {
                lg.enabled = value;
            }
        }
    }

    public bool MissileLocked { get; private set; }
    public bool MissileTracking { get; private set; }
    public Target Target
    {
        get
        {
            return target;
        }
    }
    public Vector3 MissileLockDirection
    {
        get
        {
            return Rigidbody.rotation * missileLockDirection;
        }
    }

    /// <summary>
    /// Initializes the health of the aircraft based on a given maximum value.
    /// </summary>
    /// <param name="max">Maximum health to assign.</param>
    public void InitHealth(float max)
    {
        MaxHealth = Mathf.Max(0, max);
        Health = MaxHealth;
    }

    /// <summary>
    /// Updates the HUD when health changes, called via OnChangedRender.
    /// </summary>
    public void OnHealthChanged()
    {
        Debug.Log($"[HUD] Vida actualizada (cliente): {Health}");

        if (Object.HasInputAuthority)
        {
            var hud = FindObjectOfType<PlaneHUD>();
            if (hud != null)
            {
                hud.UpdateHealthBar(Health, MaxHealth);
            }
        }
    }

    /// <summary>
    /// Remote Procedure Call (RPC) used to apply damage to this aircraft from another client.
    /// This ensures that only the object with StateAuthority can modify the health value.
    /// </summary>
    /// <param name="damage">Amount of damage to apply.</param>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_ApplyDamage(float damage)
    {
        ApplyDamage(damage);
    }

    /// <summary>
    /// Called automatically by Fusion when the object is spawned in the scene.
    /// Initializes rigidbody and weapon systems.
    /// </summary>
    public override void Spawned()
    {
        animation = GetComponent<PlaneAnimation>();
        Rigidbody = GetComponent<Rigidbody>();

        if (landingGear.Count > 0)
        {
            landingGearDefaultMaterial = landingGear[0].sharedMaterial;
        }

        missileReloadTimers = new List<float>(hardpoints.Count);
        foreach (var h in hardpoints)
        {
            missileReloadTimers.Add(0);
        }

        missileLockDirection = Vector3.forward;

        Rigidbody.linearVelocity = Rigidbody.rotation * new Vector3(0, 0, initialSpeed);

        // inicializar vida en el host
        if (HasStateAuthority)
        {
            InitHealth(100);
        }
        OnHealthChanged();
    }
    /*
    void Start()
    {
        animation = GetComponent<PlaneAnimation>();
        Rigidbody = GetComponent<Rigidbody>();

        if (landingGear.Count > 0)
        {
            landingGearDefaultMaterial = landingGear[0].sharedMaterial;
        }

        missileReloadTimers = new List<float>(hardpoints.Count);

        foreach (var h in hardpoints)
        {
            missileReloadTimers.Add(0);
        }

        missileLockDirection = Vector3.forward;

        Rigidbody.linearVelocity = Rigidbody.rotation * new Vector3(0, 0, initialSpeed);
    }
    */

    /// <summary>
    /// Sets the throttle input from the player. Range is typically between -1 and 1.
    /// </summary>
    /// <param name="input">Throttle input value.</param>
    public void SetThrottleInput(float input)
    {
        if (Dead) return;
        throttleInput = input;
    }

    /// <summary>
    /// Sets the control input vector for pitch, yaw, and roll.
    /// </summary>
    public void SetControlInput(Vector3 input)
    {
        if (Dead) return;
        controlInput = Vector3.ClampMagnitude(input, 1);
    }

    /// <summary>
    /// Enables or disables cannon firing based on input state.
    /// </summary>
    /// <param name="input">True if firing, false otherwise.</param>
    public void SetCannonInput(bool input)
    {
        if (Dead) return;
        cannonFiring = input;
    }

    /// <summary>
    /// Toggles the deployment of flaps if under retract speed.
    /// </summary>
    public void ToggleFlaps()
    {
        if (LocalVelocity.z < flapsRetractSpeed)
        {
            FlapsDeployed = !FlapsDeployed;
        }
    }

    /// <summary>
    /// Applies damage to the aircraft, reducing its health.
    /// Executed only on the object with StateAuthority.
    /// </summary>
    public void ApplyDamage(float damage)
    {

        if (!HasStateAuthority) return;
        float oldHealth = Health;
        Health = Mathf.Max(Health - damage, 0);
        Debug.Log($"{gameObject.name} ha recibido {damage} de daño — Vida: {oldHealth} → {Health}");
    }

    /// <summary>
    /// Instantly disables control, triggers explosion visuals and marks plane as dead.
    /// </summary>
    void Die()
    {
        throttleInput = 0;
        Throttle = 0;
        Dead = true;
        cannonFiring = false;

        damageEffect.GetComponent<ParticleSystem>().Pause();
        deathEffect.SetActive(true);
    }

    /// <summary>
    /// Updates the aircraft throttle gradually based on input and throttleSpeed.
    /// Also controls the airbrake deployment.
    /// </summary>
    /// <param name="dt">Delta time since last tick.</param>
    void UpdateThrottle(float dt)
    {
        float target = 0;
        if (throttleInput > 0) target = 1;

        //throttle input is [-1, 1]
        //throttle is [0, 1]
        Throttle = Utilities.MoveTo(Throttle, target, throttleSpeed * Mathf.Abs(throttleInput), dt);

        AirbrakeDeployed = Throttle == 0 && throttleInput == -1;

        if (AirbrakeDeployed)
        {
            foreach (var lg in landingGear)
            {
                lg.sharedMaterial = landingGearBrakesMaterial;
            }
        }
        else
        {
            foreach (var lg in landingGear)
            {
                lg.sharedMaterial = landingGearDefaultMaterial;
            }
        }
    }

    /// <summary>
    /// Automatically retracts flaps if the forward velocity exceeds the threshold.
    /// </summary>
    void UpdateFlaps()
    {
        if (LocalVelocity.z > flapsRetractSpeed)
        {
            FlapsDeployed = false;
        }
    }

    /// <summary>
    /// Calculates the Angle of Attack (AoA) and yaw AoA for lift computation.
    /// </summary>
    void CalculateAngleOfAttack()
    {
        if (LocalVelocity.sqrMagnitude < 0.1f)
        {
            AngleOfAttack = 0;
            AngleOfAttackYaw = 0;
            return;
        }

        AngleOfAttack = Mathf.Atan2(-LocalVelocity.y, LocalVelocity.z);
        AngleOfAttackYaw = Mathf.Atan2(LocalVelocity.x, LocalVelocity.z);
    }

    /// <summary>
    /// Calculates G-force experienced by the plane based on acceleration.
    /// </summary>
    /// <param name="dt">Delta time.</param>
    void CalculateGForce(float dt)
    {
        var invRotation = Quaternion.Inverse(Rigidbody.rotation);
        var acceleration = (Velocity - lastVelocity) / dt;
        LocalGForce = invRotation * acceleration;
        lastVelocity = Velocity;
    }

    /// <summary>
    /// Calculates velocity, local velocity and angular velocity of the aircraft.
    /// </summary>
    /// <param name="dt">Delta time.</param>
    void CalculateState(float dt)
    {
        var invRotation = Quaternion.Inverse(Rigidbody.rotation);
        Velocity = Rigidbody.linearVelocity;
        LocalVelocity = invRotation * Velocity;  //transform world velocity into local space
        LocalAngularVelocity = invRotation * Rigidbody.angularVelocity;  //transform into local space

        CalculateAngleOfAttack();
    }

    /// <summary>
    /// Applies forward thrust force based on throttle input.
    /// </summary>
    void UpdateThrust()
    {
        Rigidbody.AddRelativeForce(Throttle * maxThrust * Vector3.forward);
    }

    /// <summary>
    /// Applies aerodynamic drag based on the aircraft’s current velocity and drag curves.
    /// </summary>
    void UpdateDrag()
    {
        var lv = LocalVelocity;
        var lv2 = lv.sqrMagnitude;  //velocity squared

        float airbrakeDrag = AirbrakeDeployed ? this.airbrakeDrag : 0;
        float flapsDrag = FlapsDeployed ? this.flapsDrag : 0;

        //calculate coefficient of drag depending on direction on velocity
        var coefficient = Utilities.Scale6(
            lv.normalized,
            dragRight.Evaluate(Mathf.Abs(lv.x)), dragLeft.Evaluate(Mathf.Abs(lv.x)),
            dragTop.Evaluate(Mathf.Abs(lv.y)), dragBottom.Evaluate(Mathf.Abs(lv.y)),
            dragForward.Evaluate(Mathf.Abs(lv.z)) + airbrakeDrag + flapsDrag,   //include extra drag for forward coefficient
            dragBack.Evaluate(Mathf.Abs(lv.z))
        );

        var drag = coefficient.magnitude * lv2 * -lv.normalized;    //drag is opposite direction of velocity

        Rigidbody.AddRelativeForce(drag);
    }

    /// <summary>
    /// Calculates the aerodynamic lift and induced drag based on angle of attack, local velocity,
    /// and wing orientation. Returns the total force vector to apply to the aircraft.
    /// </summary>
    /// <param name="angleOfAttack">The angle between the chord line of the wing and the direction of airflow (in radians).</param>
    /// <param name="rightAxis">The local axis perpendicular to the lift-producing surface (usually Vector3.right or Vector3.up).</param>
    /// <param name="liftPower">The lift force multiplier for the surface.</param>
    /// <param name="aoaCurve">The lift coefficient curve based on angle of attack (AOA).</param>
    /// <param name="inducedDragCurve">The drag coefficient curve based on forward velocity.</param>
    /// <returns>A Vector3 representing the lift and induced drag force to apply.</returns>
    Vector3 CalculateLift(float angleOfAttack, Vector3 rightAxis, float liftPower, AnimationCurve aoaCurve, AnimationCurve inducedDragCurve)
    {
        var liftVelocity = Vector3.ProjectOnPlane(LocalVelocity, rightAxis);    //project velocity onto YZ plane
        var v2 = liftVelocity.sqrMagnitude;                                     //square of velocity

        //lift = velocity^2 * coefficient * liftPower
        //coefficient varies with AOA
        var liftCoefficient = aoaCurve.Evaluate(angleOfAttack * Mathf.Rad2Deg);
        var liftForce = v2 * liftCoefficient * liftPower;

        //lift is perpendicular to velocity
        var liftDirection = Vector3.Cross(liftVelocity.normalized, rightAxis);
        var lift = liftDirection * liftForce;

        //induced drag varies with square of lift coefficient
        var dragForce = liftCoefficient * liftCoefficient;
        var dragDirection = -liftVelocity.normalized;
        var inducedDrag = dragDirection * v2 * dragForce * this.inducedDrag * inducedDragCurve.Evaluate(Mathf.Max(0, LocalVelocity.z));

        return lift + inducedDrag;
    }

    /// <summary>
    /// Calculates and applies aerodynamic lift and yaw force using AoA and lift curves.
    /// </summary>
    void UpdateLift()
    {
        if (LocalVelocity.sqrMagnitude < 1f) return;

        float flapsLiftPower = FlapsDeployed ? this.flapsLiftPower : 0;
        float flapsAOABias = FlapsDeployed ? this.flapsAOABias : 0;

        var liftForce = CalculateLift(
            AngleOfAttack + (flapsAOABias * Mathf.Deg2Rad), Vector3.right,
            liftPower + flapsLiftPower,
            liftAOACurve,
            inducedDragCurve
        );

        var yawForce = CalculateLift(AngleOfAttackYaw, Vector3.up, rudderPower, rudderAOACurve, rudderInducedDragCurve);

        Rigidbody.AddRelativeForce(liftForce);
        Rigidbody.AddRelativeForce(yawForce);
    }

    /// <summary>
    /// Calculates and applies angular drag to simulate resistance to rotation.
    /// </summary>
    void UpdateAngularDrag()
    {
        var av = LocalAngularVelocity;
        var drag = av.sqrMagnitude * -av.normalized;    //squared, opposite direction of angular velocity
        Rigidbody.AddRelativeTorque(Vector3.Scale(drag, angularDrag), ForceMode.Acceleration);  //ignore rigidbody mass
    }

    Vector3 CalculateGForce(Vector3 angularVelocity, Vector3 velocity)
    {
        //estiamte G Force from angular velocity and velocity
        //Velocity = AngularVelocity * Radius
        //G = Velocity^2 / R
        //G = (Velocity * AngularVelocity * Radius) / Radius
        //G = Velocity * AngularVelocity
        //G = V cross A
        return Vector3.Cross(angularVelocity, velocity);
    }

    /// <summary>
    /// Calculates the maximum allowed G-force in each axis based on pilot input.
    /// Scales pitch, yaw, and roll limits accordingly and returns a force vector.
    /// </summary>
    /// <param name="input">The control input direction vector (usually normalized).</param>
    /// <returns>The maximum G-force vector allowed in each axis (in m/s²).</returns>
    Vector3 CalculateGForceLimit(Vector3 input)
    {
        return Utilities.Scale6(input,
            gLimit, gLimitPitch,    //pitch down, pitch up
            gLimit, gLimit,         //yaw
            gLimit, gLimit          //roll
        ) * 9.81f;
    }

    /// <summary>
    /// Calculates a limiter factor to scale down the control input if the expected G-force
    /// would exceed the configured limits of the aircraft structure.
    /// </summary>
    /// <param name="controlInput">The normalized control input vector (pitch, yaw, roll).</param>
    /// <param name="maxAngularVelocity">The maximum angular velocity for each axis.</param>
    /// <returns>A float between 0 and 1 representing how much to scale the input down.</returns>
    float CalculateGLimiter(Vector3 controlInput, Vector3 maxAngularVelocity)
    {
        if (controlInput.magnitude < 0.01f)
        {
            return 1;
        }

        //if the player gives input with magnitude less than 1, scale up their input so that magnitude == 1
        var maxInput = controlInput.normalized;

        var limit = CalculateGForceLimit(maxInput);
        var maxGForce = CalculateGForce(Vector3.Scale(maxInput, maxAngularVelocity), LocalVelocity);

        if (maxGForce.magnitude > limit.magnitude)
        {
            //example:
            //maxGForce = 16G, limit = 8G
            //so this is 8 / 16 or 0.5
            return limit.magnitude / maxGForce.magnitude;
        }

        return 1;
    }

    /// <summary>
    /// Calculates the steering correction value for one axis based on acceleration constraints.
    /// </summary>
    /// <param name="dt">Delta time.</param>
    /// <param name="angularVelocity">Current angular velocity.</param>
    /// <param name="targetVelocity">Target angular velocity.</param>
    /// <param name="acceleration">Acceleration limit.</param>
    /// <returns>Steering correction value.</returns>
    float CalculateSteering(float dt, float angularVelocity, float targetVelocity, float acceleration)
    {
        var error = targetVelocity - angularVelocity;
        var accel = acceleration * dt;
        return Mathf.Clamp(error, -accel, accel);
    }

    /// <summary>
    /// Calculates and applies angular corrections based on control input and G-limiter.
    /// </summary>
    /// <param name="dt">Delta time.</param>
    void UpdateSteering(float dt)
    {
        var speed = Mathf.Max(0, LocalVelocity.z);
        var steeringPower = steeringCurve.Evaluate(speed);

        var gForceScaling = CalculateGLimiter(controlInput, turnSpeed * Mathf.Deg2Rad * steeringPower);

        var targetAV = Vector3.Scale(controlInput, turnSpeed * steeringPower * gForceScaling);
        var av = LocalAngularVelocity * Mathf.Rad2Deg;

        var correction = new Vector3(
            CalculateSteering(dt, av.x, targetAV.x, turnAcceleration.x * steeringPower),
            CalculateSteering(dt, av.y, targetAV.y, turnAcceleration.y * steeringPower),
            CalculateSteering(dt, av.z, targetAV.z, turnAcceleration.z * steeringPower)
        );

        Rigidbody.AddRelativeTorque(correction * Mathf.Deg2Rad, ForceMode.VelocityChange);    //ignore rigidbody mass

        var correctionInput = new Vector3(
            Mathf.Clamp((targetAV.x - av.x) / turnAcceleration.x, -1, 1),
            Mathf.Clamp((targetAV.y - av.y) / turnAcceleration.y, -1, 1),
            Mathf.Clamp((targetAV.z - av.z) / turnAcceleration.z, -1, 1)
        );

        var effectiveInput = (correctionInput + controlInput) * gForceScaling;

        EffectiveInput = new Vector3(
            Mathf.Clamp(effectiveInput.x, -1, 1),
            Mathf.Clamp(effectiveInput.y, -1, 1),
            Mathf.Clamp(effectiveInput.z, -1, 1)
        );
    }

    /// <summary>
    /// Fires a missile from the next available hardpoint.
    /// </summary>
    public void TryFireMissile()
    {
        if (Dead) return;

        //try all available missiles
        for (int i = 0; i < hardpoints.Count; i++)
        {
            var index = (missileIndex + i) % hardpoints.Count;
            if (missileDebounceTimer == 0 && missileReloadTimers[index] == 0)
            {
                FireMissile(index);

                missileIndex = (index + 1) % hardpoints.Count;
                missileReloadTimers[index] = missileReloadTime;
                missileDebounceTimer = missileDebounceTime;

                animation.ShowMissileGraphic(index, false);
                break;
            }
        }
    }

    /// <summary>
    /// Spawns and fires a missile from a specific hardpoint index.
    /// </summary>
    /// <param name="index">Hardpoint index.</param>
    void FireMissile(int index)
    {
        var hardpoint = hardpoints[index];
        var missileObj = Runner.Spawn(missilePrefab, hardpoint.position, hardpoint.rotation, Object.InputAuthority);
        if (missileObj.TryGetComponent<Missile>(out var missile))
        {
            missile.Launch(this, MissileLocked ? Target : null);
        }
    }

    /// <summary>
    /// Updates missile firing, locking and cannon fire.
    /// </summary>
    /// <param name="dt">Delta time.</param>
    void UpdateWeapons(float dt)
    {
        UpdateWeaponCooldown(dt);
        UpdateMissileLock(dt);
        UpdateCannon(dt);
    }

    /// <summary>
    /// Updates the missile and cannon cooldown timers.
    /// Also resets visual indicators if missiles are reloaded.
    /// </summary>
    /// <param name="dt">Delta time.</param>
    void UpdateWeaponCooldown(float dt)
    {
        missileDebounceTimer = Mathf.Max(0, missileDebounceTimer - dt);
        cannonDebounceTimer = Mathf.Max(0, cannonDebounceTimer - dt);
        cannonFiringTimer = Mathf.Max(0, cannonFiringTimer - dt);

        for (int i = 0; i < missileReloadTimers.Count; i++)
        {
            missileReloadTimers[i] = Mathf.Max(0, missileReloadTimers[i] - dt);

            if (missileReloadTimers[i] == 0)
            {
                animation.ShowMissileGraphic(i, true);
            }
        }
    }

    /// <summary>
    /// Computes missile lock state based on angle and distance to current target.
    /// </summary>
    /// <param name="dt">Delta time.</param>
    void UpdateMissileLock(float dt)
    {
        //default neutral position is forward
        Vector3 targetDir = Vector3.forward;
        MissileTracking = false;

        if (Target != null && !Target.Plane.Dead)
        {
            var error = target.Position - Rigidbody.position;
            var errorDir = Quaternion.Inverse(Rigidbody.rotation) * error.normalized; //transform into local space

            if (error.magnitude <= lockRange && Vector3.Angle(Vector3.forward, errorDir) <= lockAngle)
            {
                MissileTracking = true;
                targetDir = errorDir;
            }
        }

        //missile lock either rotates towards the target, or towards the neutral position
        missileLockDirection = Vector3.RotateTowards(missileLockDirection, targetDir, Mathf.Deg2Rad * lockSpeed * dt, 0);

        MissileLocked = Target != null && MissileTracking && Vector3.Angle(missileLockDirection, targetDir) < lockSpeed * dt;

        // LOG solo si el estado cambia
        if (!previousMissileLocked && MissileLocked)
        {
            Debug.Log($"{gameObject.name} HA BLOQUEADO a {Target?.Name}");
        }
        previousMissileLocked = MissileLocked;
    }

    /// <summary>
    /// Fires a cannon bullet with spread and cooldown.
    /// </summary>
    void UpdateCannon(float dt)
    {
        if (cannonFiring && cannonFiringTimer == 0)
        {
            cannonFiringTimer = 60f / cannonFireRate;

            var spread = Random.insideUnitCircle * cannonSpread;

            var rotation = cannonSpawnPoint.rotation * Quaternion.Euler(spread.x, spread.y, 0);
            var bulletObj = Runner.Spawn(bulletPrefab, cannonSpawnPoint.position, rotation, Object.StateAuthority);
            if (bulletObj.TryGetComponent<Bullet>(out var bullet))
            {
                bullet.Fire(this);
            }
        }
    }

    /// <summary>
    /// Assigns a new target for missiles and HUD locking.
    /// </summary>
    /// <param name="newTarget">The new target to lock onto.</param>
    public void SetTarget(Target newTarget)
    {
        if (!HasStateAuthority) return;
        target = newTarget;
    }

    /// <summary>
    /// Main update loop for the aircraft. Handles physics and control logic.
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        float dt = Runner.DeltaTime;
        CheckAndRecoverAuthority();

        if (HasStateAuthority == false) return;


        //calculate at start, to capture any changes that happened externally
        CalculateState(dt);
        CalculateGForce(dt);
        UpdateFlaps();

        //handle user input
        UpdateThrottle(dt);

        if (!Dead)
        {
            //apply updates
            UpdateThrust();
            UpdateLift();
            UpdateSteering(dt);
        }
        else
        {
            //align with velocity
            Vector3 up = Rigidbody.rotation * Vector3.up;
            Vector3 forward = Rigidbody.linearVelocity.normalized;
            Rigidbody.rotation = Quaternion.LookRotation(forward, up);
        }

        UpdateDrag();
        UpdateAngularDrag();

        //calculate again, so that other systems can read this plane's state
        CalculateState(dt);

        //update weapon state
        UpdateWeapons(dt);

        if (Health <= MaxHealth * 0.5f && Health > 0)
        {
            damageEffect.SetActive(true);
        }
        else
        {
            damageEffect.SetActive(false);
        }

        if (Health == 0 && MaxHealth != 0 && !Dead)
        {
            Die();
        }
    }

    /// <summary>
    /// Checks if the aircraft has lost StateAuthority and attempts to recover it via respawn.
    /// </summary>
    private void CheckAndRecoverAuthority()
    {
        if (Object.HasInputAuthority && !Object.HasStateAuthority)
        {
            Debug.LogWarning($"[!] {gameObject.name} perdió StateAuthority pero tiene InputAuthority. Intentando recuperar...");

            if (Runner.IsServer) // solo el servidor puede reasignar
            {
                Runner.Despawn(Object);

                var newPlane = Runner.Spawn(playerPlanePrefab, transform.position, transform.rotation, Object.InputAuthority);
                Debug.Log($"✅ StateAuthority reasignada a {Object.InputAuthority.PlayerId}");
            }
        }
    }

    /// <summary>
    /// Handles damage and destruction on collision with the environment.
    /// </summary>
    /// <param name="collision">Collision information from Unity's physics system.</param>
    void OnCollisionEnter(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            var contact = collision.contacts[i];

            if (landingGear.Contains(contact.thisCollider))
            {
                return;
            }

            Health = 0;

            Rigidbody.isKinematic = true;
            Rigidbody.position = contact.point;
            Rigidbody.rotation = Quaternion.Euler(0, Rigidbody.rotation.eulerAngles.y, 0);

            foreach (var go in graphics)
            {
                go.SetActive(false);
            }

            return;
        }
    }
}
