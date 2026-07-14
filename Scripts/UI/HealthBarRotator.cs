using UnityEngine;

public class HealthBarRotator : MonoBehaviour
{
    private Quaternion _fixedRotation;

    void Start()
    {
        // Capture the starting rotation of the health bar canvas at spawn.
        // This ensures it points in the correct top-down direction you designed.
        _fixedRotation = transform.rotation;
    }

    void LateUpdate()
    {
        // Force the Canvas back to its initial rotation every frame, 
        // completely overriding the unit parent's spinning.
        transform.rotation = _fixedRotation;
    }
}