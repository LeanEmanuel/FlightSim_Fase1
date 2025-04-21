using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the camera follow system for the player-controlled aircraft.
/// Smoothly interpolates position and rotation based on aircraft movement,
/// look input, and death state.
/// </summary>
public class PlaneCamera : MonoBehaviour
{
    [SerializeField]
    new Camera camera;
    [SerializeField]
    Vector3 cameraOffset;
    [SerializeField]
    Vector2 lookAngle;
    [SerializeField]
    float movementScale;
    [SerializeField]
    float lookAlpha;
    [SerializeField]
    float movementAlpha;
    [SerializeField]
    Vector3 deathOffset;
    [SerializeField]
    float deathSensitivity;

    Transform cameraTransform;
    Plane plane;
    Transform planeTransform;
    Vector2 lookInput;
    bool dead;

    Vector2 look;
    Vector2 lookAverage;
    Vector3 avAverage;

    /// <summary>
    /// Initializes references and disables the camera if this is not the local player.
    /// </summary>
    void Awake()
    {
        cameraTransform = camera.GetComponent<Transform>();

        // Desactiva la cámara si no es el jugador local
        var netObj = GetComponentInParent<Fusion.NetworkObject>();
        if (netObj != null && !netObj.HasInputAuthority)
        {
            camera.gameObject.SetActive(false); // Desactiva la cámara si no tiene autoridad
            enabled = false;                    // Opcional: desactiva el script
        }
    }

    /// <summary>
    /// Assigns the plane to follow and sets the camera's initial transform.
    /// </summary>
    /// <param name="plane">Plane object to follow.</param>
    public void SetPlane(Plane plane)
    {
        this.plane = plane;

        if (plane == null)
        {
            planeTransform = null;
        }
        else
        {
            planeTransform = plane.GetComponent<Transform>();
        }

        cameraTransform.SetParent(planeTransform);

        // Aquí forzamos la posición y rotación inicial
        var initialRotation = Quaternion.Euler(-lookAverage.y, lookAverage.x, 0);
        var turningRotation = Quaternion.Euler(Vector3.zero); // aún no hay movimiento
        cameraTransform.localPosition = initialRotation * turningRotation * cameraOffset;
        cameraTransform.localRotation = initialRotation * turningRotation;
    }

    /// <summary>
    /// Sets the current camera look input (e.g., from mouse or stick).
    /// </summary>
    /// <param name="input">Look direction input vector.</param>
    public void SetInput(Vector2 input)
    {
        lookInput = input;
    }

    /// <summary>
    /// Applies smoothed camera movement and rotation every frame,
    /// following the aircraft and reacting to its angular velocity.
    /// </summary>
    void LateUpdate()
    {
        if (plane == null) return;

        var cameraOffset = this.cameraOffset;

        if (plane.Dead)
        {
            look += lookInput * deathSensitivity * Time.deltaTime;
            look.x = (look.x + 360f) % 360f;
            look.y = Mathf.Clamp(look.y, -lookAngle.y, lookAngle.y);

            lookAverage = look;
            avAverage = new Vector3();

            cameraOffset = deathOffset;
        }
        else
        {
            var targetLookAngle = Vector2.Scale(lookInput, lookAngle);
            lookAverage = (lookAverage * (1 - lookAlpha)) + (targetLookAngle * lookAlpha);

            var angularVelocity = plane.LocalAngularVelocity;
            angularVelocity.z = -angularVelocity.z;

            avAverage = (avAverage * (1 - movementAlpha)) + (angularVelocity * movementAlpha);
        }

        var rotation = Quaternion.Euler(-lookAverage.y, lookAverage.x, 0);  //get rotation from camera input
        var turningRotation = Quaternion.Euler(new Vector3(-avAverage.x, -avAverage.y, avAverage.z) * movementScale);   //get rotation from plane's AV

        cameraTransform.localPosition = rotation * turningRotation * cameraOffset;  //calculate camera position;
        cameraTransform.localRotation = rotation * turningRotation;                 //calculate camera rotation
    }
}
