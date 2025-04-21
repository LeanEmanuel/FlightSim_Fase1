using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the in-game Head-Up Display (HUD) for the player's aircraft,
/// including visual indicators for speed, altitude, G-force, angle of attack,
/// targeting, missile lock, health, and warning systems.
/// </summary>
public class PlaneHUD : MonoBehaviour
{
    [SerializeField]
    float updateRate;
    [SerializeField]
    Color normalColor;
    [SerializeField]
    Color lockColor;
    [SerializeField]
    List<GameObject> helpDialogs;
    [SerializeField]
    Compass compass;
    [SerializeField]
    PitchLadder pitchLadder;
    [SerializeField]
    Bar throttleBar;
    [SerializeField]
    Transform hudCenter;
    [SerializeField]
    Transform velocityMarker;
    [SerializeField]
    Text airspeed;
    [SerializeField]
    Text aoaIndicator;
    [SerializeField]
    Text gforceIndicator;
    [SerializeField]
    Text altitude;
    [SerializeField]
    Bar healthBar;
    [SerializeField]
    Text healthText;
    [SerializeField]
    Transform targetBox;
    [SerializeField]
    Text targetName;
    [SerializeField]
    Text targetRange;
    [SerializeField]
    Transform missileLock;
    [SerializeField]
    Transform reticle;
    [SerializeField]
    RectTransform reticleLine;
    [SerializeField]
    RectTransform targetArrow;
    [SerializeField]
    RectTransform missileArrow;
    [SerializeField]
    float targetArrowThreshold;
    [SerializeField]
    float missileArrowThreshold;
    [SerializeField]
    float cannonRange;
    [SerializeField]
    float bulletSpeed;
    [SerializeField]
    GameObject aiMessage;

    [SerializeField]
    List<Graphic> missileWarningGraphics;

    Plane plane;
    AIController aiController;
    Target selfTarget;
    Transform planeTransform;
    new Camera camera;
    Transform cameraTransform;

    GameObject hudCenterGO;
    GameObject velocityMarkerGO;
    GameObject targetBoxGO;
    Image targetBoxImage;
    GameObject missileLockGO;
    Image missileLockImage;
    GameObject reticleGO;
    GameObject targetArrowGO;
    GameObject missileArrowGO;

    float lastUpdateTime;

    const float metersToKnots = 1.94384f;
    const float metersToFeet = 3.28084f;

    void Start()
    {
        hudCenterGO = hudCenter.gameObject;
        velocityMarkerGO = velocityMarker.gameObject;
        targetBoxGO = targetBox.gameObject;
        targetBoxImage = targetBox.GetComponent<Image>();
        missileLockGO = missileLock.gameObject;
        missileLockImage = missileLock.GetComponent<Image>();
        reticleGO = reticle.gameObject;
        targetArrowGO = targetArrow.gameObject;
        missileArrowGO = missileArrow.gameObject;
    }

    /// <summary>
    /// Sets the plane to associate with this HUD.
    /// Updates references and visual elements accordingly.
    /// </summary>
    /// <param name="plane">The Plane object to link to the HUD.</param>
    public void SetPlane(Plane plane)
    {
        this.plane = plane;

        if (plane == null)
        {
            planeTransform = null;
            selfTarget = null;
        }
        else
        {
            aiController = plane.GetComponent<AIController>();
            planeTransform = plane.GetComponent<Transform>();
            selfTarget = plane.GetComponent<Target>();
        }

        if (compass != null)
        {
            compass.SetPlane(plane);
        }

        if (pitchLadder != null)
        {
            pitchLadder.SetPlane(plane);
        }
    }

    /// <summary>
    /// Sets the camera used for HUD rendering and position calculations.
    /// </summary>
    /// <param name="camera">The Camera to associate with the HUD.</param>
    public void SetCamera(Camera camera)
    {
        this.camera = camera;

        if (camera == null)
        {
            cameraTransform = null;
        }
        else
        {
            cameraTransform = camera.GetComponent<Transform>();
        }

        if (compass != null)
        {
            compass.SetCamera(camera);
        }

        if (pitchLadder != null)
        {
            pitchLadder.SetCamera(camera);
        }
    }

    /// <summary>
    /// Toggles the visibility of all help dialog overlays.
    /// </summary>
    public void ToggleHelpDialogs()
    {
        foreach (var dialog in helpDialogs)
        {
            dialog.SetActive(!dialog.activeSelf);
        }
    }

    /// <summary>
    /// Updates the visual position of the velocity marker based on aircraft motion.
    /// </summary>
    void UpdateVelocityMarker()
    {
        var velocity = planeTransform.forward;

        if (plane.LocalVelocity.sqrMagnitude > 1)
        {
            velocity = plane.Rigidbody.linearVelocity;
        }

        var hudPos = TransformToHUDSpace(cameraTransform.position + velocity);

        if (hudPos.z > 0)
        {
            velocityMarkerGO.SetActive(true);
            velocityMarker.localPosition = new Vector3(hudPos.x, hudPos.y, 0);
        }
        else
        {
            velocityMarkerGO.SetActive(false);
        }
    }

