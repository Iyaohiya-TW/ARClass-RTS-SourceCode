using UnityEngine;

public class Projectile : MonoBehaviour
{
    private GameObject _target;
    public UnitEffect _effect;
    private float _speed = 10f;
    private bool _isHoming;
    private TeamTag _teamTag;

    private Vector3 _startPosition;
    private Vector3 _fixedDestination;
    private float _progress;
    private float _flightDuration;
    private bool _isInitialized = false; // Prevents Update from running too early

    [Header("Trajectory Settings")]
    public float arcHeight = 2f;

    public void Launch(GameObject target, UnitEffect effect, float speed, TeamTag teamTag, bool isHoming = true)
    {
        _target = target;
        _effect = effect;
        _speed = Mathf.Max(speed, 0.1f); // Safety: Speed cannot be 0
        _teamTag = teamTag;
        _isHoming = isHoming;

        _startPosition = transform.position;
        _progress = 0f;

        // If no target is provided, fly forward from current orientation
        if (target != null)
        {
            _fixedDestination = target.transform.position;
        }
        else
        {
            _fixedDestination = transform.position + transform.forward * 20f;
        }

        // Calculate duration safely
        float distance = Vector3.Distance(_startPosition, _fixedDestination);
        _flightDuration = distance / _speed;

        // Ensure duration is never 0 to avoid Division by Zero errors
        if (_flightDuration <= 0) _flightDuration = 0.1f;

        _isInitialized = true;

        // Failsafe destroy
        Destroy(gameObject, 10f);
    }

    void Update()
    {
        // Don't do anything until Launch() has been called
        if (!_isInitialized) return;

        // 1. Update progress
        _progress += Time.deltaTime / _flightDuration;
        float clampedProgress = Mathf.Clamp01(_progress);

        // 2. Determine end point (Homing or Fixed)
        Vector3 endPos = (_isHoming && _target != null) ? _target.transform.position : _fixedDestination;

        // 3. Calculate position with Arc
        Vector3 currentPos = Vector3.Lerp(_startPosition, endPos, clampedProgress);

        // Add the skyward curve
        float heightOffset = arcHeight * Mathf.Sin(clampedProgress * Mathf.PI);
        currentPos.y += heightOffset;

        // 4. Update Rotation (Look where we are going)
        Vector3 moveDirection = currentPos - transform.position;
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(moveDirection);
        }

        // 5. Update Position
        transform.position = currentPos;

        // Optional: If progress is finished and we didn't hit anything, destroy
        if (clampedProgress >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isInitialized) return;

        if (other.TryGetComponent<Projectile>(out var proj)) return;

        if (other.TryGetComponent<Unit>(out var unit))
        {
            if (unit.TeamTag == _teamTag) return; // Teammate: Ignore

            unit.RecieveEffect(_effect);
            Destroy(gameObject);
        }
        else
        {
            // You can add "Floor" or "Wall" tags here
            Destroy(gameObject);
        }
    }
}