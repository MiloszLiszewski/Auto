
using UnityEngine;
using UnityEngine.InputSystem;

public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider rearLeft;
    public WheelCollider rearRight;

    [Header("Wheel Models")]
    public Transform frontLeftMesh;
    public Transform frontRightMesh;
    public Transform rearLeftMesh;
    public Transform rearRightMesh;

    [Header("Car Settings")]
    public float motorTorque = 2000f;
    public float brakeTorque = 3000f;
    public float maxSteerAngle = 30f;

    private Rigidbody rb;

    private Vector3 offsetFL, offsetFR, offsetRL, offsetRR;
    private Quaternion rotFL, rotFR, rotRL, rotRR;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("CarController: Brak Rigidbody na tym obiekcie!");
            enabled = false;
            return;
        }

        // Sprawdzenie przypisania wszystkich kół
        if (frontLeft == null || frontRight == null ||
            rearLeft == null || rearRight == null ||
            frontLeftMesh == null || frontRightMesh == null ||
            rearLeftMesh == null || rearRightMesh == null)
        {
            Debug.LogError("CarController: Nie wszystkie WheelCollidery lub modele kół zostały przypisane!");
            enabled = false;
            return;
        }

        rb.centerOfMass = new Vector3(0f, -0.5f, 0f);

        // Przygotowanie przesunięć modeli kół
        offsetFL = PrepareWheel(frontLeft, frontLeftMesh);
        offsetFR = PrepareWheel(frontRight, frontRightMesh);
        offsetRL = PrepareWheel(rearLeft, rearLeftMesh);
        offsetRR = PrepareWheel(rearRight, rearRightMesh);

        // Zachowanie początkowych różnic obrotu
        rotFL = Quaternion.Inverse(frontLeft.transform.rotation) * frontLeftMesh.rotation;
        rotFR = Quaternion.Inverse(frontRight.transform.rotation) * frontRightMesh.rotation;
        rotRL = Quaternion.Inverse(rearLeft.transform.rotation) * rearLeftMesh.rotation;
        rotRR = Quaternion.Inverse(rearRight.transform.rotation) * rearRightMesh.rotation;
    }

    Vector3 PrepareWheel(WheelCollider collider, Transform wheelMesh)
    {
        Renderer[] renderers = wheelMesh.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogWarning("CarController: Nie znaleziono Renderer dla modelu koła: " + wheelMesh.name);
            return Vector3.zero;
        }

        Bounds bounds = renderers[0].bounds;

        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        Vector3 center = bounds.center;

        return Quaternion.Inverse(wheelMesh.rotation) *
               (center - wheelMesh.position);
    }

    void FixedUpdate()
    {
        if (Keyboard.current == null)
            return;

        float vertical = 0f;
        float horizontal = 0f;

        // Odczyt sterowania
        if (Keyboard.current.wKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed)
            vertical += 1f;

        if (Keyboard.current.sKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed)
            vertical -= 1f;

        if (Keyboard.current.dKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed)
            horizontal += 1f;

        if (Keyboard.current.aKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed)
            horizontal -= 1f;

        // Skręcanie przednich kół
        frontLeft.steerAngle = horizontal * maxSteerAngle;
        frontRight.steerAngle = horizontal * maxSteerAngle;

        // Napęd na tylne koła
        rearLeft.motorTorque = vertical * motorTorque;
        rearRight.motorTorque = vertical * motorTorque;

        // Hamowanie
        float brake = 0f;

        if (Keyboard.current.spaceKey.isPressed)
            brake = brakeTorque;
        else if (vertical == 0f)
            brake = 600f;

        frontLeft.brakeTorque = brake;
        frontRight.brakeTorque = brake;
        rearLeft.brakeTorque = brake;
        rearRight.brakeTorque = brake;

        // Aktualizacja modeli kół
        UpdateWheel(frontLeft, frontLeftMesh, offsetFL, rotFL);
        UpdateWheel(frontRight, frontRightMesh, offsetFR, rotFR);
        UpdateWheel(rearLeft, rearLeftMesh, offsetRL, rotRL);
        UpdateWheel(rearRight, rearRightMesh, offsetRR, rotRR);

        // Odczyt prędkości
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log("Prędkość: " +
                      rb.linearVelocity.magnitude.ToString("F2") +
                      " m/s");
        }
    }

    void UpdateWheel(
        WheelCollider collider,
        Transform wheelMesh,
        Vector3 offset,
        Quaternion rotationOffset)
    {
        collider.GetWorldPose(out Vector3 position, out Quaternion rotation);

        wheelMesh.rotation = rotation * rotationOffset;
        wheelMesh.position = position - wheelMesh.rotation * offset;
    }
}