    /// <summary>
    /// Updates the airspeed display based on the aircraft's local forward velocity.
    /// </summary>
    void UpdateAirspeed()
    {
        var speed = plane.LocalVelocity.z * metersToKnots;
        airspeed.text = string.Format("{0:0}", speed);
    }

    /// <summary>
    /// Updates the angle of attack (AOA) indicator in degrees.
    /// </summary>
    void UpdateAOA()
    {
        aoaIndicator.text = string.Format("{0:0.0} AOA", plane.AngleOfAttack * Mathf.Rad2Deg);
    }

    /// <summary>
    /// Updates the G-force indicator based on local Y-axis acceleration.
    /// </summary>
    void UpdateGForce()
    {
        var gforce = plane.LocalGForce.y / 9.81f;
        gforceIndicator.text = string.Format("{0:0.0} G", gforce);
    }

    /// <summary>
    /// Updates the altitude readout based on aircraft's world Y position.
    /// </summary>
    void UpdateAltitude()
    {
        var altitude = plane.Rigidbody.position.y * metersToFeet;
        this.altitude.text = string.Format("{0:0}", altitude);
    }

    /// <summary>
    /// Converts a world position to HUD-relative coordinates.
    /// </summary>
    /// <param name="worldSpace">World space position to convert.</param>
    /// <returns>HUD-local position in screen space.</returns>
    Vector3 TransformToHUDSpace(Vector3 worldSpace)
    {
        var screenSpace = camera.WorldToScreenPoint(worldSpace);
        return screenSpace - new Vector3(camera.pixelWidth / 2, camera.pixelHeight / 2);
    }

    /// <summary>
    /// Updates the crosshair position (HUD center) based on camera orientation and aircraft forward vector.
    /// </summary>
    void UpdateHUDCenter()
    {
        var rotation = cameraTransform.localEulerAngles;
        var hudPos = TransformToHUDSpace(cameraTransform.position + planeTransform.forward);

        if (hudPos.z > 0)
        {
            hudCenterGO.SetActive(true);
            hudCenter.localPosition = new Vector3(hudPos.x, hudPos.y, 0);
            hudCenter.localEulerAngles = new Vector3(0, 0, -rotation.z);
        }
        else
        {
            hudCenterGO.SetActive(false);
        }
    }

    /// <summary>
    /// Updates the health bar and text according to the plane's health values.
    /// </summary>
    void UpdateHealth()
    {
        healthBar.SetValue(plane.Health / plane.MaxHealth);
        healthText.text = string.Format("{0:0}", plane.Health);
    }

    /// <summary>
    /// Updates all weapon-related HUD elements including target box, missile lock, reticle, etc.
    /// </summary>
    void UpdateWeapons()
    {
        if (plane.Target == null)
        {
            targetBoxGO.SetActive(false);
            missileLockGO.SetActive(false);
            return;
        }

        //update target box, missile lock
        var targetDistance = Vector3.Distance(plane.Rigidbody.position, plane.Target.Position);
        var targetPos = TransformToHUDSpace(plane.Target.Position);
        var missileLockPos = plane.MissileLocked ? targetPos : TransformToHUDSpace(plane.Rigidbody.position + plane.MissileLockDirection * targetDistance);

        if (targetPos.z > 0)
        {
            targetBoxGO.SetActive(true);
            targetBox.localPosition = new Vector3(targetPos.x, targetPos.y, 0);
        }
        else
        {
            targetBoxGO.SetActive(false);
        }

        if (plane.MissileTracking && missileLockPos.z > 0)
        {
            missileLockGO.SetActive(true);
            missileLock.localPosition = new Vector3(missileLockPos.x, missileLockPos.y, 0);
        }
        else
        {
            missileLockGO.SetActive(false);
        }

        if (plane.MissileLocked)
        {
            targetBoxImage.color = lockColor;
            targetName.color = lockColor;
            targetRange.color = lockColor;
            missileLockImage.color = lockColor;
        }
        else
        {
            targetBoxImage.color = normalColor;
            targetName.color = normalColor;
            targetRange.color = normalColor;
            missileLockImage.color = normalColor;
        }

        targetName.text = plane.Target.Name;
        targetRange.text = string.Format("{0:0 m}", targetDistance);

        //update target arrow
        var targetDir = (plane.Target.Position - plane.Rigidbody.position).normalized;
        var targetAngle = Vector3.Angle(cameraTransform.forward, targetDir);

        if (targetAngle > targetArrowThreshold)
        {
            targetArrowGO.SetActive(true);
            //add 180 degrees if target is behind camera
            float flip = targetPos.z > 0 ? 0 : 180;
            targetArrow.localEulerAngles = new Vector3(0, 0, flip + Vector2.SignedAngle(Vector2.up, new Vector2(targetPos.x, targetPos.y)));
        }
        else
        {
            targetArrowGO.SetActive(false);
        }

        //update target lead
        var leadPos = Utilities.FirstOrderIntercept(plane.Rigidbody.position, plane.Rigidbody.linearVelocity, bulletSpeed, plane.Target.Position, plane.Target.Velocity);
        var reticlePos = TransformToHUDSpace(leadPos);

        if (reticlePos.z > 0 && targetDistance <= cannonRange)
        {
            reticleGO.SetActive(true);
            reticle.localPosition = new Vector3(reticlePos.x, reticlePos.y, 0);

            var reticlePos2 = new Vector2(reticlePos.x, reticlePos.y);
            if (Mathf.Sign(targetPos.z) != Mathf.Sign(reticlePos.z)) reticlePos2 = -reticlePos2;    //negate position if reticle and target are on opposite sides
            var targetPos2 = new Vector2(targetPos.x, targetPos.y);
            var reticleError = reticlePos2 - targetPos2;

            var lineAngle = Vector2.SignedAngle(Vector3.up, reticleError);
            reticleLine.localEulerAngles = new Vector3(0, 0, lineAngle + 180f);
            reticleLine.sizeDelta = new Vector2(reticleLine.sizeDelta.x, reticleError.magnitude);
        }
        else
        {
            reticleGO.SetActive(false);
        }
    }

    /// <summary>
    /// Updates the directional warning indicators if the aircraft is being tracked by a missile.
    /// </summary>
    void UpdateWarnings()
    {
        if (selfTarget == null || plane == null || plane.Rigidbody == null || cameraTransform == null) return;
        var incomingMissile = selfTarget.GetIncomingMissile();

        if (incomingMissile != null)
        {
            var missilePos = TransformToHUDSpace(incomingMissile.Rigidbody.position);
            var missileDir = (incomingMissile.Rigidbody.position - plane.Rigidbody.position).normalized;
            var missileAngle = Vector3.Angle(cameraTransform.forward, missileDir);

            if (missileAngle > missileArrowThreshold)
            {
                missileArrowGO.SetActive(true);
                //add 180 degrees if target is behind camera
                float flip = missilePos.z > 0 ? 0 : 180;
                missileArrow.localEulerAngles = new Vector3(0, 0, flip + Vector2.SignedAngle(Vector2.up, new Vector2(missilePos.x, missilePos.y)));
            }
            else
            {
                missileArrowGO.SetActive(false);
            }

            foreach (var graphic in missileWarningGraphics)
            {
                graphic.color = lockColor;
            }

            pitchLadder.UpdateColor(lockColor);
            compass.UpdateColor(lockColor);
        }
        else
        {
            missileArrowGO.SetActive(false);

            foreach (var graphic in missileWarningGraphics)
            {
                graphic.color = normalColor;
            }

            pitchLadder.UpdateColor(normalColor);
            compass.UpdateColor(normalColor);
        }
    }

    /// <summary>
    /// Updates the health bar UI externally, used with OnChangedRender on health changes.
    /// </summary>
    /// <param name="current">Current health.</param>
    /// <param name="max">Maximum health.</param>
    public void UpdateHealthBar(float current, float max)
    {
        if (healthBar != null)
            healthBar.SetValue(current / max);

        if (healthText != null)
            healthText.text = $"{current:0}";
    }

    /// <summary>
    /// Main update loop. Updates all HUD elements each frame, including HUD center, speed, altitude, and targeting.
    /// </summary>
    void LateUpdate()
    {
        if (plane == null || camera == null) return;
        if (plane.Rigidbody == null || planeTransform == null || cameraTransform == null) return;

        float degreesToPixels = camera.pixelHeight / camera.fieldOfView;

        throttleBar.SetValue(plane.Throttle);

        if (!plane.Dead)
        {
            UpdateVelocityMarker();
            UpdateHUDCenter();
        }
        else
        {
            hudCenterGO.SetActive(false);
            velocityMarkerGO.SetActive(false);
        }

        if (aiController != null)
        {
            aiMessage.SetActive(aiController.enabled);
        }

        UpdateAirspeed();
        UpdateAltitude();
        UpdateHealth();
        UpdateWeapons();
        UpdateWarnings();

        //update these elements at reduced rate to make reading them easier
        if (Time.time > lastUpdateTime + (1f / updateRate))
        {
            UpdateAOA();
            UpdateGForce();
            lastUpdateTime = Time.time;
        }
    }
}
